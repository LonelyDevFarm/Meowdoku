# R17 — Portfolio Visual Pass

## Phạm vi và quyết định

Đóng F1 cho luồng hình ảnh chính của portfolio. Static CAT fallback được chấp nhận theo phạm vi portfolio.

## Thay đổi chính

- Win title dùng asset nguồn, căn giữa tại top `482`.
- `UIManager` reapply assigned sorting sau khi kích hoạt lại window đang inactive.
- Bổ sung Game View adapter chỉ dùng trong Editor để đặt chính xác các độ phân giải mobile.
- Regression fixture có Canvas cha tương ứng cấu trúc `AppScene`.

## Bằng chứng

- Platform EditMode: `60/60`, duration `74.824s`.
- Portfolio Visual PlayMode: `1/1`, duration `36.959s`.
- Manifest 8 ảnh trong `Temp/PortfolioVisualAudit`: `01_Splash`, `02_Home`, `03_Tutorial`, `04_Game`, `04b_Game_1080x2400`, `05_Fail`, `06_Win`, `06b_Win_1080x2400`.
- Unity compile sạch.

## Known differences

- CAT 297-frame atlas chưa port; static sprite + DOTween được chấp nhận.
- Online, ads và IAP nằm ngoài phạm vi.

## Kết luận

F1 `DONE`; F2 `ACTIVE`. Kết luận này chỉ đóng visual pass F1, không đồng nghĩa toàn dự án đã hoàn thành.
