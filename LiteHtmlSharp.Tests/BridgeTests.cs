using LiteHtmlSharp.Interop;
using Xunit;

namespace LiteHtmlSharp.Tests;

public class BridgeTests
{
    private const string Reset = "<style>html,body{margin:0;padding:0}</style>";

    private static DisplayList Paint(RecordingContainer container, string html)
    {
        container.Document.Load(Reset + html);
        container.Document.Render(300);
        return container.Document.Draw(0, 0, new RectF(0, 0, 300, 300));
    }

    private static List<T> Commands<T>(DisplayList list, CommandType type) where T : unmanaged
    {
        var result = new List<T>();
        foreach (var command in list)
            if (command.Type == type)
                result.Add(command.Read<T>());
        return result;
    }

    [Fact]
    public void CustomElementPlacementSurvivesMediaChangesAndResizes()
    {
        using var host = new ViewportContainer();
        host.ShouldCreateElementCallback = tag => tag == "input";
        host.CreateElementCallback = (string tag, IReadOnlyDictionary<string, string> attrs, out SizeF size) =>
        { size = new(80, 24); return 1; };
        host.Document.Load(Reset + "<style>input{display:block;width:50%;height:24px}@media(min-width:250px){input{height:30px}}</style><input>");
        foreach (var width in new float[] { 300, 180, 420, 300 })
        {
            host.Size = new(width, 300);
            host.Document.OnMediaChanged();
            host.Document.Render(width);
            var info = host.Document.GetElementInfo(1);
            Assert.NotNull(info);
            Assert.Equal(width / 2, info.Value.Width);
            Assert.Equal(width >= 250 ? 30 : 24, info.Value.Height);
        }
    }

