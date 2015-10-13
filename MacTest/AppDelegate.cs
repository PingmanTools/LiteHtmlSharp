using System;

using Foundation;
using AppKit;
using System.Runtime.InteropServices;
using LiteHtmlSharp;

namespace MacTest
{
   public partial class AppDelegate : NSApplicationDelegate
   {

      public AppDelegate()
      {

      }

      public override void DidFinishLaunching(NSNotification notification)
      {
         var testWindow = new TestWindow(new CoreGraphics.CGRect(0, 0, 700, 500));
         testWindow.Center();
         testWindow.MakeKeyAndOrderFront(this);
      }
   }
}

