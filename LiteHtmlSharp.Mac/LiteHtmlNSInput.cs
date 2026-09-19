using System;
using System.Collections.Generic;
using AppKit;
using ObjCRuntime;

namespace LiteHtmlSharp.Mac
{
   public class LiteHtmlNSInput : NSTextField, ICustomTagView
   {
      public void Setup(IReadOnlyDictionary<string,string> attributes)
      {
         HasSetup = true;
         if(attributes.TryGetValue("value",out var value))StringValue=value;
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

      // Embedded windows may not provide an Edit menu to route standard shortcuts.
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

