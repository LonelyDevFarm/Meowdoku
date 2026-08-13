# Source Map — Godot sang Unity

> Cập nhật: 2026-08-10  
> Nguồn: `D:\Projects\_GameExtract\Main_Meokdoku`  
> Đích: `D:\Projects\Meowdoku`

Tài liệu này là bảng truy xuất bắt buộc khi port. Một dòng chỉ chuyển sang **Hoàn thành** khi API/hành vi cần thiết đã đủ và có parity test hoặc checklist đối chiếu.

## Ký hiệu

- **Port**: có thể dịch logic gần 1:1.
- **Adapter**: phải thay API Godot bằng API Unity nhưng giữ contract/hành vi.
- **Rebuild**: scene/view phải dựng lại bằng Unity UI/Animator/Spine từ tài nguyên và reference gốc.
- Trạng thái: `Chưa có`, `Một phần`, `Hoàn thành`, `Tạm`.

## A. Bootstrap và UI core

| Godot | Vai trò nguồn | Unity đích dự kiến | Kiểu | Trạng thái |
|---|---|---|---|---|
| `project.godot` | Autoload, main scene, viewport, renderer | Project Settings + App Bootstrap | Adapter | Một phần |
| `scripts/module/ui/panel/launcher.gd` | Thứ tự khởi động, splash, prewarm, route đầu tiên | `AppBootstrap.cs` | Adapter | Chưa có |
| `scripts/module/ui/ui_manager.gd` | Registry, cache, stack, layer, mask, back, input guard | `UIManager.cs` | Adapter | Chưa có |
| `scripts/module/ui/ui_registry.gd` | Tên page → scene/loader | `UIRegistry.cs` | Port/Adapter | Chưa có |
| `scripts/module/ui/ui_name.gd` | ID của page | `UIName.cs` | Port | Chưa có |
| `scripts/module/ui/ui_layer_config.gd` | Layer và thứ tự UI | `UILayerConfig.cs` | Port | Chưa có |
| `scripts/module/ui/base/ui_base_window.gd` | Lifecycle window và helper chung | `UIBaseWindow.cs` | Adapter | Chưa có |
| `scripts/module/ui/base/ui_frame_window.gd` | Page toàn màn hình | `UIFrameWindow.cs` | Adapter | Chưa có |
| `scripts/module/ui/base/ui_child_window.gd` | Popup/child window | `UIChildWindow.cs` | Adapter | Chưa có |
| `scripts/module/ui/queue/ui_popup_entry.gd` | Dữ liệu popup queue | `UIPopupEntry.cs` | Port | `UIPopupStartupTests`, AppScene Home queue |
| `scripts/module/ui/queue/ui_popup_queue.gd` | Hàng đợi/priority popup | `UIPopupQueue.cs` | Port | Priority/stable/insert/cancel EditMode và Home abort/reopen PlayMode đạt |
| `scripts/module/ui/clock_ticker.gd` | Tick thời gian/ngày | `ClockTicker.cs` | Adapter | Chưa có |
| `Assets/_Project/Scripts/Core/SceneLoader.cs` | Loader Unity do dự án tạo | Có thể nhập vào UIManager | Tạm | Một phần |

## B. Config, state và persistence

