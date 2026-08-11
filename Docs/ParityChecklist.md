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
- `[~]` `P-BOOT-006`: `AppScene/UI/SharedOverlays/ModalMask` và release `InputGuard` đã serialized; local `block_input_briefly` dùng transparent raycast canvas z=4095 đúng nguồn. AppScene PlayMode gửi pointer down/up thật, xác nhận guard release bật rồi tự dọn cuối frame, local blocker refresh deadline/chỉ còn một instance/tự dọn; Fail xác nhận Game 2,0 giây + Fail 1,5 giây cùng hoạt động. Còn touch/raycast device thật.
- `[x]` `P-BOOT-007`: Cache/one-flight/loading guard đạt AppScene PlayMode: 96 vòng Hide → Closing → Show giữ cùng instance, không duplicate shown/hidden, không rò mask, Z compact trong range; hai `ShowAsync(Language)` đồng thời chỉ create/show một page và kết thúc `IsAnyLoading=false`.
- `[x]` `P-BOOT-009`: Registry của 8 page mục tiêu trỏ đúng prefab/presenter, không missing script, binding bắt buộc được serialize và từng page đã show/hide trong AppScene PlayMode; Game dựng level 1. Bằng chứng: composition fixtures + `PrimaryNavigationPlayModeTests`; EditMode 508/508, PlayMode 1/1.
- `[ ]` `P-BOOT-008`: Mất focus/resume không tạo session/game page trùng.

## P-SAVE — State và persistence

- `[x]` `P-SAVE-001`: Save → restart khôi phục current level/strategy. Bằng chứng: `GameStateRepositoryTests.PlayerState_RoundTripsP0Fields`.
- `[x]` `P-SAVE-002`: Khôi phục tool counts và settings. Bằng chứng: `GameStateRepositoryTests.PlayerState_RoundTripsP0Fields`.
- `[ ]` `P-SAVE-003`: Atomic write không thay slot tốt khi verify thất bại.
- `[x]` `P-SAVE-004`: Slot chính hỏng thì đọc slot dự phòng. Bằng chứng: `SaveStoreTests.DualSlot_CorruptPrimaryFallsBackToPreviousSlot`.
- `[ ]` `P-SAVE-005`: Cả hai slot lỗi thì dùng state mặc định an toàn.
- `[ ]` `P-SAVE-006`: Legacy save migration chỉ chạy đúng một lần.
- `[~]` `P-SAVE-007`: PlayMode lifecycle boundary khôi phục schema snapshot với `in_game_sec` và chụp MARK đang debounce; hard-kill/resume trên thiết bị thật còn chờ R17.
- `[x]` `P-SAVE-008`: Playing/Fail/Revive/Win/Next giữ hoặc xóa snapshot đúng thời điểm trong AppScene matrix; Win suspend không tái tạo snapshot hoàn tất và Next ghi level mới.
- `[~]` `P-SAVE-009`: Endgame runtime không mã hóa/verify/fsync trên main thread; background coalesce, immutable snapshot, focus-out/pause/quit force-rebuild và callback dedup đã có EditMode/PlayMode test. Còn app-kill trên thiết bị thật.

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
- `[~]` `P-GAME-011`: Snapshot Playing/Fail/Revive/Win/Next và elapsed restore đã đạt AppScene PlayMode; exit/hard-kill rồi cold resume trên thiết bị thật còn chờ.
- `[~]` `P-GAME-012`: Focus-out/pause dùng chung một durability boundary, không nhân action/save và snapshot lấy elapsed mới nhất; timer background thật còn chờ device matrix.
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

- `[x]` `P-HOME-001`: Offline defaults hiển thị Daily Streak, ẩn Profile và dùng hard-button variant 0 đúng source. `HomeConfigSet` thuộc shared `AbConfigRuntime`; fixture default/provider reload đã qua Unity EditMode thật.
- `[~]` `P-HOME-002`: Presenter đọc current level/hard state mỗi lần `OnShow`, level text dùng catalog động và prefab đã có serialized binding; chờ PlayMode locale/level refresh xác nhận.
- `[x]` `P-HOME-003`: `StartBtn` thật đã mở Game ở marker, dựng puzzle 4×4 và ẩn Home cuối animation trong AppScene PlayMode; Game `BackBtn` quit rồi trở về Home.
- `[~]` `P-HOME-004`: Home/Settings presenter, prefab, registry và AppScene composition đã có; `SettingsBtn`/`CloseBtn` thật mở/đóng đúng trong PlayMode. Còn kiểm tra toàn bộ toggle và layout state theo config.
- `[x]` `P-HOME-005`: Bốn slot nguồn giữ đúng cây và cùng feature availability runtime. AppScene PlayMode xác nhận Profile theo `leaderboard_func`, Daily khóa/mở ở level 21, Streak route thật, Rank ẩn dưới level 11 rồi mở popup/tham gia; không tạo nút/page giả cho entry bị khóa.
- `[~]` `P-HOME-006`: `OnHide` đã kill transition, abort popup queue và reset exit/page state; fixture xác nhận queue không mắc `IsRunning` khi Unity dừng coroutine, còn chờ vòng reopen PlayMode.
- `[ ]` `P-HOME-007`: Layout/animation Home khớp reference ở 1080×1920 và 1080×2400.
- `[~]` `P-HOME-008`: Home đọc priority JSON nguồn, filter scene, stable-sort giảm dần và đã nối `ab_switch_popup`, Rank reward/open popup theo đúng wait/confirm/profile-guide flow. First-period open/confirm/Profile-or-Game đã đạt AppScene PlayMode; reward/reopen và rewarded-ad restore cần matrix riêng.

