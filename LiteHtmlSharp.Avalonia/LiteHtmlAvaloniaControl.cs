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
            // Let the child canvas use the full size - no manual scroll transformation
            _childCanvas.Arrange(new Rect(finalSize));
            return finalSize;
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            // First render HTML background if available
            if (Container == null || !Container.Document.HasRendered || HtmlControl == null) return;
            using (context.PushTransform(Matrix.Identity))
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

        public LiteHtmlAvaloniaControl(ScrollViewer parent, string masterCss, IResourceLoader loader, bool createInteractiveElements = true)
        {
            ScrollViewerParent = parent;
            SetupScrollView();
            InitializeCanvas();
            Container = new AvaloniaContainer(masterCss, loader);
            SetupContainerCallbacks(createInteractiveElements);
        }

        private void InitializeCanvas()
        {
            // Create HtmlRenderPanel that can render HTML background
            var htmlRenderPanel = new HtmlRenderPanel
            {
                IsVisible = true,
                ClipToBounds = false
            };

            _controlPanel = htmlRenderPanel;
            Content = _controlPanel;

            // Make sure this control is visible and fills available space
            Background = Brushes.Transparent; // Let HTML background show through
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;

            // Let it size naturally - no artificial minimums

            Console.WriteLine("LiteHtmlAvaloniaControl initialized");
        }

        private void SetupScrollView()
        {
            if (ScrollViewerParent == null) return;
            ScrollViewerParent.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            ScrollViewerParent.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            ScrollViewerParent.Content = this;
            ScrollViewerParent.ScrollChanged += ScrollParent_ScrollChanged;
            ScrollViewerParent.SizeChanged += ScrollParent_SizeChanged;
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
            SetViewport();
            // Just trigger a redraw - no manual positioning needed
            _controlPanel?.InvalidateVisual();
        }

        private void ScrollParent_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Re-layout HTML when ScrollViewer size changes
            if (!Container.Document.HasLoadedHtml) return;
            var viewportWidth = e.NewSize.Width;
            // Re-layout HTML with new width, use minimal height for natural layout
            Container.SetViewport(new LiteHtmlPoint(0, 0), new LiteHtmlSize(viewportWidth, 1));
            Container.CheckViewportChange(forceRender: true);
            // Don't override the control size here - let DocumentSizeKnown handle it
        }

        private void Document_ViewElementsNeedLayout()
        {
            // Don't process inputs during render - defer to next UI thread cycle
            Dispatcher.UIThread.Post(ProcessInputs, DispatcherPriority.Background);
        }

        private void Container_DocumentSizeKnown(LiteHtmlSize size)
        {
            // Use the document's natural size, but ensure we have reasonable viewport width
            var viewportWidth = ScrollViewerParent?.Viewport.Width ?? size.Width;
            if (viewportWidth <= 0) viewportWidth = size.Width; // Use document width as fallback

            // Set the control size to the actual content dimensions
            Width = Math.Max(viewportWidth, size.Width);
            Height = size.Height;

            // Update viewport to match what we're actually rendering
            Container.SetViewport(new LiteHtmlPoint(0, 0), new LiteHtmlSize(Width, Height));

            Console.WriteLine($"Document size known: {size.Width}x{size.Height}, control size set to: {Width}x{Height}");
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
            if (Container.Document.HasRendered)
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
            Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Render);
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (Container.Document.HasRendered)
            {
                var pos = e.GetPosition(this);
                if (Container.Document.OnMouseMove((int)pos.X, (int)pos.Y))
                {
                    TriggerRedraw();
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
            if (Container.Document.HasRendered && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                var pos = e.GetPosition(this);
                var (x, y) = ToDocumentCoords(pos);
                // Determine if this press should be consumed by HTML (link or interactive element)
                var interactive = IsCursorOverLink || HitInteractiveElement(pos);
                if (interactive)
                {
                    LastPointerDownHandledByHtml = true; // so parent window knows not to start drag
                    e.Handled = true; // stop bubbling to window for drag
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
            if (Container.Document.HasRendered && e.InitialPressMouseButton == MouseButton.Left)
            {
                var pos = e.GetPosition(this);
                var (x, y) = ToDocumentCoords(pos);
                if (Container.Document.OnLeftButtonUp(x, y))
                {
                    TriggerRedraw();
                }
            }
            // Reset flag after a full click sequence (release) unless still over link (allow drag next time if moved off)
            if (!IsCursorOverLink)
            {
                LastPointerDownHandledByHtml = false;
            }
            base.OnPointerReleased(e);
        }


        private void LoadHtml(string html)
        {
            Console.WriteLine("LoadHtml called");
            ClearInputs();
            Container.Document.CreateFromString(html);

            // Set up HtmlRenderPanel references for HTML rendering
            if (_controlPanel is HtmlRenderPanel htmlRenderPanel)
            {
                htmlRenderPanel.Container = Container;
                htmlRenderPanel.HtmlControl = this;
            }

            // Set viewport size to match ScrollViewer dimensions for proper layout
            var viewportWidth = ScrollViewerParent?.Viewport.Width ?? 0;
            if (viewportWidth <= 0) viewportWidth = 1024; // Reasonable default for initial layout
            var size = new LiteHtmlSize(viewportWidth, 1); // Use minimal height, let HTML calculate natural size

            Container.SetViewport(new LiteHtmlPoint(0, 0), size);
            Container.CheckViewportChange(forceRender: true);

            Console.WriteLine($"HTML loaded, document has rendered: {Container.Document.HasRendered}");

            // Process inputs immediately after HTML is loaded and rendered
            Dispatcher.UIThread.Post(ProcessInputs, DispatcherPriority.Background);

            TriggerRedraw();
        }

        private void SetViewport(bool forceRedraw = false)
        {
            // Don't override viewport size here - it should be managed by load/resize events
            // This method is mainly for triggering redraws on scroll
            if (forceRedraw)
            {
                TriggerRedraw();
            }
        }

        // UserControl doesn't use Render override - need different approach for HTML rendering

        public void RenderHtmlBackground(DrawingContext context)
        {
            // Fill the entire control area with white background
            var renderBounds = new Rect(0, 0, Bounds.Width, Bounds.Height);
            context.FillRectangle(Brushes.White, renderBounds);

            if (!Container.Document.HasRendered)
            {
                // Show loading message
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

            // No scroll transformation needed - let ScrollViewer handle scrolling
            Container.DrawingContext = context;
            Container.Draw();
            Container.DrawingContext = null;
        }

        // public Point GetCurrentScrollOffset()
        // {
        //     if (ScrollViewerParent != null && ScrollParentIsLayedOut)
        //     {
        //         return new Point(ScrollViewerParent.Offset.X, ScrollViewerParent.Offset.Y);
        //     }
        //     return new Point(0, 0);
        // }
        //
        // Point ViewportPoint
        // {
        //     get
        //     {
        //         if (Container.HasCustomViewport)
        //         {
        //             return new Point(Container.ScrollOffset.X, Container.ScrollOffset.Y);
        //         }
        //         else
        //         {
        //             return new Point(0, 0);
        //         }
        //     }
        // }

        private static bool ShouldCreateElement(string tag)
        {
            Console.WriteLine($"ShouldCreateElement called for tag: {tag}");
            switch (tag.ToLowerInvariant())
            {
                case "input":
                case "button":
                    Console.WriteLine($"Creating element for tag: {tag}");
                    return true;
                default:
                    return false;
            }
        }

        private int CreateElement(string tag, string attributes, ElementInfo elementInfo)
        {
            Console.WriteLine($"CreateElement called for tag: {tag}, attributes: {attributes}");
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
            input.SetupAttributes(attributes); // This sets the button content
            input.AttributesSetup = true;
            input.IsPlaced = true;

            // Add to visual tree - let HTML engine determine sizing
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
            Console.WriteLine($"ProcessInputs called, {Inputs.Count} inputs to process");

            foreach (var input in Inputs)
            {
                var info = Container.Document.GetElementInfo(input.ID);

                Console.WriteLine($"ElementInfo for {input.ID}: PosX={info.PosX}, PosY={info.PosY}, Width={info.Width}, Height={info.Height}");

                // Use exact HTML dimensions - they should now include our measured sizes
                input.Element.Width = info.Width;
                input.Element.Height = info.Height > 0 ? info.Height : (input.Type == InputType.Button ? 25.0 : 22.0);

                // Clear margins for pixel-perfect positioning
                input.Element.Margin = new Thickness(0);

                // Handle edge case from WPF implementation
                if (info.PosX > 999999)
                {
                    info.PosX = 0;
                }

                // Use exact positioning from HTML layout - no adjustments
                Canvas.SetLeft(input.Element, info.PosX);
                Canvas.SetTop(input.Element, info.PosY);

                Console.WriteLine($"Set Canvas position for {input.Element.GetType().Name}: Left={Canvas.GetLeft(input.Element)}, Top={Canvas.GetTop(input.Element)}");
            }
        }

        public void AddChildControl(Control control)
        {
            Console.WriteLine($"AddChildControl called for {control.GetType().Name}");

            // Add control to the HtmlRenderPanel
            if (_controlPanel is not HtmlRenderPanel htmlRenderPanel) return;
            htmlRenderPanel.AddChild(control);
            control.ZIndex = 1000; // Ensure controls appear on top

            // Make sure control is visible and has proper styling
            control.IsVisible = true;
            control.Opacity = 1.0;

            switch (control)
            {
                // Add some basic styling to make controls more visible
                case TextBox textBox:
                    textBox.Background = Brushes.White;
                    textBox.BorderBrush = Brushes.Black;
                    textBox.BorderThickness = new Thickness(1);
                    break;
                case Button button:
                    button.Background = Brushes.LightGray;
                    button.BorderBrush = Brushes.Black;
                    button.BorderThickness = new Thickness(1);
                    break;
            }

            Console.WriteLine($"Added control to panel, total children: {htmlRenderPanel.ChildCount}");
            Console.WriteLine($"Control bounds: {control.Bounds}, IsVisible: {control.IsVisible}");
        }

        private void ClearInputs()
        {
            if (_controlPanel is HtmlRenderPanel htmlRenderPanel)
            {
                foreach (var input in Inputs)
                {
                    htmlRenderPanel.RemoveChild(input.Element);
                }
            }
            Inputs.Clear();
        }

        public void RemoveChildControl(Control control)
        {
            if(_controlPanel is HtmlRenderPanel htmlRenderPanel)
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
                ScrollViewerParent.SizeChanged -= ScrollParent_SizeChanged;
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

