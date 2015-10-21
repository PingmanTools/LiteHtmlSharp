using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.IO;
using System.Windows.Media.Imaging;

namespace LiteHtmlSharp
{
   public interface IResourceLoader
   {
      byte[] GetResourceBytes(string resource);
      string GetResourceString(string resource);
   }

   public struct IntPoint
   {
      public int Width;
      public int Height;

      public IntPoint(int width, int height)
      {
         Width = width;
         Height = height;
      }
   }

   public class WPFContainer : Container
   {
      IResourceLoader _loader;
      DrawingContext _dc;
      HTMLVisual _visualControl;
      DrawingVisual _visual;
      Dictionary<string, Brush> _brushes = new Dictionary<string, Brush>();
      Dictionary<string, BitmapImage> _images = new Dictionary<string, BitmapImage>();
      Dictionary<UIntPtr, FontInfo> _fonts = new Dictionary<UIntPtr, FontInfo>();
      List<Input> _inputs = new List<Input>();
      public bool Loaded = false;
      private bool _rendering = false;
      public static string BaseURL;
      static uint _nextFontID;
      public IntPoint _size;

      public WPFContainer(HTMLVisual visual, string css, IResourceLoader loader) : base(css)
      {
         _loader = loader;
         _visualControl = visual;
         _visual = _visualControl.GetDrawingVisual();
      }

      protected override void SetCaption(string caption)
      {

      }

      protected override string GetDefaultFontName()
      {
         return "Arial";
      }

      protected override int GetDefaultFontSize()
      {
         return 12;
      }

      public void Render(string html)
      {
         Clear();
         if (_rendering) return;

         _images.Clear();
         _inputs.Clear();
         _rendering = true;

         Document.CreateFromString(html);
         Document.Render(_size.Width);

         int newHeight = Document.Height();
         if (newHeight != _size.Height && newHeight > 0)
         {
            _visualControl.SetHeight(Document.Height());
         }

         Draw();

         _rendering = false;
         Loaded = true;
      }

      private void ProcessInputs()
      {
         foreach (var input in _inputs)
         {
            if (input.TextBox == null)
            {
               ElementInfo info = Document.GetElementInfo(input.ID);
               input.TextBox = new TextBox();
               input.TextBox.HorizontalAlignment = HorizontalAlignment.Left;
               input.TextBox.VerticalAlignment = VerticalAlignment.Top;
               input.TextBox.Width = info.Width;
               if (info.Height > 0)
               {
                  input.TextBox.Height = info.Height;
               }
               input.TextBox.Margin = new Thickness(info.PosX, info.PosY, 0, 0);
            }

            if (!input.IsPlaced)
            {
               _visualControl.AddChildControl(input.TextBox);
               input.IsPlaced = true;
            }
         }
      }

      public void Clear()
      {
         foreach (var input in _inputs)
         {
            if (input.IsPlaced)
            {
               _visualControl.RemoveChildControl(input.TextBox);
               input.IsPlaced = false;
            }
         }

         _dc = _visual.RenderOpen();
         _dc.Close();
         _dc = null;
      }

      public void Draw()
      {
         _dc = _visual.RenderOpen();
         var clip = new position()
         {
            width = _size.Width,
            height = _size.Height
         };
         Document.Draw(0, 0, clip);
         _dc.Close();
         _dc = null;
         ProcessInputs();
      }

      public void OnMouseMove(double x, double y)
      {
         if (Loaded)
         {
            if (Document.OnMouseMove((int)x, (int)y))
            {
               Draw();
            }
         }
      }

      public void OnMouseLeave()
      {
         if (Loaded)
         {
            if (Document.OnMouseLeave())
            {
               Draw();
            }
         }
      }

      public void OnLeftButtonDown(double x, double y)
      {
         if (Loaded)
         {
            if (Document.OnLeftButtonDown((int)x, (int)y))
            {
               Draw();
            }
         }
      }

