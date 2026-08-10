# Báo cáo đặc tả kỹ thuật: GEM-R12-018 (Settings, Language & Anchor)

**Nguồn đối chiếu:** `D:\Projects\_GameExtract\Main_Meokdoku`
**Mục tiêu:** Đặc tả Hierarchy, logic State (Âm thanh, Rung, Ngôn ngữ), luồng Mở/Đóng và cơ chế Neo toạ độ (Anchor).

---

## 1. Cấu trúc Cây Node (Hierarchy)

### A. Settings Page (`scripts/module/setting/ui/setting_page.tscn`)
- `Root` (Control)
  - `Content` (Control)
    - `PanelContainer` (Background)
      - `VBoxContainer`
        - `GridContainer`: Chứa các nút Toggle (`MusicCtrl`, `SoundCtrl`, `VibrationCtrl`, `PeopleCtrl`).
        - `ToggleContainer`: Chứa `PatternModeSwitch` (Chế độ mù màu) và `LanguageSwitchWidget` (Dropdown ngôn ngữ).
        - `BtnContainer`: Chứa `LanguageBtn`, `FeedbackBtn`, `HowToPlayBtn`, `OrangeRestartBtn`.
        - `PrivacyContainer` & `TermContainer`: Các link ToS, Privacy, CMP.
        - `HBoxContainer`: Chứa `VersionLabel`.
  - `AnimationPlayer`: Chạy anim `GenericPopup`.

### B. Language Page (`scripts/module/language/ui/language_page.tscn`)
- `Root`
  - `Overlay` (ColorRect): Nền đen mờ cản chuột.
  - `Content`
    - `ScrollContainer`: Chứa danh sách `OptionsList` (Các `LanguageOption` instance).
    - `YesBtn`: Nút xác nhận đổi ngôn ngữ.

### C. How-to-play Page (`scripts/module/how_to_play/ui/how_to_play_paged_page.tscn`)
- `Root`
  - `Overlay` (ColorRect).
  - `Content`: Khung hiển thị.
    - `BoardClip` (Control, `clip_contents = true`): Chứa các `Board1`, `Board2`, `Board3` để vuốt/chuyển trang.
    - `Caption` (RichTextLabel).
    - `ButtonRow`: Chứa `BackBtn` (Quay lại trang trước) và `MainBtn` (Trang tiếp / Chơi).

---

## 2. Handler Mở / Đóng / Back & Animation Timing
- **Mở (Settings)**: Gọi `UIManager.show_ui(UiName.SETTING)`. Hàm `on_show` sẽ chạy Anim `GenericPopup` bằng Marker từ rỗng `&""` đến `&"Mark"`.
- **Đóng (Settings)**: 
  - Khi bấm CloseBtn (`_on_close_btn_pressed`), gọi `UIManager.hide_ui`.
  - Hàm `on_hide()` chạy Anim ngược từ `&"Mark"` về `&""`. Đợi `_anim.animation_finished` mới set `visible = false` và gọi `_on_close_cb`.
- **Luồng Settings -> How-to-play**: 
  - Bấm How To Play, Settings set cờ `_skip_next_close_anim = true` và `_suppress_next_close_cb = true`, sau đó tự ẩn ngay lập tức (không chạy Anim tắt).
  - Mở HowToPlay: `UIManager.show_ui(UiName.HOW_TO_PLAY_PAGED)`. Dùng `await htp.closed` để chờ màn HowToPlay đóng lại mới kích hoạt `_on_close_cb` của Settings để dọn dẹp cấp trên.

---

## 3. Quản lý Trạng thái (Music, Sound, Vibration, Language)
| Tính năng | Model Lưu Trữ | Hàm Kích Hoạt Ngay Lập Tức (UI Reaction) |
| :--- | :--- | :--- |
| **Music** | `GameState.set_music_on()` | Đổi Texture Icon On/Off, gọi `SoundManager.refresh_bgm()`, hiện Toast `SETTING_MUSIC_ON`. |
| **Sound** | `GameState.set_sound_on()` | Đổi Texture, phát test tiếng click `SoundManager.play(SoundManager.Kind.BTN_CLICK)`. |
| **Vibration** | `GameState.set_vibration_on()` | Đổi Texture, rung thử máy: `VibrateManager.play_vibrate(VibrateManager.Level.LEVEL3)`. |
| **Language** | `LanguageManager.set_locale()`, `GameState.set_apply_locale()` | Đóng form Language, OS bắn `NOTIFICATION_TRANSLATION_CHANGED` làm hàm `_refresh_dynamic_text()` cập nhật toàn UI. |

