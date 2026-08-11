# R15 Rank Change Celebration Report — 2026-08-11

## Nguồn đối chiếu

- `scripts/module/rank_activity/view/rank_cell.gd`
- `scripts/module/rank_activity/ui/rank_change_collection.tscn`
- `scripts/module/rank_activity/ui/rank_cell_particles.tscn`
- `scripts/module/rank_activity/rank_activity_change_page.gd`

## Sai lệch đã tìm thấy

- Unity mới chỉ chạy rise/domino/scroll-follow; chưa có sáu biểu tượng Cat/Fish bay về self-row, arrow loop và glow/star burst.
- Nhánh có tăng điểm nhưng không thăng hạng kết thúc sau score roll, thiếu `Lift` rồi `Drop` của nguồn.
- Lift/drop cũ dùng timing gần đúng và `SetRank` gọi lại `Apply`, làm reset presentation giữa animation.

## Phần đã port

- Thêm `RankActivityRowCelebrationView` làm adapter UGUI cho `CPUParticles2D`, dùng sprite nguồn và DOTween thay vì giả một particle runtime không có trong Unity.
- Collection giữ đúng sáu vị trí nguồn, đổi Cat/Fish theo group, stagger 0,1 giây, arrival 1,0333333 giây và tổng duration 1,6666701 giây; mỗi item có glow/star burst riêng.
- Arrow dùng bốn Image chạy loop hướng lên; rise burst gồm glow giữa và 12 edge star. Toàn bộ visual nằm trong cây `VisualRoot/Effects/{Collection, Arrow, RiseBurst}`.
- Lift giữ scale 1→1,05 trong 0,23333333 giây. Drop giữ 1,05→1,08→0,95→1 trong 0,33333334 giây; arrow fade 0,06666667 giây và burst bắt đầu tại 0,23333335 giây.
- Timeline Change giữ Count tại 0,7333 giây, chờ score roll, arrow/lift/rise/settle/drop theo nguồn; final row được đổi tại 0,23 giây trong Drop. Nhánh không rise chờ collection xong rồi vẫn chạy Lift/Drop.
- `ApplyPreservingPresentation` tách cập nhật nội dung khỏi reset visual, tránh làm gãy lift/drop khi rank thay đổi.
- Tween chạy unscaled time, liên kết GameObject và được kill/reset khi hide, disable hoặc destroy. Không thêm runtime log.
- Prefab hiện hữu được nâng cấp idempotent qua Unity Prefab API, giữ nguyên `.meta`, GUID và serialized reference.

## Kiểm chứng

- Unity EditMode Test Runner: **529 passed, 0 failed**.
- Unity PlayMode Test Runner: **17 passed, 0 failed**, duration **245,873 giây**.
- Phiên PlayMode đầu bị gián đoạn khi Editor/reload làm kết quả còn `RUNNING`; reset bridge rồi chạy lại toàn suite đạt. Unity vẫn responsive, không phải vòng lặp celebration.

## Còn lại

- Rank Gift chest/celebration Spine-equivalent và frame fly-to-profile.
- Pixel/video timing comparison cùng profiler/device soak ở R17.
