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
| `scripts/module/ui/queue/ui_popup_entry.gd` | Dữ liệu popup queue | `UIPopupEntry.cs` | Port | Chưa có |
| `scripts/module/ui/queue/ui_popup_queue.gd` | Hàng đợi/priority popup | `UIPopupQueue.cs` | Port | Chưa có |
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
| `scripts/module/language/language_manager.gd` | Locale và text refresh | `Localization/LanguageManager.cs` | Adapter | Chưa có |
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
| `game/input/board_gesture_recognizer.gd` | `Gameplay/Input/BoardGestureRecognizer.cs` | Một phần | Port + configurable Unity time adapter; tap đầu trả ngay như nguồn |
| `game/input/swipe_guard_recognizer.gd` | `Gameplay/Input/SwipeGuardRecognizer.cs` | Một phần | Port/nối runtime; PlayMode touch chưa test |
| `gameplay/core/swipe_axis_guard.gd` | `Gameplay/Input/SwipeAxisGuard.cs` | Một phần | Logic/test/config runtime đã nối; touch thực địa còn thiếu |
| `gameplay/core/swipe_velocity_gate.gd` | `Gameplay/Input/SwipeVelocityGate.cs` | Một phần | Logic/test/dynamic runtime đã nối; framerate parity còn thiếu |
| Godot BoardView `_gui_input` + `_input`/drag signals, CellView `MOUSE_FILTER_IGNORE` | `BoardView` board-level pointer handlers + non-raycast `CellView` | Một phần | Unity adapter bắt buộc; desktop raw event + top UI raycast tránh EventSystem dispatch trễ, touch vẫn dùng latch; PlayMode/touch còn phải xác nhận |
| `doubletap_protect`/`swipe_protect` configs | `Core/Config/InputAndLayoutConfigs.cs` | Một phần | Policy và recognizer runtime đã nối; default path đang dùng |

## F. Board, cell và game page

| Godot | Unity hiện tại/đích | Trạng thái | Kiểu |
|---|---|---|---|
| `gameplay/view/cell_view.gd` + `assets/prefab/cell.tscn` | `CellView.cs` + `Cell.prefab` | Tạm/Một phần | Rebuild |
| `gameplay/view/board_view.gd` | `BoardView.cs`, `SourceBoardLayout.cs` | Một phần | Đã port intrinsic size, padding/gap/slot và fixed visible width 1008; border/corner/overlay/animation còn mở |
| `gameplay/view/board_grid_overlay.gd` | Board overlay/mesh | Chưa có | Rebuild |
| `game/view/base_game_page.gd` | `Gameplay/GameSession.cs`, `ToolResourceCoordinator.cs` + controllers/views | Một phần | State machine, lives/mistake, score, board, history, wrong/win/fail/revive, Clear/Locate/Hint/AutoComplete, resource consume và idle-hint policy đã tách; result/tool UI còn thiếu |
| `game/view/game_page.gd` | `Gameplay/GameSessionSnapshot.cs` + `GameplayManager.cs` | Một phần | Snapshot v2 validation/restore, retry cache, PreCat consumer và Unity focus/pause persistence scheduler đã nối; page/result UI còn thiếu |
| `game/ui/game_page.tscn` | `GameplayPageLayoutPresenter.cs`, `GameplayHudPresenter.cs` + Gameplay scene | Một phần | Đã port VBox vị trí Header/CatHeart/RuleBar/Board và HUD Level/Score/progress; tools/ad/toast còn mở |
| `game/view/life_slot.gd` | `LifeSlotView.cs` | Chưa có | Rebuild |
| `game/view/tool_button.gd` | `ToolResourceCoordinator.cs` + `ToolButtonView.cs` | Logic một phần/UI chưa có | Resource/free/reward decision và idle timer đã port; badge, animation `tool_loop`/`RESET`, click/obtain cần rebuild ở R9 |
| `game/view/combo_feedback_view.gd` | `GameplayFeedbackPresenter.cs`, `GameplayHudPresenter.cs` | Một phần | Score/combo/Level-Score visibility và progress feedback đã port; hard-tag/variant còn mở |
| `game/view/hint_overlay.gd` | `HintOverlay.cs` | Chưa có | Rebuild |
| `game/view/rule_info_bar_v4/v7.gd` | Rule bar views | Chưa có | Rebuild |
| `game/view/game_*_toast.gd` + scenes | Start/hard/win toast views | Chưa có | Rebuild |
| `GameplayManager.cs` | Unity view/input coordinator dùng `GameSession` | Một phần | Không còn sở hữu board/score/history/lives; còn level entry, timer adapter và BoardView wiring |
| `PoolManager.cs` | Object pool Unity | Một phần | Adapter; cần reset/reactivate |

