using LiteHtmlSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.IO;
using System.Windows.Media.Imaging;
using System.Globalization;

namespace Browser
{
   public class WPFContainer : Container
   {
      DrawingContext _dc;
      HTMLVisual _visualControl;
      DrawingVisual _visual;
      Dictionary<string, Brush> _brushes = new Dictionary<string, Brush>();
      Dictionary<string, BitmapImage> _images = new Dictionary<string, BitmapImage>();
      Dictionary<UIntPtr, FontInfo> _fonts = new Dictionary<UIntPtr, FontInfo>();
      List<int> _inputs = new List<int>();
      public bool Loaded = false;
      private bool _rendering = false;
      public static string BaseURL;
      static uint _nextFontID;
      public Point _size;

      public WPFContainer(HTMLVisual visual, string css) : base(css)
      {
         _visualControl = visual;
         _visual = _visualControl.GetDrawingVisual();
      }

      public void Render(string html)
      {
         if (_rendering) return;

         _inputs.Clear();
         _rendering = true;

         PInvokes.RenderHTML(CPPContainer, html, (int)_size.X);
         Draw();
         ProcessInputs();

         _rendering = false;
         Loaded = true;
      }

      private void ProcessInputs()
      {
         foreach(int i in _inputs)
         {
            ElementInfo info = PInvokes.GetElementInfo(CPPContainer, i);
            TextBox tb = new TextBox();
            tb.HorizontalAlignment = HorizontalAlignment.Left;
            tb.VerticalAlignment = VerticalAlignment.Top;
            tb.Width = info.Width;
            if(info.Height > 0)
            {
               tb.Height = info.Height;
            }
            tb.Margin = new Thickness(info.PosX, info.PosY, 0, 0);
            _visualControl.AddChildControl(tb);
         }
      }

      public void Clear()
      {
         _dc = _visual.RenderOpen();
         _dc.Close();
         _dc = null;
      }

      public void Draw()
      {
         _dc = _visual.RenderOpen();
         var clip = new position()
         {
            width = (int)_size.X,
            height = (int)_size.Y
         };
         PInvokes.Draw(CPPContainer, 0, 0, clip);
         _dc.Close();
         _dc = null;
      }

      public void OnMouseMove(double x, double y)
      {
         if (Loaded)
         {
            if(PInvokes.OnMouseMove(CPPContainer, (int)x, (int)y))
            {
               Draw();
            }
         }
      }

      public void OnMouseLeave()
      {
         if (Loaded)
         {
            if (PInvokes.OnMouseLeave(CPPContainer))
            {
               Draw();
            }
         }
      }

      public void OnLeftButtonDown(double x, double y)
      {
         if (Loaded)
         {
            if (PInvokes.OnLeftButtonDown(CPPContainer, (int)x, (int)y))
            {
               Draw();
            }
         }
      }

      public void OnLeftButtonUp(double x, double y)
      {
         if (Loaded)
         {
            if (PInvokes.OnLeftButtonUp(CPPContainer, (int)x, (int)y))
            {
               Draw();
            }
         }
      }

      public void OnSizeChanged(double x, double y)
      {
         _size = new Point(x, y);

         if (Loaded)
         {
            PInvokes.OnMediaChanged(CPPContainer);
            PInvokes.Render(CPPContainer, (int)_size.X);
            Draw();
         }
      }

      private void TestDrawing()
      {
         var color = new web_color() { alpha = 255, green = 255 };
         DrawRect(0, 0, 50, 50, GetBrush(ref color));
      }

