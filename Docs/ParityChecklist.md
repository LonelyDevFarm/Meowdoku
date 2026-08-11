# Parity Checklist — Godot ↔ Unity

> Cập nhật: 2026-08-11  
> Trạng thái ban đầu chủ ý để trống; đánh dấu sau khi có bằng chứng test hoặc recording đối chiếu.

## Cách ghi bằng chứng

Mỗi case hoàn thành phải ghi một trong các bằng chứng cạnh checkbox hoặc trong test report:

- Tên automated test.
- Video/screenshot Godot và Unity cùng thao tác.
- Fixture đầu vào + output kỳ vọng lấy từ mã nguồn.

Không dùng cảm giác “có vẻ giống” để đóng case.

## P-BOOT — Startup và navigation

- `[~]` `P-BOOT-001`: Contract launcher 2,0+0,5 giây và Splash 3,0/0,1 giây đã port; prefab thật/AppScene đã sinh, chờ PlayMode timing. Bằng chứng code: `UIPopupStartupTests.SplashTiming_MatchesLauncher`.
- `[~]` `P-BOOT-002`: Lần đầu chưa hoàn thành tutorial route đến Tutorial đã nối page/registry thật; chờ PlayMode. Bằng chứng code: `UIPopupStartupTests.InitialRoute_UsesPersistedTutorialDone`.
- `[x]` `P-BOOT-003`: Với `tutorial_done=true`, AppScene PlayMode hoàn tất bootstrap và route đến Home thật. Bằng chứng: `PrimaryNavigationPlayModeTests.AppScene_PrimaryRoutes_OpenCloseAndReuseAtRuntime`.
- `[ ]` `P-BOOT-004`: Config/state lỗi hoặc thiếu vẫn dùng default và không kẹt splash.
- `[ ]` `P-BOOT-005`: Back trên Home mở confirm/thoát giống nguồn.
- `[~]` `P-BOOT-006`: `AppScene/UI/SharedOverlays/ModalMask` và `InputGuard` đã serialized vào UIManager; fixture framework đã có, chờ PlayMode chạm xuyên popup.
- `[~]` `P-BOOT-007`: Cache/one-flight/loading guard đã port; PlayMode xác nhận Hide → Closing → Show tái sử dụng đúng instance và không kẹt Loading. Còn stress nhiều request/input release.
- `[x]` `P-BOOT-009`: Registry của 8 page mục tiêu trỏ đúng prefab/presenter, không missing script, binding bắt buộc được serialize và từng page đã show/hide trong AppScene PlayMode; Game dựng level 1. Bằng chứng: composition fixtures + `PrimaryNavigationPlayModeTests`; EditMode 508/508, PlayMode 1/1.
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
- `[~]` `P-TUT-009`: Presenter đã khóa board khi chuyển bước, mask/message không bắt raycast và clone cell tắt toàn bộ graphic raycast; prefab thật đã sinh, chờ PlayMode xác nhận không lọt input.
- `[~]` `P-TUT-010`: Completion committer lưu `tutorial_done` đúng một lần; presenter đã gọi Game level 1 rồi đóng Tutorial. Registry/startup/Game page thật đã có, còn PlayMode route. Fixture `CompletionCommitter_SavesTutorialDoneExactlyOnce` đã compile.

## P-HOME — Home và điều hướng ngoại vi

