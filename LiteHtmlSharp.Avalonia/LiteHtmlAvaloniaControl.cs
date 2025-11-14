using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Threading;

namespace LiteHtmlSharp.Avalonia
{
    // Custom control that renders HTML background and positions children
    public class HtmlRenderPanel : Control
    {
        public AvaloniaContainer Container { get; set; }
        public LiteHtmlAvaloniaControl HtmlControl { get; set; }
        private Canvas _childCanvas;

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

        public void ClearChildren()
        {
            _childCanvas.Children.Clear();
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
            // First render HTML background if available
            if (Container != null && Container.Document.HasRendered && HtmlControl != null)
            {
                HtmlControl.RenderHtmlBackground(context);
            }
            
            base.Render(context);
        }

        public void NotifyContentSizeChanged()
        {
            // Trigger re-measurement when content size changes
            InvalidateMeasure();
        }
    }

    public delegate void LinkClickedHandler(string url);

    public class LiteHtmlAvaloniaControl : UserControl, IDisposable
    {
        ScrollViewer _scrollParent;
        Control _controlPanel;

        public ScrollViewer ScrollViewerParent => _scrollParent;

        public AvaloniaInputs Inputs { get; private set; } = new AvaloniaInputs();

        public AvaloniaContainer Container { get; private set; }

        public event LinkClickedHandler LinkClicked;

        public bool HTMLLoaded
        {
            get { return Container.Loaded; }
        }

        public LiteHtmlAvaloniaControl()
        {
            InitializeCanvas();
            Container = new AvaloniaContainer(IncludedMasterCss.CSS, (url) => null, (url) => null);
            SetupContainerCallbacks();
        }

        public LiteHtmlAvaloniaControl(string masterCSS, IResourceLoader loader)
        {
            InitializeCanvas();
            Container = new AvaloniaContainer(masterCSS, loader);
            SetupContainerCallbacks();
        }

        public LiteHtmlAvaloniaControl(ScrollViewer parent, string masterCSS, IResourceLoader loader, bool createInteractiveElements = true)
        {
            _scrollParent = parent;
            SetupScrollView();
            InitializeCanvas();
            Container = new AvaloniaContainer(masterCSS, loader);
            SetupContainerCallbacks(createInteractiveElements);
        }

        public LiteHtmlAvaloniaControl(ScrollViewer parent, AvaloniaContainer container, bool createInteractiveElements = true)
        {
            _scrollParent = parent;
            SetupScrollView();
            InitializeCanvas();
            Container = container;
            SetupContainerCallbacks(createInteractiveElements);
        }

        public LiteHtmlAvaloniaControl(AvaloniaContainer container)
        {
            InitializeCanvas();
            Container = container;
            SetupContainerCallbacks();
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
            this.Content = _controlPanel;
            
            // Make sure this control is visible and fills available space
            this.Background = Brushes.Transparent; // Let HTML background show through
            this.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.VerticalAlignment = VerticalAlignment.Stretch;
            
            // Let it size naturally - no artificial minimums
            
            System.Diagnostics.Debug.WriteLine($"LiteHtmlAvaloniaControl initialized");
        }

        void SetupScrollView()
        {
            if (_scrollParent != null)
            {
                _scrollParent.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                _scrollParent.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                _scrollParent.Content = this;
                _scrollParent.ScrollChanged += ScrollParent_ScrollChanged;
                _scrollParent.SizeChanged += ScrollParent_SizeChanged;
            }
        }

        void SetupContainerCallbacks(bool createInteractiveElements = true)
        {
            Container.RenderHtmlRequested += Container_RenderHtmlRequested;
            Container.AnchorClicked += Container_AnchorClicked;
            Container.DocumentSizeKnown += Container_DocumentSizeKnown;
            Container.Document.ViewElementsNeedLayout += Document_ViewElementsNeedLayout;
            Container.SetCursorCallback = cursor => SetCursor(cursor);

            if (createInteractiveElements)
            {
                Container.ShouldCreateElementCallback = ShouldCreateElement;
                Container.CreateElementCallback = CreateElement;
            }
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
            if (Container.Document.HasLoadedHtml)
            {
                var viewportWidth = e.NewSize.Width;
                // Re-layout HTML with new width, use minimal height for natural layout
                Container.SetViewport(new LiteHtmlPoint(0, 0), new LiteHtmlSize(viewportWidth, 1));
                Container.CheckViewportChange(forceRender: true);
                // Don't override the control size here - let DocumentSizeKnown handle it
            }
        }

