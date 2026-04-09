#!/bin/bash

# LisTube Avalonia Build Script for Linux
# This script builds the cross-platform Avalonia version

set -e

echo "================================"
echo "  LisTube Avalonia - Build Script"
echo "================================"
echo ""

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    echo ".NET SDK not found. Installing..."
    echo ""
    
    # Detect distribution
    if [ -f /etc/os-release ]; then
        . /etc/os-release
        
        # Check for Ubuntu-based systems (including Zorin, Mint, Pop!_OS, etc.)
        if [[ "$ID_LIKE" == *"ubuntu"* ]] || [[ "$ID_LIKE" == *"debian"* ]] || [[ "$ID" == "ubuntu" ]] || [[ "$ID" == "debian" ]]; then
            echo "Detected Debian/Ubuntu-based system: $NAME"
            echo "Installing .NET SDK 8.0..."
            
            # Use ubuntu repository for Ubuntu-based distros
            if [[ "$UBUNTU_CODENAME" == "noble" ]] || [[ "$VERSION_CODENAME" == "noble" ]]; then
                # Ubuntu 24.04 LTS
                wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
            elif [[ "$UBUNTU_CODENAME" == "jammy" ]] || [[ "$VERSION_CODENAME" == "jammy" ]]; then
                # Ubuntu 22.04 LTS
                wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
            else
                # Default to 22.04 for older Ubuntu-based distros
                wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
            fi
            
            sudo dpkg -i packages-microsoft-prod.deb
            rm packages-microsoft-prod.deb
            sudo apt-get update
            sudo apt-get install -y dotnet-sdk-8.0
        elif [[ "$ID" == "fedora" ]]; then
            echo "Detected Fedora system"
            sudo dnf install -y dotnet-sdk-8.0
        elif [[ "$ID" == "arch" ]] || [[ "$ID" == "manjaro" ]]; then
            echo "Detected Arch/Manjaro system"
            sudo pacman -S --noconfirm dotnet-sdk
        else
            echo "Distribution '$ID' not automatically supported."
            echo "Trying Ubuntu/Debian method as fallback..."
            echo "Installing .NET SDK 8.0..."
            wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
            sudo dpkg -i packages-microsoft-prod.deb
            rm packages-microsoft-prod.deb
            sudo apt-get update
            sudo apt-get install -y dotnet-sdk-8.0
        fi
    else
        echo "Cannot detect distribution. Please install .NET 8.0 SDK manually:"
        echo "https://dotnet.microsoft.com/download/dotnet/8.0"
        exit 1
    fi
fi

# Check .NET version
DOTNET_VERSION=$(dotnet --version)
echo ".NET SDK Version: $DOTNET_VERSION"
echo ""

# Navigate to project directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

# Check if Avalonia project exists
if [ ! -d "LisTube.Avalonia" ]; then
    echo "ERROR: LisTube.Avalonia directory not found!"
    exit 1
fi

cd LisTube.Avalonia

echo "[1/5] Restoring NuGet packages..."
dotnet restore

echo ""
echo "[2/5] Building project..."
dotnet build --configuration Release --no-restore

echo ""
echo "[3/5] Publishing application..."
dotnet publish --configuration Release --output ../publish-avalonia --self-contained true --runtime linux-x64

echo ""
echo "[4/5] Setting executable permissions..."
chmod +x ../publish-avalonia/LisTube

echo ""
echo "[5/5] Build completed successfully!"
echo ""
echo "================================"
echo "  Build Complete!"
echo "================================"
echo ""
echo "Output directory: $SCRIPT_DIR/publish-avalonia"
echo "Executable: $SCRIPT_DIR/publish-avalonia/LisTube"
echo ""
echo "To run the application:"
echo "  cd $SCRIPT_DIR/publish-avalonia"
echo "  ./LisTube"
echo ""
echo "Or use the run script:"
echo "  ./run-avalonia.sh"
echo ""
