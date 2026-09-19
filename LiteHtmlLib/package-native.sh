#!/bin/bash
# Package a release library without modifying its unstripped build output.
# Usage: bash package-native.sh <library> <osx-x64|osx-arm64|linux-x64|linux-arm64>
# Linux cross-builds may set STRIP to an architecture-compatible strip executable.
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LIBRARY=${1:?Specify a built library}
RID=${2:?Specify a runtime identifier}
case "$RID" in
    osx-x64) ARCH=x86_64; NAME=liblitehtml.dylib ;;
    osx-arm64) ARCH=arm64; NAME=liblitehtml.dylib ;;
    linux-x64|linux-arm64) NAME=liblitehtml.so ;;
    *) echo "Unsupported runtime: $RID" >&2; exit 1 ;;
esac
DEST="$SCRIPT_DIR/../runtimes/$RID/native"
mkdir -p "$DEST"
TEMP=$(mktemp "$DEST/.package.XXXXXX")
trap 'rm -f "$TEMP"' EXIT
case "$RID" in
    osx-*)
        /usr/bin/lipo "$LIBRARY" -thin "$ARCH" -output "$TEMP"
        /usr/bin/strip -x "$TEMP"
        /usr/bin/codesign --force --sign - "$TEMP"
        ;;
    linux-*)
        cp "$LIBRARY" "$TEMP"
        "${STRIP:-strip}" --strip-unneeded "$TEMP"
        ;;
esac
chmod 755 "$TEMP"
# Rename instead of truncating a library that a running demo may have mapped.
mv -f "$TEMP" "$DEST/$NAME"
echo "Packaged: $DEST/$NAME"
