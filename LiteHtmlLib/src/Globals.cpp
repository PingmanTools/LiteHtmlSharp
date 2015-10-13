#include "Globals.h"

#include "DocContainer.h"

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

__declspec(dllexport) void TriggerTestCallback(DocContainer* container)
{
   container->TestCallback(1337);
}

__declspec(dllexport) void SetTestFunction(Test_function callback)
{
   callback(50);
}

__declspec(dllexport) DocContainer* Init()
{
   DocContainer* container = new DocContainer();
   _containers.push_back(container);
   return container;
}

__declspec(dllexport) void RenderHTML(DocContainer* container, const litehtml::tchar_t* html)
{
   container->RenderHTML(html);
}

__declspec(dllexport) void SetMasterCSS(DocContainer* container, const litehtml::tchar_t* css)
{
   container->SetMasterCSS(css);
}

__declspec(dllexport) bool OnMouseMove(DocContainer* container, int x, int y)
{
   return container->OnMouseMove(x, y);
}

__declspec(dllexport) void Draw(DocContainer* container)
{
   container->Draw();
}