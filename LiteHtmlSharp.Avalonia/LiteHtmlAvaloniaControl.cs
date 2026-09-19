using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace LiteHtmlSharp.Avalonia;

public delegate void LinkClickedHandler(string url);

public sealed class HtmlRenderPanel : Control
{
    private readonly Canvas canvas = new();
    internal LiteHtmlAvaloniaControl HtmlControl { get; set; }
    public Controls Children => canvas.Children;

    public HtmlRenderPanel()
    {
        LogicalChildren.Add(canvas);
        VisualChildren.Add(canvas);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        canvas.Measure(availableSize);
        return canvas.DesiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        canvas.Arrange(new Rect(finalSize));
        return finalSize;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        HtmlControl?.RenderHtmlBackground(context);
    }
}

public class LiteHtmlAvaloniaControl : UserControl, IDisposable
{
    private readonly Cursor arrowCursor = new(StandardCursorType.Arrow);
    private readonly Cursor handCursor = new(StandardCursorType.Hand);
    private readonly ScrollViewer scrollParent;
    private readonly HtmlRenderPanel panel;
    private readonly AnimationDriver animationDriver;
    private readonly DispatcherTimer animationTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private bool refreshQueued;
    private bool layoutPending;
    private bool refreshing;
    private bool disposed;
    private bool cursorOverLink;
    private bool cursorUpdateQueued;
    private Cursor pendingCursor;
    private string tooltip;
    private int nextInput;
    private LiteHtmlPoint recordedOffset;

    public AvaloniaContainer Container { get; }
    public AvaloniaInputs Inputs { get; } = new();
    public event LinkClickedHandler LinkClicked;
    public bool LastPointerDownHandledByHtml { get; private set; }
    private bool ExternalViewport => scrollParent == null && Container.HasCustomViewport;

