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

      private nfloat bitmapScale;

      public LiteHtmlMacContainer LiteHtmlContainer { get; private set; }

      public LiteHtmlView(CGRect frame)
         : base(frame)
      {
         WantsLayer = true;
         CreateBitmapContext();
         LiteHtmlContainer = new LiteHtmlMacContainer(this);
      }

      void CreateBitmapContext()
      {
         bitmapScale = Layer.ContentsScale;

         var width = (int)(Bounds.Width * bitmapScale);
         var height = (int)(Bounds.Height * bitmapScale);

         var colorSpace = CGColorSpace.CreateDeviceRGB();
         const int bytesPerPixel = 4;
         var bytes = new byte[width * height * bytesPerPixel];
         int bytesPerRow = bytesPerPixel * width;
         const int bitsPerComponent = 8;
         BitmapContext = new CGBitmapContext(bytes, width, height, bitsPerComponent, bytesPerRow, colorSpace, CGBitmapFlags.PremultipliedLast | CGBitmapFlags.ByteOrder32Big);
      }

      public override void DrawRect(CoreGraphics.CGRect dirtyRect)
      {
         if (bitmapScale != Layer.ContentsScale)
         {
            CreateBitmapContext();
            LiteHtmlContainer.Redraw();
         }

         var gfxc = NSGraphicsContext.CurrentContext.GraphicsPort;
         gfxc.SaveState();
         //gfxc.ScaleCTM(bitmapScale, bitmapScale);
         gfxc.DrawImage(Bounds, BitmapContext.ToImage());
         gfxc.RestoreState();
      }


   }
}

