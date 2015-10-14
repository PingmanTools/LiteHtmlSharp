using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using CoreGraphics;
using System.IO;
using System.Diagnostics;
using LiteHtmlSharp;

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
      NSButton clearBtn;

      Dictionary<string, object> fileCache;

      // Called when created from unmanaged code
      public TestWindow()
         : base(new CGRect(0, 0, 400, 450), NSWindowStyle.Titled | NSWindowStyle.Resizable, NSBackingStore.Buffered, false)
      {
         TestLibLoadTime();

         this.ContentView = new WindowContentView();
         DidResize += TestWindow_DidResize;

         fileCache = new Dictionary<string, object>();

         drawBtn = new NSButton { BezelStyle = NSBezelStyle.TexturedSquare, Title = "Draw" };
         drawBtn.Activated += (s, e) => Init();
         ContentView.AddSubview(drawBtn);

         clearBtn = new NSButton { BezelStyle = NSBezelStyle.TexturedSquare, Title = "Clear cache & draw" };
         clearBtn.Activated += (s, e) =>
         {
            fileCache.Clear();
            Init();
         };
         ContentView.AddSubview(clearBtn);

         Init();
      }

      CGRect liteHtmlViewFrame()
      {
         return new CGRect(0, 0, ContentView.Bounds.Width, ContentView.Bounds.Height - 50);
      }

      void TestWindow_DidResize(object sender, EventArgs e)
      {
         liteHtmlView.Frame = liteHtmlViewFrame();
         drawBtn.Frame = new CGRect(50, ContentView.Bounds.Height - 40, 100, 30);
         clearBtn.Frame = new CGRect(150, ContentView.Bounds.Height - 40, 100, 30);
         Console.WriteLine("resize");
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
         const float drawTries = 20;

         Stopwatch stop = new Stopwatch();
         stop.Start();

         for (var i = 0; i < drawTries; i++)
         {

            if (liteHtmlView != null)
            {
               liteHtmlView.RemoveFromSuperview();
               liteHtmlView.Dispose();
            }
               
            liteHtmlView = new LiteHtmlView(liteHtmlViewFrame(), (string)FileFromCache("master.css", p => File.ReadAllText(p)));
            liteHtmlView.LiteHtmlContainer.ImportCssCallback = (url, baseUrl) => (string)FileFromCache(Path.Combine("WebPage", url), p => File.ReadAllText(p));
            liteHtmlView.LiteHtmlContainer.LoadImageCallback = (imgUrl) => (byte[])FileFromCache(Path.Combine("WebPage", imgUrl), p => File.ReadAllBytes(p));

            ContentView.AddSubview(liteHtmlView);

            var htmlStr = (string)FileFromCache(Path.Combine("WebPage", "da900255-ffe1-46f2-a78f-88f6ac671c49.html"), p => File.ReadAllText(p));
            liteHtmlView.LiteHtmlContainer.RenderHtml(htmlStr);

         }

         stop.Stop();
         Console.WriteLine("took: {0} ms", stop.ElapsedMilliseconds / drawTries);
      }

      object FileFromCache(string path, Func<string, object> loader)
      {
         object obj;
         if (!fileCache.TryGetValue(path, out obj))
         {
            obj = loader(path);
            fileCache.Add(path, obj);
         }
         return obj;
      }

      #endregion
   }
}
