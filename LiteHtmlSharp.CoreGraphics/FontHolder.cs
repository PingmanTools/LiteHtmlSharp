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

      public static FontHolder Create(string faceName, int size, int weight, font_style italic, font_decoration decoration, ref font_metrics fm)
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
         return fontHolder;
      }

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

