using System;
using AppKit;

namespace LiteHtmlSharp.Mac
{
   public class LiteHtmlNSButton : NSButton, ICustomTagView
   {
      public bool HasSetup { get; private set; }
      public string Href { get; private set; }
      public string ID { get; private set; }
      public string Value { get; private set; }

      public NSView View { get { return this; } }

      public LiteHtmlNSButton()
      {
         //BezelStyle = NSBezelStyle.TexturedSquare;
         BezelStyle = NSBezelStyle.RegularSquare;
      }



      public void Setup(ElementInfo elementInfo)
      {
         HasSetup = true;
         var lines = elementInfo.Attributes.Split('\n');
         foreach (var line in lines)
         {
            var keyVal = line.Split('=');
            if (keyVal.Length > 1)
            {
               switch (keyVal[0].ToLower())
               {
                  case "id":
                     ID = keyVal[1];
                     break;
                  case "value":
                     Title = Value = keyVal[1];
                     break;
                  case "href":
                     Href = keyVal[1];
                     break;
                  case "bezel":
                     BezelStyle = (NSBezelStyle)int.Parse(keyVal[1]);
                     break;
               }
            }
         }

      }

   }
}

