using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace LiteHtmlSharp
{
   [UnmanagedFunctionPointer(PInvokes.cc)]
   public delegate void DrawBordersFunc(UIntPtr hdc,ref borders borders,ref position draw_pos,bool root);

   [UnmanagedFunctionPointer(PInvokes.cc, CharSet = PInvokes.cs)]
   public delegate void CallbackFunc(string testString);

   [UnmanagedFunctionPointer(PInvokes.cc, CharSet = PInvokes.cs)]
   public delegate void DrawBackgroundFunc(UIntPtr hdc,string image,background_repeat repeat,ref web_color color,ref position pos);

   [UnmanagedFunctionPointer(PInvokes.cc, CharSet = PInvokes.cs)]
   public delegate void GetImageSizeFunc(string image,ref size size);
}