- `[~]` `P-HOME-001`: Offline defaults hiển thị Daily Streak, ẩn Profile và dùng hard-button variant 0 đúng source. Fixture `HomePageContractTests.DefaultPresentation_MatchesOfflineSourceConfiguration` và `AbConfigTests.HomeConfigs_UseOfflineSourceDefaults` đã compile, chờ Unity Test Runner.
- `[~]` `P-HOME-002`: Presenter đọc current level/hard state mỗi lần `OnShow`, level text dùng catalog động và prefab đã có serialized binding; chờ PlayMode locale/level refresh xác nhận.
- `[x]` `P-HOME-003`: `StartBtn` thật đã mở Game ở marker, dựng puzzle 4×4 và ẩn Home cuối animation trong AppScene PlayMode; Game `BackBtn` quit rồi trở về Home.
- `[~]` `P-HOME-004`: Home/Settings presenter, prefab, registry và AppScene composition đã có; `SettingsBtn`/`CloseBtn` thật mở/đóng đúng trong PlayMode. Còn kiểm tra toàn bộ toggle và layout state theo config.
- `[~]` `P-HOME-005`: Bốn slot nguồn giữ đúng cây; Daily/Streak/Rank entry và Profile route đều có presenter/runtime thật. Unity Refresh đã serialize Rank entry và composition fixture đạt; còn PlayMode xác nhận.
- `[~]` `P-HOME-006`: `OnHide` đã kill transition, abort popup queue và reset exit/page state; fixture xác nhận queue không mắc `IsRunning` khi Unity dừng coroutine, còn chờ vòng reopen PlayMode.
- `[ ]` `P-HOME-007`: Layout/animation Home khớp reference ở 1080×1920 và 1080×2400.
- `[~]` `P-HOME-008`: Home đọc priority JSON nguồn, filter scene, stable-sort giảm dần và đã nối `ab_switch_popup`, Rank reward/open popup theo đúng wait/confirm/profile-guide flow. Rewarded-ad handler còn chờ R16; Rank cần Unity Refresh/PlayMode.

## P-SETTINGS — Settings, Language và How-to-play

- `[~]` `P-SET-001`: Music/Sound/Vibration/People defaults, persistence và presenter binding đã có; thay đổi được lưu ngay, chờ Unity Test Runner/PlayMode.
- `[~]` `P-SET-002`: Offline outgame layout đã resolve Music ẩn, ba toggle còn lại hiện, Language/Pattern/Restart/HTP ẩn, Feedback/Terms/Version hiện; presenter/prefab áp dụng contract, chờ PlayMode.
- `[~]` `P-SET-003`: Game-mode layout đã resolve Restart hiện, Terms/Version ẩn và Pattern/HTP theo config; presenter/prefab áp dụng contract, chờ PlayMode.
- `[~]` `P-SET-004`: Pattern mode và hai dismissed-dot field lưu đúng key nguồn; service setter/dismiss idempotent và presenter dot state đã có fixture/binding.
- `[~]` `P-SET-005`: Toggle cập nhật sprite/panel/toast ngay, Sound bật phát preview và Vibration bật gọi platform boundary; chờ PlayMode nghe/cảm nhận trên thiết bị.
- `[~]` `P-SET-006`: `GenericPopupAnimator` giữ source marker/timing, presenter có skip-close khi mở HTP và callback one-shot; HTP page thật cùng PlayMode route còn chờ.
- `[~]` `P-SET-007`: CSV/catalog, locale persistence, Language popup/dropdown và refresh text Home/Settings đã port; chờ Unity Test Runner, PlayMode restart và device-font.
- `[~]` `P-SET-008`: Settings prefab đã sinh theo nhánh chức năng, không missing script và có serialized localization/language bindings; pixel parity outgame/game-mode còn chờ.
- `[~]` `P-SET-009`: Full How-to-play giữ ba board 3×5, matrix/state/frame schedule, tap-anywhere close và demo loop đúng source; prefab thật đã được Unity sinh/đăng ký và AppScene có route thật, chờ PlayMode.
- `[~]` `P-SET-010`: Paged How-to-play giữ ba page, board scale, Previous/Next/Got it, slide 16 frame, caption/localization highlight và demo loop đúng source; prefab thật đã được Unity sinh/đăng ký, chờ PlayMode.
- `[~]` `P-SET-011`: Hai page bật sound silence khi show và cleanup coroutine/tween/cell/silence khi hide/destroy/reopen; code path đã có, chờ PlayMode vòng lặp xác nhận.

## P-BANK — Bank browser

