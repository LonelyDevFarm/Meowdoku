# Parity Checklist — Godot ↔ Unity

> Cập nhật: 2026-08-12  
> Trạng thái ban đầu chủ ý để trống; đánh dấu sau khi có bằng chứng test hoặc recording đối chiếu.

## Cách ghi bằng chứng

Mỗi case hoàn thành phải ghi một trong các bằng chứng cạnh checkbox hoặc trong test report:

- Tên automated test.
- Video/screenshot Godot và Unity cùng thao tác.
- Fixture đầu vào + output kỳ vọng lấy từ mã nguồn.

Không dùng cảm giác “có vẻ giống” để đóng case.

## P-BOOT — Startup và navigation

- `[x]` `P-BOOT-001`: Contract launcher chờ theo mốc 2,0+0,5 giây; Splash dùng progress mặc định 3,0 giây và force-finish tween 0,1 giây đúng nguồn. Bằng chứng: `UIPopupStartupTests.SplashTiming_MatchesLauncher` cùng AppScene timing trong `PlatformStartup_CorruptPlayerSlotsUseDefaultsAndExitSplash`.
- `[x]` `P-BOOT-002`: First-session/default state chưa hoàn thành tutorial đóng Splash rồi route tới prefab `TutorialPagePresenter` thật. Bằng chứng: `InitialRoute_UsesPersistedTutorialDone` và `PlatformStartup_CorruptPlayerSlotsUseDefaultsAndExitSplash`.
- `[x]` `P-BOOT-003`: Với `tutorial_done=true`, AppScene PlayMode hoàn tất bootstrap và route đến Home thật. Bằng chứng: `PrimaryNavigationPlayModeTests.AppScene_PrimaryRoutes_OpenCloseAndReuseAtRuntime`.
- `[x]` `P-BOOT-004`: State thiếu có repository regression; khi cả hai player slot hỏng, AppScene vẫn dùng level/strategy/tool defaults, hoàn tất bootstrap, đóng Splash, route Tutorial và ghi lại slot mặc định hợp lệ. Bằng chứng: `GameStateRepositoryTests.MissingSave_LoadsSourceDefaults` và `PlatformStartup_CorruptPlayerSlotsUseDefaultsAndExitSplash`.
- `[x]` `P-BOOT-005`: Escape/Android Back trên Home đi qua raw Input System boundary và mở Confirm nguồn; Back lần hai được top Confirm tiêu thụ nhưng không đóng, Close chỉ đóng popup và Action gọi quit đúng một lần. Bằng chứng: `PrimaryNavigationPlayModeTests.PlatformNavigation_HomeBackOpensSourceQuitConfirm`.
- `[~]` `P-BOOT-006`: `AppScene/UI/SharedOverlays/ModalMask` và release `InputGuard` đã serialized; local `block_input_briefly` dùng transparent raycast canvas z=4095 đúng nguồn. AppScene PlayMode gửi pointer down/up thật, xác nhận guard release bật rồi tự dọn cuối frame, local blocker refresh deadline/chỉ còn một instance/tự dọn; Fail xác nhận Game 2,0 giây + Fail 1,5 giây cùng hoạt động. Còn touch/raycast device thật.
- `[x]` `P-BOOT-007`: Cache/one-flight/loading guard đạt AppScene PlayMode: 96 vòng Hide → Closing → Show giữ cùng instance, không duplicate shown/hidden, không rò mask, Z compact trong range; hai `ShowAsync(Language)` đồng thời chỉ create/show một page và kết thúc `IsAnyLoading=false`.
- `[x]` `P-BOOT-009`: Registry của 8 page mục tiêu trỏ đúng prefab/presenter, không missing script, binding bắt buộc được serialize và từng page đã show/hide trong AppScene PlayMode; Game dựng level 1. Bằng chứng: composition fixtures + `PrimaryNavigationPlayModeTests`; EditMode 508/508, PlayMode 1/1.
- `[x]` `P-BOOT-008`: Ba chu kỳ callback Unity ghép `focus+pause`/resume không tạo session hoặc Game page trùng; session id/count giữ nguyên, `session_record` tăng đúng một lần mỗi chu kỳ và CAT đang chơi không bị reset. Bằng chứng: `PlatformLifecycle_FocusPauseResumeDoesNotDuplicateSessionOrGamePage`.

## P-SAVE — State và persistence

- `[x]` `P-SAVE-001`: Save → restart khôi phục current level/strategy. Bằng chứng: `GameStateRepositoryTests.PlayerState_RoundTripsP0Fields`.
- `[x]` `P-SAVE-002`: Khôi phục tool counts và settings. Bằng chứng: `GameStateRepositoryTests.PlayerState_RoundTripsP0Fields`.
- `[x]` `P-SAVE-003`: Atomic write không thay slot tốt hoặc đổi flag khi verify file tạm thất bại; file `.tmp` lỗi được dọn và lần load kế tiếp vẫn trả slot đã commit mới nhất. Bằng chứng: `SaveStoreTests.DualSlot_VerifyFailurePreservesCommittedSlotsAndFlag`.
- `[x]` `P-SAVE-004`: Slot chính hỏng thì đọc slot dự phòng. Bằng chứng: `SaveStoreTests.DualSlot_CorruptPrimaryFallsBackToPreviousSlot`.
- `[x]` `P-SAVE-005`: Cả hai player slot lỗi thì `GameStateRepository` trả state mặc định nguồn an toàn; endgame file lỗi bị cô lập, không làm mất player progress hợp lệ. Bằng chứng: `BothPlayerSlotsCorrupt_LoadsSourceDefaultsSafely` và `CorruptEndgame_DoesNotDiscardValidPlayerState`.
- `[x]` `P-SAVE-006`: Legacy save migration chỉ chạy đúng một lần. `SaveStoreTests.LegacyMigration_WritesFirstSlotAndPreservesLegacy` xác nhận lần đầu ghi slot A, giữ legacy và lần gọi thứ hai trả `NotNeeded` mà bytes slot không đổi; Unity targeted runner đạt.
- `[~]` `P-SAVE-007`: PlayMode lifecycle boundary khôi phục schema snapshot với `in_game_sec` và chụp MARK đang debounce; hard-kill/resume trên thiết bị thật còn chờ R17.
- `[x]` `P-SAVE-008`: Playing/Fail/Revive/Win/Next giữ hoặc xóa snapshot đúng thời điểm trong AppScene matrix; Win suspend không tái tạo snapshot hoàn tất và Next ghi level mới.
- `[~]` `P-SAVE-009`: Player, endgame, profile, streak, rank và robot runtime không còn mã hóa/verify/fsync trên main thread; mỗi store coalesce immutable snapshot mới nhất và toàn app dùng một background write lane để tránh tranh CPU mobile. Pause/focus-out/quit flush đúng lifecycle; regression player/profile background đạt. Còn app-kill trên thiết bị thật.

## P-LEVEL — Bank và level selection

