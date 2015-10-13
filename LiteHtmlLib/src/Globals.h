#pragma once

#include "../../litehtml/src/html.h"

class DocContainer;

typedef void(*Test_function)(const litehtml::tchar_t* someStr);

typedef void(*DrawBorders_function)(litehtml::uint_ptr hdc, const litehtml::borders & borders, const litehtml::position & draw_pos, bool root);
typedef void(*DrawBackground_function)(litehtml::uint_ptr hdc, const litehtml::tchar_t* image, litehtml::background_repeat repeat, const litehtml::web_color& color, const litehtml::position& pos);
typedef void(*GetImageSize_function)(const litehtml::tchar_t* image, litehtml::size& size);

// callback registration
extern "C" __declspec(dllexport) void SetDrawBorders(DocContainer* container, DrawBorders_function callback);
extern "C" __declspec(dllexport) void SetDrawBackground(DocContainer* container, DrawBackground_function callback);
extern "C" __declspec(dllexport) void SetGetImageSize(DocContainer* container, GetImageSize_function callback);

// container functions
extern "C" __declspec(dllexport) DocContainer* Init();
extern "C" __declspec(dllexport) bool OnMouseMove(DocContainer* container, int x, int y);
extern "C" __declspec(dllexport) void RenderHTML(DocContainer* container, const litehtml::tchar_t* html);
extern "C" __declspec(dllexport) void Draw(DocContainer* container);
extern "C" __declspec(dllexport) void SetMasterCSS(DocContainer* container, const litehtml::tchar_t* css);
extern "C" __declspec(dllexport) void SetTestCallback(DocContainer* container, Test_function callback);
extern "C" __declspec(dllexport) void TriggerTestCallback(DocContainer* container, const litehtml::tchar_t* testString);

extern "C" __declspec(dllexport) void SetTestFunction(Test_function callback);