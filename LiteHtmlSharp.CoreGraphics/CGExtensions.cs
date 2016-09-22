using System;
using LiteHtmlSharp;
using CoreGraphics;

namespace LiteHtmlSharp.CoreGraphics
{
   public static class CGExtensions
   {
      public static CGRect ToRect(this position pos)
      {
         return new CGRect(pos.x, pos.y, pos.width, pos.height);
      }

      public static position ToPosition(this CGRect rect)
      {
         return new position
         { 
            width = (int)Math.Round(rect.Width), 
            height = (int)Math.Round(rect.Height), 
            x = (int)Math.Round(rect.X), 
            y = (int)Math.Round(rect.Y) 
         };
      }

      const float MaxByteAsFloat = (float)byte.MaxValue;

      public static CGColor ToCGColor(this web_color wc)
      {
         return new CGColor(wc.red / MaxByteAsFloat, wc.green / MaxByteAsFloat, wc.blue / MaxByteAsFloat, wc.alpha / MaxByteAsFloat);
      }

      public static CGSize ToCGSize(this LiteHtmlSize viewportSize)
      {
         return new CGSize(viewportSize.Width, viewportSize.Height);
      }

      public static CGPoint ToCGPoint(this LiteHtmlPoint viewportPoint)
      {
         return new CGPoint(viewportPoint.X, viewportPoint.Y);
      }

      public static LiteHtmlSize ToLiteHtmlSize(this CGSize size)
      {
         return new LiteHtmlSize(size.Width, size.Height);
      }

      public static LiteHtmlPoint ToLiteHtmlPoint(this CGPoint point)
      {
         return new LiteHtmlPoint(point.X, point.Y);
      }
   }
}

