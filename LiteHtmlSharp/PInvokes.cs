using System;
using System.Runtime.InteropServices;
using System.Text;

#if __MonoCS__
using Utf8Str = System.String;
#else
using Utf8Str = System.IntPtr;
#endif

namespace LiteHtmlSharp
{
   public static class PInvokes
   {
#if __MonoCS__
      const string LiteHtmlLibFile = "litehtml";
      public const CharSet cs = CharSet.Ansi;

#else
      const string LiteHtmlLibFile = "LiteHtmlLib.dll";
      public const CharSet cs = CharSet.Unicode;
#endif

      const string LiteHtmlLibFile_x64 = "x64\\LiteHtmlLib.dll";
      const string LiteHtmlLibFile_x86 = "x86\\LiteHtmlLib.dll";

      public const CallingConvention cc = CallingConvention.Cdecl;

      static PInvokes()
      {
         if (Environment.OSVersion.Platform == PlatformID.Win32NT)
         {
            var is64Bit = Environment.Is64BitProcess;
            LoadLibrary(is64Bit ? LiteHtmlLibFile_x64 : LiteHtmlLibFile_x86);
         }
      }

      [DllImport("kernel32.dll")]
      private static extern IntPtr LoadLibrary(string dllToLoad);

      [DllImport(LiteHtmlLibFile, CallingConvention = cc, SetLastError = true)]
      public static extern void Init(ref DocumentCalls document, InitCallbacksFunc initFunc);

      [DllImport(LiteHtmlLibFile, CallingConvention = cc, SetLastError = true)]
      public static extern int GetWidthTest(Utf8Str docContainer);

      [DllImport(LiteHtmlLibFile, CallingConvention = cc, SetLastError = true)]
      public static extern Utf8Str CreateDocContainer();

      [DllImport(LiteHtmlLibFile, CallingConvention = cc, SetLastError = true)]
      public static extern Utf8Str EchoTest(Utf8Str testStr);


   }
}

