# R12 Home Meta Entry Availability Report — 2026-08-11

## Nguồn đối chiếu

- `scripts/module/home/view/home_page.gd`
- `scripts/module/daily/view/daily_challenge_entry_cell.gd`
- `scripts/module/daily_streak/view/streak_entry_cell.gd`
- `scripts/module/rank_activity/view/rank_activity_entry_cell.gd`
- `scripts/module/abtest/config/daily_streak_config.gd`
- `scripts/module/abtest/config/leaderboard_func_config.gd`
- `scripts/module/abtest/config/hard_button_config.gd`

## Sai lệch phát hiện

Godot giữ ba config Home trong A/B manager dùng chung. Unity đã có presenter và runtime thật cho Daily, Streak, Rank và Profile, nhưng mỗi consumer tự giữ một config local mặc định; `AbConfigRuntime` chỉ catalog hóa Ads/Settings. Vì vậy provider production không thể bật/tắt các entry Home một cách thống nhất dù prefab và route đã tồn tại.

## Thay đổi

- Thêm `HomeConfigSet` gồm `DailyStreak`, `Leaderboard` và `HardButton` vào catalog chung của `AbConfigRuntime`.
- `HomePagePresenter` đọc config hiện hành từ runtime được inject, vẫn giữ fallback default nguồn khi chạy độc lập.
- `DailyMetaRuntime` bind cùng Daily/Streak config; `StreakFeature` cho phép nhận instance dùng chung thay vì cố định config local lúc khởi tạo.
- `RankActivityRuntime` đọc động `leaderboard_func` từ cùng runtime; không cache variant cũ sau reload.
- `UIManager` nối runtime A/B cho Daily và Rank trước khi tạo page.
- Không thêm singleton, lookup hot-path, page giả hoặc runtime log.

## AppScene PlayMode

- Test provider chỉ tồn tại trong test assembly và bật `daily_streak=Basic`, `leaderboard_func=CatsProp`, `hard_button=Default`.
- Ở level 1, Daily vẫn hiện trạng thái khóa nhưng click không tạo/mở `DailyGame`; Streak mở page thật rồi Back về Home; Rank ẩn dưới unlock level 11; Profile hiện theo config leaderboard.
- Ở level 21, Daily mở `DailyGame`, session ở `GameplaySessionMode.Daily`, sau đó Back về Home.
- Rank hiện hoặc được Home popup queue mở, Action xác nhận participation, đi qua Profile guide khi cần rồi vào Game; `RankActivityManager.IsJoined` được commit.

## Kết quả

- Unity compile/reload sạch.
- EditMode: **514 passed, 0 failed, 0 skipped, 0 inconclusive** (`63,716 s`).
- PlayMode: **11 passed, 0 failed, 0 skipped, 0 inconclusive** (`154,417 s`).
- Không thêm runtime log.

## Còn lại

- Daily fail/revive/restart/win nhiều vòng và rollover ngày.
- Streak lịch nhiều ngày/rewarded provider thật.
- Rank close-popup confirm, reopen sau 10 win, leaderboard scroll/countdown/change animation.
- Pixel/animation parity Home ở 1080×1920 và 1080×2400; device touch/notch/profiler thuộc R17.
