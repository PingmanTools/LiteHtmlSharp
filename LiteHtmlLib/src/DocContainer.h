#pragma once

#include "CallbackDelegates.h"
#include "../../litehtml/src/html.h"
#include "../../litehtml/src/context.h"
#include "../../litehtml/src/document.h"
#include "TagElement.h"

#if defined(LITEHTML_UTF8)
#define _T(x) x
#else
#define _T(x) L ##x
#endif

class DocContainer : public litehtml::document_container
{
public:
   DocContainer();
   ~DocContainer();

   // Inherited via document_container
   virtual litehtml::uint_ptr create_font(const litehtml::tchar_t * faceName, int size, int weight, litehtml::font_style italic, unsigned int decoration, litehtml::font_metrics * fm) override;
   virtual void delete_font(litehtml::uint_ptr hFont) override;
   virtual int text_width(const litehtml::tchar_t * text, litehtml::uint_ptr hFont) override;
   virtual void draw_text(litehtml::uint_ptr hdc, const litehtml::tchar_t * text, litehtml::uint_ptr hFont, litehtml::web_color color, const litehtml::position & pos) override;
   virtual int pt_to_px(int pt) override;
   virtual int get_default_font_size() const override;
   virtual const litehtml::tchar_t * get_default_font_name() const override;
   virtual void draw_list_marker(litehtml::uint_ptr hdc, const litehtml::list_marker & marker) override;
   virtual void load_image(const litehtml::tchar_t * src, const litehtml::tchar_t * baseurl, bool redraw_on_ready) override;
   virtual void get_image_size(const litehtml::tchar_t * src, const litehtml::tchar_t * baseurl, litehtml::size & sz) override;
   virtual void draw_background(litehtml::uint_ptr hdc, const litehtml::background_paint & bg) override;
   virtual void draw_borders(litehtml::uint_ptr hdc, const litehtml::borders & borders, const litehtml::position & draw_pos, bool root) override;
   virtual void set_caption(const litehtml::tchar_t * caption) override;
   virtual void set_base_url(const litehtml::tchar_t * base_url) override;
   virtual void link(const std::shared_ptr<litehtml::document>& doc, const litehtml::element::ptr & el) override;
   virtual void on_anchor_click(const litehtml::tchar_t * url, const litehtml::element::ptr & el) override;
   virtual void set_cursor(const litehtml::tchar_t * cursor) override;
   virtual void transform_text(litehtml::tstring & text, litehtml::text_transform tt) override;
   virtual void import_css(litehtml::tstring & text, const litehtml::tstring & url, litehtml::tstring & baseurl) override;
   virtual void set_clip(const litehtml::position & pos, const litehtml::border_radiuses & bdr_radius, bool valid_x, bool valid_y) override;
   virtual void del_clip() override;
   virtual void get_client_rect(litehtml::position & client) const override;
   virtual std::shared_ptr<litehtml::element> create_element(const litehtml::tchar_t * tag_name, const litehtml::string_map & attributes, const std::shared_ptr<litehtml::document>& doc) override;
   virtual void get_media_features(litehtml::media_features & media) const override;
   virtual void get_language(litehtml::tstring & language, litehtml::tstring & culture) const override;

   void RenderHTML(const litehtml::tchar_t* html, int maxWidth);
   void Render(int maxWidth);
   void SetMasterCSS(const litehtml::tchar_t* css);
   bool OnMouseMove(int x, int y);
   bool OnMouseLeave();
   bool OnLeftButtonDown(int x, int y);
   bool OnLeftButtonUp(int x, int y);
   void Draw(int x, int y, litehtml::position clip);
   ElementInfo& GetTagElementInfo(int ID);
   bool OnMediaChanged();


   std::shared_ptr<litehtml::document> _document;

   DrawBorders_function DrawBorders = 0;
   DrawBackground_function DrawBackground = 0;
   GetImageSize_function GetImageSize = 0;
   Test_function TestCallback = 0;
   ImportCss_function ImportCss = 0;
   TransformText_function TransformText = 0;

   DrawText_function DrawText = 0;
   GetTextWidth_function GetTextWidth = 0;
   CreateFont_function CreateFont = 0;
   GetDefaultFontSize_function GetDefaultFontSize = 0;
   GetDefaultFontName_function GetDefaultFontName = 0;

   GetClientRect_function GetClientRect = 0;
   GetMediaFeatures_function GetMediaFeatures = 0;

   OnAnchorClick_function OnAnchorClick = 0;
   SetCursor_function SetCursor = 0;
   SetBaseURL_function SetBaseURL = 0;
   SetCaption_function SetCaption = 0;
   PTtoPX_function PTtoPX = 0;
   CreateElement_function CreateElement = 0;

private:
   litehtml::context _context;
   litehtml::position _drawPos;

   std::map<int, std::shared_ptr<TagElement>> _elements;
};