# Development guide

## Prerequisites

- .NET 10 SDK; the core also targets .NET 8 and 9.
- CMake 3.21 or newer and a C++17 compiler.
- For macOS: Apple command-line tools; the macOS workload and compatible Xcode for AppKit projects.
- For Windows: Visual Studio or EWDK with MSBuild and x86/x64/ARM64 C++ toolchains. CMake is not required by the Windows MSBuild flow.
- For cross-builds on macOS: Zig or Docker. Scripts use tools from PATH.

The `litehtml` submodule points to the [PingmanTools fork](https://github.com/PingmanTools/litehtml). The parent commit pins the exact native revision. Both the engine and bridge build as C++17.

## Native builds

### macOS universal

Run from the repository root:

```sh
cmake --preset osx-universal
cmake --build --preset osx-universal --parallel
ctest --preset osx-universal --output-on-failure
/usr/bin/lipo -info LiteHtmlLib/build/osx-universal/liblitehtml.dylib
```

`LiteHtmlLib/build.sh` performs configuration and compilation. The output contains x86_64 and ARM64 slices. The wrapper exports only the `lh_*` C ABI and removes unused linked code. To build, run native tests, and update both packaged macOS binaries:

```sh
bash LiteHtmlLib/build.sh --install
```

Packaging extracts each architecture, strips local symbols, then ad-hoc signs the final file. The unstripped universal build output remains available for diagnostics. Release distribution signing can replace the ad-hoc signature later.

### Linux

On Linux, `LiteHtmlLib/lib_so/build.sh --install` builds for the current architecture and strips a copy into `runtimes/`. The `linux-x64` and `linux-arm64` CMake presets select target system/processor; supply a matching compiler for cross-compilation.

From macOS:

```sh
STRIP=/opt/homebrew/opt/llvm/bin/llvm-strip LiteHtmlLib/lib_so/build-cross.sh --zig
# Or use GCC Docker images:
LiteHtmlLib/lib_so/build-cross.sh --docker
```

Linux exports only the `lh_*` C ABI and discards unused function/data sections. Packaging uses `strip --strip-unneeded` on a copy; unstripped build outputs are retained. For Zig cross-builds, set `STRIP` to an ELF-capable `llvm-strip` executable if it is not on PATH.

An optional final argument selects `x64` or `arm64`. These scripts update packaged runtimes. Zig uses its own archive tools; its unsupported linker push/pop capability is declared explicitly so CMake does not probe a host linker.

### Windows

```bat
build-windows-dlls.bat
```

The script preserves Visual Studio/EWDK discovery and builds all three platforms using `LiteHtmlLib/LiteHtmlLib.vcxproj`. The project uses wildcard source lists corresponding to the unified CMake build. Release builds enable string pooling (`/GF`) both from Visual Studio and the script, while retaining `/Ox`, whole-program optimization, and the static runtime. Keep PDBs separately from packaged DLLs.

For macOS cross-compilation, use the installed Docker LLVM-MinGW image:

```sh
LiteHtmlLib/lib_dll/build-cross.sh x64
LiteHtmlLib/lib_dll/build-cross.sh x86
LiteHtmlLib/lib_dll/build-cross.sh arm64
```

Cross-compilation does not establish Windows runtime correctness; run the examples and tests on Windows separately.

## Managed builds and tests

```sh
dotnet build LiteHtmlSharp/LiteHtmlSharp.csproj
dotnet test LiteHtmlSharp.Tests/LiteHtmlSharp.Tests.csproj
dotnet test LiteHtmlSharp.Avalonia.Tests/LiteHtmlSharp.Avalonia.Tests.csproj
dotnet run --project Example.Avalonia
dotnet run --project Example.Mac
dotnet build LiteHtmlSharp.Wpf/LiteHtmlSharp.Wpf.csproj -p:EnableWindowsTargeting=true
```

The test fixture selects the native build output through `LITEHTML_NATIVE_LIBRARY`. Tests cover ABI layout and display-list semantics without a GUI. CTest also runs the engine regressions directly from `litehtml/tests`; the bridge retains its own C ABI and lifecycle tests. Platform examples require their respective desktop environment. A whole-solution build requires the macOS workload and Windows targeting support; use project-specific builds on other development machines.

## UTF-8-only native builds

LiteHtmlSharp supplies Unicode strings as UTF-8. Its native build defaults to `LITEHTML_UTF8_ONLY=ON`, omitting legacy decoder implementations and tables while retaining UTF-8 validation and HTML entities. HTML charset declarations do not reinterpret these UTF-8 bytes. Decode legacy file/network bytes in the managed loader before calling `Document.Load`. Explicit non-UTF-8 input to the UTF-8-only native parser is unsupported and rejected.

Standalone litehtml keeps `LITEHTML_UTF8_ONLY=OFF` by default. To build the wrapper with all native decoders, configure CMake with `-DLITEHTML_UTF8_ONLY=OFF`. For the Windows project, use `/p:LiteHtmlUtf8Only=false`; the existing build script uses the project's UTF-8-only default. The C ABI always expects UTF-8 regardless of this build choice.

The option also means UTF-16 CSS bytes must be decoded by the caller; CSS validation continues to replace malformed UTF-8 with U+FFFD. No struct layout or exported function changes are involved. Rebuild native binaries after changing the option.

## Native ABI and bindings

The opacity-group display commands require ABI version 2. Rebuild native libraries alongside the managed projects; older DLLs are rejected at load instead of silently rendering the wrong opacity. Custom managed renderers should override `Container.PushOpacity` / `PopOpacity` with group compositing. Standalone litehtml containers opt in via `supports_opacity_groups()` and the corresponding native methods; containers that do not opt in retain the previous per-operation alpha behavior.


`LiteHtmlLib/include/lh_api.h` is the ABI source of truth. All coordinates are floats; booleans are bytes; strings returned by host callbacks use sinks. `lh_abi_layout` exposes structure sizes, alignment, and field offsets. Every managed load validates the ABI before creating a document.

```sh
LiteHtmlLib/generate-bindings.sh
```

The generation script records the supported local binding-generation path. Keep `Interop/lh_api.cs`, callback thunks, and the native layout table consistent when changing the header. Run both CTest and managed tests afterward.

## Local native loading

The core's resolver first checks `LITEHTML_NATIVE_LIBRARY`, then runtime-relative and adjacent library paths. For a local development command, an explicit path avoids accidentally loading an installed package's native binary:

```sh
LITEHTML_NATIVE_LIBRARY="$PWD/LiteHtmlLib/build/osx-universal/liblitehtml.dylib" dotnet test LiteHtmlSharp.Tests/LiteHtmlSharp.Tests.csproj
```

NuGet platform packages carry their native assets automatically. With project references, copy the matching `runtimes/<rid>/native` files into the application's output, or set the environment variable. macOS app bundles use `NativeReference` entries for their matching dylib; see `Example.Mac/Example.Mac.csproj`.

## Packaging

`Directory.Build.props` owns the package version, `3.0.0-preview1`. The core package contains managed assemblies only. Avalonia carries all seven native runtimes; WPF carries three Windows runtimes; Mac carries two macOS runtimes. iOS is not packaged in this release.

After native validation:

```sh
dotnet pack LiteHtmlSharp/LiteHtmlSharp.csproj -c Release -p:GeneratePackageOnBuild=false -o artifacts/packages
dotnet pack LiteHtmlSharp.Avalonia/LiteHtmlSharp.Avalonia.csproj -c Release -p:GeneratePackageOnBuild=false -o artifacts/packages
dotnet pack LiteHtmlSharp.Wpf/LiteHtmlSharp.Wpf.csproj -c Release -p:GeneratePackageOnBuild=false -o artifacts/packages
dotnet pack LiteHtmlSharp.Mac/LiteHtmlSharp.Mac.csproj -c Release -p:GeneratePackageOnBuild=false -o artifacts/packages
unzip -l artifacts/packages/LiteHtmlSharp.Avalonia.3.0.0-preview1.nupkg
```

Inspect each archive's runtime list and verify that packaged bytes match validated `runtimes/` files. Packages are local build artifacts; packing does not publish them.

### Platform checks

`dotnet workload install macos` installs the workload directly when solution workload discovery fails before installation. Workloads belong to the selected SDK feature band. The solution excludes the legacy CoreGraphics `.shproj`; Mac still imports its `.projitems` source list. Xcode must match the selected macOS workload for supported app builds.

The Mac decoder executable tests are in `LiteHtmlSharp.Mac.Tests`; they require a macOS workload and run on macOS. WPF GIF metadata/composition tests run in the portable core test project, but Windows/WIC rendering still requires Windows verification.
