# R13 PlayMode Main Result and Bank Loop Report — 2026-08-11

## Mục tiêu

Kiểm chứng Main Game và Bank bằng `AppScene` cùng presenter/prefab thật: từ Home vào Game, Fail/Restart, Win, meta flow trước result, Continue sang level kế tiếp; đồng thời bấm từ Bank vào bank-mode, quay lại Bank và thắng để sang level Bank kế tiếp.

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
- Ở một run độc lập, AutoComplete SP #1 mở Win; `Next` thật load SP #2, giữ `GameplaySessionMode.Bank` và đóng Win đúng một lần.
- Godot không truyền lại `from_bank_browser` trong Bank Next. Unity đã bỏ key này khỏi next request và phát `SessionPresentationChanged` khi reload session, vì vậy `ReturnBankBtn` ẩn ở SP #2 dù Game page được tái sử dụng.
- Ba wrong double-tap ở SP #2 mở Fail; `RestartButton` thật load lại đúng SP #2, phục hồi 3 mạng, giữ bank-mode và không làm direct-return xuất hiện lại.
- Retry sau Bank Next giữ `bank_total`, pool flags và strategy fields như `bank_params.duplicate()` nguồn; cờ direct-return được xử lý độc lập, không còn làm mất metadata điều hướng.
- Test provider được gắn vào `AdRuntime` trước khi Game/Fail được materialize, nên luồng đi qua đúng `AdService` và binding của `UIManager`, không gọi tắt presenter.
- Ở Fail Main, bấm `ReviveButton` tạo reward show đúng placement/position; trước `ad_rewarded` session vẫn Failed. Callback reward hồi đúng 1 mạng, đóng Fail và tăng revive stat đúng một lần; `ad_closed` sau đó không settle lặp.
- Ở vòng Fail kế tiếp, `ad_closed` không có reward giữ session Failed/0 mạng, giữ Fail mở và mở lại `ReviveButton`, đúng nguyên tắc nguồn “close không cấp thưởng”.

## Test-only boundary

- `LivesForTests`, `SolutionColumnForTests` và `DoubleTapForTests` chỉ compile với `UNITY_INCLUDE_TESTS`.
- `PlayModeAdProvider` nằm trong `Meowdoku.PlayModeTests`, được tạo động trên GameObject của `AdRuntime` rồi bị hủy cùng test scene; player/runtime production không reference provider này.
- `ConsumeDoubleTap` được đổi từ `void` sang trả lại chính `SessionActionResult`; call site runtime bỏ qua return nên thứ tự xử lý, feedback và delay không đổi.
- Toàn run dùng `GameStateRuntime.OverrideForTests`, không ghi hoặc flush save thật.

## Kết quả

- Unity PlayMode Test Runner: **6 passed, 0 failed, 0 skipped, 0 inconclusive**.
- Unity EditMode Test Runner sau thay đổi: **510 passed, 0 failed, 0 skipped, 0 inconclusive**.
- Unity compile/reload sạch; không thêm runtime log.
- Ba named-event bridge bỏ qua `AssetImportWorker`; sau khi compile, PlayMode command được Editor chính nhận ngay thay vì đôi lúc bị worker nuốt.

## Còn lại của R13

- Null-provider gate và provider-rewarded revive success/close-failure đã đạt bằng AppScene PlayMode.
- Launch, Win, Next, Fail, Restart và Return Bank đã đạt bằng Button thật cho SP.
- Launch/Next matrix cho Regular/LK/LK Modified/LK Style/GC và pool-soak.
- App-kill/pause/focus ở từng transition và visual/pixel parity Win/Fail.