## P-SETTINGS — Settings, Language và How-to-play

- `[~]` `P-SET-001`: Music/Sound/Vibration/People defaults, persistence và presenter binding đã có; AppScene PlayMode đã bấm Sound/Vibration/People và xác nhận state đổi/lưu ngay. Còn Music game-mode bị ẩn đúng nguồn và cảm nhận audio/vibration trên thiết bị.
- `[~]` `P-SET-002`: Offline outgame layout đã resolve Music ẩn, ba toggle còn lại hiện, Language/Pattern/Restart/HTP ẩn, Feedback/Terms/Version hiện. AppScene PlayMode xác nhận popup route và dropdown non-English; `ISettingsExternalServices` gate offline/online Feedback, CMP visibility/action và localized Terms/Privacy URL đúng boundary. Còn full layout/pixel parity và production SDK adapter.
- `[~]` `P-SET-003`: Game-mode layout đã resolve Restart hiện, Terms/Version/Language ẩn và Pattern/HTP theo config. AppScene PlayMode với `rule_text=setting_entry`, `blind_mod=1` xác nhận Language ẩn, Pattern/HTP hiện; double invoke Restart chỉ tăng `RestartCount` một lần, đóng Settings và tải lại cùng level. Còn device/pixel parity.
- `[~]` `P-SET-004`: Pattern mode dùng đúng 12 sprite/màu nguồn theo color index; callback Settings áp dụng ngay lên board, hide-on-filled đạt PlayMode và hai dismissed field lưu đúng thời điểm. Còn visual red-dot ở Settings entry và `blind_mod=2` device/pixel check.
- `[~]` `P-SET-005`: Toggle cập nhật sprite/panel/toast ngay, Sound bật phát preview và Vibration bật gọi platform boundary; chờ PlayMode nghe/cảm nhận trên thiết bị.
- `[x]` `P-SET-006`: `GenericPopupAnimator` giữ source marker/timing; AppScene PlayMode xác nhận bấm HTP mở Paged page, Settings skip close/ẩn, signal `Closed` phát trước close animation đúng Godot rồi trả lifecycle về Game mà không kẹt Loading.
- `[~]` `P-SET-007`: CSV/catalog, locale persistence, Language popup/dropdown và refresh text Home/Settings đã port. AppScene PlayMode dùng system locale `vi_VN` xác nhận dropdown hiện, outside pointer-down đóng mà không đóng Settings, System option apply/persist rồi đóng; blocker prefab là Graphic raycast không có Button-release. Còn cold restart theo locale và device-font.
- `[~]` `P-SET-008`: Settings prefab đã sinh theo nhánh chức năng, không missing script và có serialized localization/language bindings; pixel parity outgame/game-mode còn chờ.
- `[~]` `P-SET-009`: Full How-to-play giữ ba board 3×5, matrix/state/frame schedule, tap-anywhere close và demo loop đúng source; prefab thật đã được Unity sinh/đăng ký và AppScene có route thật, chờ PlayMode.
- `[~]` `P-SET-010`: Paged How-to-play giữ ba page, board scale, Previous/Next/Got it, slide 16 frame, caption/localization highlight và demo loop đúng source; Settings route cùng Previous/Next/Got it đã được bấm trong AppScene PlayMode. Còn VFX/pixel parity và soak reopen dài.
- `[~]` `P-SET-011`: Hai page bật sound silence khi show và cleanup coroutine/tween/cell/silence khi hide/destroy/reopen; code path đã có, chờ PlayMode vòng lặp xác nhận.

## P-BANK — Bank browser

