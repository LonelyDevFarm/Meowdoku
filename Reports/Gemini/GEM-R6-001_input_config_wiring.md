# GEM-R6-001 Báo cáo Phân tích Input Config Wiring và Gesture

**STATUS**: COMPLETE  
**GENERATED_AT**: 2026-08-08 16:55:00  
**SOURCE_ROOT**: `D:\Projects\_GameExtract\Main_Meokdoku`

## 1. Event Flow (Pointer Events to Gesture Recognizer)

| Bước | File:dòng | Input | State change | Output / Callee |
|---|---|---|---|---|
| **Press** | `board_view.gd:1048-1060` | `InputEventMouseButton` (pressed=true) | `_dragging = true` | `cell_drag_start.emit(local_pos)` |
| **Move** | `board_view.gd:1066-1074` | `InputEventMouseMotion` | N/A (chỉ phát khi `_dragging == true`) | `cell_drag_over.emit(local_pos)` |
| **Release** | `board_view.gd:1058-1060` | `InputEventMouseButton` (pressed=false) | `_dragging = false` | `cell_drag_end.emit()` |
| **Recv Start** | `base_game_page.gd:2630` | `cell_drag_start(pos)` | Cập nhật logic màn chơi (reset hint) | Gọi `_gesture_recognizer.on_drag_start(pos)` |
| **Recv Over** | `base_game_page.gd:2648` | `cell_drag_over(pos)` | N/A | Gọi `_gesture_recognizer.on_drag_over(pos)` |
| **Recv End** | `base_game_page.gd:2657` | `cell_drag_end()` | `_drag_in_progress = false` | Gọi `_gesture_recognizer.on_drag_end()`, gọi `_commit_pending_x_marks()` |
| **Process** | `board_gesture_recognizer.gd:37,57,86` | Vector2 `pos` | Phân giải `pos` thành cell (r, c), nội suy các cell bị bỏ qua. | Sinh ra mảng `CellAction` (tap/paint) truyền lại cho `base_game_page` để render. |

## 2. Config Wiring (A/B Test Configurations)

| Config value | Điều kiện | Guard / Window result | Caller |
|---|---|---|---|
| `swipe_protect` (Các hệ số HOTZONE) | `n >= min_size()` (mặc định n>=0, RAISED thì n>=7) | Kích hoạt `SwipeAxisGuard`. Ngưỡng `threshold` = 4 (RAISED: `ceil(n*0.6)`). Dung sai (tolerance) = 10-50% của `CELL_PX`. | `SwipeGuardRecognizer._swipe_guard_enabled_for_level` |
| `swipe_protect: DYNAMIC_INTENT` | Kích hoạt guard VÀ (chuẩn bị tô X pending HOẶC target != EMPTY) | Kích hoạt `SwipeVelocityGate`. `window_ms` = 100, `threshold` = 1.2 px/ms. | `SwipeGuardRecognizer._configure_guard` |
| `doubletap_protect: SHORTEN` | Luôn luôn | Window = 0.25 giây | `base_game_page.gd:_double_tap_window_sec` |
| `doubletap_protect: BY_TRUTH` | `solution_has_cat(r, c) == true` | Có mèo thật -> 0.35s (LONG). Khác -> 0.25s (SHORT). | `base_game_page.gd:_double_tap_window_sec` |
| `doubletap_protect: BY_CONFLICT` | `would_cat_conflict(r, c) == true` | Vi phạm luật (xung đột) -> 0.25s (SHORT). Lặp luật -> 0.35s (LONG). | `base_game_page.gd:_double_tap_window_sec` |

## 3. Coordinate Contract (Hợp đồng Tọa độ)

| Giá trị | Trục / Đơn vị | Nơi tạo | Nơi dùng |
|---|---|---|---|
| `pos` | Pixel (Local to BoardView) | `make_input_local(mm).position` trong `board_view.gd` | Truyền xuống suốt chuỗi Recognizer. |
| `cell` | Tọa độ lưới (x = Cột, y = Hàng) | `_resolve_cell` hoặc `pointer_to_cell` | `c = cell.x`, `r = cell.y`. |
| `_slot` | Pixel (Kích thước ô + Spacing) | `board_view.get_grid_slot()` | Dùng trong `SwipeAxisGuard` để tính `_raw_cell` và `_overshoot`. |
| `_padding` | Pixel (Đệm xung quanh lưới) | `board_view.get_grid_padding()` | Bù trừ tọa độ (offset) trước khi chia cho `_slot`. |
| `_tol_px` | Pixel | `tolerance_pct * CELL_PX` (VD: 0.4 * 123px) | Ngưỡng văng khỏi quỹ đạo trong `_process_locked`. |

