#pragma once
#include "litehtml/el_image.h"
class host_element : public litehtml::el_image {
  litehtml::size intrinsic;

public:
  host_element(const litehtml::document::ptr &doc, float w, float h)
      : el_image(doc), intrinsic(litehtml::pixel_t(w), litehtml::pixel_t(h)) {}
  void parse_attributes() override {
    if (const char *value = get_attr("width"))
      map_to_dimension_property(litehtml::_width_, value);
    if (const char *value = get_attr("height"))
      map_to_dimension_property(litehtml::_height_, value);
  }
  void get_content_size(litehtml::size &out, litehtml::pixel_t) override {
    out = intrinsic;
  }
  void draw(litehtml::uint_ptr, litehtml::pixel_t, litehtml::pixel_t,
            const litehtml::position *,
            const std::shared_ptr<litehtml::render_item> &) override {}
};
