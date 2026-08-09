# GEM-R3-004 Báo cáo Phân tích API Settings, Tools & Runtime Progression

**STATUS**: COMPLETE  
**GENERATED_AT**: 2026-08-08 16:25:00  
**SOURCE_ROOT**: `D:\Projects\_GameExtract\Main_Meokdoku`

## 1. API Nguồn (Từ `game_state.gd`)

| File | Dòng | Chữ ký | Dữ liệu đọc | Dữ liệu ghi | Persist | Signal/Side effect |
|---|---|---|---|---|---|---|
| `game_state.gd` | 1195 | `is_music_on() -> bool` | `_music_on` | N/A | Không | N/A |
| `game_state.gd` | 1198 | `set_music_on(value: bool)` | N/A | `_music_on = value`, `_music_user_modified = true` | CÓ | N/A |
| `game_state.gd` | 1205 | `init_music_default(default_on: bool)` | N/A | `_music_on = default_on` | CÓ | Chỉ set nếu `_music_user_modified == false` |
| `game_state.gd` | 1213 | `is_sound_on() -> bool` | `_sound_on` | N/A | Không | N/A |
| `game_state.gd` | 1216 | `set_sound_on(value: bool)` | N/A | `_sound_on = value` | CÓ | N/A |
| `game_state.gd` | 1220 | `is_vibration_on() -> bool` | `_vibration_on` | N/A | Không | N/A |
| `game_state.gd` | 1223 | `set_vibration_on(value: bool)` | N/A | `_vibration_on = value` | CÓ | Gọi `VibrateManager.set_enabled(value)` |
| `game_state.gd` | 1228 | `is_people_on() -> bool` | `_people_on` | N/A | Không | N/A |
| `game_state.gd` | 1231 | `set_people_on(value: bool)` | N/A | `_people_on = value` | CÓ | N/A |
| `game_state.gd` | 1151 | `get_apply_locale() -> String` | `_apply_locale` | N/A | Không | N/A |
| `game_state.gd` | 1154 | `set_apply_locale(value: String)` | N/A | `_apply_locale = value` | CÓ | N/A |
| `game_state.gd` | 1037 | `get_tool_count(kind: String) -> int` | `_tool_locate`, `_tool_hint` | N/A | Không | Return 0 nếu truyền `"undo"` |
| `game_state.gd` | 1103 | `set_tool_count(kind: String, count: int)` | N/A | `_tool_locate = count` / `_tool_hint = count`, `_has_used_tool = true` (nếu giảm) | CÓ | Emit `tool_count_changed(kind, count)` |
| `game_state.gd` | 453 | `get_current_level() -> int` | `_current_level` | N/A | Không | N/A |
| `game_state.gd` | 456 | `set_current_level(value: int)` | N/A | `_current_level = value` | CÓ | N/A |
| `game_state.gd` | 460 | `is_tutorial_done() -> bool` | `_tutorial_done` | N/A | Không | N/A |
| `game_state.gd` | 463 | `set_tutorial_done(value: bool)` | N/A | `_tutorial_done = value` | CÓ | N/A |
| `game_state.gd` | 725 | `get_current_strategy() -> int` | `_current_strategy` | N/A | Không | N/A |
| `game_state.gd` | 728 | `set_current_strategy(value: int)` | N/A | `_current_strategy = value` | CÓ | N/A |

## 2. Call sites quan trọng

