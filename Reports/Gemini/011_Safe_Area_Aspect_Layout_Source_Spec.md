# Báo cáo đặc tả kỹ thuật: GEM-R7-011 (Safe Area, Aspect Ratio & Layout Source Spec)

**Nguồn đối chiếu:** `D:\Projects\_GameExtract\Main_Meokdoku`
**Mục tiêu:** Đặc tả hệ thống safe-area, adaptation, và các quy tắc giãn layout Gameplay R7 theo mã nguồn gốc của Godot.

---

## 1. Cấu hình Viewport & Màn Hình (Từ `project.godot`)
- **Độ phân giải gốc (Base Resolution):** 
  - `window/size/viewport_width=1080`
  - `window/size/viewport_height=2400`
- **Stretch Mode:** 
  - `stretch/mode="canvas_items"`
  - `stretch/aspect="keep_width"`
- **Phân tích:** 
  Game luôn giữ cố định chiều rộng ở 1080 (Pixel-perfect width). Chiều cao (height) sẽ được kéo dãn (expand) dài hoặc ngắn đi so với 2400 tùy thuộc vào màn hình thiết bị. Mọi sự thay đổi về layout theo chiều cao đều do các Lò Xo (Spacer/AdaptHolder) trong `VBoxContainer` gánh vác.

---

## 2. Cây Layout & Hệ Thống Các Adapt Holder

### A. Root và VBoxContainer (game_page.tscn)
Thành phần chính chịu trách nhiệm sắp xếp layout dọc là `VBoxContainer`.
- **Node:** `Root/VBoxContainer`
- **Groups:** Thuộc group `_safe_top` và `_safe_bottom` để tự động thò/thụt tránh tai thỏ.
- **Anchors & Offsets:**
  - `anchor_left = 0.5`, `anchor_right = 0.5` (Căn giữa theo chiều ngang)
  - `anchor_top = 0.0` (Mặc định ngầm hiểu do preset 13), `anchor_bottom = 1.0` (Trải dài xuống hết đáy)
  - `offset_left = -540.0`, `offset_right = 540.0` => **Khóa cứng chiều rộng ở mức 1080px**. Chiều cao linh hoạt theo thiết bị.

### B. Mức độ dãn (Stretch Ratios) của các phần tử
Trong `VBoxContainer`, các phần tử lò xo (AdaptHolder) và UI cố định có `custom_minimum_size` (min size) và `size_flags_stretch_ratio` xác định từ 2 file cấu hình `.tres`.

**Layout Mặc Định (`board_no_fuction.tres`)**
- `HeaderAdaptHolder`: Stretch Ratio = 65.0
- `Header`: Min Size Y = 120, Stretch = 0
- `CatAdaptHolder`: Min Size Y = 4, Stretch Ratio = 91.0
- `CatHeartRow`: Min Size Y = 88, Stretch = 0
- `RuleBarAdaptHolder`: Min Size Y = 4, Stretch Ratio = 34.0
- `RuleBar`: Min Size Y = 170, Stretch = 0
- `BoardAdaptHolder`: Min Size Y = 4, Stretch Ratio = 128.0
- `BoardContainer`: Min Size Y = 1008, Stretch = 0
- `FunctionAdaptHolder`: Stretch Ratio = 16.0
- `FunctionArea`: Stretch = 0
- `BottonAdaptHolder`: Stretch Ratio = 190.0 (Nhận nhiều không gian thừa nhất)
- `BottomTools`: Min Size Y = 200, Stretch = 0
- `AdAdaptHolder`: Stretch Ratio = 70.0
- `AdBanner`: Min Size Y = 180, Stretch = 0
- `AdDownAdaptHolder`: Stretch Ratio = 40.0

**Layout cho Board To (Enlarged) (`board_big_no_fuction.tres`)**
Áp dụng khi cấu hình A/B kích hoạt phóng to board:
- `BoardAdaptHolder`: Stretch Ratio = 107.0 (Giảm từ 128)
- `BottonAdaptHolder`: Min Size Y = 2, Stretch Ratio = 169.0 (Giảm từ 190)
- `HeaderAdaptHolder`: Stretch Ratio = 66.0 (+1)
- `CatAdaptHolder`: Stretch Ratio = 93.0 (+2)
- `RuleBarAdaptHolder`: Stretch Ratio = 35.0 (+1)
- *Các AdaptHolder khác giữ nguyên.*

