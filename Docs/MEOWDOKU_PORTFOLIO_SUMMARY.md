# Meowdoku — Portfolio & Repository Review

> Phạm vi review: trạng thái repository hiện tại ngày 2026-08-16. Các claim dưới đây được đối chiếu từ source C#, `AppScene`, `UIRegistry.asset`, prefab, data asset, `Packages/` và `ProjectSettings/`. Tài liệu cũ trong `Docs/`/`Reports/` chỉ được dùng như chỉ dẫn tìm kiếm, không được xem là nguồn sự thật thay cho project hiện tại.
>
> Giới hạn xác minh: Unity Editor 6000.3.19f1 không được tìm thấy trên máy trong phiên review, nên không chạy lại EditMode/PlayMode tests hoặc build. “Có implementation/wiring” không đồng nghĩa đã QA trên thiết bị hay đã phát hành.

## 1. Project Overview

### Thể loại

Meowdoku là game puzzle logic 2D kiểu **Queens/Queendoku**: người chơi đặt mèo lên lưới màu sao cho mỗi hàng, mỗi cột và mỗi vùng màu có đúng một mèo; các mèo không được chạm nhau theo đường chéo lân cận.

### Core gameplay loop

1. Mở app qua splash và tutorial lần đầu, sau đó vào Home.
2. Chọn level chính, daily challenge hoặc một puzzle từ bank browser.
3. Tap/double-tap/swipe để đặt mèo hoặc đánh dấu ô bị loại.
4. Dùng Undo/Clear/Locate/Hint; nhận phản hồi lỗi, combo, điểm, mạng sống và rule highlight.
5. Hoàn thành hoặc thất bại; có result flow, retry/revive/next level, cập nhật progress, daily/streak và rank activity cục bộ.

### Nền tảng mục tiêu

- **Android** là target được xác minh rõ: application ID `com.meowdoku.portfolio`, minimum API 25, ARM64, fullscreen và Android game category trong `ProjectSettings/ProjectSettings.asset`.
- Code có nhánh iOS cho vibration, privacy/ATT và build defines, nhưng repository chưa đủ bằng chứng về một iOS build đã chạy hoặc đã QA. **Needs verification** trước khi quảng bá iOS là platform được hỗ trợ.
- UI có hỗ trợ pointer/desktop trong Editor để phát triển và test; đây không phải bằng chứng về bản PC phát hành.

### Main scenes

- `Assets/_Project/Scenes/AppScene.unity` — scene duy nhất được enable trong Build Settings; chứa runtime composition cho app, UI, audio, state và service boundaries.
- `Assets/_Project/Scenes/HomeScene.unity`, `GameplayScene.unity`, `LoadingScene.unity` — scene project còn tồn tại nhưng không nằm trong build list hiện tại; flow runtime chính dùng UI prefab qua `UIRegistry` trong `AppScene`.

### Trạng thái hoàn thiện có thể xác minh

- Có một vertical slice rộng và được nối vào một app scene: splash/tutorial/home, gameplay, result, settings/language/how-to-play, bank, daily/streak, profile và rank activity.
- Có 224 runtime C# scripts, 59 test scripts, 34 project prefabs và 28 encrypted level-bank JSON files.
- Có 1.695 localization keys và 75 locale columns ngoài cột `key` trong `Assets/_Project/Localization/translations.csv`.
- Có EditMode và PlayMode test assemblies, nhưng kết quả pass hiện tại **Needs verification** vì tests không được chạy lại trong review này.
- Ads, auth, data sync, analytics sink và native permission SDK được thiết kế dưới dạng adapter boundary; build hiện tại có fallback offline/null. Không nên mô tả chúng là production integrations.
- Release store, APK/AAB, device QA và public release status: **NEEDS USER INPUT**.

### Mô tả ngắn

Meowdoku là một mobile-first Unity 2D logic puzzle game, kết hợp luật Queens theo vùng màu với progression, daily streak, local rank simulation và một hệ thống UI/feedback tương đối hoàn chỉnh.

## 2. Player-Facing Features

### Core gameplay

| Feature | Mô tả | Evidence |
|---|---|---|
| Queens/region rule puzzle | Kiểm tra một mèo trên mỗi hàng, cột, vùng và xung đột chéo lân cận; theo dõi trạng thái board và completion. | `Assets/_Project/Scripts/Core/QueendokuCore.cs`, `CellState.cs`, `Assets/_Project/Scripts/Gameplay/GameSession.cs` |
| Board sizes and puzzle banks | Chọn level theo size/rank/tier từ nhiều bank; data được lưu dạng JSON XOR-obfuscated và load qua Unity `Resources`. | `Assets/_Project/Scripts/Core/LevelData.cs`, `LevelBankIO.cs`, `BankData.cs`, `Assets/_Project/Resources/Levels/` |
| Touch/pointer input | Tap, double-tap và swipe với gesture recognizer, velocity/axis guard và nội suy stroke qua nhiều ô. | `Assets/_Project/Scripts/Gameplay/BoardView.cs`, `Assets/_Project/Scripts/Gameplay/Input/` |
| Mistakes, lives and fail | Wrong guess tạo error/conflict feedback, trừ điểm/mạng và chuyển fail khi hết mạng. | `Assets/_Project/Scripts/Gameplay/GameSession.cs`, `GameplayLifeHudPresenter.cs`, `GameFailPagePresenter.cs` |
| Gameplay tools | Undo, Clear, Locate, Hint và Auto Complete; tool resource coordinator quyết định free/owned/reward-required. | `Assets/_Project/Scripts/Gameplay/ToolResourceCoordinator.cs`, `GameplayToolBarPresenter.cs`, `GameSession.cs` |
| Hint system | Hint engine suy luận mark/single, subset/chain contradiction và trả về cell/unit/highlight metadata cho overlay. | `Assets/_Project/Scripts/Core/HintEngine.cs`, `Assets/_Project/Scripts/Gameplay/GameplayHintOverlayPresenter.cs` |

### Game modes and progression

