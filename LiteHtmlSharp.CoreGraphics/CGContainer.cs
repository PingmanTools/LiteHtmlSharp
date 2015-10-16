using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;

using AppKit;
using Foundation;
using CoreGraphics;
using CoreText;

using LiteHtmlSharp;


namespace LiteHtmlSharp.CoreGraphics
{
   public class CGContainer : Container
   {
      public delegate byte[] LoadImageDelegate(string imageUrl);

      public event Action ContextDrawn;

      public event Action<string> CaptionDefined;

      public LoadImageDelegate LoadImageCallback;

      public CGContext Context { get; private set; }

      public CGSize ContextSize { get; private set; }

      Dictionary<string, ImageHolder> imageCache;
      Dictionary<UIntPtr, FontHolder> fontCache;
      uint lastFontId = 0;


      public CGContainer(string masterCssData)
         : base(masterCssData)
      {
         fontCache = new Dictionary<UIntPtr, FontHolder>();
         imageCache = new Dictionary<string, ImageHolder>();
      }

      public void DrawRect(nfloat x, nfloat y, nfloat width, nfloat height, CGColor color)
      {
         Context.SetFillColor(color);
         Context.FillRect(new CGRect(x, y, width, height));
         if (ContextDrawn != null)
         {
            ContextDrawn();
         }
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

      public void RenderHtml(string html)
      {
         base.RenderHtml(html, (int)ContextSize.Width);
      }

      void Render()
      {
         Render((int)ContextSize.Width);
      }

      void Draw()
      {
         Draw(0, 0, new position{ x = 0, y = 0, width = (int)ContextSize.Width, height = (int)ContextSize.Height });
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

      public override bool OnMouseLeave()
      {
         var needsRedraw = base.OnMouseLeave();
         if (needsRedraw)
         {
            Draw();
         }
         return needsRedraw;
      }

      protected override void SetCursor(string cursor)
      {
         switch (cursor)
         {
            case "pointer":
               NSCursor.PointingHandCursor.Set();
               break;
            default:
               NSCursor.ArrowCursor.Set();
               break;
         }
      }

      protected override void OnAnchorClick(string url)
      {
         NSAlert.WithMessage("URL", "Okay", null, null, url).RunModal();
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

      protected override void SetCaption(string caption)
      {
         if (CaptionDefined != null)
         {
            CaptionDefined(caption);
         }
      }


      #region Font / Text

      protected override UIntPtr CreateFont(string faceName, int size, int weight, font_style italic, font_decoration decoration, ref font_metrics fm)
      {
         var fontId = new UIntPtr(++lastFontId);
         var fontHolder = FontHolder.Create(faceName, size, weight, italic, decoration, ref fm);
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

      protected override int GetDefaultFontSize()
      {
         return (int)Math.Round(NSFont.SystemFontSize) + 20;
      }

      protected override string GetDefaultFontName()
      {
         return NSFont.SystemFontOfSize(NSFont.SystemFontSize).FontName;
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

         Context.SaveState();
         Context.TranslateCTM(pos.x, pos.y + fontHolder.Attributes.Font.AscentMetric);
         Context.ScaleCTM(1, -1);
         Context.TextMatrix = CGAffineTransform.MakeIdentity();

         /*Context.SetAllowsAntialiasing(true);
         Context.SetShouldAntialias(true);
         Context.SetAllowsFontSmoothing(true);
         Context.SetShouldSmoothFonts(true);*/

         using (var textLine = new CTLine(attrString))
         {
            textLine.Draw(Context);
         }

         Context.RestoreState();
         if (ContextDrawn != null)
         {
            ContextDrawn();
         }
      }

      #endregion


      #region Image

      ImageHolder GetCacheImage(string imageUrl)
      {
         ImageHolder imageHolder;
         if (!imageCache.TryGetValue(imageUrl, out imageHolder))
         {
            if (LoadImageCallback == null)
            {
               throw new Exception(nameof(LoadImageCallback) + " must be set before an image is requested while rendering");
            }
            var imgBytes = LoadImageCallback(imageUrl);
            var nsImage = new NSImage(NSData.FromArray(imgBytes));
            var rect = new CGRect(new CGPoint(0, 0), nsImage.Size);
            var image = nsImage.AsCGImage(ref rect, null, null);
            imageHolder = new ImageHolder{ Image = image, Size = nsImage.Size };
            imageCache.Add(imageUrl, imageHolder);
         }
         return imageHolder;
      }

      protected override void GetImageSize(string imageUrl, ref size size)
      {
         var imageHolder = GetCacheImage(imageUrl);
         size.width = (int)imageHolder.Size.Width;
         size.height = (int)imageHolder.Size.Height;
      }

      private void DrawImage(string image, CGRect rect)
      {
         var imageHolder = GetCacheImage(image);
         Context.SaveState();
         Context.TranslateCTM(rect.X, rect.Y + rect.Height);
         Context.ScaleCTM(1, -1);
         Context.DrawImage(new CGRect(0, 0, rect.Width, rect.Height), imageHolder.Image);
         Context.RestoreState();
      }

      #endregion


      #region Rect

      protected override void DrawBackground(UIntPtr hdc, string image, background_repeat repeat, ref web_color color, ref position pos, ref border_radiuses br, ref position borderBox)
      {
         var cgColor = color.ToCGColor();
         var rect = pos.ToRect();

         if (string.IsNullOrEmpty(image))
         {
            CGPath path = new CGPath();
            path.MoveToPoint(rect.Left + br.top_left_x, rect.Top);
            path.AddLineToPoint(rect.Right - br.top_right_x, rect.Top);
            path.AddQuadCurveToPoint(rect.Right, rect.Top, rect.Right, rect.Top + br.top_right_y);
            path.AddLineToPoint(rect.Right, rect.Bottom - br.bottom_right_y);
            path.AddQuadCurveToPoint(rect.Right, rect.Bottom, rect.Right - br.bottom_right_x, rect.Bottom);
            path.AddLineToPoint(rect.Left + br.bottom_left_x, rect.Bottom);
            path.AddQuadCurveToPoint(rect.Left, rect.Bottom, rect.Left, rect.Bottom - br.bottom_left_y);
            path.AddLineToPoint(rect.Left, rect.Top + br.top_left_y);
            path.AddQuadCurveToPoint(rect.Left, rect.Top, rect.Left + br.top_left_x, rect.Top);

            Context.SetFillColor(cgColor);
            Context.AddPath(path);
            Context.FillPath();
         }
         else
         {
            DrawImage(image, rect);
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

      #endregion

   }
}

