using System;
using AppKit;
using CoreGraphics;
using Foundation;
using System.Collections.Generic;

namespace MacTest
{
   public class LiteHtmlView : NSView
   {
      public override bool IsFlipped { get { return true; } }

      public CGBitmapContext BitmapContext { get; private set; }

      public LiteHtmlMacContainer LiteHtmlContainer { get; private set; }

      NSTrackingArea trackingArea = null;
      private CGSize lastBounds;

      public LiteHtmlView(CGRect frame, string masterCssData)
         : base(frame)
      {
         WantsLayer = true;
         LiteHtmlContainer = new LiteHtmlMacContainer(this, masterCssData);
         CreateBitmapContext();
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

      void CreateBitmapContext()
      {
         LiteHtmlContainer.ScaleFactor = (int)Layer.ContentsScale;
         lastBounds = new CGSize(Bounds.Width, Bounds.Height);
         var width = (int)(Bounds.Width * Layer.ContentsScale);
         var height = (int)(Bounds.Height * Layer.ContentsScale);

         var colorSpace = CGColorSpace.CreateDeviceRGB();
         const int bytesPerPixel = 4;
         var bytes = new byte[width * height * bytesPerPixel];
         int bytesPerRow = bytesPerPixel * width;
         const int bitsPerComponent = 8;
         BitmapContext = new CGBitmapContext(bytes, width, height, bitsPerComponent, bytesPerRow, colorSpace, CGBitmapFlags.PremultipliedLast | CGBitmapFlags.ByteOrder32Big);
      }

      public override void DrawRect(CoreGraphics.CGRect dirtyRect)
      {
         if (LiteHtmlContainer.ScaleFactor != (int)Layer.ContentsScale || lastBounds != Bounds.Size)
         {
            CreateBitmapContext();
            LiteHtmlContainer.Redraw();
            Log.W("redraw called");
         }

         var gfxc = NSGraphicsContext.CurrentContext.GraphicsPort;
         gfxc.SaveState();
         gfxc.DrawImage(Bounds, BitmapContext.ToImage());
         gfxc.RestoreState();
      }

      public override void MouseMoved(NSEvent theEvent)
      {
         var point = ConvertPointFromView(theEvent.LocationInWindow, null);
         LiteHtmlContainer.OnMouseMove((int)point.X, (int)point.Y);
         base.MouseMoved(theEvent);
      }

   }
}

