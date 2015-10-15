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
   public class LiteHtmlCGContainer : Container
   {
      public delegate byte[] LoadImageDelegate(string imageUrl);

      public event Action ContextDrawn;

      public LoadImageDelegate LoadImageCallback;

      public CGContext Context { get; private set; }

      public CGSize ContextSize { get; private set; }

      class FontHolder
      {
         public CTStringAttributes Attributes { get; set; }

         public int Scale { get; set; }

         public int Size { get; set; }

         public font_decoration Decoration { get; set; }

         public int Weight { get; set; }

         public NSAttributedString GetAttributedString(string text, web_color color)
         {
            Attributes.ForegroundColor = color.ToCGColor();

            // CoreText won't draw underlines for whitespace so draw a transparent dot
            if (text == " ")
            {
               text = ".";
               Attributes.ForegroundColor = new CGColor(0, 0, 0, 0);
            }

            var range = new NSRange(0, text.Length);
            var attrString = new NSMutableAttributedString(text, Attributes);

            // these styles can only be applied to the NSAttributedString
            if (Decoration.HasFlag(font_decoration.font_decoration_linethrough))
            {
               attrString.AddAttribute(NSStringAttributeKey.StrikethroughStyle, new NSNumber(1), range);
            }
            if (Decoration.HasFlag(font_decoration.font_decoration_underline))
            {
               attrString.AddAttribute(NSStringAttributeKey.UnderlineStyle, new NSNumber(1), range);
               attrString.AddAttribute(NSStringAttributeKey.UnderlineColor, new NSObject(color.ToCGColor().Handle), range);
            }

            if (Decoration.HasFlag(font_decoration.font_decoration_overline))
            {

            }

            if (Weight > 400)
            {
               attrString.ApplyFontTraits(NSFontTraitMask.Bold, new NSRange(0, text.Length));
            }

            return attrString;
         }
      }

      class ImageHolder
      {
         public CGImage Image { get; set; }

         public CGSize Size { get; set; }

      }

      Dictionary<UIntPtr, FontHolder> fontCache;
      uint lastFontId = 0;

      Dictionary<string, ImageHolder> imageCache;

      public LiteHtmlCGContainer(string masterCssData)
         : base(masterCssData)
      {
         fontCache = new Dictionary<UIntPtr, FontHolder>();
         imageCache = new Dictionary<string, ImageHolder>();
      }

      public void SetContext(CGContext context, CGSize contextSize, int scaleFactor)
      {
         var needsUpdate = contextSize != ContextSize || ScaleFactor != scaleFactor;
         Context = context;
         ContextSize = contextSize;
         ScaleFactor = scaleFactor;
         if (needsUpdate)
         {
            OnMediaChanged();
            Render();
            Draw();
         }
      }

      public override bool OnMouseMove(int x, int y)
      {
         var needsRedraw = base.OnMouseMove(x, y);
         if (needsRedraw)
         {
            Draw();
         }
         return needsRedraw;
      }

      void Render()
      {
         Render((int)ContextSize.Width);
      }

      void Draw()
      {
         Draw(0, 0, new position{ x = 0, y = 0, width = (int)ContextSize.Width, height = (int)ContextSize.Height });
      }

      protected override void OnAnchorClick(string url)
      {
         NSAlert.WithMessage("URL", "Okay", null, null, url).RunModal();
      }

      protected override int CreateElement(string tag, string attributes)
      {
         return 0;
      }

      protected override int PTtoPX(int pt)
      {
         return 1;
      }

      protected override void GetClientRect(ref position client)
      {
         client.width = (int)ContextSize.Width;
         client.height = (int)ContextSize.Height;
      }

      protected override void GetMediaFeatures(ref media_features media)
      {
         media.width = media.device_width = (int)ContextSize.Width;
         media.height = media.device_height = (int)ContextSize.Height;
      }

      protected override void SetBaseURL(ref string base_url)
      {
         
      }

      protected override UIntPtr CreateFont(string faceName, int size, int weight, font_style italic, font_decoration decoration, ref font_metrics fm)
      {
         var font = new CTFont(faceName, size);
         var strAttrs = new CTStringAttributes { Font = font };

         // Bold & italic are properties of the CTFont
         var traits = CTFontSymbolicTraits.None;
         if (italic == font_style.fontStyleItalic)
         {
            traits |= CTFontSymbolicTraits.Italic;
         }
         if (weight > 400)
         {
            traits |= CTFontSymbolicTraits.Bold;
         }
         font = font.WithSymbolicTraits(font.Size, traits, traits);

         // initial size must be unscaled when getting these metrics
         fm.ascent = (int)Math.Round(font.AscentMetric);
         fm.descent = (int)Math.Round(font.DescentMetric);
         fm.height = (int)Math.Round(font.AscentMetric + font.DescentMetric);
         fm.x_height = (int)Math.Round(font.XHeightMetric);

         fm.draw_spaces = decoration.HasFlag(font_decoration.font_decoration_underline) || decoration.HasFlag(font_decoration.font_decoration_linethrough);

         var fontHolder = new FontHolder
         { 
            Size = size,
            Attributes = strAttrs,
            Decoration = decoration,
            Weight = weight
         };

         lastFontId++;
         var fontId = new UIntPtr(lastFontId);
         fontCache.Add(fontId, fontHolder);
         return fontId;
      }

      FontHolder GetFont(UIntPtr fontId)
      {
         var fontHolder = fontCache[fontId];
         // if different scale then recreate the font with new size
         if (fontHolder.Scale != ScaleFactor)
         {
            fontHolder.Attributes.Font = fontHolder.Attributes.Font.WithSymbolicTraits(fontHolder.Size * ScaleFactor, CTFontSymbolicTraits.None, CTFontSymbolicTraits.None);
            fontHolder.Scale = ScaleFactor;
         }
         return fontHolder;
      }

      protected override int GetTextWidth(string text, UIntPtr fontId)
      {
         var fontHolder = GetFont(fontId);
         var attrString = fontHolder.GetAttributedString(text, new web_color());
         var size = attrString.Size;
         return (int)Math.Round(size.Width / ScaleFactor);
      }

      protected override void DrawText(string text, UIntPtr fontId, ref web_color color, ref position pos)
      {
         var fontHolder = GetFont(fontId);


         var attrString = fontHolder.GetAttributedString(text, color);

         gfx.SaveState();
         gfx.TranslateCTM(pos.x, pos.y + fontHolder.Attributes.Font.AscentMetric);
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
         if (ContextDrawn != null)
         {
            ContextDrawn();
         }
      }

      private CGContext gfx { get { return Context; } }

      private CGSize gfxSize { get { return ContextSize; } }

      public void DrawRect(nfloat x, nfloat y, nfloat width, nfloat height, CGColor color)
      {
         gfx.SetFillColor(color);
         gfx.FillRect(new CGRect(x, y, width, height));
         if (ContextDrawn != null)
         {
            ContextDrawn();
         }
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
         if (ContextDrawn != null)
         {
            ContextDrawn();
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
         if (ContextDrawn != null)
         {
            ContextDrawn();
         }
      }

   }
}

