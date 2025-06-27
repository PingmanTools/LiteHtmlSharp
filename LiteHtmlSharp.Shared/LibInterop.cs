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
        const string LiteHtmlLibFile = "liblitehtml";
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
                return NativeLibrary.Load(LiteHtmlLibFile, assembly, searchPath);
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

