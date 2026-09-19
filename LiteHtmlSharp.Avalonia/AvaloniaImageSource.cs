using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SkiaSharp;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("LiteHtmlSharp.Avalonia.Tests")]

namespace LiteHtmlSharp.Avalonia;

internal sealed class AvaloniaImageSource : IImageSource
{
    private readonly List<Bitmap> frames = new();
    private readonly List<TimeSpan> delays = new();
    public SizeF Size { get; private set; }
    public int FrameCount => frames.Count;
    public IReadOnlyList<TimeSpan> FrameDelays => delays;
    public int LoopCount { get; private set; }
    public object GetFrame(int index) => frames[index];

    internal static AvaloniaImageSource Decode(byte[] bytes, CancellationToken cancellationToken)
    {
        using var data = SKData.CreateCopy(bytes);
        using var codec = SKCodec.Create(data) ?? throw new InvalidDataException("Unsupported image format.");
        var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        var count = Math.Max(1, codec.FrameCount);
        // Bound eagerly decoded frames before allocating native and platform bitmaps.
        if (info.Width <= 0 || info.Height <= 0 || (long)info.Width * info.Height > (256L * 1024 * 1024) / (4L * count))
            throw new InvalidDataException("Decoded image exceeds 256 MiB.");
        var result = new AvaloniaImageSource
        {
            Size = new SizeF(info.Width, info.Height),
            LoopCount = codec.RepetitionCount < 0 ? 0 : checked(codec.RepetitionCount + 1)
        };
        try
        {
            var metadata = codec.FrameInfo;
            using var pixels = new SKBitmap(info);
            for (var i = 0; i < count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Reuse the already-composited canvas only when Skia's dependency
                // and disposal contract permits it. RestorePrevious needs fallback.
                var reuse = i > 0 && i < metadata.Length && metadata[i].RequiredFrame >= 0 &&
                    metadata[i].RequiredFrame <= i - 1 &&
                    metadata[i - 1].DisposalMethod != SKCodecAnimationDisposalMethod.RestorePrevious;
                if (!reuse) pixels.Erase(SKColors.Transparent);
                var status = codec.GetPixels(info, pixels.GetPixels(), new SKCodecOptions(i, reuse ? i - 1 : -1));
                if (status != SKCodecResult.Success)
                    throw new InvalidDataException($"Image frame {i} could not be decoded: {status}.");
                result.frames.Add(new Bitmap(PixelFormat.Bgra8888, AlphaFormat.Premul, pixels.GetPixels(),
                    new PixelSize(info.Width, info.Height), new Vector(96, 96), pixels.RowBytes));
                result.delays.Add(TimeSpan.FromMilliseconds(i < metadata.Length && metadata[i].Duration > 0
                    ? metadata[i].Duration : 100));
            }
            return result;
        }
        catch
        {
            result.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        foreach (var frame in frames) frame.Dispose();
        frames.Clear();
    }
}
