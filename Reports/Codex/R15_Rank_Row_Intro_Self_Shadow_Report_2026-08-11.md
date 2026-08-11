# R15 Rank Row Intro and Self-shadow Report — 2026-08-11

## Nguồn đối chiếu

- `scripts/module/rank_activity/ui/rank_cell.tscn`
- `scripts/module/rank_activity/view/rank_cell.gd`
- `scripts/module/rank_activity/view/rank_activity_page.gd`
- `scripts/module/rank_activity/view/rank_activity_change_page.gd`
- `scripts/module/ui/common/rank_list_view_impl.gd`

## Sai lệch đã tìm thấy

- `RankActivityRow` Unity đặt toàn bộ visual trực tiếp trên RectTransform do `VerticalLayoutGroup` quản lý, nên chưa có root riêng để chạy animation mà không phá layout.
- Leaderboard chỉ kẹp clone self-row vào viewport; chưa có shadow 1.033×270, fade 0,15 giây và lật dọc khi clone ghim ở mép trên.
- Page chưa chạy `Appear1` theo nhịp 0,06 + 0,05 × row; clone ở mép chưa chạy `Appear2` sau 0,3 giây.
- Rank Change chỉ fade CanvasGroup tạm trong 0,16 giây, chưa dùng keyframe scale `Appear3` của row nguồn.

## Phần đã port

- Nâng cấp prefab thành cây `RankActivityRow/VisualRoot/{Shadow, CanvasGroup}`. Layout vẫn sở hữu root ngoài; DOTween chỉ dịch/scale `VisualRoot`.
- Port ba animation row: `Appear1` trượt từ X=1100 trong 0,36666667 giây; `Appear2` đi theo Y=200→-20→5→0 trong 0,42151412 giây; `Appear3` scale 0,6→1,05→1 trong 0,3 giây. Hệ trục Y được đổi dấu đúng khi ánh xạ Godot sang RectTransform Unity.
- Leaderboard phát `Appear1` theo delay nguồn. Floating self-row dùng cùng delay với row thật khi chưa ghim, hoặc `Appear2` ở 0,3 giây khi đang ghim mép.
- `SyncFloatingSelf` nay phân biệt mép trên/dưới; shadow dùng đúng geometry 1.033×270, top offset -41,45/-51, lật Y ở mép trên và fade 0,15 giây.
- Rank Change dùng `Appear3` cho các row đang thấy với lịch 0,2 + 0,0667 × visible index; row thật của self được ẩn khi clone celebration thay thế.
- Tween chạy unscaled time, liên kết lifecycle GameObject, bị kill/reset khi Apply, hide, disable hoặc destroy; intro dừng shadow tween cũ để shadow không ló trước `Appear2`.
- Prefab hiện hữu được nâng cấp idempotent qua Unity Prefab API, giữ nguyên `.meta`, GUID và mọi serialized reference.
- PlayMode bridge nay triển khai `IErrorCallbacks` và reset event; một lượt bị Editor dừng player không còn có thể để kết quả kẹt vĩnh viễn ở `RUNNING`.

## Kiểm tra

- EditMode khóa timing nguồn và đọc prefab thật để xác nhận `VisualRoot`, `Shadow`, `CanvasGroup`, geometry cùng serialized reference.
- Unity compile/domain reload sạch; prefab installer hoàn tất không đổi GUID.
- EditMode: **529 passed, 0 failed, 0 skipped, 0 inconclusive** (`64,646 s`).
- PlayMode: **17 passed, 0 failed, 0 skipped, 0 inconclusive** (`289,124 s`) trên assembly cuối.
- Không thêm runtime log.

## Còn lại

- Rank Gift chest/celebration Spine-equivalent và frame fly-to-profile.
- Pixel/video timing comparison cùng device touch/notch/performance soak ở R17.
