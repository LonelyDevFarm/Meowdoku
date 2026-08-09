# GEM-R5-001 Báo cáo Dependency Map và Đặc tả Gameplay Domain

**STATUS**: COMPLETE  
**GENERATED_AT**: 2026-08-08 17:52:00  
**SOURCE_ROOT**: `D:\Projects\_GameExtract\Main_Meokdoku`

## 1. Dependency Map (Thứ tự Port sang Unity C# tối ưu)

Để Codex có thể port toàn bộ logic trò chơi sang C# mà không cần đụng đến GameObject hay Monobehaviour, cần tuân thủ thứ tự xây dựng các lớp Domain thuần (Pure C# Classes) như sau:

1. **`CellState`** (Tầng đáy): Chỉ chứa các Enum trạng thái ô. Không phụ thuộc ai.
2. **`QueendokuCore`** (Tầng Rules): Chứa luật chơi tĩnh. Phụ thuộc `CellState`.
3. **`StepRecord` & `StepHistory`** (Tầng State): Quản lý Undo/Redo. Phụ thuộc `CellState`.
4. **`HintEngine`** (Tầng Solver): Phụ thuộc `CellState` và mảng 2D. (Có thể chạy trên một background thread ở Unity).
5. **`BoardModel` / `GameLogic`** (Tầng quản lý, cần thiết kế mới): Tách phần Data thuần túy từ `BoardView` và `GamePage` hiện tại ra một class C# thuần. Chịu trách nhiệm giữ State hiện tại, đếm remaining cats và xử lý sự kiện đặt ô.

---

## 2. Đặc tả Tầng Domain Core

### A. CellState (`cell_state.gd`)
- **Trạng thái**: `EMPTY (0)`, `CAT (1)`, `MARK (2)`, `ERROR (3)`, `DRAFT_CROSS (4)`, `DRAFT_CAT (5)`, `LOCKED_MARK (6)`.
- **Hệ quả**: `is_blank` (`EMPTY`, `DRAFT_CROSS`, `DRAFT_CAT`), `is_cross` (`MARK`, `ERROR`, `LOCKED_MARK`).

### B. QueendokuCore (`queendoku_core.gd`)
Nơi chứa toàn bộ luật (Rules) xác định đúng/sai của lưới.
- **Rule Enum**: `NONE (0)`, `SAME_COLOR (1)`, `SAME_LINE (2)`, `NO_TOUCH (3)`. Mức độ nghiêm trọng tăng dần theo giá trị nhỏ (ưu tiên báo lỗi SAME_COLOR trước).
- **`_classify_pair(a, b, regions)`**: Phân loại lỗi giữa 2 ô mèo.
- **`classify_violation(r, c, placed_cats, regions)`**: Tìm lỗi vi phạm luật của 1 ô so với các ô đã đặt. Trả về lỗi nghiêm trọng nhất.
- **`find_conflicts`**: Trả về Dictionary các ô đang vi phạm luật.
- **`is_complete`**: Trả về `true` nếu `piece_count == size` và `find_conflicts().is_empty()`.

### C. StepHistory & StepRecord (`step_history.gd`)
- **Dữ liệu**: Mỗi `StepRecord` chứa mảng `cells` (mỗi phần tử có `pos`, `before`, `after`). Cờ `is_cat_placement` và `is_wrong_guess`.
- **Hành vi**: Stack tiêu chuẩn `push_step`, `pop_last`, `serialize`/`deserialize` mảng JSON.

---

## 3. Đặc tả HintEngine (`hint_engine.gd`)

Chứa toàn bộ thuật toán mô phỏng cách con người giải đố (Human-like Solving). Hoạt động hoàn toàn độc lập với UI.

- **`find_mark_hint`**: R0. Đánh dấu X vào hàng/cột/vùng lân cận 3x3 quanh 1 con mèo đã đặt.
- **`find_r1_hint`**: R1. Quét tìm 1 Hàng, 1 Cột, hoặc 1 Region chỉ còn **đúng 1 ô trống** hợp lệ duy nhất để đặt mèo. Trả về `unit_type` ("row", "col", "region").
- **`find_r2_hint`**: R2 (Intersection / Pointing). Gồm 4 mode: `r2a_row`, `r2a_col`, `r2b_row`, `r2b_col`. Phát hiện nếu mọi ứng viên của một Region đều nằm trên 1 hàng/cột thì có thể loại trừ các ô khác trên hàng/cột đó; hoặc ngược lại.
- **`find_r3_r4_hint`**: R3 & R4 (Naked/Hidden Subsets). Sử dụng `_gen_subsets` để duyệt tổ hợp chập `k` (từ 2 đến 6). Phát hiện `k` Regions bị giam trong đúng `k` hàng hoặc `k` cột. Trả về `locked_rows` hoặc `locked_cols` để đánh chữ X (Mark).
- **`find_chain_hint`**: R5 (Backtracking/Contradiction). `_chain_try_contradiction` thử giả sử đặt mèo vào 1 ô, sau đó lan truyền ép buộc (Force-place). Nếu dẫn đến việc 1 hàng/cột/vùng không còn ô trống nào (`cnt == 0`), kết luận giả thiết sai và ô đó phải là Dấu X. Tính độ sâu (`depth`) để định giá độ khó.
- **`compute_cell_ranks`**: Phân loại độ khó từng ô mèo (dùng cho tính năng Locate Tool). Mô phỏng giải lần lượt R1, R2, R3. Ô nào giải được bằng R1 thì Rank 1, bằng R2 thì Rank 2, v.v. Các ô quá khó (cần R4, Chain) sẽ bị đánh Rank fallback (mặc định 4).
- **`compute_r4_plus_cells`**: Tìm các ô mèo cực khó chưa thể giải bằng R1-R3.

---

## 4. Bóc tách Domain Logic đang rò rỉ (Leaking) trong BoardView

Một lượng lớn Domain Logic thuần túy đang bị viết trực tiếp vào file hiển thị `board_view.gd`. Khi port sang Unity, Codex phải bóc tách các logic này đưa vào class GameModel/BoardModel C# thuần:

1. **Kiểm tra Integrity (Chống hack/lỗi)**: `board_view.gd` dòng 636-642 liên tục so khớp mảng state với `_solution_cols` để phát hiện Mèo đặt ở ô sai (CAT at non-solution) hoặc Dấu X đặt ở ô đúng (ERROR at solution). Nếu phát hiện, nó "tự chữa lành" (healing) biến ô đó thành EMPTY.
2. **Theo dõi lượng Mèo và Remaining Cats**: Tính toán mèo còn lại (`_puzzle_size - count_cat_cells()`) đang nằm ở UI.
3. **Phân loại Sai lầm (Wrong-cat / Error classification)**:
   - Dấu X đỏ (Error) được set qua hàm `mark_cell_error(r, c)`.
   - Cờ `has_ever_errored()` lưu trữ trạng thái một ô đã từng bị người chơi đặt sai hay chưa, dùng để tính toán điểm sao cuối ván.
4. **Tool Prefill (Mèo cho sẵn)**: `_meow_prefill_cat_count` đang đếm số mèo prefill ngay trong view.