| Feature | Mô tả | Evidence |
|---|---|---|
| Main progression | Lưu current level, chọn độ khó/pool, record puzzle gần đây, retry cache và advance sau kết quả. | `Assets/_Project/Scripts/Core/GameStateService.cs`, `LevelData.cs`, `Assets/_Project/Scripts/Gameplay/MainGameTransitionCoordinator.cs` |
| Daily challenge | Chọn daily puzzle, theo dõi trạng thái entry/result/stats, daily win/fail/restart/quit bằng coordinator riêng. | `Assets/_Project/Scripts/Core/Daily/DailyPuzzleSelector.cs`, `DailyEntryState.cs`, `DailyStats.cs`, `Assets/_Project/Scripts/Gameplay/DailyGameTransitionCoordinator.cs` |
| Bank browser | Duyệt root/pool/size/tier/level, hiển thị progress và launch puzzle được chọn. | `Assets/_Project/Scripts/Core/UI/BankBrowserContract.cs`, `Assets/_Project/Scripts/Gameplay/BankBrowserPagePresenter.cs`, `Assets/_Project/Prefabs/UI/BankPage.prefab` |

### Score and feedback

| Feature | Mô tả | Evidence |
|---|---|---|
| Score/combo | Tính điểm đặt mèo đúng, multiplier/combo/max combo, penalty và life bonus; dữ liệu được serialize trong snapshot. | `Assets/_Project/Scripts/Core/GameScoreModel.cs`, `Assets/_Project/Scripts/Gameplay/GameSession.cs` |
| Animated feedback | Score flight, multiplier, bubble feedback, cat burst, board intro, win/fail celebration và toast dùng DOTween. | `Assets/_Project/Scripts/Gameplay/GameplayFeedbackPresenter.cs`, `GameplayScoreFlightView.cs`, `GameplayMultiplierView.cs`, `GameplayCatBurstView.cs`, `ResultCelebrationEffects.cs` |
| Rule and hint presentation | Rule bar chỉ ra loại vi phạm; hint overlay highlight cell/unit/region theo metadata từ hint engine. | `Assets/_Project/Scripts/Gameplay/GameplayRuleBarPresenter.cs`, `GameplayHintPresentationData.cs`, `GameplayHintOverlayPresenter.cs` |

### Daily streak and rewards

| Feature | Mô tả | Evidence |
|---|---|---|
| Multi-day streak | Ghi nhận ngày chơi, rollover, backfill/resume/revive state và trình bày day slots. | `Assets/_Project/Scripts/Core/Daily/StreakFeature.cs`, `StreakRepository.cs`, `StreakData.cs`, `Assets/_Project/Scripts/Gameplay/StreakPagePresenter.cs` |
| Awards | Tạo/claim pending awards, chống claim lặp qua UID/in-flight state và hiển thị reward page. | `Assets/_Project/Scripts/Core/Daily/AwardManager.cs`, `AwardItem.cs`, `Assets/_Project/Scripts/Gameplay/AwardPagePresenter.cs` |

### Profile and local rank activity

| Feature | Mô tả | Evidence |
|---|---|---|
| Profile customization | Chọn nickname, avatar và frame; catalog và repository lưu lựa chọn/progress. | `Assets/_Project/Scripts/Core/Profile/`, `Assets/_Project/Scripts/Gameplay/ProfilePagePresenter.cs`, `ProfileSelectionCell.cs` |
| Rank activity | Period/tier/ranking/reward flow với podium, list, countdown, rank-change celebration và chest/frame rewards. | `Assets/_Project/Scripts/Core/Rank/`, `Assets/_Project/Scripts/Gameplay/RankActivityPagePresenter.cs`, `RankActivityChangePresenter.cs`, `RankGiftView.cs` |
| Simulated competitors | Robot service sinh timeline/điểm/nickname để tạo leaderboard cục bộ. Đây không phải online multiplayer leaderboard. | `Assets/_Project/Scripts/Core/Robot/` |

### Tutorial and help

| Feature | Mô tả | Evidence |
|---|---|---|
| Interactive tutorial | State machine nhiều phase, input gating, feedback và completion commit; first-launch routing dựa trên `TutorialDone`. | `Assets/_Project/Scripts/Core/Tutorial/TutorialStateMachine.cs`, `TutorialPuzzle.cs`, `Assets/_Project/Scripts/Gameplay/TutorialPagePresenter.cs` |
| How to Play | Có full demo và paged demo, board minh họa và navigation riêng. | `Assets/_Project/Scripts/Core/UI/HowToPlayContract.cs`, `Assets/_Project/Scripts/Gameplay/HowToPlayPagePresenter.cs`, `HowToPlayPagedPagePresenter.cs` |

### Settings, audio and localization

| Feature | Mô tả | Evidence |
|---|---|---|
| Settings | Toggle sound/music/vibration và các lựa chọn gameplay/pattern; state được persist. External feedback/privacy actions có offline fallback. | `Assets/_Project/Scripts/Gameplay/SettingsPagePresenter.cs`, `SettingsToggleView.cs`, `Assets/_Project/Scripts/Core/GameStateService.cs`, `Assets/_Project/Scripts/Core/UI/SettingsExternalServices.cs` |
| Audio | Scene-owned sound runtime/service, ScriptableObject catalog, fixed/dynamic clips và centralized button-click emitter. | `Assets/_Project/Scripts/Services/`, `Assets/_Project/Settings/SoundCatalog.asset`, `Assets/_Project/Audio/` |
| Vibration/haptics | Level-based vibration contract; Android dùng native vibration APIs/JNI và iOS có coarse Unity fallback. Device behavior **Needs verification**. | `Assets/_Project/Scripts/Core/VibrationService.cs`, `Assets/Plugins/Android/MeowdokuVibration.androidlib/` |
| Localization | CSV-driven catalog, fallback/locale aliases, persisted locale, localized TMP text và language page/widget. | `Assets/_Project/Scripts/Core/Localization/`, `Assets/_Project/Localization/translations.csv`, `Assets/_Project/Settings/LocalizationCatalog.asset` |

### Save and mobile UX

