using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace LiteHtmlSharp
{
   [StructLayout(LayoutKind.Sequential)]
   public struct position
   {
      public int x;
      public int y;
      public int width;
      public int height;
   }

   [StructLayout(LayoutKind.Sequential)]
   public struct border_radiuses
   {
      public int top_left_x;
      public int top_left_y;

      public int top_right_x;
      public int top_right_y;

      public int bottom_right_x;
      public int bottom_right_y;

      public int bottom_left_x;
      public int bottom_left_y;
   }

   [StructLayout(LayoutKind.Sequential)]
   public struct borders
   {
      public border left;
      public border top;
      public border right;
      public border bottom;
      public border_radiuses radius;
   }

   [StructLayout(LayoutKind.Sequential)]
   public struct border
   {
      public int width;
      public border_style style;
      public web_color color;
   }

   [StructLayout(LayoutKind.Sequential)]
   public struct web_color
   {
      public byte blue;
      public byte green;
      public byte red;
      public byte alpha;
   }

   public enum border_style
   {
      border_style_none,
      border_style_hidden,
      border_style_dotted,
      border_style_dashed,
      border_style_solid,
      border_style_double,
      border_style_groove,
      border_style_ridge,
      border_style_inset,
      border_style_outset
   }

   public enum background_repeat
   {
      background_repeat_repeat,
      background_repeat_repeat_x,
      background_repeat_repeat_y,
      background_repeat_no_repeat
   }
}