---

## 4. Đặc tả HomeSettingAnchor (Inference vs Fact)
- **INFERENCE của Yêu Cầu**: Yêu cầu ngụ ý màn Settings sẽ đọc `HomeSettingAnchor` để đặt Popover theo nút Settings ngoài Home.
- **FACT (Sự thật Source Code)**: 
  - `home_page.gd` **ghi** tọa độ nút Settings xuống Autoload `HomeSettingAnchor.set_settingbtn_y()`.
  - Tuy nhiên, màn **Settings KHÔNG đọc** giá trị này. Settings tự động căn giữa màn hình qua hàm `_center_panel_vertically()` (Dùng `offset_top = -h / 2.0`).
  - Lớp thực sự **đọc** `HomeSettingAnchor` là màn Daily Streak (`streak_page.tscn`), thông qua file extension `scripts/module/ui/extensions/follow_home_settingbtn_y.gd` để neo cửa sổ Streak Popover lên ngang hàng với nút Settings ở Home.

---

## 5. Asset, Theme & Localization Key
- **Sprite / Icon Setting**: Tự động bind logic đổi trạng thái qua các biến export `_tex_music_off`, `_tex_sound_off`, `_tex_vibrate_off`, `_tex_people_off`. (Mặc định On dùng Texture của node, Off dùng Texture load từ inspector).
- **Localization Keys**:
  - Setting Toast: `SETTING_MUSIC_ON`, `SETTING_SOUND_OFF`, `SETTING_VIBRATION_ON`, `SETTING_PEOPLE_ON`, `SETTING_PATTERN_ON`.
  - Khác: `SETTING_VERSION`, `NETWORK_ERROR` (Khi mất mạng bấm Feedback).
  - Language Key (trong _OPTIONS dictionary): `LANG_NAME_EN`, `LANG_NAME_JA`, `LANG_NAME_ES`, `LANG_NAME_FR`...

---

## 6. Dependency Map
- **UIManager**: Cung cấp Navigation Contract (`show_ui`, `hide_ui`, stack logic).
- **GameState**: Single source of truth để ghi/đọc `is_music_on()`, `is_sound_on()`, `is_pattern_mode_on()`, v.v.
- **SoundManager / VibrateManager**: Tầng Hardware Wrapper để phản hồi ngay lập tức cho người dùng khi Switch bật.
- **HelpshiftManager**: Tầng SDK xử lý FAQ, lấy số tin nhắn chưa đọc (unread).
- **ABTestManager**: Quyết định Settings hiển thị Dropdown ngôn ngữ (`settings_language.is_dropdown_mode()`) hay màn chọn riêng, hiện/ẩn CMP, Rule Text v.v.

---

## 7. Bảng Evidence Kỹ thuật

| Loại | Chi tiết Kỹ thuật | File Nguồn | Hàm / Dòng mã | Mức chắc chắn |
| :--- | :--- | :--- | :--- | :--- |
| **FACT** | Close Anim Timing | `setting_page.gd` | `on_hide()` đợi `await _anim.animation_finished` (Dòng 237) | 100% |
| **FACT** | How-to-play Skip Anim | `setting_page.gd` | `_on_how_to_play_btn_pressed()` set `_skip_next_close_anim = true` (Dòng 548) | 100% |
| **FACT** | Translation Notification | `setting_page.gd` | Bắt event `NOTIFICATION_TRANSLATION_CHANGED` gọi `_refresh_dynamic_text()` (Dòng 246) | 100% |
| **FACT** | Home Anchor Ghi (Write) | `home_page.gd` | `HomeSettingAnchor.set_settingbtn_y` (Dòng 87) | 100% |
| **FACT** | Home Anchor Đọc (Read) | `streak_page.tscn`, `follow_home_settingbtn_y.gd` | `HomeSettingAnchor.anchor_changed.connect` (Dòng 21) | 100% |
| **INFERENCE** | Settings không dùng Anchor | `setting_page.gd` | Dùng `_center_panel_vertically()` (Dòng 211) thay vì Anchor | 100% |
| **FACT** | Vibration Preview | `setting_page.gd` | `VibrateManager.play_vibrate(VibrateManager.Level.LEVEL3)` (Dòng 449) | 100% |

STATUS: COMPLETE
