using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using System.Linq;
using System.Windows;

namespace LiteHtmlSharp
{
   public class FontInfo
   {
      FontFamily Family;
      public Typeface TypeFace;
      public int Size;
      public TextDecorationCollection Decorations = new TextDecorationCollection();

      public FontInfo(FontFamily family, FontStyle style, FontWeight weight, int size)
      {
         Family = family;
         TypeFace = new Typeface(Family, style, weight, new FontStretch());
         Size = size;
      }

   }
}
