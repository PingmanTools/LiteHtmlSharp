using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace LiteHtmlSharp.Avalonia
{
    public interface IResourceLoader
    {
        byte[] GetResourceBytes(string resource);
        string GetResourceString(string resource);
    }

    public delegate FontFamily FontAbsolutePathDelegate(string fontName);

    public class AvaloniaContainer : ViewportContainer
    {
        IResourceLoader _loader;

        public FontAbsolutePathDelegate FontAbsolutePathDelegate;

        private class ResourceLoader : IResourceLoader
        {
            private readonly Func<string, string> _getStringResource;
            private readonly Func<string, byte[]> _getBytesResource;

            public ResourceLoader(Func<string, string> getStringResource, Func<string, byte[]> getBytesResource)
            {
                _getStringResource = getStringResource;
                _getBytesResource = getBytesResource;
            }

            public byte[] GetResourceBytes(string resource) => _getBytesResource(resource);
            public string GetResourceString(string resource) => _getStringResource(resource);
        }

        private static readonly Dictionary<string, Bitmap> Images = new();
        private static readonly Dictionary<UIntPtr, FontInfo> Fonts = new();

        public bool Loaded = false;
        public static string BaseUrl;
        private static uint nextFontId;

        public string DefaultFontName { get; set; } = "Arial";
        public int DefaultFontSize { get; set; } = 12;

        public event Action<string> RenderHtmlRequested;
        public Action<string> SetCursorCallback;

        public DrawingContext DrawingContext;

        public AvaloniaContainer(string css, IResourceLoader loader) : base(css, LibInterop.Instance)
        {
            _loader = loader;
        }

        public AvaloniaContainer(string css, Func<string, string> getStringResource,
            Func<string, byte[]> getBytesResource) : this(css, new ResourceLoader(getStringResource, getBytesResource))
        {
        }

        protected override void SetCaption(string caption)
        {
        }

        protected override string GetDefaultFontName()
        {
            return DefaultFontName;
        }

        protected override int GetDefaultFontSize()
        {
            return DefaultFontSize;
        }

        public override void Render(string html)
        {
            RenderHtmlRequested?.Invoke(html);
        }

        private static void ClampCornerRadii(ref border_radiuses br, double width, double height)
        {
            // Sum pairs; if they exceed side length scale them down proportionally (CSS spec)
            double topSum = br.top_left_x + br.top_right_x;
            if (topSum > width && topSum > 0)
            {
                var scale = width / topSum;
                br.top_left_x = (int)(br.top_left_x * scale);
                br.top_right_x = (int)(br.top_right_x * scale);
            }

            double bottomSum = br.bottom_left_x + br.bottom_right_x;
            if (bottomSum > width && bottomSum > 0)
            {
                var scale = width / bottomSum;
                br.bottom_left_x = (int)(br.bottom_left_x * scale);
                br.bottom_right_x = (int)(br.bottom_right_x * scale);
            }

            double leftSum = br.top_left_y + br.bottom_left_y;
            if (leftSum > height && leftSum > 0)
            {
                var scale = height / leftSum;
                br.top_left_y = (int)(br.top_left_y * scale);
                br.bottom_left_y = (int)(br.bottom_left_y * scale);
            }

            double rightSum = br.top_right_y + br.bottom_right_y;
            if (rightSum > height && rightSum > 0)
            {
                var scale = height / rightSum;
                br.top_right_y = (int)(br.top_right_y * scale);
                br.bottom_right_y = (int)(br.bottom_right_y * scale);
            }
        }

        private static Geometry BuildRoundedRectGeometry(Rect rect, border_radiuses br)
        {
            var geo = new StreamGeometry();
            using var ctx = geo.Open();
            var tlr = br is { top_left_x: > 0, top_left_y: > 0 };
            var trr = br is { top_right_x: > 0, top_right_y: > 0 };
            var brr = br is { bottom_right_x: > 0, bottom_right_y: > 0 };
            var blr = br is { bottom_left_x: > 0, bottom_left_y: > 0 };

            // Start after top-left corner (or at raw corner if no radius)
            ctx.BeginFigure(new Point(rect.Left + (tlr ? br.top_left_x : 0), rect.Top), true);

            // Top edge + top-right corner
            ctx.LineTo(new Point(rect.Right - (trr ? br.top_right_x : 0), rect.Top));
            if (trr)
            {
                ctx.ArcTo(new Point(rect.Right, rect.Top + br.top_right_y),
                    new Size(br.top_right_x, br.top_right_y),
                    0, false, SweepDirection.Clockwise);
            }

            // Right edge + bottom-right corner
            ctx.LineTo(new Point(rect.Right, rect.Bottom - (brr ? br.bottom_right_y : 0)));
            if (brr)
            {
                ctx.ArcTo(new Point(rect.Right - br.bottom_right_x, rect.Bottom),
                    new Size(br.bottom_right_x, br.bottom_right_y),
                    0, false, SweepDirection.Clockwise);
            }

            // Bottom edge + bottom-left corner
            ctx.LineTo(new Point(rect.Left + (blr ? br.bottom_left_x : 0), rect.Bottom));
            if (blr)
            {
                ctx.ArcTo(new Point(rect.Left, rect.Bottom - br.bottom_left_y),
                    new Size(br.bottom_left_x, br.bottom_left_y),
                    0, false, SweepDirection.Clockwise);
            }

            // Left edge + top-left corner
            ctx.LineTo(new Point(rect.Left, rect.Top + (tlr ? br.top_left_y : 0)));
            if (tlr)
            {
                ctx.ArcTo(new Point(rect.Left + br.top_left_x, rect.Top),
                    new Size(br.top_left_x, br.top_left_y),
                    0, false, SweepDirection.Clockwise);
            }

            ctx.EndFigure(true);

            return geo;
        }

        protected override void DrawBackground(UIntPtr hdc, string image, background_repeat repeat,
            ref web_color color, ref position pos, ref border_radiuses br, ref position borderBox, bool isRoot)
        {
            if (pos.width <= 0 || pos.height <= 0) return;

            if (!string.IsNullOrEmpty(image))
            {
                var bitmap = LoadImage(image);
                if (bitmap != null)
                    DrawImage(bitmap, new Rect(pos.x, pos.y, pos.width, pos.height));
                return;
            }

            var rect = new Rect(pos.x, pos.y, pos.width, pos.height);
            var hasRadii =
                (br.top_left_x | br.top_left_y | br.top_right_x | br.top_right_y |
                 br.bottom_right_x | br.bottom_right_y | br.bottom_left_x | br.bottom_left_y) != 0;

            if (!hasRadii)
            {
                DrawingContext.DrawRectangle(color.GetBrush(), null, rect);
                return;
            }

            ClampCornerRadii(ref br, rect.Width, rect.Height);
            var geometry = BuildRoundedRectGeometry(rect, br);
            DrawingContext.DrawGeometry(color.GetBrush(), null, geometry);
        }

        protected override void DrawBorders(UIntPtr hdc, ref borders borders, ref position draw_pos, bool root)
        {
            if (draw_pos.width < 0) draw_pos.width = 0;
            if (draw_pos.height < 0) draw_pos.height = 0;

            var rect = new Rect(draw_pos.x, draw_pos.y, draw_pos.width, draw_pos.height);
            var br = borders.radius;

            var hasRadii =
                (br.top_left_x | br.top_left_y | br.top_right_x | br.top_right_y |
                 br.bottom_right_x | br.bottom_right_y | br.bottom_left_x | br.bottom_left_y) != 0;

            var uniform =
                borders.top.width > 0 &&
                borders.top.width == borders.right.width &&
                borders.top.width == borders.bottom.width &&
                borders.top.width == borders.left.width &&
                borders.top.color.Equals(borders.right.color) &&
                borders.top.color.Equals(borders.bottom.color) &&
                borders.top.color.Equals(borders.left.color);

            if (uniform)
            {
                var pen = borders.top.color.GetPen(borders.top.width);
                if (!hasRadii)
                {
                    DrawingContext.DrawRectangle(null, pen, rect);
                }
                else
                {
                    ClampCornerRadii(ref br, rect.Width, rect.Height);
                    var geometry = BuildRoundedRectGeometry(rect, br);
                    DrawingContext.DrawGeometry(null, pen, geometry);
                }

                return;
            }

            if (!hasRadii)
            {
                if (borders.top.width > 0)
                    DrawingContext.DrawLine(borders.top.color.GetPen(borders.top.width), rect.TopLeft, rect.TopRight);
                if (borders.right.width > 0)
                    DrawingContext.DrawLine(borders.right.color.GetPen(borders.right.width), rect.TopRight,
                        rect.BottomRight);
                if (borders.bottom.width > 0)
                    DrawingContext.DrawLine(borders.bottom.color.GetPen(borders.bottom.width), rect.BottomRight,
                        rect.BottomLeft);
                if (borders.left.width > 0)
                    DrawingContext.DrawLine(borders.left.color.GetPen(borders.left.width), rect.BottomLeft,
                        rect.TopLeft);
                return;
            }

            // Per-side with trimmed ends to respect radii
            ClampCornerRadii(ref br, rect.Width, rect.Height);

            double tlx = br.top_left_x;
            double tly = br.top_left_y;
            double trx = br.top_right_x;
            double try_ = br.top_right_y;
            double brx = br.bottom_right_x;
            double bry = br.bottom_right_y;
            double blx = br.bottom_left_x;
            double bly = br.bottom_left_y;

            // Top
            if (borders.top.width > 0)
            {
                var pen = borders.top.color.GetPen(borders.top.width);
                var y = rect.Top;
                DrawingContext.DrawLine(pen,
                    new Point(rect.Left + tlx, y),
                    new Point(rect.Right - trx, y));
            }

            // Right
            if (borders.right.width > 0)
            {
                var pen = borders.right.color.GetPen(borders.right.width);
                var x = rect.Right;
                DrawingContext.DrawLine(pen,
                    new Point(x, rect.Top + try_),
                    new Point(x, rect.Bottom - bry));
            }

            // Bottom
            if (borders.bottom.width > 0)
            {
                var pen = borders.bottom.color.GetPen(borders.bottom.width);
                var y = rect.Bottom;
                DrawingContext.DrawLine(pen,
                    new Point(rect.Right - brx, y),
                    new Point(rect.Left + blx, y));
            }

            // Left
            if (borders.left.width > 0)
            {
                var pen = borders.left.color.GetPen(borders.left.width);
                var x = rect.Left;
                DrawingContext.DrawLine(pen,
                    new Point(x, rect.Bottom - bly),
                    new Point(x, rect.Top + tly));
            }
        }

        private void DrawRect(double x, double y, double width, double height, IBrush brush)
        {
            var rect = new Rect(x, y, width, height);
            DrawingContext.DrawRectangle(brush, null, rect);
        }

        protected override void GetImageSize(string image, ref size size)
        {
            var bmp = LoadImage(image);
            if (bmp == null) return;
            size.width = bmp.PixelSize.Width;
            size.height = bmp.PixelSize.Height;
        }

        private static FontInfo GetFont(UIntPtr fontID)
        {
            return Fonts[fontID];
        }

        private void DrawImage(Bitmap image, Rect rect)
        {
            DrawingContext.DrawImage(image, rect);
        }

        private Bitmap LoadImage(string image)
        {
            try
            {
                if (Images.TryGetValue(image, out var result))
                {
                    return result;
                }

                var bytes = _loader.GetResourceBytes(image);
                if (bytes == null || bytes.Length <= 0) return result;
                using var stream = new MemoryStream(bytes);
                result = new Bitmap(stream);
                Images.Add(image, result);

                return result;
            }
            catch
            {
                return null;
            }
        }

        protected override int GetTextWidth(string text, UIntPtr font)
        {
            var fontInfo = GetFont(font);
            var formattedText = fontInfo.GetFormattedText(text);
            return (int)Math.Round(formattedText.WidthIncludingTrailingWhitespace + 0.25f);
        }

        protected override void DrawText(string text, UIntPtr font, ref web_color color, ref position pos)
        {
            text = text.Replace(' ', (char)160);
            var fontInfo = GetFont(font);
            var formattedText = fontInfo.GetFormattedText(text);
            formattedText.SetForegroundBrush(color.GetBrush());
            DrawingContext.DrawText(formattedText, new Point(pos.x, pos.y));
        }

        protected override UIntPtr CreateFont(string faceName, int size, int weight, font_style italic,
            font_decoration decoration, ref font_metrics fm)
        {
            var fontweight = weight >= 700 ? FontWeight.Bold : FontWeight.Normal;
            var font = new FontInfo(faceName,
                italic == font_style.fontStyleItalic ? FontStyle.Italic : FontStyle.Normal, fontweight, size,
                FontAbsolutePathDelegate?.Invoke(faceName));

            if ((decoration & font_decoration.font_decoration_underline) != 0)
            {
                font.Decorations = TextDecorations.Underline;
            }

            var fontID = new UIntPtr(nextFontId++);
            Fonts.Add(fontID, font);

            fm.x_height = font.xHeight;
            fm.ascent = font.Ascent;
            fm.descent = font.Descent;
            fm.height = font.LineHeight;
            fm.draw_spaces = decoration > 0;

            return fontID;
        }

        protected override string ImportCss(string url, string baseurl)
        {
            return _loader.GetResourceString(url);
        }

        protected override void GetMediaFeatures(ref media_features media)
        {
            base.GetMediaFeatures(ref media);
            media.device_width = media.width;
            media.device_height = media.height;
            media.resolution = 96;
            media.color = 24;
            media.type = media_type.media_type_all;
        }

        protected override void SetBaseURL(string baseUrl)
        {
            // Base URL stored for relative href resolution.
            BaseUrl = baseUrl;
        }

        protected override int PTtoPX(int pt)
        {
            return pt;
        }

        protected override void SetCursor(string cursor)
        {
            SetCursorCallback?.Invoke(cursor);
        }

        protected override void DrawListMarker(string image, string baseURL, list_style_type marker_type,
            ref web_color color, ref position pos)
        {
            DrawRect(pos.x, pos.y, pos.width, pos.height, color.GetBrush());
        }

        protected override string TransformText(string text, text_transform tt)
        {
            return text;
        }
    }

    public enum InputType
    {
        Unknown,
        Button,
        Textbox
    }

    public class AvaloniaInputs : List<AvaloniaInput>
    {
        public AvaloniaInput GetInputByTagID(string id)
        {
            return this.FirstOrDefault(i => i.TagID == id);
        }
    }

    public class AvaloniaInput
    {
        public int ID;
        public Control Element;
        public bool IsPlaced;
        public InputType Type;
        public string Onclick;
        public string Href;
        public string TagID;

        public bool AttributesSetup { get; set; }

        public AvaloniaInput(InputType type)
        {
            Type = type;
        }

        public TextBox TextBox => Element as TextBox;

        public Button Button => Element as Button;

        public void SetupAttributes(string attrString)
        {
            if (string.IsNullOrEmpty(attrString))
            {
                return;
            }

            var lines = attrString.Split('\n');
            foreach (var line in lines)
            {
                var keyVal = line.Split('=');
                if (keyVal.Length > 1)
                {
                    var key = keyVal[0].ToLower();
                    var value = keyVal[1];

                    switch (key)
                    {
                        case "value":
                            switch (Element)
                            {
                                case Button button:
                                    button.Content = value;
                                    break;
                                case TextBox textBox:
                                    textBox.Text = value;
                                    break;
                            }

                            break;
                        case "href":
                            Href = value;
                            break;
                        case "id":
                            TagID = value;
                            break;
                        case "onclick":
                            Onclick = value;
                            break;
                    }
                }
            }
        }
    }
}