      protected override void DrawBackground(UIntPtr hdc, string image, background_repeat repeat, ref web_color color, ref position pos)
      {
         if (pos.width > 0 && pos.height > 0)
         {
            if (!String.IsNullOrEmpty(image))
            {
               DrawImage(LoadImage(image), new Rect(pos.x, pos.y, pos.width, pos.height));
            }
            else
            {
               DrawRect(pos.x, pos.y, pos.width, pos.height, GetBrush(ref color));
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
            DrawRect(draw_pos.x, draw_pos.y, borders.right.width, draw_pos.width, GetBrush(ref borders.right.color));
         }

         if (borders.bottom.width > 0)
         {
            DrawRect(draw_pos.x, draw_pos.y, draw_pos.width, borders.bottom.width, GetBrush(ref borders.bottom.color));
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
         size.width = bmp.PixelWidth;
         size.height = bmp.PixelHeight;
      }

      private FontInfo GetFont(UIntPtr fontID)
      {
         return _fonts[fontID];
      }

      FormattedText GetFormattedText(string text, FontInfo font, TextAlignment alignment = TextAlignment.Left)
      {
         var formattedText = new FormattedText(text, CultureInfo.InvariantCulture, System.Windows.FlowDirection.LeftToRight, font.TypeFace, font.Size, null);
         return formattedText;
      }

      private void DrawImage(BitmapImage image, Rect rect)
      {
         _dc.DrawImage(image, rect);
      }

      private BitmapImage LoadImage(string image)
      {
         BitmapImage result = null;
         if (!_images.TryGetValue(image, out result))
         {
            Uri uri = GetAbsoluteFile(image);

            result = new BitmapImage(uri);
            _images.Add(image, result);
         }

         return result;
      }

      private Uri GetAbsoluteFile(string file)
      {
         Uri uri;
         if (!Uri.TryCreate(file, UriKind.Absolute, out uri))
         {
            var fullpath = Path.Combine(BaseURL, file);
            uri = new Uri(fullpath);
         }

         return uri;
      }

      protected override int GetTextWidth(string text, UIntPtr font)
      {
         var fontInfo = GetFont(font);
         var formattedText = GetFormattedText(text, fontInfo);
         formattedText.SetTextDecorations(fontInfo.Decorations);
         return (int)formattedText.WidthIncludingTrailingWhitespace;
      }

      protected override void DrawText(string text, UIntPtr font, ref web_color color, ref position pos)
      {
         text = text.Replace(' ', (char)160);
         var fontInfo = GetFont(font);
         var formattedText = new FormattedText(text, CultureInfo.InvariantCulture, System.Windows.FlowDirection.LeftToRight, fontInfo.TypeFace, fontInfo.Size, null);
         formattedText.SetTextDecorations(fontInfo.Decorations);
         formattedText.SetForegroundBrush(GetBrush(ref color));
         _dc.DrawText(formattedText, new Point(pos.x, pos.y));
      }

      protected override UIntPtr CreateFont(string faceName, int size, int weight, font_style italic, font_decoration decoration, ref font_metrics fm)
      {
         FontInfo font = new FontInfo(faceName, italic == font_style.fontStyleItalic ? FontStyles.Italic : FontStyles.Normal, FontWeight.FromOpenTypeWeight(weight), size);
         if((decoration & font_decoration.font_decoration_underline) != 0)
         {
            font.Decorations.Add(TextDecorations.Underline);
         }

         UIntPtr fontID = new UIntPtr(_nextFontID++);
         _fonts.Add(fontID, font);

         fm.x_height = 7;
         fm.ascent = 10;
         fm.descent = 3;
         fm.height = fm.ascent + fm.descent + 2;
         fm.draw_spaces = decoration > 0;

         return fontID;
      }

      protected override string ImportCss(string url, string baseurl)
      {
         var file = GetAbsoluteFile(url);
         if(File.Exists(file.OriginalString))
         {
            return File.ReadAllText(file.OriginalString);
         }

         return string.Empty;
      }

      protected override void GetClientRect(ref position client)
      {
         client.width = (int)_size.X;
         client.height = (int)_size.Y;
      }

      protected override void GetMediaFeatures(ref media_features media)
      {
         media.width = (int)_size.X;
         media.height = (int)_size.Y;
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

      protected override void OnAnchorClick(ref string url)
      {
         throw new NotImplementedException();
      }

      protected override int PTtoPX(int pt)
      {
         return pt;
      }

      protected override int CreateElement(string tag, string attributes)
      {
         if (string.Equals(tag, "input", StringComparison.OrdinalIgnoreCase))
         {
            int elementID = _inputs.Count + 1;
            _inputs.Add(elementID);
            return elementID;
         }
         else
         {
            return 0;
         }
      }
   }
}
