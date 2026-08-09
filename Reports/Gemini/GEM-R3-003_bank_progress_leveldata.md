# GEM-R3-003 Báo cáo Phân tích Tiến độ (Bank Progress & Level Data)

**STATUS**: COMPLETE  
**GENERATED_AT**: 2026-08-08 16:15:00  
**SOURCE_ROOT**: `D:\Projects\_GameExtract\Main_Meokdoku`

## 1. Mọi hàm đọc/tăng/ghi tiến độ bank, main và lkmod (game_state.gd)

| File | Dòng | Hàm/biến | Hành vi nguyên bản | Dữ liệu đọc | Dữ liệu ghi | Ghi chú |
|---|---|---|---|---|---|---|
| `game_state.gd` | 59 | `_bank_progress` | Lưu tiến độ dạng dictionary | N/A | N/A | Khai báo biến |
| `game_state.gd` | 62 | `_main_bank_progress` | Lưu tiến độ main dictionary | N/A | N/A | Khai báo biến |
| `game_state.gd` | 65 | `_lkmod_progress` | Lưu tiến độ lkmod dictionary | N/A | N/A | Khai báo biến |
| `game_state.gd` | 1470 | `get_bank_index(sz: int, rank: int, tier: String = "") -> int` | Lấy bank index cũ | `_bank_progress.get(key, 0)` | N/A | Key = `"%d_%d%s" % [sz, rank, "_H" nếu tier=="H"]` |
| `game_state.gd` | 1476 | `advance_bank_index(sz: int, rank: int, tier: String = "", persist: bool = true) -> void` | Tăng bank index cũ | Đọc `_bank_progress` | `_bank_progress[key] += 1` | Gọi `_save_data()` nếu `persist == true` |
| `game_state.gd` | 1483 | `get_main_progress(sz: int, rank: int, tier: String = "") -> Dictionary` | Lấy main progress | Đọc `_main_bank_progress` | Khởi tạo nếu thiếu: `{"lk_mod": 0, "regular": 0, "lkstyle": 0, "transform": 0}` | Key giống get_bank_index |
| `game_state.gd` | 1490 | `set_main_progress(sz: int, rank: int, tier: String, progress: Dictionary, persist: bool = true) -> void` | Ghi đè main progress | N/A | `_main_bank_progress[key] = progress` | Gọi `_save_data()` nếu `persist == true` |
| `game_state.gd` | 1497 | `get_lkmod_progress(sz: int, rank: int) -> Dictionary` | Lấy lkmod progress | Đọc `_lkmod_progress` | Khởi tạo nếu thiếu: `{"idx": 0}` | Key = `"%d_%d" % [sz, rank]` (không có tier) |
| `game_state.gd` | 1504 | `set_lkmod_progress(sz: int, rank: int, progress: Dictionary, persist: bool = true) -> void` | Ghi đè lkmod progress | N/A | `_lkmod_progress[key] = progress` | Gọi `_save_data()` nếu `persist == true` |
| `game_state.gd` | 1513 | `commit_bank_progress() -> void` | Lưu xuống đĩa | N/A | `_save_data()` | Force lưu các thay đổi pending |

## 2. Snapshot và Commit tiến độ

- **Snapshot**: Các hàm `get_bank_progress_snapshot()`, `get_main_bank_progress_snapshot()`, `get_lkmod_progress_snapshot()` được gọi để lấy toàn bộ dữ liệu. Dữ liệu ra là bản sao chép sâu (`duplicate(true)`) của các dictionary tương ứng để tránh bị thay đổi bên ngoài. Các snapshot này được dùng để ghi log vào hàm `record_puzzle`.
- **Cờ persist (bool)**: 
  - Nếu `persist = false`: Các hàm `advance_*` và `set_*` chỉ thay đổi Dictionary trên RAM. Cờ này được `level_data.gd` sử dụng trong lúc lặp (while loop) dò tìm puzzle hợp lệ, tránh việc gọi I/O `_save_data()` liên tục mỗi khi skip qua một câu đố lỗi hoặc vi phạm luật `single_region_num`.
  - Nếu `persist = true`: Ngay sau khi gán RAM, `_save_data()` được gọi để lưu cấu hình xuống đĩa.
  - **Commit**: Sau khi thuật toán lọc thành công trả về entry, hàm `_get_next_entry_with_filter` (dòng 694) gọi `GameState.commit_bank_progress()` để lưu trạng thái một lần duy nhất.

## 3. Lời gọi GameState trong LevelData và BankData

| Nơi gọi | Lệnh gọi GameState | Vai trò |
|---|---|---|
| `bank_data.gd` | KHÔNG CÓ | BankData hoàn toàn thuần tuý đọc file tĩnh. |
| `level_data.gd` | `get_current_strategy()` (d57) | Tính toán level rank/tier |
| `level_data.gd` | `set_current_strategy(2)` (d59) | Điều chỉnh chiến thuật nếu level >= 51 |
| `level_data.gd` | `is_daily_first_easy_available()` (d630) | Đánh giá giảm độ khó ván đầu ngày |
| `level_data.gd` | `consume_daily_first_easy_and_mark()` (d633) | Xác nhận đã dùng quyền giảm độ khó |
| `level_data.gd` | `get_bank_index(sz, rank, tier)` (d127, d428) | Đọc index cũ để fetch, hoặc để di cư (migration) |
| `level_data.gd` | `advance_bank_index(...)` (d78, 163, 176) | Tăng index cũ (có skip nếu check failed) |
| `level_data.gd` | `get_main_progress(sz, rank, tier)` (d84, 88, 420, 513) | Đọc progress của flow cấp >= 51 |
| `level_data.gd` | `set_main_progress(...)` (d86, 91, 434, 444, 515, 535) | Cập nhật idx, since_lk, transform |
| `level_data.gd` | `get_lkmod_progress(sz, rank)` (d81, 436, 509, 530) | Đọc progress của pool lk_mod |
| `level_data.gd` | `set_lkmod_progress(...)` (d83, 511, 532) | Cập nhật idx cho pool lk_mod |
| `level_data.gd` | `commit_bank_progress()` (d694) | Xả dữ liệu xuống đĩa sau vòng lặp tìm puzzle |

