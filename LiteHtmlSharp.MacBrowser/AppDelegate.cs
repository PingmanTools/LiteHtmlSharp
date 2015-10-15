using System;
using Foundation;
using AppKit;
using System.Runtime.InteropServices;
using LiteHtmlSharp;
using CoreGraphics;
using System.Diagnostics;
using LiteHtmlSharp.Mac;
using System.IO;
using System.Text;

namespace LiteHtmlSharp.MacBrowser
{
   public partial class AppDelegate : NSApplicationDelegate
   {

      public AppDelegate()
      {
         
      }

      public override void DidFinishLaunching(NSNotification notification)
      {
         #if DEBUG
         TestLibLoadTime();
         Stopwatch stop = new Stopwatch();
         stop.Start();
         #endif


         var masterCss = File.ReadAllText("master.css", Encoding.UTF8);
         var htmlStr = File.ReadAllText(Path.Combine("ExampleWebPage", "index.html"));

         var testWindow = new LiteHtmlNSWindow(new CGRect(0, 0, 400, 400), NSWindowStyle.Titled | NSWindowStyle.Resizable, masterCss);
         testWindow.LiteHtmlContainer.ImportCssCallback = (url, baseUrl) => File.ReadAllText(Path.Combine("ExampleWebPage", url), Encoding.UTF8);
         testWindow.LiteHtmlContainer.LoadImageCallback = (url) => File.ReadAllBytes(Path.Combine("ExampleWebPage", url));
         testWindow.LiteHtmlView.RenderHtml(htmlStr);


         #if DEBUG
         Action drawnCallback = null;
         drawnCallback = () =>
         {
            testWindow.LiteHtmlView.Drawn -= drawnCallback;
            stop.Stop();
            testWindow.Title = String.Format("draw took: {0} ms", stop.ElapsedMilliseconds);
         };
         testWindow.LiteHtmlView.Drawn += drawnCallback;
         #endif


         testWindow.Center();
         testWindow.MakeKeyAndOrderFront(this);
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
   }
}

