using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LiteHtmlSharp.Wpf;

internal sealed class WpfImageSource : IImageSource
{
    private readonly List<BitmapSource> frames = new();
    private readonly List<TimeSpan> delays = new();
    public SizeF Size { get; private set; }
    public int FrameCount => frames.Count;
    public IReadOnlyList<TimeSpan> FrameDelays => delays;
    public int LoopCount { get; private set; } = 1;
    public object GetFrame(int index) => frames[index];

    internal static WpfImageSource Decode(byte[] bytes, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = new MemoryStream(bytes, false);
        var gif = GifMetadata.IsGif(bytes) ? GifMetadata.Parse(bytes) : null;
        if (gif != null) ImageDecodeBudget.Validate(gif.Width, gif.Height, gif.Frames.Count);
        BitmapDecoder decoder = gif != null
            ? new GifBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None)
            : BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);
        var count = gif?.Frames.Count ?? 1;
        if (decoder.Frames.Count < count) throw new InvalidDataException("GIF frame metadata differs from decoded frames.");
        var width = gif?.Width ?? decoder.Frames[0].PixelWidth;
        var height = gif?.Height ?? decoder.Frames[0].PixelHeight;
        // Eager frames retain independent platform bitmaps, so bound the aggregate decoded size.
        ImageDecodeBudget.Validate(width, height, count);
        var result = new WpfImageSource { Size = new(width, height), LoopCount = gif?.LoopCount ?? 1 };
        try
        {
            var compositor = gif != null ? new GifCompositor(gif) : null;
            for (var i = 0; i < count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var frame = decoder.Frames[i];
                if (gif == null)
                {
                    var convertedFrame = new FormatConvertedBitmap(frame, PixelFormats.Pbgra32, null, 0);
                    var staticPixels = new byte[checked(width * height * 4)];
                    convertedFrame.CopyPixels(staticPixels, width * 4, 0);
                    var staticBitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Pbgra32, null, staticPixels, width * 4);
                    staticBitmap.Freeze(); result.frames.Add(staticBitmap); result.delays.Add(TimeSpan.FromMilliseconds(100));
                    continue;
                }
                var metadata = gif.Frames[i];
                if (frame.PixelWidth != metadata.Width || frame.PixelHeight != metadata.Height)
                    throw new InvalidDataException("Decoded GIF patch dimensions differ from its descriptor.");
                var converted = new FormatConvertedBitmap(frame, PixelFormats.Pbgra32, null, 0);
                var patch = new byte[checked(metadata.Width * metadata.Height * 4)];
                converted.CopyPixels(patch, metadata.Width * 4, 0);
                var pixels = compositor.Compose(metadata, patch);
                var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Pbgra32, null, pixels, width * 4);
                bitmap.Freeze(); result.frames.Add(bitmap);
                result.delays.Add(TimeSpan.FromMilliseconds(metadata.DelayMilliseconds));
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
        // WPF owns native bitmap handles; releasing frozen frame references permits their finalization.
        frames.Clear(); delays.Clear();
    }
}
