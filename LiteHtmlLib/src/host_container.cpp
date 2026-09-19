#include "host_container.h"
#include "host_element.h"
#include "litehtml/render_item.h"
#include <cstring>
using namespace litehtml;
namespace {
struct string_sink {
  std::string &value;
  std::exception_ptr error;
  explicit string_sink(std::string &s) : value(s) {}
  void check() {
    if (error)
      std::rethrow_exception(error);
  }
};
void LH_CALL sink_write(void *ctx, const char *s, int32_t n) noexcept {
  auto &target = *static_cast<string_sink *>(ctx);
  try {
    if (s && n > 0 && !target.error)
      target.value.append(s, static_cast<size_t>(n));
  } catch (...) {
    target.error = std::current_exception();
  }
}
lh_sink sink(string_sink &s) { return {sink_write, &s}; }
lh_color color(const web_color &c) { return {c.red, c.green, c.blue, c.alpha}; }
lh_radii radii(const border_radiuses &r) {
  return {r.top_left_x,     r.top_left_y,     r.top_right_x,   r.top_right_y,
          r.bottom_right_x, r.bottom_right_y, r.bottom_left_x, r.bottom_left_y};
}
lh_layer layer(const background_layer &b) {
  return {rect(b.border_box),
          rect(b.clip_box),
          rect(b.origin_box),
          radii(b.border_radius),
          b.attachment,
          b.repeat,
          static_cast<uint8_t>(b.is_root),
          {},
          b.opacity};
}
lh_border border_value(const border &b) {
  return {b.width, b.style, color(b.color)};
}
lh_span stops(display_list &list, const background_layer::gradient_base &g) {
  std::vector<lh_color_stop> values;
  for (auto &s : g.color_points)
    values.push_back({s.offset,
                      color(s.color),
                      s.hint.value_or(0),
                      static_cast<uint8_t>(s.hint.has_value()),
                      {}});
  return list.append(values.data(), values.size() * sizeof(lh_color_stop));
}
} // namespace
lh_rect rect(const litehtml::position &p) {
  return {p.x, p.y, p.width, p.height};
}
litehtml::position make_position(const lh_rect &p) {
  return {pixel_t(p.x), pixel_t(p.y), pixel_t(p.width), pixel_t(p.height)};
}
uint_ptr host_container::create_font(const font_description &f,
                                     const document *, font_metrics *out) {
  lh_font_desc d{};
  d.family = f.family.c_str();
  d.emphasis_style = f.emphasis_style.c_str();
  d.size = f.size;
  d.style = f.style;
  d.weight = f.weight;
  d.decoration_line = f.decoration_line;
  d.decoration_thickness = f.decoration_thickness.val();
  d.decoration_thickness_predefined = f.decoration_thickness.is_predefined()
                                          ? f.decoration_thickness.predef()
                                          : -1;
  d.decoration_thickness_units = f.decoration_thickness.units();
  d.decoration_style = f.decoration_style;
  d.decoration_color = color(f.decoration_color);
  d.emphasis_color = color(f.emphasis_color);
  d.emphasis_position = f.emphasis_position;
  lh_font_metrics m{d.size,
                    d.size,
                    d.size * .8f,
                    d.size * .2f,
                    d.size * .5f,
                    d.size * .5f,
                    0,
                    0,
                    1,
                    {}};
  auto font = host.create_font ? host.create_font(host.user, &d, &m) : 0;
  if (out) {
    out->font_size = pixel_t(m.font_size);
    out->height = pixel_t(m.height);
    out->ascent = pixel_t(m.ascent);
    out->descent = pixel_t(m.descent);
    out->x_height = pixel_t(m.x_height);
    out->ch_width = pixel_t(m.ch_width);
    out->sub_shift = pixel_t(m.sub_shift);
    out->super_shift = pixel_t(m.super_shift);
    out->draw_spaces = m.draw_spaces != 0;
  }
  return font;
}
void host_container::delete_font(uint_ptr f) {
  if (host.delete_font)
    host.delete_font(host.user, f);
}
pixel_t host_container::text_width(const char *t, uint_ptr f) {
  return pixel_t(host.text_width ? host.text_width(host.user, t, f)
                                 : float(strlen(t)) * 8);
}
void host_container::draw_text(uint_ptr, const char *t, uint_ptr f, web_color c,
                               const litehtml::position &p) {
  list.command(LH_CMD_TEXT, lh_cmd_text{rect(p), color(c), 0x80000000u | static_cast<uint32_t>(std::lround(std::clamp(paint_opacity(), 0.f, 1.f) * 255)), f, list.text(t)});
}
pixel_t host_container::pt_to_px(float p) const {
  return pixel_t(host.pt_to_px ? host.pt_to_px(host.user, p) : p * 96 / 72);
}
pixel_t host_container::get_default_font_size() const {
  return pixel_t(host.default_font_size ? host.default_font_size(host.user)
                                        : 16);
}
const char *host_container::get_default_font_name() const {
  if (host.default_font_name) {
    std::string result;
    string_sink buffer(result);
    auto s = sink(buffer);
    host.default_font_name(host.user, &s);
    buffer.check();
    font_name = std::move(result);
  }
  return font_name.c_str();
}
void host_container::draw_list_marker(uint_ptr, const list_marker &m) {
  list.command(LH_CMD_LIST_MARKER,
               lh_cmd_list_marker{rect(m.pos), color(m.color), m.marker_type,
                                  m.index, 0, m.font, list.text(m.image),
                                  list.text(m.baseurl ? m.baseurl : "")});
}
void host_container::load_image(const char *s, const char *b, bool r) {
  if (host.load_image)
    host.load_image(host.user, s, b ? b : "", r);
}
void host_container::get_image_size(const char *s, const char *b,
                                    litehtml::size &out) {
  float w = 0, h = 0;
  if (host.get_image_size)
    host.get_image_size(host.user, s, b ? b : "", &w, &h);
  out.width = pixel_t(w);
  out.height = pixel_t(h);
}
void host_container::draw_image(uint_ptr, const background_layer &b,
                                const std::string &u, const std::string &base) {
  list.command(LH_CMD_IMAGE,
               lh_cmd_image{layer(b), list.text(u), list.text(base)});
}
void host_container::draw_solid_fill(uint_ptr, const background_layer &b,
                                     const web_color &c) {
  list.command(LH_CMD_SOLID_FILL, lh_cmd_solid_fill{layer(b), color(c)});
}
void host_container::draw_linear_gradient(
    uint_ptr, const background_layer &b,
    const background_layer::linear_gradient &g) {
  list.command(LH_CMD_LINEAR_GRADIENT,
               lh_cmd_linear_gradient{layer(b), g.start.x, g.start.y, g.end.x,
                                      g.end.y, stops(list, g), g.color_space,
                                      g.hue_interpolation});
}
void host_container::draw_radial_gradient(
    uint_ptr, const background_layer &b,
    const background_layer::radial_gradient &g) {
  list.command(LH_CMD_RADIAL_GRADIENT,
               lh_cmd_radial_gradient{layer(b), g.position.x, g.position.y,
                                      g.radius.x, g.radius.y, stops(list, g),
                                      g.color_space, g.hue_interpolation});
}
void host_container::draw_conic_gradient(
    uint_ptr, const background_layer &b,
    const background_layer::conic_gradient &g) {
  list.command(LH_CMD_CONIC_GRADIENT,
               lh_cmd_conic_gradient{layer(b), g.position.x, g.position.y,
                                     g.angle, g.radius, stops(list, g),
                                     g.color_space, g.hue_interpolation});
}
void host_container::draw_borders(uint_ptr, const borders &b,
                                  const litehtml::position &p, bool root) {
  list.command(LH_CMD_BORDERS,
               lh_cmd_borders{{border_value(b.left), border_value(b.top),
                               border_value(b.right), border_value(b.bottom),
                               radii(b.radius)},
                              rect(p),
                              static_cast<uint8_t>(root),
                              {}});
}
void host_container::set_caption(const char *s) {
  if (host.set_caption)
    host.set_caption(host.user, s);
}
void host_container::set_base_url(const char *s) {
  if (host.set_base_url)
    host.set_base_url(host.user, s);
}
void host_container::set_cursor(const char *s) {
  if (host.set_cursor)
    host.set_cursor(host.user, s);
}
void host_container::link(const std::shared_ptr<document> &,
                          const element::ptr &) {}
