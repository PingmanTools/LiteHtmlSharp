using System;
using AppKit;
using CoreGraphics;
using Foundation;
using System.Collections.Generic;
using LiteHtmlSharp;
using LiteHtmlSharp.CoreGraphics;

namespace LiteHtmlSharp.Mac
{
   public class LiteHtmlNSView : NSView
   {
      public override bool IsFlipped { get { return true; } }

      public CGContainer LiteHtmlContainer { get; private set; }

      NSTrackingArea trackingArea = null;

      public event Action Drawn;

      public LiteHtmlNSView(CGRect rect, string masterCssData)
         : base(rect)
      {
         WantsLayer = true;
         LiteHtmlContainer = new CGContainer(masterCssData);
         LiteHtmlContainer.ContextDrawn += LiteHtmlContainer_ContextDrawn;
      }

      public void RenderHtml(string html)
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
         trackingArea = new NSTrackingArea(Bounds, NSTrackingAreaOptions.ActiveAlways | NSTrackingAreaOptions.MouseMoved, this, null);
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
         var bitmapContext = new CGBitmapContext(bytes, width, height, bitsPerComponent, bytesPerRow, colorSpace, CGBitmapFlags.PremultipliedLast | CGBitmapFlags.ByteOrder32Big);
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
         if (!LiteHtmlContainer.HasRendered)
         {
            return;
         }

         var point = ConvertPointFromView(theEvent.LocationInWindow, null);
         LiteHtmlContainer.OnMouseMove((int)point.X, (int)point.Y);
         base.MouseMoved(theEvent);
      }

      public override void MouseDown(NSEvent theEvent)
      {
         if (!LiteHtmlContainer.HasRendered)
         {
            return;
         }

         var point = ConvertPointFromView(theEvent.LocationInWindow, null);
         LiteHtmlContainer.OnLeftButtonDown((int)point.X, (int)point.Y);
         base.MouseDown(theEvent);
      }

      public override void MouseUp(NSEvent theEvent)
      {         
         if (!LiteHtmlContainer.HasRendered)
         {
            return;
         }

         var point = ConvertPointFromView(theEvent.LocationInWindow, null);
         LiteHtmlContainer.OnLeftButtonDown((int)point.X, (int)point.Y);
         base.MouseUp(theEvent);
      }
   }
}

