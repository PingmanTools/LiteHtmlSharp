#pragma once

#include "CallbackDelegates.h"
#include "DocContainer.h"
#include "TagElement.h"

// callback registration
extern "C" __declspec(dllexport) void SetDrawBorders(DocContainer* container, DrawBorders_function callback);
extern "C" __declspec(dllexport) void SetDrawBackground(DocContainer* container, DrawBackground_function callback);
extern "C" __declspec(dllexport) void SetGetImageSize(DocContainer* container, GetImageSize_function callback);

extern "C" __declspec(dllexport) void SetCreateFont(DocContainer* container, CreateFont_function callback);
extern "C" __declspec(dllexport) void SetGetTextWidth(DocContainer* container, GetTextWidth_function callback);
extern "C" __declspec(dllexport) void SetDrawText(DocContainer* container, DrawText_function callback);
extern "C" __declspec(dllexport) void SetGetDefaultFontSize(DocContainer* container, GetDefaultFontSize_function callback);
extern "C" __declspec(dllexport) void SetGetDefaultFontName(DocContainer* container, GetDefaultFontName_function callback);

extern "C" __declspec(dllexport) void SetImportCss(DocContainer* container, ImportCss_function callback);

extern "C" __declspec(dllexport) void SetGetClientRect(DocContainer* container, GetClientRect_function callback);
extern "C" __declspec(dllexport) void SetGetMediaFeatures(DocContainer* container, GetMediaFeatures_function callback);

extern "C" __declspec(dllexport) void SetSetBaseURL(DocContainer* container, SetBaseURL_function callback);
extern "C" __declspec(dllexport) void SetOnAnchorClick(DocContainer* container, OnAnchorClick_function callback);

extern "C" __declspec(dllexport) void SetPTtoPX(DocContainer* container, PTtoPX_function callback);
extern "C" __declspec(dllexport) void SetCreateElement(DocContainer* container, CreateElement_function callback);

// container functions
extern "C" __declspec(dllexport) DocContainer* Init();
extern "C" __declspec(dllexport) void Delete(DocContainer* container);

extern "C" __declspec(dllexport) bool OnMouseMove(DocContainer* container, int x, int y);
extern "C" __declspec(dllexport) bool OnMouseLeave(DocContainer* container);
extern "C" __declspec(dllexport) bool OnLeftButtonUp(DocContainer* container, int x, int y);
extern "C" __declspec(dllexport) bool OnLeftButtonDown(DocContainer* container, int x, int y);

extern "C" __declspec(dllexport) void RenderHTML(DocContainer* container, const litehtml::tchar_t* html, int maxWidth);
extern "C" __declspec(dllexport) void Render(DocContainer* container, int maxWidth);
extern "C" __declspec(dllexport) void Draw(DocContainer* container, int x, int y, litehtml::position clip);
extern "C" __declspec(dllexport) void SetMasterCSS(DocContainer* container, const litehtml::tchar_t* css);
extern "C" __declspec(dllexport) ElementInfo GetElementInfo(DocContainer* container, int ID);
extern "C" __declspec(dllexport) bool OnMediaChanged(DocContainer* container);

extern "C" __declspec(dllexport) void SetTestCallback(DocContainer* container, Test_function callback);
extern "C" __declspec(dllexport) void TriggerTestCallback(DocContainer* container, const litehtml::tchar_t* testString);
extern "C" __declspec(dllexport) void SetTestFunction(Test_function callback);