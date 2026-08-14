# R17 — Hoàn tất hình ảnh Rank — 2026-08-14

## Khiếm khuyết quan sát được

- RankChange chỉ hiện `FloatingSelfRow`.
- Danh sách trên RankPage trống.

## Nguyên nhân gốc

- Visual fixture đặt điểm của toàn bộ bot về `0`, nên không tạo được nhóm xếp hạng lân cận hữu ích.
- RankChange tắt `VerticalLayoutGroup` trong lúc animation, khiến `ContentSizeFitter` co chiều cao `RowList` về `0`.
- `Mask` stencil cũ loại các row trực tiếp; floating row nằm ngoài mask nên vẫn sống sót và hiển thị.

## Sửa chữa

- Dùng fixture trực quan xác định với năm bot điểm thấp để luôn có nhiều hàng lân cận.
- Đóng băng rồi bật lại fitter quanh rise animation, đồng thời khôi phục đúng trạng thái trong cleanup.
- Thêm upgrader chuyển viewport của `RankActivityPage` và `RankActivityChange` sang `RectMask2D`, đồng thời vô hiệu hóa `Mask` cũ.

## Căn chỉnh RankPage theo scene nguồn

- Sửa lỗi gốc làm toàn bộ header lệch sang phải: `RankActivityPageLayoutPresenter` đã đổi một `RectTransform` stretch sang anchor giữa nhưng giữ chiều rộng bằng `0`. Runtime giờ luôn khôi phục khung nguồn `1080×184` cho Header và `1080×521` cho Podium.
- Header bổ sung hai sprite cá trắng và giữ đúng vị trí nguồn cho Back, Title và Countdown.
- Ba podium dùng đúng vị trí cuối animation của `top3_podium.tscn`: Top 1 ở giữa và cao hơn, Top 2 bên trái, Top 3 bên phải; avatar, medal, số hạng, tên, điểm và chest được dựng thành nhánh riêng để không còn ghép sai người/sai bục.
- `RankCell` giữ khung nguồn `968×180`; Content, Avatar, Name, Score và Chest được khóa bằng contract test theo đúng tọa độ trong `rank_cell.tscn`, tránh thành phần tụt xuống dưới tâm row.
- Prefab được rebuild bằng Unity API trên đúng asset hiện có nên GUID và serialized reference được bảo toàn.

## Bằng chứng

- Unity compile clean.
- Platform EditMode: `89/89`.
- Portfolio Visual PlayMode: `4/4`.
- Ảnh: `Temp/PortfolioRankAudit/20_RankChange.png` và `Temp/PortfolioRankAudit/23_RankPage.png`.
- Full EditMode sau thay đổi layout: `705/708`; ba lỗi còn lại đều là Bank readable-string đã biết. Các test hình học và hierarchy Rank mới đều đạt.
- Đã thêm lệnh `Tools/Meowdoku/Run Rank Visual Audit` để các lượt sau chỉ chạy luồng Rank, không bắt đầu lại toàn bộ bộ ảnh.

Lượt Rank PlayMode mới ngày 2026-08-14 không hoàn tất vì Unity Test Runner đứng sau khi nạp AppScene và chưa đi tới bước chụp Rank. Editor đã được dừng/reset an toàn; cần một lượt manual QA ngắn cho hình ảnh runtime, không coi đây là thất bại logic Rank.

Platform PlayMode chưa chạy lại; kết quả mới nhất vẫn là `25/27` với hai lỗi Win/WinToast đã biết.

## Phạm vi còn lại

- Win/Fail — VA-07.
- Streak — VA-08.
- Độ tin cậy của Settings.
- Regression Platform cuối và build trình diễn.
