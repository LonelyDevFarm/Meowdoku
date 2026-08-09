# Parity Checklist — Godot ↔ Unity

> Cập nhật: 2026-08-10  
> Trạng thái ban đầu chủ ý để trống; đánh dấu sau khi có bằng chứng test hoặc recording đối chiếu.

## Cách ghi bằng chứng

Mỗi case hoàn thành phải ghi một trong các bằng chứng cạnh checkbox hoặc trong test report:

- Tên automated test.
- Video/screenshot Godot và Unity cùng thao tác.
- Fixture đầu vào + output kỳ vọng lấy từ mã nguồn.

Không dùng cảm giác “có vẻ giống” để đóng case.

## P-BOOT — Startup và navigation

- `[ ]` `P-BOOT-001`: Cold start hiển thị splash đúng thời lượng tối thiểu.
- `[ ]` `P-BOOT-002`: Lần đầu chưa hoàn thành tutorial route đến Tutorial.
- `[ ]` `P-BOOT-003`: Đã hoàn thành tutorial route đến Home.
- `[ ]` `P-BOOT-004`: Config/state lỗi hoặc thiếu vẫn dùng default và không kẹt splash.
- `[ ]` `P-BOOT-005`: Back trên Home mở confirm/thoát giống nguồn.
- `[ ]` `P-BOOT-006`: Popup khóa input page phía dưới.
- `[ ]` `P-BOOT-007`: Không thể double-open cùng page trong lúc transition.
- `[ ]` `P-BOOT-008`: Mất focus/resume không tạo session/game page trùng.

## P-SAVE — State và persistence

- `[x]` `P-SAVE-001`: Save → restart khôi phục current level/strategy. Bằng chứng: `GameStateRepositoryTests.PlayerState_RoundTripsP0Fields`.
- `[x]` `P-SAVE-002`: Khôi phục tool counts và settings. Bằng chứng: `GameStateRepositoryTests.PlayerState_RoundTripsP0Fields`.
- `[ ]` `P-SAVE-003`: Atomic write không thay slot tốt khi verify thất bại.
- `[x]` `P-SAVE-004`: Slot chính hỏng thì đọc slot dự phòng. Bằng chứng: `SaveStoreTests.DualSlot_CorruptPrimaryFallsBackToPreviousSlot`.
- `[ ]` `P-SAVE-005`: Cả hai slot lỗi thì dùng state mặc định an toàn.
- `[ ]` `P-SAVE-006`: Legacy save migration chỉ chạy đúng một lần.
- `[ ]` `P-SAVE-007`: App kill trong ván khôi phục snapshot theo config mặc định.
- `[ ]` `P-SAVE-008`: Win/fail/restart xóa hoặc giữ snapshot đúng thời điểm.
- `[~]` `P-SAVE-009`: Endgame runtime không mã hóa/verify/fsync trên main thread; background coalesce, immutable snapshot và lifecycle flush đã có smoke/EditMode test, còn app-kill trên thiết bị thật.

## P-LEVEL — Bank và level selection

- `[x]` `P-LEVEL-001`: 5.411 level thường có region/solution hợp lệ.
- `[ ]` `P-LEVEL-002`: Size của level 1–100 và chu kỳ 101+ khớp nguồn.
- `[ ]` `P-LEVEL-003`: Special level mapping trả đúng SP/LK entry.
- `[ ]` `P-LEVEL-004`: Hard level chọn rank/tier đúng.
- `[ ]` `P-LEVEL-005`: Progress tăng đúng sau entry hợp lệ.
- `[ ]` `P-LEVEL-006`: Entry lỗi được bỏ qua và progress advance giống nguồn.
- `[ ]` `P-LEVEL-007`: Transform 0–7 cho cùng region map và solution.
- `[ ]` `P-LEVEL-008`: LK Modified/LK Style/GC đọc và lọc đúng.
- `[ ]` `P-LEVEL-009`: Recent-puzzle protection/fallback đúng.
- `[ ]` `P-LEVEL-010`: Prefill/pre-cat deterministic theo state/config.
- `[ ]` `P-LEVEL-011`: Color map default/seed/RGB/Lab/pattern khớp fixture.

