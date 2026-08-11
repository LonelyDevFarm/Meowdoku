# R16 Tracker/Session Core Report — 2026-08-11

## Phạm vi đã port

- Đối chiếu trực tiếp `tracker.gd`, `session_manager.gd`, `ui_tracker_observer.gd` và lifecycle show/hide trong `ui_manager.gd`.
- Port catalog event, screen, dialog, game type/status/result và user property với đúng chuỗi key nguồn.
- Port source stack cho screen/dialog/button, game ID, round stats, restart reset, ad-show ID, rank/frame events và question-transform encoding.
- Port session active-time dùng monotonic clock, flush 60 giây và tạo session mới khi thời gian background lớn hơn 1.800 giây.
- Lưu GRT level/event dedup vào `GameStateData` để giữ idempotency qua restart.
- Thêm `TrackingRuntime` scene-owned và `NullTrackingSink`; không ghi log rác và không phụ thuộc SDK online.
- Port `UITrackerObserver`: track sau khi window show thành công, pop dialog source stack ở ranh giới close trước `OnHide`.
- Nối metadata chính xác cho Splash, Home, Main/Daily Game, Main/Daily Win/Fail, Settings/Options, Language, Profile, Streak/Game Streak và Rank Activity.
- Nối Language confirm/cancel + `ui_language`, Settings switch/button và dropdown `language_picker_dlg` lồng theo đúng push/pop source stack.
- Nối Rank Award hai pha `challenge_reward_dlg → challenge_reward_get_dlg`, gồm Collect event và close-stack tại đúng pha.
- Nối Main/Daily game lifecycle: `SetActiveGameType`, New/Continue/Restart, game ID, `game_start`, Fail/Win/Restart `game_end` và thứ tự restart giống `LevelOps.gd`.
- Qid dùng đúng mapping rank/tier H (`R4H→5`, `R5→6`, `R5H→7`), bank index/transform; end payload dùng counters được transition chụp trước khi load bàn mới.
- Nối đúng thứ tự nguồn cho Hint/Locate/Clear, Hint Apply/Stop/Detail, một stat step cho mỗi gesture có thay đổi, Locate và Hint Apply; đồng thời port `erase_count`, `hint_cross_count`, `invalid_sign_total` và `gamedie_count`.
- `game_end.step_used` lấy `Tracker.step_used` tích lũy như Godot, không dùng số history còn lại sau Undo.
- Nối button event hiện có của Home, Profile, Streak, Rank Activity và Main/Daily Win/Fail; Streak Lit phát `game_streak_scr` đúng lúc chuyển sang Settle.
- Port catalog/payload ad timing/interstitial/rewarded cùng placement/position/prop-source chính xác; tiêu Hint/Locate phát `prop_use` sau decrement và Award tool phát `prop_get` sau inventory mutation.
- AwardManager nhận Tracker qua cây serialized `DailyMetaRuntime → UIManager → TrackingRuntime`, không thêm lookup hay cạnh hierarchy chéo.
- `AppRuntimeSceneInstaller` thêm/nâng cấp `TrackingRuntime` và serialized reference của `UIManager` bằng Unity API, không sửa YAML thủ công.

## Xác minh

- `Meowdoku.Core`: compile sạch.
- `Meowdoku.Gameplay`: compile sạch.
- `Meowdoku.Editor`: compile sạch.
- `Meowdoku.EditModeTests`: compile sạch.
- Regression runner: **58 passed, 0 failed**.
- Unity refresh named-event: **`REFRESH_SIGNAL_SENT`**.

## Còn lại trong R16

- Chưa gắn SDK analytics/ads/auth/cloud/push; runtime hiện cố ý dùng no-op sink.
- Coordinate button chưa có UI tương ứng; ad provider/lifecycle, consent gate và online failure policy chưa nối.
- Chưa chạy PlayMode focus/pause/source-attribution và device app-kill tests.
