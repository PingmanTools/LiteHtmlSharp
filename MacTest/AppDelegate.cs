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
         var menu = new NSMenu();

         var menuItem = new NSMenuItem();
         menu.AddItem(menuItem);

         var appMenu = new NSMenu();
         var quitItem = new NSMenuItem("Quit " + NSProcessInfo.ProcessInfo.ProcessName, "q", (s, e) => NSApplication.SharedApplication.Terminate(menu));
         appMenu.AddItem(quitItem);

         menuItem.Submenu = appMenu;
         NSApplication.SharedApplication.MainMenu = menu;
      }
   }
}

