using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
            // Create HtmlRenderPanel that can render HTML background
            var htmlRenderPanel = new HtmlRenderPanel
            {
                IsVisible = true,
                ClipToBounds = false,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top
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
            ScrollViewerParent.VerticalAlignment = VerticalAlignment.Stretch;
            ScrollViewerParent.HorizontalContentAlignment = HorizontalAlignment.Left;
            ScrollViewerParent.VerticalContentAlignment = VerticalAlignment.Top;
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
            try
            {
                FireLink(link);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling anchor click for '{link}': {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
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
            var viewportHeight = e.NewSize.Height;
            // Re-layout HTML with new viewport size
            Container.SetViewport(
                new LiteHtmlPoint(ScrollViewerParent?.Offset.X ?? 0, ScrollViewerParent?.Offset.Y ?? 0),
                new LiteHtmlSize(viewportWidth, viewportHeight));
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
            // Match WPF pattern: control size = document size
            Width = size.Width;
            Height = size.Height;
            SetViewport();
        }

        public void Container_RenderHtmlRequested(string html)
        {
            LoadHtml(html);
        }

        private void FireLink(string url)
        {
            try
            {
                LinkClicked?.Invoke(url);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LinkClicked event handler for '{url}': {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                // Re-throw to let the caller know an error occurred, but after logging
                throw;
            }
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            try
            {
                if (Container?.Document?.HasRendered == true)
                {
                    if (Container.Document.OnMouseLeave())
                    {
                        TriggerRedraw();
                    }
                }

                SetCursor(null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnPointerExited: {ex.Message}");
            }

            base.OnPointerExited(e);
        }

        private void TriggerRedraw()
        {
            Dispatcher.UIThread.Post(() => _controlPanel?.InvalidateVisual(), DispatcherPriority.Render);
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            try
            {
                if (Container?.Document?.HasRendered == true)
                {
                    var pos = e.GetPosition(this);
                    // Validate coordinates
                    if (pos.X >= 0 && pos.Y >= 0)
                    {
                        if (Container.Document.OnMouseMove((int)pos.X, (int)pos.Y))
                        {
                            TriggerRedraw();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnPointerMoved: {ex.Message}");
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
                try
                {
                    var pos = e.GetPosition(this);
                    var (x, y) = ToDocumentCoords(pos);

                    // Validate coordinates are within reasonable bounds
                    if (x < 0 || y < 0)
                    {
                        Console.WriteLine($"Warning: Invalid pointer press coordinates: ({x}, {y})");
                        base.OnPointerPressed(e);
                        return;
                    }

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
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in OnPointerPressed: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                }
            }

            base.OnPointerPressed(e);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (Container?.Document?.HasRendered == true && e.InitialPressMouseButton == MouseButton.Left)
            {
                try
                {
                    var pos = e.GetPosition(this);
                    var (x, y) = ToDocumentCoords(pos);

                    // Validate coordinates are within reasonable bounds
                    if (x < 0 || y < 0)
                    {
                        Console.WriteLine($"Warning: Invalid pointer release coordinates: ({x}, {y})");
                        base.OnPointerReleased(e);
                        return;
                    }

                    if (Container.Document.OnLeftButtonUp(x, y))
                    {
                        TriggerRedraw();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in OnPointerReleased: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                }
            }

            // Reset flag after a full click sequence (release) unless still over link (allow drag next time if moved off)
            if (!IsCursorOverLink)
            {
                LastPointerDownHandledByHtml = false;
            }

            base.OnPointerReleased(e);
        }


        public void LoadHtml(string html)
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
            // Like Mac, use the actual viewport dimensions (set by parent before LoadHtml)
            var viewportWidth = ScrollViewerParent?.Viewport.Width ?? 0;
            var viewportHeight = ScrollViewerParent?.Viewport.Height ?? 0;
            if (viewportWidth <= 0) viewportWidth = 1024; // Reasonable default
            if (viewportHeight <= 0) viewportHeight = 1; // Minimal height if not set

            var size = new LiteHtmlSize(viewportWidth, viewportHeight);
            Container.SetViewport(new LiteHtmlPoint(0, 0), size);
            Container.CheckViewportChange(forceRender: true);

            Console.WriteLine($"HTML loaded, viewport: {viewportWidth}x{viewportHeight}, rendered: {Container.Document.HasRendered}");

            // Process inputs immediately after HTML is loaded and rendered
            Dispatcher.UIThread.Post(ProcessInputs, DispatcherPriority.Background);

            TriggerRedraw();
        }

        private void SetViewport(bool forceRedraw = false)
        {
            // Match WPF pattern: viewport = ScrollViewer's visible area
            if (ScrollViewerParent == null) return;

            var viewportWidth = ScrollViewerParent.Viewport.Width;
            var viewportHeight = ScrollViewerParent.Viewport.Height;

            // Only set viewport if we have valid dimensions
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

        // UserControl doesn't use Render override - need different approach for HTML rendering

        public void RenderHtmlBackground(DrawingContext context)
        {
            var renderBounds = new Rect(0, 0, Bounds.Width, Bounds.Height);
            //let the rectangle be transparent, so the control / window backgrounds can be controlled elsewhere by theme.
            context.FillRectangle(Brushes.Transparent, renderBounds);

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

                Console.WriteLine(
                    $"ElementInfo for Type: {input.Element.GetType()} {input.ID}: PosX={info.PosX}, PosY={info.PosY}, Width={info.Width}, Height={info.Height}");

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

                Console.WriteLine(
                    $"Set Canvas position for {input.Element.GetType().Name}: Left={Canvas.GetLeft(input.Element)}, Top={Canvas.GetTop(input.Element)}");
                if (input.AttributesSetup) continue;
                input.AttributesSetup = true;
                input.SetupAttributes(info.Attributes);
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
                    textBox.BorderThickness = new Thickness(1);
                    break;
                case Button button:
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