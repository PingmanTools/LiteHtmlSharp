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
   return CreateFont(faceName, size, weight, italic, decoration, *fm);
}

void DocContainer::delete_font(litehtml::uint_ptr hFont)
{
   int i = 0;
}

int DocContainer::text_width(const litehtml::tchar_t * text, litehtml::uint_ptr hFont)
{
   return GetTextWidth(text, hFont);
}

void DocContainer::draw_text(litehtml::uint_ptr hdc, const litehtml::tchar_t * text, litehtml::uint_ptr hFont, litehtml::web_color color, const litehtml::position & pos)
{
   DrawText(text, hFont, color, pos);
}

int DocContainer::pt_to_px(int pt)
{
   return PTtoPX(pt);
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
   SetBaseURL(base_url);
}

void DocContainer::link(const std::shared_ptr<litehtml::document>& doc, const litehtml::element::ptr & el)
{
   int i = 0;
}

void DocContainer::on_anchor_click(const litehtml::tchar_t * url, const litehtml::element::ptr & el)
{
   OnAnchorClick(url);
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
   text = ImportCss(url.c_str(), baseurl.c_str());
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
   GetClientRect(client);
}

std::shared_ptr<litehtml::element> DocContainer::create_element(const litehtml::tchar_t * tag_name, const litehtml::string_map & attributes, const std::shared_ptr<litehtml::document>& doc)
{
   /*litehtml::tstring strToReturn; //This is no longer on the heap

   for (auto attr : attributes)
   {
      strToReturn.append(attr.first);
      strToReturn.append("=");
      strToReturn.append(attr.second);
      strToReturn.append("\n");
   }*/

   int elementID = CreateElement(tag_name, nullptr);
   if (elementID > 0)
   {
      std::shared_ptr<TagElement> result(new TagElement(doc));
      _elements[elementID] = result;
      return std::shared_ptr<litehtml::element>(result.get());
   }
   else
   {
      return 0;
   }
}

ElementInfo& DocContainer::GetTagElementInfo(int ID)
{
   auto element = _elements[ID];
   return element->GetManagedInfo();
}

void DocContainer::get_media_features(litehtml::media_features & media) const
{
   GetMediaFeatures(media);
}

void DocContainer::get_language(litehtml::tstring & language, litehtml::tstring & culture) const
{
   int i = 0;
}

void DocContainer::SetMasterCSS(const tchar_t* css)
{
   _context.load_master_stylesheet(css);
}

void DocContainer::RenderHTML(const tchar_t* html, int maxWidth)
{
   if (_document != nullptr)
   {
      _document.reset();
   }

   _document = document::createFromString(html, this, &_context);
   _document->render(maxWidth);
}

void DocContainer::Render(int maxWidth)
{
   _document->render(maxWidth);
}

void DocContainer::Draw(int x, int y, litehtml::position clip)
{
   _document->draw(NULL, x, y, &clip);
}

bool DocContainer::OnMouseMove(int x, int y)
{
   std::vector<litehtml::position> redraw_boxes;
   return _document->on_mouse_over(x, y, 0, 0, redraw_boxes);
}

bool DocContainer::OnMouseLeave()
{
   std::vector<litehtml::position> redraw_boxes;
   return _document->on_mouse_leave(redraw_boxes);
}

bool DocContainer::OnLeftButtonDown(int x, int y)
{
   std::vector<litehtml::position> redraw_boxes;
   return _document->on_lbutton_down(x, y, 0, 0, redraw_boxes);
}

bool DocContainer::OnLeftButtonUp(int x, int y)
{
   std::vector<litehtml::position> redraw_boxes;
   return _document->on_lbutton_up(x, y, 0, 0, redraw_boxes);
}

bool DocContainer::OnMediaChanged()
{
   return _document->media_changed();
}