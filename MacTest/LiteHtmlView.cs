using System;
using AppKit;
using CoreGraphics;

namespace MacTest
{
   public class LiteHtmlView : NSView
   {
      public override bool IsFlipped { get { return true; } }

      public CGBitmapContext BitmapContext { get; private set; }

      public LiteHtmlMacContainer LiteHtmlContainer { get; private set; }

      public LiteHtmlView(CGRect frame)
         : base(frame)
      {
         var width = (int)frame.Size.Width;
         var height = (int)frame.Size.Height;

         var colorSpace = CGColorSpace.CreateDeviceRGB();
         const int bytesPerPixel = 4;
         var bytes = new byte[width * height * bytesPerPixel];
         int bytesPerRow = bytesPerPixel * width;
         const int bitsPerComponent = 8;
         BitmapContext = new CGBitmapContext(bytes, width, height, bitsPerComponent, bytesPerRow, colorSpace, CGBitmapFlags.PremultipliedLast | CGBitmapFlags.ByteOrder32Big);
         LiteHtmlContainer = new LiteHtmlMacContainer(this);
      }

      public void DrawRect(nfloat x, nfloat y, nfloat width, nfloat height, CGColor color)
      {
         BitmapContext.SetFillColor(color);
         BitmapContext.FillRect(new CGRect(x, y, width, height));
      }

      public override void DrawRect(CoreGraphics.CGRect dirtyRect)
      {
         var gfxc = NSGraphicsContext.CurrentContext.GraphicsPort;
         gfxc.DrawImage(Bounds, BitmapContext.ToImage());
      }
   }
}

