using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LiteHtmlSharp.Interop;

namespace LiteHtmlSharp.Wpf;

public interface IResourceLoader
{
    /// <summary>Called on a worker thread so image I/O cannot block native layout.</summary>
    byte[] GetResourceBytes(string resource);
    string GetResourceString(string resource);
}
public delegate FontFamily FontAbsolutePathDelegate(string fontName);

public class WpfContainer : ViewportContainer
{
    private sealed class ResourceLoader(Func<string, string> text, Func<string, byte[]> bytes) : IResourceLoader
    {
        public byte[] GetResourceBytes(string resource) => bytes(resource);
        public string GetResourceString(string resource) => text(resource);
    }
    private readonly IResourceLoader loader;
    private readonly Dictionary<nuint, FontInfo> fonts = new();
    private readonly Dictionary<ColorRgba, Brush> brushes = new();
    private readonly Dictionary<(RectF, BorderRadii), Geometry> geometry = new();
    private sealed record GradientEntry(lh_color_stop[] Stops, Brush Brush);
    private readonly Dictionary<(int, RectF, float, float, float, float, int, int, int), List<GradientEntry>> gradients = new();
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<BitmapSource, Brush> imageBrushes = new();
    private readonly Dictionary<(Borders, RectF), DrawingGroup> borderDrawings = new();
    private nuint nextFont = 1;
    private string baseUrl = "";
    private bool disposed;
    public bool Loaded => Document.HasLoadedHtml;
    public FontAbsolutePathDelegate FontAbsolutePathDelegate { get; set; }
    public override string DefaultFontName { get; set; } = "Arial";
    public DrawingContext DrawingContext { get; set; }
    public Action<string> SetCursorCallback { get; set; }
    public WpfContainer(string css, IResourceLoader loader) : base(css) => this.loader = loader;
    public WpfContainer(string css, Func<string, string> text, Func<string, byte[]> bytes) : this(css, new ResourceLoader(text, bytes)) { }
    public override void SetCursor(string cursor) => SetCursorCallback?.Invoke(cursor);
    public override void SetBaseUrl(string value) => baseUrl = value;
    internal Action<string> HtmlRenderHandler { get; set; }
    protected override void OnRenderHtml(string html)
    {
        if (HtmlRenderHandler is { } render) render(html);
        else base.OnRenderHtml(html);
    }
    public override nuint CreateFont(FontDescription description, out FontMetrics metrics)
    {
        FontFamily family;
        try { family = FontAbsolutePathDelegate?.Invoke(description.Family) ?? new FontFamily(description.Family.Replace("'", "").Replace("\"", "")); }
        catch (ArgumentException) { family = new FontFamily(DefaultFontName); }
        var font = new FontInfo(description, family, DefaultFontSize, Viewport);
        var id = nextFont++;
        fonts.Add(id, font);
        metrics = font.Metrics;
        return id;
    }
    public override void DeleteFont(nuint font) => fonts.Remove(font);
    public override float TextWidth(string text, nuint font) => fonts[font].Width(text);
    public override void DrawText(RectF position, ColorRgba color, nuint font, string text, float decorationOpacity = 1) => fonts[font].Draw(DrawingContext, position, text, color, decorationOpacity);
    private Brush Brush(ColorRgba color)
    {
        if (!brushes.TryGetValue(color, out var value)) brushes[color] = value = color.GetBrush();
        return value;
    }
    private static Rect Rect(RectF r) => new(r.X, r.Y, Math.Max(0, r.Width), Math.Max(0, r.Height));
    private Geometry Rounded(RectF box, BorderRadii radii)
    {
        var key = (box, radii);
        if (geometry.TryGetValue(key, out var cached)) return cached;
        var r = Rect(box);
        double factor = 1;
        void Fit(double sum, double maximum) { if (sum > 0) factor = Math.Min(factor, maximum / sum); }
        Fit(radii.TopLeftX + radii.TopRightX, r.Width); Fit(radii.BottomLeftX + radii.BottomRightX, r.Width);
        Fit(radii.TopLeftY + radii.BottomLeftY, r.Height); Fit(radii.TopRightY + radii.BottomRightY, r.Height);
        Size Size(float x, float y) => new(Math.Max(0, x * factor), Math.Max(0, y * factor));
        var tl = Size(radii.TopLeftX, radii.TopLeftY); var tr = Size(radii.TopRightX, radii.TopRightY);
        var br = Size(radii.BottomRightX, radii.BottomRightY); var bl = Size(radii.BottomLeftX, radii.BottomLeftY);
        var result = new StreamGeometry();
        using (var c = result.Open())
        {
            void Arc(Point p, Size size) { if (size.Width > 0 && size.Height > 0) c.ArcTo(p, size, 0, false, SweepDirection.Clockwise, true, false); else c.LineTo(p, true, false); }
            c.BeginFigure(new Point(r.Left + tl.Width, r.Top), true, true);
            c.LineTo(new Point(r.Right - tr.Width, r.Top), true, false); Arc(new Point(r.Right, r.Top + tr.Height), tr);
            c.LineTo(new Point(r.Right, r.Bottom - br.Height), true, false); Arc(new Point(r.Right - br.Width, r.Bottom), br);
            c.LineTo(new Point(r.Left + bl.Width, r.Bottom), true, false); Arc(new Point(r.Left, r.Bottom - bl.Height), bl);
            c.LineTo(new Point(r.Left, r.Top + tl.Height), true, false); Arc(new Point(r.Left + tl.Width, r.Top), tl);
        }
        result.Freeze();
        if (geometry.Count > 1024) geometry.Clear();
        geometry[key] = result;
        return result;
    }
    public override void PushClip(RectF position, BorderRadii radii) => DrawingContext.PushClip(Rounded(position, radii));
    public override void PopClip() => DrawingContext.Pop();
    public override void PushTransform(Matrix4x4 m) => DrawingContext.PushTransform(new MatrixTransform(m.M11, m.M12, m.M21, m.M22, m.M41, m.M42));
    public override void PushOpacity(float opacity) => DrawingContext.PushOpacity(opacity);
    public override void PopOpacity() => DrawingContext.Pop();
    public override void PopTransform() => DrawingContext.Pop();
    public override void FillRect(Layer layer, ColorRgba color)
    {
        DrawingContext.PushClip(new RectangleGeometry(Rect(layer.ClipBox)));
        DrawingContext.PushOpacity(layer.Opacity);
        try { DrawingContext.DrawGeometry(Brush(color), null, Rounded(layer.BorderBox, layer.Radius)); }
        finally { DrawingContext.Pop(); DrawingContext.Pop(); }
    }
    private void PaintTiles(Layer layer, Brush brush)
    {
        var tile = layer.OriginBox;
        if (tile.Width <= 0 || tile.Height <= 0) return;
        DrawingContext.PushClip(Rounded(layer.BorderBox, layer.Radius));
        DrawingContext.PushClip(new RectangleGeometry(Rect(layer.ClipBox)));
        DrawingContext.PushOpacity(layer.Opacity);
        try
        {
            bool repeatX = layer.Repeat is 0 or 1, repeatY = layer.Repeat is 0 or 2;
            var x0 = repeatX ? tile.X + Math.Floor((layer.ClipBox.X - tile.X) / tile.Width) * tile.Width : tile.X;
            var y0 = repeatY ? tile.Y + Math.Floor((layer.ClipBox.Y - tile.Y) / tile.Height) * tile.Height : tile.Y;
            var right = repeatX ? layer.ClipBox.X + layer.ClipBox.Width : x0 + tile.Width;
            var bottom = repeatY ? layer.ClipBox.Y + layer.ClipBox.Height : y0 + tile.Height;
            for (double y = y0; y < bottom; y += tile.Height)
                for (double x = x0; x < right; x += tile.Width)
                {
                    DrawingContext.PushTransform(new TranslateTransform(x - tile.X, y - tile.Y));
                    try { DrawingContext.DrawRectangle(brush, null, Rect(tile)); }
                    finally { DrawingContext.Pop(); }
                }
        }
        finally { DrawingContext.Pop(); DrawingContext.Pop(); DrawingContext.Pop(); }
    }
    private static Color Color(lh_color c) => System.Windows.Media.Color.FromArgb(c.a, c.r, c.g, c.b);
    private static Color Blend(lh_color a, lh_color b, float t, bool linear)
    {
        var aa = a.a / 255f; var ab = b.a / 255f; var alpha = aa * (1 - t) + ab * t;
        float Decode(float c) => !linear ? c : c <= .04045f ? c / 12.92f : MathF.Pow((c + .055f) / 1.055f, 2.4f);
        float Encode(float c) => !linear ? c : c <= .0031308f ? c * 12.92f : 1.055f * MathF.Pow(c, 1 / 2.4f) - .055f;
        byte Channel(byte x, byte y) => alpha <= 0 ? (byte)0 : (byte)Math.Clamp(MathF.Round(Encode((Decode(x / 255f) * aa * (1 - t) + Decode(y / 255f) * ab * t) / alpha) * 255), 0, 255);
        return System.Windows.Media.Color.FromArgb((byte)Math.Clamp(MathF.Round(alpha * 255), 0, 255), Channel(a.r, b.r), Channel(a.g, b.g), Channel(a.b, b.b));
    }
    private static GradientStopCollection Stops(ReadOnlySpan<lh_color_stop> source, int space)
    {
        var result = new GradientStopCollection();
        for (int i = 0; i < source.Length; i++)
        {
            var stop = source[i]; result.Add(new GradientStop(Color(stop.color), stop.offset));
            if (i + 1 == source.Length || (stop.has_hint == 0 && space != 2)) continue;
            var next = source[i + 1]; var distance = next.offset - stop.offset;
            if (distance <= 0) continue;
            var mid = (stop.hint - stop.offset) / distance;
            for (int j = 1; j < 32; j++)
            {
                var p = j / 32f;
                var blend = stop.has_hint == 0 ? p : mid <= 0 ? 1 : mid >= 1 ? 0 : MathF.Pow(p, MathF.Log(.5f) / MathF.Log(mid));
                result.Add(new GradientStop(Blend(stop.color, next.color, blend, space == 2), stop.offset + p * distance));
            }
        }
        return result;
    }
    private static (int, RectF, float, float, float, float, int, int, int) GradientKey(int kind, Layer layer, float a, float b, float c, float d, ReadOnlySpan<lh_color_stop> stops, int space, int hue)
    {
        var hash = new HashCode();
        foreach (var value in MemoryMarshal.AsBytes(stops)) hash.Add(value);
        return (kind, layer.OriginBox, a, b, c, d, space, hue, hash.ToHashCode());
    }
    private bool FindGradient((int, RectF, float, float, float, float, int, int, int) key, ReadOnlySpan<lh_color_stop> stops, out Brush brush)
    {
        if (gradients.TryGetValue(key, out var entries))
            foreach (var entry in entries)
                if (MemoryMarshal.AsBytes(stops).SequenceEqual(MemoryMarshal.AsBytes(entry.Stops.AsSpan())))
                { brush = entry.Brush; return true; }
        brush = null;
        return false;
    }
    private Brush Cache((int, RectF, float, float, float, float, int, int, int) key, ReadOnlySpan<lh_color_stop> stops, Brush value)
    {
        value.Freeze();
        if (gradients.Count > 256) gradients.Clear();
        if (!gradients.TryGetValue(key, out var entries)) gradients[key] = entries = new();
        entries.Add(new(stops.ToArray(), value));
        return value;
    }
    public override void DrawLinearGradient(Layer layer, float x0, float y0, float x1, float y1, ReadOnlySpan<lh_color_stop> stops, int space, int hue)
    {
        var key = GradientKey(1, layer, x0, y0, x1, y1, stops, space, hue);
        if (!FindGradient(key, stops, out var brush)) brush = Cache(key, stops, new LinearGradientBrush(Stops(stops, space), new Point(x0, y0), new Point(x1, y1)) { MappingMode = BrushMappingMode.Absolute });
        PaintTiles(layer, brush);
    }
    public override void DrawRadialGradient(Layer layer, float cx, float cy, float rx, float ry, ReadOnlySpan<lh_color_stop> stops, int space, int hue)
    {
        if (rx <= 0 || ry <= 0) return;
        var key = GradientKey(2, layer, cx, cy, rx, ry, stops, space, hue);
        if (!FindGradient(key, stops, out var brush)) brush = Cache(key, stops, new RadialGradientBrush(Stops(stops, space)) { MappingMode = BrushMappingMode.Absolute, Center = new(cx, cy), GradientOrigin = new(cx, cy), RadiusX = rx, RadiusY = ry });
        PaintTiles(layer, brush);
    }
    public override void DrawConicGradient(Layer layer, float cx, float cy, float angle, float radius, ReadOnlySpan<lh_color_stop> stops, int space, int hue)
    {
        var tile = layer.OriginBox;
        if (tile.Width <= 0 || tile.Height <= 0 || stops.IsEmpty) return;
        var key = GradientKey(3, layer, cx, cy, angle, radius, stops, space, hue);
        if (!FindGradient(key, stops, out var brush))
        {
            // WPF has no conic brush; rasterize one tile and reuse it during replay.
            int w = Math.Clamp((int)Math.Ceiling(tile.Width), 1, 2048), h = Math.Clamp((int)Math.Ceiling(tile.Height), 1, 2048);
            var pixels = new byte[w * h * 4];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                var turn = (Math.Atan2(tile.Y + (y + .5) * tile.Height / h - cy, tile.X + (x + .5) * tile.Width / w - cx) + Math.PI / 2 - angle * Math.PI / 180) / (2 * Math.PI);
                turn -= Math.Floor(turn);
                int n = 0; while (n + 1 < stops.Length && stops[n + 1].offset <= turn) n++;
                var a = stops[n]; var b = stops[Math.Min(n + 1, stops.Length - 1)];
                var t = b.offset <= a.offset ? 0 : Math.Clamp((float)(turn - a.offset) / (b.offset - a.offset), 0, 1);
                if (a.has_hint != 0 && b.offset > a.offset) { var mid = (a.hint - a.offset) / (b.offset - a.offset); t = mid <= 0 ? 1 : mid >= 1 ? 0 : MathF.Pow(t, MathF.Log(.5f) / MathF.Log(mid)); }
                var color = Blend(a.color, b.color, t, space == 2); int i = (y * w + x) * 4;
                pixels[i] = (byte)(color.B * color.A / 255); pixels[i + 1] = (byte)(color.G * color.A / 255); pixels[i + 2] = (byte)(color.R * color.A / 255); pixels[i + 3] = color.A;
            }
            var bitmap = BitmapSource.Create(w, h, 96, 96, PixelFormats.Pbgra32, null, pixels, w * 4); bitmap.Freeze();
            brush = Cache(key, stops, new ImageBrush(bitmap) { Stretch = Stretch.Fill });
        }
        PaintTiles(layer, brush);
    }
    private string Resolve(string source, string context)
    {
        if (Uri.TryCreate(source, UriKind.Absolute, out var absolute)) return absolute.ToString();
        return Uri.TryCreate(string.IsNullOrEmpty(context) ? baseUrl : context, UriKind.Absolute, out var root) && Uri.TryCreate(root, source, out var full) ? full.ToString() : source;
    }
    protected override (string Source, string BaseUrl) ResolveImageKey(string source, string context) => (Resolve(source, context), "");

    protected override async ValueTask<IImageSource> LoadImageSourceAsync(string source, string context, CancellationToken cancellationToken)
    {
        var resolved = source;
        // Resource callbacks run synchronously on the requesting thread.
        cancellationToken.ThrowIfCancellationRequested();
        var bytes = loader?.GetResourceBytes(resolved);
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return bytes == null || bytes.Length == 0 ? null : WpfImageSource.Decode(bytes, cancellationToken);
        }, cancellationToken).ConfigureAwait(false);
    }
    private BitmapSource Image(string source, string context) => GetImageFrame(source, context) as BitmapSource;
    public override void DrawImage(Layer layer, string source, string context)
    {
        var image = Image(source, context); if (image != null)
        {
            var brush = imageBrushes.GetValue(image, bitmap =>
            {
                var value = new ImageBrush(bitmap) { Stretch = Stretch.Fill };
                value.Freeze();
                return value;
            });
            PaintTiles(layer, brush);
        }
    }
    protected override (string Css, string BaseUrl) ImportCss(string url, string context)
    {
        if (ImportCssRequest != null) return base.ImportCss(url, context);
        var resolved = Resolve(url, context); return (loader?.GetResourceString(resolved) ?? "", resolved);
    }
    public override void DrawBorders(Borders borders, RectF position, bool root)
    {
        var key = (borders, position);
        if (borderDrawings.TryGetValue(key, out var cached)) { DrawingContext.DrawDrawing(cached); return; }
        var drawing = new DrawingGroup();
        using (var context = drawing.Open())
        {
            var outer = Rounded(position, borders.Radius);
            var innerBox = new RectF(position.X + borders.Left.Width, position.Y + borders.Top.Width, Math.Max(0, position.Width - borders.Left.Width - borders.Right.Width), Math.Max(0, position.Height - borders.Top.Width - borders.Bottom.Width));
            var r = borders.Radius;
            var inner = Rounded(innerBox, new(Math.Max(0, r.TopLeftX - borders.Left.Width), Math.Max(0, r.TopLeftY - borders.Top.Width), Math.Max(0, r.TopRightX - borders.Right.Width), Math.Max(0, r.TopRightY - borders.Top.Width), Math.Max(0, r.BottomRightX - borders.Right.Width), Math.Max(0, r.BottomRightY - borders.Bottom.Width), Math.Max(0, r.BottomLeftX - borders.Left.Width), Math.Max(0, r.BottomLeftY - borders.Bottom.Width)));
            var ring = new CombinedGeometry(GeometryCombineMode.Exclude, outer, inner); ring.Freeze();
            var box = Rect(position); var inside = Rect(innerBox);
            void Side(LiteHtmlSharp.Border border, Point a, Point b, Point innerB, Point innerA)
            {
                if (border.Width <= 0 || border.Style < 2) return;
                var triangle = new StreamGeometry(); using (var c = triangle.Open()) { c.BeginFigure(a, true, true); c.LineTo(b, true, false); c.LineTo(innerB, true, false); c.LineTo(innerA, true, false); }
                triangle.Freeze();
                context.PushClip(triangle); try { context.DrawGeometry(Brush(border.Color), null, ring); } finally { context.Pop(); }
            }
            if (borders.Left == borders.Top && borders.Top == borders.Right && borders.Right == borders.Bottom && borders.Top.Width > 0 && borders.Top.Style is 2 or 3 or 5)
            {
                var border = borders.Top;
                void Outline(float inset, float width)
                {
                    var rect = new RectF(position.X + inset, position.Y + inset, Math.Max(0, position.Width - 2 * inset), Math.Max(0, position.Height - 2 * inset));
                    var radius = new BorderRadii(Math.Max(0, r.TopLeftX - inset), Math.Max(0, r.TopLeftY - inset), Math.Max(0, r.TopRightX - inset), Math.Max(0, r.TopRightY - inset), Math.Max(0, r.BottomRightX - inset), Math.Max(0, r.BottomRightY - inset), Math.Max(0, r.BottomLeftX - inset), Math.Max(0, r.BottomLeftY - inset));
                    var pen = new Pen(Brush(border.Color), width) { DashStyle = border.Style == 2 ? DashStyles.Dot : border.Style == 3 ? DashStyles.Dash : DashStyles.Solid }; pen.Freeze();
                    context.DrawGeometry(null, pen, Rounded(rect, radius));
                }
                if (border.Style == 5) { Outline(border.Width / 6, border.Width / 3); Outline(border.Width * 5 / 6, border.Width / 3); }
                else Outline(border.Width / 2, border.Width);
            }
            else { Side(borders.Top, box.TopLeft, box.TopRight, inside.TopRight, inside.TopLeft); Side(borders.Right, box.TopRight, box.BottomRight, inside.BottomRight, inside.TopRight); Side(borders.Bottom, box.BottomRight, box.BottomLeft, inside.BottomLeft, inside.BottomRight); Side(borders.Left, box.BottomLeft, box.TopLeft, inside.TopLeft, inside.BottomLeft); }
        }
        drawing.Freeze();
        if (borderDrawings.Count > 512) borderDrawings.Clear();
        borderDrawings[key] = drawing;
        DrawingContext.DrawDrawing(drawing);
    }
    public override void DrawListMarker(RectF position, ColorRgba color, int markerType, int index, nuint font, string image, string context)
    {
        if (!string.IsNullOrEmpty(image)) { var bitmap = Image(image, context); if (bitmap != null) DrawingContext.DrawImage(bitmap, Rect(position)); return; }
        if (markerType is 1 or 2) { var rect = Rect(position); DrawingContext.DrawEllipse(markerType == 2 ? Brush(color) : null, markerType == 1 ? new Pen(Brush(color), 1) : null, new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2), rect.Width / 2, rect.Height / 2); }
        else if (markerType == 3) DrawingContext.DrawRectangle(Brush(color), null, Rect(position));
        else if (markerType != 0 && fonts.ContainsKey(font)) DrawText(position, color, font, index.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".");
    }
    public override void Dispose()
    {
        if (disposed) return;
        try { base.Dispose(); }
        finally { if (Document.IsDisposed) { disposed = true; fonts.Clear(); brushes.Clear(); geometry.Clear(); gradients.Clear(); imageBrushes.Clear(); borderDrawings.Clear(); DrawingContext = null; } }
    }
}
public enum InputType { Unknown, Button, Textbox }
public class Inputs : List<Input>
{
    public Input GetInputByTagID(string id) => this.FirstOrDefault(x => x.TagID == id);
}
public class Input(InputType type)
{
    public int ID; public FrameworkElement Element; public bool IsPlaced; public InputType Type = type;
    public string Onclick; public string Href; public string TagID; public bool AttributesSetup { get; set; }
    public TextBox TextBox => Element as TextBox; public Button Button => Element as Button;
    public void SetupAttributes(IReadOnlyDictionary<string, string> attributes)
    {
        if (attributes.TryGetValue("value", out var value)) { if (Element is Button button) button.Content = value; else if (Element is TextBox text) text.Text = value; }
        attributes.TryGetValue("href", out Href); attributes.TryGetValue("id", out TagID); attributes.TryGetValue("onclick", out Onclick);
    }
}
