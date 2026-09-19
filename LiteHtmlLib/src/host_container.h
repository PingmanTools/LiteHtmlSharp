#pragma once
#include "display_list.h"
#include "lh_api.h"
#include "litehtml.h"
#include <unordered_map>
class host_element;
class host_container : public litehtml::document_container {
public:
  lh_host host{};
  display_list list;
  mutable std::string font_name = "sans-serif";
  std::unordered_map<int32_t, std::weak_ptr<host_element>> custom;
  std::unordered_map<lh_element, litehtml::element::ptr> elements;
  explicit host_container(const lh_host &h) : host(h) {}
  ~host_container() override = default;
  lh_element retain(const litehtml::element::ptr &el) {
    if (!el)
      return 0;
    auto id = reinterpret_cast<lh_element>(el.get());
    elements[id] = el;
    return id;
  }
  void redraw(const litehtml::position &p);
  litehtml::uint_ptr create_font(const litehtml::font_description &descr,
                                 const litehtml::document *doc,
                                 litehtml::font_metrics *fm) override;
  void delete_font(litehtml::uint_ptr hFont) override;
  litehtml::pixel_t text_width(const char *text,
                               litehtml::uint_ptr hFont) override;
  void draw_text(litehtml::uint_ptr hdc, const char *text,
                 litehtml::uint_ptr hFont, litehtml::web_color color,
                 const litehtml::position &pos) override;
  litehtml::pixel_t pt_to_px(float pt) const override;
  litehtml::pixel_t get_default_font_size() const override;
  const char *get_default_font_name() const override;
  void draw_list_marker(litehtml::uint_ptr hdc,
                        const litehtml::list_marker &marker) override;
  void load_image(const char *src, const char *baseurl,
                  bool redraw_on_ready) override;
  void get_image_size(const char *src, const char *baseurl,
                      litehtml::size &sz) override;
  void draw_image(litehtml::uint_ptr hdc,
                  const litehtml::background_layer &layer,
                  const std::string &url, const std::string &base_url) override;
  void draw_solid_fill(litehtml::uint_ptr hdc,
                       const litehtml::background_layer &layer,
                       const litehtml::web_color &color) override;
  void draw_linear_gradient(
      litehtml::uint_ptr hdc, const litehtml::background_layer &layer,
      const litehtml::background_layer::linear_gradient &gradient) override;
  void draw_radial_gradient(
      litehtml::uint_ptr hdc, const litehtml::background_layer &layer,
      const litehtml::background_layer::radial_gradient &gradient) override;
  void draw_conic_gradient(
      litehtml::uint_ptr hdc, const litehtml::background_layer &layer,
      const litehtml::background_layer::conic_gradient &gradient) override;
  void draw_borders(litehtml::uint_ptr hdc, const litehtml::borders &borders,
                    const litehtml::position &draw_pos, bool root) override;

  void set_caption(const char *caption) override;
  void set_base_url(const char *base_url) override;
  void link(const std::shared_ptr<litehtml::document> &doc,
            const litehtml::element::ptr &el) override;
  void on_anchor_click(const char *url,
                       const litehtml::element::ptr &el) override;
  bool on_element_click(const litehtml::element::ptr &el) override;
  void on_mouse_event(const litehtml::element::ptr &el,
                      litehtml::mouse_event event) override;
  void set_cursor(const char *cursor) override;
  void transform_text(std::string &text, litehtml::text_transform tt) override;
  void import_css(std::string &text, const std::string &url,
                  std::string &baseurl) override;
  // Managed renderers measure and draw in logical units, then apply host scaling.
  litehtml::pixel_t resolve_font_size(litehtml::pixel_t size) const override { return size; }
  litehtml::pixel_t resolve_border_width(litehtml::pixel_t width) const override { return width; }
  void round_paint_position(litehtml::position&) const override {}
  bool supports_opacity_groups() const override { return true; }
  void push_opacity(float opacity) override {
    list.command(LH_CMD_PUSH_OPACITY, lh_cmd_push_opacity{opacity});
  }
  void pop_opacity() override { list.command(LH_CMD_POP_OPACITY, nullptr, 0); }
  void push_transform(const litehtml::motion_matrix &matrix) override;
  void pop_transform() override;
  void set_clip(const litehtml::position &pos,
                const litehtml::border_radiuses &bdr_radius) override;
  void del_clip() override;
  void get_viewport(litehtml::position &viewport) const override;
  litehtml::element::ptr
  create_element(const char *tag_name, const litehtml::string_map &attributes,
                 const std::shared_ptr<litehtml::document> &doc) override;

  void get_media_features(litehtml::media_features &media) const override;
  void get_language(std::string &language, std::string &culture) const override;
  std::string resolve_color(const std::string &) const override { return {}; }
  void split_text(const char *text,
                  const std::function<void(const char *)> &on_word,
                  const std::function<void(const char *)> &on_space) override;
};
lh_rect rect(const litehtml::position &p);
litehtml::position make_position(const lh_rect &p);
