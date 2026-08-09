# Báo cáo đặc tả kỹ thuật: GEM-R11-014 (Tutorial State Machine Source Spec)

**Nguồn đối chiếu:** `D:\Projects\_GameExtract\Main_Meokdoku`
**Mục tiêu:** Phân tích logic và State Machine của hệ thống Tutorial 4x4 để cung cấp đặc tả chính xác cho việc thiết kế và kiểm thử.

---

## 1. Cơ chế State Machine & Reset Logic (Restart/App-kill)

- **State Machine Control:** Dựa vào `_current_mode` (kiểu `enum StepMode {NONE, PLACE_CAT, MARK_CELLS, FREE_PLAY, CONFIRM}`) làm cổng gác (Gatekeeper) tại các input handler.
- **Tiến trình:** `_flow_token` lưu dấu ấn version hiện tại của luồng tutorial. Nếu Tutorial bị ngắt ngang, `_flow_token` khác với token ban đầu sẽ lập tức trả về (return), dừng coroutine.
- **Lưu trữ Persistent Data (App-kill):** 
  - Hoàn toàn **KHÔNG CÓ LƯU TRỮ** state của từng bước. Nếu user Force Quit ở bước 5, khi mở lại GameState sẽ thấy `is_tutorial_done() == false` và Launcher sẽ đưa thẳng vào Tutorial Page, **chạy lại từ bước 1**.
  - `tutorial_done` chỉ được `commit` duy nhất một lần ở hàm `complete_tutorial()` được gọi khi ấn nút Confirm ở màn kết thúc (sau pháo hoa).

---

## 2. Đặc tả chi tiết từng bước (Tutorial 4x4)
- **Board Configuration:** 
  - Kích thước: 4x4 (scale up thành `_GUIDE_BOARD_WIDTH`).
  - Region/Color: Đọc từ `BankData.get_sp_levels()` với id `pattern == "guide"`.

### Step 1: Đặt Mèo đầu tiên
- **Mục tiêu:** (0, 2)
- **Thông điệp:** `TUTORIAL_STEP1_RICH` kết hợp `TUTORIAL_STEP1_HIGHLIGHT` (dùng BBCode hiệu ứng breath).
- **Thao tác:** 
  - Allowed cells: `[(0, 2)]`
  - Current mode: `StepMode.PLACE_CAT`.
  - Khóa (Block): Chặn mọi drag/tap ra ngoài ô (0, 2). Chặn đánh dấu X.
- **UI & Hand:** `_show_mask_hints` tại `(0, 2)`. Bàn tay chỉ thẳng vào `(0, 2)`.

### Step 2: Giải thích "Mỗi màu 1 mèo"
- **Mục tiêu:** Đọc hiểu luật.
- **Thông điệp:** `TUTORIAL_STEP2_RICH`
- **UI & Confirm:** Nút Confirm với text `TUTORIAL_GOT_IT`. Chờ user ấn `_on_confirm_btn_pressed` (StepMode = CONFIRM).

### Step 3: Đánh dấu Hàng/Cột (Mark Cells)
- **Mục tiêu:** Hướng dẫn drag mark X ở Hàng 0 và Cột 2.
- **Thông điệp:** `TUTORIAL_STEP5_RICH`
- **SubMessage:** `TUTORIAL_SUB_EXCLUDE` nằm dưới board.
- **Thao tác:**
  - Allowed cells: 6 ô `(0,0), (0,1), (0,3)` và `(1,2), (2,2), (3,2)`.
  - Condition: `_marked_count >= 6`. Phải kéo hoặc tap ra X.
- **Hand Loop:** Bàn tay không chạy swipe loop (chỉ mở mark).

### Step 4: Đặt Mèo thứ hai (Vùng màu hồng)
- **Mục tiêu:** (3, 1)
- **Thông điệp:** Dùng variant config A/B `RegionColorConfig`. Nếu là control, hiện "Vùng màu HỒNG" (`TUTORIAL_STEP4_PINK_RICH`). Nếu variant, lấy tên màu động `TUTORIAL_STEP4_COLOR_RICH`.
- **Thao tác:** Đặt mèo tại `(3, 1)`. 
- **Mirror Cells:** Ô `(2, 2)` và `(3, 2)` (đã đánh dấu ở step 3) được hiện lên khỏi lớp mask tối.

### Step 5: Đánh dấu Chéo (Neighbors)
- **Mục tiêu:** Drag mark X xung quanh Mèo Hồng.
- **Thông điệp:** Tuỳ config `ABTestManager.tutorial_diagonal.is_diagonal_copy()` sẽ hiện `TUTORIAL_STEP3_RICH_DIAGONAL` hoặc `TUTORIAL_STEP3_RICH`.
- **SubMessage:** `TUTORIAL_SUB_SWIPE_EXCLUDE`.
- **Thao tác:**
  - Allowed cells: 3 ô `(2,0), (2,1), (3,0)`.
  - Condition: `_marked_count >= 3`.
