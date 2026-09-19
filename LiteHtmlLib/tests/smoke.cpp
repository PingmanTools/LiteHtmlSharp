#include "lh_api.h"
#include <cassert>
#include <cmath>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <stdexcept>
#include <string>
static int creates, deletes;
static uintptr_t LH_CALL font(void *, const lh_font_desc *,
                              lh_font_metrics *m) {
  ++creates;
  *m = {16, 16, 12, 4, 8, 10, 0, 0, 1, {}};
  return 42;
}
static void LH_CALL delete_font(void *, uintptr_t f) {
  assert(f == 42);
  ++deletes;
}
static float LH_CALL width(void *, const char *s, uintptr_t) {
  return float(strlen(s)) * 10;
}
static uint8_t LH_CALL custom(void *, const char *s) {
  return strcmp(s, "input") == 0 || strcmp(s, "button") == 0;
}
static int next_id = 1;
static bool duplicate;
static void LH_CALL collect(void *ctx, const char *s, int32_t n) {
  static_cast<std::string *>(ctx)->append(s, n);
}
static float LH_CALL fail_width(void *, const char *, uintptr_t) {
  throw std::runtime_error("font backend failed");
}
static int32_t LH_CALL create(void *, const char *, const lh_attr *, int32_t,
                              float *w, float *h) {
  *w = 80;
  *h = 24;
  return duplicate ? 1 : next_id++;
}
static float LH_CALL fractional_width(void *, const char *text, uintptr_t) {
  return strcmp(text, " ") == 0 ? 3.375f : float(strlen(text)) * 7.25f;
}
static void fractional_text_positions() {
  lh_host host{};
  host.create_font = font;
  host.delete_font = delete_font;
  host.text_width = fractional_width;
  auto doc = lh_document_create(&host);
  assert(doc);
  assert(lh_document_load(
      doc, "<style>html,body,p{margin:0;padding:0}</style><p>aa bb</p>",
      nullptr, nullptr));
  lh_document_render(doc, 300);
  lh_rect clip{0, 0, 300, 100};
  lh_document_draw(doc, .125f, .375f, &clip);
  const uint8_t *stream, *blob;
  uint32_t length, blob_length;
  lh_document_display_list(doc, &stream, &length, &blob, &blob_length);
  const char *expected_text[] = {"aa", " ", "bb"};
  const float expected_width[] = {14.5f, 3.375f, 14.5f};
  float next_x = .125f;
  int count = 0;
  for (uint32_t offset = 0; offset < length;) {
    lh_cmd_header header;
    memcpy(&header, stream + offset, sizeof header);
    if (header.type == LH_CMD_TEXT) {
      lh_cmd_text text;
      memcpy(&text, stream + offset + sizeof header, sizeof text);
      assert(count < 3);
      assert(text.utf8.length == strlen(expected_text[count]));
      assert(memcmp(blob + text.utf8.offset, expected_text[count],
                    text.utf8.length) == 0);
      if (std::abs(text.pos.x - next_x) >= .0001f) {
        fprintf(stderr, "Fractional text x expected %.3f, got %.3f\n", next_x,
                text.pos.x);
        std::exit(1);
      }
      assert(std::abs(text.pos.width - expected_width[count]) < .0001f);
      next_x = text.pos.x + text.pos.width;
      ++count;
    }
    offset += header.size;
  }
  assert(count == 3);
  assert(std::abs(next_x - 32.5f) < .0001f);
  lh_document_destroy(doc);
  assert(creates == deletes);
  puts("FRACTIONAL adjacent text/space positions and widths remain contiguous");
}
static void decoration_opacity() {
  lh_host host{};
  host.create_font = font;
  host.delete_font = delete_font;
  host.text_width = width;
  auto doc = lh_document_create(&host);
  assert(doc && lh_document_load(doc, "<div style='opacity:.5;text-decoration:underline red'>fade</div>", nullptr, nullptr));
  lh_document_render(doc, 200);
  lh_rect clip{0, 0, 200, 100};
  lh_document_draw(doc, 0, 0, &clip);
  const uint8_t *stream, *blob;
  uint32_t length, blob_length;
  lh_document_display_list(doc, &stream, &length, &blob, &blob_length);
  bool found = false;
  int opacity_depth = 0, groups = 0;
  for (uint32_t offset = 0; offset < length;) {
    lh_cmd_header header;
    memcpy(&header, stream + offset, sizeof header);
    assert(header.size >= 8 && offset + header.size <= length);
    if (header.type == LH_CMD_PUSH_OPACITY) {
      lh_cmd_push_opacity group;
      memcpy(&group, stream + offset + 8, sizeof group);
      assert(group.opacity == .5f);
      ++opacity_depth;
      ++groups;
    } else if (header.type == LH_CMD_POP_OPACITY) {
      assert(opacity_depth == 1);
      --opacity_depth;
    }
    if (header.type == LH_CMD_TEXT) {
      lh_cmd_text text;
      memcpy(&text, stream + offset + 8, sizeof text);
      assert(opacity_depth == 1 && text.color.a == 255 && text.padding == (0x80000000u | 255u));
      found = true;
    }
    offset += header.size;
  }
  assert(found && groups == 1 && opacity_depth == 0);
  lh_document_destroy(doc);
}
int main() {
  decoration_opacity();
  lh_host host{};
  host.create_font = font;
  host.delete_font = delete_font;
  host.text_width = width;
  host.should_create_element = custom;
  host.create_element = create;
  auto d = lh_document_create(&host);
  assert(d);
  assert(lh_document_load(
      d,
      "<div style=\"width:100px;height:50px;background:red\"><p>hi</p></div>",
      nullptr, nullptr));
  lh_document_render(d, 300);
  lh_rect clip{0, 0, 300, 300};
  lh_document_draw(d, 0, 0, &clip);
  const uint8_t *stream, *blob;
  uint32_t length, bl;
  lh_document_display_list(d, &stream, &length, &blob, &bl);
  bool fill = false, text = false;
  for (uint32_t offset = 0; offset < length;) {
    lh_cmd_header h;
    memcpy(&h, stream + offset, sizeof h);
    assert(h.size >= 8 && h.size % 8 == 0 && offset + h.size <= length);
    printf("command %u size %u\n", h.type, h.size);
    if (h.type == LH_CMD_SOLID_FILL) {
      lh_cmd_solid_fill c;
      memcpy(&c, stream + offset + 8, sizeof c);
      if (c.color.r == 255 && c.color.g == 0 &&
          c.layer.border_box.width == 100 && c.layer.border_box.height == 50) {
        fill = true;
        puts("SOLID_FILL red 100x50");
      }
    }
    if (h.type == LH_CMD_TEXT) {
      lh_cmd_text c;
      memcpy(&c, stream + offset + 8, sizeof c);
      assert(c.font == 42);
      if (c.utf8.length == 2 && !memcmp(blob + c.utf8.offset, "hi", 2)) {
        text = true;
        puts("TEXT hi font=42");
      }
    }
    offset += h.size;
  }
  assert(fill && text);
  auto element = lh_document_element_at(d, 10, 20);
  assert(element);
  std::string tag;
  lh_sink output{collect, &tag};
  lh_element_get_tag(d, element, &output);
  assert(!tag.empty());
  auto original_tag = tag;
  auto parent = lh_element_get_parent(d, element);
  assert(parent);
  tag.clear();
  lh_element_get_tag(d, parent, &output);
  assert(!tag.empty());
  auto parent_tag = tag;
  auto other = lh_document_create(&host);
  tag.clear();
  assert(lh_element_get_parent(other, element) == 0);
  lh_element_get_tag(other, element, &output);
  assert(tag.empty());
  lh_document_destroy(other);
  lh_element_get_tag(d, UINTPTR_MAX, &output);
  assert(tag.empty());

  assert(lh_document_load(d, "<input><button>OK</button>", nullptr, nullptr));
  lh_document_render(d, 300);
  lh_rect box{};
  assert(lh_document_custom_element_box(d, 1, &box));
  assert(box.width == 80 && box.height == 24);
  assert(lh_document_custom_element_box(d, 2, &box));
  assert(box.width == 80 && box.height == 24);
  puts("CUSTOM input/button 80x24");
  assert(deletes > 0);
  tag.clear();
  lh_element_get_tag(d, element, &output);
  assert(tag == original_tag);
  tag.clear();
  lh_element_get_tag(d, parent, &output);
  assert(tag == parent_tag);
  duplicate = true;
  assert(!lh_document_load(d, "<input><input>", nullptr, nullptr));
  duplicate = false;
  assert(lh_document_custom_element_box(d, 2, &box));
  assert(box.width == 80);
  assert(lh_document_load(d, "<div>replacement</div>", nullptr, nullptr));
  assert(!lh_document_custom_element_box(d, 1, &box));
  lh_document_display_list(d, &stream, &length, &blob, &bl);
  assert(length == 0 && bl == 0);
  assert(lh_document_load(
      d,
      "<style>html,body{margin:0;padding:0}</style><div title='fixed' "
      "style='position:fixed;left:0;top:0;width:100px;height:20px;z-index:10;"
      "background:red'></div><div title='normal' "
      "style='height:300px;background:blue'></div>",
      nullptr, nullptr));
  lh_document_render(d, 300);
  auto fixed_element = lh_document_element_at_client(d, 5, 105, 5, 5);
  tag.clear();
  lh_element_get_attr(d, fixed_element, "title", &output);
  assert(tag == "fixed");
  auto normal_element = lh_document_element_at(d, 5, 105);
  tag.clear();
  lh_element_get_attr(d, normal_element, "title", &output);
  assert(tag == "normal");
  puts("FIXED hit testing separates document and client coordinates");
  lh_document_destroy(d);
  assert(creates == deletes);
  // Timer-driven hosts repeatedly replace the document behind one ABI handle.
  auto reloader = lh_document_create(&host);
  for (int i = 0; i < 250; ++i) {
    assert(lh_document_load(reloader,
        "<style>@keyframes fade{to{opacity:.2}}p{animation:fade 1s infinite}</style><p>replacement</p>",
        nullptr, nullptr));
    lh_document_set_time(reloader, i * 16);
    lh_document_render(reloader, 200 + i % 2);
    lh_rect viewport{0, 0, 200, 100};
    lh_document_draw(reloader, 0, -float(i), &viewport);
    duplicate = true;
    assert(!lh_document_load(reloader, "<input><input>", nullptr, nullptr));
    duplicate = false;
    // Rollback must leave the prior animated document usable.
    lh_document_render(reloader, 200);
  }
  lh_document_destroy(reloader);
  assert(creates == deletes);
  printf("FONT ownership create=%d delete=%d\n", creates, deletes);
  fractional_text_positions();
  host.text_width = fail_width;
  auto failing = lh_document_create(&host);
  if (lh_document_load(failing, "<p>error</p>", nullptr, nullptr))
    assert(std::isnan(lh_document_render(failing, 100)));
  lh_document_destroy(failing);
  puts("RELOAD handles, rollback, font disposal, and exception containment "
       "passed");
  const lh_layout_entry *entries;
  int32_t count;
  lh_abi_layout(&entries, &count);
  assert(count > 150);
  printf("ABI entries=%d\n", count);
}