- `[~]` `P-BANK-001`: `LevelEntry` giữ đủ union schema thật của 25 bank asset (`id/date/label/r1…r5/transform/seq` cùng board/pattern/color fields); fixture parse/clone compile sạch, chờ Test Runner.
- `[~]` `P-BANK-002`: Root browser có đúng sáu nhánh Regular/LK/LK Modified/LK Style/GC/SP và chỉ hiện pool có dữ liệu; presenter/installer compile sạch, chờ prefab/PlayMode.
- `[~]` `P-BANK-003`: Size/rank và hard-tier keys `7:4, 8:4/5, 9:4/5, 10:4/5, 11:4/5, 12:4` được tách N/H đúng source; fixture compile sạch.
- `[~]` `P-BANK-004`: Launch Regular/LK Style/GC giữ seed + r1…r5 + tier flags; LK/LK Modified giữ id/maxR; SP giữ id/r1…r5/colorMap. Exact-key fixtures compile sạch, chờ Game page consumer.
- `[x]` `P-BANK-005`: Initial route priority `go_lk_style → go_lk → go_regular`, panel back stack, selector clamp và row launch khớp nguồn. AppScene PlayMode đã bấm root/Size/Tier/Level row thật cho cả Regular/LK/LK Modified/LK Style/GC/SP; Tier/LK +/- giữ đúng hai cận, launch entry #2 và Back trả đúng panel riêng của từng pool.
- `[~]` `P-BANK-006`: Dynamic size/tier/LK/SP rows được tái sử dụng theo pool và bind release-frame guard khi materialize. AppScene PlayMode stress 8 vòng Regular Root→Size→Tier→Back cùng reopen SP và xác nhận tổng `BankSizeCardView`, `BankTierCardView`, `BankLevelRowView` không tăng; còn profiler/device soak dài.
- `[x]` `P-BANK-007`: `BankPage.prefab` đã được Unity sinh và registry có `UiName.Bank`; structure fixture cùng AppScene PlayMode xác nhận presenter/binding hợp lệ, SP panel/row động materialize và không missing script.
- `[x]` `P-BANK-008`: AppScene PlayMode xác nhận `GamePage` consume launch của cả sáu pool thành `GameplaySessionMode.Bank`; launch đầu hiện `ReturnBankBtn`, Win/Next giữ đúng pool/index và ẩn direct-return. SP Fail/Restart giữ #2/3 mạng/pool metadata; Return Bank reset root.

## P-RESULT — Win, fail và progression

- `[~]` `P-RESULT-001`: Win default + pass-text V0–V3 và pass-page G1/G2/G4 đã port; stats/timing/beat-percent compile sạch, `Root/PassPanel` đã được Unity sinh với reference hợp lệ. PlayMode/pixel parity còn chờ.
- `[x]` `P-RESULT-002`: Coordinator settle/Next main và Bank next-launch có guard một lần cùng fixtures. AppScene PlayMode xác nhận Main Win → Streak/Award → level 2 và cả sáu Bank pool Win/Next đều giữ đúng pool/index, tải đúng một lần.
- `[~]` `P-RESULT-003`: Fail presenter dùng remaining cats từ terminal transition, đúng title/encourage/promote source; AppScene PlayMode xác nhận toàn page bị chặn 1,5 giây trong khi button giữ trạng thái/tint, còn visual parity.
- `[~]` `P-RESULT-004`: Fail restart không settle/advance lần hai và giữ `restart_count`; AppScene PlayMode đã xác nhận 3 wrong → Fail → Restart vẫn level 1/3 mạng rồi Win → level 2. Rewarded revive cũng chịu được vòng Fail thứ hai; còn app-kill PlayMode.
- `[x]` `P-RESULT-005`: Revive khôi phục 1/3 lives theo `revive_life`, resume clock và đóng Fail. AppScene PlayMode xác nhận default hồi đúng 1 mạng sau callback reward và không settle lặp khi ad đóng.
- `[x]` `P-RESULT-006`: Free-once persisted/idempotent và reward revive đi qua boundary. AppScene PlayMode xác nhận null provider ẩn Revive; test provider xác nhận đúng position Main, chỉ `ad_rewarded` mới hồi sinh, còn `ad_closed` không cấp mạng và mở lại nút. Provider test không tồn tại trong runtime production.
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

