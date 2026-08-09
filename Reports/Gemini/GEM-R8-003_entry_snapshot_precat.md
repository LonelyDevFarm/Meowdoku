# GEM-R8-003 Đặc tả Entry Flow, Endgame Snapshot & PreCat

**STATUS**: COMPLETE  
**GENERATED_AT**: 2026-08-08 18:55:00  
**SOURCE_ROOT**: `D:\Projects\_GameExtract\Main_Meokdoku`

---

## 1. Luồng Vào Màn Chơi (Entry Flow)
Khi GamePage nhận tín hiệu `on_show(params)` cho chế độ chơi thường (`level_idx > 0`), chuỗi quyết định được xử lý theo trình tự sau (`game_page.gd`, dòng 480 - 580):

1. **Daily First Easy (Giảm độ khó trận đầu ngày)**:
   - Nếu level hiện tại là Special hoặc Hard: Tiêu hao ngay cơ hội giảm độ khó (do không được giảm).
   - Nếu Snapshot (ván dang dở) có chứa tiến trình của người chơi (`_has_user_progress_in_endgame_snapshot`): Tiêu hao cơ hội (ưu tiên khôi phục ván cũ thay vì tạo ván mới).
   - Nếu không thỏa 2 điều kiện trên: **Xóa sạch Endgame Snapshot** và **Xóa bộ nhớ Retry Puzzle**, bắt buộc hệ thống sinh một ván mới hoàn toàn (để áp dụng giảm độ khó).
2. **Khôi Phục Snapshot (Endgame Restore)**:
   - Gọi `_try_consume_endgame_snapshot(params)`.
   - Nếu Snapshot hợp lệ, chuyển đổi Snapshot thành dạng `params`, gắn cờ `endgame_restore = true`, xóa `current_level_dirty` và gọi đệ quy `on_show(new_params)` để khôi phục rồi `return` sớm.
3. **Chơi Lại Ván Cũ (Retry Puzzle / Recent Update)**:
   - Nếu không có Snapshot, kiểm tra xem `GameState.get_retry_puzzle(lv)` có tồn tại bộ đệm cấu hình ván cũ (khi người chơi bấm Restart/Retry trước đó) hay không.
   - Nếu có: Tải cấu hình từ `retry_puzzle`, gọi đệ quy `on_show(cached_params)` với trạng thái `Tracker.GameStatus.CONTINUE` rồi `return` sớm.
4. **Khởi Tạo Ván Mới (New Entry)**:
   - Gọi logic phân giải cấu hình `_resolve_entry_mode()` (ví dụ `_setup_entry_normal`).
   - Gọi `_resolve_pre_cat()` để xử lý cơ chế Mèo cho sẵn.
   - Nếu cơ hội Daily First Easy chưa bị tiêu hao ở bước 1, thực hiện tiêu hao vét đáy để kết thúc.

---

## 2. Vòng Đời Endgame Snapshot

### A. Kích hoạt Lưu (Save/Flush)
- **Real-time (Throttle)**: Signal `cell_state_changed` của `BoardView` kích hoạt `_persist_endgame_snapshot`. Khởi động `_endgame_persist_timer` để debounce/throttle. Hết thời gian sẽ gọi `_flush_endgame_snapshot()`.
- **Sự kiện Hệ thống**: Tự động lưu ngay lập tức khi người dùng ẩn app (Pause), mất Focus, hoặc bấm thoát (Back Button).

### B. Điều kiện Xóa (Clear)
Sẽ gọi `GameState.clear_endgame_snapshot()` nếu:
- Trọng tài kiểm tra Snapshot thấy không hợp lệ (lỗi Version, lỗi định dạng, dữ liệu OOB).
- Game Update Version lên >= 1.12.0 và phát hiện lệch Level (`snap_level != current_level`).
- Số mạng lưu trong Snapshot `lives <= 0`.
- Khi Restore Snapshot, nếu bàn cờ đã kín (`is_complete() == true`), game tự động đánh dấu thắng Level, nâng Level tiếp theo và xóa Snapshot cũ.
- Khi người chơi Win (`_on_game_complete`) hoặc Fail và bấm Restart/Give Up.

---

## 3. Quy tắc Thẩm định (Validation Rules)

Việc thẩm định chạy qua 2 bước trong `game_page.gd` (`_validate_endgame_snapshot` và `_validate_snapshot_data`):
1. **Kiểm tra Layout**:
   - Khớp `version` (`GameState.ENDGAME_SNAPSHOT_VERSION`).
   - Yêu cầu đủ các key bắt buộc: `size`, `r`, `id`, `regionMap`, `solution`, `level`, `lives`, `placed_cats`, `marks`, `errors`.
   - `regionMap` phải là mảng 2D kích thước chính xác `sz x sz`. `solution` là mảng 1D độ dài `sz`.
2. **Kiểm tra Toàn Vẹn Data (Data Integrity)**:
   - Các giá trị cột trong `solution` không được nằm ngoài biên `[0, sz - 1]`.
   - Các tọa độ trong mảng `placed_cats` (Mèo đã đặt) **phải thuộc về `solution`**. (Nghĩa là nếu có 1 con mèo bị đặt sai trong Snapshot, Snapshot bị coi là hỏng (corrupted) và bị xóa ngay lập tức!).
