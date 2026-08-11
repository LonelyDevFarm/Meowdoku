# R10–R12 UI Navigation Verification Report — 2026-08-11

## Mục tiêu

Kiểm chứng bằng trạng thái dự án thật rằng Home, Tutorial, Settings, Language, How-to-play, Bank và Gameplay có composition hợp lệ và có thể mở/đóng trên `AppScene` trong Play Mode.

## Đối chiếu nguồn

- Tutorial hoàn tất gọi Game level 1 rồi ẩn Tutorial như `tutorial_page.gd`.
- Settings mở Language trực tiếp; mở How-to-play paged rồi ẩn Settings như `setting_page.gd`.
- Bank launch gọi Game với bank parameters; Back ở root gọi Home rồi ẩn Bank như `bank_page.gd`.
- Gameplay Back về Home, Settings mở dạng game-mode, How-to-play dùng page toàn màn hình và bank session có Return Bank.

## Thay đổi kiểm chứng

- `AppRuntimeCompositionTests.Registry_ContainsEveryPrimaryNavigationPage` khóa đúng mapping registry → prefab → presenter cho Home, Tutorial, Setting, Language, HowToPlay, HowToPlayPaged, Bank và Game.
- `PrimaryNavigationPresenters_HaveRequiredBindings` khóa các Button, BoardView, popup animator, demo boards, bank cards, GameplayManager và WinToast bắt buộc.
- Mọi prefab mục tiêu được kiểm tra không có missing script.
- UI lifecycle test kiểm tra cache/reuse, Hide → Closing, Show lại trong lúc Closing không phát duplicate shown event, rồi close hoàn tất Hidden và phát đúng một hidden event.

## Unity Test Runner automation

- Thêm `UnityEditModeTestBridge` trong assembly Editor-only.
- Named event: `Local\\Meowdoku.UnityEditModeTests`.
- Dùng `TestRunnerApi` chính thức, lọc assembly `Meowdoku.EditModeTests`.
- Ghi kết quả tóm tắt tại `Temp/MeowdokuEditModeTestResult.txt` và NUnit XML tại `Temp/MeowdokuEditModeTestResult.xml`.
- Không chạy trong player và không thêm runtime log.
- Thêm `UnityPlayModeTestBridge` với event `Local\\Meowdoku.UnityPlayModeTests`; bridge dùng `SessionState` để giữ callback qua domain reload khi vào/thoát Play Mode.
- PlayMode dùng `GameStateRuntime.OverrideForTests` để thay state bằng service bộ nhớ tạm và phục hồi đúng service/repository trước đó; không flush hoặc ghi vào save thật.

## PlayMode AppScene smoke

- Tải đúng `Assets/_Project/Scenes/AppScene.unity` và chờ `AppBootstrap` tới `Complete`.
- Xác nhận Splash về `Hidden` và Home ở `Showing` khi `tutorial_done=true`.
- Mở/đóng thật Tutorial, Language, HowToPlay, HowToPlayPaged và Bank.
- Settings nhận back request và đóng về `Hidden`.
- Hide Language rồi Show lại ngay khi `Closing` tái sử dụng cùng cached instance và không bị coroutine cũ đóng nhầm.
- Game mở với `level_index=1`, `GameplayManager` dựng puzzle 4×4 thật rồi đóng sạch; Home vẫn còn hoạt động và `IsAnyLoading=false`.
- Test interaction thứ hai invoke đúng listener đã serialize: `Home/SettingsBtn` mở Settings, `Settings/CloseBtn` đóng; `Home/StartBtn` chạy animation marker để mở Game và ẩn Home; `Game/BackBtn` quit session, đóng Game rồi hiện lại Home.

## Sai lệch runtime phát hiện và sửa

- Khi Bank materialize root cards, bank LK Style thật lộ `OverflowException` ở `LevelEntry.ReadInt("seed")`.
- Dữ liệu nguồn có seed `5.319.538.187`, `7.047.525.929` và `9.579.550.828`, vượt `Int32.MaxValue`.
- Godot `int` là signed 64-bit; Unity model đã đổi `LevelEntry.Seed`, Daily selection và Bank launch seed sang C# `long` xuyên suốt. Regression `LevelEntry_PreservesGodotInt64Seed` khóa lại case này.

## Lỗi suite cũ được hiệu chỉnh theo nguồn

- Daily fixture đổi solution 4×4 sang `[1,3,0,2]`; `[0,1,2,3]` cũ vi phạm luật mèo kề chéo nên selector đúng khi bỏ qua.
- Level 40 vừa là hard level vừa nằm trong `_SPECIAL_LEVELS` ở cả Godot và Unity.
- Sau hai fail rồi win, pre-cat chuyển `PendingStruggle=true` và reset fail count về 0 đúng `game_state.gd`.
- EditMode không tick coroutine do `MonoBehaviour.StartCoroutine`; test dùng driver `UNITY_EDITOR` để enumerate cùng `HideRoutine`, không đổi scheduling player.

## Kết quả

- Unity Tundra compile thành công và assembly reload hoàn tất.
- Unity EditMode Test Runner: **508 passed, 0 failed, 0 skipped, 0 inconclusive**.
- Unity PlayMode Test Runner: **2 passed, 0 failed, 0 skipped, 0 inconclusive**.
- Kết quả đều lấy từ Test Runner thật và có NUnit XML dưới `Temp`.

## Còn lại

- Mở rộng Button test cho các nhánh phụ có config gate: Settings Language/HTP và Bank launch/Return Bank.
- Stress nhiều transition/input release để khóa input guard và one-flight dưới thao tác nhanh.
- So pixel/timing ở 1080×1920 và 1080×2400.
