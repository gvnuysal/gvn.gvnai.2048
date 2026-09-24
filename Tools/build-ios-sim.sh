#!/usr/bin/env bash
# Builds the game for the iOS Simulator and launches it on a booted simulator.
# Usage: Tools/build-ios-sim.sh [--setup] [simulator-udid]
#   --setup   regenerate scene/prefab/player settings first (2048 / Setup Project)
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
UNITY="${UNITY:-/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity}"
export DEVELOPER_DIR="${DEVELOPER_DIR:-/Applications/Xcode.app/Contents/Developer}"
BUNDLE_ID="com.gvnai.game2048"
LOGS="$ROOT/Logs"
mkdir -p "$LOGS"

if [[ "${1:-}" == "--setup" ]]; then
  shift
  "$UNITY" -batchmode -quit -nographics -projectPath "$ROOT" \
    -executeMethod Game2048.Editor.ProjectBootstrap.SetupProject -logFile "$LOGS/setup.log"
fi

DEVICE="${1:-booted}"

"$UNITY" -batchmode -quit -nographics -projectPath "$ROOT" \
  -executeMethod Game2048.Editor.BuildScripts.BuildiOSSimulator -logFile "$LOGS/ios-build.log"

xcodebuild -project "$ROOT/Builds/iOS-Sim/Unity-iPhone.xcodeproj" -scheme Unity-iPhone \
  -sdk iphonesimulator -configuration Debug -derivedDataPath "$ROOT/Builds/iOS-Sim-DerivedData" \
  build > "$LOGS/xcodebuild.log" 2>&1 || { tail -40 "$LOGS/xcodebuild.log"; exit 1; }

APP="$ROOT/Builds/iOS-Sim-DerivedData/Build/Products/Debug-iphonesimulator/2048.app"
xcrun simctl terminate "$DEVICE" "$BUNDLE_ID" 2>/dev/null || true
xcrun simctl install "$DEVICE" "$APP"
xcrun simctl launch "$DEVICE" "$BUNDLE_ID"
echo "Launched $BUNDLE_ID on $DEVICE"
