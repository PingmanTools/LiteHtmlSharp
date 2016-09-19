using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace LiteHtmlSharp
{
   // test methods
   [UnmanagedFunctionPointer(PInvokes.cc, CharSet = PInvokes.cs)]
   public delegate void TestCallbackFunc(int x, IntPtr testString);

   [UnmanagedFunctionPointer(PInvokes.cc, CharSet = PInvokes.cs)]
   public delegate void TriggerTestCallbackFunc(IntPtr container, int x, IntPtr testString);

   // callbacks
   [UnmanagedFunctionPointer(PInvokes.cc, CharSet = PInvokes.cs)]
   public delegate void SetCaptionFunc(IntPtr caption);

   [UnmanagedFunctionPointer(PInvokes.cc, CharSet = PInvokes.cs)]
   public delegate int GetDefaultFontSizeFunc();

   [UnmanagedFunctionPointer(PInvokes.cc, CharSet = PInvokes.cs)]
   public delegate IntPtr GetDefaultFontNameFunc();

   [UnmanagedFunctionPointer(PInvokes.cc)]
   public delegate void DrawBordersFunc(UIntPtr hdc, ref borders borders, ref position draw_pos, [MarshalAs(UnmanagedType.I1)] bool root);

   [UnmanagedFunctionPointer(PInvokes.cc, CharSet = PInvokes.cs)]
   public delegate void DrawBackgroundFunc(UIntPtr hdc, IntPtr image, background_repeat repeat, ref web_color color, ref position pos, ref border_radiuses borderRadiuses, ref position borderBox, [MarshalAs(UnmanagedType.I1)] bool isRoot);

   [UnmanagedFunctionPointer(PInvokes.cc, CharSet = PInvokes.cs)]
   public delegate void GetImageSizeFunc(IntPtr image, ref size size);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate void DrawTextFunc(IntPtr text, UIntPtr font, ref web_color color, ref position pos);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate int GetTextWidthFunc(IntPtr text, UIntPtr font);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate UIntPtr CreateFontFunc(IntPtr faceName, int size, int weight, font_style italic, uint decoration, ref font_metrics fm);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate IntPtr ImportCssFunc(IntPtr url, IntPtr baseurl);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate void GetClientRectFunc(ref position client);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate void GetMediaFeaturesFunc(ref media_features media);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate void SetBaseURLFunc(IntPtr base_url);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate void OnAnchorClickFunc(IntPtr url);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate int PTtoPXFunc(int pt);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate bool ShouldCreateElementFunc(IntPtr tag);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate int CreateElementFunc(IntPtr tag, IntPtr attributes, [Out, In] ref ElementInfoStruct elementInfo);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate void SetCursorFunc(IntPtr cursor);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate void DrawListMarkerFunc(IntPtr image, IntPtr baseURL, list_style_type marker_type, ref web_color color, ref position pos);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate IntPtr TransformTextFunc(IntPtr text, text_transform tt);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate void InitCallbacksFunc(ref Callbacks callbacks);

   // Invoke methods

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate void CreateFromStringFunc(IntPtr container, IntPtr html);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate int RenderFunc(IntPtr container, int maxWidth);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate void DrawFunc(IntPtr container, int x, int y, position clip);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate void SetMasterCSSFunc(IntPtr container, IntPtr css);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   [return: MarshalAs(UnmanagedType.I1)]
   public delegate bool OnMouseMoveFunc(IntPtr container, int x, int y);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   [return: MarshalAs(UnmanagedType.I1)]
   public delegate bool OnMouseLeaveFunc(IntPtr container);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   [return: MarshalAs(UnmanagedType.I1)]
   public delegate bool OnLeftButtonUpFunc(IntPtr container, int x, int y);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   [return: MarshalAs(UnmanagedType.I1)]
   public delegate bool OnLeftButtonDownFunc(IntPtr container, int x, int y);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate IntPtr GetElementInfoFunc(IntPtr container, int ID);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   [return: MarshalAs(UnmanagedType.I1)]
   public delegate bool OnMediaChangedFunc(IntPtr container);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate void DeleteFunc(IntPtr container);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate int GetWidthFunc(IntPtr container);

   [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = PInvokes.cs)]
   public delegate int GetHeightFunc(IntPtr container);
}
