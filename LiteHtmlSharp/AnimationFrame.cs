namespace LiteHtmlSharp;

/// <summary>Scheduling information produced by rendering a frame.</summary>
/// <param name="Width">Rendered width.</param>
/// <param name="NextDelay">Milliseconds until the next frame, or -1 until input, style, or viewport changes.</param>
/// <param name="Active">Includes off-screen motion that must be resampled when it becomes visible.</param>
public readonly record struct AnimationFrame(float Width, double NextDelay, bool Active);
