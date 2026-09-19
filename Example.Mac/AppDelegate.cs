using AppKit;
using CoreGraphics;
using Foundation;
using System;
using LiteHtmlSharp.Mac;
using Example.Shared;

namespace Example.Mac;

[Register("AppDelegate")]
public class AppDelegate : NSApplicationDelegate
{
    private NSWindow window;
    private LiteHtmlWindowHelper helper;
    public override void DidFinishLaunching(NSNotification notification)
    {
        var menu = new NSMenu();
        var applicationItem = new NSMenuItem();
        menu.AddItem(applicationItem);
        var applicationMenu = new NSMenu();
        applicationMenu.AddItem(
            new NSMenuItem("Quit LiteHtmlSharp", "q", (_, _) => NSApplication.SharedApplication.Terminate(this)));
        applicationItem.Submenu = applicationMenu;
        NSApplication.SharedApplication.MainMenu = menu;
        window = new NSWindow(
            new CGRect(0, 0, 820, 720),
            NSWindowStyle.Titled | NSWindowStyle.Resizable | NSWindowStyle.Closable | NSWindowStyle.Miniaturizable,
            NSBackingStore.Buffered, false) { Title = "LiteHtmlSharp — CoreGraphics",
                                              Appearance = NSAppearance.GetAppearance(NSAppearance.NameAqua) };
        helper = new LiteHtmlWindowHelper(window, window.Frame, DemoPage.MasterCss);
        helper.LiteHtmlView.LiteHtmlContainer.LoadImageDataCallback = DemoPage.LoadImage;
        helper.LiteHtmlView.LinkClicked += url =>
        {
            window.Title = "Activated: " + url;
            Console.WriteLine("Link activated: " + url);
        };
        helper.LiteHtmlView.LiteHtmlContainer.Render(DemoPage.Html);
        window.Center();
        window.MakeKeyAndOrderFront(this);
    }
    public override bool ApplicationShouldTerminateAfterLastWindowClosed(NSApplication sender) => true;
    public override void WillTerminate(NSNotification notification)
    {
        helper?.Dispose();
        window?.Dispose();
    }
}
