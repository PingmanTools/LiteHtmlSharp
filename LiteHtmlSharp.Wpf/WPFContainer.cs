using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.IO;
using System.Windows.Media.Imaging;
using System.Linq;

namespace LiteHtmlSharp.Wpf
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

   public class WpfContainer : ViewportContainer
   {
      IResourceLoader _loader;

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

      static Dictionary<string, BitmapImage> _images = new Dictionary<string, BitmapImage>();
      static Dictionary<UIntPtr, FontInfo> _fonts = new Dictionary<UIntPtr, FontInfo>();

      public bool Loaded = false;
      public static string BaseURL;
      static uint _nextFontID;

      public string DefaultFontName { get; set; } = "Arial";
      public int DefaultFontSize { get; set; } = 12;

      public event Action<string> RenderHtmlRequested;
      public Action<string> SetCursorCallback;

      public DrawingContext DrawingContext;

      public WpfContainer(string css, IResourceLoader loader) : base(css, LibInterop.Instance)
      {
         _loader = loader;
      }

      public WpfContainer(string css, Func<string, string> getStringResource, Func<string, byte[]> getBytesResource) : this(css, new ResourceLoader(getStringResource, getBytesResource))
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

               DrawingContext.DrawGeometry(color.GetBrush(), null, geometry);
            }
         }
      }

      private void DrawCurvedPath(Point p1, Point p2, Point p3, Point p4, ref web_color color, double thickness)
      {
         var geometry = new PathGeometry();
         PathSegmentCollection path = new PathSegmentCollection();

         path.Add(new LineSegment(p2, true));
         path.Add(new QuadraticBezierSegment(p3, p4, true));
         geometry.Figures.Add(new PathFigure(p1, path, false));

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
            //            DrawRect(rect.Left + br.top_left_x, rect.Top, rect.Width - br.top_left_x - br.top_right, GetBrush(ref borders.top.color));
         }

         if (borders.right.width > 0)
         {
            Point p1 = new Point(rect.Right, rect.Top + br.top_right_y);
            Point p2 = new Point(rect.Right, rect.Bottom - br.bottom_right_y);
            Point p3 = new Point(rect.Right, rect.Bottom);
            Point p4 = new Point(rect.Right - br.bottom_right_x, rect.Bottom);
            DrawCurvedPath(p1, p2, p3, p4, ref borders.right.color, borders.right.width);
            //            DrawRect(p4.x,  draw_pos.x + draw_pos.width - borders.right.width, draw_pos.y, borders.right.width, draw_pos.height, GetBrush(ref borders.right.color));
         }

         if (borders.bottom.width > 0)
         {
            Point p1 = new Point(rect.Right - br.bottom_right_x, rect.Bottom);
            Point p2 = new Point(rect.Left + br.bottom_left_x, rect.Bottom);
            Point p3 = new Point(rect.Left, rect.Bottom);
            Point p4 = new Point(rect.Left, rect.Bottom - br.bottom_left_y);
            DrawCurvedPath(p1, p2, p3, p4, ref borders.bottom.color, borders.bottom.width);
            //           DrawRect(draw_pos.x, draw_pos.y + draw_pos.height - borders.bottom.width, draw_pos.width, borders.bottom.width, GetBrush(ref borders.bottom.color));
         }

         if (borders.left.width > 0)
         {
            Point p1 = new Point(rect.Left, rect.Bottom - br.bottom_left_y);
            Point p2 = new Point(rect.Left, rect.Top + br.top_left_y);
            Point p3 = new Point(rect.Left, rect.Top);
            Point p4 = new Point(rect.Left + br.top_left_x, rect.Top);
            DrawCurvedPath(p1, p2, p3, p4, ref borders.left.color, borders.left.width);
            //            DrawRect(draw_pos.x, draw_pos.y, borders.left.width, draw_pos.height, GetBrush(ref borders.left.color));
         }
      }

      private void DrawRect(double x, double y, double width, double height, Brush brush)
      {
         Rect rect = new Rect(x, y, width, height);
         DrawingContext.DrawRectangle(brush, null, rect);
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
         DrawingContext.DrawImage(image, rect);
      }

      private BitmapImage LoadImage(string image)
      {
         try
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
         catch
         {
            return null;
         }
      }

      protected override int GetTextWidth(string text, UIntPtr font)
      {
         var fontInfo = GetFont(font);
         var formattedText = fontInfo.GetFormattedText(text);
         formattedText.SetTextDecorations(fontInfo.Decorations);
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

   public class Inputs : List<Input>
   {
      public Input GetInputByTagID(string id)
      {
         return this.FirstOrDefault(i => i.TagID == id);
      }
   }

   public class Input
   {
      public int ID;
      public FrameworkElement Element;
      public bool IsPlaced;
      public InputType Type;
      public string Onclick;
      public string Href;
      public string TagID;

      public bool AttributesSetup { get; set; }

      public Input(InputType type)
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
                     if (Element is Button)
                     {
                        ((Button)Element).Content = value;
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
