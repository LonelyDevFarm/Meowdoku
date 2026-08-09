# Báo cáo đặc tả kỹ thuật: GEM-R11-015 (Tutorial Visual, Layout & Timing Spec)

**Nguồn đối chiếu:** `D:\Projects\_GameExtract\Main_Meokdoku`
**Mục tiêu:** Đặc tả cấu trúc Scene gốc, Animations và Timeline của màn Tutorial (không lặp lại logic State Machine của GEM-R11-014).

---

## 1. Cấu trúc Cây Node (tutorial_page.tscn)
Được đặt tại `scripts/module/tutorial/ui/tutorial_page.tscn`. Dưới đây là hierarchy các Control phân tầng Z-Index quan trọng:

- **Root (Control)**
  - **Background** (Z-Index: -1): Hậu cảnh.
  - **BoardContainer**: Scale chứa bảng 4x4.
    - `BoardView`: (919px width, pivot 0,0, scale down/up dựa trên 919 / intrinsic_size).
    - `SelectFrame`: Khung chọn có padding = 8.0px so với cell_rect.
    - `SuccessCheck`: Tick xanh.
  - **MaskLayer** (Control, Z-Index: 4): Lớp phủ tối che bảng.
    - `MaskBg`: (ColorRect). Mặc định `modulate.a = 0.0`.
  - **MessagePanel** (Panel, Z-Index: 5): Nền tin nhắn chính.
    - `MsgRich` (RichTextLabel).
  - **SubMsgPanel** (Panel, Z-Index: 5): Nằm dưới bảng, hướng dẫn exclude.
  - **HintToolPanel** (Panel, Z-Index: 5): Chứa `LampIcon` và `HintLabel`.
  - **ConfirmBtn** (Z-Index: 5): Nút bấm xác nhận.
  - **HandHint** (Control, Z-Index: 6):
    - `ui_guide_hand` (SpineSprite): Bàn tay động (animation "click").
    - `HandStatic` (TextureRect): Bàn tay tĩnh dùng để tween lướt (swipe).
  - **EffectIqBarGlow** (Z-Index: 5) & **IqBar** (Z-Index: 10): UI thanh IQ.
  - **EffectFlreworks** (Z-Index: 10): Pháo hoa chiến thắng.
  - **AnimationPlayers:** `MessagePanel_appear_1`, `ConfirmBtn_loop`, `GuideEncourage`, `EffectIqBar`, `EffectFlreworks`, v.v.

---

## 2. Animation, Timing & Tween

### A. Bàn tay (Hand Loop & Swipe)
- **Căn vị trí tĩnh:** Offset cố định của mũi nhọn ngón tay là `(111.0, -316.0)` tính từ Top-Left của ô (0, 2).
- **Tap Anim:** Spine `ui_guide_hand` play animation `"click"` lặp vô hạn.
- **Swipe Anim:** Cầm `HandStatic` lướt qua mảng `offsets` (VD: 3 điểm).
  - Delay chờ 0.15s.
  - Lerp di chuyển từ điểm A sang B mất **0.3s**, nghỉ **0.1s** giữa các điểm.
  - Sau khi lướt hết mảng, chờ tiếp 0.15s, sau đó fade out `modulate.a` về 0.0 trong **0.2s**.
  - Delay chờ **0.35s** trước khi lặp lại vòng mới.

### B. Hiệu ứng Mask Transition
- `_fade_in_mask_layer`: Kích hoạt layer (Z=4) và Tween `modulate:a` từ `0.0 -> 1.0` trong **0.12s**.
- `_fade_out_mask_layer`: Tween ngược lại trong **0.12s**, sau đó `visible = false`.

### C. Confetti (Hoa giấy chiến thắng mặc định)
- Tung **30** mảnh (ColorRect), trộn random 6 mã màu: `#FF5252, #448AFF, #69F0AE, #FFD740, #FF4081, #40C4FF`.
- Sinh ra tại tọa độ `X = [40, 1040], Y = [-150, -40]`.
- Rơi xuống `Y = 1980` trong thời gian random **2.0s - 3.5s**.
- Delay rơi random **0.0 - 0.6s**, ease IN, trans QUAD. Khử object (`queue_free`) sau khi rơi xong.

### D. IQ Feedback (Dùng cho A/B Test)
- `_iq_fill_right` và `_set_iq_number`: Được Tween method đồng thời.
- Thời gian chạy thanh tiến trình: **0.4s** (`TRANS_CUBIC, EASE_OUT`).

