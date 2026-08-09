# R9 — Audio bootstrap và cat-hand mặc định

Ngày: 2026-08-09

## Kết quả

- Sửa ERROR thành terminal gesture đúng `BaseGamePage`: không thể bấm xóa, kéo xuyên để đổi nó hoặc bắt đầu stroke từ X đỏ.
- RuleBar v0 dùng wrap, auto-fit 12–36, line spacing tương đương -10 và Glow `et_mask_001.png` của source.
- Xác minh ba `RuleDiagram` mặc định có `visible=false`; chúng chỉ bật ở AB variant, vì vậy không đưa icon vào bản `rule_text=0`.
- Port `ComboVoiceConfig` mặc định 6, `MeowFeedbackConfig` mặc định 0 và `ThumbUpConfig` mặc định 0.
- Cat-hand/Clap/Hawk-eye/Magnifier mặc định tắt hoàn toàn đúng source. Không tạo animation thay thế từ PNG vì Unity chưa có Spine runtime.
- Bổ sung catalog 27 fixed clip và 39 dynamic combo/meow clip; tất cả 66 file `.ogg` cần thiết đều có trong Unity.
- Editor installer tạo hierarchy:
  - `Systems/Audio`
  - `Systems/Audio/Bgm`
  - `SoundService` nằm trên nhánh `Audio`, được nối vào `GameplayManager` và `GameplayFeedbackPresenter`.
- Nối call site nguồn hiện có trong main gameplay: BoardEnter, Mark/Unmark/Cat/Error, UseHint, AllCleared và combo voice theo combo count.
- Bổ sung cleanup toàn bộ voice pool khi SoundService bị hủy.

## Spine

Các `.skel`, `.atlas`, `.png` và `.tres` của bảy LikeHand đã được copy trong `Assets/_Project/Animations/Spine/ui_like_hand`. Unity project chưa cài Spine-Unity runtime nên không thể import/render skeleton trực tiếp. Vì `thumb_up=0`, dependency này không ảnh hưởng hành vi offline mặc định; AB visual variant vẫn mở và phải chọn runtime tương thích trước khi port.

## Kiểm tra

- Core, Services, Gameplay, Editor và EditModeTests compile sạch bằng Roslyn.
- Runner độc lập: 257 case đạt; 7 case LevelGenerator cũ không chạy ngoài Unity vì `Array.Fill/Array.Reverse`.
- Unity cần Refresh để import installer mới, tạo `SoundCatalog.asset`, ghi scene và cho phép nghe PlayMode.

## Đối chiếu Gemini

`GEM-R9-007` tìm đúng các scene/asset và call site âm thanh chính nhưng bỏ sót `thumb_up` default 0 cùng phần lớn decision/config. Codex đã kiểm chứng trực tiếp `thumb_up_config.gd`, `combo_voice_config.gd`, `meow_feedback_config.gd`, `base_game_page.gd`, `combo_feedback_view.gd` và `sound_manager.gd` trước khi sửa.
