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
      List<int> elementIDs = new List<int>();
      Dictionary<int, NSView> viewElements = new Dictionary<int, NSView>();

      bool hasCustomViewport = false;
      CGRect customViewport;

      CGRect Viewport { get { return hasCustomViewport ? customViewport : Bounds; } }


      public LiteHtmlNSView(CGRect rect, string masterCssData)
         : base(rect)
      {
         WantsLayer = true;
         LiteHtmlContainer = new CGContainer(masterCssData);
         LiteHtmlContainer.CreateElementCallback = CreateElement;
         LiteHtmlContainer.Document.ViewElementsNeedLayout += LiteHtmlContainer_ViewElementsNeedLayout;
      }

      void LiteHtmlContainer_ViewElementsNeedLayout()
      {
         try
         {
            foreach (var id in elementIDs)
            {
               var elementInfo = LiteHtmlContainer.Document.GetElementInfo(id);
               NSView view;
               if (!viewElements.TryGetValue(id, out view))
               {
                  view = new LiteHtmlNSInput();
                  AddSubview(view);
                  viewElements.Add(id, view);
               }
               var newRect = new CGRect(elementInfo.PosX, elementInfo.PosY, elementInfo.Width, elementInfo.Height);
               if (newRect != view.Frame)
               {
                  view.Frame = newRect;
               }
            }
         }
         catch (Exception ex)
         {
         }
      }

      int CreateElement(string tag, string attributes)
      {
         if (string.Equals(tag, "input", StringComparison.InvariantCultureIgnoreCase))
         {
            var newID = ++lastViewElementId;
            elementIDs.Add(newID);
            return newID;
         }
         return 0;
      }

      private void RemoveAllViewElements()
      {
         foreach (var el in viewElements)
         {
            el.Value.RemoveFromSuperview();
         }
         viewElements.Clear();
         elementIDs.Clear();
      }

      public void LoadHtml(string html)
      {
         RemoveAllViewElements();
         LiteHtmlContainer.Document.CreateFromString(html);
         CheckViewportChange(forceRender: true);
         SetNeedsDisplayInRect(Viewport);
      }

      public override void UpdateTrackingAreas()
      {
         if (trackingArea != null)
         {
            RemoveTrackingArea(trackingArea);
         }
         var areaOptions = NSTrackingAreaOptions.ActiveAlways | NSTrackingAreaOptions.MouseMoved | NSTrackingAreaOptions.MouseEnteredAndExited;
         trackingArea = new NSTrackingArea(Bounds, areaOptions, this, null);
         AddTrackingArea(trackingArea);
      }

      // If true then a redraw is needed
      bool CheckViewportChange(bool forceRender = false)
      {
         if (forceRender
             || (int)LiteHtmlContainer.ContextSize.Width != (int)Viewport.Size.Width
             || (int)LiteHtmlContainer.ContextSize.Height != (int)Viewport.Size.Height)
         {
            LiteHtmlContainer.ContextSize = Viewport.Size;
            LiteHtmlContainer.ScrollOffset = Viewport.Location;
            LiteHtmlContainer.Document.OnMediaChanged();
            LiteHtmlContainer.Render();
            return true;
         }

         if ((int)LiteHtmlContainer.ScrollOffset.Y != (int)Viewport.Location.Y
             || (int)LiteHtmlContainer.ScrollOffset.X != (int)Viewport.Location.X)
         {
            LiteHtmlContainer.ScrollOffset = Viewport.Location;
            return true;
         }

         return false;
      }

      // custom viewport is used for offsetting/scrolling the canvas on this view
      public void SetViewport(CGRect viewport)
      {
         hasCustomViewport = true;
         this.customViewport = viewport;
         if (!LiteHtmlContainer.Document.HasLoadedHtml)
         {
            return;
         }
         if (CheckViewportChange())
         {
            SetNeedsDisplayInRect(Viewport);
         }
      }

      public override void DrawRect(CGRect dirtyRect)
      {
         if (!LiteHtmlContainer.Document.HasRendered)
         {
            return;
         }

         var gfxc = NSGraphicsContext.CurrentContext.GraphicsPort;

         gfxc.SaveState();
         gfxc.TranslateCTM(Viewport.X, Viewport.Y);

         CheckViewportChange();

         LiteHtmlContainer.Context = gfxc;
         LiteHtmlContainer.Draw();
         LiteHtmlContainer.Context = null;

         gfxc.RestoreState();

         if (Drawn != null)
         {
            Drawn();
         }
      }

      public override void MouseMoved(NSEvent theEvent)
      {
         if (LiteHtmlContainer.Document.HasRendered)
         {
            var point = ConvertPointFromView(theEvent.LocationInWindow, null);
            if (LiteHtmlContainer.Document.OnMouseMove((int)(point.X), (int)(point.Y)))
            {
               SetNeedsDisplayInRect(Viewport);
            }
         }
         base.MouseMoved(theEvent);
      }

      public override void MouseExited(NSEvent theEvent)
      {
         if (LiteHtmlContainer.Document.HasRendered)
         {
            if (LiteHtmlContainer.Document.OnMouseLeave())
            {
               SetNeedsDisplayInRect(Viewport);
            }
         }
         base.MouseExited(theEvent);
      }

      public override void MouseDown(NSEvent theEvent)
      {
         if (LiteHtmlContainer.Document.HasRendered)
         {
            var point = ConvertPointFromView(theEvent.LocationInWindow, null);
            LiteHtmlContainer.Document.OnLeftButtonDown((int)point.X, (int)point.Y);
         }
         base.MouseDown(theEvent);
      }

      public override void MouseUp(NSEvent theEvent)
      {         
         if (LiteHtmlContainer.Document.HasRendered)
         {
            var point = ConvertPointFromView(theEvent.LocationInWindow, null);
            LiteHtmlContainer.Document.OnLeftButtonUp((int)point.X, (int)point.Y);
         }
         base.MouseUp(theEvent);
      }

   }
}

