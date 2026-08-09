# R11 — Tutorial State Machine Report

Ngày: 2026-08-10  
Nguồn đối chiếu: `D:\Projects\_GameExtract\Main_Meokdoku`

## Kết quả

Đã port lớp domain của tutorial 4×4 theo `tutorial_page.gd`, chưa dựng presenter/UI. Phần mới không tự sáng tạo luật và không tạo runtime log.

- Guide puzzle id `51`, pattern `guide`, board width `919`.
- Region map: `[[0,1,2,1],[0,1,1,1],[0,0,3,1],[0,3,3,1]]`.
- Solution columns: `[2,0,3,1]`; color map: `[8,4,10,1]`.
- Bảy bước Current: first cat → confirm → 6 row/column marks → second cat → 3 neighbor marks → third cat → free play/last cat → finish confirm.
- Mọi bước đặt cat cần hai tap cùng cell trong cửa sổ mặc định `0,35 s`.
- Check/IQ không có confirm riêng sau first cat; mỗi trong sáu interaction đi qua feedback gate.
- IQ chạy từ `60` đến `180`, tăng `20` sau mỗi interaction.
- Hint step 7 là sáu lượt reveal/apply: blue row, pink row, last cat; người chơi vẫn có thể hoàn thành từng pha bằng tay.
- Không lưu tiến độ từng step. Reset/recreate trở về bước đầu; chỉ completion mới ghi `tutorial_done`.
- Completion nguồn mở `GAME` với `level_index=1`, sau đó ẩn `TUTORIAL`; không route Home ngay lúc hoàn thành.

## File thay đổi

- `Assets/_Project/Scripts/Core/Config/InputAndLayoutConfigs.cs`
- `Assets/_Project/Scripts/Core/Config/DefaultConfigProfile.cs`
- `Assets/_Project/Scripts/Core/LevelEntry.cs`
- `Assets/_Project/Scripts/Core/Tutorial/TutorialPuzzle.cs`
- `Assets/_Project/Scripts/Core/Tutorial/TutorialStateMachine.cs`
- `Assets/_Project/Tests/EditMode/AbConfigTests.cs`
- `Assets/_Project/Tests/EditMode/TutorialStateMachineTests.cs`
- `Docs/SourceMap.md`
- `Docs/ParityChecklist.md`
- `PORTING_ROADMAP.md`

## Kiểm tra

- Core compile sạch bằng Unity Roslyn, gồm cả các source mới chưa được AssetDatabase refresh.
- Gameplay compile sạch.
- Editor compile sạch.
- EditMode test assembly compile sạch với 11 test tutorial mới, 1 config test mới và 2 config source cases mới.
- Chưa chạy Unity Test Runner và chưa kiểm tra PlayMode theo yêu cầu gom test của người dùng; checklist giữ trạng thái `[~]`.

## Sai lệch đã sửa so với báo cáo/roadmap cũ

- Đặt cat là double-tap, không phải single tap.
- Check/IQ bỏ bước confirm one-per-color riêng.
- Hint không phải ba click tự hoàn tất; mỗi pha có reveal rồi apply.
- Hoàn thành tutorial route Game level 1, không route Home.

## Việc kế tiếp

Dựng `TutorialPage` presenter và hierarchy theo source: BoardContainer, MaskLayer, Message/SubMessage, HintTool, Confirm, HandHint, IQ, feedback và finish effects; sau đó nối state machine, input blocking và completion route.
