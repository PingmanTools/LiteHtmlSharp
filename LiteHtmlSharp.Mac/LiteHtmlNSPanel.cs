using System;
using AppKit;
using LiteHtmlSharp.CoreGraphics;
using CoreGraphics;
using Foundation;

namespace LiteHtmlSharp.Mac
{
   public class LiteHtmlNSPanel : NSPanel
   {
      public CGContainer LiteHtmlContainer { get { return LiteHtmlView.LiteHtmlContainer; } }

      public LiteHtmlNSView LiteHtmlView { get { return windowHelper.LiteHtmlView; } }

      LiteHtmlWindowHelper windowHelper;

      public LiteHtmlNSPanel(CGRect rect, string masterCssData)
      {
         windowHelper = new LiteHtmlWindowHelper(this, rect, masterCssData);
      }


   }
}

