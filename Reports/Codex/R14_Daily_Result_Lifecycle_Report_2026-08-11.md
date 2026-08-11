# R14 Daily Result Lifecycle Report — 2026-08-11

## Nguồn đối chiếu

- `scripts/module/daily/view/daily_game_page.gd`
- `scripts/module/daily/view/daily_fail_page.gd`
- `scripts/module/daily/view/daily_win_page.gd`
- `scripts/module/game/view/game_page.gd`

## Sai lệch phát hiện

1. Godot chỉ gọi `RankActivityManager.notify_level_start/restart/exit/win` và `set_level_collect` trong Main `game_page.gd`; `daily_game_page.gd` không tham gia Rank. Unity dùng chung `GameplayManager` nhưng chưa gate theo session mode, nên Daily có thể xóa Rank cache khi Restart hoặc cộng Rank khi Win.
2. Godot Daily Continue gọi show `GAME`, sau đó hide `DAILY_WIN` và `DAILY_GAME`. Unity trước đó gọi `ContinueToNextLevel()` trên manager thuộc DailyGame; session đổi sang Main nhưng vẫn nằm trong instance/page DailyGame.

## Thay đổi

- Gate bốn lifecycle call Rank ở start/restart/exit/win để `GameplaySessionMode.Daily` không chạm Rank state.
- `GameWinPagePresenter` xử lý Daily Continue bằng `UIManager.Show(Game)` với Main level hiện tại và tracker status Continue, rồi đóng `DailyWin`/`DailyGame`.
- Giữ nhánh Main/Bank `ContinueToNextLevel()` hiện hành không đổi.
- Thêm test seams dưới `UNITY_INCLUDE_TESTS` cho Daily date/index và Rank level-cache/in-level; không vào player build.
- Tổng quát helper PlayMode Fail/result để dùng đúng `DailyFail`, `DailyGame` và `DailyWin` thay vì giả lập bằng page Main.
- Không thêm runtime log.

## AppScene PlayMode matrix

- Bật provider test cho rewarded ad và Home leaderboard; mở Rank, confirm participation rồi đặt Rank cache sentinel 17.
- Đặt Main sentinel cho level 21, strategy, fail count, retry puzzle, endgame snapshot và Main stats trước khi mở Daily.
- Daily launch giữ Rank ở ngoài level lifecycle và không thay sentinel Main.
- Ba wrong guess mở `DailyFail`; rewarded callback đúng position `daily_game_fail` hồi một mạng và chỉ tăng `daily.revive_count`.
- Wrong guess tiếp theo Fail; Restart giữ nguyên Daily date/index/size/solution, hồi ba mạng và không xóa Rank cache.
- AutoComplete mở `DailyWin`, lưu Daily completion/elapsed/beat, không advance Main và không commit Rank collect.
- Sau input gate 2 giây, Continue đóng `DailyWin` + `DailyGame`, mở instance `Game` khác ở Main level 21 và không kẹt loading.

## Kết quả

- Unity compile/reload sạch.
- EditMode: **514 passed, 0 failed, 0 skipped, 0 inconclusive** (`64,785 s`).
- PlayMode: **12 passed, 0 failed, 0 skipped, 0 inconclusive** (`182,963 s`).

## Còn lại

- Daily rollover/focus/timezone và ad-time compensation trên thiết bị.
- Streak nhiều ngày, rewarded restore và settle-reorder variant.
- Tracker sink-capture cho full Daily payload/order.
- Spine/VFX/pixel parity của Daily Win/Fail.
