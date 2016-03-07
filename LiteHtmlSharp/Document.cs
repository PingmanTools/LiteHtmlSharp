using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

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
         if (html == null)
         {
            throw new Exception("Cannot render a null string.");
         }
         else
         {
            Calls.CreateFromString(Calls.ID, html);
            HasLoadedHtml = true;
         }
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

      public int Render(int maxWidth)
      {
         HasRendered = true;
         return Calls.Render(Calls.ID, maxWidth);
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

      public ElementInfo? GetElementInfo(int ID)
      {
         IntPtr ptr = Calls.GetElementInfo(Calls.ID, ID);
         if (ptr == IntPtr.Zero)
         {
            return null;
         }
         ElementInfo info = Marshal.PtrToStructure<ElementInfo>(ptr);
         return info;
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
