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
      public FontFamily Family;
      public Typeface TypeFace;
      public int Size;
      public int Ascent;
      public int Descent;
      public int Height;

      public TextDecorationCollection Decorations = new TextDecorationCollection();

      public FontInfo(string faceName, FontStyle style, FontWeight weight, int size)
      {
         Family = new FontFamily(faceName);
         TypeFace = new Typeface(Family, style, weight, new FontStretch());
         Size = size;

         //System.Drawing.FontFamily ff = new System.Drawing.FontFamily(faceName);
         //Ascent = ff.GetCellAscent(System.Drawing.FontStyle.Regular);
         //Descent = ff.GetCellDescent(System.Drawing.FontStyle.Regular);
      }

   }
}
