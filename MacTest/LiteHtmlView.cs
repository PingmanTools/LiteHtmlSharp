using System;
using AppKit;
using CoreGraphics;
using Foundation;
using System.Collections.Generic;
using LiteHtmlSharp;

namespace MacTest
{
   public class LiteHtmlView : NSView
   {
      public override bool IsFlipped { get { return true; } }

      public CGBitmapContext BitmapContext { get; private set; }

      public CGSize BitmapContextSize { get; private set; }

      public LiteHtmlMacContainer LiteHtmlContainer { get; private set; }

      NSTrackingArea trackingArea = null;

      public event Action Drawn;

      public LiteHtmlView(string masterCssData)
      {
         Init(masterCssData);
      }

      public LiteHtmlView(CGRect frame, string masterCssData)
         : base(frame)
      {
         Init(masterCssData);
      }

      void Init(string masterCssData)
      {
         WantsLayer = true;
         LiteHtmlContainer = new LiteHtmlMacContainer(this, masterCssData);
         LiteHtmlContainer.ScaleFactor = (int)Layer.ContentsScale;
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
         BitmapContextSize = new CGSize(Bounds.Width, Bounds.Height);
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
         if (LiteHtmlContainer.ScaleFactor != (int)Layer.ContentsScale || BitmapContextSize != Bounds.Size)
         {
            CreateBitmapContext();
            Redraw();
         }

         var gfxc = NSGraphicsContext.CurrentContext.GraphicsPort;
         gfxc.SaveState();
         gfxc.DrawImage(Bounds, BitmapContext.ToImage());
         gfxc.RestoreState();

         if (Drawn != null)
         {
            Drawn();
         }
      }

      void Redraw()
      {
         LiteHtmlContainer.Draw(0, 0, new position{ x = 0, y = 0, width = (int)Bounds.Width, height = (int)Bounds.Height });
      }

      public override void MouseMoved(NSEvent theEvent)
      {
         var point = ConvertPointFromView(theEvent.LocationInWindow, null);
         if (LiteHtmlContainer.OnMouseMove((int)point.X, (int)point.Y))
         {
            Redraw();
         }
         base.MouseMoved(theEvent);
      }

   }
}