3. **Xác nhận User Progress**:
   - Nếu chỉ có những con Mèo cho sẵn (`prefill_positions`), game không tính là có Progress. Phải có số lượng Mèo đặt > Mèo cho sẵn, hoặc có ít nhất 1 dấu MARK/ERROR thì mới khóa cơ hội Daily First Easy.

---

## 4. Cơ Chế Pre-Cat (Mèo Cho Sẵn)
Nằm trong hàm `_resolve_pre_cat()` (`game_page.gd:2269`):
- Kiểm tra A/B Test Group và đọc trạng thái `GameState.consume_pre_cat_pending()`.
- **Khóa (Lock)**: Đọc tọa độ khóa từ `GameState.get_pre_cat_lock(lv)`. Mục đích của Lock là: Nếu người chơi chơi lại chính Level này, vị trí PreCat phải **cố định không đổi**.
- Nếu Lock không tương thích với bàn cờ mới (VD: chuyển sang ván khác), gọi `PreCatDecider.pick_prefill_cell` để chọn lại 1 ô ngẫu nhiên và lưu Lock mới.
- Tọa độ sau đó đưa vào `_level_config["prefill_positions"]` và hiển thị bằng `BoardView.set_cell_state(..., PREFILL)` trong hàm `_prefill_hints()`.

---

## 5. Schema Dữ Liệu Endgame Snapshot

| Trường (Field) | Loại | Bắt buộc | Ghi chú (Nguồn / Fallback) |
| --- | --- | --- | --- |
| `version` | Int | Có | Version Snapshot hiện tại |
| `level`, `size`, `r`, `id` | Int | Có | Định danh Level và Bank Puzzle |
| `regionMap` | Array 2D | Có | Layout các Chuồng |
| `solution` | Array 1D | Có | Đáp án (chỉ chứa tọa độ Cột) |
| `lives` | Int | Có | Số mạng còn lại |
| `placed_cats`, `marks`, `errors`| Array | Có | Mảng chứa `[r, c]`. |
| `prefill_positions` | Array | Tùy chọn | Các ô Mèo được điền sẵn |
| `bank_source`, `bank_source_main`, `bank_tier` | String | Tùy chọn | Nguồn Bank (Fallback: "") |
| `pre_type` | String | Tùy chọn | Loại PreCat |
| `step_history` | Array | Tùy chọn | Lịch sử đánh (dùng cho Undo/Restore) |
| `score`, `combo`, `max_combo` | Int | Tùy chọn | Model Điểm Số (Có fallback legacy: `combo_count`, `se_score`, `combo_score`) |
| `restart_count`, `revive_count` | Int | Tùy chọn | Thống kê số lần chết |

---

## 6. Lập Fixtures Mẫu (Unit Test C#)

| Scenario | Input / State Before | Kết quả mong đợi (Action / State After) |
| --- | --- | --- |
| **New Entry (Normal)** | Chơi Level 5. Không có Snapshot, Không có Retry Cache. | Gọi thẳng vào `_setup_entry_normal`, sinh ván mới. `_toast_first_try_hint` = True. |
| **Valid Restore** | Chơi Level 5. Snapshot chứa `level=5`, `lives=2`, `placed_cats=[(0,0)]`. Ô (0,0) thuộc `solution`. | Hợp lệ. Chuyển hướng nạp Snapshot, gọi đệ quy `on_show` với `endgame_restore=true`. Trạng thái `CONTINUE`. |
| **Invalid Restore** | Như trên, nhưng `placed_cats=[(0,1)]` mà ô (0,1) KHÔNG thuộc `solution`. | Vô hiệu (Data Integrity Error). Gọi `clear_endgame_snapshot()`, fallback tạo ván mới. |
| **Retry Puzzle** | User chọn Restart màn hình Thua. Cache `retry_puzzle` chứa config bàn cờ. | Lấy config từ Cache, gọi `on_show` với trạng thái `CONTINUE`. |
| **Win Clear** | Gọi `_cmd_win()` hoặc giải xong bàn. | Trigger `_on_game_complete()`, lập tức gọi `clear_endgame_snapshot()`. |

---

## 7. Ranh Giới Domain - Repository - View (Kiến trúc Unity)

- **GameSession / Domain**: Chứa cơ chế Validation (`_validate_snapshot_data`, đối chiếu OOB, logic số lượng Mèo đặt lớn hơn số lượng cho sẵn).
- **GameState Repository**: Là Persistent Layer chứa các hàm getter/setter thô như `set_endgame_snapshot`, `get_pre_cat_lock`, `get_retry_puzzle`.
- **Entry Coordinator (Presenter/Controller)**: Logic của `on_show` đứng ra điều phối thứ tự ưu tiên (Daily First Easy -> Snapshot -> Retry -> New).
- **Unity Scheduler / View**: Hẹn giờ Timer (`_endgame_persist_timer`), sự kiện Pause/Focus (OnApplicationPause), Tracker (Bắn Log).

STATUS: COMPLETE
