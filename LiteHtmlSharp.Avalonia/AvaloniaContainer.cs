using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace LiteHtmlSharp.Avalonia
{
    public interface IResourceLoader
    {
        byte[] GetResourceBytes(string resource);
        string GetResourceString(string resource);
    }

    public struct IntSize
    {
        public int Width;
        public int Height;

        public IntSize(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }

    public struct IntPoint
    {
        public int X;
        public int Y;

        public IntPoint(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    public delegate FontFamily FontAbsolutePathDelegate(string fontName);

    public class AvaloniaContainer : ViewportContainer
    {
        IResourceLoader _loader;

        public FontAbsolutePathDelegate FontAbsolutePathDelegate;

        class ResourceLoader : IResourceLoader
        {
            Func<string, string> _getStringResource;
            Func<string, byte[]> _getBytesResource;

            public ResourceLoader(Func<string, string> getStringResource, Func<string, byte[]> getBytesResource)
            {
                _getStringResource = getStringResource;
                _getBytesResource = getBytesResource;
            }

            public byte[] GetResourceBytes(string resource) => _getBytesResource(resource);
            public string GetResourceString(string resource) => _getStringResource(resource);
        }

        static Dictionary<string, Bitmap> _images = new Dictionary<string, Bitmap>();
        static Dictionary<UIntPtr, FontInfo> _fonts = new Dictionary<UIntPtr, FontInfo>();

        public bool Loaded = false;
        public static string BaseURL;
        static uint _nextFontID;

        public string DefaultFontName { get; set; } = "Arial";
        public int DefaultFontSize { get; set; } = 12;

        public event Action<string> RenderHtmlRequested;
        public Action<string> SetCursorCallback;

        public DrawingContext DrawingContext;

        public AvaloniaContainer(string css, IResourceLoader loader) : base(css, LibInterop.Instance)
        {
            _loader = loader;
        }

        public AvaloniaContainer(string css, Func<string, string> getStringResource, Func<string, byte[]> getBytesResource) : this(css, new ResourceLoader(getStringResource, getBytesResource))
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

        protected override void DrawBackground(UIntPtr hdc, string image, background_repeat repeat, ref web_color color, ref position pos, ref border_radiuses br, ref position borderBox, bool isRoot)
        {
            if (pos.width > 0 && pos.height > 0)
            {
                if (!String.IsNullOrEmpty(image))
                {
                    var bitmap = LoadImage(image);
                    if (bitmap != null)
                    {
                        DrawImage(bitmap, new Rect(pos.x, pos.y, pos.width, pos.height));
                    }
                }
                else
                {
                    Rect rect = new Rect(pos.x, pos.y, pos.width, pos.height);

                    var geometry = new PathGeometry();
                    var figure = new PathFigure();
                    figure.StartPoint = new Point(rect.Left + br.top_left_x, rect.Top);
                    figure.IsClosed = true;

                    // Top edge with top-right radius
                    figure.Segments.Add(new LineSegment { Point = new Point(rect.Right - br.top_right_x, rect.Top) });
                    if (br.top_right_x > 0 || br.top_right_y > 0)
                    {
                        figure.Segments.Add(new QuadraticBezierSegment
                        {
                            Point1 = new Point(rect.Right, rect.Top),
                            Point2 = new Point(rect.Right, rect.Top + br.top_right_y)
                        });
                    }

                    // Right edge with bottom-right radius
                    figure.Segments.Add(new LineSegment { Point = new Point(rect.Right, rect.Bottom - br.bottom_right_y) });
                    if (br.bottom_right_x > 0 || br.bottom_right_y > 0)
                    {
                        figure.Segments.Add(new QuadraticBezierSegment
                        {
                            Point1 = new Point(rect.Right, rect.Bottom),
                            Point2 = new Point(rect.Right - br.bottom_right_x, rect.Bottom)
                        });
                    }

                    // Bottom edge with bottom-left radius
                    figure.Segments.Add(new LineSegment { Point = new Point(rect.Left + br.bottom_left_x, rect.Bottom) });
                    if (br.bottom_left_x > 0 || br.bottom_left_y > 0)
                    {
                        figure.Segments.Add(new QuadraticBezierSegment
                        {
                            Point1 = new Point(rect.Left, rect.Bottom),
                            Point2 = new Point(rect.Left, rect.Bottom - br.bottom_left_y)
                        });
                    }

                    // Left edge with top-left radius
                    figure.Segments.Add(new LineSegment { Point = new Point(rect.Left, rect.Top + br.top_left_y) });
                    if (br.top_left_x > 0 || br.top_left_y > 0)
                    {
                        figure.Segments.Add(new QuadraticBezierSegment
                        {
                            Point1 = new Point(rect.Left, rect.Top),
                            Point2 = new Point(rect.Left + br.top_left_x, rect.Top)
                        });
                    }

                    geometry.Figures.Add(figure);
                    DrawingContext.DrawGeometry(color.GetBrush(), null, geometry);
                }
            }
        }

        private void DrawCurvedPath(Point p1, Point p2, Point p3, Point p4, ref web_color color, double thickness)
        {
            var geometry = new PathGeometry();
            var figure = new PathFigure();
            figure.StartPoint = p1;
            
            figure.Segments.Add(new LineSegment { Point = p2 });
            figure.Segments.Add(new QuadraticBezierSegment { Point1 = p3, Point2 = p4 });
            
            geometry.Figures.Add(figure);
            DrawingContext.DrawGeometry(null, color.GetPen(thickness), geometry);
        }

        protected override void DrawBorders(UIntPtr hdc, ref borders borders, ref position draw_pos, bool root)
        {
            // Skinny controls can push borders off, in which case we can't create a rect with a negative size.
            if (draw_pos.width < 0) draw_pos.width = 0;
            if (draw_pos.height < 0) draw_pos.height = 0;
            Rect rect = new Rect(draw_pos.x, draw_pos.y, draw_pos.width, draw_pos.height);
            var br = borders.radius;

            if (borders.top.width > 0)
            {
                Point p1 = new Point(rect.Left + br.top_left_x, rect.Top);
                Point p2 = new Point(rect.Right - br.top_right_x, rect.Top);
                Point p3 = new Point(rect.Right, rect.Top);
                Point p4 = new Point(rect.Right, rect.Top + br.top_right_y);
                DrawCurvedPath(p1, p2, p3, p4, ref borders.top.color, borders.top.width);
            }

            if (borders.right.width > 0)
            {
                Point p1 = new Point(rect.Right, rect.Top + br.top_right_y);
                Point p2 = new Point(rect.Right, rect.Bottom - br.bottom_right_y);
                Point p3 = new Point(rect.Right, rect.Bottom);
                Point p4 = new Point(rect.Right - br.bottom_right_x, rect.Bottom);
                DrawCurvedPath(p1, p2, p3, p4, ref borders.right.color, borders.right.width);
            }

            if (borders.bottom.width > 0)
            {
                Point p1 = new Point(rect.Right - br.bottom_right_x, rect.Bottom);
                Point p2 = new Point(rect.Left + br.bottom_left_x, rect.Bottom);
                Point p3 = new Point(rect.Left, rect.Bottom);
                Point p4 = new Point(rect.Left, rect.Bottom - br.bottom_left_y);
                DrawCurvedPath(p1, p2, p3, p4, ref borders.bottom.color, borders.bottom.width);
            }

            if (borders.left.width > 0)
            {
                Point p1 = new Point(rect.Left, rect.Bottom - br.bottom_left_y);
                Point p2 = new Point(rect.Left, rect.Top + br.top_left_y);
                Point p3 = new Point(rect.Left, rect.Top);
                Point p4 = new Point(rect.Left + br.top_left_x, rect.Top);
                DrawCurvedPath(p1, p2, p3, p4, ref borders.left.color, borders.left.width);
            }
        }

        private void DrawRect(double x, double y, double width, double height, IBrush brush)
        {
            Rect rect = new Rect(x, y, width, height);
            DrawingContext.DrawRectangle(brush, null, rect);
        }

        protected override void GetImageSize(string image, ref size size)
        {
            var bmp = LoadImage(image);
            if (bmp != null)
            {
                size.width = bmp.PixelSize.Width;
                size.height = bmp.PixelSize.Height;
            }
        }

        private FontInfo GetFont(UIntPtr fontID)
        {
            return _fonts[fontID];
        }

        private void DrawImage(Bitmap image, Rect rect)
        {
            DrawingContext.DrawImage(image, rect);
        }

        private Bitmap LoadImage(string image)
        {
            try
            {
                Bitmap result;

                if (_images.TryGetValue(image, out result))
                {
                    return result;
                }

                var bytes = _loader.GetResourceBytes(image);
                if (bytes != null && bytes.Length > 0)
                {
                    using (var stream = new MemoryStream(bytes))
                    {
                        result = new Bitmap(stream);
                        _images.Add(image, result);
                    }
                }

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

        protected override UIntPtr CreateFont(string faceName, int size, int weight, font_style italic, font_decoration decoration, ref font_metrics fm)
        {
            var fontweight = weight >= 700 ? FontWeight.Bold : FontWeight.Normal;
            FontInfo font = new FontInfo(faceName, italic == font_style.fontStyleItalic ? FontStyle.Italic : FontStyle.Normal, fontweight, size, FontAbsolutePathDelegate?.Invoke(faceName));

            if ((decoration & font_decoration.font_decoration_underline) != 0)
            {
                font.Decorations = TextDecorations.Underline;
            }

            UIntPtr fontID = new UIntPtr(_nextFontID++);
            _fonts.Add(fontID, font);

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

        protected override void SetBaseURL(string base_url)
        {
            base_url = BaseURL;
        }

        protected override int PTtoPX(int pt)
        {
            return pt;
        }

        protected override void SetCursor(string cursor)
        {
            SetCursorCallback?.Invoke(cursor);
        }

        protected override void DrawListMarker(string image, string baseURL, list_style_type marker_type, ref web_color color, ref position pos)
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

        public TextBox TextBox
        {
            get
            {
                return Element as TextBox;
            }
        }

        public Button Button
        {
            get
            {
                return Element as Button;
            }
        }

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
                    string key = keyVal[0].ToLower();
                    string value = keyVal[1];

                    switch (key)
                    {
                        case "value":
                            if (Element is Button button)
                            {
                                button.Content = value;
                            }
                            else if (Element is TextBox textBox)
                            {
                                textBox.Text = value;
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