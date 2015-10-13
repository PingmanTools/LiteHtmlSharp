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

      protected override void GetImageSize(string image, ref size size)
      {
         
      }

      protected override void DrawBackground(UIntPtr hdc, string image, background_repeat repeat, ref web_color color, ref position pos)
      {
         view.BitmapContext.SetFillColor(color.ToCGColor());
         view.BitmapContext.FillRect(pos.ToRect());
         view.SetNeedsDisplayInRect(view.Bounds);
      }

      protected override void DrawBorders(UIntPtr hdc, ref borders borders, ref position draw_pos, bool root)
      {
         if (borders.top.width > 0)
         {
            view.DrawRect(draw_pos.x, draw_pos.y, draw_pos.width, borders.top.width, borders.top.color.ToCGColor());
         }
         if (borders.left.width > 0)
         {
            view.DrawRect(draw_pos.x, draw_pos.y, borders.left.width, draw_pos.width, borders.left.color.ToCGColor());
         }
         if (borders.right.width > 0)
         {
            view.DrawRect(draw_pos.x, draw_pos.y, borders.right.width, draw_pos.width, borders.right.color.ToCGColor());
         }
         if (borders.bottom.width > 0)
         {
            view.DrawRect(draw_pos.x, draw_pos.y, draw_pos.width, borders.bottom.width, borders.bottom.color.ToCGColor());
         }
         view.SetNeedsDisplayInRect(view.Bounds);
      }

   }
}

