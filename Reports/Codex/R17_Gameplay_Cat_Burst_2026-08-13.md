# R17 — Gameplay CAT Burst và Feedback Sorting

## Phạm vi

Hoàn thiện CAT star/glow burst trong gameplay và bảo đảm feedback hiển thị đúng thứ tự trên UI. Hạng mục CAT burst được đóng riêng; toàn bộ F2 chưa hoàn tất.

## Đối chiếu nguồn

Godot `cell.tscn` phát emission tại `0.1164s`, glow trong `0.5s` và stars trong `1.02s`; hiệu ứng gồm `2x12` stars với ba màu green/yellow/purple. `ComboFeedback` nằm sau `RuleBar` để feedback hiển thị phía trên.

## Thực thi

Unity dùng pool UGUI cố định sáu view; mỗi view có một glow và 24 stars từ `et_glow_002` và `et_star_1`. DOTween chạy unscaled và được cleanup khi disable hoặc tái sử dụng. Runtime lifecycle sorting guard duy trì thứ tự đúng cho các cached page.

## Bằng chứng

- EditMode: `61/61`, duration `76.231s`.
- Platform PlayMode: `25/25`, duration `228.905s`.
- Visual: `1/1`, duration `24.367s`.
- Ảnh: `04c_CatBurst`.

## Trạng thái

CAT burst item: `DONE`. F2 vẫn `ACTIVE`; không tuyên bố các hạng mục F2 còn lại đã hoàn tất.
