# Báo cáo đặc tả kỹ thuật: GEM-R11-016 (Tutorial Completion, Startup Integration & Asset Map)

**Nguồn đối chiếu:** `D:\Projects\_GameExtract\Main_Meokdoku`
**Mục tiêu:** Bản đồ tài nguyên (Asset Map), tích hợp Launcher và tiến trình dọn dẹp bộ nhớ (Cleanup) của Tutorial.

---

## 1. Bản đồ Tài nguyên (Asset & Dependency Map)
Dựa vào `ext_resource` được serialize trong file `tutorial_page.tscn`.

### A. Scripts & Prefabs
- **Main Script:** `res://scripts/module/tutorial/view/tutorial_page.gd`
- **Effect Script:** `res://scripts/module/ui/extensions/rich_text_breath.gd`
- **Board Prefab:** `res://assets/prefab/board.tscn`
- **Cell Prefab:** `res://assets/prefab/cell.tscn`
- **Button Prefab:** `res://assets/prefab/btn_with_tag.tscn`

### B. Texture / Sprite
- **UI Nhỏ:** `res://assets/sprites/common/success_check_circle.png`, `success_check_mark.png`
- **Icon Gợi Ý:** `res://assets/sprites/game/icon_hint_lamp.png`
- **Hạt & Lân tinh (Confetti/Fireworks):** 
  - `et_star_2.png`
  - `et_glow_003.png`
  - `et_line_001.png`
  - `et_ribbon_001.png`
- **Khung (Mask):** `et_mask_010.png`, `et_mask_009.png`, `et_mask_008.png`, `et_mask_007.png`.

### C. Spine & Font
- **Bàn Tay (Hand Spine):** `res://assets/effect/texture/ui_guide/ui_guide_hand.tres` (Kèm Atlas `.png` tương ứng).
- **Font:** `res://assets/fonts/Roboto-medium.tres`

### D. Localization Keys
Các chuỗi ngôn ngữ được inject qua hàm `tr()`:
`TUTORIAL_STEP1_RICH`, `TUTORIAL_STEP1_HIGHLIGHT`, `TUTORIAL_STEP1_ONE_PER_COLOR`, `TUTORIAL_STEP2_RICH`, `TUTORIAL_GOT_IT`, `TUTORIAL_STEP3_RICH`, `TUTORIAL_STEP3_RICH_DIAGONAL`, `TUTORIAL_STEP4_COLOR_RICH`, `TUTORIAL_STEP4_PINK_RICH`, `TUTORIAL_STEP4_BLUE_RICH`, `TUTORIAL_STEP5_RICH`, `TUTORIAL_SUB_EXCLUDE`, `TUTORIAL_SUB_SWIPE_EXCLUDE`, `TUTORIAL_STEP6_RICH`, `TUTORIAL_START_GAME`, `TUTORIAL_LAST_ONE_RICH`, `TUTORIAL_STEP7_HINT`, `TUTORIAL_STEP7_ROW_BLUE`, `TUTORIAL_STEP7_ROW_PINK`, `TUTORIAL_STEP7_PLACE_LAST`, `TUTORIAL_IQ_FORMAT`.

---

## 2. Trình tự Completion & Chuyển giao sang Game Level 1

Khi người chơi bấm nút **TUTORIAL_START_GAME**, chuỗi hành vi sau được kích hoạt (theo hàm `complete_tutorial()`):
1. Tính thời gian chơi (tính từ `_guide_start_ms`).
2. Gọi `Tracker.track_new_guide_end(1, time_sec)` đẩy log analytics.
3. Chốt sổ trạng thái Offline: Gọi `GameState.set_tutorial_done(true)` (Điều này lưu INI file vào ổ cứng thiết bị, đánh dấu đã hoàn thành).
4. Khởi động màn Game: Gọi `UIManager.show_ui(UiName.GAME, {"level_index": 1})`. Cơ chế `show_ui` đẩy màn Game lên đầu Stack. Vì Window của màn Game có config `show_mask = true`, `ui_manager.gd` sẽ trải lớp `ColorRect` tên `_GlobalMask` ra chặn toàn bộ `Mouse_Filter_Stop`. Do đó, input bị khóa tạm thời trong lúc màn hình chuyển giao.
5. Ẩn màn Tutorial: `UIManager.hide_ui(UiName.TUTORIAL)` đẩy Tutorial vào chu trình fade out / hide.

