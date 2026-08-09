# GEM-R9-002 Đặc tả Contract Combo Feedback Presenter & Assets

**STATUS**: COMPLETE  
**GENERATED_AT**: 2026-08-09 02:05:00  
**SOURCE_ROOT**: `D:\Projects\_GameExtract\Main_Meokdoku`

---

## 1. Cấu trúc Cây Node & Properties Cần Dựng (Unity)

Trong Godot, hệ thống UI điểm số bay lơ lửng được quản lý bởi `ComboFeedbackView` (thực thể này thường nằm đè lên Board).
Các Prefab (Scene) con được nạp sẵn để instantiate động:
*   `SCORE_BUBBLE_SCENE`: `level_flow_score.tscn` (Hiển thị điểm số + cộng, ví dụ "+ 10")
*   `_SE_MULTIPLIER_SCENE`: `level_flow_multiplier.tscn` (Hiển thị hệ số nhân "x 1.2")
*   `_SE_SKILL_SCORE_SCENE`: `level_flow_skill_score.tscn` (Hiển thị điểm bonus kỹ năng)
*   `_SE_DEDUCTION_SCENE`: `level_flow_deduction.tscn` (Hiển thị trừ điểm "- 5")
*   `ENCOURAGE_SCENE`: `level_encourage.tscn` (Chữ bay lên như "Good!", "Excellent!")
*   `_SE_TRAIL_SCENE`: `effect_score_trail.tscn` (Đuôi sáng bay lên thanh điểm tổng)
*   `_SE_LIFE_TRAIL_SCENE`: `effect_life_trail.tscn` (Đuôi sáng hình trái tim bay lên)
*   `_SE_BURST_SCENE`: `effect_score_burst.tscn` (Nổ sáng đập vào thanh điểm)

**Thiết kế trên Unity**:
Cần 1 GameObject rỗng làm Container tên là `ComboFeedbackPresenter`. Nó quản lý việc Spawn ra các Prefab bọt biển bằng Object Pool (để tránh rác bộ nhớ).
`LifeSlot` (3 máu) nằm ở khu vực Hard UI, có chức năng chạy Anim "Appear", "Disappear", "Revive" (được gọi qua `play_revive()`, `show_lost()`).

---

## 2. Bảng Asset Mapping (Tài nguyên Hình ảnh)

Godot dùng ảnh số cắt lẻ cho các phông chữ đặc biệt. Phía Unity **phải dùng Sprite Atlas** hoặc **TMP_FontAsset** thay thế.

| Chức năng | Nguồn (Godot) | Giải pháp Unity (Đề xuất) | Ghi chú / Thiếu sót |
| :--- | :--- | :--- | :--- |
| **Score Digit** | `assets/sprites/game/score_font/ui_mao_sz_pic_00.png` -> `09.png` | Dùng TextMeshPro Font (Sprite/Bitmap Font) | Cần generate file `.asset` Font cho TMP. |
| **Score Dấu Cộng** | `assets/sprites/game/score_font/ui_mao_sz_pic_10.png` | Add vào bảng mã Font của TMP ở trên. | |
| **Multiplier Digit** | `assets/sprites/game/multiplier_font/ui_mao_cf_pic_00.png` -> `09.png` | Dùng TextMeshPro Font riêng. | |
| **Multiplier "X" & "."** | `ui_mao_cf_pic_11.png` (X), `12.png` (.) | Add vào bảng mã Font của TMP. | |
| **Encourage Art** | Nằm trong scene `level_encourage.tscn`. | Các sprite "Good", "Excellent" | Có thể map 1:1 Sprite. |
| **Hard Icon** | `assets/sprites/game/hard.png` | Gắn vào thanh UI tĩnh. | Map 1:1. |
| **Life Hearts** | `assets/sprites/game/fish_slot.tscn` | Prefab LifeSlot. | Gồm tim đỏ, xám và mask. |
| **Trails/Particles**| CPUParticles2D trong `effect_score_trail.tscn` | Dùng Unity Particle System. | **THIẾU SÓT LỚN**: Thông số Curve của hạt bay. Cần port tay tham số. |

---

## 3. Presenter Contract & Side Effects

### A. ComboFeedbackView
*   **`show_combo(combo_count, cell_global_pos, total_score, gain)`**: Gọi khi ăn điểm + combo. Chạy logic nảy Encourage Text, đọc Voice, kích hoạt số nảy ở thanh tổng `RollingNumber`, và tung `SCORE_BUBBLE`.
*   **`show_score_only(cell_global_pos, gain, total_score, like_hand_hw)`**: Chỉ nảy số tổng và tung bubble `SCORE_BUBBLE`, không có Encourage/Voice.
*   **`show_se_bubble(cell_global_pos, params)`**: Tung bọt điểm + Multiplier/Skill.
*   **`play_se_fly_effect(from_global_pos, final_gain, se_score)`**: Tạo vệt sáng (`trail`) bắn từ `from_global_pos` (điểm đặt) lên `_score_value_label` trên thanh Top UI.
*   **`show_se_deduction_bubble(cell_global_pos, amount)`**: Tung chữ đỏ trừ điểm.
*   **`play_life_bonus_fly(heart: LifeSlot, bonus, total_se_score)`**: Từ LifeSlot, tung bọt cộng điểm, thả vệt bay lên thanh điểm. Trái tim sẽ chạy Anim mất đi (`heart.show_lost(true, true)`).