## G. Audio và feedback

| Godot | Unity đích dự kiến | Trạng thái |
|---|---|---|
| `scripts/module/sound/sound_manager.gd` | `Audio/SoundManager.cs` | Chưa có |
| `scripts/module/common/vibrate_manager.gd` | `Feedback/VibrationService.cs` | Chưa có |
| `game/view/level_flow_*.gd` + scenes | Score/multiplier/deduction views | Chưa có |
| `game/ui/compont/game_like_hand.tscn` | Spine feedback view/pool | Chưa có |
| Audio assets | Audio catalog/mixer settings | Asset đã copy, chưa nối |

## H. Page và feature P0/P1

| Feature | Godot nguồn chính | Unity đích | Trạng thái |
|---|---|---|---|
| Splash | `splash/view/splash_page.gd`, scene tương ứng | `SplashPage` | Chưa có |
| Tutorial | `tutorial/view/tutorial_page.gd`, `tutorial_page.tscn` | `TutorialPuzzle`, `TutorialStateMachine`, `TutorialPage` | Domain/config bảy bước đã port và compile; presenter/visual chưa có |
| Home | `home/view/home_page.gd`, `home_page.tscn` | `HomePage` | Scene khung |
| Settings | `setting/view/setting_page.gd` | `SettingPage` | Chưa có |
| Language | `language_manager.gd`, language page/option | Localization pages | Chưa có |
| How to play | Hai script/scene page | HowToPlay pages | Chưa có |
| Win | `result/view/game_win_page.gd`, hai scene variant | GameWin pages | Chưa có |
| Fail | `result/view/game_fail_page.gd` | GameFail page | Chưa có |
| Revive restored | `ad_reward_restored_page.gd` | Popup tương ứng | Chưa có |
| Daily | `daily_game_page.gd`, entry, win/fail, stats | Daily module | Chưa có |
| Daily Streak | `feature_daily_streak.gd`, data, pages/cells | Streak module | Chưa có |
| Award | `award_manager.gd`, render/model/page | Award module | Chưa có |

## I. Feature P2 và dịch vụ ngoài

Các nhóm sau chỉ triển khai sau vòng offline P0/P1, nhưng không được xóa khỏi kiến trúc: Profile, Robot, Rank Activity, Tracker, Ads/UniKit, Auth, Data Sync, Push, Privacy/ATT, Feedback, Rate Us, Helpshift và remote A/B.

Trong giai đoạn offline, mỗi dịch vụ ngoài phải có interface và implementation no-op/mock rõ ràng; gameplay không được gọi SDK trực tiếp.

## Quy tắc cập nhật Source Map

Khi thêm một class Unity:

1. Ghi file Godot nguồn ngay trong XML summary hoặc tài liệu module.
2. Cập nhật dòng tương ứng ở đây.
3. Nếu không có file nguồn trực tiếp, đăng ký trong “Sổ chuyển thể” của `PORTING_ROADMAP.md`.
4. Chỉ ghi `Hoàn thành` sau khi test/checklist liên quan trong `ParityChecklist.md` đạt.
