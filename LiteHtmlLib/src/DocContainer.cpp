#include "DocContainer.h"
#include <functional>

using namespace litehtml;

DocContainer::DocContainer()
{
}


DocContainer::~DocContainer()
{
}

litehtml::uint_ptr DocContainer::create_font(const litehtml::tchar_t * faceName, int size, int weight, litehtml::font_style italic, unsigned int decoration, litehtml::font_metrics * fm)
{
	return _callbacks.CreateFont(faceName, size, weight, italic, decoration, *fm);
}

void DocContainer::delete_font(litehtml::uint_ptr hFont)
{
	int i = 0;
}

int DocContainer::text_width(const litehtml::tchar_t * text, litehtml::uint_ptr hFont)
{
	return _callbacks.GetTextWidth(text, hFont);
}

void DocContainer::draw_text(litehtml::uint_ptr hdc, const litehtml::tchar_t * text, litehtml::uint_ptr hFont, litehtml::web_color color, const litehtml::position & pos)
{
	_callbacks.DrawText(text, hFont, color, pos);
}

int DocContainer::pt_to_px(int pt)
{
	return _callbacks.PTtoPX(pt);
}

int DocContainer::get_default_font_size() const
{
	return _callbacks.GetDefaultFontSize();
}

const litehtml::tchar_t * DocContainer::get_default_font_name() const
{
	return _callbacks.GetDefaultFontName();
}

void DocContainer::draw_list_marker(litehtml::uint_ptr hdc, const litehtml::list_marker & marker)
{
	_callbacks.DrawListMarker(marker.image.c_str(), marker.baseurl, marker.marker_type, marker.color, marker.pos);
}

void DocContainer::load_image(const litehtml::tchar_t * src, const litehtml::tchar_t * baseurl, bool redraw_on_ready)
{
	int i = 0;
}

void DocContainer::get_image_size(const litehtml::tchar_t * src, const litehtml::tchar_t * baseurl, litehtml::size & sz)
{
	_callbacks.GetImageSize(src, sz);
}

void DocContainer::draw_background(litehtml::uint_ptr hdc, const litehtml::background_paint & bg)
{
	_callbacks.DrawBackground(hdc, bg.image.c_str(), bg.repeat, bg.color, bg.image.empty() ? bg.clip_box : bg.origin_box, bg.border_radius, bg.border_box, bg.is_root);
}

void DocContainer::draw_borders(litehtml::uint_ptr hdc, const litehtml::borders & borders, const litehtml::position & draw_pos, bool root)
{
	_callbacks.DrawBorders(hdc, borders, draw_pos, root);
}

void DocContainer::set_caption(const litehtml::tchar_t * caption)
{
	_callbacks.SetCaption(caption);
}

void DocContainer::set_base_url(const litehtml::tchar_t * base_url)
{
	_callbacks.SetBaseURL(base_url);
}

void DocContainer::link(const std::shared_ptr<litehtml::document>& doc, const litehtml::element::ptr & el)
{
	int i = 0;
}

void DocContainer::on_anchor_click(const litehtml::tchar_t * url, const litehtml::element::ptr & el)
{
	_callbacks.OnAnchorClick(url);
}

void DocContainer::set_cursor(const litehtml::tchar_t * cursor)
{
	_callbacks.SetCursor(cursor);
}

void DocContainer::transform_text(litehtml::tstring & text, litehtml::text_transform tt)
{
	text = _callbacks.TransformText(text.c_str(), tt);
}

void DocContainer::import_css(litehtml::tstring & text, const litehtml::tstring & url, litehtml::tstring & baseurl)
{
	text = _callbacks.ImportCss(url.c_str(), baseurl.c_str());
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
	_callbacks.GetClientRect(client);
}

std::shared_ptr<litehtml::element> DocContainer::create_element(const litehtml::tchar_t * tag_name, const litehtml::string_map & attributes, const std::shared_ptr<litehtml::document>& doc)
{
   if (!_callbacks.ShouldCreateElement(tag_name))
   {
      return 0;
   }

	// Pass all attributes to c#
	litehtml::tstring attributeStr;
	for (auto attr : attributes)
	{
		attributeStr.append(attr.first);
		attributeStr.append(_T("="));
		attributeStr.append(attr.second);
		attributeStr.append(_T("\n"));
	}
	std::string _attributes = std::string(attributeStr.begin(), attributeStr.end());
  
	// Create the ElementInfo that c# will use to set things it needs to.  This is marshalled, so be careful on types.
	ElementInfo elementInfo = {};
	int elementID = _callbacks.CreateElement(tag_name, _attributes.c_str(), elementInfo);
	if (elementID > 0)
	{
		std::shared_ptr<TagElement> result(new TagElement(doc));
		result->SetManagedInfo(elementInfo);
		_elements[elementID] = result;
		return std::shared_ptr<litehtml::element>(result.get());
	}
	else
	{
		return 0;
	}
}

