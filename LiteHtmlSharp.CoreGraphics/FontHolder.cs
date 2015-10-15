using System;
using Foundation;
using CoreText;
using CoreGraphics;
using AppKit;

namespace LiteHtmlSharp.CoreGraphics
{
   public class FontHolder
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

}

