# R15 Rank Leaderboard Layout, Scroll and Rise Report — 2026-08-11

## Nguồn đối chiếu

- `scripts/module/rank_activity/ui/rank_activity_page.tscn`
- `scripts/module/rank_activity/view/rank_activity_page.gd`
- `scripts/module/rank_activity/ui/rank_activity_change_page.tscn`
- `scripts/module/rank_activity/view/rank_activity_change_page.gd`
- `scripts/module/ui/common/rank_list_view_impl.gd`
- `scripts/module/ui/ui_manager.gd`

## Sai lệch đã tìm thấy

- Rank page Unity dùng podium cao 470 thay vì 521, list bắt đầu sai vị trí, row cách 10 thay vì 20 và CTA 820×180/bottom 35 thay vì 784×258/bottom 130.
- Page chưa có `HeaderAdaptHolder`, safe top/bottom và self-row nổi theo row thật khi cuộn.
- Rank Change chưa có margin dọc 200 của `RankMargin`, luôn bắt đầu ở đầu list thay vì căn người chơi giữa viewport.
- Animation Change chỉ scale và đổi rank tại chỗ; chưa di chuyển self-row qua các slot, chưa đẩy row bị vượt và chưa cho scroll bám theo chuyển động.

## Phần đã port

- Thêm contract thuần `SourceRankActivityLayout` cho Page/Change, dùng đúng số liệu scene nguồn ở 1080×1920 và 1080×2400.
- Page áp dụng HeaderAdaptHolder 0→65, collapse khi có top safe inset, safe bottom cho list/CTA, list rộng 1008, viewport top 20/bottom 18, row 968×180 cách 20 và scroll clamped.
- Thêm self-row clone nổi: bám row thật khi còn trong viewport, kẹp vào mép trên/dưới khi row thật ra ngoài, giữ route mở self profile.
- Change áp safe group đúng nguồn, padding trên/dưới 200, căn self-row giữa viewport trước animation, chỉ intro các row nhìn thấy.
- Port rise duration `clamp(1 + 0.05 × advance, 1, 3)`, self-row đi từ hạng cũ lên hạng mới, các row bị vượt rơi domino về slot cuối và scroll bám theo self-row mỗi tick.
- Floating self-row của Change nằm ở layer riêng như `_my_cell` nguồn; row thật giữ layout, clone được cleanup và trả presentation cuối khi animation kết thúc/ẩn page.
- Follow-up đã nâng row thành `VisualRoot/{Shadow, CanvasGroup}`, port `Appear1/2/3` và sticky self-shadow fade/lật theo đúng `rank_cell.gd/.tscn`; chi tiết nằm trong báo cáo row intro/self-shadow kế tiếp.
- Nâng cấp prefab hiện hữu bằng Unity Prefab API; giữ nguyên `.meta`, GUID và registry reference.

## Kiểm tra

- Contract regression: Page 1920, Page 2400, safe-area collapse/insets, Change safe groups, center offset có padding 200 và rise duration clamp.
- Prefab regression đọc asset thật: layout component/reference, podium/list/CTA, viewport offsets, spacing/padding, clamped movement, FloatRow và PlayerCelebrate.
- Unity compile/domain reload sạch.
- EditMode: **525 passed, 0 failed, 0 skipped, 0 inconclusive** (`57,381 s`).
- PlayMode: **16 passed, 0 failed, 0 skipped, 0 inconclusive** (`222,947 s`).
- Không thêm runtime log.

## Còn lại

- Wide-screen particles của leaderboard.
- Arrow, collection, lift/drop Spine-equivalent và celebration VFX của Rank Change.
- Process restart/time rollback và device soak; frame-only AppScene proof đã được khóa ở báo cáo Award kế tiếp.
