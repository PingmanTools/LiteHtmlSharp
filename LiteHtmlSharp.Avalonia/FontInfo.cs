using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Media;

namespace LiteHtmlSharp.Avalonia
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

        public TextDecorationCollection Decorations;

        public FontInfo(string faceName, FontStyle style, FontWeight weight, int size, FontFamily fontFamily = null)
        {
            Family = fontFamily ?? new FontFamily(faceName);
            TypeFace = new Typeface(Family, style, weight);
            Size = size;

            FormattedText format = GetFormattedText("x");
            xHeight = (int)Math.Round(format.Height);
            LineHeight = (int)Math.Ceiling(size * 1.2); // Default line spacing factor

            format = GetFormattedText("X");
            Ascent = (int)Math.Round(format.Height);

            format = GetFormattedText("p");
            Descent = (int)Math.Round(format.Height) - xHeight;
        }

        public FormattedText GetFormattedText(string text)
        {
            var formattedText = new FormattedText(
                text, 
                CultureInfo.InvariantCulture, 
                FlowDirection.LeftToRight, 
                TypeFace, 
                Size, 
                null); // Use null brush, will be set later

            if (Decorations != null)
            {
                formattedText.SetTextDecorations(Decorations);
            }
            
            return formattedText;
        }
    }
}