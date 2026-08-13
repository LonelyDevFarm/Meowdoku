# R8 Main Lifecycle Closure — 2026-08-13

## Kết quả

Đã đóng `P-GAME-001..003`, `P-GAME-005/006` và `P-GAME-008..010`
bằng một AppScene flow xuyên suốt cùng các HintEngine fixture độc lập. Đồng thời
sửa ownership/timing của toàn bộ result config đang được Win/Fail/Toast dùng.

## Đối chiếu nguồn

- `scripts/module/game/view/base_game_page.gd`
- `scripts/module/game/view/game_page.gd`
- `scripts/module/game/view/game_fail_page.gd`
- `scripts/module/result/view/game_win_page.gd`
- `scripts/module/gameplay/model/level_data.gd::compute_prefill`
- `scripts/module/gameplay/core/hint_engine.gd`
- `scripts/module/abtest/abtest_manager.gd`

## Sai lệch đã sửa

- `pass_page`, `pass_text`, `revive_free_logic`, `revive_life`, `win_toast`
  và `fail_text` có class nhưng từng presenter tự tạo instance local.
- `GameplayPagePresenter` chưa reload timing `game_end` trước khi mở Fail,
  khiến `fail_text` không thể nhận variant provider.
- Hint fixtures có R1/R2/R3/chain nhưng chưa khóa một output R4 subset thật.

`ResultConfigSet` nay giữ thứ tự đăng ký nguồn, được thêm vào
`AbConfigRuntime`, và cùng instance được truyền tới Win/Fail/WinToast. Fail
reload `game_end` trước `Owner.Show`, còn các result config GameStart đã được
load cùng gameplay entry.

## AppScene invariants đã khóa

- Main level 1: đúng puzzle 4×4, một tutorial prefill CAT, 3 lives và
  score/combo/mistake bằng 0; mọi cell model khớp `CellView`.
- Correct CAT: model/view CAT, remaining giảm một, score tăng và combo thành 1.
- Wrong CAT: model/view ERROR, lives giảm, mistake tăng, combo về 0 và input
  bị khóa trong `ResolvingWrongGuess`.
- Terminal wrong: đúng một Failed transition và một Fail page.
- Restart: giữ puzzle id/solution, trả đúng initial prefill/model/view, lives 3
  và score/combo/mistake 0.
- AutoComplete: vào Won và từ chối double-tap/board edit trước khi visual
  settlement; đúng một Won transition, một lần đánh giá WinToast và một Win page.
- Provider variants tới đúng Fail/Win/Toast presenter; `fail_text` chỉ có giá
  trị remote sau timing `game_end`.

## Hint evidence

R1 ordering/state filter, R2 row lock, R3 pair subset, R4 four-region subset
không có pair/triple sớm hơn và chain contradiction detail đều có expected
output hard-code, không tự so implementation với chính nó.

## Bằng chứng Unity

- Full EditMode: **667 passed, 0 failed** — 144,376 giây.
- Platform PlayMode: **13 passed, 0 failed** — 147,373 giây.
- Không thêm runtime log.

## Còn mở trong P-GAME

- `P-GAME-007`: ToolButton/reward UI và transition persistence còn một phần.
- `P-GAME-011/012`: cold resume, suspend/timer thiết bị thật.
- `P-GAME-013`: pulse UI của idle tool hint.