| Feature | Mô tả | Evidence |
|---|---|---|
| Persistent game state | Lưu progress, settings, stats, tools, daily/streak/profile/rank data và endgame snapshot; có migration legacy. | `Assets/_Project/Scripts/Core/GameStateData.cs`, `GameStateService.cs`, `GameStateRepository.cs`, `SaveStore.cs` |
| Resume/retry | Snapshot gồm board, lives, score, history và context; save/restore khi pause/focus hoặc transition. | `Assets/_Project/Scripts/Gameplay/GameSessionSnapshot.cs`, `GameplayManager.cs` |
| Mobile layout | Safe-area handling, reference layout 1080-wide, board resizing và portrait-oriented page composition. | `Assets/_Project/Scripts/Gameplay/SourceGameplayPageLayout.cs`, `GameplayPageLayoutPresenter.cs`, `SourceBoardLayout.cs` |

## 3. Technical Systems

### Puzzle domain and session state machine

- **Purpose:** tách luật puzzle và session state khỏi presentation.
- **Main scripts:** `QueendokuCore.cs`, `GameSession.cs`, `CellState.cs`, `GameScoreModel.cs`, `StepHistory.cs`.
- **Dependencies:** C# collections/value types; Unity `Vector2Int`; config models.
- **How it works:** `BoardStateModel` giữ cell state và kiểm tra conflict/completion. `GameSession` nhận edit/tool action, cập nhật history, score, lives và trả về `SessionActionResult`; `GameplayManager` chuyển kết quả domain thành view feedback và transition.

### Level selection, generation and bank loading

- **Purpose:** chọn puzzle theo progression, size, tier, DDA và tránh lặp; dựng region/color pipeline.
- **Main scripts:** `LevelData.cs`, `LevelGenerator.cs`, `LevelBankIO.cs`, `BankData.cs`, `LevelEntry.cs`.
- **Dependencies:** `Resources.Load<TextAsset>`, `MiniJson`, `GameStateService`, AB config.
- **How it works:** encrypted/obfuscated resources được XOR-decode rồi deserialize; `LevelData` lọc/chọn entry và cập nhật progress; `LevelGenerator` xử lý region mapping, palette và pattern.

### Input and board presentation

- **Purpose:** xử lý input mobile ổn định và render board/cell theo puzzle size.
- **Main scripts:** `BoardView.cs`, `CellView.cs`, `BoardGestureRecognizer.cs`, `SwipeGuardRecognizer.cs`, `SwipeVelocityGate.cs`, `SwipeAxisGuard.cs`, `BoardGridOverlayGraphic.cs`.
- **Dependencies:** Unity EventSystem/Input System, uGUI, DOTween.
- **How it works:** board-level pointer stream được đổi thành tap/double-tap/swipe operations; guards lọc hướng/vận tốc; board dùng cell pooling/prewarm, layout metrics và custom graphic cho grid/region boundary.

### Hint engine

- **Purpose:** tìm bước logic và cung cấp dữ liệu giải thích/highlight.
- **Main scripts:** `HintEngine.cs`, `HintMutex.cs`, `GameplayHintPresentationData.cs`, `GameplayHintOverlayPresenter.cs`.
- **Dependencies:** board state và puzzle region map.
- **How it works:** engine xây candidate state, thử chiến lược từ direct mark/single đến subset/contradiction chain; mutex khóa input trong lúc presentation đang active.

### UI framework and app bootstrap

- **Purpose:** quản lý toàn bộ page/popup lifecycle trong một app scene.
- **Main scripts:** `AppBootstrap.cs`, `UIManager.cs`, `UIRegistry.cs`, `UIBaseWindow.cs`, `UIFrameWindow.cs`, `UIPopupQueue.cs`, `UIButtonPressGuard.cs`.
- **Assets:** `Assets/_Project/Settings/UIRegistry.asset`, `Assets/_Project/Prefabs/UI/`, `Assets/_Project/Scenes/AppScene.unity`.
- **Dependencies:** uGUI, coroutines, localization/config/runtime services.
- **How it works:** registry map stable `UiName` IDs sang prefab. `UIManager` instantiate/cache/prewarm, giữ stack/layer/mask/back/input guard; bootstrap khởi tạo state/config/locale, chạy splash, optional external boundaries rồi route Tutorial hoặc Home.

### Persistence and lifecycle

- **Purpose:** lưu state bền vững mà không chặn frame chính.
- **Main scripts:** `SaveStore.cs`, `GameStateRepository.cs`, `GameStateService.cs`, `GameStateData.cs`, `GameSessionSnapshot.cs`.
- **Dependencies:** `Application.persistentDataPath`, AES, PBKDF2/SHA-256/HMAC-SHA256, background thread, `MiniJson`.
- **How it works:** serialized state được mã hóa và xác thực, ghi atomic vào dual slots với flag/fallback; worker writer xử lý disk work, lifecycle flush pending writes; legacy migration và endgame snapshot có đường xử lý riêng.

### Configuration/feature variants

- **Purpose:** gom typed defaults và variant policy cho input, layout, gameplay, result, settings, ads và daily/streak.
- **Main scripts:** `Assets/_Project/Scripts/Core/Config/`, đặc biệt `AbConfigRuntime.cs`, `DefaultConfigProfile.cs`, `AbConfigBase.cs`.
- **Dependencies:** `GameStateService`; optional `IAbValueProvider`.
- **How it works:** các config typed lấy default hoặc provider override theo timing AppStart/GameStart. Repository chưa chứng minh remote-config SDK thực tế; default provider vẫn cho phép offline behavior.

### Daily, streak and award domain

- **Purpose:** điều phối daily puzzle, calendar state, streak lifecycle và reward claim.
- **Main scripts:** `Assets/_Project/Scripts/Core/Daily/`, `DailyGameTransitionCoordinator.cs`, `StreakFlowCoordinator.cs`.
- **Dependencies:** `GameStateService`, clock/date providers, UI result flow.
- **How it works:** daily selector tạo launch request; result coordinator settle win/fail/revive; streak repository merge/rollover theo ngày; awards dùng pending/in-flight state để bảo vệ idempotency.

