#include "DocContainer.h"

using namespace litehtml;

DocContainer::DocContainer()
{
}


DocContainer::~DocContainer()
{
}

litehtml::uint_ptr DocContainer::create_font(const litehtml::tchar_t * faceName, int size, int weight, litehtml::font_style italic, unsigned int decoration, litehtml::font_metrics * fm)
{
   return litehtml::uint_ptr();
}

void DocContainer::delete_font(litehtml::uint_ptr hFont)
{
   int i = 0;
}

int DocContainer::text_width(const litehtml::tchar_t * text, litehtml::uint_ptr hFont)
{
   return 0;
}

void DocContainer::draw_text(litehtml::uint_ptr hdc, const litehtml::tchar_t * text, litehtml::uint_ptr hFont, litehtml::web_color color, const litehtml::position & pos)
{
   int i = 0;
}

int DocContainer::pt_to_px(int pt)
{
   return 0;
}

int DocContainer::get_default_font_size() const
{
   return 12;
}

const litehtml::tchar_t * DocContainer::get_default_font_name() const
{
   return _T("Arial");
}

void DocContainer::draw_list_marker(litehtml::uint_ptr hdc, const litehtml::list_marker & marker)
{
   int i = 0;
}

void DocContainer::load_image(const litehtml::tchar_t * src, const litehtml::tchar_t * baseurl, bool redraw_on_ready)
{
   int i = 0;
}

void DocContainer::get_image_size(const litehtml::tchar_t * src, const litehtml::tchar_t * baseurl, litehtml::size & sz)
{
   GetImageSize(src, sz);
}

void DocContainer::draw_background(litehtml::uint_ptr hdc, const litehtml::background_paint & bg)
{
   DrawBackground(hdc, bg.image.c_str(), bg.repeat, bg.color, bg.origin_box);
}

void DocContainer::draw_borders(litehtml::uint_ptr hdc, const litehtml::borders & borders, const litehtml::position & draw_pos, bool root)
{
   DrawBorders(hdc, borders, draw_pos, root);
}

void DocContainer::set_caption(const litehtml::tchar_t * caption)
{
   int i = 0;
}

void DocContainer::set_base_url(const litehtml::tchar_t * base_url)
{
   int i = 0;
}

void DocContainer::link(const std::shared_ptr<litehtml::document>& doc, const litehtml::element::ptr & el)
{
   int i = 0;
}

void DocContainer::on_anchor_click(const litehtml::tchar_t * url, const litehtml::element::ptr & el)
{
   int i = 0;
}

void DocContainer::set_cursor(const litehtml::tchar_t * cursor)
{
   int i = 0;
}

void DocContainer::transform_text(litehtml::tstring & text, litehtml::text_transform tt)
{
   int i = 0;
}

void DocContainer::import_css(litehtml::tstring & text, const litehtml::tstring & url, litehtml::tstring & baseurl)
{
   int i = 0;
}

void DocContainer::set_clip(const litehtml::position & pos, const litehtml::border_radiuses & bdr_radius, bool valid_x, bool valid_y)
{
   int i = 0;
}

void DocContainer::del_clip()
{
   int i = 0;
}

void DocContainer::get_client_rect(litehtml::position & client) const
{
   int i = 0;
}

std::shared_ptr<litehtml::element> DocContainer::create_element(const litehtml::tchar_t * tag_name, const litehtml::string_map & attributes, const std::shared_ptr<litehtml::document>& doc)
{
   //MyElement* result = new MyElement(doc);
   //_elements.push_back(result);
   //return std::shared_ptr<litehtml::element>();
   return 0;
}

void DocContainer::get_media_features(litehtml::media_features & media) const
{
   media.width = 100;
   media.height = 100;
   media.resolution = 96;
   media.color = 24;
   media.device_width = 100;
   media.device_height = 100;
   media.color_index = 0;
   media.type = litehtml::media_type::media_type_all;
}

void DocContainer::get_language(litehtml::tstring & language, litehtml::tstring & culture) const
{
   int i = 0;
}

void DocContainer::SetMasterCSS(const tchar_t* css)
{
   _context.load_master_stylesheet(css);
}

void DocContainer::RenderHTML(const tchar_t* html)
{
   /*_drawPos.width = 100;
   _drawPos.height = 100;
   _drawPos.x = 0;
   _drawPos.y = 0;*/

   if (_document != nullptr)
   {
      _document.reset();
   }

   _document = document::createFromString(html, this, &_context);
   _document->render(100);
   _document->draw(NULL, 0, 0, nullptr);
}

void DocContainer::Draw()
{
   _document->draw(NULL, 0, 0, nullptr);
}

bool DocContainer::OnMouseMove(int x, int y)
{
   std::vector<litehtml::position> redraw_boxes;

   if (_document->on_mouse_over(x, y, 0, 0, redraw_boxes))
   {
      return true;
      /*for (litehtml::position::vector::iterator box = redraw_boxes.begin(); box != redraw_boxes.end(); box++)
      {
         _document->draw(NULL, box->x, box->y, box._Ptr);
      }*/
   }

   return false;
}