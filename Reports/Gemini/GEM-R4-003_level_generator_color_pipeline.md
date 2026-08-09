# GEM-R4-003 Báo cáo Đặc tả Pipeline LevelGenerator và Thuật toán Màu

**STATUS**: COMPLETE  
**GENERATED_AT**: 2026-08-08 17:30:00  
**SOURCE_ROOT**: `D:\Projects\_GameExtract\Main_Meokdoku`

## 1. Mạng lưới File và Hàm tham gia Color Pipeline

| Component | File gốc | Danh sách hàm (Signatures) | Nhiệm vụ chính |
|---|---|---|---|
| **LevelGenerator** | `level_generator.gd` | `compute_color_map(size, regions)`<br>`compute_color_map_for_rgb(size, regions, rgb)`<br>`compute_color_map_with_seed(size, regions, seed)`<br>`compute_color_map_for_rgb_with_pattern(size, regions, rgb, pattern_regions)` | Tính toán bản đồ màu (color map) bằng giải thuật Greedy dựa trên không gian 3D RGB. Phân chia Pattern tối/sáng. |
| **LevelGenerator (Lab)** | `level_generator.gd` | `_srgb_to_lab(r, g, b)`<br>`_srgb_linear(c)`<br>`_lab_f(t)`<br>`_lab_dist(a, b)`<br>`compute_color_map_for_lab(...)`<br>`compute_color_map_for_lab_with_pattern(...)` | Chuyển đổi mã màu sang không gian CIE-L*a*b* để đo lường khoảng cách màu chuẩn xác hơn so với RGB. |
| **BoardView** | `board_view.gd` | `setup(puzzle_size, regions, color_map, pattern_regions)`<br>`_colors_from_rgb(rgb)` | Trực tiếp ghi đè `color_map` gốc từ GamePage thông qua các hệ thống A/B test, thiết lập hằng số Warm/Cool pool. |

## 2. Các Thuật Toán Cốt Lõi

### A. Hệ thống không gian màu CIE L*a*b* (CIE 1976)
- **Công thức Linear hóa (`_srgb_linear`)**: Nếu `c > 0.04045`, áp dụng `pow((c + 0.055) / 1.055, 2.4)`, ngược lại `c / 12.92`.
- **Ma trận D65 (`_srgb_to_lab`)**:
  - `X = (R * 0.4124 + G * 0.3576 + B * 0.1805) / 0.95047`
  - `Y = (R * 0.2126 + G * 0.7152 + B * 0.0722) / 1.0`
  - `Z = (R * 0.0193 + G * 0.1192 + B * 0.9505) / 1.08883`
- **Khoảng cách (`_lab_dist`)**: Đo bằng Euclidean trong không gian Lab 3D: `sqrt(dL^2 + da^2 + db^2)`. Khoảng cách Lab phản ánh độ tương phản thị giác của người tốt hơn RGB.

### B. Thuật toán chọn màu Greedy (Maximizing Minimum Distance)
1. **Xây dựng danh sách kề (Adjacency List)**: Duyệt mảng 2D `regions`, nếu hai ô liền kề có `region_id` khác nhau, ghi nhận chúng là hàng xóm.
2. **Sắp xếp ưu tiên (Priority Order)**: Mặc định sắp xếp các Region giảm dần theo số lượng hàng xóm (Region nào giáp nhiều vùng khác nhất sẽ được tô trước).
3. **Phân bổ màu**: Duyệt từng Region theo thứ tự. Lọc ra các màu đã được dùng bởi hàng xóm (`adj_colors`). Quét toàn bộ bảng màu, chọn màu **chưa được dùng ở bất kỳ đâu** (`if used_colors.has(ci): continue`) sao cho có khoảng cách (RGB hoặc Lab) **xa nhất so với những màu liền kề**.

