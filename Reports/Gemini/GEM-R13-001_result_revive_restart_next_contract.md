# GEM-R13-001 Đặc tả Contract Result / Revive / Restart / Next

**STATUS**: COMPLETE  
**GENERATED_AT**: 2026-08-09 01:05:00  
**SOURCE_ROOT**: `D:\Projects\_GameExtract\Main_Meokdoku`

---

## 1. Dữ liệu Đầu vào (Contract) của các màn hình Kết quả

### A. Màn hình Thắng (GameWinPage)
Mở thông qua `UIManager.show_ui(UiName.WIN, params)` từ `game_page.gd:_on_game_complete`.
*   **Tham số truyền (Params)**:
    *   `level_config` (Dictionary): Mang dữ liệu cấu hình level gốc và được tiêm thêm các thông số thống kê ván chơi:
        *   `restart_count`: int
        *   `revive_count`: int
        *   `mistake_count`: int
        *   `final_score`: int
        *   `max_combo`: int
        *   `elapsed_sec`: float
        *   `completion_rate`: int (0-100)
        *   `tools_used`: int
    *   `board_view` (Object): Tham chiếu trực tiếp tới scene bàn cờ đang chơi. (Dùng để clone trạng thái hoặc show animation).

### B. Màn hình Thua (GameFailPage)
Mở thông qua `UIManager.show_ui(UiName.FAIL, params)` từ `game_page.gd:_on_game_over`.
*   **Tham số truyền (Params)**:
    *   `level_config` (Dictionary): Chứa thông tin level (`level`, `size`, `is_daily`).
    *   `retry_params` (Dictionary): Gói cache chứa sẵn bàn cờ đã được làm sạch (chỉ có Mèo cố định `pre_cat`, giữ nguyên `solution`, `regions`, `level_seed`). Các bước đi của người chơi đã bị xóa hoàn toàn.
    *   `remaining_cats` (int): Số mèo còn lại chưa xếp.

---

## 2. Trình tự Gọi Hàm (Call Sequence) và Guard

### A. Win → Next Level
1. **Hoàn thành ván (`_on_game_complete`)**:
    *   Guard: `_is_complete == true`. Cờ này CHỈ ngăn gọi `GameState.on_game_finished()` nhiều lần. Tuyệt đối lưu ý: nó **không chặn** toàn bộ hàm, nên nếu bị spam callback, `GameState.on_level_won(lv)` vẫn có thể bị gọi lặp lại.
    *   Tăng tiến trình: `GameState.on_level_won(lv)` chạy **TRƯỚC** khi mở màn hình Win. Lúc này `current_level` đã được tăng.
    *   Xóa bản sao lưu: `GameState.clear_endgame_snapshot()`.
2. **Màn hình Win (`GameWinPage`)**:
    *   Sử dụng biến `_show_seq_id` (tăng lên mỗi lần ẩn/hiện) làm guard nội bộ để hủy các Coroutine/Tween (ví dụ: đang bay animation thì bị đóng -> ngắt không chạy dòng code tiếp theo).
3. **Bấm Next (`_on_next_btn_pressed`)**:
    *   Khớp nối với các hệ thống ngoài (P2): Nhận thưởng Rank, hiển thị Popup Streak/Rank.
    *   Lệnh nhảy trang: Bắn `UIManager.show_ui(UiName.GAME, {"level_index": lv + 1})` (Load lại GamePage với level kế tiếp).

### B. Fail → Restart
1. **Game Over (`_on_game_over`)**:
    *   Tương tự Win, dùng cờ `_is_complete` để khóa lệnh `on_game_finished()`, nhưng **không khóa** được `on_level_failed()`.
    *   Báo thua: `GameState.on_level_failed(lv)` (Đánh dấu Level này đã mất chuỗi Clean Win).
2. **Màn hình Fail (`GameFailPage`)**:
    *   Bấm Restart: Truyền lại tham số `retry_params` vào thẳng `UiName.GAME` để khởi động lại nhanh.
