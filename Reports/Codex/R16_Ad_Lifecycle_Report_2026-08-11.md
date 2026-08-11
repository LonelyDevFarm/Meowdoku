# R16 Ad Lifecycle Report — 2026-08-11

## Đã port

- Đối chiếu trực tiếp `scripts/common/unikit_manager.gd` và các call site rewarded-ad của Base Game, Main/Daily Fail, Streak Revive và SoundManager.
- Thêm `IAdProvider`, `NullAdProvider`, `AdService` và scene-owned `AdRuntime`; không tạo SDK giả, singleton hoặc runtime lookup.
- Giữ riêng callback `shown`, `rewarded`, `closed`, `error`, `impression`; đóng quảng cáo không đồng nghĩa nhận thưởng.
- Readiness ghi `ad_show_timing` và giữ show-id/position; chỉ impression thật mới phát `interstitial_ad_show` hoặc `rewarded_ad_show`.
- Rewarded request là one-flight, completion đúng một lần; dispose tháo toàn bộ provider callback và hủy request đang chờ an toàn.
- Nối Hint/Locate rewarded flow tới `AwardManager`, giữ `rv_count` và `prop_get` đúng sau callback rewarded.
- Nối Main/Daily Fail revive và Streak Resume/Backfill với đúng ad position nguồn.
- Nối `shown/closed` tới SoundService; Daily reward-ad tạm dừng gameplay clock và chỉ tiếp tục nếu session còn chơi.
- `AppRuntimeSceneInstaller` tạo `AdRuntime` tại `App/Systems` và serialize `UIManager.adRuntime` bằng Unity API.
- Port `inter_unlock_level=11`, `inter_unlock_session=2`, `inter_unlock_memory=300`, `inter_cd_lc={60}` và `inter_extra_protect_lc={session_game_2}`.
- Port đúng thứ tự gate interstitial, persisted unlock và session reward-view probability 80%; entry board/input chờ close/error/focus như source Main flow.
- Port `IAbRuntimeProvider`, `AbConfigService` và scene-owned `AbConfigRuntime`; init/remote-ready/params-updated, timeout fallback và dye theo timing giữ đúng ranh giới nguồn nhưng không giả lập backend.
- Port `living_days={0,2},{2,4},{4,7},{7,inf}`, persisted `first_open_time_ms` và cách tính ngày lịch địa phương; config segment chỉ chọn theo LivingDays khi số phần tử khớp, nếu không dùng phần tử đầu đúng source.
- Interstitial, banner và reward-restore dùng chung `AdConfigSet` được reload tại `game_start`; không còn tạo config default rời ở mỗi lần đánh giá policy.

## Xác minh

- Core, Gameplay, Editor và EditModeTests compile sạch bằng Unity Roslyn.
- Regression runner: **90 passed, 0 failed**.
- Unity Editor đã compile lại bốn assembly lúc 08:20 và serialize `AbConfigRuntime` cùng reference vào AppScene lúc 08:23.
- Installer upgrade đã chuyển từ preview scene không thể save sang additive scene; lần chạy lại không còn C#/installer exception.
- Unity refresh bridge: `REFRESH_SIGNAL_SENT`.

## Còn lại

- SDK/provider quảng cáo production chưa có; null provider chủ động trả unavailable và không cấp thưởng.
- Banner lifecycle đã port theo gate session/level/protection/size và đúng điểm show/destroy của Gameplay.
- Reward-restore đã port watchdog 30 giây, callback muộn, pending/history persistence, anti-abuse, newest-first, popup Home và collect/close cleanup.
- Production remote A/B SDK adapter chưa có; runtime mặc định dùng provider offline và giữ default chính xác của nguồn.
- Cần PlayMode với fake provider kiểm tra UI disable/re-enable và callback sau hide; device test chờ provider thật.
