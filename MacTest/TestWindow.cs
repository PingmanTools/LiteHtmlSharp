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
         var containerView = new LiteHtmlView(rect);
         ContentView.AddSubview(containerView);
      }

      #endregion
   }
}
