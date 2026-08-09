# GEM-R5-003 Đặc tả Domain Logic: Điểm số & Xử lý Đoán Sai

**STATUS**: COMPLETE  
**GENERATED_AT**: 2026-08-08 18:25:00  
**SOURCE_ROOT**: `D:\Projects\_GameExtract\Main_Meokdoku`

## 1. Mạch Máu Điểm Số (GameScoreModel & A/B Config)

### Lớp dữ liệu thuần `GameScoreModel` (`game_score_model.gd`)
- **Fields (Khởi tạo mặc định = 0)**: `score`, `combo`, `max_combo`.
- **API cơ bản**: `add_combo()`, `reset_combo()`, `add_score(gain)`, `apply_deduction(amount)`, `reset_all()`, `to_dict()`, `restore(d)`.
- Chú ý: Lớp này chỉ là một Data Container (tương đương Model/Struct). Logic thực sự quyết định lượng điểm được tính toán ở ngoài (BaseGamePage).

### Nguồn tính điểm (`base_game_page.gd` & ABTestManager)
- **Tăng Combo & Tính Điểm (`_on_board_cell_state_changed_for_combo`, dòng 3039 - 3110)**:
  - Khi người chơi đặt CAT thành công, `_se_count` (số mèo đặt đúng) tăng lên.
  - Điểm gốc (Mặc định `VALUE_DISABLED = 0`): `gain = min(600 + max(0, combo_count - 1) * 80, 1320)`. (Tức là cộng 600, 680, 760... tối đa 1320).
  - Điểm gốc (Variant `VALUE_NON_ROUND = 2`): `gain = min(576 + max(0, combo_count - 1) * 96, 1440)`.
  - Multiplier (Hệ số nhân): Tính từ combo 3 trở lên (`1.2 + 0.1 * combo`), chỉ áp dụng cho variant Multiplier.
  - Skill Bonus (Điểm thưởng độ khó): Nếu ở variant `VALUE_SKILL_SCORE`, cộng thêm 20, 30, 50, 100, 200, 300 tuỳ thuộc vào Rank của ô (tính từ HintEngine).
  - Cuối cùng gọi `_score_model.add_score(final_gain + skill_bonus)`.
- **Trừ điểm khi Sai (`_on_wrong_guess`, dòng 2289 - 2348)**:
  - Hàm `has_deduction()` mặc định trả về **False**. Game **KHÔNG trừ điểm** mặc định khi đoán sai.
  - Chỉ khi ở đúng Variant `VALUE_DEDUCTION (5)`, hàm `apply_deduction(100)` mới được thực thi.
- **Life Bonus (Thưởng mạng cuối game, dòng 3110 - 3138)**:
  - Khi hoàn thành ván, nếu còn mạng, game quy đổi số mạng thừa thành điểm.
  - Dùng mảng `bonus_seq = calc_life_bonus_sequence(_lives)`. Duyệt từng mạng và gọi `_score_model.add_score(bonus)`.

---

## 2. Luồng Xử lý Hành Động (Correct / Wrong Guess)

Quy trình người chơi Double-Tap để đặt Mèo (`base_game_page.gd` dòng 2767 - 2850):

### A. Đặt Mèo Đúng (`do_place_cat`)
- Xác nhận: Ô này có thuộc mảng `solution` không (`_is_solution_cell`). Nếu Có -> Trúng.
- Gọi `_record_cell_change(r, c, original_before, CellState.CAT)` đưa vào bộ nhớ tạm.
- Gọi `BoardView.set_cell_state(..., CAT)` hiển thị.
- Tính toán phản hồi ngẫu nhiên (ThumbsUp / Like). Ghi nhận bước đi (`_commit_current_step(is_cat=true, is_wrong_guess=false)`). 
- State lịch sử đẩy vào `StepHistory`. Mạng giữ nguyên. Combo tăng.

