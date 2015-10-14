#pragma once

#include "../../litehtml/src/html.h"

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