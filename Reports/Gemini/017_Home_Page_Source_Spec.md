# Báo cáo đặc tả kỹ thuật: GEM-R12-017 (Home Page Source Spec)

**Nguồn đối chiếu:** `D:\Projects\_GameExtract\Main_Meokdoku`
**Mục tiêu:** Đặc tả toàn bộ UI Hierarchy, Logic điều hướng, Hoạt ảnh, Tích hợp UIManager và Asset của màn hình Home (Home Page).

---

## 1. Cấu trúc Cây Node (home_page.tscn)
File cấu trúc: `scripts/module/home/ui/home_page.tscn`.

- **HomePage** (Control, `layout_mode = 3`)
  - **Root** (Control, `anchors_preset = 13` - căn giữa toàn màn hình)
    - **Loge**: Chứa `LogoSpine` (SpineSprite) vị trí (304, 241). Tích hợp `SpineAnimationTrack` để chơi anim `"LogoSpine Track 0"`.
    - **StartBtn**: Nút Play chính (Instance từ `btn_with_tag.tscn`).
    - **DailyStreakLayout**: Hệ thống Layout cho Daily/Rank (Chứa 4 slot: `DcEntrySlot`, `StreakEntrySlot`, `StreakSmallEntrySlot`, `RankEntrySlot`).
    - **VBoxContainer** (`group = _safe_top`): Vùng tránh tai thỏ (Safe Area).
      - **HeaderAdaptHolder**: Component tự động offset xuống.
      - **Header** (Instance `back_and_setting_header.tscn`):
        - `BackBtn`: (Trạng thái mặc định: `visible = false`).
        - `ProfileEntry`: Cụm Avatar người chơi (Nằm bên trái). Chứa `AvatarSlot` và `AvatarBtn`.
        - `SettingsBtn`: Nút cài đặt góc phải.
  - **Background** (ColorRect, `z_index = -1`): Chứa `GridFlowLoop` (Sprite2D) shader UV Scroll chạy nền lưới.
  - **AnimationPlayer**: Chứa anim `MainInterface` điều khiển ra/vào màn hình.

---

## 2. Luồng On Show, On Hide & Cleanup
File kịch bản: `scripts/module/home/view/home_page.gd`.

- **`on_show()` Logic:**
  1. Hủy quảng cáo Banner (`UniKitManager.destroy_ad("banner")`).
  2. Phát BGM (`SoundManager.start_bgm()`).
  3. Hiện nút Profile nếu config cho phép: `_profile_entry.visible = ABTestManager.leaderboard_func.is_enabled()`.
  4. Play Anim ra sân: `_anim.play_section_with_markers(&"MainInterface", &"", &"disappear")`.
  5. Cập nhật Text level (`_refresh_progress_display()`).
  6. Warm pool UIManager (Prewarm): Các UI `RANK_ACTIVITY_CHANGE` và `AWARD` nếu có logic pending.
  7. Lên lịch Popup (`_build_popup_queue()`): Quét file `assets/cfg/dialog_priority_strategy.json` sinh hàng đợi popup tự động.

- **`on_hide()` Logic & Cleanup:**
  - Cực kỳ tối giản: Hàm `on_hide()` chỉ gọi `_popup_queue.clear()` và `visible = false`. Không cần hủy timer hay tween phức tạp do các popup tự giải phóng và AnimPlayer tự dừng.

---

## 3. Chức năng Nút bấm (Buttons)
*Lưu ý: Không tìm thấy nút "Bank" hay "How-to-play" trực tiếp trên Home Page. Khả năng cao các nút này nằm bên trong màn hình `Settings` hoặc được spawn động từ Popup.*

- **Start / Continue (`_on_start_btn_pressed`)**: 
  - Đóng tất cả UI trừ Home và Game: `UIManager.hide_all_except([UiName.HOME, UiName.GAME])`.
  - Ghi Log: `Tracker.track_btn_click(Tracker.Btn.NORMAL_PLAY)`.
  - Khởi tạo Exit Transition: Mở màn Game level hiện tại (đọc từ `GameState.get_current_level()`).
