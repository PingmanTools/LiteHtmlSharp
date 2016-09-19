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

   public delegate bool ShouldCreateElementDelegate(string tag);
   public delegate int CreateElementDelegate(string tag, string attributes, ElementInfo elementInfo);
   public delegate string ImportCssDelegate(string url, string baseurl);

   public abstract class Container
   {
      int _testNumber = 0;
      string _testText = null;

      Callbacks _callbacks;
      public Document Document;

      public int ScaleFactor = 1;

      public ShouldCreateElementDelegate ShouldCreateElementCallback;
      public CreateElementDelegate CreateElementCallback;

      public event Action DocumentDrawn;

      public event Action<string> AnchorClicked;

      public Container(string masterCssData)
      {

         Document = new Document();
         PInvokes.Init(ref Document.Calls, InitCallbacks);
         Document.SetMasterCSS(masterCssData);

#if DEBUG
         TestFramework();
#endif
      }

      public virtual void Render(string html) { }

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
         callbacks.OnAnchorClick = OnAnchorClickHandler;

         callbacks.PTtoPX = PTtoPX;
         callbacks.ShouldCreateElement = ShouldCreateElement;
         callbacks.CreateElement = CreateElementWrapper;

         callbacks.SetCursor = SetCursor;
         callbacks.DrawListMarker = DrawListMarker;

         callbacks.TransformText = TransformText;
         callbacks.TestCallback = TestCallback;

         callbacks.SetCaption = SetCaption;
         callbacks.GetDefaultFontName = GetDefaultFontNameWrapper;
         callbacks.GetDefaultFontSize = GetDefaultFontSize;

         _callbacks = callbacks;
      }

      public void Draw(int x, int y, position clip)
      {
         Document.Draw(x, y, clip);
         if (DocumentDrawn != null)
         {
            DocumentDrawn();
         }
      }

      void TestCallback(int number, IntPtr text)
      {
         _testText = PInvokes.PtrToStringUTF8(text);
         _testNumber = number;
      }

      void TestFramework()
      {
         string testStringResult = "Test 1234 ....  \U0001D11E 𝄞 𩸽, ₤ · ₥ · ₦ · ₮ · ₯ · ₹";
         var input = PInvokes.StringToHGlobalUTF8(testStringResult);

         var echoTest = PInvokes.EchoTest(input);
         var echoResult = PInvokes.PtrToStringUTF8(echoTest);

         var worked = testStringResult == echoResult;

         Document.TriggerTestCallback(50, testStringResult);
         if (_testText != testStringResult || _testNumber != 50)
         {
            throw new Exception("Container instance callback test failed. Something is broken!");
         }
      }

      // -----

      private void DrawBackgroundScaled(UIntPtr hdc, IntPtr image, background_repeat repeat, ref web_color color, ref position pos, ref border_radiuses borderRadiuses, ref position borderBox, bool isRoot)
      {
         pos.Scale(ScaleFactor);
         borderRadiuses.Scale(ScaleFactor);
         borderBox.Scale(ScaleFactor);
         DrawBackground(hdc, PInvokes.PtrToStringUTF8(image), repeat, ref color, ref pos, ref borderRadiuses, ref borderBox, isRoot);
      }

      protected abstract void DrawBackground(UIntPtr hdc, string image, background_repeat repeat, ref web_color color, ref position pos, ref border_radiuses borderRadiuses, ref position borderBox, bool isRoot);

      // -----

      private void DrawBordersScaled(UIntPtr hdc, ref borders borders, ref position draw_pos, bool root)
      {
         borders.Scale(ScaleFactor);
         draw_pos.Scale(ScaleFactor);
         DrawBorders(hdc, ref borders, ref draw_pos, root);
      }

      protected abstract void DrawBorders(UIntPtr hdc, ref borders borders, ref position draw_pos, bool root);

      // -----

      private void DrawTextScaled(IntPtr text, UIntPtr font, ref web_color color, ref position pos)
      {
         pos.Scale(ScaleFactor);
         DrawText(PInvokes.PtrToStringUTF8(text), font, ref color, ref pos);
      }

      protected abstract void DrawText(string text, UIntPtr font, ref web_color color, ref position pos);

      // -----

      private void GetImageSize(IntPtr image, ref size size)
      {
         GetImageSize(PInvokes.PtrToStringUTF8(image), ref size);
      }

      protected abstract void GetImageSize(string image, ref size size);

      private int GetTextWidth(IntPtr text, UIntPtr font)
      {
         return GetTextWidth(PInvokes.PtrToStringUTF8(text), font);
      }

      protected abstract int GetTextWidth(string text, UIntPtr font);

      protected abstract void GetClientRect(ref position client);

      private void SetCaption(IntPtr caption)
      {
         SetCaption(PInvokes.PtrToStringUTF8(caption));
      }

      protected abstract void SetCaption(string caption);

      protected abstract int GetDefaultFontSize();

      private IntPtr GetDefaultFontNameWrapper()
      {
         return PInvokes.StringToHGlobalUTF8(GetDefaultFontName());
      }

      protected abstract string GetDefaultFontName();

      protected UIntPtr CreateFontWrapper(IntPtr faceName, int size, int weight, font_style italic, uint decoration, ref font_metrics fm)
      {
         return CreateFont(PInvokes.PtrToStringUTF8(faceName), size, weight, italic, (font_decoration)decoration, ref fm);
      }

      protected abstract UIntPtr CreateFont(string faceName, int size, int weight, font_style italic, font_decoration decoration, ref font_metrics fm);

      public ImportCssDelegate ImportCssCallback;

      private IntPtr ImportCss(IntPtr url, IntPtr baseurl)
      {
         return PInvokes.StringToHGlobalUTF8(ImportCss(PInvokes.PtrToStringUTF8(url), PInvokes.PtrToStringUTF8(baseurl)));
      }

      protected virtual string ImportCss(string url, string baseurl)
      {
         if (ImportCssCallback == null)
         {
            throw new Exception("ImportCss must be overridden or the callback delegate set");
         }
         return ImportCssCallback(url, baseurl);
      }

      protected abstract void GetMediaFeatures(ref media_features media);

      void SetBaseURL(IntPtr base_url)
      {
         SetBaseURL(PInvokes.PtrToStringUTF8(base_url));
      }

      protected abstract void SetBaseURL(string base_url);

      protected void OnAnchorClickHandler(IntPtr url)
      {
         if (AnchorClicked != null)
         {
            AnchorClicked(PInvokes.PtrToStringUTF8(url));
         }
         OnAnchorClick(PInvokes.PtrToStringUTF8(url));
      }

      protected abstract void OnAnchorClick(string url);

      protected abstract int PTtoPX(int pt);

      private bool ShouldCreateElement(IntPtr tag)
      {
         if (ShouldCreateElementCallback != null)
         {
            return ShouldCreateElementCallback(PInvokes.PtrToStringUTF8(tag));
         }
         return false;
      }

      private int CreateElementWrapper(IntPtr tag, IntPtr attributes, [Out, In] ref ElementInfoStruct elementInfo)
      {
         if (CreateElementCallback != null)
         {
            return CreateElementCallback(PInvokes.PtrToStringUTF8(tag), PInvokes.PtrToStringUTF8(attributes), new ElementInfo(elementInfo));
         }
         else
         {
            return 0;
         }
      }

      private void SetCursor(IntPtr cursor)
      {
         SetCursor(PInvokes.PtrToStringUTF8(cursor));
      }

      protected abstract void SetCursor(string cursor);

      private void DrawListMarker(IntPtr image, IntPtr baseURL, list_style_type marker_type, ref web_color color, ref position pos)
      {
         DrawListMarker(PInvokes.PtrToStringUTF8(image), PInvokes.PtrToStringUTF8(baseURL), marker_type, ref color, ref pos);
      }

      protected abstract void DrawListMarker(string image, string baseURL, list_style_type marker_type, ref web_color color, ref position pos);

      private IntPtr TransformText(IntPtr text, text_transform t)
      {
         return PInvokes.StringToHGlobalUTF8(TransformText(PInvokes.PtrToStringUTF8(text), t));
      }

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
