# R12 Settings Shared A/B and Conditional Routes Report — 2026-08-11

## Sai lệch phát hiện

Godot giữ `settings_language`, `blind_mod` và `rule_text` trong `ABTestManager` dùng chung; Settings gọi dye timing `open_setting` trước khi đọc variant. Unity trước lượt này chỉ đăng ký nhóm Ads trong `AbConfigRuntime`, còn Home/Settings/Game tự tạo config local mặc định. Vì vậy production provider không thể bật Language/How-to-play dù contract và prefab đã tồn tại.

## Thay đổi

- Thêm `SettingsConfigSet` vào catalog chung của `AbConfigRuntime`.
- `SettingsPagePresenter` nhận runtime qua `IAbConfigRuntimeConsumer`, reload `open_setting` khi show và dùng chung `SettingsLanguage/BlindMod/RuleText`.
- `GameplayPagePresenter` đọc cùng `RuleText` sau timing `game_start`, tránh Settings hiện HTP nhưng Game dùng variant khác.
- AppBootstrap và Home dùng provider peek của chính runtime khi quyết định system locale.
- Không thêm singleton/global state mới; runtime vẫn do AppScene sở hữu và UIManager inject theo interface.

## AppScene PlayMode

- Test provider chỉ thuộc `Meowdoku.PlayModeTests`, đặt `settings_language=popup` và `rule_text=setting_entry`.
- Từ Home, Button `SettingsBtn` mở Settings outgame; `LanguageBtn` hiện, `HowToPlayBtn` ẩn; bấm Language mở page Language thật trong khi Settings vẫn giữ đúng stack.
- Sau khi đóng Settings, Button `StartBtn` mở Game; Game reload timing `game_start` trên cùng runtime.
- Button `SettingsBtn` trong Game mở Settings game-mode; Language ẩn, HTP hiện; bấm `HowToPlayBtn` mở `HowToPlayPaged`, ẩn Settings theo skip-close source và sau khi page hidden vẫn giữ Game/session Playing.
- Không kẹt `IsAnyLoading`, không tạo provider runtime production và không ghi save thật.

## Kết quả

- Unity compile/reload sạch.
- EditMode: **511 passed, 0 failed, 0 skipped, 0 inconclusive**.
- PlayMode: **7 passed, 0 failed, 0 skipped, 0 inconclusive**.
- Không thêm runtime log.

## Còn lại

- Language dropdown outside-click và locale persistence qua restart.
- Settings toggle/Pattern/Restart interaction matrix.
- Previous/Next/Got it của HowToPlayPaged, device-font và pixel/VFX parity.
