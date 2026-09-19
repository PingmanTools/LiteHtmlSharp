using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using LiteHtmlSharp.Interop;
using Border = LiteHtmlSharp.Border;

namespace LiteHtmlSharp.Avalonia;

public interface IResourceLoader
{
    byte[] GetResourceBytes(string resource);
    string GetResourceString(string resource);
}
public delegate FontFamily FontAbsolutePathDelegate(string fontName);

public class AvaloniaContainer : ViewportContainer
{
    private sealed class ResourceLoader(Func<string, string> text, Func<string, byte[]> bytes) : IResourceLoader
    {
        public byte[] GetResourceBytes(string resource) => bytes?.Invoke(resource);
        public string GetResourceString(string resource) => text?.Invoke(resource);
    }
    private readonly IResourceLoader loader;
    private readonly Dictionary<nuint, FontInfo> fonts = new();
    private readonly Dictionary<ColorRgba, IBrush> brushes = new();
    private readonly Dictionary<ColorRgba, IPen> markerPens = new();
    private readonly ConcurrentDictionary<(string, string), string> resolvedUrls = new();
    private readonly Dictionary<(RectF, BorderRadii), Geometry> rounded = new();
    private sealed record GradientEntry(lh_color_stop[] Stops, IBrush Brush);
    private readonly Dictionary<(int, Layer, float, float, float, float, int), List<GradientEntry>> gradients = new();
    private readonly Dictionary<(Borders, RectF), (Geometry Geometry, IBrush Brush, IPen Pen)[]> borderPaints = new();
    private readonly Dictionary<int, string> markerText = new();
    private readonly Stack<DrawingContext.PushedState> scopes = new();
    private nuint nextFont = 1;
    private string baseUrl = "";
    private bool disposed;
    public DrawingContext DrawingContext
    {
        get; set;
    }
    public FontAbsolutePathDelegate FontAbsolutePathDelegate
    {
        get; set;
    }
    public override float DefaultFontSize { get; set; } = 12;
    private string defaultFontName;
    public override string DefaultFontName { get => defaultFontName ?? FontManager.Current.DefaultFontFamily.Name; set => defaultFontName = value; }
    public Action<string> SetCursorCallback
    {
        get; set;
    }
    public AvaloniaContainer(string css, IResourceLoader loader) : base(css) { this.loader = loader; }
    public AvaloniaContainer(string css, Func<string, string> text, Func<string, byte[]> bytes) : this(css, new ResourceLoader(text, bytes)) { }
    public override void SetCursor(string cursor) => SetCursorCallback?.Invoke(cursor);
    public override void SetBaseUrl(string url) => baseUrl = url;
    internal Action<string> HtmlRenderHandler { get; set; }
    protected override void OnRenderHtml(string html)
    {
        if (HtmlRenderHandler is { } render) render(html);
        else base.OnRenderHtml(html);
    }
    public override nuint CreateFont(FontDescription description, out FontMetrics metrics)
    {
        FontFamily family;
        try
        {
            family = FontAbsolutePathDelegate?.Invoke(description.Family) ?? new FontFamily(description.Family);
        }
        catch (ArgumentException) { family = FontFamily.Default; }
        var font = new FontInfo(description, family, DefaultFontSize, Viewport);
        var id = nextFont++;
        fonts.Add(id, font);
        metrics = font.Metrics;
        return id;
    }
    public override void DeleteFont(nuint font)
    {
        if (fonts.Remove(font, out var value))
            value.Dispose();
    }
    public override float TextWidth(string text, nuint font) => fonts[font].Width(text);
    public override void DrawText(RectF position, ColorRgba color, nuint font, string text, float decorationOpacity = 1)
        => fonts[font].Draw(DrawingContext, position, text, color, Brush(color), decorationOpacity);
    private IBrush Brush(ColorRgba color)
    {
        if (!brushes.TryGetValue(color, out var brush))
        {
            brush = color.GetBrush();
            brushes[color] = brush;
        }
        return brush;
    }
    private static Rect Rect(RectF r) => new(r.X, r.Y, Math.Max(0, r.Width), Math.Max(0, r.Height));
    private Geometry Rounded(RectF rect, BorderRadii radius)
    {
        var key = (rect, radius);
        if (rounded.TryGetValue(key, out var geometry))
            return geometry;
        if (rounded.Count > 2048)
            rounded.Clear();
        var r = Rect(rect);
        var scale = 1d;
        void Fit(double sum, double side)
        {
            if (sum > 0)
                scale = Math.Min(scale, side / sum);
        }
        Fit(radius.TopLeftX + radius.TopRightX, r.Width);
        Fit(radius.BottomLeftX + radius.BottomRightX, r.Width);
        Fit(radius.TopLeftY + radius.BottomLeftY, r.Height);
        Fit(radius.TopRightY + radius.BottomRightY, r.Height);
        var tl = new Size(radius.TopLeftX * scale, radius.TopLeftY * scale);
        var tr = new Size(radius.TopRightX * scale, radius.TopRightY * scale);
        var br = new Size(radius.BottomRightX * scale, radius.BottomRightY * scale);
        var bl = new Size(radius.BottomLeftX * scale, radius.BottomLeftY * scale);
        var path = new StreamGeometry();
        using (var c = path.Open())
        {
            c.BeginFigure(new Point(r.Left + tl.Width, r.Top), true);
            c.LineTo(new Point(r.Right - tr.Width, r.Top));
            if (tr.Width > 0 && tr.Height > 0)
                c.ArcTo(new Point(r.Right, r.Top + tr.Height), tr, 0, false, SweepDirection.Clockwise);
            else
                c.LineTo(r.TopRight);
            c.LineTo(new Point(r.Right, r.Bottom - br.Height));
            if (br.Width > 0 && br.Height > 0)
                c.ArcTo(new Point(r.Right - br.Width, r.Bottom), br, 0, false, SweepDirection.Clockwise);
            else
                c.LineTo(r.BottomRight);
            c.LineTo(new Point(r.Left + bl.Width, r.Bottom));
            if (bl.Width > 0 && bl.Height > 0)
                c.ArcTo(new Point(r.Left, r.Bottom - bl.Height), bl, 0, false, SweepDirection.Clockwise);
            else
                c.LineTo(r.BottomLeft);
            c.LineTo(new Point(r.Left, r.Top + tl.Height));
            if (tl.Width > 0 && tl.Height > 0)
                c.ArcTo(new Point(r.Left + tl.Width, r.Top), tl, 0, false, SweepDirection.Clockwise);
            else
                c.LineTo(r.TopLeft);
            c.EndFigure(true);
        }
        rounded[key] = path;
        return path;
    }
    public override void PushClip(RectF position, BorderRadii radii) => scopes.Push(DrawingContext.PushGeometryClip(Rounded(position, radii)));
    public override void PopClip() => scopes.Pop().Dispose();
    public override void PushTransform(Matrix4x4 m) => scopes.Push(DrawingContext.PushTransform(new Matrix(m.M11, m.M12, m.M21, m.M22, m.M41, m.M42)));
    public override void PushOpacity(float opacity)
    {
        // Request a real compositing layer locally; ordinary PushOpacity can
        // otherwise multiply each primitive's alpha in Avalonia's Skia backend.
        scopes.Push(DrawingContext.PushRenderOptions(new RenderOptions { RequiresFullOpacityHandling = true }));
        try { scopes.Push(DrawingContext.PushOpacity(opacity)); }
        catch { scopes.Pop().Dispose(); throw; }
    }
    public override void PopOpacity()
    {
        scopes.Pop().Dispose();
        scopes.Pop().Dispose();
    }
    public override void PopTransform() => scopes.Pop().Dispose();
    public override void FillRect(Layer layer, ColorRgba color)
    {
        using var clip = DrawingContext.PushClip(Rect(layer.ClipBox));
        using var opacity = layer.Opacity == 1 ? default : DrawingContext.PushOpacity(layer.Opacity);
        DrawingContext.DrawGeometry(Brush(color), null, Rounded(layer.BorderBox, layer.Radius));
    }
    private static int StopsHash(ReadOnlySpan<lh_color_stop> stops, int space, int hue)
    {
        var hash = new HashCode();
        hash.Add(space);
        hash.Add(hue);
        foreach (var s in stops)
        {
            hash.Add(s.offset);
            hash.Add(s.color.r);
            hash.Add(s.color.g);
            hash.Add(s.color.b);
            hash.Add(s.color.a);
            hash.Add(s.hint);
            hash.Add(s.has_hint);
        }
        return hash.ToHashCode();
    }
    private bool FindGradient((int, Layer, float, float, float, float, int) key, ReadOnlySpan<lh_color_stop> stops, out IBrush brush)
    {
        if (gradients.TryGetValue(key, out var candidates))
            foreach (var entry in candidates)
                if (MemoryMarshal.AsBytes(stops).SequenceEqual(MemoryMarshal.AsBytes(entry.Stops.AsSpan())))
                {
                    brush = entry.Brush;
                    return true;
                }
        brush = null;
        return false;
    }
    private void CacheGradient((int, Layer, float, float, float, float, int) key, ReadOnlySpan<lh_color_stop> stops, IBrush brush)
    {
        if (gradients.Count > 512)
            gradients.Clear();
        if (!gradients.TryGetValue(key, out var candidates))
            gradients[key] = candidates = new();
        candidates.Add(new(stops.ToArray(), brush));
    }
    private static GradientStops Stops(ReadOnlySpan<lh_color_stop> source, int colorSpace)
    {
        var result = new GradientStops();
        for (var i = 0; i < source.Length; i++)
        {
            var stop = source[i];
            result.Add(new GradientStop(Color.FromArgb(stop.color.a, stop.color.r, stop.color.g, stop.color.b), stop.offset));
            if (i + 1 == source.Length || (stop.has_hint == 0 && colorSpace != 2))
                continue;
            var next = source[i + 1];
            var distance = next.offset - stop.offset;
            if (distance <= 0)
                continue;
            var midpoint = (stop.hint - stop.offset) / distance;
            for (var sample = 1; sample < 32; sample++)
            {
                var position = sample / 32f;
                var blend = position;
                if (stop.has_hint != 0)
                    blend = midpoint <= 0 ? 1 : midpoint >= 1 ? 0 : MathF.Pow(position, MathF.Log(.5f) / MathF.Log(midpoint));
                result.Add(new GradientStop(Interpolate(stop.color, next.color, blend, colorSpace == 2), stop.offset + position * distance));
            }
        }
        return result;
    }
    private static Color Interpolate(lh_color a, lh_color b, float t, bool linear)
    {
        var alphaA = a.a / 255f;
        var alphaB = b.a / 255f;
        var alpha = alphaA + (alphaB - alphaA) * t;
        float Decode(float c) => !linear ? c : c <= .04045f ? c / 12.92f : MathF.Pow((c + .055f) / 1.055f, 2.4f);
        float Encode(float c) => !linear ? c : c <= .0031308f ? c * 12.92f : 1.055f * MathF.Pow(c, 1 / 2.4f) - .055f;
        byte Channel(byte left, byte right)
        {
            if (alpha <= 0)
                return 0;
            var v = Decode(left / 255f) * alphaA * (1 - t) + Decode(right / 255f) * alphaB * t;
            return (byte)Math.Clamp(MathF.Round(Encode(v / alpha) * 255), 0, 255);
        }
        return Color.FromArgb((byte)Math.Clamp(MathF.Round(alpha * 255), 0, 255), Channel(a.r, b.r), Channel(a.g, b.g), Channel(a.b, b.b));
    }
    private void PaintTiles(Layer layer, IBrush brush, Bitmap bitmap = null)
    {
        var tile = layer.OriginBox;
        if (tile.Width <= 0 || tile.Height <= 0)
            return;
        using var outer = DrawingContext.PushGeometryClip(Rounded(layer.BorderBox, layer.Radius));
        using var clip = DrawingContext.PushClip(Rect(layer.ClipBox));
        using var opacity = layer.Opacity == 1 ? default : DrawingContext.PushOpacity(layer.Opacity);
        var repeatX = layer.Repeat is 0 or 1;
        var repeatY = layer.Repeat is 0 or 2;
        var x0 = repeatX ? tile.X + MathF.Floor((layer.ClipBox.X - tile.X) / tile.Width) * tile.Width : tile.X;
        var y0 = repeatY ? tile.Y + MathF.Floor((layer.ClipBox.Y - tile.Y) / tile.Height) * tile.Height : tile.Y;
        var right = repeatX ? layer.ClipBox.X + layer.ClipBox.Width : x0 + tile.Width;
        var bottom = repeatY ? layer.ClipBox.Y + layer.ClipBox.Height : y0 + tile.Height;
        for (double y = y0; y < bottom; y += tile.Height)
            for (double x = x0; x < right; x += tile.Width)
            {
                var rect = new Rect(x, y, tile.Width, tile.Height);
                if (bitmap != null)
                    DrawingContext.DrawImage(bitmap, rect);
                else
                {
                    using var placement = DrawingContext.PushTransform(Matrix.CreateTranslation(x - tile.X, y - tile.Y));
                    DrawingContext.DrawRectangle(brush, null, Rect(tile));
                }
            }
    }
    public override void DrawLinearGradient(Layer layer, float x0, float y0, float x1, float y1, ReadOnlySpan<lh_color_stop> stops, int space, int hue)
    {
        var key = (1, layer, x0, y0, x1, y1, StopsHash(stops, space, hue));
        if (!FindGradient(key, stops, out var brush))
        {
            brush = new LinearGradientBrush { StartPoint = new RelativePoint(x0, y0, RelativeUnit.Absolute), EndPoint = new RelativePoint(x1, y1, RelativeUnit.Absolute), GradientStops = Stops(stops, space) }.ToImmutable();
            CacheGradient(key, stops, brush);
        }
        PaintTiles(layer, brush);
    }
    public override void DrawRadialGradient(Layer layer, float cx, float cy, float rx, float ry, ReadOnlySpan<lh_color_stop> stops, int space, int hue)
    {
        var key = (2, layer, cx, cy, rx, ry, StopsHash(stops, space, hue));
        if (!FindGradient(key, stops, out var brush))
        {
            var center = new RelativePoint(cx, cy, RelativeUnit.Absolute);
            brush = new RadialGradientBrush { Center = center, GradientOrigin = center, RadiusX = new RelativeScalar(rx, RelativeUnit.Absolute), RadiusY = new RelativeScalar(ry, RelativeUnit.Absolute), GradientStops = Stops(stops, space) }.ToImmutable();
            CacheGradient(key, stops, brush);
        }
        PaintTiles(layer, brush);
    }
    public override void DrawConicGradient(Layer layer, float cx, float cy, float angle, float radius, ReadOnlySpan<lh_color_stop> stops, int space, int hue)
    {
        var key = (3, layer, cx, cy, angle, radius, StopsHash(stops, space, hue));
        if (!FindGradient(key, stops, out var brush))
        {
            brush = new ConicGradientBrush { Center = new RelativePoint(cx, cy, RelativeUnit.Absolute), Angle = angle, GradientStops = Stops(stops, space) }.ToImmutable();
            CacheGradient(key, stops, brush);
        }
        PaintTiles(layer, brush);
    }
    private string Resolve(string source, string root)
    {
        var basis = string.IsNullOrEmpty(root) ? baseUrl : root;
        var key = (source, basis);
        return resolvedUrls.GetOrAdd(key, static pair =>
            Uri.TryCreate(pair.Item2, UriKind.Absolute, out var b) && Uri.TryCreate(b, pair.Item1, out var resolved)
                ? resolved.ToString() : pair.Item1);
    }
    protected override (string Source, string BaseUrl) ResolveImageKey(string source, string root)
        => (Resolve(source, root), "");

