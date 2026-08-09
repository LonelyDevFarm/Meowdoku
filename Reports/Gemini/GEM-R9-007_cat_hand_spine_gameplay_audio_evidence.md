# Báo cáo bằng chứng: GEM-R9-007 (Spine Hand & Gameplay Audio)

**Nguồn đối chiếu:** `D:\Projects\_GameExtract\Main_Meokdoku`
**Mục tiêu:** Bảng bằng chứng chính xác về Spine Hand (Cat-hand) và Gameplay Audio.

---

## 1. Spine / Cat-Hand Feedback (Hiệu ứng khen ngợi Mèo)

**Nơi quản lý Logic xuất hiện tay mèo (Quyết định):**
*   **File:** `scripts/module/game/view/base_game_page.gd`
*   **Hàm Quyết Định:** `func _decide_like_hand_for_correct_cat(trigger: int) -> Dictionary:`
*   **Điều kiện khoảng thời gian (Interval):**
    ```gdscript
    var override: float = ABTestManager.thumb_up.get_like_interval_override(sz)
    var interval: float = override if override > 0.0 else float(cfg["min_interval"])
    if _like_hand_state["in_game_sec"] - _like_hand_state["last_cat_sec"] < interval:
        return none
    ```

**Ghi nhận dữ liệu sau khi quyết định xuất hiện tay:**
*   **Hàm Ghi Nhận:** `func _record_correct_cat(decision: Dictionary) -> void:`
    ```gdscript
    _like_hand_state["last_cat_sec"] = _like_hand_state["in_game_sec"]
    if decision.get("should_play", false):
        _like_hand_state["triggered_count"] += 1
    ```

**Node / Scene chứa Spine Asset:**
*   **File Scene:** `scripts/module/game/ui/compont/game_like_hand.tscn`
*   **Cấu trúc Node SpineSprite:**
    *   Node `Like`: `skeleton_data_res` = `ExtResource("1")`, Scale mặc định `(1, 1)`.
    *   Node `Clap`: `skeleton_data_res` = `ExtResource("2")`, Scale mặc định `(1, 1)`.
    *   Node `BlowTrumpet`: `skeleton_data_res` = `ExtResource("3")`, Scale mặc định `(1, 1)`.
    *   Node `DoubleThumbs`: `skeleton_data_res` = `ExtResource("4")`, Scale mặc định `(1, 1)`, có modulate alpha tắt/mở qua Animation.
    *   Node `CorrectionCheer`: `skeleton_data_res` = `ExtResource("5")`, Scale lớn hơn `Vector2(1.13, 1.13)`.
    *   Node `LikeHand_6`: Scale `Vector2(0.5, 0.5)`.
    *   Node `LikeHand_7`: Scale `Vector2(0.5, 0.5)`.

*   **Animation Tracks (Điều khiển Spine):**
    Nằm tại Node `AnimationPlayer` root của `game_like_hand.tscn`:
    *   `Like`: Kích hoạt track Animation `SpineSprite Track 0`, duration `0.8339s`.
    *   `Clap`: Kích hoạt track Animation `LikeHand_2 Track 0`, duration `1.0856s`.
    *   `BlowTrumpet`: Kích hoạt track Animation `LikeHand_3 Track 0`.
    *   `CorrectionCheer`: Kích hoạt track Animation `LikeHand_5 Track 0`, duration `1.1s`.
    *   `DoubleThumbs`: Kích hoạt track Animation `LikeHand_4 Track 0`, duration `1.1s`.

---

## 2. Gameplay Audio (Hệ thống âm thanh)

**Khai báo hằng số và Asset Path (SoundManager):**
*   **File:** `scripts/module/sound/sound_manager.gd`
*   **Enum Kind:**
    ```gdscript
    enum Kind { BOARD_ENTER, MARK_X, UNMARK_X, MARK_CAT, MARK_WRONG, USE_HINT, ALL_CLEARED, LEVEL_WIN, LEVEL_FAIL, CLAP, BLOW_TRUMPET, COMBO, ... }
    ```
