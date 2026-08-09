# Báo cáo đặc tả kỹ thuật: GEM-R9-009 (Gameplay HUD Remaining - Source Spec)

**Nguồn đối chiếu:** `D:\Projects\_GameExtract\Main_Meokdoku`
**Mục tiêu:** Liệt kê đặc tả chính xác cho toàn bộ HUD gameplay còn lại sau RuleBar và Board: Header, CatHeartRow, các AdaptHolder, FunctionArea, BottomTools và AdBanner. Bao gồm chi tiết về điều kiện ẩn/hiện từ Script.

---

## 1. Cấu trúc và Thứ tự Node Chính trong VBoxContainer
Theo bằng chứng từ file `scripts/module/game/ui/game_page.tscn` (từ dòng 969 đến cuối file), thẻ `Root/VBoxContainer` có thứ tự duyệt các node từ trên xuống dưới (Top to Bottom) như sau:

1. **Header** (Control)
2. **CatAdaptHolder** (Control) - `stretch_ratio = 91.0`
3. **CatHeartRow** (Control)
4. **RuleBarAdaptHolder** (Control) - `stretch_ratio = 34.0`
5. **RuleBar** (Control)
6. **ComboFeedback** (Control) - `stretch_ratio = 0.0`
7. **BoardAdaptHolder** (Control) - `stretch_ratio = 128.0`
8. **BoardContainer** (Control) - `stretch_ratio = 0.0` (Chứa `BoardView`)
9. **FunctionAdaptHolder** (Control) - `stretch_ratio = 16.0`
10. **FunctionArea** (Control) - `stretch_ratio = 0.0`
11. **BottonAdaptHolder** (Control) - `stretch_ratio = 190.0`
12. **BottomTools** (Control) - `stretch_ratio = 0.0`
13. **AdAdaptHolder** (Control) - `stretch_ratio = 70.0`
14. **AdBanner** (Control) - `stretch_ratio = 0.0`
15. **AdDownAdaptHolder** (Control) - `stretch_ratio = 40.0`

*Dữ kiện suy luận từ cấu trúc Layout:* Các `AdaptHolder` có stretch ratio lớn đóng vai trò là lò xo (Flexible Space / Spacer) để đẩy các thành phần UI dãn ra vừa khít mọi tỷ lệ màn hình (Aspect Ratio).

---

## 2. Đặc tả chi tiết từng thành phần HUD và Điều kiện Ẩn/Hiện

### A. Header (Đỉnh màn hình)
* **Nút Back (Gear / Setting):** `Header/BackBtn` (Button), visible mặc định. Kết nối tín hiệu pressed gọi hàm `_on_gear_btn_pressed`.
* **Nút Settings:** `Header/SettingsBtn` (Button). Kết nối tín hiệu pressed gọi `_on_settings_btn_pressed`.
* **Level Display:** `Header/LevelDisplay` (Control), visible mặc định trong tscn là `false`. Chứa `Title` ("GAME_HEADER_LEVEL") và `Value`.
* **Score Display:** `Header/ScoreDisplay` (Control), visible mặc định = `false`. Gồm `Title` ("GAME_HEADER_SCORE").
* **IQ Display:** `Header/IQDisplay` (Control), visible mặc định = `false`.
* **Các nút bị giấu theo Code (.gd):**
  - **StrategyBtn:** Chỉ hiển thị trong môi trường test/debug, bị ẩn ở bản release. Dữ kiện từ `game_page.gd:572`: `_strategy_btn.visible = not OS.has_feature("rel")`.
  - **PrevBtn / NextBtn:** Ẩn/hiện tuỳ vào chế độ điều hướng màn chơi (Test Tool). Dữ kiện `game_page.gd:575`: `_prev_btn.visible = _nav_enabled`.
  - **InfoBtn:** Dữ kiện `base_game_page.gd:342`: `info_btn.visible = is_info_popup`.

### B. CatHeartRow (Thanh tiến trình Mèo và Mạng)
* **Kích thước tối thiểu:** `custom_minimum_size = Vector2(0, 88)` (Dòng 1276).
* **Trạng thái mặc định:** `modulate = Color(1, 1, 1, 0)` (Tàng hình hoàn toàn chờ hoạt ảnh `Appear` kích hoạt fade-in).
* **Target (Số lượng mèo cần thu thập):**
  - `CatFaceIcon` (Texture: `res://assets/sprites/common/cat_face.png`).
  - `CatCountLabel` (RichTextLabel).
  - Dữ kiện ẩn/hiện Glow từ `base_game_page.gd:601`: `cat_glow.visible = keep_cat`.