- `[~]` `P-BANK-001`: `LevelEntry` giữ đủ union schema thật của 25 bank asset (`id/date/label/r1…r5/transform/seq` cùng board/pattern/color fields); fixture parse/clone compile sạch, chờ Test Runner.
- `[~]` `P-BANK-002`: Root browser có đúng sáu nhánh Regular/LK/LK Modified/LK Style/GC/SP và chỉ hiện pool có dữ liệu; presenter/installer compile sạch, chờ prefab/PlayMode.
- `[~]` `P-BANK-003`: Size/rank và hard-tier keys `7:4, 8:4/5, 9:4/5, 10:4/5, 11:4/5, 12:4` được tách N/H đúng source; fixture compile sạch.
- `[~]` `P-BANK-004`: Launch Regular/LK Style/GC giữ seed + r1…r5 + tier flags; LK/LK Modified giữ id/maxR; SP giữ id/r1…r5/colorMap. Exact-key fixtures compile sạch, chờ Game page consumer.
- `[~]` `P-BANK-005`: Initial route priority `go_lk_style → go_lk → go_regular`, panel back stack, selector clamp và LK/SP row launch đã port. AppScene PlayMode đã bấm SP root card và row động đầu tiên để launch Game thật; còn LK/size/tier interaction matrix.
- `[~]` `P-BANK-006`: Dynamic size/tier/LK/SP rows được tái sử dụng theo pool và bind release-frame guard khi materialize; cần profiler/PlayMode xác nhận vòng reopen không tăng object vô hạn.
- `[x]` `P-BANK-007`: `BankPage.prefab` đã được Unity sinh và registry có `UiName.Bank`; structure fixture cùng AppScene PlayMode xác nhận presenter/binding hợp lệ, SP panel/row động materialize và không missing script.
- `[~]` `P-BANK-008`: AppScene PlayMode xác nhận `GamePage` consume launch SP qua Button thật thành `GameplaySessionMode.Bank`, hiển thị `ReturnBankBtn`, đóng Game và reset Bank về root đúng nguồn. Prev/Next/result còn thuộc R13.

## P-RESULT — Win, fail và progression

- `[~]` `P-RESULT-001`: Win default + pass-text V0–V3 và pass-page G1/G2/G4 đã port; stats/timing/beat-percent compile sạch, `Root/PassPanel` đã được Unity sinh với reference hợp lệ. PlayMode/pixel parity còn chờ.
- `[~]` `P-RESULT-002`: Coordinator settle/Next main và Bank next-launch có guard một lần cùng fixtures. AppScene PlayMode đã xác nhận Win → pre-result Streak/Award → Next tải level 2 và progress tăng đúng một; còn Bank Next PlayMode.
- `[~]` `P-RESULT-003`: Fail presenter dùng remaining cats từ terminal transition, đúng title/encourage/promote source; chờ PlayMode và visual parity.
- `[~]` `P-RESULT-004`: Fail restart không settle/advance lần hai và giữ `restart_count`; AppScene PlayMode đã xác nhận 3 wrong → Fail → Restart vẫn level 1/3 mạng rồi Win → level 2. Còn provider-revive nhiều vòng/app-kill PlayMode.
- `[~]` `P-RESULT-005`: Revive khôi phục 1/3 lives theo `revive_life`, resume clock và đóng Fail; chờ PlayMode.
- `[~]` `P-RESULT-006`: Free-once persisted/idempotent; reward revive đi qua boundary. AppScene PlayMode xác nhận default `reward_unlock_level=0`, `revive_free_logic=0` cùng null provider làm Revive ẩn đúng nguồn; provider-reward PlayMode còn chờ.
- `[~]` `P-RESULT-007`: `win_toast` threshold/message/highlight/presenter và nhánh chờ 1,5/1,2 giây đã port; default 0 tắt đúng nguồn. `Overlays/WinToast` đã được Unity sinh với đủ sprite/reference; PlayMode config override và rank/streak/rate/push sequence còn chờ.

## P-AUDIO — Audio, vibration và animation timing

- `[ ]` `P-AUDIO-001`: Mỗi SoundManager Kind ánh xạ đúng asset.
- `[ ]` `P-AUDIO-002`: Polyphony không cắt/mở quá số nguồn.
- `[ ]` `P-AUDIO-003`: Music/sound settings tắt đúng bus/player và được lưu.
- `[ ]` `P-AUDIO-004`: BGM pause/resume/duck đúng khi dialog, SFX và ad.
- `[ ]` `P-AUDIO-005`: Combo/meow voice-by-path đúng config.
- `[ ]` `P-AUDIO-006`: Vibration no-op an toàn trên platform không hỗ trợ.
- `[ ]` `P-AUDIO-007`: Animation completion không làm transition logic chạy hai lần.

