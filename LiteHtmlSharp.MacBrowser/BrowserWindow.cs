using System;
using AppKit;
using LiteHtmlSharp.Mac;
using CoreGraphics;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;

namespace LiteHtmlSharp.MacBrowser
{
   public class BrowserWindow : NSWindow
   {
      class BrowserContentView : NSView
      {
         public override bool IsFlipped { get { return true; } }
      }

      NSTextField urlInput;
      LiteHtmlNSView LiteHtmlView;
      HttpClient httpClient;

      Uri target;

      public BrowserWindow(CGRect rect, NSWindowStyle windowStyle)
         : base(rect, windowStyle, NSBackingStore.Buffered, false)
      {
         DidResize += TestWindow_DidResize;
         ContentView = new BrowserContentView();

         var masterCssData = File.ReadAllText("master.css", Encoding.UTF8);
         LiteHtmlView = new LiteHtmlNSView(new CGRect(0, 0, rect.Width, rect.Height), masterCssData);
         //LiteHtmlView.LiteHtmlContainer.InvokeOnMainThread = InvokeOnMainThread;
         LiteHtmlView.LiteHtmlContainer.CaptionDefined += LiteHtmlView_LiteHtmlContainer_CaptionDefined;
         LiteHtmlView.LiteHtmlContainer.ImportCssCallback = (url, baseUrl) => Encoding.UTF8.GetString(DownloadResource(url, baseUrl));
         LiteHtmlView.LiteHtmlContainer.LoadImageCallback = (url) => DownloadResource(url);
         LiteHtmlView.LiteHtmlContainer.AnchorClicked += LiteHtmlView_LiteHtmlContainer_AnchorClicked;
         ContentView.AddSubview(LiteHtmlView);

         urlInput = new NSTextField();
         urlInput.Activated += TextField_Activated;
         ContentView.AddSubview(urlInput);

         httpClient = new HttpClient();
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
            content = httpClient.GetByteArrayAsync(url).Result;
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
         //Task.Run(() =>
         //   {
         var html = Encoding.UTF8.GetString(DownloadResource(url.ToString()));
         LiteHtmlView.LoadHtml(html);
         LiteHtmlView.SetNeedsDisplayInRect(LiteHtmlView.Bounds);
         //   });
         
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
         InvokeOnMainThread(() => Title = caption);
      }

      void LayoutViews()
      {
         urlInput.Frame = new CGRect(0, 0, ContentView.Frame.Width, 25);
         LiteHtmlView.Frame = new CGRect(0, urlInput.Frame.Bottom, ContentView.Frame.Width, ContentView.Frame.Height - urlInput.Frame.Height);
      }

      void TestWindow_DidResize(object sender, EventArgs e)
      {
         LayoutViews();
      }
   }
}

