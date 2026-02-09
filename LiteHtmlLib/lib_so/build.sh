#!/bin/bash
#
# Build liblitehtml.so for Linux
#
# Usage:
#   ./build.sh              # Build for current architecture
#   ./build.sh --install    # Build and install to runtimes directory
#
# For cross-compilation to ARM64 from x64:
#   CC=aarch64-linux-gnu-gcc CXX=aarch64-linux-gnu-g++ ./build.sh
#
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUILD_DIR="$SCRIPT_DIR/build"
INSTALL_FLAG=""

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --install)
            INSTALL_FLAG="--target install"
            shift
            ;;
        --clean)
            rm -rf "$BUILD_DIR"
            echo "Cleaned build directory"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Detect architecture
ARCH=$(uname -m)
case $ARCH in
    x86_64)
        ARCH_NAME="x64"
        ;;
    aarch64|arm64)
        ARCH_NAME="arm64"
        ;;
    *)
        echo "Unsupported architecture: $ARCH"
        exit 1
        ;;
esac

echo "========================================"
echo "Building liblitehtml.so for Linux $ARCH_NAME"
echo "========================================"

# Create build directory
mkdir -p "$BUILD_DIR"
cd "$BUILD_DIR"

# Configure
cmake ..

# Build
make -j$(nproc)

# Install if requested
if [ -n "$INSTALL_FLAG" ]; then
    make install
    echo ""
    echo "Installed to: $SCRIPT_DIR/../../runtimes/linux-$ARCH_NAME/native/"
fi

echo ""
echo "Build complete: $BUILD_DIR/liblitehtml.so"
ls -la "$BUILD_DIR/liblitehtml.so"