- `[x]` `P-LEVEL-001`: 5.411 level thường có region/solution hợp lệ.
- `[x]` `P-LEVEL-002`: Static `LevelData.get_size` khớp 1–100 và chu kỳ 101+; Main gameplay dùng riêng `_get_ab_size` control/variant A–F đúng nguồn. AppScene level 3 xác nhận production dựng 6×6 dù static bank schedule là 5×5. Bằng chứng: `SizeCycle_ControlMatchesSourceGameplayScheduleOneTo250`, `SizeCycle_VariantsMatchSourceBoundaries` và `PlatformLevelSelection_MainUsesSourceControlSizeCycle`.
- `[x]` `P-LEVEL-003`: Toàn bộ 18 Special mapping trả đúng SP/LK source/index; `normal_level_10=1` đổi riêng level 10 từ SP44 sang SP57. Bằng chứng: `GetLevelEntry_SpecialMapMatchesEverySourceEntry` và `GetLevelEntry_Level10VariantSelectsSp57`.
- `[x]` `P-LEVEL-004`: Hard level thường từ level 21 với bội số 10 chọn cố định rank 5, tier N, strategy 5; Special có cùng số level vẫn được ưu tiên bởi mapping nguồn. Bằng chứng: `ResolveDifficulty_OrdinaryHardLevelUsesRankFiveNormalTier` và `ResolveDifficulty_LowerRngUsesInclusiveLowerBound`.
- `[x]` `P-LEVEL-005`: Ordinary entry hợp lệ advance đúng một index; merged Main tăng `idx/since_lk` theo source và toàn lượt chỉ commit một lần. Special SP/LK không chạm sequential progress. Bằng chứng: `GetLevelEntry_ValidOrdinaryAdvancesOnceAndCommitsOnce`, `GetLevelEntry_InvalidMainEntryAdvancesThenCommitsAcceptedEntry` và `GetLevelEntry_SpecialDoesNotMutateSequentialProgress`.
- `[x]` `P-LEVEL-006`: Ordinary/Main entry có solution lỗi được advance trong bộ nhớ rồi bỏ qua; khi cả pool lỗi, fallback cố ý trả entry cuối nhưng không advance entry cuối đúng source. Không có intermediate disk write. Bằng chứng: `GetLevelEntry_InvalidOrdinaryIsSkippedBeforeValidEntry`, `GetLevelEntry_AllInvalidOrdinaryReturnsLastWithoutAdvancingIt` và fixture Main tương ứng.
- `[x]` `P-LEVEL-007`: Đủ tám transform 0–7 giữ đúng thứ tự nguồn: 0–3 xoay theo chiều kim đồng hồ, 4–7 lật ngang trước rồi xoay; region map và solution biến đổi cùng tọa độ, không làm bẩn entry bank gốc. Bằng chứng: tám case của `ApplyTransform_AllEightVariantsMatchSourceRegionMapAndSolution` với expected map/solution hard-code độc lập.
- `[x]` `P-LEVEL-008`: 13 asset LK Modified/LK Style/GC trong Unity có SHA-256 giống hệt nguồn; parser giữ đúng inventory size/rank/tier thật. Selector merge `regular → lkstyle → gc`, chỉ thêm GC tại 10×10 rank 1 hoặc size 11, bỏ regular Main 10×10 rank 3/4, loại đúng 9 LK Modified reserved, chèn LK sau bốn ordinary, không transform LK và relax `r → maxR` sau entry lỗi đúng nguồn. Bằng chứng: `BankData_RealVariantBanksMatchSourceInventory`, `BankData_RealVariantTierFiltersMatchSourceInventory` cùng bốn fixture selector Main/ordinary.
- `[x]` `P-LEVEL-009`: Main entry mới tạo puzzle ID canonical, ghi lịch sử kèm version/source và snapshot ba progress sau selection; trùng puzzle ở level khác thì persistent-advance thêm đúng branch ordinary/Main/LK rồi chọn lại một lần. Cùng puzzle ở cùng level không retry; lần chọn lại vẫn trùng thì chấp nhận fallback, không lặp vô hạn. Snapshot/retry cache/Bank/Daily không đi qua protection này. AppScene fixture khóa lịch sử `old A, old C → new A → new C`, bank index `0→1→2→3` và retry cache giữ C.
- `[x]` `P-LEVEL-010`: Main mới level 1–10 gọi đúng tutorial `compute_prefill`, retry/snapshot giữ cùng prefill và không tính lại; level 21+ đọc `pre_cat` từ shared A/B timing, consume pending một lần, giữ lock vị trí qua retry, loại riêng pre-cat khỏi retry payload và không tạo lock khi không có scenario. Bằng chứng: `ComputePrefill_UsesSourceTutorialRegionRules`, `LevelSelectionConfigSet_ReloadsPreCatOnlyAtNormalLevel21Timing`, `PlatformLevelSelection_MainUsesSourceControlSizeCycle` và `PlatformLevelSelection_PreCatUsesConfigAndKeepsLockedCellOnRetry`.
- `[x]` `P-LEVEL-011`: Color map default, `bank_transform` seed, RGB, Lab và hai nhánh pattern khớp fixture nguồn; toàn bộ palette `region_color` 0–12 giữ đúng thứ tự/độ dài. `RegionColorConfig` nay thuộc shared `BoardConfigSet`, nên giá trị AppStart từ provider đi tới cả Main/Tutorial `BoardView` thay vì luôn dùng instance mặc định. Bằng chứng: `LevelGenerator_DefaultMapKeepsGodotComparatorDirection`, `LevelGenerator_SeededLcgMatchesSourceFixture`, hai fixture RGB/Lab/pattern, 13 case palette, composition consumer và `PlatformColorMap_RegionColorRuntimeReachesBoard`.

## P-CORE — Luật gameplay

- `[x]` `P-CORE-001`: Cell enum values và `is_blank`/`is_cross` khớp nguồn. Bằng chứng: `CellStateTests`.
- `[x]` `P-CORE-002`: Hai cat cùng region trả `SameColor`. Bằng chứng: `QueendokuCoreTests.ClassifyViolation_UsesSourceRulePriority`.
- `[x]` `P-CORE-003`: Hai cat cùng hàng/cột trả `SameLine`. Bằng chứng: `QueendokuCoreTests.ClassifyViolation_UsesSourceRulePriority`.
- `[x]` `P-CORE-004`: Hai cat chạm chéo/kề trả `NoTouch`. Bằng chứng: `QueendokuCoreTests.ClassifyViolation_UsesSourceRulePriority`.
- `[x]` `P-CORE-005`: Khi có nhiều vi phạm, priority rule khớp nguồn. Bằng chứng: cùng region được ưu tiên trước same line trong `QueendokuCoreTests`.
- `[x]` `P-CORE-006`: `FindConflicts` chỉ xét CAT, trả đủ mọi CAT tham gia ít nhất một cặp xung đột và không đưa MARK/ERROR vào kết quả. Fixture năm CAT khóa hai cặp cùng hàng, trong khi CAT giữa không xung đột.
- `[x]` `P-CORE-007`: `CellsExcludedByCat` trả union row/column/diagonal-touch/same-region đúng thứ tự row-major, bỏ chính CAT và không nhân đôi cell thỏa nhiều luật.
- `[x]` `P-CORE-008`: Complete chỉ true khi đủ N cat và không xung đột. Bằng chứng: `QueendokuCoreTests.IsComplete_RequiresExactlyFourNonConflictingCats`.
- `[x]` `P-CORE-009`: Validator trả `false` an toàn cho region null/sai shape/null row, solution null/sai length/out-of-range và solution có conflict; không tạo session từ bank lỗi vì Main/Daily selector đều gọi cùng validator trước khi chấp nhận entry.
- `[x]` `P-CORE-010`: Score/combo/max combo/restore khớp model nguồn. Bằng chứng: `GameScoreModelTests`.

## P-INPUT — Tap, double tap và swipe