### Profile, robot and rank activity

- **Purpose:** tạo meta progression và leaderboard-style activity hoạt động offline.
- **Main scripts:** `Assets/_Project/Scripts/Core/Profile/`, `Core/Robot/`, `Core/Rank/` và các rank/profile presenters.
- **Dependencies:** game state persistence, clock, DOTween UI.
- **How it works:** profile repository giữ avatar/frame/nickname; robot algorithms sinh score timeline; rank manager kết hợp player và bots, quản lý period/tier/settlement/reward rồi presenter dựng podium/list/celebration. Không có bằng chứng đây là leaderboard server thật.

### Localization

- **Purpose:** load translation table, resolve locale/fallback và refresh UI khi đổi ngôn ngữ.
- **Main scripts:** `LocalizationCsvReader.cs`, `LocalizationCatalog.cs`, `LocalizationLocaleContract.cs`, `LocalizedText.cs`, language presenters.
- **Dependencies:** CSV TextAsset, TextMeshPro/uGUI, persisted `GameStateService` locale.
- **How it works:** catalog parse 1.695 key rows, map system locale/alias sang translation column, fallback English và phát `LocaleChanged` cho localized views.

### Audio, animation and feedback

- **Purpose:** centralize sound, vibration và tween lifecycle.
- **Main scripts:** `Assets/_Project/Scripts/Services/`, `VibrationService.cs`, `TweenRuntimeConfiguration.cs`, gameplay feedback views.
- **Dependencies:** AudioSource, DOTween (vendored DLL/modules), Android JNI.
- **How it works:** `SoundCatalog` là ScriptableObject map enum/path sang clip; runtime inject service vào consumers; 42 runtime scripts dùng DOTween và nhiều presenter kill/reset tween khi hide/destroy/reopen.

### Optional production-service boundaries

- **Purpose:** cô lập ads, auth, sync, tracking, privacy/push và product actions khỏi gameplay.
- **Main scripts:** `Assets/_Project/Scripts/Core/Ads/`, `Core/Online/`, `Core/Tracking/`, `Core/Platform/`.
- **Dependencies:** interfaces như `IAdProvider`, `IAuthProvider`, `ITrackingSink`, `IPlatformPermissionProvider`; `UnityWebRequest` cho data-sync HTTP boundary.
- **How it works:** service/policy logic tồn tại và `AppScene` chứa runtime components, nhưng provider defaults là `NullAdProvider`, `NullAuthProvider`, `NullTrackingSink` và `OfflinePlatformPermissionProvider` khi thiếu adapter. Đây là extensibility/testability architecture, không phải bằng chứng SDK production đã tích hợp.

### Automated test structure

- **Purpose:** khóa contract cho puzzle, input, persistence, UI, daily/streak, rank/profile, service policies và app navigation.
- **Main paths:** `Assets/_Project/Tests/EditMode/`, `Assets/_Project/Tests/PlayMode/`.
- **Dependencies:** Unity Test Framework 1.6.0; assemblies `Meowdoku.EditModeTests` và `Meowdoku.PlayModeTests`.
- **Status:** 59 test scripts có trong project; pass/fail hiện tại **Needs verification**.

## 4. Important Project Structure

```text
Assets/
├── _Project/
│   ├── Animations/              # Spine-format animation data used by cat/result visuals
│   ├── Audio/                   # BGM and SFX clips
│   ├── Data/                    # Popup/dialog strategy JSON
│   ├── Editor/                  # Scene/prefab installers, migration and test bridges
│   ├── Fonts/                   # Roboto/Noto font assets and TMP font resources
│   ├── Localization/            # translations.csv
│   ├── Materials/               # Project-specific UI material
│   ├── Prefabs/
│   │   └── UI/                  # 33 page/popup/view prefabs plus Cell.prefab
│   ├── Resources/
│   │   └── Levels/              # 28 obfuscated level-bank JSON assets
│   ├── Scenes/                  # AppScene plus Home/Gameplay/Loading scenes
│   ├── Scripts/
│   │   ├── Core/                # Domain, state, save, config, daily, rank, profile, UI services
│   │   ├── Gameplay/            # Presenters, views, gameplay orchestration and input
│   │   └── Services/            # Audio contracts/runtime/service
│   ├── Settings/                # UIRegistry, localization, sound and cat animation catalogs
│   ├── Sprites/                 # Feature-grouped 2D UI/game art
│   └── Tests/                   # EditMode and PlayMode test assemblies
├── Plugins/
│   ├── Android/                 # Android vibration library
│   └── Demigiant/DOTween/       # Vendored DOTween binaries/modules
└── Settings/                    # URP/2D renderer assets
Packages/                        # Unity package manifest and lockfile
ProjectSettings/                 # Unity 6000.3.19f1 project/build/player settings
Docs/                            # Project audits and portfolio documentation
Reports/                         # Historical Codex/Gemini port and verification reports
```

`Library/`, `Logs/`, `UserSettings/` và `.git/` là local/generated/repository internals nên không thuộc README project tree.

## 5. Technical Highlights

1. **Domain-first puzzle/session architecture** — luật, board state, scoring và history không bị nhúng hoàn toàn vào MonoBehaviour; `QueendokuCore.cs` và `GameSession.cs` có thể được test độc lập. Điều này đáng chú ý vì thể hiện separation of concerns trong một game Unity nhỏ-vừa.
2. **Non-trivial hint solver** — `HintEngine.cs` không chỉ reveal đáp án mà tạo metadata cho nhiều chiến lược logic và contradiction chain; tốt để minh họa algorithmic gameplay work.
3. **Resilient encrypted persistence** — `SaveStore.cs` dùng AES + HMAC, dual-slot fallback, atomic replace, flush và background writer; vượt xa `PlayerPrefs` cho state phức tạp.
4. **Single-scene UI runtime with registry and pooling** — `UIManager.cs`, `UIRegistry.asset` và `AppBootstrap.cs` quản lý 33 registered UI routes, prewarm/cache, layers, popup queue và startup routing.
5. **Mobile input pipeline** — `BoardGestureRecognizer` cùng axis/velocity/swipe guards xử lý tap/double-tap/drag ở board level, phù hợp với interaction dày trên mobile.
6. **Config-driven behavior** — typed config groups tách variant policy khỏi gameplay call sites; hữu ích cho tuning và test dù remote provider production chưa được chứng minh.
7. **Lifecycle-aware presentation** — nhiều view dùng DOTween có cleanup/reopen behavior; snapshot/save được nối với pause/focus/transition.
8. **Offline-capable meta loop** — daily/streak/profile/rank/robot tạo cảm giác sản phẩm hoàn chỉnh mà vẫn có contract rõ ràng giữa domain, repository và presenter.
9. **Broad automated-test surface** — test source bao phủ core rules, persistence, navigation, services và meta systems; cần chạy lại trước khi công bố số test/pass rate.

