using System;
using System.Windows;
using LiteHtmlSharp.Wpf;
using Example.Shared;

namespace Example.Wpf;

public partial class MainWindow : Window
{
    private readonly LiteHtmlPanel visual;
    public MainWindow()
    {
        InitializeComponent();
        var masterCss = DemoPage.MasterCss;
        var container = new WpfContainer(masterCss, _ => "", DemoPage.LoadImage);
        visual = new LiteHtmlPanel(ScrollViewer, container);
        visual.LinkClicked += url => Status.Text = url == "demo://button" ? "Button activated." : "Link activated.";
        Closed += (_, _) => visual.Dispose();
        visual.Container.Render(DemoPage.Html);

    }

}
