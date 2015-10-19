using System;
using System.Runtime.InteropServices;

namespace LiteHtmlSharp
{
   public static class PInvokes
   {
      #if __MonoCS__
      const string LiteHtmlLibFile = "litehtml";
      #else
      const string LiteHtmlLibFile = "LiteHtmlLib.dll";
      #endif

      public const CallingConvention cc = CallingConvention.Cdecl;
      public const CharSet cs = CharSet.Ansi;

      [DllImport(LiteHtmlLibFile, CallingConvention = cc, SetLastError = true)]
      public static extern void Init(ref DocumentCalls document, InitCallbacksFunc initFunc);
   }
}

