namespace LiteHtmlSharp;

/// <summary>Owns decoded, fully composited frames and their platform drawing handles.</summary>
public interface IImageSource : IDisposable
{
    SizeF Size
    {
        get;
    }
    int FrameCount
    {
        get;
    }
    IReadOnlyList<TimeSpan> FrameDelays
    {
        get;
    }
    /// <summary>Number of complete plays; zero repeats indefinitely.</summary>
    int LoopCount
    {
        get;
    }
    /// <summary>The returned handle remains owned by the source.</summary>
    object GetFrame(int index);
}
