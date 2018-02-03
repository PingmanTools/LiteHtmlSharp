using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using System.Linq;
using System.Windows;
using System.Globalization;

namespace LiteHtmlSharp.Wpf
{
   public class FontInfo
   {
      public FontFamily Family;
      public Typeface TypeFace;
      public int Size;
      public int Ascent;
      public int Descent;
      public int xHeight;
      public int LineHeight;

      public TextDecorationCollection Decorations = new TextDecorationCollection();

      public FontInfo(string faceName, FontStyle style, FontWeight weight, int size, FontFamily fontFamily = null)
      {
         // Using the pack URI means we can look inside resources in addition to system fonts - this is only supported for backwards compatibility, as it causes memory leaks.
         // https://stackoverflow.com/questions/31452443/wpf-textblock-memory-leak-when-using-font
         // FontFamily will allow comma separated font names, but does not support quotes of any kind (so don't quote it in your HTML/CSS!).

         Family = fontFamily ?? new FontFamily(new Uri("pack://application:,,,/Fonts/"), faceName);

         TypeFace = new Typeface(Family, style, weight, new FontStretch());
         Size = size;

         FormattedText format = GetFormattedText("x");
         xHeight = (int)Math.Round(format.Extent);
         LineHeight = (int)Math.Ceiling(size * Family.LineSpacing);

         format = GetFormattedText("X");

         Ascent = (int)Math.Round(format.Extent);

         format = GetFormattedText("p");
         Descent = (int)Math.Round(format.Extent) - xHeight;
      }

      public FormattedText GetFormattedText(string text, TextAlignment alignment = TextAlignment.Left)
      {
         var formattedText = new FormattedText(text, CultureInfo.InvariantCulture, System.Windows.FlowDirection.LeftToRight, TypeFace, Size, null);
         formattedText.SetTextDecorations(Decorations);
         return formattedText;
      }

   }
}
