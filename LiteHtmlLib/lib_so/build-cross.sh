#!/bin/bash
#
# Cross-compile liblitehtml.so for Linux from macOS
#
# Options:
#   --docker    Use Docker (most reliable, requires Docker Desktop)
#   --zig       Use Zig as cross-compiler (requires: brew install zig)
#
# Usage:
#   ./build-cross.sh --docker          # Build both architectures via Docker
#   ./build-cross.sh --zig             # Build both architectures via Zig
#   ./build-cross.sh --docker x64      # Build only x64
#   ./build-cross.sh --zig arm64       # Build only arm64
#
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RUNTIMES_DIR="$SCRIPT_DIR/../../runtimes"
METHOD=""
ARCH=""

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --docker)
            METHOD="docker"
            shift
            ;;
        --zig)
            METHOD="zig"
            shift
            ;;
        x64|arm64)
            ARCH="$1"
            shift
            ;;
        *)
            echo "Unknown option: $1"
            echo "Usage: $0 [--docker|--zig] [x64|arm64]"
            exit 1
            ;;
    esac
done

if [ -z "$METHOD" ]; then
    echo "Please specify build method: --docker or --zig"
    exit 1
fi

# Determine which architectures to build
if [ -z "$ARCH" ]; then
    ARCHS="x64 arm64"
else
    ARCHS="$ARCH"
fi

echo "========================================"
echo "Cross-compiling liblitehtml.so for Linux"
echo "Method: $METHOD"
echo "Architectures: $ARCHS"
echo "========================================"

#
# Docker-based build
#
build_docker() {
    local arch=$1
    local docker_platform
    local docker_arch

    case $arch in
        x64)
            docker_platform="linux/amd64"
            docker_arch="x86_64"
            ;;
        arm64)
            docker_platform="linux/arm64"
            docker_arch="aarch64"
            ;;
    esac

    echo ""
    echo "Building for linux-$arch via Docker..."

    # Check Docker is available
    if ! command -v docker &> /dev/null; then
        echo "Error: Docker not found. Install Docker Desktop for Mac."
        exit 1
    fi

    # Create output directory
    mkdir -p "$RUNTIMES_DIR/linux-$arch/native"

    # Build in Docker container
    docker run --rm --platform "$docker_platform" \
        -v "$SCRIPT_DIR/../..:/src" \
        -w /src/LiteHtmlLib/lib_so \
        gcc:13 \
        bash -c "
            apt-get update && apt-get install -y cmake
            rm -rf build-$arch && mkdir build-$arch && cd build-$arch
            cmake ..
            make -j\$(nproc)
            cp liblitehtml.so /src/runtimes/linux-$arch/native/
        "

    echo "Built: $RUNTIMES_DIR/linux-$arch/native/liblitehtml.so"
    ls -la "$RUNTIMES_DIR/linux-$arch/native/liblitehtml.so"
}

#
# Zig-based cross-compilation
#
build_zig() {
    local arch=$1
    local zig_target

    case $arch in
        x64)
            zig_target="x86_64-linux-gnu"
            ;;
        arm64)
            zig_target="aarch64-linux-gnu"
            ;;
    esac

    echo ""
    echo "Building for linux-$arch via Zig..."

    # Check Zig is available
    if ! command -v zig &> /dev/null; then
        echo "Error: Zig not found. Install via: brew install zig"
        exit 1
    fi

    # Create output directory
    mkdir -p "$RUNTIMES_DIR/linux-$arch/native"

    BUILD_DIR="$SCRIPT_DIR/build-zig-$arch"
    rm -rf "$BUILD_DIR"
    mkdir -p "$BUILD_DIR"
    cd "$BUILD_DIR"

    # Configure CMake with Zig as the compiler
    CC="zig cc -target $zig_target" \
    CXX="zig c++ -target $zig_target" \
    cmake .. \
        -DCMAKE_SYSTEM_NAME=Linux \
        -DCMAKE_C_COMPILER_WORKS=1 \
        -DCMAKE_CXX_COMPILER_WORKS=1

    # Build
    make -j$(sysctl -n hw.ncpu)

    # Copy to runtimes
    cp liblitehtml.so "$RUNTIMES_DIR/linux-$arch/native/"

    echo "Built: $RUNTIMES_DIR/linux-$arch/native/liblitehtml.so"
    ls -la "$RUNTIMES_DIR/linux-$arch/native/liblitehtml.so"
}

# Run builds
for arch in $ARCHS; do
    case $METHOD in
        docker)
            build_docker $arch
            ;;
        zig)
            build_zig $arch
            ;;
    esac
done

echo ""
echo "========================================"
echo "Cross-compilation complete!"
echo "========================================"
ls -la "$RUNTIMES_DIR"/linux-*/native/*.so 2>/dev/null || echo "No .so files found"