| Godot | Vai trò nguồn | Unity đích dự kiến | Kiểu | Trạng thái |
|---|---|---|---|---|
| `scripts/module/abtest/config/ab_config_base.gd` | Contract config + default/override | `Core/Config/AbConfigBase.cs` | Port | Một phần: typed default/reload/peek/debug override; switch-history chưa có |
| `scripts/module/abtest/abtest_manager.gd` | Registry, timing, remote/default | `Core/Config/DefaultConfigProfile.cs` | Adapter | Một phần: catalog/default P0; timing runtime/remote chưa có |
| `scripts/module/abtest/config/*.gd` | Khoảng 89 config cụ thể | `Core/Config/*.cs` | Port | Một phần: 24 metadata P0; 4 policy input/layout và `score_encourage` đã port |
| `scripts/module/game_state/game_state.gd` | State/progress/settings/reward/snapshot | `Core/GameStateData.cs`, `Core/GameStateService.cs`, repository | Port/Adapter | Một phần: P0 schema, progress, settings, tools, prop-highlight, runtime dirty/DDA, retry/pre-cat/endgame APIs đã có; aggregate win/fail và migration còn thiếu |
| `scripts/module/game_state/save_store.gd` | Encrypted atomic dual-slot save | `Core/SaveStore.cs`, `Core/GameStateRepository.cs` | Adapter | Một phần: dual-slot/failure matrix/legacy migration pass; endgame runtime mã hóa/verify/fsync tuần tự trên worker và flush ở lifecycle; format Unity riêng, app-kill thực địa chưa test |
| `scripts/module/session/session_manager.gd` | ID và vòng đời session | `Services/SessionManager.cs` | Adapter | Chưa có |
| `scripts/module/language/language_manager.gd` | Locale startup, fallback và text refresh | `LocalizationCatalog`, `LocalizationLocaleContract`, `AppBootstrap` | Adapter | Parser/alias/persist/cold-start AppScene đạt; còn device glyph |
| `PlayerPrefs` trong `LevelData.cs` | Tiến độ bank tạm | Đã chuyển vào `GameStateRuntime` | Adapter | Đã loại bỏ |

Nhóm config P0 phải port trước: `region_color`, `size_cycle`, `rule_highlight`, `goal_emphasis`, `auto_complete`, `error_feedback`, `swipe_protect`, `dda_rank`, `revive_life`, `life_icon`, `single_region_num`, `board_size_big`, `score_encourage`, `pre_cat`, `game_grid_ui`, `hint_cat`, `doubletap_protect`, `vibrate_combo`, `combo_text`, `combo_voice`, `undo_btn`, `game_auto_mark`, `game_life_rule`, `wrong_cat_effect`.

## C. Level và bank data

| Godot | Unity hiện tại/đích | Trạng thái | Phần còn thiếu chính |
|---|---|---|---|
| `scripts/module/bank/model/level_bank_io.gd` | `Core/LevelBankIO.cs` | Một phần | Parity lỗi file/JSON, test dữ liệu hỏng |
| `scripts/module/bank/model/bank_data.gd` | `Core/BankData.cs` | Một phần | LK Modified, LK Style, GC, SP contract đầy đủ |
| `scripts/module/gameplay/model/level_data.gd` | `Core/LevelData.cs` | Một phần | `get_next_entry_main`, filter, progress, transforms đầy đủ, prefill, puzzle ID, DDA |
| `scripts/module/gameplay/core/level_generator.gd` | `Core/LevelGenerator.cs` | Một phần | RGB/Lab/pattern/seed và palette config |
| `scripts/module/gameplay/core/pre_cat_decider.gd` | `Core/PreCatDecider.cs` | Hoàn thành logic | Scenario order, pre-type, rank >= 3, half/always decision; runtime GameSession consumer còn thiếu |
| Bank mã hóa trong `assets/bankData` | `Resources/Levels` | Một phần | Audit tất cả nhánh bank và import catalog |
| `scripts/module/bank/view/bank_page.gd` | `Bank/BankPage.cs` | Chưa có | Model/view/launch params/progress |

## D. Gameplay domain

| Godot | Unity hiện tại/đích | Trạng thái | Ghi chú |
|---|---|---|---|
| `gameplay/model/cell_state.gd` | `Core/CellState.cs` | Hoàn thành logic cơ bản | Giữ enum value tương đương |
| `gameplay/model/game_score_model.gd` | `Core/GameScoreModel.cs`, `Core/Config/ScoreEncourageConfig.cs` | Hoàn thành logic | Model/restore, legacy gain, 8 variant, multiplier, skill, deduction và life bonus đã test/nối; UI fly feedback ở R9 |
| `gameplay/core/queendoku_core.gd` | `Core/QueendokuCore.cs` | Hoàn thành logic | Conflict priority, completion và board-state consumer đã có fixture |
| `gameplay/core/hint_engine.gd` | `Core/HintEngine.cs` | Hoàn thành logic | R1 mark/single, R2, R3/R4 subset k<=6, chain contradiction, rank và R4+ |
| `game/view/step_history.gd` | `Gameplay/Input/StepHistory.cs` | Hoàn thành logic | Đủ API, metadata và định dạng serialize/deserialize nguồn |
| `game/view/hint_mutex.gd` | `Gameplay/HintMutex.cs` | Hoàn thành logic | Port không log; Godot khai báo nhưng không gọi, Unity dùng làm adapter tương đương overlay input guard |
| `game/level_ops.gd` | `Gameplay/LevelOps.cs` | Chưa có | End/start level operations |

