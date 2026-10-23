#!/usr/bin/env bash
# Creates iOS-safe fallback renditions of the feed videos in ServerData/videos/.
# Why: the source files are not all mobile-safe. pv-video.mp4 is H.264 High 4:4:4,
# which iOS cannot decode; the others are HEVC 1080p-1440p, up to 60 fps and 23 Mbps,
# and none are "fast-start" (moov atom at the end of the file).
# Output: H.264 High 4:2:0, max 720x1280, 30 fps, ~2.5 Mbps, AAC, +faststart.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="$ROOT/ServerData/videos"
mkdir -p "$OUT"
command -v ffmpeg >/dev/null || { echo "ffmpeg missing: brew install ffmpeg" >&2; exit 1; }

SOURCES=(
  "corestorm-mecha-arena https://assets.elseland.elser.ai/games/corestorm-mecha-arena/pv-6s.mp4"
  "gothic-blast https://assets.elseland.elser.ai/games/gothic-blast/pv-video.mp4"
  "emoji-cube-match-3d https://assets.elseland.elser.ai/games/emoji-cube-match-3d/emoji-cube-match-3d-9-16-5s.mp4"
)

for entry in "${SOURCES[@]}"; do
  name="${entry%% *}"
  url="${entry#* }"
  target="$OUT/$name.mp4"
  if [[ -s "$target" && "${FORCE:-0}" != "1" ]]; then
    echo "skip $name (exists, FORCE=1 to redo)"
    continue
  fi
  echo "==> $name"
  ffmpeg -hide_banner -loglevel error -y -i "$url" \
    -vf "scale='trunc(min(720,iw)/2)*2':-2,fps=30,format=yuv420p" \
    -c:v libx264 -profile:v high -preset medium -crf 23 -maxrate 2500k -bufsize 5000k \
    -c:a aac -b:a 96k -ac 2 \
    -movflags +faststart "$target"
done
ls -lh "$OUT"
