using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace LiteHtmlSharp.Avalonia;

internal sealed class FontInfo : IDisposable
{
    private readonly FontDescription description;
    private readonly Typeface typeface;
    private readonly Dictionary<(string, ColorRgba), TextLayout> layouts = new();
    private readonly Dictionary<string, float> widths = new(StringComparer.Ordinal);
    public FontMetrics Metrics
    {
        get;
    }
    private readonly float thickness;
    private readonly double underlineOffset;
    private readonly Pen decorationPen;
    public FontInfo(FontDescription description, FontFamily family, float defaultSize, RectF viewport)
    {
        this.description = description;
        typeface = new Typeface(family, description.Style == 1 ? FontStyle.Italic : FontStyle.Normal, (FontWeight)Math.Clamp(description.Weight, 1, 1000));
        using var sample = new TextLayout("x", typeface, description.Size, Brushes.Black);
        var ascent = (float)sample.Baseline;
        var height = (float)sample.Height;
        var xHeight = description.Size * .5f;
        var recommendedThickness = Math.Max(.5f, description.Size / 16);
        underlineOffset = sample.Baseline + description.Size * .1;
        if (FontManager.Current.TryGetGlyphTypeface(typeface, out var glyph))
        {
            var scale = description.Size / glyph.Metrics.DesignEmHeight;
            ascent = Math.Abs(glyph.Metrics.Ascent * scale);
            height = (Math.Abs(glyph.Metrics.Ascent) + Math.Abs(glyph.Metrics.Descent) + glyph.Metrics.LineGap) * scale;
            recommendedThickness = Math.Max(.5f, glyph.Metrics.UnderlineThickness * scale);
            underlineOffset = sample.Baseline + glyph.Metrics.UnderlinePosition * scale;
            if (glyph.TryGetGlyph('x', out var x) && glyph.TryGetGlyphMetrics(x, out var gm))
                // Glyph bounds can have a negative height; CSS needs a positive distance.
                xHeight = Math.Abs(gm.Height * scale);
        }
        using var ch = new TextLayout("0", typeface, description.Size, Brushes.Black);
        Metrics = new(description.Size, height, ascent, Math.Max(0, height - ascent), xHeight, (float)ch.WidthIncludingTrailingWhitespace, DrawSpaces: description.DecorationLine != 0);
        thickness = description.DecorationThicknessPredefined >= 0 ? recommendedThickness : ConvertLength(description.DecorationThickness, description.DecorationThicknessUnits, description.Size, xHeight, Metrics.ChWidth, defaultSize, viewport);
        decorationPen = new Pen(description.DecorationColor.GetBrush(), Math.Max(0, thickness),
            description.DecorationStyle == 2 ? DashStyle.Dot : description.DecorationStyle == 3 ? DashStyle.Dash : null);
    }
    private static float ConvertLength(float value, int unit, float em, float ex, float ch, float root, RectF viewport) => unit switch
    {
        1 => value * em / 100,
        2 => value * 96,
        3 => value * 96 / 2.54f,
        4 => value * 96 / 25.4f,
        5 => value * em,
        6 => value * ex,
        7 => value * 96 / 72,
        8 => value * 16,
        10 => value * viewport.Width / 100,
        11 => value * viewport.Height / 100,
        12 => value * Math.Min(viewport.Width, viewport.Height) / 100,
        13 => value * Math.Max(viewport.Width, viewport.Height) / 100,
        14 => value * root,
        15 => value * ch,
        _ => value
    };
    public float Width(string text)
    {
        if (!widths.TryGetValue(text, out var width))
        {
            using var measured = new TextLayout(text, typeface, description.Size, Brushes.Black);
            width = (float)measured.WidthIncludingTrailingWhitespace;
            if (widths.Count >= 2048) widths.Clear();
            widths[text] = width;
        }
        return width;
    }
    public TextLayout Layout(string text, ColorRgba color, IBrush foreground)
    {
        var key = (text, color);
        if (!layouts.TryGetValue(key, out var layout))
        {
            if (layouts.Count >= 512)
            {
                foreach (var old in layouts.Values) old.Dispose();
                layouts.Clear();
            }
            layout = new TextLayout(text, typeface, description.Size, foreground);
            layouts[key] = layout;
        }
        return layout;
    }
    public void Draw(DrawingContext context, RectF position, string text, ColorRgba color, IBrush foreground, float decorationOpacity = 1)
    {
        var layout = Layout(text, color, foreground);
        layout.Draw(context, new Point(position.X, position.Y));
        using var opacity = decorationOpacity == 1 ? default : context.PushOpacity(decorationOpacity);
        // litehtml emits spaces separately; its full run width keeps decorations continuous.
        if ((description.DecorationLine & 1) != 0)
            DrawDecoration(context, position, underlineOffset);
        if ((description.DecorationLine & 2) != 0)
            DrawDecoration(context, position, layout.Baseline - Metrics.Ascent);
        if ((description.DecorationLine & 4) != 0)
            DrawDecoration(context, position, layout.Baseline - Metrics.XHeight / 2);
    }
    private void DrawDecoration(DrawingContext context, RectF position, double offset)
    {
        var y = position.Y + offset;
        context.DrawLine(decorationPen, new Point(position.X, y), new Point(position.X + position.Width, y));
    }
    public void Dispose()
    {
        foreach (var layout in layouts.Values)
            layout.Dispose();
        layouts.Clear();
        widths.Clear();
    }
}
