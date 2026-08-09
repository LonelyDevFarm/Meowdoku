# GEM-R8-001 Báo cáo State Machine của Game Session

**STATUS**: COMPLETE  
**GENERATED_AT**: 2026-08-08 18:35:00  
**SOURCE_ROOT**: `D:\Projects\_GameExtract\Main_Meokdoku`

## 1. Trình Tự Khởi Tạo (Initialization Sequence)

Dựa trên `base_game_page.gd` (dòng 493-700) và `game_page.gd` (dòng 456-750), một session được dựng lên thông qua hàm `on_show(params)` theo trật tự chính xác sau:

1. **Resolve Entry Mode** (`game_page.gd:557`): Parse `params` để chọn `_setup_entry_normal`, `_setup_entry_debug` hoặc nạp snapshot.
2. **Setup State Cơ bản** (`game_page.gd:582`): Đặt lại `_lives = 3` (hoặc restore), `_mistake_count = 0`, `_is_complete = false`, `_wrong_guess_pending = false`.
3. **Super `on_show`** (`game_page.gd:467` -> `base_game_page.gd:493`): Nạp UI, khởi tạo Data Model (`_score_model.restore`), gọi `_init_session_state` (deserialize `StepHistory`).
4. **Board Setup** (`game_page.gd:671`): Gọi `_board_view.setup(size, regions, ...)` và gán `solution`.
5. **Prefill (Pre-cat)** (`game_page.gd:677`): Đặt các con mèo cho sẵn (nếu mức độ dễ).
6. **Restore Board** (`game_page.gd:680`): Apply toàn bộ thao tác từ `StepHistory` lên bàn cờ để tái tạo hình ảnh.
7. **Endgame Persist Hook** (`game_page.gd:688`): Kết nối tín hiệu `cell_state_changed` với hàm lưu snapshot ngầm (chỉ áp dụng cho normal entry).
8. **Tracker & Animation** (`game_page.gd:708`): Gửi API `track_game_start`, bắt đầu chạy Tween `Appear`. Cờ `_entry_anim_playing = true`.
9. **Input Enable** (`base_game_page.gd:2912`): Hàm `_on_appear_animation_finished` tháo cờ khóa, mở `MOUSE_FILTER_STOP` cho `_board_view` và cấp quyền chơi.

---

## 2. Các Cờ Khóa (Guard Flags) trong Quá trình Chơi

Bất kỳ thao tác Input nào (Double-tap, Hint, Undo) đều bị cản lại nếu dính một trong các Guards sau (`base_game_page.gd:2639` & `2767`):

- `_entry_anim_playing`: Khóa toàn bộ input khi UI đang bay vào lúc đầu game.
- `_is_complete`: Khóa khi game đã Win.
- `_wrong_guess_pending`: Khóa thao tác trong 0.4s – 2.0s kể từ lúc bấm sai để đợi hiệu ứng trừ điểm / mèo khóc kết thúc.
- `UIManager.block_input_briefly(self, 2.0)`: Khóa Hard-level của Engine khi hết mạng (chuẩn bị Game Over).
- `_hint_data.is_empty() == false`: Trạng thái mutex khi người chơi bấm nút Hint nhưng chưa chọn ô. (Phải apply Hint hoặc Cancel thì mới chơi tiếp được).
- `OS.has_feature("focus_out")` (App Focus, thông qua `_notification(NOTIFICATION_APPLICATION_PAUSED)`): Tạm dừng bộ đếm thời gian.

---

## 3. Bảng Transition State (Action -> Next State)

| Action | Điều Kiện (Guard) | Trạng Thái Tiếp Theo | Hậu quả (Side-Effect) |
| --- | --- | --- | --- |
| **Correct Cat** | Ô thuộc Solution | `playing` | Tăng Combo, Score, thêm vào `StepHistory`. |
| **Correct (Cuối)** | `QueendokuCore.is_complete` | `win` | Gọi `_on_win()`, `_is_complete = true`. |
| **Wrong Guess** | Ô không thuộc Solution | `wrong_pending` | Ô chuyển thành MARK (X), `_lives -= 1`, reset Combo, trừ điểm, màn hình rung. Đợi 0.4s rồi gỡ khóa. |
| **Wrong-to-Fail**| `_lives <= 0` | `game_over` | Khóa UI 2.0s. Gọi `_on_game_over()`. Hiện bảng Hồi Sinh. |
| **Revive** | User chọn Hồi Sinh ở bảng Fail | `playing` | `_lives` hồi về đầy (3), `_wrong_guess_pending = false`, chơi tiếp tục. |
| **Undo** | `StepHistory` có >= 1 step | `playing` | Gỡ trạng thái ô cuối cùng, khôi phục `before_state`. |
| **Auto-Complete**| Bàn cờ còn đúng 1 mèo | `win` | Tự động chạy chuỗi Animation đặt mèo cuối. Tương đương Win. |

---

## 4. Phân Định Kiến Trúc (Architecture Boundary)

Dữ liệu nào thuộc **GameSession thuần (Core)** để Codex có thể port:
- `_lives`, `_mistake_count`, `_puzzle` (solution, regions, seed), `_step_history`.
- Các hàm sinh/nhận/thẩm định toạ độ `(r, c)`.

Dữ liệu nào thuộc **Ngoại Vi (View/Service)** (R8/R9):
- `_like_hand_state` (Tracker cho tay Thumbs Up).
- `_entry_anim_playing` (Tween delay).
- Gọi API `Tracker.track_...`.
- Gọi API `GameState.save_endgame_snapshot`.
- `ABTestManager` cho Score và Goal Emphasis.

---

## 5. Fixtures Tối Thiểu để Viết Unit Test State Machine

Codex có thể sử dụng các Fixture sau để mock StateMachine C#:

### Fixture 1: New Game
- **State Trước**: Không có.
- **Action**: Gọi `Start(level=1, restore_data=null)`.
- **State Sau**: `lives=3`, `history=[]`, `wrong_pending=false`, `is_complete=false`.

### Fixture 2: Restore Game (Resume)
- **State Trước**: Database lưu `lives=1`, `history=[Step(0,0)]`.
- **Action**: Gọi `Start(level=1, restore_data=DB)`.
- **State Sau**: Board có 1 con mèo ở (0,0), `lives=1`, `wrong_pending=false`. Bàn cờ chạy `RestorePartialBoard`.

### Fixture 3: Wrong-to-Fail Transition
- **State Trước**: `lives=1`, `wrong_pending=false`.
- **Action**: Player Double-Tap ô X (Wrong Guess).
- **State Sau**: Ô biến thành MARK. `lives=0`, `wrong_pending=true`. Sau Delay(0.4s), `OnGameOver` được kích hoạt.

### Fixture 4: Win Transition
- **State Trước**: `board_count = size - 1` (Thiếu 1 ô).
- **Action**: Player Double-Tap ô Đúng cuối cùng.
- **State Sau**: `is_complete=true`. Khóa toàn bộ input. Chuyển sang bảng `OnWin`.

STATUS: COMPLETE
