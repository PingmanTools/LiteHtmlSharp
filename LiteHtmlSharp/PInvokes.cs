using System;
using System.Runtime.InteropServices;

namespace LiteHtmlSharp
{
   public static class PInvokes
   {
      [DllImport("LiteHtmlLib.dll", CallingConvention = CallingConvention.Cdecl)]
      public static extern void SetTestFunction(CallbackFunc func);

      [DllImport("LiteHtmlLib.dll", CallingConvention = CallingConvention.Cdecl)]
      public static extern void SetDrawBorders(IntPtr container, DrawBordersFunc func);

      [DllImport("LiteHtmlLib.dll", CallingConvention = CallingConvention.Cdecl)]
      public static extern void SetDrawBackground(IntPtr container, DrawBackgroundFunc func);

      [DllImport("LiteHtmlLib.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
      public static extern void RenderHTML(IntPtr container, string html);

      [DllImport("LiteHtmlLib.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
      public static extern void SetMasterCSS(IntPtr container, string css);

      [DllImport("LiteHtmlLib.dll", CallingConvention = CallingConvention.Cdecl)]
      public static extern IntPtr Init();
   }
}

