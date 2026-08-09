# R8 Win/Fail Aggregate Transition Test Report

- Date: 2026-08-09
- Unity: 6000.3.19f1
- Result: **224 passed, 0 failed**

## Scope

- Port `on_game_finished`, `on_level_won` và `on_level_failed` cùng các bộ đếm session/ngày, clean win, fail/retry và DDA strategy.
- Tách `MainGameTransitionCoordinator` để điều phối Fail, Revive, Win, Restart giữa ván và Quit mà không đẩy state mutation vào view.
- Giữ đúng ownership snapshot: Fail không ghi snapshot `lives=0` và không xóa snapshot; Revive ghi ngay; Win xóa sau khi settle; Restart giữa ván xóa trước khi tính fail; Quit đánh dấu dirty rồi ghi trạng thái hiện tại.
- Tạo retry payload từ puzzle gốc, loại PreCat hiện tại và không mang board edits sang lượt mới.
- Nối `GameplayManager` với transition event và các đường Revive, Restart, Retry sau Fail, Quit, Next.
- Dọn hint/drag/debounce cũ khi tải lại và không chạy Daily First Easy trên direct retry.

## Source corrections

`GEM-R8-005` được dùng để dò đường, sau đó các nhánh liên quan được đọc trực tiếp trong `game_page.gd`, `game_fail_page.gd`, `level_ops.gd` và `game_state.gd`. Những điểm đã khóa lại:

- `_on_game_over` không ghi snapshot `lives=0`; snapshot ERROR đã được flush trước đó.
- `_is_complete` của nguồn chỉ bảo vệ `on_game_finished`, không bảo vệ toàn bộ callback terminal. Unity dùng guard một-lần tại coordinator để không tăng fail/win lặp do callback re-entry.
- Clean win dùng `not _current_level_dirty`, không dùng `not _current_level_retried`.
- Restart giữa ván gọi clear snapshot → game finished → level failed; Restart từ Fail page chỉ mở lại game bằng `retry_params`, không settle fail lần hai.
- Quit chỉ đánh dấu dirty trong callback; Unity ghi snapshot ngay tại adapter vì không có lifecycle hide-page tương đương ở prototype hiện tại.

## Remaining boundary

- Result/Fail page, tracker, animation, toast và popup ordering chưa được dựng; chúng phụ thuộc R9/R10/R13.
- Retry payload chưa có `r1_steps`…`r5_steps` vì Unity chưa port strategy-step breakdown; dữ liệu này hiện không được `LevelEntry` tiêu thụ và được giữ là khoảng trống P1 để port từ nguồn, không tự tạo.
- `start_toast` variant state và tracker/Rank callbacks vẫn ở các module sau.
- EditMode đã kiểm chứng contract logic; PlayMode đóng app tại từng thời điểm transition và device filesystem vẫn là cổng bắt buộc.
