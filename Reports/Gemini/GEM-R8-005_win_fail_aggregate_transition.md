# GEM-R8-005 Đặc tả Aggregate Transition Win/Fail của Main Game

**STATUS**: COMPLETE  
**GENERATED_AT**: 2026-08-08 22:50:00  
**SOURCE_ROOT**: `D:\Projects\_GameExtract\Main_Meokdoku`

---

## 1. Phân Tách P0 (Domain Bắt Buộc) vs P1/P2 (Ngoại Vi)

Để Codex dễ dàng kiến trúc lại bên Unity, toàn bộ luồng Win/Fail được tách thành:
- **P0 Bắt buộc (Domain & Cốt lõi)**: Thay đổi state bàn cờ (lives, pending error), logic đóng băng (Idempotency Guard), dọn dẹp Snapshot, tăng tiến trình (`current_level`), lưu Cache chơi lại (`retry_puzzle`), cờ DDA (Clean Win, Revived).
- **P1/P2 Ngoại vi (UI/Analytics)**: Bắn event Tracker/Analytics, Animation khóc/cười của mèo, Delay chờ popup, xếp hạng Rank/Streak, Rung thiết bị, Âm thanh. Lớp Domain bên Unity (`GameSession`) **không được** gọi trực tiếp các tính năng P1/P2 này.

---

## 2. Các Call Sequence Chuyển Trạng Thái (State Transition)

### A. Wrong Guess (Sai nhưng Còn Mạng)
*(Bắt nguồn từ: `game_page.gd:_on_board_cell_wrong_guess`)*
1. **Trừ mạng**: `_lives -= 1`.
2. **Khóa thao tác**: `_wrong_guess_pending = true`. Dừng đồng hồ.
3. **Mở khóa (User bấm tiếp)**: `_reset_life_warning()`, đặt lại `_wrong_guess_pending = false`.
4. **Lưu trạng thái**: Gọi `_flush_endgame_snapshot()` để lưu lượng máu vừa bị trừ xuống Local Storage. Trò chơi tiếp tục.

### B. Game Over (Hết Mạng / Fail)
*(Bắt nguồn từ: `game_page.gd:_on_game_over` dòng 1829)*
1. **Idempotency Guard**: Kiểm tra `if not _is_complete`. Nếu chưa, gọi `GameState.on_game_finished()` (Tăng session play count).
2. **Khóa State**: Đặt `_is_complete = true`. Tắt mọi đồng hồ, idle hint.
3. **Cập nhật Tiến trình**: Gọi `GameState.on_level_failed(lv)`:
   - Đánh dấu cờ `_current_level_retried = true` và `_last_level_clean_win = false`. Mặc dù `_is_complete` được set, nó KHÔNG chặn toàn bộ hàm, nên nếu callback bị gọi lại, `on_level_failed` vẫn sẽ bị lặp!
4. **Lưu Cache Retry**: Tạo Dictionary `retry_params` (giữ config, làm sạch bàn cờ) và lưu qua `GameState.set_retry_puzzle(lv, retry_params)`.
5. **Snapshot**: LƯU Ý: Hàm `_on_game_over` **KHÔNG** chủ động lưu hay clear snapshot. Snapshot thực tế đã được flush và lưu với `lives = 0` ngay từ lúc xử lý thay đổi ERROR trước đó. Hệ thống entry sẽ tự clear snapshot này vào lần khởi động sau.
6. **Bật UI**: Truyền `retry_params`, `remaining_cats` sang `GameFailPage`.

### C. Revive (Hồi Sinh từ màn hình Fail)
*(Bắt nguồn từ: `game_page.gd:_on_revive_requested` dòng 1921)*
1. **Khôi phục UI**: Ẩn bảng Fail. Đặt lại `_is_complete = false` (Mở khóa Idempotency).
2. **Cập nhật Mạng**: Cộng thêm máu (`_lives = mini(_lives + restore_amount, 3)`).
3. **Đánh dấu DDA**: Gọi `GameState.mark_dda_tool_or_revive_used()`, `mark_dda_revive_used()`, `mark_pre_cat_revived()` để khóa đặc quyền DDA của ván này.
4. **Lưu State tức thì**: Lập tức gọi `_flush_endgame_snapshot()` để giữ trạng thái hồi sinh, chống crash mất mạng. Trò chơi tiếp tục.

### D. Restart (Chơi Lại giữa ván / từ Pause Menu)
*(Bắt nguồn từ: `game_page.gd:_on_restart_requested` dòng 1669)*
1. **Xác nhận Thất bại**: Gọi thẳng `LevelOps.confirm_level_failed_main(QUIT)`.
   - Hàm này XÓA NGAY `clear_endgame_snapshot()`.
   - Gọi `on_game_finished()` và `on_level_failed(lv)`.
