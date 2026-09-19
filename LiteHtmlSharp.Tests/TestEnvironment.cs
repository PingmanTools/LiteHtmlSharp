using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace LiteHtmlSharp.Tests;

internal static class TestEnvironment
{
    [ModuleInitializer]
    internal static void ConfigureNativeLibrary()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LITEHTML_NATIVE_LIBRARY")))
            return;

        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "CMakePresets.json")))
            root = root.Parent;
        if (root == null)
            throw new InvalidOperationException("Cannot find the repository native build. Set LITEHTML_NATIVE_LIBRARY explicitly.");

        var preset = OperatingSystem.IsMacOS() ? "osx-universal" :
            OperatingSystem.IsLinux() ? (RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "linux-arm64" : "linux-x64") :
            throw new PlatformNotSupportedException("Set LITEHTML_NATIVE_LIBRARY to the Windows test DLL.");
        var name = OperatingSystem.IsMacOS() ? "liblitehtml.dylib" : "liblitehtml.so";
        var library = Path.Combine(root.FullName, "LiteHtmlLib", "build", preset, name);
        if (!File.Exists(library))
            throw new FileNotFoundException($"Build the native test library using cmake --preset {preset} and cmake --build --preset {preset}.", library);
        Environment.SetEnvironmentVariable("LITEHTML_NATIVE_LIBRARY", library);
    }
}
