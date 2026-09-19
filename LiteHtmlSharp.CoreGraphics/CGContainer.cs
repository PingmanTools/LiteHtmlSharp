using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Numerics;
using System.Runtime.InteropServices;
using CoreGraphics;
using LiteHtmlSharp.Interop;

namespace LiteHtmlSharp.CoreGraphics;


public class CGContainer : ViewportContainer
{
    public delegate ImageHolder LoadImageDelegate(string imageUrl);
    public delegate void SetCursorDelegate(string cursor);
    public LoadImageDelegate LoadImageCallback { get; set; }
    public Func<string, byte[]> LoadImageDataCallback { get; set; }
    public Func<string, string> LoadCssCallback { get; set; }
    public SetCursorDelegate SetCursorCallback { get; set; }
    public event Action<string> CaptionDefined;
    public CGContext Context { get; set; }
    public override string DefaultFontName { get; set; } = "Helvetica";
    private readonly Dictionary<nuint, FontHolder> fonts = new();
    private readonly Dictionary<(RectF, BorderRadii), CGPath> paths = new();
    private readonly Dictionary<int, List<GradientEntry>> gradients = new();
    private sealed record GradientEntry(lh_color_stop[] Stops, int Space, int Hue, CGGradient Gradient);
    private sealed record ConicEntry(lh_color_stop[] Stops, CGImage Image);
    private readonly Dictionary<(float, float, float, float, float, float, int, int), List<ConicEntry>> conics = new();
    private nuint nextFont;
    private string baseUrl = "";
    private bool disposed;
    public CGContainer(string masterCssData = null) : base(masterCssData)
    {
    }
    public override float PointToPixels(float points) => points * 96 / 72;
    public override void SetCaption(string caption) => CaptionDefined?.Invoke(caption);
    public override void SetBaseUrl(string url) => baseUrl = url;
    public override void SetCursor(string cursor) => SetCursorCallback?.Invoke(cursor);
    internal Action<string> HtmlRenderHandler { get; set; }
    protected override void OnRenderHtml(string html)
    {
        if (HtmlRenderHandler is { } render) render(html);
        else base.OnRenderHtml(html);
    }
    public override nuint CreateFont(FontDescription description, out FontMetrics metrics)
    {
        var font = new FontHolder(description, DefaultFontSize, Viewport);
        var id = ++nextFont;
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
        => fonts[font].Draw(Context, position, color, text, decorationOpacity);
    public override void PushTransform(Matrix4x4 m)
    {
        Context.SaveState();
        Context.ConcatCTM(new CGAffineTransform(m.M11, m.M12, m.M21, m.M22, m.M41, m.M42));
    }
    public override void PushOpacity(float opacity)
    {
        Context.SaveState();
        Context.SetAlpha(opacity);
        Context.BeginTransparencyLayer(null);
    }
    public override void PopOpacity()
    {
        Context.EndTransparencyLayer();
        Context.RestoreState();
    }
    public override void PopTransform() => Context.RestoreState();
    public override void PushClip(RectF position, BorderRadii radii)
    {
        Context.SaveState();
        Context.AddPath(Rounded(position, radii));
        Context.Clip();
    }
    public override void PopClip() => Context.RestoreState();
    private CGPath Rounded(RectF r, BorderRadii radius)
    {
        if (paths.TryGetValue((r, radius), out var path))
            return path;
        if (paths.Count >= 512)
        {
            foreach (var p in paths.Values)
                p.Dispose();
            paths.Clear();
        }
        var rect = r.ToRect();
        var factor = 1f;
        void Fit(float sum, float side)
        {
            if (sum > 0)
                factor = Math.Min(factor, side / sum);
        }
        Fit(radius.TopLeftX + radius.TopRightX, r.Width);
        Fit(radius.BottomLeftX + radius.BottomRightX, r.Width);
        Fit(radius.TopLeftY + radius.BottomLeftY, r.Height);
        Fit(radius.TopRightY + radius.BottomRightY, r.Height);
        var tlx = Math.Max(0, radius.TopLeftX * factor);
        var tly = Math.Max(0, radius.TopLeftY * factor);
        var trx = Math.Max(0, radius.TopRightX * factor);
        var tryy = Math.Max(0, radius.TopRightY * factor);
        var brx = Math.Max(0, radius.BottomRightX * factor);
        var bry = Math.Max(0, radius.BottomRightY * factor);
        var blx = Math.Max(0, radius.BottomLeftX * factor);
        var bly = Math.Max(0, radius.BottomLeftY * factor);
        const float k = .55228475f;
        path = new CGPath();
        path.MoveToPoint(rect.Left + tlx, rect.Top);
        path.AddLineToPoint(rect.Right - trx, rect.Top);
        path.AddCurveToPoint(rect.Right - trx + k * trx, rect.Top, rect.Right, rect.Top + tryy - k * tryy, rect.Right,
                             rect.Top + tryy);
        path.AddLineToPoint(rect.Right, rect.Bottom - bry);
        path.AddCurveToPoint(rect.Right, rect.Bottom - bry + k * bry, rect.Right - brx + k * brx, rect.Bottom,
                             rect.Right - brx, rect.Bottom);
        path.AddLineToPoint(rect.Left + blx, rect.Bottom);
        path.AddCurveToPoint(rect.Left + blx - k * blx, rect.Bottom, rect.Left, rect.Bottom - bly + k * bly, rect.Left,
                             rect.Bottom - bly);
        path.AddLineToPoint(rect.Left, rect.Top + tly);
        path.AddCurveToPoint(rect.Left, rect.Top + tly - k * tly, rect.Left + tlx - k * tlx, rect.Top, rect.Left + tlx,
                             rect.Top);
        path.CloseSubpath();
        paths[(r, radius)] = path;
        return path;
    }
    private void BeginLayer(Layer layer)
    {
        Context.SaveState();
        Context.AddPath(Rounded(layer.BorderBox, layer.Radius));
        Context.Clip();
        Context.ClipToRect(layer.ClipBox.ToRect());
        Context.SetAlpha(layer.Opacity);
    }
    public override void FillRect(Layer layer, ColorRgba color)
    {
        BeginLayer(layer);
        try
        {
            using var fill = color.ToCGColor();
            Context.SetFillColor(fill);
            Context.FillRect(layer.BorderBox.ToRect());
        }
        finally
        {
            Context.RestoreState();
        }
    }
    private void Tiles(Layer layer, Action<float, float> draw)
    {
        var tile = layer.OriginBox;
        if (tile.Width <= 0 || tile.Height <= 0)
            return;
        var repeatX = layer.Repeat is 0 or 1;
        var repeatY = layer.Repeat is 0 or 2;
        var firstX = repeatX ? tile.X + MathF.Floor((layer.ClipBox.X - tile.X) / tile.Width) * tile.Width : tile.X;
        var firstY = repeatY ? tile.Y + MathF.Floor((layer.ClipBox.Y - tile.Y) / tile.Height) * tile.Height : tile.Y;
        var right = repeatX ? layer.ClipBox.X + layer.ClipBox.Width : firstX + tile.Width;
        var bottom = repeatY ? layer.ClipBox.Y + layer.ClipBox.Height : firstY + tile.Height;
        BeginLayer(layer);
        try
        {
            for (double y = firstY; y < bottom; y += tile.Height)
                for (double x = firstX; x < right; x += tile.Width)
                {
                    Context.SaveState();
                    try
                    {
                        Context.ClipToRect(new CGRect(x, y, tile.Width, tile.Height));
                        draw((float)x, (float)y);
                    }
                    finally
                    {
                        Context.RestoreState();
                    }
                }
        }
        finally
        {
            Context.RestoreState();
        }
    }
    private static lh_color Interpolate(lh_color a, lh_color b, float t, bool linear)
    {
        float Decode(float v) => !linear ? v : v <= .04045f ? v / 12.92f : MathF.Pow((v + .055f) / 1.055f, 2.4f);
        float Encode(float v) => !linear ? v : v <= .0031308f ? v * 12.92f : 1.055f * MathF.Pow(v, 1 / 2.4f) - .055f;
        var aa = a.a / 255f;
        var ab = b.a / 255f;
        var alpha = aa + (ab - aa) * t;
        byte C(byte x, byte y) =>
            alpha <= 0
                ? (byte)0
                : (byte)Math.Clamp(
                      MathF.Round(Encode((Decode(x / 255f) * aa * (1 - t) + Decode(y / 255f) * ab * t) / alpha) * 255),
                      0, 255);
        return new lh_color { r = C(a.r, b.r), g = C(a.g, b.g), b = C(a.b, b.b),
                              a = (byte)Math.Clamp(MathF.Round(alpha * 255), 0, 255) };
    }
    private static lh_color Sample(ReadOnlySpan<lh_color_stop> stops, float t, int space)
    {
        if (stops.Length == 0)
            return default;
        if (t <= stops[0].offset)
            return stops[0].color;
        for (int i = 1; i < stops.Length; i++)
            if (t <= stops[i].offset)
            {
                var a = stops[i - 1];
                var b = stops[i];
                var distance = b.offset - a.offset;
                if (distance <= 0)
                    return b.color;
                var blend = (t - a.offset) / distance;
                if (a.has_hint != 0)
                {
                    var midpoint = (a.hint - a.offset) / distance;
                    blend = midpoint <= 0   ? 1
                            : midpoint >= 1 ? 0
                                            : MathF.Pow(blend, MathF.Log(.5f) / MathF.Log(midpoint));
                }
                return Interpolate(a.color, b.color, blend, space == 2);
            }
        return stops[^1].color;
    }
    private CGGradient Gradient(ReadOnlySpan<lh_color_stop> stops, int space, int hue)
    {
        var hash = new HashCode();
        hash.Add(space);
        hash.Add(hue);
        foreach (var stop in stops)
        {
            hash.Add(stop.offset);
            hash.Add(stop.color.r);
            hash.Add(stop.color.g);
            hash.Add(stop.color.b);
            hash.Add(stop.color.a);
            hash.Add(stop.hint);
            hash.Add(stop.has_hint);
        }
        int key = hash.ToHashCode();
        if (gradients.TryGetValue(key, out var existing))
            foreach (var entry in existing)
                if (entry.Space == space && entry.Hue == hue &&
                    MemoryMarshal.AsBytes(stops).SequenceEqual(MemoryMarshal.AsBytes(entry.Stops.AsSpan())))
                    return entry.Gradient;
        if (gradients.Count >= 256)
        {
            foreach (var values in gradients.Values)
                foreach (var e in values)
                    e.Gradient.Dispose();
            gradients.Clear();
        }
        var locations = new List<nfloat>();
        var components = new List<nfloat>();
        void Add(float offset, lh_color c)
        {
            locations.Add(offset);
            components.Add(c.r / 255f);
            components.Add(c.g / 255f);
            components.Add(c.b / 255f);
            components.Add(c.a / 255f);
        }
        Add(0, Sample(stops, 0, space));
        for (int i = 0; i < stops.Length; i++)
        {
            var stop = stops[i];
            if (stop.offset > 0 && stop.offset < 1)
                Add(stop.offset, stop.color);
            if (i + 1 == stops.Length || (stop.has_hint == 0 && space != 2))
                continue;
            for (int step = 1; step < 32; step++)
            {
                float offset = stop.offset + (stops[i + 1].offset - stop.offset) * step / 32;
                if (offset > 0 && offset < 1)
                    Add(offset, Sample(stops, offset, space));
            }
        }
        Add(1, Sample(stops, 1, space));
        using var colorSpace = CGColorSpace.CreateDeviceRGB();
        var gradient = new CGGradient(colorSpace, components.ToArray(), locations.ToArray());
        if (!gradients.TryGetValue(key, out existing))
            gradients[key] = existing = new();
        existing.Add(new(stops.ToArray(), space, hue, gradient));
        return gradient;
    }
    public override void DrawLinearGradient(Layer layer, float x0, float y0, float x1, float y1,
                                            ReadOnlySpan<lh_color_stop> stops, int colorSpace, int hueInterpolation)
    {
        var gradient = Gradient(stops, colorSpace, hueInterpolation);
        Tiles(layer, (x, y) =>
                     {
                         var dx = x - layer.OriginBox.X;
                         var dy = y - layer.OriginBox.Y;
                         Context.DrawLinearGradient(gradient, new CGPoint(x0 + dx, y0 + dy),
                                                    new CGPoint(x1 + dx, y1 + dy),
                                                    CGGradientDrawingOptions.DrawsBeforeStartLocation |
                                                        CGGradientDrawingOptions.DrawsAfterEndLocation);
                     });
    }
    public override void DrawRadialGradient(Layer layer, float cx, float cy, float rx, float ry,
                                            ReadOnlySpan<lh_color_stop> stops, int colorSpace, int hueInterpolation)
    {
        if (rx <= 0 || ry <= 0)
            return;
        var gradient = Gradient(stops, colorSpace, hueInterpolation);
        Tiles(layer, (x, y) =>
                     {
                         Context.TranslateCTM(cx + x - layer.OriginBox.X, cy + y - layer.OriginBox.Y);
                         Context.ScaleCTM(rx, ry);
                         Context.DrawRadialGradient(gradient, new CGPoint(0, 0), 0, new CGPoint(0, 0), 1,
                                                    CGGradientDrawingOptions.DrawsBeforeStartLocation |
                                                        CGGradientDrawingOptions.DrawsAfterEndLocation);
                     });
    }
    public override void DrawConicGradient(Layer layer, float cx, float cy, float angle, float radius,
                                           ReadOnlySpan<lh_color_stop> stops, int colorSpace, int hueInterpolation)
    {
        var tile = layer.OriginBox;
        if (tile.Width <= 0 || tile.Height <= 0)
            return;
        var key = (tile.Width, tile.Height, cx - tile.X, cy - tile.Y, angle, radius, colorSpace, hueInterpolation);
        CGImage image = null;
        if (conics.TryGetValue(key, out var entries))
            foreach (var entry in entries)
                if (MemoryMarshal.AsBytes(stops).SequenceEqual(MemoryMarshal.AsBytes(entry.Stops.AsSpan())))
                {
                    image = entry.Image;
                    break;
                }
        if (image == null)
        {
            if (conics.Count >= 32)
            {
                foreach (var group in conics.Values)
                    foreach (var entry in group)
                        entry.Image.Dispose();
                conics.Clear();
            }
            int width = Math.Clamp((int)MathF.Ceiling(tile.Width), 1, 2048),
                height = Math.Clamp((int)MathF.Ceiling(tile.Height), 1, 2048);
            using var space = CGColorSpace.CreateDeviceRGB();
            using var bitmap = new CGBitmapContext(IntPtr.Zero, width, height, 8, width * 4, space,
                                                   CGImageAlphaInfo.PremultipliedLast);
            // Sampling each pixel avoids antialiased wedge edges accumulating into radial seams.
            var pixels = new byte[width * height * 4];
            for (int y = 0; y < height; y++)
            {
                var dy = (y + .5f) * tile.Height / height - (cy - tile.Y);
                for (int x = 0; x < width; x++)
                {
                    var dx = (x + .5f) * tile.Width / width - (cx - tile.X);
                    var turn = (MathF.Atan2(dy, dx) * 180 / MathF.PI + 90 - angle) / 360;
                    turn -= MathF.Floor(turn);
                    var color = Sample(stops, turn, colorSpace);
                    var offset = (y * width + x) * 4;
                    pixels[offset] = (byte)((color.r * color.a + 127) / 255);
                    pixels[offset + 1] = (byte)((color.g * color.a + 127) / 255);
                    pixels[offset + 2] = (byte)((color.b * color.a + 127) / 255);
                    pixels[offset + 3] = color.a;
                }
            }
            Marshal.Copy(pixels, 0, bitmap.Data, pixels.Length);
            image = bitmap.ToImage();
            if (!conics.TryGetValue(key, out entries))
                conics[key] = entries = new();
            entries.Add(new(stops.ToArray(), image));
        }
        Tiles(layer, (x, y) =>
                     {
                         Context.TranslateCTM(x, y + tile.Height);
                         Context.ScaleCTM(1, -1);
                         Context.DrawImage(new CGRect(0, 0, tile.Width, tile.Height), image);
                     });
    }
    private string Resolve(string source, string root) => Uri.TryCreate(string.IsNullOrEmpty(root) ? baseUrl : root,
                                                                        UriKind.Absolute, out var b) &&
                                                                  Uri.TryCreate(b, source, out var result)
                                                              ? result.ToString()
                                                              : source;
    protected override (string Source, string BaseUrl) ResolveImageKey(string source, string root) => (Resolve(source, root), "");
    protected override ValueTask<IImageSource> LoadImageSourceAsync(string source, string root, CancellationToken cancellationToken)
    {
        var key = Resolve(source, root);
        if (LoadImageDataCallback == null)
            return ValueTask.FromResult<IImageSource>(LoadImageCallback?.Invoke(key));
        var loader = LoadImageDataCallback;
        // Resource callbacks run synchronously on the requesting thread.
        cancellationToken.ThrowIfCancellationRequested();
        var bytes = loader(key);
        return new ValueTask<IImageSource>(Task.Run<IImageSource>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return bytes is { Length: > 0 } ? CGImageSourceFrames.Decode(bytes, cancellationToken) : null;
        }, cancellationToken));
    }
    public override void LoadImage(string source, string root, bool redrawOnReady) => _ = LoadImageAsync(source, root, redrawOnReady);
    public override void DrawImage(Layer layer, string source, string root)
    {
        if (GetImageFrame(source, root) is not CGImage image) return;
        Tiles(layer, (x, y) =>
        {
            Context.TranslateCTM(x, y + layer.OriginBox.Height);
            Context.ScaleCTM(1, -1);
            Context.DrawImage(new CGRect(0, 0, layer.OriginBox.Width, layer.OriginBox.Height), image);
        });
    }
    protected override (string Css, string BaseUrl) ImportCss(string url, string root)
    {
        if (ImportCssRequest != null) return base.ImportCss(url, root);
        var resolved = Resolve(url, root);
        return (LoadCssCallback?.Invoke(resolved) ?? "", resolved);
    }
    public override void DrawBorders(Borders b, RectF position, bool root)
    {
        var outer = Rounded(position, b.Radius);
        var inner = new RectF(position.X + b.Left.Width, position.Y + b.Top.Width,
                              Math.Max(0, position.Width - b.Left.Width - b.Right.Width),
                              Math.Max(0, position.Height - b.Top.Width - b.Bottom.Width));
        var ir = new BorderRadii(
            Math.Max(0, b.Radius.TopLeftX - b.Left.Width), Math.Max(0, b.Radius.TopLeftY - b.Top.Width),
            Math.Max(0, b.Radius.TopRightX - b.Right.Width), Math.Max(0, b.Radius.TopRightY - b.Top.Width),
            Math.Max(0, b.Radius.BottomRightX - b.Right.Width), Math.Max(0, b.Radius.BottomRightY - b.Bottom.Width),
            Math.Max(0, b.Radius.BottomLeftX - b.Left.Width), Math.Max(0, b.Radius.BottomLeftY - b.Bottom.Width));
        Context.SaveState();
        try
        {
            Context.AddPath(outer);
            Context.AddPath(Rounded(inner, ir));
            Context.EOClip();
            var r = position.ToRect();
            var inside = inner.ToRect();
            void Side(Border side, CGPoint a, CGPoint c, CGPoint d, CGPoint e)
            {
                if (side.Width <= 0 || side.Style < 2)
                    return;
                using var color = side.Color.ToCGColor();
                Context.SetFillColor(color);
                Context.BeginPath();
                Context.MoveTo(a.X, a.Y);
                Context.AddLineToPoint(c.X, c.Y);
                Context.AddLineToPoint(d.X, d.Y);
                Context.AddLineToPoint(e.X, e.Y);
                Context.ClosePath();
                Context.FillPath();
            }
            Side(b.Top, new(r.Left, r.Top), new(r.Right, r.Top), new(inside.Right, inside.Top),
                 new(inside.Left, inside.Top));
            Side(b.Right, new(r.Right, r.Top), new(r.Right, r.Bottom), new(inside.Right, inside.Bottom),
                 new(inside.Right, inside.Top));
            Side(b.Bottom, new(r.Right, r.Bottom), new(r.Left, r.Bottom), new(inside.Left, inside.Bottom),
                 new(inside.Right, inside.Bottom));
            Side(b.Left, new(r.Left, r.Bottom), new(r.Left, r.Top), new(inside.Left, inside.Top),
                 new(inside.Left, inside.Bottom));
        }
        finally
        {
            Context.RestoreState();
        }
    }
    public override void DrawListMarker(RectF position, ColorRgba color, int type, int index, nuint font, string image,
                                        string root)
    {
        if (image.Length != 0)
        {
            var holder = GetImageFrame(image, root) as CGImage;
            if (holder != null)
            {
                Context.SaveState();
                try
                {
                    Context.TranslateCTM(position.X, position.Y + position.Height);
                    Context.ScaleCTM(1, -1);
                    Context.DrawImage(new CGRect(0, 0, position.Width, position.Height), holder);
                }
                finally
                {
                    Context.RestoreState();
                }
            }
            return;
        }
        using var fill = color.ToCGColor();
        Context.SetFillColor(fill);
        Context.SetStrokeColor(fill);
        if (type == 1)
        {
            Context.SetLineWidth(1);
            Context.StrokeEllipseInRect(position.ToRect());
        }
        else if (type == 2)
            Context.FillEllipseInRect(position.ToRect());
        else if (type == 3)
            Context.FillRect(position.ToRect());
        else if (type > 3)
            DrawText(position, color, font, index + ".");
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
                foreach (var path in paths.Values)
                    path.Dispose();
                paths.Clear();
                foreach (var list in gradients.Values)
                    foreach (var entry in list)
                        entry.Gradient.Dispose();
                gradients.Clear();
                foreach (var list in conics.Values)
                    foreach (var entry in list)
                        entry.Image.Dispose();
                conics.Clear();
            }
        }
    }
}
