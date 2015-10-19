using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Globalization;

namespace LiteHtmlSharp
{
   public abstract class Container
   {
      int _testNumber = 0;
      string _testText = null;

      Callbacks _callbacks;
      public Document Document;

      public int ScaleFactor = 1;
      public CreateElementFunc CreateElementCallback;

      public Container(string masterCssData)
      {
         Document = new Document();

         PInvokes.Init(ref Document.Calls, InitCallbacks);

#if DEBUG
         TestFramework();
#endif

         Document.SetMasterCSS(masterCssData);
      }

      private void InitCallbacks(ref Callbacks callbacks)
      {
         callbacks.DrawBorders = DrawBordersScaled;
         callbacks.DrawBackground = DrawBackgroundScaled;
         callbacks.GetImageSize = GetImageSize;

         callbacks.DrawText = DrawTextScaled;
         callbacks.GetTextWidth = GetTextWidth;
         callbacks.CreateFont = CreateFontWrapper;

         callbacks.ImportCss = ImportCss;

         callbacks.GetClientRect = GetClientRect;
         callbacks.GetMediaFeatures = GetMediaFeatures;

         callbacks.SetBaseURL = SetBaseURL;
         callbacks.OnAnchorClick = OnAnchorClick;

         callbacks.PTtoPX = PTtoPX;
         callbacks.CreateElement = CreateElement;

         callbacks.SetCursor = SetCursor;
         callbacks.DrawListMarker = DrawListMarker;

         callbacks.TransformText = TransformText;
         callbacks.TestCallback = TestCallback;

         callbacks.SetCaption = SetCaption;
         callbacks.GetDefaultFontName = GetDefaultFontName;
         callbacks.GetDefaultFontSize = GetDefaultFontSize;

         _callbacks = callbacks;
      }

      void TestCallback(int number, string text)
      {
         _testText = text;
         _testNumber = number;
      }

      void TestFramework()
      {
         string testStringResult = "Test 1234 .... ₤ · ₥ · ₦ · ₮ · ₯ · ₹";

         Document.TriggerTestCallback(50, testStringResult);
         if (_testText != testStringResult || _testNumber != 50)
         {
            throw new Exception("Container instance callback test failed. Something is broken!");
         }
      }

      // -----

      private void DrawBackgroundScaled(UIntPtr hdc, string image, background_repeat repeat, ref web_color color, ref position pos, ref border_radiuses borderRadiuses, ref position borderBox)
      {
         pos.Scale(ScaleFactor);
         borderRadiuses.Scale(ScaleFactor);
         borderBox.Scale(ScaleFactor);
         DrawBackground(hdc, image, repeat, ref color, ref pos, ref borderRadiuses, ref borderBox);
      }

      protected abstract void DrawBackground(UIntPtr hdc, string image, background_repeat repeat, ref web_color color, ref position pos, ref border_radiuses borderRadiuses, ref position borderBox);

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

      protected abstract void SetCaption(string caption);

      protected abstract int GetDefaultFontSize();

      protected abstract string GetDefaultFontName();

      protected UIntPtr CreateFontWrapper(string faceName, int size, int weight, font_style italic, uint decoration, ref font_metrics fm)
      {
         return CreateFont(faceName, size, weight, italic, (font_decoration)decoration, ref fm);
      }

      protected abstract UIntPtr CreateFont(string faceName, int size, int weight, font_style italic, font_decoration decoration, ref font_metrics fm);

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

      protected abstract void OnAnchorClick(string url);

      protected abstract int PTtoPX(int pt);

      protected int CreateElementWrapper(string tag, string attributes)
      {
         if (CreateElementCallback != null)
         {
            return CreateElementCallback(tag, attributes);
         }
         else
         {
            return CreateElement(tag, attributes);
         }
      }

      protected virtual int CreateElement(string tag, string attributes)
      {
         return 0;
      }

      protected abstract void SetCursor(string cursor);

      protected abstract void DrawListMarker(string image, string baseURL, list_style_type marker_type, ref web_color color, ref position pos);

      protected virtual string TransformText(string text, text_transform t)
      {
         switch (t)
         {
            case text_transform.text_transform_capitalize:
               return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text);
            case text_transform.text_transform_lowercase:
               return text.ToLower();
            case text_transform.text_transform_uppercase:
               return text.ToUpper();
            default:
               return text;
         }
      }
   }
}