### C. Thuật toán Pattern phân tách Tối/Sáng (`with_pattern`)
Được dùng khi màn chơi có chỉ định `pattern_regions` (VD: tạo hình con mèo, ngôi sao).
1. Tính độ sáng Luminance (`0.299*R + 0.587*G + 0.114*B`) cho toàn bộ Palette.
2. Sắp xếp Palette từ tối nhất đến sáng nhất.
3. Chia Palette thành `dark_pool` (có độ dài bằng số lượng `pattern_regions`) và `light_pool` (số màu còn lại).
4. Tô các vùng Pattern bằng `dark_pool` trước để làm nổi bật hình dáng.
5. Tô các vùng còn lại (nền) bằng `light_pool`. Nếu `light_pool` rỗng, fallback về `dark_pool`.

### D. Thuật toán LCG Seed (Fisher-Yates Shuffle)
- **Công thức LCG**: `rng_state = (rng_state * 1664525 + 1013904223) & 2147483647` (Khớp với chuẩn C/C++ LCG).
- Nếu `seed != 0`: Mảng Priority Order không giữ nguyên trạng thái tĩnh mà sẽ bị xáo trộn (Shuffle ngược từ `size - 1` về `0`). Điều này khiến thứ tự ưu tiên vùng tô màu bị thay đổi, sinh ra kết quả màu ngẫu nhiên nhưng vẫn đảm bảo tính tất định.

## 3. Quá trình Ghi đè (Override) Dữ liệu Đầu Vào/Đầu Ra

**Caller gốc (GamePage / DailyGamePage)**:
- Truyền `color_seed` và `regions` vào `LevelGenerator.compute_color_map_with_seed`. Nhận được mảng `color_map`.
- Sau đó gọi `_board_view.setup(..., color_map)`.

**Hành vi Cướp quyền ở BoardView (`board_view.gd:255-370`)**:
Đáng chú ý nhất, `color_map` được truyền từ Caller sẽ bị **phớt lờ/vứt bỏ hoàn toàn** nếu game đang chạy thực tế (`not is_editor_hint`) và cờ `ABTestManager.region_color` yêu cầu sử dụng Palette V3 đến V9, hoặc Warm/Cool/Balanced. BoardView sẽ tự gọi lại `LevelGenerator.compute_color_map_for_lab` (hoặc RGB) với Palette tĩnh nhúng thẳng trong `board_view.gd`.

## 4. Các Hằng số / Palette Mặc Định (Từ BoardView)

- **WARM_POOL_RGB**: Chứa 6 màu nghiêng tông ấm: `[248, 155, 229], [205, 164, 0], [168, 109, 74], [251, 217, 131], [250, 157, 92], [211, 111, 143]`.
- **COOL_POOL_RGB**: Chứa 6 màu nghiêng tông lạnh: `[137, 121, 218], [139, 213, 125], [56, 169, 192], [42, 140, 83], [80, 118, 165], [165, 198, 231]`.

Các tổ hợp A/B Test động dựa trên Warm/Cool:
- **`is_all_warm()`**: Lấy toàn bộ 6 màu WARM + bù thêm màu từ COOL nếu Puzzle Size > 6.
- **`is_all_cool()`**: Lấy toàn bộ 6 màu COOL + bù thêm màu từ WARM nếu Puzzle Size > 6.
- **`is_temp_balanced()`**: Lấy chính xác `ceil(size / 2)` từ WARM và phần còn lại từ COOL ghép lại.

## 5. Fallback và Điểm chưa xác định

- **Fallback Hết màu**: Lệnh `if used_colors.has(ci): continue` khóa chặn một màu được sử dụng 2 lần trong 1 bảng. Bảng màu tĩnh cung cấp đủ 12 màu cho mức size lớn nhất hiện tại (12x12). Nếu size vượt 12, hoặc số regions > size, thuật toán sẽ cạn kiệt màu chưa sử dụng, vòng lặp không thể gán `best_color` mới và liên tục trả về `0` (văng lỗi trùng màu toàn bộ).
- **Điểm chưa xác định**: `ABTestManager.region_color` cung cấp một lượng lớn Palette nhúng cứng mã nguồn trong file view (`v3`, `v5` -> `v9`). Không rõ quy trình quản lý các hằng số màu này (liệu có được đồng bộ từ CMS/Backend hay designer hardcode trực tiếp vào client). Điều này có nguy cơ phình to file `board_view.gd` nếu A/B test được triển khai dài hạn.
