#include "host_container.h"
#include "host_element.h"
#include "litehtml/render_item.h"
#include <algorithm>
#include <cstring>
#include <limits>
struct lh_document {
  host_container container;
  litehtml::document::ptr doc;
  explicit lh_document(const lh_host &h) : container(h) {}
  ~lh_document() {
    container.elements.clear();
    doc.reset();
  }
};
namespace {
void emit(lh_sink *sink, const char *s) {
  if (sink && sink->write && s)
    sink->write(sink->ctx, s, static_cast<int32_t>(strlen(s)));
}
} // namespace
extern "C" {
uint32_t LH_CALL lh_abi_version(void) { return LH_ABI_VERSION; }
lh_document *LH_CALL lh_document_create(const lh_host *host) {
  try {
    return host ? new lh_document(*host) : nullptr;
  } catch (...) {
    return nullptr;
  }
}
void LH_CALL lh_document_destroy(lh_document *doc) {
  try {
    delete doc;
  } catch (...) {
  }
}
uint8_t LH_CALL lh_document_load(lh_document *d, const char *html,
                                 const char *master, const char *user) {
  if (!d || !html)
    return 0;
  auto old_custom = std::move(d->container.custom);
  try {
    auto result = litehtml::document::createFromString(
        litehtml::estring(html, litehtml::encoding::utf_8, litehtml::confidence::certain),
        &d->container, master ? master : litehtml::master_css,
        user ? user : "");
    if (!result) {
      d->container.custom = std::move(old_custom);
      return 0;
    }
    d->doc = std::move(result);
    d->container.list.clear();
    return 1;
  } catch (...) {
    d->container.custom = std::move(old_custom);
    return 0;
  }
}
float LH_CALL lh_document_render(lh_document *d, float width) {
  try {
    return d && d->doc ? d->doc->render(litehtml::pixel_t(width)).value()
                       : std::numeric_limits<float>::quiet_NaN();
  } catch (...) {
    return std::numeric_limits<float>::quiet_NaN();
  }
}
void LH_CALL lh_document_draw(lh_document *d, float x, float y,
                              const lh_rect *clip) {
  try {
    if (!d)
      return;
    d->container.list.clear();
    if (!d->doc)
      return;
    litehtml::position p;
    if (clip)
      p = make_position(*clip);
    d->doc->draw(0, litehtml::pixel_t(x), litehtml::pixel_t(y),
                 clip ? &p : nullptr);
  } catch (...) {
    if (d)
      d->container.list.clear();
  }
}
void LH_CALL lh_document_display_list(lh_document *d, const uint8_t **stream,
                                      uint32_t *sl, const uint8_t **blob,
                                      uint32_t *bl) {
  if (stream)
    *stream = d ? d->container.list.stream.data() : nullptr;
  if (sl)
    *sl = d ? static_cast<uint32_t>(d->container.list.stream.size()) : 0;
  if (blob)
    *blob = d ? d->container.list.blob.data() : nullptr;
  if (bl)
    *bl = d ? static_cast<uint32_t>(d->container.list.blob.size()) : 0;
}
float LH_CALL lh_document_width(lh_document *d) {
  try {
    return d && d->doc ? d->doc->width().value() : 0;
  } catch (...) {
    return 0;
  }
}
float LH_CALL lh_document_height(lh_document *d) {
  try {
    return d && d->doc ? d->doc->height().value() : 0;
  } catch (...) {
    return 0;
  }
}
#define LH_MOUSE(name)                                                         \
  uint8_t LH_CALL lh_document_##name(lh_document *d, float x, float y,         \
                                     float cx, float cy) {                     \
    try {                                                                      \
      return d && d->doc                                                       \
                 ? d->doc->name(litehtml::pixel_t(x), litehtml::pixel_t(y),    \
                                litehtml::pixel_t(cx), litehtml::pixel_t(cy),  \
                                [d](const litehtml::position &p) {             \
                                  d->container.redraw(p);                      \
                                })                                             \
                 : 0;                                                          \
    } catch (...) {                                                            \
      return 0;                                                                \
    }                                                                          \
  }
