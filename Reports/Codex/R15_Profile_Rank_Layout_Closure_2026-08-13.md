# R15 Profile–Rank Layout Closure — 2026-08-13

## Kết quả

Đã đóng `P-PROFILE-005` và cập nhật `P-BOARD-009` theo tiêu chí portfolio/offline đã được người dùng chốt.

- Platform EditMode: **51/51**, 0 fail, 0 skip, 79,088 giây.
- Platform PlayMode: **24/24**, 0 fail, 0 skip, 279,531 giây.
- XML PlayMode xác nhận riêng `PlatformProfile_PendingCloseConfirmLockedAndRedDotReopenMatchSource` đạt trong 9,864845 giây.
- Unity Tundra compile thành công; không có C# error hoặc missing script mới.
- Không ghi profile/save người dùng trong fixture; Rank/Profile/Award dùng store/service bộ nhớ.
- Không thêm runtime log.

## Nguồn đối chiếu

- `scripts/module/profile/ui/profile_page.tscn`
- `scripts/module/profile/view/profile_page.gd`
- `scripts/module/profile/ui/avatar_profile_cell.tscn`
- `scripts/module/profile/ui/avatar_cell.tscn`
- `scripts/module/rank_activity/rank_activity_manager.gd::open_home_entry`

## Sai lệch đã sửa

1. Adapter Profile cũ được viết trước R15 nên luôn ẩn `GO` của tooltip frame khóa, dù Rank đang chạy.
2. Tham số nguồn `from_rank_open_guide` chưa được đọc, nên không có gate chống route Rank lặp lại.
3. Tab Avatar/Frame chỉ đổi nội dung; viewport chưa áp dụng `Avatar bottom +20`, `Frame top +10/bottom +10` như nguồn.
4. Prefab dùng màu nâu cho Title và trắng cho title text, khác `StyleBox/title font` trong `.tscn`; label tab cũng dùng offset/màu tạm.
5. Migration prefab ban đầu bị bỏ qua bởi `File.Exists` trên asset path tương đối và gate sprite/font dành cho build mới. `ProfilePagePrefabInstaller` nay dùng `AssetDatabase`, migration asset hiện hữu chạy trước gate build và được gọi trực tiếp bởi `UnityRefreshBridge`.

## Contract đã khóa

- Content cố định 900×1253, anchor/pivot center và `anchoredPosition=0`; do đó giữ đúng tâm ở 1080×1920 và 1080×2400.
- Grid bốn cột, cell 185×185, gap 6; title/tab colors và offsets theo scene nguồn.
- Avatar viewport cao thêm 20 px; Frame viewport dịch xuống 10 px và giữ chiều cao.
- Locked frame shake/tooltip giữ timing hiện có; `GO` chỉ hiện khi Rank `IsRunning` và Profile không đến từ Rank guide.
- `GO` đóng Profile, mở Rank page hoặc request Home rank popup theo cùng nhánh `open_home_entry` nguồn.
- Close/Confirm/red-dot/unlock/equip và cached reopen tiếp tục đúng, không nhân cell.

## Quyết định visual parity

Source extract không kèm custom Spine runtime nên Godot portable không tạo được baseline pixel đầy đủ, đáng tin cậy. Theo mục tiêu CV, dự án vẫn source-first nhưng dùng source geometry/timing, automated regression và nghiệm thu trực quan thay cho ngưỡng pixel giả. Board 1080×1920 trước đó đã được người dùng duyệt; `P-BOARD-009` vì vậy được đóng theo tiêu chí visual acceptance, không tuyên bố pixel-identical.

## Việc tiếp theo

Audit build-readiness, tạo development build Windows/Android khả dụng và tiếp tục visual/VFX pass cho các page còn mở. SDK quảng cáo/IAP/backend thật giữ ngoài phạm vi.