void host_container::on_anchor_click(const char *s, const element::ptr &e) {
  if (host.on_anchor_click)
    host.on_anchor_click(host.user, s, retain(e));
}
bool host_container::on_element_click(const element::ptr &e) {
  return host.on_element_click && host.on_element_click(host.user, retain(e));
}
void host_container::on_mouse_event(const element::ptr &e, mouse_event k) {
  if (host.on_mouse_event)
    host.on_mouse_event(host.user, retain(e), k);
}
void host_container::transform_text(std::string &t, text_transform tt) {
  if (host.transform_text) {
    std::string result;
    string_sink buffer(result);
    auto s = sink(buffer);
    host.transform_text(host.user, t.c_str(), tt, &s);
    buffer.check();
    t = std::move(result);
  }
}
void host_container::import_css(std::string &t, const std::string &url,
                                std::string &base) {
  if (host.import_css) {
    std::string newbase;
    string_sink tb(t), bb(newbase);
    auto ts = sink(tb), bs = sink(bb);
    host.import_css(host.user, url.c_str(), base.c_str(), &ts, &bs);
    tb.check();
    bb.check();
    base = std::move(newbase);
  }
}
void host_container::set_clip(const litehtml::position &p,
                              const border_radiuses &r) {
  list.command(LH_CMD_SET_CLIP, lh_cmd_set_clip{rect(p), radii(r)});
}
void host_container::del_clip() { list.command(LH_CMD_DEL_CLIP, nullptr, 0); }
void host_container::get_viewport(litehtml::position &out) const {
  lh_rect r{0, 0, 800, 600};
  if (host.get_viewport)
    host.get_viewport(host.user, &r);
  out = make_position(r);
}
element::ptr
host_container::create_element(const char *tag, const string_map &attrs,
                               const std::shared_ptr<document> &doc) {
  if (!host.should_create_element || !host.create_element ||
      !host.should_create_element(host.user, tag))
    return {};
  std::vector<lh_attr> pairs;
  for (auto &a : attrs)
    pairs.push_back({a.first.c_str(), a.second.c_str()});
  float w = 0, h = 0;
  int32_t id = host.create_element(host.user, tag, pairs.data(),
                                   static_cast<int32_t>(pairs.size()), &w, &h);
  if (id <= 0)
    return {};
  if (custom.count(id))
    throw std::invalid_argument("Duplicate custom element ID");
  auto el = std::make_shared<host_element>(doc, w, h);
  custom[id] = el;
  return el;
}
void host_container::get_media_features(media_features &out) const {
  lh_rect v{};
  if (host.get_viewport)
    host.get_viewport(host.user, &v);
  lh_media_features m{
      media_type_screen, v.width, v.height, v.width, v.height, 8, 0, 0, 96};
  if (host.get_media_features)
    host.get_media_features(host.user, &m);
  out.type = static_cast<media_type>(m.type);
  out.width = pixel_t(m.width);
  out.height = pixel_t(m.height);
  out.device_width = pixel_t(m.device_width);
  out.device_height = pixel_t(m.device_height);
  out.color = m.color;
  out.color_index = m.color_index;
  out.monochrome = m.monochrome;
  out.resolution = pixel_t(m.resolution);
}
void host_container::get_language(std::string &lang,
                                  std::string &culture) const {
  if (host.get_language) {
    string_sink lb(lang), cb(culture);
    auto l = sink(lb), c = sink(cb);
    host.get_language(host.user, &l, &c);
    lb.check();
    cb.check();
  }
}
void host_container::split_text(
    const char *t, const std::function<void(const char *)> &word,
    const std::function<void(const char *)> &space) {
  document_container::split_text(t, word, space);
}
void host_container::redraw(const litehtml::position &p) {
  if (host.request_redraw) {
    auto r = rect(p);
    host.request_redraw(host.user, &r);
  }
}

void host_container::push_transform(const litehtml::motion_matrix &matrix) {
  lh_cmd_push_transform command{};
  std::copy(matrix.values.begin(), matrix.values.end(), command.m);
  list.command(LH_CMD_PUSH_TRANSFORM, command);
}
void host_container::pop_transform() { list.command(LH_CMD_POP_TRANSFORM, nullptr, 0); }
