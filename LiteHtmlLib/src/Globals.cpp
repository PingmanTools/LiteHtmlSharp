#include "Globals.h"

std::vector<DocContainer*> _containers;

__declspec(dllexport) void SetDrawBorders(DocContainer* container, DrawBorders_function callback)
{
   container->DrawBorders = callback;
}

__declspec(dllexport) void SetDrawBackground(DocContainer* container, DrawBackground_function callback)
{
   container->DrawBackground = callback;
}

__declspec(dllexport) void SetGetImageSize(DocContainer* container, GetImageSize_function callback)
{
   container->GetImageSize = callback;
}

__declspec(dllexport) void SetTestCallback(DocContainer* container, Test_function callback)
{
   container->TestCallback = callback;
}

__declspec(dllexport) void TriggerTestCallback(DocContainer* container, const litehtml::tchar_t* testString)
{
   container->TestCallback(testString);
}

__declspec(dllexport) void SetTestFunction(Test_function callback)
{
   callback(_T("xxx test string"));
}

__declspec(dllexport) DocContainer* Init()
{
   DocContainer* container = new DocContainer();
   _containers.push_back(container);
   return container;
}

__declspec(dllexport) void Delete(DocContainer* container)
{
   auto foundAt = std::find(_containers.begin(), _containers.end(), container);
   if (foundAt != _containers.end())
   {
      _containers.erase(_containers.begin() + (foundAt - _containers.begin()));
   }
   delete container;
}

__declspec(dllexport) void RenderHTML(DocContainer* container, const litehtml::tchar_t* html, int maxWidth)
{
   container->RenderHTML(html, maxWidth);
}

__declspec(dllexport) void Render(DocContainer* container, int maxWidth)
{
   container->Render(maxWidth);
}

__declspec(dllexport) int Height(DocContainer* container)
{
	return container->Height();
}

__declspec(dllexport) int Width(DocContainer* container)
{
	return container->Width();
}

__declspec(dllexport) void SetMasterCSS(DocContainer* container, const litehtml::tchar_t* css)
{
   container->SetMasterCSS(css);
}

__declspec(dllexport) bool OnMouseMove(DocContainer* container, int x, int y)
{
   return container->OnMouseMove(x, y);
}

__declspec(dllexport) bool OnMouseLeave(DocContainer* container)
{
   return container->OnMouseLeave();
}

__declspec(dllexport) bool OnLeftButtonUp(DocContainer* container, int x, int y)
{
   return container->OnLeftButtonUp(x, y);
}

__declspec(dllexport) bool OnLeftButtonDown(DocContainer* container, int x, int y)
{
   return container->OnLeftButtonDown(x, y);
}

__declspec(dllexport) void Draw(DocContainer* container, int x, int y, litehtml::position clip)
{
   container->Draw(x, y, clip);
}

__declspec(dllexport) void SetCreateFont(DocContainer* container, CreateFont_function callback)
{
   container->CreateFont = callback;
}

__declspec(dllexport) void SetGetTextWidth(DocContainer* container, GetTextWidth_function callback)
{
   container->GetTextWidth = callback;
}

__declspec(dllexport) void SetDrawText(DocContainer* container, DrawText_function callback)
{
   container->DrawText = callback;
}

__declspec(dllexport) void SetGetDefaultFontSize(DocContainer* container, GetDefaultFontSize_function callback)
{
   container->GetDefaultFontSize = callback;
}

__declspec(dllexport) void SetGetDefaultFontName(DocContainer* container, GetDefaultFontName_function callback)
{
   container->GetDefaultFontName = callback;
}

__declspec(dllexport) void SetImportCss(DocContainer* container, ImportCss_function callback)
{
   container->ImportCss = callback;
}

__declspec(dllexport) void SetTransformText(DocContainer* container, TransformText_function callback)
{
   container->TransformText = callback;
}

__declspec(dllexport) void SetGetClientRect(DocContainer* container, GetClientRect_function callback)
{
   container->GetClientRect = callback;
}

__declspec(dllexport) void SetGetMediaFeatures(DocContainer* container, GetMediaFeatures_function callback)
{
   container->GetMediaFeatures = callback;
}

__declspec(dllexport) void SetSetBaseURL(DocContainer* container, SetBaseURL_function callback)
{
   container->SetBaseURL = callback;
}

__declspec(dllexport) void SetCaption(DocContainer* container, SetCaption_function callback)
{
   container->SetCaption = callback;
}

__declspec(dllexport) void SetOnAnchorClick(DocContainer* container, OnAnchorClick_function callback)
{
   container->OnAnchorClick = callback;
}

__declspec(dllexport) void SetSetCursor(DocContainer* container, SetCursor_function callback)
{
   container->SetCursor = callback;
}

__declspec(dllexport) void SetPTtoPX(DocContainer* container, PTtoPX_function callback)
{
   container->PTtoPX = callback;
}

__declspec(dllexport) void SetCreateElement(DocContainer* container, CreateElement_function callback)
{
   container->CreateElement = callback;
}

__declspec(dllexport) ElementInfo GetElementInfo(DocContainer* container, int ID)
{
   return container->GetTagElementInfo(ID);
}

__declspec(dllexport) bool OnMediaChanged(DocContainer* container)
{
   return container->OnMediaChanged();
}
