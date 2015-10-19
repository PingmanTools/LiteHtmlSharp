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
      CGPoint scrollOffset;

      public LiteHtmlNSView(CGRect rect, string masterCssData)
         : base(rect)
      {
         WantsLayer = true;
         LiteHtmlContainer = new CGContainer(masterCssData);
         LiteHtmlContainer.ContextDrawn += LiteHtmlContainer_ContextDrawn;
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
               var newRect = new CGRect(elementInfo.PosX - scrollOffset.X, elementInfo.PosY - scrollOffset.Y, elementInfo.Width, elementInfo.Height);
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
         CreateBitmapContext();
         LiteHtmlContainer.Draw();
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

      public void Scroll(CGPoint scrollOffset)
      {
         this.scrollOffset = scrollOffset;
         SetNeedsDisplayInRect(Bounds);
      }

      CGBitmapContext lastBitmapContext = null;

      void CreateBitmapContext()
      {
         var width = (int)(Bounds.Width * Layer.ContentsScale);
         var height = (int)(Bounds.Height * Layer.ContentsScale);

         var colorSpace = CGColorSpace.CreateDeviceRGB();
         const int bytesPerPixel = 4;
         var bytes = new byte[width * height * bytesPerPixel];
         int bytesPerRow = bytesPerPixel * width;
         const int bitsPerComponent = 8;
         var bitmapFlags = CGBitmapFlags.PremultipliedLast | CGBitmapFlags.ByteOrder32Big;
         var context = new CGBitmapContext(bytes, width, height, bitsPerComponent, bytesPerRow, colorSpace, bitmapFlags);

         if (lastBitmapContext != null)
         {
            lastBitmapContext.Dispose();
         }
         lastBitmapContext = context;

         LiteHtmlContainer.ScaleFactor = (int)Layer.ContentsScale;
         LiteHtmlContainer.ContextSize = Bounds.Size;
         LiteHtmlContainer.Context = context;
         LiteHtmlContainer.Document.OnMediaChanged();
         LiteHtmlContainer.Render();
      }

      bool CheckContextSizeChange()
      {
         if (LiteHtmlContainer.ScaleFactor != (int)Layer.ContentsScale || LiteHtmlContainer.ContextSize != Bounds.Size)
         {
            CreateBitmapContext();
            return true;
         }
         return false;
      }

      bool CheckScrollOffsetChange()
      {
         if (LiteHtmlContainer.ScrollOffset != scrollOffset)
         {
            LiteHtmlContainer.ScrollOffset = scrollOffset;
            return true;
         }
         return false;
      }

      public override void DrawRect(CGRect dirtyRect)
      {
         if (CheckContextSizeChange() || CheckScrollOffsetChange())
         {
            LiteHtmlContainer.Draw();
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
         if (LiteHtmlContainer.Document.HasRendered)
         {
            var point = ConvertPointFromView(theEvent.LocationInWindow, null);
            LiteHtmlContainer.OnMouseMove((int)(point.X + scrollOffset.X), (int)(point.Y + scrollOffset.Y));
         }
         base.MouseMoved(theEvent);
      }

      public override void MouseExited(NSEvent theEvent)
      {
         if (LiteHtmlContainer.Document.HasRendered)
         {
            LiteHtmlContainer.OnMouseLeave();
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

