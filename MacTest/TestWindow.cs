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
         const string exHtml = @"
<html>
<head></head>
<body>
<div style='width:100px; height:100px; background-color:red'>HELLO WORLD</div>
<div style='margin:20px; width:100px; height:100px; background-color:green'>second</div>
</body>
</html>
";
         var containerView = new LiteHtmlView(rect);

         ContentView.AddSubview(containerView);
         containerView.LiteHtmlContainer.RenderHtml(exHtml);
      }

      #endregion
   }
}