*   **Ánh xạ Asset (Map):**
    *   `Kind.MARK_X`: `"res://assets/audio/sfx/mark_x_2.ogg"`
    *   `Kind.UNMARK_X`: `"res://assets/audio/sfx/unmark_x_2.ogg"`
    *   `Kind.MARK_CAT`: `"res://assets/audio/sfx/mark_cat.ogg"`
    *   `Kind.MARK_WRONG`: `"res://assets/audio/sfx/mark_wrong_1.ogg"`
    *   `Kind.USE_HINT`: `"res://assets/audio/sfx/use_hint.ogg"`
    *   `Kind.ALL_CLEARED`: `"res://assets/audio/sfx/all_cleared.ogg"`
    *   `Kind.LEVEL_WIN`: `"res://assets/audio/sfx/level_win.ogg"`
    *   `Kind.CLAP`: `"res://assets/audio/sfx/tile_handlike_clip.ogg"`
    *   `Kind.BLOW_TRUMPET`: `"res://assets/audio/sfx/tile_handlike_genius.ogg"`

*   **Cấu hình Polyphony (Giới hạn tiếng ồn chồng chéo):**
    ```gdscript
    const _POLYPHONY: Dictionary = {
        Kind.MARK_X: 4, 
        Kind.UNMARK_X: 4, 
        Kind.MARK_CAT: 3, 
        Kind.MARK_WRONG: 2, 
        Kind.CLAP: 2, 
        Kind.BLOW_TRUMPET: 2, 
        Kind.COMBO: 2, 
    }
    ```

**Bằng chứng Call-Site chính trong Gameplay:**
1.  **Khi Ô chuyển trạng thái (Mark X, Mark Cat, Mark Wrong):**
    *   **File:** `scripts/module/gameplay/view/cell_view.gd`
    *   **Hàm:** `_emit_state_sound(prev_state: int, state: int, meow_path: String = "")`
    *   **Snippet:**
        ```gdscript
        if state == CellState.CAT:
            SoundManager.play(SoundManager.Kind.MARK_CAT)
        elif state == CellState.ERROR:
            SoundManager.play(SoundManager.Kind.MARK_WRONG)
        elif state == CellState.MARK and prev_state == CellState.EMPTY:
            if ABTestManager.mark_sound.is_soft_variant_1():
                SoundManager.play(SoundManager.Kind.MARK_X_SOFT_1)
            else:
                SoundManager.play(SoundManager.Kind.MARK_X)
        elif state == CellState.EMPTY and prev_state == CellState.MARK:
            SoundManager.play(SoundManager.Kind.UNMARK_X)
        ```
2.  **Khi Dùng Gợi Ý (Hint):**
    *   **File:** `scripts/module/game/view/base_game_page.gd`
    *   **Snippet:**
        ```gdscript
        SoundManager.play(SoundManager.Kind.USE_HINT)
        ```
3.  **Tất cả mèo ra sân:**
    *   **File:** `scripts/module/gameplay/view/board_view.gd`
    *   **Hàm:** `replay_all_cat_appear()`
    *   **Snippet:**
        ```gdscript
        SoundManager.play(SoundManager.Kind.ALL_CLEARED)
        ```

---
### Checklist Bàn Giao
1. [x] **Source files đã đọc**: `base_game_page.gd`, `game_like_hand.tscn`, `sound_manager.gd`, `cell_view.gd`, `board_view.gd`.
2. [x] **Trích nguyên văn ngắn**: Có.
3. [x] **Tách rõ từng mục**: Có.
4. [x] **Khẳng định**: Không có bất kỳ dòng suy đoán kiến trúc hay hướng dẫn porting Unity nào được viết thêm.
5. [x] **Xác nhận**: KHÔNG sửa hoặc tạo thêm bất kỳ file mã nguồn/prefab nào ngoài báo cáo này.
