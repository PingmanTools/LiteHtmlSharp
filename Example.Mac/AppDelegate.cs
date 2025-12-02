using AppKit;
using CoreGraphics;
using Foundation;

namespace Example.Mac
{
   [Register("AppDelegate")]
   public class AppDelegate : NSApplicationDelegate
   {

      public override void DidFinishLaunching(NSNotification notification)
      {
         var window = new NSWindow(new CGRect(0, 0, 500, 500), NSWindowStyle.Titled | NSWindowStyle.Resizable | NSWindowStyle.Closable, NSBackingStore.Buffered, false);
         var scrollView = new NSScrollView();
         scrollView.BackgroundColor = NSColor.Blue;
         window.ContentView = scrollView;
         window.Center();
         window.MakeKeyAndOrderFront(this);

         // Try to find master.css in multiple locations
         string masterCss = null;
         var resourcePath = NSBundle.MainBundle.PathForResource("master", "css");
         if (resourcePath != null && System.IO.File.Exists(resourcePath))
         {
            masterCss = System.IO.File.ReadAllText(resourcePath);
         }
         else if (System.IO.File.Exists("master.css"))
         {
            masterCss = System.IO.File.ReadAllText("master.css");
         }
         else
         {
            masterCss = "body { font-family: Arial, sans-serif; }"; // Fallback CSS
         }

         var liteHtmlWindow = new LiteHtmlSharp.Mac.LiteHtmlWindowHelper(window, window.Frame, masterCss);
         liteHtmlWindow.LiteHtmlView.LoadHtml(SampleHtml);
      }

      const string SampleHtml = @"
         <html>
            <head></head>
            <body>
               <div style='width:100px; height:100px; background-color:red'></div>
               <p>
                  Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nullam auctor nisi quis ultrices scelerisque. 
                  Mauris imperdiet vehicula metus quis bibendum. Maecenas non erat quis est imperdiet vehicula. Proin 
                  scelerisque mauris purus, elementum sodales tellus imperdiet id. Vivamus luctus lorem nec augue 
                  porttitor, eu mattis nisl laoreet. Cras fringilla vel purus ut imperdiet. Donec luctus finibus elit, 
                  eu elementum purus cursus a. Suspendisse mollis tristique leo a auctor. Vivamus pulvinar pretium 
                  elementum. Donec purus sapien, consequat laoreet eros viverra, laoreet pulvinar ligula. Sed faucibus 
                  nisl odio, sed facilisis odio scelerisque ut.
               </p>
               <p>
                  Nullam dapibus enim vel tortor luctus molestie. Vestibulum non sagittis leo, non vulputate magna. 
                  Aliquam erat volutpat. Nulla hendrerit vel metus nec condimentum. Sed aliquet purus id ipsum interdum 
                  ullamcorper. Nullam congue luctus urna eu bibendum. Morbi non tellus turpis. Mauris nec dui in massa 
                  facilisis imperdiet. Proin metus purus, imperdiet ac laoreet vel, elementum ac nulla. Vivamus dolor 
                  tellus, blandit auctor elementum id, mattis consequat tellus. Vivamus id maximus felis. Praesent 
                  aliquet augue id metus rutrum maximus. Etiam et nulla eu lectus efficitur elementum. Integer porttitor 
                  quis erat sit amet feugiat. In id magna mollis, viverra nibh at, sollicitudin leo.
               </p>
            </body>
         </html>
      ";
   }
}
