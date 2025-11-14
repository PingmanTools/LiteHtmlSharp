using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LiteHtmlSharp.Avalonia
{
    public static class IncludedMasterCss
    {
        public static string CSS => _includedMasterCss.Value;

        static Lazy<string> _includedMasterCss = new Lazy<string>(GetMasterCssResource);

        static string GetMasterCssResource()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var masterCssResourceName = assembly.GetManifestResourceNames().FirstOrDefault(s => s.Contains("master.css"));
            if (masterCssResourceName != null)
            {
                using (var reader = new StreamReader(assembly.GetManifestResourceStream(masterCssResourceName), Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
            return string.Empty;
        }
    }
}