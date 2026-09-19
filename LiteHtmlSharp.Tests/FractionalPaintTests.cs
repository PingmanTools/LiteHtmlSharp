using LiteHtmlSharp.Interop;
using Xunit;

namespace LiteHtmlSharp.Tests;

public class FractionalPaintTests
{
    [Theory]
    [InlineData(271.126f)]
    [InlineData(271.75f)]
    public void FooterPaintEndsAtTheMeasuredDocumentBottom(float top)
    {
        using var host = new Container();
        host.Document.Load(FormattableString.Invariant($"<style>html,body{{margin:0;padding:0}}div{{display:block}}</style><div style='height:{top}px'></div><div style='height:26px;background:blue;border:0.25px solid red;box-sizing:border-box'></div>"));
        host.Document.Render(620);
        var list = host.Document.Draw(0, 0, new RectF(0, 0, 620, 400));
        var fills = new List<lh_cmd_solid_fill>();
        var borders = new List<lh_cmd_borders>();
        foreach (var command in list)
        {
            if (command.Type == CommandType.SolidFill) fills.Add(command.Read<lh_cmd_solid_fill>());
            if (command.Type == CommandType.Borders) borders.Add(command.Read<lh_cmd_borders>());
        }
        var fill = Assert.Single(fills).layer.border_box;
        var border = Assert.Single(borders).pos;
        Assert.Equal(top, fill.y, 3);
        Assert.Equal(host.Document.Height(), fill.y + fill.height, 3);
        Assert.Equal(fill.y, border.y, 3);
        Assert.Equal(fill.height, border.height, 3);
    }
}