### B. Đặt Mèo Sai (`do_wrong_guess_mark` & `_on_wrong_guess`)
- Xác nhận: Nếu `_is_solution_cell` là False.
- **Biến thái State**: Hệ thống KHÔNG đặt `CAT` hay `ERROR` mà tự động **chuyển ô đó thành Dấu X (`CellState.MARK`)** thông qua lệnh `_board_view.set_cell_state(r, c, CellState.MARK)`. (Ý nghĩa: Báo cho người chơi biết ô này không thể là mèo, hãy gạch nó đi).
- **Phát hiện Luật Vi Phạm (Rule Violation)**: 
  - Gọi `_try_emit_rule_violation(r, c)` -> `QueendokuCore.classify_violation(...)`. 
  - Các luật được phân tách rõ (độ ưu tiên giảm dần): `SAME_COLOR` (1), `SAME_LINE` (2), `NO_TOUCH` (3). Gửi Signal cảnh báo chớp đỏ (Rule Highlight).
- **Side Effects (`_on_wrong_guess`)**:
  - Gắn cờ khóa UI: `_wrong_guess_pending = true`.
  - Đặt lại Combo: `_score_model.reset_combo()`.
  - Trừ mạng: `_lives = maxi(_lives - 1, 0)`.
  - Phạt điểm: `_score_model.apply_deduction(...)`.
  - Gây Ức chế Mèo (Cat Frustration): Quét mảng các con mèo đã đặt trên bàn `QueendokuCore.find_conflicting_cats` và chạy hoạt ảnh bực tức cho các con mèo liên quan.
  - Screen Shake. 
  - Đợi 0.4s: Xóa cờ khóa UI `_wrong_guess_pending = false`, gọi `_update_remaining()`.
  - Nếu `_lives <= 0`: Đợi 0.6s -> Game Over.
  - Commit Lịch sử: `_commit_current_step(is_cat=false, is_wrong_guess=true)`.

---

## 3. Khuyến nghị Fixture (Mock Data Porting)

### Fixture 1: Đúng (Correct Cat)
- **Input**: Size = 4x4. Solution `(0,0)` = `CAT`. State trước: `_lives = 3, score = 100, combo = 2`.
- **Hành động**: User Double-Tap `(0,0)`.
- **State Sau**: 
  - Ô `(0,0)` chuyển sang `CAT`.
  - `combo` = 3, `score` = 100 + Gain(3). 
  - `_lives` = 3. 
  - Sinh ra StepRecord `(is_cat=true, is_wrong=false)`.

### Fixture 2: Sai bét (Wrong Guess)
- **Input**: Size = 4x4. Solution `(0,1)` = `EMPTY`. State trước: `_lives = 3, score = 100, combo = 2`.
- **Hành động**: User Double-Tap `(0,1)`.
- **State Sau**: 
  - Ô `(0,1)` TỰ CHUYỂN SANG DẤU X (`MARK`). (Không phải `ERROR`, không phải `CAT`).
  - `combo` = 0, `score` = 100 - Deduction. 
  - `_lives` = 2. 
  - Lấy ra Rule Violation tương ứng nếu ô `(0,1)` vi phạm luật. 
  - Sinh ra StepRecord `(is_cat=false, is_wrong=true)`.

---

## 4. Ranh giới Porting (R5 vs R8/R9)

- **Thuần Domain (Port ngay ở R5)**: `GameScoreModel` (Logic data), `QueendokuCore.classify_violation`, logic đếm mạng (`_lives`), hệ thống `StepHistory`, chuyển đổi trạng thái `MARK` khi đoán sai.
- **Giao diện/Audio (Đẩy lùi về R8/R9)**: `ABTestManager.score_encourage` (vì dính đến config động và UI bay chữ số), Tween cho Rule Highlight, `play_screen_shake`, `play_cat_cry_loop_all`, và `VibrateManager`. Các side effect này cần được đóng gói qua Interface/Event bus để tách khỏi Core C#.

STATUS: COMPLETE
