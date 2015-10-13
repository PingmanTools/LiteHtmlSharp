using System;

using Foundation;
using AppKit;
using System.Runtime.InteropServices;

namespace MacTest
{
   public partial class AppDelegate : NSApplicationDelegate
   {
      [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
      public delegate void CallbackFunc(int someNumber);

      [DllImport("litehtml", CallingConvention = CallingConvention.Cdecl)]
      public static extern void SetTestFunction(CallbackFunc func);

      public AppDelegate()
      {
         SetTestFunction(n =>
            {
               Console.WriteLine("WORKS: " + n);
            });
      }

      public override void DidFinishLaunching(NSNotification notification)
      {
         var testWindow = new TestWindow(new CoreGraphics.CGRect(50, 50, 500, 500));
         testWindow.MakeKeyAndOrderFront(this);
      }
   }
}

