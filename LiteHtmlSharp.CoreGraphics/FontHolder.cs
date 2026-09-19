using System;
using System.Collections.Generic;
using CoreGraphics;
using CoreText;
using Foundation;
#if MAC
using AppKit;
#elif IOS
using UIKit;
#endif

namespace LiteHtmlSharp.CoreGraphics;

public sealed class FontHolder : IDisposable
{
    private readonly Dictionary<(string, ColorRgba), CTLine> lines = new();
    private readonly Dictionary<string, float> widths = new();
    private readonly FontDescription description;
    private readonly float thickness;
    public CTFont Font { get; }
    public FontMetrics Metrics { get; }
    public FontHolder(FontDescription description, float rootSize, RectF viewport)
    {
        this.description = description;
        var family = description.Family.Split(',') [0].Trim().Trim('\'', '"');
        using var initial = new CTFont(string.IsNullOrWhiteSpace(family) ? "Helvetica" : family, description.Size);
        var traits = (description.Style == 1 ? CTFontSymbolicTraits.Italic : CTFontSymbolicTraits.None) |
                     (description.Weight >= 600 ? CTFontSymbolicTraits.Bold : CTFontSymbolicTraits.None);
        Font = initial.WithSymbolicTraits(description.Size, traits,
                                          CTFontSymbolicTraits.Italic | CTFontSymbolicTraits.Bold) ??
               new CTFont("Helvetica", description.Size);
        using var zero = MakeLine("0", new ColorRgba(0, 0, 0));
        Metrics = new(description.Size, (float)(Font.AscentMetric + Font.DescentMetric + Font.LeadingMetric),
                      (float)Font.AscentMetric, (float)Font.DescentMetric, (float)Font.XHeightMetric,
                      (float)zero.GetTypographicBounds(), DrawSpaces: description.DecorationLine != 0);
        thickness = description.DecorationThicknessPredefined >= 0
                        ? Math.Max(.5f, (float)Font.UnderlineThickness)
                        : ConvertLength(description.DecorationThickness, description.DecorationThicknessUnits,
                                        description.Size, Metrics.XHeight, Metrics.ChWidth, rootSize, viewport);
    }
    private static float ConvertLength(float v, int unit, float em, float ex, float ch, float root,
                                       RectF viewport) => unit switch {
        1 => v * em / 100,
        2 => v * 96,
        3 => v * 96 / 2.54f,
        4 => v * 96 / 25.4f,
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
    private CTLine MakeLine(string text, ColorRgba color)
    {
        using var cgColor = color.ToCGColor();
        var attributes = new CTStringAttributes { Font = Font, ForegroundColor = cgColor };
        using var value = new NSAttributedString(text, attributes);
        attributes.Dictionary.Dispose();
        return new CTLine(value);
    }
    public float Width(string text)
    {
        if (!widths.TryGetValue(text, out var width))
        {
            using var line = MakeLine(text, new ColorRgba(0, 0, 0));
            if (widths.Count >= 2048) widths.Clear();
            widths[text] = width = (float)line.GetTypographicBounds();
        }
        return width;
    }
    public void Draw(CGContext context, RectF position, ColorRgba color, string text, float decorationOpacity = 1)
    {
        if (!lines.TryGetValue((text, color), out var line))
        {
            if (lines.Count >= 512)
            {
                foreach (var old in lines.Values) old.Dispose();
                lines.Clear();
            }
            lines[(text, color)] = line = MakeLine(text, color);
        }
        context.SaveState();
        try
        {
            context.TranslateCTM(position.X, position.Y + Font.AscentMetric);
            context.ScaleCTM(1, -1);
            context.TextMatrix = CGAffineTransform.MakeIdentity();
            context.TextPosition = new CGPoint(0, 0);
            line.Draw(context);
        }
        finally
        {
            context.RestoreState();
        }
        if (description.DecorationLine == 0 || thickness <= 0)
            return;
        context.SaveState();
        try
        {
            context.SetAlpha(Math.Clamp(decorationOpacity, 0, 1));
            using var stroke = description.DecorationColor.ToCGColor();
            context.SetStrokeColor(stroke);
            context.SetLineWidth(Math.Max(0, thickness));
            if (description.DecorationStyle == 2)
                context.SetLineDash(0, new nfloat[] { 1, 1 });
            if (description.DecorationStyle == 3)
                context.SetLineDash(0, new nfloat[] { 3, 2 });
            void DrawAt(float y)
            {
                void Line(float offset)
                {
                    context.MoveTo(position.X, y + offset);
                    context.AddLineToPoint(position.X + position.Width, y + offset);
                    context.StrokePath();
                }
                if (description.DecorationStyle == 1)
                {
                    context.SetLineWidth(thickness / 3);
                    Line(-thickness / 3);
                    Line(thickness / 3);
                }
                else
                    Line(0);
            }
            if ((description.DecorationLine & 1) != 0)
                DrawAt(position.Y + Metrics.Ascent - (float)Font.UnderlinePosition);
            if ((description.DecorationLine & 2) != 0)
                DrawAt(position.Y);
            if ((description.DecorationLine & 4) != 0)
                DrawAt(position.Y + Metrics.Ascent - Metrics.XHeight / 2);
        }
        finally
        {
            context.RestoreState();
        }
    }
    public void Dispose()
    {
        foreach (var line in lines.Values)
            line.Dispose();
        lines.Clear();
        widths.Clear();
        Font.Dispose();
    }
}
