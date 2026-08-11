# R15 Rank Expiry, Reward and Next Period Report — 2026-08-11

## Nguồn đối chiếu

- `scripts/module/rank_activity/rank_activity_manager.gd`
- `scripts/module/rank_activity/core/rank_activity_period.gd`
- `scripts/module/game/view/game_page.gd`
- `scripts/module/home/view/home_page.gd`
- `scripts/module/rank_activity/view/rank_activity_change_page.gd`
- `scripts/module/rank_activity/view/rank_activity_page.gd`
- `scripts/module/rank_activity/view/rank_activity_open_popup_page.gd`

## Contract nguồn

- Khi period hết trong lúc Main đang chạy, manager chuyển sang `SETTLING` nhưng chưa settle vì `_in_level=true`.
- Main Win commit level cache rồi mới chốt hạng; presentation đợi collection flight, mở Rank Change và chỉ cho dismiss sau animation.
- Reward trong Game gọi `claim_reward(false)`: Rank Gift đi qua durable Award transaction và hai pha podium/rương → item.
- Khi Award kết thúc, kỳ cũ fold về `NOT_OPENED`. Vì `at_home=false`, period có thưởng không được mở kỳ tiếp theo ngay trong Game.
- Khi người chơi thực sự quay lại Home, `on_home_shown()` mở period kế tiếp. Ở kỳ sau, Close popup vẫn confirm participation nhưng không tự vào Game vì `was_started=false`.

## Kiểm tra AppScene

1. Dùng Rank/Robot/Profile/Award store chỉ sống trong bộ nhớ; không đọc hoặc ghi Rank/Profile save của máy.
2. Mở và join period 1 ở level 21, vào Main thật và xác nhận manager đang ở lifecycle level.
3. Tiến clock vượt 86.400 giây: state thành `Settling`, chưa có pending reward và vẫn giữ `_in_level`.
4. Auto-complete bàn thật; chờ collection flight phát Win settlement, xác nhận hạng 1 và pending reward.
5. Rank Change hiện trước, `TapToContinue` chỉ bật sau animation; dismiss xong mới tới Award.
6. Rank Gift cần hai lần Collect: pha podium/rương rồi pha item. Sau đó cộng đúng +1 leaderboard Frame, +2 Hint, +2 Locate và xóa `in_flight_awards`.
7. Manager vẫn `NotOpened`, period count vẫn 1 trong Game; Win mới xuất hiện.
8. Next sang level 22 rồi Back về Home; lúc này period 2 mới mở và popup xuất hiện.
9. Close popup period 2 giữ `WasStarted=false`, manager vẫn confirm join, Home vẫn là page hiện tại và UI không còn loading.

## Kết quả

- Không phát hiện sai lệch production trong phạm vi expiry/reward/next-period; thay đổi mã chỉ bổ sung regression PlayMode và fixture cô lập.
- Unity compile/domain reload sạch.
- EditMode gần nhất: **515 passed, 0 failed, 0 skipped, 0 inconclusive** (`60,751 s`).
- PlayMode: **16 passed, 0 failed, 0 skipped, 0 inconclusive** (`237,741 s`).
- Không thêm runtime log.

## Còn lại

- Leaderboard scroll/countdown và Change rise/scroll visual ở 1080×1920/1080×2400.
- Frame-only Rank Gift presentation trong AppScene.
- Chest Spine, celebration particle, frame-fly VFX và pixel parity.
- Process restart/time rollback, hard-kill và device lifecycle soak.
