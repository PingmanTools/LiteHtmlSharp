using System;
using System.Collections.Generic;
using System.Numerics;
using LiteHtmlSharp.Interop;
using AppKit;
using CoreGraphics;
using Foundation;
using LiteHtmlSharp.CoreGraphics;

namespace LiteHtmlSharp.Mac;

public class LiteHtmlNSView : NSView, INSToolTipOwner
{
    public override bool IsFlipped => true;
    public CGContainer LiteHtmlContainer { get; }
    public event Action Drawn;
    public event Action<string> LinkClicked;
    public event Action<ICustomTagView> CustomTagViewHasSetup;
    public IEnumerable<ICustomTagView> ViewElements => elements.Values;
    private readonly Dictionary<int, ICustomTagView> elements = new();
    private NSTrackingArea trackingArea;
    private readonly Dictionary<CGRect, nint> tooltipRegions = new();
    private readonly AnimationDriver animationDriver;
    private NSTimer animationTimer;
    private CGRect viewport;
    private LiteHtmlPoint recordedOffset;
    private bool queued, layoutPending, paintPending, disposed;
    private int nextElement;
    public LiteHtmlNSView(CGRect rect, string masterCssData) : base(rect)
    {
        WantsLayer = true;
        viewport = new CGRect(0, 0, rect.Width, rect.Height);
        using var systemFont = NSFont.SystemFontOfSize(NSFont.SystemFontSize);
        LiteHtmlContainer = new CGContainer(masterCssData) { DefaultFontName = systemFont.FontName,
                                                             DefaultFontSize = (float)NSFont.SystemFontSize };
        LiteHtmlContainer.SetCursorCallback = SetCursor;
        LiteHtmlContainer.ShouldCreateElementCallback = tag => tag is "input" or "button";
        LiteHtmlContainer.CreateElementCallback = CreateElement;
        animationDriver = new AnimationDriver(LiteHtmlContainer.Document, ScheduleAnimation, () => QueueRefresh(true),
            () => new RectF(LiteHtmlContainer.ScrollOffset.X, LiteHtmlContainer.ScrollOffset.Y,
                LiteHtmlContainer.Size.Width, LiteHtmlContainer.Size.Height));
        LiteHtmlContainer.RedrawRequested += Redraw;
        LiteHtmlContainer.ImageReady += ImageReady;
        LiteHtmlContainer.HtmlRenderHandler = LoadHtml;
        LiteHtmlContainer.AnchorClicked += AnchorClicked;
    }
    public override void ViewDidMoveToWindow()
    {
        base.ViewDidMoveToWindow();
        if (Window == null) animationDriver?.Suspend();
        else animationDriver?.Resume();
    }
    private void ScheduleAnimation(TimeSpan? delay)
    {
        animationTimer?.Invalidate();
        animationTimer?.Dispose();
        animationTimer = null;
        if (delay is { } interval)
            animationTimer = NSTimer.CreateRepeatingScheduledTimer(interval <= TimeSpan.Zero ? TimeSpan.FromMilliseconds(1) : interval,
                _ => animationDriver.Pulse(Refresh));
    }
    private void SetCursor(string value)
    {
        // Mouse and cursor-update callbacks already run on the UI thread.
        // Apply synchronously so AppKit cannot leave its default arrow visible
        // while a queued hand-cursor update waits for another event-loop turn.
        if (!disposed) (value == "pointer" ? NSCursor.PointingHandCursor : NSCursor.ArrowCursor).Set();
    }
    private void AnchorClicked(string url) => BeginInvokeOnMainThread(() =>
                                                                      {
                                                                          if (!disposed)
                                                                              LinkClicked?.Invoke(url);
                                                                      });
    private void ImageReady(string source, string root) => BeginInvokeOnMainThread(() =>
    {
        if (!disposed) { paintPending = true; QueueRefresh(true); }
    });
    private void Redraw(RectF box) => BeginInvokeOnMainThread(() =>
                                                              {
                                                                  if (!disposed)
                                                                      SetNeedsDisplayInRect(viewport);
                                                              });
    private int CreateElement(string tag, IReadOnlyDictionary<string, string> attributes, out SizeF size)
    {
        ICustomTagView control = tag == "button" ? new LiteHtmlNSButton() : new LiteHtmlNSInput();
        control.Setup(attributes);
        if (control is LiteHtmlNSButton button)
        {
            button.SizeToFit();
            button.Activated += ButtonActivated;
        }
        if (control is LiteHtmlNSInput input)
            input.SizeToFit();
        size = new SizeF((float)Math.Max(80, control.View.Frame.Width) / LiteHtmlContainer.ScaleFactor,
                         (float)Math.Max(28, control.View.Frame.Height) / LiteHtmlContainer.ScaleFactor);
        var id = ++nextElement;
        elements.Add(id, control);
        AddSubview(control.View);
        BeginInvokeOnMainThread(() =>
                                {
                                    if (!disposed && elements.ContainsKey(id))
                                        CustomTagViewHasSetup?.Invoke(control);
                                });
        return id;
    }
    private void ButtonActivated(object sender, EventArgs e)
    {
        if (sender is LiteHtmlNSButton button && !string.IsNullOrEmpty(button.Href))
            AnchorClicked(button.Href);
    }
    private void ClearInputs()
    {
        foreach (var input in elements.Values)
        {
            if (input is LiteHtmlNSButton b)
                b.Activated -= ButtonActivated;
            input.View.RemoveFromSuperview();
            input.View.Dispose();
        }
        elements.Clear();
    }
    private void ProcessInputs()
    {
        if (disposed || !LiteHtmlContainer.Document.HasRendered)
            return;
        foreach (var (id, input) in elements)
        {
            var box = LiteHtmlContainer.Document.GetElementInfo(id)?.Bounds;
            if (input.View.Hidden != !box.HasValue) input.View.Hidden = !box.HasValue;
            if (box is {} r)
            {
                var scale = LiteHtmlContainer.ScaleFactor;
                var frame = new CGRect(r.X * scale, r.Y * scale, r.Width * scale, r.Height * scale);
                if (input.View.Frame != frame) input.View.Frame = frame;
            }
        }
    }
    public void SetViewport(CGRect value)
    {
        if (viewport == value)
            return;
        var layout = viewport.Width != value.Width || viewport.Height != value.Height;
        viewport = value;
        QueueRefresh(layout);
    }
    private void LoadHtml(string html)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ClearInputs();
        nextElement = 0;
        LiteHtmlContainer.Document.Load(html);
        QueueRefresh(true);
    }
    private void QueueRefresh(bool layout)
    {
        if (disposed || !LiteHtmlContainer.Document.HasLoadedHtml)
            return;
        layoutPending |= layout;
        if (queued)
            return;
        queued = true;
        BeginInvokeOnMainThread(Refresh);
    }
    private void Refresh()
    {
        queued = false;
        if (disposed || !LiteHtmlContainer.Document.HasLoadedHtml)
            return;
        var scale = LiteHtmlContainer.ScaleFactor;
        var changed = LiteHtmlContainer.Size.Width != (float)viewport.Width / scale ||
            LiteHtmlContainer.Size.Height != (float)viewport.Height / scale;
        LiteHtmlContainer.Size = new LiteHtmlSize((float)viewport.Width / scale, (float)viewport.Height / scale);
        LiteHtmlContainer.ScrollOffset = new LiteHtmlPoint((float)viewport.X / scale, (float)viewport.Y / scale);
        if (layoutPending || animationDriver.NeedsAnimationSample || !LiteHtmlContainer.Document.HasRendered)
        {
            layoutPending = false;
            if (changed || !LiteHtmlContainer.Document.HasRendered) LiteHtmlContainer.Document.OnMediaChanged();
            LiteHtmlContainer.Render(animationDriver.Render);
        }
        var offset = LiteHtmlContainer.ScrollOffset;
        var previous = LiteHtmlContainer.Document.LastDisplayList;
        var list = LiteHtmlContainer.Document.Draw(-offset.X, -offset.Y, LiteHtmlContainer.Viewport);
        var paintChanged = paintPending || !list.ContentEquals(previous) || recordedOffset.X != offset.X || recordedOffset.Y != offset.Y;
        recordedOffset = offset;
        paintPending = false;
        if (paintChanged)
        {
            UpdateTooltipRegions(list);
            SetNeedsDisplayInRect(viewport);
        }
        ProcessInputs();
        animationDriver.FrameCompleted();
    }
    public override void UpdateTrackingAreas()
    {
        base.UpdateTrackingAreas();
        if (disposed) return;
        // InVisibleRect tracks scrolling and resizing itself. Recreating it on
        // every animated layout also disturbs AppKit's hover tracking.
        if (trackingArea == null)
        {
            trackingArea = new NSTrackingArea(Bounds,
                NSTrackingAreaOptions.ActiveInKeyWindow | NSTrackingAreaOptions.MouseMoved | NSTrackingAreaOptions.CursorUpdate |
                NSTrackingAreaOptions.MouseEnteredAndExited | NSTrackingAreaOptions.InVisibleRect,
                this, null);
            AddTrackingArea(trackingArea);
        }
    }
    private void UpdateTooltipRegions(DisplayList list)
    {
        // Give AppKit real content regions so leaving a word/image rearms its
        // tooltip. One document-wide region can only trigger once per entry.
        // Preserve unchanged registrations across animation frames.
        var regions = new HashSet<CGRect>();
        var matrices = new Stack<Matrix4x4>();
        var clips = new Stack<CGRect>();
        var matrix = Matrix4x4.Identity;
        var clip = new CGRect(0, 0, viewport.Width / LiteHtmlContainer.ScaleFactor, viewport.Height / LiteHtmlContainer.ScaleFactor);
        CGRect Project(lh_rect r)
        {
            var a = Vector2.Transform(new Vector2(r.x, r.y), matrix);
            var b = Vector2.Transform(new Vector2(r.x + r.width, r.y), matrix);
            var c = Vector2.Transform(new Vector2(r.x, r.y + r.height), matrix);
            var d = Vector2.Transform(new Vector2(r.x + r.width, r.y + r.height), matrix);
            var min = Vector2.Min(Vector2.Min(a, b), Vector2.Min(c, d));
            var max = Vector2.Max(Vector2.Max(a, b), Vector2.Max(c, d));
            return new CGRect(min.X, min.Y, max.X - min.X, max.Y - min.Y);
        }
        static CGRect Intersect(CGRect a, CGRect b)
        {
            var x = Math.Max(a.X, b.X); var y = Math.Max(a.Y, b.Y);
            return new CGRect(x, y, Math.Max(0, Math.Min(a.Right, b.Right) - x),
                Math.Max(0, Math.Min(a.Bottom, b.Bottom) - y));
        }
        void Add(lh_rect r)
        {
            var box = Intersect(Project(r), clip);
            if (box.Width <= 0 || box.Height <= 0) return;
            var scale = LiteHtmlContainer.ScaleFactor;
            regions.Add(new CGRect((box.X + recordedOffset.X) * scale,
                (box.Y + recordedOffset.Y) * scale, box.Width * scale, box.Height * scale));
        }
        foreach (var command in list)
        {
            switch (command.Type)
            {
                case CommandType.Text: Add(command.Read<lh_cmd_text>().pos); break;
                case CommandType.Image: Add(command.Read<lh_cmd_image>().layer.clip_box); break;
                case CommandType.PushTransform:
                    matrices.Push(matrix);
                    var m = command.Read<lh_cmd_push_transform>().m;
                    matrix = new Matrix4x4(m[0], m[1], m[2], m[3], m[4], m[5], m[6], m[7],
                        m[8], m[9], m[10], m[11], m[12], m[13], m[14], m[15]) * matrix;
                    break;
                case CommandType.PopTransform: matrix = matrices.Pop(); break;
                case CommandType.SetClip:
                    clips.Push(clip);
                    clip = Intersect(clip, Project(command.Read<lh_cmd_set_clip>().pos));
                    break;
                case CommandType.DelClip: clip = clips.Pop(); break;
            }
        }
        foreach (var box in new List<CGRect>(tooltipRegions.Keys))
        {
            if (regions.Contains(box)) continue;
            RemoveToolTip(tooltipRegions[box]);
            tooltipRegions.Remove(box);
        }
        foreach (var box in regions)
            if (!tooltipRegions.ContainsKey(box))
                tooltipRegions.Add(box, AddToolTip(box, (INSToolTipOwner)this, IntPtr.Zero));
    }
    [Export("view:stringForToolTip:point:userData:")]
    public string GetStringForToolTip(NSView view, nint tag, CGPoint point, IntPtr userData)
    {
        if (disposed || !LiteHtmlContainer.Document.HasRendered) return null;
        var scale = LiteHtmlContainer.ScaleFactor;
        var text = LiteHtmlContainer.Document.GetTooltipText((float)point.X / scale, (float)point.Y / scale,
            (float)(point.X - viewport.X) / scale, (float)(point.Y - viewport.Y) / scale);
        return string.IsNullOrEmpty(text) ? null : text;
    }
    public override void DrawRect(CGRect dirtyRect)
    {
        if (disposed || LiteHtmlContainer.Document.LastDisplayList is not {} list)
            return;
        var context = NSGraphicsContext.CurrentContext.CGContext;
        context.SaveState();
        LiteHtmlContainer.Context = context;
        try
        {
            context.TranslateCTM(recordedOffset.X * LiteHtmlContainer.ScaleFactor,
                                 recordedOffset.Y * LiteHtmlContainer.ScaleFactor);
            DisplayListReplayer.Replay(list, LiteHtmlContainer);
        }
        finally
        {
            LiteHtmlContainer.Context = null;
            context.RestoreState();
        }
        Drawn?.Invoke();
    }
    private (float X, float Y, float ClientX, float ClientY) Point(NSEvent e)
    {
        var p = ConvertPointFromView(e.LocationInWindow, null);
        var scale = LiteHtmlContainer.ScaleFactor;
        return ((float)p.X / scale, (float)p.Y / scale, (float)(p.X - viewport.X) / scale,
                (float)(p.Y - viewport.Y) / scale);
    }
    private void UpdateHover(NSEvent e)
    {
        if (!disposed && LiteHtmlContainer.Document.HasRendered)
        {
            animationDriver.Synchronize();
            var p = Point(e);
            if (LiteHtmlContainer.Document.OnMouseOver(p.X, p.Y, p.ClientX, p.ClientY))
                QueueRefresh(true);
        }
    }
    public override void MouseMoved(NSEvent e)
    {
        base.MouseMoved(e);
        UpdateHover(e);
    }
    public override void CursorUpdate(NSEvent e)
    {
        // AppKit requests a fresh cursor when crossing tooltip/tracking regions.
        // Resolve the current HTML target rather than restoring its default arrow.
        UpdateHover(e);
    }
    public override void MouseExited(NSEvent e)
    {
        if (!disposed && LiteHtmlContainer.Document.HasRendered)
        {
            animationDriver.Synchronize();
            if (LiteHtmlContainer.Document.OnMouseLeave())
                QueueRefresh(true);
            SetCursor(null);
        }
        base.MouseExited(e);
    }
    public override void MouseDown(NSEvent e)
    {
        if (!disposed && LiteHtmlContainer.Document.HasRendered)
        {
            animationDriver.Synchronize();
            var p = Point(e);
            if (LiteHtmlContainer.Document.OnLeftButtonDown(p.X, p.Y, p.ClientX, p.ClientY))
                QueueRefresh(true);
        }
        base.MouseDown(e);
    }
    public override void MouseUp(NSEvent e)
    {
        if (!disposed && LiteHtmlContainer.Document.HasRendered)
        {
            animationDriver.Synchronize();
            var p = Point(e);
            if (LiteHtmlContainer.Document.OnLeftButtonUp(p.X, p.Y, p.ClientX, p.ClientY))
                QueueRefresh(true);
        }
        base.MouseUp(e);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing && !disposed)
        {
            try
            {
                LiteHtmlContainer.Dispose();
            }
            finally
            {
                if (LiteHtmlContainer.Document.IsDisposed)
                {
                    disposed = true;
                    animationDriver.Dispose();
                    foreach (var tag in tooltipRegions.Values) RemoveToolTip(tag);
                    tooltipRegions.Clear();
                    LiteHtmlContainer.RedrawRequested -= Redraw;
                    LiteHtmlContainer.ImageReady -= ImageReady;
                    LiteHtmlContainer.HtmlRenderHandler = null;
                    LiteHtmlContainer.AnchorClicked -= AnchorClicked;
                    LiteHtmlContainer.SetCursorCallback = null;
                    LiteHtmlContainer.CreateElementCallback = null;
                    LiteHtmlContainer.ShouldCreateElementCallback = null;
                    ClearInputs();
                    if (trackingArea != null)
                    {
                        RemoveTrackingArea(trackingArea);
                        trackingArea.Dispose();
                        trackingArea = null;
                    }
                }
            }
        }
        base.Dispose(disposing);
    }
}