- `[x]` `P-INPUT-001`: EMPTY → MARK được trả ngay tại gesture start. Bằng chứng: `SingleTap_IsReturnedImmediatelyLikeGodotSource`, Platform PlayMode runtime gesture và nghiệm thu desktop trước đó của người dùng.
- `[x]` `P-INPUT-002`: Một tap MARK → EMPTY tức thời. Bằng chứng: `SingleTapOnMark_ReturnsEmptyImmediately` và Platform PlayMode runtime gesture.
- `[x]` `P-INPUT-003`: Hai tap cùng cell trong cửa sổ tạo đúng một DoubleTap. Bằng chứng: `BoardGestureRecognizerTests.SecondTapOnSameCell_WithinWindowEmitsDoubleTap`.
- `[x]` `P-INPUT-004`: Double tap solution kết thúc bằng CAT, không để lại MARK và gộp tap đầu thành một bước EMPTY → CAT. Một Undo trở thẳng về EMPTY; Platform PlayMode và nghiệm thu desktop đều đạt.
- `[x]` `P-INPUT-005`: Double tap non-solution tạo ERROR/wrong-guess, trừ đúng một mạng và gộp tap đầu thành một bước EMPTY → ERROR. Bằng chứng: `GameSession_WrongDoubleTapPersistsSourceErrorState` và `GameSession_WrongDoubleTapFoldsPriorMarkIntoOneUndoStep`.
- `[x]` `P-INPUT-006`: Tap cell khác trong cửa sổ không bị nhận nhầm là double tap và phản hồi độc lập. Bằng chứng: `BoardGestureRecognizerTests.NewCellTap_DoesNotWaitForPreviousDoubleTapWindow`.
- `[x]` `P-INPUT-007`: Swipe từ blank chọn MARK và paint toàn đường. Bằng chứng: `BoardGestureRecognizerTests.Swipe_ReturnsStartImmediatelyAndInterpolatesSkippedCells`.
- `[x]` `P-INPUT-008`: Swipe từ MARK chọn EMPTY và erase toàn đường. Bằng chứng: `BoardGestureRecognizerTests.FastEraseAcrossThreeCells_ChangesStartMiddleAndEnd`.
- `[x]` `P-INPUT-009`: Swipe bắt đầu từ CAT xác định MARK/EMPTY theo cell hợp lệ đầu tiên; fixture khóa cả hai nhánh.
- `[x]` `P-INPUT-010`: Nội suy đường chéo chỉ thay các cell nằm trên đường, không chạm cell kề ngoài đường.
- `[x]` `P-INPUT-011`: CAT/ERROR bị operation bỏ qua; `LOCKED_MARK` bị board model từ chối mutation.
- `[x]` `P-INPUT-012`: Ra ngoài board không phát action; quay lại tiếp tục từ cell hợp lệ cuối và chỉ nội suy đường cần thiết.
- `[x]` `P-INPUT-013`: Pointer up ngoài board vẫn kết thúc đúng gesture đang sở hữu.
- `[x]` `P-INPUT-014`: Mất focus kết thúc gesture một lần, xóa pointer owner và press-position latch.
- `[x]` `P-INPUT-015`: Pointer phụ không thể chiếm hoặc kết thúc gesture của pointer chính.
- `[x]` `P-INPUT-016`: Tap đầu của correct/wrong double-tap được pop rồi gộp theo `consume_prior_tap_before`; một Undo phục hồi thẳng trạng thái trước gesture.
- `[x]` `P-INPUT-017`: Cùng quỹ đạo mô phỏng ở 30/60/120 FPS cho chuỗi cell MARK giống nhau nhờ nội suy bỏ-frame.

## P-BOARD — Board, cell và layout

- `[x]` `P-BOARD-001`: Fixture `BoardGrid_AllSourceSizesRemainSquareAndRowMajorAfterResize` dựng đủ size 4–10, khóa `FixedColumnCount=N` và đúng N² cell active.
- `[x]` `P-BOARD-002`: Cùng fixture resize theo chuỗi tăng/giảm `4→7→5→10→6→9→8`, khóa thứ tự row-major và tọa độ từng cell nên không thể hoán đổi hàng/cột.
- `[x]` `P-BOARD-003`: `SourceBoardLayout` giữ đúng công thức intrinsic `108×N+30`, padding/gap/slot của bốn nhánh `game_grid_ui`; đủ size 4–10 và runtime variant đã qua Unity test thật.
- `[x]` `P-BOARD-004`: Default `new_cell_only`, 13 palette/seed/RGB/Lab/pattern đã khóa ở `P-LEVEL-011`; `RegionColor`, `GameGridUi` và `BoardSizeBig` nay cùng `BoardConfigSet` nguồn và tới Board qua AppScene PlayMode.
- `[x]` `P-BOARD-005`: Single-line overlay khóa đúng 11 cạnh khác vùng trên fixture neighborhood, chỉ vẽ cạnh khi region kề khác nhau; bốn cell góc nhận đúng TL/TR/BR/BL outer radius, cell trong radius 0 và hard-edge đúng source.
- `[x]` `P-BOARD-006`: Fixture pool thực xác nhận release/respawn giữ instance, bật/tắt object đúng và reset state, CAT/cross/error, pattern, hint/prompt, particle cùng transform; `ResetToEmpty` nay luôn stop+clear particle.
- `[x]` `P-BOARD-007`: Fixture visual khóa CAT, MARK trắng, ERROR cam, hai DRAFT không icon và LOCKED_MARK là cross bất biến; `BoardView` cũng giữ CAT đã xác nhận bất biến như source.
- `[x]` `P-BOARD-008`: `PlatformGameplayLayout_ShortLongAndSafeInsetsKeepPrimaryRegionsVisible` mở `GamePage.prefab` thật và khóa cùng `GameplayPageLayoutPresenter` ở 1080×1920/2160/2400, thêm hai profile safe inset 96/54 và 120/80. Header/CatHeart/RuleBar/Board/BottomTools giữ đúng thứ tự, tâm theo contract nguồn và không vượt mép safe top/bottom.
- `[x]` `P-BOARD-009`: Theo phạm vi portfolio/offline đã chốt ngày 2026-08-13, ngưỡng pixel tuyệt đối không còn là release gate khi không có capture Godot đáng tin cậy. Board 1080×1920 đã được người dùng duyệt trực quan; rounded frame/corner, cell state, CAT/MARK/ERROR, region boundary và toàn layout HUD có source-contract/EditMode/AppScene evidence ở `P-BOARD-005..008`. Không dùng bản Godot thiếu Spine runtime để tạo metric giả.

## P-GAME — Vòng đời Main Game

- `[x]` `P-GAME-001`: AppScene Main level 1 tạo đúng puzzle 4×4, một tutorial
  prefill CAT, 3 lives, score/combo/mistake 0; mọi initial model state khớp
  `CellView`.
- `[x]` `P-GAME-002`: Correct cat cập nhật model/view CAT, remaining giảm 1,
  score tăng và combo 1 trong cùng AppScene fixture.
- `[x]` `P-GAME-003`: Wrong cat tạo ERROR ở model/view, giảm life, tăng mistake,
  reset combo và khóa input trong `ResolvingWrongGuess` tới deadline nguồn.
- `[x]` `P-GAME-004`: Rule violation giữ đúng priority và chỉ pulse RuleBar
  theo `rule_highlight`: control tắt, variant 1 chỉ level 1–5 sau Tutorial,
  variant 2 mọi level; Daily không override nên không pulse. Shared config và
  RuleBar thật đã qua EditMode/Platform PlayMode.
