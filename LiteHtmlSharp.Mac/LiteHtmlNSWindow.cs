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

      public new NSScrollView ContentView
      { 
         get { return (NSScrollView)base.ContentView; } 
         set { base.ContentView = value; } 
      }

      public LiteHtmlNSWindow(CGRect rect, NSWindowStyle windowStyle, string masterCssData)
         : base(rect, windowStyle, NSBackingStore.Buffered, false)
      {
         LiteHtmlView = new LiteHtmlNSView(new CGRect(0, 0, rect.Width, rect.Height), masterCssData);
         LiteHtmlView.LiteHtmlContainer.CaptionDefined += LiteHtmlView_LiteHtmlContainer_CaptionDefined;
         LiteHtmlView.DocumentSizeKnown += LiteHtmlView_DocumentSizeKnown;

         ContentView = new NSScrollView();
         ContentView.AutohidesScrollers = true;
         ContentView.HasHorizontalScroller = true;
         ContentView.HasVerticalScroller = true;
         ContentView.DocumentView = LiteHtmlView;

         DidResize += LiteHtmlNSWindow_DidResize;
      }

      void LiteHtmlNSWindow_DidResize(object sender, EventArgs e)
      {
         LiteHtmlView.Frame = new CGRect(0, 0, ContentView.Bounds.Width, LiteHtmlView.Bounds.Height);
      }

      void LiteHtmlView_DocumentSizeKnown(CGSize size)
      {
         LiteHtmlView.Frame = new CGRect(0, 0, ContentView.Bounds.Width, size.Height);
      }

      void LiteHtmlView_LiteHtmlContainer_CaptionDefined(string caption)
      {
         Title = caption;
      }

   }
}