## P-CORE — Luật gameplay

- `[x]` `P-CORE-001`: Cell enum values và `is_blank`/`is_cross` khớp nguồn. Bằng chứng: `CellStateTests`.
- `[x]` `P-CORE-002`: Hai cat cùng region trả `SameColor`. Bằng chứng: `QueendokuCoreTests.ClassifyViolation_UsesSourceRulePriority`.
- `[x]` `P-CORE-003`: Hai cat cùng hàng/cột trả `SameLine`. Bằng chứng: `QueendokuCoreTests.ClassifyViolation_UsesSourceRulePriority`.
- `[x]` `P-CORE-004`: Hai cat chạm chéo/kề trả `NoTouch`. Bằng chứng: `QueendokuCoreTests.ClassifyViolation_UsesSourceRulePriority`.
- `[x]` `P-CORE-005`: Khi có nhiều vi phạm, priority rule khớp nguồn. Bằng chứng: cùng region được ưu tiên trước same line trong `QueendokuCoreTests`.
- `[ ]` `P-CORE-006`: `FindConflicts` trả đúng toàn bộ cell xung đột.
- `[ ]` `P-CORE-007`: `CellsExcludedByCat` trả đúng tập cell.
- `[x]` `P-CORE-008`: Complete chỉ true khi đủ N cat và không xung đột. Bằng chứng: `QueendokuCoreTests.IsComplete_RequiresExactlyFourNonConflictingCats`.
- `[ ]` `P-CORE-009`: Invalid board/solution bị từ chối an toàn.
- `[x]` `P-CORE-010`: Score/combo/max combo/restore khớp model nguồn. Bằng chứng: `GameScoreModelTests`.

## P-INPUT — Tap, double tap và swipe

- `[~]` `P-INPUT-001`: Recognizer trả EMPTY → MARK tức thời; desktop mouse gesture chạy ngay từ raw event sau khi xác nhận top UI raycast, không chờ EventSystem dispatch. EditMode recognizer/latch đã pass; PlayMode retest vẫn bắt buộc trước khi đóng cổng.
- `[ ]` `P-INPUT-002`: Một tap MARK → EMPTY.
- `[x]` `P-INPUT-003`: Hai tap cùng cell trong cửa sổ tạo đúng một DoubleTap. Bằng chứng: `BoardGestureRecognizerTests.SecondTapOnSameCell_WithinWindowEmitsDoubleTap`.
- `[ ]` `P-INPUT-004`: Double tap solution cell kết thúc bằng CAT, không chớp X quan sát được.
- `[ ]` `P-INPUT-005`: Double tap non-solution tạo wrong-guess flow đúng.
- `[x]` `P-INPUT-006`: Tap cell khác trong cửa sổ không bị nhận nhầm là double tap và phản hồi độc lập. Bằng chứng: `BoardGestureRecognizerTests.NewCellTap_DoesNotWaitForPreviousDoubleTapWindow`.
- `[x]` `P-INPUT-007`: Swipe từ blank chọn MARK và paint toàn đường. Bằng chứng: `BoardGestureRecognizerTests.Swipe_ReturnsStartImmediatelyAndInterpolatesSkippedCells`.
- `[x]` `P-INPUT-008`: Swipe từ MARK chọn EMPTY và erase toàn đường. Bằng chứng: `BoardGestureRecognizerTests.FastEraseAcrossThreeCells_ChangesStartMiddleAndEnd`.
- `[ ]` `P-INPUT-009`: Swipe bắt đầu từ CAT xác định target theo cell đầu hợp lệ.
- `[ ]` `P-INPUT-010`: Nội suy qua cell bị bỏ qua không thay cell kề ngoài đường.
- `[ ]` `P-INPUT-011`: Không đổi CAT/ERROR/LOCKED cell trái phép.
- `[ ]` `P-INPUT-012`: Ra ngoài board rồi quay lại không phát action rác.
- `[ ]` `P-INPUT-013`: Pointer up ngoài board vẫn kết thúc stroke.
- `[ ]` `P-INPUT-014`: Mất focus hủy stroke/pending input an toàn.
- `[ ]` `P-INPUT-015`: Multi-touch không trộn hai pointer.
- `[ ]` `P-INPUT-016`: Undo đảo ngược đúng một gesture/step.
- `[ ]` `P-INPUT-017`: Hành vi ổn định ở 30/60/120 FPS mô phỏng.

