#!/bin/bash

# LisTube Avalonia Run Script
# This script runs the Avalonia version of LisTube

set -e

echo "================================"
echo "  LisTube - Run Script"
echo "================================"
echo ""

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Check if published version exists
if [ -f "$SCRIPT_DIR/publish-avalonia/LisTube" ]; then
    echo "Running LisTube..."
    cd "$SCRIPT_DIR/publish-avalonia"
    ./LisTube "$@"
# Check if development build exists
elif [ -f "$SCRIPT_DIR/LisTube.Avalonia/bin/Release/net8.0/LisTube" ]; then
    echo "Running LisTube (development build)..."
    cd "$SCRIPT_DIR/LisTube.Avalonia/bin/Release/net8.0"
    ./LisTube "$@"
elif command -v dotnet >/dev/null 2>&1; then
    echo "Running LisTube directly via dotnet run..."
    cd "$SCRIPT_DIR/LisTube.Avalonia"
    dotnet run "$@"
else
    echo "LisTube executable not found!"
    echo ""
    echo "Please build the project first:"
    echo "  ./build-avalonia.sh"
    echo ""
    exit 1
fi
