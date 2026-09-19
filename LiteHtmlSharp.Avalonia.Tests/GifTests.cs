using Avalonia;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using LiteHtmlSharp.Avalonia;
using LiteHtmlSharp;
using SkiaSharp;
using Xunit;

public class GifTests
{
    static GifTests() => AppBuilder.Configure<Application>().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).UseSkia().SetupWithoutStarting();

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void FractionalBordersScaleWithTheRenderedContent(int scale)
    {
        using var host = new AvaloniaContainer(null, (IResourceLoader)null!);
        host.Document.Load("<style>html,body{margin:0;padding:0}div{width:20px;height:20px;background:blue;border:.5px solid black}</style><div></div>");
        host.Document.Render(80);
        var list = host.Document.Draw(0, 0, new RectF(0, 0, 80, 80));
        using var bitmap = new RenderTargetBitmap(new PixelSize(80 * scale, 80 * scale), new Vector(96 * scale, 96 * scale));
        using (var context = bitmap.CreateDrawingContext())
        {
            host.DrawingContext = context;
            DisplayListReplayer.Replay(list, host);
        }
        using var stream = new MemoryStream();
        bitmap.Save(stream);
        stream.Position = 0;
        using var pixels = SKBitmap.Decode(stream);
        var left = pixels.Width;
        var top = pixels.Height;
        var right = -1;
        var bottom = -1;
        for (var y = 0; y < pixels.Height; y++)
        for (var x = 0; x < pixels.Width; x++)
        {
            // Ignore the low-coverage antialiasing fringe outside the box.
            if (pixels.GetPixel(x, y).Alpha < 128) continue;
            left = Math.Min(left, x); top = Math.Min(top, y);
            right = Math.Max(right, x); bottom = Math.Max(bottom, y);
        }
        Assert.Equal(21 * scale, right - left + 1);
        Assert.Equal(21 * scale, bottom - top + 1);
        if (scale == 2)
        {
            var center = (left + right) / 2;
            Assert.Equal(SKColors.Black, pixels.GetPixel(center, top));
            Assert.Equal(SKColors.Blue, pixels.GetPixel(center, top + 1));
        }
    }

    [Fact]
    public void FontXHeightIsAPositiveDistanceForVerticalAlignment()
    {
        using var host = new AvaloniaContainer("", (string _) => "", (string _) => Array.Empty<byte>());
        var description = new FontDescription("Arial", 14, 0, 400, 0, 0, 0, 0, 0, default, "", default, 0);
        var font = host.CreateFont(description, out var metrics);
        try
        {
            Assert.InRange(metrics.XHeight, 1f, metrics.Ascent);
        }
        finally { host.DeleteFont(font); }
    }

    [Fact]
    public void ContainerRenderRoutesThroughHostAndReplacesChildControls()
    {
        using var host = new AvaloniaContainer("", (string _) => "", (string _) => Array.Empty<byte>());
        using var control = new LiteHtmlAvaloniaControl(null!, host, "", null!);
        host.Render("<input value='first'>");
        Assert.Single(control.Inputs);
        host.Render("<p>replacement</p>");
        Assert.Empty(control.Inputs);
        Assert.True(host.Document.HasLoadedHtml);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ControlDisposalHonorsContainerOwnership(bool ownsContainer)
    {
        using var host = new AvaloniaContainer("", (string _) => "", (string _) => Array.Empty<byte>());
        Func<string, bool> callback = tag => tag == "widget";
        host.ShouldCreateElementCallback = callback;
        var control = new LiteHtmlAvaloniaControl(null!, host, "", null!,
            createInteractiveElements: false, ownsContainer: ownsContainer);
        control.Dispose();
        Assert.Equal(ownsContainer, host.Document.IsDisposed);
        Assert.Same(callback, host.ShouldCreateElementCallback);
        if (!ownsContainer)
        {
            host.Render("<p>still usable</p>");
            host.Document.Render(100);
        }
    }

    [Fact]
    public void DecodesCompositedFramesWithRestorePreviousAndDelays()
    {
        using var source = AvaloniaImageSource.Decode(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "Fixtures", "disposal.gif")), CancellationToken.None);
        Assert.Equal(3, source.FrameCount);
        Assert.Equal(0, source.LoopCount);
        Assert.Equal(new double[] { 80, 140, 220 }, source.FrameDelays.Select(x => x.TotalMilliseconds));
        Assert.Equal(SKColors.Red, Pixel(source, 0, 0, 0));
        Assert.Equal(SKColors.Blue, Pixel(source, 1, 0, 0));
        Assert.Equal(SKColors.Red, Pixel(source, 2, 0, 0));
        Assert.Equal(SKColors.Green, Pixel(source, 2, 3, 3));
    }

    [Theory]
    [InlineData("disposal-previous.gif", 255)]
    [InlineData("disposal-background.gif", 0)]
    public void TransparentOverlaysHonorDisposal(string fixture, int expectedAlpha)
    {
        using var source = AvaloniaImageSource.Decode(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "Fixtures", fixture)), CancellationToken.None);
        Assert.Equal(SKColors.Blue, Pixel(source, 1, 0, 0));
        var restored = Pixel(source, 2, 0, 0);
        Assert.Equal(expectedAlpha, (int)restored.Alpha);
        if (expectedAlpha != 0) Assert.Equal(SKColors.Red, restored);
        Assert.Equal(SKColors.Green, Pixel(source, 2, 3, 3));
    }

    [Fact]
    public void CancellationAndMalformedImagesFailCleanly()
    {
        Assert.Throws<InvalidDataException>(() => AvaloniaImageSource.Decode(new byte[] { 1, 2, 3 }, CancellationToken.None));
        Assert.Throws<OperationCanceledException>(() => AvaloniaImageSource.Decode(File.ReadAllBytes(
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "disposal.gif")), new CancellationToken(true)));
    }

    [Fact]
    public async Task GifFramesReuseDisplayListWithoutNativeRender()
    {
        using var clock = new FrameClock(automatic: false);
        using var host = new DecodedContainer(clock);
        await host.LoadImageAsync("fixture", "");
        host.Document.Load("<img src='fixture' width='4' height='4'>");
        var renders = 0;
        host.Document.ViewElementsNeedLayout += () => renders++;
        host.Document.Render(100);
        var snapshot = host.Document.Draw(0, 0, host.Viewport);
        var frame = host.GetImageFrame("fixture", "");
        clock.Advance(TimeSpan.FromMilliseconds(90));
        Assert.NotSame(frame, host.GetImageFrame("fixture", ""));
        Assert.Same(snapshot, host.Document.LastDisplayList);
        Assert.Equal(1, renders);
        Console.WriteLine($"GIF advanced: native Render calls before=1, after={renders}; retained display list reused.");
    }

    [Theory]
    [InlineData("", "", 0, 128)]
    [InlineData("position:relative;", "", 0, 128)]
    [InlineData("display:inline-block;", "", 0, 128)]
    [InlineData("float:left;", "", 0, 128)]
    [InlineData("background:green;", "", 0, 128)]
    [InlineData("position:relative;", "position:absolute;left:20px;top:0;margin:0;z-index:2;", 0, 128)]
    [InlineData("position:relative;", "position:absolute;left:20px;top:0;margin:0;z-index:-1;", 0, 128)]
    [InlineData("", "opacity:.5;", 0, 128)]
    [InlineData("animation:fade 1s linear forwards;", "", 500, 128)]
    public void ParentOpacityCompositesOverlappingChildren(string parentStyle, string childStyle, int time, int alpha)
    {
        using var host = new AvaloniaContainer(null, (IResourceLoader)null!);
        host.Document.Load("<style>html,body{margin:0;padding:0;background:transparent}"
            + "@keyframes fade{from{opacity:1}to{opacity:0}}"
            + "#group{opacity:.5;width:80px;height:40px;" + parentStyle + "}"
            + "#blue,#orange{width:40px;height:40px;}"
            + "#blue{background:blue}#orange{background:#ff8000;margin-left:20px;margin-top:-40px;" + childStyle + "}"
            + "</style><div id='group'><div id='blue'></div><div id='orange'></div></div>");
        host.Document.SetTime(0); host.Document.Render(100);
        host.Document.SetTime(time); host.Document.Render(100);
        var list = host.Document.Draw(0, 0, new RectF(0, 0, 100, 100));
        using var bitmap = new RenderTargetBitmap(new PixelSize(100, 100), new Vector(96, 96));
        using (var context = bitmap.CreateDrawingContext())
        {
            host.DrawingContext = context;
            DisplayListReplayer.Replay(list, host);
        }
        using var stream = new MemoryStream();
        bitmap.Save(stream);
        using var pixels = SKBitmap.Decode(stream.ToArray());
        var overlap = pixels.GetPixel(30, 10);
        Assert.InRange((int)overlap.Alpha, alpha - 1, alpha + 1);
        if (!childStyle.Contains("opacity:"))
        {
            var negative = childStyle.Contains("z-index:-1");
            Assert.InRange((int)overlap.Red, negative ? 0 : 253, negative ? 1 : 255);
            Assert.InRange((int)overlap.Blue, negative ? 253 : 0, negative ? 255 : 1);
        }
        else
        {
            // The half-opacity orange child blends with opaque blue INSIDE
            // the parent; the resulting group still has only 50% coverage.
            Assert.InRange((int)overlap.Red, 126, 129);
            Assert.InRange((int)overlap.Blue, 126, 129);
        }
        Assert.InRange((int)pixels.GetPixel(10, 10).Alpha, 127, 129);
    }

    [Theory]
    [InlineData("table")]
    [InlineData("tr")]
    [InlineData("td")]
    public void TableOpacityGroupsBackgroundAndContent(string element)
    {
        using var host = new AvaloniaContainer(null, (IResourceLoader)null!);
        host.Document.Load("<style>html,body{margin:0;padding:0;background:transparent}"
            + "table{border-spacing:0}td{padding:0;width:60px;height:40px}"
            + element + "{opacity:.5;background:blue}div{width:40px;height:40px;background:#ff8000}"
            + "</style><table><tr><td><div></div></td></tr></table>");
        host.Document.Render(100);
        var list = host.Document.Draw(0, 0, new RectF(0, 0, 100, 100));
        using var bitmap = new RenderTargetBitmap(new PixelSize(100, 100), new Vector(96, 96));
        using (var context = bitmap.CreateDrawingContext())
        {
            host.DrawingContext = context;
            DisplayListReplayer.Replay(list, host);
        }
        using var stream = new MemoryStream();
        bitmap.Save(stream);
        using var pixels = SKBitmap.Decode(stream.ToArray());
        var overlap = pixels.GetPixel(10, 10);
        Assert.InRange((int)overlap.Alpha, 127, 129);
        Assert.InRange((int)overlap.Red, 253, 255);
        Assert.InRange((int)overlap.Blue, 0, 1);
    }

    private sealed class DecodedContainer(FrameClock clock) : Container(frameClock: clock)
    {
        protected override ValueTask<IImageSource?> LoadImageSourceAsync(string source, string baseUrl, CancellationToken token)
            => ValueTask.FromResult<IImageSource?>(AvaloniaImageSource.Decode(File.ReadAllBytes(
                Path.Combine(AppContext.BaseDirectory, "Fixtures", "disposal.gif")), token));
    }

    private static SKColor Pixel(IImageSource source, int frame, int x, int y)
    {
        using var stream = new MemoryStream();
        ((Bitmap)source.GetFrame(frame)).Save(stream);
        using var bitmap = SKBitmap.Decode(stream.ToArray());
        return bitmap.GetPixel(x, y);
    }
}
