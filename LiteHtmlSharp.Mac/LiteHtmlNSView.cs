using System;
using AppKit;
using CoreGraphics;
using Foundation;
using System.Collections.Generic;
using LiteHtmlSharp;
using LiteHtmlSharp.CoreGraphics;
using System.Linq;
using System.Diagnostics;

namespace LiteHtmlSharp.Mac
{
   public class LiteHtmlNSView : NSView
   {
      public override bool IsFlipped { get { return true; } }

      public CGContainer LiteHtmlContainer { get; private set; }

      NSTrackingArea trackingArea = null;

      public event Action Drawn;

      int lastViewElementId = 0;
      Dictionary<int, ICustomTagView> viewElements = new Dictionary<int, ICustomTagView> ();

      public IEnumerable<ICustomTagView> ViewElements { get { return viewElements.Values; } }


      public event Action<ICustomTagView> CustomTagViewHasSetup;


      public LiteHtmlNSView (CGRect rect, string masterCssData)
         : base (rect)
      {
         WantsLayer = true;
         //LiteHtmlContainer = new CGContainer (InjectDefaultFontColorCss(masterCssData));
         LiteHtmlContainer = new CGContainer(masterCssData);
         LiteHtmlContainer.SetCursorCallback = SetCusor;
         LiteHtmlContainer.DefaultFontSize = GetDefaultFontSize();
         LiteHtmlContainer.DefaultFontName = GetDefaultFontName();
         LiteHtmlContainer.DefaultFontColor = GetDefaultFontColor();
         LiteHtmlContainer.ShouldCreateElementCallback = ShouldCreateElement;
         LiteHtmlContainer.CreateElementCallback = CreateElement;
         LiteHtmlContainer.Document.ViewElementsNeedLayout += LiteHtmlContainer_ViewElementsNeedLayout;
      }

      string GetDefaultFontName ()
      {
         return NSFont.SystemFontOfSize (NSFont.SystemFontSize).FontName;
      }

      int GetDefaultFontSize ()
      {
         return (int)Math.Round (NSFont.SystemFontSize);
      }

      web_color GetDefaultFontColor()
      {
         var defaultColor = NSColor.LabelColor;
         // Use components for color values because AppKit.NSColor.LabelColor (and others) 
         // do not work with NSColor.GetRgba or NSColor.GetComponents
         var components = defaultColor.CGColor.Components;
         var retVal = new web_color();
         if (components.Length == 2)
         {
            retVal.red = retVal.green = retVal.blue = (byte)(byte.MaxValue * components[0]);
            retVal.alpha = (byte)(byte.MaxValue * components[1]);
         }
         else if (components.Length == 4)
         {
            retVal.red = (byte)(byte.MaxValue * components[0]);
            retVal.green = (byte)(byte.MaxValue * components[1]);
            retVal.blue = (byte)(byte.MaxValue * components[2]);
            retVal.alpha = (byte)(byte.MaxValue * components[3]);
         }
         else
         {
            // Default to Black
            retVal.red = retVal.green = retVal.blue = 0;
            retVal.alpha = byte.MaxValue;
         }
            
         return retVal;
      }

      /*string InjectDefaultFontColorCss(string cssData)
      {
         var color = GetDefaultFontColor();
         var rgbColor = String.Format("rgba({0:D0},{1:D0},{2:D0},{3})", color.red, color.green, color.blue, color.alpha / (float)byte.MaxValue);
         return "body { color: " + rgbColor + "; }" + cssData;
      }*/

      void SetCusor(string cursor)
      {
         switch (cursor)
         {
            case "pointer":
               NSCursor.PointingHandCursor.Set();
               break;
            default:
               NSCursor.ArrowCursor.Set();
               break;
         }
      }

      void LiteHtmlContainer_ViewElementsNeedLayout ()
      {
         foreach (var el in viewElements) {
            var elementInfo = LiteHtmlContainer.Document.GetElementInfo (el.Key);
            if (elementInfo != null)
            {
               var newRect = new CGRect(elementInfo.PosX, elementInfo.PosY, elementInfo.Width, elementInfo.Height);
               if (newRect != el.Value.View.Frame)
               {
                  el.Value.View.Frame = newRect;
               }

               if (!el.Value.HasSetup)
               {
                  el.Value.Setup(elementInfo);
                  if (el.Value is LiteHtmlNSButton)
                  {
                     (el.Value as LiteHtmlNSButton).Activated += Btn_Activated;
                  }
                  if (CustomTagViewHasSetup != null)
                  {
                     CustomTagViewHasSetup(el.Value);
                  }
               }
            }
         }
      }

      void Btn_Activated (object sender, EventArgs e)
      {
         var btn = (LiteHtmlNSButton)sender;
         LiteHtmlContainer.TriggerAnchorClicked (btn.Href);
      }

