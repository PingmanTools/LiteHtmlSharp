using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LiteHtmlSharp
{
   public abstract class Container
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

      protected IntPtr CPPContainer;

      public Container()
      {
         CPPContainer = Init();

         SetDrawBorders(CPPContainer, DrawBorders);
         SetDrawBackground(CPPContainer, DrawBackground);
      }

      protected abstract void DrawBackground(UIntPtr hdc, string image, background_repeat repeat, ref web_color color, ref position pos);
      protected abstract void DrawBorders(UIntPtr hdc, ref borders borders, ref position draw_pos, bool root);
   }
}