## E. Input

| Godot | Unity hiện tại/đích | Trạng thái | Loại |
|---|---|---|---|
| `game/input/cell_action.gd` | `Gameplay/Input/CellAction.cs` | Một phần | Port |
| `game/input/board_stroke_context.gd` | `Gameplay/Input/BoardStrokeContext.cs` | Một phần | Port |
| `game/input/board_input_scheme.gd` | `Gameplay/Input/BoardInputOperations.cs` | Một phần | Port |
| `game/input/board_gesture_recognizer.gd` | `Gameplay/Input/BoardGestureRecognizer.cs` | Hoàn thành logic | Tap đầu tức thời, double-window, target pending, interpolation và reset lifecycle đã khóa bằng fixture |
| `game/input/swipe_guard_recognizer.gd` | `Gameplay/Input/SwipeGuardRecognizer.cs` | Một phần | Port/nối runtime; PlayMode touch chưa test |
| `gameplay/core/swipe_axis_guard.gd` | `Gameplay/Input/SwipeAxisGuard.cs` | Một phần | Logic/test/config runtime đã nối; touch thực địa còn thiếu |
| `gameplay/core/swipe_velocity_gate.gd` | `Gameplay/Input/SwipeVelocityGate.cs` | Một phần | Logic/test/dynamic runtime đã nối; framerate parity còn thiếu |
| Godot BoardView `_gui_input` + `_input`/drag signals, CellView `MOUSE_FILTER_IGNORE` | `BoardView` board-level pointer handlers + non-raycast `CellView` | Một phần | Unity adapter bắt buộc; desktop raw event + top UI raycast tránh EventSystem dispatch trễ, touch vẫn dùng latch; PlayMode/touch còn phải xác nhận |
| `doubletap_protect`/`swipe_protect` configs | `Core/Config/InputAndLayoutConfigs.cs`, `AbConfigRuntime.Input` | Hoàn thành logic | Shared AppStart/GameStart catalog đi tới Main/Tutorial; default và remote variants dùng cùng instance |

## F. Board, cell và game page

