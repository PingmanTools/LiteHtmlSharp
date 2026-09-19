using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Example.Shared;
using LiteHtmlSharp.Avalonia;

namespace Example.Avalonia;

/// <summary>A small HTML surface demonstrating hover transitions and toggled animation.</summary>
public sealed class SmallAnimationWindow : Window
{
    private readonly LiteHtmlAvaloniaControl visual;
    private bool animationEnabled;

    public SmallAnimationWindow()
    {
        Title = "LiteHtmlSharp — 800 × 35";
        SizeToContent = SizeToContent.WidthAndHeight;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var viewport = new ScrollViewer
        {
            Width = 800, Height = 35,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            ClipToBounds = true
        };
        var container = new AvaloniaContainer(DemoPage.MasterCss, null);
        visual = new LiteHtmlAvaloniaControl(viewport, container, DemoPage.MasterCss, null);
        viewport.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        visual.LinkClicked += url =>
        {
            if (url != "demo://toggle-animation") return;
            animationEnabled = !animationEnabled;
            Load(animationEnabled);
        };
        Content = new StackPanel
        {
            Margin = new Thickness(20), Spacing = 14,
            Children =
            {
                new TextBlock { Text = "Small HTML surface · 800 × 35 logical pixels" },
                viewport,
                new TextBlock { Text = "Hover the button for a transition. Use the HTML link to start or stop continuous motion.",
                    HorizontalAlignment = HorizontalAlignment.Left }
            }
        };
        Closed += (_, _) => visual.Dispose();
        Load(false);
    }

    private void Load(bool animate)
    {
        visual.Container.Render("""
            <html><head><style>
            html,body { margin:0; padding:0; width:800px; height:35px; overflow:hidden; }
            body { background:#edf3fa; color:#20334f; font-family:Arial; font-size:13px; }
            .label { position:absolute; left:12px; top:9px; }
            .status { position:absolute; left:165px; top:9px; color:#176e65;
                      animation:fade 2s ease-in-out infinite alternate; }
            .track { position:absolute; left:320px; top:13px; width:170px; height:9px;
                     background:#d4dfea; border-radius:5px; }
            .indicator { width:28px; height:9px; background:#259bad; border-radius:5px;
                         animation:move 3s ease-in-out infinite alternate; }
            a { text-decoration:none; }
            .button { position:absolute; left:515px; top:4px; padding:4px 12px;
                      border:1px solid #b3c7df; border-radius:5px; background:#edf3fa; color:#20334f;
                      transition:background-color 180ms linear, border-color 180ms linear; }
            .button:hover { background:#c3e5ee; border-color:#259bad; }
            .toggle { position:absolute; left:650px; top:9px; color:#176e65; text-decoration:underline; }
            @keyframes fade { from { opacity:1; } to { opacity:.3; } }
            @keyframes move { from { transform:translateX(0px); } to { transform:translateX(142px); } }
            """ + (animate ? "" : ".status,.indicator { animation:none; }") + """
            </style></head><body>
            <span class="label">Connection status</span>
            <span class="status">Monitoring…</span>
            <div class="track"><div class="indicator"></div></div>
            <a class="button" href="demo://hover" title="A short hover transition">Hover me</a>
            <a class="toggle" href="demo://toggle-animation">
            """ + (animate ? "Stop animation" : "Start animation") + "</a></body></html>");
    }
}
