using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using CoreGraphics;
using System.IO;
using System.Diagnostics;
using LiteHtmlSharp;
using LiteHtmlSharp.CoreGraphics;
using System.Text;

namespace LiteHtmlSharp.Mac
{
   public class LiteHtmlNSWindow : NSWindow
   {
      public CGContainer LiteHtmlContainer { get { return LiteHtmlView.LiteHtmlContainer; } }

      public LiteHtmlNSView LiteHtmlView { get; private set; }

      NSScrollView scrollView;

      public LiteHtmlNSWindow(CGRect rect, NSWindowStyle windowStyle, string masterCssData)
         : base(rect, windowStyle, NSBackingStore.Buffered, false)
      {
         LiteHtmlView = new LiteHtmlNSView(new CGRect(0, 0, rect.Width, rect.Height), masterCssData);
         LiteHtmlView.LiteHtmlContainer.CaptionDefined += LiteHtmlView_LiteHtmlContainer_CaptionDefined;
         LiteHtmlView.LiteHtmlContainer.DocumentSizeKnown += LiteHtmlView_DocumentSizeKnown;

         scrollView = new NSScrollView();
         scrollView.AutohidesScrollers = true;
         scrollView.HasHorizontalScroller = true;
         scrollView.HasVerticalScroller = true;
         scrollView.DocumentView = LiteHtmlView;
         scrollView.ContentView.PostsBoundsChangedNotifications = true;

         ContentView = scrollView;
         NSNotificationCenter.DefaultCenter.AddObserver(NSView.BoundsChangedNotification, scrollViewScrolled, scrollView.ContentView);

         DidResize += LiteHtmlNSWindow_DidResize;
      }

      void scrollViewScrolled(NSNotification ns)
      {
         LiteHtmlView.SetViewport(new CGRect(scrollView.ContentView.Bounds.Location, scrollView.ContentView.Frame.Size));
      }

      void LiteHtmlView_DocumentSizeKnown(CGSize size)
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
         Title = caption;
      }

   }
}
