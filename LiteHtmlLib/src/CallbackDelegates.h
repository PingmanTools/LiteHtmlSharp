#pragma once

#include "../../litehtml/src/html.h"
#include "TagElement.h"

struct Callbacks;
class DocContainer;

// test functions
typedef void(*TestCallbackFunc)(int x, const litehtml::tchar_t* testString);
typedef void(*TriggerTestCallbackFunc)(DocContainer* container, int x, const litehtml::tchar_t* testString);

// callbacks
typedef void(*DrawBordersFunc)(litehtml::uint_ptr hdc, const litehtml::borders & borders, const litehtml::position & draw_pos, bool root);
typedef void(*DrawBackgroundFunc)(litehtml::uint_ptr hdc, const litehtml::tchar_t* image, litehtml::background_repeat repeat, const litehtml::web_color& color, const litehtml::position& pos, const litehtml::border_radiuses& borderRadiuses, const litehtml::position& borderBox, bool isRoot);
typedef void(*GetImageSizeFunc)(const litehtml::tchar_t* image, litehtml::size& size);

typedef void(*DrawTextFunc)(const litehtml::tchar_t* text, litehtml::uint_ptr font, litehtml::web_color& color, const litehtml::position& pos);
typedef int(*GetTextWidthFunc)(const litehtml::tchar_t* text, litehtml::uint_ptr font);
typedef litehtml::uint_ptr(*CreateFontFunc)(const litehtml::tchar_t* faceName, int size, int weight, litehtml::font_style italic, unsigned int decoration, litehtml::font_metrics& fm);
typedef int(*GetDefaultFontSizeFunc)();
typedef litehtml::tchar_t*(*GetDefaultFontNameFunc)();

typedef litehtml::tchar_t*(*ImportCssFunc)(const litehtml::tchar_t* url, const litehtml::tchar_t* baseurl);

typedef void(*GetClientRectFunc)(litehtml::position & client);
typedef void(*GetMediaFeaturesFunc)(litehtml::media_features & media);

typedef void(*SetBaseURLFunc)(const litehtml::tchar_t * base_url);
typedef void(*SetCaptionFunc)(const litehtml::tchar_t * caption);
typedef void(*OnAnchorClickFunc)(const litehtml::tchar_t * url);

typedef int(*PTtoPXFunc)(int pt);

typedef bool(*ShouldCreateElementFunc)(const litehtml::tchar_t * tag);

typedef int(*CreateElementFunc)(const litehtml::tchar_t * tag, const char * attributes, ElementInfo& elementInfo);

typedef void(*SetCursorFunc)(const litehtml::tchar_t * cursor);

typedef void(*DrawListMarkerFunc)(const litehtml::tchar_t* image, const litehtml::tchar_t* baseurl, litehtml::list_style_type marker_type, const litehtml::web_color& color, const litehtml::position& pos);

typedef litehtml::tchar_t*(*TransformTextFunc)(const litehtml::tchar_t* text, litehtml::text_transform tt);

typedef void(*SetCallbacksFunc)(Callbacks& callbacks);

// invoke functions

typedef void(*DeleteFunc)(DocContainer* container);

typedef bool(*OnMouseMoveFunc)(DocContainer* container, int x, int y);
typedef bool(*OnMouseLeaveFunc)(DocContainer* container);
typedef bool(*OnLeftButtonUpFunc)(DocContainer* container, int x, int y);
typedef bool(*OnLeftButtonDownFunc)(DocContainer* container, int x, int y);

typedef void(*CreateFromStringFunc)(DocContainer* container, const litehtml::tchar_t* html);
typedef void(*RenderFunc)(DocContainer* container, int maxWidth);
typedef void(*DrawFunc)(DocContainer* container, int x, int y, litehtml::position clip);
typedef void(*SetMasterCSSFunc)(DocContainer* container, const litehtml::tchar_t* css);
typedef ElementInfo*(*GetElementInfoFunc)(DocContainer* container, int ID);
typedef bool(*OnMediaChangedFunc)(DocContainer* container);

typedef int(*GetWidthFunc)(DocContainer* container);
typedef int(*GetHeightFunc)(DocContainer* container);

struct DocumentCalls
{
public:
   DocContainer* Container = 0;

   // Test Methods
   TriggerTestCallbackFunc TriggerTestCallback = 0;

   // Invoke Methods
   DeleteFunc Delete = 0;

   OnMouseMoveFunc OnMouseMove = 0;
   OnMouseLeaveFunc OnMouseLeave = 0;
   OnLeftButtonUpFunc OnLeftButtonUp = 0;
   OnLeftButtonDownFunc OnLeftButtonDown = 0;

   CreateFromStringFunc CreateFromString = 0;
   RenderFunc Render = 0;
   DrawFunc Draw = 0;
   SetMasterCSSFunc SetMasterCSS = 0;
   GetElementInfoFunc GetElementInfo = 0;
   OnMediaChangedFunc OnMediaChanged = 0;

   GetWidthFunc GetWidth = 0;
   GetHeightFunc GetHeight = 0;
};

struct Callbacks
{
public:
   // Test Methods
   TestCallbackFunc TestCallback = 0;

   // Callbacks
   SetCaptionFunc SetCaption = 0;
   GetDefaultFontNameFunc GetDefaultFontName = 0;
   GetDefaultFontSizeFunc GetDefaultFontSize = 0;

   DrawBordersFunc DrawBorders = 0;
   DrawBackgroundFunc DrawBackground = 0;
   GetImageSizeFunc GetImageSize = 0;
   ImportCssFunc ImportCss = 0;

   DrawTextFunc DrawText = 0;
   GetTextWidthFunc GetTextWidth = 0;
   CreateFontFunc CreateFont = 0;

   GetClientRectFunc GetClientRect = 0;
   GetMediaFeaturesFunc GetMediaFeatures = 0;

   OnAnchorClickFunc OnAnchorClick = 0;
   SetBaseURLFunc SetBaseURL = 0;
   PTtoPXFunc PTtoPX = 0;
   ShouldCreateElementFunc ShouldCreateElement = 0;
   CreateElementFunc CreateElement = 0;

   DrawListMarkerFunc DrawListMarker = 0;
   SetCursorFunc SetCursor = 0;

   TransformTextFunc TransformText = 0;
};