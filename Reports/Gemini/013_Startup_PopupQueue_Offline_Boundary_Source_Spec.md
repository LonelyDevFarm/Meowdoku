# Báo cáo đặc tả kỹ thuật: GEM-R10-013 (Startup & Popup Queue Boundary Source Spec)

**Nguồn đối chiếu:** `D:\Projects\_GameExtract\Main_Meokdoku`
**Mục tiêu:** Đặc tả quy trình Startup (Launcher), cấu hình Popup Queue và các ranh giới Offline/Online.

---

## 1. Trình tự Khởi tạo (Startup Flow từ Cold Start đến Home)
Nằm trong `scripts/module/ui/panel/launcher.gd`, hàm `_ready()` là entry point khởi chạy game.

| Thứ tự | Hành động (Code) | Phân loại | Mục đích & Chi tiết |
| :--- | :--- | :--- | :--- |
| 1 | `UIManager.show_ui(UiName.SPLASH)` | **Bắt buộc / Offline** | Kích hoạt ngay lập tức màn hình Splash để chặn người dùng. |
| 2 | `_hide_native_splash(500)` | Bắt buộc / Offline | Tắt màn hình Splash native (Android/iOS) để chuyển giao cho UI Godot. |
| 3 | `_wait_cmp_then_att_max_2s()` | SDK / Chặn UI | Đợi User đồng ý GDPR/Privacy (CMP) và iOS ATT tối đa 2s. Nếu không đồng ý sẽ block UI. Không bắt buộc khi port Offline. |
| 4 | `ABTestManager.await_remote_ready(2.0)` | SDK / Chặn UI | Đợi Remote Config trả về để áp dụng biến A/B test. Không bắt buộc khi Offline (fallback default). |
| 5 | `_register_daily_pushes()` | SDK / Nền | Đăng ký local push notification (12h, 20h) thông qua `UniKitManager`. Gọi ẩn trong nền. |
| 6 | `_prewarm_game()` | **Bắt buộc / Offline** | Chạy ngầm `UIManager.warm_pool_async(UiName.GAME)`. Buộc load Scene GAME và init BankData. |
| 7 | `DataSyncManager.await_startup_synced()`| SDK / Online | Đồng bộ dữ liệu Firebase Auth/Save. Bỏ qua nếu `AuthManager.is_available() == false`. |
| 8 | `_wait_splash_complete()` | UI Block | Ép màn Splash hiện tối thiểu 2 giây để che giấu quá trình tải dữ liệu bên dưới. |
| 9 | `while UIManager.is_any_loading()` | **Bắt buộc / Offline** | Ngăn điều hướng nếu Scene GAME hoặc UI khác chưa load xong vào Pool. |
| 10 | `show_ui(HOME)` hoặc `TUTORIAL` | **Bắt buộc / Offline** | Kiểm tra `GameState.is_tutorial_done()`. Nếu `true` vào Home, `false` vào Tutorial. |

---

## 2. Phân tích Popup Queue Config (JSON Schema & Lifecycle)

### A. Lifecycle Enqueue / Dequeue
- **File kích hoạt:** `scripts/module/home/view/home_page.gd`
- **Thời điểm (Enqueue):** Khi Home Page gọi hàm `_build_popup_queue()`, nó sẽ load file JSON config ưu tiên.
- **Ánh xạ hàm:** Hệ thống móc (bind) thủ công thông qua `Callable(self, "_show_%s" % key)`. Nếu có key là `ab_switch_popup`, nó sẽ tìm hàm `_show_ab_switch_popup()` trên Home Page.
- **Xả hàng (Dequeue):** Sau khi nạp xong, Home Page gọi `_popup_queue.flush()` (trong `ui_popup_queue.gd`) để Pop liên tục danh sách các popup.

### B. Cấu trúc JSON 1: Priority Queue
- **Đường dẫn gốc:** `assets/cfg/dialog_priority_strategy.json`
- **Schema:**
  ```json
  {
    "OpenScene": "home",              // Scene được phép bung popup
    "Priority": 10011,                // Mức ưu tiên sắp xếp trong Queue (Số lớn / nhỏ tuỳ logic so sánh sort)
    "Key": "ab_switch_popup",         // Key ánh xạ hàm
    "CanExceedLimit": 0               // Cờ cho phép xuất hiện bỏ qua limit show
  }
  ```

### C. Cấu trúc JSON 2: A/B Switch Popup
- **Đường dẫn gốc:** `assets/cfg/ab_switch_popup_strategy.json`
- **Mục đích:** Khi `_show_ab_switch_popup` (trong JSON 1) được kích hoạt, nó sẽ nạp JSON này để xác định popup thông báo tính năng mới (A/B Test Update).
- **Schema & DSL:**
  ```json
  {
    "Trigger": "trigger=abtest_switch,key=daily_streak,bf={3},af={1,2,4}",
    "Param": "title=DAILY_STREAK_MAJOR_UPDATE,body=DAILY_STREAK_SWITCH3_DESC..."
  }
  ```
  - **Trigger:** Viết theo DSL (Domain Specific Language) dạng chuỗi parse thủ công (Ví dụ: `bf={3}` nghĩa là trước đây ở group 3, `af={1,2,4}` nghĩa là hiện tại ở group 1,2 hoặc 4). Chịu trách nhiệm parse bằng `_parse_trigger_dsl(s: String)`.

