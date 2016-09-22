using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LiteHtmlSharp.Wpf
{
   public delegate void LinkClickedHandler(string url);

   public class LiteHtmlPanel : Canvas, IDisposable
   {
      ScrollViewer _scrollParent;

      public Inputs Inputs { get; private set; } = new Inputs();

      public WpfContainer Container { get; private set; }

      public event LinkClickedHandler LinkClicked;

      public bool HTMLLoaded
      {
         get { return Container.Loaded; }
      }

      public LiteHtmlPanel(ScrollViewer parent, string masterCSS, IResourceLoader loader, bool createdOldStyleControls = true)
      {
         _scrollParent = parent;
         SetupScrollView();
         Container = new WpfContainer(masterCSS, loader);
         SetupContainerCallbacks(createdOldStyleControls);
      }

      public LiteHtmlPanel(ScrollViewer parent, WpfContainer container, bool createOldStyleControls = true)
      {
         _scrollParent = parent;
         SetupScrollView();
         Container = container;
         SetupContainerCallbacks(createOldStyleControls);
      }

      void SetupScrollView()
      {
         _scrollParent.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
         _scrollParent.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
         _scrollParent.CanContentScroll = false;
         _scrollParent.Content = this;
         _scrollParent.ScrollChanged += ScrollParent_ScrollChanged;
      }

      void SetupContainerCallbacks(bool createOldStyleControls)
      {
         Container.RenderHtmlRequested += Container_RenderHtmlRequested;

         Container.AnchorClicked += Container_AnchorClicked;
         Container.DocumentSizeKnown += Container_DocumentSizeKnown;
         Container.Document.ViewElementsNeedLayout += Document_ViewElementsNeedLayout;
         Container.SetCursorCallback = cursor => SetCursor(cursor);

         if (createOldStyleControls)
         {
            Container.ShouldCreateElementCallback = ShouldCreateElement;
            Container.CreateElementCallback = CreateElement;
         }
      }

      private void Container_AnchorClicked(string link)
      {
         FireLink(link);
      }

      private void Document_ViewElementsNeedLayout()
      {
         ProcessInputs();
      }

      private void Container_DocumentSizeKnown(LiteHtmlSize size)
      {
         Console.WriteLine("doc size known - setting viewport");
         Width = size.Width;
         Height = size.Height;
         SetViewport();
      }

      private void ScrollParent_ScrollChanged(object sender, ScrollChangedEventArgs e)
      {
         SetViewport();
      }

      bool ShouldCreateElement(string tag)
      {
         switch (tag.ToLowerInvariant())
         {
            case "input":
            case "button":
               return true;
            default:
               return false;
         }
      }

      int CreateElement(string tag, string attributes, ElementInfo elementInfo)
      {
         Input input = null;

         switch (tag.ToLowerInvariant())
         {
            case "input":
               {
                  input = new Input(InputType.Textbox);
                  input.Element = new TextBox();
                  break;
               }
            case "button":
               {
                  input = new Input(InputType.Button);
                  var button = new Button();
                  button.Click += Button_Click;
                  input.Element = button;
                  break;
               }
            default:
               return 0;
         }

         input.ID = Inputs.Count + 1;
         input.Element.Tag = input;
         input.Element.VerticalAlignment = VerticalAlignment.Top;
         input.Element.HorizontalAlignment = HorizontalAlignment.Left;
         input.SetupAttributes(attributes);
         input.AttributesSetup = true;
         input.IsPlaced = true;
         Inputs.Add(input);
         AddChildControl(input.Element);
         return input.ID;
      }

      private void ProcessInputs()
      {
         foreach (var input in Inputs)
         {
            ElementInfo info = Container.Document.GetElementInfo(input.ID);
            input.Element.Width = info.Width;
            if (info.Height > 0)
            {
               input.Element.Height = info.Height;
            }
            if (info.PosX > 999999)
            {
               info.PosX = 0;
            }
            var elementRect = new Rect(info.PosX, info.PosY, info.Width, info.Height);
            //input.Element.Arrange(elementRect);
            //input.Element.Margin = new Thickness(info.PosX, info.PosY, 0, 0);
            input.Element.Width = info.Width;
            input.Element.Height = info.Height;
            input.Element.RenderTransform = new TranslateTransform(info.PosX, info.PosY);
            //SetLeft(this, info.PosX);
            //SetTop(this, info.PosY);
            if (!input.AttributesSetup)
            {
               input.AttributesSetup = true;
               input.SetupAttributes(info.Attributes);
            }
         }
      }

      public void AddChildControl(FrameworkElement control)
      {
         Children.Add(control);
         UpdateLayout();
         //InternalChildren.Add(control);
      }

      public void RemoveChildControl(FrameworkElement control)
      {
         Children.Remove(control);
         //InternalChildren.Remove(control);
      }

      private void Container_RenderHtmlRequested(string html)
      {
         LoadHtml(html);
      }

      public void FireLink(string url)
      {
         LinkClicked?.Invoke(url);
      }

      protected override void OnMouseLeave(MouseEventArgs e)
      {
         if (Container.Document.HasRendered)
         {
            if (Container.Document.OnMouseLeave())
            {
               TriggerRedraw();
            }
         }
         Mouse.OverrideCursor = null;
         base.OnMouseLeave(e);
      }

      void TriggerRedraw()
      {
         InvalidateVisual();
         //UpdateLayout();
      }

      protected override void OnMouseMove(MouseEventArgs e)
      {
         if (Container.Document.HasRendered)
         {
            var pos = e.GetPosition(this);
            if (Container.Document.OnMouseMove((int)pos.X, (int)pos.Y))
            {
               TriggerRedraw();
            }
         }

         base.OnMouseMove(e);
      }

      protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
      {
         if (Container.Document.HasRendered)
         {
            var pos = e.GetPosition(this);
            if (Container.Document.OnLeftButtonDown((int)pos.X, (int)pos.Y))
            {
               TriggerRedraw();
            }
         }
         
         base.OnMouseLeftButtonDown(e);
      }

      protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
      {
         if (Container.Document.HasRendered)
         {
            var pos = e.GetPosition(this);
            if (Container.Document.OnLeftButtonUp((int)pos.X, (int)pos.Y))
            {
               TriggerRedraw();
            }
         }
         base.OnMouseLeftButtonUp(e);
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

      private void Button_Click(object sender, RoutedEventArgs e)
      {
         Input input = ((Button)sender).Tag as Input;
         Container.TriggerAnchorClicked(input.Href);
      }

      public void ClearInputs()
      {
         foreach (var input in Inputs)
         {
            if (input.IsPlaced)
            {
               RemoveChildControl(input.Element);
               input.IsPlaced = false;
            }
         }
         Inputs.Clear();
      }

      public void LoadHtml(string html)
      {
         ClearInputs();
         Container.ResetViewport();
         Container.Document.CreateFromString(html);
         /*if (ScrollParentIsLayedOut)
         {
            Container.Size = new LiteHtmlSize(_scrollParent.ViewportWidth, _scrollParent.ViewportHeight);
         }*/
         Container.CheckViewportChange(forceRender: true);
         TriggerRedraw();
      }

      bool ScrollParentIsLayedOut => _scrollParent.ViewportWidth != 0 && _scrollParent.ViewportHeight != 0;

      public void SetViewport()
      {
         if (Container.SetViewport(new LiteHtmlPoint(_scrollParent.HorizontalOffset, _scrollParent.VerticalOffset), new LiteHtmlSize(_scrollParent.ViewportWidth, _scrollParent.ViewportHeight)))
         {
            TriggerRedraw();
         }
      }

      Point ViewportPoint
      {
         get
         {
            if (Container.HasCustomViewport)
            {
               return new Point(Container.ScrollOffset.X, Container.ScrollOffset.Y);
            }
            else
            {
               return new Point(0, 0);
            }
         }
      }

      protected override void OnRender(DrawingContext dc)
      {

         if (!Container.Document.HasRendered)
         {
            return;
         }

         Console.WriteLine($"OnRender called, children: {Children.Count}");
         var point = ViewportPoint;
         dc.PushTransform(new TranslateTransform(point.X, point.Y));

         //Container.CheckViewportChange();
         Container.DrawingContext = dc;
         Container.Draw();
         Container.DrawingContext = null;
         
         base.OnRender(dc);
      }

      public void Dispose()
      {
         _scrollParent.ScrollChanged -= ScrollParent_ScrollChanged;

         Container.RenderHtmlRequested -= Container_RenderHtmlRequested;
         Container.AnchorClicked -= Container_AnchorClicked;
         Container.DocumentSizeKnown -= Container_DocumentSizeKnown;
         Container.Document.ViewElementsNeedLayout -= Document_ViewElementsNeedLayout;
      }

      /*protected override Size ArrangeOverride(Size finalSize)
      {
         Console.WriteLine("arrange called for " + InternalChildren.Count + " children");
         var size = base.ArrangeOverride(finalSize);
         ArrangeChildrenCallback?.Invoke();
         return size;
      }*/


   }
}