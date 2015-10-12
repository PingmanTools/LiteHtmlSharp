#pragma once

#include "../litehtml/src/html.h"

typedef void(*Test_function)(int someNumber);
typedef void(*DrawBorders_function)(litehtml::uint_ptr hdc, const litehtml::borders & borders, const litehtml::position & draw_pos, bool root);
typedef void(*DrawBackground_function)(litehtml::uint_ptr hdc, const litehtml::tchar_t* image, litehtml::background_repeat repeat, const litehtml::web_color& color, const litehtml::position& pos);

extern "C" __declspec(dllexport) void SetDrawBorders(DrawBorders_function callback);
extern "C" __declspec(dllexport) void SetDrawBackground(DrawBackground_function callback);
extern "C" __declspec(dllexport) void SetTestFunction(Test_function callback);
extern "C" __declspec(dllexport) void Init(const litehtml::tchar_t* html);
extern "C" __declspec(dllexport) void OnMouseMove();
