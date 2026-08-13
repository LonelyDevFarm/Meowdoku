# R17 Release Readiness & Save Lifecycle Gate — 2026-08-12

## Mục tiêu

Khóa một cổng QA có thể kiểm chứng trong Unity Editor trước khi làm device/pixel polish. Cổng này không thay thế kiểm thử Android/iOS thật và không biến các scene prototype thành production route.

## Kiểm tra đã thêm

`Assets/_Project/Tests/EditMode/AppRuntimeCompositionTests.cs` bổ sung:

1. `AppScene_ExcludesLegacyPrototypeServices`: mở `AppScene` bằng `EditorSceneManager.OpenPreviewScene` và xác nhận không có `SceneLoader` hoặc singleton `PoolManager`. AppScene dùng `AppBootstrap`/`UIManager` và pool thuộc `BoardView`.
2. `BuildSettings_StartWithAppScene`: ngoài kiểm tra `AppScene` là scene đầu tiên và enabled, kiểm tra mọi scene enabled tồn tại trên disk và không xuất hiện trùng đường dẫn.
3. `BuildSettingsScenes_HaveNoMissingScripts`: mở preview từng scene enabled và quét toàn bộ root bằng `GameObjectUtility.GetMonoBehavioursWithMissingScriptCount`.
4. `BoardPool_SetupClearReusesCellsAndResetsState`: setup board 2×2, đổi một cell thành CAT, clear, setup lại và xác nhận đúng 4 instance cũ được dùng lại, mọi cell inactive/EMPTY sau clear và active/EMPTY sau setup mới.

`Assets/_Project/Tests/EditMode/SaveStoreTests.cs` cũng được tăng assertion cho `LegacyMigration_WritesFirstSlotAndPreservesLegacy`: sau lần migrate đầu, lần gọi thứ hai phải trả `NotNeeded` và giữ nguyên bytes slot đã commit. Điều này khóa migration one-shot mà không cần giả lập server hay thiết bị.

`Assets/_Project/Tests/EditMode/GameStateRepositoryTests.cs` bổ sung hai integration case:

- Cả `save_a.cfg` và `save_b.cfg` cùng hỏng phải trả `GameStateData` mặc định nguồn thay vì null/partial state.
- `endgame.cfg` hỏng phải chỉ bỏ snapshot/game id, còn player progress/settings hợp lệ vẫn được khôi phục.

`SaveStoreTests.DualSlot_VerifyFailurePreservesCommittedSlotsAndFlag` mô phỏng file `.tmp` bị hỏng ngay sau khi flush và trước bước đọc-verify. Hook `beforeVerify` chỉ là constructor `internal` cho test assembly; API runtime công khai không đổi. Regression xác nhận:

- `SaveConfig` trả `false`.
- Flag vẫn trỏ slot B đã commit, bytes slot A cũ không đổi.
- File `.tmp` lỗi được dọn.
- Lần load tiếp theo vẫn trả dữ liệu mới nhất từ slot B.

Thứ tự này khớp `save_store.gd`: ghi temp → load temp để verify → rename vào target → chỉ sau thành công mới ghi flag. Unity giữ thêm replace/backup adapter phù hợp filesystem của nền tảng.

`PrimaryNavigationPlayModeTests.PlatformLifecycle_FocusPauseResumeDoesNotDuplicateSessionOrGamePage` đối chiếu `session_manager.gd`, `launcher.gd` và `base_game_page.gd`, rồi mô phỏng ba chu kỳ callback ghép mà Unity thường phát khi suspend/resume. Test xác nhận:

- Startup chỉ tăng `SessionCount` đúng một lần dù `TrackingRuntime` khởi tạo trước bootstrap.
- Resume ngắn giữ nguyên session id/count và chỉ tăng `session_record` một lần mỗi chu kỳ, dù cả callback pause và focus cùng tới.
- `UIManager` vẫn trả đúng Game page/`GameplayManager` instance cũ và trong scene chỉ có một `GameplayManager`.
- CAT đặt trước suspend, trạng thái Playing và active GameSession đều được giữ sau resume.

