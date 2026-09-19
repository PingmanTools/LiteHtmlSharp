using Xunit;

namespace LiteHtmlSharp.Tests;

public class ViewportTests
{
    [Fact]
    public void ViewportSetBeforeHtmlIsAvailableForTheFirstLayout()
    {
        using var host = new ViewportContainer();
        Assert.False(host.SetViewport(new LiteHtmlPoint(0, 0), new LiteHtmlSize(620, 100)));
        Assert.True(host.HasCustomViewport);
        Assert.Equal(620, host.Size.Width);
        Assert.Equal(100, host.Size.Height);
        Assert.False(host.Document.HasRendered);

        host.Render("<style>html,body{margin:0;padding:0}div{width:100%;height:40px}</style><div>First layout</div>");
        host.Render();
        Assert.Equal(620f, host.Document.Width());
        Assert.Equal(40f, host.Document.Height());
    }
}
