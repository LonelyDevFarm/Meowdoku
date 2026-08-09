# R9 — HintOverlay, RuleBar và ErrorAppear

Ngày: 2026-08-09

## Kết quả

- Sửa sai lệch domain cũ: double-tap sai giữ `CellStateType.ERROR` như `BoardView.play_error_feedback` của Godot; chỉ snapshot cấp cho solver mới coi ERROR như MARK.
- Port animation mặc định `ErrorAppear`: X trắng xuất hiện trước, mờ đi rồi X đỏ `#FD6A2E` bật ở khoảng 0,599 giây và nảy về scale 1.
- Bổ sung Cell hint visuals từ asset nguồn: `icon_prompt_frame.png`, `icon_mark_white_3.png`; pulse alpha 50/255 ↔ 1 với nửa chu kỳ 0,65 giây, fade-out 0,12 giây và preview delay 0,317 giây + stagger.
- Dựng `HintOverlay` mặc định: dim alpha 0,749, banner 900×190, căn cách board 15 px, Apply/Detail/Dismiss lifecycle và clone Cell tạm thời trên layer highlight. Clone không sửa board thật và được hủy khi đóng.
- Dựng `rule_info_bar_v0` mặc định: khung 1080×170, ba rule pill và text tiếng Anh đúng CSV nguồn. `rule_highlight=0` nên pulse vi phạm mặc định tắt; presenter vẫn giữ đúng timing AB-on là hai nhịp 0,6 giây.
- Hierarchy được gom theo cây:
  - `Canvas/HUD/RuleBar/{Background,Rules,Highlights}`
  - `Canvas/Overlays/HintOverlay/{Dim,Highlights,Banner,Buttons}`
  - `Cell/HintVisuals/{HintLight,PromptFrame,PromptCross}`

## Chuyển thể Unity bắt buộc

- Godot tạo Cell tạm trong CanvasLayer; Unity tạo prefab Cell tạm dưới `HintOverlay/Highlights` và quy đổi tọa độ bằng RectTransform.
- Godot StyleBox bo góc không thể copy trực tiếp. Hiện giữ đúng màu/kích thước nhưng chưa thay bằng texture không liên quan; mesh bo góc và pixel parity cuối cùng vẫn thuộc R7.
- Localization runtime chưa port; text hiện dùng đúng cột tiếng Anh trong `translations.csv`, không hiện localization key cho người chơi.

## Kiểm tra

- `Meowdoku.Core`, `Meowdoku.Gameplay`, `Meowdoku.Editor` và `Meowdoku.EditModeTests`: Roslyn compile sạch.
- Hai regression mới xác nhận wrong double-tap trả ERROR và fallback text lấy đúng câu nguồn.
- Runner độc lập đạt 252 case. Bảy case LevelGenerator cũ không chạy vì runner .NET ngoài Unity thiếu `Array.Fill/Array.Reverse`; đây là giới hạn runner đã biết, không phải lỗi thay đổi này.
- Unity đang mở nhưng chưa import file installer mới; cần Refresh để Unity sinh `.meta`, chạy installer và ghi scene/prefab. Sau đó cần PlayMode kiểm tra layout/timing bằng mắt.

## Gemini

`GEM-R9-006` hữu ích để dò bước đầu, nhưng nhận diện sai RuleBar mặc định là v4 và sai một số đường dẫn. Codex đã kiểm tra trực tiếp source và chọn đúng `rule_info_bar_v0` theo `rule_text=0`, `rule_highlight=0`.

`GEM-R9-007` cho bước kế tiếp đã có sẵn. Báo cáo giúp chỉ đường tới Spine/audio nhưng còn thiếu bảng config/decision chi tiết, nên bước sau vẫn cần spot-check nguồn trước khi sửa.
