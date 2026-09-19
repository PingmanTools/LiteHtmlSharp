using Avalonia.Media;
using Avalonia.Media.Immutable;
namespace LiteHtmlSharp.Avalonia;

public static class LiteHtmlExtensions
{
    public static Color ToAvalonia(this ColorRgba color) => Color.FromArgb(color.A, color.R, color.G, color.B);
    public static IBrush GetBrush(this ColorRgba color) => new ImmutableSolidColorBrush(color.ToAvalonia());
}