    public LiteHtmlAvaloniaControl(ScrollViewer parent, AvaloniaContainer container, string masterCss,
        IResourceLoader loader, bool createInteractiveElements = true, bool ownsContainer = true)
    {
        this.ownsContainer = container == null || ownsContainer;
        installsElementCallbacks = createInteractiveElements;
        scrollParent = parent;
        Container = container ?? new AvaloniaContainer(masterCss, loader);
        panel = new HtmlRenderPanel { HtmlControl = this };
        Background = Brushes.Transparent;
        Content = panel;
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Top;
        Container.HtmlRenderHandler = LoadHtml;
        Container.AnchorClicked += OnAnchorClicked;
        Container.DocumentSizeKnown += OnDocumentSizeKnown;
        Container.RedrawRequested += OnRedrawRequested;
        Container.ImageReady += OnImageReady;
        animationDriver = new AnimationDriver(Container.Document, ScheduleAnimation, () => QueueRefresh(true),
            () => new RectF(Container.ScrollOffset.X, Container.ScrollOffset.Y, Container.Size.Width, Container.Size.Height));
        animationTimer.Tick += OnAnimationTick;
        Container.SetCursorCallback = SetCursor;
        if (createInteractiveElements)
        {
            Container.ShouldCreateElementCallback = tag => tag is "input" or "button";
            Container.CreateElementCallback = CreateElement;
        }
        if (scrollParent != null)
        {
            scrollParent.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            scrollParent.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scrollParent.HorizontalContentAlignment = HorizontalAlignment.Left;
            scrollParent.VerticalContentAlignment = VerticalAlignment.Top;
            scrollParent.Content = this;
            scrollParent.ScrollChanged += OnScrollChanged;
            scrollParent.SizeChanged += OnViewportSizeChanged;
        }
        else
        {
            SizeChanged += OnViewportSizeChanged;
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        animationDriver.Resume();
        QueueRefresh(true);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        animationDriver.Suspend();
        base.OnDetachedFromVisualTree(e);
    }

    private void ScheduleAnimation(TimeSpan? delay)
    {
        animationTimer.Stop();
        if (delay is { } interval)
        {
            animationTimer.Interval = interval <= TimeSpan.Zero ? TimeSpan.FromMilliseconds(1) : interval;
            animationTimer.Start();
        }
    }
    private void OnAnimationTick(object sender, EventArgs e) => animationDriver.Pulse(Refresh);
    private void OnImageReady(string source, string baseUrl) => Dispatcher.UIThread.Post(() => QueueRefresh(true));
    private void SyncAnimationTime() => animationDriver.Synchronize();


    private void LoadHtml(string html)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ClearInputs();
        nextInput = 0;
        Container.Document.Load(html);
        QueueRefresh(true);
    }

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e) => QueueRefresh(false);
    private void OnViewportSizeChanged(object sender, SizeChangedEventArgs e) => QueueRefresh(true);

    private void QueueRefresh(bool layout)
    {
        if (disposed || !Container.Document.HasLoadedHtml) return;
        layoutPending |= layout;
        if (refreshQueued) return;
        refreshQueued = true;
        // Native callbacks must finish before layout can enter the document again.
        Dispatcher.UIThread.Post(Refresh, DispatcherPriority.Loaded);
    }

    private static double Positive(double value, double fallback) => double.IsFinite(value) && value > 0 ? value : fallback;

    private void Refresh()
    {
        refreshQueued = false;
        if (disposed || !Container.Document.HasLoadedHtml) return;
        refreshing = true;
        try
        {
            var scale = Container.ScaleFactor;
            var width = ExternalViewport ? Container.Size.Width :
                Positive(scrollParent?.Viewport.Width ?? 0, Positive(scrollParent?.Bounds.Width ?? Bounds.Width, 800)) / scale;
            var height = ExternalViewport ? Container.Size.Height :
                Positive(scrollParent?.Viewport.Height ?? 0, Positive(scrollParent?.Bounds.Height ?? Bounds.Height, 600)) / scale;
            var changed = Container.Size.Width != (float)width || Container.Size.Height != (float)height;
            if (!ExternalViewport)
            {
                Container.Size = new LiteHtmlSize((float)width, (float)height);
                Container.ScrollOffset = new LiteHtmlPoint((float)(scrollParent?.Offset.X ?? 0) / scale,
                    (float)(scrollParent?.Offset.Y ?? 0) / scale);
            }
            var render = layoutPending || changed || animationDriver.NeedsAnimationSample || !Container.Document.HasRendered;
            layoutPending = false;
            if (render)
            {
                if (changed || !Container.Document.HasRendered) Container.Document.OnMediaChanged();
                Container.Render(animationDriver.Render);
            }
            var offset = Container.ScrollOffset;
            Container.Document.Draw(-offset.X, -offset.Y, Container.Viewport);
            recordedOffset = offset;
            ProcessInputs();
            panel.InvalidateVisual();
            animationDriver.FrameCompleted();
        }
        finally
        {
            refreshing = false;
        }
    }

    private void OnDocumentSizeKnown(LiteHtmlSize size)
    {
        Width = Math.Max(ExternalViewport ? Container.Size.Width : size.Width, Container.Size.Width) * Container.ScaleFactor;
        Height = (ExternalViewport ? Container.Size.Height : size.Height) * Container.ScaleFactor;
        panel.Width = Width;
        panel.Height = Height;
        if (!refreshing) QueueRefresh(false);
    }

    private void OnRedrawRequested(RectF box)
    {
        if (disposed) return;
        if (Dispatcher.UIThread.CheckAccess()) panel.InvalidateVisual();
        else Dispatcher.UIThread.Post(() => { if (!disposed) panel.InvalidateVisual(); });
    }

    public void RenderHtmlBackground(DrawingContext context)
    {
        if (disposed || Container.Document.LastDisplayList is not { } snapshot) return;
        Container.DrawingContext = context;
        try
        {
            // ScrollViewer translates the full page; native fixed elements use viewport coordinates.
            var translation = scrollParent == null ? Matrix.Identity :
                Matrix.CreateTranslation(recordedOffset.X * Container.ScaleFactor, recordedOffset.Y * Container.ScaleFactor);
            using (context.PushTransform(translation)) DisplayListReplayer.Replay(snapshot, Container);
        }
        finally { Container.DrawingContext = null; }
    }

    private Point DocumentPoint(PointerEventArgs e)
    {
        var point = e.GetPosition(panel);
        var offset = ExternalViewport ? Container.ScrollOffset : default;
        return new Point(point.X / Container.ScaleFactor + offset.X, point.Y / Container.ScaleFactor + offset.Y);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (!disposed && Container.Document.HasRendered)
        {
            SyncAnimationTime();
            var p = DocumentPoint(e);
            if (Container.Document.OnMouseOver((float)p.X, (float)p.Y, (float)p.X - Container.ScrollOffset.X, (float)p.Y - Container.ScrollOffset.Y)) QueueRefresh(true);
            var title = Container.Document.GetTooltipText((float)p.X, (float)p.Y, (float)p.X - Container.ScrollOffset.X, (float)p.Y - Container.ScrollOffset.Y);
            if (title != tooltip)
            {
                tooltip = title;
                ToolTip.SetTip(this, string.IsNullOrWhiteSpace(title) ? null : title);
            }
        }
        base.OnPointerMoved(e);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        if (!disposed && Container.Document.HasRendered)
        {
            SyncAnimationTime();
            if (Container.Document.OnMouseLeave()) QueueRefresh(true);
        }
        tooltip = null;
        ToolTip.SetTip(this, null);
        SetCursor(null);
        base.OnPointerExited(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        LastPointerDownHandledByHtml = false;
        if (!disposed && Container.Document.HasRendered && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            SyncAnimationTime();
            var p = DocumentPoint(e);
            LastPointerDownHandledByHtml = cursorOverLink;
            if (cursorOverLink) e.Handled = true;
            if (Container.Document.OnLeftButtonDown((float)p.X, (float)p.Y, (float)p.X - Container.ScrollOffset.X, (float)p.Y - Container.ScrollOffset.Y)) QueueRefresh(true);
        }
        base.OnPointerPressed(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (!disposed && Container.Document.HasRendered && e.InitialPressMouseButton == MouseButton.Left)
        {
            SyncAnimationTime();
            var p = DocumentPoint(e);
            if (Container.Document.OnLeftButtonUp((float)p.X, (float)p.Y, (float)p.X - Container.ScrollOffset.X, (float)p.Y - Container.ScrollOffset.Y)) QueueRefresh(true);
        }
        base.OnPointerReleased(e);
    }

    public void SetCursor(string cursor)
    {
        cursorOverLink = string.Equals(cursor, "pointer", StringComparison.OrdinalIgnoreCase);
        pendingCursor = cursorOverLink ? handCursor : arrowCursor;
        if (!OperatingSystem.IsMacOS())
        {
            Cursor = pendingCursor;
            return;
        }
        if (cursorUpdateQueued) return;
        cursorUpdateQueued = true;
        // Cocoa handles the mouse event after Avalonia and can replace its cursor.
        Dispatcher.UIThread.Post(() =>
        {
            cursorUpdateQueued = false;
            if (disposed) return;
            Cursor = null;
            Cursor = pendingCursor;
        }, DispatcherPriority.Input);
    }

    private void OnAnchorClicked(string url)
    {
        // Consumers may load another page from a link handler after the native call returns.
        Dispatcher.UIThread.Post(() => { if (!disposed) LinkClicked?.Invoke(url); });
    }

    private int CreateElement(string tag, IReadOnlyDictionary<string, string> attributes, out SizeF size)
    {
        var input = new AvaloniaInput(tag == "button" ? InputType.Button : InputType.Textbox)
        {
            ID = ++nextInput,
            Element = tag == "button" ? new Button { Content = "Button" } : new TextBox(),
            IsPlaced = true,
            AttributesSetup = true
        };
        input.Element.Tag = input;
        input.Element.VerticalAlignment = VerticalAlignment.Top;
        input.Element.HorizontalAlignment = HorizontalAlignment.Left;
        input.SetupAttributes(attributes);
        if (input.Element is Button button) button.Click += OnButtonClick;
        input.Element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        size = new SizeF((float)Math.Max(input.Element.DesiredSize.Width, 80) / Container.ScaleFactor,
            (float)Math.Max(input.Element.DesiredSize.Height, 28) / Container.ScaleFactor);
        Inputs.Add(input);
        AddChildControl(input.Element);
        return input.ID;
    }

    private void OnButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: AvaloniaInput input } && !string.IsNullOrEmpty(input.Href))
            LinkClicked?.Invoke(input.Href);
    }

    private void ProcessInputs()
    {
        if (disposed || !Container.Document.HasRendered) return;
        var offset = ExternalViewport ? Container.ScrollOffset : default;
        foreach (var input in Inputs)
        {
            var box = Container.Document.GetElementInfo(input.ID)?.Bounds;
            input.Element.IsVisible = box.HasValue;
            if (box is not { } r) continue;
            input.Element.Width = Math.Max(0, r.Width * Container.ScaleFactor);
            input.Element.Height = Math.Max(0, r.Height * Container.ScaleFactor);
            Canvas.SetLeft(input.Element, (r.X - offset.X) * Container.ScaleFactor);
            Canvas.SetTop(input.Element, (r.Y - offset.Y) * Container.ScaleFactor);
        }
    }

    public void AddChildControl(Control control) => panel.Children.Add(control);
    public void RemoveChildControl(Control control) => panel.Children.Remove(control);

    private void ClearInputs()
    {
        foreach (var input in Inputs)
        {
            if (input.Element is Button button) button.Click -= OnButtonClick;
            panel.Children.Remove(input.Element);
        }
        Inputs.Clear();
    }

    private readonly bool ownsContainer;
    private readonly bool installsElementCallbacks;

    public void Dispose()
    {
        if (disposed) return;
        try { if (ownsContainer) Container.Dispose(); }
        finally { if (!ownsContainer || Container.Document.IsDisposed) Detach(); }
    }

    private void Detach()
    {
        disposed = true;
        animationTimer.Stop();
        animationTimer.Tick -= OnAnimationTick;
        animationDriver.Dispose();
        Container.ImageReady -= OnImageReady;
        if (scrollParent != null)
        {
            scrollParent.ScrollChanged -= OnScrollChanged;
            scrollParent.SizeChanged -= OnViewportSizeChanged;
        }
        SizeChanged -= OnViewportSizeChanged;
        Container.HtmlRenderHandler = null;
        Container.AnchorClicked -= OnAnchorClicked;
        Container.DocumentSizeKnown -= OnDocumentSizeKnown;
        Container.RedrawRequested -= OnRedrawRequested;
        Container.SetCursorCallback = null;
        Cursor = null;
        arrowCursor.Dispose();
        handCursor.Dispose();
        if (installsElementCallbacks)
        {
            Container.ShouldCreateElementCallback = null;
            Container.CreateElementCallback = null;
        }
        ClearInputs();
        panel.HtmlControl = null;
    }
}