Không nên mô tả các highlight này như “tự tay lập trình toàn bộ”. Cách trung thực là nói project thể hiện khả năng **thiết kế, tích hợp, kiểm tra và hoàn thiện hệ thống Unity với AI-assisted implementation**, sau khi người dùng xác nhận contribution split.

## 6. Portfolio-Worthy Features

| Feature | One-sentence description | Evidence |
|---|---|---|
| Queens-style puzzle rules | A color-region logic puzzle with row, column, region and adjacent-diagonal constraints. | `Core/QueendokuCore.cs`, `Gameplay/GameSession.cs` |
| Responsive mobile board input | Board-level tap, double-tap and swipe handling with gesture protection and stroke interpolation. | `Gameplay/BoardView.cs`, `Gameplay/Input/` |
| Multi-strategy hint engine | Generates structured logical hints and highlight data rather than only revealing an answer. | `Core/HintEngine.cs`, `GameplayHintOverlayPresenter.cs` |
| Score, combo and feedback | Combines scoring rules with animated score flights, multipliers, rule errors and cat reactions. | `Core/GameScoreModel.cs`, `Gameplay/GameplayFeedbackPresenter.cs` |
| Daily challenge and streak | Tracks daily puzzle results, calendar rollover, multi-day streak states and reward flow. | `Core/Daily/`, `Gameplay/DailyGameTransitionCoordinator.cs` |
| Progression and puzzle browser | Supports main progression plus browsing multiple puzzle banks by size, tier and level. | `Core/LevelData.cs`, `Gameplay/BankBrowserPagePresenter.cs` |
| Interactive onboarding | Routes first-time players through a phase-based tutorial and includes animated How to Play demos. | `Core/Tutorial/`, `Gameplay/TutorialPagePresenter.cs` |
| Profile and rank activity | Provides local avatar/frame customization and a simulated leaderboard activity with rewards. | `Core/Profile/`, `Core/Rank/`, `Core/Robot/` |
| Robust local persistence | Uses authenticated encrypted dual-slot saves, migration and resumable gameplay snapshots. | `Core/SaveStore.cs`, `Core/GameStateRepository.cs`, `Gameplay/GameSessionSnapshot.cs` |
| Localized UI | Loads a large CSV translation catalog with locale aliases, fallback and live text refresh. | `Core/Localization/`, `Localization/translations.csv` |
| Modular UI navigation | Uses a registry-driven, cached page/popup framework with startup routing and input guards. | `Core/UI/UIManager.cs`, `Core/UI/AppBootstrap.cs`, `Settings/UIRegistry.asset` |
| Mobile audio and haptics | Centralizes audio playback and provides Android-native vibration behavior with safe platform fallbacks. | `Scripts/Services/`, `Core/VibrationService.cs` |

## 7. Tech Stack

- **Engine:** Unity 6000.3.19f1.
- **Language:** C#.
- **Primary target:** Android, minimum API level 25, ARM64.
- **Rendering/UI:** Universal Render Pipeline 17.3.0 with 2D Renderer assets; Unity uGUI 2.0.0; TextMeshPro assets are used in prefabs/scripts through Unity UI tooling.
- **Input:** Unity Input System 1.19.0 plus custom gesture recognition.
- **Animation/tweening:** DOTween, vendored under `Assets/Plugins/Demigiant/DOTween`; Spine-format `.skel`/`.atlas` animation assets are present, but the exact Spine Unity runtime package/source needs verification before listing it as a dependency.
- **Localization:** custom CSV parser/catalog with `LocalizedText` components; not Unity Localization package.
- **Persistence:** custom JSON-like serialization through `MiniJson`; AES encryption, PBKDF2 key derivation, HMAC-SHA256 verification, atomic dual-slot file storage under `Application.persistentDataPath`.
- **Testing:** Unity Test Framework 1.6.0; EditMode and PlayMode assemblies.
- **Data:** ScriptableObject catalogs for UI/audio/localization/animation; encrypted/obfuscated JSON level banks in Resources.
- **Mobile integration:** Android JNI/native vibration library; platform SDKs for ads/auth/analytics/push are not verified as installed.
- **Not present as project dependencies:** UniTask and Addressables.

## 8. GitHub Repository Review