- `[~]` `P-META-001`: Daily unlock/date/countdown, persisted completion state, beat-percent, deterministic bank/8-transform selection, launch contract và Home entry presenter đã port. AppScene xác nhận locked entry không mở page, level 21 mở đúng `DailyGame` mode Daily, Back về Home và entry rollover Done→Normal qua ngày mới; còn visual parity.
- `[~]` `P-META-002`: Daily Fail→rewarded Revive→Fail→Restart→Win đã đạt AppScene PlayMode; restart giữ ngày/index/puzzle, revive chỉ tăng stats `daily`, win lưu date/elapsed/beat một lần và Main level/strategy/retry/snapshot/DDA/stats không đổi. Continue mở page Game thật rồi đóng hai page Daily đúng nguồn. Còn tracker sink-capture và visual parity.
- `[x]` `P-META-003`: `ClockTicker` scene-owned phát tick giây và day-watch độc lập page, reschedule pause/focus, dùng local date key và không catch-up burst. AppScene mô phỏng ngày 10→11 rồi quay lùi: Daily/Streak cùng refresh, Home chỉ advance `max_daily_date` khi show và không cho clock rollback mở lại ngày cũ.
- `[~]` `P-META-004`: Streak check-in, chu kỳ 7, resume/backfill/protect, week slots, pending-win crash recovery và flow Main/Lit/Settle đã port. AppScene đạt ngày 1→7→8, same-day idempotency, broken reset, slot settle delay và chest ngày 7 đúng một lần; còn production rewarded-ad provider cùng visual backfill/resume.
- `[~]` `P-META-005`: Award transaction được ghi vào `in_flight_awards` trước presentation, cold-start sweep cấp lại đúng một lần và trang Collect hoàn tất transaction. AppScene xác nhận Rank Gift group 1 đi đủ hai pha podium/rương → item, cấp Frame +2 Hint +2 Locate đúng một lần; group 3 giữ panel item ẩn, chạy `FrameAddEffect`, chỉ persist +1 frame sau effect rồi xóa in-flight. Còn device app-kill.
- `[~]` `P-META-006`: `CompleteAward`/cold sweep idempotent, double chỉ nhân tool và không nhân frame; reflection fixture đạt, chờ PlayMode thao tác nhanh/đóng popup.

## P-PROFILE — Identity, avatar và frame

- `[x]` `P-PROFILE-001`: Profile rỗng tự tạo nickname 6 ký tự, avatar/frame hợp lệ và sở hữu đủ 8 classic frame; initialization chỉ save một lần. Repository ghi đúng envelope nguồn `profile/data`, đọc tương thích flat schema Unity cũ và file-backed restart giữ nguyên identity/inventory.
- `[~]` `P-PROFILE-002`: Nickname trim/giới hạn 12 code point, avatar/frame validation, unlock/equip/count và frame red-dot giữ đúng source. Profile presenter dùng pending selection, Avatar/Frame tabs, clear red-dot khi vào Frame, locked-frame shake/tooltip và chỉ commit khi Confirm; chờ PlayMode.
- `[~]` `P-PROFILE-003`: Remote export mã hóa nickname `b64:`, chỉ đồng bộ frame id ≥100 và chỉ merge khi remote ahead; fixture round-trip đạt, backend sync chờ R16.
- `[~]` `P-PROFILE-004`: Award frame đi qua `ProfileRuntime` scene-owned vào ProfileService và persistence thật. AppScene frame-only xác nhận inventory chỉ tăng khi effect kết thúc; file-backed cold-start xác nhận Rank reward dở dang cấp frame đúng một lần qua hai restart liên tiếp. Còn hard-kill thiết bị thật.
- `[~]` `P-PROFILE-005`: Installer dựng cây prefab lồng `ProfileAvatarView → ProfileSelectionCell → ProfilePage` từ sprite gốc, layout 900×1253, grid 4 cột 185 px/gap 6, localization và route `UiName.Profile`; chờ Unity Refresh sinh asset và kiểm tra hai aspect.

## P-ROBOT — Robot leaderboard simulation

- `[~]` `P-ROBOT-001`: `RobotConfig`, pool/data/timeline và repository giữ đúng key/source defaults trong file riêng `robots.cfg`; model round-trip fixture đạt.
- `[~]` `P-ROBOT-002`: Player-base theo bốn rank band, bot random-power, closest-approach/fill-to-zero, weighted timeline, first-hour và cooldown/backward clamp đã port; fixture bao phủ floor/ceiling, overshoot và cooldown.
- `[~]` `P-ROBOT-003`: Ranking cộng timeline theo phút, hòa điểm ưu tiên timestamp sớm và ánh xạ robot sang `PlayerInfo`/award theo rank đúng source; fixture đạt.
- `[~]` `P-ROBOT-004`: Khi player vượt `x_base`, future timeline bị freeze và top pool catch-up theo capacity/gap/delta-time; overtake delay giữ đúng đơn vị giây thực tế của source. Fixture freeze/catch-up đạt.
- `[x]` `P-ROBOT-005`: Effective now clamp `end_unix`, không lùi dưới `last_seen_unix`; create/discard/reset persistence và `RobotRuntime` scene-owned đã có. File-backed process restart với clock rollback giữ cùng rank; pool kỳ cũ bị discard và pool kỳ 2 sống qua restart tiếp theo.
- `[~]` `P-ROBOT-006`: Catalog đúng 1.699 nickname gốc, first/last và SHA-256 `f864d2094a2587d4c030371373120fd44c693061edde7af3c54292d3dac7e4fd` có regression; không dùng tên tự tạo.

