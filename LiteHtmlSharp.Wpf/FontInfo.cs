using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace LiteHtmlSharp.Wpf;

internal sealed class FontInfo
{
    private readonly FontDescription description;
    private readonly Typeface typeface;
    private readonly Dictionary<(string, ColorRgba), FormattedText> texts = new();
    private readonly Dictionary<string, float> widths = new(StringComparer.Ordinal);
    private readonly Pen decorationPen;
    private readonly double underlineOffset;
    public FontMetrics Metrics { get; }
    public FontInfo(FontDescription description, FontFamily family, float rootSize, RectF viewport)
    {
        this.description = description;
        typeface = new Typeface(family, description.Style == 1 ? FontStyles.Italic : FontStyles.Normal,
            FontWeight.FromOpenTypeWeight(Math.Clamp(description.Weight, 1, 999)), FontStretches.Normal);
        var sample = Format("x", Brushes.Black);
        var ascent = sample.Baseline;
        var height = sample.Height;
        var xHeight = description.Size * .5;
        var thickness = description.Size / 16d;
        underlineOffset = ascent + description.Size * .1;
        if (typeface.TryGetGlyphTypeface(out var glyph))
        {
            ascent = glyph.Baseline * description.Size;
            height = glyph.Height * description.Size;
            xHeight = glyph.XHeight * description.Size;
            thickness = glyph.UnderlineThickness * description.Size;
            underlineOffset = sample.Baseline - glyph.UnderlinePosition * description.Size;
        }
        var ch = Format("0", Brushes.Black).WidthIncludingTrailingWhitespace;
        Metrics = new(description.Size, (float)height, (float)ascent, (float)Math.Max(0, height - ascent),
            (float)xHeight, (float)ch, DrawSpaces: description.DecorationLine != 0);
        thickness = Math.Max(.5, thickness);
        if (description.DecorationThicknessPredefined < 0)
            thickness = Length(description.DecorationThickness, description.DecorationThicknessUnits, description.Size, xHeight, ch, rootSize, viewport);
        decorationPen = new Pen(description.DecorationColor.GetBrush(), Math.Max(0, thickness));
        if (description.DecorationStyle == 2) decorationPen.DashStyle = DashStyles.Dot;
        if (description.DecorationStyle == 3) decorationPen.DashStyle = DashStyles.Dash;
        decorationPen.Freeze();
    }
    private static double Length(float v, int unit, double em, double ex, double ch, double root, RectF viewport) => unit switch
    {
        1 => v * em / 100,
        2 => v * 96,
        3 => v * 96 / 2.54,
        4 => v * 96 / 25.4,
        5 => v * em,
        6 => v * ex,
        7 => v * 96 / 72,
        8 => v * 16,
        10 => v * viewport.Width / 100,
        11 => v * viewport.Height / 100,
        12 => v * Math.Min(viewport.Width, viewport.Height) / 100,
        13 => v * Math.Max(viewport.Width, viewport.Height) / 100,
        14 => v * root,
        15 => v * ch,
        _ => v
    };
    private FormattedText Format(string text, Brush brush) => new(text, CultureInfo.InvariantCulture,
        FlowDirection.LeftToRight, typeface, description.Size, brush, 1);
    private FormattedText Text(string text, ColorRgba color)
    {
        var key = (text, color);
        if (!texts.TryGetValue(key, out var result))
        {
            if (texts.Count >= 512) texts.Clear();
            texts[key] = result = Format(text, color.GetBrush());
        }
        return result;
    }
    public float Width(string text)
    {
        if (!widths.TryGetValue(text, out var width))
        {
            width = (float)Format(text, Brushes.Black).WidthIncludingTrailingWhitespace;
            if (widths.Count >= 2048) widths.Clear();
            widths[text] = width;
        }
        return width;
    }
    public void Draw(DrawingContext context, RectF box, string text, ColorRgba color, float decorationOpacity = 1)
    {
        var formatted = Text(text, color);
        context.DrawText(formatted, new Point(box.X, box.Y));
        if (decorationPen.Thickness <= 0) return;
        context.PushOpacity(Math.Clamp(decorationOpacity, 0, 1));
        try
        {
            void Line(double offset) => context.DrawLine(decorationPen, new Point(box.X, box.Y + offset), new Point(box.X + box.Width, box.Y + offset));
            // Whitespace arrives as separate runs; use the layout width for continuous decoration.
            if ((description.DecorationLine & 1) != 0) Line(underlineOffset);
            if ((description.DecorationLine & 2) != 0) Line(formatted.Baseline - Metrics.Ascent);
            if ((description.DecorationLine & 4) != 0) Line(formatted.Baseline - Metrics.XHeight / 2);
        }
        finally { context.Pop(); }
    }
}
