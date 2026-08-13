# R6 Input Parity Closure — 2026-08-13

## Kết quả

Đã khép `P-INPUT-001..017` cho input Main Game theo hành vi nguồn. Tap, toggle, double-tap, swipe interpolation, CAT-start, pointer lifecycle và one-step Undo đều có bằng chứng tự động; desktop gesture trước đó đã được người dùng nghiệm thu trực tiếp.

## Đối chiếu nguồn

- `scripts/module/gameplay/view/board_view.gd`
- `scripts/module/game/input/board_gesture_recognizer.gd`
- `scripts/module/game/input/swipe_guard_recognizer.gd`
- `scripts/module/game/input/operations/*`
- `scripts/module/game/view/base_game_page.gd`

## Thay đổi production

- Thêm `InputConfigSet` vào `AbConfigRuntime`: `doubletap_protect` reload ở AppStart, `swipe_protect` reload ở GameStart và cùng instance được truyền tới Main/Tutorial.
- `GameSession.DoubleTap` port `consume_prior_tap_before`: step tap đầu cùng cell được pop, rồi correct/wrong double-tap ghi một step từ trạng thái gốc tới CAT/ERROR.
- Không đổi cảm giác input đã được duyệt và không thêm runtime log.

## Bằng chứng

- Tap EMPTY→MARK và MARK→EMPTY tức thời.
- Double-tap correct kết thúc CAT; double-tap wrong kết thúc ERROR; một Undo trở thẳng về trạng thái trước tap đầu.
- Swipe bắt đầu từ CAT lấy target ở cell hợp lệ đầu tiên; interpolation không thay cell ngoài đường.
- CAT/ERROR/LOCKED không bị đổi trái phép.
- Ra ngoài/re-entry, pointer-up ngoài board, focus loss và multi-pointer đều giữ đúng ownership/lifecycle.
- Cùng quỹ đạo mô phỏng ở 30/60/120 FPS tạo cùng chuỗi cell.

Unity Test Runner:

- Full EditMode: **652 passed, 0 failed** — 141,902 giây.
- Platform PlayMode: **11 passed, 0 failed** — 113,317 giây.

Unity Test Runner không cung cấp phép chiếu Screen/Game View ổn định để tạo synthetic screen pointer theo cell, nên adapter tọa độ màn hình không được giả lập bằng tọa độ đoán. Phần này dùng fixture lifecycle trực tiếp và bằng chứng nghiệm thu desktop trước đó; touch/notch/device thật vẫn là cổng R17.
