# R13 Win Toast Runtime Closure — 2026-08-13

## Kết quả

Đã kiểm chứng runtime thật của Win Toast thay vì chỉ gọi presenter trên bàn không đủ điều kiện.

- Full EditMode: **679/679**, 0 fail, 0 skip, 149,062 giây.
- Platform PlayMode: **19/19**, 0 fail, 0 skip, 208,232 giây.
- Không thêm runtime log, network/SDK hoặc thay đổi ngoài workspace.

## Đối chiếu nguồn

Nguồn trực tiếp:

- `scripts/module/game/view/base_game_page.gd::_play_win_toast_and_wait`
- `scripts/module/game/view/base_game_page.gd::_maybe_show_win_toast`
- `scripts/module/game/view/game_win_toast.gd`
- `scripts/module/game/view/game_page.gd::on_show`
- `scripts/module/abtest/abtest_manager.gd::dye_at_game_start`
- `scripts/module/abtest/config/win_toast_config.gd`

Nguồn chỉ phân tier cho bàn size 6–12. Nếu toast hiện, animation bắt đầu đóng ở 1,3 giây và result tiếp tục sau 1,5 giây; nếu không hiện, Win dùng delay 1,2 giây.

## Sai lệch đã phát hiện và sửa

Test cũ đặt `win_toast=P20` nhưng chơi level 1 bàn 4×4. Nó chỉ xác nhận `TryShow` được gọi, vì vậy xanh dù toast không thể hiện theo threshold nguồn.

Test AppScene mới dùng level 11 bàn 6×6 và thắng hoàn hảo. Lượt chạy đầu phát hiện Unity giữ cùng Game page khi bấm Next nên `OnShow` không chạy lại; cấu hình P20 bị giữ sang level 12. Thử reload trong callback presentation lại quá muộn vì `GameplayManager` đã chọn puzzle và xử lý PreCat trước đó.

Bản sửa thêm `SessionLoadPreparing` ngay sau khi xác định mode nhưng trước khi bất kỳ config nào được tiêu thụ. `GameplayPagePresenter` dye các timing `game_start`, normal/daily và level 11/21 tại mốc này cho entry, Next và Restart. Callback presentation chỉ còn cập nhật UI sau khi session đã dựng xong.

Chuỗi Win Toast nguồn có BBCode `[b]`; presenter nay chuyển sang rich text UGUI trước khi gán `Text`, đồng thời giữ nguyên thẻ màu số do tier tạo. Regression EditMode kiểm tra chuyển đổi xác định, không phụ thuộc message được chọn ngẫu nhiên.

## Matrix runtime đã đạt

1. Level 11, P20: thắng hoàn hảo, đúng tier/icon, toast thực sự visible.
2. Toast đóng theo lifecycle nguồn; Win không xuất hiện trước 1,5 giây.
3. Đổi provider sang Control rồi bấm Next.
4. Level 12 reload Control trước puzzle selection, toast không hiện và Win giữ delay 1,2 giây.
5. Toàn Platform suite vẫn giữ PreCat lock/retry và 18 flow cũ.

## Còn mở

`P-RESULT-007` giữ `[~]` cho tới khi có matrix kết hợp Win Toast với toàn chuỗi rank/streak/rate/push và pixel/VFX trên các aspect/device mục tiêu. Runtime độc lập, lifecycle config và timing chính đã có bằng chứng tự động.
