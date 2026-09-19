using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Controls;
using LiteHtmlSharp.Avalonia;
using Example.Shared;

namespace Example.Avalonia;

public partial class MainWindow : Window
{
    private readonly LiteHtmlAvaloniaControl visual;
    private readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(20) };
    private CancellationTokenSource navigation;
    private Uri pageUrl;
    private bool closed;

    public MainWindow()
    {
        InitializeComponent();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("LiteHtmlSharp-Demo/3.0");
        var masterCss = DemoPage.MasterCss;
        var container = new AvaloniaContainer(masterCss, LoadCss, LoadBytes);
        visual = new LiteHtmlAvaloniaControl(HtmlScrollViewer, container, masterCss, null);
        visual.LinkClicked += async url =>
        {
            if (url.StartsWith("demo://", StringComparison.Ordinal))
                Status.Text = url == "demo://button" ? "Button activated." : "Link activated.";
            else await Navigate(url);
        };
        Closed += (_, _) =>
        {
            closed = true;
            navigation?.Cancel();
            visual.Dispose();
            http.Dispose();
        };
        ShowHome();
    }

    private void ShowHome()
    {
        navigation?.Cancel();
        pageUrl = null;
        Address.Text = "";
        visual.Container.SetBaseUrl("");
        HtmlScrollViewer.Offset = default;
        visual.Container.Render(DemoPage.Html);
        Status.Text = "Test page. HTML and CSS only; JavaScript does not run.";
    }

    private void HomeClicked(object sender, RoutedEventArgs e) => ShowHome();
    private async void GoClicked(object sender, RoutedEventArgs e) => await Navigate(Address.Text, true);
    private async void AddressKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        await Navigate(Address.Text, true);
    }

    private async Task Navigate(string address, bool fromAddress = false)
    {
        address = address?.Trim();
        if (string.IsNullOrEmpty(address)) return;
        Uri target;
        if (!Uri.TryCreate(address, UriKind.Absolute, out target))
        {
            if (fromAddress || pageUrl == null || !Uri.TryCreate(pageUrl, address, out target))
                Uri.TryCreate("https://" + address, UriKind.Absolute, out target);
        }
        if (target == null || target.Scheme is not ("http" or "https"))
        {
            Status.Text = "Enter an http:// or https:// address.";
            return;
        }
        navigation?.Cancel();
        var request = new CancellationTokenSource();
        navigation = request;
        Status.Text = "Loading " + target.Host + "…";
        try
        {
            using var response = await http.GetAsync(target, request.Token);
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync(request.Token);
            if (closed || request.IsCancellationRequested) return;
            pageUrl = response.RequestMessage?.RequestUri ?? target;
            Address.Text = pageUrl.AbsoluteUri;
            visual.Container.SetBaseUrl(pageUrl.AbsoluteUri);
            HtmlScrollViewer.Offset = default;
            visual.Container.Render(html);
            Status.Text = "Loaded " + pageUrl.Host + ". HTML and CSS only.";
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested) { }
        catch (Exception error)
        {
            if (!closed && !request.IsCancellationRequested) Status.Text = "Could not load page: " + error.Message;
        }
        finally
        {
            if (ReferenceEquals(navigation, request)) navigation = null;
            request.Dispose();
        }
    }

    private string LoadCss(string url)
    {
        try { return IsWebUrl(url) ? http.GetStringAsync(url).GetAwaiter().GetResult() : ""; }
        catch (HttpRequestException) { return ""; }
        catch (OperationCanceledException) { return ""; }
    }

    private byte[] LoadBytes(string url)
    {
        if (url == "demo://motion.gif") return DemoPage.LoadImage(url);
        try { return IsWebUrl(url) ? http.GetByteArrayAsync(url).GetAwaiter().GetResult() : Array.Empty<byte>(); }
        catch (HttpRequestException) { return Array.Empty<byte>(); }
        catch (OperationCanceledException) { return Array.Empty<byte>(); }
    }

    private static bool IsWebUrl(string url) => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";

}