### B. LevelFlowMultiplier
*   **`play_appear2_anim(multiplier, prev_multiplier)`**: Xuất hiện thường.
*   **`play_appear3_anim(multiplier, prev_multiplier)`**: Kiểu mới có nảy dọc số. Có 2 mảng chữ số cuộn dọc (RollDigit) bằng Tween.

---

## 4. Timeline và Timing Cơ bản

1.  **Delay Âm Thanh (Combo Voice)**:
    *   `show_combo` -> Lập tức gọi `SoundManager.play_combo_voice_by_path`. Voice này sẽ nổ đồng thời với lúc tung Encourage Text.
2.  **Bubble Fly (Bay hạt sáng)**:
    *   Hạt sáng bắn đi bằng hàm Cubic Bezier phi tuyến tính (`_se_fly_pos` / `_se_life_fly_pos`). Thời gian bay cố định: **`0.57s`** (`_SE_FLY_DURATION`).
    *   Khi vệt sáng chạm đích (`_on_se_trail_arrived`), nó không xóa ngay mà chờ **`0.067s`** (`_SE_TRAIL_LINGER`) rồi tạo ra nổ `BURST_SCENE`.
    *   Nổ vụn dăm `BURST_SCENE` kéo dài **1.5s** rồi tự hủy.
3.  **Score Roll (Số cuộn lên)**:
    *   Duration cuộn của thanh điểm tổng trên Top UI: **`0.35s`**.
    *   Score bar Bounce (Làm phình số tổng ra khi ăn vệt sáng): Nở to (`1.2` scale) trong **`0.1s`**, Thu lại 1.0 trong **`0.2s`**.
4.  **Multiplier Roll (Cuộn số lẻ)**:
    *   Cuộn dọc các chữ số hệ số nhân: Thời lượng cuộn Tween: **`0.35s`**.

---

## 5. Quy tắc Positioning, Clamp, Padding

*   **Bubble Z-Index**: `SCORE_BUBBLE` thường ăn z-index 10, vệt bay `_SE_TRAIL_Z` có Z=41. Trái tim Bonus Z=50.
*   **Khoảng cách dọc (Y Offset)**: Các bọt số (Bubble) bay lên từ tâm cell cộng thêm một khoảng cao. Chiều cao nổi của bọt là `BUBBLE_HEIGHT` (83px) + `BUBBLE_GAP` (10px) + (`CAT_TOP_UNSCALED` * board_scale). Tức là bọt không đè vào mặt mèo.
*   **Kẹp lề màn hình (Clamp X)**:
    *   Do mép bàn cờ sát mép điện thoại, bọt dễ bị rớt chữ.
    *   Hàm `compute_clamped_center_x(local_x, half_widths)` lấy toàn bộ nửa bề rộng của cụm Text (Ví dụ Score + Gap + Multiplier) để đẩy lùi tọa độ X. Nếu nó thò ra ngoài mép trái/phải (`size.x`), nó sẽ bị ép tịnh tiến vào trong màn hình sao cho cách đều cạnh.
*   **Gap (Khe hở ghép số)**: Giữa bọt Score và bọt Multiplier có khoảng cách `_SE_SCORE_MULT_GAP = 10.0px`.

---

## 6. Cleanup & Caching Hành vi

*   Tránh rác Tween/Coroutines: Khi `reset(score)` được gọi, `_reset_generation` được cộng lên. Bất kỳ vệt bay (Trail) nào kết thúc trễ, đếm thấy `generation` đã cũ sẽ bị `queue_free()` lập tức, không sinh nổ vụn, không cộng điểm lên UI (`_on_se_trail_arrived`).
*   Xóa hạt sáng đang tồn đọng: Vòng lặp `_cleanup_residual_effects()` sẽ duyệt tìm mọi bọt (Score/Multiplier/Skill) và xóa hết. Trả scale thanh Score về 1.0.

---

## 7. Khác biệt (Gaps) Unity & Phương án P0/P1/P2

| Domain | Vấn đề / Khác biệt với Unity | Mức độ | Khuyến nghị cho Codex |
| :--- | :--- | :--- | :--- |
| **Number Font Rendering** | Godot dùng TextureRect dán 4 chữ số thủ công vào HBoxContainer. | P0 | Unity nên làm Custom TMP_FontAsset và sinh chuỗi string bình thường `<align=center>`. Không ghép Sprite tay! Dùng Layout Group nếu ghép Sprite. |
| **Object Pooling** | Godot dùng `PackedScene.instantiate()` và `queue_free()` cực kỳ tốn CPU. | P0 | Phải dùng Object Pool cho Bubble, Trail Particle và Burst. |
| **Fly Bezier Curve** | Quỹ đạo hạt bay là Cubic Bezier nhúng tay vào mã (`_cubic_bezier_ease`). | P1 | Có thể dùng DOTween `.DOPath()` hoặc `.Ease(AnimationCurve)` thay cho hàm tự chế. |
| **Encourage Width Cache**| Code gốc chạy 1 đợt For Loop ép Frame của Animation Player để tìm độ phình cực đại của ảnh Encourage. Rất "hacky"! | P2 | Hardcode sẵn Half Width của 5 cấp độ Encourage vào bảng Const trong Unity. Tránh chạy Animation lúc Runtime để dò box size. |

---

*(Trích xuất toàn bộ từ `combo_feedback_view.gd`, `level_flow_score.gd`, `level_flow_multiplier.gd`, và `life_slot.gd`)*
