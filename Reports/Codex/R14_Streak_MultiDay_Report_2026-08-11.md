# R14 Streak Multi-Day Report — 2026-08-11

## Nguồn đối chiếu

- `scripts/module/daily_streak/core/feature_daily_streak.gd`
- `scripts/module/daily_streak/model/streak_data.gd`
- `scripts/module/daily_streak/view/streak_page.gd`
- `scripts/module/daily_streak/view/streak_revive_flow.gd`

## Contract nguồn

- Mỗi local day chỉ check-in một lần; chuỗi liên tục tăng streak/reward cycle và ngày bị bỏ lỡ làm chuỗi broken.
- Ngày thứ 7 hiển thị chest, dispatch hai Hint và hai Locate qua durable Award transaction; ngày thứ 8 bắt đầu lại một slot của chu kỳ hiển thị.
- Main mở trực tiếp; streak mới đi Lit→Settle, streak từ ngày thứ 2 mở Settle.
- Trong Settle, ô check-in mới nhất ban đầu chưa sáng; chỉ reveal sau `20/60s`, hoặc `62/60s` sau khi chạm mặt trời ở Lit.
- Delay tăng số `0,9s` không khóa Continue của normal settle.

## Sai lệch và thay đổi

- Unity trước đó render toàn bộ week slot đã checked ngay khi Settle mở, làm mất trạng thái trước/sau check-in của Godot.
- `RunSettle` còn dùng `AddAfterCheckinSeconds` để trì hoãn Continue, trong khi nguồn chỉ dùng thời gian đó cho number-roll effect chạy độc lập.
- `StreakPagePresenter` nay giữ trạng thái reveal riêng cho normal settle: ẩn checked slot cuối, reveal sau đúng slot delay, rồi mở Continue ngay khi flow/reward hoàn tất.
- Revive Backfill/Resume không dùng normal-slot rule; visual riêng của hai nhánh vẫn được giữ trong backlog thay vì áp sai phép ẩn slot.
- Test helper Award chọn `CollectBtn` trong nhánh cây đang active, vì prefab có regular và Rank Gift cùng tên nhưng chỉ một nhánh hiển thị.
- Không thêm runtime log.

## AppScene PlayMode matrix

1. Mở Streak từ Home ở trạng thái Main, tuần rỗng và Back về Home.
2. Chạy liên tục ngày 10–16/08: ngày 1 đi Lit→Settle; ngày 2–7 đi Settle; trước delay UI có `day-1` ô checked, sau delay có `day` ô.
3. Ngày 16 (ngày thứ 7) tạo đúng một Streak Gift durable; Collect xóa in-flight transaction và cộng đúng +2 Hint/+2 Locate.
4. Win lặp trong cùng ngày 16 là no-op, không tạo pending presentation và không cộng thêm tool.
5. Ngày 17 tăng streak lên 8 nhưng week display quay về một checked slot.
6. Bỏ ngày 18, sang ngày 19 xác nhận broken/display 0; check-in mới reset current/reward cycle về 1, giữ best streak 8 và đi lại Lit→Settle.

## Kết quả

- Unity compile/domain reload sạch.
- EditMode: **515 passed, 0 failed, 0 skipped, 0 inconclusive** (`61,296 s`).
- PlayMode: **15 passed, 0 failed, 0 skipped, 0 inconclusive** (`222,514 s`).

## Còn lại

- Rewarded-ad production provider/device callback cho Backfill/Resume.
- Animation recover/new-week/number-roll, Spine/VFX và pixel parity.
- App-kill trong pending revive/Award thuộc R17 device matrix.
