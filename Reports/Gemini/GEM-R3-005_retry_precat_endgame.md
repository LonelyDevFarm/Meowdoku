# GEM-R3-005 Báo cáo Phân tích Retry, Pre-cat và Endgame State

**STATUS**: COMPLETE  
**GENERATED_AT**: 2026-08-08 16:35:00  
**SOURCE_ROOT**: `D:\Projects\_GameExtract\Main_Meokdoku`

## 1. API Nguồn (Từ `game_state.gd`)

| File | Dòng | Chữ ký | Read | Write | Persist target | Signal/Side effect |
|---|---|---|---|---|---|---|
| `game_state.gd` | 1273 | `set_retry_puzzle(level: int, params: Dictionary)` | N/A | `_retry_puzzle_level`, `_retry_puzzle_params` | Player Store | Gọi `_save_data()` |
| `game_state.gd` | 1279 | `get_retry_puzzle(level: int) -> Dictionary` | `_retry_puzzle_level`, `_retry_puzzle_params` | N/A | Không | Trả về params nếu trùng level, ngược lại trả `{}` |
| `game_state.gd` | 1286 | `get_pre_cat_fail_count(lv: int) -> int` | `_pre_cat_fail_count`, `_pre_cat_fail_lv` | N/A | Không | Trả về 0 nếu khác level |
| `game_state.gd` | 1291 | `mark_pre_cat_revived()` | N/A | `_pre_cat_revived_this_level` = true | Player Store | Gọi `_save_data()` |
| `game_state.gd` | 1299 | `consume_pre_cat_pending() -> Dictionary` | `_pre_cat_pending_...` (hard/struggle/demote) | Các biến `pending` -> false | Player Store | Đọc 1 lần rồi reset false, gọi `_save_data()` |
| `game_state.gd` | 1313 | `get_pre_cat_lock(lv: int) -> Dictionary` | `_pre_cat_lock_...` | N/A | Không | Trả `{locked: bool, pre_type: str, position: vec2}` |
| `game_state.gd` | 1319 | `set_pre_cat_lock(lv, pre_type, position)` | N/A | `_pre_cat_lock_...` (lv, type, pos) | Player Store | Gọi `_save_data()` |
| `game_state.gd` | 1560 | `get_endgame_snapshot() -> Dictionary` | `_endgame_snapshot` | N/A | Không | Trả về dictionary gốc (không copy) |
| `game_state.gd` | 1565 | `set_endgame_snapshot(snapshot: Dictionary)` | N/A | `_endgame_snapshot = snapshot` | Endgame Store | Gắn thêm `app_version`, gọi `_save_endgame()` |
| `game_state.gd` | 1576 | `clear_endgame_snapshot()` | N/A | `_endgame_snapshot = {}` | Endgame Store | Gọi `_save_endgame()` |
| `game_state.gd` | 746 | `inc_game_total_stat(game_type, key, delta)` | `_main_game_total_stats` / `_daily...` | Tăng giá trị key thêm delta | Endgame Store | Lấy Dictionary theo `game_type`, gọi `_request_save_endgame()` |
| `game_state.gd` | 787 | `persist_game_round_stats(game_type, stats)` | N/A | `_main_game_round_stats` / `_daily...` | Endgame Store | Ghi đè stats, gọi `_request_save_endgame()` |
| `game_state.gd` | 795 | `reset_game_round_stats(game_type)` | N/A | Clear Dictionary tương ứng | Endgame Store | Clear RAM và gọi `_save_endgame()` |
| `game_state.gd` | 763 | `set_persisted_game_id(game_type, value)` | N/A | `_main_game_id` / `_daily_game_id` | Endgame Store | Ghi đè ID, gọi `_save_endgame()` |

## 2. Call sites quan trọng

| API | Caller file:dòng | Ngữ cảnh | Thứ tự/Hành vi kế tiếp |
|---|---|---|---|
| `set_retry_puzzle` | `game/view/game_page.gd:1122, 1889` | Game khởi tạo màn/Lưu retry | Lưu lại bộ params (regionMap, seed...) để ván sau gọi lại đúng map này (khi fail). |
| `get_retry_puzzle` | `game/view/game_page.gd:503, 522` | Game Init Board | Nếu có dữ liệu retry, build bảng dựa vào params thay vì random mới. |
| `mark_pre_cat_revived` | `game/view/game_page.gd:1928` | Revive thành công | Đánh dấu ván này có revive. `_apply_level_won` sẽ dùng cờ này định đoạt "struggle". |
| `consume_pre_cat_pending` | `game/view/game_page.gd:2289` | Sinh mèo có sẵn (Pre-cat) | Đọc pending (hard/struggle/demote) để quyết định xem ván này rớt loại Pre-cat nào, sau đó reset các cờ. |
| `set_pre_cat_lock` | `game/view/game_page.gd:2317, 2346` | Đặt Pre-cat xuống bảng | Khóa vị trí này, lưu xuống đĩa. |
| `get_pre_cat_lock` | `game/view/game_page.gd:2297` | Bắt đầu check Pre-cat | Kiểm tra nếu vị trí đã bị khóa thì khôi phục lại vị trí đó (chống reset đổi chỗ). |
| `set_endgame_snapshot` | `game/view/game_page.gd:2688` | Auto-save mỗi bước chơi | Gọi `_build_endgame_snapshot()` gom toàn bộ board state lưu vào RAM và xả ra đĩa. |
| `clear_endgame_snapshot` | `game/view/game_page.gd:1195, 1201, 2015...` | Bắt đầu ván mới / Bỏ cuộc / Hết máu | Xóa sạch snapshot cũ để tránh resume sai ván. |
| `get_endgame_snapshot` | `game/view/game_page.gd:1197, 1300` | Resume ván chơi cũ | Lấy snapshot, parse các arrays (`placed_cats`, `errors`) để khôi phục board. |
| `inc_game_total_stat` | `base_game_page.gd:2296, 2391, 2863...` | Trong lúc chơi (VD: dùng tool, step) | Cộng dồn step/time/tool_used vào Dictionary tổng. |
| `reset_game_round_stats` | `tracker.gd:264` | Khởi tạo tracker ván mới | Reset mọi chỉ số thống kê vòng đấu. |
| `persist_game_round_stats` | `tracker.gd:274, 286` | Trong lúc chơi/tính điểm | Ghi đè chỉ số vòng đấu để duy trì qua các session. |