LH_MOUSE(on_mouse_over)
LH_MOUSE(on_lbutton_down)
LH_MOUSE(on_lbutton_up)
#undef LH_MOUSE
uint8_t LH_CALL lh_document_on_mouse_leave(lh_document *d) {
  try {
    return d && d->doc
               ? d->doc->on_mouse_leave([d](const litehtml::position &p) {
                   d->container.redraw(p);
                 })
               : 0;
  } catch (...) {
    return 0;
  }
}
uint8_t LH_CALL lh_document_media_changed(lh_document *d) {
  try {
    return d && d->doc ? d->doc->media_changed() : 0;
  } catch (...) {
    return 0;
  }
}
lh_element LH_CALL lh_document_element_at(lh_document *d, float x, float y) {
  return lh_document_element_at_client(d, x, y, x, y);
}
lh_element LH_CALL lh_document_element_at_client(lh_document *d, float x,
                                                 float y, float client_x,
                                                 float client_y) {
  try {
    if (!d || !d->doc || !d->doc->root_render())
      return 0;
    return d->container.retain(d->doc->root_render()->get_element_by_point(
        litehtml::pixel_t(x), litehtml::pixel_t(y), litehtml::pixel_t(client_x),
        litehtml::pixel_t(client_y), [](const auto &) { return true; }));
  } catch (...) {
    return 0;
  }
}
uint8_t LH_CALL lh_document_custom_element_box(lh_document *d, int32_t id,
                                               lh_rect *out) {
  try {
    if (!d || !out)
      return 0;
    auto i = d->container.custom.find(id);
    if (i == d->container.custom.end())
      return 0;
    auto el = i->second.lock();
    if (!el)
      return 0;
    bool found = false;
    // Live render records can belong to an obsolete layout tree.
    el->run_on_renderers([&](const std::shared_ptr<litehtml::render_item>& item) {
      if (!item->is_visible())
        return true;
      auto root = item;
      while (auto parent = root->parent())
        root = parent;
      if (root != d->doc->root_render())
        return true;
      *out = rect(item->get_placement());
      found = true;
      return false;
    });
    return found ? 1 : 0;
  } catch (...) {
    return 0;
  }
}
void LH_CALL lh_document_set_time(lh_document *d, double milliseconds) {
  try {
    if (d && d->doc)
      d->doc->set_time(milliseconds);
  } catch (...) {}
}
double LH_CALL lh_document_next_animation_delay(lh_document *d, const lh_rect *visible, double cadence) {
  try {
    if (!d || !d->doc || !visible) return -1;
    return d->doc->next_animation_delay(
        litehtml::position(visible->x, visible->y, visible->width, visible->height), cadence);
  } catch (...) { return -1; }
}
double LH_CALL lh_document_render_frame(lh_document *d, float width, double milliseconds,
    const lh_rect *visible, double cadence, float *rendered_width, uint8_t *active) {
  if (rendered_width) *rendered_width = 0;
  if (active) *active = 0;
  try {
    if (d && d->doc && visible) {
      const auto frame = d->doc->render_frame(milliseconds, litehtml::pixel_t(width),
          make_position(*visible), cadence);
      if (rendered_width) *rendered_width = frame.width.value();
      if (active) *active = frame.active;
      return frame.next_delay;
    }
  } catch (...) {}
  return std::numeric_limits<double>::quiet_NaN();
}
uint8_t LH_CALL lh_document_animations_active(lh_document *d) {
  try {
    return d && d->doc && d->doc->animations_active();
  } catch (...) { return 0; }
}
lh_element LH_CALL lh_element_get_parent(lh_document *d, lh_element el) {
  try {
    if (!d)
      return 0;
    auto i = d->container.elements.find(el);
    return i == d->container.elements.end()
               ? 0
               : d->container.retain(i->second->parent());
  } catch (...) {
    return 0;
  }
}
void LH_CALL lh_element_get_attr(lh_document *d, lh_element el,
                                 const char *name, lh_sink *out) {
  try {
    if (!d || !name)
      return;
    auto i = d->container.elements.find(el);
    if (i != d->container.elements.end())
      emit(out, i->second->get_attr(name, ""));
  } catch (...) {
  }
}
void LH_CALL lh_element_get_tag(lh_document *d, lh_element el, lh_sink *out) {
  try {
    if (!d)
      return;
    auto i = d->container.elements.find(el);
    if (i != d->container.elements.end())
      emit(out, i->second->get_tagName());
  } catch (...) {
  }
}
}
#include "abi_layout.inc"
