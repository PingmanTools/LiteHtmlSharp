using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace LiteHtmlSharp.Wpf;

public delegate void LinkClickedHandler(string url);
public class LiteHtmlPanel : Canvas, IDisposable
{
    private readonly ScrollViewer scrollParent;
    private readonly AnimationDriver animationDriver;
    private readonly DispatcherTimer animationTimer = new();
    private bool disposed, refreshQueued, layoutPending;
    private LiteHtmlPoint recordedOffset;
    public ScrollViewer ScrollViewerParent => scrollParent;
    public Inputs Inputs { get; } = new();
    public WpfContainer Container { get; }
    public event LinkClickedHandler LinkClicked;
    public bool HTMLLoaded => Container.Document.HasLoadedHtml;
    public LiteHtmlPanel(ScrollViewer parent, string masterCss, IResourceLoader loader, bool createdOldStyleControls = true)
        : this(parent, new WpfContainer(masterCss, loader), createdOldStyleControls) { }
    public LiteHtmlPanel(ScrollViewer parent, WpfContainer container, bool createOldStyleControls = true, bool ownsContainer = true)
    {
        this.ownsContainer = ownsContainer;
        installsElementCallbacks = createOldStyleControls;
        scrollParent = parent; Container = container;
        Background = Brushes.Transparent;
        HorizontalAlignment = HorizontalAlignment.Left; VerticalAlignment = VerticalAlignment.Top;
        scrollParent.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        scrollParent.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        scrollParent.CanContentScroll = false; scrollParent.Content = this;
        scrollParent.ScrollChanged += ScrollChanged;
        scrollParent.SizeChanged += SizeChangedHandler;
        Loaded += LoadedHandler;
        Unloaded += UnloadedHandler;
        animationDriver = new AnimationDriver(Container.Document, ScheduleAnimation, () => QueueRefresh(true),
            () => new RectF(Container.ScrollOffset.X, Container.ScrollOffset.Y, Container.Size.Width, Container.Size.Height));
        animationTimer.Tick += AnimationTick;
        Container.HtmlRenderHandler = LoadHtml;
        Container.AnchorClicked += AnchorClicked;
        Container.DocumentSizeKnown += DocumentSizeKnown;
        Container.RedrawRequested += RedrawRequested;
        Container.ImageReady += ImageReady;
        Container.SetCursorCallback = SetCursor;
        if (createOldStyleControls)
        {
            Container.ShouldCreateElementCallback = tag => tag is "input" or "button";
            Container.CreateElementCallback = CreateElement;
        }
    }
    private void LoadedHandler(object sender, RoutedEventArgs e) { animationDriver.Resume(); QueueRefresh(true); }
    private void UnloadedHandler(object sender, RoutedEventArgs e) => animationDriver.Suspend();
    private void AnimationTick(object sender, EventArgs e) => animationDriver.Pulse(Refresh);
    private void ScheduleAnimation(TimeSpan? delay)
    {
        animationTimer.Stop();
        if (delay is { } interval)
        {
            animationTimer.Interval = interval <= TimeSpan.Zero ? TimeSpan.FromMilliseconds(1) : interval;
            animationTimer.Start();
        }
    }
    private void ScrollChanged(object sender, ScrollChangedEventArgs e) => QueueRefresh(e.ViewportWidthChange != 0 || e.ViewportHeightChange != 0);
    private void SizeChangedHandler(object sender, SizeChangedEventArgs e) => QueueRefresh(true);
    private void DocumentSizeKnown(LiteHtmlSize size)
    {
        Width = Math.Max(size.Width, Container.Size.Width) * Container.ScaleFactor;
        Height = size.Height * Container.ScaleFactor;
    }
    private void LoadHtml(string html)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ClearInputs(); Container.Document.Load(html); QueueRefresh(true);
    }
    public void SetViewport(bool forceRedraw = false) => QueueRefresh(forceRedraw);
    private void QueueRefresh(bool layout)
    {
        if (disposed || !HTMLLoaded) return;
        layoutPending |= layout;
        if (refreshQueued) return;
        refreshQueued = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(Refresh));
    }
    private static double Positive(double value, double fallback) => double.IsFinite(value) && value > 0 ? value : fallback;
    private void Refresh()
    {
        refreshQueued = false;
        if (disposed || !HTMLLoaded) return;
        var scale = Container.ScaleFactor;
        var width = (float)(Positive(scrollParent.ViewportWidth, Positive(scrollParent.ActualWidth, 800)) / scale);
        var height = (float)(Positive(scrollParent.ViewportHeight, Positive(scrollParent.ActualHeight, 600)) / scale);
        var changed = width != Container.Size.Width || height != Container.Size.Height;
        Container.Size = new(width, height);
        Container.ScrollOffset = new((float)scrollParent.HorizontalOffset / scale, (float)scrollParent.VerticalOffset / scale);
        if (layoutPending || changed || animationDriver.NeedsAnimationSample || !Container.Document.HasRendered)
        {
            layoutPending = false;
            if (changed || !Container.Document.HasRendered) Container.Document.OnMediaChanged();
            Container.Render(animationDriver.Render);
        }
        var offset = Container.ScrollOffset;
        Container.Document.Draw(-offset.X, -offset.Y, Container.Viewport);
        recordedOffset = offset;
        ProcessInputs(); InvalidateVisual();
        animationDriver.FrameCompleted();
    }
    private void ImageReady(string source, string context) => Dispatcher.BeginInvoke(new Action(() => QueueRefresh(true)));
    private void RedrawRequested(RectF box)
    {
        if (disposed) return;
        if (Dispatcher.CheckAccess()) InvalidateVisual();
        else Dispatcher.BeginInvoke(new Action(() => { if (!disposed) InvalidateVisual(); }));
    }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (disposed || Container.Document.LastDisplayList is not { } snapshot) return;
        dc.PushTransform(new TranslateTransform(recordedOffset.X * Container.ScaleFactor, recordedOffset.Y * Container.ScaleFactor));
        Container.DrawingContext = dc;
        try { DisplayListReplayer.Replay(snapshot, Container); }
        finally { Container.DrawingContext = null; dc.Pop(); }
    }
    private Point DocumentPoint(MouseEventArgs e)
    {
        var p = e.GetPosition(this); return new(p.X / Container.ScaleFactor, p.Y / Container.ScaleFactor);
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (!disposed && Container.Document.HasRendered)
        {
            animationDriver.Synchronize(); var p = DocumentPoint(e); var o = Container.ScrollOffset;
            if (Container.Document.OnMouseOver((float)p.X, (float)p.Y, (float)p.X - o.X, (float)p.Y - o.Y)) QueueRefresh(true);
            var text = Container.Document.GetTooltipText((float)p.X, (float)p.Y, (float)p.X - o.X, (float)p.Y - o.Y);
            ToolTip = string.IsNullOrWhiteSpace(text) ? null : text;
        }
        base.OnMouseMove(e);
    }
    protected override void OnMouseLeave(MouseEventArgs e)
    {
        if (!disposed && Container.Document.HasRendered)
        {
            animationDriver.Synchronize();
            if (Container.Document.OnMouseLeave()) QueueRefresh(true);
        }
        ToolTip = null; Cursor = null; base.OnMouseLeave(e);
    }
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (!disposed && Container.Document.HasRendered) { animationDriver.Synchronize(); var p = DocumentPoint(e); var o = Container.ScrollOffset; if (Container.Document.OnLeftButtonDown((float)p.X, (float)p.Y, (float)p.X - o.X, (float)p.Y - o.Y)) QueueRefresh(true); }
        base.OnMouseLeftButtonDown(e);
    }
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (!disposed && Container.Document.HasRendered) { animationDriver.Synchronize(); var p = DocumentPoint(e); var o = Container.ScrollOffset; if (Container.Document.OnLeftButtonUp((float)p.X, (float)p.Y, (float)p.X - o.X, (float)p.Y - o.Y)) QueueRefresh(true); }
        base.OnMouseLeftButtonUp(e);
    }
    public void SetCursor(string cursor) => Cursor = string.Equals(cursor, "pointer", StringComparison.OrdinalIgnoreCase) ? Cursors.Hand : null;
    private void AnchorClicked(string url) => Dispatcher.BeginInvoke(new Action(() => { if (!disposed) FireLink(url); }));
    public void FireLink(string url) => LinkClicked?.Invoke(url);
    private int CreateElement(string tag, IReadOnlyDictionary<string, string> attributes, out SizeF size)
    {
        var input = new Input(tag == "button" ? InputType.Button : InputType.Textbox)
        {
            ID = Inputs.Count + 1,
            Element = tag == "button" ? new Button { Content = "Button" } : new TextBox(),
            IsPlaced = true,
            AttributesSetup = true
        };
        input.Element.Tag = input; input.SetupAttributes(attributes);
        if (input.Element is Button button) button.Click += ButtonClicked;
        input.Element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        size = new((float)Math.Max(input.Element.DesiredSize.Width, 80) / Container.ScaleFactor, (float)Math.Max(input.Element.DesiredSize.Height, 28) / Container.ScaleFactor);
        Inputs.Add(input); Children.Add(input.Element); return input.ID;
    }
    private void ButtonClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Input input } && !string.IsNullOrEmpty(input.Href)) FireLink(input.Href);
    }
    private void ProcessInputs()
    {
        if (disposed || !Container.Document.HasRendered) return;
        foreach (var input in Inputs)
        {
            var box = Container.Document.GetElementInfo(input.ID)?.Bounds;
            input.Element.Visibility = box.HasValue ? Visibility.Visible : Visibility.Collapsed;
            if (box is not { } r) continue;
            input.Element.Width = Math.Max(0, r.Width * Container.ScaleFactor); input.Element.Height = Math.Max(0, r.Height * Container.ScaleFactor);
            SetLeft(input.Element, r.X * Container.ScaleFactor); SetTop(input.Element, r.Y * Container.ScaleFactor);
        }
    }
    public void AddChildControl(FrameworkElement control) => Children.Add(control);
    public void RemoveChildControl(FrameworkElement control) => Children.Remove(control);
    public void ClearInputs()
    {
        foreach (var input in Inputs) { if (input.Element is Button button) button.Click -= ButtonClicked; Children.Remove(input.Element); }
        Inputs.Clear();
    }
    private readonly bool ownsContainer;
    private readonly bool installsElementCallbacks;

    public void Dispose()
    {
        if (disposed) return;
        try { if (ownsContainer) Container.Dispose(); }
        finally
        {
            if (!ownsContainer || Container.Document.IsDisposed)
            {
                animationDriver.Dispose();
                animationTimer.Tick -= AnimationTick;
                Unloaded -= UnloadedHandler;
                disposed = true; scrollParent.ScrollChanged -= ScrollChanged; scrollParent.SizeChanged -= SizeChangedHandler; Loaded -= LoadedHandler;
                Container.HtmlRenderHandler = null; Container.AnchorClicked -= AnchorClicked; Container.DocumentSizeKnown -= DocumentSizeKnown;
                Container.RedrawRequested -= RedrawRequested; Container.ImageReady -= ImageReady;
                Container.SetCursorCallback = null;
                if (installsElementCallbacks)
                {
                    Container.ShouldCreateElementCallback = null; Container.CreateElementCallback = null;
                }
                ClearInputs(); Cursor = null;
            }
        }
    }
}
