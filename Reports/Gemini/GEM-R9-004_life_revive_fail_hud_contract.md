# GEM-R9-004 Đặc tả Contract Life, Revive & Fail HUD

**STATUS**: COMPLETE  
**GENERATED_AT**: 2026-08-09 02:22:00  
**SOURCE_ROOT**: `D:\Projects\_GameExtract\Main_Meokdoku`

---

## 1. Thành phần Cấu trúc HUD (Mạng/Life)

**Nguồn Asset & Script:**
*   **Scene**: `assets/game/ui/compont/fish_slot.tscn` (Chứa ảnh tim màu và tim xám nứt).
*   **Script**: `life_slot.gd`.
*   **Top UI**: `game_page.gd` / `base_game_page.gd` khai báo 3 node `_heart1`, `_heart2`, `_heart3` thuộc kiểu `LifeSlot`.

**Trạng thái hình ảnh (LifeSlot):**
Có 2 cụm `AnimationPlayer` chịu trách nhiệm:
1.  `AnimationPlayer` (Cơ bản): 
    *   `RESET`: Tim đầy đủ.
    *   `Appear`: Hiệu ứng MẤT TIM (Duration 0.8s) - tim đỏ vỡ/mất đi và hiện tim xám nứt.
    *   `Disappear`: Mất tim không có tiếng động/nhanh (Duration 0.3s).
2.  `AnimLifePlus`:
    *   `Revive`: Hiệu ứng bơm lại tim (Duration 0.5s).

**Khác biệt logic Model vs Presenter:**
*   Model chỉ lưu `_lives` (số lượng mạng tối đa, thường là 3) và `_mistake_count` (số lần sai).
*   Presenter tính toán `lost_index` = `_mistake_count - 1` để gọi Anim vỡ tim.

---

## 2. Chuỗi Phản Hồi Khi Mất Mạng (Wrong Guess)

Được kích hoạt tại hàm `base_game_page.gd:_on_wrong_guess()`.

**Thứ tự xử lý (Order of operations):**
1.  **Block Input tức thì**: Biến cờ `_wrong_guess_pending = true` được bật. Ngay lập tức, toàn bộ các hàm nhận input như `_on_board_cell_tapped`, `_on_board_cell_drag_over` sẽ bị chặn return sớm.
2.  **Cập nhật Model**: `_mistake_count += 1`, Combo bị reset về 0.
3.  **Visual Feedback (HUD)**:
    *   Gọi `_animate_heart_lost(lost_index)`. Trái tim thứ `lost_index` chạy Anim `Appear` (Vỡ tim 0.8s).
4.  **Board Feedback (Trên Bàn Cờ)**:
    *   Nếu chưa hết mạng: Kích hoạt `_play_wrong_guess_cat_feedback(r, c)` (Hiệu ứng Cat Hand gạch chéo X hoặc Blow Trumpet).
    *   Nếu hết mạng: Khóa Input Board 2.0s bằng `UIManager.block_input_briefly(self, 2.0)`.
5.  **Chờ Đợi (Timer)**: Game tạo một Timer **0.4 giây** để chờ hiệu ứng vỡ tim diễn ra trước khi kiểm tra chết hẳn.
6.  **Resolve (Hết Delay)**: Hết 0.4s, cờ `_wrong_guess_pending = false` tắt đi. Gọi `_update_remaining()` -> `_validate_board()`.

---

## 3. Game Over (Fail State)

Được xử lý tại `base_game_page.gd:_validate_board()` và `game_page.gd:game_fail()`.

**Điều kiện văng Game Over:**
*   Trong `_validate_board()`, nếu `_mistake_count >= _lives`, hàm `game_fail()` được kích hoạt.

**Trình tự Game Fail:**
1.  Bật cờ `_is_complete = true`. Khóa mọi Input mãi mãi (cho đến khi Revive hoặc Restart).
2.  Gửi tham số tracking Analytics (`_build_game_end_params`).
3.  Tương tác Board: Mèo khóc (`_board_view.play_cat_cry_loop_all()`).
4.  Rung: Rung điện thoại cường độ mạnh (`VibrateManager.Level.LEVEL4`).
5.  Hiển thị UI: Gọi `UIManager.show_ui(UiName.FAIL)` và kết nối tín hiệu lắng nghe `revive_requested` / `revive_ad_started`.

---

## 4. Hồi Sinh (Revive)

Được xử lý tại `game_page.gd:_on_revive_requested()`.

1.  **Đóng Popup**: Tắt bảng Fail UI.
2.  **Cập nhật Data**: 
    *   `_is_complete = false` (Mở lại input).
    *   `_revive_count += 1`.
    *   Ghi nhận DDA (Độ khó động): `GameState.mark_dda_revive_used()`.
3.  **Bơm Mạng (HUD)**:
    *   `_lives` được cộng thêm số mạng (từ ABTest config, thường là +3), clamp tối đa 3.
    *   Gọi `_refresh_hearts()`. Với mỗi LifeSlot, gọi `slot.play_revive()` (chạy Anim bơm tim 0.5s).
4.  **Dọn Dẹp Bàn Cờ**: Gọi `_board_view.revive_all_cat_to_idle()` để xóa các ô đỏ báo lỗi và đưa mèo về trạng thái rảnh rỗi.
5.  **Persistence**: Ghi đè lại bản lưu Snapshot thông qua `_flush_endgame_snapshot()`.

---

## 5. Đề xuất Port sang Unity

**Phân tách Rõ Ràng Model và View (Presenter):**
*   **Trái Tim (LifeSlot)**: Nên dùng Unity UI `Animator` với 3 Trigger: `Alive`, `Die`, `Revive`. Dùng `Image` component. Không sử dụng AnimationPlayer kết hợp Tween Bezier phức tạp như Godot.
*   **Coroutines thay thế Timer**: Delay 0.4s ở bước Wrong Guess cần viết gọn trong một `IEnumerator WrongGuessFlow()` sử dụng `yield return new WaitForSeconds(0.4f);`.
*   **Chống Spam (Idempotency)**: Đảm bảo biến `_wrong_guess_pending` và `_is_complete` bọc toàn bộ khối raycast/pointer event của bảng cờ để tránh lỗi người dùng quẹt nhanh đẻ ra nhiều lỗi khi tim đang vỡ.
*   **Revive Logic**: Bóc tách hàm phục hồi cờ thành `BoardView.ClearAllErrors()`, không trộn lẫn logic giao diện và logic State của lưới.

*(Kết luận được đối chiếu trực tiếp từ `base_game_page.gd` và `game_page.gd`)*