| Godot | Unity hiện tại/đích | Trạng thái | Kiểu |
|---|---|---|---|
| `gameplay/view/cell_view.gd` + `assets/prefab/cell.tscn` | `CellView.cs` + `Cell.prefab` | Một phần | State visibility/immutability, error, pattern, pool reset và SDF corner đã port/test; sprite/VFX/animation variant còn mở |
| `gameplay/view/board_view.gd` | `BoardView.cs`, `SourceBoardLayout.cs` | Một phần | Size 4–10, resize row-major, intrinsic/layout, CAT guard, fixed width 1008 và board-size-big đã port/test; device pixel parity còn mở |
| `abtest/config/region_color_config.gd`, `game_grid_ui_config.gd`, `board_size_big_config.gd` | `BoardConfigSet`, `RegionColorPipeline`, `SourceBoardLayout` | Đã port | Shared AppStart/GameStart catalog đi cùng instance tới Main/Tutorial/Board; runtime fixture đã qua AppScene PlayMode |
| `gameplay/view/board_grid_overlay.gd` | `BoardGridOverlayGraphic.cs` | Đã port theo phạm vi portfolio | Thin grid, region-neighbor thick boundary, rounded outer frame và intro timing đã port; topology/corners có fixture và board 1080×1920 đã được duyệt trực quan. Metric pixel tuyệt đối không dùng khi source extract thiếu runtime render đáng tin cậy |
| `game/view/base_game_page.gd` | `Gameplay/GameSession.cs`, `ToolResourceCoordinator.cs` + controllers/views | Một phần | State machine, lives/mistake, score, board, history, wrong/win/fail/revive, Clear/Locate/Hint/AutoComplete, resource consume, ToolButton UI và idle-hint đã nối; result/pixel VFX còn mở |
| `game/view/game_page.gd` | `Gameplay/GameSessionSnapshot.cs` + `GameplayManager.cs` | Một phần | Snapshot v2 validation/restore, retry cache, PreCat consumer và Unity focus/pause persistence scheduler đã nối; page/result UI còn thiếu |
| `abtest/config/rule_highlight_config.gd` + `game_page.gd::_on_rule_violated` | `GameplayConfigSet`, `GameplayManager.ShouldHighlightRuleViolation`, `GameplayRuleBarPresenter` | Đã port | Đủ control/level 1–5 sau Tutorial/all-level; shared GameStart instance, normal-only presenter gate và RuleBar PlayMode đã khóa |
| Gameplay configs đăng ký trong `abtest_manager.gd` | `AbConfigRuntime.Gameplay` / `GameplayConfigSet` | Một phần | Shared catalog đã nối `daily_first_level_difficulty`, `dda_rank`, `reward_unlock_level`, `prop_highlight`, `mark_sound`, `rule_highlight`; các config gameplay nguồn khác tiếp tục audit theo từng P-GAME/R9 item |
| `abtest_manager.gd` result registrations + `game_page/game_fail_page/game_win_page` | `AbConfigRuntime.Result` / `ResultConfigSet` + Win/Fail/Toast presenters | Đã port config flow | Shared `fail_text/pass_text/revive_free_logic/revive_life/win_toast/pass_page`; GameStart reload tại pre-load boundary cho entry/Next/Restart, GameEnd đi tới presenter thật; Win Toast P20→Control đã đạt AppScene, pixel/VFX vẫn thuộc R13 |
| `game/ui/game_page.tscn`, `game/ui/compont/header_adapt_holder.tscn` + `ui_manager.gd::_apply_safe_area` | `SourceGameplayPageLayout.cs`, `GameplayPageLayoutPresenter.cs`, `GameplayHudPresenter.cs` + `GamePage.prefab` | Một phần | Đã port viewport 1080×2400 keep-width, HeaderAdapt 1920–2400/collapse khi có top inset, VBox Header/CatHeart/RuleBar/Board/BottomTools và safe top/bottom. GamePage runtime đã qua matrix aspect 1920/2160/2400 cùng notch 96/54 và 120/80; ad/toast/pixel/device parity còn mở |
| `game/view/life_slot.gd` | `LifeSlotView.cs` | Chưa có | Rebuild |
| `game/view/tool_button.gd` | `ToolResourceCoordinator.cs`, `GameplayToolBarPresenter.cs`, `ToolButtonView.cs` | Đã port | NO_TOOL/HAS_TOOL/FREE, badge count/99+/plus, press scale, obtain hook, `tool_loop`/`RESET`, click, GameStart refresh và transition persistence đã qua AppScene fixture; reward provider thật ngoài phạm vi |
| `game/view/combo_feedback_view.gd` | `GameplayFeedbackPresenter.cs`, `GameplayHudPresenter.cs` | Một phần | Score/combo/Level-Score visibility và progress feedback đã port; hard-tag/variant còn mở |
| `game/view/hint_overlay.gd` | `HintOverlay.cs` | Chưa có | Rebuild |
| `game/view/rule_info_bar_v4/v7.gd` | Rule bar views | Chưa có | Rebuild |
| `game/view/game_*_toast.gd` + scenes | Start/hard/win toast views | Chưa có | Rebuild |
| `GameplayManager.cs` | Unity view/input coordinator dùng `GameSession` | Một phần | Không còn sở hữu board/score/history/lives; còn level entry, timer adapter và BoardView wiring |
| `PoolManager.cs` | Object pool Unity | Một phần | Adapter; cần reset/reactivate |

