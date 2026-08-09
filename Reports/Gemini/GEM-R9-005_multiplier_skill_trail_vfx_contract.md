# GEM-R9-005 Đặc tả Multiplier, Skill Score & Trail VFX

**STATUS**: COMPLETE  
**GENERATED_AT**: 2026-08-09 12:25:00  
**SOURCE_ROOT**: `D:\Projects\_GameExtract\Main_Meokdoku`

---

## 1. Điều kiện xuất hiện và Trình tự gọi (Call Order)

**Trình tự khi đặt đúng Mèo (`base_game_page.gd`):**
1. **Kiểm tra Mode**: Nếu `ABTestManager.score_encourage.is_enabled()` là `false`, trò chơi sẽ dùng cách tính điểm Combo kiểu cũ (không có trail, không có multiplier). Nếu `true`, hệ thống mới (SE) được chạy.
2. **Tính toán (SE Mode)**:
   - `gain = calc_gain(_se_count)`
   - `multiplier = calc_multiplier(_se_count)`
   - `final_gain = int(gain * multiplier)`
   - Tính thêm `skill_bonus = calc_skill_bonus(cell_rank)` (dựa vào độ khó của ô cờ `cell_rank`).
3. **Cộng điểm Model**: Gọi `_score_model.add_score(final_gain + skill_bonus)`.
4. **Hiển thị Bọt biển (Score Bubble)**: Gọi `_combo_feedback_view.show_se_bubble(params)` với các tham số gain, multiplier, skill_bonus.
5. **Bay hạt sáng (Trail)**: Tính toán độ trễ `fly_delay` rồi phóng hạt sáng: `_combo_feedback_view.play_se_fly_effect(cell_global_pos, final_gain + skill_bonus, total_score)`.

---

## 2. Hệ thống Điểm số Kỹ năng (Skill Score) và Multiplier

**Màn hình Bọt Biển (Bubble):**
* Nằm trong `combo_feedback_view.gd` hàm `show_se_bubble`.
* **Multiplier Bubble** (`level_flow_multiplier.gd`): Chứa các chữ số cuộn. Có 2 loại hoạt ảnh: `Appear2` (Xuất hiện ngang) và `Appear3` (Cuộn số RollDigit bằng Tween vị trí Y dọc). Dùng font ảnh riêng.
* **Skill Score Bubble** (`level_flow_skill_score.gd`): Nếu `skill_bonus > 0`, nó sinh ra bong bóng Skill Score. Cấu trúc gồm ảnh dấu cộng (`PLUS_TEXTURE = ui_mao_cf_pic_10.png`) và tối đa 4 ảnh chữ số `DIGIT_TEXTURES` (font `ui_mao_cf_pic_00-09.png`).
* **Sắp xếp vị trí (Layout)**: Điểm gốc `gain` nằm bên trái, `multiplier` hoặc `skill_bonus` nằm cách một khoảng `_SE_SCORE_MULT_GAP = 10.0` px bên phải. Hàm `compute_clamped_center_x` ép cụm số này vào trong màn hình để không bị lẹm viền.

---

## 3. Trail VFX & Burst (Hiệu ứng Bay và Nổ)

**Hành trình Hạt sáng bay lên (Trail):**
* **Scene**: `effect_score_trail.tscn` (Vệt sáng bay) và `effect_life_trail.tscn` (Tim bay khi cộng Life Bonus).
* **Z-Index**: Cố định ở `_SE_TRAIL_Z = 41` (Bay đè lên mọi thứ trừ Overlay Menu).
* **Quỹ đạo (Trajectory)**: Điểm bắt đầu `from_pos` (Vị trí đặt Mèo), điểm đến `to_pos` (Chính giữa nhãn tổng điểm). 
* **Toán học Bezier**: Sử dụng hàm tự chế `_cubic_bezier_ease` trong Godot để vẽ đường cong phi tuyến:
  * Điểm thường (`_se_fly_pos`): Tọa độ X dùng `px(0.2, 0.0, 0.8, 1.0)`, Tọa độ Y uốn cong vòng lên một chút `py(0.5, -0.343, 1.0, 1.0)`.
  * Trái tim (`_se_life_fly_pos`): Y uốn cong mạnh hơn `py(0.2, -1.176, 1.0, 1.0)`.
* **Thời gian bay**: Cố định `_SE_FLY_DURATION = 0.57s`.

**Nổ tại đích (Burst):**
* Gọi tại `_on_se_trail_arrived`. Khi Trail tới đích:
  1. Tắt chế độ xả hạt: Particle `emitting = false`.
  2. Kéo dài tàn dư `_SE_TRAIL_LINGER = 0.067s` rồi xóa hẳn Trail (queue_free).
  3. Spawn `effect_score_burst.tscn` ngay tâm điểm đến.
  4. Kích hoạt 3 Particle Systems: `GlowAlp`, `StarAlp`, `StarAdd`.
  5. Cục nổ tồn tại 1.5s rồi bị hủy bằng Tween.
* Thanh tổng điểm cũng sẽ phình ra (`_play_se_score_bar_bounce`): Scale lên `1.2` trong `0.1s` và thu về trong `0.2s`.

---

## 4. Chuyển đổi sang Unity (Porting Notes)

1. **Multiplier & Skill UI (P0):**
   * **Không dùng Sprite Image thủ công:** Godot cắt 10 số (0-9) thành ảnh rời và gắn vào `TextureRect`. Trên Unity, phải bake font TMP (TextMeshPro Font Asset) từ các Sprite này và dùng `<size>` + Layout Component để định dạng.
   * **Hiệu ứng Cuộn (RollDigit):** Unity có thể dễ dàng làm số cuộn bằng Mask UI (hoặc RectMask2D) kết hợp Tween dịch chuyển Anchor Y của dải Text.
2. **Quỹ Đạo Bay (Trajectory - P1):**
   * Bỏ hẳn vòng lặp toán học 8 step tự chế của Godot. Sử dụng DOTween `.DOPath()` hoặc `.Ease(AnimationCurve)` cho trục X và Y độc lập để tái tạo đường cong mượt mà.
3. **Particle VFX (P1):**
   * `CPUParticles2D` trong Godot chuyển thành `Shuriken Particle System` của Unity. Phải tune bằng mắt cho `GlowAlp`, `StarAlp`, `StarAdd`.

---

### Checklist Bàn giao
- [x] **Source files đã đọc**: `base_game_page.gd`, `combo_feedback_view.gd`, `level_flow_multiplier.gd`, `level_flow_skill_score.gd`, `game_score_model.gd`.
- [x] **Asset paths đã xác minh**: `effect_score_trail.tscn`, `effect_score_burst.tscn`, `ui_mao_cf_pic_XX.png`.
- [x] **Các điểm chưa xác minh được**: Các tham số vật lý của CPUParticles2D trong Burst và Trail (không xem được dạng Text thô vì Godot giấu trong Serialize, phải canh tay khi port bằng Unity Editor).
- [x] **Trạng thái**: Không có thay đổi (chỉ đọc) ngoài file báo cáo này.
