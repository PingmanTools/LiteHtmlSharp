using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;

namespace LiteHtmlSharp
{
   public class HTMLVisual : FrameworkElement
   {
      DrawingVisual _visual;
      WPFContainer _container;
      Canvas _canvas;

      public bool HTMLLoaded
      {
         get { return _container.Loaded; }
      }

      public HTMLVisual(Canvas parent, string masterCSS, IResourceLoader loader)
      {
         _canvas = parent;
         _visual = new DrawingVisual();

         _container = new WPFContainer(this, masterCSS, loader);

         this.AddVisualChild(_visual);
         this.AddLogicalChild(_visual);
         parent.Children.Add(this);
      }

      public DrawingVisual GetDrawingVisual()
      {
         return _visual;
      }

      public void AddChildControl(FrameworkElement control)
      {
         _canvas.Children.Add(control);
      }

      public void RemoveChildControl(FrameworkElement control)
      {
         _canvas.Children.Remove(control);
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

      public void OnLeftButtonDown(double x, double y)
      {
         _container.OnLeftButtonDown(x, y);
      }

      public void OnLeftButtonUp(double x, double y)
      {
         _container.OnLeftButtonUp(x, y);
      }

      public void OnSizeChanged()
      {
         FrameworkElement parent = Parent as FrameworkElement;
         UpdateLayout();
         _container.OnSizeChanged(parent.ActualWidth, parent.ActualHeight);
      }
   }
}