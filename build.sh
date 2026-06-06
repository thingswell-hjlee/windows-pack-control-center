#!/usr/bin/env bash
# build.sh - Windows Pack Control Center Build Script (Linux/CI)
# Builds the React frontend and publishes the .NET gateway as a self-contained win-x64 application.
# Output: dist/ folder containing the complete deployment package.

set -e

CONFIGURATION="${1:-Release}"
RUNTIME="${2:-win-x64}"
OUTPUT_DIR="${3:-dist}"

PROJECT_ROOT="$(cd "$(dirname "$0")" && pwd)"
WEB_DIR="$PROJECT_ROOT/web"
GATEWAY_DIR="$PROJECT_ROOT/gateway/src"
OUTPUT_PATH="$PROJECT_ROOT/$OUTPUT_DIR"

echo "======================================"
echo " Control Center Build Script"
echo "======================================"
echo ""

# Step 1: Clean output directory
echo "[1/3] Cleaning output directory..."
if [ -d "$OUTPUT_PATH" ]; then
    rm -rf "$OUTPUT_PATH"
fi

# Step 2: Build React frontend
echo "[2/3] Building React frontend..."
cd "$WEB_DIR"
echo "  Installing npm dependencies..."
npm install
echo "  Building frontend..."
npm run build
echo "  Frontend built successfully."

# Step 3: Publish .NET Gateway
echo "[3/3] Publishing .NET Gateway ($CONFIGURATION, $RUNTIME, self-contained)..."
cd "$GATEWAY_DIR"
dotnet publish -c "$CONFIGURATION" -r "$RUNTIME" --self-contained -o "$OUTPUT_PATH"
echo "  Gateway published successfully."

# Summary
echo ""
echo "======================================"
echo " Build Complete!"
echo "======================================"
echo ""
echo "Output: $OUTPUT_PATH"
echo ""
echo "To run the application:"
echo "  cd $OUTPUT_DIR"
echo "  ./ControlCenter.Gateway.exe"
echo ""
echo "Access the dashboard at: http://localhost:8088"
