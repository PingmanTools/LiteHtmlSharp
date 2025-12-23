using System;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

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

        public bool HasUnderline;
        public double UnderlineOffset;
        public double UnderlineThickness;

        public FontInfo(string faceName, FontStyle style, FontWeight weight, int size, FontFamily fontFamily = null)
        {
            Family = fontFamily ?? SafeCreateFontFamily(faceName);
            TypeFace = new Typeface(Family, style, weight);
            Size = size;

            var layout = CreateTextLayout("x", null);
            xHeight = (int)Math.Round(layout.Height);
            LineHeight = (int)Math.Ceiling(size * 1.2); // Default line spacing factor

            layout = CreateTextLayout("X", null);
            Ascent = (int)Math.Round(layout.Height);

            layout = CreateTextLayout("p", null);
            Descent = (int)Math.Round(layout.Height) - xHeight;

            // Calculate underline position using font metrics
            var baselineLayout = CreateTextLayout("X", null);
            var baseline = baselineLayout.Baseline;

            if (FontManager.Current.TryGetGlyphTypeface(TypeFace, out var glyphTypeface))
            {
                var metrics = glyphTypeface.Metrics;
                var scale = (double)size / metrics.DesignEmHeight;
                // UnderlinePosition is positive in Avalonia (distance below baseline)
                UnderlineOffset = baseline + metrics.UnderlinePosition * scale;
                UnderlineThickness = metrics.UnderlineThickness * scale;
            }
            else
            {
                // Fallback using typical font proportions
                UnderlineOffset = baseline + size * 0.1;
                UnderlineThickness = size * 0.05;
            }
        }

        private static FontFamily SafeCreateFontFamily(string faceName)
        {
            // Normalize and guard against relative placeholders like "." or "./"
            if (string.IsNullOrWhiteSpace(faceName) ||
                faceName == "." ||
                faceName == "./")
            {
                return FontFamily.Default;
            }

            // If it starts with "./" strip that so at least a plain family name remains
            if (faceName.StartsWith("./"))
            {
                faceName = faceName.Substring(2);
                if (string.IsNullOrWhiteSpace(faceName))
                    return FontFamily.Default;
            }

            // Avoid accidentally passing a pure relative path
            if (Uri.TryCreate(faceName, UriKind.Relative, out var rel) &&
                !Uri.TryCreate(faceName, UriKind.Absolute, out _))
            {
                return FontFamily.Default;
            }

            try
            {
                return new FontFamily(faceName);
            }
            catch (ArgumentException)
            {
                return FontFamily.Default;
            }
        }

        public TextLayout CreateTextLayout(string text, IBrush brush)
        {
            return new TextLayout(text, TypeFace, Size, brush);
        }

        public FormattedText GetFormattedText(string text)
        {
            return new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                TypeFace,
                Size,
                null);
        }
    }
}