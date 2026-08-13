# R15 Profile Interaction Closure — 2026-08-13

## Kết quả

Đã đóng `P-PROFILE-002` bằng AppScene PlayMode thao tác trên prefab/presenter thật.

- Platform PlayMode: **20/20**, 0 fail, 0 skip, 218,207 giây.
- Full EditMode gần nhất: **679/679**, 0 fail.
- Profile fixture dùng repository bộ nhớ; không đọc/ghi dữ liệu profile người dùng trong các mutation của test.
- Không thêm runtime log hoặc thay đổi hành vi production.

## Nguồn đối chiếu

- `scripts/module/profile/view/profile_page.gd`
- `scripts/module/profile/view/avatar_profile_cell.gd`
- `scripts/module/profile/profile_service.gd`
- `scripts/module/profile/model/profile_catalog.gd`

## Matrix đã đạt

1. Mở Profile tạo đúng tám avatar cell, pending khởi tạo từ equipped identity.
2. Chọn avatar chỉ đổi pending; Close không thay đổi `ProfileService`.
3. Reopen dùng cùng page instance, reset pending từ service và không nhân cell.
4. Nickname, avatar và classic frame chỉ commit khi Confirm.
5. Frame leaderboard chưa sở hữu không đổi pending, chạy locked shake và mở tooltip; dismiss đóng sạch.
6. Nhận frame leaderboard bật red-dot; mở tab Frame xóa red-dot đúng nguồn.
7. Frame vừa mở khóa có thể chọn/Confirm; reopen giữ avatar/frame mới.

## Phạm vi còn lại

Profile page còn pixel/animation ở 1080×1920/2400, glyph/IME và touch/device thật trong `P-PROFILE-005`/R17. Remote backend thật vẫn ngoài phạm vi bản thử nghiệm offline.
