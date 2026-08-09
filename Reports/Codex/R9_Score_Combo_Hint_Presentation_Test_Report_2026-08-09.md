# R9 Score/Combo + Hint Presentation Report — 2026-08-09

## Kết quả

- Port P0 presenter từ `combo_feedback_view.gd`: score header, bitmap score bubble, bitmap deduction bubble, Encourage art, rolling score và fixed pools.
- Thêm batch feedback timing plan và Win completion gate. Khi không có presenter, transition vẫn settle ngay; presenter chỉ có quyền kéo dài deadline đồng bộ lúc nhận batch.
- Port contract Hint UI: lifecycle request/apply/cancel, localization key nguồn, strategy label, highlight cell và preview stagger cho R1-mark/R2/R3/R4, chain-detail availability.
- Thêm editor-time installer idempotent để tạo và serialize UI bằng Unity API. Không sửa tay YAML scene/prefab và không dùng runtime lookup/bootstrap.

## Đối chiếu nguồn trực tiếp

- `combo_feedback_view.gd`: bubble 83 px, gap 10 px, cat top 50 px; clamp; score roll 0.35 s; fly delay 0.8/1.367/1.45 s; life sequence gap 0.3 s; completion/reset generation.
- `level_flow_score.tscn`: bitmap glyph, separation -18, Appear 1.0166667 s, scale 0.4 → 1.15 → 0.98 → 1 và alpha timeline.
- `level_flow_deduction.tscn`: bitmap glyph riêng, separation -6 và Appear 1.0166667 s.
- `level_encourage.tscn`: dùng sprite nguồn 01–06; không hard-code width theo đề xuất report.
- `game_page.tscn`: Score title/value 50/58 px, Roboto bold, màu `(0.576, 0.353, 0.353, 1)`, right-anchor offsets nguồn.
- `hint_overlay.gd` và `_build_hint_highlights`: overlay lifecycle, strategy/description key, R1/R2/R3/R4/chain flow và stagger 0.06/0.1 s.

## Sai lệch trong báo cáo Gemini đã loại bỏ

- `GEM-R9-002`: đề xuất thay bitmap digit bằng TMP và hard-code Encourage half-width; không dùng vì nguồn có bitmap glyph và đo kích thước động.
- `GEM-R9-003/004`: không cung cấp `file:line` như yêu cầu nên mọi dữ kiện quan trọng đều được spot-check.
- `GEM-R9-004`: `lost_index` thực tế được lấy từ `_lives - 1` trước khi decrement, không phải `_mistake_count - 1`.
- Fail dựa trên lives/session state, không dùng điều kiện `_mistake_count >= _lives` như report mô tả.
- Wrong-resolution hiện có variant 0.4/0.6 s theo config; report chỉ ghi cố định 0.4 s.
- Fish slot nằm tại `scripts/module/game/ui/compont/fish_slot.tscn`, không phải đường dẫn report nêu.

## Kiểm thử

- Runtime Gameplay assembly: compile sạch.
- Editor installer: compile sạch.
- Test mới: 6/6 pass.
- Regression trước đó chạy với Gameplay assembly mới: 244/244 pass.
- Tổng: 250/250 pass.

## Cổng PlayMode

Unity Editor hiện không tự refresh thay đổi ngoài editor. Sau `Assets > Refresh`, `GameplayFeedbackSceneInstaller` sẽ tạo và serialize `GameplayFeedbackPresenter` trong `GameplayScene`. Cần kiểm tra trực quan score header, bubble/Encourage/deduction và clamp ở hai mép board. Multiplier/skill pair, trail/burst, heart/life, HintOverlay thực tế, RuleBar và cat-hand/Spine vẫn là phần tiếp theo; không coi chúng là hoàn thành.
