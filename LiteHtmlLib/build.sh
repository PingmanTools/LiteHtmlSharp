#!/bin/bash
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INSTALL=false
if [[ ${1:-} == --install && $# == 1 ]]; then
    INSTALL=true
elif [[ $# != 0 ]]; then
    echo "Usage: $0 [--install]" >&2
    exit 1
fi
cd "$SCRIPT_DIR/.."
cmake --preset osx-universal
cmake --build --preset osx-universal --parallel
if $INSTALL; then
    ctest --preset osx-universal --output-on-failure
    for rid in osx-x64 osx-arm64; do
        bash "$SCRIPT_DIR/package-native.sh" "$SCRIPT_DIR/build/osx-universal/liblitehtml.dylib" "$rid"
    done
fi
