using System;
using System.Collections.Generic;
using System.Text;

namespace LiteHtmlSharp
{
   public class Document
   {
      public DocumentCalls Calls = new DocumentCalls();
      public IntPtr Container;

      public event Action ViewElementsNeedLayout;


      public bool HasLoadedHtml { get; private set; }

      public bool HasRendered { get; private set; }

      public void SetMasterCSS(string css)
      {
         Calls.SetMasterCSS(Calls.ID, css);
      }

      public void CreateFromString(string html)
      {
         Calls.CreateFromString(Calls.ID, html);
         HasLoadedHtml = true;
      }

      public virtual void Draw(int x, int y, position clip)
      {
         Calls.Draw(Calls.ID, x, y, clip);

         if (ViewElementsNeedLayout != null)
         {
            ViewElementsNeedLayout();
         }
      }

      public bool OnMouseMove(int x, int y)
      {
         return Calls.OnMouseMove(Calls.ID, x, y);
      }

      public bool OnMouseLeave()
      {
         return Calls.OnMouseLeave(Calls.ID);
      }

      public void Render(int maxWidth)
      {
         HasRendered = true;
         Calls.Render(Calls.ID, maxWidth);
      }

      public void OnMediaChanged()
      {
         Calls.OnMediaChanged(Calls.ID);
      }

      public bool OnLeftButtonDown(int x, int y)
      {
         return Calls.OnLeftButtonDown(Calls.ID, x, y);
      }

      public bool OnLeftButtonUp(int x, int y)
      {
         return Calls.OnLeftButtonUp(Calls.ID, x, y);
      }

      public ElementInfo GetElementInfo(int ID)
      {
         return Calls.GetElementInfo(Calls.ID, ID);
      }

      public void TriggerTestCallback(int number, string text)
      {
         Calls.TriggerTestCallback(Calls.ID, number, text);
      }

      public int Height()
      {
         return Calls.GetHeight(Calls.ID);
      }

      public int Width()
      {
         return Calls.GetWidth(Calls.ID);
      }

   }
}
