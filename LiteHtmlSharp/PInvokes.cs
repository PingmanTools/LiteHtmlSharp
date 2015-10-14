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


      #region Tests

      [DllImport(LiteHtmlLibFile, CallingConvention = cc)]
      public static extern void SetTestFunction(CallbackFunc func);

      [DllImport(LiteHtmlLibFile, CallingConvention = cc)]
      public static extern void SetTestCallback(IntPtr container, CallbackFunc func);

      [DllImport(LiteHtmlLibFile, CallingConvention = cc, CharSet = cs)]
      public static extern void TriggerTestCallback(IntPtr container, string testString);

      #endregion


      #region Callback Injection

      [DllImport(LiteHtmlLibFile, CallingConvention = cc)]
      public static extern void SetDrawBorders(IntPtr container, DrawBordersFunc func);

      [DllImport(LiteHtmlLibFile, CallingConvention = cc)]
      public static extern void SetDrawBackground(IntPtr container, DrawBackgroundFunc func);

      [DllImport(LiteHtmlLibFile, CallingConvention = cc)]
      public static extern void SetGetImageSize(IntPtr container, GetImageSizeFunc func);

      [DllImport(LiteHtmlLibFile, CallingConvention = cc, CharSet = cs, SetLastError = true)]
      public static extern void SetDrawText(IntPtr container, DrawTextFunc func);

      [DllImport(LiteHtmlLibFile, CallingConvention = cc, CharSet = cs)]
      public static extern void SetCreateFont(IntPtr container, CreateFontFunc func);

      [DllImport(LiteHtmlLibFile, CallingConvention = cc, CharSet = cs)]
      public static extern void SetGetTextWidth(IntPtr container, GetTextWidthFunc func);

      [DllImport(LiteHtmlLibFile, CallingConvention = cc, CharSet = cs)]
      public static extern void SetImportCss(IntPtr container, ImportCssFunc func);

      [DllImport(LiteHtmlLibFile, CallingConvention = cc, CharSet = cs)]
      public static extern void SetGetClientRect(IntPtr container, GetClientRectFunc func);

      [DllImport(LiteHtmlLibFile, CallingConvention = cc, CharSet = cs)]
      public static extern void SetGetMediaFeatures(IntPtr container, GetMediaFeaturesFunc func);

      [DllImport(LiteHtmlLibFile, CallingConvention = cc, CharSet = cs)]
      public static extern void SetSetBaseURL(IntPtr container, SetBaseURLFunc func);

      [DllImport(LiteHtmlLibFile, CallingConvention = cc, CharSet = cs)]
      public static extern void SetOnAnchorClick(IntPtr container, OnAnchorClickFunc func);

      [DllImport(LiteHtmlLibFile, CallingConvention = cc, CharSet = cs)]
      public static extern void SetPTtoPX(IntPtr container, PTtoPXFunct func);

      [DllImport(LiteHtmlLibFile, CallingConvention = cc, CharSet = cs)]
      public static extern void SetCreateElement(IntPtr container, CreateElementFunc func);
      #endregion


      #region Instance Methods

      [DllImport(LiteHtmlLibFile, CallingConvention = cc, CharSet = cs, SetLastError = true)]
      public static extern void RenderHTML(IntPtr container, string html, int maxWidth);

      [DllImport(LiteHtmlLibFile, CallingConvention = cc, CharSet = cs)]
      public static extern void SetMasterCSS(IntPtr container, string css);

      [DllImport(LiteHtmlLibFile, CallingConvention = cc, SetLastError = true)]
      public static extern bool OnMouseMove(IntPtr container, int x, int y);

      [DllImport(LiteHtmlLibFile, CallingConvention = cc, SetLastError = true)]
      public static extern bool OnMouseLeave(IntPtr container);

      [DllImport(LiteHtmlLibFile, CallingConvention = cc, SetLastError = true)]
      public static extern bool OnLeftButtonUp(IntPtr container, int x, int y);

      [DllImport(LiteHtmlLibFile, CallingConvention = cc, SetLastError = true)]
      public static extern bool OnLeftButtonDown(IntPtr container, int x, int y);

      [DllImport(LiteHtmlLibFile, CallingConvention = cc, SetLastError = true)]
      public static extern void Draw(IntPtr container, int x, int y, position clip);

      [DllImport(LiteHtmlLibFile, CallingConvention = cc, SetLastError = true)]
      public static extern ElementInfo GetElementInfo(IntPtr container, int ID);
      #endregion



      [DllImport(LiteHtmlLibFile, CallingConvention = cc, SetLastError = true)]
      public static extern IntPtr Init();
   }
}

