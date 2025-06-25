using System;
using UIKit;
using LiteHtmlSharp.CoreGraphics;
using CoreGraphics;

namespace LiteHtmlSharp.iOS
{
    public class LiteHtmlUIView : UIView
    {
        public CGContainer LiteHtmlContainer { get; private set; }

        bool hasCustomViewport = false;
        CGRect customViewport;
        CGRect Viewport { get { return hasCustomViewport ? customViewport : Bounds; } }

        public event Action Drawn;

        public LiteHtmlUIView(string masterCssData)
        {
            LiteHtmlContainer = new CGContainer(masterCssData);
            //LiteHtmlContainer.CreateElementCallback = CreateElement;
            //LiteHtmlContainer.Document.ViewElementsNeedLayout += LiteHtmlContainer_ViewElementsNeedLayout;

            LiteHtmlContainer.DefaultFontSize = GetDefaultFontSize();
            LiteHtmlContainer.DefaultFontName = GetDefaultFontName();
            LiteHtmlContainer.DefaultFontColor = GetDefaultFontColor();

            UserInteractionEnabled = true;
        }


        public override void TouchesBegan(Foundation.NSSet touches, UIEvent evt)
        {
            UITouch touch = touches.AnyObject as UITouch;
            if (touch != null)
            {
                var point = touch.LocationInView(this);
                LiteHtmlContainer.Document.OnLeftButtonDown((int)point.X, (int)point.Y);
            }
        }

        public override void TouchesEnded(Foundation.NSSet touches, UIEvent evt)
        {
            UITouch touch = touches.AnyObject as UITouch;
            if (touch != null)
            {
                var point = touch.LocationInView(this);
                LiteHtmlContainer.Document.OnLeftButtonUp((int)point.X, (int)point.Y);
            }
        }

        string GetDefaultFontName()
        {
            return UIFont.SystemFontOfSize(1).Name;
        }

        int GetDefaultFontSize()
        {
            return (int)Math.Round(UIFont.SystemFontSize);
        }

        web_color GetDefaultFontColor()
        {
            var defaultColor = UIColor.Label;
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

        public void LoadHtml(string html)
        {
            LiteHtmlContainer.Document.CreateFromString(html);
            CheckViewportChange(forceRender: true);
            SetNeedsDisplayInRect(Viewport);
        }

        // If true then a redraw is needed
        bool CheckViewportChange(bool forceRender = false)
        {
            if (forceRender
                || (int)LiteHtmlContainer.Size.Width != (int)Viewport.Size.Width
                || (int)LiteHtmlContainer.Size.Height != (int)Viewport.Size.Height)
            {
                LiteHtmlContainer.Size = Viewport.Size.ToLiteHtmlSize();
                LiteHtmlContainer.ScrollOffset = Viewport.Location.ToLiteHtmlPoint();
                LiteHtmlContainer.Document.OnMediaChanged();
                LiteHtmlContainer.Render();
                return true;
            }

            if ((int)LiteHtmlContainer.ScrollOffset.Y != (int)Viewport.Location.Y
                || (int)LiteHtmlContainer.ScrollOffset.X != (int)Viewport.Location.X)
            {
                LiteHtmlContainer.ScrollOffset = Viewport.Location.ToLiteHtmlPoint();
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

        public override void Draw(CGRect rect)
        {

            if (!LiteHtmlContainer.Document.HasRendered)
            {
                return;
            }

            using (CGContext gfxc = UIGraphics.GetCurrentContext())
            {
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
        }
    }
}