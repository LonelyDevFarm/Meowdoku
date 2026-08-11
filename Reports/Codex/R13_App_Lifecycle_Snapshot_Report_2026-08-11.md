# R13 App Lifecycle + Snapshot Durability

Ngày: 2026-08-11  
Unity: 6000.3.19f1

## Phạm vi nguồn đã đối chiếu

- `scripts/module/game/view/game_page.gd`
- `scripts/module/game/view/base_game_page.gd`
- `scripts/module/game_state/game_state.gd`
- `scripts/module/session/session_manager.gd`

## Sai khác đã sửa

- Godot ghi `in_game_sec` vào endgame snapshot và khôi phục clock sau cold resume; Unity trước đó không có trường này.
- Godot hủy debounce timer rồi rebuild toàn bộ snapshot khi focus-out, kể cả lần ghi gần nhất đã hoàn tất. Unity trước đó chỉ flush khi `_snapshotDirty`, nên có thể mất phần elapsed time sau thao tác cuối.
- Unity có thể phát cả `OnApplicationFocus(false)` và `OnApplicationPause(true)` cho cùng một lần xuống nền. Hai callback nay dùng chung một durability boundary và không ghi snapshot lặp.
- `OnApplicationQuit` dùng cùng boundary tương đương `NOTIFICATION_WM_CLOSE_REQUEST`; trạng thái Won/Leaving không được phép tái tạo snapshot đã clear.

## Thay đổi

- `GameSessionSnapshot` round-trip `in_game_sec`, tương thích snapshot cũ thiếu field và clamp giá trị không hợp lệ về 0.
- `GameplayManager` khôi phục elapsed clock từ snapshot, cập nhật elapsed trước Revive/Quit và force-rebuild snapshot tại focus-out/pause/quit.
- Không thay đổi debounce 0,5 giây của MARK hoặc immediate policy của CAT/ERROR; không thêm runtime log.

## Kiểm thử Unity thật

- EditMode: `RESULT passed=513 failed=0 skipped=0 inconclusive=0 duration=64,908`.
- PlayMode: `RESULT passed=10 failed=0 skipped=0 inconclusive=0 duration=137,006`.
- AppScene matrix: Playing với MARK còn debounce → suspend flush → Fail → suspend → Revive → suspend → Win → suspend → Next level 2 → suspend.
- Xác nhận level/lives/marks đúng, elapsed snapshot bằng clock hiện tại, focus-out+pause không tạo lần ghi thứ hai và suspend ở Win không tái tạo snapshot hoàn tất.

## Còn lại

- Hard-kill/cold resume, thời gian background, touch/notch và filesystem behavior phải kiểm tra trên Android/iOS thật ở R17; PlayMode simulation không được dùng để đóng cổng thiết bị.
