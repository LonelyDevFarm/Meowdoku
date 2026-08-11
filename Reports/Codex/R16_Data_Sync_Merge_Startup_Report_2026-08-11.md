# R16 Data Sync, Merge and Startup Report — 2026-08-11

## Phạm vi

Port lát cắt Data Sync từ nguồn Godot sang Unity: registry/savable, merge, remote snapshot, HTTP API, Auth-token retry, upload conflict và startup boundary.

Nguồn đối chiếu chính:

- `scripts/module/data_sync/data_sync_manager.gd`
- `scripts/module/data_sync/core/data_sync_config.gd`
- `scripts/module/data_sync/core/data_sync_registry.gd`
- `scripts/module/data_sync/core/data_sync_api.gd`
- `scripts/core/net/http_client.gd`, `api_config.gd`
- remote contract của GameState, Profile, Streak và Rank Activity
- `launcher.gd`, `home_page.gd` và test merge Godot

## Kết quả

- `DataSyncRegistry` giữ đăng ký idempotent, mark-synced và late savable callback.
- `GameStateService`, `ProfileService`, `StreakFeature` và `RankActivityManager` cùng thực thi `IDataSyncSavable` với ID/field/merge rule đúng nguồn.
- `DataSyncService` giữ remote snapshot baseline, unknown root/block field, shared `remote_ahead`, malformed-body protection và empty-body semantics.
- Luồng sync giữ first upload, cached meta fast-path, no-local-change skip, sync-code tăng, token refresh và tối đa ba conflict retry.
- Runtime coalesce request khi một sync đang chạy; trigger đúng startup, level won/failed, profile save, streak revive và late savable.
- HTTP adapter dùng UnityWebRequest nhưng giữ raw-body MD5 signature, timestamp, source headers, Authorization, server `Date` calibration/retry và timeout 10 giây.
- Request được abort khi runtime/API disable hoặc destroy; không thêm runtime log.
- AppBootstrap chờ Data Sync tối đa 2 giây khi Auth provider tồn tại; thiếu provider/SDK không chặn offline startup.
- Home chỉ refresh progress khi sync thành công với `changed=true`, tương đương nguồn.
- AppScene installer tạo/nâng cấp `DataSyncHttpApi` và `DataSyncRuntime` dưới `App/Systems`, serialize Auth/API/meta/profile/rank/bootstrap/UIManager references qua Unity API.
- Remote snapshot và development sync switch round-trip qua verified file theo logical schema nguồn.

## Automation fix

Ba bridge Refresh/EditMode/PlayMode trước đây có thể giữ `_pending=true` vĩnh viễn nếu callback `delayCall` chạy trong lúc compile/import. Các bridge nay poll pending ở mỗi Editor update và thực thi ngay khi Editor rảnh; domain reload không còn buộc người dùng bấm tay.

## Kiểm thử

- EditMode: **547 passed, 0 failed, 0 skipped, 0 inconclusive**, 125,636 giây.
- PlayMode: **17 passed, 0 failed, 0 skipped, 0 inconclusive**, 260,772 giây.
- Matrix mới bao phủ signing fixture, registry late/idempotent, unknown passthrough, parse failure baseline, GameState remote merge/tool signal, first upload, cached meta skip, remote merge, conflict retry, token refresh và file-backed repository round-trip.
- Log cuối không có C# error, exception hoặc Data Sync runtime log.

## Chuyển thể Unity

- Godot static registry/autoload coroutine được tách thành scene-owned `DataSyncRuntime`, core `DataSyncService` và interface có kiểu để giữ lifecycle/cancellation rõ ràng.
- Godot `HTTPRequest` được chuyển thành UnityWebRequest; wire contract và retry policy không đổi.
- Godot ConfigFile được lưu bằng verified `SaveStore`; logical section/key giữ nguyên nhưng file không binary-compatible với Godot.

## Còn lại

- Native UniKit metadata/Auth provider cho online state, version/country/learnings ID/LUID/device ID.
- Backend staging/production integration và callback thực.
- Thiết bị thật: offline chuyển trạng thái, clock skew, background/app-kill và iOS/Android network policy.

