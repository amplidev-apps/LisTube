#!/bin/bash

# LisTube Run Script
# This script runs the LisTube application (Windows only via Wine) or builds it

set -e

echo "================================"
echo "  LisTube - Run Script"
echo "================================"
echo ""

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

# Check if running on Windows (WSL) or Linux
if [[ "$OSTYPE" == "msys" ]] || [[ "$OSTYPE" == "cygwin" ]] || [[ -n "$WSL_DISTRO_NAME" ]]; then
    # Windows or WSL environment
    if [[ -f "$SCRIPT_DIR/publish/LisTube.exe" ]]; then
        echo "Running LisTube..."
        "$SCRIPT_DIR/publish/LisTube.exe" &
    elif [[ -f "$SCRIPT_DIR/LisTube/bin/Release/net8.0-windows/LisTube.exe" ]]; then
        echo "Running LisTube (from bin)..."
        "$SCRIPT_DIR/LisTube/bin/Release/net8.0-windows/LisTube.exe" &
    else
        echo "LisTube executable not found!"
        echo "Please build the project first with: ./build.sh"
        exit 1
    fi
else
    # Native Linux - check for Wine
    if command -v wine &> /dev/null; then
        echo "Wine detected. Attempting to run with Wine..."
        if [[ -f "$SCRIPT_DIR/publish/LisTube.exe" ]]; then
            wine "$SCRIPT_DIR/publish/LisTube.exe" &
        elif [[ -f "$SCRIPT_DIR/LisTube/bin/Release/net8.0-windows/LisTube.exe" ]]; then
            wine "$SCRIPT_DIR/LisTube/bin/Release/net8.0-windows/LisTube.exe" &
        else
            echo "LisTube executable not found!"
            echo "Please build the project first with: ./build.sh"
            exit 1
        fi
    else
        echo "This is a Windows WPF application."
        echo ""
        echo "To run on Linux, you need Wine:"
        echo "  sudo apt-get install wine"
        echo ""
        echo "Alternatively, you can:"
        echo "1. Build on Windows with Visual Studio or dotnet CLI"
        echo "2. Use the project in WSL (Windows Subsystem for Linux)"
        echo ""
        echo "To build the project (without running):"
        echo "  ./build.sh"
    fi
fi
