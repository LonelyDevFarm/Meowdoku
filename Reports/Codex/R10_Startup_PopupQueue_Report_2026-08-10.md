# R10 Startup và Popup Queue

Ngày: 2026-08-10  
Trạng thái: domain/runtime contract compile sạch; chưa gắn scene vì Splash/Home/Tutorial prefab chưa được port.

## Nguồn đã kiểm chứng

- `scripts/module/ui/panel/launcher.gd`
- `scripts/module/ui/queue/ui_popup_entry.gd`
- `scripts/module/ui/queue/ui_popup_queue.gd`
- `scripts/module/home/view/home_page.gd`
- `scripts/module/ab_switch_popup/view/ab_switch_popup.gd`
- `scripts/module/game_state/game_state.gd`
- `assets/cfg/dialog_priority_strategy.json`
- `assets/cfg/ab_switch_popup_strategy.json`

## Hiệu chỉnh báo cáo Gemini 013

1. Launcher thực tế xử lý privacy và push permission trước `_wait_cmp_then_att_max_2s()` và remote defaults; bảng tóm tắt của report đã đổi thứ tự.
2. `dialog_priority_strategy.json` là một mảng bốn entry, không phải một object đơn.
3. `CanExceedLimit` có trong JSON nhưng `_build_popup_queue()` hiện không đọc field này; Unity chỉ parse, không tự tạo luật limit.
4. First-session và tutorial đều nằm trong dual-slot `GameState`, không dùng PlayerPrefs.

## Phần đã port

- `UIPopupEntry` và `UIPopupQueue`: priority cao chạy trước, stable khi bằng nhau, insert-next, cancel theo key, clear, re-entry guard và await từng handler.
- `UIPopupConfig`: parse priority JSON, AB trigger DSL có list `{...}`, parameter DSL có nested reward dictionary.
- Explicit handler map thay Godot `has_method/Callable`; không dùng reflection.
- `UIManager.AwaitHidden` để popup handler chờ đóng đúng lifecycle.
- `GameStateData.IsFirstSession` và runtime split: persistent flag chuyển false trong launcher nhưng vẫn true suốt first runtime, đúng source.
- `AppBootstrap`: 60 FPS, keep-awake, state/session/locale hook, Splash, platform boundary, remote boundary, Game/board/bank prewarm, sync boundary, splash timing và Tutorial/Home route.
- `IAppStartupExternalServices` cùng `OfflineStartupExternalServices`: privacy/ATT/push/remote/data-sync/shortcut không tồn tại sẽ no-op, không chặn offline.

## Timing và prewarm

- Splash chờ phần còn lại tới mốc 2 giây rồi thêm 0,5 giây; nếu startup đã quá 2 giây vẫn chờ thêm 0,5 giây.
- Android delay 1 giây trước UI Splash được giữ trong startup timer.
- Game prewarm được khởi chạy đồng thời như source, sau đó prewarm board size hiện tại và load rank regular/LK Style/GC.
- UI one-flight flag vẫn là điều kiện trước route; không giả Addressables khi package chưa có.

## Chưa gắn vào scene vì sao

Registry hiện chưa có Splash/Home/Tutorial prefab thật. Nếu bật AppBootstrap lúc này, nó chỉ tạo một startup lỗi hoặc buộc phải dựng page giả khác nguồn. Vì vậy component và contract đã sẵn sàng nhưng chỉ được installer gắn khi ba page được port ở R11/R12.

## Xác minh

- Core compile sạch bằng Unity Roslyn.
- Gameplay compile sạch sau thay đổi Core.
- EditMode test assembly compile sạch.
- Fixture mới: priority/stable order, insert/cancel, bốn JSON entry, trigger/parameter DSL, splash timing, Tutorial/Home route và first-session runtime/persist.
- Không thêm runtime debug log.

### Sửa lỗi import ngày 2026-08-10

Unity AssetDatabase chưa đưa `AppBootstrap.cs` vào Core response file trong lần Refresh đầu, khiến `UIPopupStartupTests` không tìm thấy `AppStartupContract`. Contract thuần đã được chuyển vào `UIContracts.cs` (file luôn thuộc Core assembly), còn component `AppBootstrap` giữ riêng phần scene/lifecycle. Core và toàn bộ EditMode test assembly đã compile sạch lại bằng đúng response files của Unity; GUID chưa có serialized reference của `AppBootstrap.cs` được làm mới để AssetDatabase import lại file ở lần Refresh kế tiếp.

## Checklist gộp cho lần test sau

Chưa cần test riêng lúc này. Sau khi Splash/Tutorial/Home được nối:

1. Save mới route Tutorial; save có `tutorial_done=true` route Home.
2. Splash tồn tại ít nhất 2,5 giây ở startup nhanh và thêm 0,5 giây ở startup chậm.
3. Nhấn Start sau Home không stall vì Game/board/bank đã prewarm.
4. Bốn Home popup chạy đúng priority 10012→10009 và chờ popup trước đóng.
5. Offline/mất mạng không làm startup đứng.
6. Warm/show trùng Game không tạo instance thứ hai.