## 4. Pending Tap Lifecycle (Vòng đời của Tap chờ)

- **Flush / Cancel Single Tap (Double tap hiện X rồi mèo)**:
  1. Khi người chơi chạm ô lần đầu (`on_drag_start`), `tap_op` tạo action đánh X mờ (Pending state = true). Action này được gửi lên UI render ngay lập tức.
  2. `_open_double_tap_window(r, c)` được gọi, lưu `_last_tap_cell` và mở một `SceneTree Timer` đếm lùi (0.25s hoặc 0.35s).
  3. **Trường hợp hủy (Cancel pending)**: Nếu chạm lần 2 vào ĐÚNG ô đó trước khi Timer nổ, `on_drag_start` thấy `_last_tap_cell == (r, c)`, nó sẽ gán `_last_tap_cell = (-1, -1)`, reset stroke và phát lệnh `double_tap_op.on_double_tap` (Đặt mèo thật, đè lên X).
  4. **Trường hợp chốt (Flush single)**: Nếu timer nổ, biến nhớ `_last_tap_cell` bị xóa rỗng. Sau đó khi user buông tay (`on_drag_end`), hàm `_commit_pending_x_marks()` ở `base_game_page` sẽ chốt vĩnh viễn các ô X mờ thành X thật.

## 5. Parity obligations (Giải quyết 3 lỗi đã biết)

1. **"Ô bên phải tự bật X khi kéo"**:
   - Khắc phục bằng **SwipeAxisGuard**. Khi đếm (`_run_count`) di chuyển thẳng hàng vượt ngưỡng (`threshold`), trục đó sẽ bị khóa (`_axis = Axis.ROW`).
   - Hàm `_process_locked` sau đó bóp méo giá trị raw: Bỏ qua hoàn toàn trục Y thật (trừ phi độ văng `_overshoot_1d` > dung sai `_tol_px`), ép cứng tọa độ cell vào cái hàng/cột đã khóa (`return Vector2i(col, _lock_value)`).
2. **"Double tap hiện X rồi mèo"**:
   - Lỗi này xuất phát từ việc lần chạm 1 phát ra thao tác X ngay lập tức. Giải quyết bằng Timer DoubleTap + Pending Target (đã phân tích ở Mục 4). 
3. **"Layout/cell size làm sai hit test"**:
   - `_raw_cell` trong guard không dùng chung viewport raw pixels mà dùng `px, py` đã quy đổi qua `make_input_local(mm).position` (local của UI Control).
   - Nó cẩn thận trừ `_padding` và chia đều cho `_slot` thay vì `_cell` để xử lý luôn cả vùng khoảng trống (spacing) giữa các cell, khiến ngón tay không bị trượt khi đi ngang rãnh lưới.

## 6. Điểm chưa xác định

- Việc nhận diện **Dynamic Intent (Vuốt nhanh đổi hướng)** thông qua `SwipeVelocityGate` yêu cầu mẫu tốc độ >= 1.2 px/ms. Tuy nhiên, hành vi "bẻ khóa" (Unlock) khi vượt quá tốc độ và dung sai (overshoot) chỉ đơn thuần gọi hàm `_release`, thả khóa trục quay về trạng thái `Axis.NONE`, nhưng ngay sau đó lại bắt đầu một `_advance_run` mới. Không rõ sự trơn tru khi bẻ góc (cornering) 90 độ có nhạy trong thực tế hay bị giật 1 frame do `_run_count` bị reset về 1.
- Signal `cell_drag_tick(pos)` được nhắc đến trong hàm `on_drag_tick` của Guard, nhưng file `board_view.gd` không thấy emit tick mỗi khung hình, có thể nó được gọi bởi Timer hoặc `_process` ở đâu đó ngoài UI event.
