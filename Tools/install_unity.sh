#!/usr/bin/env bash
# Installs the pinned Unity Editor + iOS Build Support through the Unity Hub CLI.
# Needs ~15 GB free disk and a Unity account signed in to Unity Hub (Personal licence is fine).
set -euo pipefail

VERSION="6000.6.7f1"
CHANGESET="b22767f04f04"
HUB="/Applications/Unity Hub.app/Contents/MacOS/Unity Hub"

if [[ ! -x "$HUB" ]]; then
  echo "Unity Hub not found. Install it with: brew install --cask unity-hub" >&2
  exit 1
fi

FREE_GB=$(df -g /System/Volumes/Data | awk 'NR==2 {print $4}')
if (( FREE_GB < 15 )); then
  echo "Only ${FREE_GB} GB free; Unity ${VERSION} + iOS module needs ~15 GB." >&2
  exit 1
fi

"$HUB" -- --headless install --version "$VERSION" --changeset "$CHANGESET" --architecture arm64 --module ios
echo "Installed: /Applications/Unity/Hub/Editor/${VERSION}/Unity.app"
