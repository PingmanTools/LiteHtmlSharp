using System;
using AppKit;

namespace LiteHtmlSharp.Mac
{
   public interface ICustomTagView
   {
      bool HasSetup { get; }
      void Setup(ElementInfo elementInfo);

      NSView View { get; }
   }
}