ElementInfo* DocContainer::GetElementInfo(int ID)
{
	auto element = _elements[ID];
	if (!element) {
		return NULL;
	}
	return element->GetManagedInfo();
}

void DocContainer::get_media_features(litehtml::media_features & media) const
{
	_callbacks.GetMediaFeatures(media);
}

void DocContainer::get_language(litehtml::tstring & language, litehtml::tstring & culture) const
{
	int i = 0;
}

void DocContainer::SetMasterCSS(const tchar_t* css)
{
	_context.load_master_stylesheet(css);
}

void DocContainer::CreateFromString(const tchar_t* html)
{
	if (_document != nullptr)
	{
		_document.reset();
	}

	_document = document::createFromString(html, this, &_context);
}

int DocContainer::Render(int maxWidth)
{
	return _document->render(maxWidth);
}

void DocContainer::Draw(int x, int y, litehtml::position clip)
{
	_document->draw(NULL, x, y, &clip);
}

bool DocContainer::OnMouseMove(int x, int y)
{
	_mouseX = x;
	_mouseY = y;
	std::vector<litehtml::position> redraw_boxes;
	return _document->on_mouse_over(x, y, x, y, redraw_boxes);
}

bool DocContainer::OnMouseLeave()
{
	std::vector<litehtml::position> redraw_boxes;
	return _document->on_mouse_leave(redraw_boxes);
}

bool DocContainer::OnLeftButtonDown(int x, int y)
{
	std::vector<litehtml::position> redraw_boxes;
	return _document->on_lbutton_down(x, y, x, y, redraw_boxes);
}

bool DocContainer::OnLeftButtonUp(int x, int y)
{
	std::vector<litehtml::position> redraw_boxes;
	return _document->on_lbutton_up(x, y, x, y, redraw_boxes);
}

bool DocContainer::OnMediaChanged()
{
	return _document->media_changed();
}

int DocContainer::GetWidth()
{
	return _document->width();
}

int DocContainer::GetHeight()
{
	return _document->height();
}

const litehtml::tchar_t* DocContainer::GetTooltipText()
{
	if (!_document || !_document->root()) return nullptr;
	auto el = _document->root()->get_element_by_point(_mouseX, _mouseY, _mouseX, _mouseY);
	while (el) {
		auto title = el->get_attr(_T("title"));
		if (title && title[0] != '\0') {
			return title;
		}
		el = el->parent();
	}
	return nullptr;
}

// -------------static wrapper functions ----------
void SetMasterCSS(DocContainer* container, const tchar_t* css)
{
	container->SetMasterCSS(css);
}

void CreateFromString(DocContainer* container, const tchar_t* html)
{
	container->CreateFromString(html);
}

void Render(DocContainer* container, int maxWidth)
{
	container->Render(maxWidth);
}

void Draw(DocContainer* container, int x, int y, litehtml::position clip)
{
	container->Draw(x, y, clip);
}

bool OnMouseMove(DocContainer* container, int x, int y)
{
	return container->OnMouseMove(x, y);
}

bool OnMouseLeave(DocContainer* container)
{
	return container->OnMouseLeave();
}

bool OnLeftButtonDown(DocContainer* container, int x, int y)
{
	return container->OnLeftButtonDown(x, y);
}

bool OnLeftButtonUp(DocContainer* container, int x, int y)
{
	return container->OnLeftButtonUp(x, y);
}

bool OnMediaChanged(DocContainer* container)
{
	return container->OnMediaChanged();
}

int GetHeight(DocContainer* container)
{
	return container->GetHeight();
}

int GetWidth(DocContainer* container)
{
	return container->GetWidth();
}

ElementInfo* GetElementInfo(DocContainer* container, int ID)
{
	return container->GetElementInfo(ID);
}

const litehtml::tchar_t* GetTooltipText(DocContainer* container)
{
	return container->GetTooltipText();
}

void DocContainer::SetDocumentCalls(DocumentCalls& docCalls)
{
	docCalls.OnMouseMove = ::OnMouseMove;
	docCalls.OnMouseLeave = ::OnMouseLeave;
	docCalls.OnLeftButtonUp = ::OnLeftButtonUp;
	docCalls.OnLeftButtonDown = ::OnLeftButtonDown;

	docCalls.CreateFromString = ::CreateFromString;
	docCalls.Render = ::Render;
	docCalls.Draw = ::Draw;
	docCalls.SetMasterCSS = ::SetMasterCSS;
	docCalls.GetElementInfo = ::GetElementInfo;
	docCalls.OnMediaChanged = ::OnMediaChanged;

	docCalls.GetWidth = ::GetWidth;
	docCalls.GetHeight = ::GetHeight;

	docCalls.GetTooltipText = ::GetTooltipText;

	docCalls.Container = this;
}