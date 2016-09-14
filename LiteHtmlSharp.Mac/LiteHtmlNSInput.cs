using System;
using AppKit;
using ObjCRuntime;

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

      // this view can be shown without the MainMenu Edit Menu being initialized which is what normally handles these..
      public override bool PerformKeyEquivalent(NSEvent e)
      {
         if (e.Type == NSEventType.KeyDown)
         {
            var inputKey = e.CharactersIgnoringModifiers.ToLower();
            if ((e.ModifierFlags & NSEventModifierMask.DeviceIndependentModifierFlagsMask) == NSEventModifierMask.CommandKeyMask
                || (e.ModifierFlags & NSEventModifierMask.DeviceIndependentModifierFlagsMask) == (NSEventModifierMask.CommandKeyMask | NSEventModifierMask.AlphaShiftKeyMask))
            {
               switch (inputKey)
               {
                  case "x":
                     NSApplication.SharedApplication.SendAction(new Selector("cut:"), null, this);
                     return true;
                  case "c":
                     NSApplication.SharedApplication.SendAction(new Selector("copy:"), null, this);
                     return true;
                  case "v":
                     NSApplication.SharedApplication.SendAction(new Selector("paste:"), null, this);
                     return true;
                  case "z":
                     NSApplication.SharedApplication.SendAction(new Selector("undo:"), null, this);
                     return true;
                  case "a":
                     NSApplication.SharedApplication.SendAction(new Selector("selectAll:"), null, this);
                     return true;
               }
            }
            else if ((e.ModifierFlags & NSEventModifierMask.DeviceIndependentModifierFlagsMask) == (NSEventModifierMask.CommandKeyMask | NSEventModifierMask.ShiftKeyMask)
                     || (e.ModifierFlags & NSEventModifierMask.DeviceIndependentModifierFlagsMask) == (NSEventModifierMask.CommandKeyMask | NSEventModifierMask.ShiftKeyMask | NSEventModifierMask.AlphaShiftKeyMask))
            {
               switch (inputKey)
               {
                  case "z":
                     NSApplication.SharedApplication.SendAction(new Selector("redo:"), null, this);
                     return true;
               }
            }
         }
         return base.PerformKeyEquivalent(e);
      }

   }
}

