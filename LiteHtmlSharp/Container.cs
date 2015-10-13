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

      public Container()
      {
         #if DEBUG
         //TestFramework();
         #endif

         CPPContainer = PInvokes.Init();

         var cssData = GetMasterCssData();
         PInvokes.SetMasterCSS(CPPContainer, cssData);

         PInvokes.SetDrawBorders(CPPContainer, CreateDelegate(new DrawBordersFunc(DrawBorders)));
         PInvokes.SetDrawBackground(CPPContainer, CreateDelegate(new DrawBackgroundFunc(DrawBackground)));
         PInvokes.SetGetImageSize(CPPContainer, CreateDelegate(new GetImageSizeFunc(GetImageSize)));

         PInvokes.SetDrawText(CPPContainer, CreateDelegate(new DrawTextFunc(DrawText)));
         PInvokes.SetGetTextWidth(CPPContainer, CreateDelegate(new GetTextWidthFunc(GetTextWidth)));
         PInvokes.SetCreateFont(CPPContainer, CreateDelegate(new CreateFontFunc(CreateFont)));

         PInvokes.SetImportCss(CPPContainer, CreateDelegate(new ImportCssFunc(ImportCss)));

         PInvokes.SetGetClientRect(CPPContainer, CreateDelegate(new GetClientRectFunc(GetClientRect)));
         PInvokes.SetGetMediaFeatures(CPPContainer, CreateDelegate(new GetMediaFeaturesFunc(GetMediaFeatures)));
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

      protected abstract void DrawBackground(UIntPtr hdc, string image, background_repeat repeat, ref web_color color, ref position pos);

      protected abstract void DrawBorders(UIntPtr hdc, ref borders borders, ref position draw_pos, bool root);

      protected abstract void GetImageSize(string image, ref size size);

      protected abstract int GetTextWidth(string text, UIntPtr font);

      protected abstract void DrawText(string text, UIntPtr font, ref web_color color, ref position pos);

      protected abstract UIntPtr CreateFont(string faceName, int size, int weight, font_style italic, uint decoration, ref font_metrics fm);

      protected abstract string ImportCss(string url, string baseurl);

      protected abstract string GetMasterCssData();

      protected abstract void GetClientRect(ref position client);

      protected abstract void GetMediaFeatures(ref media_features media);

      public virtual void RenderHtml(string html)
      {
         PInvokes.RenderHTML(CPPContainer, html);
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
   }
}
