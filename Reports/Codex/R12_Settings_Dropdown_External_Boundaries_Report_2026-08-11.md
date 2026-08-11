# R12 Settings Dropdown + External Boundaries

Ngày: 2026-08-11  
Unity: 6000.3.19f1

## Phạm vi nguồn đã đối chiếu

- `scripts/module/setting/view/language_switch_widget.gd`
- `scripts/module/language/view/language_page.gd`
- `scripts/module/setting/view/setting_page.gd`
- `scripts/module/splash/view/privacy_dialog.gd`
- `scripts/module/feedback/view/feedback_page.gd`

## Thay đổi

- Language dropdown dùng Graphic blocker bắt pointer-down; không còn Button chờ release. Press option rồi release ngoài vẫn không chọn và dropdown giữ nguyên như nguồn.
- System locale được canonicalize, hiển thị native name; chọn System apply vào catalog, persist `AppliedLocale` rồi đóng Settings.
- Restart game-mode giữ `_restartConsumed`, callback chạy tối đa một lần dù hai click được dispatch liên tiếp.
- Thêm `ISettingsExternalServices`/consumer boundary cho trạng thái online, CMP required, FAQ, CMP UI và localized Terms/Privacy URL.
- `AppBootstrap` đưa cùng external adapter vào `UIManager`; page cache mới/cũ đều được bind lại. Khi không có SDK, offline fallback không chặn startup, Feedback dừng ở network toast, CMP ẩn và URL mặc định vẫn mở được.
- Settings prefab được nâng cấp bằng Unity installer; không sửa YAML bằng suy đoán và không thêm runtime log.

## Kiểm thử Unity thật

- Compile/import: Core, Gameplay, Editor, EditMode và PlayMode assembly cập nhật không có C# hoặc installer exception.
- PlayMode: `RESULT passed=9 failed=0 skipped=0 inconclusive=0 duration=127,164`.
- EditMode: `RESULT passed=512 failed=0 skipped=0 inconclusive=0 duration=64,724`.
- AppScene xác nhận popup Language route cũ vẫn hoạt động; dropdown `vi_VN` đóng bằng outside pointer-down, System option persist locale; offline Feedback không gọi provider, online gọi đúng một lần; CMP/Terms/Privacy gọi đúng boundary.
- Game Settings double Restart chỉ tăng `RestartCount` một lần, đóng page, tải lại cùng level rồi vẫn mở How-to-play bình thường.
- Composition fixture xác nhận `OutsideBlocker` là Graphic raycast và không có Button.

## Còn lại

- Production Helpshift/CMP/localized-URL SDK adapter vẫn là integration R16; boundary đã sẵn sàng và offline path đã khóa.
- Cold restart theo locale, glyph thiết bị và pixel parity để ở vòng device/polish.