| API | Caller file:dòng | Ngữ cảnh gọi | Hành vi sau lời gọi |
|---|---|---|---|
| `is_music/sound/vibration/people_on` | `module/setting/view/setting_page.gd:97-100` | Mở Setting UI | Cập nhật hình ảnh Toggle tương ứng |
| `set_music/sound/vibration/people_on` | `module/setting/view/setting_page.gd:414-486` | Nhấn nút Toggle | Đảo ngược giá trị cũ, gọi API lưu và đổi giao diện |
| `is_sound_on`, `is_people_on` | `module/sound/sound_manager.gd:138,160` | Phát sinh âm thanh | Hủy phát âm thanh/giọng nói nếu `false` |
| `get_apply_locale` | `module/language/language_manager.gd:44` | Khởi tạo ngôn ngữ | Lấy locale lưu trữ để áp dụng font/translation |
| `set_apply_locale` | `module/language/view/language_page.gd:246` | Chọn ngôn ngữ | Lưu locale mới |
| `get_tool_count` | `module/award/award_manager.gd:213` | Nhận thưởng (Award) | Lấy số dư hiện tại cộng thêm số lượng thưởng |
| `get_tool_count` | `module/game/view/base_game_page.gd:1095,1102` | Khởi tạo nút Tool trong game | Tắt nút nếu count <= 0 |
| `set_tool_count` | `module/award/award_manager.gd:214` | Nhận thưởng (Award) | Ghi đè số lượng mới lên GameState |
| `set_tool_count` | `module/ui/panel/cheat_commands.gd:956` | Dùng Cheat Panel | Trực tiếp add công cụ cho UI/GameState |
| `get_current_level` | `module/home/view/home_page.gd:95` | Mở Home UI | Hiển thị chữ `GAME_LEVEL_TITLE` (ví dụ: Level 5) |
| `get_current_level` | `common/unikit_manager.gd:1134` | Hiện quảng cáo | Gửi lên Tracker kèm số Level để thống kê |
| `set_tutorial_done` | `module/tutorial/view/tutorial_page.gd:582` | Hoàn thành Tutorial | Mark trạng thái và lưu |
| `get_current_strategy` | `module/gameplay/model/level_data.gd:57` | Lấy luật (rule) câu đố | Đọc Rank và Tier để query câu đố độ khó tương ứng |
| `set_current_strategy` | `module/gameplay/model/level_data.gd:59` | Level > 51 Clamp | Ép strategy tối thiểu lên 2 nếu level >= 51 |

## 3. Defaults và Schema

- **File lưu trữ**: Config của những giá trị này được persist trong `user://save_store/save_a.cfg` (và `save_b.cfg`).
- **RAM variables (Mặc định nếu thiếu)**:
  - Âm thanh/Haptics: `_music_on = true`, `_sound_on = true`, `_vibration_on = true`, `_people_on = true`.
  - Locale: `_apply_locale = ""` (Rỗng sẽ auto fallback ngôn ngữ hệ thống).
  - Tools: `_tool_locate = 5`, `_tool_hint = 5`, `_tool_undo = 3`.
  - Tiến độ: `_current_level = 1`, `_tutorial_done = false`, `_current_strategy = 1`.
- **Hành vi sai kiểu/thiếu key**: `cfg.get_value("progress", "tool_locate", 5)` - Nếu file JSON/CFG bị hỏng hoặc thiếu key, GameState tự dùng giá trị default an toàn để fallback (VD fallback 5 cái gợi ý).

## 4. Thứ tự khởi tạo/Runtime

1. **Khởi động game**: `game_state.gd` gọi `_load_data()` từ file. Đọc CFG vào RAM.
2. `language_manager.gd` gọi `get_apply_locale()` để setup i18n.
3. `sound_manager.gd` và `VibrateManager` sử dụng các getter âm thanh để thiết lập môi trường.
4. Khi vào `base_game_page.gd`, giao diện sẽ binding signal `tool_count_changed` để realtime update số lượng hiển thị.
5. **Runtime Gameplay**:
  - Chiến thắng (`_apply_level_won`): `_current_level` tăng tự động nếu thỏa mãn `next_level > _current_level`. Strategy bị điều chỉnh dựa vào `_consecutive_clean_wins` (thăng hạng) hoặc DDA demote (giảm hạng). Các thay đổi này lưu xuống đĩa (persist = true) vào cuối hàm.
  - Tiêu xài công cụ: Trừ count bằng `set_tool_count`, kéo theo flag `_has_used_tool = true`, tự động save cấu hình.

## 5. Điểm chưa xác định

- **Bí ẩn của Tool Undo**: Mặc dù biến `_tool_undo` được định nghĩa trong `game_state.gd` với giá trị mặc định là `3` và được lưu đọc đầy đủ từ file Config (dòng 110, 1979, 2084), hàm `get_tool_count(kind)` **KHÔNG** hề trả về `_tool_undo` (match string `"undo"` sẽ rớt xuống nhánh default `return 0`). Tương tự, `set_tool_count` cũng bỏ qua `"undo"`. 
- **Kết luận quan sát**: Tool Undo trong phiên bản hiện tại hoặc (1) là vô hạn, (2) được quản lý số lượng ở một scope khác/local của Scene (GameController/GamePage), hoặc (3) chỉ tốn số đếm ở màn chơi đặc biệt không dùng GameState. Biến `_tool_undo` trong GameState có khả năng là mã nguồn kế thừa (legacy code).
