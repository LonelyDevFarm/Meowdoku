# R13 — Win pass-page/pass-text runtime closure

Ngày kiểm tra: 2026-08-13  
Unity: 6000.3.19f1

## Phạm vi nguồn

- `game_win_page.gd`
- `pass_page_g1_board.gd`, `pass_page_g2_board.gd`
- `pass_text_strategy_v0.gd` đến `pass_text_strategy_v3_g3.gd`
- `pass_page_config.gd`, `pass_text_config.gd`

## Matrix runtime

- Mở/đóng cùng một cached `GameWinPagePresenter` qua pass-page Control, G1, G2, G4.
- Bao phủ pass-text Control, Beat Percent, V2, V3-G1, V3-G2 và V3-G3.
- Xác nhận default visuals/PassPanel/ExtraStatistics reset đúng giữa các lần mở.
- Xác nhận Size `6×6`, Time `01:06`, Score `12,345`, Combo `7`; G2 có Completion `87%`, Mistake `2`, Tools `3` còn G1 không hiện extra panel.
- Xác nhận source BBCode được chuyển sang UGUI rich text, percent highlight đổi từ xanh nguồn sang cam của pass page.
- Xác nhận G1/G2 CTA khóa ngay khi `OnShow`, tự mở sau marker 0,69804 giây; G4 roll đúng Time/Score/Combo và Back luôn bị consume.

## Kết quả

- Không phát hiện sai lệch production; không sửa runtime.
- Hai lượt test đầu cho thấy `UiWindowState.Showing` không phải mốc cố định trước/sau CTA marker vì hai animation chạy song song. Fixture được đổi sang kiểm tra trạng thái ngay tại `Show()` và chờ marker với timeout, đúng hợp đồng nguồn và ổn định theo frame.
- Platform PlayMode: **22 passed, 0 failed, 0 skipped**, thời lượng **289,813 giây**.
- Full EditMode ổn định gần nhất: **679/679**; không chạy lại vì thay đổi chỉ là PlayMode fixture và Unity compile sạch.
- Không thêm runtime log, không ghi save/profile thật.

## Trạng thái

- `P-RESULT-001`: hoàn thành về runtime/composition.
- Pixel, Spine và video-reference tiếp tục được theo dõi trong khối Visual/VFX.