      bool ShouldCreateElement(string tag)
      {
         switch (tag.ToLowerInvariant())
         {
            case "input":
            case "button":
               return true;
            default:
               return false;
         }
      }

      int CreateElement(string tag, string attributes, ElementInfo elementInfo)
      {
         switch (tag.ToLowerInvariant())
         {
            case "input":
               {
                  var newID = ++lastViewElementId;
                  var view = new LiteHtmlNSInput();
                  AddSubview(view);
                  viewElements.Add(newID, view);
                  return newID;
               }
            case "button":
               {
                  var newID = ++lastViewElementId;
                  var view = new LiteHtmlNSButton();
                  AddSubview(view);
                  viewElements.Add(newID, view);
                  return newID;
               }
            default:
               return 0;
         }

      }

      private void RemoveAllViewElements ()
      {
         foreach (var el in viewElements) {
            el.Value.View.RemoveFromSuperview ();
         }
         viewElements.Clear ();
      }

      public void SetViewport(CGRect viewport)
      {
         if (LiteHtmlContainer.SetViewport(viewport.Location.ToLiteHtmlPoint(), viewport.Size.ToLiteHtmlSize()))
         {
            SetNeedsDisplayInRect(ViewportRect);
         }
      }

      CGRect ViewportRect
      {
         get
         {
            if (LiteHtmlContainer.HasCustomViewport)
            {
               return new CGRect(LiteHtmlContainer.ScrollOffset.X, LiteHtmlContainer.ScrollOffset.Y, LiteHtmlContainer.Size.Width, LiteHtmlContainer.Size.Height);;
            }
            else
            {
               return Bounds;
            }
         }
      }

      CGPoint ViewportPoint 
      {
         get
         {
            if (LiteHtmlContainer.HasCustomViewport)
            {
               return new CGPoint(LiteHtmlContainer.ScrollOffset.X, LiteHtmlContainer.ScrollOffset.Y);
            }
            else
            {
               return Bounds.Location;
            }
         }
      }

      public void LoadHtml (string html)
      {
         RemoveAllViewElements ();
         LiteHtmlContainer.Document.CreateFromString (html);
         LiteHtmlContainer.CheckViewportChange (forceRender: true);
         SetNeedsDisplayInRect (ViewportRect);
      }

      public override void UpdateTrackingAreas ()
      {
         if (trackingArea != null) {
            RemoveTrackingArea (trackingArea);
         }
         var areaOptions = NSTrackingAreaOptions.ActiveAlways | NSTrackingAreaOptions.MouseMoved | NSTrackingAreaOptions.MouseEnteredAndExited;
         trackingArea = new NSTrackingArea (Bounds, areaOptions, this, null);
         AddTrackingArea (trackingArea);
      }


      public override void DrawRect (CGRect dirtyRect)
      {
         if (!LiteHtmlContainer.Document.HasRendered) {
            return;
         }

         var gfxc = NSGraphicsContext.CurrentContext.GraphicsPort;

         gfxc.SaveState ();
         var point = ViewportPoint;
         gfxc.TranslateCTM (point.X, point.Y);

         LiteHtmlContainer.CheckViewportChange ();

         LiteHtmlContainer.Context = gfxc;
         LiteHtmlContainer.Draw ();
         LiteHtmlContainer.Context = null;

         gfxc.RestoreState ();

         if (Drawn != null) {
            Drawn ();
         }
      }

      public override void MouseMoved (NSEvent theEvent)
      {
         if (LiteHtmlContainer.Document.HasRendered) {
            var point = ConvertPointFromView (theEvent.LocationInWindow, null);
            if (LiteHtmlContainer.Document.OnMouseMove ((int)(point.X), (int)(point.Y))) {
               SetNeedsDisplayInRect (ViewportRect);
            }
         }
         base.MouseMoved (theEvent);
      }

      public override void MouseExited (NSEvent theEvent)
      {
         if (LiteHtmlContainer.Document.HasRendered) {
            if (LiteHtmlContainer.Document.OnMouseLeave ()) {
               SetNeedsDisplayInRect (ViewportRect);
            }
         }
         base.MouseExited (theEvent);
      }

      public override void MouseDown (NSEvent theEvent)
      {
         if (LiteHtmlContainer.Document.HasRendered) {
            var point = ConvertPointFromView (theEvent.LocationInWindow, null);
            LiteHtmlContainer.Document.OnLeftButtonDown ((int)point.X, (int)point.Y);
         }
         base.MouseDown (theEvent);
      }

      public override void MouseUp (NSEvent theEvent)
      {
         if (LiteHtmlContainer.Document.HasRendered) {
            var point = ConvertPointFromView (theEvent.LocationInWindow, null);
            LiteHtmlContainer.Document.OnLeftButtonUp ((int)point.X, (int)point.Y);
         }
         base.MouseUp (theEvent);
      }

   }
}

