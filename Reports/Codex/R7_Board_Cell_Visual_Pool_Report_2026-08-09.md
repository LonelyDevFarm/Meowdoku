# R7 Board/Cell visual và lifecycle pool

## Phạm vi

- Đối chiếu trực tiếp `board_view.gd`, `cell_view.gd`, `board_grid_overlay.gd`, hai shader cell và config `game_grid_ui`/`region_color`.
- Port nền Board, palette, bốn góc cell, hard-edge, region overlay và lifecycle tái sử dụng Cell.

## Kết luận nguồn quan trọng

- Offline mặc định là `game_grid_ui = 0` (`VALUE_NOMAL`), không bật single-line overlay.
- Offline mặc định là `region_color = 2` (`VALUE_NEW_CELL_ONLY`).
- Default dùng nền Board trắng, corner 30; cell dùng SDF anti-aliased với corner hình ảnh 10 px.
- `BoardGridOverlay`, cell hard-edge và bốn outer corner chỉ bật ở variant single-line.
- Báo cáo Gemini 010 hữu ích để tìm file nhưng không phân biệt đủ mạnh default với variant; mọi nhánh đã được kiểm chứng lại bằng source.

## Thay đổi Unity

- Thêm `GameGridUiConfig` và phép tính `solve_local_layout` cho đủ group 0–3.
- Thêm shader `Meowdoku/UI/RoundedRect`, giữ bốn bán kính độc lập, hard-edge và UGUI stencil/clip.
- Cache shared material theo shader/size/radii/hard để không tạo một material cho mỗi Cell.
- Board Image được đưa về trắng alpha 1 thay cho prototype alpha 0.392.
- Thêm `BoardGridOverlayGraphic` dựng thin grid, boundary vùng và rounded frame bằng UI mesh; chỉ active ở single-line.
- Board bỏ `PoolManager.Instance`; dùng queue Cell cục bộ, reactivation và reset tween/hint/VFX/state trước mỗi lần dùng.
- Installer gắn shader bằng serialized reference qua Unity API cho Board và Cell prefab.

## Kiểm chứng

- `Meowdoku.Core`, `Meowdoku.Gameplay`, `Meowdoku.Editor` và `Meowdoku.EditModeTests` compile sạch bằng Unity Roslyn.
- Bổ sung 8 case cho layout group 0–3 và corner-radius-by-size.
- Còn bắt buộc: Unity Refresh để import shader/meta và installer ghi reference; PlayMode kiểm tra board 4–10, restart/đổi size liên tục và shader trên GPU thực.

## Sai khác còn mở

- Cell icon/VFX và toàn bộ animation variant chưa thuộc phạm vi đóng của lượt này.
- Frame intro đã có contract/runtime nhưng cần video parity; default offline không hiển thị overlay này.
- Safe-area/aspect/layout expansion chờ báo cáo GEM-R7-011 và lượt R7 kế tiếp.
