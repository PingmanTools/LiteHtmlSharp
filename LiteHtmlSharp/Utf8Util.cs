using System.Runtime.InteropServices;
namespace LiteHtmlSharp;

public static class Utf8Util
{
    public static string? Utf8PtrToString(nint pointer) => Marshal.PtrToStringUTF8(pointer);
    public static nint StringToHGlobalUTF8(string? text)
    {
        if (text is null)
            return 0;
        var bytes = System.Text.Encoding.UTF8.GetBytes(text + '\0');
        var p = Marshal.AllocHGlobal(bytes.Length);
        Marshal.Copy(bytes, 0, p, bytes.Length);
        return p;
    }
}