* **HeartBg (Thanh Mạng - Life):**
  - Gắn vào bên phải: `anchor_left = 1.0, anchor_right = 1.0`, offset `X = -515.0 đến -255.0`.
  - Chứa 3 Slot mạng bằng các Prefab con: `LifeSlot`, `LifeSlot2`, `LifeSlot3` (Instance: `res://scripts/module/game/ui/compont/fish_slot.tscn`).
  - Dữ kiện ẩn/hiện slot mạng `game_page.gd:357`: `slot.visible = false` (khi cần giấu luôn slot) hoặc `(slot as Control).visible = true`.
* **Tips (Cảnh báo Last Chance):** 
  - Gồm `Bubble` và `Label`. 
  - Dữ kiện bật từ `game_page.gd:1771`: `_warn_tips.visible = true` (Hiện bubble "WARN_LIFE_LAST_CHANCE" khi chỉ còn 1 mạng). 
  - Dữ kiện mờ đi từ `game_page.gd:1825`: `_warn_bubble.modulate.a = 0.0`.

### C. FunctionArea (Nút chức năng góc dưới bàn cờ)
* **Gồm nút:** `ClearBtn` (Instance từ `assets/prefab/tool_button.tscn`).
* **Kích thước tối thiểu:** `Vector2(210, 250)`.
* **Neo vị trí:** Bên phải màn hình (`anchor_left = 1.0`, `anchor_right = 1.0`), offset `-172.0` đến `38.0`.
* **Icon:** Sử dụng texture `res://assets/sprites/game/item_tool_clear.png`.

### D. BottomTools (Thanh công cụ dưới cùng)
* **Kích thước tối thiểu:** `custom_minimum_size = Vector2(0, 200)` (Dòng 1726).
* **Gồm các nút (Prefab tool_button):**
  - `RevealBtn`: Tọa độ X = 250. Icon: `res://assets/sprites/game/tool_cat_item.png`.
  - `HintBtn`: Tọa độ X = 620. Icon: `res://assets/sprites/game/icon_hint_lamp.png`.
  - `CoordBtn`: Tọa độ X = 817. Icon: `res://assets/sprites/game/icon_tool.png`. Trạng thái mặc định: `visible = false`.

### E. AdBanner (Khung Quảng Cáo)
* **Kích thước tối thiểu:** `custom_minimum_size = Vector2(0, 180)` (Dòng 1792).
* **Khung viền (AdBg) & Text (AdLabel):** Đều bị ẩn mặc định (`visible = false`).
* **Dữ kiện Script (`base_game_page.gd:1009`):** Khung banner không chứa logic hiển thị banner UI trực tiếp mà gọi SDK plugin bên ngoài thông qua `UniKitManager.show_banner("banner", ad_position, true, 0, banner_h + adapt_h)`. Khung này đóng vai trò chừa sẵn không gian (Placeholder) để không bị quảng cáo đè lên UI.

### F. Hoạt ảnh xuất hiện (AnimationPlayer - .tscn)
* **Node:** `AnimationPlayer` (Dòng 1859).
* **Animation `Appear`:** Thực hiện trình tự mờ dần (Fade-in alpha qua track thuộc tính modulate) từ `0` lên `1` cho các bộ phận:
  - `CatHeartRow`: từ 0.016s đến 0.300s.
  - `RuleBar`: từ 0.359s đến 0.578s.
  - `BottomTools/HintBtn` & `RevealBtn`: từ 0.409s đến 0.639s.
  - `FunctionArea/ClearBtn`: từ 0.453s đến 0.684s.
  - `Header/BackBtn`: từ 0.512s đến 0.765s.

---
STATUS: COMPLETE
(Đã cập nhật báo cáo bao hàm đầy đủ các chỉ số Layout, Stretch Ratio, Assets, cấu hình Animation từ `.tscn` và các logic bật/tắt (visible) từ `.gd`. Không tìm thấy thêm dữ kiện bị ẩn giấu nào khác).
