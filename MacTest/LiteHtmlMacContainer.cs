using System;
using LiteHtmlSharp;
using AppKit;
using CoreGraphics;
using System.IO;
using CoreText;
using System.Collections.Generic;
using Foundation;

namespace MacTest
{
   public class LiteHtmlMacContainer : Container
   {
      public delegate byte[] LoadImageDelegate(string imageUrl);

      public LoadImageDelegate LoadImageCallback;


      class FontHolder
      {
         public CTStringAttributes Attributes { get; set; }

         public CTFont Font { get; set; }
      }

      class ImageHolder
      {
         public CGImage Image{ get; set; }

         public CGSize Size { get; set; }
      }

      LiteHtmlView view;

      Dictionary<UIntPtr, FontHolder> fontCache;
      uint lastFontId = 0;

      Dictionary<string, ImageHolder> imageCache;

      public LiteHtmlMacContainer(LiteHtmlView view, string masterCssData)
         : base(masterCssData)
      {
         this.view = view;
         fontCache = new Dictionary<UIntPtr, FontHolder>();
         imageCache = new Dictionary<string, ImageHolder>();
      }

      protected override int CreateElement(string tag)
      {
         return 0;
      }

      protected override int PTtoPX(int pt)
      {
         return 1;
      }

      protected override void GetClientRect(ref position client)
      {
         client.width = (int)view.Bounds.Width;
         client.height = (int)view.Bounds.Height;
      }

      protected override void GetMediaFeatures(ref media_features media)
      {
         media.device_width = (int)view.Bounds.Width;
         media.device_height = (int)view.Bounds.Height;
      }

      protected override void SetBaseURL(ref string base_url)
      {
         
      }

      protected override void OnAnchorClick(ref string url)
      {
         
      }

      protected override UIntPtr CreateFont(string faceName, int size, int weight, font_style italic, uint decoration, ref font_metrics fm)
      {
         var symbolicTrait = italic == font_style.fontStyleItalic ? CTFontSymbolicTraits.Italic : CTFontSymbolicTraits.None;
         var font = new CTFont(faceName, size).WithSymbolicTraits(size, symbolicTrait, symbolicTrait);

         fm.ascent = (int)Math.Round(font.AscentMetric);
         fm.descent = (int)Math.Round(font.DescentMetric);
         fm.draw_spaces = false;
         fm.height = (int)Math.Round(font.AscentMetric + font.DescentMetric);
         fm.x_height = (int)Math.Round(font.XHeightMetric);

         var strAttrs = new CTStringAttributes { Font = font };
         var fontHolder = new FontHolder
         { 
            Font = font, 
            Attributes = strAttrs
         };

         lastFontId++;
         var fontId = new UIntPtr(lastFontId);
         fontCache.Add(fontId, fontHolder);
         return fontId;
      }

      protected override int GetTextWidth(string text, UIntPtr fontId)
      {
         var size = new NSAttributedString(text, fontCache[fontId].Attributes).Size;
         return (int)Math.Round(size.Width);
      }

      protected override void DrawText(string text, UIntPtr fontId, ref web_color color, ref position pos)
      {
         var fontHolder = fontCache[fontId];
         var scaledFont = new CTFont(fontHolder.Font.GetFontDescriptor(), fontHolder.Font.Size * ScaleFactor);
         var ctAttrs = new CTStringAttributes{ Font = scaledFont, ForegroundColor = color.ToCGColor() };
         var attrString = new NSAttributedString(text, ctAttrs);

         gfx.SaveState();
         gfx.TranslateCTM(pos.x, pos.y + scaledFont.AscentMetric);
         gfx.ScaleCTM(1, -1);
         gfx.TextMatrix = CGAffineTransform.MakeIdentity();

         gfx.SetAllowsAntialiasing(true);
         gfx.SetShouldAntialias(true);
         gfx.SetAllowsFontSmoothing(true);
         gfx.SetShouldSmoothFonts(true);

         using (var textLine = new CTLine(attrString))
         {
            textLine.Draw(gfx);
         }

         gfx.RestoreState();

      }

      private CGBitmapContext gfx { get { return view.BitmapContext; } }

      private CGSize gfxSize { get { return view.BitmapContextSize; } }

      public void DrawRect(nfloat x, nfloat y, nfloat width, nfloat height, CGColor color)
      {
         gfx.SetFillColor(color);
         gfx.FillRect(new CGRect(x, y, width, height));
      }

      protected override void GetImageSize(string imageUrl, ref size size)
      {
         ImageHolder imageHolder;
         if (!imageCache.TryGetValue(imageUrl, out imageHolder))
         {
            var imgBytes = LoadImageCallback(imageUrl);
            var nsImage = new NSImage(NSData.FromArray(imgBytes));
            var rect = new CGRect(new CGPoint(0, 0), nsImage.Size);
            var image = nsImage.AsCGImage(ref rect, null, null);
            imageHolder = new ImageHolder{ Image = image, Size = nsImage.Size };
            imageCache.Add(imageUrl, imageHolder);
         }
         size.width = (int)imageHolder.Size.Width;
         size.height = (int)imageHolder.Size.Height;
      }

      protected override void DrawBackground(UIntPtr hdc, string image, background_repeat repeat, ref web_color color, ref position pos)
      {
         var cgColor = color.ToCGColor();
         var rect = pos.ToRect();

         if (string.IsNullOrEmpty(image))
         {
            gfx.SetFillColor(cgColor);
            gfx.FillRect(rect);
         }
         else
         {
            var imageHolder = imageCache[image];
            gfx.SaveState();
            gfx.TranslateCTM(rect.X, rect.Y + rect.Height);
            gfx.ScaleCTM(1, -1);
            gfx.DrawImage(new CGRect(0, 0, rect.Width, rect.Height), imageHolder.Image);
            gfx.RestoreState();
         }

      }

      protected override void DrawBorders(UIntPtr hdc, ref borders borders, ref position draw_pos, bool root)
      {
         if (borders.top.width > 0)
         {
            DrawRect(draw_pos.x, draw_pos.y, draw_pos.width, borders.top.width, borders.top.color.ToCGColor());
         }
         if (borders.left.width > 0)
         {
            DrawRect(draw_pos.x, draw_pos.y, borders.left.width, draw_pos.width, borders.left.color.ToCGColor());
         }
         if (borders.right.width > 0)
         {
            DrawRect(draw_pos.x, draw_pos.y, borders.right.width, draw_pos.width, borders.right.color.ToCGColor());
         }
         if (borders.bottom.width > 0)
         {
            DrawRect(draw_pos.x, draw_pos.y, draw_pos.width, borders.bottom.width, borders.bottom.color.ToCGColor());
         }
         view.SetNeedsDisplayInRect(view.Bounds);
      }

   }
}

