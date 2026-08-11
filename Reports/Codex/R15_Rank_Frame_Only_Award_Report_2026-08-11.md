# R15 Rank Frame-only Award Report — 2026-08-11

## Nguồn đối chiếu

- `scripts/module/award/view/award_page.gd`
- `scripts/module/award/ui/award_page.tscn`
- `scripts/module/rank_activity/config/rank_activity_config.gd`
- `scripts/module/rank_activity/view/rank_gift_cell.gd`

## Sai lệch đã tìm thấy

- Unity đã nhận đúng reward group 3 chỉ có leaderboard frame, nhưng sau phase podium vẫn bật `AwardPanel` thường và render frame như một item thường.
- Godot loại frame khỏi `_render_award_cells`; frame luôn đi qua `FrameAddEffect` riêng. Với frame-only, cả hai nút Collect bị ẩn và Award tự đóng sau hiệu ứng.
- Unity dùng một khoảng chờ cố định 0,8 giây, chưa có avatar/frame động và chưa giữ đúng timing Appear/Hold/Disappear nguồn.
- Rank Gift đầy đủ cũng từng render frame lẫn với tool ở phase item, trong khi nguồn chỉ render tool rồi phát hiệu ứng frame khi đóng.

## Phần đã port

- Thêm `FrameAwardEffectView` dùng DOTween và `ProfileAvatarView` đã serialize; avatar hiện tại, frame được thưởng và count trước/sau lấy từ `ProfileRuntime`.
- Giữ đúng key timing nguồn: Appear `0,56666666 s`, giữ frame mới `0,6334 s`, giữ frame đã sở hữu `0,8 s`, Disappear `0,33333334 s`.
- Port các mốc scale/rotation/fade chính của `FrameAddEffect`; tween dùng unscaled time và được kill/cleanup khi page ẩn hoặc bị destroy.
- `AwardPagePresenter` giờ scan frame một lần, chỉ render non-frame item vào panel thường, và dùng chung đường `BeginCloseWithFrameEffect` cho frame-only lẫn reward đầy đủ.
- Frame-only không bật `AwardPanel`; inventory chỉ persist sau khi effect kết thúc. Nếu page bị đóng ngoài dự kiến, `OnHide` vẫn hoàn tất transaction đúng một lần.
- Prefab installer nâng cấp `AwardPage.prefab` qua Unity Prefab API, thêm nhánh `FrameAddEffect/EffectRayLight` và `FrameAddEffect/AvatarCell`, giữ GUID/reference hiện hữu.

## Kiểm tra

- Contract timing và serialized prefab regression đạt.
- AppScene PlayMode tạo Rank Gift group 3 thật, bấm Collect phase podium, xác nhận effect đang chạy và panel item thường vẫn ẩn.
- Trong lúc effect chạy, frame count chưa tăng; sau khi Award đóng, count tăng đúng `+1`, tool không đổi, in-flight transaction rỗng và render count về 0.
- Unity compile/domain reload sạch.
- EditMode: **527 passed, 0 failed, 0 skipped, 0 inconclusive** (`61,547 s`).
- PlayMode: **17 passed, 0 failed, 0 skipped, 0 inconclusive** (`236,502 s`).
- Không thêm runtime log.

## Còn lại

- Particle Shine/Star/Glow và burst/trail chưa đạt pixel parity với Godot.
- Nhánh frame mới bay về Profile khi Home đang hiển thị vẫn còn là VFX adapter cần port.
- Chest/celebration Spine-equivalent và device app-kill soak vẫn thuộc phần R15/R17 còn lại.
