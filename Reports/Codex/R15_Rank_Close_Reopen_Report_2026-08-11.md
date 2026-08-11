# R15 Rank Close and Reopen Report — 2026-08-11

## Nguồn đối chiếu

- `scripts/module/home/view/home_page.gd`
- `scripts/module/rank_activity/view/rank_activity_open_popup_page.gd`
- `scripts/module/rank_activity/rank_activity_manager.gd`
- `scripts/module/rank_activity/model/rank_activity_config.gd`

## Contract nguồn

- Nút Action đặt `started=true`; nút Close chỉ đóng popup và giữ `started=false`.
- Sau khi popup ẩn, Home luôn gọi `confirm_participation()` dù người chơi dùng Action hay Close.
- Ở kỳ đầu, identity mặc định đi qua Profile guide rồi vào Main Game; điều kiện `started` chỉ quyết định route ở kỳ sau.
- Kỳ không thưởng đã fold về `NOT_OPENED` chỉ mở lại sau đủ 10 Main win, trừ nhánh new-session riêng; awarded period có luật at-home riêng.

## Kiểm tra bổ sung

- AppScene bật `leaderboard_func`, mở kỳ đầu ở level 21 và mở popup thật từ Home queue/entry.
- Bấm `CloseBtn`; `RankActivityOpenPopupPresenter.WasStarted` vẫn false trước và sau close.
- Sau close animation, manager chuyển `OpenNotJoined → OpenJoined`; Profile guide được đóng nếu xuất hiện, rồi Game thật mở level 21 và session Playing.
- Fixture manager kết thúc một kỳ group 3 không thưởng, gọi chín vòng `NotifyLevelStart/NotifyLevelWin` vẫn giữ `NotOpened`, vòng thứ 10 mới tạo period 2 `OpenNotJoined` và reset `WinsSinceClose`.

## Kết quả

- Không phát hiện sai khác production trong phạm vi close/reopen; chỉ bổ sung regression.
- Unity compile/reload sạch.
- EditMode: **515 passed, 0 failed, 0 skipped, 0 inconclusive** (`64,712 s`).
- PlayMode: **13 passed, 0 failed, 0 skipped, 0 inconclusive** (`197,589 s`).
- Không thêm runtime log.

## Còn lại

- Popup kỳ sau: Close chỉ join/ở Home, Action mới vào Game.
- Reward page → claim → try-open kỳ kế tiếp.
- Leaderboard scroll/countdown, expiry presentation và change animation trên hai aspect.
