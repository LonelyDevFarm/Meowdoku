# R14 — Daily core, state và puzzle selection

Ngày: 2026-08-10  
Unity: 6000.3.19f1

## Đã port

- `DailyEntryStateContract`: unlock level 21, trạng thái Locked/Normal/Done, date key, month key, countdown và done time/top-percent.
- `GameStateData/Service`: `daily_index`, completed/max/started date, elapsed, beat percent và best beat percent; mutation/save giữ thứ tự nguồn.
- `DailyStats`: công thức normal CDF, bộ `mu/sigma` cho size 10/12 rank 3–5, snap 0,1 và clamp 49–99.
- `DcLevelConfig`, `NoDcConfig`, `DcTagUiConfig`; chỉ `dc_level` được source AB manager đăng ký, hai class còn lại được ghi đúng là unregistered.
- `DailyPuzzleSelector`: epoch 21/04/2026, JDN local date, level band mặc định, A/B pool plan, Regular/GC/LK Style, tám rotate/mirror variant, bỏ entry invalid theo vòng và giữ fallback cuối như nguồn.
- `LevelEntry.HasSeed` để phân biệt thiếu key `seed` với seed 0 thật; Daily dùng `id` chỉ khi key seed vắng mặt.
- `DailyGameLaunchRequest` chuyển selection thành tham số source-shaped, giữ date/index/transform/bank/strategy metadata và board đã transform.
- `GameplaySessionMode` phân tách Main/Bank/Daily. Daily dùng chung `GameSession`, Board/input/tool/score/life feedback nhưng coordinator riêng không đụng retry/snapshot/DDA/PreCat/progress Main.
- Daily fail/revive/restart/quit/win giữ lifecycle nguồn; restart dùng lại puzzle cùng ngày, revive tăng đúng `daily.revive_count/rv_count`, win commit date/elapsed/beat idempotent.
- Home có `DailyChallengeEntryPresenter` Locked/Normal/Done và countdown; registry có route riêng cho `DailyGame`, `DailyWin`, `DailyFail`. Result chung đã có nhánh Daily time/beat trong khi visual chuyên biệt còn chờ.

## Kiểm chứng

- Core/Gameplay/Editor/EditModeTests compile sạch bằng Unity Roslyn.
- Fixture bao phủ entry state/date text, countdown biên ngày, stats reference/clamp, pool plan, A/B variants, epoch, entry/transform cycle, invalid fallback, explicit seed 0, launch metadata, state persistence, ranh giới Daily/Main, fail-revive, restart cùng puzzle và win commit một lần.
- Đã giải mã bank chỉ trong bộ nhớ bằng cùng XOR key để kiểm kê: Regular 10×10 rank 3/4 có dữ liệu; GC 10×10 và 12×12 có rank 3–5; LK Style 12×12 rank 4 tier N có 429 puzzle.
- Unity Auto Refresh đã tạo meta; Editor log chưa có compile exception liên quan.

## Còn lại

- Unity Refresh để sinh meta/cập nhật Home prefab và registry từ installer, sau đó PlayMode toàn vòng Home → Daily → Fail/Revive/Restart/Win → Main.
- Dựng Daily header/timer và visual Win/Fail chuyên biệt trong nhánh prefab dùng chung.
- Port rollover/focus/ad time compensation, settle stats và Streak/Award queue.
- Chạy Unity Test Runner và PlayMode qua nhiều ngày/level band/config override.

## Cập nhật 2026-08-11 — Clock, Streak và Award

- Thêm `ClockTicker` scene-owned, căn tick đúng công thức nguồn, local date key và không phát catch-up burst sau pause/focus.
- Port `StreakData`, repository riêng, merge, check-in chu kỳ 7 ngày, reward chest, resume/backfill/protect, pending-win crash recovery, day watch và group-switch state.
- Port Home Streak entry cùng các page Main/Lit/Settle, Resume, Backfill; flow thắng mặc định giữ thứ tự delay/toast → revive → Streak → Award → Result.
- Port `AwardManager` transaction in-flight, cold-start sweep, direct/streak/rank boundary, double tool và Award Collect page. Không gộp nhầm `pending_rewards/history` của rewarded-ad restore vào AwardManager.
- Thêm `DailyMetaRuntime` tại `App/Systems`, inject qua `UIManager`, giữ event/ticker lifecycle và route Award bằng serialized composition.
- Installer tạo bốn prefab/route `Streak`, `StreakResume`, `StreakBackfill`, `Award` và nâng cấp slot Streak của Home; không sửa scene/prefab YAML.
- Home popup queue đã dùng priority/config JSON nguồn, port `ab_switch_popup` theo Daily Streak switch occurrence, trình bày reward tool và cleanup bằng `Abort` khi Unity dừng coroutine. Rank/rewarded-ad handler chưa được dựng giả trước module phụ thuộc.
- Installer bổ sung prefab/route `AbSwitchPopup` và serialized config cho Home; không sửa scene/prefab YAML.
- Core, Gameplay, Editor và EditModeTests compile sạch bằng Unity Roslyn. Reflection regression Daily Streak/Award/Popup đạt 25/25.
- Còn chờ Unity refresh để chạy installer thật, Unity Test Runner và PlayMode trực quan; restore/double qua quảng cáo giữ disabled tới khi có adapter R16.