`PrimaryNavigationPlayModeTests.PlatformStartup_CorruptPlayerSlotsUseDefaultsAndExitSplash` dùng thư mục tạm và production `GameStateRepository`: tạo đủ slot A/B, phá hỏng cả hai trước khi nạp AppScene, rồi xác nhận contract `_load_data()`/`launcher.gd` của nguồn:

- Repository trả defaults thay vì null/partial state.
- Bootstrap hoàn tất, Splash chuyển Hidden và route Tutorial vì `tutorial_done=false` mặc định.
- Route dùng đúng prefab có `TutorialPagePresenter`, không chỉ kiểm tra enum/state giả.
- Launcher chỉ force Splash sau cổng thời gian tối thiểu theo công thức 2,0+0,5 giây; default progress của presenter là 3,0 giây và force-complete thực sự chạy finish tween 0,1 giây trước routing.
- Level 1, strategy 1 và tool counts 5/5/3 được giữ.
- `OnSessionStarted` chỉ chạy một lần và startup tự commit lại một slot mặc định đọc được; không chạm `Application.persistentDataPath` thật.

Trường hợp save hoàn toàn thiếu tiếp tục được bao phủ bởi `GameStateRepositoryTests.MissingSave_LoadsSourceDefaults`.

`PrimaryNavigationPlayModeTests.PlatformNavigation_HomeBackOpensSourceQuitConfirm` đối chiếu `home_page.gd`, `confirm_dialog.gd/.tscn` và global `ui_cancel` trong `ui_manager.gd`. Unity dùng `InputSystem.onEvent` để đọc cạnh nhấn Escape/Android Back hoặc Gamepad East ngay tại event, thay cho một callback `InputAction.performed` không ổn định trong nhịp Test Runner. Test đợi Splash đóng hoàn toàn rồi xác nhận:

- Home Back mở đúng prefab Confirm đã đăng ký và các key title/content/action được dịch.
- Back lần hai vẫn để Confirm hiển thị, đúng source vì dialog có `CloseButton` thay vì close button convention của frame.
- Close chỉ đóng dialog, không gọi quit.
- Action đóng dialog và gọi callback quit đúng một lần dù bị invoke liên tiếp.
- Subscription/input edge được cleanup theo `OnDisable`/`OnDestroy`; không dùng polling `Update` và không tạo log runtime.

Hai EditMode composition case cũng khóa registry Confirm và các serialized binding bắt buộc của prefab.

Timing launcher còn được khóa độc lập bởi `UIPopupStartupTests.SplashTiming_MatchesLauncher`; timestamp runtime chỉ compile khi `UNITY_INCLUDE_TESTS`. Các assertion dùng Unity Editor API, không đọc/sửa YAML scene bằng suy đoán và không tạo runtime log.

## Kết quả Unity thật

- EditMode targeted (composition/platform/product/save/repository): **48 passed, 0 failed**.
- PlayMode targeted Platform: **6 passed, 0 failed**.
- Không có thay đổi Build Settings hoặc xóa scene prototype trong lượt này; Loading/Home/Gameplay cũ vẫn được giữ để đối chiếu, còn production entry là `AppScene`.

## Phần còn mở của R17

- Android/iOS touch, notch, OS pause/resume thật và hard-kill.
- Unity Profiler: startup, GPU/batch/draw call và GC trên thiết bị.
- Soak level 1–250/daily/restart/pool, pixel comparison và audio/video timing.
- Build signing, symbols và release checklist.
- Hard-kill filesystem thật và update/schema migration tương lai.

Những mục này không ảnh hưởng acceptance của bản thử nghiệm offline hiện tại; online SDK/ads/review/feedback thật cũng không nằm trong phạm vi bắt buộc.
