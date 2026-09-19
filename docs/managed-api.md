# Managed API evolution

Version 3 retains the application integration model around `Container`, `Document`,
and `ViewportContainer`. Geometry and measurements use `float`. This is a source
migration, not binary compatibility with version 2.

## Shared application hooks

Code shared by desktop platforms can hold a `Container` and use:

- `Render(html)`, `Document`, and `AnchorClicked`.
- `ShouldCreateElementCallback` and `CreateElementCallback`.
- `ImportCssRequest`, returning both CSS and its base URL for relative resources.
- Writable `DefaultFontName`, `DefaultFontSize`, and floating-point `ScaleFactor`.

Platform containers inherit these hooks. There is one creation delegate, rather
than separate Avalonia/WPF/Cocoa delegates. Attached controls handle `Render(html)`
and schedule layout and painting. Without an attached control it loads the document;
call `Document.Render(width)` to calculate layout. Use the UI thread for attached hosts.

```csharp
Container container = platformContainer;
container.ShouldCreateElementCallback = tag => tag == "widget";
container.CreateElementCallback = (
    string tag, IReadOnlyDictionary<string, string> attributes, out SizeF size) =>
{
    // Resolve and measure the application's control here.
    size = new SizeF(80.5f, 24.25f);
    return 42;
};
container.Document.ViewElementsNeedLayout += () =>
{
    if (container.Document.GetElementInfo(42) is { } info)
    {
        // Position the application control using info.PosX/PosY/Width/Height.
        // These are document coordinates; apply host scrolling/scaling as needed.
    }
};
container.Render("<widget data-control-ident='example'></widget>");
```

Custom IDs must be positive and unique within a load; zero declines replacement.
Attributes are a case-insensitive dictionary, preserving values containing newlines
or equals signs. Creation returns the measured `SizeF` separately from its ID.
The callback must not re-enter the document.

`GetElementInfo(id)` returns a nullable immutable snapshot with `Id`, `Bounds`,
`PosX`, `PosY`, `Width`, and `Height`. Null means absent or without visible layout.
It replaces the preview's `GetCustomElementBox`; there is no parallel legacy API.
The snapshot owns no native handle and remains a snapshot after HTML replacement.
Attributes come from the creation callback, not a serialized string on the layout
result. The old `ElementInfo.Text` field is not part of this layout API.

`DocumentSizeKnown`, `SetViewport`, and `SetMasterCSS` remain available. Changing
master CSS affects the next HTML load. Applications can continue switching a body
class and reloading HTML for theme changes. Desktop controls drive animations
without an application timer.

## Ownership

Avalonia and WPF controls accept `ownsContainer` (default `true`, preserving their
existing disposal behavior). Pass `false` when the application owns a supplied
container; disposing the control then detaches its subscriptions and timer while
leaving the container usable. The application must eventually dispose that container.
An Avalonia control that creates its own container always owns it.

Use `createInteractiveElements: false` (Avalonia) or `createOldStyleControls: false`
(WPF) when the application supplies custom-element callbacks. Disposal preserves
those application callbacks. A replacement control must reconnect the application's
child controls; built-in child controls are removed on disposal and require an HTML
reload to recreate. Attach only one host control to a container at a time.

## Intentional changes retained

- Native handles and callback tables are owned by the wrapper, not the application.
- `Document.Draw` records an immutable display list; platform replay performs drawing.
- Renderer hooks retain structured gradients, transforms, and group opacity rather
  than returning to the old background-only drawing contract.
- `LibInterop.GetDiagnostics()` and `Document.GetDiagnostics()` return immutable
  lifecycle snapshots. The obsolete `DocumentRefCount` is removed; use
  `NativeDocumentCount` to monitor owned bridge handles. See the diagnostics
  section below.
- Viewport values, font metrics, custom sizes, and layout results use floats. Convert
  platform doubles at the boundary rather than rounding to integer pixels.

No litehtml engine or native ABI changes are required for this managed API revision.

## Diagnostics

`LibInterop.GetDiagnostics()` aggregates currently live containers in this loaded
assembly. `container.Document.GetDiagnostics()` reports one container and returns
zero counters after disposal. Both can be read without invoking native code.

| Counter | Meaning |
| --- | --- |
| `TotalContainerCount` | Containers with an owned native bridge handle |
| `NativeDocumentCount` | Owned native bridge handles (currently one per container, including before HTML is loaded) |
| `HasDocument` | Containers with a successfully loaded HTML document; 0 or 1 for a document snapshot |
| `CustomElementCount` | Distinct positive custom-element IDs registered by the current successful load, including hidden elements |
| `ParseCount` | Successful HTML loads since creation, summed across currently live containers for the global snapshot |

A successful replacement replaces the custom-element count and increments parses.
A failed native load retains the previous snapshot. A captured managed callback
exception can be reported after the native load succeeds; counters reflect the
native load's result. Disposal or finalization removes a container's contribution,
including its parse history. Render, draw, scroll, and animation pulses do not
increment parse counts.

These counters measure wrapper ownership and registrations, not every internal
engine allocation. They help identify accumulating containers and registrations;
they cannot prove the absence of native memory leaks. For the old debug display,
replace `DocumentRefCount` with `NativeDocumentCount` and a corresponding label;
the latter counts owned bridge handles, not C++ shared-pointer references. The diagnostics registry holds no container references.

## Entry points and subclass hooks

Use `Container.Render(html)` to replace HTML in an attached desktop control. The
controls' load handlers are implementation details; examples use this common entry
point. Custom container subclasses can override protected `OnRenderHtml` to supply
host scheduling. The default implementation calls `Document.Load`.

For a custom host, the distinct low-level steps remain explicit:

- `Document.Load(html, userCss)` parses HTML without scheduling a UI update.
- `Document.Render(width)` calculates layout at the current animation time.
- `Document.RenderFrame(...)` advances time, calculates layout, and returns scheduling
  information in one operation. It replaces the separate `NextAnimationDelay` query.
- `Document.SetTime(...)` remains useful for synchronizing the clock before hover or
  other input, independently of layout. `AnimationDriver` does this for desktop hosts.
- `Document.Draw(...)` records drawing commands; `ViewportContainer.Draw()` also
  replays them with the viewport's offset. These are distinct host responsibilities.

`CreateFromString` was an alias for `Load` and is removed. Mouse movement uses
`OnMouseOver` with overloads for document coordinates alone or separate document and
client coordinates; the differently named `OnMouseMove` alias is removed. Text drawing
has one virtual method with optional `decorationOpacity`, rather than two override paths.

Applications configure custom elements and CSS through callback properties. The
`ShouldCreateElement`, `CreateElement`, and `ImportCss` methods are protected subclass
hooks (also accessible internally to the bridge), not alternative public entry points.
There is no public HTML-dispatch event between a container and its control.
