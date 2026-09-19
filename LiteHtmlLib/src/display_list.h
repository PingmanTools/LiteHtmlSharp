#pragma once
#include "lh_api.h"
#include <cstring>
#include <stdexcept>
#include <string>
#include <vector>
#include <algorithm>
#include <optional>
class display_list {
  struct paint_state {
    double a = 1, b = 0, c = 0, d = 1, x = 0, y = 0;
    bool affine = true;
    std::optional<lh_rect> clip;
    lh_rect project(lh_rect r) const {
      const double xs[] = {r.x, r.x + r.width, r.x, r.x + r.width};
      const double ys[] = {r.y, r.y, r.y + r.height, r.y + r.height};
      double left = a * xs[0] + c * ys[0] + x, right = left;
      double top = b * xs[0] + d * ys[0] + y, bottom = top;
      for (int i = 1; i < 4; ++i) {
        const auto px = a * xs[i] + c * ys[i] + x, py = b * xs[i] + d * ys[i] + y;
        left = std::min(left, px); right = std::max(right, px);
        top = std::min(top, py); bottom = std::max(bottom, py);
      }
      return {float(left), float(top), float(right-left), float(bottom-top)};
    }
  } state;
  struct scope { size_t start, blob_start; uint64_t paints; paint_state before; };
  std::vector<scope> scopes;
  bool outside(uint32_t type, const void* payload) const {
    if (!state.affine || !state.clip) return false;
    lh_rect box;
    switch (type) {
      case LH_CMD_SOLID_FILL: box = static_cast<const lh_cmd_solid_fill*>(payload)->layer.clip_box; break;
      case LH_CMD_LINEAR_GRADIENT: box = static_cast<const lh_cmd_linear_gradient*>(payload)->layer.clip_box; break;
      case LH_CMD_RADIAL_GRADIENT: box = static_cast<const lh_cmd_radial_gradient*>(payload)->layer.clip_box; break;
      case LH_CMD_CONIC_GRADIENT: box = static_cast<const lh_cmd_conic_gradient*>(payload)->layer.clip_box; break;
      case LH_CMD_IMAGE: box = static_cast<const lh_cmd_image*>(payload)->layer.clip_box; break;
      case LH_CMD_TEXT: box = static_cast<const lh_cmd_text*>(payload)->pos; break;
      case LH_CMD_BORDERS: box = static_cast<const lh_cmd_borders*>(payload)->pos; break;
      case LH_CMD_LIST_MARKER: box = static_cast<const lh_cmd_list_marker*>(payload)->pos; break;
      default: return false;
    }
    box = state.project(box);
    const auto& clip = *state.clip;
    return clip.width <= 0 || clip.height <= 0 || box.x + box.width <= clip.x ||
        box.x >= clip.x + clip.width || box.y + box.height <= clip.y || box.y >= clip.y + clip.height;
  }
  uint64_t paints = 0;
public:
  std::vector<uint8_t> stream, blob;
  void clear() {
    stream.clear();
    blob.clear();
    scopes.clear();
    paints = 0;
    state = {};
  }
  lh_span append(const void *data, size_t size) {
    if (size > UINT32_MAX || blob.size() > UINT32_MAX - size)
      throw std::length_error("display blob");
    lh_span s{static_cast<uint32_t>(blob.size()), static_cast<uint32_t>(size)};
    if (size)
      blob.insert(blob.end(), static_cast<const uint8_t *>(data),
                  static_cast<const uint8_t *>(data) + size);
    return s;
  }
  lh_span text(const std::string &s) { return append(s.data(), s.size()); }
  void command(uint32_t type, const void *payload, size_t size) {
    if (type == LH_CMD_POP_TRANSFORM || type == LH_CMD_DEL_CLIP || type == LH_CMD_POP_OPACITY) {
      if (!scopes.empty()) {
        const auto closed = scopes.back();
        scopes.pop_back();
        state = closed.before;
        if (closed.paints == paints) {
          stream.resize(closed.start);
          blob.resize(closed.blob_start);
          return;
        }
      }
    } else if (type == LH_CMD_PUSH_TRANSFORM || type == LH_CMD_SET_CLIP || type == LH_CMD_PUSH_OPACITY) {
      scopes.push_back({stream.size(), blob.size(), paints, state});
      if (type == LH_CMD_PUSH_TRANSFORM) {
        const auto* m = static_cast<const lh_cmd_push_transform*>(payload)->m;
        const auto old = state;
        state.affine = state.affine && m[2] == 0 && m[3] == 0 && m[6] == 0 && m[7] == 0 &&
            m[8] == 0 && m[9] == 0 && m[10] == 1 && m[11] == 0 && m[14] == 0 && m[15] == 1;
        state.a = old.a*m[0] + old.c*m[1]; state.b = old.b*m[0] + old.d*m[1];
        state.c = old.a*m[4] + old.c*m[5]; state.d = old.b*m[4] + old.d*m[5];
        state.x = old.a*m[12] + old.c*m[13] + old.x;
        state.y = old.b*m[12] + old.d*m[13] + old.y;
      } else if (type == LH_CMD_SET_CLIP && state.affine) {
        auto clip = state.project(static_cast<const lh_cmd_set_clip*>(payload)->pos);
        if (state.clip) {
          const auto left = std::max(clip.x, state.clip->x), top = std::max(clip.y, state.clip->y);
          const auto right = std::min(clip.x+clip.width, state.clip->x+state.clip->width);
          const auto bottom = std::min(clip.y+clip.height, state.clip->y+state.clip->height);
          clip = {left, top, std::max(0.f, right-left), std::max(0.f, bottom-top)};
        }
        state.clip = clip;
      }
    } else {
      if (outside(type, payload)) return;
      ++paints;
    }
    size_t length = (sizeof(lh_cmd_header) + size + 7) & ~size_t(7);
    if (stream.size() > UINT32_MAX - length)
      throw std::length_error("display stream");
    auto start = stream.size();
    stream.resize(start + length, 0);
    lh_cmd_header h{type, static_cast<uint32_t>(length)};
    memcpy(stream.data() + start, &h, sizeof h);
    if (size)
      memcpy(stream.data() + start + sizeof h, payload, size);
  }
  template <class T> void command(uint32_t type, const T &value) {
    command(type, &value, sizeof value);
  }
};
