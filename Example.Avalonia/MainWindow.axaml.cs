using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using LiteHtmlSharp.Avalonia;
using System;
using System.Net.Http;

namespace Example.Avalonia
{
    public partial class MainWindow : Window
    {
        AvaloniaContainer _litehtmlContainer;
        LiteHtmlAvaloniaControl _liteHtmlControl;
        HttpClient _httpClient;
        string _lastUrl;

        public MainWindow()
        {
            InitializeComponent();
            
            var masterCss = IncludedMasterCss.CSS;
            _httpClient = new HttpClient();

            // Create the HTML control
            _litehtmlContainer = new AvaloniaContainer(masterCss, GetResourceString, GetResourceBytes);
            _liteHtmlControl = new LiteHtmlAvaloniaControl(HtmlScrollViewer, _litehtmlContainer);
            _liteHtmlControl.LinkClicked += LiteHtmlControl_LinkClicked;
            
            _liteHtmlControl.LoadHtml("<p style='line-height:30px'>Enter your name: <input type='text' id='nameInput' value='' style='width:150px; vertical-align: baseline;'></p>\n");

            // Load HTML with interactive elements
            // _liteHtmlControl.LoadHtml(@"
            //     <html>
            //         <head></head>
            //         <body>
            //             <div><a href='http://www.google.com'>google.com</a></div>
            //             <div><a href='http://www.github.com'>github.com</a></div>
            //             <br />
            //             
            //             <h2>Interactive Elements Test</h2>
            //             <p>Enter your name: <input type='text' id='nameInput' value='Type here...' style='width:150px;'></p>
            //             <p><button id='submitButton' value='Click Me!' style='width:80px;height:30px;'>Click Me!</button> <button id='clearButton' value='Clear'>Clear</button></p>
            //             <br />
            //             <div style='width:200px; height:100px; background-color:red; margin:10px;'></div>
            //             <div style='width:200px; height:100px; background-color:blue; margin:10px;'></div>
            //             <div style='width:200px; height:100px; background-color:green; margin:10px;'></div>
            //             
            //             <h2>Lorem Ipsum Text - Section 1</h2>
            //             <p>
            //                 Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nullam auctor nisi quis ultrices scelerisque. 
            //                 Mauris imperdiet vehicula metus quis bibendum. Maecenas non erat quis est imperdiet vehicula. Proin 
            //                 scelerisque mauris purus, elementum sodales tellus imperdiet id. Vivamus luctus lorem nec augue 
            //                 porttitor, eu mattis nisl laoreet. Cras fringilla vel purus ut imperdiet. Donec luctus finibus elit, 
            //                 eu elementum purus cursus a. Suspendisse mollis tristique leo a auctor. Vivamus pulvinar pretium 
            //                 elementum. Donec purus sapien, consequat laoreet eros viverra, laoreet pulvinar ligula. Sed faucibus 
            //                 nisl odio, sed facilisis odio scelerisque ut.
            //             </p>
            //             
            //             <h2>Lorem Ipsum Text - Section 2</h2>
            //             <p>
            //                 Nullam dapibus enim vel tortor luctus molestie. Vestibulum non sagittis leo, non vulputate magna. 
            //                 Aliquam erat volutpat. Nulla hendrerit vel metus nec condimentum. Sed aliquet purus id ipsum interdum 
            //                 ullamcorper. Nullam congue luctus urna eu bibendum. Morbi non tellus turpis. Mauris nec dui in massa 
            //                 facililis imperdiet. Proin metus purus, imperdiet ac laoreet vel, elementum ac nulla. Vivamus dolor 
            //                 tellus, blandit auctor elementum id, mattis consequat tellus. Vivamus id maximus felis. Praesent 
            //                 aliquet augue id metus rutrum maximus. Etiam et nulla eu lectus efficitur elementum. Integer porttitor 
            //                 quis erat sit amet feugiat. In id magna mollis, viverra nibh at, sollicitudin leo.
            //             </p>
            //             
            //             <h2>Lorem Ipsum Text - Section 3</h2>
            //             <p>
            //                 Sed ut perspiciatis unde omnis iste natus error sit voluptatem accusantium doloremque laudantium, 
            //                 totam rem aperiam, eaque ipsa quae ab illo inventore veritatis et quasi architecto beatae vitae dicta sunt explicabo. 
            //                 Nemo enim ipsam voluptatem quia voluptas sit aspernatur aut odit aut fugit, sed quia consequuntur magni dolores eos 
            //                 qui ratione voluptatem sequi nesciunt. Neque porro quisquam est, qui dolorem ipsum quia dolor sit amet, consectetur, 
            //                 adipisci velit, sed quia non numquam eius modi tempora incidunt ut labore et dolore magnam aliquam quaerat voluptatem.
            //             </p>
            //             
            //             <h2>The End</h2>
            //             <p>This should be enough content to test scrolling!</p>
            //         </body>
            //     </html>");
        }

        private void LiteHtmlControl_LinkClicked(string url)
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
                _liteHtmlControl.LoadHtml(pageContent);
            }
            catch (Exception ex)
            {
                // In a real app, you'd want better error handling
                System.Diagnostics.Debug.WriteLine("Error loading page: " + (ex.InnerException ?? ex).Message);
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