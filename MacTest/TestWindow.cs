using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using CoreGraphics;

namespace MacTest
{
   public partial class TestWindow : AppKit.NSWindow
   {
      #region Constructors

      // Called when created from unmanaged code
      public TestWindow(CGRect rect)
         : base(rect, NSWindowStyle.Titled, NSBackingStore.Buffered, false)
      {
         const string exHtml = "<html><head></head><body><div style='width:100px; height:100px; background-color:red'>HELLO WORLD</div></body></html>";
         var containerView = new LiteHtmlView(rect);

         ContentView.AddSubview(containerView);

         NSTimer.CreateScheduledTimer(TimeSpan.FromSeconds(0.1), t =>
            {
               containerView.LiteHtmlContainer.RenderHtml(exHtml);
            });
      }

      #endregion
   }
}