      public void OnLeftButtonUp(double x, double y)
      {
         if (Loaded)
         {
            if (Document.OnLeftButtonUp((int)x, (int)y))
            {
               Draw();
            }
         }
      }

      public void OnSizeChanged(double width, double height)
      {
         if (_size.Width != (int)width || _size.Height != (int)height)
         {
            _size = new IntPoint((int)width, (int)height);

            if (Loaded)
            {
               Document.OnMediaChanged();
               Document.Render((int)_size.Width);
               Draw();
            }
         }
      }

      private void TestDrawing()
      {
         var color = new web_color() { alpha = 255, green = 255 };
         DrawRect(0, 0, 50, 50, GetBrush(ref color));
      }

      protected override void DrawBackground(UIntPtr hdc, string image, background_repeat repeat, ref web_color color, ref position pos, ref border_radiuses br, ref position borderBox)
      {
         if (pos.width > 0 && pos.height > 0)
         {
            if (!String.IsNullOrEmpty(image))
            {
               DrawImage(LoadImage(image), new Rect(pos.x, pos.y, pos.width, pos.height));
            }
            else
            {
               Rect rect = new Rect(pos.x, pos.y, pos.width, pos.height);

               var geometry = new PathGeometry();
               PathSegmentCollection path = new PathSegmentCollection();

               path.Add(new LineSegment(new Point(rect.Right - br.top_right_x, rect.Top), false));
               path.Add(new QuadraticBezierSegment(new Point(rect.Right, rect.Top), new Point(rect.Right, rect.Top + br.top_right_y), false));

               path.Add(new LineSegment(new Point(rect.Right, rect.Bottom - br.bottom_right_y), false));
               path.Add(new QuadraticBezierSegment(new Point(rect.Right, rect.Bottom), new Point(rect.Right - br.bottom_right_x, rect.Bottom), false));

               path.Add(new LineSegment(new Point(rect.Left + br.bottom_left_x, rect.Bottom), false));
               path.Add(new QuadraticBezierSegment(new Point(rect.Left, rect.Bottom), new Point(rect.Left, rect.Bottom - br.bottom_left_y), false));

               path.Add(new LineSegment(new Point(rect.Left, rect.Top + br.top_left_y), false));
               path.Add(new QuadraticBezierSegment(new Point(rect.Left, rect.Top), new Point(rect.Left + br.top_left_x, rect.Top), false));

               geometry.Figures.Add(new PathFigure(new Point(rect.Left + br.top_left_x, rect.Top), path, true));

               _dc.DrawGeometry(GetBrush(ref color), null, geometry);
            }
         }
      }

      protected override void DrawBorders(UIntPtr hdc, ref borders borders, ref position draw_pos, bool root)
      {
         if (borders.top.width > 0)
         {
            DrawRect(draw_pos.x, draw_pos.y, draw_pos.width, borders.top.width, GetBrush(ref borders.top.color));
         }

         if (borders.left.width > 0)
         {
            DrawRect(draw_pos.x, draw_pos.y, borders.left.width, draw_pos.width, GetBrush(ref borders.left.color));
         }

         if (borders.right.width > 0)
         {
            DrawRect(draw_pos.x + draw_pos.width - borders.right.width, draw_pos.y, borders.right.width, draw_pos.height, GetBrush(ref borders.right.color));
         }

         if (borders.bottom.width > 0)
         {
            DrawRect(draw_pos.x, draw_pos.y + draw_pos.height - borders.bottom.width, draw_pos.width, borders.bottom.width, GetBrush(ref borders.bottom.color));
         }
      }

      private void DrawRect(int x, int y, int width, int height, Brush brush)
      {
         Rect rect = new Rect(x, y, width, height);
         _dc.DrawRectangle(brush, null, rect);
      }

      private Brush GetBrush(ref web_color color)
      {
         string key = color.red.ToString() + color.green.ToString() + color.blue.ToString();

         Brush result = null;
         if (!_brushes.TryGetValue(key, out result))
         {
            result = new SolidColorBrush(Color.FromArgb(color.alpha, color.red, color.green, color.blue));
            _brushes.Add(key, result);
         }

         return result;
      }