---

## 3. So sánh 3 Luồng A/B (CURRENT / CHECK / IQ)

- **Default (VALUE_CURRENT):** Gọi `_run_guide_flow_default()`. Luồng tiêu chuẩn, hoàn thành bài thì bắn Confetti (hoa giấy 2D). Đây là **luồng mặc định chạy Offline**.
- **CHECK (VALUE_CHECK):** Gọi `_run_guide_flow_check()`. Sau mỗi step thành công, dừng lại đợi `_anim_guide_encourage.play("SuccessCheck")` trước khi sang step sau.
- **IQ (VALUE_IQ):** Gọi `_run_guide_flow_iq()`. Hiện IQ Bar trên cùng. Sau mỗi step, thanh IQ nhích lên với anim `EffectIqBar01` (0.4s). Cuối game, gọi `_step_finish(true)` để **bắn Pháo hoa Particle (Fireworks)** thay vì Confetti.

---

## 4. Bảng Localization Keys

| Key | Ngữ cảnh sử dụng | Định dạng đặc biệt |
| :--- | :--- | :--- |
| `TUTORIAL_STEP1_RICH` | Lời chào & Đặt mèo | Trộn biến `{breath}` BBCode |
| `TUTORIAL_STEP1_HIGHLIGHT` | Chữ nổi bật Step 1 | Áp hiệu ứng BBCode `[breath]` đỏ |
| `TUTORIAL_STEP1_ONE_PER_COLOR` | IQ flow SubMsg | - |
| `TUTORIAL_STEP2_RICH` | Luật mỗi màu 1 mèo | Nằm ở MessagePanel |
| `TUTORIAL_GOT_IT` | Text Nút | Confirm Button |
| `TUTORIAL_STEP3_RICH(_DIAGONAL)` | Đánh dấu lân cận (Đường chéo) | Tuỳ config chéo của AB Test |
| `TUTORIAL_STEP4_{COLOR/PINK/BLUE}_RICH`| Đặt mèo vùng cụ thể | Màu động phụ thuộc RegionColorConfig |
| `TUTORIAL_STEP5_RICH` | Đánh dấu Hàng/Cột | Nằm ở MessagePanel |
| `TUTORIAL_SUB_EXCLUDE` / `SWIPE_EXCLUDE` | Hướng dẫn kéo thả đánh X | Nằm ở SubMessagePanel (Dưới Board) |
| `TUTORIAL_STEP6_RICH` | Hoàn thành | Lời chúc mừng kết thúc |
| `TUTORIAL_START_GAME` | Text Nút (Bắt đầu) | Bấm vào Game |
| `TUTORIAL_LAST_ONE_RICH` | Tự do bước cuối | - |
| `TUTORIAL_STEP7_HINT` | Tooltip ở bóng đèn | Hint Panel |
| `TUTORIAL_STEP7_ROW_BLUE/PINK`, `PLACE_LAST`| Hint Text ở các Phase bấm đèn | - |
| `TUTORIAL_IQ_FORMAT` | Text thanh IQ | "%d" để format integer |

---

## 5. Bảng Evidence Trích Dẫn

| Tiêu chí | File Nguồn | Node / Hàm / Dòng mã |
| :--- | :--- | :--- |
| Hierarchy & Nodes | `ui/tutorial_page.tscn` | `Root/MaskLayer`, `Root/BoardContainer`, `Root/HandHint` |
| Tween Bàn Tay | `tutorial_page.gd` | Hàm `_start_swipe_hand_loop()` (Dòng 674), Tween method 0.3s + delay. |
| Tween Mask | `tutorial_page.gd` | Hàm `_fade_in_mask_layer()` (Dòng 804), `.tween_property("modulate:a", 1.0, 0.12)` |
| Sinh Confetti | `tutorial_page.gd` | Hàm `_spawn_confetti()` (Dòng 823), tạo 30 ColorRect, tween `position:y` 2.0-3.5s |
| Default Offline Config | `guide_feedback_config.gd` | Khởi tạo `default_value = VALUE_CURRENT` |
| Bơm BBCode Text | `tutorial_page.gd` | Hàm `_step1_rich_action_line()` chèn `[breath amp=0.03 freq=5...]` |

STATUS: COMPLETE
