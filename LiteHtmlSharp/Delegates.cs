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
   public delegate void DrawBackgroundFunc(UIntPtr hdc,string image,background_repeat repeat,ref web_color color,ref position pos,ref border_radiuses borderRadiuses,ref position borderBox);

   [UnmanagedFunctionPointer(PInvokes.cc, CharSet = PInvokes.cs)]
   public delegate void GetImageSizeFunc(string image,ref size size);

   [UnmanagedFunctionPointer(PInvokes.cc, CharSet = PInvokes.cs)]
   public delegate void SetCaptionFunc(string caption);

   [UnmanagedFunctionPointer(PInvokes.cc, CharSet = PInvokes.cs)]
   public delegate void DrawTextFunc(string text,UIntPtr font,ref web_color color,ref position pos);

   [UnmanagedFunctionPointer(PInvokes.cc, CharSet = PInvokes.cs)]
   public delegate int GetTextWidthFunc(string text,UIntPtr font);

   [UnmanagedFunctionPointer(PInvokes.cc, CharSet = PInvokes.cs)]
   public delegate int GetDefaultFontSizeFunc();

   [UnmanagedFunctionPointer(PInvokes.cc, CharSet = PInvokes.cs)]
   public delegate string GetDefaultFontNameFunc();

   [UnmanagedFunctionPointer(PInvokes.cc, CharSet = PInvokes.cs)]
   public delegate UIntPtr CreateFontFunc(string faceName,int size,int weight,font_style italic,uint decoration,ref font_metrics fm);

   [UnmanagedFunctionPointer(PInvokes.cc, CharSet = PInvokes.cs)]
   public delegate string ImportCssFunc(string url,string baseurl);

   [UnmanagedFunctionPointer(PInvokes.cc, CharSet = PInvokes.cs)]
   public delegate string TransformTextFunc(string text,text_transform tt);

   [UnmanagedFunctionPointer(PInvokes.cc, CharSet = PInvokes.cs)]
   public delegate void GetClientRectFunc(ref position client);

   [UnmanagedFunctionPointer(PInvokes.cc, CharSet = PInvokes.cs)]
   public delegate void GetMediaFeaturesFunc(ref media_features media);

   [UnmanagedFunctionPointer(PInvokes.cc, CharSet = PInvokes.cs)]
   public delegate void SetBaseURLFunc(ref string base_url);

   [UnmanagedFunctionPointer(PInvokes.cc, CharSet = PInvokes.cs)]
   public delegate void OnAnchorClickFunc(string url);

   [UnmanagedFunctionPointer(PInvokes.cc, CharSet = PInvokes.cs)]
   public delegate void SetCursorFunc(string cursor);

   [UnmanagedFunctionPointer(PInvokes.cc, CharSet = PInvokes.cs)]
   public delegate int PTtoPXFunct(int pt);

   [UnmanagedFunctionPointer(PInvokes.cc, CharSet = PInvokes.cs)]
   public delegate int CreateElementFunc(string tag,string attributes);
}
