using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using LiteHtmlSharp.Interop;
namespace LiteHtmlSharp;

public sealed unsafe class Document : IDisposable
{
    private readonly Container owner;
    private readonly CallbackState callbacks;
    private GCHandle context;
    private lh_document* handle;
    private readonly object gate = new();
    private bool active;
    private long diagnosticId;
    private HashSet<int>? loadingCustomElements;
    /// <summary>Return lifecycle counters for this document, or zero counters after disposal.
    /// During a load, the previous completed load remains visible until the new load succeeds.</summary>
    public LiteHtmlDiagnostics GetDiagnostics() => DiagnosticRegistry.Snapshot(diagnosticId);
    internal void RecordCustomElement(int id)
    {
        if (id > 0) loadingCustomElements?.Add(id);
    }
    private string? masterCss;
    internal int Generation
    {
        get; private set;
    }
    public bool IsDisposed => handle == null;
    internal Container Owner => owner;
    public bool HasLoadedHtml
    {
        get; internal set;
    }
    public bool HasRendered
    {
        get; internal set;
    }
    public DisplayList? LastDisplayList
    {
        get; private set;
    }
    public event Action? ViewElementsNeedLayout;
    /// <summary>Raised after a successful HTML replacement, outside native callbacks.</summary>
    public event Action? Loaded;
    internal Document(Container container, string? master)
    {
        owner = container;
        masterCss = master;
        AbiLayout.Validate();
        callbacks = new(container);
        context = GCHandle.Alloc(callbacks);
        var host = HostThunks.Create((void*)GCHandle.ToIntPtr(context));
        handle = NativeMethods.lh_document_create(&host);
        if (handle == null)
        {
            context.Free();
            throw new InvalidOperationException("Native document creation failed.");
        }
        diagnosticId = DiagnosticRegistry.Register();
    }
    private T Invoke<T>(Func<T> action)
    {
        lock (gate)
        {
            if (handle == null)
                throw new ObjectDisposedException(nameof(Document));
            if (active)
                throw new InvalidOperationException("Re-entering a document during a native callback or display replay is not supported.");
            active = true;
            try
            {
                var result = action();
                callbacks.ThrowPending();
                return result;
            }
            finally { active = false; GC.KeepAlive(owner); }
        }
    }
    public void SetMasterCSS(string css)
    {
        ArgumentNullException.ThrowIfNull(css);
        Invoke(() => { masterCss = css; return 0; });
    }
    public void Load(string html, string? userCss = null)
    {
        ArgumentNullException.ThrowIfNull(html);
        Invoke(() =>
        {
            var h = Encoding.UTF8.GetBytes(html + '\0');
            var m = masterCss is null ? null : Encoding.UTF8.GetBytes(masterCss + '\0');
            var u = userCss is null ? null : Encoding.UTF8.GetBytes(userCss + '\0');
            byte success = 0;
            owner.BeginImageLoad();
            loadingCustomElements = new HashSet<int>();
            try
            {
                fixed (byte* hp = h, mp = m, up = u)
                    success = NativeMethods.lh_document_load(handle, (sbyte*)hp, (sbyte*)mp, (sbyte*)up);
                if (success != 0)
                {
                    DiagnosticRegistry.Loaded(diagnosticId, loadingCustomElements.Count);
                    Generation++;
                    LastDisplayList = null;
                    HasLoadedHtml = true;
                    HasRendered = false;
                }
            }
            finally { loadingCustomElements = null; owner.EndImageLoad(success != 0); }
            callbacks.ThrowPending();
            if (success == 0)
                throw new InvalidOperationException("Native HTML loading failed.");
            return 0;
        });
        Loaded?.Invoke();
    }
    public float Render(float maxWidth)
    {
        if (!float.IsFinite(maxWidth) || maxWidth < 0)
            throw new ArgumentOutOfRangeException(nameof(maxWidth));
        var width = Invoke(() => { var result = NativeMethods.lh_document_render(handle, maxWidth); callbacks.ThrowPending(); if (!float.IsFinite(result)) throw new InvalidOperationException("Native rendering failed."); HasRendered = true; return result; });
        ViewElementsNeedLayout?.Invoke();
        return width;
    }
    /// <summary>Advances time, renders, and returns scheduling information in one native call.</summary>
    public AnimationFrame RenderFrame(float maxWidth, double milliseconds, RectF visibleBounds,
        double frameIntervalMilliseconds = 1000.0 / 60)
    {
        if (!float.IsFinite(maxWidth) || maxWidth < 0) throw new ArgumentOutOfRangeException(nameof(maxWidth));
        if (!double.IsFinite(milliseconds) || milliseconds < 0) throw new ArgumentOutOfRangeException(nameof(milliseconds));
        if (!double.IsFinite(frameIntervalMilliseconds) || frameIntervalMilliseconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(frameIntervalMilliseconds));
        var frame = Invoke(() =>
        {
            var visible = visibleBounds.Native;
            float width;
            byte active;
            var delay = NativeMethods.lh_document_render_frame(handle, maxWidth, milliseconds, &visible,
                frameIntervalMilliseconds, &width, &active);
            callbacks.ThrowPending();
            if (!double.IsFinite(delay) || !float.IsFinite(width)) throw new InvalidOperationException("Native rendering failed.");
            HasRendered = true;
            return new AnimationFrame(width, delay, active != 0);
        });
        ViewElementsNeedLayout?.Invoke();
        return frame;
    }
    public DisplayList Draw(float x, float y, RectF clip) => Invoke(() =>
    {
        var c = clip.Native;
        NativeMethods.lh_document_draw(handle, x, y, &c);
        callbacks.ThrowPending();
        byte* stream;
        byte* blob;
        uint streamLength, blobLength;
        NativeMethods.lh_document_display_list(handle, &stream, &streamLength, &blob, &blobLength);
        var snapshot = new DisplayList(this, Generation, new ReadOnlySpan<byte>(stream, checked((int)streamLength)).ToArray(), new ReadOnlySpan<byte>(blob, checked((int)blobLength)).ToArray());
        LastDisplayList = snapshot;
        owner.UpdateImageVisibility(snapshot);
        return snapshot;
    });
    public float Width() => Invoke(() => NativeMethods.lh_document_width(handle));
    public float Height() => Invoke(() => NativeMethods.lh_document_height(handle));
    public bool OnMouseOver(float x, float y) => OnMouseOver(x, y, x, y);
    public bool OnMouseOver(float x, float y, float clientX, float clientY) => Invoke(() => NativeMethods.lh_document_on_mouse_over(handle, x, y, clientX, clientY) != 0);
    public bool OnMouseLeave() => Invoke(() => NativeMethods.lh_document_on_mouse_leave(handle) != 0);
    public bool OnLeftButtonDown(float x, float y) => OnLeftButtonDown(x, y, x, y);
    public bool OnLeftButtonDown(float x, float y, float clientX, float clientY) => Invoke(() => NativeMethods.lh_document_on_lbutton_down(handle, x, y, clientX, clientY) != 0);
    public bool OnLeftButtonUp(float x, float y) => OnLeftButtonUp(x, y, x, y);
    public bool OnLeftButtonUp(float x, float y, float clientX, float clientY) => Invoke(() => NativeMethods.lh_document_on_lbutton_up(handle, x, y, clientX, clientY) != 0);
    public void OnMediaChanged() => Invoke(() => NativeMethods.lh_document_media_changed(handle));
    public nuint ElementAt(float x, float y) => Invoke(() => NativeMethods.lh_document_element_at(handle, x, y));
    public nuint ElementAt(float x, float y, float clientX, float clientY) => Invoke(() => NativeMethods.lh_document_element_at_client(handle, x, y, clientX, clientY));
    /// <summary>Return a snapshot of a visible custom element's layout in document coordinates,
    /// or null when the ID is absent or has no visible layout. Render before querying.</summary>
    public ElementInfo? GetElementInfo(int id) => Invoke<ElementInfo?>(() => { lh_rect box; return NativeMethods.lh_document_custom_element_box(handle, id, &box) == 0 ? null : new ElementInfo(id, RectF.From(box)); });
    private sealed class TextResult
    {
        internal readonly StringBuilder Text = new(); internal Exception? Error;
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void Receive(void* state, sbyte* text, int length)
    {
        TextResult? result = null;
        try
        {
            result = (TextResult)GCHandle.FromIntPtr((nint)state).Target!;
            result.Text.Append(Encoding.UTF8.GetString(new ReadOnlySpan<byte>(text, length)));
        }
        catch (Exception error) { if (result != null) result.Error ??= error; }
    }
    private string ElementText(nuint element, string? name) => Invoke(() =>
    {
        var result = new TextResult();
        var gc = GCHandle.Alloc(result);
        try
        {
            var sink = new lh_sink { write = &Receive, ctx = (void*)GCHandle.ToIntPtr(gc) };
            if (name is null)
                NativeMethods.lh_element_get_tag(handle, element, &sink);
            else
            {
                var bytes = Encoding.UTF8.GetBytes(name + '\0');
                fixed (byte* p = bytes)
                    NativeMethods.lh_element_get_attr(handle, element, (sbyte*)p, &sink);
            }
            if (result.Error != null)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(result.Error).Throw();
            return result.Text.ToString();
        }
        finally { gc.Free(); }
    });
    public string GetElementAttribute(nuint element, string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return ElementText(element, name);
    }
    public string GetElementTag(nuint element) => ElementText(element, null);
    public nuint GetElementParent(nuint element) => Invoke(() => NativeMethods.lh_element_get_parent(handle, element));
    public string GetTooltipText(float x, float y) => GetTooltipText(x, y, x, y);
    public string GetTooltipText(float x, float y, float clientX, float clientY)
    {
        for (var element = ElementAt(x, y, clientX, clientY); element != 0; element = GetElementParent(element))
        {
            var title = GetElementAttribute(element, "title");
            if (title.Length != 0)
                return title;
        }
        return "";
    }
    /// <summary>Synchronize the animation clock before input in a custom host.
    /// Use RenderFrame to advance, lay out, and obtain the next timer delay together.</summary>
    public void SetTime(double milliseconds)
    {
        if (!double.IsFinite(milliseconds) || milliseconds < 0)
            throw new ArgumentOutOfRangeException(nameof(milliseconds));
        Invoke(() => { NativeMethods.lh_document_set_time(handle, milliseconds); return 0; });
    }
    public bool AnimationsActive => Invoke(() => NativeMethods.lh_document_animations_active(handle) != 0);
    internal void Replay(DisplayList list, Action action) => Invoke(() => { list.EnsureValid(); action(); return 0; });
    public void Dispose()
    {
        lock (gate)
        {
            if (handle == null)
                return;
            if (active)
                throw new InvalidOperationException("Cannot destroy a document during a callback or replay.");
            active = true;
            var pointer = handle;
            try
            {
                NativeMethods.lh_document_destroy(pointer);
            }
            finally { DiagnosticRegistry.Remove(diagnosticId); handle = null; Generation++; LastDisplayList = null; if (context.IsAllocated) context.Free(); active = false; GC.SuppressFinalize(this); GC.KeepAlive(owner); }
            callbacks.ThrowPending();
        }
    }
    ~Document()
    {
        if (handle != null)
        {
            var pointer = handle;
            handle = null;
            callbacks.CleanupOwner = owner;
            try
            {
                NativeMethods.lh_document_destroy(pointer);
            }
            finally { DiagnosticRegistry.Remove(diagnosticId); callbacks.CleanupOwner = null; }
        }
        if (context.IsAllocated)
            context.Free();
    }
}
