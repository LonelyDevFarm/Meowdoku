# R8 Tool Resource + Idle Hint Test Report

- Date: 2026-08-09
- Unity: 6000.3.19f1
- Result: **216 passed, 0 failed**

## Scope

- `reward_unlock_level` và `prop_highlight` default/policy.
- Persist `prop_highlight_shown`; runtime flags current-level dirty và DDA tool/revive.
- Locate/Hint resource quyết định Free, Consumed, RewardRequired và shared 800 ms cooldown.
- Idle hint guard, once-per-lifetime, block sau khi dùng tool, thiếu animation không đánh dấu đã xem.
- Nhịp repeat đúng nguồn: chờ 20 giây, chạy 10 giây, dừng rồi chờ lại 20 giây.
- Tích hợp `GameplayManager` compile cùng Core/Gameplay/EditMode Tests.

## Source corrections

`GEM-R8-004` được dùng như tài liệu dò đường nhưng ba kết luận đã được sửa sau khi đọc trực tiếp `base_game_page.gd` và hai config gốc:

- Giá trị mặc định `prop_highlight = 2` chọn Hint một lần, không phải Locate.
- Chế độ repeat không phát mỗi 10 giây; animation chạy 10 giây rồi idle timer trở về 0 và phải chờ thêm 20 giây.
- Locate đánh dấu current level dirty và DDA tool/revive ngay sau `_consume_tool`, kể cả khi tool không được consume.

## Remaining boundary

Logic/policy đã hoàn tất ở phạm vi R8 hiện tại. `ToolButtonView`, badge, animation `tool_loop`/`RESET`, ad/award adapter và visual reward không được tự sáng tạo ở bước này; chúng sẽ được rebuild từ scene/script/asset nguồn trong R9/R16.
