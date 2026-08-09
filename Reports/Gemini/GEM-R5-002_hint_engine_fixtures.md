# GEM-R5-002 Đặc tả Fixtures và API Contract cho HintEngine

**STATUS**: COMPLETE  
**GENERATED_AT**: 2026-08-08 18:10:00  
**SOURCE_ROOT**: `D:\Projects\_GameExtract\Main_Meokdoku`

## 1. Trình tự và Chữ ký API (API Signatures)

`HintEngine` đánh giá lưới theo thứ tự ưu tiên độ khó tăng dần. Hàm `compute_cell_ranks` (và `compute_r4_plus_cells`) mô phỏng lại quá trình này bằng vòng lặp:
1. `find_mark_hint` (R0: Auto-mark lân cận)
2. `find_r1_hint` (R1: Chỉ còn 1 ô duy nhất)
3. `find_r2_hint` (R2: Giao điểm / Khóa hướng)
4. `find_r3_r4_hint` (R3/R4: Tổ hợp bộ k vùng)
5. `find_chain_hint` (R5: Phản chứng / Backtracking - *không dùng trong compute_cell_ranks, chỉ dùng cho gợi ý R5 thủ công*)

**Contract Candidate (`_can_place`)**:
- Trả về `true` CHỈ KHI `board[r][c] == CellState.EMPTY (0)`. 
- BẤT KỲ trạng thái nào khác: `MARK (2)`, `ERROR (3)`, `DRAFT_CROSS (4)`, `DRAFT_CAT (5)`, `LOCKED_MARK (6)`, `CAT (1)` đều lập tức bị loại khỏi danh sách candidate.

---

## 2. Đặc tả Fixtures Cơ học

Codex có thể dùng các Fixture (Board, Size, Regions, Solution) tối giản dưới đây để viết Unit Test nhằm đảm bảo quá trình Port sang C# giữ nguyên vẹn logic mảng 2D. (Ký hiệu: `C`=CAT, `E`=EMPTY, `X`=MARK).

### A. R0 (Mark Hint / `find_mark_hint`)
- **Tình huống**: Có một con mèo đã đặt hợp lệ, cần X toàn bộ hàng, cột và 8 ô xung quanh.
- **Fixture 3x3**:
  - `regions`: `[[0, 0, 0], [1, 1, 2], [1, 2, 2]]`
  - `board`: `[[C, E, E], [E, E, E], [E, E, E]]`
- **Expected Output**:
  - `found`: `true`
  - `strategy`: `"R1_mark"`
  - `cat_cell`: `(0,0)`
  - `unit_cells`: Chứa các toạ độ bị đánh X: `(0,1), (0,2), (1,0), (2,0)` (hàng/cột) và `(1,1)` (đường chéo).

### B. R1 (Naked Single / `find_r1_hint`)
- **Tình huống**: Hàng 0 chỉ còn đúng ô `(0,2)` là EMPTY, các ô khác đã bị MARK.
- **Fixture 3x3**:
  - `regions`: `[[0, 0, 0], [1, 1, 2], [1, 2, 2]]`
  - `board`: `[[X, X, E], [E, E, E], [E, E, E]]`
- **Expected Output**:
  - `found`: `true`
  - `unit_type`: `"row"`
  - `unit_index`: `0`
  - `cell`: `(0,2)` (Vị trí cần đặt mèo)

### C. R2 (Intersection / `find_r2_hint`)
- **Tình huống (`r2a_row`)**: Toàn bộ candidate của Region 0 đều nằm trên Hàng 0. Ta có thể MARK các ô thuộc Region khác nằm trên Hàng 0.
- **Fixture 3x3**:
  - `regions`: `[[0, 0, 1], [0, 2, 1], [2, 2, 1]]` (Vùng 0 hình chữ L)
  - `board`: `[[E, E, E], [X, E, E], [E, E, E]]` (Ô `(1,0)` của Vùng 0 bị X)
- **Cơ chế**: Vùng 0 hiện tại chỉ còn candidate ở `(0,0)` và `(0,1)`. Cả hai đều thuộc hàng 0. Suy ra hàng 0 không thể chứa mèo ở Region 1 `(0,2)`.
- **Expected Output**:
  - `found`: `true`
  - `mode`: `"r2a_row"`
  - `region`: `0`, `row`: `0`
  - Phải sinh ra MARK cho ô `(0,2)`.

