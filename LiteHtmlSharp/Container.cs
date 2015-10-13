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
         #if DEBUG
         int testStaticCallback = 0;
         PInvokes.SetTestFunction(result => testStaticCallback = result);
         if (testStaticCallback == 0)
         {
            throw new Exception("Container instance callback test failed. Something is really broken!");
         }
         #endif

         CPPContainer = PInvokes.Init();

         #if DEBUG
         int testInstanceCallback = 0;
         PInvokes.SetTestCallback(CPPContainer, result => testInstanceCallback = result);
         PInvokes.TriggerTestCallback(CPPContainer);
         if (testInstanceCallback == 0)
         {
            throw new Exception("Container instance callback test failed. Something is broken!");
         }
         #endif

         PInvokes.SetMasterCSS(CPPContainer, GetMasterCssData());
         PInvokes.SetDrawBorders(CPPContainer, DrawBorders);
         PInvokes.SetDrawBackground(CPPContainer, DrawBackground);

      }

      protected abstract void DrawBackground(UIntPtr hdc, string image, background_repeat repeat, ref web_color color, ref position pos);

      protected abstract void DrawBorders(UIntPtr hdc, ref borders borders, ref position draw_pos, bool root);

      protected abstract string GetMasterCssData();

      public virtual void RenderHtml(string html)
      {
         PInvokes.RenderHTML(CPPContainer, html);
      }
   }
}
