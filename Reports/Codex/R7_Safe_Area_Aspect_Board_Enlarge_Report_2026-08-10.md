# R7 Safe Area, Aspect và Board Enlarge

Ngày: 2026-08-10  
Trạng thái: code/assembly compile hoàn tất; chờ Unity Refresh và PlayMode/device parity.

## Nguồn đã đối chiếu

- `project.godot`: viewport/override 1080×2400, `stretch/mode="canvas_items"`, `stretch/aspect="keep_width"`.
- `scripts/ui/ui_manager.gd`: lấy safe area trên Android/iOS, áp top/bottom inset và cập nhật khi viewport đổi kích thước.
- `scripts/ui/adapt/header_adapt_holder.gd`: nội suy minimum height 0→65 khi chiều cao tăng 1920→2400; collapse khi top safe inset dương.
- `scripts/module/game/cfg/board_no_fuction.tres`: profile layout thường.
- `scripts/module/game/cfg/board_big_no_fuction.tres`: profile layout board lớn.
- `scripts/manager/ab_config/board_size_big_config.gd`: mặc định normal, enlarged khi config bằng 1.
- `scripts/module/game/page/base_game_page.gd`: board size 8+ được phóng `1008×1,04167`, container giữ tối thiểu 1008 và căn giữa.

Gemini report `011_Safe_Area_Aspect_Layout_Source_Spec.md` được dùng làm bản đồ file. Codex kiểm chứng lại trực tiếp vì report bỏ sót hành vi `HeaderAdaptHolder`.

## Phần đã port

- Thêm `BoardSizeBigConfig` với contract normal/enlarged nguồn.
- `SourceBoardLayout` giữ board chuẩn 1008 và trả width phóng cho size 8+.
- `BoardView` nhận config, scale grid theo visible width, công bố chiều cao thật và phát `LayoutChanged` sau setup.
- `GameplayManager` tạo/pass config mặc định cho board.
- `SourceGameplayPageLayout` port minimum, stretch ratio và profile normal/big; hỗ trợ top/bottom safe inset và HeaderAdapt collapse.
- `GameplayPageLayoutPresenter` cập nhật khi enable, resize, focus và khi board layout đổi.
- `GameplayPresentationSceneInstaller` cấu hình `CanvasScaler`: Scale With Screen Size, reference 1080×2400, match width.
- Bổ sung test nội suy HeaderAdapt, safe bounds và ngưỡng/hệ số board-enlarge.

## Adapter Unity bắt buộc

Godot trả safe rectangle theo pixel màn hình trong khi layout UGUI chạy theo Canvas unit. Unity lấy `Screen.safeArea`, sau đó đổi inset theo `layoutWidth / Screen.width` trước khi đưa vào công thức nguồn. Safe area chỉ được áp khi `Application.isMobilePlatform`, tương ứng nhánh Android/iOS của Godot.

## Xác minh đã làm

- Core assembly: compile sạch bằng Unity Roslyn.
- Gameplay assembly: compile sạch bằng Unity Roslyn.
- Editor assembly: compile sạch bằng Unity Roslyn.
- EditMode test assembly: compile sạch bằng Unity Roslyn.
- Không thêm runtime debug log.
- Không sửa scene/prefab YAML thủ công; installer Unity chịu trách nhiệm serialized reference và CanvasScaler.

## Cần xác nhận trong Unity

1. Refresh project để Unity import assembly/installer mới.
2. Mở GameplayScene và xác nhận CanvasScaler là `Scale With Screen Size`, reference `1080 × 2400`, Match = Width.
3. Play ở 1080×1920 và 1080×2400; header/rule/board không chồng nhau, board vẫn N×N.
4. Test ít nhất size 4, 7, 8 và 10; size 8+ phải rộng hơn theo profile nguồn nhưng không đổi hàng/cột.
5. Play→Stop và xác nhận Console không có error mới.
6. Notch/safe-area thật để kiểm tra trên thiết bị hoặc Device Simulator ở R17.

