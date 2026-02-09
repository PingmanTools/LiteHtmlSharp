using AppKit;

namespace Example.Mac
{
   static class MainClass
   {
      static void Main(string[] args)
      {
         NSApplication.Init();
         var app = NSApplication.SharedApplication;
         app.Delegate = new AppDelegate();
         app.Run();
      }
   }
}
