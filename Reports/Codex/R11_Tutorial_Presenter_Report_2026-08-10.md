# R11 Tutorial Presenter — 2026-08-10

## Kết quả

Đã nối domain Tutorial 4×4 vào một `UIFrameWindow` thật và bổ sung installer tạo prefab bằng Unity Editor API. Không tạo registry/startup giả khi Splash và Home chưa tồn tại.

Các assembly Core, Gameplay, Editor và fixture Tutorial đều biên dịch sạch bằng Roslyn/reference do Unity 6000.3.19f1 tạo. Unity Editor đang mở chưa nhận external file change nên prefab chỉ được sinh ở lần Refresh kế tiếp; Test Runner và PlayMode vẫn để trạng thái chờ.

Sau Refresh đầu tiên, Unity phát hiện installer dùng trực tiếp `Meowdoku.Core.UI` nhưng `Meowdoku.Editor.asmdef` mới chỉ tham chiếu Gameplay. Đã bổ sung reference `Meowdoku.Core` trực tiếp theo quy tắc asmdef (không dựa vào dependency bắc cầu) và biên dịch lại Editor sạch.

## Phạm vi đã port

- `TutorialPagePresenter` sở hữu state machine, BoardView, input policy, message/control, feedback và completion route.
- Board guide dùng đúng entry `pattern=guide`, id 51, kích thước 4×4 và visible width 919 px.
- Tọa độ Godot Y-down được đổi sang UGUI Y-up; Message/SubMessage bám theo board bounds thật với gap nguồn 78/30 px.
- Mask alpha 0,75 và mask/mirror cell clone dùng Cell prefab nguồn; toàn bộ graphic trên clone/mask không bắt raycast.
- Input board bị khóa trong transition/feedback, mở lại chỉ ở phase tương tác.
- Tap hand pulse, swipe hand loop 0,15 + 0,3/ô + 0,1 + 0,15 + fade 0,2 + wait 0,35 giây.
- Panel appear 0,2 + 0,133333 giây; confirm loop tổng 1,2 giây; mask fade 0,12 giây.
- Check feedback 0,95 giây; IQ fill 0,4 giây; default finish dùng 30 confetti theo range/timing nguồn.
- Completion lưu `tutorial_done`, mở Game với `level_index=1`, sau đó đóng Tutorial.
- Hide/destroy/reopen hủy tween, event, mask clone, board cells và effect object; state mới luôn trở về bước đầu.

## Hierarchy prefab

```text
TutorialPage
├─ Background
└─ Root
   ├─ Board
   │  ├─ BoardView
   │  └─ HighlightOverlay
   │     └─ SelectFrame
   ├─ Mask
   │  ├─ Background
   │  └─ Cells
   ├─ Guidance
   │  ├─ Message
   │  ├─ SubMessage
   │  ├─ Hint
   │  ├─ Confirm
   │  └─ Hand
   └─ Feedback
      ├─ SuccessCheck
      ├─ IqBar
      └─ Effects
```

## Adapter có chủ đích

- Project chưa có Spine runtime tương đương Godot `SpineSprite`, nên dùng đúng sprite `ui_guide_hand_0` và DOTween pulse/swipe. Không tạo animation xương giả.
- Default offline dùng confetti và đã port. IQ variant phụ thuộc cụm `CPUParticles2D`; chưa giả lập bằng effect khác để tránh sai nguồn.
- Localization runtime nằm ở R12; presenter tạm dùng chuỗi tiếng Anh nguồn với Unity rich-text color, không sáng tác copy mới.

## File chính

- `Assets/_Project/Scripts/Gameplay/TutorialPagePresenter.cs`
- `Assets/_Project/Scripts/Gameplay/TutorialFinishEffects.cs`
- `Assets/_Project/Scripts/Gameplay/RoundedImageView.cs`
- `Assets/_Project/Editor/TutorialPagePrefabInstaller.cs`
- `Assets/_Project/Scripts/Core/Tutorial/TutorialStateMachine.cs`
- `Assets/_Project/Scripts/Gameplay/BoardView.cs`

## Cần xác nhận sau Refresh

1. Prefab `Assets/_Project/Prefabs/UI/TutorialPage.prefab` được sinh và không có Console error.
2. Chạy Unity Test Runner cho EditMode suite.
3. Sau khi registry/startup có đủ Splash/Home/Tutorial/Game, kiểm tra toàn bộ flow 7 bước, reopen/reset và route Game level 1.
4. So hình/timing trên 1080×1920 và 1080×2400; kiểm tra mask không lọt input và pool Cell sạch sau đóng/mở lại.