## P-META — Daily, streak và award

- `[~]` `P-META-001`: Daily unlock/date/countdown, persisted completion state, beat-percent, deterministic bank/8-transform selection, launch contract và Home entry presenter đã port; fixture/bốn assembly compile sạch, chờ Unity Refresh/Test Runner/PlayMode.
- `[~]` `P-META-002`: Daily fail/revive/restart/win có coordinator riêng; settlement một lần, revive stats thuộc `daily`, win lưu date/elapsed/beat một lần và không chạm Main. Còn tracker tổng, visual Daily result và PlayMode nhiều vòng.
- `[~]` `P-META-003`: `ClockTicker` scene-owned phát tick giây và day-watch độc lập page, có reschedule pause/focus và local date key; fixture alignment compile sạch, chờ PlayMode đổi ngày/timezone.
- `[~]` `P-META-004`: Streak check-in, chu kỳ 7, resume/backfill/protect, week slots, pending-win crash recovery và flow Main/Lit/Settle đã port; reflection fixture đạt, chờ Unity prefab/Test Runner/PlayMode và rewarded-ad adapter.
- `[~]` `P-META-005`: Award transaction được ghi vào `in_flight_awards` trước presentation, cold-start sweep cấp lại đúng một lần và trang Collect hoàn tất transaction. Rank Gift có hai pha podium/rương → item, frame-only tự hoàn tất sau frame presentation; reflection fixture đạt, chờ PlayMode app-kill.
- `[~]` `P-META-006`: `CompleteAward`/cold sweep idempotent, double chỉ nhân tool và không nhân frame; reflection fixture đạt, chờ PlayMode thao tác nhanh/đóng popup.

## P-PROFILE — Identity, avatar và frame

- `[~]` `P-PROFILE-001`: Profile rỗng tự tạo nickname 6 ký tự, avatar/frame hợp lệ và sở hữu đủ 8 classic frame; initialization chỉ save một lần. Reflection fixture đạt, chờ Unity Test Runner/restart.
- `[~]` `P-PROFILE-002`: Nickname trim/giới hạn 12 code point, avatar/frame validation, unlock/equip/count và frame red-dot giữ đúng source. Profile presenter dùng pending selection, Avatar/Frame tabs, clear red-dot khi vào Frame, locked-frame shake/tooltip và chỉ commit khi Confirm; chờ PlayMode.
- `[~]` `P-PROFILE-003`: Remote export mã hóa nickname `b64:`, chỉ đồng bộ frame id ≥100 và chỉ merge khi remote ahead; fixture round-trip đạt, backend sync chờ R16.
- `[~]` `P-PROFILE-004`: Award frame đi qua `ProfileRuntime` scene-owned vào ProfileService và persistence thật; fixture tích hợp đạt, chờ Unity Refresh/AppScene và PlayMode cold-start.
- `[~]` `P-PROFILE-005`: Installer dựng cây prefab lồng `ProfileAvatarView → ProfileSelectionCell → ProfilePage` từ sprite gốc, layout 900×1253, grid 4 cột 185 px/gap 6, localization và route `UiName.Profile`; chờ Unity Refresh sinh asset và kiểm tra hai aspect.

## P-ROBOT — Robot leaderboard simulation

- `[~]` `P-ROBOT-001`: `RobotConfig`, pool/data/timeline và repository giữ đúng key/source defaults trong file riêng `robots.cfg`; model round-trip fixture đạt.
- `[~]` `P-ROBOT-002`: Player-base theo bốn rank band, bot random-power, closest-approach/fill-to-zero, weighted timeline, first-hour và cooldown/backward clamp đã port; fixture bao phủ floor/ceiling, overshoot và cooldown.
- `[~]` `P-ROBOT-003`: Ranking cộng timeline theo phút, hòa điểm ưu tiên timestamp sớm và ánh xạ robot sang `PlayerInfo`/award theo rank đúng source; fixture đạt.
- `[~]` `P-ROBOT-004`: Khi player vượt `x_base`, future timeline bị freeze và top pool catch-up theo capacity/gap/delta-time; overtake delay giữ đúng đơn vị giây thực tế của source. Fixture freeze/catch-up đạt.
- `[~]` `P-ROBOT-005`: Effective now clamp `end_unix`, không lùi dưới `last_seen_unix`; create/discard/reset persistence và `RobotRuntime` scene-owned đã có. Chờ Unity Refresh/Test Runner và PlayMode restart/time rollback.
- `[~]` `P-ROBOT-006`: Catalog đúng 1.699 nickname gốc, first/last và SHA-256 `f864d2094a2587d4c030371373120fd44c693061edde7af3c54292d3dac7e4fd` có regression; không dùng tên tự tạo.

