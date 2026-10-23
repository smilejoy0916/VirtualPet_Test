#!/usr/bin/env bash
# Serves the Addressables build output (ServerData/) on http://localhost:8000 for local testing.
# The iOS Simulator shares the Mac's network, so the app reaches it at localhost.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PORT="${1:-8000}"

if [[ ! -d "$ROOT/ServerData" ]]; then
  echo "ServerData/ not found. Build content first (VirtualPet > Content > Build Remote Content)." >&2
  exit 1
fi

# Range-capable server: iOS video playback needs 206 Partial Content responses.
exec python3 "$ROOT/Tools/serve_content.py" "$ROOT/ServerData" "$PORT"
