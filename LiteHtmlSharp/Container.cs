using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace LiteHtmlSharp
{
   public abstract class Container
   {
      protected IntPtr CPPContainer;
      private static List<Delegate> _delegates = new List<Delegate>();

      public int ScaleFactor = 1;

      public Container(string masterCssData)
      {
         #if DEBUG
         //TestFramework();
         #endif

         CPPContainer = PInvokes.Init();

         PInvokes.SetMasterCSS(CPPContainer, masterCssData);

         PInvokes.SetDrawBorders(CPPContainer, CreateDelegate(new DrawBordersFunc(DrawBordersScaled)));
         PInvokes.SetDrawBackground(CPPContainer, CreateDelegate(new DrawBackgroundFunc(DrawBackgroundScaled)));
         PInvokes.SetGetImageSize(CPPContainer, CreateDelegate(new GetImageSizeFunc(GetImageSize)));

         PInvokes.SetDrawText(CPPContainer, CreateDelegate(new DrawTextFunc(DrawTextScaled)));
         PInvokes.SetGetTextWidth(CPPContainer, CreateDelegate(new GetTextWidthFunc(GetTextWidth)));
         PInvokes.SetCreateFont(CPPContainer, CreateDelegate(new CreateFontFunc(CreateFont)));

         PInvokes.SetImportCss(CPPContainer, CreateDelegate(new ImportCssFunc(ImportCss)));

         PInvokes.SetGetClientRect(CPPContainer, CreateDelegate(new GetClientRectFunc(GetClientRect)));
         PInvokes.SetGetMediaFeatures(CPPContainer, CreateDelegate(new GetMediaFeaturesFunc(GetMediaFeatures)));

         PInvokes.SetSetBaseURL(CPPContainer, CreateDelegate(new SetBaseURLFunc(SetBaseURL)));
         PInvokes.SetOnAnchorClick(CPPContainer, CreateDelegate(new OnAnchorClickFunc(OnAnchorClick)));

         PInvokes.SetPTtoPX(CPPContainer, CreateDelegate(new PTtoPXFunct(PTtoPX)));
         PInvokes.SetCreateElement(CPPContainer, CreateDelegate(new CreateElementFunc(CreateElement)));
      }

      static void TestFramework()
      {
         string testStaticCallback = null;
         PInvokes.SetTestFunction(result => testStaticCallback = result);
         if (string.IsNullOrEmpty(testStaticCallback))
         {
            throw new Exception("Container instance callback test failed. Something is really broken!");
         }

         var CPPContainer = PInvokes.Init();

         string testInstanceCallback = null;
         string testInstanceCallbackResult = "Test 1234 .... ₤ · ₥ · ₦ · ₮ · ₯ · ₹";
         PInvokes.SetTestCallback(CPPContainer, result => testInstanceCallback = result);
         PInvokes.TriggerTestCallback(CPPContainer, testInstanceCallbackResult);
         if (testInstanceCallback != testInstanceCallbackResult)
         {
            throw new Exception("Container instance callback test failed. Something is broken!");
         }
      }

      private T CreateDelegate<T>(T someDelegate)
      {
         _delegates.Add(someDelegate as Delegate);
         return someDelegate;
      }

      // -----

      private void DrawBackgroundScaled(UIntPtr hdc, string image, background_repeat repeat, ref web_color color, ref position pos)
      {
         pos.Scale(ScaleFactor);
         DrawBackground(hdc, image, repeat, ref color, ref pos);
      }

      protected abstract void DrawBackground(UIntPtr hdc, string image, background_repeat repeat, ref web_color color, ref position pos);

      // -----

      private void DrawBordersScaled(UIntPtr hdc, ref borders borders, ref position draw_pos, bool root)
      {
         borders.Scale(ScaleFactor);
         draw_pos.Scale(ScaleFactor);
         DrawBorders(hdc, ref borders, ref draw_pos, root);
      }

      protected abstract void DrawBorders(UIntPtr hdc, ref borders borders, ref position draw_pos, bool root);

      // -----

      private void DrawTextScaled(string text, UIntPtr font, ref web_color color, ref position pos)
      {
         pos.Scale(ScaleFactor);
         DrawText(text, font, ref color, ref pos);
      }

      protected abstract void DrawText(string text, UIntPtr font, ref web_color color, ref position pos);

      // -----

      protected abstract void GetImageSize(string image, ref size size);

      protected abstract int GetTextWidth(string text, UIntPtr font);

      protected abstract void GetClientRect(ref position client);

      protected abstract UIntPtr CreateFont(string faceName, int size, int weight, font_style italic, uint decoration, ref font_metrics fm);

      public ImportCssFunc ImportCssCallback;

      protected virtual string ImportCss(string url, string baseurl)
      {
         if (ImportCssCallback == null)
         {
            throw new Exception("ImportCss must be overridden or the callback delegate set");
         }
         return ImportCssCallback(url, baseurl);
      }

      protected abstract void GetMediaFeatures(ref media_features media);

      protected abstract void SetBaseURL(ref string base_url);

      protected abstract void OnAnchorClick(ref string url);

      protected abstract int PTtoPX(int pt);

      protected abstract int CreateElement(string tag);

      public virtual void RenderHtml(string html, int maxWidth)
      {
         PInvokes.RenderHTML(CPPContainer, html, maxWidth);
      }

      protected virtual void CheckLastError()
      {
         var lastError = Marshal.GetLastWin32Error();
         if (lastError > 0)
         {
            var e = new Win32Exception(lastError);
            //throw e;
         }
      }

      public virtual void Draw(int x, int y, position clip)
      {
         PInvokes.Draw(CPPContainer, x, y, clip);
      }

      public virtual bool OnMouseMove(int x, int y)
      {
         return PInvokes.OnMouseMove(CPPContainer, x, y);
      }

   }
}
