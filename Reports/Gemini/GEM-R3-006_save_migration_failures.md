# GEM-R3-006 Báo cáo Phân tích Load/Save, Migration & Failure Handling

**STATUS**: COMPLETE  
**GENERATED_AT**: 2026-08-08 16:45:00  
**SOURCE_ROOT**: `D:\Projects\_GameExtract\Main_Meokdoku`

## 1. Bảng phân nhánh Load (Load branches - Dual Slot)

Hàm `_load_once()` trong `save_store.gd` (Dòng 60) lặp tối đa 3 lần (cách nhau 60ms) để chống khóa file tạm thời từ OS.

| Điều kiện | Slot thử (Primary) | Kết quả / Fallback 1 (Backup) | Fallback 2 (Legacy) | Side effect (Log) |
|---|---|---|---|---|
| Flag == `"A"` | Slot A (`_path_a`) | Nếu hỏng -> thử Slot B (`_path_b`) | Nếu B hỏng -> thử `_legacy_path` | Báo lỗi "[SaveStore] 主槽损坏..." nếu A hỏng. Báo "[SaveStore] 已从备槽恢复..." nếu B thành công. |
| Flag == `"B"` | Slot B (`_path_b`) | Nếu hỏng -> thử Slot A (`_path_a`) | Nếu A hỏng -> thử `_legacy_path` | Tương tự trên. |
| Flag rỗng / hỏng / không có | Slot A (`_path_a`) | Nếu hỏng -> thử Slot B (`_path_b`) | Nếu B hỏng -> thử `_legacy_path` | Tương tự trên. Trả về `null` nếu tất cả đều hỏng hoặc thiếu. |

## 2. Bảng phân nhánh Save (Save branches - Dual Slot)

Hàm `save_config(cfg)` (Dòng 32) và `_atomic_write` (Dòng 89).

| Bước | File Target | Verify | Flag update | Failure result |
|---|---|---|---|---|
| Xác định Slot | Nếu flag cũ là `"A"`, chọn `"B"`. Ngược lại chọn `"A"`. | N/A | N/A | N/A |
| Atomic Write | Ghi dữ liệu vào `<final_path>.tmp`. | Đọc lại `<final_path>.tmp` để test decrypt/parse. | Nếu verify OK, rename `.tmp` đè lên `<final_path>`. | Nếu save hoặc verify lỗi: Return false, **không** rename file, **không** cập nhật Flag. Báo lỗi "原子写失败... 保留旧槽". |
| Cập nhật cờ | Ghi đè file `flag.txt` bằng Slot mới (chữ `"A"` hoặc `"B"`). | N/A | Hoàn tất chu trình Ping-pong. | Nếu ghi cờ lỗi, hàm báo `[SaveStore] 写入 flag 文件失败`. |

## 3. Migration và Version (Legacy & Schema)

| File:dòng | Điều kiện | Chuyển đổi | Persist / Delete |
|---|---|---|---|
| `game_state.gd:2156` (`_migrate_legacy_save`) | Chạy lúc `_ready`. Điều kiện: `SAVE_FLAG` chưa tồn tại VÀ `SAVE_PATH_OLD` tồn tại. | Đọc file OLD, truyền nguyên ConfigFile vào `_player_store.save_config(cfg)`. Tự động nhân bản ra A/B và sinh Flag. | Persist: Lưu vào Slot A/B. Delete: **KHÔNG DELETE**. File `SAVE_PATH_OLD` được giữ nguyên làm Fallback vĩnh viễn. |
| `game_state.gd:2047` (`_load_data`) | Chạy ngay sau khi load CFG từ RAM. | Dùng `cfg.get_value("progress", key, default)`. Không có cấu trúc "if version < 2 thì map lại cấu trúc". | Giá trị khuyết/lỗi kiểu được tự động khỏa lấp bằng `default value` hardcode. Cứ thế load lên RAM. |
| `game_state.gd:1565` (`set_endgame_snapshot`) | Khi có snapshot mới. Biến hardcode `ENDGAME_SNAPSHOT_VERSION = 2`. | Bơm thêm field `"app_version"` từ `UniKitManager` vào Dictionary snapshot trước khi xả ra đĩa. | Persist vào file `endgame.cfg`. Cập nhật field metadata cho mục đích debug/tracking. |

