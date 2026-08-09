# GEM-R4-004 Báo cáo Đặc tả Random, DDA và Sequence Chọn Level

**STATUS**: COMPLETE  
**GENERATED_AT**: 2026-08-08 17:45:00  
**SOURCE_ROOT**: `D:\Projects\_GameExtract\Main_Meokdoku`

## 1. Cơ chế DDA (Dynamic Difficulty Adjustment)

Chỉ số `_current_strategy` được cập nhật và giới hạn liên tục qua các hàm win/fail trong `game_state.gd`:

- **Thất bại (Fail):**
  - Tăng `_consecutive_fails`. Threshold: `1` (level < 21), `2` (level >= 21).
  - Nếu `_consecutive_fails >= threshold`: `_current_strategy -= 1` (giáng cấp ngay lập tức).
- **Chiến thắng (Win):**
  - Tăng `_consecutive_clean_wins` nếu không sai lỗi nào (`_current_level_dirty == false`). Threshold: `2` (level < 51), `1` (level >= 51).
  - Nếu đủ threshold: `_current_strategy += 1`.
  - **Demote sau Win (DDA A/B Config)**: Nếu người chơi dùng Tool/Hồi sinh hoặc Retry nhiều lần, hệ thống A/B test (cờ `ABTestManager.dda_rank`) có thể trigger giáng cấp ngay sau khi Win.
  - **Trì hoãn (Pending Demote)**: Nếu level kế tiếp là Hard Level (chia hết cho 10) hoặc Special Level, quá trình giáng cấp bị ghim lại (`_dda_pending_demote = true`) để không giáng cấp nhầm bài Hard, mà sẽ giáng cấp vào ván thường ngay sau đó.

*Persist Timing*: Mọi thay đổi về strategy, win/fail count đều được ghi xuống đĩa ngay lập tức qua `_save_data()` ở cuối hàm `on_level_won` và `on_level_failed`.

## 2. Sequence Chọn Level (Mốc 1 - 250)

| Mốc Level | Size (sz) | Nguồn Pool | DDA Strategy Max | Tính Tất định (Determinism) | Cản trở RNG / Daily |
|---|---|---|---|---|---|
| **1 – 5** | `SIZES[lv-1]` | `get_next_entry` | 1 | **Tất định 100%**. Luôn là Strategy 1 (Rank 1, Tier "N"). | Không có RNG, không có Daily Easy. |
| **6 – 20** | `SIZES[lv-1]` | `get_next_entry` | 2 | **Tất định theo State**. Strategy 1 hoặc 2 tùy thuộc vào số trận thắng/thua. | Không có RNG, không có Daily Easy. |
| **21 – 50** | `SIZES[lv-1]` | `get_next_entry` | 3 | **Phụ thuộc RNG một phần**. Nếu DDA đẩy lên Strategy 3, hàm `randi_range(2, 3)` sẽ đổ xí ngầu để lấy 2 hoặc 3. | Bị trừ 1 cấp nếu đây là trận đấu đầu tiên trong ngày (Daily First Easy). |
| **51 – 100** | `SIZES[lv-1]` | `get_next_entry_main` | 4 | **Bị xáo trộn bởi LK_Mod & RNG**. Dùng hệ thống ghép bài chu kỳ (`since_lk`), đổ xí ngầu `randi_range(2, strat)`. | Có Daily First Easy. Sinh ra Tier "H" nếu lấy trúng Strategy 5 (dành cho Hard Level). |
| **101 – 200** | `_SIZES_101_PLUS` | `get_next_entry_main` | 5 | Trộn pool ba vòng, đổ xí ngầu `randi_range(2, strat)`. Chiến lược tối thiểu (min) bị đẩy lên 2. | Như trên. |
| **201 – 250** | `_SIZES_101_PLUS` | `get_next_entry_main` | 6 | Như trên, Strategy max vươn tới 6 (tương đương Rank 5 Tier "H"). | Như trên. |
| **Hard Level** | Tuỳ biến | `get_next_entry` / `main` | N/A | Cứ mỗi level chia hết cho 10 (từ lv 20 trở đi), ép cứng Rank 5 Tier "N". | Bỏ qua toàn bộ RNG và DDA. |

## 3. Contract Random của Godot (LevelData.gd)

**Caller:** `LevelData.get_level_entry()` (Dòng 627)
**Cú pháp:** `strategy = randi_range(2, strategy)`
**Đặc tả API:**
- Hàm `randi_range(from, to)` của Godot trả về một số nguyên ngẫu nhiên trong khoảng **đóng (inclusive)** `[from, to]`.
- Phụ thuộc hoàn toàn vào Global RandomNumberGenerator của Godot. Nếu tại thời điểm khởi động game không có hàm `randomize()` hay `seed(x)` nào được gọi, Godot sẽ dùng chuỗi pseudo-random dựa trên seed của hệ điều hành (System Time).
- Tính ngẫu nhiên này có nghĩa là cùng một file save (cùng progress index), hai người chơi ở cấp DDA cao có thể bốc trúng Rank khác nhau (2 hoặc 3), dẫn đến rẽ nhánh pool hoàn toàn khác nhau.

**Cản trở Daily (State-time):**
- Hàm `GameState.is_daily_first_easy_available()` so sánh chuỗi ngày tháng lưu trong config (`_daily_first_easy_date`) với thời gian thực `TimeSystem.get_current_date_string()`. Trận đầu tiên trong ngày luôn bị giáng 1 Strategy.

## 4. Khuyến nghị Fixture (Dành cho Unit Test / Codex)

Để tự động hóa việc so chuỗi chọn level (Parity check) giữa Godot và Unity, Codex BẮT BUỘC phải giả lập (Mock) 3 thành phần sau để loại bỏ nhiễu RNG và Time:

1. **Mock RNG:** Override hàm `randi_range(a, b)` để luôn trả về cận trên `b` (hoặc cận dưới `a`), chặn sự rẽ nhánh ngẫu nhiên.
2. **Mock Time:** Override hàm `TimeSystem.get_current_date_string()` trả về một chuỗi tĩnh (ví dụ: `"2026-01-01"`) hoặc chặn không cho thực thi logic Daily First Easy ở đoạn `level_data.gd:630`.
3. **Mock User Perf:** Giả lập toàn bộ là Clean Win (không có fail, `dirty = false`) để đẩy `_current_strategy` lên kịch trần (max cap).

*Bằng chứng tham chiếu:*
- Random gọi tại: `level_data.gd:627`
- Daily First Easy gọi tại: `level_data.gd:630`
- DDA Win/Fail lưu tại: `game_state.gd:1642` và `1749`