---

## 3. Tích hợp Launcher & Startup
Cách Tutorial được khởi động từ lúc bật App:
- **Prewarm:** Tại hàm `_prewarm_game()` của Launcher, UI màn `GAME` được Prewarm ngầm bằng `warm_pool_async` để khi User xong Tutorial bấm "Start", màn Game hiện ra tức thì mà không lag. (Tutorial không được Prewarm, nó sinh ra ngay lần đầu do là màn hiển thị ngay).
- **Navigation Branch:** Tại `launcher.gd`, sau bước Wait CMP và AB Test, Engine check `if GameState.is_tutorial_done():`.
  - NẾU TRUE: `UIManager.show_ui(UiName.HOME)`.
  - NẾU FALSE: `UIManager.show_ui(UiName.TUTORIAL)`.
- Chốt bằng việc tắt Splash: `UIManager.hide_ui(UiName.SPLASH)`. Cấu trúc này chia nhánh (Branch) cực kỳ rõ ràng giữa Home và Tutorial.

---

## 4. Reset & Cleanup Logic (On Hide/Reopen)
Điểm độc đáo ở `tutorial_page.gd` là nó **không có hàm `on_hide()`**. Mọi thao tác dọn dẹp đều được thiết kế Lazy-reset và đập nát state cũ thông qua `on_show()` khi mở lại (nhằm tái tạo trạng thái sạch bong):
- **Tránh rò rỉ Async:** Tăng biến `_flow_token += 1`. Các Coroutine `await` cũ đang treo sẽ tự động `return` khi `_flow_token != _tok`.
- **Reset Board:** `_reset_ui()` giấu toàn bộ Panel, Message, Check, IQ Bar, Layer Mask.
- **Xóa Mask Temp:** Gọi `_clear_mask_hint_cells()` duyệt Dict `_mask_hint_cells` và `queue_free()` toàn bộ Cell Clone giả lập trên Mask Layer.
- **Tween Kill:** `_mask_tween.kill()` và `_swipe_hand_tween.kill()` (thông qua `_stop_swipe_hand_loop`) đảm bảo không có Tween bóng ma nào tiếp tục vặn vẹo UI.
- **Biến đệm:** `_drag_start_cell = (-1, -1)` dọn sạch state vuốt nhầm.

---

## 5. Bảng Evidence Trích Dẫn

| Tiêu chí | File Nguồn | Class/Hàm/Dòng/Đoạn mã | Mức chắc chắn |
| :--- | :--- | :--- | :--- |
| Assets Map | `tutorial_page.tscn` | Headers `ext_resource` (Dòng 1-19) | 100% |
| Complete Sequence | `tutorial_page.gd` | `complete_tutorial()` gọi `UIManager.show_ui` và `hide_ui` (Dòng 582) | 100% |
| Khóa Input Transition| `ui_manager.gd` | `_show_mask()` tạo `_mask.mouse_filter = Control.MOUSE_FILTER_STOP` chặn Input sau lưng (Dòng 482) | 100% |
| Launcher Check | `launcher.gd` | Nhánh `if GameState.is_tutorial_done(): show_ui(HOME) else: show_ui(TUTORIAL)` (Dòng 280) | 100% |
| Coroutine Token Guard| `tutorial_page.gd` | `_flow_token += 1` trong `on_show()` và check `if _flow_token != _tok: return` ở luồng async | 100% |
| Memory Mask Clear | `tutorial_page.gd` | `_clear_mask_hint_cells()` gọi `.queue_free()` trên dict values (Dòng 787) | 100% |

STATUS: COMPLETE