## P-RANK — Rank Activity

- `[x]` `P-RANK-001`: Ba group, unlock level 11, period 86.400 giây, first-session/home rules và reopen giữ đúng source. AppScene xác nhận entry ẩn level 1/hiện level 21; manager integration xác nhận kỳ không thưởng không mở ở 9 win và mở kỳ 2 đúng win thứ 10.
- `[~]` `P-RANK-002`: Level collect Main/Bank chỉ commit khi thắng; restart/exit xóa cache, win ghi score/timestamp, rank dùng RobotService tie-break và expiry trong level defer settlement. Daily lifecycle đã được gate hoàn toàn; AppScene giữ Rank qua Daily Fail/Revive/Restart/Win. File-backed restart/time rollback/expiry/reward recovery/kỳ 2 đã đạt; còn device/long-session soak.
- `[x]` `P-RANK-003`: Home entry/open popup/confirm/profile-guide và popup priority đã nối. AppScene xác nhận Action/Close kỳ đầu đều commit participation; Close giữ `WasStarted=false` nhưng vẫn đi Profile guide/Game. Sau kỳ có thưởng, period 2 chỉ mở khi quay lại Home; Close kỳ sau confirm join và ở Home, không tự vào Game.
- `[~]` `P-RANK-004`: Leaderboard page có header/countdown/top-3/list/self/profile/info/CTA; row dùng avatar/frame/medal/badge/chest mapping hạng 1→tier3, 2→tier2, 3→tier1. VBox/safe-area đã khớp 1080×1920/2400; list/CTA geometry, row spacing, clamped scroll, `Appear1/2`, self-row nổi và shadow fade/lật ở hai mép có prefab/contract regression. Còn particle/pixel parity.
- `[~]` `P-RANK-005`: Change page giữ appear/count/score-roll/rank-settle timing, khóa dismiss/scroll khi animation, encouragement source và strip BBCode Godot. Row nhìn thấy dùng `Appear3`; padding dọc 200, center-scroll, safe groups hai aspect, sáu Cat/Fish collection, bốn arrow, lift/rise/drop, glow/star burst và domino/scroll-follow đã port. Nhánh không thăng hạng cũng chạy lift/drop sau collection đúng nguồn; final row swap tại 0,23 giây trong drop. AppScene xác nhận không thể tiếp tục trước khi animation mở `TapToContinue` và cleanup đạt. Còn pixel/video parity.
- `[~]` `P-RANK-006`: HTP đổi Cat/Fish và full/frame-only reward theo group, tap/escape đóng đúng route; chờ prefab generation/visual parity.
- `[~]` `P-RANK-007`: Settlement reward table đúng nguồn, durable RankGift transaction, top-3 params/win-count, hai pha podium/rương → item, frame-only và period-next flow đã nối. Appear1/Appear3, Open1 cue 0,8834 giây, chest/celebration UGUI, frame trail/burst với cubic Bézier X/Y và profile shake đã port từ nguồn. AppScene group 1 xác nhận +1 leaderboard Frame, +2 Hint, +2 Locate; group 3 xác nhận panel item không xuất hiện, +1 frame sau effect và transaction rỗng. Còn SFX, pixel/video parity và device app-kill.

## P-AUTH — Auth, device identity và API

- `[x]` `P-AUTH-001`: `ApiConfig` giữ đúng app ID, dev/prod base URL, sign secret, sync/account path, response code và platform string của nguồn.
- `[x]` `P-AUTH-002`: Auth chỉ bootstrap đúng một lần sau khi cả Analytics và LUID sẵn sàng; payload giữ `base_url/secret/luid/show_log/is_keychain_sync_enabled=false`, rồi guest login đúng source.
- `[x]` `P-AUTH-003`: Access-token request giữ provider JSON boundary, callback một lần, force-refresh và timeout monotonic đúng 12.000 ms; plugin thiếu trả `-100` an toàn mà không chặn startup.
- `[x]` `P-AUTH-004`: Login result/error/profile và expired signal đã port; auto guest relogin debounce 60.000 ms, dừng sau 5 lần liên tiếp và reset counter khi login thành công.
- `[~]` `P-AUTH-005`: `AuthRuntime` scene-owned được installer đặt trong `App/Systems`, dispose callback đúng lifecycle và null provider không log rác. Còn native iOS/Android AuthPlugin + UniKit prerequisite/device adapter và kiểm thử thiết bị thật.

## P-SYNC — Data Sync và merge