2. **Nạp lại**: Nạp trực tiếp `on_show(retry_params)`.

### E. Quit (Thoát về Home giữa ván)
*(Bắt nguồn từ: `game_page.gd:_on_gear_btn_pressed` dòng 1652)*
1. **Chặn Xóa**: KHÔNG gọi failed, KHÔNG clear snapshot.
2. **Đánh dấu Dirty**: Gọi `GameState.mark_current_level_dirty()`. Lưu ý, Quit không chủ động flush snapshot. Việc lưu snapshot đang pending sẽ được tự động flush bởi hệ thống lifecycle khi màn hình GamePage thực sự bị ẩn đi (on_hide/pause). (Ván chơi được bảo toàn).

### F. Correct Win (Hoàn thành ván)
*(Bắt nguồn từ: `game_page.gd:_on_game_complete` dòng 1954)*
1. **Idempotency Guard**: Giống hệt Fail. `if not _is_complete: GameState.on_game_finished()`. Đặt `_is_complete = true`.
2. **Cập nhật Tiến trình**: Gọi `GameState.on_level_won(lv)`.
   - Nếu `lv + 1 > current_level`, tăng max level.
   - Đặt cờ `_last_level_clean_win = not _current_level_dirty`.
   - Reset `_current_level_retried = false`.
3. **Dọn dẹp**: Gọi `GameState.clear_endgame_snapshot()`.
4. **Tham số đẩy sang UI Win**: Đẩy `mistake_count`, `revive_count`, `final_score`, `completion_rate` sang `GameWinPage`.

---

## 3. Điều kiện Idempotency (Chống lặp)
- Biến **`_is_complete`** trong `game_page.gd` CHỈ chặn lệnh `GameState.on_game_finished()`. Nó **KHÔNG** chặn toàn bộ hàm `_on_game_over` hay `_on_game_complete`! Nếu các hàm này lỡ bị gọi nhiều lần do callback, các thao tác hệ lụy (như `on_level_failed`, `on_level_won`, mở UI) vẫn sẽ bị gọi lại. Đây là một lỗ hổng rất cần fix trên Unity.
- Cả hàm `_on_game_over` (Thua) và `_on_game_complete` (Thắng) đều chia sẻ chung biến `_is_complete = true`. Hàm duy nhất gỡ cờ này xuống là `_on_revive_requested` (Hồi sinh).

---

## 4. Đối chiếu Unity vs Godot
Dựa trên `GameSession.cs` của Unity hiện tại:

**[Đã có]**:
- Có Enum `GameSessionState` (Won, Failed, ResolvingWrongGuess, Playing) phân tách rất rõ ràng, ưu việt hơn cờ boolean `_is_complete` của Godot.
- Gom nhóm `SessionActionResult` để trả về cho Presenter (View) xử lý UI độc lập. Đạt chuẩn P0 vs P1/P2.

**[Còn thiếu / Đang khác]**:
- Cấu trúc Unity chưa bao hàm/liên kết với DDA flags (`_current_level_retried`, `_last_level_clean_win`, `_pre_cat_revived`).
- Hàm Khôi phục `GameSessionRestoreData` đang tập trung ở `Lives`, History mà chưa tích hợp việc **flush snapshot ngay lập tức sau khi Revive** (điều sống còn để khỏi mất data nếu crash).
- Logic `GameState.set_retry_puzzle` (Cache ván chơi nguyên bản không chứa Cat/Error) chưa thấy xuất hiện ở Session. Việc lưu Cache này phải do một Controller bên ngoài Session đảm nhận (LevelCoordinator).

---

## 5. Bảng Fixture Kiểm Thử (Cho Codex)

| Trạng thái Trước (State) | Hành động (Action) | Trạng thái Sau mong đợi (Expected Post-State) | DB Persistence |
| --- | --- | --- | --- |
| `Playing`, Lives = 1 | Sai lầm cuối cùng -> Fail | `State = Failed`. Bắn sự kiện ShowFailUI. | Save Snapshot (Lives=0), DDA `retried=true`. |
| `Failed`, Lives = 0 | Xem Ad, bấm Hồi sinh | `State = Playing`. Lives = 3. | Flush ngay Snapshot (Lives=3). DDA `revived=true`. |
| `Playing`, Tồn tại Snapshot | Bấm Restart / Retry | `State = Loading` ván mới. | **Clear** Snapshot. DDA `retried=true`. |
| `Playing`, Tồn tại Snapshot | Bấm nút Home / Quit | Về Menu, GameSession tự hủy. | **Lưu** Snapshot. Level **Không bị đánh dấu Failed**. |
| `Playing`, Đặt Cat cuối cùng | Giải quyết xong bàn cờ | `State = Won`. Khóa mọi Input. | **Clear** Snapshot. `current_level += 1`. |

STATUS: COMPLETE
REPORT_ID: GEM-R8-005
