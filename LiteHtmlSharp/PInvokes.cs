using System;
using System.Runtime.InteropServices;

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

      public const CallingConvention cc = CallingConvention.Cdecl;

      [DllImport(LiteHtmlLibFile, CallingConvention = cc, SetLastError = true)]
      public static extern void Init(ref DocumentCalls document, InitCallbacksFunc initFunc);
   }
}

