using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using CoreGraphics;
using System.IO;
using System.Diagnostics;
using LiteHtmlSharp;
using System.Text;

namespace LiteHtmlSharp.Mac
{
   public class LiteHtmlNSWindow : NSWindow
   {

      LiteHtmlNSView liteHtmlView;

      // Called when created from unmanaged code
      public LiteHtmlNSWindow(CGRect rect, NSWindowStyle windowStyle)
         : base(rect, windowStyle, NSBackingStore.Buffered, false)
      {
         #if DEBUG
         TestLibLoadTime();
         #endif

         DidResize += TestWindow_DidResize;
         Init();
      }

      void TestWindow_DidResize(object sender, EventArgs e)
      {
         if (liteHtmlView != null)
         {
            liteHtmlView.Frame = ContentView.Bounds;
         }
      }

      void TestLibLoadTime()
      {
         Stopwatch stop = new Stopwatch();
         stop.Start();
         string testStaticCallback = null;
         PInvokes.SetTestFunction(result => testStaticCallback = result);
         if (string.IsNullOrEmpty(testStaticCallback))
         {
            throw new Exception("Container instance callback test failed. Something is really broken!");
         }

         stop.Stop();
         Console.WriteLine("Lib load took: {0} ms", stop.ElapsedTicks / (float)TimeSpan.TicksPerMillisecond);
      }


      void Init()
      {
         Stopwatch stop = new Stopwatch();
         stop.Start();

         liteHtmlView = new LiteHtmlNSView(File.ReadAllText("master.css", Encoding.UTF8));
         liteHtmlView.LiteHtmlContainer.ImportCssCallback = (url, baseUrl) => File.ReadAllText(Path.Combine("WebPage", url), Encoding.UTF8);
         liteHtmlView.LiteHtmlContainer.LoadImageCallback = (url) => File.ReadAllBytes(Path.Combine("WebPage", url));

         ContentView = liteHtmlView;

         var htmlStr = File.ReadAllText(Path.Combine("WebPage", "da900255-ffe1-46f2-a78f-88f6ac671c49.html"));
         liteHtmlView.LiteHtmlContainer.RenderHtml(htmlStr, (int)liteHtmlView.Bounds.Width);

         #if DEBUG
         Action drawnCallback = null;
         drawnCallback = () =>
         {
            liteHtmlView.Drawn -= drawnCallback;
            stop.Stop();
            this.Title = String.Format("draw took: {0} ms", stop.ElapsedMilliseconds);
         };
         liteHtmlView.Drawn += drawnCallback;
         #endif
      }

   }
}
