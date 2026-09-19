using LiteHtmlSharp.Wpf;
using System.Text;
using Xunit;

namespace LiteHtmlSharp.Tests;

public class WpfGifTests
{
    private static byte[] Fixture() => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "WpfFixtures", "disposal.gif"));
    private static byte[] Patch(GifFrameData frame, byte blue = 0, byte green = 0, byte red = 0, byte alpha = 0)
    {
        var bytes = new byte[frame.Width * frame.Height * 4];
        for (var i = 0; i < bytes.Length; i += 4) { bytes[i] = blue; bytes[i + 1] = green; bytes[i + 2] = red; bytes[i + 3] = alpha; }
        return bytes;
    }
    private static byte[] Pixel(byte[] bytes, int width, int x, int y) => bytes.AsSpan((y * width + x) * 4, 4).ToArray();

    [Fact]
    public void ReadsGifFrameOffsetsDelaysAndLoopCounts()
    {
        var bytes = Fixture(); var metadata = GifMetadata.Parse(bytes);
        Assert.Equal(4, metadata.Width); Assert.Equal(4, metadata.Height);
        Assert.Equal(3, metadata.Frames.Count); Assert.Equal(0, metadata.LoopCount);
        Assert.Equal(new[] { 80, 140, 220 }, metadata.Frames.Select(frame => frame.DelayMilliseconds));
        Assert.Equal(3, metadata.Frames[1].Disposal);
        var extension = Encoding.ASCII.GetString(bytes).IndexOf("NETSCAPE2.0", StringComparison.Ordinal);
        Assert.True(extension > 0);
        bytes[extension + 13] = 2;
        Assert.Equal(3, GifMetadata.Parse(bytes).LoopCount);
    }

    [Fact]
    public void CompositesTransparentPatchesAndRestoresPreviousCanvas()
    {
        var metadata = GifMetadata.Parse(Fixture()); var compositor = new GifCompositor(metadata);
        var full = new GifFrameData(0, 0, 4, 4, 1, 80, true);
        var red = compositor.Compose(full, Patch(full, red: 255, alpha: 255));
        var overlay = new GifFrameData(0, 0, 1, 1, 3, 140, true);
        var blue = compositor.Compose(overlay, Patch(overlay, blue: 255, alpha: 255));
        var last = new GifFrameData(3, 3, 1, 1, 1, 220, true);
        var green = compositor.Compose(last, Patch(last, green: 128, alpha: 255));
        Assert.Equal(new byte[] { 0, 0, 255, 255 }, Pixel(red, 4, 0, 0));
        Assert.Equal(new byte[] { 255, 0, 0, 255 }, Pixel(blue, 4, 0, 0));
        Assert.Equal(new byte[] { 0, 0, 255, 255 }, Pixel(green, 4, 0, 0));
        Assert.Equal(new byte[] { 0, 128, 0, 255 }, Pixel(green, 4, 3, 3));
        Assert.Equal(new byte[] { 0, 0, 255, 255 }, Pixel(red, 4, 3, 3));
    }

    [Fact]
    public void BackgroundDisposalClearsOnlyPreviousPatchAndPreservesAlpha()
    {
        var metadata = GifMetadata.Parse(Fixture()); var compositor = new GifCompositor(metadata);
        var full = new GifFrameData(0, 0, 4, 4, 1, 100, true);
        compositor.Compose(full, Patch(full, red: 255, alpha: 255));
        var clear = new GifFrameData(1, 1, 1, 1, 2, 100, true);
        compositor.Compose(clear, Patch(clear, blue: 255, alpha: 255));
        var transparent = new GifFrameData(0, 0, 1, 1, 1, 100, true);
        var result = compositor.Compose(transparent, Patch(transparent));
        Assert.Equal(new byte[4], Pixel(result, 4, 1, 1));
        Assert.Equal(new byte[] { 0, 0, 255, 255 }, Pixel(result, 4, 0, 0));
    }

    [Fact]
    public void RejectsMalformedAndTruncatedMetadata()
    {
        Assert.Throws<InvalidDataException>(() => GifMetadata.Parse(new byte[] { 1, 2, 3 }));
        Assert.Throws<InvalidDataException>(() => GifMetadata.Parse(Fixture()[..^1]));
        var bytes = Fixture(); bytes[6] = 0; bytes[7] = 0;
        Assert.Throws<InvalidDataException>(() => GifMetadata.Parse(bytes));
    }
    [Theory]
    [InlineData("disposal-previous.gif", 3, 255)]
    [InlineData("disposal-background.gif", 2, 0)]
    public void PreciseFixtureMetadataDrivesDisposalWithoutRepaintingTheClearedPixel(string fixture, int disposal, byte expectedAlpha)
    {
        var metadata = GifMetadata.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "WpfFixtures", fixture)));
        Assert.Equal(new[] { 80, 140, 220 }, metadata.Frames.Select(frame => frame.DelayMilliseconds));
        Assert.Equal(0, metadata.LoopCount);
        Assert.Equal(disposal, metadata.Frames[1].Disposal);
        var compositor = new GifCompositor(metadata);
        compositor.Compose(metadata.Frames[0], Patch(metadata.Frames[0], red: 255, alpha: 255));
        var blueFrame = metadata.Frames[1];
        var blue = Patch(blueFrame);
        var blueOffset = ((0 - blueFrame.Top) * blueFrame.Width + 0 - blueFrame.Left) * 4;
        blue[blueOffset] = 255; blue[blueOffset + 3] = 255;
        compositor.Compose(blueFrame, blue);
        var greenFrame = metadata.Frames[2];
        var green = Patch(greenFrame);
        var greenOffset = ((3 - greenFrame.Top) * greenFrame.Width + 3 - greenFrame.Left) * 4;
        green[greenOffset + 1] = 128; green[greenOffset + 3] = 255;
        var result = compositor.Compose(greenFrame, green);
        Assert.Equal(expectedAlpha, Pixel(result, metadata.Width, 0, 0)[3]);
        if (disposal == 3) Assert.Equal(new byte[] { 0, 0, 255, 255 }, Pixel(result, metadata.Width, 0, 0));
        Assert.Equal(new byte[] { 0, 128, 0, 255 }, Pixel(result, metadata.Width, 3, 3));
    }

    [Theory]
    [InlineData(int.MaxValue, int.MaxValue, int.MaxValue)]
    [InlineData(int.MaxValue, int.MaxValue, 1)]
    [InlineData(1, 1, int.MaxValue)]
    [InlineData(0, 1, 1)]
    [InlineData(1, -1, 1)]
    [InlineData(1, 1, 0)]
    public void RejectsInvalidOrOversizedDecodeBudgetsWithoutOverflow(int width, int height, int count)
        => Assert.Throws<InvalidDataException>(() => ImageDecodeBudget.Validate(width, height, count));

    [Fact]
    public void AcceptsExactDecodedBudgetBoundary() => ImageDecodeBudget.Validate(8192, 8192, 1);

}
