# R4/R7 Main Level Selection Parity — 2026-08-12

## Sai lệch phát hiện

Godot giữ hai lịch kích thước khác nhau:

- `LevelData.get_size()` là lịch static của bank: 100 level đầu và chu kỳ 10 level từ 101 trở đi.
- Main `game_page.gd` luôn gọi `LevelData.get_level_entry(level, _get_ab_size(level))`. Với `size_cycle=2` mặc định, level 3 là 6×6; Unity trước đó bỏ qua lớp này và dùng static 5×5.

Source còn thay mapping level 10 từ SP44 sang SP57 khi `normal_level_10=1`; Unity trước đó chỉ có mapping mặc định.

## Thay đổi

- `SizeCycleConfig.ResolveSize` port nguyên bảng control và variant A–F cùng các boundary 1–10, 11–20, 21–50, 51+, 101+.
- Thêm `NormalLevel10Config` đúng key/default/timing `game_start_normal`.
- Thêm `LevelSelectionConfigSet` vào shared `AbConfigRuntime`, gồm `size_cycle`, `single_region_num` và `normal_level_10` để presenter reload một timing rồi Gameplay dùng cùng instance.
- `GameplayManager` truyền override size, filter config và level-10 config vào `LevelData.GetLevelEntry`; retry/snapshot/direct Bank/Daily không bị đổi đường chọn entry.
- `LevelData` giữ mapping mặc định và chỉ thay level 10 thành SP57 khi config bật.

## Bằng chứng

- `SizeCycle_ControlMatchesSourceGameplayScheduleOneTo250` khóa toàn control schedule 1–250.
- `SizeCycle_VariantsMatchSourceBoundaries` khóa các boundary khác biệt của A–F.
- `GetLevelEntry_ControlCycleOverridesBaseLevelSize` chứng minh static level 3 là 5 nhưng Main override là 6.
- `GetLevelEntry_SpecialMapMatchesEverySourceEntry` khóa đủ 18 level SP/LK và index 1-based.
- `GetLevelEntry_Level10VariantSelectsSp57` khóa SP44 mặc định và SP57 variant.
- `ResolveDifficulty_OrdinaryHardLevelUsesRankFiveNormalTier` khóa hard thường rank 5/tier N/strategy 5.
- `PlatformLevelSelection_MainUsesSourceControlSizeCycle` đi qua AppScene, Home `StartBtn`, shared A/B runtime và Gameplay thật; level 3 đạt Playing với board 6×6.

## Kết quả Unity

- Full EditMode: **617 passed, 0 failed**.
- Platform PlayMode: **8 passed, 0 failed**.
- Unity Tundra compile sạch; không thêm runtime log.

## Progress và invalid-entry fallback

Đối chiếu tiếp `advance_for_entry`, `get_next_entry`, `get_next_entry_main` và `_get_next_entry_with_filter` xác nhận implementation hiện tại đã đúng nên không sửa production:

- Entry ordinary hợp lệ advance một index; Main ordinary tăng `idx` và `since_lk`.
- Entry lỗi chỉ mutation progress trong bộ nhớ rồi tiếp tục tìm; commit player store đúng một lần sau khi chọn xong.
- Nếu toàn pool lỗi, source cố ý trả entry lỗi cuối cùng và không advance entry cuối; Unity giữ nguyên fallback này.
- Special SP/LK dùng fixed index và không chạm sequential bank progress.
- Outer single-region filter tiếp tục tiêu thụ entry mà inner selector đã advance, đúng thứ tự nguồn.

Các fixture `GetLevelEntry_ValidOrdinaryAdvancesOnceAndCommitsOnce`, `GetLevelEntry_InvalidOrdinaryIsSkippedBeforeValidEntry`, `GetLevelEntry_AllInvalidOrdinaryReturnsLastWithoutAdvancingIt`, `GetLevelEntry_InvalidMainEntryAdvancesThenCommitsAcceptedEntry` và `GetLevelEntry_SpecialDoesNotMutateSequentialProgress` khóa các mutation/persistence invariant trên.