- `[x]` `P-SYNC-001`: Registry idempotent, late-registration handler và bốn savable ID `core/profile/streak/rank` đúng nguồn; GameState là merge-basis đầu tiên.
- `[x]` `P-SYNC-002`: Download parse, shared `remote_ahead`, per-savable merge, unknown root/block passthrough và malformed-body giữ nguyên baseline đã có regression.
- `[x]` `P-SYNC-003`: First upload, cached meta fast-path, no-local-change skip, sync-code upload và conflict download/merge/retry tối đa 3 đúng source.
- `[x]` `P-SYNC-004`: Token thường/force-refresh, invalid/expired retry, trigger startup/level/profile/streak/late savable, one-flight coalescing và Home refresh khi `changed=true` đã nối.
- `[x]` `P-SYNC-005`: HTTP giữ raw-body MD5 sign, source header, server-Date offset retry và timeout 10 giây; AppBootstrap wait tối đa 2 giây, request cleanup khi disable/destroy.
- `[~]` `P-SYNC-006`: Snapshot `sync/remote_root` và development `cheat/sync_enabled` đã round-trip qua verified file; AppScene offline regression đạt. Còn native UniKit metadata/Auth provider, backend integration và offline/clock/app-kill trên thiết bị thật.

## P-PLATFORM — Privacy, CMP, ATT và Push

- `[x]` `P-PLATFORM-001`: Privacy dialog chặn startup khi provider yêu cầu; Accept đóng animation trước, gọi `AgreePrivacy` đúng một lần rồi mới tiếp tục init ATT/startup push.
- `[x]` `P-PLATFORM-002`: Startup push chỉ chạy mobile khi `push_ask_count < 2`, dùng request `System` tại `app_start`, tăng ask count/mark triggered; sau đó enable push, remove hai lịch cũ và đăng ký lại noon/evening sau A/B ready + 0,5 giây.
- `[x]` `P-PLATFORM-003`: Android gọi CMP rồi trả ngay. iOS/new user chờ CMP tối đa 2 giây, chọn Pre-ATT thường/V2/skip theo `att_dlg_logic`, chỉ gọi system ATT khi status `NotDetermined`, mark guide và giữ post-ATT delay 1 giây đúng nguồn.
- `[x]` `P-PLATFORM-004`: Push guide giữ level ≥20, group recent-three-day/session-streak, popup cap 5 và cooldown 5 ngày; Win normal block/delay 2,467 giây chỉ khi đủ điều kiện. Allow chọn `SystemAndSetting` khi ask count <2, nếu không dùng `Setting`; Close không request nhưng cả hai mark triggered/popup shown.
- `[x]` `P-PLATFORM-005`: `push_local_text` legacy dùng 4 nội dung noon + 4 evening; hai pool mới shuffle 100 và lấy 5, lịch 12:00/20:00 lặp vô hạn 86.400.000 ms với advance-day đúng giờ local.
- `[~]` `P-PLATFORM-006`: `PrivacyPermissionRuntime` nằm dưới `App/Systems`; UIManager/AppBootstrap/Settings/Win dùng cùng serialized runtime. Bốn prefab/registry không missing script và targeted Unity đạt EditMode 17/17, PlayMode 3/3. Còn native iOS/Android UniKit adapter, OS dialog callback/focus fallback thực tế, local-notification persistence và device/pixel validation.

## P-TRACK — Tracker và Session

