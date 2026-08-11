# R12 App Runtime Composition Report — 2026-08-10

## Kết quả

Đã tạo entry point tích hợp theo mô hình một UIManager của nguồn thay cho việc nối thêm scene-per-page:

```text
AppScene
├─ Main Camera
├─ Global Light 2D
├─ EventSystem
└─ App
   ├─ UI
   │  ├─ Windows
   │  └─ SharedOverlays
   │     ├─ ModalMask
   │     └─ InputGuard
   └─ Systems
      └─ AppBootstrap
```

Unity đã sinh `SplashPage.prefab`, `GamePage.prefab` và `AppScene.unity` bằng serialization API. `UIRegistry.asset` hiện đăng ký đủ Splash, Home, Game, Bank, Tutorial, Setting, Language, HowToPlay và HowToPlayPaged. GameplayScene cũ không bị sửa và vẫn dùng làm scene test trực tiếp.

## Parity nguồn đã khóa

- Startup giữ launcher wait 2,0 giây + padding 0,5 giây; Splash giữ minimum 3,0 giây, finish tween 0,1 giây, 67 slogan và quy tắc slogan đầu ngày.
- Route đầu vào dùng `tutorial_done`: Tutorial hoặc Home.
- Game page nhận `level_index` hoặc toàn bộ direct Bank/retry parameters; `custom_color_map` và scalar bank metadata đi qua `LevelEntry` hiện có.
- Back, Settings, How-to-play và Return Bank đi qua UIManager, không dùng scene lookup.
- Direct Bank session dùng level 0 và không ghi/xóa snapshot của level thường khi win/quit/restart.
- Board prewarm tạo tối đa bốn cell mỗi frame vào pool như `board_view.gd:prewarm_cells`, và dừng an toàn nếu page bắt đầu setup trong lúc coroutine đang yield.

## File chính

- `Assets/_Project/Scripts/Gameplay/SplashPagePresenter.cs`
- `Assets/_Project/Scripts/Gameplay/GameplayPagePresenter.cs`
- `Assets/_Project/Scripts/Gameplay/GameplayManager.cs`
- `Assets/_Project/Scripts/Gameplay/BoardView.cs`
- `Assets/_Project/Scripts/Gameplay/MainGameTransitionCoordinator.cs`
- `Assets/_Project/Editor/SplashPagePrefabInstaller.cs`
- `Assets/_Project/Editor/AppRuntimeSceneInstaller.cs`
- `Assets/_Project/Editor/UIRegistryAssetInstaller.cs`
- `Assets/_Project/Tests/EditMode/AppRuntimeCompositionTests.cs`

## Kiểm chứng

- `Meowdoku.Gameplay`, `Meowdoku.Editor` và `Meowdoku.EditModeTests` compile sạch bằng Unity Roslyn sau thay đổi cuối.
- YAML sinh bởi Unity đã được kiểm tra có App/UI/Systems tree, serialized UIManager/AppBootstrap references, GamePage presenter/buttons/manager và `startAutomatically: 0`.
- Có fixture kiểm tra registry, prefab presenter/missing script, AppScene composition, Build Settings và board prewarm bốn cell/frame.

## Còn chờ

- Installer đã được sửa để ghi AppScene vào Build Settings trước thao tác SaveScene có thể kích hoạt refresh/domain reload. Editor hiện chưa quay lại callback Edit Mode nên `ProjectSettings/EditorBuildSettings.asset` chưa phản ánh lần sửa cuối.
- Cần chạy Unity Test Runner và Play Mode từ AppScene để xác nhận cold/warm route, popup input guard, Home ↔ các page, Tutorial → Game và Bank → Game → Bank.
- Pixel/animation parity và Win/Fail/Revive tiếp tục ở R13; không đánh dấu cổng R10/R12 hoàn thành trước các phép thử này.
