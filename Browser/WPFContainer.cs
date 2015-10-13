using LiteHtmlSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.IO;

namespace Browser
{
   public class WPFContainer : Container
   {
      DrawingContext _dc;
      DrawingVisual _visual;
      Dictionary<string, Brush> _brushes = new Dictionary<string, Brush>();
      static string _masterCSS;
      public bool Loaded = false;
      private bool _rendering = false;

      public WPFContainer(DrawingVisual visual) : base()
      {
         _visual = visual;

         if (string.IsNullOrEmpty(_masterCSS))
         {
            _masterCSS = File.ReadAllText("master.css");
         }

         PInvokes.SetMasterCSS(CPPContainer, _masterCSS);
      }

      protected override string GetMasterCssData()
      {
         if (string.IsNullOrEmpty(_masterCSS))
         {
            _masterCSS = File.ReadAllText("master.css");
         }
         return _masterCSS;
      }

      public void Render(string html)
      {
         if (_rendering) return;

         _rendering = true;
         _dc = _visual.RenderOpen();
         PInvokes.RenderHTML(CPPContainer, html);
         _dc.Close();
         _dc = null;
         _rendering = false;
         Loaded = true;
      }

      public void Clear()
      {
         _dc = _visual.RenderOpen();
         _dc.Close();
         _dc = null;
      }

      public void Draw()
      {
         _dc = _visual.RenderOpen();
         PInvokes.Draw(CPPContainer);
         _dc.Close();
         _dc = null;
      }

      public void OnMouseMove(double x, double y)
      {
         if (Loaded)
         {
            if(PInvokes.OnMouseMove(CPPContainer, (int)x, (int)y))
            {
               Draw();
            }
         }
      }

      private void TestDrawing()
      {
         var color = new web_color() { alpha = 255, green = 255 };
         DrawRect(0, 0, 50, 50, GetBrush(ref color));
      }

      protected override void DrawBackground(UIntPtr hdc, string image, background_repeat repeat, ref web_color color, ref position pos)
      {
         if (pos.width > 0 && pos.height > 0)
         {
            DrawRect(pos.x, pos.y, pos.width, pos.height, GetBrush(ref color));
         }
      }

      protected override void DrawBorders(UIntPtr hdc, ref borders borders, ref position draw_pos, bool root)
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
         if (!_brushes.TryGetValue(key, out result))
         {
            result = new SolidColorBrush(Color.FromArgb(color.alpha, color.red, color.green, color.blue));
            _brushes.Add(key, result);
         }

         return result;
      }

      protected override void GetImageSize(string image, ref size size)
      {
         throw new NotImplementedException();
      }
   }
}