---

## 3. Hệ thống lưu trạng thái (Tutorial & Offline Config)
- **Cơ chế đọc/ghi Tutorial:** 
  - Đọc: `GameState.is_tutorial_done()`
  - Ghi: `GameState.set_tutorial_done(value)` -> Gọi hàm nội bộ `_save_data()`
- **Động cơ lưu trữ dưới ngầm:** (Source: `scripts/module/game_state/game_state.gd` dòng `2200-2280` và các hằng số ở đầu file)
  - GameState **KHÔNG dùng** `PlayerPrefs`.
  - GameState sử dụng class `ConfigFile` (chuẩn file INI của Godot).
  - Nó dump các key (vd: `cfg.set_value("progress", "tutorial_done", _tutorial_done)`) và gọi `cfg.save(SAVE_PATH_A)`.
  - Vị trí thực tế trên thiết bị: `user://save_store/save_a.cfg` (và `save_b.cfg` để chống corrupt).

---

## 4. Prewarm UI & Xử lý Trùng lặp (Dedup)
- **Hành vi Bắt buộc:** `launcher.gd` gọi `UIManager.warm_pool_async(UiName.GAME)`. Đây là yêu cầu bắt buộc vì màn Game nặng, cần Instantiate/Prewarm ngay trong Splash, tránh giật lag lúc ấn nút Play từ Home.
- **Cơ chế tải ngầm & Chống Trùng Lặp (Dedup):**
  - **File:** `scripts/module/ui/ui_manager.gd`
  - **Cách hoạt động:** Khi gọi `_load_scene_async(ui_name)` (để load 1 `.tscn` chạy ngầm luồng Thread), UIManager lưu cờ `_loading[ui_name] = true`. 
  - **Chặn đụng độ:** Nếu cùng lúc có lệnh yêu cầu Load UI đó, hệ thống sẽ chặn ở đầu hàm `warm_pool_async`:
    ```gdscript
    if _cache.has(ui_name) or _loading.has(ui_name):
        return
    ```
    Hoặc trong `show_ui_async`: `while _loading.has(ui_name): await get_tree().process_frame`. Nó sẽ chờ tới khi luồng cũ chạy xong để lấy luôn kết quả từ `_cache` thay vì Load Scene lại.

---

## 5. Bảng Evidence Trích Dẫn

| Tiêu chí | File Nguồn | Class/Hàm/Dòng/Đoạn mã |
| :--- | :--- | :--- |
| Launcher Flow (Wait CMP) | `scripts/module/ui/panel/launcher.gd` | `_wait_cmp_then_att_max_2s()` (Dòng 268) |
| Launcher Flow (Prewarm) | `scripts/module/ui/panel/launcher.gd` | `_prewarm_game()` (Dòng 145) gọi `UIManager.warm_pool_async(UiName.GAME)` |
| Launcher Flow (Sync Queue) | `scripts/module/ui/panel/launcher.gd` | Hàm `_ready()`, vòng lặp `while UIManager.is_any_loading(): await get_tree().process_frame` |
| Popup Queue Build & Enqueue | `scripts/module/home/view/home_page.gd` | Hàm `_build_popup_queue()` (Dòng 178), đọc `_POPUP_CONFIG_PATH` |
| Popup Queue Flush | `scripts/module/home/view/home_page.gd` | `_popup_queue.flush()` cuối hàm `_build_popup_queue` |
| A/B Switch Popup Config | `scripts/module/home/view/home_page.gd` | Hàm `_show_ab_switch_popup()` (Dòng 239) đọc `_AB_SWITCH_POPUP_CONFIG_PATH` |
| A/B Trigger DSL Parser | `scripts/module/home/view/home_page.gd` | Hàm tĩnh `_parse_trigger_dsl(s: String)` (Dòng 222) |
| Tutorial State Get/Set | `scripts/module/game_state/game_state.gd` | Hàm `is_tutorial_done()` và `set_tutorial_done(value: bool)` (Dòng 460-465) |
| Config File Save | `scripts/module/game_state/game_state.gd` | Khởi tạo file `SAVE_PATH_A: = "user://save_store/save_a.cfg"`, hàm `_save_data()` dùng `ConfigFile.new()`, `cfg.set_value` và cuối cùng `cfg.save(path)` |
| Prewarm Async Load | `scripts/module/ui/ui_manager.gd` | Hàm `warm_pool_async(ui_name: String)` (Dòng 248) chặn bằng cờ `_loading.has(ui_name)` |

STATUS: COMPLETE
