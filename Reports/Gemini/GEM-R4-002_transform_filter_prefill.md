# GEM-R4-002 Báo cáo Phân tích Transform, Filter, và Prefill của Ngân hàng Dữ liệu

**STATUS**: COMPLETE  
**GENERATED_AT**: 2026-08-08 17:15:00  
**SOURCE_ROOT**: `D:\Projects\_GameExtract\Main_Meokdoku`

## 1. Transform 0–7

Hàm `apply_transform` nhận giá trị `transform` (gọi là `t` từ 0-7) và thực hiện biến đổi toạ độ hình học (hoán vị) cho `regionMap` và `solution`.
Công thức giải mã: `mirror = t / 4`, `rot = t % 4`. Lưu ý: nhánh `mirror == 2` trong code là dead code do `t / 4` lớn nhất chỉ bằng 1.

| Transform ID | Nhóm Mirror | Số lần Rotate 90° (rot) | Phép biến đổi thực tế |
|---|---|---|---|
| **0** | `mirror = 0` | 0 | Không thay đổi (Bản gốc) |
| **1, 2, 3** | `mirror = 0` | 1, 2, 3 | Xoay bảng 90°, 180°, 270° (ngược hoặc xuôi chiều kim đồng hồ tuỳ theo quy ước toạ độ) |
| **4** | `mirror = 1` | 0 | Lật đối xứng theo trục dọc (Horizontal Flip: `c` đổi thành `sz - 1 - c`) |
| **5, 6, 7** | `mirror = 1` | 1, 2, 3 | Lật ngang, sau đó xoay 90°, 180°, 270° |

*Thay đổi Metadata:*
Toàn bộ `entry` gốc được clone, ghi đè `regionMap` và `solution` mới. Giá trị `transform` được lưu vào `_bank_transform`.

## 2. Filter loop

Hàm `_get_next_entry_with_filter` là lớp màng lọc cấp cao, gọi vòng lặp vô tận (được bảo vệ bằng budget) để đảm bảo đầu ra hợp lệ:

| Bước | Điều kiện | Progress mutation | Remaining | Return / Continue |
|---|---|---|---|---|
| 1. Lấy entry thô | Gọi `get_next_entry_main` (lv >= 51) hoặc `get_next_entry` | Các hàm con này tự advance progress nội bộ trong RAM (không persist) | Không đổi ở vòng ngoài | - |
| 2. Kiểm tra Threshold | `_thresh = single_limit_at(...)` >= 0 và không rỗng | Không trực tiếp sửa progress, nhưng do entry bị bỏ qua, lần lặp sau sẽ lấy entry khác (do RAM progress đã tiến lên) | Giảm 1 nếu vi phạm và budget > 1 | `continue` (lấy câu hỏi tiếp theo) |
| 3. Budget cạn kiệt | `remaining <= 1` hoặc trùng `_seen` key | Ghi log báo lỗi: "budget cạn kiệt" hoặc "vòng lặp hết ngân hàng" | - | **Return** entry đang lỗi để game không bị treo |
| 4. Hợp lệ | `_single <= _thresh` hoặc source được miễn | Gọi `GameState.commit_bank_progress()` để lưu vĩnh viễn xuống đĩa (Save slot) | - | **Return** entry hợp lệ |

## 3. Config effects (Lọc Single-Region)

Bộ lọc `single_region_num` có 2 cấp độ được kích hoạt độc lập:

- **Coarse Limit (Lọc thô):**
  - Chạy ngay bên trong `get_next_entry_main`/`get_next_entry`.
  - Miễn trừ (Exempt): Các pool `lk_mod`, `sp`, `lk`.
  - Điều kiện: Cứ hễ config khác `DEFAULT`, tự động loại bỏ mọi màn chơi có **nhiều hơn 2** vùng kích thước 1 ô (single-cell region).

- **Strict Limit (Lọc tinh):**
  - Chạy ở vòng lặp ngoài cùng `_get_next_entry_with_filter` dựa trên `single_limit_at(level_num, rank)`.
  - Các cấu hình (Values):
    - `STRICT`: Chỉ cho phép tối đa 1 vùng-1-ô từ level 21 trở đi.
    - `ALL_ONE`: Khắt khe nhất, luôn luôn tối đa 1 vùng-1-ô ở mọi level.
    - `ZERO_51`: Từ level 51, Rank 1 chỉ cho phép 1 vùng, Rank > 1 **không cho phép bất kỳ vùng 1 ô nào** (`limit = 0`). Dưới 51 như `STRICT`.
    - `ZERO_101`: Từ level 101, áp dụng luật khắt khe như `ZERO_51`. Dưới 101 như `STRICT`.
  - Miễn trừ (Exempt): Pool `lk_mod`, `sp`, `lk`.

## 4. Fallback / error behavior (Khi Solution hỏng)