- `[x]` `P-GAME-005`: Hết lives phát đúng một Failed transition và mở một Fail
  page; `fail_text` reload đúng timing `game_end` trước khi presenter đọc config.
- `[x]` `P-GAME-006`: Complete phát đúng một Won transition, đánh giá WinToast
  đúng một lần và mở một Win page. `win_toast/pass_page/pass_text` dùng cùng
  shared GameStart config thật.
- `[x]` `P-GAME-007`: Clear/Locate/Hint domain, resource/free/cooldown và
  `HUD/BottomTools/{Locate,Hint}` đã nối đủ. ToolButton hiển thị FREE/count/99+/plus,
  click gọi đúng action, count/badge giữ qua Home→Game; reward đi qua provider
  offline/no-op đúng phạm vi dự án. Bằng chứng: `ToolResourceCoordinatorTests`,
  các case `GameSession_*` và
  `PlatformToolBar_SourceBadgeClickAndPulseStayCoherent`.
- `[x]` `P-GAME-008`: Hint R1/R2/R3/R4/chain có fixture output độc lập; gồm
  ordering/dedup mark, cell state filtering, R2 lock, subset R3/R4 và chain
  contradiction detail.
- `[x]` `P-GAME-009`: Auto-complete khóa session ở Won ngay khi domain hoàn tất;
  double-tap và board edit đều bị từ chối trước visual/result settlement.
- `[x]` `P-GAME-010`: Restart sau Fail giữ nguyên puzzle id/solution, khôi phục
  đúng initial prefill ở cả model/view, đưa lives về 3 và score/combo/mistake
  về 0; rapid Settings restart đã có guard riêng.
- `[~]` `P-GAME-011`: Snapshot Playing/Fail/Revive/Win/Next và elapsed restore đã đạt AppScene PlayMode; exit/hard-kill rồi cold resume trên thiết bị thật còn chờ.
- `[~]` `P-GAME-012`: Focus-out/pause dùng chung một durability boundary, không nhân action/save và snapshot lấy elapsed mới nhất; timer background thật còn chờ device matrix.
- `[x]` `P-GAME-013`: Idle tool hint giữ đúng guard và nhịp 20 giây chờ →
  10 giây chạy → 20 giây chờ; `tool_loop`/`RESET` đã port sang DOTween với
  scale, xoay icon và light pulse, cleanup khi page ẩn. Bằng chứng:
  `ToolResourceCoordinatorTests.RepeatableIdleHint_UsesTwentyTenTwentyCadence`
  và Platform ToolBar fixture.
- `[x]` `P-GAME-014`: Board enter dùng delay ring `column + (N - 1 - row)`, nên cell hiện theo diagonal bottom-left → top-right; normal/reduce-spacing/single-line giữ ba curve và timing nguồn, input chỉ mở sau callback kết thúc. Stop/restart không reset nhầm visual trước lần init.
- `[x]` `P-GAME-015`: `score_encourage` thuộc shared `GameplayConfigSet` và reload ở `GameStart` trước khi dựng session. Runtime 6×6 đã xác minh bubble, multiplier, score-flight hội tụ về authoritative score, RuleBar pulse hai vòng, ToolButton pulse cleanup, completion không mở Win cùng frame; Win phủ trên Game như source và các pool tự trả về 0 sau trail/burst cuối.

## P-TUTORIAL — Tutorial

- `[x]` `P-TUT-001`: Board/solution/region 4×4 đúng source. Bằng chứng: `GuidePuzzle_MatchesDecodedGodotBankEntryExactly` và AppScene runtime dựng board size 4.
- `[x]` `P-TUT-002`: Step 1 chỉ nhận first cat ở allowed cell và cần double-tap cùng ô trong cửa sổ nguồn. Bằng chứng: `PlaceCatSteps_RequireSameCellDoubleTapWithinSourceWindow` và `PlatformTutorial_FullFlowRoutesGameAndReopensCleanly`.
- `[x]` `P-TUT-003`: Step 2 confirm one-per-color đúng ở Current; Check/IQ bỏ confirm riêng. Bằng chứng: fixture Default/Check/IQ và runtime Confirm thật.
- `[x]` `P-TUT-004`: Step 3 chỉ nhận đúng sáu mark hàng/cột. Bằng chứng: `DefaultFlow_UsesAllSevenSourceInteractionsAndFinalConfirm` và runtime flow.
- `[x]` `P-TUT-005`: Step 4 đặt second cat bằng double-tap đúng ô nguồn. Bằng chứng: domain fixture và runtime flow.
- `[x]` `P-TUT-006`: Step 5 chỉ nhận đúng ba neighbor; diagonal variant chỉ đổi presentation contract. `TutorialDiagonalConfig` nay dùng shared AppStart catalog và các fixture tương ứng đều đạt.
- `[x]` `P-TUT-007`: Step 6 đặt third cat bằng double-tap đúng ô nguồn. Bằng chứng: domain fixture và runtime flow.
- `[x]` `P-TUT-008`: Step 7 free play cùng ba pha reveal/apply hint hoàn tất trong sáu lần bấm, gồm đúng blank-row cells + cat mirror. Bằng chứng: `HintFlow_RevealsThenAppliesTwoRowsAndLastCatInSixPresses` và runtime flow.
- `[x]` `P-TUT-009`: Presenter khóa board khi chuyển bước; toàn bộ Graphic dưới Mask không bắt raycast, input ngoài allowed-cell bị từ chối và reopen không tăng Board/Mask CellView. Bằng chứng: `PlatformTutorial_FullFlowRoutesGameAndReopensCleanly`.
- `[x]` `P-TUT-010`: Completion committer lưu `tutorial_done` idempotent, đóng Tutorial sau khi mở Game level 1; Gameplay thật vào `Playing`, reopen Tutorial reset về bước đầu. Bằng chứng: completion fixture và Platform PlayMode.

## P-HOME — Home và điều hướng ngoại vi

- `[x]` `P-HOME-001`: Offline defaults hiển thị Daily Streak, ẩn Profile và dùng hard-button variant 0 đúng source. `HomeConfigSet` thuộc shared `AbConfigRuntime`; fixture default/provider reload đã qua Unity EditMode thật.
- `[x]` `P-HOME-002`: Presenter đọc current level/hard state mỗi lần `OnShow`; AppScene xác nhận level text đổi 1→7, phản ứng `LocaleChanged`, rồi hide/set level 9/reopen cùng instance hiển thị đúng catalog hiện hành.
- `[x]` `P-HOME-003`: `StartBtn` thật đã mở Game ở marker, dựng puzzle 4×4 và ẩn Home cuối animation trong AppScene PlayMode; Game `BackBtn` quit rồi trở về Home.
- `[x]` `P-HOME-004`: Home/Settings presenter, prefab, registry và AppScene composition đã đạt. Platform matrix mở/đóng cùng cached Settings page qua Home→Game, xác nhận toàn bộ toggle, nhánh On/Off, config Language/Pattern/HTP, legal/restart/spacer và panel centering đều reset đúng theo mode.
- `[x]` `P-HOME-005`: Bốn slot nguồn giữ đúng cây và cùng feature availability runtime. AppScene PlayMode xác nhận Profile theo `leaderboard_func`, Daily khóa/mở ở level 21, Streak route thật, Rank ẩn dưới level 11 rồi mở popup/tham gia; không tạo nút/page giả cho entry bị khóa.
- `[x]` `P-HOME-006`: `OnHide` kill transition, abort popup queue và reset exit/page state; EditMode khóa abort/finally, AppScene hide/reopen cùng presenter xác nhận queue không mắc `IsRunning` và không sinh Home trùng.
- `[ ]` `P-HOME-007`: Layout/animation Home khớp reference ở 1080×1920 và 1080×2400.
- `[x]` `P-HOME-008`: Home đọc priority JSON nguồn, filter scene, stable-sort giảm dần và nối `ab_switch_popup`, Rank reward/open cùng rewarded-ad restore theo đúng wait/confirm/profile-guide flow. AppScene đã đạt first-period/reward/reopen; restore Collect cấp đúng tool/quota, Close bỏ batch nhưng không cấp thưởng/quota, ba lần reopen dùng cùng presenter và queue rỗng không bật lại popup.

