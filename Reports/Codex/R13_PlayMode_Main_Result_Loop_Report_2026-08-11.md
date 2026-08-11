# R13 PlayMode Main Result and Bank Loop Report — 2026-08-11

## Mục tiêu

Kiểm chứng Main Game và Bank bằng `AppScene` cùng presenter/prefab thật: từ Home vào Game, Fail/Restart, Win, meta flow trước result, Continue sang level kế tiếp; đồng thời bấm từ Bank vào bank-mode rồi quay lại Bank.

## Phạm vi chạy

- `Home/StartBtn` mở Game và chờ `GameSessionState.Playing`.
- Ba wrong double-tap đi qua cùng `GameplayManager.ConsumeDoubleTap`, delay resolve và transition thật; mạng giảm 3 → 2 → 1 → 0 rồi mở Fail.
- Default nguồn `reward_unlock_level=0`, `revive_free_logic=0` cùng null ad provider làm `ReviveButton` ẩn; test không bật nút giả.
- `RestartButton` thật đóng Fail, load lại level 1, phục hồi 3 mạng và không tăng current level.
- AutoComplete chỉ dùng để đưa session thật tới Won; settlement, feedback delay và result route vẫn chạy nguyên lifecycle.
- Thắng đầu tiên đi qua Streak Lit → Settle, Award Collect nếu có, Streak Continue rồi mới mở Win đúng `StreakFlowCoordinator`.
- `Win/Next` thật đóng Win, load level 2 trên Game page và tăng `GameStateRuntime.Current.CurrentLevel` đúng một lần.
- `Bank/SPCard` thật mở panel SP; `SpecialRow1` được materialize động và Button của row launch Game bằng parameters nguồn.
- Gameplay dựng puzzle ở `GameplaySessionMode.Bank`, hiện `ReturnBankBtn`; bấm nút này đóng Game, show lại cùng Bank instance và reset về root panel đúng lifecycle nguồn.

## Test-only boundary

- `LivesForTests`, `SolutionColumnForTests` và `DoubleTapForTests` chỉ compile với `UNITY_INCLUDE_TESTS`.
- `ConsumeDoubleTap` được đổi từ `void` sang trả lại chính `SessionActionResult`; call site runtime bỏ qua return nên thứ tự xử lý, feedback và delay không đổi.
- Toàn run dùng `GameStateRuntime.OverrideForTests`, không ghi hoặc flush save thật.

## Kết quả

- Unity PlayMode Test Runner: **4 passed, 0 failed, 0 skipped, 0 inconclusive**.
- Unity EditMode Test Runner sau thay đổi: **508 passed, 0 failed, 0 skipped, 0 inconclusive**.
- Unity compile/reload sạch; không thêm runtime log.

## Còn lại của R13

- Provider-rewarded revive trong PlayMode; null-provider gate đã đạt.
- Bank Win/Fail → Next; launch và Return Bank đã đạt bằng Button thật.
- App-kill/pause/focus ở từng transition và visual/pixel parity Win/Fail.
