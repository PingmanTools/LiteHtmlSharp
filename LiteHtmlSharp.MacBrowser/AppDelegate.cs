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
         //LoadExamplePage();
         OpenBrowserWindow();
      }

      void OpenBrowserWindow()
      {
         var browserWindow = new BrowserWindow(
                                new CGRect(0, 0, 400, 425), 
                                NSWindowStyle.Closable | NSWindowStyle.Titled | NSWindowStyle.Resizable);
         browserWindow.WillClose += (s, e) => NSApplication.SharedApplication.Terminate(this);
         browserWindow.Center();
         browserWindow.MakeKeyAndOrderFront(this);
         browserWindow.GoToUrl(new Uri("example://local/index.html"));
      }

      void LoadExamplePage()
      {
         var testWindow = new LiteHtmlNSWindow(
                             new CGRect(0, 0, 400, 400), 
                             NSWindowStyle.Closable | NSWindowStyle.Titled | NSWindowStyle.Resizable, 
                             File.ReadAllText("master.css", Encoding.UTF8)
                          );
         testWindow.WillClose += (s, e) => NSApplication.SharedApplication.Terminate(this);
         testWindow.Center();
         testWindow.MakeKeyAndOrderFront(this);

         testWindow.LiteHtmlContainer.ImportCssCallback = (url, baseUrl) => File.ReadAllText(Path.Combine("ExampleWebPage", url), Encoding.UTF8);
         testWindow.LiteHtmlContainer.LoadImageCallback = (url) => File.ReadAllBytes(Path.Combine("ExampleWebPage", url));


         Stopwatch stop = new Stopwatch();
         stop.Start();

         var htmlStr = File.ReadAllText(Path.Combine("ExampleWebPage", "index.html"));
         testWindow.LiteHtmlView.LoadHtml(htmlStr);

         Action drawnCallback = null;
         drawnCallback = () =>
         {
            testWindow.LiteHtmlView.Drawn -= drawnCallback;
            stop.Stop();
            var result = String.Format("Cold start took: {0} ms", stop.ElapsedMilliseconds);
            Console.WriteLine(result);
            //testWindow.Title = result;
         };
         testWindow.LiteHtmlView.Drawn += drawnCallback;
      }

   }
}

