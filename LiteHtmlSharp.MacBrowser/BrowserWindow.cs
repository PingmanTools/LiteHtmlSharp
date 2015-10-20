using System;
using AppKit;
using LiteHtmlSharp.Mac;
using CoreGraphics;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using Foundation;
using ObjCRuntime;

namespace LiteHtmlSharp.MacBrowser
{
   public class BrowserWindow : NSWindow
   {
      // derive from LiteHtmlNSWindow

      class BrowserContentView : NSView
      {
         public override bool IsFlipped { get { return true; } }
      }


      NSScrollView scrollView;
      NSTextField urlInput;
      LiteHtmlNSView LiteHtmlView;
      HttpClient httpClient = new HttpClient();

      Uri target;

      public BrowserWindow(CGRect rect, NSWindowStyle windowStyle)
         : base(rect, windowStyle, NSBackingStore.Buffered, false)
      {
         DidResize += Window_DidResize;

         var masterCssData = File.ReadAllText("master.css", Encoding.UTF8);
         LiteHtmlView = new LiteHtmlNSView(new CGRect(0, 0, rect.Width, rect.Height), masterCssData);
         LiteHtmlView.LiteHtmlContainer.CaptionDefined += LiteHtmlView_LiteHtmlContainer_CaptionDefined;
         LiteHtmlView.LiteHtmlContainer.ImportCssCallback = (url, baseUrl) => Encoding.UTF8.GetString(DownloadResource(url, baseUrl));
         LiteHtmlView.LiteHtmlContainer.LoadImageCallback = (url) => DownloadResource(url);
         LiteHtmlView.LiteHtmlContainer.AnchorClicked += LiteHtmlView_LiteHtmlContainer_AnchorClicked;
         LiteHtmlView.LiteHtmlContainer.DocumentSizeKnown += LiteHtmlView_DocumentSizeKnown;


         scrollView = new NSScrollView();
         scrollView.AutohidesScrollers = true;
         scrollView.HasHorizontalScroller = true;
         scrollView.HasVerticalScroller = true;
         scrollView.DocumentView = LiteHtmlView;

         urlInput = new NSTextField();
         urlInput.Activated += TextField_Activated;

         ContentView = new BrowserContentView();
         ContentView.AddSubview(scrollView);
         ContentView.AddSubview(urlInput);

         scrollView.ContentView.PostsBoundsChangedNotifications = true;
         NSNotificationCenter.DefaultCenter.AddObserver(NSView.BoundsChangedNotification, scrollViewScrolled, scrollView.ContentView);

         LayoutViews();
      }

      void scrollViewScrolled(NSNotification ns)
      {
         LiteHtmlView.SetViewport(new CGRect(scrollView.ContentView.Bounds.Location, scrollView.Frame.Size));
      }

      void LiteHtmlView_DocumentSizeKnown(CGSize size)
      {
         LiteHtmlView.SetFrameSize(new CGSize(scrollView.Bounds.Width, size.Height));
      }

      void LayoutViews()
      {
         urlInput.Frame = new CGRect(0, 0, ContentView.Frame.Width, 25);
         scrollView.Frame = new CGRect(0, urlInput.Frame.Bottom, ContentView.Frame.Width, ContentView.Frame.Height - urlInput.Frame.Height);
         LiteHtmlView.SetViewport(new CGRect(scrollView.ContentView.Bounds.Location, scrollView.Frame.Size));
      }


      void Window_DidResize(object sender, EventArgs e)
      {
         LayoutViews();
      }

      byte[] DownloadResource(Uri url)
      {
         byte[] content;
         if (url.Scheme == "example")
         {
            content = File.ReadAllBytes(Path.Combine("ExampleWebPage", url.LocalPath.TrimStart('/')));
         }
         else
         {
            try
            {
               content = httpClient.GetByteArrayAsync(url).Result;
            }
            catch (Exception ex)
            {
               return null;
            }
         }

         return content;
      }

      byte[] DownloadResource(string url, string baseUrl = null)
      {
         if (string.IsNullOrEmpty(url))
         {
            return null;
         }
         return DownloadResource(new Uri(target, url));
      }

      public void GoToUrl(Uri url)
      {
         target = url;
         urlInput.StringValue = url.ToString();
         var html = Encoding.UTF8.GetString(DownloadResource(url.ToString()));
         LiteHtmlView.LoadHtml(html);
      }

      void TextField_Activated(object sender, EventArgs e)
      {
         var url = urlInput.StringValue.Trim();
         if (!url.StartsWith("http") && !url.StartsWith("example://"))
         {
            url = "http://" + url;
         }
         GoToUrl(new Uri(url));
      }

      void LiteHtmlView_LiteHtmlContainer_AnchorClicked(string url)
      {
         Uri newUri;
         if (Uri.TryCreate(target, url, out newUri))
         {
            if (newUri.Scheme == "command")
            {
               NSAlert.WithMessage("Command", "Okay", null, null, "Command clicked: " + newUri.Host).RunModal();
            }
            else
            {
               GoToUrl(newUri);
            }
         }
         else
         {
            NSAlert.WithMessage("Error", "Okay", null, null, "Unable to parse url: " + url).RunModal();
         }

      }

      void LiteHtmlView_LiteHtmlContainer_CaptionDefined(string caption)
      {
         Title = caption;
      }

   }
}

