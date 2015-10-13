using System;
using LiteHtmlSharp;
using AppKit;
using CoreGraphics;
using System.IO;

namespace MacTest
{
   public class LiteHtmlMacContainer : Container
   {
      LiteHtmlView view;

      static string masterCss;

      public LiteHtmlMacContainer(LiteHtmlView view)
         : base()
      {
         this.view = view;
      }

      protected override string GetMasterCssData()
      {
         if (masterCss == null)
         {
            masterCss = File.ReadAllText("master.css");
         }
         return masterCss;
      }

      protected override void DrawBackground(UIntPtr hdc, string image, background_repeat repeat, ref web_color color, ref position pos)
      {
         view.BitmapContext.SetFillColor(GetCGColor(ref color));
         view.BitmapContext.FillRect(new CGRect(pos.x, pos.y, pos.width, pos.height));
         view.SetNeedsDisplayInRect(view.Bounds);
      }

      protected override void DrawBorders(UIntPtr hdc, ref borders borders, ref position draw_pos, bool root)
      {
         if (borders.top.width > 0)
         {
            view.DrawRect(draw_pos.x, draw_pos.y, draw_pos.width, borders.top.width, GetCGColor(ref borders.top.color));
         }

         if (borders.left.width > 0)
         {
            view.DrawRect(draw_pos.x, draw_pos.y, borders.left.width, draw_pos.width, GetCGColor(ref borders.left.color));
         }

         if (borders.right.width > 0)
         {
            view.DrawRect(draw_pos.x, draw_pos.y, borders.right.width, draw_pos.width, GetCGColor(ref borders.right.color));
         }

         if (borders.bottom.width > 0)
         {
            view.DrawRect(draw_pos.x, draw_pos.y, draw_pos.width, borders.bottom.width, GetCGColor(ref borders.bottom.color));
         }
         view.SetNeedsDisplayInRect(view.Bounds);
      }

      const float MaxByteAsFloat = (float)byte.MaxValue;

      public static CGColor GetCGColor(ref web_color wc)
      {
         return new CGColor(wc.red / MaxByteAsFloat, wc.green / MaxByteAsFloat, wc.blue / MaxByteAsFloat, wc.alpha / MaxByteAsFloat);
      }

   }
}