## Transform 0–7

Đối chiếu trực tiếp `apply_transform` và `_apply_region_transform` xác nhận Unity giữ đúng quy ước nguồn:

- `0…3` lần lượt là nguyên trạng, xoay 90°, 180° và 270° theo chiều kim đồng hồ.
- `4…7` lật ngang trước, sau đó áp dụng cùng chuỗi xoay 0…3.
- Solution được đổi tọa độ đồng bộ với region map sau từng bước mirror/rotate.
- Unity clone input trước khi biến đổi nên cached bank entry không bị sửa tại chỗ.

Tám case của `ApplyTransform_AllEightVariantsMatchSourceRegionMapAndSolution` dùng region map bất đối xứng và expected map/solution hard-code độc lập, đồng thời khóa invariant không mutation input. Không cần sửa production.

## LK Modified, LK Style và GC

Đối chiếu `bank_data.gd`, `get_next_entry` và `get_next_entry_main` xác nhận implementation Unity hiện tại đã đúng, không cần sửa production:

- Cả 13 bank asset LK Modified/LK Style/GC trong Unity có SHA-256 giống hệt file nguồn.
- Asset thật giải mã/parse ra đúng 169 LK Modified; LK Style có size 7–12; GC có size 6, 8–12 và đúng toàn bộ count rank 1–5 đã kiểm kê từ nguồn.
- Tier `N`, `H` và tier rỗng được giữ/lọc riêng, gồm các bank trộn cả ba dạng.
- Ordinary merge theo `regular → lkstyle → gc`; GC chỉ tham gia tại 10×10 rank 1 hoặc mọi rank size 11.
- Main 10×10 rank 3/4 loại regular; LK Modified loại đúng index 1-based `20, 30, 53, 71, 72, 75, 114, 141, 164`.
- LK Modified được chèn sau bốn ordinary, giữ transform metadata nhưng không biến đổi board; hard selection dùng `r` trước và chỉ relax sang `maxR` sau entry lỗi.

Bằng chứng gồm `BankData_RealVariantBanksMatchSourceInventory`, `BankData_RealVariantTierFiltersMatchSourceInventory`, `GetLevelEntry_OrdinaryPoolOrderIncludesLkStyleThenEligibleGc`, `GetLevelEntry_MainSkipsReservedLkModifiedAndDoesNotTransformIt`, `GetLevelEntry_HardMainRelaxesLkModifiedRankAfterInvalidEntry` và `GetLevelEntry_MainTenRankThreeExcludesRegularPool`.

## Recent-puzzle protection

Đối chiếu `game_page.gd`, `game_state.gd` và `level_data.gd` phát hiện một sai lệch production: Unity đã port `ComputePuzzleId`, `RecordPuzzle` và persistence lịch sử 100 entry nhưng Gameplay chưa gọi chúng khi dựng Main puzzle mới.

Luồng đã được nối đúng ranh giới nguồn:

- Chỉ Main entry mới được chọn từ bank ghi canonical puzzle ID; snapshot restore, retry cache, Bank và Daily không ghi lại.
- Record xảy ra sau selector đã advance/commit, nên snapshot `bank/main/lkmod_progress` phản ánh đúng thời điểm nguồn.
- Duplicate chỉ có ý nghĩa khi cùng puzzle ID từng xuất hiện ở level khác; cùng level được ghi nhưng không chọn lại.
- Duplicate lần đầu gọi public `LevelData.AdvanceForEntry` thêm một lần với persistence đúng branch ordinary/Main/LK Modified rồi chọn lại.
- Lần chọn lại được ghi lịch sử; nếu vẫn duplicate thì chấp nhận ngay, giữ fallback một retry và tránh vòng lặp vô hạn.
- Không thêm log runtime cho chẩn đoán duplicate.

