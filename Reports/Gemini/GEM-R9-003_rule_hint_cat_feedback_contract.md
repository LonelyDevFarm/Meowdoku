# GEM-R9-003 Đặc tả Contract Rule, Hint & Cat-Hand (ThumbUp)

**STATUS**: COMPLETE  
**GENERATED_AT**: 2026-08-09 02:20:00  
**SOURCE_ROOT**: `D:\Projects\_GameExtract\Main_Meokdoku`

---

## 1. Rule Feedback (Hiển thị Luật & Vi phạm)

**Các Scene/Node liên quan:**
*   `RuleInfoBarV4` (`rule_info_bar_v4.gd`): Thanh hiển thị luật có nút gập/mở (Collapse/Expand) nằm ở Top UI.
*   `RuleInfoBarV7` (`rule_info_bar_v7.gd`): Dành cho level > 10, hiển thị dạng SwipeCard gồm 3 luật: "One per color", "One per line", "No touch".
*   `Rule Highlight`: Nằm bên trong cấu trúc RuleBar.

**Hành vi & Timing:**
*   **Trạng thái gập mở**: Được lưu vào `GameState.is_rule_info_bar_collapsed()`.
*   **Animation gập mở**: Dùng `create_tween()` dịch chuyển `position:x` của `_control`.
    *   Collapse: Duration 0.3s, EASE_IN_OUT, TRANS_QUAD. Lật mũi tên (`flip_h = true`) sau delay 0.28s.
    *   Expand: Dịch tới -10.0 (0.25s), lật mũi tên, rồi giật lùi về 0.0 (0.1s) tạo hiệu ứng nảy (EASE_IN_OUT, TRANS_QUAD).
*   **Vi phạm luật (Rule Violation)**:
    *   Khi người dùng đặt sai (Mark / Wrong Guess), gọi `_try_emit_rule_violation(r, c)`.
    *   Truyền logic cho `QueendokuCore.classify_violation()`. Nếu vi phạm (ví dụ trùng hàng, chạm góc), trả về index của luật (1, 2, 3).
    *   Gọi `_play_rule_highlight(rule_index)`: Lấy ảnh nền của luật tương ứng (`RuleHighlight1..3`) và chạy hiệu ứng chớp tắt.
    *   **Hiệu ứng chớp**: Tween dùng hàm biến thiên Alpha: `a = RULE_HL_FLOOR + (1.0 - RULE_HL_FLOOR) * sin(t * PI)`. Quét từ 0.0 đến 1.0 với thời lượng `RULE_HL_PERIOD`, lặp 2 vòng (`set_loops(2)`).

**Chuyển đổi sang Unity:**
*   Phần Tween gập mở thay bằng DOTween `DOAnchorPosX`.
*   Phần Highlight nhấp nháy: Dùng DOTween `DOFade` hoặc Material Property Block kết hợp hàm Sine tương tự. Không nên dùng Animation Curve phức tạp.

---

## 2. Hint System (Gợi ý)

**Các Node/Scripts liên quan:**
*   `HintEngine` (`hint_engine.gd`): Engine thuật toán giải đồ thị thuần túy (Static class). Phân loại chiến thuật: `R1` (Row/Col/Region/Intersection), `R2`, `R3`, `R4`, `R4_chain`, v.v.
*   `HintOverlay` (`hint_overlay.gd`): UI làm xám màn hình để focus vào ô gợi ý.
*   `HintMutex` (`hint_mutex.gd`): Lock chống bấm Hint nhiều lần gây lặp logic.

**Hành vi & Flow:**
1.  Người dùng bấm nút Hint -> Gọi `HintMutex.try_acquire()`.
2.  `HintEngine` tính toán nước đi.
3.  `HintOverlay.show_hint(hint_dict)`:
    *   Màn xám (Overlay ColorRect) hiện lên bằng Tween: Alpha `0.0 -> 0.75` trong thời gian `0.3s`.
    *   Text Description: "HINT_ROW", "HINT_COLOR_REGION"...
    *   Banner và BtnGroup được định vị động (`_align_to_board()`): Banner nằm trên bảng (`board_top - SPACING_TO_BOARD`), BtnGroup nằm dưới đáy bảng.
4.  Bảng chờ Input: User bấm `ApplyBtn` (Áp dụng) hoặc `DismissBtn` (Hủy bỏ).
5.  Gửi Signal: `hint_applied` hoặc `hint_dismissed`. Main page nhận Signal và đổi State cell thành Cat / Mark. Xóa màn xám.

**Chuyển đổi sang Unity:**
*   Thuật toán tĩnh `HintEngine` port 1:1 sang C# static.
*   Layout của `HintOverlay` trên Unity cần dùng `RectTransform` tính World Corners của bảng (Board) để ghim mép (Top/Bottom) chính xác giống hàm `_align_to_board()` của Godot. Màn xám là 1 UI Image với CanvasGroup `DOFade(0.75f, 0.3f)`.

---

## 3. Cat-Hand (ThumbUp / Clap / Phản hồi cổ vũ)

Đây là hệ thống gọi Spine Animation (Bàn chân mèo, kèn, vỗ tay) khi ăn điểm tốt.

**Node/Asset:**
*   Cấu hình: `ThumbUpConfig.gd` định nghĩa Enum `LIKE, CLAP, BLOW_TRUMPET, CORRECTION_CHEER, MISSED_CAT`.
*   Prefab: `game_like_hand.tscn`. Chứa các SpineSprite Node: `Like`, `Clap`, `BlowTrumpet`, `DoubleThumbs`, `CorrectionCheer`, và ảnh 2D `HawkEye`.

**Hành vi & Flow:**
1.  Bất cứ khi nào đặt mèo đúng (`BoardView.ChangeSource.USER_ACTION` hoặc `HINT`), hệ thống kiểm tra ngưỡng combo/điểm qua hàm `_arbitrate_feedback_for_correct_cat()`.
2.  Ra quyết định Feedback: Tùy lịch sử chơi sẽ ra `CLAP` (vỗ tay) hay `BLOW_TRUMPET` (thổi kèn), hoặc `CORRECTION_CHEER` (Sửa sai thành đúng).
3.  Tính toán lề bọt điểm (Z-Order & Layout): `_predict_like_hand_hw(r, c)` tính trước chiều rộng của Spine Anim sắp chạy để đẩy lệch Bong bóng Điểm (`Score Bubble`) không bị đè vào tay mèo.
4.  Play Anim: Gọi `_play_like_hand_on_cell(r, c, anim)` gắn thẳng lên cell, hoặc `_play_feedback_at_fixed_pos()` gắn cố định ở vị trí `HawkEye` (Giữa đáy màn hình, `_HAWK_EYE_Y_RATIO = 0.57`).
5.  Audio: Gọi lập tức `SoundManager.play(Kind.CLAP)` hoặc `Kind.BLOW_TRUMPET`.

**Chuyển đổi sang Unity:**
*   Dùng **Spine-Unity** runtime (SkeletonGraphic cho UI hoặc SkeletonAnimation cho World Space) thay thế cho SpineSprite của Godot.
*   Logic tránh đè Bubble: Cực kỳ quan trọng. Port chính xác hàm `_predict_like_hand_hw()` để Score Bubble lùi sang bên.
*   Audio trigger nên map trực tiếp vào Spine Animation Event nếu có thể, hoặc dùng Unity Coroutine Delay giống Godot.

---

*(Trích xuất từ `base_game_page.gd`, `rule_info_bar_v4.gd`, `hint_overlay.gd`, `hint_engine.gd`, `thumb_up_config.gd`)*
