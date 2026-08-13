# R7 — Board safe-area/aspect closure

Ngày kiểm tra: 2026-08-13  
Unity: 6000.3.19f1

## Phạm vi nguồn

- `project.godot`: viewport 1080×2400, `canvas_items`, `keep_width`.
- `scripts/module/game/ui/game_page.tscn`: VBox của Header, CatHeart, RuleBar, Board và BottomTools; root nhận safe top/bottom.
- `scripts/module/game/view/compont/header_adapt_holder.gd`: nội suy 0→65 trong khoảng cao 1920→2400.
- `scripts/module/ui/ui_manager.gd::_apply_safe_area`: dịch safe top/bottom và collapse HeaderAdapt khi có top inset.
- `board_no_fuction.tres`, `board_big_no_fuction.tres`: minimum height và stretch ratio của hai profile Board.

## Thay đổi

- Tách overload nội bộ trong `GameplayPageLayoutPresenter` để đường runtime và test cùng dùng chính xác một phép tính/đặt transform.
- Thêm test seam chỉ tồn tại khi `UNITY_INCLUDE_TESTS`; không thêm provider, lookup runtime hay thuật toán layout thứ hai.
- Thêm Platform PlayMode matrix mở `GamePage.prefab` qua AppScene và kiểm tra trực tiếp các RectTransform đã serialize.

## Matrix runtime

- 1080×1920, không inset.
- 1080×2160, không inset.
- 1080×2400, không inset.
- 1080×1920, safe top/bottom 96/54.
- 1080×2400, safe top/bottom 120/80.
- Mọi profile khóa tâm Header/CatHeart/RuleBar/Board/BottomTools theo `SourceGameplayPageLayout`, thứ tự từ trên xuống, board height sau scale và mép trên/dưới trong vùng an toàn.

## Kết quả

- Platform PlayMode: **24 passed, 0 failed, 0 skipped**, thời lượng **311,260 giây**.
- Full EditMode ổn định gần nhất: **679/679**. Không chạy lại vì contract thuần không đổi; Unity đã compile toàn bộ Gameplay/Editor/EditMode/PlayMode assembly sạch và full Platform regression bao phủ page runtime vừa chỉnh.
- Không thêm runtime log, không sửa prefab/scene YAML bằng tay, không commit.

## Trạng thái

- `P-BOARD-008`: hoàn thành về runtime/simulation.
- `P-BOARD-009`: còn mở cho pixel reference 1080×1920.
- Notch/touch trên thiết bị vật lý vẫn thuộc R17 và không được suy ra từ simulation.
