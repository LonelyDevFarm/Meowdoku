# R17 Streak Visual Closure — 2026-08-14

## Phạm vi

Khép khoảng trống hình ảnh của thẻ Chuỗi ở Home và trang Daily Streak trong AppScene, ưu tiên chuyển đúng tài nguyên/cấu trúc từ Godot trước khi dùng adapter Unity.

## Bằng chứng nguồn

- `streak_page.tscn`: `bg_9grid`, `sun`, `sudoku_bg_round20`, bảy day slot, `normal_btn_bg` và `vector_1`.
- `streak_day_slot.tscn`: unchecked node 120×120, màu `(0.886, 0.835, 0.768, 1)`, bán kính bốn góc 60; checked dùng `dot`, ngày cuối dùng reward chest.
- `streak_entry_cell.tscn`: nền `state_checked1`, lớp Sun và checked badge `state_checked2`.
- `streak_mini_entry_cell.tscn`: shadow `mini_bg`, panel vàng bo góc, glow và checked/unchecked state.
- `streak_page.gd`: trạng thái Main chỉ vô hiệu hóa nút Sun; `SunImg` vẫn hiển thị. CAT không thuộc main Streak page nên không được tự thêm.

## Thay đổi Unity

- Giữ `SunRoot` hiển thị ở mọi display state; chỉ bật tương tác Sun trong trạng thái `Lit`.
- Dựng lại `StreakPage.prefab` qua Unity Prefab API với hierarchy `Background / Top / Hero / WeekSlots / Instructions / Actions`.
- Thay ô unchecked hình vuông bằng `RoundedImageView` bán kính 60, giữ đúng kích thước/màu nguồn.
- Dùng source best frame, back button/icon, checked dot và reward chest.
- Phục hồi đủ lớp của Home full/mini Streak entry: background, shadow/panel, Sun, checkmark và count badge bo tròn.
- Installer nhận cả tên sprite atlas có hậu tố `_0` và idempotent qua refresh; không sửa YAML prefab thủ công.

## Kiểm thử

- Unity compile: sạch, không có `error CS`.
- Full EditMode: `704/707`; hai test composition Streak mới đạt. Ba lỗi còn lại đều là Bank readable-string đã biết và không liên quan thay đổi này.
- Portfolio Visual PlayMode: `4/4`.
- Ảnh runtime:
  - `Temp/PortfolioVisualAudit/02_Home.png`
  - `Temp/PortfolioMetaAudit/31_Streak.png`

## USER QA còn lại

Ở Home kiểm tra thẻ Chuỗi; mở trang Chuỗi, nhìn mặt trời/nút ngày/rương và thử nút Back trên màn hình thực tế. Đây là kiểm tra cảm nhận cuối, không phải khoảng trống implementation.
