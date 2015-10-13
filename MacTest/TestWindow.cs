using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using CoreGraphics;
using System.IO;

namespace MacTest
{
   public partial class TestWindow : AppKit.NSWindow
   {
      #region Constructors

      // Called when created from unmanaged code
      public TestWindow(CGRect rect)
         : base(rect, NSWindowStyle.Titled, NSBackingStore.Buffered, false)
      {
         string html = File.ReadAllText(Path.Combine("WebPage", "da900255-ffe1-46f2-a78f-88f6ac671c49.html"));

         var containerView = new LiteHtmlView(rect);
         containerView.LiteHtmlContainer.ImportCssCallback = (url, baseUrl) => File.ReadAllText(Path.Combine("WebPage", url));
         containerView.LiteHtmlContainer.LoadImageCallback = (imgUrl) => File.ReadAllBytes(Path.Combine("WebPage", imgUrl));

         ContentView.AddSubview(containerView);
         containerView.LiteHtmlContainer.RenderHtml(html);
      }

      #endregion
   }
}
