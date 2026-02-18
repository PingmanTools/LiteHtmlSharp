using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

#if AUTO_UTF8
using Utf8Str = System.String;
#else
using Utf8Str = System.IntPtr;
#endif

namespace LiteHtmlSharp
{
    public class LibInterop : ILibInterop
    {
        const string LiteHtmlLibFile = "LiteHtmlLib";
        public const CharSet cs = CharSet.Unicode;
        public const CallingConvention cc = CallingConvention.Cdecl;

        readonly static Lazy<LibInterop> _instance = new Lazy<LibInterop>(() => new LibInterop());
        public static LibInterop Instance => _instance.Value;

        static LibInterop()
        {
            NativeLibrary.SetDllImportResolver(typeof(LibInterop).Assembly, DllImportResolver);
        }

        LibInterop()
        {
        }

        private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (libraryName == LiteHtmlLibFile)
            {
                // On macOS/Linux, the native library is named liblitehtml.dylib/so
                // On Windows, it's LiteHtmlLib.dll
                string platformLibName = LiteHtmlLibFile;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    platformLibName = "litehtml";
                }

                // Try to load with default resolution first
                if (NativeLibrary.TryLoad(platformLibName, assembly, searchPath, out IntPtr handle))
                {
                    return handle;
                }

                // Fallback: Try to construct the path manually based on RID
                string assemblyDir = System.IO.Path.GetDirectoryName(assembly.Location) ?? "";
                string rid = RuntimeInformation.ProcessArchitecture switch
                {
                    Architecture.X64 => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win-x64" :
                                       RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx-x64" : "linux-x64",
                    Architecture.X86 => "win-x86",
                    Architecture.Arm64 => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win-arm64" :
                                         RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx-arm64" : "linux-arm64",
                    _ => ""
                };

                if (!string.IsNullOrEmpty(rid))
                {
                    string extension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".dll" :
                        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? ".dylib" : ".so";
                    string libPrefix = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "" : "lib";

                    // Simple filename without RID: liblitehtml.dylib or LiteHtmlLib.dll
                    string fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                        ? $"{platformLibName}{extension}"
                        : $"{libPrefix}{platformLibName}{extension}";

                    // Try in standard NuGet runtimes folder structure first
                    string runtimePath = System.IO.Path.Combine(assemblyDir, "runtimes", rid, "native", fileName);
                    if (System.IO.File.Exists(runtimePath) && NativeLibrary.TryLoad(runtimePath, out handle))
                    {
                        return handle;
                    }

                    // Try in RID subdirectory (simplified structure)
                    runtimePath = System.IO.Path.Combine(assemblyDir, rid, fileName);
                    if (System.IO.File.Exists(runtimePath) && NativeLibrary.TryLoad(runtimePath, out handle))
                    {
                        return handle;
                    }

                    // Fallback: Try in assembly directory root (for direct file placement)
                    runtimePath = System.IO.Path.Combine(assemblyDir, fileName);
                    if (System.IO.File.Exists(runtimePath) && NativeLibrary.TryLoad(runtimePath, out handle))
                    {
                        return handle;
                    }
                }

                // If all else fails, throw with helpful error message
#if DEBUG
                throw new DllNotFoundException(
                    $"Unable to load native library '{platformLibName}' for RID '{rid}'.\n" +
                    $"Searched in: {assemblyDir}\n\n" +
                    $"For local development setup instructions, see:\n" +
                    $"https://github.com/PingmanTools/LiteHtmlSharp/blob/master/DEVELOPMENT.md");
#else
                throw new DllNotFoundException(
                    $"Unable to load native library '{platformLibName}' for RID '{rid}'.");
#endif
            }
            return IntPtr.Zero;
        }

        public void InitDocument(ref DocumentCalls document, InitCallbacksFunc initFunc) => Init(ref document, initFunc);

        public Utf8Str LibEchoTest(Utf8Str testStr) => EchoTest(testStr);

        [DllImport(LiteHtmlLibFile, CallingConvention = cc, SetLastError = true)]
        static extern void Init(ref DocumentCalls document, InitCallbacksFunc initFunc);

        [DllImport(LiteHtmlLibFile, CallingConvention = cc, SetLastError = true)]
        static extern int GetWidthTest(Utf8Str docContainer);

        [DllImport(LiteHtmlLibFile, CallingConvention = cc, SetLastError = true)]
        static extern Utf8Str CreateDocContainer();

        [DllImport(LiteHtmlLibFile, CallingConvention = cc, SetLastError = true)]
        static extern Utf8Str EchoTest(Utf8Str testStr);


    }
}
