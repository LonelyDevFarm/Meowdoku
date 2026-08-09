# Báo cáo đặc tả kỹ thuật: GEM-R9-010 (Board & Cell Visual Source Spec)

**Nguồn đối chiếu:** `D:\Projects\_GameExtract\Main_Meokdoku`
**Mục tiêu:** Đặc tả nguồn cho phần R7 còn lại của Board/Cell: nền board, border/outline vùng, corner mask, hard-state, grid overlay, palette, animation mặc định và lifecycle pool.

---

## 1. Nền Board (Board Background)
Dữ kiện từ `scripts/module/gameplay/view/board_view.gd`:
- Mặc định khởi tạo layout lưới: `CELL_PX = 100`, `BOARD_PADDING = 15`, `CELL_GAP = 4`. Tổng slot size cho một ô là `SLOT_PX = 108` (Tính theo `100 + 4*2`).
- Vẽ qua hàm `_draw()`, sử dụng hàm API `draw_style_box(_board_bg_style, Rect2(...))`.
- Bán kính bo góc: Lấy từ `_grid_bg_corner` (chịu ảnh hưởng bởi scale của board `s`), mặc định gọi `BOARD_BG_CORNER_RADIUS`.

## 2. Border / Outline Vùng / Grid Overlay
Được quản lý hoàn toàn độc lập bởi một custom node `BoardGridOverlay` (thêm vào qua hàm `_ensure_grid_overlay()`), vẽ chồng lên trên tất cả Cell.
Dữ kiện hằng số từ `scripts/module/gameplay/view/board_grid_overlay.gd` (Dòng 20 - 37):
- **Lưới phân cách từng ô (Thin):** Bề dày `GRID_THIN_WIDTH = 3.0`.
- **Outline phân vùng (Thick edge):** Bề dày `GRID_THICK_WIDTH = 5.0`.
- **Viền bao quanh toàn bộ Board (Border):** Bề dày `GRID_BORDER_WIDTH = 7.0`.
- **Bán kính bo góc Border:** `GRID_FRAME_CORNER_RADIUS = 30`.
- **Màu sắc lưới:** Cả mỏng và dày đều dùng chung một mã màu xám-nâu: `GRID_THIN_COLOR = Color(0.4196, 0.2235, 0.2235)` và `GRID_THICK_COLOR = Color(0.4196, 0.2235, 0.2235)`.
- **Hiệu ứng vẽ dần (Grow Effect):** Sử dụng các hằng số thời gian `GRID_THIN_DUR = 0.75s`, `GRID_FRAME_DUR = 0.75s` với `GRID_THIN_START = 0.1s`.

## 3. Corner Mask từng Cell (Bo góc độc lập)
Dữ kiện từ `board_view.gd:512` (`corner_round_mask_for`) và `cell_view.gd:222` (`_apply_corner_radius`):
- Khi A/B Test kích hoạt `is_single_line()`, Grid sẽ xác định 4 ô nằm ở 4 góc của toàn bộ Board (TL, TR, BR, BL) để gán cờ `CellView.CORNER_TL`, v.v...
- Tại `cell_view.gd`, bán kính góc (`outer_rad` hoặc `base_rad`) được đưa vào `Vector4(c_tl, c_tr, c_br, c_bl)` thông qua Shader Parameter `corner_radius`.
- Shader mặc định sử dụng: `res://assets/shaders/cell_bg_round.gdshader`.

## 4. Trạng thái Nếp Gấp Cứng (Hard-state)
Dữ kiện từ `cell_view.gd:50` (`set_use_hard_bg`):
- Cơ chế hard-state không tải Texture mới mà chỉ thay đổi Shader Material.
- Nếu `_use_hard_bg = true`, material của ô sẽ chuyển sang Shader: `res://assets/shaders/cell_bg_round_hard.gdshader` (từ biến `_BG_SHADER_HARD`). 
- Node nhận shader này là thẻ `BgPanel` (Kiểu `ColorRect` nằm trên cùng của `cell.tscn`).

## 5. Bảng Màu Hiển thị (A/B Test Palette)
Dữ kiện từ `board_view.gd` (Hàm tĩnh `resolve_region_palette()`):
- **Custom Palette** (`is_custom_palette()`): 12 màu bắt đầu bằng `#CBCB24`, `#E45F8A`, `#8D7AEB`, `#F4A2E4`...
- **New Cell Only Palette** (`is_new_cell_only_palette()`): 12 màu bắt đầu bằng `#CDA400`, `#D36F8F`, `#8979DA`, `#F89BE5`...
- **Cell Color V3 Palette** (`is_cell_color_v3()`): 12 màu bắt đầu bằng `#c9b35b`, `#d37291`, `#8175bf`, `#efa2e0`...
Mọi cấu hình này đều dùng mảng `PackedColorArray`.

## 6. Lifecycle Reset và Object Pooling
Dữ kiện từ `cell_view.gd:133` (`_reset_to_empty_baseline`):
- Để tái sử dụng (Pool) cell mà không bị rò rỉ rác hoặc kẹt trạng thái visual, hàm reset sẽ thực hiện:
  1. `_hint_tween.kill()`
  2. `_frame_tween.kill()`
  3. `_preview_tween.kill()`
  4. `_idle_timer.stop()`
- **Tối ưu Shader Caching:** (Hàm `_get_bg_material`) Material được cache lại theo string key tĩnh: `"%d_%d_%d_%d_%d" % [c_tl, c_tr, c_br, c_bl, int(hard)]`. Các cell có cùng bo góc và cùng trạng thái hard-state sẽ dùng chung 1 instance `ShaderMaterial` để tiết kiệm draw calls.

## 7. Cây Node và Animation Mặc định của Cell
Dữ kiện từ cấu trúc `assets/prefab/cell.tscn` và `AnimationPlayer`:
- **Hierarchy Node Chính:**
  - `BgPanel` (Nền chính) -> `BgRed` (Phản hồi lỗi màu đỏ).
  - `Pattern` (Biểu tượng nền: Fishbone, Claw, Yarn, Dot...).
  - `CatPrompt`, `CatIcon` (AnimatedSprite2D với 81 frame cho hoạt ảnh Spine nướng sẵn thành Sprite sheet).
  - `EffectCatIconAppear2`, `EffectCollectGlow` (Các node CPUParticles2D phát sáng/nổ hạt).
  - `Crosses` -> Nhóm các dấu gạch chéo: `CrossOut`, `PromptCrossOut`, `LockMask`, `RedCross`.
  - `Heart` -> `BrokenHeart1`, `BrokenHeart2`, `BrokenFish`.
- **Animation Resource Chính:** Các track animation chạy keyframe modulate/scale/visible đã được định nghĩa cứng trong `.tscn` bao gồm:
  `CatIconCry`, `CatIconFrustrated`, `CatIcon`, `CrossOut`, `CrossOutAppear_2`, `CrossOutDisAppear`, `Disappear`, `ErrorAppear1`, `ErrorAppear2`, `ErrorAppear_2`, `PromptCrossOutDisappear`, `Glow`, `Idle`.

---
STATUS: COMPLETE
(Đã bao trọn toàn bộ thông số Shader, Layout, Asset Path, Animation Names, Hierarchy Node và logic tái sử dụng. Không có thông tin Unity lai tạo hay tự suy đoán kiến trúc).
