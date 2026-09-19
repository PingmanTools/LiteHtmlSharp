#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LiteHtmlSharp.Wpf;

internal static class ImageDecodeBudget
{
    internal static void Validate(int width, int height, int count)
    {
        const long limit = 256L * 1024 * 1024;
        if (width <= 0 || height <= 0 || count <= 0 || (long)width * height > limit / (4L * count))
            throw new InvalidDataException("Decoded image dimensions are invalid or exceed 256 MiB.");
    }
}

internal sealed record GifFrameData(int Left, int Top, int Width, int Height, int Disposal, int DelayMilliseconds, bool Transparent);

internal sealed class GifMetadata
{
    internal int Width { get; private init; }
    internal int Height { get; private init; }
    internal int LoopCount { get; private set; } = 1;
    internal uint Background { get; private set; }
    internal List<GifFrameData> Frames { get; } = new();

    internal static bool IsGif(byte[] data) => data.Length >= 6 &&
        (Encoding.ASCII.GetString(data, 0, 6) is "GIF87a" or "GIF89a");

    internal static GifMetadata Parse(byte[] data)
    {
        if (!IsGif(data)) throw new InvalidDataException("Invalid GIF signature.");
        var reader = new Reader(data, 6);
        var result = new GifMetadata { Width = reader.Word(), Height = reader.Word() };
        if (result.Width == 0 || result.Height == 0) throw new InvalidDataException("Empty GIF canvas.");
        var flags = reader.Byte(); var backgroundIndex = reader.Byte(); reader.Byte();
        if ((flags & 128) != 0)
        {
            var colors = 1 << ((flags & 7) + 1);
            for (var i = 0; i < colors; i++)
            {
                var red = reader.Byte(); var green = reader.Byte(); var blue = reader.Byte();
                if (i == backgroundIndex) result.Background = (uint)(blue | green << 8 | red << 16) | 0xff000000;
            }
        }
        var disposal = 0; var delay = 100; var transparent = false;
        while (true)
        {
            var marker = reader.Byte();
            if (marker == 0x3b) break;
            if (marker == 0x21)
            {
                var label = reader.Byte();
                if (label == 0xf9)
                {
                    if (reader.Byte() != 4) throw new InvalidDataException("Invalid GIF graphic control extension.");
                    var control = reader.Byte(); disposal = (control >> 2) & 7; transparent = (control & 1) != 0;
                    delay = reader.Word() * 10; if (delay == 0) delay = 100;
                    reader.Byte();
                    if (reader.Byte() != 0) throw new InvalidDataException("Unterminated GIF graphic control extension.");
                }
                else if (label == 0xff)
                {
                    var application = Encoding.ASCII.GetString(reader.Block());
                    var loopExtension = application is "NETSCAPE2.0" or "ANIMEXTS1.0";
                    byte[] block;
                    while ((block = reader.Block()).Length != 0)
                        if (loopExtension && block.Length >= 3 && block[0] == 1)
                        {
                            var repetitions = block[1] | block[2] << 8;
                            result.LoopCount = repetitions == 0 ? 0 : repetitions + 1;
                        }
                }
                else
                {
                    reader.SkipBlocks();
                    // Graphic-control extensions apply to the next rendering block, including plain text.
                    if (label == 1) { disposal = 0; delay = 100; transparent = false; }
                }
                continue;
            }
            if (marker != 0x2c) throw new InvalidDataException("Invalid GIF block.");
            var left = reader.Word(); var top = reader.Word(); var width = reader.Word(); var height = reader.Word();
            if (width == 0 || height == 0 || left + width > result.Width || top + height > result.Height)
                throw new InvalidDataException("GIF frame exceeds its canvas.");
            result.Frames.Add(new(left, top, width, height, disposal, delay, transparent));
            var imageFlags = reader.Byte();
            if ((imageFlags & 128) != 0) reader.Skip(3 * (1 << ((imageFlags & 7) + 1)));
            reader.Byte(); reader.SkipBlocks();
            disposal = 0; delay = 100; transparent = false;
        }
        if (result.Frames.Count == 0) throw new InvalidDataException("GIF has no image frames.");
        return result;
    }

    private sealed class Reader(byte[] data, int position)
    {
        internal int Byte()
        {
            if (position >= data.Length) throw new InvalidDataException("Truncated GIF.");
            return data[position++];
        }
        internal int Word() => Byte() | Byte() << 8;
        internal void Skip(int count)
        {
            if (count < 0 || count > data.Length - position) throw new InvalidDataException("Truncated GIF block.");
            position += count;
        }
        internal byte[] Block()
        {
            var size = Byte(); var start = position; Skip(size);
            return data.AsSpan(start, size).ToArray();
        }
        internal void SkipBlocks()
        {
            int size; while ((size = Byte()) != 0) Skip(size);
        }
    }
}

internal sealed class GifCompositor
{
    private readonly GifMetadata metadata;
    private readonly byte[] canvas;
    private byte[]? previous;
    private GifFrameData? last;
    internal GifCompositor(GifMetadata metadata)
    {
        this.metadata = metadata;
        canvas = new byte[checked(metadata.Width * metadata.Height * 4)];
        Fill(0, 0, metadata.Width, metadata.Height, metadata.Frames[0].Transparent ? 0 : metadata.Background);
    }
    internal byte[] Compose(GifFrameData frame, byte[] pixels)
    {
        if (pixels.Length != checked(frame.Width * frame.Height * 4)) throw new InvalidDataException("GIF patch dimensions differ from metadata.");
        if (last?.Disposal == 2) Fill(last.Left, last.Top, last.Width, last.Height, last.Transparent ? 0 : metadata.Background);
        else if (last?.Disposal == 3 && previous != null) previous.CopyTo(canvas, 0);
        previous = frame.Disposal == 3 ? (byte[])canvas.Clone() : null;
        for (var y = 0; y < frame.Height; y++)
            for (var x = 0; x < frame.Width; x++)
            {
                var source = (y * frame.Width + x) * 4;
                var target = ((frame.Top + y) * metadata.Width + frame.Left + x) * 4;
                var inverse = 255 - pixels[source + 3];
                for (var channel = 0; channel < 4; channel++)
                    canvas[target + channel] = (byte)Math.Min(255, pixels[source + channel] + (canvas[target + channel] * inverse + 127) / 255);
            }
        last = frame;
        return (byte[])canvas.Clone();
    }
    private void Fill(int left, int top, int width, int height, uint color)
    {
        for (var y = top; y < top + height; y++)
            for (var x = left; x < left + width; x++)
            {
                var offset = (y * metadata.Width + x) * 4;
                canvas[offset] = (byte)color; canvas[offset + 1] = (byte)(color >> 8);
                canvas[offset + 2] = (byte)(color >> 16); canvas[offset + 3] = (byte)(color >> 24);
            }
    }
}
