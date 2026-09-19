using System;
using System.Collections.Generic;
using CoreGraphics;
namespace LiteHtmlSharp.CoreGraphics;

public sealed class ImageHolder : IImageSource
{
    public CGImage Image { get; set; }
    public CGSize Size { get; set; }
    SizeF IImageSource.Size => new((float)Size.Width, (float)Size.Height);
    public int FrameCount => 1;
    public IReadOnlyList<TimeSpan> FrameDelays { get; } = new[] { TimeSpan.FromMilliseconds(100) };
    public int LoopCount => 1;
    public object GetFrame(int index)
    {
        if (index != 0)
            throw new ArgumentOutOfRangeException(nameof(index));
        return Image ?? throw new ObjectDisposedException(nameof(ImageHolder));
    }
    public void Dispose()
    {
        Image?.Dispose();
        Image = null;
    }
}
