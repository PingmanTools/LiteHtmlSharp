using Xunit;
namespace LiteHtmlSharp.Tests;

public class ContainerApiTests
{
    [Fact]
    public void SharedContainerHooksPreserveFractionalLayoutAndReloadLifetime()
    {
        using Container host = new Container();
        IReadOnlyDictionary<string, string>? received = null;
        host.ShouldCreateElementCallback = tag => tag == "widget";
        host.CreateElementCallback = (string tag, IReadOnlyDictionary<string, string> attributes, out SizeF size) =>
        {
            received = attributes;
            size = new SizeF(31.25f, 12.5f);
            return 42;
        };
        host.Render("<style>html,body{margin:0}widget{display:block}</style><widget data-id='a=b'></widget>");
        host.Document.Render(200);
        var info = host.Document.GetElementInfo(42)!.Value;
        Assert.Equal("a=b", received!["DATA-ID"]);
        Assert.Equal(31.25f, info.Width);
        Assert.Equal(12.5f, info.Height);
        host.Render("<p>replacement</p>");
        host.Document.Render(200);
        Assert.Null(host.Document.GetElementInfo(42));
        Assert.Equal(31.25f, info.Width); // A snapshot survives replacement without retaining native state.
    }

    [Fact]
    public void SharedEventsAndCssImportDoNotRequirePlatformTypes()
    {
        using var host = new CustomHost();
        string? clicked = null;
        bool imported = false;
        host.AnchorClicked += url => clicked = url;
        host.ImportCssRequest = (url, root) => { imported = true; return ("p{color:red}", root + "/styles"); };
        host.Render("<style>@import url(theme.css);</style><p>hello</p>");
        host.OnAnchorClick("example", 0);
        Assert.Equal("<style>@import url(theme.css);</style><p>hello</p>", host.Rendered);
        Assert.Equal("example", clicked);
        Assert.True(imported);
        host.Dispose();
        Assert.Throws<ObjectDisposedException>(() => host.Render(""));
    }
    private sealed class CustomHost : Container
    {
        public string? Rendered;
        protected override void OnRenderHtml(string html)
        {
            Rendered = html;
            base.OnRenderHtml(html);
        }
    }

}
