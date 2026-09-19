using System;
using System.IO;

namespace Example.Shared;

internal static class DemoPage
{
    public static string Html { get; } = ReadText("page.html");
    public static string MasterCss { get; } = ReadText("master.css");

    public static byte[] LoadImage(string url)
    {
        if (url != "demo://motion.gif") return Array.Empty<byte>();
        using var stream = Open("motion.gif");
        using var output = new MemoryStream();
        stream.CopyTo(output);
        return output.ToArray();
    }

    private static string ReadText(string name)
    {
        using var stream = Open(name);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static Stream Open(string name) => typeof(DemoPage).Assembly.GetManifestResourceStream(
        "LiteHtmlSharp.Examples." + name) ?? throw new InvalidOperationException("Missing demo resource: " + name);
}
