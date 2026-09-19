using System.Windows.Media;
namespace LiteHtmlSharp.Wpf;

public static class LiteHtmlExtensions
{
    public static SolidColorBrush GetBrush(this ColorRgba color)
    {
        var brush = new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
        brush.Freeze();
        return brush;
    }
    public static Pen GetPen(this ColorRgba color, double thickness)
    {
        var pen = new Pen(color.GetBrush(), thickness);
        pen.Freeze();
        return pen;
    }
}
