#!/usr/bin/env python3
"""Static file server for ServerData/ with HTTP Range support.

Python's built-in http.server ignores Range headers. AVFoundation (iOS video playback)
requires byte-range responses (206 Partial Content) to stream MP4 files, so this
server adds them. Development use only; production content belongs on an HTTPS CDN.

Usage: serve_content.py <directory> [port]
"""
import os
import re
import sys
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer

RANGE = re.compile(r"bytes=(\d*)-(\d*)$")


class RangeRequestHandler(SimpleHTTPRequestHandler):
    def send_head(self):
        header = self.headers.get("Range")
        path = self.translate_path(self.path)
        if not header or not os.path.isfile(path):
            return super().send_head()

        match = RANGE.match(header.strip())
        size = os.path.getsize(path)
        if not match or size == 0:
            self.send_error(416, "Invalid range")
            return None

        first, last = match.groups()
        if first:
            start, end = int(first), int(last) if last else size - 1
        else:  # suffix range: last N bytes
            start, end = max(0, size - int(last)), size - 1
        end = min(end, size - 1)
        if start > end:
            self.send_response(416)
            self.send_header("Content-Range", f"bytes */{size}")
            self.end_headers()
            return None

        f = open(path, "rb")
        f.seek(start)
        self.send_response(206)
        self.send_header("Content-Type", self.guess_type(path))
        self.send_header("Content-Range", f"bytes {start}-{end}/{size}")
        self.send_header("Content-Length", str(end - start + 1))
        self.send_header("Accept-Ranges", "bytes")
        self.end_headers()
        self.range_remaining = end - start + 1
        return f

    def end_headers(self):
        if not self.headers.get("Range"):
            self.send_header("Accept-Ranges", "bytes")
        super().end_headers()

    def copyfile(self, source, outputfile):
        # Video players routinely close a range request once they have the bytes they need.
        try:
            remaining = getattr(self, "range_remaining", None)
            if remaining is None:
                return super().copyfile(source, outputfile)
            while remaining > 0:
                chunk = source.read(min(64 * 1024, remaining))
                if not chunk:
                    break
                outputfile.write(chunk)
                remaining -= len(chunk)
        except (BrokenPipeError, ConnectionResetError):
            self.close_connection = True


if __name__ == "__main__":
    directory = sys.argv[1]
    port = int(sys.argv[2]) if len(sys.argv) > 2 else 8000
    handler = partial(RangeRequestHandler, directory=directory)
    print(f"Serving {directory} at http://localhost:{port}  (Ctrl+C to stop)", flush=True)
    ThreadingHTTPServer(("", port), handler).serve_forever()