Ba fixture EditMode khóa extra-advance và save ordering cho ordinary, Main ordinary và LK Modified. `PlatformLevelSelection_CrossLevelDuplicateRetriesOnceThenAcceptsFallback` chạy AppScene thật với lịch sử `old A, old C`, xác nhận selection ghi `new A`, skip entry B, chọn `new C`, chấp nhận C dù cũng duplicate, đưa bank progress từ 0 lên 3 và lưu C vào retry cache.

## Tutorial prefill và PreCat

Đối chiếu `LevelData.compute_prefill`, `_setup_entry_normal`, `_resolve_pre_cat`, `_prefill_positions_without_pre_cat`, `pre_cat_decider.gd` và state pending/lock phát hiện hai sai lệch production:

- Unity đã port thuật toán tutorial prefill nhưng chưa gọi nó khi dựng Main entry mới, nên level 1–10 không có CAT đặt sẵn như nguồn.
- `PreCatConfig` tồn tại nhưng không nằm trong shared A/B catalog, khiến runtime luôn dùng default Off dù provider trả nhóm Always/Half.

Luồng đã được nối lại theo source:

- Chỉ Main puzzle mới level 1–10 tính một tutorial prefill theo region area và thứ tự solution; retry đọc vị trí đã lưu, snapshot đọc board đã restore, Bank/Daily không tự tính.
- `LevelSelectionConfigSet` sở hữu `PreCatConfig`; presenter reload đúng timing `game_start_normal_21` trước khi Gameplay đọc cùng instance.
- Pending hard/struggle/demote được consume một lần. Level ≤20, config Off hoặc không có scenario đều không tạo lock mới.
- Khi có scenario, PreCat dùng cell rank ≥3, lưu `pre_type`/position lock và đặt CAT; Half skip vẫn lưu lock rỗng đúng source để quyết định không bị gieo lại khi retry.
- Retry payload loại riêng vị trí pre-cat nhưng giữ tutorial prefill; lần vào lại dựng pre-cat từ lock, và lock sai solution được chọn lại như nguồn.

`PlatformLevelSelection_MainUsesSourceControlSizeCycle` nay khóa luôn một CAT tutorial level 3 cùng cached retry. `PlatformLevelSelection_PreCatUsesConfigAndKeepsLockedCellOnRetry` dùng bank deterministic, provider `pre_cat=Always` và pending struggle để xác nhận `pre_type=2`, CAT hợp lệ, retry payload không nhân đôi và lần mở lại giữ đúng vị trí lock.

## Color map và region palette

Đối chiếu toàn bộ `level_generator.gd`, `_compute_color_map_for_current` trong `game_page.gd`, `board_view.gd` và palette serialized của `cell.tscn` xác nhận:

- Unity dùng đúng `bank_transform` làm seed khi bank không cấp `colorMap`, và seed 0 quay về comparator mặc định như nguồn.
- Comparator degree tăng, LCG/Fisher–Yates, khoảng cách RGB, sRGB→Lab và dark/light pool cho pattern giữ đúng thứ tự nguồn.
- Palette control/custom/new-cell/V3/V5–V9/warm/cool/balanced giữ đúng RGB, thứ tự và số phần tử theo puzzle size.
- Phát hiện một gap production: `RegionColorConfig` đã được port nhưng chưa thuộc shared A/B catalog, nên provider không thể thay đổi palette của ván thật. `BoardConfigSet` nay sở hữu config AppStart; cả `GameplayManager` và `TutorialPagePresenter` dùng đúng shared instance như global `BoardView` của nguồn.
- Fixture mới khóa riêng RGB và Lab bằng palette đen/trắng/đỏ/xanh, khóa hai kết quả pattern khác nhau, 13 branch palette và AppScene thật với provider `region_color=V8` đi tới `BoardView`.

## Kết quả cập nhật

- Full EditMode: **634 passed, 0 failed** trong 167,169 giây.
- Platform PlayMode: **10 passed, 0 failed** trong 150,213 giây.
- Unity compile sạch; không thêm runtime log.
