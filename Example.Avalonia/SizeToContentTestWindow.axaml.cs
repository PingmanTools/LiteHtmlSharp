using System;
using System.IO;
using Avalonia.Controls;
using LiteHtmlSharp;
using LiteHtmlSharp.Avalonia;

namespace Example.Avalonia;

/// <summary>
/// Example window demonstrating how to create a popup/dialog that sizes to fit HTML content.
///
/// Pattern:
/// 1. Set initial window size before rendering (gives litehtml expected viewport for layout)
/// 2. Call Container.SetViewport() with initial size BEFORE LoadHtml
/// 3. Subscribe to DocumentSizeKnown to resize window to actual document size
/// </summary>
public partial class SizeToContentTestWindow : Window
{
    private LiteHtmlAvaloniaControl _visual;
    private static readonly string MasterCss = File.Exists("master.css") ? File.ReadAllText("master.css") : "";

    // Initial size (in real usage, this comes from config/bundle metadata)
    private const int InitialWidth = 400;
    private const int InitialHeight = 300;

    public SizeToContentTestWindow()
    {
        InitializeComponent();

        // Set initial window size
        Width = InitialWidth;
        Height = InitialHeight;

        // Create the visual
        _visual = CreateVisual(canvas);

        // Render HTML content
        RenderContent();
    }

    private LiteHtmlAvaloniaControl CreateVisual(ScrollViewer scrollViewer)
    {
        var visual = new LiteHtmlAvaloniaControl(scrollViewer, null, MasterCss, null);

        // Subscribe to DocumentSizeKnown to resize window to fit content
        visual.Container.DocumentSizeKnown += Container_DocumentSizeKnown;

        return visual;
    }

    private void Container_DocumentSizeKnown(LiteHtmlSize size)
    {
        // Resize window to fit document content (both width and height)
        Width = size.Width;
        Height = size.Height;
    }

    private void RenderContent()
    {
        // Set viewport with initial size BEFORE LoadHtml
        _visual.Container.SetViewport(
            new LiteHtmlPoint(0, 0),
            new LiteHtmlSize(InitialWidth, InitialHeight));

        var html = @"
            <html>
            <head>
                <style>
                    body { margin: 0; padding: 20px; background: #f0f0f0; }
                    .box { background: #4a90d9; color: white; padding: 20px; margin: 10px 0; }
                </style>
            </head>
            <body>
                <h1>Size To Content Example</h1>
                <div class='box'>
                    This window resizes to fit the HTML content.
                    Initial viewport size determines text flow/wrapping.
                </div>
                <div class='box'>
                    <p>Paragraph 1: Lorem ipsum dolor sit amet, consectetur adipiscing elit.</p>
                    <p>Paragraph 2: Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.</p>
                    <p>Paragraph 3: Ut enim ad minim veniam, quis nostrud exercitation.</p>
                </div>
            </body>
            </html>
        ";

        _visual.LoadHtml(html);
    }
}
