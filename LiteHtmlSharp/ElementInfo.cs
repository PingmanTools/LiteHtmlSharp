namespace LiteHtmlSharp;

/// <summary>Immutable custom-element layout snapshot in floating-point document coordinates.
/// Attributes are supplied separately to the creation callback; this snapshot does not own a native handle.</summary>
public readonly record struct ElementInfo(int Id, RectF Bounds)
{
    public float PosX => Bounds.X;
    public float PosY => Bounds.Y;
    public float Width => Bounds.Width;
    public float Height => Bounds.Height;
}
