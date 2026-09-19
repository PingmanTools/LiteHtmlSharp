#ifndef LH_API_H
#define LH_API_H
#include <stddef.h>
#include <stdint.h>
#if defined(_WIN32)
#ifdef LH_BUILD
#define LH_API __declspec(dllexport)
#else
#define LH_API __declspec(dllimport)
#endif
#define LH_CALL __cdecl
#else
#define LH_API __attribute__((visibility("default")))
#define LH_CALL
#endif
#if defined(_MSC_VER)
#define LH_ALIGN4 __declspec(align(4))
#else
#define LH_ALIGN4 __attribute__((aligned(4)))
#endif
#ifdef __cplusplus
extern "C" {
#endif
#define LH_ABI_VERSION 2u
typedef struct lh_document lh_document;
typedef uintptr_t lh_element;
typedef struct LH_ALIGN4 lh_color {
  uint8_t r, g, b, a;
} lh_color;
typedef struct lh_rect {
  float x, y, width, height;
} lh_rect;
typedef struct lh_radii {
  float top_left_x, top_left_y, top_right_x, top_right_y, bottom_right_x,
      bottom_right_y, bottom_left_x, bottom_left_y;
} lh_radii;
typedef struct lh_span {
  uint32_t offset, length;
} lh_span;
typedef struct lh_sink {
  void(LH_CALL *write)(void *ctx, const char *utf8, int32_t len);
  void *ctx;
} lh_sink;
typedef struct lh_attr {
  const char *name;
  const char *value;
} lh_attr;
typedef struct lh_font_desc {
  const char *family;
  const char *emphasis_style;
  float size;
  int32_t style, weight, decoration_line;
  float decoration_thickness;
  int32_t decoration_thickness_predefined, decoration_thickness_units,
      decoration_style;
  lh_color decoration_color, emphasis_color;
  int32_t emphasis_position;
#if UINTPTR_MAX == UINT64_MAX
  uint32_t padding;
#endif
} lh_font_desc;
typedef struct lh_font_metrics {
  float font_size, height, ascent, descent, x_height, ch_width, sub_shift,
      super_shift;
  uint8_t draw_spaces;
  uint8_t padding[3];
} lh_font_metrics;
typedef struct lh_media_features {
  int32_t type;
  float width, height, device_width, device_height;
  int32_t color, color_index, monochrome;
  float resolution;
} lh_media_features;
typedef struct lh_border {
  float width;
  int32_t style;
  lh_color color;
} lh_border;
typedef struct lh_borders {
  lh_border left, top, right, bottom;
  lh_radii radius;
} lh_borders;
typedef struct lh_layer {
  lh_rect border_box, clip_box, origin_box;
  lh_radii radius;
  int32_t attachment, repeat;
  uint8_t is_root;
  uint8_t padding[3];
  float opacity;
} lh_layer;
typedef struct lh_color_stop {
  float offset;
  lh_color color;
  float hint;
  uint8_t has_hint;
  uint8_t padding[3];
} lh_color_stop;
typedef struct lh_cmd_header {
  uint32_t type, size;
} lh_cmd_header;
typedef struct lh_cmd_solid_fill {
  lh_layer layer;
  lh_color color;
} lh_cmd_solid_fill;
typedef struct lh_cmd_linear_gradient {
  lh_layer layer;
  float x0, y0, x1, y1;
  lh_span stops;
  int32_t color_space, hue_interp;
} lh_cmd_linear_gradient;
typedef struct lh_cmd_radial_gradient {
  lh_layer layer;
  float cx, cy, rx, ry;
  lh_span stops;
  int32_t color_space, hue_interp;
} lh_cmd_radial_gradient;
typedef struct lh_cmd_conic_gradient {
  lh_layer layer;
  float cx, cy, angle, radius;
  lh_span stops;
  int32_t color_space, hue_interp;
} lh_cmd_conic_gradient;
typedef struct lh_cmd_image {
  lh_layer layer;
  lh_span url, base_url;
} lh_cmd_image;
typedef struct lh_cmd_text {
  lh_rect pos;
  lh_color color;
  /* Bit 31 marks decoration opacity in bits 0..7 (0..255); zero retains legacy opacity 1. */
  uint32_t padding;
  uintptr_t font;
  lh_span utf8;
} lh_cmd_text;
typedef struct lh_cmd_borders {
  lh_borders borders;
  lh_rect pos;
  uint8_t root;
  uint8_t padding[3];
} lh_cmd_borders;
typedef struct lh_cmd_list_marker {
  lh_rect pos;
  lh_color color;
  int32_t marker_type, index;
  uint32_t padding;
  uintptr_t font;
  lh_span image, baseurl;
} lh_cmd_list_marker;
typedef struct lh_cmd_set_clip {
  lh_rect pos;
  lh_radii radius;
} lh_cmd_set_clip;
/* Column-major 4x4 matrix, with draw offset already included. Nested pushes
   concatenate with the current transform; POP restores the preceding transform. */
typedef struct lh_cmd_push_transform {
  float m[16];
} lh_cmd_push_transform;
/* Composite enclosed drawing commands as one group, then apply opacity once. */
typedef struct lh_cmd_push_opacity { float opacity; } lh_cmd_push_opacity;
typedef struct lh_layout_entry {
  const char *structure;
  const char *field;
  uint32_t size, offset;
} lh_layout_entry;
/* Callbacks must not throw. Callbacks and sinks are synchronous. Hosts must not
   re-enter or destroy their document in a callback. UTF-8 pointers and sink
   contexts expire on callback return; user must outlive the document. */
typedef struct lh_host {
  void *user;
  uintptr_t(LH_CALL *create_font)(void *, const lh_font_desc *,
                                  lh_font_metrics *);
  void(LH_CALL *delete_font)(void *, uintptr_t);
  float(LH_CALL *text_width)(void *, const char *, uintptr_t);
  float(LH_CALL *pt_to_px)(void *, float);
  float(LH_CALL *default_font_size)(void *);
  void(LH_CALL *default_font_name)(void *, lh_sink *);
  void(LH_CALL *load_image)(void *, const char *, const char *, uint8_t);
  void(LH_CALL *get_image_size)(void *, const char *, const char *, float *,
                                float *);
  void(LH_CALL *set_caption)(void *, const char *);
  void(LH_CALL *set_base_url)(void *, const char *);
  void(LH_CALL *set_cursor)(void *, const char *);
  void(LH_CALL *on_anchor_click)(void *, const char *, lh_element);
  uint8_t(LH_CALL *on_element_click)(void *, lh_element);
  void(LH_CALL *on_mouse_event)(void *, lh_element, int32_t);
  void(LH_CALL *transform_text)(void *, const char *, int32_t, lh_sink *);
  void(LH_CALL *import_css)(void *, const char *, const char *, lh_sink *,
                            lh_sink *);
  void(LH_CALL *get_viewport)(void *, lh_rect *);
  void(LH_CALL *get_media_features)(void *, lh_media_features *);
  void(LH_CALL *get_language)(void *, lh_sink *, lh_sink *);
  uint8_t(LH_CALL *should_create_element)(void *, const char *);
  /* Positive IDs identify controls; nonpositive IDs decline replacement.
   * Duplicate IDs fail load. */
  int32_t(LH_CALL *create_element)(void *, const char *, const lh_attr *,
                                   int32_t, float *, float *);
  void(LH_CALL *request_redraw)(void *, const lh_rect *);
} lh_host;
enum lh_command_type {
  LH_CMD_SOLID_FILL = 1,
  LH_CMD_LINEAR_GRADIENT = 2,
  LH_CMD_RADIAL_GRADIENT = 3,
  LH_CMD_CONIC_GRADIENT = 4,
  LH_CMD_IMAGE = 5,
  LH_CMD_TEXT = 6,
  LH_CMD_BORDERS = 7,
  LH_CMD_LIST_MARKER = 8,
  LH_CMD_SET_CLIP = 9,
  LH_CMD_DEL_CLIP = 10,
  LH_CMD_PUSH_TRANSFORM = 11,
  LH_CMD_POP_TRANSFORM = 12,
  LH_CMD_PUSH_OPACITY = 13,
  LH_CMD_POP_OPACITY = 14
};
LH_API uint32_t LH_CALL lh_abi_version(void);
LH_API void LH_CALL lh_abi_layout(const lh_layout_entry **entries,
                                  int32_t *count);
LH_API lh_document *LH_CALL lh_document_create(const lh_host *host);
LH_API void LH_CALL lh_document_destroy(lh_document *doc);
/* HTML and CSS strings are UTF-8. HTML charset declarations do not override
 * this already-decoded input contract. */
LH_API uint8_t LH_CALL lh_document_load(lh_document *doc, const char *html,
                                        const char *master_css,
                                        const char *user_css);
LH_API float LH_CALL lh_document_render(lh_document *doc, float max_width);
LH_API void LH_CALL lh_document_draw(lh_document *doc, float x, float y,
                                     const lh_rect *clip);
/* The document owns these bytes until draw, successful load, or destroy.
   Records are padded to 8 bytes. Span lengths count bytes, including gradient
   stop arrays; strings have no trailing NUL. */
LH_API void LH_CALL lh_document_display_list(lh_document *doc,
                                             const uint8_t **stream,
                                             uint32_t *stream_len,
                                             const uint8_t **blob,
                                             uint32_t *blob_len);
LH_API float LH_CALL lh_document_width(lh_document *doc);
LH_API float LH_CALL lh_document_height(lh_document *doc);
LH_API uint8_t LH_CALL lh_document_on_mouse_over(lh_document *doc, float x,
                                                 float y, float client_x,
                                                 float client_y);
LH_API uint8_t LH_CALL lh_document_on_lbutton_down(lh_document *doc, float x,
                                                   float y, float client_x,
                                                   float client_y);
LH_API uint8_t LH_CALL lh_document_on_lbutton_up(lh_document *doc, float x,
                                                 float y, float client_x,
                                                 float client_y);
LH_API uint8_t LH_CALL lh_document_on_mouse_leave(lh_document *doc);
LH_API uint8_t LH_CALL lh_document_media_changed(lh_document *doc);
LH_API uint8_t LH_CALL lh_document_animations_active(lh_document *doc);
/* Scheduling hint in milliseconds at the last supplied document time. visible is
   in document coordinates. Negative means no wake-up needed until a viewport,
   style, or input change. Time continues while off-screen; render once on waking.
   cadence must be finite and positive. This function never advances time. */
LH_API double LH_CALL lh_document_next_animation_delay(lh_document *doc,
    const lh_rect *visible, double cadence);
/* Advance time and render. Returns next delay (-1 means asleep), NaN on failure.
   active includes off-screen motion that must be resampled after scrolling.
   Outputs are optional. Use this instead of separate time/render/schedule calls. */
LH_API double LH_CALL lh_document_render_frame(lh_document *doc, float max_width,
    double milliseconds, const lh_rect *visible, double cadence,
    float *rendered_width, uint8_t *active);
LH_API lh_element LH_CALL lh_document_element_at(lh_document *doc, float x,
                                                 float y);
LH_API lh_element LH_CALL lh_document_element_at_client(lh_document *doc,
                                                        float x, float y,
                                                        float client_x,
                                                        float client_y);
LH_API uint8_t LH_CALL lh_document_custom_element_box(lh_document *doc,
                                                      int32_t id, lh_rect *out);
LH_API void LH_CALL lh_document_set_time(lh_document *doc, double milliseconds);
/* Element handles are document-scoped and retained until destroy, including
 * across loads. */
LH_API lh_element LH_CALL lh_element_get_parent(lh_document *doc,
                                                lh_element el);
LH_API void LH_CALL lh_element_get_attr(lh_document *doc, lh_element el,
                                        const char *name, lh_sink *out);
LH_API void LH_CALL lh_element_get_tag(lh_document *doc, lh_element el,
                                       lh_sink *out);
#ifdef __cplusplus
}
#endif
#endif
