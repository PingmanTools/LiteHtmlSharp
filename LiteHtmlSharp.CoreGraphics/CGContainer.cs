using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using Foundation;
using CoreGraphics;
using CoreText;

using LiteHtmlSharp;
using System.Runtime.InteropServices;

namespace LiteHtmlSharp.CoreGraphics
{
   public class CGContainer : ViewportContainer
   {
      public delegate ImageHolder LoadImageDelegate(string imageUrl);

      public LoadImageDelegate LoadImageCallback;

      public delegate void SetCursorDelegate(string cursor);

      public SetCursorDelegate SetCursorCallback { get; set; }

      public event Action<string> CaptionDefined;

      public CGContext Context { get; set; }


      public event Action<string> RenderHtmlRequested;

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
      }

      protected override void DrawListMarker(string image, string baseURL, list_style_type marker_type, ref web_color color, ref position pos)
      {
         Context.SetFillColor(color.ToCGColor());

         switch (marker_type)
         {
         case list_style_type.list_style_type_square:
            Context.FillRect(pos.ToRect());
            break;
         case list_style_type.list_style_type_circle:
         default:
            Context.FillEllipseInRect(pos.ToRect());
            break;
         }
      }

      public override void Render(string html)
      {
         RenderHtmlRequested?.Invoke(html);
      }

      protected override void SetCursor(string cursor)
      {
         if (SetCursorCallback != null)
         {
            SetCursorCallback(cursor);
         }
      }

      protected override int PTtoPX(int pt)
      {
         return 1;
      }

      protected override void SetBaseURL(string base_url)
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
         var fontHolder = fontCache [fontId];
         // if different scale then recreate the font with new size
         if (fontHolder.Scale != ScaleFactor)
         {
            fontHolder.Attributes.Font = fontHolder.Attributes.Font.WithSymbolicTraits(fontHolder.Size * ScaleFactor, CTFontSymbolicTraits.None, CTFontSymbolicTraits.None);
            fontHolder.Scale = ScaleFactor;
         }
         return fontHolder;
      }

      public string DefaultFontName { get; set; }
      public int DefaultFontSize { get; set; }
      public web_color DefaultFontColor { get; set; }

      protected override int GetDefaultFontSize()
      {
         return DefaultFontSize;
      }

      protected override string GetDefaultFontName()
      {
         return DefaultFontName;
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
      }

      #endregion


      #region Image

      ImageHolder GetCacheImage(string imageUrl)
      {
         ImageHolder imageHolder;
         if (imageCache.TryGetValue(imageUrl, out imageHolder))
         {
            return imageHolder;
         }

         if (LoadImageCallback == null)
         {
            throw new Exception(nameof(LoadImageCallback) + " must be set before an image is requested while rendering");
         }
         imageHolder = LoadImageCallback(imageUrl);
         if (imageHolder == null)
         {
            imageCache.Add(imageUrl, null);
            return null;
         }

         imageCache.Add(imageUrl, imageHolder);
         return imageHolder;
      }

      protected override void GetImageSize(string imageUrl, ref size size)
      {
         var imageHolder = GetCacheImage(imageUrl);
         if (imageHolder != null)
         {
            size.width = (int)imageHolder.Size.Width;
            size.height = (int)imageHolder.Size.Height;
         }
      }

      private void DrawImage(string image, CGRect rect)
      {
         var imageHolder = GetCacheImage(image);
         if (imageHolder != null)
         {
            Context.SaveState();
            Context.TranslateCTM(rect.X, rect.Y + rect.Height);
            Context.ScaleCTM(1, -1);
            Context.DrawImage(new CGRect(0, 0, rect.Width, rect.Height), imageHolder.Image);
            Context.RestoreState();
         }
      }

      #endregion


      #region Rect

      protected override void DrawBackground(UIntPtr hdc, string image, background_repeat repeat, ref web_color color, ref position pos, ref border_radiuses br, ref position borderBox, bool isRoot)
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
         else {
            DrawImage(image, rect);
         }
      }

      protected override void DrawBorders(UIntPtr hdc, ref borders borders, ref position draw_pos, bool root)
      {
         var rect = draw_pos.ToRect();
         var br = borders.radius;

         if (borders.top.width > 0)
         {
            Context.MoveTo(rect.Left + br.top_left_x, rect.Top);
            Context.AddLineToPoint(rect.Right - br.top_right_x, rect.Top);
            Context.AddQuadCurveToPoint(rect.Right, rect.Top, rect.Right, rect.Top + br.top_right_y);

            Context.SetLineWidth(borders.top.width);
            Context.SetStrokeColor(borders.top.color.ToCGColor());
            Context.StrokePath();
         }
         if (borders.right.width > 0)
         {
            Context.MoveTo(rect.Right, rect.Top + br.top_right_y);
            Context.AddLineToPoint(rect.Right, rect.Bottom - br.bottom_right_y);
            Context.AddQuadCurveToPoint(rect.Right, rect.Bottom, rect.Right - br.bottom_right_x, rect.Bottom);

            Context.SetLineWidth(borders.right.width);
            Context.SetStrokeColor(borders.right.color.ToCGColor());
            Context.StrokePath();
         }
         if (borders.bottom.width > 0)
         {
            Context.MoveTo(rect.Right - br.bottom_right_x, rect.Bottom);
            Context.AddLineToPoint(rect.Left + br.bottom_left_x, rect.Bottom);
            Context.AddQuadCurveToPoint(rect.Left, rect.Bottom, rect.Left, rect.Bottom - br.bottom_left_y);

            Context.SetLineWidth(borders.bottom.width);
            Context.SetStrokeColor(borders.bottom.color.ToCGColor());
            Context.StrokePath();
         }
         if (borders.left.width > 0)
         {
            Context.MoveTo(rect.Left, rect.Bottom - br.bottom_left_y);
            Context.AddLineToPoint(rect.Left, rect.Top + br.top_left_y);
            Context.AddQuadCurveToPoint(rect.Left, rect.Top, rect.Left + br.top_left_x, rect.Top);

            Context.SetLineWidth(borders.left.width);
            Context.SetStrokeColor(borders.left.color.ToCGColor());
            Context.StrokePath();
         }

      }

      #endregion

   }
}

