# R17 — Last Cat Timing And Scale

Ngày: 2026-08-14

## Kết luận

Khoảng chờ sau khi chọn mèo cuối là một lỗi timeline thật, không chỉ do máy chậm. Unity đã cộng tuần tự thời gian score flight vào life bonus, trong khi bản Godot chạy hai nhánh này song song.

Hiệu ứng CAT không chạy ngược chiều. Sai khác nằm ở cách Unity ép mọi sprite vào một RectTransform cố định, khiến frame appear lớn bị thu nhỏ để luôn lọt trong cell.

## Bằng chứng từ nguồn

- `base_game_page.gd` khởi chạy life bonus ngay trong lúc score flight của mèo đúng đang tiếp diễn.
- `cell.tscn` đặt `CatIcon` tại `(49, 51)` trong cell 100 px và dùng node scale `0.5`.
- Frame đầu của CAT appear có rect `287 x 232`; với scale `0.5`, phần hiển thị có thể vượt nhẹ kích thước cell. Đây là chủ ý hình ảnh của nguồn.

## Thay đổi

- Life bonus bắt đầu ở offset `0`, chạy song song với score flight.
- Gate ba mạng còn `(3 - 1) * 0.3 + 0.57 + 0.35 = 1.52s` thay vì cộng thêm fly delay `0.8–1.45s`.
- `CatSpriteAnimationView` dùng native rect của từng sprite, neo giữa cell, offset nguồn `(-1, -1)` và scale `0.5`.
- `ShowIdleFinal` đi qua cùng hàm layout để không giữ nhầm kích thước frame appear.

## Regression

- Full EditMode: `702/705`; các test timing và native-frame CAT mới đều đạt.
- Ba lỗi EditMode còn lại thuộc Bank readable-string, không phát sinh từ thay đổi gameplay này.
- Portfolio Visual PlayMode: `4/4`, thời gian `102.480s`.

## USER QA còn lại

Tìm mèo cuối trong một ván có đủ ba mạng:

1. CAT phải xuất hiện ngay khi thao tác được nhận.
2. Trong đoạn appear, CAT phóng lớn nhẹ và có thể vượt cell rồi trở về idle.
3. Score/life feedback bắt đầu song song, không còn khoảng đứng hình cộng thừa gần một giây.
4. Khoảng `1.2–1.5s` trình bày trước Win vẫn là timing chủ ý, không phải load asset.
