using Xunit;

namespace LiteHtmlSharp.Tests;

public class TypographyTests
{
    [Theory]
    [InlineData("200", 200)]
    [InlineData("300", 300)]
    [InlineData("500", 500)]
    [InlineData("700", 700)]
    [InlineData("normal", 400)]
    [InlineData("bold", 700)]
    public void CssFontWeightReachesTheHost(string cssWeight, int expectedWeight)
    {
        using var host = new FontProbe();
        host.Document.Load($"<div style='font-family:Roboto;font-size:16px;font-weight:{cssWeight}'>Sample</div>");
        host.Document.Render(300);
        Assert.Contains(host.Requests, font => font.Family == "Roboto" &&
            font.Weight == expectedWeight && font.Size == 16);
    }

    [Fact]
    public void FractionalFontSizeIsUsedForMeasurementAndEmLayout()
    {
        using var host = new FontProbe();
        host.Document.Load("<style>html,body{margin:0;padding:0}div{font-size:12.5px;height:10px;padding:1em;border:.5px solid black}</style><div>Sample</div>");
        foreach (var width in new[] { 400f, 201.5f, 400f })
        {
            host.Document.Render(width);
            Assert.Contains(host.Requests, font => font.Size == 12.5f);
            Assert.Equal(36f, host.Document.Height(), 3);
        }
    }

    private sealed class FontProbe : Container
    {
        public List<FontDescription> Requests { get; } = [];
        public override nuint CreateFont(FontDescription description, out FontMetrics metrics)
        {
            Requests.Add(description);
            metrics = new(description.Size, 18, 14, 4, 8, 8);
            return (nuint)Requests.Count;
        }
        public override float TextWidth(string text, nuint font) => text.Length * 8;
    }
}
