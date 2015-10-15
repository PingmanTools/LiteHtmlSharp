using System;
using Foundation;
using AppKit;
using System.Runtime.InteropServices;
using LiteHtmlSharp;
using CoreGraphics;
using System.Diagnostics;
using LiteHtmlSharp.Mac;

namespace LiteHtmlSharp.MacBrowser
{
   public partial class AppDelegate : NSApplicationDelegate
   {

      public AppDelegate()
      {
         
      }

      public override void DidFinishLaunching(NSNotification notification)
      {
         var testWindow = new LiteHtmlNSWindow(new CGRect(0, 0, 400, 450), NSWindowStyle.Titled | NSWindowStyle.Resizable);
         testWindow.Center();
         testWindow.MakeKeyAndOrderFront(this);
      }
   }
}