- **Settings (`_on_settings_btn_pressed`)**: Gọi `UIManager.show_ui(UiName.SETTING)`. Nút này lưu toạ độ Y xuống `HomeSettingAnchor` để màn Setting thả popover đúng vị trí.
- **Profile / Avatar (`_on_avatar_btn_pressed`)**: Mở màn hồ sơ `UIManager.show_ui(UiName.PROFILE)`.
- **Back / Quit (`_on_back_request`)**:
  - Khi ấn phím Back phần cứng của thiết bị (Android), gọi `_request_quit_confirm()`.
  - Hiện popup xác nhận thoát game: `UIManager.show_ui(UiName.CONFIRM, {"on_confirm": quit})`.

---

## 4. Hiển thị Cấp độ, Tiến trình & Độ khó
- Nút Play luôn tự động lấy Text theo cấu trúc: `tr("GAME_LEVEL_TITLE") % GameState.get_current_level()`. 
- Logic Level Khó: Dùng `LevelData.is_hard_level(lv)` truyền vào thuộc tính `show_difficult` của `StartBtn`.
- Hiệu ứng nút Khó: Config A/B qua `ABTestManager.hard_button.effect_variant()`. 

---

## 5. Animation, Tween & Transition Màn Hình
- Quá trình đi từ Home sang Game (`_exit_to_page`) được kiểm soát bởi chuỗi Delay và Tween:
  1. `_is_exiting = true` (Chặn mọi Input/Back).
  2. Bắt đầu play đoạn Anim `disappear` làm mờ Dần (Fade out các nút).
  3. Lấy `entry_delay` = Hiệu số Timer Marker để load Async màn tiếp theo, chèn UI mới lên trên với Z-Index = `_new_page_node.z_index + 1` (để layer Home vẫn đứng lót dưới cùng).
  4. `anim_finish_delay` = Chờ Anim kết thúc hẳn mới `UIManager.hide_ui(UiName.HOME)`.

---

## 6. Bản đồ Asset, Theme & Localization Key
- **Spine Logo:** `res://assets/effect/spine/ui_logo/ui_logo.tres`.
- **Prefab Nút (Tag Btn):** `res://assets/prefab/btn_with_tag.tscn`.
- **Thanh Header Config:** `res://assets/prefab/back_and_setting_header.tscn`.
- **Ảnh nền lưới (Flow Grid):** `res://assets/effect/texture/ui/et_main_interface_flow.png` (kết hợp Mask `et_main_interface_flow_mask.png` và Shader `fx_uv_scroll.gdshader`).
- **Font chữ chính:** `res://assets/fonts/Roboto-medium.tres`.
- **Localization Key:**
  - `GAME_LEVEL_TITLE`: Dùng để nối chuỗi (Ví dụ: "Level %d").

---

## 7. Bảng Evidence Trích Dẫn

| Chi tiết Kỹ thuật | File Nguồn | Node / Hàm / Dòng mã | Mức chắc chắn |
| :--- | :--- | :--- | :--- |
| Z-Index, Safe Area | `home_page.tscn` | `Root/VBoxContainer` có group `_safe_top`, Background `z_index = -1` | 100% |
| Back/Quit Action | `home_page.gd` | `_on_back_request()` mở `UiName.CONFIRM` quit game (Dòng 547) | 100% |
| Default Offline Hard Level | `home_page.gd` | `_refresh_progress_display()` đọc `LevelData.is_hard_level(lv)` (Dòng 110) | 100% |
| Transition Delay | `home_page.gd` | `_exit_to_page()` dùng Marker timer tính `entry_delay` Async Load (Dòng 624) | 100% |
| AB Test Variant Avatar | `home_page.gd` | Check `ABTestManager.leaderboard_func.is_enabled()` bật Profile (Dòng 136) | 100% |
| Home Cleanup | `home_page.gd` | `on_hide()` gọi `_popup_queue.clear()` (Dòng 534) | 100% |
| Popup Queue Config | `home_page.gd` | Đọc `assets/cfg/dialog_priority_strategy.json` lọc `OpenScene == "home"` (Dòng 182) | 100% |

STATUS: COMPLETE