    private Bitmap Image(string source, string root) => GetImageFrame(source, root) as Bitmap;

    protected override ValueTask<IImageSource> LoadImageSourceAsync(string source, string root, CancellationToken cancellationToken)
    {
        var resolved = Resolve(source, root);
        // Resource callbacks run synchronously on the requesting thread.
        cancellationToken.ThrowIfCancellationRequested();
        var bytes = loader?.GetResourceBytes(resolved);
        return new ValueTask<IImageSource>(Task.Run<IImageSource>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return bytes is { Length: > 0 } ? AvaloniaImageSource.Decode(bytes, cancellationToken) : null;
        }, cancellationToken));
    }
    public override void DrawImage(Layer layer, string source, string root)
    {
        var image = Image(source, root);
        if (image != null)
            PaintTiles(layer, null, image);
    }
    protected override (string Css, string BaseUrl) ImportCss(string url, string root)
    {
        if (ImportCssRequest != null) return base.ImportCss(url, root);
        var resolved = Resolve(url, root);
        return (loader?.GetResourceString(resolved) ?? "", resolved);
    }
    public override void DrawBorders(Borders borders, RectF position, bool root)
    {
        var key = (borders, position);
        if (!borderPaints.TryGetValue(key, out var paints))
        {
            var list = new List<(Geometry, IBrush, IPen)>();
            var r = Rect(position);
            var uniform = borders.Top == borders.Left && borders.Top == borders.Right && borders.Top == borders.Bottom;
            if (uniform && borders.Top.Width > 0 && borders.Top.Style >= 2)
            {
                var side = borders.Top;
                void Outline(double inset, double width)
                {
                    var rect = new RectF(position.X + (float)inset, position.Y + (float)inset, Math.Max(0, position.Width - (float)inset * 2), Math.Max(0, position.Height - (float)inset * 2));
                    var rad = InsetRadii(borders.Radius, (float)inset, (float)inset, (float)inset, (float)inset);
                    var dash = side.Style == 2 ? DashStyle.Dot : side.Style == 3 ? DashStyle.Dash : null;
                    list.Add((Rounded(rect, rad), null, new Pen(Brush(side.Color), width, dash)));
                }
                if (side.Style == 5)
                {
                    Outline(side.Width / 6, side.Width / 3);
                    Outline(side.Width * 5 / 6, side.Width / 3);
                }
                else
                    Outline(side.Width / 2, side.Width);
            }
            else
            {
                var inner = new RectF(position.X + borders.Left.Width, position.Y + borders.Top.Width, Math.Max(0, position.Width - borders.Left.Width - borders.Right.Width), Math.Max(0, position.Height - borders.Top.Width - borders.Bottom.Width));
                var innerRadius = InsetRadii(borders.Radius, borders.Left.Width, borders.Top.Width, borders.Right.Width, borders.Bottom.Width);
                var ring = new CombinedGeometry(GeometryCombineMode.Exclude, Rounded(position, borders.Radius), Rounded(inner, innerRadius));
                var inside = Rect(inner);
                void Side(Border border, Point a, Point b, Point c, Point d)
                {
                    if (border.Width <= 0 || border.Style < 2)
                        return;
                    var path = new StreamGeometry();
                    using (var context = path.Open())
                    {
                        context.BeginFigure(a, true);
                        context.LineTo(b);
                        context.LineTo(c);
                        context.LineTo(d);
                        context.EndFigure(true);
                    }
                    list.Add((new CombinedGeometry(GeometryCombineMode.Intersect, ring, path), Brush(border.Color), null));
                }
                Side(borders.Top, r.TopLeft, r.TopRight, inside.TopRight, inside.TopLeft);
                Side(borders.Right, r.TopRight, r.BottomRight, inside.BottomRight, inside.TopRight);
                Side(borders.Bottom, r.BottomRight, r.BottomLeft, inside.BottomLeft, inside.BottomRight);
                Side(borders.Left, r.BottomLeft, r.TopLeft, inside.TopLeft, inside.BottomLeft);
            }
            paints = list.ToArray();
            if (borderPaints.Count > 512)
                borderPaints.Clear();
            borderPaints[key] = paints;
        }
        foreach (var paint in paints)
            DrawingContext.DrawGeometry(paint.Brush, paint.Pen, paint.Geometry);
    }
    private static BorderRadii InsetRadii(BorderRadii r, float left, float top, float right, float bottom)
        => new(Math.Max(0, r.TopLeftX - left), Math.Max(0, r.TopLeftY - top), Math.Max(0, r.TopRightX - right), Math.Max(0, r.TopRightY - top), Math.Max(0, r.BottomRightX - right), Math.Max(0, r.BottomRightY - bottom), Math.Max(0, r.BottomLeftX - left), Math.Max(0, r.BottomLeftY - bottom));
    public override void DrawListMarker(RectF position, ColorRgba color, int type, int index, nuint font, string image, string root)
    {
        if (image.Length != 0)
        {
            var bitmap = Image(image, root);
            if (bitmap != null)
                DrawingContext.DrawImage(bitmap, Rect(position));
            return;
        }
        if (type == 0)
            return;
        if (type == 1)
        {
            if (!markerPens.TryGetValue(color, out var pen))
                markerPens[color] = pen = new Pen(Brush(color), 1);
            DrawingContext.DrawEllipse(null, pen, Rect(position));
        }
        else if (type == 2)
            DrawingContext.DrawEllipse(Brush(color), null, Rect(position));
        else if (type == 3)
            DrawingContext.DrawRectangle(Brush(color), null, Rect(position));
        else
        {
            if (!markerText.TryGetValue(index, out var text))
                markerText[index] = text = index + ".";
            DrawText(position, color, font, text);
        }
    }
    public override void Dispose()
    {
        if (disposed)
            return;
        try
        {
            base.Dispose();
        }
        finally
        {
            if (Document.IsDisposed)
            {
                disposed = true;
                fonts.Clear();
                brushes.Clear();
                markerPens.Clear();
                resolvedUrls.Clear();
                rounded.Clear();
                gradients.Clear();
                borderPaints.Clear();
                markerText.Clear();
            }
        }
    }

}
public enum InputType
{
    Unknown, Button, Textbox
}
public class AvaloniaInputs : List<AvaloniaInput>
{
    public AvaloniaInput GetInputByTagID(string id) => this.FirstOrDefault(x => x.TagID == id);
}
public class AvaloniaInput(InputType type)
{
    public int ID;
    public Control Element;
    public InputType Type = type;
    public string Onclick, Href, TagID;
    public bool IsPlaced, AttributesSetup;
    public TextBox TextBox => Element as TextBox;
    public Button Button => Element as Button;
    public void SetupAttributes(IReadOnlyDictionary<string, string> attributes)
    {
        if (attributes.TryGetValue("value", out var value))
        {
            if (Element is Button button)
                button.Content = value;
            if (Element is TextBox text)
                text.Text = value;
        }
        if (Element is TextBox password && attributes.TryGetValue("type", out var type) &&
            string.Equals(type, "password", StringComparison.OrdinalIgnoreCase)) password.PasswordChar = '●';
        attributes.TryGetValue("href", out Href);
        attributes.TryGetValue("id", out TagID);
        attributes.TryGetValue("onclick", out Onclick);
    }
}
