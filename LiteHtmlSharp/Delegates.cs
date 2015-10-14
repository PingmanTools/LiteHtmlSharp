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

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate void DrawTextFunc(string text, UIntPtr font, ref web_color color, ref position pos);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate int GetTextWidthFunc(string text, UIntPtr font);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate UIntPtr CreateFontFunc(string faceName, int size, int weight, font_style italic, uint decoration, ref font_metrics fm);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate string ImportCssFunc(string url, string baseurl);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate void GetClientRectFunc(ref position client);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate void GetMediaFeaturesFunc(ref media_features media);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate void SetBaseURLFunc(ref string base_url);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate void OnAnchorClickFunc(ref string url);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate int PTtoPXFunct(int pt);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate int CreateElementFunc(string tag);
}
