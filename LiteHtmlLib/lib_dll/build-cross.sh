#!/bin/bash
#
# Cross-compile LiteHtmlLib.dll for Windows ARM64 from macOS
#
# Uses llvm-mingw Docker image for cross-compilation
#
# Usage:
#   ./build-cross.sh arm64    # Build for Windows ARM64
#   ./build-cross.sh x64      # Build for Windows x64
#   ./build-cross.sh x86      # Build for Windows x86
#
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RUNTIMES_DIR="$SCRIPT_DIR/../../runtimes"
ARCH="${1:-arm64}"

case $ARCH in
    arm64|aarch64)
        ARCH_NAME="arm64"
        MINGW_PREFIX="aarch64-w64-mingw32"
        ;;
    x64|x86_64)
        ARCH_NAME="x64"
        MINGW_PREFIX="x86_64-w64-mingw32"
        ;;
    x86|i686)
        ARCH_NAME="x86"
        MINGW_PREFIX="i686-w64-mingw32"
        ;;
    *)
        echo "Unsupported architecture: $ARCH"
        echo "Usage: $0 [arm64|x64|x86]"
        exit 1
        ;;
esac

echo "========================================"
echo "Cross-compiling LiteHtmlLib.dll for Windows $ARCH_NAME"
echo "========================================"

# Check Docker is available
if ! command -v docker &> /dev/null; then
    echo "Error: Docker not found. Install Docker Desktop for Mac."
    exit 1
fi

# Create output directory
mkdir -p "$RUNTIMES_DIR/win-$ARCH_NAME/native"

# Build using llvm-mingw Docker image
# mstorsjo/llvm-mingw provides cross-compilers for all Windows architectures
docker run --rm \
    -v "$SCRIPT_DIR/../..:/src" \
    -w /src/LiteHtmlLib/lib_dll \
    mstorsjo/llvm-mingw:latest \
    bash -c "
        rm -rf build-$ARCH_NAME && mkdir build-$ARCH_NAME && cd build-$ARCH_NAME
        cmake .. \
            -DCMAKE_SYSTEM_NAME=Windows \
            -DCMAKE_C_COMPILER=$MINGW_PREFIX-clang \
            -DCMAKE_CXX_COMPILER=$MINGW_PREFIX-clang++ \
            -DCMAKE_RC_COMPILER=$MINGW_PREFIX-windres \
            -DTARGET_ARCH=$ARCH_NAME
        make -j\$(nproc)
        cp LiteHtmlLib.dll /src/runtimes/win-$ARCH_NAME/native/
    "

echo ""
echo "Built: $RUNTIMES_DIR/win-$ARCH_NAME/native/LiteHtmlLib.dll"
ls -la "$RUNTIMES_DIR/win-$ARCH_NAME/native/LiteHtmlLib.dll"
file "$RUNTIMES_DIR/win-$ARCH_NAME/native/LiteHtmlLib.dll"
