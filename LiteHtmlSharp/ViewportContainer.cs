namespace LiteHtmlSharp;

public struct LiteHtmlSize
{
    public float Width, Height;
    public LiteHtmlSize(float width, float height)
    {
        Width = width;
        Height = height;
    }
}
public struct LiteHtmlPoint
{
    public float X, Y;
    public LiteHtmlPoint(float x, float y)
    {
        X = x;
        Y = y;
    }
}
public struct LiteHtmlRect
{
    public LiteHtmlPoint Point; public LiteHtmlSize Size;
}
public class ViewportContainer : Container
{
    public LiteHtmlSize Size;
    public LiteHtmlPoint ScrollOffset;
    private LiteHtmlSize desiredSize;
    private LiteHtmlPoint desiredScrollOffset;
    private bool rendering;
    public bool HasCustomViewport
    {
        get; private set;
    }
    public event Action<LiteHtmlSize>? DocumentSizeKnown;
    public ViewportContainer(string? masterCss = null) : base(masterCss) { }
    public override RectF Viewport => new(0, 0, Size.Width, Size.Height);
    public override MediaFeatures MediaFeatures => new(3, Size.Width, Size.Height, Size.Width, Size.Height);
    public void ResetViewport()
    {
        Document.HasLoadedHtml = false;
        Document.HasRendered = false;
        Size = default;
        ScrollOffset = default;
        desiredSize = default;
        desiredScrollOffset = default;
        HasCustomViewport = false;
    }
    public DisplayList Draw()
    {
        var list = Document.Draw(-ScrollOffset.X, -ScrollOffset.Y, Viewport);
        DisplayListReplayer.Replay(list, this);
        return list;
    }
    public void Render() => Render(Document.Render);
    public void Render(Func<float, float> renderFrame)
    {
        if (rendering)
            return;
        rendering = true;
        try
        {
            renderFrame(Size.Width);
            DocumentSizeKnown?.Invoke(new(Document.Width(), Document.Height()));
        }
        finally { rendering = false; }
    }
    public bool CheckViewportChange(bool forceRender = false)
    {
        if (forceRender || Size.Width != desiredSize.Width || Size.Height != desiredSize.Height)
        {
            Size = desiredSize;
            ScrollOffset = desiredScrollOffset;
            Document.OnMediaChanged();
            Render();
            return true;
        }
        if (ScrollOffset.X != desiredScrollOffset.X || ScrollOffset.Y != desiredScrollOffset.Y)
        {
            ScrollOffset = desiredScrollOffset;
            return true;
        }
        return false;
    }
    public bool SetViewport(LiteHtmlPoint scrollOffset, LiteHtmlSize size)
    {
        HasCustomViewport = true;
        desiredScrollOffset = scrollOffset;
        desiredSize = size;
        if (!Document.HasLoadedHtml)
        {
            // The first layout needs the viewport before HTML is loaded.
            Size = desiredSize;
            ScrollOffset = desiredScrollOffset;
            return false;
        }
        return CheckViewportChange();
    }
}
