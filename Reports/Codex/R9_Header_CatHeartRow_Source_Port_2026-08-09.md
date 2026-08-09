# R9 — Header và CatHeartRow nguồn

## Kết quả

- Chuyển `HUD/TopBar` thành `HUD/Header`; giữ `ScoreDisplay` và serialized reference hiện có.
- Thêm `BackBtn` và `SettingsBtn` bằng `round_btn_base.png`, `icon_back.png`, `icon_settings.png` đúng kích thước/offset nguồn.
- Thêm `LevelDisplay` và giữ `ScoreDisplay` theo layout hai cột mà `combo_feedback_view.gd` bật mặc định.
- Thêm `HUD/CatHeartRow/Target/{CatCountBg,CatFaceIcon,CatCountLabel}`.
- Chuyển nhánh mạng cũ thành `HUD/CatHeartRow/HeartBg/LifeSlot1..3` thay vì để rời ở HUD.
- Mở rộng phép tính VBox để đặt cả Header và CatHeartRow, không hard-code theo vị trí Board hiện tại.
- `GameplayManager` phát `GameplayHudState` từ session thật; progress là `puzzleSize - RemainingCats`, nên restore, Undo, Locate, Hint và AutoComplete dùng chung dữ liệu.
- Progress đổi sẽ pulse đúng track nguồn: scale `1 → 1.1 → 1` trong `0.6 s`.

## Gemini 009

Báo cáo có ích để định tuyến node, asset, ratio và điều kiện visible, nên giảm phần quét rộng. Tuy nhiên trạng thái `COMPLETE` không đáng tin hoàn toàn: báo cáo bỏ `HeaderAdaptHolder`, không ghi override Level/Score đang được bật trong `combo_feedback_view.gd` và thiếu nhiều offset cần dựng. Codex đã spot-check trực tiếp các file liên quan trước khi port.

## Xác minh

- Gameplay assembly compile sạch bằng Unity Roslyn.
- Editor assembly compile sạch bằng Unity Roslyn.
- Unity project đang mở nên scene/import/test thực tế chờ Refresh.

## Chưa thuộc lượt này

- Back/Settings popup và navigation contract.
- Last-chance Tips bubble.
- Rounded StyleBox adapter, hard tag và các header AB variant.
- FunctionArea, BottomTools và ad SDK adapter.