- Khi `QueendokuCore.validate_solution_entry(entry, sz)` trả về False (lời giải không khớp luật sudoku/vùng):
  - In ra lỗi.
  - Tự động cộng tay `idx += 1` vào `lkprog` hoặc `main_progress` (chỉ ở RAM).
  - Trừ `remaining_attempts`.
  - **Khử Strict Rank:** Gắn `cur_strict = false` để phép lọc `lk_mod` có thể lùi xuống sử dụng thuộc tính `maxR` thay vì `r` (rộng lượng hơn).
  - Tiếp tục (`continue`) lặp. Nếu hết budget `remaining_attempts <= 1`, trả về chính entry lỗi để khỏi crash cứng loop.

## 5. Entry output metadata và Prefill (Pre-cat)

- Dữ liệu thêm (Injected Metadata) có trong entry trả về:
  - `_bank_source` và `_bank_source_main`: Tên pool ("regular", "lkstyle", "gc", "lk_mod", "sp", "lk").
  - `_bank_idx`: Vị trí lấy (1-based index).
  - `_bank_tier`: Tier phân loại ("N", "H" hoặc rỗng).
  - `_bank_rank`: Độ khó Rank (1-5).
  - `_bank_transform`: Trạng thái đã biến đổi (0-7).
- Dữ liệu Prefill (Hỗ trợ tân thủ):
  - Hàm `compute_prefill` sinh ra mảng `[r, c]` chứa tọa độ của một con mèo để hiện sẵn trên lưới (không thể tẩy).
  - Áp dụng: Chỉ cho Level 1 đến 10.
  - Level 1-6: Tìm vị trí mèo nằm ở vùng có kích thước > 1 ô.
  - Level 7-10: Tìm vị trí mèo nằm ở vùng có kích thước đúng bằng 1 ô.
  - Nếu không tìm thấy, mặc định lấy toạ độ ở hàng đầu tiên: `[0, solution[0]]`.

## 6. Randomness and determinism

Mức độ khó (Rank/Tier) được quyết định trước khi bốc bài từ Bank, chịu ảnh hưởng bởi 3 yếu tố:
1. **DDA (Dynamic Difficulty Adjustment):** `strategy = GameState.get_current_strategy()`. Bị giới hạn (clamp) theo khoảng level:
   - Level < 21: Max strategy = 2.
   - Level 21 - 50: Max strategy = 3.
   - Level >= 51: Max strategy = 4.
2. **Randomness (Giảm nhiệt):** Nếu `strategy >= 3`, game sẽ thả xúc xắc `randi_range(2, strategy)`. Điều này có nghĩa là dù DDA đánh giá người chơi ở cấp 4, game vẫn có tỷ lệ tung ra màn chơi dễ hơn (cấp 2 hoặc 3) để người chơi đỡ áp lực.
3. **Daily First Easy:** Nếu cờ `is_daily_first_easy_available` đang bật và `strategy > 1`, game ưu tiên trừ đi 1 cấp khó (`strategy -= 1`) và tắt cờ này. Đảm bảo trận đầu ngày luôn thư giãn.
4. **Hard Level Cố định:** Cứ mỗi 10 level (20, 30, 40...) và >= 21 thì sẽ bỏ qua DDA, ép cứng lấy bài `Rank 5`, Tier `"N"`.

## 7. LevelGenerator Color Palette

- Cấp phát màu (`compute_color_map_with_seed`):
  - Lưới màu RGB gồm 12 màu tĩnh, cố định sẵn.
  - Xây dựng danh sách kề (Adjacency list) để các vùng chung biên giới biết nhau.
  - Sắp xếp ưu tiên: Vùng nào giáp nhiều vùng khác (degree lớn nhất) sẽ được ưu tiên chọn màu trước.
  - Thuật toán trộn (`seed != 0`): Sử dụng chuỗi sinh số giả ngẫu nhiên LCG (Linear Congruential Generator) thuật toán Fisher-Yates shuffle để hoán vị thứ tự ưu tiên vùng, làm cho bài giải trông khác biệt màu sắc ngay cả khi bị trùng sơ đồ.
  - Cơ chế Greedy: Chọn màu từ 12 màu sao cho chưa từng được dùng (`used_colors`) VÀ có "khoảng cách Euclidean màu" (`sqrt(dr^2+dg^2+db^2)`) xa nhất với các màu liền kề đã tô, tối đa hoá sự tương phản thị giác giữa các vùng.

## 8. Điểm chưa xác định

- Tại hàm `compute_color_map_with_seed`, mảng `used_colors` lưu các chỉ mục màu (0-11) đã được chọn. Khi `size > 12` (nếu tồn tại trong tương lai), thuật toán sẽ hết màu chưa sử dụng do mảng màu gốc chỉ có 12 mã RGB, có thể dẫn đến fallback lỗi (toàn bộ màu về sau lấy mã `0`). Tuy nhiên hiện tại config size lớn nhất là 12, số lượng region tương đương 12, nên thuật toán vừa khít hoàn hảo.