## P-RANK — Rank Activity

- `[~]` `P-RANK-001`: Ba group, unlock level 11, period 86.400 giây, reopen sau 10 win và first-session/home rules giữ đúng source; fixture period/open đạt.
- `[~]` `P-RANK-002`: Level collect chỉ commit khi thắng; restart/exit xóa cache, win ghi score/timestamp, rank dùng RobotService tie-break và expiry trong level defer settlement. Fixture end-to-end đạt, chờ PlayMode nhiều vòng.
- `[~]` `P-RANK-003`: Home entry/open popup/confirm/profile-guide và popup priority đã nối; close popup vẫn xác nhận participation như nguồn. Chờ Unity Refresh và PlayMode first-period/reopen.
- `[~]` `P-RANK-004`: Leaderboard page có header/countdown/top-3/list/self/profile/info/CTA; row dùng avatar/frame/medal/badge/chest mapping hạng 1→tier3, 2→tier2, 3→tier1. Chờ PlayMode scroll và hai aspect.
- `[~]` `P-RANK-005`: Change page giữ appear/count/score-roll/rank-settle timing, khóa dismiss/scroll khi animation, encouragement source và strip BBCode Godot. Chờ PlayMode xác nhận rise/scroll visual.
- `[~]` `P-RANK-006`: HTP đổi Cat/Fish và full/frame-only reward theo group, tap/escape đóng đúng route; chờ prefab generation/visual parity.
- `[~]` `P-RANK-007`: Settlement reward table đúng nguồn, durable RankGift transaction, top-3 params/win-count, hai pha podium/rương → item, frame-only và period-next flow đã nối. Chest Spine, celebration particle và frame-fly VFX còn chờ adapter parity.

## P-TRACK — Tracker và Session

- `[~]` `P-TRACK-001`: Event/screen/dialog/game/ad/prop schema, source stack, game ID, round stats và transform→question rotation giữ đúng key/giá trị nguồn; `TrackingCoreTests` đạt qua runner thuần.
- `[~]` `P-TRACK-002`: Session cộng active time theo monotonic clock, flush mỗi 60 giây và chỉ tạo session mới khi background lớn hơn 1.800 giây; fixture boundary đạt, còn PlayMode focus/pause và app-kill thiết bị thật.
- `[~]` `P-TRACK-003`: `UITrackerObserver` tập trung nghe `WindowShown`; dialog được pop khỏi source stack tại đúng ranh giới close lifecycle. Splash/Home/Game/Daily/Win/Fail/Settings/Language/Profile/Streak/Rank metadata đã nối; các page/call site online còn lại chưa hoàn tất.
- `[~]` `P-TRACK-004`: Runtime mặc định dùng no-op sink, không log debug và không chặn startup khi thiếu SDK. Online provider, consent gate và network-failure parity thuộc phần còn lại của R16.
- `[~]` `P-TRACK-005`: Language confirm/cancel/property, Settings button/switch/dropdown lồng và Rank Award `challenge_reward → challenge_reward_get` đã nối đúng source. Còn PlayMode sink-capture để xác nhận thứ tự window animation thực tế.
- `[~]` `P-TRACK-006`: Main/Daily phát `game_start` sau khi session/board sẵn sàng; New/Continue/Restart, game ID, qid hard-tier, qrotate và challenge flag có fixture. Fail/Win/Restart phát full `game_end`; restart giữ đúng thứ tự nguồn và transition chụp board counters trước reload. Hint/Locate/Clear/apply/stop/detail, gesture step, erase, hint-cross, invalid-sign, game-die và button call site hiện có của Home/Profile/Streak/Rank/Result đã nối đúng nguồn; tiêu tool/Award tool phát `prop_use/prop_get` đúng sau mutation, fixture khóa `step_used` từ round stat. Coordinate UI và device attribution còn thiếu.
- `[~]` `P-TRACK-007`: `AdService` giữ show-id/position từ readiness tới impression, chỉ impression phát `interstitial_ad_show/rewarded_ad_show`; shown/rewarded/closed độc lập và request rewarded hoàn tất đúng một lần. Null provider không track/cấp thưởng, dispose tháo callback. Hint/Locate, Fail revive, Streak revive, audio/Daily clock và full default interstitial gate order đã nối; board entry chỉ chạy sau close/error/focus. Banner gate/lifecycle và reward-restore watchdog 30 giây + late callback + durable Home popup đã port. Provider-neutral A/B timing, persisted first-open và LivingDays local-calendar segment đã nối vào ba policy Ads; regression 90/90. Còn production ad/A-B SDK adapter và PlayMode/device callback parity.

