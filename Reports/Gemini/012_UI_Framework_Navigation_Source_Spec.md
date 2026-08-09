# Báo cáo đặc tả kỹ thuật: GEM-R10-012 (UI Framework & Navigation Source Spec)

**Nguồn đối chiếu:** `D:\Projects\_GameExtract\Main_Meokdoku`
**Mục tiêu:** Đặc tả kiến trúc UI Registry, Layering, Cache, Caching, và luồng Navigation cơ bản (Launcher).

---

## 1. Entry / Autoload & Khởi tạo
- **Autoload:** `UIManager` được nạp tự động qua `project.godot` (Dòng 28: `UIManager="*res://scripts/module/ui/ui_manager.gd"`).
- **Thứ tự & Phụ thuộc:** `UIManager` khởi tạo `_registry` bằng cách gọi `UIRegistry.build_registry()`, và khởi tạo `events: UIEvents = UIEvents.new()`. Nó lắng nghe tín hiệu màn hình `get_viewport().size_changed` để chạy tính toán safe_area.

## 2. Registry Page & Popup
- **File config chính:** `scripts/module/ui/ui_registry.gd` và `scripts/module/ui/ui_name.gd`.
- **Cấu trúc:** Sử dụng Dictionary `PAGES` map từ schema key (`UiName.*` ví dụ `UiName.HOME`, `UiName.GAME`) sang đường dẫn `.tscn` cứng (VD: `"res://scripts/module/home/ui/home_page.tscn"`).
- **Phân loại nhánh (AB/Variant):** Trong `ui_registry.gd`, hàm `build_registry()` có logic tiêm thêm các dev/debug page tùy vào OS feature. Nó cũng móc nối thủ công màn hình WIN: `reg[UiName.WIN] = _resolve_win_path` để quyết định load bản G1 hay bản G4 tùy vào `ABTestManager.pass_page.is_g4()`.
- **Cache / Prewarm:** Các page được cache (lưu trữ Node instanced) vào dictionary `_cache` trong `UIManager`. Việc Prewarm được thực hiện thông qua `UIManager.warm_pool(ui_name)` hoặc `warm_pool_async(ui_name)`.

## 3. Hệ thống Layer & Render Order
- **Định nghĩa Layer:** Trong `scripts/module/ui/ui_layer_config.gd`.
- **Phân cấp:**
  - `LAYER_DEFAULT: 0` (Dành cho Home, Game)
  - `LAYER_POPUP: 100` (Dành cho Setting, Result...)
  - `LAYER_NOTICE: 200`
  - `LAYER_MODAL: 300`
  - `LAYER_TUTORIAL: 400`
  - `LAYER_LOADING: 500`
- **Render thứ tự (Z-Index):** `UIManager` có hằng số `Z_STEP = 50`. Khi `_assign_z_index` được gọi, window sẽ nhận `layer_base + (stack_size * Z_STEP)`.

## 4. Contract Điều hướng (Open/Close/Replace/Back)
Mọi window kế thừa từ `UIFrameWindow` (extends `UIBaseWindow`). Các contract chính trong `ui_manager.gd`:
- **Show (`show_ui` / `show_ui_async`):**
  - Nhận tên `ui_name` và dictionary `params`.
  - Gọi `_get_or_create()` để instanced hoặc tái sử dụng từ `_cache`. Ném node vào `current_scene`.
  - Nếu tái sử dụng, gọi `move_child(win, -1)` để đôn node lên cuối cây (hiển thị trên cùng).
  - Push vào từ điển stack `_stacks[layer].append(win)`.
  - Kích hoạt `win._do_show(params)` để chạy animation mở và emit tín hiệu `window_shown`.
- **Hide (`hide_ui`):**
  - Lấy instance từ cache, chuyển trạng thái sang `CLOSING`, play animation. 
  - Gọi `win._do_hide()`, gỡ khỏi stack `_stacks[layer].erase(win)`.
  - Emit tín hiệu `window_hidden`.
- **Cleanup:** 
  - `hide_ui` không phá hủy (destroy) node ngay mà giữ trong `_cache`. Node chỉ bị hủy khi `_evict_cached` được gọi, lúc này `queue_free()` mới chạy.
- **Escape / Back (trong `UIFrameWindow`):** Gọi hàm ảo `on_escape()`. Nếu có `_close_btn`, tự động emit `"pressed"`. Nếu định nghĩa hàm `_on_back_request`, gọi hàm này.

## 5. Popup Queue & Priority
- **Trách nhiệm:** `UIManager` KHÔNG quản lý popup queue. Stack `_stacks` của `UIManager` chỉ là mảng lưu các page đang mở theo layer để tính toán Z-Index, chứ không bắt ai phải đợi ai.
- **Quản lý thực sự:** Hệ thống queue nằm ở `scripts/module/ui/queue/ui_popup_queue.gd` (`UIPopupQueue`) và `ui_popup_entry.gd`.
- **Cách thức:** Danh sách queue `_queue: Array[UIPopupEntry]` được gọi thủ công tại các điểm ngắt (như sau khi thắng game, login...) để pop từng cái ra hiển thị.

## 6. Input Guard & Screen Mask
Thực thi tại `ui_manager.gd`:
- **Chống Double Open (Guard):** Theo dõi các nút bấm qua `_guard_held_buttons`. Nếu đang có cửa sổ mở hoặc nút bị đè, `_guard_active = true`. Hàm `_input()` toàn cục sẽ khóa mọi Input bằng `get_viewport().set_input_as_handled()` cho đến khi nhận được sự kiện `is_release`. Ngăn ngừa click xuyên thủng khi play animation mở UI.
- **Mask (Background tối):** Sử dụng chung một Node `ColorRect` tên là `_mask`. `UIManager` có biến đếm `_mask_ref_count`. Nếu nhiều UI cùng bật cờ `show_mask`, nó sẽ chèn `_mask` nằm dưới UI trên cùng `_restack_mask()`.