| Path | Vấn đề | Recommended action |
|---|---|---|
| `Assets/_Project/Scripts/Core/Online/ApiConfig.cs` | Chứa `SignSecret` hard-coded cùng dev/prod endpoints. Dù có thể là dữ liệu từ source port, secret trong client/repository không thể được xem là bí mật và tạo rủi ro security/recruiter impression. | Rotate/revoke nếu từng dùng thật; chuyển secret signing về server/secure build configuration; scrub lịch sử Git nếu secret nhạy cảm. Không public repository trước khi xử lý. |
| `Library/` (~9.64 GB), `Logs/`, `UserSettings/` | Generated/local Unity data, đã được `.gitignore` ignore. Không nên commit; làm repo rất lớn và chứa state máy cá nhân. | Xác minh bằng `git ls-files` trên máy có Git; remove khỏi Git index/history nếu từng track. Không xóa local nếu vẫn cần cache. |
| `.git/` (~255 MB) | Repository metadata khá lớn so với source/assets (~66 MB), có thể do lịch sử chứa binary/generated assets. Không phải file để commit nhưng ảnh hưởng clone/push. | Dùng `git count-objects -vH` và audit large historical blobs trước khi public; cân nhắc Git LFS/filter-repo sau backup nếu cần. |
| `Assets/_Recovery/0.unity`, `Assets/_Recovery/0 (1).unity` | Unity recovery scenes thường là crash/recovery artifacts, dễ làm recruiter hiểu nhầm và tăng clutter. | Mở/đối chiếu trong Unity; nếu không chứa work duy nhất, loại khỏi repository trong một thay đổi riêng. |
| `Reports/Gemini/ReportGen.exe`, `ReportR0Gen.exe` | Compiled helper executables trong documentation/report area khó audit và không cần cho người đọc portfolio. | Giữ source generator nếu thật sự cần; bỏ binaries khỏi public repo và thêm build instructions/checksum nếu phải phân phối. |
| `Reports/Gemini/temp_*.txt`, `src_sprites.txt`, `dest_sprites.txt` | Intermediate audit dumps làm repository noisy. | Chuyển artifact hữu ích thành một report sạch; bỏ temp files khỏi public branch. |
| `Reports/` (172 files), `PORTING_ROADMAP.md` (~154 KB), `PORTFOLIO_FINISH_ROADMAP.md` | Khối lượng AI/port reports áp đảo code-facing documentation và có nhiều trạng thái lịch sử có thể đã stale. | Archive vào release/internal branch hoặc rút gọn thành `Docs/Architecture.md`, `Docs/Testing.md`, `Docs/AI_USAGE.md`; chỉ giữ report có giá trị provenance rõ. |
| `02_Home.png`, `31_Streak.png` ở repository root | Screenshots có tên số và nằm rời rạc, không tạo gallery dễ hiểu. | Chuyển sang `Docs/Media/` với tên mô tả và dùng trong README sau khi kiểm tra quyền asset. |
| `Assets/_Project/Fonts/*.ttf.import`, `Sprites/**/*.png.import`, `*.svg.import` và `.meta` tương ứng | Nhiều source-side import artifacts ngoài chuẩn Unity `.meta`; có thể cần cho pipeline port, nhưng mục đích không rõ với recruiter. | Xác minh tool/pipeline nào dùng `.import`; nếu không dùng runtime/editor, archive khỏi public portfolio branch. Không xóa `.meta` của asset Unity được giữ. |
| `Assets/Plugins/Demigiant/DOTween/` | Vendored DLL/plugin phải có license phù hợp để public redistribution; binary làm diff/review khó hơn. | Xác minh DOTween license/version và ghi attribution; cân nhắc documented install/package approach nếu license/workflow phù hợp. |
| `Assets/_Project/Animations/Spine/` và art/audio/font assets | Ownership/license không thể suy ra từ repository. Đây là rủi ro quan trọng khi public portfolio. | Lập `THIRD_PARTY_NOTICES.md`/asset credits và chỉ public asset có quyền redistribution. **NEEDS USER INPUT**. |
| Root `README.md` | Không tồn tại ở root trong snapshot được inspect. Recruiter thiếu entry point, setup, screenshots, controls và contribution disclosure. | Tạo README sau human review bằng source material ở mục 9; chưa tạo trong task này. |
| `LICENSE` | Không thấy license ở root. | Chọn license chỉ sau khi xác minh quyền đối với code và toàn bộ asset; proprietary/all-rights-reserved có thể phù hợp hơn open source. |
| `.github/` | Không thấy issue/PR templates hoặc CI workflow. | Optional: thêm CI/test instructions sau khi có headless Unity license strategy; không bắt buộc cho intern portfolio. |
| Repository status | Git executable không có trong môi trường review, nên không xác định chính xác generated/temp files nào đang tracked và worktree có dirty hay không. | Chạy `git status --short`, `git ls-files`, và large-file audit trước khi public. **Needs verification**. |

## 9. GitHub README Source Material

### Short Description

Meowdoku is a mobile-first Unity logic puzzle game that combines color-region Queens rules with polished feedback, daily challenges, streaks, progression, and offline meta systems.

### About

Meowdoku is a 2D logic puzzle game built in Unity for mobile. Players place cats so that every row, column, and colored region contains exactly one cat while avoiding adjacent diagonal conflicts. The project includes a complete app flow—from onboarding and responsive board interaction to results, progression, daily streaks, localization, profile customization, and a locally simulated rank activity—and was developed with substantial AI-assisted implementation.

### Key Features

- Queens-style row, column, color-region, and diagonal-adjacency puzzle rules.
- Tap, double-tap, and swipe controls designed for a responsive mobile board.
- Multi-strategy logical hints with contextual board highlighting.
- Score, combo, lives, tools, animated feedback, win, fail, retry, and revive flows.
- Main progression, multiple puzzle banks, and a browsable level-selection flow.
- Daily challenges, streak tracking, calendar rollover, and reward presentation.
- Interactive tutorial, animated How to Play pages, settings, and localized UI.
- Avatar/frame customization and an offline rank activity with simulated competitors.
- Resumable sessions and authenticated encrypted local saves.

### Technical Highlights

- Domain-oriented puzzle and session logic separated from Unity presentation components.
- Registry-driven UI navigation with cached pages, popup priority, layers, and input guards.
- Custom gesture pipeline with swipe axis/velocity protection and stroke interpolation.
- AES-encrypted, HMAC-authenticated dual-slot persistence with background writes.
- ScriptableObject catalogs and typed configuration groups for UI, audio, localization, and gameplay tuning.
- EditMode and PlayMode test assemblies covering gameplay, persistence, UI, and meta systems.
- Provider-neutral boundaries for optional ads, authentication, sync, tracking, and platform permissions; production SDK adapters are not included/verified.

### Tech Stack

- Unity 6000.3.19f1 and C#
- Universal Render Pipeline / Unity 2D Renderer
- Unity uGUI and Input System
- DOTween
- Unity Test Framework
- Custom CSV localization and encrypted file persistence
- Android JNI/native vibration support