## P-SETTINGS — Settings, Language và How-to-play

- `[x]` `P-SET-001`: Music/Sound/Vibration/People defaults, persistence và presenter binding đã đạt. Platform matrix xác nhận Music luôn ẩn đúng nguồn, Sound/Vibration/People đổi model + nhánh ToggleOn/Off/icon, giữ qua outgame reopen rồi qua cùng popup game-mode; cảm nhận native được theo dõi riêng ở Audio/Device QA.
- `[~]` `P-SET-002`: Offline outgame layout đã resolve Music ẩn, ba toggle còn lại hiện, Language/Pattern/Restart/HTP ẩn, Feedback/Terms/Version hiện. AppScene PlayMode xác nhận popup route và dropdown non-English; `ISettingsExternalServices` gate offline/online Feedback, CMP visibility/action và localized Terms/Privacy URL đúng boundary. Còn full layout/pixel parity và production SDK adapter.
- `[~]` `P-SET-003`: Game-mode layout đã resolve Restart hiện, Terms/Version/Language ẩn và Pattern/HTP theo config. AppScene PlayMode với `rule_text=setting_entry`, `blind_mod=1` xác nhận Language ẩn, Pattern/HTP hiện; double invoke Restart chỉ tăng `RestartCount` một lần, đóng Settings và tải lại cùng level. Còn device/pixel parity.
- `[~]` `P-SET-004`: Pattern mode dùng đúng 12 sprite/màu nguồn theo color index; callback Settings áp dụng ngay lên board, hide-on-filled đạt PlayMode và hai dismissed field lưu đúng thời điểm. Còn visual red-dot ở Settings entry và `blind_mod=2` device/pixel check.
- `[~]` `P-SET-005`: Toggle cập nhật sprite/panel/toast ngay, Sound bật phát preview và Vibration bật gọi platform boundary; chờ PlayMode nghe/cảm nhận trên thiết bị.
- `[x]` `P-SET-006`: `GenericPopupAnimator` giữ source marker/timing; AppScene PlayMode xác nhận bấm HTP mở Paged page, Settings skip close/ẩn, signal `Closed` phát trước close animation đúng Godot rồi trả lifecycle về Game mà không kẹt Loading.
- `[~]` `P-SET-007`: CSV/catalog, locale persistence, Language popup/dropdown và refresh text Home/Settings đã port. AppScene PlayMode dùng system locale `vi_VN` xác nhận dropdown/outside-close/System apply-persist; cold start với state `apply_locale=vi` và `settings_language` bật đã khởi động thẳng cột `vi`, Home “Màn 1” và entry Language đúng nguồn. Chỉ còn device-font/glyph.
- `[~]` `P-SET-008`: Settings prefab đã sinh theo nhánh chức năng, không missing script và có serialized localization/language bindings; pixel parity outgame/game-mode còn chờ.
- `[x]` `P-SET-009`: Full How-to-play giữ ba board 3×5, matrix/state/frame schedule, tap-anywhere close và demo loop đúng source; AppScene xác nhận demo qua start delay, ba vòng reopen dùng cùng page/cell population và reset sạch.
- `[~]` `P-SET-010`: Paged How-to-play giữ ba page, board scale, Previous/Next/Got it, slide 16 frame, caption/localization highlight và demo loop đúng source; Settings route cùng Previous/Next/Got it đã được bấm trong AppScene PlayMode. Còn VFX/pixel parity và soak reopen dài.
- `[x]` `P-SET-011`: Hai page dùng shared App `SoundRuntime`, bật silence khi show và trả silence/coroutine/tween/cell khi hide; Full ba vòng và Paged reopen page 0 đã đạt AppScene PlayMode, không tăng fixed demo-cell population.

## P-BANK — Bank browser

- `[x]` `P-BANK-001`: `LevelEntry` giữ đủ union schema thật của 25 bank asset (`id/date/label/r1…r5/transform/seq` cùng board/pattern/color fields), Godot Int64 seed và clone không làm bẩn cache; full EditMode 678/678 đã chạy các fixture parse/schema/inventory thật.
- `[x]` `P-BANK-002`: Root browser có đúng sáu nhánh Regular/LK/LK Modified/LK Style/GC/SP và chỉ hiện pool có dữ liệu; prefab/registry hợp lệ, AppScene PlayMode đã materialize rồi mở cả sáu nhánh từ root thật.
- `[x]` `P-BANK-003`: Size/rank và hard-tier keys `7:4, 8:4/5, 9:4/5, 10:4/5, 11:4/5, 12:4` được tách N/H đúng source; fixture exact-key và inventory/tier của asset thật đều nằm trong full EditMode 678/678 đã đạt.
- `[x]` `P-BANK-004`: Launch Regular/LK Style/GC giữ seed + r1…r5 + tier flags; LK/LK Modified giữ id/maxR; SP giữ id/r1…r5/colorMap. Exact-key EditMode đạt và Platform PlayMode 16/16 đã để `GamePage` consume cả sáu pool, giữ đúng pool/index qua Win/Next cùng SP Fail/Restart.
- `[x]` `P-BANK-005`: Initial route priority `go_lk_style → go_lk → go_regular`, panel back stack, selector clamp và row launch khớp nguồn. AppScene PlayMode đã bấm root/Size/Tier/Level row thật cho cả Regular/LK/LK Modified/LK Style/GC/SP; Tier/LK +/- giữ đúng hai cận, launch entry #2 và Back trả đúng panel riêng của từng pool.
- `[~]` `P-BANK-006`: Dynamic size/tier/LK/SP rows được tái sử dụng theo pool và bind release-frame guard khi materialize. AppScene PlayMode stress 8 vòng Regular Root→Size→Tier→Back cùng reopen SP và xác nhận tổng `BankSizeCardView`, `BankTierCardView`, `BankLevelRowView` không tăng; còn profiler/device soak dài.
- `[x]` `P-BANK-007`: `BankPage.prefab` đã được Unity sinh và registry có `UiName.Bank`; structure fixture cùng AppScene PlayMode xác nhận presenter/binding hợp lệ, SP panel/row động materialize và không missing script.
- `[x]` `P-BANK-008`: AppScene PlayMode xác nhận `GamePage` consume launch của cả sáu pool thành `GameplaySessionMode.Bank`; launch đầu hiện `ReturnBankBtn`, Win/Next giữ đúng pool/index và ẩn direct-return. SP Fail/Restart giữ #2/3 mạng/pool metadata; Return Bank reset root.

## P-RESULT — Win, fail và progression