## P-PERF — Mobile performance và lifecycle

- `[~]` `P-PERF-001`: Raw mouse/touch coordinate path không còn gọi `GetComponentInParent<Canvas>` hoặc `GetComponent<GridLayoutGroup>` ở mỗi pointer move; cache có invalidation theo parent/container và không đổi gesture contract nguồn.
- `[~]` `P-PERF-002`: Cell/board layout tái sử dụng buffer bốn world-corner; rounded frame mesh tái sử dụng path buffer trong intro thay vì cấp phát mỗi rebuild. Intro mesh vẫn chỉ dirty trong đúng thời lượng animation.
- `[~]` `P-PERF-003`: Board local pool giữ ownership/reset hiện tại; singleton `PoolManager` legacy không có code consumer nhưng còn serialized trong `LoadingScene`, chờ khóa build/test scene mới được dọn.
- `[~]` `P-PERF-004`: Core/Gameplay/Editor/EditModeTests compile sạch, regression runner **90 passed, 0 failed**, Unity refresh/build sạch. Còn Unity Profiler/device GC, soak restart/pool và touch thật.

## Nhật ký chạy checklist

| Ngày | Build/commit | Phạm vi | Kết quả | Bằng chứng |
|---|---|---|---|---|
| 2026-08-08 | Workspace hiện tại | Kiểm kê ban đầu | Chưa chạy parity suite | Roadmap + SourceMap |
| 2026-08-08 | Unity 6000.3.19f1 | Pure EditMode suite | 67 passed, 0 failed | `Reports/Codex/R1_EditMode_Test_Report_2026-08-08.md` |
| 2026-08-09 | Unity 6000.3.19f1 | R8 tool resource + idle policy regression | 216 passed, 0 failed | `Reports/Codex/R8_Tool_Resource_Idle_Hint_Test_Report_2026-08-09.md` |
| 2026-08-10 | Unity 6000.3.19f1 | R11 tutorial domain/config fixtures | 14 case mới compile sạch; chưa chạy Test Runner | `Reports/Codex/R11_Tutorial_StateMachine_Report_2026-08-10.md` |
| 2026-08-10 | Unity 6000.3.19f1 | R11 tutorial presenter/prefab installer | Core/Gameplay/Editor + fixture compile sạch bằng Unity Roslyn; Refresh/PlayMode còn chờ | `Reports/Codex/R11_Tutorial_Presenter_Report_2026-08-10.md` |
| 2026-08-10 | Unity 6000.3.19f1 | R12 Home config/presentation contract | Core + fixture compile sạch bằng Unity Roslyn; Test Runner/PlayMode còn chờ | `Reports/Codex/R12_Home_Core_Contract_Report_2026-08-10.md` |
| 2026-08-10 | Unity 6000.3.19f1 | R12 Home presenter/prefab installer | Core/Gameplay/Editor/EditMode compile sạch; Unity Refresh sinh prefab/material và PlayMode còn chờ | `Reports/Codex/R12_Home_Presenter_Report_2026-08-10.md` |
| 2026-08-10 | Unity 6000.3.19f1 | R12 Settings core state/config | Core + fixture compile sạch; Unity Test Runner và presenter còn chờ | `Reports/Codex/R12_Settings_Core_State_Report_2026-08-10.md` |
| 2026-08-10 | Unity 6000.3.19f1 | R12 Settings, localization và Language UI | Core/Gameplay/Editor/EditMode compile sạch; parser CSV thật và prefab/registry structure đã kiểm chứng; Test Runner/PlayMode còn chờ | `Reports/Codex/R12_Settings_Localization_Language_Report_2026-08-10.md` |
| 2026-08-10 | Unity 6000.3.19f1 | R12 hai How-to-play page | Matrix/frame/layout contract và bốn assembly compile sạch; prefab installer/Test fixture đã có, Auto Refresh/Test Runner/PlayMode còn chờ | `Reports/Codex/R12_How_To_Play_Report_2026-08-10.md` |
| 2026-08-10 | Unity 6000.3.19f1 | R12 Bank model/browser/launch contract | Core/Gameplay/Editor/EditMode compile sạch bằng Unity Roslyn; Auto Refresh prefab/registry, Test Runner và Game route còn chờ | `Reports/Codex/R12_Bank_Browser_Report_2026-08-10.md` |
| 2026-08-10 | Unity 6000.3.19f1 | R10–R12 App runtime composition | Unity đã sinh Splash/Game prefab, AppScene và registry 9 page; board prewarm 4 cell/frame cùng bốn assembly compile sạch, Build Settings/Test Runner/PlayMode chờ Editor về Edit Mode | `Reports/Codex/R12_App_Runtime_Composition_Report_2026-08-10.md` |
| 2026-08-10 | Unity 6000.3.19f1 | R13 Win/Fail/Revive runtime slice | Unity đã sinh Win/Fail/Game result branches, không missing script/import error; regression transition mới và bốn assembly compile sạch, Test Runner/PlayMode còn chờ | `Reports/Codex/R13_Win_Fail_Revive_Report_2026-08-10.md` |
| 2026-08-11 | Unity 6000.3.19f1 | R15 Robot core/cache/runtime | Bốn assembly compile sạch; reflection regression 39/39, gồm checksum 1.699 nickname và 7 case thuật toán/service Robot; Unity Refresh/Test Runner/PlayMode còn chờ | `Reports/Codex/R15_Robot_Core_Report_2026-08-11.md` |
| 2026-08-11 | Unity 6000.3.19f1 | R15 Rank Activity core/UI/reward flow | Bốn assembly compile sạch; reflection regression 48/48. Prefab Change/Rank Gift và registry chờ Unity Refresh, toàn flow chờ PlayMode | `Reports/Codex/R15_Rank_Activity_Core_UI_Report_2026-08-11.md` |
| 2026-08-11 | Unity 6000.3.19f1 | R17 gameplay hot-path/lifecycle audit | Cache Canvas/Grid, tái sử dụng geometry buffer; bốn assembly compile sạch, regression 90/90 và Unity Tundra build sạch | `Reports/Codex/R17_Mobile_Lifecycle_Audit_Report_2026-08-11.md` |
| 2026-08-11 | Unity 6000.3.19f1 | R10–R12 UI composition/navigation verification | Registry/presenter/binding của 8 page đạt; lifecycle test đạt; Unity Test Runner thật 507 passed, 0 failed | `Reports/Codex/R10_R12_UI_Navigation_Verification_Report_2026-08-11.md` |
| 2026-08-11 | Unity 6000.3.19f1 | R10–R12 AppScene PlayMode navigation | Startup Home, 8 page show/hide, Back, abort-close/reopen, Bank resource thật, seed Godot Int64 và Button Home→Settings/Game→Home đạt; EditMode 508/508, PlayMode 2/2 | `Reports/Codex/R10_R12_UI_Navigation_Verification_Report_2026-08-11.md` |
| 2026-08-11 | Unity 6000.3.19f1 | R13 Main result + Bank navigation PlayMode | Fail 3 mạng, null-ad Revive gate, Restart, Win, Streak/Award, Next level 2 và SP card→row→Game bank-mode→Return Bank đạt; EditMode 508/508, PlayMode 4/4 | `Reports/Codex/R13_PlayMode_Main_Result_Loop_Report_2026-08-11.md` |
