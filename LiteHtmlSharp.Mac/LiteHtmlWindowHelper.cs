using System;
using CoreGraphics;
using Foundation;
using AppKit;

namespace LiteHtmlSharp.Mac
{
   public class LiteHtmlWindowHelper
   {
      public LiteHtmlNSView LiteHtmlView { get; private set; }

      NSWindow window;
      NSScrollView scrollView;

      public LiteHtmlWindowHelper(NSWindow window, CGRect rect, string masterCssData)
      {
         this.window = window;
         this.LiteHtmlView = LiteHtmlView;

         LiteHtmlView = new LiteHtmlNSView(new CGRect(0, 0, rect.Width, rect.Height), masterCssData);
         LiteHtmlView.LiteHtmlContainer.CaptionDefined += LiteHtmlView_LiteHtmlContainer_CaptionDefined;
         LiteHtmlView.LiteHtmlContainer.DocumentSizeKnown += LiteHtmlView_DocumentSizeKnown;

         scrollView = new NSScrollView();
         scrollView.VerticalScrollElasticity = NSScrollElasticity.None;

         scrollView.AutohidesScrollers = true;
         scrollView.HasHorizontalScroller = true;
         scrollView.HasVerticalScroller = true;
         scrollView.DocumentView = LiteHtmlView;
         scrollView.ContentView.PostsBoundsChangedNotifications = true;

         window.ContentView = scrollView;
         NSNotificationCenter.DefaultCenter.AddObserver(NSView.BoundsChangedNotification, scrollViewScrolled, scrollView.ContentView);

         window.DidResize += LiteHtmlNSWindow_DidResize;
      }

      void scrollViewScrolled(NSNotification ns)
      {
         LiteHtmlView.SetViewport(new CGRect(scrollView.ContentView.Bounds.Location, scrollView.ContentView.Frame.Size));
      }

      void LiteHtmlView_DocumentSizeKnown(LiteHtmlSize size)
      {
         LiteHtmlView.SetFrameSize(new CGSize(scrollView.ContentView.Bounds.Width, size.Height));
         LiteHtmlView.SetViewport(new CGRect(scrollView.ContentView.Bounds.Location, scrollView.ContentView.Frame.Size));
      }

      void LiteHtmlNSWindow_DidResize(object sender, EventArgs e)
      {
         LiteHtmlView.SetViewport(new CGRect(scrollView.ContentView.Bounds.Location, scrollView.ContentView.Frame.Size));
      }

      void LiteHtmlView_LiteHtmlContainer_CaptionDefined(string caption)
      {
         window.Title = caption;
      }

   }
}

