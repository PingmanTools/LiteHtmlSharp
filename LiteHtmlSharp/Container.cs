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

      protected IntPtr CPPContainer;

      public Container()
      {
         CPPContainer = PInvokes.Init();

         PInvokes.SetDrawBorders(CPPContainer, DrawBorders);
         PInvokes.SetDrawBackground(CPPContainer, DrawBackground);
      }

      protected abstract void DrawBackground(UIntPtr hdc, string image, background_repeat repeat, ref web_color color, ref position pos);

      protected abstract void DrawBorders(UIntPtr hdc, ref borders borders, ref position draw_pos, bool root);

      public virtual void RenderHtml(string html)
      {
         PInvokes.RenderHTML(CPPContainer, html);
      }
   }
}