- `[x]` `P-RESULT-001`: Win default + đủ sáu giá trị pass-text V0/V1/V2/V3-G1/G2/G3 và pass-page Control/G1/G2/G4 đã port. AppScene matrix mở/đóng cùng cached presenter qua mọi variant, khóa default/panel/extra-stat state, Size/Time/Score/Combo, Completion/Mistake/Tools, percent highlight, G1/G2 CTA marker, G4 roll và Back consume; pixel/Spine được theo dõi riêng ở nhóm visual.
- `[x]` `P-RESULT-002`: Coordinator settle/Next main và Bank next-launch có guard một lần cùng fixtures. AppScene PlayMode xác nhận Main Win → Streak/Award → level 2 và cả sáu Bank pool Win/Next đều giữ đúng pool/index, tải đúng một lần.
- `[x]` `P-RESULT-003`: Fail presenter đã khóa timeline phong cách nguồn theo overlay/cat/title/remaining/encourage/CTA bằng các group riêng biệt; button mở khóa sau `1.5s`, close dài `0.1s`, cleanup và reopen không giữ tween/state cũ. Bằng chứng: AppScene PlayMode xác nhận timing, interaction gate và vòng đóng/mở lại.
- `[~]` `P-RESULT-004`: Fail restart không settle/advance lần hai và giữ `restart_count`; AppScene PlayMode đã xác nhận 3 wrong → Fail → Restart vẫn level 1/3 mạng rồi Win → level 2. Rewarded revive cũng chịu được vòng Fail thứ hai; còn app-kill PlayMode.
- `[x]` `P-RESULT-005`: Revive khôi phục 1/3 lives theo `revive_life`, resume clock và đóng Fail. AppScene PlayMode xác nhận default hồi đúng 1 mạng sau callback reward và không settle lặp khi ad đóng.
- `[x]` `P-RESULT-006`: Free-once persisted/idempotent và reward revive đi qua boundary. AppScene PlayMode xác nhận null provider ẩn Revive; test provider xác nhận đúng position Main, chỉ `ad_rewarded` mới hồi sinh, còn `ad_closed` không cấp mạng và mở lại nút. Provider test không tồn tại trong runtime production.
- `[~]` `P-RESULT-007`: `win_toast` threshold size 6–12, tier/message pool, BBCode→UGUI rich text, highlight/sprite và nhánh chờ 1,5/1,2 giây đã port; default 0 tắt đúng nguồn. AppScene PlayMode xác nhận P20 hiện ở level 11, đóng trước Win, rồi Next level 12 reload Control trước khi dựng bàn nên không hiện toast và vẫn giữ delay 1,2 giây. Còn pixel/VFX và một matrix ghép toast với toàn chuỗi rank/streak/rate/push.

## P-AUDIO — Audio, vibration và animation timing

- `[x]` `P-AUDIO-001`: Enum 29 Kind/27 mapped path và `SoundCatalog.asset` đủ đúng 27 fixed clip + 39 dynamic clip nguồn; hai Kind không có path giữ no-op.
- `[x]` `P-AUDIO-002`: Fixed pool dùng đúng polyphony nguồn và cắt voice bắt đầu lâu nhất khi đầy; PlayMode khóa MarkX=4, MarkCat=3 và BoardEnter=1.
- `[x]` `P-AUDIO-003`: Sound/People/Silent gate có hiệu lực ngay; Music giữ hard-off đúng nguồn. `App/Systems/Audio` sở hữu một SoundService dùng chung qua `SoundRuntime`; Home/Settings/HTP/Game không còn dùng reference rỗng hoặc pool GamePage cục bộ. Bốn setting đã persist và Vibration cập nhật platform sink ngay.
- `[x]` `P-AUDIO-004`: BGM source contract hard-off; dialog, non-banner ad, banner-ignore và hai duck kind giữ đúng state. Duck tự nhả theo SFX và cleanup khi disable.
- `[x]` `P-AUDIO-005`: Combo/meow voice-by-path dùng shared GameStart/AppStart config, catalog cache và meow delay theo MARK_CAT; mặc định meow=0 vẫn tắt đúng nguồn.
- `[x]` `P-AUDIO-006`: Unsupported/editor vibration là no-op không exception; Android port duration/amplitude low/high-RAM, iOS dùng fallback thô. Cảm nhận native thuộc device QA.
- `[x]` `P-AUDIO-007`: Completion delay chỉ mở rộng một settlement deadline; Platform lifecycle đã khóa Fail/Win đúng một lần, không bị tween/audio callback transition lặp.
- `[~]` `P-AUDIO-008`: Toàn bộ `66` clip serialized decode thành sample hữu hạn, không im lặng; visual primary flow khóa play-count cho `BoardEnter/MarkCat/Wrong/Fail/AllCleared/Win`. Đánh giá chủ quan bằng tai/thiết bị vẫn là `USER QA`.

## P-META — Daily, streak và award

- `[~]` `P-META-001`: Daily unlock/date/countdown, persisted completion state, beat-percent, deterministic bank/8-transform selection, launch contract và Home entry presenter đã port. AppScene xác nhận locked entry không mở page, level 21 mở đúng `DailyGame` mode Daily, Back về Home và entry rollover Done→Normal qua ngày mới; còn visual parity.
- `[~]` `P-META-002`: Daily Fail→rewarded Revive→Fail→Restart→Win đã đạt AppScene PlayMode; restart giữ ngày/index/puzzle, revive chỉ tăng stats `daily`, win lưu date/elapsed/beat một lần và Main level/strategy/retry/snapshot/DDA/stats không đổi. Continue mở page Game thật rồi đóng hai page Daily đúng nguồn. Còn tracker sink-capture và visual parity.
- `[x]` `P-META-003`: `ClockTicker` scene-owned phát tick giây và day-watch độc lập page, reschedule pause/focus, dùng local date key và không catch-up burst. AppScene mô phỏng ngày 10→11 rồi quay lùi: Daily/Streak cùng refresh, Home chỉ advance `max_daily_date` khi show và không cho clock rollback mở lại ngày cũ.
- `[~]` `P-META-004`: Streak check-in, chu kỳ 7, resume/backfill/protect, week slots, pending-win crash recovery và flow Main/Lit/Settle đã port. AppScene đạt ngày 1→7→8, same-day idempotency, broken reset, slot settle delay và chest ngày 7 đúng một lần; còn production rewarded-ad provider cùng visual backfill/resume.
- `[~]` `P-META-005`: Award transaction được ghi vào `in_flight_awards` trước presentation, cold-start sweep cấp lại đúng một lần và trang Collect hoàn tất transaction. AppScene xác nhận Rank Gift group 1 đi đủ hai pha podium/rương → item, cấp Frame +2 Hint +2 Locate đúng một lần; group 3 giữ panel item ẩn, chạy `FrameAddEffect`, chỉ persist +1 frame sau effect rồi xóa in-flight. Còn device app-kill.
- `[x]` `P-META-006`: `CompleteAward`/cold sweep idempotent, double chỉ nhân tool và không nhân frame. AppScene xác nhận Collect gate, click sớm/click dồn, Back consume, đóng cưỡng bức giữa frame effect, callback/effect hủy, reopen pooled presenter và persistence Tool+Frame đều đúng một lần.

## P-PROFILE — Identity, avatar và frame

