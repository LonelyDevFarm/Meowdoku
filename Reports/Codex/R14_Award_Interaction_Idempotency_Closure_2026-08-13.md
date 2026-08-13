# R14 — Award interaction/idempotency closure

Ngày kiểm tra: 2026-08-13  
Unity: 6000.3.19f1

## Phạm vi

- Đối chiếu lifecycle Award với `award_page.gd`, đặc biệt hợp đồng `on_hide` phải persist transaction đang trình bày.
- Kiểm tra Collect gate trong Appear, click sớm, callback click dồn, Back, đóng cưỡng bức giữa `FrameAddEffect` và reopen presenter được cache.
- Kiểm tra mixed Tool+Frame, tool-only, double chỉ áp dụng cho tool, callback/end event và transaction đều đúng một lần.

## Kết quả

- Runtime hiện có đã đúng hợp đồng nguồn; không cần thay đổi production.
- Bài test đầu tiên thất bại tại helper mô phỏng pointer-down thứ hai sau khi click đầu đã lập tức khóa và ẩn nút. Đây là hành vi runtime đúng. Fixture được sửa để mô phỏng callback click thứ hai đã xếp hàng và xác nhận guard idempotent chặn nó.
- Đóng cưỡng bức giữa frame effect vẫn gọi completion boundary đúng một lần; callback effect bị hủy không thể cấp quà lần nữa.
- Reopen dùng cùng presenter, reset tương tác sạch và tool-only Award tiếp theo vẫn được cấp đúng một lần.

## Bằng chứng

- Platform PlayMode: **21 passed, 0 failed, 0 skipped**, thời lượng **302,784 giây**.
- Full EditMode ổn định gần nhất: **679/679**. Không chạy lại vì thay đổi chỉ nằm trong PlayMode fixture và Unity đã compile sạch.
- Không ghi profile thật, không thêm runtime log.

## Trạng thái

- `P-META-006`: hoàn thành.
- `P-META-005`: vẫn mở riêng cho hard-kill trên thiết bị thật.
