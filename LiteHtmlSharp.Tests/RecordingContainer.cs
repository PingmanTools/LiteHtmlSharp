using System.Numerics;

namespace LiteHtmlSharp.Tests;

internal sealed class RecordingContainer : Container
{
    private nuint _nextFont;
    private int _nextElement;
    public HashSet<nuint> LiveFonts { get; } = [];
    public int FontsCreated { get; private set; }
    public int FontsDeleted { get; private set; }
    public bool ThrowOnTextWidth { get; set; }
    public IReadOnlyDictionary<string, string> CustomAttributes { get; private set; } = new Dictionary<string, string>();

    public override RectF Viewport => new(0, 0, 300, 300);

    public override nuint CreateFont(FontDescription description, out FontMetrics metrics)
    {
        metrics = new FontMetrics(description.Size, 16, 12, 4, 8, 10);
        var handle = ++_nextFont;
        LiveFonts.Add(handle);
        FontsCreated++;
        return handle;
    }

    public override void DeleteFont(nuint font)
    {
        if (!LiveFonts.Remove(font))
            throw new InvalidOperationException("Font was deleted twice or was never created.");
        FontsDeleted++;
    }

    public override float TextWidth(string text, nuint font)
    {
        if (ThrowOnTextWidth)
            throw new InvalidOperationException("Synthetic font measurement failure.");
        if (!LiveFonts.Contains(font))
            throw new InvalidOperationException("Font handle is no longer live.");
        var count = 0;
        foreach (var _ in text.EnumerateRunes()) count++;
        return count * 10;
    }

    protected override bool ShouldCreateElement(string tag) => tag is "input" or "button";

    protected override int CreateElement(string tag, IReadOnlyDictionary<string, string> attributes, out SizeF size)
    {
        CustomAttributes = new Dictionary<string, string>(attributes);
        size = new SizeF(80, 24);
        return ++_nextElement;
    }

    public List<float> DecorationOpacities { get; } = [];
    public override void DrawText(RectF position, ColorRgba color, nuint font, string text, float decorationOpacity)
        => DecorationOpacities.Add(decorationOpacity);

    public List<Matrix4x4> Transforms { get; } = [];
    public List<Layer> Fills { get; } = [];
    public int DrawingScopeDepth { get; private set; }
    public bool ThrowOnFill { get; set; }

    public override void PushTransform(Matrix4x4 transform)
    {
        Transforms.Add(transform);
        DrawingScopeDepth++;
    }

    public List<float> OpacityGroups { get; } = [];
    public override void PushOpacity(float opacity) { OpacityGroups.Add(opacity); DrawingScopeDepth++; }
    public override void PopOpacity() => DrawingScopeDepth--;
    public override void PopTransform() => DrawingScopeDepth--;
    public override void PushClip(RectF position, BorderRadii radii) => DrawingScopeDepth++;
    public override void PopClip() => DrawingScopeDepth--;

    public override void FillRect(Layer layer, ColorRgba color)
    {
        if (ThrowOnFill) throw new InvalidOperationException("Synthetic drawing failure.");
        Fills.Add(layer);
    }
}
