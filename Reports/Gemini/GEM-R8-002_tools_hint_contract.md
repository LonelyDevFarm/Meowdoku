# GEM-R8-002 Đặc tả Contract của Công cụ (Tools & Hint)

**STATUS**: COMPLETE  
**GENERATED_AT**: 2026-08-08 18:45:00  
**SOURCE_ROOT**: `D:\Projects\_GameExtract\Main_Meokdoku`

---

## 1. Hủy Bỏ (Undo)
- **Thực trạng**: **CHƯA ĐƯỢC TRIỂN KHAI (NOT IMPLEMENTED)**.
- **Chi tiết**: Trong `base_game_page.gd` và `game_page.gd`, không có hàm `undo()`, `_on_undo_btn_pressed()` hay bất kỳ logic xử lý nút Undo nào được gọi từ UI. Mặc dù có tồn tại hằng số `BoardView.ChangeSource.UNDO` (`board_view.gd:42`), `Tracker.PropSource.UNDO_REWARD_AD` (`base_game_page.gd:788`) và `StepHistory` hỗ trợ hàm `pop_last()`, tựu trung lại tính năng "Tool Undo" không xuất hiện trong vòng lặp gameplay chính. (Trường hợp duy nhất `pop_last` được dùng là để hủy một *draft mark* chưa chính thức).

## 2. Dọn Dẹp (Clear / Unmark)
- **Hàm xử lý**: `_on_clear_btn_pressed()` (`base_game_page.gd:3800`).
- **Guards**: `_entry_anim_playing == false`, `_is_complete == false`, `_wrong_guess_pending == false`.
- **Tiêu hao**: Là một công cụ miễn phí vô hạn, không gọi `_consume_tool`.
- **Hành động & Board**: Duyệt lưới `sz x sz`. Nếu phát hiện `get_cell_state(r, c) == CellState.MARK` (Dấu X), chuyển ngay về `CellState.EMPTY`. Nguồn đổi trạng thái mặc định (`USER_ACTION`).
- **History/Combo/Lives**: **Không** thay đổi Combo. **Không** ghi lưu vào `StepHistory` (nghĩa là Clear không phải là một nước đi). Mạng giữ nguyên.
- **Chỉ số / Lưu**: `Tracker.inc_stat("clear_used")`. Chơi âm thanh `SoundManager.Kind.UNMARK_X`. Kích hoạt UI `_update_remaining()`.

## 3. Định Vị (Locate)
- **Hàm xử lý**: `_on_locate_btn_pressed()` (`base_game_page.gd:2378`).
- **Guards**: Tương tự Clear.
- **Tiêu hao & Snapshot**: 
  - Gọi `_consume_tool(_tool_locate_btn)`. Nếu hết tài nguyên, mở bảng mua/xem quảng cáo và return sớm.
  - Kích hoạt `GameState.mark_current_level_dirty()` để yêu cầu lưu Database Snapshot ngầm.
- **Logic chọn ô**:
  - Quét tính "số lượng ô trống (không phải MARK/ERROR)" còn lại trong từng chuồng (Region).
  - Thu thập toàn bộ các ô thuộc Solution nhưng chưa được đặt Mèo.
  - **Sắp xếp**: Ưu tiên những ô nằm trong chuồng sắp hoàn thành nhất (Region Size nhỏ nhất). Nếu hòa, ưu tiên từ trên xuống, trái sang phải.
  - Chọn ứng viên đầu tiên (`best`).
- **Hành động & Board**: 
  - `_board_view.set_cell_state(..., CellState.CAT, source=LOCATE)`.
  - Cập nhật Data: Gọi `_record_cell_change(..., prev, CAT)` và `_commit_current_step(true, false)` để đưa vào `StepHistory`.
- **Score/Combo/Lives**: Gọi gián tiếp `_on_board_cell_state_changed_for_combo`. Combo tăng thêm 1. Điểm Score nhận được = Điểm chuẩn + Skill Bonus. Bắn animation ThumbsUp (`LikeHandTrigger.LOCATE`).

## 4. Gợi Ý (Hint)
Gợi ý là quy trình 2 bước: Yêu cầu (Request) -> Áp dụng (Apply).
- **Yêu cầu Hint (`_on_hint_btn_pressed` - `base_game_page.gd:3662`)**:
  - **Guards**: Phải qua `_hint_cooldown == false`.
  - **Tiêu hao**: Bị trừ Tool ngay lúc nhấn nút, chứ không phải lúc Apply.
  - **Thứ tự sinh (HintEngine)**: `find_mark_hint` -> Fallback `WRONG_MARK` -> `find_r1` -> `find_r2` -> `find_r3_r4` -> `find_chain`.
  - Nếu tất cả rỗng (trường hợp Bank_SP), tự động gọi fallback sang Locate (`_on_locate_btn_pressed`).
  - Gắn dữ liệu vào `_hint_data`, bật `_hint_overlay` và đánh dấu Snapshot (`mark_current_level_dirty()`). Bắt đầu mutex (khóa các thao tác khác dưới UI).

