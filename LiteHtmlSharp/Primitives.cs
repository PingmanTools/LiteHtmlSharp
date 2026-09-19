using LiteHtmlSharp.Interop;
namespace LiteHtmlSharp;

public readonly record struct RectF(float X, float Y, float Width, float Height)
{
    internal lh_rect Native => new() { x = X, y = Y, width = Width, height = Height };
    internal static RectF From(lh_rect r) => new(r.x, r.y, r.width, r.height);
}
public readonly record struct SizeF(float Width, float Height);
public readonly record struct ColorRgba(byte R, byte G, byte B, byte A = 255)
{
    internal static ColorRgba From(lh_color c) => new(c.r, c.g, c.b, c.a);
}
public readonly record struct BorderRadii(float TopLeftX, float TopLeftY, float TopRightX, float TopRightY, float BottomRightX, float BottomRightY, float BottomLeftX, float BottomLeftY)
{
    internal static BorderRadii From(lh_radii r) => new(r.top_left_x, r.top_left_y, r.top_right_x, r.top_right_y, r.bottom_right_x, r.bottom_right_y, r.bottom_left_x, r.bottom_left_y);
}
public readonly record struct Layer(RectF BorderBox, RectF ClipBox, RectF OriginBox, BorderRadii Radius, int Attachment, int Repeat, bool IsRoot, float Opacity)
{
    internal static Layer From(lh_layer l) => new(RectF.From(l.border_box), RectF.From(l.clip_box), RectF.From(l.origin_box), BorderRadii.From(l.radius), l.attachment, l.repeat, l.is_root != 0, l.opacity);
}
public readonly record struct Border(float Width, int Style, ColorRgba Color)
{
    internal static Border From(lh_border b) => new(b.width, b.style, ColorRgba.From(b.color));
}
public readonly record struct Borders(Border Left, Border Top, Border Right, Border Bottom, BorderRadii Radius)
{
    internal static Borders From(lh_borders b) => new(Border.From(b.left), Border.From(b.top), Border.From(b.right), Border.From(b.bottom), BorderRadii.From(b.radius));
}
public readonly record struct FontMetrics(float FontSize, float Height, float Ascent, float Descent, float XHeight, float ChWidth, float SubShift = 0, float SuperShift = 0, bool DrawSpaces = true)
{
    internal lh_font_metrics Native => new() { font_size = FontSize, height = Height, ascent = Ascent, descent = Descent, x_height = XHeight, ch_width = ChWidth, sub_shift = SubShift, super_shift = SuperShift, draw_spaces = (byte)(DrawSpaces ? 1 : 0) };
}
public sealed record FontDescription(string Family, float Size, int Style, int Weight, int DecorationLine, float DecorationThickness, int DecorationThicknessPredefined, int DecorationThicknessUnits, int DecorationStyle, ColorRgba DecorationColor, string EmphasisStyle, ColorRgba EmphasisColor, int EmphasisPosition);
public readonly record struct MediaFeatures(int Type, float Width, float Height, float DeviceWidth, float DeviceHeight, int Color = 8, int ColorIndex = 0, int Monochrome = 0, float Resolution = 96)
{
    internal lh_media_features Native => new() { type = Type, width = Width, height = Height, device_width = DeviceWidth, device_height = DeviceHeight, color = Color, color_index = ColorIndex, monochrome = Monochrome, resolution = Resolution };
}
public enum CommandType : uint
{
    SolidFill = 1, LinearGradient, RadialGradient, ConicGradient, Image, Text, Borders, ListMarker, SetClip, DelClip, PushTransform, PopTransform, PushOpacity, PopOpacity
}
