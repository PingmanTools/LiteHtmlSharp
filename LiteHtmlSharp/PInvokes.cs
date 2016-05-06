using System;
using System.Runtime.InteropServices;

namespace LiteHtmlSharp
{
   public static class PInvokes
   {
#if __MonoCS__
      const string LiteHtmlLibFile = "litehtml";
      public const CharSet cs = CharSet.Ansi;
#elif WIN64
      const string LiteHtmlLibFile = "x64\\LiteHtmlLib.dll";
      public const CharSet cs = CharSet.Unicode;
#else
      const string LiteHtmlLibFile = "x86\\LiteHtmlLib.dll";
      public const CharSet cs = CharSet.Unicode;
#endif

      public const CallingConvention cc = CallingConvention.Cdecl;

      [DllImport(LiteHtmlLibFile, CallingConvention = cc, SetLastError = true)]
      public static extern void Init(ref DocumentCalls document, InitCallbacksFunc initFunc);
   }
}

