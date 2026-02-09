using System;
using System.Collections.Generic;
using Avalonia.Media;

namespace LiteHtmlSharp.Avalonia
{
    public static class LiteHtmlExtensions
    {
        static Dictionary<string, IBrush> _brushes = new Dictionary<string, IBrush>();
        static Dictionary<string, IPen> _pens = new Dictionary<string, IPen>();

        public static IBrush GetBrush(this web_color color)
        {
            string key = color.red.ToString() + color.green.ToString() + color.blue.ToString() + color.alpha.ToString();

            IBrush result = null;
            if (!_brushes.TryGetValue(key, out result))
            {
                result = new SolidColorBrush(Color.FromArgb(color.alpha, color.red, color.green, color.blue));
                _brushes.Add(key, result);
            }

            return result;
        }

        public static IPen GetPen(this web_color color, double thickness)
        {
            string key = color.red.ToString() + color.green.ToString() + color.blue.ToString() + thickness;

            IPen result = null;
            if (!_pens.TryGetValue(key, out result))
            {
                IBrush brush = color.GetBrush();
                result = new Pen(brush, thickness);
                _pens.Add(key, result);
            }

            return result;
        }
    }
}