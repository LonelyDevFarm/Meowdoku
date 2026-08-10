# Báo cáo đặc tả kỹ thuật: GEM-R12-019 (Home Entries, Streak & Anchor)

**Nguồn đối chiếu:** `D:\Projects\_GameExtract\Main_Meokdoku`
**Mục tiêu:** Đặc tả toàn bộ các Entry Cell ngoài Home (Daily Challenge, Streak lớn/nhỏ, Rank), cơ chế xếp Layout, thiết kế màn hình Streak và cơ chế chạy Anchor với nút Settings.

---

## 1. Cơ chế Layout Chọn Lọc (Home Page Layout Logic)
File kịch bản chính: `scripts/module/home/view/home_page.gd` (Hàm `_apply_home_layout`, `_apply_streak_dead_layout`, `_apply_rank_entry_layout`).

Cơ chế phân loại và hiển thị `DailyStreakLayout`:
1. **Kiểm tra A/B Test cơ bản**: Nếu `ABTestManager.daily_streak.is_enabled()` là `false`, toàn bộ `DailyStreakLayout` sẽ ẩn. Nếu `true`, mặc định sinh ra `dc_cell` (Daily Challenge) và `streak_cell` (Streak Lớn) gắn vào `_dc_slot` và `_streak_slot`.
2. **Kiểm tra trạng thái Rank (Ưu tiên đè Streak Lớn)**:
   - Đọc `RankActivityManager.has_home_entry()`.
   - Nếu có Rank: Ẩn `streak_cell` (Lớn), hiển thị bộ đôi `streak_mini_cell` (Streak Nhỏ) và `rank_cell` (Hoạt động Rank).
   - Nếu không có Rank: Chỉ hiện `streak_cell` Lớn (Và `dc_cell`).
3. **Trạng thái "Streak Dead" (Challenge-only Locked Layout)**:
   - Nếu biến thể AB Test là `is_challenge_only()` VÀ `DailyEntryState.compute_state() == LOCKED` (Daily Challenge đang khoá), thì coi như "Streak Dead".
   - Kết quả: Ẩn luôn `streak_cell` (Lớn). Di chuyển `dc_cell` ra chính giữa vùng `DailyStreakLayout` bằng phép tính tọa độ tĩnh: `position.x = size.x * 0.5 - dc_slot.x - dc_slot.size.x * 0.5`.

---

## 2. Đặc tả các Node Cell (Các thẻ Entry ngoài Home)

### A. Daily Challenge Entry (`daily_challenge_entry_cell`)
- **Hierarchy:** `$DcEntryImg` (chứa `StateNormal`, `StateLocked`, `StateDone`) và nút bấm `$ClickBtn`.
- **States & Handlers:**
  - Lắng nghe `ClockTicker.second_tick` để cập nhật đồng hồ mượt mà.
  - Tùy vào state từ `DailyEntryState.compute_state()`:
    - `NORMAL`: Hiện ngày (`today_date_text`), đếm ngược (`countdown_text`).
    - `LOCKED`: Hiện `DAILY_CHALLENGE_UNLOCK_AT` với cấp độ yêu cầu.
    - `DONE`: Hiện ngày, thời gian hoàn thành (`done_time_text`) và thứ hạng (`done_rank_text`).
  - **Click:** Gọi `UIManager.show_ui(UiName.DAILY_GAME)` và tự ẩn màn `HOME`.

### B. Streak Entry Lớn (`streak_entry_cell`)
- **Hierarchy:** `$StreakEntryImg` (chứa `StateChecked`, `StateUnchecked`), `$CountBadge`, `$ClickBtn`.
- **States & Handlers:**
  - Đọc `StreakManager.can_checkin_today()`. Nếu false => Đã điểm danh (Bật `StateChecked`).
  - Gắn nhãn số từ `StreakManager.get_display_streak()`.
  - Lắng nghe sự kiện `StreakManager.streak_updated` để tự làm mới. Bấm vào gọi `StreakPage.open_main()`.

### C. Streak Mini Entry (`streak_mini_entry_cell`)
- Tương tự Streak Lớn nhưng thu nhỏ không gian, có thêm cụm Particles hạt phát sáng `$Content/Bg/Bg/Particle` khi ở trạng thái đã điểm danh. Bấm vào cũng mở `StreakPage.open_main()`.

### D. Rank Activity Entry (`rank_activity_entry_cell`)
- **Hierarchy:** `$StateOpen` (Chứa rương Spine), `$StateActive` (Chứa Countdown, Medal), `$ClickBtn`.
- **Hoạt ảnh Rương (Spine):** 
  - Nếu `pending_reward` có giá trị, thay da (Skin) rương thành `RankBox3`, `RankBox2`, `RankBox1` dựa vào rank bằng hàm `set_skin_by_name`. Chơi animation `"Idle"` liên tục.
  - Bấm vào mở `RankActivityManager.open_home_entry()`. Cập nhật Text qua `_format_hms`.

---

## 3. Đặc tả Streak Page và Cơ chế Anchor

