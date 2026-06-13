#!/usr/bin/env bash
# build.sh - Windows Pack Control Center Build Script (Linux/CI)
# Builds the React frontend, copies to gateway wwwroot, and publishes as a self-contained single-file win-x64 application.
# Output: dist/ folder containing the complete deployment package.
# Startup time target: < 5 seconds (self-contained single-file exe)
# Note: Cross-compiles for Windows (win-x64) even when run on Linux.

set -e

CONFIGURATION="${1:-Release}"
RUNTIME="${2:-win-x64}"
OUTPUT_DIR="${3:-dist}"

PROJECT_ROOT="$(cd "$(dirname "$0")" && pwd)"
WEB_DIR="$PROJECT_ROOT/web"
GATEWAY_DIR="$PROJECT_ROOT/gateway/src"
WWWROOT_DIR="$GATEWAY_DIR/wwwroot"
OUTPUT_PATH="$PROJECT_ROOT/$OUTPUT_DIR"

echo "======================================"
echo " Control Center Build Script"
echo "======================================"
echo ""

# Step 1: Clean output directory
echo "[1/4] Cleaning output directory..."
if [ -d "$OUTPUT_PATH" ]; then
    rm -rf "$OUTPUT_PATH"
fi

# Step 2: Build React frontend
echo "[2/4] Building React frontend..."
cd "$WEB_DIR"
echo "  Installing npm dependencies..."
npm install
echo "  Building frontend..."
npm run build
echo "  Frontend built successfully."

# Step 3: Copy frontend build to gateway wwwroot
echo "[3/4] Copying frontend assets to gateway/src/wwwroot/..."
WEB_DIST_DIR="$WEB_DIR/dist"
if [ ! -d "$WEB_DIST_DIR" ]; then
    echo "ERROR: Frontend build output not found at $WEB_DIST_DIR"
    exit 1
fi
# Clean existing wwwroot and copy fresh build
if [ -d "$WWWROOT_DIR" ]; then
    rm -rf "$WWWROOT_DIR"
fi
cp -r "$WEB_DIST_DIR" "$WWWROOT_DIR"
echo "  Frontend assets copied to wwwroot."

# Step 4: Publish .NET Gateway (self-contained single-file, cross-compile for Windows)
echo "[4/4] Publishing .NET Gateway ($CONFIGURATION, $RUNTIME, self-contained, single-file)..."
cd "$GATEWAY_DIR"
dotnet publish -c "$CONFIGURATION" -r "$RUNTIME" --self-contained -p:PublishSingleFile=true -o "$OUTPUT_PATH"
echo "  Gateway published successfully."

# Summary
echo ""
echo "======================================"
echo " Build Complete!"
echo "======================================"
echo ""
echo "Output: $OUTPUT_PATH"
echo "  - Single-file executable: ControlCenter.Gateway.exe"
echo "  - Self-contained: No .NET runtime required on target machine"
echo "  - Target: Windows x64 (cross-compiled)"
echo "  - Startup time target: < 5 seconds"
echo ""
echo "To run the application (on Windows):"
echo "  cd $OUTPUT_DIR"
echo "  ./ControlCenter.Gateway.exe"
echo ""
echo "Access the dashboard at: http://localhost:8088"
