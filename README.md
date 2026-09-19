# LiteHtmlSharp

LiteHtmlSharp renders HTML and CSS in .NET applications using the [litehtml](https://github.com/litehtml/litehtml) layout engine and platform drawing APIs. It is suitable for embedded content such as welcome screens, help panels, and rich labels. It does not run JavaScript.

## Packages

The current development version is **3.0.0-preview1**, defined in `Directory.Build.props`.

| Package | Framework | Native runtimes |
| --- | --- | --- |
| LiteHtmlSharp | .NET 8, 9, 10 | None; supplied by a platform package or host |
| LiteHtmlSharp.Avalonia | .NET 10, Avalonia 11 | Windows x86/x64/ARM64, macOS x64/ARM64, Linux x64/ARM64 |
| LiteHtmlSharp.Wpf | .NET 10 Windows | Windows x86/x64/ARM64 |
| LiteHtmlSharp.Mac | .NET 10 macOS | macOS x64/ARM64 |

The iOS source remains available as a starting point for a future port. It is excluded from the solution and this release.

## Architecture

- `LiteHtmlLib/include/lh_api.h` defines the flat C ABI, using float coordinates and opaque document/element handles.
- The native container asks the host for font metrics, images, viewport information, and custom elements. It records drawing commands into a retained display list.
- `LiteHtmlSharp/Interop` contains source-generated `LibraryImport` entry points and unmanaged callback thunks. A native layout table validates managed structure sizes and offsets at startup.
- `Document` owns the native handle and copies each drawing result into an immutable managed `DisplayList`. Platform containers replay fills, text, images, borders, gradients, and clips.
- The CoreGraphics shared source supplies the macOS drawing implementation. The former interop shared project is not required.

Version 3 changes the native ABI and managed container API. Upgrade the managed packages and their native libraries together. See the [managed API migration guide](docs/managed-api.md) and examples for the current integration API.

## Fractional geometry

The native bridge retains fractional font sizes, border widths, and paint rectangles in logical units. Platform scaling is applied afterward; a half-unit border occupies one device pixel at 2× scale. Font measurement uses the same resolved size as drawing.

Native litehtml containers can override `resolve_font_size`, `resolve_border_width`, and `round_paint_position` independently. The default implementations preserve historical integer rounding. The first two affect layout and must remain stable while a document is in use; changing them requires recomputing styles and layout. Paint rounding operates on copies and does not alter measured geometry. LiteHtmlSharp overrides these hooks entirely in native code, without extra managed callbacks or a managed API change.

## Animation and images

Set `container.MinimumAnimationFrameInterval = TimeSpan.FromMilliseconds(100)` to cap CSS animation timer updates at 10 FPS (the default is approximately 60 FPS). Avalonia, WPF, and Cocoa share this setting. Changes on the UI thread take effect while running without restarting the timeline. Animation progress uses actual elapsed time; delayed starts and final frames may be sampled up to one interval late. Input and viewport refreshes remain immediate. Animated images use their own frame clock and are not capped by this setting.

CSS keyframes and transitions animate opacity, transform, text/background colors, and box geometry. Avalonia, WPF, and Cocoa controls manage motion automatically through the shared `AnimationDriver`. Its timer pulse renders a frame and returns the next interval; a recurring timer keeps its interval until the hint changes by at least 1 ms. Native `render_frame` (`Document.RenderFrame` in C#) advances time, renders, and returns the next delay and activity together. Static documents skip scheduling traversal. Delayed animations sleep until their start; safely off-screen paint-only motion sleeps until viewport/style/input changes, then resumes at the current timeline position. Horizontal-only translations can also sleep when the complete subtree is above or below the viewport. Layout changes and transforms that might enter the viewport stay scheduled.

Transform parsing supports 2D/3D matrices; the desktop drawing backends project their affine 2D portion, so perspective rendering is limited. Opacity composites a parent’s background and descendants as a group, including nested opacity. Avalonia, WPF, and Cocoa use their platform compositing scopes; fully opaque elements do not create opacity groups.

Platform image sources decode animated GIF frames into a shared `FrameClock`. Image readiness schedules layout; subsequent frames repaint the retained display list without native layout. Resource loaders should support background calls for image bytes. The Mac legacy image callback keeps its existing calling thread; use `LoadImageDataCallback` for background byte loading.

All three examples load their rendering test page, master CSS, and GIF from the shared embedded resources in `Example.Shared`. Each platform control owns its animation clock. The shared page includes small text and overlapping opacity comparisons.

`Example.Avalonia` opens a Home test page with gradients, native controls, animations, and a GIF. Its address bar and links load HTTP(S) pages, CSS, and images. This is a rendering demo; JavaScript, form submission, and full browser behavior are not implemented.

## Build and run

Clone with submodules, install the .NET 10 SDK, and use CMake 3.21 or newer with a C++17 compiler for native builds. macOS projects also require the .NET macOS workload and compatible Xcode.

```sh
git submodule update --init
cmake --preset osx-universal
cmake --build --preset osx-universal --parallel
ctest --preset osx-universal
dotnet test LiteHtmlSharp.Tests/LiteHtmlSharp.Tests.csproj
dotnet test LiteHtmlSharp.Avalonia.Tests/LiteHtmlSharp.Avalonia.Tests.csproj
dotnet run --project Example.Avalonia
```

Windows native builds use `build-windows-dlls.bat` and MSBuild on a Visual Studio/EWDK machine. Linux and LLVM-MinGW Docker cross-build entry points are documented in [DEVELOPMENT.md](DEVELOPMENT.md).

## Validation and development

See [DEVELOPMENT.md](DEVELOPMENT.md) for native compilation, binding generation, local library resolution, and packaging.

Licensed under [BSD-3-Clause](LICENSE).

Run the small Avalonia animation demo with `dotnet run --project Example.Avalonia -- --small-animation`. It renders an 800 × 35 HTML strip using the default animation cadence, with a hover transition and an HTML link to toggle continuous animation (off initially).