## G. Audio và feedback

| Godot | Unity đích dự kiến | Trạng thái |
|---|---|---|
| `scripts/module/sound/sound_manager.gd` | `Services/SoundContract.cs`, `SoundCatalog.cs`, `SoundService.cs` | Hoàn thành logic/call site; device auditory parity còn chờ |
| `scripts/module/common/vibrate_manager.gd` | `Core/VibrationService.cs` | Hoàn thành contract/Android/no-op; iOS fallback thô, device QA còn chờ |
| `game/view/level_flow_*.gd` + scenes | `GameplayFeedbackPresenter` + pooled score/multiplier/skill/flight views | Một phần; logic/timing P0 đã port, visual/video parity còn chờ |
| `game/ui/compont/game_like_hand.tscn` | Spine feedback view/pool | Chưa có |
| Audio assets + `sound_manager.gd` autoload | `Settings/SoundCatalog.asset`, `App/Systems/Audio/{SoundRuntime,SoundService,Bgm}` | 27 fixed + 39 dynamic clip; một scene-owned service bind Home/Settings/HTP/Game, GamePage không nhúng pool riêng; EditMode composition và AppScene PlayMode đạt |

## H. Page và feature P0/P1

| Feature | Godot nguồn chính | Unity đích | Trạng thái |
|---|---|---|---|
| Splash | `splash/view/splash_page.gd`, scene tương ứng | `SplashPagePresenter`, `Prefabs/UI/SplashPage.prefab` | Presenter/prefab/registry và startup route đã port; chờ PlayMode parity |
| Tutorial | `tutorial/view/tutorial_page.gd`, `tutorial_page.tscn` | `TutorialPuzzle`, `TutorialStateMachine`, `TutorialPagePresenter`, `Prefabs/UI/TutorialPage.prefab`, `AbConfigRuntime.Input` | P-TUT-001..010 và AppScene full flow đã đạt; shared diagonal/feedback/double-tap config, mask/input lock, completion route và reopen pool đã khóa. Spine hand/IQ particle cùng pixel/device parity còn adapter |
| Home | `home/view/home_page.gd`, `home_page.tscn`; `daily_challenge_entry_cell.gd/.tscn`; Home configs; `fx_uv_scroll.gdshader` | `HomePageContract`, `HomePagePresenter`, `DailyChallengeEntryPresenter`, `UIHomeFlow.shader`, `HomePagePrefabInstaller` | Level/locale live refresh, hide/reopen cleanup, priority queue, AB/Rank/reward-restore và Daily/Streak/Rank/Profile routes đã đạt AppScene; pixel/animation parity còn mở |
| Settings | `setting/view/setting_page.gd`, `setting_page.tscn`; `settings_language_config.gd`, `blind_mod_config.gd` | `SettingsPageContract`, `SettingsPagePresenter`, `SettingsToggleView`, `Prefabs/UI/SettingsPage.prefab`, pattern fields trong `GameStateData/Service` | Full toggle/persistence và cached Home↔Game mode layout/Pattern refresh đã đạt Platform PlayMode; còn pixel/native feeling và SDK boundary ngoài phạm vi |
| Language | `language_manager.gd`, language page/option, `translations.csv` | `LocalizationCatalog`, `LocalizationLocaleContract`, `LanguagePagePresenter`, `Prefabs/UI/LanguagePage.prefab` | CSV/parser/alias/fallback/persist, popup/dropdown và enabled-feature cold start đã đạt AppScene; chỉ còn device-font/glyph |
| How to play | Hai script/scene page | `HowToPlayPagePresenter`, `HowToPlayPagedPagePresenter`, hai prefab registry, shared `SoundRuntime` | Matrix/frame/timing/navigation, Full/Paged demo, silence/cleanup và reopen đã đạt AppScene; VFX/pixel/device soak còn mở |
| Bank | Bank browser page/panels và level entry handlers | `BankBrowserPagePresenter`, `BankBrowserContract`, `Prefabs/UI/BankPage.prefab` | Browser/launch/return route đã port; chờ PlayMode pool/Next parity |
| Win | `result/view/game_win_page.gd`, `pass_page_g1/g2_board.gd`, pass-text V0–V3, win-toast scripts/scenes | `GameWinPagePresenter`, `GameplayWinToastPresenter`, `Prefabs/UI/WinPage.prefab`, `Overlays/WinToast` | Control/G1/G2/G4 + đủ sáu pass-text value, stats/layout/highlight/CTA/G4 roll/cached reopen và Win Toast lifecycle đã đạt AppScene; còn matrix meta kết hợp và pixel/VFX parity |
| Fail | `result/view/game_fail_page.gd`, fail-text/revive configs | `GameFailPagePresenter`, `Prefabs/UI/FailPage.prefab`, `IRewardedReviveService` | Fail/restart/revive/free/reward boundary đã port; chờ PlayMode/Spine/ad adapter |
| Ad reward restored | `result/view/ad_reward_restored_page.gd/.tscn`, `home_page.gd::_show_ad_reward_restored` | `RewardRestoreService`, `AdRewardRestoredPagePresenter`, `Prefabs/UI/AdRewardRestoredPage.prefab`, Home queue | Collect/close/quota/pending/reopen/no-duplicate đạt Platform PlayMode; production SDK ngoài phạm vi |
| Daily | `daily_entry_state.gd`, `daily_stats.gd`, `daily_game_page.gd`, `daily_win_page.gd`, `daily_fail_page.gd` | `Core/Daily/*`, `DailyChallengeEntryPresenter`, `DailyGameTransitionCoordinator`, shared `GameplayManager/GameSession`, Daily branches trong result presenters | Entry/launch/gameplay/fail/revive/restart/win progress đã nối và compile; chờ Refresh, Daily header/result visual, tracker/Streak và PlayMode |
| Daily Streak | `feature_daily_streak.gd`, data, pages/cells | Streak module | Chưa có |
| Award | `award_manager.gd`, award render/model và `award_page.gd/.tscn` | `AwardManager`, `DailyMetaRuntime`, `AwardPagePresenter`, `AwardItemView`, `FrameAwardEffectView`, `Prefabs/UI/AwardPage.prefab` | Direct/Streak/Rank, durable in-flight/cold sweep, tool-only double, frame effect, fast click/Back/forced hide/reopen và exact-once persistence đạt AppScene; device hard-kill còn ở `P-META-005` |
| Profile | `profile_service.gd`, `profile_page.gd`, `avatar_profile_cell.gd`, profile model/catalog | `ProfileService`, `ProfileRuntime`, `ProfilePagePresenter`, `ProfileAvatarView`, `ProfileSelectionCell`, `Prefabs/UI/ProfilePage.prefab` | Domain/persistence/Award sink, full pending/Close/Confirm/locked/red-dot/equip/reopen, source geometry 900×1253/grid/tab viewport và locked-frame GO→Rank/from-guide gate đã đạt EditMode/AppScene. IME/touch thiết bị thật là QA thủ công, không còn blocker portfolio |

## I. Feature P2 và dịch vụ ngoài

Các nhóm sau chỉ triển khai sau vòng offline P0/P1, nhưng không được xóa khỏi kiến trúc: Profile, Robot, Rank Activity, Tracker, Ads/UniKit, Auth, Data Sync, Push, Privacy/ATT, Feedback, Rate Us, Helpshift và remote A/B.

Trong giai đoạn offline, mỗi dịch vụ ngoài phải có interface và implementation no-op/mock rõ ràng; gameplay không được gọi SDK trực tiếp.

## Quy tắc cập nhật Source Map

Khi thêm một class Unity:

1. Ghi file Godot nguồn ngay trong XML summary hoặc tài liệu module.
2. Cập nhật dòng tương ứng ở đây.
3. Nếu không có file nguồn trực tiếp, đăng ký trong “Sổ chuyển thể” của `PORTING_ROADMAP.md`.
4. Chỉ ghi `Hoàn thành` sau khi test/checklist liên quan trong `ParityChecklist.md` đạt.