---

## 3. Cấu hình A/B (Board Size Big Config)
- **File:** `scripts/module/abtest/config/board_size_big_config.gd`
- **Giá trị Offline Mặc định (Default):** `VALUE_NORMAL = 0` (Không phóng to).
- **Variant:** `VALUE_ENLARGED = 1` (Bật phóng to).
- Lệnh kiểm tra: `ABTestManager.board_size_big.is_enlarged()`.

---

## 4. Công Thức Scale Board (`_relayout_board`)
Được tìm thấy tại `base_game_page.gd` (Dòng 1374, 1427-1439).
- **Hằng số thiết kế:** `FIXED_BOARD_WIDTH = 1008.0`
- **Logic:**
  ```gdscript
  var target_width: float = FIXED_BOARD_WIDTH
  if ABTestManager.board_size_big.is_enlarged() and size_n >= 8: # _BOARD_ENLARGE_MIN_SIZE
      target_width = 1008.0 * 1.04167 # _BOARD_ENLARGE_FACTOR (Scale board lên ~1050 px)
  
  var board_scale: float = target_width / intrinsic.x
  _board_container.custom_minimum_size.y = maxf(FIXED_BOARD_WIDTH, visible_h)
  ```
- Tính toán kích thước lưới: Dựa trên logic này, chiều cao tối thiểu của khu vực bàn cờ được giữ là 1008.0 px để đảm bảo luôn chừa đủ một vùng hình vuông ngay cả khi puzzle nhỏ.

---

## 5. Safe Area & Notch System (Tai thỏ)
Quản lý toàn cục qua `scripts/module/ui/ui_manager.gd`.

### A. Lấy kích thước Notch từ Native OS
- Sử dụng hàm `DisplayServer.get_display_safe_area()` cho các thiết bị di động (`iOS/Android`).
- Tính `top_inset` (viền tai thỏ trên) và `bottom_inset` (thanh home bar dưới).

### B. Cơ Chế Áp Dụng (Group Pattern)
- Node nào muốn tránh Notch sẽ được thêm vào các nhóm (`Groups`) đặc biệt trên Editor Godot:
  - `_safe_top`: Cộng dồn `top_inset` vào `offset_top` (Đẩy UI xuống dưới tai thỏ).
  - `_safe_bottom`: Trừ bớt `bottom_inset` khỏi `offset_bottom` (Đẩy UI lên trên home bar).
  - `_collapse_when_top_safe`: Hệ thống tự động thu nhỏ/ẩn đi nếu tai thỏ quá lớn.

### C. Vòng đời cập nhật (Signal/Callback)
- Lắng nghe thay đổi xoay màn hình (orientation) hoặc Resize cửa sổ thông qua Signal:
  `get_viewport().size_changed.connect(_reapply_safe_area_all)`
- Ngay lập tức gọi lại `_apply_safe_area()` để tính và cập nhật offset các node trong group _safe_top / _safe_bottom.

---

## 6. Checklist Parity (Dành cho Codex Unity Port)
Để Unity có behaviour hoàn toàn giống Godot:
- [ ] Màn hình (Canvas Scaler) phải đặt: Scale with Screen Size, Reference `1080x2400`, Match Width or Height -> `Match = 0 (Width)`.
- [ ] Root Gameplay phải là một `Vertical Layout Group` tương đương với `VBoxContainer` của Godot.
- [ ] Phải cấu hình các phần tử Spacer (AdaptHolder) tương đương tính năng Flexible Space của Layout Element trong Unity, với Flexible Height tương ứng Stretch Ratio (65, 91, 190...).
- [ ] Tích hợp hệ thống đọc Safe Area (`Screen.safeArea`) của Unity và apply giá trị top/bottom vào Padding của vùng chứa cha.
- [ ] Code scale board: Nếu kích thước >= 8 và cờ A/B đang bật, thay đổi Target Width của board container từ 1008 lên 1050.

---
STATUS: COMPLETE
(Đã định hình toàn bộ tư duy giãn layout từ `.tres`, công thức ép size `.gd` và cách ứng xử vòng đời Safe Area. Không thiếu dữ liệu lõi).
