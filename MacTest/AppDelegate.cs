using System;

using Foundation;
using AppKit;
using System.Runtime.InteropServices;
using LiteHtmlSharp;
using CoreGraphics;
using System.Diagnostics;

namespace MacTest
{
   public static class Log
   {
      public static void W(string msg)
      {
         Console.WriteLine(Stopwatch.GetTimestamp() + ": " + msg);
      }
   }

   public partial class AppDelegate : NSApplicationDelegate
   {

      public AppDelegate()
      {

      }

      public override void DidFinishLaunching(NSNotification notification)
      {
         var testWindow = new TestWindow();
         testWindow.Center();
         testWindow.MakeKeyAndOrderFront(this);
      }
   }
}

