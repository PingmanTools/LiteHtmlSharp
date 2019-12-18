using LiteHtmlSharp.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Example.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        WpfContainer _litehtmlContainer;
        LiteHtmlPanel _liteHtmlPanel;
        HttpClient _httpClient;
        string _lastUrl;

        public MainWindow()
        {
            InitializeComponent();
            var masterCss = IncludedMasterCss.CssString;

            _httpClient = new HttpClient();

            _litehtmlContainer = new WpfContainer(masterCss, GetResourceString, GetResourceBytes);
            _liteHtmlPanel = new LiteHtmlPanel(ScrollViewer, _litehtmlContainer);
            _liteHtmlPanel.LinkClicked += LiteHtmlPanel_LinkClicked;
            using (var reader = new StreamReader(@"C:\Users\ckindel\source\winprint\tests\Program.cs.html", Encoding.UTF8))
            {
                _liteHtmlPanel.LoadHtml(reader.ReadToEnd());
            }
            //   _liteHtmlPanel.LoadHtml(@"
            //   <html>
            //      <head></head>
            //      <body>
            //         <div><a href='http://www.google.com'>google.com</a></div>
            //         <div><a href='http://www.pingplotter.com'>pingplotter.com</a></div>
            //         <br />
            //         <div style='width:100px; height:100px; background-color:red'></div>
            //         <p>
            //            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nullam auctor nisi quis ultrices scelerisque. 
            //            Mauris imperdiet vehicula metus quis bibendum. Maecenas non erat quis est imperdiet vehicula. Proin 
            //            scelerisque mauris purus, elementum sodales tellus imperdiet id. Vivamus luctus lorem nec augue 
            //            porttitor, eu mattis nisl laoreet. Cras fringilla vel purus ut imperdiet. Donec luctus finibus elit, 
            //            eu elementum purus cursus a. Suspendisse mollis tristique leo a auctor. Vivamus pulvinar pretium 
            //            elementum. Donec purus sapien, consequat laoreet eros viverra, laoreet pulvinar ligula. Sed faucibus 
            //            nisl odio, sed facilisis odio scelerisque ut.
            //         </p>
            //         <p>
            //            Nullam dapibus enim vel tortor luctus molestie. Vestibulum non sagittis leo, non vulputate magna. 
            //            Aliquam erat volutpat. Nulla hendrerit vel metus nec condimentum. Sed aliquet purus id ipsum interdum 
            //            ullamcorper. Nullam congue luctus urna eu bibendum. Morbi non tellus turpis. Mauris nec dui in massa 
            //            facilisis imperdiet. Proin metus purus, imperdiet ac laoreet vel, elementum ac nulla. Vivamus dolor 
            //            tellus, blandit auctor elementum id, mattis consequat tellus. Vivamus id maximus felis. Praesent 
            //            aliquet augue id metus rutrum maximus. Etiam et nulla eu lectus efficitur elementum. Integer porttitor 
            //            quis erat sit amet feugiat. In id magna mollis, viverra nibh at, sollicitudin leo.
            //         </p>
            //      </body>
            //   </html>
            //");
        }

        private void LiteHtmlPanel_LinkClicked(string url)
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
                var pageContent = _httpClient.GetStringAsync(_lastUrl).Result;
                _liteHtmlPanel.LoadHtml(pageContent);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading page. " + (ex.InnerException ?? ex).Message);
            }
        }

        public byte[] GetResourceBytes(string resource)
        {
            if (string.IsNullOrWhiteSpace(resource))
            {
                return new byte[0];
            }

            try
            {
                var url = GetUrlForRequest(resource);
                return _httpClient.GetByteArrayAsync(url).Result;
            }
            catch
            {
                return null;
            }
        }

        public string GetResourceString(string resource)
        {
            if (string.IsNullOrWhiteSpace(resource))
            {
                return string.Empty;
            }
            try
            {
                var url = GetUrlForRequest(resource);
                return _httpClient.GetStringAsync(url).Result;
            }
            catch
            {
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