## P-BOARD — Board, cell và layout

- `[ ]` `P-BOARD-001`: Board size 4–10 luôn hiển thị đúng N hàng × N cột.
- `[ ]` `P-BOARD-002`: Resize không đổi thứ tự hoặc hoán đổi hàng/cột.
- `[ ]` `P-BOARD-003`: Intrinsic size/padding theo size khớp source.
- `[ ]` `P-BOARD-004`: Region color/palette đúng default config.
- `[ ]` `P-BOARD-005`: Border và bốn góc cell đúng region neighborhood.
- `[ ]` `P-BOARD-006`: Pool respawn bật object và reset toàn bộ visual/state.
- `[ ]` `P-BOARD-007`: CAT/MARK/ERROR/DRAFT/LOCKED hiển thị đúng.
- `[ ]` `P-BOARD-008`: Safe area và aspect dài/ngắn không che board/tools/header.
- `[ ]` `P-BOARD-009`: Reference 1080×1920 đạt ngưỡng pixel sai lệch đã duyệt.

## P-GAME — Vòng đời Main Game

- `[ ]` `P-GAME-001`: Start level tạo đúng puzzle, prefill và initial state.
- `[ ]` `P-GAME-002`: Correct cat cập nhật board/remaining/score/combo.
- `[ ]` `P-GAME-003`: Wrong cat giảm lives, reset combo và chạy pending flow đúng.
- `[ ]` `P-GAME-004`: Rule violation chỉ highlight/feedback theo config.
- `[ ]` `P-GAME-005`: Hết lives mở Fail đúng một lần.
- `[ ]` `P-GAME-006`: Complete mở win toast/page đúng một lần.
- `[~]` `P-GAME-007`: Clear/Locate/Hint domain và Locate/Hint resource/free/cooldown đã có fixture; ToolButton/reward UI và transition persistence còn thiếu. Bằng chứng: `ToolResourceCoordinatorTests` và các case `GameSession_*` trong `BoardGestureRecognizerTests`.
- `[ ]` `P-GAME-008`: Hint R1/R2/R3/R4/chain cho output đúng fixture.
- `[ ]` `P-GAME-009`: Auto-complete không nhận input chen giữa.
- `[ ]` `P-GAME-010`: Restart tạo lại đúng puzzle/state theo nguồn.
- `[ ]` `P-GAME-011`: Exit rồi resume xử lý snapshot đúng.
- `[ ]` `P-GAME-012`: App focus out/in không tăng timer hoặc action sai.
- `[~]` `P-GAME-013`: Idle tool hint giữ đúng guard và nhịp 20 giây chờ → 10 giây chạy → 20 giây chờ; pulse UI chưa dựng. Bằng chứng: `ToolResourceCoordinatorTests.RepeatableIdleHint_UsesTwentyTenTwentyCadence`.

## P-TUTORIAL — Tutorial