- **Hand Loop:** Chạy `_start_swipe_hand_loop` (cầm bàn tay static lướt qua 3 điểm (3,0), (2,0), (2,1)).

### Step 6: Đặt Mèo thứ ba (Vùng màu xanh)
- **Mục tiêu:** (1, 0)
- **Thông điệp:** Giống step 4, tuỳ thuộc `RegionColorConfig`, hiện `TUTORIAL_STEP4_BLUE_RICH` hoặc màu động.
- **Thao tác:** Đặt mèo tại `(1, 0)`. Lớp mask hiện rõ con mèo và các vùng đã mark kế cận.

### Step 7: Free Play (Tự do đặt mèo cuối cùng)
- **Mục tiêu:** (2, 3)
- **Thông điệp:** `TUTORIAL_LAST_ONE_RICH`.
- **Giao diện:** Bật `_hint_tool_panel` (Nút Gợi ý). Mask bị tắt (nhìn full bảng).
- **Thao tác:** 
  - Allowed: User có thể tap thoải mái nhưng nếu không phải ô (2,3) thì không giải quyết được bảng (Bị chặn logic nếu khác step).
  - Hệ thống Hint (Bóng đèn): 
    - Bấm lần 1 (Phase 1): Mask các ô trống ở hàng con mèo xanh -> Yêu cầu đánh X.
    - Bấm lần 2 (Phase 2): Mask các ô trống ở hàng con mèo hồng -> Yêu cầu đánh X.
    - Bấm lần 3 (Phase 3): Chỉ thẳng tay vào đích (2,3) -> Đặt mèo cuối.

### Step 8: Kết thúc
- Hiển thị nút `TUTORIAL_START_GAME`.
- Chơi pháo hoa (Nếu dùng config `GuideFeedbackConfig = IQ`) hoặc Tung hoa giấy (Confetti) mặc định.
- Chuyển Scene sang Game `level_index: 1`. Đóng Tutorial, ghi nhận `GameState.set_tutorial_done(true)`.

---

## 3. Xác minh A/B Testing Configs
- **GuideFeedbackConfig:** Xác nhận tồn tại và đang hoạt động.
  - Được định nghĩa tại `scripts/module/abtest/config/guide_feedback_config.gd` với 3 mode: `CURRENT` (0), `CHECK` (1), `IQ` (2).
  - Tuỳ Mode sẽ gọi luồng tương ứng: `_run_guide_flow_default()`, `_run_guide_flow_check()` hoặc `_run_guide_flow_iq()`.
  - `CHECK`: Thêm anim success tick xanh sau mỗi thao tác.
  - `IQ`: Hiện thanh tiến trình IQ phía trên bảng, mỗi bước qua sẽ tăng thanh IQ (`_play_iq_feedback`).
- **TutorialDiagonalConfig:** Xác nhận tồn tại (`ABTestManager.tutorial_diagonal`).
- **DoubleTapProtectConfig:** Được sử dụng cho toàn bộ thao tác đặt mèo trong Tutorial! Hàm `_open_double_tap_window` gọi `ABTestManager.doubletap_protect.window_sec` và `QueendokuCore.classify_violation` để quyết định xem click đó được tính ngay lập tức hay phải block trong cửa sổ miligiây để chờ double-click.

---

## 4. Bảng Evidence Trích Dẫn

| Tiêu chí | File Nguồn | Class/Hàm/Dòng/Đoạn mã |
| :--- | :--- | :--- |
| State Machine Mode | `tutorial_page.gd` | `enum StepMode {NONE, PLACE_CAT, MARK_CELLS, FREE_PLAY, CONFIRM}` (Dòng 49) |
| Chặn Input Mode | `tutorial_page.gd` | Hàm `_on_board_cell_drag_start()` (Dòng 870) chặn mọi Drag/Tap nếu nằm ngoài `_allowed_cells` hoặc sai Mode. |
| Reset & Flow Token | `tutorial_page.gd` | Các hàm `_run_guide_flow_*()` chứa lệnh `if _flow_token != _tok: return` (Dòng 151) |
| Save Data App-kill | `tutorial_page.gd` | Hàm `complete_tutorial()` chứa lệnh `GameState.set_tutorial_done(true)` duy nhất cuối game (Dòng 582) |
| Hand Swipe Animation | `tutorial_page.gd` | Hàm `_start_swipe_hand_loop(cells: Array[Vector2i])` tween lerp toạ độ bàn tay (Dòng 674) |
| Cấu hình Config IQ | `guide_feedback_config.gd` | Cả 3 constants: `VALUE_CURRENT, VALUE_CHECK, VALUE_IQ` (Dòng 10-12) |
| Cấu hình Double Tap | `tutorial_page.gd` | Hàm `_double_tap_window_sec()` gọi `ABTestManager.doubletap_protect.window_sec()` kết hợp logic conflict (Dòng 1065) |

STATUS: COMPLETE
