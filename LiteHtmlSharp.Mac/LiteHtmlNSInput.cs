using System;
using AppKit;

namespace LiteHtmlSharp.Mac
{
   public class LiteHtmlNSInput : NSTextField, ICustomTagView
   {
      public void Setup(ElementInfo elementInfo)
      {
         HasSetup = true;
         Cell.UsesSingleLineMode = true;
         Cell.Wraps = false;
      }

      public bool HasSetup { get; private set; }

      public NSView View
      {
         get
         {
            return this;
         }
      }

      public LiteHtmlNSInput()
      {
      }

   }
}