- **Áp dụng Hint (`_on_hint_applied` - `base_game_page.gd:3860`)**:
  - Dựa vào `strategy` trong `_hint_data`.
  - **R1_mark, R2, R3, R4, Chain**: Chuyển ô thành `CellState.MARK` (X). Nguồn `BoardView.ChangeSource.HINT`.
  - **R1 (Cơ bản)**: 
    - Nếu là xóa lỗi (`wrong_mark == true`): Chuyển MARK về `EMPTY`.
    - Nếu là chỉ đúng ô: Chuyển về `CAT`. Nguồn `HINT`.
  - **History/Combo**: 
    - Nước đi `MARK` -> `_commit_current_step(false, false)`. Không tăng Combo.
    - Nước đi `CAT` -> `_commit_current_step(true, false)`. Tăng Combo. Thưởng ngón cái (`HINT_R1_APPLY`).
  - Đặt `_hint_cooldown` = 0.5s hoặc 0.8s, xả `_hint_data = {}`.

- **Idle Hint / Mutex (`base_game_page.gd:1063`)**: 
  - Khởi tạo `_idle_hint_delay = 20.0s`. 
  - Nếu user không thao tác (`_idle_time > 20`), ToolButton của Hint bắt đầu nhấp nháy phát sáng.
  - Hàm `_reset_idle_hint()` được chèn vào mọi hàm chạm/tool để đặt lại `_idle_time = 0`.

## 5. Auto-Mark và Auto-Complete
- **Hàm xử lý**: `_run_auto_complete(token)` (`base_game_page.gd:2941`).
- **Config Gate**: **KHÔNG CÓ TÍNH NĂNG TỰ ĐỘNG THỜI GIAN THỰC**. Lệnh `_run_auto_complete` không được gắn vào luồng logic thông thường. Nó chỉ được kích hoạt bằng lệnh Cheat (`_cmd_win()`).
- **Thứ tự Cell**: Sắp xếp các ô cần đánh X (Mark) và Mèo (Cat) theo một đường chéo ảo: `ring = y + (sz - 1 - x)`. Các ô có cùng `ring` sẽ chạy animation cùng lúc (bay chéo).
- **Animation Delay**:
  - `AUTO_MARK_DIAG_INTERVAL_SEC = 0.06s` (Giữa các hàng chéo MARK).
  - `AUTO_MARK_TO_CAT_GAP_SEC = 0.2s` (Nghỉ chờ chuyển pha).
  - `AUTO_CAT_STEP_SEC = 0.12s` (Giữa các ô CAT, không đánh chéo mà đánh tuần tự).
- **Kiểm tra Win**: Hàm này chỉ đánh dấu UI (nguồn `AUTO_COMPLETE`). Việc bắt sự kiện Win thực thụ nằm ở `_validate_board()` được trigger đằng sau bằng Signal.

---

## 6. Fixtures Tối Thiểu Cho Các Tools (C# Porting)

| Tool | State Before | Action | State After |
| --- | --- | --- | --- |
| **Locate (Valid)** | Lưới có 10 ô 빈 thuộc Solution. `_combo = 2`. ToolCount=1. | Gọi `Locate()`. | Ô 빈 (thuộc vùng còn ít ô nhất) thành CAT. `_combo = 3`. Tăng Score. Thêm vào StepHistory. ToolCount=0. |
| **Locate (Invalid)** | Bàn cờ đã full mèo, chỉ còn thiéu X. | Gọi `Locate()`. | Không làm gì, `return`. Khởi tạo ad-request. |
| **Clear (Valid)** | Bàn cờ có 5 ô là Dấu X (`MARK`). | Gọi `Clear()`. | Tất cả 5 ô về `EMPTY`. Không đổi Combo/Score/History. |
| **Clear (Locked)** | Mạng (Lives) = 0, đang chờ hiệu ứng thua (`wrong_pending=true`). | Gọi `Clear()`. | Guard chặn lại. Không làm gì. |
| **Hint Apply (R2/R3/R4)** | Đang duyệt Gợi ý dạng loại trừ chéo. | Gọi `Hint.Apply()`. | Ô chỉ định biến thành `MARK` (X). Không đổi Combo. Thêm step(Cat=false) vào History. |

---

## 7. Ranh Giới Kiến Trúc (Architecture Boundary)
Phục vụ Codex khi chuyển mã sang C# Unity:
- **GameSession (Lớp Domain Thuần)**: Chứa logic đếm số ô còn lại của Region (Locate weight), lưu `StepHistory` (dành cho Hint/Locate), và thực thi trạng thái MARK/EMPTY/CAT.
- **Ngoại Vi (UI/Audio/Vibrate)**: Delay và Ring Sort của `_run_auto_complete`, Tween ThumbsUp (`LikeHandTrigger`), UI Overlay của Hint (`_hint_overlay`), Hệ số trừ/cộng công cụ thông qua `ABTestManager`. Nên đóng gói qua Event Bus.

STATUS: COMPLETE