### A. Màn hình Streak (`streak_page.tscn`, `streak_page.gd`)
- **Cấu trúc:** 
  - `$StreakContent/Top/BackBtnGroup`: Nút Back (Gắn kèm `$Top/YFollow` để tracking vị trí).
  - `$StreakContent/StreakPanel`: Gồm 7 ô Slot (`SlotWed` đến `SlotTue`).
  - Chứa `ClaimBtn` và `GoToPlayBtn`. AnimationPlayer điều khiển hiệu ứng nảy số, chuyển tuần (NewWeek).
- **Hoạt ảnh & State:** Hỗ trợ 3 State `MAIN` (Mở xem), `LIT` (Từ Home bấm vào khi đủ điều kiện), `SETTLE` (Kết toán check-in). Có cả hệ thống Delay phức tạp phục hồi chuỗi bị gãy (Recover/Backfill).

### B. Cơ chế HomeSettingAnchor (`follow_home_settingbtn_y.gd`)
- **Cơ chế hoạt động thực tế:** Thay vì màn Settings, chính biến `$StreakContent/Top` của màn Streak Page mới dùng Script `follow_home_settingbtn_y.gd`.
- **Cách thức tracking:**
  - `home_page.gd` chạy lệnh `HomeSettingAnchor.set_settingbtn_y(gb.global_position.y + ...)` để lưu vị trí Y của nút Settings.
  - Script Extension trên màn Streak Page lắng nghe `HomeSettingAnchor.anchor_changed` và `visibility_changed`.
  - Nếu tọa độ lệch (`delta = anchor_y - cur_center_y`), nó sẽ tự động tăng/giảm thuộc tính `offset_top` và `offset_bottom` để dịch chuyển Node Top của màn Streak (bao gồm cả BackBtnGroup) sao cho thẳng hàng tuyệt đối với nút Settings đang nằm mờ ở phía sau lưng. (Cần `await get_tree().process_frame` để Godot tính toán kích thước thực tế trước khi căn).

---

## 4. Danh sách Asset, Font & Localization
- **Asset/Prefab:**
  - `res://scripts/module/daily_streak/ui/streak_entry_cell.tscn`
  - `res://scripts/module/daily_streak/ui/streak_mini_entry_cell.tscn`
  - `res://scripts/module/rank_activity/ui/rank_activity_entry_cell.tscn`
- **Spine:** `RankBox1`, `RankBox2`, `RankBox3` (Bên trong prefab Rank Entry).
- **Localization:** 
  - `DAILY_CHALLENGE_UNLOCK_AT`: Thông báo level mở khoá Daily.
  - `DAILY_STREAK_BEST_FORMAT`: Chuỗi Format kỷ lục chuỗi Streak (Streak Page).
  - `WEEKDAY_SUN`, `WEEKDAY_MON`, ...: Tên các ngày trong tuần (Streak Page).

---

## 5. Dependency Map
- **UI & Flow:** `UIManager`, `ClockTicker` (Gọi hàm tick để update đồng hồ 1s/1 lần).
- **Model / Manager:** `DailyEntryState` (Xử lý model Daily), `StreakManager` (Lưu data Chuỗi ngày điểm danh), `RankActivityManager` (Event Rank, Rương thưởng), `ABTestManager`.
- **Hardware:** `VibrateManager`, `Tracker` (Gửi Analytics `Btn.STREAK`, `Btn.DAILY_PLAY`).

---

## 6. Bảng Evidence Kỹ thuật

| Loại | Chi tiết Kỹ thuật | File Nguồn | Node / Hàm / Dòng mã | Mức chắc chắn |
| :--- | :--- | :--- | :--- | :--- |
| **FACT** | Layout: Check Rank hiển thị | `home_page.gd` | `_apply_rank_entry_layout()` (Dòng 747) | 100% |
| **FACT** | Layout: Challenge-only Locked | `home_page.gd` | `_apply_streak_dead_layout()` kiểm tra `is_challenge_only()` và trạng thái LOCKED (Dòng 733) | 100% |
| **FACT** | YFollow Script | `streak_page.tscn` | `$StreakContent/Top/YFollow` đính `follow_home_settingbtn_y.gd` | 100% |
| **FACT** | Anchor Sync Logic | `follow_home_settingbtn_y.gd` | `_on_sync_requested()` đổi `offset_top` và `offset_bottom` (Dòng 45) | 100% |
| **FACT** | Đồng hồ (Clock Ticker) | `daily_challenge_entry_cell.gd` | Kết nối `ClockTicker.second_tick` (Dòng 47) | 100% |
| **FACT** | Đổi Skin Rương | `rank_activity_entry_cell.gd` | `sk.set_skin_by_name(skin)` dựa trên Rank 1, 2, 3 (Dòng 107) | 100% |
| **INFERENCE** | Settings không có YFollow | `setting_page.tscn` | Không thấy sử dụng class này trong Hierarchy. Settings căn tự động bằng script của chính nó. | 100% |

STATUS: COMPLETE