3. **Dọn Snapshot (Lazy Clear)**:
    *   Khác với Restart (xóa ngay), hàm `_on_game_over` không ép xóa snapshot lúc này. Thậm chí nó cũng KHÔNG chủ động lưu snapshot `lives=0` (snapshot này đã được lưu trước đó lúc vừa bị trừ máu). Khi khởi tạo lại `GamePage`, hàm `_try_consume_endgame_snapshot` đọc thấy máu bằng 0 nên mới tiến hành XÓA snapshot.

### C. Fail → Revive (Hồi Sinh)
*   **Hồi sinh Miễn phí (`_is_free_revive`)**: Kích hoạt khi chưa mở khóa hệ thống Ad (`reward_unlock_level`), hoặc trúng cấu hình AB Test `revive_free_logic` (Lv1 luôn free, hoặc được free 1 lần duy nhất trong đời).
*   **Quy trình gọi Callback**:
    *   Gửi lệnh mở Quảng cáo: `UniKitManager.show_reward(...)`.
    *   Nếu thành công, kết nối callback một lần (One-shot) vào `ad_rewarded`.
    *   Emit signal `revive_requested` báo về GamePage.
    *   Tại GamePage: Gỡ cờ `_is_complete = false`, khôi phục máu, lập tức lưu snapshot mới.

---

## 3. Khoảng trống Kiến trúc (Unity Gaps) cần lưu ý
| Thành phần | Godot hiện tại | Thiết kế Unity (GameSession) nên sửa |
| :--- | :--- | :--- |
| **Data Contract (P0)** | Truyền param bằng `Dictionary` lỏng lẻo. Cần truy xuất string key. | Nên dùng `GameResultData` (Thắng) và `GameRetryData` (Thua) kiểu tĩnh (DTO). |
| **Kiến trúc MVC (P0)** | `GameWinPage` nhận tham chiếu `BoardView` (View) trực tiếp từ GamePage. Vi phạm nghiêm trọng MVP. | `GameSession` chỉ trả về State. Việc clone bàn cờ để show animation thuộc về Flow Coordinator xử lý, UI Result không được đụng vào logic/view của game. |
| **Logic Mạng Quảng Cáo (P2)** | UI `GameFailPage` tự phán đoán xem có miễn phí không, và tự gọi `UniKitManager` mở quảng cáo. | UI chỉ nên phát event `ReviveRequested`. Controller/Flow Coordinator bên ngoài sẽ xử lý AD logic, nếu thỏa mãn mới gọi `GameSession.Revive()`. |
| **Hệ thống Ngoại vi (P1/P2)**| `GameWinPage` gắn cứng việc check `StreakManager`, `RankActivityManager`. | Cần đóng gói vào hệ thống Global Observer hoặc Task Queue để tránh đứt gãy nếu tính năng này chưa port sang Unity kịp. |

---

## 4. Fixture Kiểm Thử (Cho Codex)

| Nhánh | State Trước | Hành Động | State Chuyển Chờ | Sự Kiện Kích Hoạt (P0) |
| :--- | :--- | :--- | :--- | :--- |
| **Win** | `Playing` | Đặt mảnh cuối, `is_complete` true | `Won` | `on_level_won(lv)` gọi tăng Level -> Xóa Snapshot -> Show Win UI |
| **Fail** | `Playing`, `Lives=1` | Chọn sai | `Failed` | Máu = 0, khóa bàn cờ -> `on_level_failed(lv)` lưu Cache -> Show Fail UI |
| **Fail -> Restart** | UI Fail mở | Click Restart | `Loading` (Map cũ) | Gọi load `GamePage` truyền vào Cache (đã clear lỗi, giữ seed). |
| **Fail -> Revive** | UI Fail mở | Click Revive (Watch Ad) | `Playing` | Cộng máu -> Gỡ khóa State -> **Flush Snapshot Lập tức** (Rất quan trọng). |

*(Ghi chú: Mọi logic này được verify nguyên gốc từ hàm `_on_game_over`, `_on_game_complete`, `_on_revive_requested` trong `game_page.gd` và `_on_next_btn_pressed` trong `game_win_page.gd`)*