- `[x]` `P-PROFILE-001`: Profile rỗng tự tạo nickname 6 ký tự, avatar/frame hợp lệ và sở hữu đủ 8 classic frame; initialization chỉ save một lần. Repository ghi đúng envelope nguồn `profile/data`, đọc tương thích flat schema Unity cũ và file-backed restart giữ nguyên identity/inventory.
- `[x]` `P-PROFILE-002`: Nickname trim/giới hạn 12 code point, avatar/frame validation, unlock/equip/count và frame red-dot giữ đúng source. AppScene PlayMode thao tác cell/Button thật đã khóa pending selection, Close bỏ thay đổi, Confirm commit nickname/avatar/frame, locked leaderboard frame chỉ shake/mở tooltip, vào Frame xóa red-dot, frame vừa nhận có thể equip và pooled reopen không nhân cell.
- `[~]` `P-PROFILE-003`: Remote export mã hóa nickname `b64:`, chỉ đồng bộ frame id ≥100 và chỉ merge khi remote ahead; fixture round-trip đạt, backend sync chờ R16.
- `[~]` `P-PROFILE-004`: Award frame đi qua `ProfileRuntime` scene-owned vào ProfileService và persistence thật. AppScene frame-only xác nhận inventory chỉ tăng khi effect kết thúc; file-backed cold-start xác nhận Rank reward dở dang cấp frame đúng một lần qua hai restart liên tiếp. Còn hard-kill thiết bị thật.
- `[x]` `P-PROFILE-005`: `ProfilePage.prefab` đã được migration qua Unity Prefab API và Refresh bridge: Content 900×1253 neo giữa nên bất biến ở 1080×1920/2400, grid 4 cột 185 px/gap 6, title/tab color/offset, Avatar/Frame viewport delta 20/10 px đúng `.tscn`. Tooltip frame khóa hiện GO khi Rank chạy, ẩn trong `from_rank_open_guide`, GO đóng Profile rồi mở Rank theo `open_home_entry`; pooled reopen không nhân cell. Platform EditMode 51/51 và Platform PlayMode 24/24 đạt.

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

## P-PRODUCT — Feedback, Rate Us, Helpshift và external services

- `[x]` `P-PRODUCT-001`: `ProductServiceRuntime` là boundary scene-owned dùng chung cho UIManager, AppBootstrap, Privacy/Settings và GameWin; thiếu provider chỉ rơi về offline/no-op, không log rác và không chặn startup.
- `[~]` `P-PRODUCT-002`: Feedback giữ đúng form source: input trim mới bật Submit, submit track/thanks/close, outside pointer-down nhả focus; presenter/prefab/registry đã có. Unity intentionally không chuyển free-form text hoặc metadata định danh sang network; PlayMode/visual parity trực tiếp còn chờ.
- `[~]` `P-PRODUCT-003`: Rate Us giữ gate `rate_us_pop` (level 8/15, win streak 5), 5-star tap/drag, close/rate result, V2 restyle và nhánh GameWin >4 store-review/≤4 Feedback. Presenter/prefab/registry đã qua composition và policy tests; native review callback/pixel parity chưa cần cho bản offline.
- `[x]` `P-PRODUCT-004`: Helpshift/online/ad/rating/feedback chỉ cần điểm móc provider và luồng fallback để bản thử nghiệm chạy được. SDK thật, account/server, quảng cáo live, metadata upload và network attribution không thuộc acceptance scope hiện tại.

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
- `[~]` `P-PERF-003`: Board local pool giữ ownership/reset hiện tại; regression Setup→Clear→Setup xác nhận cell instance được tái sử dụng và state reset. Singleton `PoolManager` legacy không có code consumer nhưng còn serialized trong `LoadingScene`, chờ khóa build/test scene mới được dọn.
- `[~]` `P-PERF-004`: Core/Gameplay/Editor/EditModeTests compile sạch, targeted regression **109 passed, 0 failed**. Còn Unity Profiler/device GC, soak restart/pool và touch thật.
- `[~]` `P-PERF-005`: Không dùng Addressables và không đổi `Time.timeScale`; sáu cửa sổ có first-use spike (DailyGame, Setting, Profile, Streak, Rank page/how-to) được instantiate dưới Splash. Board Daily được warm theo size hiện tại; các row Rank đang tồn tại được tạo dần 4 row/frame trước khi route Home để animation lần mở đầu không bị nuốt bởi frame spike. Còn nghiệm thu cold-start trên build Windows/Android.

## P-RELEASE — Build và QA readiness

- `[x]` `P-RELEASE-001`: `EditorBuildSettings` có `AppScene` ở vị trí đầu, mọi scene enabled tồn tại trên disk và không bị đăng ký trùng; `AppRuntimeCompositionTests` kiểm tra bằng Unity Editor API.
- `[x]` `P-RELEASE-002`: `AppScene` production không chứa `SceneLoader` hoặc singleton `PoolManager` prototype; board pool hiện thuộc `BoardView`. Unity targeted EditMode đạt 48/48 và PlayMode Platform đạt 6/6 sau gate này.
- `[~]` `P-RELEASE-003`: Device touch/notch/resume, profiler GPU/batch/GC, soak level 1–250, pixel comparison và build signing vẫn là QA ngoài Editor; không cần làm blocker cho bản offline thử nghiệm.

## Nhật ký chạy checklist

