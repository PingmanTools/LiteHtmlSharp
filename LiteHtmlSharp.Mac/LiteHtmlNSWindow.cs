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
      public CGContainer LiteHtmlContainer  { get { return LiteHtmlView.LiteHtmlContainer; } }

      public LiteHtmlNSView LiteHtmlView { get; private set; }

      public new LiteHtmlNSView ContentView { get { return LiteHtmlView; } set { base.ContentView = value; } }

      // Called when created from unmanaged code
      public LiteHtmlNSWindow(CGRect rect, NSWindowStyle windowStyle, string masterCssData)
         : base(rect, windowStyle, NSBackingStore.Buffered, false)
      {
         DidResize += TestWindow_DidResize;

         LiteHtmlView = new LiteHtmlNSView(new CGRect(0, 0, rect.Width, rect.Height), masterCssData);
         LiteHtmlView.LiteHtmlContainer.CaptionDefined += LiteHtmlView_LiteHtmlContainer_CaptionDefined;
         ContentView = LiteHtmlView;
      }

      void LiteHtmlView_LiteHtmlContainer_CaptionDefined(string caption)
      {
         Title = caption;
      }

      void TestWindow_DidResize(object sender, EventArgs e)
      {
         if (LiteHtmlView != null)
         {
            LiteHtmlView.Frame = ContentView.Bounds;
         }
      }

   }
}