## 4. Key và Schema

### Format Key
- Bảng thường (regular/main): `"%d_%d" % [sz, rank]` hoặc `"%d_%d_H" % [sz, rank]` (nếu có tier `"H"`).
- Bảng lkmod: `"%d_%d" % [sz, rank]`.

### Schema (Dữ liệu lưu)
- `_bank_progress`: Là số đếm `int`.
- `_lkmod_progress`: Là object `{"idx": int}`.
- `_main_bank_progress`: Là object `{"idx": int, "since_lk": int, "transform": int}`. (Mặc định khi tạo khởi tạo: `{"lk_mod": 0, "regular": 0, "lkstyle": 0, "transform": 0}` nhưng logic ghi đè dùng `idx` và `since_lk`).

## 5. Luồng xử lý (Flow)

- **`get_level_entry(level_num, override_sz)`**: 
  - Là điểm vào (Entry Point). Tính toán size, kiểm tra `_SPECIAL_LEVELS` (sp, lk). Nếu dính thì bốc luôn từ BankData, gắn metadata.
  - Nếu là bài chơi thường: Xác định rank/tier dựa vào `get_strategy()` và các rule logic (lv >= 51, lv >= 21). Xử lý buff `daily_first_easy` (rớt 1 rank).
  - Trỏ luồng sang `_get_next_entry_with_filter()`.
- **`_get_next_entry_with_filter`**:
  - Tách luồng: Nếu `level_num >= 51`, gọi `get_next_entry_main`, ngược lại gọi `get_next_entry`.
  - Validate thêm bước `single_region_num`. Nếu bị reject (too much single region), skip câu đố (đánh dấu false).
  - Lặp tới khi tìm được. Gọi `commit_bank_progress()`.
- **`get_next_entry` (Level < 51)**:
  - Gom chung mảng `regular`, `lkstyle`, `gc` -> tính `total`.
  - Lấy `idx` cũ = `GameState.get_bank_index`.
  - Dùng modulo `real_idx = idx % total` để chỉ định entry nào được lấy. Phân bổ tuần tự qua 3 pool.
  - Sau khi validate solution hợp lệ, gọi `advance_for_entry` để tăng `_bank_progress` lên 1.
- **`get_next_entry_main` (Level >= 51)**:
  - Khởi tạo array `lk_mod` (bỏ lk_mod_reserved), `regular`, `lkstyle`, `gc`. 
  - Đọc `_main_bank_progress`. Nếu thiếu (`idx` < 0), tự động fallback lấy từ `get_bank_index` cũ để chia modulo.
  - Nếu `idx` vượt quá tổng pool, reset `idx = 0`, tăng `transform = (transform + 1) % 8`.
  - Logic đan xen (Interleave logic):
    - Nếu `since_lk >= 4` VÀ `lk_idx < total_lk_mod`: bốc từ `lk_mod`. 
    - Nếu không: Bốc từ pool `regular -> lkstyle -> gc` dựa theo giá trị `idx`.
  - Transform puzzle (Xoay/lật) dựa theo biến `transform`.
  - Gọi `advance_for_entry`.
- **`advance_for_entry`**:
  - Gắn nhãn tiến độ. Nếu entry bốc ra là `"lk_mod"`, tiến độ `lkmod.idx += 1`, và reset `main.since_lk = 0`. Nếu bốc từ pool khác, tăng cả `main.idx += 1` và `main.since_lk += 1`.

## 6. Các chuỗi phân loại nguồn (Source Strings)

- `"regular"`: Tạo trong `get_next_entry` và `get_next_entry_main`. Dùng làm `_bank_source` và `_bank_source_main`. Ý nghĩa: puzzle từ `bankDataNxN.json`.
- `"lkstyle"`: Tạo trong `get_next_entry`, `get_next_entry_main`. Từ file `bankDataLKStyleNxN.json`.
- `"gc"`: Tạo trong `get_next_entry`, `get_next_entry_main`. Từ file `bankDataGCNxN.json`.
- `"lk_mod"`: Tạo trong `get_next_entry_main`. Từ file `bankDataLKModified.json`. (Luôn là level >= 51, xuất hiện mỗi 4 màn).
- `"sp"`: Tạo trong `get_level_entry`. Câu đố hardcoded (Tutorial, Special). Lấy từ `bankDataSP.json`.
- `"lk"`: Tạo trong `get_level_entry`. Lấy từ `bankDataLK.json` (chứa các mốc level cũ 200, 250, 314...).

## 7. Điểm chưa xác định / Chưa đủ căn cứ

- Schema chi tiết bên trong các object của `BankData` (ví dụ, `regionMap`, `solution`, mảng 1 chiều hay 2 chiều, các cờ `tier`, `r`, `maxR` được parse nguyên mẫu từ JSON) không thể nhìn thấy kiểu dữ liệu chính xác (typing) qua GDScript do JSON trả về `Variant/Array/Dictionary` động.
- Bản chất của `TRANSFORM_COUNT = 8` là các phép xoay lật (isometry của hình vuông), nhưng không có mapping trực tiếp của từng biến đổi ở góc độ vật lý ngoài hàm `apply_transform()`.