    [Fact]
    public void ExplicitTablePlacementAfterMediaRebuildMatchesFreshLayout()
    {
        const string html = "<style>html,body{margin:0} .table{display:table;width:100%}.row{display:table-row}.cell{display:table-cell;width:100%}input{display:inline-block;width:40px;height:27px}.label{display:inline-block;width:35%} @media(min-width:500px){.cell{color:red}}</style><div class='table'><div class='row'><div class='cell'><span class='label'>Label</span><input></div></div></div>";
        static ViewportContainer Make(float width)
        {
            var host = new ViewportContainer { Size = new(width, 300) };
            host.ShouldCreateElementCallback = tag => tag == "input";
            host.CreateElementCallback = (string tag, IReadOnlyDictionary<string, string> attrs, out SizeF size) =>
            { size = new(40, 27); return 1; };
            return host;
        }
        using var resized = Make(300);
        resized.Document.Load(html);
        resized.Document.Render(300);
        foreach (var width in new float[] { 740, 300, 740, 300 })
        {
            resized.Size = new(width, 300);
            resized.Document.OnMediaChanged();
            resized.Document.Render(width);
            using var fresh = Make(width);
            fresh.Document.Load(html);
            fresh.Document.Render(width);
            Assert.Equal(fresh.Document.GetElementInfo(1), resized.Document.GetElementInfo(1));
            Assert.Equal(fresh.Document.Height(), resized.Document.Height());
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("<meta charset='windows-1252'>")]
    [InlineData("<meta charset='shift_jis'>")]
    [InlineData("<meta http-equiv='Content-Type' content='text/html; charset=big5'>")]
    public void ManagedUnicodeIgnoresLegacyCharsetDeclarations(string meta)
    {
        using var host = new RecordingContainer();
        const string expected = "café日本語中文😀&é";
        var list = Paint(host, "<html><head>" + meta +
            "</head><body><p>café日本語中文😀&amp;&#233;</p></body></html>");
        var actual = string.Concat(Commands<lh_cmd_text>(list, CommandType.Text)
            .Select(command => list.GetText(command.utf8)));
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OffscreenTransformDoesNotChangeVisibleCommandsButVisibleMotionDoes()
    {
        using var host = new RecordingContainer();
        host.Document.Load(Reset + "<style>@keyframes slide{to{transform:translateX(50px)}}div{position:absolute;top:500px;width:20px;height:20px;background:red;animation:slide 1s linear infinite}</style><p>Static text</p><div></div>");
        host.Document.SetTime(0); host.Document.Render(300);
        var first = host.Document.Draw(0, 0, new RectF(0, 0, 300, 100));
        host.Document.SetTime(500); host.Document.Render(300);
        Assert.True(first.ContentEquals(host.Document.Draw(0, 0, new RectF(0, 0, 300, 100))));
        var visible = host.Document.Draw(0, -500, new RectF(0, 0, 300, 100));
        host.Document.SetTime(750); host.Document.Render(300);
        Assert.False(visible.ContentEquals(host.Document.Draw(0, -500, new RectF(0, 0, 300, 100))));
    }

    [Fact]
    public void FixedWidthDivProducesRedFill()
    {
        using var host = new RecordingContainer();
        var list = Paint(host, "<div style='width:100px;height:50px;background:red'></div>");
        var fill = Assert.Single(Commands<lh_cmd_solid_fill>(list, CommandType.SolidFill), c => c.color.r == 255 && c.color.g == 0);
        Assert.Equal(100, fill.layer.border_box.width);
        Assert.Equal(50, fill.layer.border_box.height);
    }

    [Fact]
    public void TextPreservesContentAndHostFontHandle()
    {
        using var host = new RecordingContainer();
        var list = Paint(host, "<p>hi</p>");
        var text = Assert.Single(Commands<lh_cmd_text>(list, CommandType.Text));
        Assert.Equal("hi", list.GetText(text.utf8));
        Assert.Contains(text.font, host.LiveFonts);
    }

    [Fact]
    public void NonAsciiTextRoundTripsAsUtf8()
    {
        using var host = new RecordingContainer();
        var list = Paint(host, "<p>café你好𝄞</p>");
        var text = string.Concat(Commands<lh_cmd_text>(list, CommandType.Text).Select(c => list.GetText(c.utf8)));
        Assert.Equal("café你好𝄞", text);
    }

    [Fact]
    public void OverflowHiddenRecordsBalancedRoundedClips()
    {
        using var host = new RecordingContainer();
        var list = Paint(host, "<div style='width:50px;height:20px;overflow:hidden;border-radius:5px'><div style='width:100px;height:50px;background:red'></div></div>");
        var clips = Commands<lh_cmd_set_clip>(list, CommandType.SetClip);
        Assert.NotEmpty(clips);
        Assert.Contains(clips, c => c.radius.top_left_x > 0);
        var depth = 0;
        foreach (var command in list)
        {
            if (command.Type == CommandType.SetClip) depth++;
            if (command.Type == CommandType.DelClip) depth--;
            Assert.True(depth >= 0);
        }
        Assert.Equal(0, depth);
    }

    [Fact]
    public void LinearGradientPreservesStops()
    {
        using var host = new RecordingContainer();
        var list = Paint(host, "<div style='width:100px;height:40px;background:linear-gradient(to right,red 0%,blue 100%)'></div>");
        var gradient = Assert.Single(Commands<lh_cmd_linear_gradient>(list, CommandType.LinearGradient));
        var stops = list.GetStops(gradient.stops).ToArray();
        Assert.True(stops.Length >= 2);
        Assert.Equal(0, stops[0].offset);
        Assert.Equal(255, stops[0].color.r);
        Assert.Equal(1, stops[^1].offset);
        Assert.Equal(255, stops[^1].color.b);
    }

    [Fact]
    public void CustomElementReturnsIntrinsicBoxAndAttributes()
    {
        using var host = new RecordingContainer();
        Paint(host, "<input id='search' value='café=你好'>");
        var box = host.Document.GetElementInfo(1)?.Bounds;
        Assert.NotNull(box);
        Assert.Equal(80, box.Value.Width);
        Assert.Equal(24, box.Value.Height);
        Assert.Equal("café=你好", host.CustomAttributes["value"]);
    }

    [Theory]
    [InlineData("<input style='display:none'>")]
    [InlineData("<div style='display:none'><input></div>")]
    [InlineData("<input style='visibility:hidden'>")]
    public void HiddenCustomElementHasNoHostBox(string html)
    {
        using var host = new RecordingContainer();
        Paint(host, html);
        Assert.Null(host.Document.GetElementInfo(1)?.Bounds);
    }

    [Fact]
    public void HoverRuleRequestsRedraw()
    {
        using var host = new RecordingContainer();
        Paint(host, "<style>div{width:100px;height:50px;background:red}div:hover{background:blue}</style><div>hi</div>");
        var redraws = 0;
        host.RedrawRequested += _ => redraws++;
        host.Document.OnMouseOver(5, 5);
        Assert.True(redraws > 0);
    }

    [Fact]
    public void ManagedAndNativeLayoutsMatch() => AbiLayout.Validate();

    [Fact]
    public void DrawCopiesNativeBuffersBeforeAnotherDraw()
    {
        using var host = new RecordingContainer();
        var first = Paint(host, "<p>snapshot</p>");
        var original = Assert.Single(Commands<lh_cmd_text>(first, CommandType.Text));
        host.Document.Draw(25, 0, new RectF(0, 0, 300, 300));
        var retained = Assert.Single(Commands<lh_cmd_text>(first, CommandType.Text));
        Assert.Equal(original.pos.x, retained.pos.x);
        Assert.Equal("snapshot", first.GetText(retained.utf8));
    }

    [Fact]
    public void ReloadAndDisposeReleaseFontHandles()
    {
        var host = new RecordingContainer();
        try
        {
            Paint(host, "<p>first</p>");
            var firstFonts = host.LiveFonts.ToArray();
            Paint(host, "<p>second</p>");
            Assert.All(firstFonts, font => Assert.DoesNotContain(font, host.LiveFonts));
            Assert.True(host.FontsDeleted > 0);
        }
        finally
        {
            host.Dispose();
        }
        Assert.Empty(host.LiveFonts);
        Assert.Equal(host.FontsCreated, host.FontsDeleted);
        host.Dispose();
        Assert.Equal(host.FontsCreated, host.FontsDeleted);
    }

    [Fact]
    public void CallbackFailureReturnsToManagedCaller()
    {
        using var host = new RecordingContainer { ThrowOnTextWidth = true };
        var error = Assert.ThrowsAny<Exception>(() => Paint(host, "<p>measurement</p>"));
        Assert.Contains("Synthetic font measurement failure", error.ToString());
    }

    [Fact]
    public void DefaultMediaMatchesScreenRules()
    {
        using var host = new RecordingContainer();
        var list = Paint(host, "<style>@media screen{div{width:80px;height:20px;background:red}}@media print{div{background:blue}}</style><div></div>");
        Assert.Contains(Commands<lh_cmd_solid_fill>(list, CommandType.SolidFill), c => c.color.r == 255 && c.color.b == 0);
    }

    [Fact]
    public void SnapshotCannotExposeExpiredFontHandlesAfterReload()
    {
        using var host = new RecordingContainer();
        var stale = Paint(host, "<p>old</p>");
        Paint(host, "<p>new</p>");
        Assert.Throws<InvalidOperationException>(() => Commands<lh_cmd_text>(stale, CommandType.Text));
    }

    [Fact]
    public void TooltipFindsTitleOnAncestorOfHoveredText()
    {
        using var host = new RecordingContainer();
        Paint(host, "<div title='Ancestor tooltip'><span>hover</span></div>");
        Assert.Equal("Ancestor tooltip", host.Document.GetTooltipText(5, 5));
    }

    [Fact]
    public void FixedElementTooltipUsesClientCoordinatesAfterScroll()
    {
        using var host = new RecordingContainer();
        Paint(host, "<div title='normal' style='height:500px'>normal</div><div title='fixed' style='position:fixed;left:0;top:0;width:80px;height:20px;background:red'>fixed</div>");
        Assert.Equal("fixed", host.Document.GetTooltipText(5, 105, 5, 5));
        Assert.Equal("normal", host.Document.GetTooltipText(5, 105));
    }

    [Fact]
    public void ReplayAppliesScaleOnceAndRestoresDrawingState()
    {
        using var host = new RecordingContainer { ScaleFactor = 2 };
        var list = Paint(host, "<div style='width:100px;height:50px;background:red'></div>");
        DisplayListReplayer.Replay(list, host);
        var transform = Assert.Single(host.Transforms);
        Assert.Equal(2, transform.M11);
        Assert.Equal(2, transform.M22);
        Assert.Equal(100, Assert.Single(host.Fills).BorderBox.Width);
        Assert.Equal(0, host.DrawingScopeDepth);
    }

    [Fact]
    public void ReplayRestoresClipTransformAndOpacityAfterDrawingFailure()
    {
        using var host = new RecordingContainer { ScaleFactor = 2, ThrowOnFill = true };
        var list = Paint(host, "<div style='opacity:.5;width:50px;height:20px;overflow:hidden'><div style='opacity:.5;width:100px;height:50px;background:red'></div></div>");
        Assert.Throws<InvalidOperationException>(() => DisplayListReplayer.Replay(list, host));
        Assert.Equal(new[] { .5f, .5f }, host.OpacityGroups);
        Assert.Equal(0, host.DrawingScopeDepth);
    }

    [Fact]
    public void CustomLayoutPreservesFractionalCssPixels()
    {
        using var host = new RecordingContainer();
        Paint(host, "<input style='width:80.5px;height:24.25px'>");
        var box = host.Document.GetElementInfo(1)!.Value.Bounds;
        Assert.Equal(80.5f, box.Width);
        Assert.Equal(24.25f, box.Height);
    }
}
