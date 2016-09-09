using System;
using Foundation;
using CoreText;
using CoreGraphics;
using System.Linq;

namespace LiteHtmlSharp.CoreGraphics
{
   public class FontHolder
   {
      public CTStringAttributes Attributes { get; set; }

      public int Scale { get; set; }

      public int Size { get; set; }

      public font_decoration Decoration { get; set; }

      public int Weight { get; set; }

      public string FaceName { get; set; }

      public static FontHolder Create(string faceName, int size, int weight, font_style italic, font_decoration decoration, ref font_metrics fm)
      {
         var possibleFonts = faceName.Split (',').ToList ();
         possibleFonts.Add("Helvetica"); // Make sure there's a font in the list.
         CTFont font = null;
         foreach (var fontName in possibleFonts) {
            // faceName = faceName; // normalize between WPF and Mac for ways to reference custom fonts.
            font = new CTFont (fontName.TrimEnd().Replace ("./#", ""), size);

            // Bold & italic are properties of the CTFont
            var traits = CTFontSymbolicTraits.None;
            if (italic == font_style.fontStyleItalic) {
               traits |= CTFontSymbolicTraits.Italic;
            }
            if (weight > 400) {
               traits |= CTFontSymbolicTraits.Bold;
            }
            font = font.WithSymbolicTraits (font.Size, traits, traits);
            if (font != null) {
               break; 
            }
         }

         var strAttrs = new CTStringAttributes { Font = font };

         // initial size must be unscaled when getting these metrics
         fm.ascent = (int)Math.Round(font.AscentMetric);
         fm.descent = (int)Math.Round(font.DescentMetric);
         fm.height = (int)Math.Round(font.AscentMetric + font.DescentMetric);
         fm.x_height = (int)Math.Round(font.XHeightMetric);

         fm.draw_spaces = decoration.HasFlag(font_decoration.font_decoration_underline) || decoration.HasFlag(font_decoration.font_decoration_linethrough);

         var fontHolder = new FontHolder
         { 
            FaceName = faceName,
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
         else if (text == null)
         {
            text = "";
         }

         var range = new NSRange(0, text.Length);
         var attrString = new NSMutableAttributedString(text, Attributes);

         // these styles can only be applied to the NSAttributedString
         if (Decoration.HasFlag(font_decoration.font_decoration_linethrough))
         {
            attrString.AddAttribute((NSString)"NSStrikethrough", new NSNumber(1), range);
         }
         if (Decoration.HasFlag(font_decoration.font_decoration_underline))
         {
            attrString.AddAttribute((NSString)"NSUnderline", new NSNumber(1), range);
            attrString.AddAttribute((NSString)"NSUnderlineColor", new NSObject(color.ToCGColor().Handle), range);
         }

         if (Decoration.HasFlag(font_decoration.font_decoration_overline))
         {

         }

         if (Weight > 400)
         {
            #if __MOBILE__
            var boldFontDescriptor = UIKit.UIFont.FromName(FaceName, Size).FontDescriptor.CreateWithTraits(UIKit.UIFontDescriptorSymbolicTraits.Bold);
            var boldFont = UIKit.UIFont.FromDescriptor(boldFontDescriptor, 0);
            attrString.AddAttribute(UIKit.UIStringAttributeKey.Font, boldFont, new NSRange(0, text.Length));
            #else
            AppKit.NSMutableAttributedStringAppKitAddons.ApplyFontTraits(attrString, AppKit.NSFontTraitMask.Bold, new NSRange(0, text.Length));
            #endif
         }

         return attrString;
      }
   }

}

