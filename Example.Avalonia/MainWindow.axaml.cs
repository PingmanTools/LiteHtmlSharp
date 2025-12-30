using Avalonia.Controls;
using LiteHtmlSharp.Avalonia;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Example.Avalonia
{
    public partial class MainWindow : Window
    {
        AvaloniaContainer _litehtmlContainer;
        LiteHtmlAvaloniaControl _liteHtmlControl;
        HttpClient _httpClient;
        string _lastUrl;
        readonly Dictionary<string, byte[]> _bytesCache = new();
        readonly Dictionary<string, string> _stringCache = new();

        public MainWindow()
        {
            InitializeComponent();

            var masterCss = System.IO.File.ReadAllText("master.css");
            _httpClient = new HttpClient();

            // Create the HTML control
            _litehtmlContainer = new AvaloniaContainer(masterCss, GetResourceString, GetResourceBytes);
            _liteHtmlControl = new LiteHtmlAvaloniaControl(HtmlScrollViewer, _litehtmlContainer, masterCss, null);
            _liteHtmlControl.LinkClicked += LiteHtmlControl_LinkClicked;

            _liteHtmlControl.LoadHtml(@"
                <html>
                    <head></head>
                    <body>
                        <div><a href='http://www.google.com'>Load google.com</a></div>
                        <div><a href='http://www.pingplotter.com'>Load pingplotter.com</a></div>
                        <br />
                        <div style='width:100px; height:100px; background-color:red'></div>
                        <div>
                            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nullam auctor nisi quis ultrices scelerisque.
                            Mauris imperdiet vehicula metus quis bibendum. Maecenas non erat quis est imperdiet vehicula. Proin
                            scelerisque mauris purus, elementum sodales tellus imperdiet id. Vivamus luctus lorem nec augue
                            porttitor, eu mattis nisl laoreet. Cras fringilla vel purus ut imperdiet. Donec luctus finibus elit,
                            eu elementum purus cursus a. Suspendisse mollis tristique leo a auctor. Vivamus pulvinar pretium
                            elementum. Donec purus sapien, consequat laoreet eros viverra, laoreet pulvinar ligula. Sed faucibus
                            nisl odio, sed facilisis odio scelerisque ut.
                        </div>
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
            ");
        }

        private async void LiteHtmlControl_LinkClicked(string url)
        {
            if (url.StartsWith("http:") || url.StartsWith("https:"))
            {
                _lastUrl = url;
            }
            else
            {
                var builder = new UriBuilder(_lastUrl);
                builder.Path = url;
                _lastUrl = builder.ToString();
            }

            try
            {
                var pageContent = await _httpClient.GetStringAsync(_lastUrl);
                _liteHtmlControl.LoadHtml(pageContent);
            }
            catch (Exception ex)
            {
                var messageBox = MessageBoxManager.GetMessageBoxStandard(
                    "Error",
                    "Error loading page. " + (ex.InnerException ?? ex).Message,
                    ButtonEnum.Ok);
                await messageBox.ShowWindowDialogAsync(this);
            }
        }

        public byte[] GetResourceBytes(string resource)
        {
            if (string.IsNullOrWhiteSpace(resource))
                return Array.Empty<byte>();

            var url = GetUrlForRequest(resource);
            if (url == null)
                return null;

            if (_bytesCache.TryGetValue(url, out var cached))
                return cached;

            try
            {
                var bytes = _httpClient.GetByteArrayAsync(url).Result;
                _bytesCache[url] = bytes;
                return bytes;
            }
            catch
            {
                _bytesCache[url] = null; // Cache failures too
                return null;
            }
        }

        public string GetResourceString(string resource)
        {
            if (string.IsNullOrWhiteSpace(resource))
                return string.Empty;

            var url = GetUrlForRequest(resource);
            if (url == null)
                return null;

            if (_stringCache.TryGetValue(url, out var cached))
                return cached;

            try
            {
                var str = _httpClient.GetStringAsync(url).Result;
                _stringCache[url] = str;
                return str;
            }
            catch
            {
                _stringCache[url] = null; // Cache failures too
                return null;
            }
        }

        string GetUrlForRequest(string resource)
        {
            try
            {
                UriBuilder urlBuilder;
                if (resource.StartsWith("//") || resource.StartsWith("http:") || resource.StartsWith("https:"))
                {
                    urlBuilder = new UriBuilder(resource.TrimStart(new char[] { '/' }));
                }
                else
                {
                    urlBuilder = new UriBuilder(_lastUrl);
                    urlBuilder.Path = resource;
                }
                var requestUrl = urlBuilder.ToString();
                return requestUrl;
            }
            catch
            {
                return null;
            }
        }
    }
}