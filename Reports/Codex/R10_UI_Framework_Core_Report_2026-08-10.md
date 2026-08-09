# R10 UI Framework Core

Ngày: 2026-08-10  
Trạng thái: contract/runtime core đã compile; AppBootstrap, registry asset và popup queue chưa làm.

## Nguồn đã kiểm chứng

- `scripts/module/ui/ui_name.gd`
- `scripts/module/ui/ui_registry.gd`
- `scripts/module/ui/ui_layer_config.gd`
- `scripts/module/ui/ui_events.gd`
- `scripts/module/ui/base/ui_base_window.gd`
- `scripts/module/ui/base/ui_frame_window.gd`
- `scripts/module/ui/ui_manager.gd`

Báo cáo Gemini `012_UI_Framework_Navigation_Source_Spec.md` được dùng để định tuyến. Hai đề xuất không có cơ sở trong project Unity hiện tại đã bị loại: lưu `tutorial_done` bằng PlayerPrefs và mặc định dùng Addressables.

## Đã port

- `UiName`: toàn bộ 39 key production/debug/editor của source.
- `UiLayer`: 0/100/200/300/400/500, `ZStep=50`, `ZMax=4000`.
- `UIRegistry`: ScriptableObject chứa prefab reference, lookup không allocation sau build, phát hiện duplicate/missing prefab.
- `UIBaseWindow`: state Invalid/Creating/Showing/Hidden/Closing/Destroyed, create/show/hide/destroy hooks, managed cleanup/coroutine lifetime.
- `UIFrameWindow`: layer/fullscreen/mask/open-sound contract, CloseBtn/back request, Canvas sorting và stack top/bottom callbacks.
- `UIManager`: cache instance, stack theo layer, compact sorting order, fullscreen occlusion, mask reference count, show/hide/hide-all/hide-except, back top-down, evict và events.
- `UIButtonPressGuard`: giữ ownership của press qua frame mở UI, chặn release chạm UI mới tương đương `_guard_held_buttons` của Godot.
- `WarmPoolAsync`/`ShowAsync`: one-flight/coalesce. Vì registry đang giữ prefab trực tiếp và project chưa có Addressables, adapter chỉ yield một frame; không tuyên bố tải asset nền giả.

## Quyết định kiến trúc

- Không dùng singleton tự tạo. `UIManager` sẽ là component có serialized dependency do AppBootstrap sở hữu.
- Chưa xóa `SceneLoader`: các scene prototype hiện còn phụ thuộc nó. Chỉ thay khi Startup/Home/Tutorial route mới đã chạy được.
- Chưa dựng prefab/page giả chỉ để lấp registry. Mỗi entry sẽ được nối khi page tương ứng được port từ source.
- Popup queue tách riêng khỏi manager đúng nguồn và thuộc bước kế tiếp.

## Xác minh

- `Meowdoku.Core`: compile sạch bằng Unity Roslyn.
- `Meowdoku.Gameplay`: compile sạch sau khi Core thay đổi.
- `Meowdoku.Editor`: compile sạch sau khi Core thay đổi.
- `Meowdoku.EditModeTests`: compile sạch với 4 fixture mới.
- Fixture bao phủ layer constants, registry duplicate/missing, cache/show/hide event và fullscreen occlusion/mask count.
- Không thêm runtime debug log.

## Checklist PlayMode gộp cho lần test sau

Phần UI framework chưa có page prefab/bootstrap nên chưa có thao tác trực quan riêng. Khi bootstrap được nối, test một lượt:

1. Mở cùng page hai lần không tạo hai instance và không push stack trùng.
2. Popup có mask chặn được input phía sau; đóng popup trả input đúng.
3. Nhấn nút mở popup rồi thả tay không kích hoạt nút nằm dưới popup.
4. Back chỉ gửi đến window nhìn thấy trên cùng.
5. Fullscreen page che page thấp hơn và page thấp hơn hiện lại sau khi đóng.
6. Warm/show đồng thời cùng page chỉ tạo một cached instance.

