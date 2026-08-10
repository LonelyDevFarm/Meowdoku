# R12 How-to-play Report — 2026-08-10

## Phạm vi

Port hai page How-to-play tồn tại thật trong bản Godot:

- `how_to_play_page`: popup toàn màn hình do nút thông tin trong Gameplay mở.
- `how_to_play_paged_page`: popup ba bước do Settings mở khi `rule_text` bật entry.

Không gộp hai page và không tạo nội dung/luật mới.

## Nguồn đã đối chiếu trực tiếp

- `scripts/module/how_to_play/view/how_to_play_page.gd`
- `scripts/module/how_to_play/view/how_to_play_paged_page.gd`
- `scripts/module/how_to_play/ui/how_to_play_page.tscn`
- `scripts/module/how_to_play/ui/how_to_play_paged_page.tscn`
- `scripts/module/gameplay/view/cell_view.gd`
- `assets/prefab/cell.tscn`
- `assets/animation/GenericPopup.res`
- `scripts/module/game/view/base_game_page.gd`
- `scripts/module/setting/view/setting_page.gd`

## Contract và dữ liệu demo

`HowToPlayContract` giữ nguyên matrix màu và tọa độ Godot, trong đó
`Vector2i.x/y` được ghi rõ thành `Row/Column`:

- Full page: ba board 3×5, error frame 72, wave bắt đầu ở frame
  `134/158/182`, `72/92` và `72`; cross cách nhau 5 frame.
- Paged page: board 4×4, 5×5, 4×4; wave bắt đầu ở frame
  `163/194/223`, `72/108` và `72`; cross cách nhau 6 frame.
- Start delay đều 6 frame ở 60 FPS.
- Full page gap 12 frame, riêng vòng cuối 24 frame.
- Paged page giữ kết quả 1,6 giây và slide 16 frame qua 900 px bằng
  OutQuart.
- `CrossOutAppear_2 = 0,35 s`, `ErrorAppear_2 = 1,1 s`,
  `DemoDisappear = 0,1 s` lấy trực tiếp từ `cell.tscn`.
- Palette B/P/Y dùng đúng index 8/1/5 của palette board mặc định.

## Presenter và lifecycle

- `HowToPlayPagePresenter` chạy đúng chuỗi: reset toàn bộ, điền sẵn board
  2/3, phát board 1 rồi luân phiên clear/play; chạm bất kỳ vị trí hoặc Back
  đóng page như source.
- `HowToPlayPagedPagePresenter` có Previous/Next/Got it, close/back,
  reset demo khi đổi page, slide theo hướng và lặp demo của page hiện tại.
- Hai presenter dùng token + coroutine cleanup; tween/coroutine/cell state được
  dừng khi hide/destroy/reopen.
- Sound silence có serialized boundary `SoundService`; bật khi page show và
  luôn trả về false khi close/hide/destroy.
- Caption dùng localization catalog, chỉ highlight keyword tiếng Anh/Trung
  giống dictionary nguồn; title và nút refresh theo locale.
- `CellView` có adapter demo riêng nên không đổi state/input flow của bàn chơi.

## Layout và prefab installer

`HowToPlayPagePrefabInstaller` dựng cây bằng Unity Editor API:

- Full page dùng viewport nguồn 1080×2400, overlay 0,85, ba card
  717×434, divider và board/cell fixed layout.
- Paged page dùng dialog 900×1450 tại `(90,475)`, title bar 133 px,
  BoardClip 900×861, caption và button row đúng tọa độ source.
- Cell là nested `Cell.prefab` có sẵn trong prefab page, tổng 45 cell ở full
  page và 57 cell ở paged page; vòng demo không instantiate GameObject.
- `RoundedImageView` được mở rộng per-corner để title bar có hai góc trên 60
  và hai góc dưới vuông đúng StyleBox nguồn.
- Registry installer chỉ thêm `HowToPlay` và `HowToPlayPaged` sau khi hai
  prefab thật tồn tại.

## Kiểm chứng hiện tại

- Unity Roslyn compile sạch `Meowdoku.Core`, `Meowdoku.Gameplay`,
  `Meowdoku.Editor` và `Meowdoku.EditModeTests` bằng response file hiện hành.
- Unity Editor đã tự rebuild `Meowdoku.Editor.dll` và
  `Meowdoku.EditModeTests.dll` sau import, không có compiler error.
- Fixture mới khóa matrix, frame timing, scale, palette, keyword highlight,
  prefab board/cell count và registry expectation.
- Unity Auto Refresh đã sinh `HowToPlayPage.prefab` lúc 19:22:02 và
  `HowToPlayPagedPage.prefab` lúc 19:22:06; `UIRegistry.asset` đã có entry
  `UiName.HowToPlay` (7) và `UiName.HowToPlayPaged` (8).
- Hai prefab không có marker `m_Script: {fileID: 0}`.

## Còn chờ

- Unity Test Runner chưa được điều khiển trong lượt không có GUI.
- Cần PlayMode xác nhận demo loop, Previous/Next/Got it, mọi cách đóng,
  silence restore, locale refresh và reopen không còn tween/coroutine cũ.
- Game scene hiện chưa được composition vào `UIManager`; vì vậy info button
  của Gameplay chỉ nối route sau khi R10/R12 scene composition hoàn tất.
- Spine/particle chi tiết trong `CatIconAppear` vẫn dùng adapter Cell hiện có;
  pixel/VFX parity cuối thuộc R17.