### Project Structure

```text
Assets/_Project/
├── Scripts/Core/       # Puzzle domain, state, persistence, config and app services
├── Scripts/Gameplay/   # Gameplay orchestration, presenters, views and input
├── Scripts/Services/   # Audio runtime and contracts
├── Prefabs/UI/         # App pages, popups and reusable UI views
├── Resources/Levels/   # Puzzle bank data
├── Scenes/             # App and supporting scenes
├── Settings/           # ScriptableObject catalogs
├── Localization/       # Translation source
└── Tests/              # EditMode and PlayMode tests
```

### Requirements

- Unity Editor **6000.3.19f1**.
- Android Build Support for Android builds.
- DOTween assets are currently included in `Assets/Plugins/Demigiant/DOTween/`.
- Any Spine runtime/import requirement: **Needs verification**.

### Platform

- Android (verified project target; minimum API 25, ARM64).
- iOS support/build status: **Needs verification**.

### Screenshots / GIF Suggestions

1. Home screen showing the main level CTA plus Daily, Streak, Rank, and Profile entries.
2. A clean mid-game 8×8 board showing colored regions, marks, placed cats, rule bar, score, lives, and tools.
3. A short GIF of tap-to-place plus swipe-to-mark input and the board’s animated response.
4. A wrong-placement moment showing conflict cells, rule highlight, life loss, and cat feedback.
5. A hint sequence showing contextual cell/unit highlighting and the applied move.
6. A high-combo moment with score flight, multiplier, and cat burst effects.
7. The win page with score/stat roll-up and the Next Level CTA.
8. The Daily Challenge entry and result state followed by the streak calendar/reward flow.
9. The rank activity page showing podium, player row, countdown, and reward chest.
10. Profile customization with avatar/frame selection.
11. The interactive tutorial or How to Play animated board.
12. Settings and language selection, ideally demonstrating an actual locale change.

### Demo Section

Sau này nên cung cấp:

- A 60–90 second gameplay showcase video (YouTube or an equivalent stable host).
- A versioned Android APK or itch.io release if redistribution rights and device QA are confirmed.
- A short technical walkthrough or architecture diagram link if a recruiter wants implementation detail.
- Optional test evidence: a screenshot/export of passing EditMode and PlayMode runs.

Không có URL nào được xác minh trong repository hiện tại.

## 10. GitHub Repository Metadata

### Description

A mobile-first Unity logic puzzle featuring color-region Queens gameplay, daily streaks, progression, localized UI, and polished feedback.

### Topics

```text
unity
csharp
unity2d
android
mobile-game
puzzle-game
logic-puzzle
queens-puzzle
dotween
unity-input-system
localization
game-development
```

Không dùng topics `multiplayer`, `online-game`, `live-service`, `ios` hoặc `released-game` khi chưa có bằng chứng bổ sung.

## 11. CV Evidence

> Đây là evidence để human review, chưa phải CV wording cuối cùng. Contribution split và mức độ AI assistance phải được nói rõ, không ngầm nhận toàn bộ implementation là manual coding.

| Accomplishment | Evidence | Relevant scripts/systems | CV value |
|---|---|---|---|
| Hoàn thiện một vertical slice mobile puzzle có app flow rộng | Build scene nối splash, tutorial, home, gameplay, result và nhiều meta pages qua 33 UI registry entries. | `AppScene.unity`, `UIRegistry.asset`, `AppBootstrap.cs` | Cho thấy khả năng đưa nhiều subsystem thành một sản phẩm có flow nhất quán. |
| Xây dựng/tích hợp luật puzzle độc lập presentation | Rule/conflict/completion nằm trong domain classes thay vì MonoBehaviour UI. | `QueendokuCore.cs`, `GameSession.cs` | Evidence tốt cho state modeling, separation of concerns và testability. |
| Tích hợp hint solver nhiều chiến lược | Hint result chứa strategy, unit, region, highlight và contradiction chain. | `HintEngine.cs`, hint presentation system | Thể hiện xử lý thuật toán và chuyển domain data sang UX. |
| Thiết kế mobile interaction có guard | Custom tap/double-tap/swipe recognizers, direction/velocity protection và board interpolation. | `Gameplay/Input/`, `BoardView.cs` | Relevant trực tiếp cho Unity mobile gameplay/input. |
| Tạo persistence bền vững cho state phức tạp | AES/HMAC dual-slot atomic save, legacy migration, background write và snapshot resume. | `SaveStore.cs`, `GameStateRepository.cs`, `GameSessionSnapshot.cs` | Mạnh hơn claim chung “used PlayerPrefs”; cho thấy quan tâm durability/lifecycle. |
| Tổ chức UI theo registry/lifecycle | Cached/prefetched windows, layers, mask, popup priority, back/input guard và startup routing. | `Core/UI/`, `UIRegistry.asset` | Evidence về architecture và production-like UI lifecycle. |
| Tích hợp daily/streak/reward meta loop | Calendar rollover, streak states, idempotent award claim và daily result coordination. | `Core/Daily/`, streak/daily presenters | Cho thấy hiểu meta-game state và edge cases theo thời gian. |
| Tạo profile/rank experience hoạt động offline | Profile catalogs, robot-generated leaderboard, period/tier/reward presentation. | `Core/Profile/`, `Core/Robot/`, `Core/Rank/` | Thể hiện integration giữa data model, algorithms, persistence và presentation; phải ghi là simulated/local. |
| Xây dựng localization data pipeline | Custom CSV parser/catalog, alias/fallback, persistence và live UI refresh trên bảng 1.695 keys. | `Core/Localization/`, `translations.csv` | Evidence cho data-driven content và internationalization. |
| Tạo feedback layer bằng DOTween/audio/haptics | 42 runtime scripts dùng DOTween; centralized audio và Android vibration adapter. | gameplay feedback views, `Scripts/Services/`, `VibrationService.cs` | Thể hiện game feel, polish và mobile platform awareness. |
| Duy trì testable boundaries | 59 EditMode/PlayMode test scripts và null/offline providers cho optional SDKs. | `Assets/_Project/Tests/`, Ads/Online/Tracking/Platform contracts | Cho thấy tư duy dependency boundary và verification; không ghi pass count cho tới khi chạy lại. |

