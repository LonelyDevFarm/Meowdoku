# GEM-R4-001 Báo cáo Phân tích Cấu trúc Ngân hàng Dữ liệu (Bank Data)

**STATUS**: COMPLETE  
**GENERATED_AT**: 2026-08-08 17:00:00  
**SOURCE_ROOT**: `D:\Projects\_GameExtract\Main_Meokdoku`

## 1. Bank files

| Pool | Path | Root schema | Lazy cache | Caller |
|---|---|---|---|---|
| `regular` | `assets/resources/levels/bankData{sz}x{sz}.json` | Dictionary: key `rank` ("1", "2") -> `Array[Entry]` | Cache theo từng Size `_load_size` | `get_next_entry`, `get_next_entry_main` |
| `lkstyle` | `assets/resources/levels/bankDataLKStyle{sz}x{sz}.json` | Dictionary: key `rank` ("1", "2") -> `Array[Entry]` | Cache theo từng Size `_load_lk_style_size` | `get_next_entry`, `get_next_entry_main` |
| `gc` | `assets/resources/levels/bankDataGC{sz}x{sz}.json` | Dictionary: key `"levels"` -> `Array[Entry]` | Cache theo từng Size `_load_gc_size` | `get_next_entry`, `get_next_entry_main` |
| `lk_mod` | `assets/resources/levels/bankDataLKModified.json` | Dictionary: key `"levels"` -> `Array[Entry]` | Load 1 lần, lưu vào `_lk_modified_levels` | `get_next_entry_main` |
| `sp` | `assets/resources/levels/bankDataSP.json` | Dictionary: key `"levels"` -> `Array[Entry]` | Load 1 lần, lưu vào `_sp_levels` | `get_level_entry` |
| `lk` | `assets/resources/levels/bankDataLK.json` | Array: `Array[Entry]` | Load 1 lần, lưu vào `_lk_levels` | `get_level_entry` |

## 2. Bank APIs

| Pool | Signature | Filter | Fallback | Metadata |
|---|---|---|---|---|
| `regular` | `get_levels(sz, rank)` / `get_levels_by_tier(sz, rank, tier)` | Theo `rank` / `tier` | `get_levels(sz, rank)` nếu pool tier trống | N/A (LevelData inject metadata) |
| `lkstyle` | `get_lk_style_levels(...)` / `get_lk_style_levels_by_tier(...)` | Theo `rank` / `tier` | Fallback về non-tier nếu tier trống | N/A |
| `gc` | `get_gc_levels(...)` / `get_gc_levels_by_tier(...)` | Thuộc tính `e.get("r", 0) == rank` | Fallback về non-tier nếu tier trống | N/A |
| `lk_mod` | `get_lk_modified_levels()` | Bên gọi (`LevelData`) tự lọc: `size == sz` và `r == rank` | N/A | N/A |
| `sp` | `get_sp_levels()` | Bên gọi tự lọc theo Index | N/A | N/A |
| `lk` | `get_lk_levels()` | Bên gọi tự lọc theo Index | N/A | N/A |

*File tham chiếu:* `scripts\module\bank\model\bank_data.gd`

## 3. Reserved / GC conditions

- **GC Condition** (`level_data.gd:113, 403`): Dữ liệu từ pool GC CHỈ được đưa vào tổ hợp khi và chỉ khi: `(sz == 10 and rank == 1) or sz == 11`.
- **LK_MOD_RESERVED Condition** (`level_data.gd:214, 381`): Các index sau (1-based) bị ép loại bỏ hoàn toàn khi duyệt file `bankDataLKModified.json`: `[20, 30, 53, 71, 72, 75, 114, 141, 164]`.

## 4. Selection order

### `get_next_entry` (Level <= 50) (`level_data.gd:98`)
- **Flow**: Gộp `regular` + `lkstyle` + `gc`.
- **Pool order**: Nửa đầu là `regular`, kế tiếp `lkstyle`, cuối cùng là `gc`.
- **Index calculation**: `real_idx = GameState.get_bank_index(sz, rank, tier) % total`.
- **Advance target**: `GameState.advance_bank_index` (tăng counter chung).

### `get_next_entry_main` (Level >= 51) (`level_data.gd:367`)
- **Flow**: Xen kẽ pool `lk_mod` vào giữa tiến trình giải pool chính bằng cách đếm chu kỳ `since_lk`.
- **Pool order**:
  - Nếu `since_lk >= 4` và chưa cạn `lk_mod`: Chọn 1 bài từ `lk_mod`.
  - Nếu không: Dùng `idx` để lấy từ `regular` -> `lkstyle` -> `gc`.
- **Index calculation**: Trạng thái được lưu trong `GameState.get_main_progress` dưới dạng Dict `{"idx": idx, "since_lk": since_lk, "transform": transform}`.
  - Khi `idx` vượt quá `total_regular + total_lkstyle + total_gc`, biến `transform = (transform + 1) % 8`, `idx = 0`, `since_lk = 0` (Áp dụng xoay/lật bàn chơi 8 hướng).
- **Advance target**:
  - Nếu chọn từ `lk_mod`: Gọi `GameState.set_lkmod_progress` (tăng `idx`), đồng thời set `since_lk = 0` cho `main_progress`.
  - Nếu chọn từ cụm chính: Gọi `GameState.set_main_progress` (tăng cả `idx` lẫn `since_lk`).

## 5. Entry schema

Dựa trên cách các field được lấy thông qua `.get()` trong mã nguồn:
- `size` (int): Dùng trong lọc lk_mod.
- `r` (int): Bank rank (Dùng ở GC, SP, lk_mod).
- `maxR` (int): Max rank (Dùng riêng ở LK và LK_mod non-strict fallback).
- `tier` (string): Tier phân nhóm (VD: "N", "H").
- `regionMap` (Array): Lưới chứa ID vùng. Cần đúng kích thước sz.
- `solution` (Array): Vị trí mèo trên mỗi hàng.

*Mọi Entry trả về từ LevelData sẽ được tiêm (inject) các biến runtime cục bộ: `_bank_source`, `_bank_source_main`, `_bank_idx`, `_bank_tier`, `_bank_rank`, `_bank_transform`.*

## 6. Số lượng entry

**Chưa đếm**. Toàn bộ file JSON trong `assets/resources/levels` đã được mã hóa XOR bằng key (`LevelBankIO._KEY = "meowdoku-2026-bank-secret"`) ngay trên ổ đĩa. Do chỉ thị cấm tuyệt đối tạo script/chương trình tạm (`.py`, `.exe`, `.cs`) để decode và count cơ học, nhiệm vụ này tạm bỏ qua.

## 7. Mismatch và Điểm chưa xác định

- **r vs maxR**: Có sự bất đồng bộ schema giữa `bankDataLK` và `bankDataLKModified`. `LK` chỉ dùng `maxR` làm fallback rank, trong khi `LKModified` ưu tiên `r` hoặc fallback qua `maxR` tùy vào cờ `cur_strict`. (Dòng `level_data.gd:386` và `595`).
- **Phụ thuộc JSON Array lồng**: `regionMap` đôi khi lưu dạng string nén hoặc array nén, phải được LevelData chuẩn hóa thành Array số nguyên khi transform.
- **Dư thừa ở Rank**: GC schema thiết kế theo cơ chế `Dictionary->levels->Array`, nhét chung mọi rank r, khác biệt so với Regular/LKStyle là `Dictionary->rank->Array`. API lấy tốn O(N) lọc filter thay vì O(1). Mismatch nhỏ nhưng không ảnh hưởng tính đúng đắn.
