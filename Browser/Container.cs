using LiteHtml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace Browser
{
   public class Container : FrameworkElement
   {
      DrawingContext _dc;
      DrawingVisual _visual = new DrawingVisual();
      Dictionary<string, Brush> _brushes = new Dictionary<string, Brush>();

      [DllImport("LiteHtmlLib.dll", CallingConvention = CallingConvention.Cdecl)]
      public static extern void SetTestFunction(CallbackFunc func);

      [DllImport("LiteHtmlLib.dll", CallingConvention = CallingConvention.Cdecl)]
      public static extern void SetDrawBorders(DrawBordersFunc func);

      [DllImport("LiteHtmlLib.dll", CallingConvention = CallingConvention.Cdecl)]
      public static extern void SetDrawBackground(DrawBackgroundFunc func);

      [DllImport("LiteHtmlLib.dll", CallingConvention = CallingConvention.Cdecl)]
      public static extern void Init();

      public Container(Grid parent)
      {
         parent.Children.Add(this);

         SetTestFunction(MyCallbackFunc);
         SetDrawBorders(DrawBorders);
         SetDrawBackground(DrawBackground);

         _dc = _visual.RenderOpen();
         Init();
         _dc.Close();
         _dc = null;
      }

      private void TestDrawing()
      {
         var color = new web_color() { alpha = 255, red = 255 };
         DrawRect(0, 0, 100, 100, GetBrush(ref color));
      }

      private void MyCallbackFunc(Int32 someNumber)
      {
         int b = someNumber;
      }

      private void DrawBackground(UIntPtr hdc, background_repeat repeat, ref web_color color, ref position pos)
      {
         if(pos.width > 0 && pos.height  > 0)
         {
            DrawRect(pos.x, pos.y, pos.width, pos.height, GetBrush(ref color));
         }
      }

      private void DrawBorders(UIntPtr hdc, ref borders borders, ref position draw_pos, bool root)
      {
         if (borders.top.width > 0)
         {
            DrawRect(draw_pos.x, draw_pos.y, draw_pos.width, borders.top.width, GetBrush(ref borders.top.color));
         }

         if (borders.left.width > 0)
         {
            DrawRect(draw_pos.x, draw_pos.y, borders.left.width, draw_pos.width, GetBrush(ref borders.left.color));
         }

         if (borders.right.width > 0)
         {
            DrawRect(draw_pos.x, draw_pos.y, borders.right.width, draw_pos.width, GetBrush(ref borders.right.color));
         }

         if (borders.bottom.width > 0)
         {
            DrawRect(draw_pos.x, draw_pos.y, draw_pos.width, borders.bottom.width, GetBrush(ref borders.bottom.color));
         }
      }

      private void DrawRect(int x, int y, int width, int height, Brush brush)
      {
         Rect rect = new Rect(x, y, width, height);
         _dc.DrawRectangle(brush, null, rect);
      }

      private Brush GetBrush(ref web_color color)
      {
         string key = color.red.ToString() + color.green.ToString() + color.blue.ToString();

         Brush result = null;
         if(!_brushes.TryGetValue(key, out result))
         {
            result = new SolidColorBrush(Color.FromArgb(color.alpha, color.red, color.green, color.blue));
            _brushes.Add(key, result);
         }

         return result;
      }

      protected override int VisualChildrenCount
      {
         get { return 1; }
      }

      protected override Visual GetVisualChild(int index)
      {
         if (index != 0)
         {
            throw new ArgumentOutOfRangeException();
         }

         return _visual;
      }
   }
}