Các accomplishment cần attribution an toàn, ví dụ mô tả “developed and integrated with substantial AI assistance” hoặc breakdown cụ thể sau khi người dùng cung cấp contribution data.

## 12. Gameplay Portfolio Plan

### 75–85 second showcase

| Timeline | Nội dung | Caption tiếng Anh đề xuất |
|---|---|---|
| 0:00–0:04 | Logo/title rồi cắt nhanh vào Home screen hoàn chỉnh. | `Meowdoku — A Mobile Logic Puzzle Built in Unity` |
| 0:04–0:10 | Home: level CTA, daily, streak, rank và profile entries; tap Play. | `A complete mobile-first app flow` |
| 0:10–0:18 | Board intro; đặt một mèo đúng, swipe qua nhiều ô để mark, double-tap một cell. | `Tap, double-tap, and swipe controls` |
| 0:18–0:25 | Cố ý đặt sai để hiện diagonal/row/region conflict, rule bar và mất life. | `Clear rule feedback and mistake handling` |
| 0:25–0:33 | Dùng Hint, quay cận cảnh contextual highlight rồi apply hint. | `Multi-strategy logical hints` |
| 0:33–0:41 | Chuỗi correct moves tạo combo, score flight, multiplier và cat burst. | `Scoring, combos, audio, and animated feedback` |
| 0:41–0:48 | Hoàn tất board; win toast/page, stats roll-up và Next Level. | `Progression with polished result flows` |
| 0:48–0:57 | Daily entry → một cut gameplay → daily result/streak calendar/reward. | `Daily challenges and multi-day streaks` |
| 0:57–1:06 | Rank activity: podium/list/player row, rank-change hoặc reward chest animation. | `Offline rank activity and rewards` |
| 1:06–1:13 | Profile avatar/frame selection rồi trở lại Home để thấy selection cập nhật. | `Persistent profile customization` |
| 1:13–1:19 | Settings/language: đổi locale và cho text refresh trực tiếp. | `Data-driven localization` |
| 1:19–1:24 | Montage nhanh Tutorial/How to Play + outro title/engine/platform. | `Unity 6 • C# • Android` |

### Capture notes

- Quay portrait ở cùng resolution/aspect; không trộn footage Editor với device chrome trừ khi cố ý.
- Bật âm thanh game vừa đủ; không cần voice narration. Dùng music nhẹ và sound effects thật của game.
- Caption chỉ 3–6 từ/ý, giữ màn hình đủ lâu để recruiter đọc nhưng không che board.
- Nếu có frame drop, input latency hoặc glyph lỗi trên device thì sửa/QA trước khi quay; code presence không thay thế device validation.
- Không đưa ads/auth/cloud sync vào video vì production adapters chưa được xác minh.

## 13. Missing Information

Mọi mục sau đều **NEEDS USER INPUT** hoặc cần validation ngoài static repository:

- **NEEDS USER INPUT — Development duration:** ngày bắt đầu, tổng thời gian và thời lượng active development.
- **NEEDS USER INPUT — Contribution split:** phần nào do bạn tự thiết kế/implement/review/debug và phần nào do AI/Codex tạo; mức độ human verification cho từng subsystem.
- **NEEDS USER INPUT — Project origin/port context:** repository cho thấy dấu vết port từ một Godot source; cần quyền sở hữu và cách mô tả nguồn hợp pháp/chính xác.
- **NEEDS USER INPUT — Asset ownership and licenses:** sprites, audio, fonts, Spine data, DOTween và mọi third-party/source-derived asset có quyền public redistribution hay không.
- **NEEDS USER INPUT — Team size/roles:** solo, team project, client work hay adaptation; contribution của người khác.
- **NEEDS USER INPUT — Release status:** prototype, portfolio build, internal beta, published hay unpublished.
- **NEEDS USER INPUT — Device QA:** model Android/iOS đã test, OS versions, performance/FPS, memory và aspect/notch matrix thực tế.
- **NEEDS USER INPUT — External playtesting:** số người test, feedback và thay đổi đã thực hiện.
- **NEEDS USER INPUT — Metrics/ratings:** retention, session length, store rating, downloads hoặc completion data; không có bằng chứng trong repo.
- **NEEDS USER INPUT — Production services:** ads, consent, ATT, push, authentication, analytics và cloud sync adapters/credentials có tồn tại ở private build khác không.
- **NEEDS USER INPUT — iOS status:** đã build/sign/test iOS hay chỉ có conditional code.
- **NEEDS USER INPUT — Test status:** ngày chạy gần nhất, số tests pass/fail và platform; cần chạy Unity Test Runner lại.
- **NEEDS USER INPUT — Build/demo links:** YouTube/gameplay video URL, Android APK/AAB/itch.io URL và source repository URL.
- **NEEDS USER INPUT — Commercial plans:** portfolio-only, open source, free release hay commercial intent.
- **NEEDS USER INPUT — Repository visibility/license:** public/private và license dự kiến sau khi audit asset rights/secrets.
- **NEEDS USER INPUT — Spine dependency:** runtime/importer version và quyền phân phối, vì `.skel`/`.atlas` assets có mặt nhưng package không được xác định rõ trong `Packages/manifest.json`.
- **NEEDS USER INPUT — Current branch/tracked files:** Git CLI không có trong môi trường review nên chưa xác minh worktree status, ignored artifacts đã từng được track hay large blobs trong history.

---

### Evidence boundary

Tài liệu này chỉ khẳng định những gì source và serialized project assets hiện tại hỗ trợ. Các hệ thống optional có interface/runtime component nhưng dùng null/offline provider không được xem là user-facing online feature. Các số liệu về chất lượng, hiệu năng, test pass rate, phát hành và đóng góp cá nhân phải chờ bằng chứng hoặc xác nhận của người dùng.
