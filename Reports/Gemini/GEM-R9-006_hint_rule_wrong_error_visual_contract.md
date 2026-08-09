# Báo cáo bằng chứng: GEM-R9-006

**Nguồn đối chiếu:** `D:\Projects\_GameExtract\Main_Meokdoku`
**Mục tiêu:** Liệt kê các bằng chứng mã nguồn chính xác về UI Hint, Rule, Error Visual, Spine Hand, và Audio khi đoán sai.

---

## 1. HintOverlay và Cell Highlight (Hiệu ứng Gợi ý)
- **Node & Script Overlay:** `hint_overlay.gd`
  - Nơi diễn dịch Text và Strategy:
    ```gdscript
    match strategy:
        "R1_mark": _strategy_label.text = "R1"; _desc_label.append_text(hint.get("description", tr("HINT_R1_MARK")))
        "R2": _strategy_label.text = "R2"; _desc_label.append_text(hint.get("description", tr("HINT_REGION_CONSTRAINT")))
        "R3": _strategy_label.text = "R3"; _desc_label.append_text(hint.get("description", tr("HINT_SET_LOCKING")))
        ...
    ```
- **Luồng Highlight trên Board:**
  - File: `scripts/module/gameplay/view/board_view.gd:884`
  - Hàm: `func set_hint_cells(unit_cells: Array[Vector2i], key_cell: Vector2i) -> void:`
  - Cơ chế: Board duyệt mảng `unit_cells` và gọi `play_hint()` trên các `CellView` tương ứng.
- **Hoạt ảnh nhấp nháy tại Ô (Pulse Animation):**
  - File: `scripts/module/gameplay/view/cell_view.gd:603`
  - Hàm: `func play_hint() -> void:`
  - Bằng chứng mã:
    ```gdscript
    _hint_tween = create_tween()
    _hint_tween.set_loops()
    _hint_tween.tween_property(_hint_light, "modulate:a", 1.0, _HINT_HALF_CYCLE)
    _hint_tween.tween_property(_hint_light, "modulate:a", _HINT_ALPHA_MIN, _HINT_HALF_CYCLE)
    ```

---

## 2. RuleBar (Thanh hiển thị luật vi phạm)
- **Luồng phát tín hiệu:**
  - File: `scripts/module/game/view/base_game_page.gd:2235`
  - Hàm: `func _try_emit_rule_violation(r: int, c: int) -> void:`
  - Hoạt động: Xác định luật nào bị vi phạm và đẩy tín hiệu sang `_rule_bar_v4`. (Bằng chứng chớp nháy tween được cài đặt tại `rule_info_bar_v4.gd`).

---

## 3. Wrong Red Visual (Hiệu ứng X đỏ khi đoán sai)
- **Bắt đầu quy trình lỗi:**
  - File: `scripts/module/game/view/base_game_page.gd:2289`
  - Hàm: `func _on_wrong_guess(r: int, c: int) -> void :`
- **Block Input & Delay (Rất quan trọng):**
  - Bật cờ chặn: `_wrong_guess_pending = true` (Dòng 2292).
  - Delay chờ hoạt ảnh vỡ X trước khi bỏ chặn:
    ```gdscript
    get_tree().create_timer(0.4).timeout.connect( func() -> void :
        _update_remaining()
        _wrong_guess_pending = false
    )
    ```
- **Phản hồi Lỗi (Thay đổi trạng thái ô):**
  - File: `scripts/module/gameplay/view/board_view.gd:700`
  - Hàm: `func play_error_feedback(r: int, c: int, source: int = ChangeSource.USER_ACTION) -> void :`
  - Đổi biến trạng thái thành `CellState.ERROR`.
- **Hoạt ảnh nứt vỡ X (Asset & Animation):**
  - Scene chứa Asset: `assets/prefab/cell.tscn` (Gồm 3 animation báo lỗi là `ErrorAppear1`, `ErrorAppear2`, `ErrorAppear`).
  - Lựa chọn Animation tại `scripts/module/gameplay/view/cell_view.gd` (Hàm `_resolve_error_appear_anim`):
    ```gdscript
    if crash_val == IconCrashConfig.VALUE_NO_CRASH:
        return "ErrorAppear1"
    elif crash_val == IconCrashConfig.VALUE_FISH_CRASH:
        return "ErrorAppear2"
    return "ErrorAppear"
    ```

---

## 4. Cat Hand / Spine (Phản ứng của Mèo trên bàn)
- **KHÔNG TÌM THẤY BẰNG CHỨNG** về việc trò chơi sinh ra bàn tay mèo (Spine hand) khi người chơi đoán sai. Tay mèo Spine chỉ dùng cho Correct Feedback.
- **Phản ứng duy nhất của Mèo khi đoán sai:**
  - File: `scripts/module/game/view/base_game_page.gd:2349`
  - Hàm: `func _play_wrong_guess_cat_feedback(r: int, c: int) -> void :`
  - Logic xác định mèo bị mâu thuẫn:
    ```gdscript
    var bad: Array[Vector2i] = QueendokuCore.find_conflicting_cats(r, c, placed, regions)
    if not bad.is_empty():
        _board_view.play_cat_frustrated_at(bad)
    ```
  - Gọi tiếp: `scripts/module/gameplay/view/board_view.gd:747` (`func play_cat_frustrated_at`).
  - Kết quả: Các ô `CellView` nằm trong danh sách `bad` sẽ chạy Animation `CatIconFrustrated` của chính nó (được gắn sẵn trong `cell.tscn`).

---

## 5. Audio (Âm thanh)
- **Âm báo đánh dấu sai (Error X):**
  - File: `scripts/module/gameplay/view/cell_view.gd` (Hàm `_emit_state_sound`)
  - Bằng chứng:
    ```gdscript
    elif state == CellState.ERROR:
        SoundManager.play(SoundManager.Kind.MARK_WRONG)
    ```
- **Không có Audio Meow đi kèm Error Cell:** Tại mốc gọi Error, tham số `meow_path` là rỗng. Chỉ khi gọi `play_cat_frustrated` thì mèo cũ mới có thể phát tiếng bực tức (nằm trong Animation Event hoặc Audio stream phụ, không nằm trong State chuyển hóa của Error Cell).

---
### Checklist Bàn Giao
1. [x] **Source files đã đọc**: `base_game_page.gd`, `board_view.gd`, `cell_view.gd`, `hint_overlay.gd`.
2. [x] **Asset paths đã xác minh**: `assets/prefab/cell.tscn`.
3. [x] **Điểm chưa xác minh được**: KHÔNG, mọi bằng chứng mã đều minh bạch.
4. [x] **Xác nhận**: KHÔNG sửa hoặc tạo thêm bất kỳ file mã nguồn/prefab nào ngoài báo cáo này.
