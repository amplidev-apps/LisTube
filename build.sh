#!/bin/bash

# LisTube Build Script
# This script builds the LisTube project

set -e

echo "================================"
echo "  LisTube - Build Script"
echo "================================"
echo ""

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: .NET SDK is not installed!"
    echo "Please install .NET 8.0 SDK from:"
    echo "https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
fi

# Check .NET version
DOTNET_VERSION=$(dotnet --version)
echo ".NET SDK Version: $DOTNET_VERSION"
echo ""

# Navigate to project directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

# Restore packages
echo "[1/4] Restoring NuGet packages..."
dotnet restore LisTube.sln

# Build solution
echo "[2/4] Building solution..."
dotnet build LisTube.sln --configuration Release --no-restore

# Publish
echo "[3/4] Publishing application..."
dotnet publish LisTube/LisTube.csproj --configuration Release --output ./publish --no-build --self-contained false

echo "[4/4] Build completed successfully!"
echo ""
echo "Output directory: $SCRIPT_DIR/publish"
echo "Executable: $SCRIPT_DIR/publish/LisTube.exe"
echo ""
echo "To run the application, use:"
echo "  ./run.sh"
echo ""
