# R12 Home presenter và prefab installer — 2026-08-10

## Kết quả

Đã chuyển phần Home có đủ bằng chứng nguồn thành presenter Unity và editor installer. Prefab chưa được nối registry/startup vì Settings, Daily/Streak/Rank/Profile và Splash vẫn chưa đủ; vì vậy UI thiếu không thể xuất hiện trong luồng chơi.

`GEM-R12-017` giúp xác nhận route, cleanup và nhóm asset chính. Các anchor/offset, animation track và policy entry bị thiếu trong báo cáo đã được kiểm tra trực tiếp ở `home_page.gd`, `home_page.tscn`, `btn_with_tag.tscn`, `back_and_setting_header.tscn` và `fx_uv_scroll.gdshader`.

## Presenter

- Mỗi `OnShow` đọc lại current level, hard-level và ba config Home; gọi BGM qua serialized `SoundService` khi scene bootstrap gắn service thật.
- Start giữ Home+Game, khóa input, mở Game tại chênh lệch marker `Entry-disappear` và đóng Home ở cuối animation.
- Settings/Profile gọi đúng `UIManager` route; Profile mặc định ẩn theo `leaderboard_func=0`.
- Back bỏ qua khi đang exit hoặc Settings đang mở; trường hợp còn lại mở Confirm với action quit.
- Hide/reopen kill DOTween sequence, clear popup queue và reset state exit/page.
- Header dùng cùng safe-top/header-adapt contract 0→65 px của source.

## Visual adapter

- `UIHomeFlow.shader` là bản chuyển trực tiếp của flow/mask: tốc độ `(0.015,-0.015)`, repeat flow, mask cố định và premultiplied alpha.
- Logo dùng `Assets/_Project/Sprites/common/logo.png`, một asset hoàn chỉnh từ nguồn, làm static adapter. Không dùng atlas Spine như ảnh phẳng và không tự dựng animation xương giả.
- Start 750×160, Root width 1080, Loge/Header/Profile/Settings và bốn slot Daily/Streak/Rank dùng offset đã đổi Godot Y-down sang UGUI Y-up.
- Hard badge mặc định dùng `difficulty_banner.png`; các hard effect variant 1–4 chưa được giả lập vì offline default là 0.

## Hierarchy installer

```text
HomePage
├─ Background
│  └─ GridFlowLoop
└─ Root
   ├─ Loge
   │  └─ LogoStaticAdapter
   ├─ StartBtn
   │  ├─ Text
   │  └─ HardBadge
   ├─ DailyStreakLayout
   │  ├─ DcEntrySlot
   │  ├─ StreakEntrySlot
   │  ├─ StreakSmallEntrySlot
   │  └─ RankEntrySlot
   └─ VBoxContainer
      ├─ HeaderAdaptHolder
      └─ Header
         ├─ BackBtn
         ├─ ProfileEntry
         │  └─ AvatarSlot
         └─ SettingsBtn
```

Các slot entry và AvatarSlot cố ý để rỗng. Chúng là điểm gắn module thật ở lượt sau, không phải UI giả.

## Kiểm tra

- `Meowdoku.Core`: compile sạch.
- `Meowdoku.Gameplay`: compile sạch với `HomePagePresenter`.
- `Meowdoku.Editor`: compile sạch với `HomePagePrefabInstaller`.
- `Meowdoku.EditModeTests`: compile sạch với timing assertions bổ sung.
- Unity Refresh đã sinh `HomeFlow.mat` và `HomePage.prefab`; material trỏ đúng shader/mask, hierarchy đầy đủ và không có missing-script/Console import error. PlayMode visual/route vẫn chờ vì prefab chưa được đưa vào registry.

## Phần tiếp theo

1. Sau Refresh, xác nhận material/prefab và Console.
2. Port Settings/Language/How-to-play từ báo cáo 018 và source trực tiếp.
3. Port entry modules mặc định trước khi đưa Home vào registry.
4. Chỉ bật startup route khi Splash/Tutorial/Home/Game đều có prefab hợp lệ.
