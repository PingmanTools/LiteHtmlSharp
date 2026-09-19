using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using CoreGraphics;
using Foundation;
using ImageIO;
namespace LiteHtmlSharp.CoreGraphics;

/// <summary>Owns ImageIO's fully composited canvas images, including GIF disposal results.</summary>
public sealed class CGImageSourceFrames : IImageSource
{
    private readonly List<CGImage> frames = new();
    private readonly List<TimeSpan> delays = new();
    private bool disposed;
    public SizeF Size { get; private set; }
    public int FrameCount => frames.Count;
    public IReadOnlyList<TimeSpan> FrameDelays => delays;
    public int LoopCount { get; private set; } = 1;
    public static CGImageSourceFrames Decode(byte[] bytes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        cancellationToken.ThrowIfCancellationRequested();
        using var data = NSData.FromArray(bytes);
        using var source = CGImageSource.FromData(data) ??
                           throw new InvalidDataException("ImageIO could not decode the image.");
        if (source.ImageCount < 1 || source.ImageCount > 10000)
            throw new InvalidDataException("The image has an unsupported frame count.");
        var result = new CGImageSourceFrames();
        try
        {
            using var properties = source.CopyProperties((NSDictionary)null);
            if (properties?[ImageIO.CGImageProperties.GIFDictionary] is NSDictionary gif &&
                gif[ImageIO.CGImageProperties.GIFLoopCount] is NSNumber loops)
            {
                // ImageIO already converts the GIF repeat count into complete plays.
                result.LoopCount = Math.Max(0, loops.Int32Value);
            }
            long pixels = 0;
            for (var i = 0; i < source.ImageCount; ++i)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var frameProperties = source.CopyProperties((NSDictionary)null, i);
                var width =
                    (frameProperties?[ImageIO.CGImageProperties.PixelWidth] as NSNumber)?.Int64Value ?? 0;
                var height =
                    (frameProperties?[ImageIO.CGImageProperties.PixelHeight] as NSNumber)?.Int64Value ?? 0;
                if (width <= 0 || height <= 0 || checked(width * height) > 64L * 1024 * 1024 - pixels)
                    throw new InvalidDataException("Decoded image frames exceed 256 MiB.");
                // ImageIO resolves frame rectangles and disposal before returning the full canvas.
                var frame = source.CreateImage(i, new CGImageOptions { ShouldCache = true,
                                                                       ShouldCacheImmediately = true }) ??
                            throw new InvalidDataException("ImageIO could not decode a frame.");
                result.frames.Add(frame);
                pixels = checked(pixels + (long)frame.Width * (long)frame.Height);
                if (pixels > 64L * 1024 * 1024)
                    throw new InvalidDataException("Decoded image frames exceed the memory limit.");
                if (i == 0)
                    result.Size = new SizeF((float)frame.Width, (float)frame.Height);
                var seconds = 0.1;
                if (frameProperties?[ImageIO.CGImageProperties.GIFDictionary] is NSDictionary frameGif)
                {
                    var delay = frameGif[ImageIO.CGImageProperties.GIFUnclampedDelayTime] as NSNumber ??
                                frameGif[ImageIO.CGImageProperties.GIFDelayTime] as NSNumber;
                    if (delay != null && double.IsFinite(delay.DoubleValue) && delay.DoubleValue > 0)
                        seconds = delay.DoubleValue;
                }
                result.delays.Add(TimeSpan.FromSeconds(seconds));
            }
            return result;
        }
        catch
        {
            result.Dispose();
            throw;
        }
    }
    public object GetFrame(int index)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return frames[index];
    }
    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        foreach (var frame in frames)
            frame.Dispose();
        frames.Clear();
    }
}
