#!/bin/bash
set -euo pipefail

# Package neverway-devtools for release.
# Usage: ./scripts/package.sh <game-dir> [version]
#
# Example:
#   ./scripts/package.sh "/path/to/Neverway" v0.1.0

GAME_DIR="${1:?Usage: $0 <game-dir> [version]}"
VERSION="${2:-dev}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
MODDED_DIR="$GAME_DIR/.modded"

if [ ! -d "$MODDED_DIR" ]; then
    echo "ERROR: $MODDED_DIR not found. Run murder-mod-install on the game first."
    exit 1
fi

echo "Building NeverwayMod.DevTools..."
dotnet build "$PROJECT_DIR" -c Release -p:GameAssemblyPath="$MODDED_DIR"

# Find ImGui.NET managed DLL from NuGet cache
IMGUI_DLL=$(find ~/.nuget/packages/imgui.net -name "ImGui.NET.dll" -path "*/net8.0/*" 2>/dev/null | sort -V | tail -1)
if [ -z "$IMGUI_DLL" ]; then
    echo "ERROR: ImGui.NET.dll not found in NuGet cache. Run dotnet restore first."
    exit 1
fi

# Find ImGui native libs (at package root, not under lib/)
IMGUI_PKG_DIR=$(echo "$IMGUI_DLL" | sed 's|/lib/.*||')
IMGUI_RUNTIMES="$IMGUI_PKG_DIR/runtimes"

# Platform name : native lib path relative to runtimes/
PLATFORMS=(
    "macos-universal:osx/native/libcimgui.dylib"
    "windows-x64:win-x64/native/cimgui.dll"
    "windows-arm64:win-arm64/native/cimgui.dll"
    "linux-x64:linux-x64/native/libcimgui.so"
)

echo "Packaging release zips..."
for entry in "${PLATFORMS[@]}"; do
    PLATFORM="${entry%%:*}"
    NATIVE_PATH="${entry#*:}"
    NATIVE_FILE="$IMGUI_RUNTIMES/$NATIVE_PATH"

    if [ ! -f "$NATIVE_FILE" ]; then
        echo "  SKIP $PLATFORM (native lib not found: $NATIVE_FILE)"
        continue
    fi

    DIST="$PROJECT_DIR/dist/$PLATFORM/devtools"
    rm -rf "$DIST"
    mkdir -p "$DIST"

    cp "$PROJECT_DIR/mod.yaml" "$DIST/"
    cp "$PROJECT_DIR/bin/Release/net8.0/NeverwayMod.DevTools.dll" "$DIST/"
    cp "$PROJECT_DIR/bin/Release/net8.0/NeverwayMod.DevTools.pdb" "$DIST/"
    cp "$IMGUI_DLL" "$DIST/"
    cp "$NATIVE_FILE" "$DIST/"

    ZIP="$PROJECT_DIR/dist/neverway-devtools-${VERSION}-${PLATFORM}.zip"
    (cd "$PROJECT_DIR/dist/$PLATFORM" && zip -r "$ZIP" devtools/)
    echo "  $ZIP"
done

# Clean up platform dirs, keep zips
for entry in "${PLATFORMS[@]}"; do
    rm -rf "$PROJECT_DIR/dist/${entry%%:*}"
done
echo "Done. Zips in $PROJECT_DIR/dist/"