## 3. Luồng (Flow) Win/Fail/Retry/Revive

### Luồng GameState Cốt lõi (Internal Logic trong `game_state.gd`)
- **Thắng (`on_level_won` -> `_apply_level_won`)**:
  - Ghi nhận `pre_cat_pending_struggle = (fail_count >= 2) or revived_this_level`. Reset fail count/lv.
  - Reset `pre_cat_lock`.
  - Nếu level >= 6:
    - Nếu "clean win" (chưa từng fail/chưa bẩn): `_consecutive_clean_wins` ++. Đủ ngưỡng -> Strategy ++.
    - Nếu có retry: Đếm `_consecutive_retry_levels`. Nếu đạt 2 và cùng 1 strategy -> Strategy -- (Demote).
  - Áp dụng DDA Demote: Nếu ván có dùng Tool/Revive (và luật A/B test cho phép), Strategy -- (Demote).
- **Thua (`on_level_failed` -> `_apply_level_failed`)**:
  - Đặt `_current_level_retried = true`, `_current_level_dirty = true`.
  - `_consecutive_clean_wins = 0`.
  - Tăng `_pre_cat_fail_count` (nếu trùng lv), ngược lại reset `fail_count = 1`.
  - Đánh dấu `_dda_tool_or_revive_used = true` (nếu luật A/B xem Thua/Revive là action demote).

## 4. Player Store so với Endgame Store

| Tiêu chí | Player Store (`save_a/b.cfg`) | Endgame Store (`endgame.cfg`) |
|---|---|---|
| **Mục đích** | Lưu tiến độ toàn cục (level, settings, tool, stats, pre_cat_lock). | Lưu snapshot bàn chơi đang dang dở (Endgame) và Stats trong game để resume/track. |
| **Thực thi ghi (Write)** | Gọi trực tiếp `_save_data()` -> Flush CFG ra I/O. | Gọi `_save_endgame()` tức thì hoặc `_request_save_endgame()` kích hoạt timer (Coalesce timer: 0.5s) gộp các thay đổi liên tục (như time_sec, step) thành 1 lần ghi. |
| **Lúc xóa (Delete)** | Không bao giờ xóa file (có luân phiên A/B). | Nếu store empty (hết snapshot và stats), nó sẽ gọi `_endgame_store.remove()` xóa sạch file trên ổ cứng. |

## 5. Defaults và Schema

- **Retry State**: 
  - `_retry_puzzle_level` = 0.
  - `_retry_puzzle_params` = `{}` (Schema bên trong phụ thuộc caller, chứa seed, regionMap, solution).
- **Pre-cat State**:
  - `_pre_cat_lock_lv` = 0, `_pre_cat_lock_pre_type` = "0", `_pre_cat_lock_pos` = `Vector2i(-1, -1)`.
  - Pending bool mặc định là `false`. Fail count mặc định là `0`.
- **Endgame Snapshot & Stats**:
  - `_endgame_snapshot` = `{}`. Khi ghi sẽ có field `"app_version"`. Schema chi tiết được sinh bởi `_build_endgame_snapshot` trong `game_page.gd`.
  - Stats (total/round) lưu dưới dạng Dictionary `String -> int`. Khởi tạo `{}`.

## 6. Điểm chưa xác định

- **Schema chi tiết của Endgame Snapshot**: File `game_state.gd` chỉ nhận Dictionary mờ (opaque). Cấu trúc chính xác (như `lives`, `prefill_positions`, `placed_cats`) chỉ có thể nội suy rải rác từ hàm khôi phục ở `game_page.gd` hoặc logic check `daily_first_easy`.
- **Đồng bộ hóa Stats/Tracker**: `inc_game_total_stat` lưu vào Endgame Store, nhưng không rõ tại sao chúng tách biệt với Player Store. Giả định là để nếu người chơi từ bỏ trận đấu hoặc uninstall/clear data dở dang, hệ thống Resume bị xóa sạch (`_is_endgame_store_empty`) nhưng không ảnh hưởng level/xu đã đạt.
