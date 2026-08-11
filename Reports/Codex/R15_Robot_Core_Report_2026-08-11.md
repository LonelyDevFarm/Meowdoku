# R15 — Robot core, cache và runtime boundary

Ngày: 2026-08-11  
Unity: 6000.3.19f1

## Đã port từ source

- `RobotConfig`, `RobotData`, `RobotTimelinePoint`, `RobotPool`, `RankInfo` và source-shaped dictionary serialization.
- `RobotScoreGenerator`: rank alpha, floor/ceiling, random-power bot score, `closest_approach`, `fill_to_zero`, weighted timeline, first-hour forcing, cooldown và minimum-scoring.
- `RobotIdentity`: random nickname/avatar/frame, top 1–3 cùng nhóm bổ sung, first-place frame và badge theo open period.
- `RobotRanking`: điểm theo elapsed minute, timestamp lần ghi điểm cuối, score-desc/timestamp-asc tie-break và chuyển robot thành `PlayerInfo`.
- `RobotStalking` và `RobotService`: kích hoạt khi player vượt `x_base`, freeze future timeline, top-pool capacity, catch-up theo gap/delta-time, overtake guard, create/get/discard/reset và effective-time không lùi.
- `RobotRepository` giữ mỗi pool là một section có key `data` trong `robots.cfg`, dùng `SaveStore` atomic hiện có thay cho bytes `ConfigFile` của Godot.
- `RobotRuntime` là composition boundary scene-owned trong `App/Systems`; chưa tạo singleton/global state.
- Chuyển nguyên 1.699 nickname từ `robot_nickname_pool.gd`; checksum UTF-8 nối bằng newline là `f864d2094a2587d4c030371373120fd44c693061edde7af3c54292d3dac7e4fd`.
- Không port `debug_dump`/warning chẩn đoán của source để tránh log runtime không cần thiết.

## Kiểm chứng

- Core, Gameplay, Editor và EditModeTests compile sạch bằng Unity Roslyn response files.
- Reflection regression đạt 39/39.
- 8 case Robot bao phủ catalog checksum, model round-trip, bốn rank band, floor/ceiling, score overshoot, timeline cooldown, timestamp tie-break, create/persist/rank mapping, end-time/clock rollback và stalking freeze/catch-up.

## Còn lại

- Unity bridge chưa sẵn sàng vì script bridge chưa được Editor import; cần một lần Refresh thủ công rồi mới có thể tự phát tín hiệu cho các lượt sau.
- Chạy Unity Test Runner và PlayMode restart/time rollback để xác nhận file thật cùng lifecycle Editor/device.
- Port `RankActivityManager`, period/points/reward/promotion-demotion rồi dùng RobotService làm leaderboard offline đúng source.
- Rank pages/Home entry/popup queue và reward → AwardManager thuộc phần tiếp theo của R15.
