#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
version=21.1.8.4
tool_dir="${CLANGSHARP_TOOL_PATH:-${TMPDIR:-/tmp}/litehtml-clangsharp-$version}"
tool="$tool_dir/ClangSharpPInvokeGenerator"
if [[ ! -x "$tool" ]]; then
    dotnet tool install ClangSharpPInvokeGenerator --version "$version" --tool-path "$tool_dir"
fi
scratch="$(mktemp -d)"
trap 'rm -rf "$scratch"' EXIT
args=(-rd "$(clang -print-resource-dir)")
if [[ "$(uname -s)" == Darwin ]]; then
    args+=(-a -isysroot -a "$(xcrun --show-sdk-path)")
fi
status=0
"$tool" @LiteHtmlLib/generate-bindings.rsp "${args[@]}" -o "$scratch/raw.cs" >"$scratch/diagnostics" 2>&1 || status=$?
cat "$scratch/diagnostics"
# ClangSharp returns the diagnostic count, so only recognized nonfatal warnings are accepted.
python3 - "$scratch/diagnostics" "$status" "$scratch/raw.cs" <<'CHECK'
from pathlib import Path
import re
import sys
log=Path(sys.argv[1]).read_text()
warnings=re.findall(r"^\s*Warning.*$",log,re.M)
allowed=("Unsupported attribute: 'Visibility'", "requests 4 byte alignment which .NET cannot honor")
if any(not any(text in line for text in allowed) for line in warnings):
    raise SystemExit("Unexpected generator warning")
if re.search(r"\b(?:error|fatal|skipping)\b",log,re.I) or int(sys.argv[2]) != len(warnings):
    raise SystemExit("Binding generation failed")
raw=Path(sys.argv[3])
if not raw.is_file() or raw.stat().st_size == 0:
    raise SystemExit("Generator produced no bindings")
CHECK
python3 LiteHtmlLib/normalize-bindings.py "$scratch/raw.cs" "$scratch/normalized.cs"
cp "$scratch/normalized.cs" LiteHtmlSharp/Interop/lh_api.cs
