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

   [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
   public delegate void DrawBackgroundFunc(UIntPtr hdc, string image, background_repeat repeat, ref web_color color, ref position pos);
}
