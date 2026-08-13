# R7 Board Core Parity — 2026-08-13

## Kết quả

Đã đóng `P-BOARD-001..007`: Board hỗ trợ đủ size 4–10, giữ N×N/row-major qua resize, dùng đúng layout/config nguồn, vẽ đúng topology vùng/góc và giữ state/pool lifecycle tương đương source.

## Đối chiếu nguồn

- `scripts/module/gameplay/view/board_view.gd`
- `scripts/module/gameplay/view/board_grid_overlay.gd`
- `scripts/module/gameplay/view/cell_view.gd`
- `scripts/module/gameplay/model/cell_state.gd`
- `scripts/module/abtest/abtest_manager.gd`
- `scripts/module/abtest/config/region_color_config.gd`
- `scripts/module/abtest/config/game_grid_ui_config.gd`
- `scripts/module/abtest/config/board_size_big_config.gd`

## Sai lệch đã sửa

Unity đã có `GameGridUiConfig` và `BoardSizeBigConfig`, nhưng `GameplayManager` và Tutorial tự tạo instance mặc định. Vì vậy provider có trả variant nguồn thì layout thật vẫn không đổi.

- `BoardConfigSet` nay sở hữu `RegionColor`, `GameGridUi` và `BoardSizeBig`.
- AppStart tải palette/layout; GameStart tải profile board lớn đúng timing nguồn.
- Main và Tutorial truyền cùng instance runtime vào `BoardView`.
- `CellView` giữ `LOCKED_MARK` bất biến; đường gameplay bình thường của
  `BoardView` không thay CAT đã xác nhận. Unity Undo dùng đường repaint tường
  minh sau khi model đã rollback để view không giữ CAT cũ.
- `ResetToEmpty` stop+clear CAT particle ở mọi lối reset, không chỉ riêng release pool.
- Không thêm runtime lookup hoặc runtime log.

## Bằng chứng

- Dựng và resize theo chuỗi `4→7→5→10→6→9→8`.
- Mỗi lượt có đúng N² cell active, `FixedColumnCount=N`, upper-left/horizontal và row-major.
- Từng cell giữ đúng row/column/name sau cả tăng và giảm kích thước; không transpose.
- Công thức intrinsic `108×N+30` được khóa đủ size 4–10.
- Bốn layout `game_grid_ui` giữ đúng padding/gap/slot/corner.
- AppScene PlayMode chứng minh ba config runtime tới `GameplayManager` và `BoardView`; single-line dùng padding 3/slot 102, palette V8 đúng RGB và board-size-big đúng ngưỡng size 8+.
- Region-neighborhood fixture khóa đúng 11 thick boundary; bốn góc ngoài nhận đúng TL/TR/BR/BL radius, cell trong radius 0 và hard-edge.
- CAT/MARK/ERROR/DRAFT_CROSS/DRAFT_CAT/LOCKED_MARK có đúng visibility; LOCKED và CAT giữ immutability nguồn.
- Pool release/respawn reset state, icon, pattern, hint/prompt, particle và transform nhưng vẫn tái dùng instance.
- Regression Undo CAT xác nhận cả `GameSession` và `CellView` cùng trở về EMPTY;
  guard CAT của mọi action bình thường vẫn giữ nguyên theo nguồn.

Unity Test Runner:

- Full EditMode sau regression Undo: **658 passed, 0 failed** — 146,142 giây.
- Platform PlayMode sau regression Undo: **11 passed, 0 failed** — 112,931 giây.

## Còn mở ở R7

`P-BOARD-008..009`: safe-area/device aspect và pixel reference 1080×1920.
