using System;
using CoreGraphics;

namespace LiteHtmlSharp.CoreGraphics;

public static class CGExtensions
{
    public static CGRect ToRect(this RectF r) => new(r.X, r.Y, Math.Max(0, r.Width), Math.Max(0, r.Height));
    public static RectF ToRectF(this CGRect r) => new((float)r.X, (float)r.Y, (float)r.Width, (float)r.Height);
    public static CGColor ToCGColor(this ColorRgba c) => new(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
    public static CGSize ToCGSize(this LiteHtmlSize s) => new(s.Width, s.Height);
    public static CGPoint ToCGPoint(this LiteHtmlPoint p) => new(p.X, p.Y);
    public static LiteHtmlSize ToLiteHtmlSize(this CGSize s) => new((float)s.Width, (float)s.Height);
    public static LiteHtmlPoint ToLiteHtmlPoint(this CGPoint p) => new((float)p.X, (float)p.Y);
}
