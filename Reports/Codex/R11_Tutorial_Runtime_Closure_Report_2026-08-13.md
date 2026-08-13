# R11 Tutorial Runtime Closure — 2026-08-13

## Kết quả

Đã khép logic/runtime `P-TUT-001..010` theo `tutorial_page.gd`: Tutorial 4×4 đi đủ bảy bước, từ chối ô ngoài policy, dùng double-tap cho ba lần đặt mèo, chạy sáu lượt hint, lưu hoàn tất rồi vào Main Game level 1. Reopen cùng page reset sạch và không tăng số `CellView` của Board/Mask.

Unity 6000.3.19f1 đạt:

- Full EditMode: **677/677**, 0 fail, 0 skip, 141,102 giây.
- Platform PlayMode: **15/15**, 0 fail, 0 skip, 142,540 giây.
- Refresh/compile: không có `error CS`, không thêm runtime log.

## Sai khác production đã sửa

- `TutorialDiagonalConfig` và `GuideFeedbackConfig` trước đây được presenter tự tạo bằng default cục bộ, nên variant AppStart từ runtime không thể tới Tutorial. Hai config nay thuộc `InputConfigSet` và presenter dùng đúng instance từ `AbConfigRuntime.Input`, cùng cách đã áp dụng cho double-tap/swipe.
- Nối vibration tại đúng hành động nguồn: mark Level2, cat Level3 và hoàn tất Level5; unsupported platform giữ no-op an toàn qua `VibrationService` hiện có.
- Thêm test surface nội bộ chỉ dưới `UNITY_INCLUDE_TESTS`; không dùng runtime lookup mới và không thay đổi hierarchy/prefab serialization.

## Bằng chứng parity

- 11 fixture `TutorialStateMachineTests` khóa puzzle guide id 51, pattern lookup, double-tap 0,35 giây, Default/Check/IQ flow, diagonal presentation, reset, completion idempotent và hint reveal/apply.
- `PlatformTutorial_FullFlowRoutesGameAndReopensCleanly` chạy trên `AppScene` và prefab thật:
  - startup route tới Tutorial khi `tutorial_done=false`;
  - board 4×4, config default nguồn và input group hoạt động;
  - mọi `Graphic` dưới `Root/Mask` có `raycastTarget=false`;
  - full flow first cat → confirm → 6 marks → second cat → 3 neighbor marks → third cat → 6 hint presses → finish;
  - board bị khóa trong transition;
  - `tutorial_done=true`, Tutorial đóng, Game level 1 vào `Playing`;
  - mở lại cùng presenter về `PlaceFirstCat`, board rỗng và tổng CellView không tăng.

## Phần còn mở

- Godot dùng Spine hand và IQ `CPUParticles2D`; Unity hiện giữ static-hand/DOTween và chưa giả lập particle khác nguồn.
- Pixel/timing cảm nhận trên nhiều aspect ratio, touch, vibration và hard-kill thiết bị thật thuộc R17.
- Các khoảng tiến độ tổng quan không đổi vì đây là closure chi tiết trong phần Tutorial đã nằm trong khoảng UI hiện tại.
