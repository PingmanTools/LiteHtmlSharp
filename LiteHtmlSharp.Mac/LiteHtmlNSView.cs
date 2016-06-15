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

      bool hasCustomViewport = false;
      CGRect customViewport;

      CGRect Viewport { get { return hasCustomViewport ? customViewport : Bounds; } }

      public event Action<ICustomTagView> CustomTabViewHasSetup;


      public LiteHtmlNSView (CGRect rect, string masterCssData)
         : base (rect)
      {
         WantsLayer = true;
         LiteHtmlContainer = new CGContainer (masterCssData);
         LiteHtmlContainer.SetCursorCallback = SetCusor;
         LiteHtmlContainer.GetDefaultFontSizeCallback = GetDefaultFontSize;
         LiteHtmlContainer.GetDefaultFontNameCallback = GetDefaultFontName;
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

      void SetCusor (string cursor)
      {
         switch (cursor) {
         case "pointer":
            NSCursor.PointingHandCursor.Set ();
            break;
         default:
            NSCursor.ArrowCursor.Set ();
            break;
         }
      }

      void LiteHtmlContainer_ViewElementsNeedLayout ()
      {
         foreach (var el in viewElements) {
            var elementInfo = LiteHtmlContainer.Document.GetElementInfo (el.Key);
            var newRect = new CGRect (elementInfo.Value.PosX, elementInfo.Value.PosY, elementInfo.Value.Width, elementInfo.Value.Height);
            if (newRect != el.Value.View.Frame) {
               el.Value.View.Frame = newRect;
            }

            if (!el.Value.HasSetup) {
               el.Value.Setup (elementInfo.Value);
               if (el.Value is LiteHtmlNSButton) {
                  (el.Value as LiteHtmlNSButton).Activated += Btn_Activated;
               }
               if (CustomTabViewHasSetup != null) {
                  CustomTabViewHasSetup (el.Value);
               }

            }
         }
      }

      void Btn_Activated (object sender, EventArgs e)
      {
         var btn = (LiteHtmlNSButton)sender;
         LiteHtmlContainer.TriggerAnchorClicked (btn.Href);
      }

      int CreateElement (string tag, string attributes, ref ElementInfo elementInfo)
      {
         switch (tag.ToLower ()) {
         case "input": {
               var newID = ++lastViewElementId;
               var view = new LiteHtmlNSInput ();
               AddSubview (view);
               viewElements.Add (newID, view);
               return newID;
            }
         case "button": {
               var newID = ++lastViewElementId;
               var view = new LiteHtmlNSButton ();
               AddSubview (view);
               viewElements.Add (newID, view);
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

      public void LoadHtml (string html)
      {
         RemoveAllViewElements ();
         LiteHtmlContainer.Document.CreateFromString (html);
         CheckViewportChange (forceRender: true);
         SetNeedsDisplayInRect (Viewport);
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

      // If true then a redraw is needed
      bool CheckViewportChange (bool forceRender = false)
      {
         if (forceRender
             || (int)LiteHtmlContainer.ContextSize.Width != (int)Viewport.Size.Width
             || (int)LiteHtmlContainer.ContextSize.Height != (int)Viewport.Size.Height) {
            LiteHtmlContainer.ContextSize = Viewport.Size;
            LiteHtmlContainer.ScrollOffset = Viewport.Location;
            LiteHtmlContainer.Document.OnMediaChanged ();
            LiteHtmlContainer.Render ();
            return true;
         }

         if ((int)LiteHtmlContainer.ScrollOffset.Y != (int)Viewport.Location.Y
             || (int)LiteHtmlContainer.ScrollOffset.X != (int)Viewport.Location.X) {
            LiteHtmlContainer.ScrollOffset = Viewport.Location;
            return true;
         }

         return false;
      }

      // custom viewport is used for offsetting/scrolling the canvas on this view
      public void SetViewport (CGRect viewport)
      {
         hasCustomViewport = true;
         this.customViewport = viewport;
         if (!LiteHtmlContainer.Document.HasLoadedHtml) {
            return;
         }
         if (CheckViewportChange ()) {
            SetNeedsDisplayInRect (Viewport);
         }
      }

      public override void DrawRect (CGRect dirtyRect)
      {
         if (!LiteHtmlContainer.Document.HasRendered) {
            return;
         }

         var gfxc = NSGraphicsContext.CurrentContext.GraphicsPort;

         gfxc.SaveState ();
         gfxc.TranslateCTM (Viewport.X, Viewport.Y);

         CheckViewportChange ();

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
               SetNeedsDisplayInRect (Viewport);
            }
         }
         base.MouseMoved (theEvent);
      }

      public override void MouseExited (NSEvent theEvent)
      {
         if (LiteHtmlContainer.Document.HasRendered) {
            if (LiteHtmlContainer.Document.OnMouseLeave ()) {
               SetNeedsDisplayInRect (Viewport);
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

