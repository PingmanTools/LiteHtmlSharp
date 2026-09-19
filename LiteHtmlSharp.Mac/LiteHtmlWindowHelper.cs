using System;
using AppKit;
using CoreGraphics;
using Foundation;
namespace LiteHtmlSharp.Mac;

public sealed class LiteHtmlWindowHelper : IDisposable
{
    public LiteHtmlNSView LiteHtmlView { get; }
    private readonly NSWindow window;
    private readonly NSScrollView scrollView;
    private readonly NSObject observer;
    private bool disposed;
    public LiteHtmlWindowHelper(NSWindow window, CGRect rect, string masterCssData)
    {
        this.window = window;
        LiteHtmlView = new LiteHtmlNSView(new CGRect(0, 0, rect.Width, rect.Height), masterCssData);
        LiteHtmlView.LiteHtmlContainer.CaptionDefined += Caption;
        LiteHtmlView.LiteHtmlContainer.DocumentSizeKnown += SizeKnown;
        scrollView =
            new NSScrollView { VerticalScrollElasticity = NSScrollElasticity.None, AutohidesScrollers = true,
                               HasHorizontalScroller = false, HasVerticalScroller = true, DocumentView = LiteHtmlView };
        scrollView.ContentView.PostsBoundsChangedNotifications = true;
        window.ContentView = scrollView;
        window.AcceptsMouseMovedEvents = true;
        observer = NSNotificationCenter.DefaultCenter.AddObserver(NSView.BoundsChangedNotification,
                                                                  _ => UpdateViewport(), scrollView.ContentView);
        window.DidResize += Resized;
        window.WillClose += Closed;
        UpdateViewport();
    }
    private void Caption(string caption) => window.Title = caption;
    private void SizeKnown(LiteHtmlSize size)
    {
        var frameSize = new CGSize(scrollView.ContentView.Bounds.Width,
            Math.Max(scrollView.ContentView.Bounds.Height, size.Height * LiteHtmlView.LiteHtmlContainer.ScaleFactor));
        if (LiteHtmlView.Frame.Size != frameSize) LiteHtmlView.SetFrameSize(frameSize);
        UpdateViewport();
    }
    private void UpdateViewport()
    {
        if (!disposed)
            LiteHtmlView.SetViewport(
                new CGRect(scrollView.ContentView.Bounds.Location, scrollView.ContentView.Bounds.Size));
    }
    private void Resized(object sender, EventArgs e) => UpdateViewport();
    private void Closed(object sender, EventArgs e) => Dispose();
    public void Dispose()
    {
        if (disposed)
            return;
        LiteHtmlView.Dispose();
        disposed = true;
        NSNotificationCenter.DefaultCenter.RemoveObserver(observer);
        observer.Dispose();
        window.DidResize -= Resized;
        window.WillClose -= Closed;
        LiteHtmlView.LiteHtmlContainer.CaptionDefined -= Caption;
        LiteHtmlView.LiteHtmlContainer.DocumentSizeKnown -= SizeKnown;
    }
}
