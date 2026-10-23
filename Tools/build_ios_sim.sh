#!/usr/bin/env bash
# Builds remote content + the iOS Simulator Xcode project with Unity, compiles it with
# xcodebuild, then installs and launches it on a simulator.
#   Tools/build_ios_sim.sh ["iPhone 17"]
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
VERSION="$(awk '/m_EditorVersion:/ {print $2}' "$ROOT/ProjectSettings/ProjectVersion.txt")"
UNITY="${UNITY_PATH:-/Applications/Unity/Hub/Editor/$VERSION/Unity.app/Contents/MacOS/Unity}"
DEVICE="${1:-iPhone 17}"
BUNDLE_ID="com.virtualpet.poc"
XCODE_PROJECT="$ROOT/Builds/iOS-Simulator/Unity-iPhone.xcodeproj"
DERIVED="$ROOT/Builds/DerivedData"

echo "==> Unity $VERSION: building content + Xcode project"
"$UNITY" -batchmode -nographics -quit -projectPath "$ROOT" -buildTarget iOS \
  -executeMethod VirtualPet.EditorTools.BuildScripts.BuildIosSimulatorCli \
  -logFile "$ROOT/Builds/unity-build.log"

echo "==> Feed video fallback renditions"
"$ROOT/Tools/transcode_videos.sh"

echo "==> xcodebuild (iphonesimulator)"
xcodebuild -project "$XCODE_PROJECT" -scheme Unity-iPhone -configuration Release \
  -sdk iphonesimulator -destination "generic/platform=iOS Simulator" \
  -derivedDataPath "$DERIVED" CODE_SIGNING_ALLOWED=NO build | tail -n 5

APP="$(find "$DERIVED/Build/Products/Release-iphonesimulator" -maxdepth 1 -name '*.app' | head -n 1)"
echo "==> Installing $APP on $DEVICE"
xcrun simctl boot "$DEVICE" 2>/dev/null || true
open -a Simulator
xcrun simctl install "$DEVICE" "$APP"
xcrun simctl launch "$DEVICE" "$BUNDLE_ID"
echo "Launched. Record with: xcrun simctl io \"$DEVICE\" recordVideo demo.mp4"
