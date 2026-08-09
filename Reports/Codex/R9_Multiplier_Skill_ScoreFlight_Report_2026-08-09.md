# R9 — Multiplier, Skill Score và Score Flight

Ngày: 2026-08-09

## Kết quả

- Port multiplier bằng đúng sprite `ui_mao_cf_pic_00..12`; không thay bằng font/TMP.
- Port skill bonus thành bitmap `+N`, giữ separation `-6`.
- Ghép score bubble bên trái và multiplier/skill bên phải với gap 10 px, clamp cả cụm trong feedback area.
- Giữ previous multiplier rồi đổi sang target đúng mốc animation nguồn.
- Port score/life flight: delay theo variant, cubic Bézier 8 vòng Newton, duration 0,57 s, trail linger 0,067 s, burst 1,5 s và score bounce 1,2 → 1.
- Score chỉ roll khi flight đến đích; life bonus dùng life curve riêng.
- Scene installer chạy lặp an toàn và tổ chức pool theo cây:
  - `HUD/Feedback/ScoreBubbles`
  - `HUD/Feedback/DeductionBubbles`
  - `HUD/Feedback/SkillBubbles`
  - `HUD/Feedback/Multipliers`
  - `HUD/Feedback/ScoreFlights`

## Chuyển thể Unity bắt buộc

Godot dùng `Line2D` và `CPUParticles2D`, không có thành phần tương đương trực tiếp trên Unity Overlay Canvas. Unity dùng pool `Image` cố định cho trail/glow/star, nhưng lấy asset, count, lifetime, timing và vận tốc từ scene nguồn. Không tạo nội dung gameplay mới.

## Kiểm tra

- `Meowdoku.Gameplay`: Roslyn compile sạch.
- `Meowdoku.Editor`: Roslyn compile sạch.
- `Meowdoku.EditModeTests`: compile sạch với 4 case mới cho endpoint, clamp và life arc.
- Unity đã chạy installer và ghi đủ 28 pool item: 8 score, 4 deduction, 4 skill, 4 multiplier, 6 flight; không có compiler error trong Editor log.
- Full Unity Test Runner và đánh giá hình ảnh/timing PlayMode chờ người dùng chạy sau Refresh.

## Gemini

`GEM-R9-005` hữu ích ở việc dò file và asset. Đề xuất TMP và nhận định thiếu particle parameters không được sử dụng vì trái với nguồn; toàn bộ hằng số quan trọng được kiểm tra trực tiếp trong Godot script/scene.
