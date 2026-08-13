# R5 Queendoku Core Parity — 2026-08-12

## Phạm vi

Đối chiếu trực tiếp `scripts/module/gameplay/core/queendoku_core.gd` với `QueendokuCore.cs` cho ba parity item còn mở: `FindConflicts`, `CellsExcludedByCat` và validation dữ liệu board/solution.

## Kết quả đối chiếu

Production Unity đã tương đương nguồn, không cần sửa runtime:

- `FindConflicts` chỉ thu thập cell CAT, xét mọi cặp và trả mỗi CAT một lần nếu tham gia ít nhất một vi phạm hàng, cột, chạm hoặc region.
- `CellsExcludedByCat` duyệt row-major, bỏ chính CAT và lấy union của same-region, same-line và touch; một cell thỏa nhiều luật vẫn chỉ xuất hiện một lần.
- `ValidateSolutionEntry` trả `false` khi region/solution sai shape, column ngoài board hoặc board dựng từ solution không complete. Main selector và Daily selector đều dùng validator này trước khi chấp nhận entry.

## Fixture bổ sung

- Board 5×5 có hai cặp CAT cùng hàng và một CAT độc lập; MARK/ERROR không xuất hiện trong tập conflict.
- Region map 4×4 khóa exact exclusion row-major, gồm cả same-region cell xa CAT và cell đồng thời chạm/cùng region.
- Ma trận null, row thiếu/null, solution null/thiếu, cột âm/quá giới hạn và duplicate column đều trả `false` an toàn.

## Xác minh

- Unity compile sạch.
- Full EditMode: **637 passed, 0 failed** trong 153,766 giây.
- PlayMode không chạy lại vì không có production, scene hoặc prefab thay đổi.
- Không thêm runtime log.
