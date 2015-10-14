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

namespace MacTest
{
   public partial class TestWindow : NSWindow
   {
      #region Constructors

      class WindowContentView : NSView
      {
         public override bool IsFlipped { get { return true; } }
      }

      LiteHtmlView liteHtmlView;
      NSButton drawBtn;


      // Called when created from unmanaged code
      public TestWindow()
         : base(new CGRect(0, 0, 400, 450), NSWindowStyle.Titled | NSWindowStyle.Resizable, NSBackingStore.Buffered, false)
      {
         TestLibLoadTime();

         this.ContentView = new WindowContentView();
         DidResize += TestWindow_DidResize;

         drawBtn = new NSButton { BezelStyle = NSBezelStyle.TexturedSquare, Title = "Draw" };
         drawBtn.Activated += (s, e) => Init();
         ContentView.AddSubview(drawBtn);

         NSTimer.CreateScheduledTimer(TimeSpan.FromMilliseconds(20), t => Init());
      }

      CGRect liteHtmlViewFrame()
      {
         return new CGRect(0, 0, ContentView.Bounds.Width, ContentView.Bounds.Height - 50);
      }

      void TestWindow_DidResize(object sender, EventArgs e)
      {
         drawBtn.Frame = new CGRect(50, ContentView.Bounds.Height - 40, 100, 30);
         if (liteHtmlView != null)
         {
            liteHtmlView.Frame = liteHtmlViewFrame();
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

         if (liteHtmlView != null)
         {
            liteHtmlView.RemoveFromSuperview();
            liteHtmlView.Dispose();
         }

         liteHtmlView = new LiteHtmlView(liteHtmlViewFrame(), File.ReadAllText("master.css", Encoding.UTF8));
         liteHtmlView.LiteHtmlContainer.ImportCssCallback = (url, baseUrl) => File.ReadAllText(Path.Combine("WebPage", url), Encoding.UTF8);
         liteHtmlView.LiteHtmlContainer.LoadImageCallback = (url) => File.ReadAllBytes(Path.Combine("WebPage", url));

         ContentView.AddSubview(liteHtmlView);

         var htmlStr = File.ReadAllText(Path.Combine("WebPage", "da900255-ffe1-46f2-a78f-88f6ac671c49.html"));
         liteHtmlView.LiteHtmlContainer.RenderHtml(htmlStr, (int)liteHtmlView.Bounds.Width);
         Action drawnCallback = null;
         drawnCallback = () =>
         {
            liteHtmlView.Drawn -= drawnCallback;
            stop.Stop();
            Console.WriteLine("total litehtml init->draw took: {0} ms", stop.ElapsedMilliseconds);
         };
         liteHtmlView.Drawn += drawnCallback;

      }


      #endregion
   }
}