| Ngày | Build/commit | Phạm vi | Kết quả | Bằng chứng |
|---|---|---|---|---|
| 2026-08-08 | Workspace hiện tại | Kiểm kê ban đầu | Chưa chạy parity suite | Roadmap + SourceMap |
| 2026-08-08 | Unity 6000.3.19f1 | Pure EditMode suite | 67 passed, 0 failed | `Reports/Codex/R1_EditMode_Test_Report_2026-08-08.md` |
| 2026-08-09 | Unity 6000.3.19f1 | R8 tool resource + idle policy regression | 216 passed, 0 failed | `Reports/Codex/R8_Tool_Resource_Idle_Hint_Test_Report_2026-08-09.md` |
| 2026-08-13 | Unity 6000.3.19f1 | R8 shared gameplay configs + RuleHighlight | 665 EditMode + 12 Platform PlayMode passed, 0 failed | `Reports/Codex/R8_Gameplay_Config_Rule_Highlight_Report_2026-08-13.md` |
| 2026-08-13 | Unity 6000.3.19f1 | R8 Main lifecycle + shared result configs + Hint R4 | 667 EditMode + 13 Platform PlayMode passed, 0 failed | `Reports/Codex/R8_Main_Lifecycle_Closure_Report_2026-08-13.md` |
| 2026-08-13 | Unity 6000.3.19f1 | R8/R9 ToolButton UI + persistence + idle pulse | 29 targeted EditMode + 1 targeted Platform PlayMode passed, 0 failed | `Reports/Codex/R8_R9_ToolButton_UI_Closure_Report_2026-08-13.md` |
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
| 2026-08-12 | Unity 6000.3.19f1 | R10/R16 Product Service UI boundary | Feedback, Rate Us, Rate Us V2, Helpshift preheat/unread và GameWin policy đã nối; offline/no-op provider, không upload metadata/text; targeted EditMode 22/22, PlayMode Platform 3/3 | `Reports/Codex/R10_Product_Service_UI_Report_2026-08-12.md` |
| 2026-08-12 | Unity 6000.3.19f1 | R17 release-readiness + save/startup/lifecycle/Back gate | Build/scene/pool/migration, corruption/atomic verify, corrupt-startup, focus-resume và Home Back→Confirm đạt; EditMode 48/48, PlayMode Platform 6/6 | `Reports/Codex/R17_Release_Readiness_2026-08-12.md` |
| 2026-08-12 | Unity 6000.3.19f1 | R4/R7 Main level selection/bank/recent parity | Static/AB size, Special/SP57, progress/fallback, transform, variant banks và recent-puzzle one-retry fallback đạt; full EditMode 617/617, Platform PlayMode 8/8 | `Reports/Codex/R7_Level_Selection_Parity_2026-08-12.md` |
| 2026-08-12 | Unity 6000.3.19f1 | R7 color map/region palette parity | Default/seed/RGB/Lab/pattern, 13 palette và shared AppStart config tới Main/Tutorial Board đạt; full EditMode 634/634, Platform PlayMode 10/10 | `Reports/Codex/R7_Level_Selection_Parity_2026-08-12.md` |
| 2026-08-13 | Unity 6000.3.19f1 | R9 Audio/vibration contract closure | 27 fixed + 39 dynamic clip, pool/settings/BGM duck, shared combo/meow và vibration no-op/Android adapter đạt; targeted EditMode 27/27, PlayMode 1/1 | `Reports/Codex/R9_Audio_Vibration_Closure_Report_2026-08-13.md` |
| 2026-08-13 | Unity 6000.3.19f1 | R11 Tutorial runtime closure | P-TUT-001..010, shared input configs, full 7-step/hint route, mask/input lock, completion và reopen pool đạt; full EditMode 677/677, Platform PlayMode 15/15 | `Reports/Codex/R11_Tutorial_Runtime_Closure_Report_2026-08-13.md` |
| 2026-08-13 | Unity 6000.3.19f1 | R12 Home/How-to-play + global audio lifecycle | Home level/locale/reopen, HTP demo/silence/cell cleanup và một App-scoped SoundRuntime đạt; full EditMode 678/678, Platform PlayMode 16/16 | `Reports/Codex/R12_Home_HowToPlay_Audio_Runtime_Closure_2026-08-13.md` |
| 2026-08-13 | Unity 6000.3.19f1 | R12 Bank evidence reconciliation | P-BANK-001…004 được đóng từ schema/inventory/exact-key EditMode và AppScene six-pool consumer đã đạt trong full EditMode 678/678, Platform PlayMode 16/16; không chạy lại test ổn định | `Reports/Codex/R13_Bank_Pool_Matrix_Report_2026-08-11.md` |
| 2026-08-13 | Unity 6000.3.19f1 | R12 Home popup/reward restore closure | P-HOME-008 đạt collect/close/quota/pending/reopen/no-duplicate; test phát hiện và sửa CloseButton không reset sau Collect; Platform PlayMode 17/17 | `Reports/Codex/R12_Home_Popup_Reward_Restore_Closure_2026-08-13.md` |
| 2026-08-13 | Unity 6000.3.19f1 | R12 locale cold-start runtime | State `apply_locale=vi` + enabled feature được inject trước AppBootstrap.Start; catalog/Home/Language entry cùng dùng tiếng Việt ngay lần mở đầu; Platform PlayMode 18/18 | `Reports/Codex/R12_Locale_Cold_Start_Report_2026-08-13.md` |
| 2026-08-13 | Unity 6000.3.19f1 | R13 Win Toast runtime lifecycle | P20 level 11 hiện/ẩn và delay 1,5 giây; Next level 12 reload Control trước puzzle selection và giữ delay 1,2 giây; BBCode nguồn được chuyển sạch, PreCat không regression; full EditMode 679/679, Platform PlayMode 19/19 | `Reports/Codex/R13_Win_Toast_Runtime_Closure_2026-08-13.md` |
| 2026-08-13 | Unity 6000.3.19f1 | R15 Profile interaction closure | Pending/Close/Confirm, nickname, avatar/frame tabs, locked tooltip, red-dot, unlock/equip và pooled reopen đạt bằng service bộ nhớ không chạm profile thật; Platform PlayMode 20/20, full EditMode gần nhất 679/679 | `Reports/Codex/R15_Profile_Interaction_Closure_2026-08-13.md` |
| 2026-08-13 | Unity 6000.3.19f1 | R14 Award interaction/idempotency closure | Collect gate, fast click, Back consume, forced hide giữa frame effect, callback cancellation, exact-once persistence và pooled reopen đạt; Platform PlayMode 21/21, full EditMode gần nhất 679/679 | `Reports/Codex/R14_Award_Interaction_Idempotency_Closure_2026-08-13.md` |
| 2026-08-13 | Unity 6000.3.19f1 | R13 Win pass-page/pass-text runtime closure | Control/G1/G2/G4 cùng V0/V1/V2/V3-G1/G2/G3, stats/layout/highlight/CTA marker/G4 roll/Back và cached reopen đạt; Platform PlayMode 22/22, full EditMode gần nhất 679/679 | `Reports/Codex/R13_Win_Pass_Page_Text_Runtime_Closure_2026-08-13.md` |
| 2026-08-13 | Unity 6000.3.19f1 | R12 Settings toggle/mode-layout closure | Music hidden, Sound/Vibration/People On/Off persistence, Home↔Game cached layout, Pattern dot/state/board refresh và hierarchy reset đạt; Platform PlayMode 23/23, full EditMode gần nhất 679/679 | `Reports/Codex/R12_Settings_Toggle_Mode_Layout_Closure_2026-08-13.md` |
| 2026-08-13 | Unity 6000.3.19f1 | R15 Profile layout + locked-frame Rank closure | Prefab migration/Refresh bridge, source title-tab-grid geometry, centered 900×1253 aspect contract, Avatar/Frame viewport, GO/from-rank guide và Rank route đạt; Platform EditMode 51/51, Platform PlayMode 24/24 | `Reports/Codex/R15_Profile_Rank_Layout_Closure_2026-08-13.md` |
| 2026-08-13 | Unity 6000.3.19f1 | F1 portfolio primary visual pass | Splash/Home/Tutorial/Game/Fail/Win; Game và Win xác minh thêm 1080×2400; Win sorting/title band và UI sorting activation đạt; Platform EditMode 60/60, Visual PlayMode 1/1 | `Reports/Codex/R17_Portfolio_Visual_Pass_2026-08-13.md` |
| 2026-08-13 | Unity 6000.3.19f1 | F2 CAT star/glow burst + feedback sorting | Timing nguồn 0.1164/0.5/1.02; sáu view pooled; score bubble trên RuleBar; cleanup đạt; EditMode 61/61, Platform PlayMode 25/25, Visual 1/1 | `Reports/Codex/R17_Gameplay_Cat_Burst_2026-08-13.md` |
| 2026-08-14 | Unity 6000.3.19f1 | F2 board enter + gameplay presentation lifecycle | Diagonal/curve source, shared score_encourage, bubble/multiplier/flight, RuleBar/ToolButton, completion/Win overlay và pool-return đạt; Platform EditMode 70/70, Platform PlayMode 27/27, Visual 1/1 | `Reports/Codex/R17_Gameplay_Presentation_Closure_2026-08-14.md` |
| 2026-08-14 | Unity 6000.3.19f1 | F2 Fail/Win result VFX + objective audio QA | Timing Fail/Win bám nguồn, Win confetti dùng pool, RateUs không còn missing script, DOTween capacity 512/128; Platform EditMode 88/88, Platform PlayMode 27/27, Visual 1/1 | `Reports/Codex/R17_Gameplay_Presentation_Closure_2026-08-14.md` |