      protected override void GetImageSize(string image, ref size size)
      {
         var bmp = LoadImage(image);
         if (bmp != null)
         {
            size.width = bmp.PixelWidth;
            size.height = bmp.PixelHeight;
         }
      }

      private FontInfo GetFont(UIntPtr fontID)
      {
         return _fonts[fontID];
      }

      private void DrawImage(BitmapImage image, Rect rect)
      {
         _dc.DrawImage(image, rect);
      }

      private BitmapImage LoadImage(string image)
      {
         BitmapImage result;

         if (_images.TryGetValue(image, out result))
         {
            return result;
         }

         var bytes = _loader.GetResourceBytes(image);
         if (bytes != null && bytes.Length > 0)
         {
            result = new BitmapImage();

            using (var stream = new MemoryStream(bytes))
            {
               result.BeginInit();
               result.CacheOption = BitmapCacheOption.OnLoad;
               result.StreamSource = stream;
               result.EndInit();

               _images.Add(image, result);
            }
         }

         return result;
      }

      protected override int GetTextWidth(string text, UIntPtr font)
      {
         var fontInfo = GetFont(font);
         var formattedText = fontInfo.GetFormattedText(text);
         formattedText.SetTextDecorations(fontInfo.Decorations);
         return (int)formattedText.WidthIncludingTrailingWhitespace;
      }

      protected override void DrawText(string text, UIntPtr font, ref web_color color, ref position pos)
      {
         text = text.Replace(' ', (char)160);
         var fontInfo = GetFont(font);
         var formattedText = fontInfo.GetFormattedText(text);
         formattedText.SetForegroundBrush(GetBrush(ref color));
         _dc.DrawText(formattedText, new Point(pos.x, pos.y));
      }

      protected override UIntPtr CreateFont(string faceName, int size, int weight, font_style italic, font_decoration decoration, ref font_metrics fm)
      {
         var fontweight = FontWeight.FromOpenTypeWeight(weight);
         FontInfo font = new FontInfo(faceName, italic == font_style.fontStyleItalic ? FontStyles.Italic : FontStyles.Normal, fontweight, size);
         if ((decoration & font_decoration.font_decoration_underline) != 0)
         {
            font.Decorations.Add(TextDecorations.Underline);
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

      protected override void GetClientRect(ref position client)
      {
         client.width = _size.Width;
         client.height = _size.Height;
      }

      protected override void GetMediaFeatures(ref media_features media)
      {
         media.width = _size.Width;
         media.height = _size.Height;
         media.device_width = media.width;
         media.device_height = media.height;
         media.resolution = 96;
         media.color = 24;
         media.type = media_type.media_type_all;
      }

      protected override void SetBaseURL(ref string base_url)
      {
         base_url = BaseURL;
      }

      protected override void OnAnchorClick(string url)
      {
         _visualControl.FireLink(url);
      }

      protected override int PTtoPX(int pt)
      {
         return pt;
      }

      protected override int CreateElement(string tag)
      {
         if (string.Equals(tag, "input", StringComparison.OrdinalIgnoreCase))
         {
            Input input = new Input();
            input.ID = _inputs.Count + 1;
            _inputs.Add(input);
            return input.ID;
         }
         else
         {
            return 0;
         }
      }

      protected override void SetCursor(string cursor)
      {
         _visualControl.SetCursor(cursor);
      }

      protected override void DrawListMarker(string image, string baseURL, list_style_type marker_type, ref web_color color, ref position pos)
      {
         DrawRect(pos.x, pos.y, pos.width, pos.height, GetBrush(ref color));
      }

      protected override string TransformText(string text, text_transform tt)
      {
         return text;
      }
   }

   class Input
   {
      public int ID;
      public TextBox TextBox;
      public bool IsPlaced;
   }
}