- `[~]` `P-TRACK-001`: Event/screen/dialog/game/ad/prop schema, source stack, game ID, round stats và transform→question rotation giữ đúng key/giá trị nguồn; `TrackingCoreTests` đạt qua runner thuần.
- `[~]` `P-TRACK-002`: Session cộng active time theo monotonic clock, flush mỗi 60 giây và chỉ tạo session mới khi background lớn hơn 1.800 giây; fixture cùng Gameplay lifecycle boundary đạt, còn actual focus/pause duration và app-kill thiết bị thật.
- `[~]` `P-TRACK-003`: `UITrackerObserver` tập trung nghe `WindowShown`; dialog được pop khỏi source stack tại đúng ranh giới close lifecycle. Splash/Home/Game/Daily/Win/Fail/Settings/Language/Profile/Streak/Rank metadata đã nối; các page/call site online còn lại chưa hoàn tất.
- `[~]` `P-TRACK-004`: Runtime mặc định dùng no-op sink, không log debug và không chặn startup khi thiếu SDK. Consent/CMP/ATT/push ordering cùng push-guide result payload đã nối; online tracking provider và network-failure/device attribution parity còn mở.
- `[~]` `P-TRACK-005`: Language confirm/cancel/property, Settings button/switch/dropdown lồng và Rank Award `challenge_reward → challenge_reward_get` đã nối đúng source. Còn PlayMode sink-capture để xác nhận thứ tự window animation thực tế.
- `[~]` `P-TRACK-006`: Main/Daily phát `game_start` sau khi session/board sẵn sàng; New/Continue/Restart, game ID, qid hard-tier, qrotate và challenge flag có fixture. Fail/Win/Restart phát full `game_end`; restart giữ đúng thứ tự nguồn và transition chụp board counters trước reload. Hint/Locate/Clear/apply/stop/detail, gesture step, erase, hint-cross, invalid-sign, game-die và button call site hiện có của Home/Profile/Streak/Rank/Result đã nối đúng nguồn; tiêu tool/Award tool phát `prop_use/prop_get` đúng sau mutation, fixture khóa `step_used` từ round stat. Coordinate UI và device attribution còn thiếu.
- `[~]` `P-TRACK-007`: `AdService` giữ show-id/position từ readiness tới impression, chỉ impression phát `interstitial_ad_show/rewarded_ad_show`; shown/rewarded/closed độc lập và request rewarded hoàn tất đúng một lần. Null provider không track/cấp thưởng, dispose tháo callback. Hint/Locate, Fail revive, Streak revive, audio/Daily clock và full default interstitial gate order đã nối; board entry chỉ chạy sau close/error/focus. Banner gate/lifecycle và reward-restore watchdog 30 giây + late callback + durable Home popup đã port. Provider-neutral A/B timing, persisted first-open và LivingDays local-calendar segment đã nối vào ba policy Ads; AppScene PlayMode đã xác nhận callback success/close-failure của Fail revive. Còn production ad/A-B SDK adapter và device callback parity.

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
| 2026-08-11 | Unity 6000.3.19f1 | R13 Main result + Bank + rewarded revive PlayMode | Main Fail/Restart/Win/Next, null-ad gate, rewarded success/close-failure, SP card→Game→Return và SP Win→Next #2→Fail→Restart đạt; EditMode 510/510, PlayMode 6/6 | `Reports/Codex/R13_PlayMode_Main_Result_Loop_Report_2026-08-11.md` |
| 2026-08-11 | Unity 6000.3.19f1 | R12 Settings shared A/B + conditional Button routes | `settings_language/rule_text` dùng chung runtime; Home Settings→Language và Game Settings→HowToPlayPaged đạt; EditMode 511/511, PlayMode 7/7 | `Reports/Codex/R12_Settings_Conditional_Routes_Report_2026-08-11.md` |
| 2026-08-11 | Unity 6000.3.19f1 | R12–R13 Bank six-pool PlayMode matrix | Regular/LK/LK Modified/LK Style/GC/SP launch và Win/Next đúng pool/index; direct-return/reuse đạt; EditMode 511/511 gần nhất, PlayMode 8/8 | `Reports/Codex/R13_Bank_Pool_Matrix_Report_2026-08-11.md` |
| 2026-08-11 | Unity 6000.3.19f1 | R10 transition/input guard stress | Source local blocker Game/Fail/Daily Win đã port; release-frame, refresh/cleanup, 96 close/reopen, mask/Z compact và concurrent one-flight đạt; EditMode 511/511, PlayMode 9/9 | `Reports/Codex/R10_UI_Transition_Input_Guards_Report_2026-08-11.md` |
| 2026-08-11 | Unity 6000.3.19f1 | R12 Settings pattern + Paged HTP interaction | 12 sprite/màu nguồn, hide-on-filled, toggle persistence, signal-before-close và Previous/Next/Got it đạt; EditMode 512/512, PlayMode 9/9 | `Reports/Codex/R12_Settings_Pattern_Paged_Interactions_Report_2026-08-11.md` |
| 2026-08-11 | Unity 6000.3.19f1 | R13 Main lifecycle + snapshot durability | `in_game_sec` round-trip, MARK debounce flush, callback dedup và Playing/Fail/Revive/Win/Next matrix đạt; EditMode 513/513, PlayMode 10/10 | `Reports/Codex/R13_App_Lifecycle_Snapshot_Report_2026-08-11.md` |
| 2026-08-11 | Unity 6000.3.19f1 | R12 Home meta feature availability | Shared Home A/B config, Profile/Daily/Streak/Rank visibility và entry routes đạt; EditMode 514/514, PlayMode 11/11 | `Reports/Codex/R12_Home_Meta_Entry_Availability_Report_2026-08-11.md` |
| 2026-08-11 | Unity 6000.3.19f1 | R14 Daily result lifecycle/isolation | Daily Fail/Revive/Restart/Win/Continue, Main sentinel isolation và Rank gate đạt; EditMode 514/514, PlayMode 12/12 | `Reports/Codex/R14_Daily_Result_Lifecycle_Report_2026-08-11.md` |
| 2026-08-11 | Unity 6000.3.19f1 | R15 Rank popup close + reopen threshold | First-period Close vẫn join/Profile/Game; no-reward period mở lại đúng win thứ 10; EditMode 515/515, PlayMode 13/13 | `Reports/Codex/R15_Rank_Close_Reopen_Report_2026-08-11.md` |
| 2026-08-11 | Unity 6000.3.19f1 | R14 Daily rollover/focus/backdate | Shared ClockTicker, focus/pause resume, Daily/Streak refresh và max-date anti-rollback đạt; EditMode 515/515, PlayMode 14/14 | `Reports/Codex/R14_Daily_Rollover_Focus_Report_2026-08-11.md` |
| 2026-08-11 | Unity 6000.3.19f1 | R14 Streak multi-day cycle | Ngày 1→7→8, same-day no-op, broken reset, settle slot delay và durable chest đạt; EditMode 515/515, PlayMode 15/15 | `Reports/Codex/R14_Streak_MultiDay_Report_2026-08-11.md` |
| 2026-08-11 | Unity 6000.3.19f1 | R15 Rank expiry/reward/next period | Expiry trong Main defer tới Win; Rank Change, RankGift hai pha, durable reward và period 2 chỉ mở ở Home đạt; EditMode 515/515 gần nhất, PlayMode 16/16 | `Reports/Codex/R15_Rank_Expiry_Reward_Next_Period_Report_2026-08-11.md` |
| 2026-08-11 | Unity 6000.3.19f1 | R15 Rank leaderboard layout/scroll/rise | VBox + safe-area 1920/2400, sticky self-row, Change padding/center/rise/domino/scroll-follow và prefab upgrade đạt; EditMode 525/525, PlayMode 16/16 | `Reports/Codex/R15_Rank_Leaderboard_Layout_Scroll_Report_2026-08-11.md` |
| 2026-08-11 | Unity 6000.3.19f1 | R15 Rank frame-only Award | Panel item loại frame, FrameAddEffect timing/avatar/count, persist-after-effect và AppScene group 3 đạt; EditMode 527/527, PlayMode 17/17 | `Reports/Codex/R15_Rank_Frame_Only_Award_Report_2026-08-11.md` |
| 2026-08-11 | Unity 6000.3.19f1 | R15 Rank row intro/self-shadow | VisualRoot độc lập layout, Appear1/2/3, sticky shadow fade/lật và bridge abort recovery đạt; EditMode 529/529, PlayMode 17/17 | `Reports/Codex/R15_Rank_Row_Intro_Self_Shadow_Report_2026-08-11.md` |
| 2026-08-11 | Unity 6000.3.19f1 | R15 Rank Change collection/arrow/rise celebration | Sáu Cat/Fish collection, arrow loop, lift/drop, glow/star burst và timeline hai nhánh đạt; EditMode 529/529, PlayMode 17/17 | `Reports/Codex/R15_Rank_Change_Celebration_Report_2026-08-11.md` |
| 2026-08-11 | Unity 6000.3.19f1 | R15 Rank Gift chest/celebration + frame flight | Appear/Open cue, chest tier, podium burst, trail/burst cubic và profile shake đạt; EditMode 530/530, PlayMode 17/17 | `Reports/Codex/R15_Rank_Gift_Chest_Frame_Flight_Report_2026-08-11.md` |
| 2026-08-11 | Unity 6000.3.19f1 | R15 Profile/Robot/Rank persistence durability | Source `profile/data` + legacy migration, clock rollback, interrupted reward, pool discard và period 2 restart đạt; EditMode 532/532, PlayMode 17/17 | `Reports/Codex/R15_Meta_Persistence_Durability_Report_2026-08-11.md` |
| 2026-08-11 | Unity 6000.3.19f1 | R16 Auth/API provider boundary | Gate Analytics+LUID, payload/guest login, token timeout 12 giây, relogin cap và API constants đạt; EditMode 537/537, PlayMode 17/17 | `Reports/Codex/R16_Auth_Api_Boundary_Report_2026-08-11.md` |
| 2026-08-11 | Unity 6000.3.19f1 | R16 Data Sync/merge/startup | Bốn savable, remote-ahead, unknown passthrough, meta/upload/conflict/token refresh, signed HTTP và startup 2 giây đạt; EditMode 547/547, PlayMode 17/17 | `Reports/Codex/R16_Data_Sync_Merge_Startup_Report_2026-08-11.md` |
| 2026-08-12 | Unity 6000.3.19f1 | R16 Privacy/CMP/ATT/Push/local notification | Source ordering, counter/cooldown/A-B policy, bốn popup/registry, Win push-guide và local schedule đạt; targeted EditMode 17/17, PlayMode 3/3 | `Reports/Codex/R16_Privacy_ATT_Push_Report_2026-08-12.md` |
