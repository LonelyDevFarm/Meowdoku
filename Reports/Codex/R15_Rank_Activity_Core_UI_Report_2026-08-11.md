# R15 — Rank Activity core, UI và reward flow

## Kết quả

Đã port lát cắt Rank Activity offline từ manager đến UI/reward dựa trực tiếp trên Godot source. Code không dùng singleton mới, không thêm log debug và mọi prefab mới được dựng bằng Unity Editor API.

## Phạm vi đã port

- `RankActivityManager`: ba group, period 24 giờ, điều kiện mở/mở lại, participation, level cache, điểm/rank, expiry, settlement, encouragement và reward.
- `RankActivityRuntime` dùng `RobotRuntime`, `ProfileRuntime`, `DailyMetaRuntime/AwardManager` làm dependency scene-owned.
- Home entry, open popup, popup queue/reward-first flow và first-period Profile guide.
- Leaderboard page: countdown, top 3, pooled list row, self/profile route, info/HTP và CTA.
- Change page: appear stagger, score roll, rank settle, encouragement, scroll/input gate và lifecycle cleanup DOTween.
- Gameplay hooks start/win/restart/exit và post-win/Continue Rank flow.
- Rank HTP cho Cat/Fish và full/frame-only group.
- Rank Gift hai pha: win count + top-3 podium + chest/OK, rồi reward items; frame item không còn render trống và frame-only hoàn tất riêng.

## Các điểm parity đã sửa

- Chest mapping nguồn: hạng 1 → tier 3, hạng 2 → tier 2, hạng 3 → tier 1.
- Chuỗi `RANK_OPEN_DESC*` và `RANK_ENCOURAGE_*` dùng localization nguồn; BBCode/`[img]` Godot được chuyển sang plain text cho UGUI.
- Restart/exit gọi Rank manager trước khi publish UI transition giống `game_page.gd`.
- Rank reward giữ transaction in-flight/idempotent của `AwardManager`; frame đi qua `ProfileRuntime`.

## Kiểm tra

- Unity Roslyn compile sạch: Core, Gameplay, Editor và EditModeTests.
- Reflection regression: **48 passed, 0 failed**.
- Các case Rank bao phủ config/reward table, BBCode adapter, period rules, data round-trip, first open, win-only commit, expiry/settlement, no-reward fold, disabled reset và interrupted claim.

## Còn chờ Unity/PlayMode

- Một lần Manual Refresh để Unity import bridge/file mới và sinh `RankActivityPage`, `RankActivityHowToPlay`, `RankActivityChange`, nâng cấp `AwardPage` và registry.
- Test full flow open → join → nhiều win → change → settle → Rank Gift → next period.
- Visual parity 1080×1920/2400, scroll centering/rise và chest/celebration/frame-fly Spine/particle adapter.