## 7. Async Load & Prewarm
- **Hàm `show_ui_async` và `_load_scene_async`:** 
  - Sử dụng `ResourceLoader.load_threaded_request`. 
  - Đánh dấu flag trong dict `_loading[ui_name] = true`.
  - Nếu có request song song cho cùng 1 ui_name, hàm sẽ đợi `while _loading.has(ui_name): await get_tree().process_frame` để tránh load 2 lần.
  - Sau khi load, ném vào `_cache`.

## 8. Luồng Điều hướng Khởi chạy & Vào Game (Flow)
Nằm tại `scripts/module/ui/panel/launcher.gd` (Script đính vào `launcher.tscn` - Main Scene của project) và `home_page.gd`:
1. **Show Splash:** `UIManager.show_ui(UiName.SPLASH)`.
2. **Setup Sub-systems:** Nạp log, Analytics, gọi kiểm tra AB.
3. **Blockers (Privacy & Push):** Đợi người dùng đồng ý tracking/push qua popup (`UIManager.show_ui(UiName.PRIVACY)` rồi await `accepted`).
4. **Prewarm:** Gọi `_prewarm_game()` -> `UIManager.warm_pool_async(UiName.GAME)`. Tải BankData ngầm.
5. **Sync Queue:** Chặn chờ `while UIManager.is_any_loading(): await get_tree().process_frame`.
6. **Navigate to Home/Tutorial:** 
   - Kiểm tra `GameState.is_tutorial_done()`.
   - Nếu xong -> `UIManager.show_ui(UiName.HOME)`.
   - Nếu chưa -> `UIManager.show_ui(UiName.TUTORIAL)`.
7. **Home to Gameplay:** 
   - Khi ấn nút Play ở `HomePage` (`_enter_main_level_covering()`), nó sẽ gọi `UIManager.show_ui(UiName.GAME, {"level_index": GameState.get_current_level()})`.
   - Ẩn Home: `UIManager.hide_ui(UiName.HOME)`. Hoặc clear stack với `UIManager.hide_all_except([UiName.HOME, UiName.GAME])` trước khi mở.

---

## 9. Bảng Evidence Trích Dẫn

| Tiêu chí | File Nguồn | Class/Hàm/Node/Line |
| :--- | :--- | :--- |
| Autoload UIManager | `project.godot` | Dòng 28 `UIManager="*res://..."` |
| UI Registry (Key-Path) | `scripts/module/ui/ui_registry.gd` | `const PAGES` và `UiName.*` (Dòng 6-39) |
| Caching Instance | `scripts/module/ui/ui_manager.gd` | `_cache` dict, `_get_or_create` (Dòng ~163) |
| Async / Threadead Load | `scripts/module/ui/ui_manager.gd` | `_load_scene_async`, `ResourceLoader.load_threaded_request` (Dòng ~265) |
| Layers & Z-Index | `scripts/module/ui/ui_layer_config.gd` | Hằng số `LAYER_POPUP`... (Dòng 4-9) |
| Push/Pop Stack | `scripts/module/ui/ui_manager.gd` | `_push_stack()`, `_pop_stack()` mảng `_stacks[layer]` |
| Input Guard | `scripts/module/ui/ui_manager.gd` | `_input(event)`, `set_input_as_handled()`, `_guard_active` |
| Mask System | `scripts/module/ui/ui_manager.gd` | `_mask_ref_count`, `_restack_mask()`, `_show_mask()` |
| Popup Queue Class | `scripts/module/ui/queue/ui_popup_queue.gd` | `class_name UIPopupQueue`, mảng `_queue` |
| Launcher Flow | `scripts/module/ui/panel/launcher.gd` | `_ready()`, `_prewarm_game()`, kiểm tra `is_tutorial_done()` |
| Home to Game | `scripts/module/home/view/home_page.gd` | Hàm `_enter_main_level_covering()` (Dòng 890) gọi `show_ui(UiName.GAME)` |

---

## 10. Checklist Chuyển đổi Unity (Dependency Order)
Để đảm bảo Port đúng cấu trúc theo thiết kế module hóa này của Godot, team Unity nên implement theo trình tự:
1. **Cấu hình & Tín hiệu (Tier 1):** Tạo `UILayerConfig` (enum), `UiName` (constants), và class quản lý Signal (Event bus).
2. **Registry & Base (Tier 2):** Định nghĩa `UIBaseWindow` (Lifecycle) và `UIFrameWindow` (Layer, Mask flag). Tạo `UIRegistry` (ScriptableObject hoặc tĩnh) ánh xạ tên sang path của Prefab.
3. **Core UIManager (Tier 3):** Xây dựng `UIManager` (MonoBehaviour/Singleton). Triển khai `ShowUI` (Instantiate/Addressables), Pool/Cache `Dictionary<string, UIFrameWindow>`, tính toán Order in Layer, Mask `Image`, Input Blocker (Raycast Target full màn).
4. **Popup Queue (Tier 4):** Triển khai lớp `UIPopupQueue` độc lập để quản lý danh sách chờ.
5. **Flow Khởi tạo (Tier 5):** Thiết lập `Launcher` đóng vai trò Scene mồi, gọi Splash, đợi Async/Addressables load Game Scene, sau đó check PlayerPrefs chuyển qua Home hoặc Tutorial.

STATUS: COMPLETE
