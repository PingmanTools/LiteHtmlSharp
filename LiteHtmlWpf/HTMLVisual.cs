using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LiteHtmlSharp
{
   public delegate void LinkClickedHandler(string url);

   public class HTMLVisual : FrameworkElement
   {
      DrawingVisual _visual;
      WPFContainer _container;
      Canvas _canvas;

      public event LinkClickedHandler LinkClicked;

      public Inputs Inputs
      {
         get
         {
            return _container.Inputs;
         }
      }

      public bool HTMLLoaded
      {
         get { return _container.Loaded; }
      }

      public HTMLVisual(Canvas parent, string masterCSS, IResourceLoader loader)
      {
         _canvas = parent;
         _canvas.ClipToBounds = true;

         _visual = new DrawingVisual();

         _container = new WPFContainer(this, masterCSS, loader);

         this.AddVisualChild(_visual);
         this.AddLogicalChild(_visual);
         parent.Children.Add(this);

         _canvas.SizeChanged += CanvasSizeChanged;
         _canvas.MouseLeave += OnMouseLeave;
         _canvas.MouseMove += OnMouseMove;
         _canvas.MouseLeftButtonDown += OnMouseLeftButtonDown;
         _canvas.MouseLeftButtonUp += OnMouseLeftButtonUp;
      }

      public void FireLink(string url)
      {
         if(LinkClicked != null)
         {
            LinkClicked(url);
         }
      }

      public void SetBackground(Brush brush)
      {
         _canvas.Background = brush;
      }

      public void SetHeight(double height)
      {
         double visibleHeight = ((FrameworkElement)_canvas.Parent).ActualHeight;

         if (height > visibleHeight)
         {
            _canvas.Height = height;
         }
         else
         {
            _canvas.VerticalAlignment = VerticalAlignment.Stretch;
            _canvas.Height = double.NaN;
         }

         OnSizeChanged();
      }

      private void CanvasSizeChanged(object sender, SizeChangedEventArgs e)
      {
         OnSizeChanged();
      }

      private void OnMouseLeave(object sender, MouseEventArgs e)
      {
         _container.OnMouseLeave();
         SetCursor(null);
      }

      private void OnMouseMove(object sender, MouseEventArgs e)
      {
         var pos = e.GetPosition(_canvas);
         _container.OnMouseMove(pos.X, pos.Y);
      }

      private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
      {
         var pos = e.GetPosition(_canvas);
         _container.OnLeftButtonDown(pos.X, pos.Y);
      }

      private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
      {
         var pos = e.GetPosition(_canvas);
         _container.OnLeftButtonUp(pos.X, pos.Y);
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

      public void SetSize(double width, double height)
      {
         _container.OnSizeChanged(width, height);
      }

      public void OnSizeChanged()
      {
         FrameworkElement parent = Parent as FrameworkElement;
         UpdateLayout();
         _container.OnSizeChanged(parent.ActualWidth, parent.ActualHeight);
      }

      public void SetCursor(string cursor)
      {
         if (string.Equals(cursor, "pointer", StringComparison.CurrentCultureIgnoreCase))
         {
            Mouse.OverrideCursor = Cursors.Hand;
         }
         else
         {
            Mouse.OverrideCursor = null;
         }
      }
   }
}