using System.Globalization;
using System.Numerics;
using LiteHtmlSharp.Interop;
namespace LiteHtmlSharp;

public delegate int CreateElementDelegate(string tag, IReadOnlyDictionary<string, string> attributes, out SizeF size);
public delegate (string Css, string BaseUrl) ImportCssDelegate(string url, string baseUrl);

public partial class Container : IDisposable
{
    public Document Document
    {
        get;
    }
    private float scale = 1;
    public float ScaleFactor
    {
        get => scale; set
        {
            if (!float.IsFinite(value) || value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            scale = value;
        }
    }
    private TimeSpan minimumAnimationFrameInterval = TimeSpan.FromSeconds(1.0 / 60);

    /// <summary>
    /// Minimum interval between CSS animation timer pulses (default: approximately 60 FPS).
    /// Set to 100 ms for at most 10 FPS. Change on the UI thread. Animation time remains real time;
    /// input and viewport refreshes are not throttled. Does not control animated image playback.
    /// </summary>
    public TimeSpan MinimumAnimationFrameInterval
    {
        get => minimumAnimationFrameInterval;
        set
        {
            if (value < TimeSpan.FromMilliseconds(1) || value > TimeSpan.FromDays(1))
                throw new ArgumentOutOfRangeException(nameof(value), "Use an interval between 1 ms and one day.");
            if (minimumAnimationFrameInterval == value) return;
            minimumAnimationFrameInterval = value;
            AnimationFrameIntervalChanged?.Invoke();
        }
    }

    internal event Action? AnimationFrameIntervalChanged;

    public Func<string, bool>? ShouldCreateElementCallback { get; set; }
    public CreateElementDelegate? CreateElementCallback { get; set; }
    public ImportCssDelegate? ImportCssRequest { get; set; }
    public event Action<string>? AnchorClicked;

    /// <summary>Load HTML through the attached platform control, or load the document directly
    /// when no control is attached. A platform control schedules layout and painting.</summary>
    public void Render(string html)
    {
        ArgumentNullException.ThrowIfNull(html);
        ObjectDisposedException.ThrowIf(Document.IsDisposed, this);
        OnRenderHtml(html);
    }
    /// <summary>Custom hosts override this to schedule loading, layout, and painting.</summary>
    protected virtual void OnRenderHtml(string html) => Document.Load(html);
    public void TriggerAnchorClicked(string url) => AnchorClicked?.Invoke(url);
    public event Action<RectF>? RedrawRequested;
    public Container(string? masterCss = null, FrameClock? frameClock = null)
    {
        Document = new Document(this, masterCss);
        ImageClock = frameClock ?? new FrameClock();
        ImageClock.FrameChanged += OnImageFrameChanged;
    }
    public virtual nuint CreateFont(FontDescription description, out FontMetrics metrics)
    {
        var s = description.Size;
        metrics = new(s, s, s * .8f, s * .2f, s * .5f, s * .5f);
        return 0;
    }
    public virtual void DeleteFont(nuint font)
    {
    }
    public virtual float TextWidth(string text, nuint font) => new StringInfo(text).LengthInTextElements * 8;
    public virtual float PointToPixels(float points) => points * 96 / 72;
    public virtual float DefaultFontSize { get; set; } = 16;
    public virtual string DefaultFontName { get; set; } = "sans-serif";
    public virtual void LoadImage(string source, string baseUrl, bool redrawOnReady) => _ = LoadImageAsync(source, baseUrl, redrawOnReady);
    public virtual SizeF GetImageSize(string source, string baseUrl) => FindImage(source, baseUrl)?.Size ?? default;
    public virtual void SetCaption(string caption)
    {
    }
    public virtual void SetBaseUrl(string baseUrl)
    {
    }
    public virtual void SetCursor(string cursor)
    {
    }
    public virtual void OnAnchorClick(string url, nuint element) => TriggerAnchorClicked(url);
    public virtual bool OnElementClick(nuint element) => false;
    public virtual void OnMouseEvent(nuint element, int kind)
    {
    }
    public virtual string TransformText(string text, int kind) => kind switch { 1 => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text), 2 => text.ToUpperInvariant(), 3 => text.ToLowerInvariant(), _ => text };
    protected internal virtual (string Css, string BaseUrl) ImportCss(string url, string baseUrl) => ImportCssRequest?.Invoke(url, baseUrl) ?? ("", baseUrl);
    public virtual RectF Viewport => new(0, 0, 800, 600);
    public virtual MediaFeatures MediaFeatures => new(3, Viewport.Width, Viewport.Height, Viewport.Width, Viewport.Height);
    public virtual (string Language, string Culture) GetLanguage() => (CultureInfo.CurrentCulture.TwoLetterISOLanguageName, CultureInfo.CurrentCulture.Name);
    protected internal virtual bool ShouldCreateElement(string tag) => ShouldCreateElementCallback?.Invoke(tag) ?? false;
    protected internal virtual int CreateElement(string tag, IReadOnlyDictionary<string, string> attributes, out SizeF size)
    {
        size = default;
        return CreateElementCallback?.Invoke(tag, attributes, out size) ?? 0;
    }
    public void RequestRedraw(RectF box) => RedrawRequested?.Invoke(box);
    public virtual void FillRect(Layer layer, ColorRgba color)
    {
    }
    public virtual void DrawLinearGradient(Layer layer, float x0, float y0, float x1, float y1, ReadOnlySpan<lh_color_stop> stops, int colorSpace, int hueInterpolation)
    {
    }
    public virtual void DrawRadialGradient(Layer layer, float cx, float cy, float rx, float ry, ReadOnlySpan<lh_color_stop> stops, int colorSpace, int hueInterpolation)
    {
    }
    public virtual void DrawConicGradient(Layer layer, float cx, float cy, float angle, float radius, ReadOnlySpan<lh_color_stop> stops, int colorSpace, int hueInterpolation)
    {
    }
    public virtual void DrawImage(Layer layer, string source, string baseUrl)
    {
    }
    public virtual void DrawText(RectF position, ColorRgba color, nuint font, string text, float decorationOpacity = 1)
    {
    }
    public virtual void DrawBorders(Borders borders, RectF position, bool root)
    {
    }
    public virtual void DrawListMarker(RectF position, ColorRgba color, int markerType, int index, nuint font, string image, string baseUrl)
    {
    }
    public virtual void PushClip(RectF position, BorderRadii radii)
    {
    }
    public virtual void PopClip()
    {
    }
    public virtual void PushTransform(Matrix4x4 transform)
    {
    }
    /// <summary>Begin a compositing group. Apply opacity once to all enclosed drawing.</summary>
    public virtual void PushOpacity(float opacity) { }
    public virtual void PopOpacity() { }
    public virtual void PopTransform()
    {
    }
    public virtual void Dispose()
    {
        try
        {
            Document.Dispose();
        }
        finally { if (Document.IsDisposed) DisposeImages(); }
    }
}