- `[~]` `P-TUT-001`: Board/solution/region 4×4 đúng source. Fixture `GuidePuzzle_MatchesDecodedGodotBankEntryExactly` đã compile, chờ Unity Test Runner.
- `[~]` `P-TUT-002`: Step 1 chỉ cho đặt first cat ở allowed cell và cần double-tap. Fixture `PlaceCatSteps_RequireSameCellDoubleTapWithinSourceWindow` đã compile.
- `[~]` `P-TUT-003`: Step 2 confirm one-per-color đúng ở Current; Check/IQ bỏ confirm riêng. Fixture flow đã compile.
- `[~]` `P-TUT-004`: Step 3 chỉ nhận đúng sáu mark hàng/cột. Fixture `DefaultFlow_UsesAllSevenSourceInteractionsAndFinalConfirm` đã compile.
- `[~]` `P-TUT-005`: Step 4 place second cat đúng bằng double-tap. Cùng fixture flow đã compile.
- `[~]` `P-TUT-006`: Step 5 chỉ nhận đúng ba neighbor; diagonal variant chỉ đổi presentation contract. Fixture flow/config đã compile.
- `[~]` `P-TUT-007`: Step 6 place third cat đúng bằng double-tap. Cùng fixture flow đã compile.
- `[~]` `P-TUT-008`: Step 7 free play và ba pha reveal/apply hint hoàn tất đúng. Fixture `HintFlow_RevealsThenAppliesTwoRowsAndLastCatInSixPresses` đã compile.
- `[ ]` `P-TUT-009`: Hand/mask/message không cho input xuyên.
- `[~]` `P-TUT-010`: Completion committer lưu `tutorial_done` đúng một lần; presenter route Game level 1 và đóng Tutorial còn thiếu. Fixture `CompletionCommitter_SavesTutorialDoneExactlyOnce` đã compile.

## P-RESULT — Win, fail và progression

- `[ ]` `P-RESULT-001`: Win hiển thị level/score/time/combo/beat percent đúng.
- `[ ]` `P-RESULT-002`: Next tăng level/progress đúng một lần.
- `[ ]` `P-RESULT-003`: Fail hiển thị remaining cats đúng.
- `[ ]` `P-RESULT-004`: Restart không vô tình advance bank/level sai.
- `[ ]` `P-RESULT-005`: Revive khôi phục đúng số lives theo config.
- `[ ]` `P-RESULT-006`: Free/reward revive idempotent.
- `[ ]` `P-RESULT-007`: Popup queue sau win đúng priority.

## P-AUDIO — Audio, vibration và animation timing

- `[ ]` `P-AUDIO-001`: Mỗi SoundManager Kind ánh xạ đúng asset.
- `[ ]` `P-AUDIO-002`: Polyphony không cắt/mở quá số nguồn.
- `[ ]` `P-AUDIO-003`: Music/sound settings tắt đúng bus/player và được lưu.
- `[ ]` `P-AUDIO-004`: BGM pause/resume/duck đúng khi dialog, SFX và ad.
- `[ ]` `P-AUDIO-005`: Combo/meow voice-by-path đúng config.
- `[ ]` `P-AUDIO-006`: Vibration no-op an toàn trên platform không hỗ trợ.
- `[ ]` `P-AUDIO-007`: Animation completion không làm transition logic chạy hai lần.

## P-META — Daily, streak và award

- `[ ]` `P-META-001`: Daily puzzle/date selection đúng.
- `[ ]` `P-META-002`: Daily win/fail cập nhật stats một lần.
- `[ ]` `P-META-003`: Rollover ngày không phụ thuộc frame/page đang mở.
- `[ ]` `P-META-004`: Streak resume/backfill/protect đúng fixture.
- `[ ]` `P-META-005`: Pending/in-flight award khôi phục sau crash.
- `[ ]` `P-META-006`: Award không nhận lặp.

## Nhật ký chạy checklist

| Ngày | Build/commit | Phạm vi | Kết quả | Bằng chứng |
|---|---|---|---|---|
| 2026-08-08 | Workspace hiện tại | Kiểm kê ban đầu | Chưa chạy parity suite | Roadmap + SourceMap |
| 2026-08-08 | Unity 6000.3.19f1 | Pure EditMode suite | 67 passed, 0 failed | `Reports/Codex/R1_EditMode_Test_Report_2026-08-08.md` |
| 2026-08-09 | Unity 6000.3.19f1 | R8 tool resource + idle policy regression | 216 passed, 0 failed | `Reports/Codex/R8_Tool_Resource_Idle_Hint_Test_Report_2026-08-09.md` |
| 2026-08-10 | Unity 6000.3.19f1 | R11 tutorial domain/config fixtures | 14 case mới compile sạch; chưa chạy Test Runner | `Reports/Codex/R11_Tutorial_StateMachine_Report_2026-08-10.md` |
