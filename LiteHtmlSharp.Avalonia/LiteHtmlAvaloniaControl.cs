using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace LiteHtmlSharp.Avalonia
{
    // Custom control that renders HTML background and positions children
    public class HtmlRenderPanel : Control
    {
        public AvaloniaContainer Container { get; set; }
        public LiteHtmlAvaloniaControl HtmlControl { get; set; }
        private readonly Canvas _childCanvas;

        public HtmlRenderPanel()
        {
            _childCanvas = new Canvas();
            LogicalChildren.Add(_childCanvas);
            VisualChildren.Add(_childCanvas);
        }

        public void AddChild(Control control)
        {
            _childCanvas.Children.Add(control);
        }

        public void RemoveChild(Control control)
        {
            _childCanvas.Children.Remove(control);
        }

        public int ChildCount => _childCanvas.Children.Count;

        protected override Size MeasureOverride(Size availableSize)
        {
            // Just measure the child canvas and return what it needs
            var safeSize = new Size(
                double.IsFinite(availableSize.Width) && availableSize.Width > 0 ? availableSize.Width : 1024,
                double.IsFinite(availableSize.Height) && availableSize.Height > 0 ? availableSize.Height : 768
            );

            _childCanvas.Measure(safeSize);
            return _childCanvas.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _childCanvas.Arrange(new Rect(finalSize));
            return finalSize;
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            if (Container == null || !Container.Document.HasRendered || HtmlControl == null) return;

            // Apply viewport transform when HasCustomViewport (like WPF)
            var viewportPoint = HtmlControl.ViewportPoint;
            using (context.PushTransform(Matrix.CreateTranslation(viewportPoint.X, viewportPoint.Y)))
            {
                HtmlControl.RenderHtmlBackground(context);
            }
        }
    }

    public delegate void LinkClickedHandler(string url);

    public class LiteHtmlAvaloniaControl : UserControl, IDisposable
    {
        private Control _controlPanel;

        private ScrollViewer ScrollViewerParent { get; }

        public AvaloniaInputs Inputs { get; private set; } = [];

        public AvaloniaContainer Container { get; private set; }

        public event LinkClickedHandler LinkClicked;

        private bool IsCursorOverLink { get; set; }

        public bool LastPointerDownHandledByHtml { get; private set; }

        /// <summary>
        /// Returns the viewport offset point for rendering transform.
        /// When HasCustomViewport is true, returns the scroll offset; otherwise returns (0,0).
        /// </summary>
        internal Point ViewportPoint
        {
            get
            {
                if (Container.HasCustomViewport)
                {
                    return new Point(Container.ScrollOffset.X, Container.ScrollOffset.Y);
                }
                return new Point(0, 0);
            }
        }

        public LiteHtmlAvaloniaControl(ScrollViewer parent, AvaloniaContainer container, string masterCss, IResourceLoader loader,
            bool createInteractiveElements = true)
        {
            ScrollViewerParent = parent;
            SetupScrollView();
            InitializeCanvas();
            Container = container ?? new AvaloniaContainer(masterCss, loader);
            SetupContainerCallbacks(createInteractiveElements);
        }

        private void InitializeCanvas()
        {
            var htmlRenderPanel = new HtmlRenderPanel
            {
                IsVisible = true,
                ClipToBounds = false,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top
            };

            _controlPanel = htmlRenderPanel;
            Content = _controlPanel;

            Background = Brushes.Transparent;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
        }

        private void SetupScrollView()
        {
            if (ScrollViewerParent == null) return;
            ScrollViewerParent.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            ScrollViewerParent.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            ScrollViewerParent.VerticalAlignment = VerticalAlignment.Stretch;
            ScrollViewerParent.HorizontalContentAlignment = HorizontalAlignment.Left;
            ScrollViewerParent.VerticalContentAlignment = VerticalAlignment.Top;
            ScrollViewerParent.Content = this;
            ScrollViewerParent.ScrollChanged += ScrollParent_ScrollChanged;
            // Note: WPF doesn't subscribe to SizeChanged - only ScrollChanged
        }

        private void SetupContainerCallbacks(bool createInteractiveElements = true)
        {
            Container.RenderHtmlRequested += Container_RenderHtmlRequested;
            Container.AnchorClicked += Container_AnchorClicked;
            Container.DocumentSizeKnown += Container_DocumentSizeKnown;
            Container.Document.ViewElementsNeedLayout += Document_ViewElementsNeedLayout;
            Container.SetCursorCallback = SetCursor;

            if (!createInteractiveElements) return;
            Container.ShouldCreateElementCallback = ShouldCreateElement;
            Container.CreateElementCallback = CreateElement;
        }

        private void Container_AnchorClicked(string link)
        {
            FireLink(link);
        }

        private void ScrollParent_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Match WPF: only call SetViewport, which triggers redraw if viewport changed
            SetViewport();
        }

        private void Document_ViewElementsNeedLayout()
        {
            Dispatcher.UIThread.Post(ProcessInputs, DispatcherPriority.Background);
        }

        private void Container_DocumentSizeKnown(LiteHtmlSize size)
        {
            // Match WPF: control size = document size
            // Consumers (BundleWindow, etc.) can add their own handlers for window resizing
            Width = size.Width;
            Height = size.Height;
            // Match WPF: call SetViewport() which uses ScrollViewer.Viewport
            // If viewport is still 0x0 (async layout not complete), this safely skips
            SetViewport();
        }

        public void Container_RenderHtmlRequested(string html)
        {
            LoadHtml(html);
        }

        private void FireLink(string url)
        {
            LinkClicked?.Invoke(url);
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            if (Container?.Document?.HasRendered == true)
            {
                if (Container.Document.OnMouseLeave())
                {
                    TriggerRedraw();
                }
            }

            SetCursor(null);
            base.OnPointerExited(e);
        }

        private void TriggerRedraw()
        {
            Dispatcher.UIThread.Post(() => _controlPanel?.InvalidateVisual(), DispatcherPriority.Render);
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (Container?.Document?.HasRendered == true)
            {
                var pos = e.GetPosition(this);
                if (pos.X >= 0 && pos.Y >= 0)
                {
                    if (Container.Document.OnMouseMove((int)pos.X, (int)pos.Y))
                    {
                        TriggerRedraw();
                    }
                }
            }

            base.OnPointerMoved(e);
        }

        public void SetCursor(string cursor)
        {
            if (string.Equals(cursor, "pointer", StringComparison.CurrentCultureIgnoreCase))
            {
                Cursor = new Cursor(StandardCursorType.Hand);
                IsCursorOverLink = true;
            }
            else
            {
                Cursor = new Cursor(StandardCursorType.Arrow);
                IsCursorOverLink = false;
            }
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            LastPointerDownHandledByHtml = false;
            if (Container?.Document?.HasRendered == true && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                var pos = e.GetPosition(this);
                var (x, y) = ToDocumentCoords(pos);

                if (x < 0 || y < 0)
                {
                    base.OnPointerPressed(e);
                    return;
                }

                var interactive = IsCursorOverLink || HitInteractiveElement(pos);
                if (interactive)
                {
                    LastPointerDownHandledByHtml = true;
                    e.Handled = true;
                }

                if (Container.Document.OnLeftButtonDown(x, y))
                {
                    TriggerRedraw();
                }
            }

            base.OnPointerPressed(e);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (Container?.Document?.HasRendered == true && e.InitialPressMouseButton == MouseButton.Left)
            {
                var pos = e.GetPosition(this);
                var (x, y) = ToDocumentCoords(pos);

                if (x < 0 || y < 0)
                {
                    base.OnPointerReleased(e);
                    return;
                }

                if (Container.Document.OnLeftButtonUp(x, y))
                {
                    TriggerRedraw();
                }
            }

            if (!IsCursorOverLink)
            {
                LastPointerDownHandledByHtml = false;
            }

            base.OnPointerReleased(e);
        }

        public void LoadHtml(string html)
        {
            ClearInputs();
            Container.Document.CreateFromString(html);

            if (_controlPanel is HtmlRenderPanel htmlRenderPanel)
            {
                htmlRenderPanel.Container = Container;
                htmlRenderPanel.HtmlControl = this;
            }

            // Match WPF: just call CheckViewportChange, let it handle viewport
            Container.CheckViewportChange(forceRender: true);

            Dispatcher.UIThread.Post(ProcessInputs, DispatcherPriority.Background);
            TriggerRedraw();
        }

        private void SetViewport(bool forceRedraw = false)
        {
            if (ScrollViewerParent == null) return;

            var viewportWidth = ScrollViewerParent.Viewport.Width;
            var viewportHeight = ScrollViewerParent.Viewport.Height;

            if (viewportWidth > 0 && viewportHeight > 0)
            {
                if (Container.SetViewport(
                    new LiteHtmlPoint(ScrollViewerParent.Offset.X, ScrollViewerParent.Offset.Y),
                    new LiteHtmlSize(viewportWidth, viewportHeight)) || forceRedraw)
                {
                    TriggerRedraw();
                }
            }
            else if (forceRedraw)
            {
                TriggerRedraw();
            }
        }

        public void RenderHtmlBackground(DrawingContext context)
        {
            var renderBounds = new Rect(0, 0, Bounds.Width, Bounds.Height);
            context.FillRectangle(Brushes.Transparent, renderBounds);

            if (!Container.Document.HasRendered)
            {
                var notRenderedText = new FormattedText(
                    "Loading HTML...",
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(FontFamily.Default),
                    14,
                    Brushes.Gray);
                context.DrawText(notRenderedText, new Point(10, 10));
                return;
            }

            Container.DrawingContext = context;
            Container.Draw();
            Container.DrawingContext = null;
        }

        private static bool ShouldCreateElement(string tag)
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

        private int CreateElement(string tag, string attributes, ElementInfo elementInfo)
        {
            AvaloniaInput input;

            switch (tag.ToLowerInvariant())
            {
                case "input":
                    {
                        input = new AvaloniaInput(InputType.Textbox)
                        {
                            Element = new TextBox()
                        };
                        break;
                    }
                case "button":
                    {
                        input = new AvaloniaInput(InputType.Button);
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

            AddChildControl(input.Element);

            Inputs.Add(input);
            return input.ID;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: AvaloniaInput input }) return;
            if (!string.IsNullOrEmpty(input.Href))
            {
                FireLink(input.Href);
            }
        }

        private void ProcessInputs()
        {
            foreach (var input in Inputs)
            {
                var info = Container.Document.GetElementInfo(input.ID);

                input.Element.Width = info.Width;
                input.Element.Height = info.Height > 0 ? info.Height : (input.Type == InputType.Button ? 25.0 : 22.0);

                input.Element.Margin = new Thickness(0);

                if (info.PosX > 999999)
                {
                    info.PosX = 0;
                }

                Canvas.SetLeft(input.Element, info.PosX);
                Canvas.SetTop(input.Element, info.PosY);

                if (input.AttributesSetup) continue;
                input.AttributesSetup = true;
                input.SetupAttributes(info.Attributes);
            }
        }

        public void AddChildControl(Control control)
        {
            if (_controlPanel is not HtmlRenderPanel htmlRenderPanel) return;
            htmlRenderPanel.AddChild(control);
            control.ZIndex = 1000;

            control.IsVisible = true;
            control.Opacity = 1.0;

            switch (control)
            {
                case TextBox textBox:
                    textBox.BorderThickness = new Thickness(1);
                    break;
                case Button button:
                    button.BorderThickness = new Thickness(1);
                    break;
            }

            // Force immediate layout like WPF's UpdateLayout()
            InvalidateMeasure();
        }

        private void ClearInputs()
        {
            if (_controlPanel is HtmlRenderPanel htmlRenderPanel)
            {
                foreach (var input in Inputs)
                {
                    if (input.IsPlaced)
                    {
                        htmlRenderPanel.RemoveChild(input.Element);
                        input.IsPlaced = false;
                    }
                }
            }

            Inputs.Clear();
        }

        public void RemoveChildControl(Control control)
        {
            if (_controlPanel is HtmlRenderPanel htmlRenderPanel)
            {
                htmlRenderPanel.RemoveChild(control);
            }
        }

        public void Dispose()
        {
            Container.RenderHtmlRequested -= Container_RenderHtmlRequested;
            Container.AnchorClicked -= Container_AnchorClicked;
            Container.DocumentSizeKnown -= Container_DocumentSizeKnown;
            Container.Document.ViewElementsNeedLayout -= Document_ViewElementsNeedLayout;

            if (ScrollViewerParent != null)
            {
                ScrollViewerParent.ScrollChanged -= ScrollParent_ScrollChanged;
            }

            ClearInputs();
        }

        private (int X, int Y) ToDocumentCoords(Point p)
        {
            if (ScrollViewerParent != null && !Container.HasCustomViewport)
            {
                return ((int)(p.X + ScrollViewerParent.Offset.X), (int)(p.Y + ScrollViewerParent.Offset.Y));
            }

            return ((int)p.X, (int)p.Y);
        }

        private bool HitInteractiveElement(Point pos)
        {
            var (docX, docY) = ToDocumentCoords(pos);
            foreach (var input in Inputs)
            {
                var left = Canvas.GetLeft(input.Element);
                var top = Canvas.GetTop(input.Element);
                var width = input.Element.Bounds.Width > 0 ? input.Element.Bounds.Width : input.Element.Width;
                var height = input.Element.Bounds.Height > 0 ? input.Element.Bounds.Height : input.Element.Height;
                if (width <= 0 || height <= 0) continue;
                if (docX >= left && docX <= left + width && docY >= top && docY <= top + height)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
