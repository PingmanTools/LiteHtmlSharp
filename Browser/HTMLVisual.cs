using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;

namespace Browser
{
   public class HTMLVisual : FrameworkElement
   {
      DrawingVisual _visual;
      WPFContainer _container;
      public bool HTMLLoaded
      {
         get { return _container.Loaded; }
      }

      public HTMLVisual(Canvas parent)
      {
         _visual = new DrawingVisual();
         _container = new WPFContainer(_visual);

         this.AddVisualChild(_visual);
         this.AddLogicalChild(_visual);
         parent.Children.Add(this);
      }

      protected override int VisualChildrenCount
      {
         get { return 1; }
      }

      protected override Visual GetVisualChild(int index)
      {
         return _visual;
      }

      public void Render(string html)
      {
         _container.Render(html);
      }

      public void Draw()
      {
         _container.Draw();
      }

      public void Clear()
      {
         _container.Clear();
      }

      public void OnMouseMove(double x, double y)
      {
         _container.OnMouseMove(x, y);
      }
   }
}