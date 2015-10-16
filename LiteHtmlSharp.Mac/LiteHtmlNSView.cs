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
      Dictionary<int, NSView> viewElements;

      public LiteHtmlNSView(CGRect rect, string masterCssData)
         : base(rect)
      {
         WantsLayer = true;
         LiteHtmlContainer = new CGContainer(masterCssData);
         LiteHtmlContainer.ContextDrawn += LiteHtmlContainer_ContextDrawn;
         LiteHtmlContainer.CreateElementCallback = CreateElement;
         LiteHtmlContainer.ViewElementsNeedLayout += LiteHtmlContainer_ViewElementsNeedLayout;
         viewElements = new Dictionary<int, NSView>();
      }

      void LiteHtmlContainer_ViewElementsNeedLayout()
      {
         foreach (var id in LiteHtmlContainer.ElementIDs)
         {
            var elementInfo = LiteHtmlContainer.GetElementInfo(id);
            NSView view;
            if (!viewElements.TryGetValue(id, out view))
            {
               view = new LiteHtmlNSInput();
               AddSubview(view);
               viewElements.Add(id, view);
            }
            view.Frame = new CGRect(elementInfo.PosX, elementInfo.PosY, elementInfo.Width, elementInfo.Height);
         }

      }

      int CreateElement(string tag, string attributes)
      {
         if (string.Equals(tag, "input", StringComparison.InvariantCultureIgnoreCase))
         {
            return ++lastViewElementId;
         }
         return 0;
      }

      public void LoadHtml(string html)
      {
         LiteHtmlContainer.RenderHtml(html);
         ResetContainerContext();
      }

      void LiteHtmlContainer_ContextDrawn()
      {
         SetNeedsDisplayInRect(Bounds);
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

      CGContext CreateBitmapContext()
      {
         var width = (int)(Bounds.Width * Layer.ContentsScale);
         var height = (int)(Bounds.Height * Layer.ContentsScale);

         var colorSpace = CGColorSpace.CreateDeviceRGB();
         const int bytesPerPixel = 4;
         var bytes = new byte[width * height * bytesPerPixel];
         int bytesPerRow = bytesPerPixel * width;
         const int bitsPerComponent = 8;
         var bitmapFlags = CGBitmapFlags.PremultipliedLast | CGBitmapFlags.ByteOrder32Big;
         var bitmapContext = new CGBitmapContext(bytes, width, height, bitsPerComponent, bytesPerRow, colorSpace, bitmapFlags);
         return bitmapContext;
      }

      void ResetContainerContext()
      {
         LiteHtmlContainer.SetContext(CreateBitmapContext(), Bounds.Size, (int)Layer.ContentsScale);
      }

      public override void DrawRect(CGRect dirtyRect)
      {
         if (!LiteHtmlContainer.HasRendered)
         {
            return;
         }

         if (LiteHtmlContainer.ScaleFactor != (int)Layer.ContentsScale || LiteHtmlContainer.ContextSize != Bounds.Size)
         {
            ResetContainerContext();
         }

         var gfxc = NSGraphicsContext.CurrentContext.GraphicsPort;
         gfxc.SaveState();
         gfxc.DrawImage(Bounds, ((CGBitmapContext)LiteHtmlContainer.Context).ToImage());
         gfxc.RestoreState();
         if (Drawn != null)
         {
            Drawn();
         }
      }

      public override void MouseMoved(NSEvent theEvent)
      {
         if (LiteHtmlContainer.HasRendered)
         {
            var point = ConvertPointFromView(theEvent.LocationInWindow, null);
            LiteHtmlContainer.OnMouseMove((int)point.X, (int)point.Y);
         }
         base.MouseMoved(theEvent);
      }

      public override void MouseExited(NSEvent theEvent)
      {
         if (LiteHtmlContainer.HasRendered)
         {
            LiteHtmlContainer.OnMouseLeave();
         }
         base.MouseExited(theEvent);
      }

      public override void MouseDown(NSEvent theEvent)
      {
         if (LiteHtmlContainer.HasRendered)
         {
            var point = ConvertPointFromView(theEvent.LocationInWindow, null);
            LiteHtmlContainer.OnLeftButtonDown((int)point.X, (int)point.Y);
         }
         base.MouseDown(theEvent);
      }

      public override void MouseUp(NSEvent theEvent)
      {         
         if (LiteHtmlContainer.HasRendered)
         {
            var point = ConvertPointFromView(theEvent.LocationInWindow, null);
            LiteHtmlContainer.OnLeftButtonUp((int)point.X, (int)point.Y);
         }
         base.MouseUp(theEvent);
      }

   }
}

