using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace LiteHtmlSharp
{
   [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
   public delegate void DrawBordersFunc(UIntPtr hdc, ref borders borders, ref position draw_pos, bool root);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
   public delegate void CallbackFunc(int someNumber);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
   public delegate void DrawBackgroundFunc(UIntPtr hdc, string image, background_repeat repeat, ref web_color color, ref position pos);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
   public delegate void GetImageSizeFunc(string image, ref size size);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
   public delegate void DrawTextFunc(string text, UIntPtr font, ref web_color color, ref position pos);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
   public delegate int GetTextWidthFunc(string text, UIntPtr font);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
   public delegate UIntPtr CreateFontFunc(string faceName, int size, int weight, font_style italic, uint decoration, ref font_metrics fm);
}