        private void Document_ViewElementsNeedLayout()
        {
            // Don't process inputs during render - defer to next UI thread cycle
            Dispatcher.UIThread.Post(() => ProcessInputs(), DispatcherPriority.Background);
        }

        private void Container_DocumentSizeKnown(LiteHtmlSize size)
        {
            // Use the document's natural size, but ensure we have reasonable viewport width
            var viewportWidth = _scrollParent?.Viewport.Width ?? size.Width;
            if (viewportWidth <= 0) viewportWidth = size.Width; // Use document width as fallback
            
            // Set the control size to the actual content dimensions
            this.Width = Math.Max(viewportWidth, size.Width);
            this.Height = size.Height;
            
            // Update viewport to match what we're actually rendering
            Container.SetViewport(new LiteHtmlPoint(0, 0), new LiteHtmlSize(this.Width, this.Height));
            
            System.Diagnostics.Debug.WriteLine($"Document size known: {size.Width}x{size.Height}, control size set to: {this.Width}x{this.Height}");
        }

        private void Container_RenderHtmlRequested(string html)
        {
            LoadHtml(html);
        }

        public void FireLink(string url)
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

        void TriggerRedraw()
        {
            Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Render);
        }

        // Remove custom MeasureOverride - let UserControl handle layout naturally

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

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (Container.Document.HasRendered && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                var pos = e.GetPosition(this);
                if (Container.Document.OnLeftButtonDown((int)pos.X, (int)pos.Y))
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
                if (Container.Document.OnLeftButtonUp((int)pos.X, (int)pos.Y))
                {
                    TriggerRedraw();
                }
            }
            base.OnPointerReleased(e);
        }

        public void SetCursor(string cursor)
        {
            if (string.Equals(cursor, "pointer", StringComparison.CurrentCultureIgnoreCase))
            {
                Cursor = new Cursor(StandardCursorType.Hand);
            }
            else
            {
                Cursor = new Cursor(StandardCursorType.Arrow);
            }
        }

        public void LoadHtml(string html)
        {
            System.Diagnostics.Debug.WriteLine("LoadHtml called");
            ClearInputs();
            Container.Document.CreateFromString(html);
            
            // Set up HtmlRenderPanel references for HTML rendering
            if (_controlPanel is HtmlRenderPanel htmlRenderPanel)
            {
                htmlRenderPanel.Container = Container;
                htmlRenderPanel.HtmlControl = this;
            }
            
            // Set viewport size to match ScrollViewer dimensions for proper layout
            var viewportWidth = _scrollParent?.Viewport.Width ?? 0;
            if (viewportWidth <= 0) viewportWidth = 1024; // Reasonable default for initial layout
            var size = new LiteHtmlSize(viewportWidth, 1); // Use minimal height, let HTML calculate natural size
            
            Container.SetViewport(new LiteHtmlPoint(0, 0), size);
            Container.CheckViewportChange(forceRender: true);
            
            System.Diagnostics.Debug.WriteLine($"HTML loaded, document has rendered: {Container.Document.HasRendered}");
            
            // Process inputs immediately after HTML is loaded and rendered
            Dispatcher.UIThread.Post(() => ProcessInputs(), DispatcherPriority.Background);
            
            TriggerRedraw();
        }

        public void SetViewport(bool forceRedraw = false)
        {
            // Don't override viewport size here - it should be managed by load/resize events
            // This method is mainly for triggering redraws on scroll
            if (forceRedraw)
            {
                TriggerRedraw();
            }
        }

        bool ScrollParentIsLayedOut => _scrollParent?.Viewport.Width > 0 && _scrollParent?.Viewport.Height > 0;

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

        public Point GetCurrentScrollOffset()
        {
            if (_scrollParent != null && ScrollParentIsLayedOut)
            {
                return new Point(_scrollParent.Offset.X, _scrollParent.Offset.Y);
            }
            return new Point(0, 0);
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

        bool ShouldCreateElement(string tag)
        {
            System.Diagnostics.Debug.WriteLine($"ShouldCreateElement called for tag: {tag}");
            switch (tag.ToLowerInvariant())
            {
                case "input":
                case "button":
                    System.Diagnostics.Debug.WriteLine($"Creating element for tag: {tag}");
                    return true;
                default:
                    return false;
            }
        }

        int CreateElement(string tag, string attributes, ElementInfo elementInfo)
        {
            System.Diagnostics.Debug.WriteLine($"CreateElement called for tag: {tag}, attributes: {attributes}");
            AvaloniaInput input = null;

            switch (tag.ToLowerInvariant())
            {
                case "input":
                    {
                        input = new AvaloniaInput(InputType.Textbox);
                        input.Element = new TextBox();
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
            if (sender is Button button && button.Tag is AvaloniaInput input)
            {
                if (!string.IsNullOrEmpty(input.Href))
                {
                    FireLink(input.Href);
                }
            }
        }

        private void ProcessInputs()
        {
            System.Diagnostics.Debug.WriteLine($"ProcessInputs called, {Inputs.Count} inputs to process");
            
            foreach (var input in Inputs)
            {
                ElementInfo info = Container.Document.GetElementInfo(input.ID);
                
                System.Diagnostics.Debug.WriteLine($"ElementInfo for {input.ID}: PosX={info.PosX}, PosY={info.PosY}, Width={info.Width}, Height={info.Height}");
                
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
                
                System.Diagnostics.Debug.WriteLine($"Set Canvas position for {input.Element.GetType().Name}: Left={Canvas.GetLeft(input.Element)}, Top={Canvas.GetTop(input.Element)}");
            }
        }

        private void AddChildControl(Control control)
        {
            System.Diagnostics.Debug.WriteLine($"AddChildControl called for {control.GetType().Name}");
            
            // Add control to the HtmlRenderPanel
            if (_controlPanel is HtmlRenderPanel htmlRenderPanel)
            {
                htmlRenderPanel.AddChild(control);
                control.ZIndex = 1000; // Ensure controls appear on top
                
                // Make sure control is visible and has proper styling
                control.IsVisible = true;
                control.Opacity = 1.0;
                
                // Add some basic styling to make controls more visible
                if (control is TextBox textBox)
                {
                    textBox.Background = Brushes.White;
                    textBox.BorderBrush = Brushes.Black;
                    textBox.BorderThickness = new Thickness(1);
                }
                else if (control is Button button)
                {
                    button.Background = Brushes.LightGray;
                    button.BorderBrush = Brushes.Black;
                    button.BorderThickness = new Thickness(1);
                }
                
                System.Diagnostics.Debug.WriteLine($"Added control to panel, total children: {htmlRenderPanel.ChildCount}");
                System.Diagnostics.Debug.WriteLine($"Control bounds: {control.Bounds}, IsVisible: {control.IsVisible}");
            }
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

        public void Dispose()
        {
            Container.RenderHtmlRequested -= Container_RenderHtmlRequested;
            Container.AnchorClicked -= Container_AnchorClicked;
            Container.DocumentSizeKnown -= Container_DocumentSizeKnown;
            Container.Document.ViewElementsNeedLayout -= Document_ViewElementsNeedLayout;
            
            if (_scrollParent != null)
            {
                _scrollParent.ScrollChanged -= ScrollParent_ScrollChanged;
                _scrollParent.SizeChanged -= ScrollParent_SizeChanged;
            }
            
            ClearInputs();
        }
    }
}