## 4. Failure Matrix (Ma trận lỗi)

| Tình huống lỗi | Hành vi của hệ thống |
|---|---|
| Cả hai slot A/B đều thiếu | Chuyển sang load Legacy (`SAVE_PATH_OLD`). Nếu không có -> Load `null` -> Game dùng toàn bộ giá trị Defaults. |
| Một slot (Primary) hỏng | Parse/Decrypt thất bại, tự động chuyển sang đọc slot Backup. Trò chơi không bị rollback nếu Backup là bản liền kề hợp lệ. |
| Cả hai slot đều hỏng | Báo lỗi "[SaveStore] 双槽均损坏". Fallback về Legacy. Nếu Legacy hỏng -> Mất save, chơi lại từ đầu. |
| Cờ (Flag) hỏng/mất | Hàm `_read_flag` trả về rỗng `""`. Hệ thống tự mặc định lấy Slot A làm Primary, Slot B làm Backup. (Self-healing khi lần save tiếp theo thành công sẽ ghi lại cờ). |
| Sai password | Hàm `load_encrypted_pass` trả về lỗi. Hệ thống đối xử giống hệt như File hỏng (chuyển Backup/Legacy). |
| Giải mã thành công nhưng Schema sai | GameState dùng `cfg.get_value` có kèm giá trị mặc định. Bất kỳ key nào sai kiểu hoặc mất mát đều rơi về mặc định của key đó, các key khác không bị ảnh hưởng. |

## 5. Player Store so với Endgame Store

| Đặc điểm | Player Store (`save_a.cfg`, `save_b.cfg`) | Endgame Store (`endgame.cfg`) |
|---|---|---|
| **Dual-Slot** | Có. Đầy đủ cơ chế A/B và Flag. | Không. Khởi tạo `dual_slot = false`. |
| **Atomic Write** | Ghi qua đuôi `.tmp` -> Xác thực -> Đổi tên. | Ghi qua đuôi `.tmp` -> Xác thực -> Đổi tên. |
| **Empty State** | Luôn tồn tại để chứa settings, level. | Kiểm tra `_is_endgame_store_empty()`. Nếu cả snapshot và round stats đều rỗng (sau khi clear), store sẽ tự động xóa file vật lý bằng lệnh `DirAccess.remove_absolute`. |
| **Load thất bại** | Fallback qua slot B, Legacy. | Thử 3 lần, thất bại thì trả về rỗng `{}`, coi như không có trận đấu dở dang. Không có fallback. |

## 6. App-kill Guarantees (Đảm bảo an toàn khi Crash/Kill)

**Đảm bảo (Guaranteed):**
- Tránh tham nhũng (Corruption) từng file: Nhờ cơ chế `.tmp` và `load` thử lại trước khi `rename_absolute`, một file CFG không bao giờ bị ghi dở dang (ví dụ ghi được nửa file bị ngắt điện).
- Tránh tham nhũng Slot: Nhờ Ping-Pong A/B, nếu app bị kill ngay giữa lệnh `rename` (hiếm) hoặc lỗi file hệ thống, slot đối diện luôn chứa bản save hợp lệ của (N-1) lần thay đổi.

**KHÔNG Đảm bảo (Not Guaranteed):**
- Ghi đè cờ (Flag Write): Hàm `_write_flag` ghi trực tiếp bằng `FileAccess.WRITE` không qua `.tmp`. App kill ngay đúng tích tắc này có thể khiến `flag.txt` trống không. Tuy nhiên, rủi ro bằng 0 vì đọc cờ rỗng sẽ tự thử A rồi B.
- Bất đồng bộ Player / Endgame: Vì Endgame Store có `_endgame_coalesce_timer` (0.5s), nếu App bị force kill (không đi qua `NOTIFICATION_WM_CLOSE_REQUEST` ở dòng 1634), Player Store có thể đã lưu `_current_level` mới, nhưng Endgame Store chưa kịp xóa/cập nhật snapshot cũ, dẫn tới logic mismatch lúc Resume.
