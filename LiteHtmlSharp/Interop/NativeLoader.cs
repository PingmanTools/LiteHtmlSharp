using System.Reflection;
using System.Runtime.InteropServices;
namespace LiteHtmlSharp.Interop;

public static class NativeLoader
{
    private static readonly Lazy<bool> ready = new(() => { NativeLibrary.SetDllImportResolver(typeof(NativeLoader).Assembly, Resolve); return true; });
    internal static void Initialize()
    {
        _ = ready.Value;
    }
    private static nint Resolve(string name, Assembly assembly, DllImportSearchPath? search)
    {
        if (name != "LiteHtmlLib")
            return 0;
        var custom = Environment.GetEnvironmentVariable("LITEHTML_NATIVE_LIBRARY");
        if (!string.IsNullOrWhiteSpace(custom))
            return NativeLibrary.Load(custom);
        var windows = OperatingSystem.IsWindows();
        var mac = OperatingSystem.IsMacOS();
        var file = windows ? "LiteHtmlLib.dll" : mac ? "liblitehtml.dylib" : "liblitehtml.so";
        var rid = (windows ? "win-" : mac ? "osx-" : "linux-") + RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
        var directory = Path.GetDirectoryName(assembly.Location) ?? AppContext.BaseDirectory;
        foreach (var path in new[] { Path.Combine(directory, "runtimes", rid, "native", file), Path.Combine(directory, rid, file), Path.Combine(directory, file) })
            if (File.Exists(path) && NativeLibrary.TryLoad(path, out var h))
                return h;
        return NativeLibrary.Load(file, assembly, search);
    }
}