### D. R3/R4 (Subsets / `find_r3_r4_hint`)
- **Tình huống**: Tổ hợp bộ K vùng (k=2). Vùng 0 và Vùng 1 hoàn toàn bị giam trong Hàng 0 và Hàng 1. Ta MARK toàn bộ các ô thuộc Vùng khác nằm ở Hàng 0 và 1.
- **Fixture 4x4**:
  - `regions`: `[[0, 0, 2, 2], [1, 1, 2, 2], [3, 3, 3, 3], [4, 4, 4, 4]]`
  - `board`: Trống 100%
- **Cơ chế**: Region `0` và `1` chỉ có thể nằm trong row `0` và `1`. Cần 2 mèo cho 2 vùng này, nên row 0 và 1 chắc chắn bị lấp đầy. Ô `(0,2), (0,3), (1,2), (1,3)` thuộc vùng 2 phải bị MARK.
- **Expected Output**:
  - `strategy`: `"R3"` (vì k=2 <= 3)
  - `regions`: `[0, 1]`
  - `locked_rows`: `[0, 1]`
  - Các ô sinh ra MARK: `(0,2), (0,3), (1,2), (1,3)`.

### E. Chain (R5 / `find_chain_hint`)
- **Tình huống**: Thử nghiệm giả định (Phản chứng).
- **Cơ chế**: Hệ thống copy mảng State sang `base` dict. Loop mọi ô. Thử ép đặt Mèo (`_chain_place`). Quét xem có hàng/cột/vùng nào bị triệt tiêu toàn bộ candidate xuống 0 không (`cnt == 0`). Nếu có, chứng tỏ ô đang xét không thể là Mèo (Nó phải là MARK).
- **Output**:
  - Trả về `"R4_chain"` nếu depth (số bước ép buộc lây lan) <= 2. Trả về `"R5_chain"` nếu depth > 2.
  - `chain`: object mô tả vết (contradiction steps).

---

## 3. Quá trình tính Hạng (Rank) Ô cờ 

Codex cần Port nguyên vẹn vòng lặp `compute_cell_ranks` vì đây là core của hệ thống Locate Tool.

**Input**: Mảng board (thường là rỗng 100%), size, regions, mảng giải pháp `solution`, rank dự phòng `fallback_strategy=4`.
**Logic Vòng Lặp Vô Hạn (`while true`)**:
1. Gọi `find_mark_hint`. Nếu có, áp dụng `CellState.MARK` vào `work` board, `continue`.
2. Gọi `find_r1_hint`. Nếu có, đặt `CellState.CAT` vào `work` board. **Lưu Rank:** Nếu toạ độ khớp `solution`, gán `ranks[cell] = current_max`. Reset `current_max = 1`. `continue`.
3. Gọi `find_r2_hint`. Nếu có, áp dụng MARK (`_apply_r2_marks`), đẩy độ khó `current_max = maxi(current_max, 2)`. `continue`.
4. Gọi `find_r3_r4_hint`. Nếu có VÀ trả về `"R3"`, áp dụng MARK, đẩy `current_max = maxi(current_max, 3)`. `continue`.
5. Nếu không tìm được hint nào từ 1-4, `break` (thoát lặp).

**Hậu xử lý (Termination & Fallback)**:
- Duyệt mọi ô `solution` bằng true. Nếu ô đó chưa có mặt trong mảng `ranks` (tức là thuật toán R1-R3 bị kẹt, không thể tự giải tiếp), gán `ranks[cell] = fallback_strategy`.

---

## 4. Các Giới Hạn Tối Đa (Hard Limits) Nội Bộ

Khi Port thuật toán `_gen_subsets` của R3/R4 sang C# đệ quy, cần giữ nguyên hard-limit sau để tránh Memory Overflow / Freeze:
- `max_k = mini(unplaced.size() - 1, 6)`. Thuật toán R3/R4 chỉ thử tìm tổ hợp Subset lên tới k=6. (Sẽ không bao giờ quét tổ hợp 7+ vùng).
- Tổ hợp R3 (k <= 3) được mang nhãn `"R3"`. Tổ hợp k=4,5,6 mang nhãn `"R4"`. Hàm Rank chỉ ăn điểm cho `"R3"`. Cố tình bỏ qua `"R4"` để cho game coi đó là bài siêu khó.
