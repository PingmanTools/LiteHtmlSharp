#pragma once

#include "../../litehtml/src/html.h"

struct ElementInfo;
class DocContainer;

typedef void(*Test_function)(const litehtml::tchar_t* someStr);

typedef void(*DrawBorders_function)(litehtml::uint_ptr hdc, const litehtml::borders & borders, const litehtml::position & draw_pos, bool root);
typedef void(*DrawBackground_function)(litehtml::uint_ptr hdc, const litehtml::tchar_t* image, litehtml::background_repeat repeat, const litehtml::web_color& color, const litehtml::position& pos);
typedef void(*GetImageSize_function)(const litehtml::tchar_t* image, litehtml::size& size);

typedef void(*DrawText_function)(const litehtml::tchar_t* text, litehtml::uint_ptr font, litehtml::web_color& color, const litehtml::position& pos);
typedef int(*GetTextWidth_function)(const litehtml::tchar_t* text, litehtml::uint_ptr font);
typedef litehtml::uint_ptr(*CreateFont_function)(const litehtml::tchar_t* faceName, int size, int weight, litehtml::font_style italic, unsigned int decoration, litehtml::font_metrics& fm);

typedef litehtml::tchar_t*(*ImportCss_function)(const litehtml::tchar_t* url, const litehtml::tchar_t* baseurl);

typedef void(*GetClientRect_function)(litehtml::position & client);
typedef void(*GetMediaFeatures_function)(litehtml::media_features & media);

typedef void(*SetBaseURL_function)(const litehtml::tchar_t * base_url);
typedef void(*OnAnchorClick_function)(const litehtml::tchar_t * url);

typedef int(*PTtoPX_function)(int pt);

typedef int(*CreateElement_function)(const litehtml::tchar_t * tag);

// callback registration
extern "C" __declspec(dllexport) void SetDrawBorders(DocContainer* container, DrawBorders_function callback);
extern "C" __declspec(dllexport) void SetDrawBackground(DocContainer* container, DrawBackground_function callback);
extern "C" __declspec(dllexport) void SetGetImageSize(DocContainer* container, GetImageSize_function callback);

extern "C" __declspec(dllexport) void SetCreateFont(DocContainer* container, CreateFont_function callback);
extern "C" __declspec(dllexport) void SetGetTextWidth(DocContainer* container, GetTextWidth_function callback);
extern "C" __declspec(dllexport) void SetDrawText(DocContainer* container, DrawText_function callback);

extern "C" __declspec(dllexport) void SetImportCss(DocContainer* container, ImportCss_function callback);

extern "C" __declspec(dllexport) void SetGetClientRect(DocContainer* container, GetClientRect_function callback);
extern "C" __declspec(dllexport) void SetGetMediaFeatures(DocContainer* container, GetMediaFeatures_function callback);

extern "C" __declspec(dllexport) void SetSetBaseURL(DocContainer* container, SetBaseURL_function callback);
extern "C" __declspec(dllexport) void SetOnAnchorClick(DocContainer* container, OnAnchorClick_function callback);

extern "C" __declspec(dllexport) void SetPTtoPX(DocContainer* container, PTtoPX_function callback);
extern "C" __declspec(dllexport) void SetCreateElement(DocContainer* container, CreateElement_function callback);

// container functions
extern "C" __declspec(dllexport) DocContainer* Init();

extern "C" __declspec(dllexport) bool OnMouseMove(DocContainer* container, int x, int y);
extern "C" __declspec(dllexport) bool OnMouseLeave(DocContainer* container);
extern "C" __declspec(dllexport) bool OnLeftButtonUp(DocContainer* container, int x, int y);
extern "C" __declspec(dllexport) bool OnLeftButtonDown(DocContainer* container, int x, int y);

extern "C" __declspec(dllexport) void RenderHTML(DocContainer* container, const litehtml::tchar_t* html, int maxWidth);
extern "C" __declspec(dllexport) void Draw(DocContainer* container, int x, int y, litehtml::position clip);
extern "C" __declspec(dllexport) void SetMasterCSS(DocContainer* container, const litehtml::tchar_t* css);
extern "C" __declspec(dllexport) ElementInfo& GetElementInfo(DocContainer* container, int ID);

extern "C" __declspec(dllexport) void SetTestCallback(DocContainer* container, Test_function callback);
extern "C" __declspec(dllexport) void TriggerTestCallback(DocContainer* container, const litehtml::tchar_t* testString);
extern "C" __declspec(dllexport) void SetTestFunction(Test_function callback);