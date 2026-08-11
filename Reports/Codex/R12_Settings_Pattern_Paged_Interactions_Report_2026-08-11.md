# R12 Settings Pattern + Paged How-to-play

Ngày: 2026-08-11  
Unity: 6000.3.19f1

## Phạm vi nguồn đã đối chiếu

- `scripts/module/game/view/base_game_page.gd`
- `scripts/module/gameplay/view/board_view.gd`
- `scripts/module/gameplay/view/cell_view.gd`
- `scripts/module/setting/view/setting_page.gd`
- `scripts/module/how_to_play/view/how_to_play_paged_page.gd`

## Thay đổi

- Port đủ 12 pattern theo đúng thứ tự sprite và màu của `BoardView` Godot.
- `CellView` chỉ hiện pattern khi mode bật và ô trống; `blind_mod=2` có thể giữ pattern trên ô đã điền.
- `GameplayManager` áp dụng config dùng chung sau mỗi lần dựng board và ngay khi Settings đổi toggle.
- Mở game Settings dismiss `pattern_entry_dot`; bấm Pattern dismiss `pattern_switch_dot` đúng hai call site nguồn.
- `HowToPlayPagedPagePresenter.Closed` phát đúng một lần trước `UIManager.Hide`, kể cả Got it, close button hoặc Back.
- Installer tạo `Pattern` trong Cell và serialize đủ palette vào cả GameplayScene lẫn GamePage; không sửa YAML bằng suy đoán.

## Kiểm thử Unity thật

- Compile: Tundra build success, không có C# error mới.
- EditMode: `RESULT passed=512 failed=0 skipped=0 inconclusive=0 duration=52,684`.
- PlayMode: `RESULT passed=9 failed=0 skipped=0 inconclusive=0 duration=121,747`.
- Case AppScene bấm Sound/Vibration/People/Pattern, kiểm tra pattern hiện ở ô trống và ẩn ở ô đã điền với `blind_mod=1`, rồi bấm Previous/Next/Got it và xác nhận signal phát trước close animation.

## Còn lại

- `blind_mod=2` đã có code path nhưng vẫn cần visual/device check.
- Red-dot trực quan trên nút Settings entry, Language dropdown theo locale thiết bị, Restart/Feedback/CMP và pixel/VFX parity chưa đóng.
