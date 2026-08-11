# R13 — Win, Fail, Revive runtime slice

Ngày: 2026-08-10  
Unity: 6000.3.19f1

## Phạm vi đã port

- Nối `GameplayPagePresenter` với terminal transition: Fail mở ngay khi hết mạng; Win mở sau 1,2 giây như nguồn mặc định.
- Dựng `WinPage.prefab` và `FailPage.prefab` bằng Editor installer, đăng ký vào `UIRegistry.asset`; cây prefab tách `Visuals`, `Content`, `Statistics/Actions` để dễ quản lý.
- Win mặc định: title normal không lặp liền, hard title, next main/Bank, sound, mask, time/score/combo G4 cùng delay và roll timing nguồn.
- Pass-page G1/G2: panel Size/Time/Score/Combo; G2 thêm Completion/Mistake/Tools với sprite nguồn; praise highlight cam, settle sound và nút Next khóa đến marker 0,69804 giây.
- Port `pass_text` V0, beat-percent, V2 và V3-G1/G2/G3 thành contract thuần; giữ pool localization key, hard/retry/perfect/percent threshold và last-win comparison.
- Port `last_win_beat_percent` vào player progress với setter idempotent.
- Fail: remaining cats, input lock 1,5 giây, BGM pause/resume, fail sound, progress/promote text và runtime per-level percent cache.
- Restart từ Fail không settle thất bại lần hai, không advance level/Bank và giữ `restart_count`.
- Revive: khôi phục lives theo `revive_life`, free-on-level/free-once theo `revive_free_logic`, persist `has_used_revive_free`; reward revive đi qua `IRewardedReviveService` và ẩn khi không có adapter.
- Gameplay elapsed time chỉ tính lúc board đang chơi; thời gian vào/ra page, Win delay và thời gian đứng ở Fail không bị cộng.
- Port metadata/default của `win_toast`; default nguồn là Off.
- Port win-toast size 6–12, bốn tier, 30 localization key, `{N}/{CATS}`, màu/icon nguồn và GenericPopup lifecycle. Game page chờ 1,5 giây khi toast hiện, ngược lại chờ 1,2 giây.

## Nguồn đã đối chiếu trực tiếp

- `scripts/module/result/view/game_win_page.gd`
- `scripts/module/result/view/game_fail_page.gd`
- `scripts/module/result/strategy/pass_text/pass_text_strategy*.gd`
- `scripts/module/result/model/pass_text_stats.gd`
- `scripts/module/result/view/pass_page_g1_board.gd`
- `scripts/module/result/view/pass_page_g2_board.gd`
- `scripts/module/abtest/config/pass_page_config.gd`
- `scripts/module/abtest/config/pass_text_config.gd`
- `scripts/module/abtest/config/fail_text_config.gd`
- `scripts/module/abtest/config/revive_life_config.gd`
- `scripts/module/abtest/config/revive_free_logic_config.gd`
- `scripts/module/abtest/config/win_toast_config.gd`
- `scripts/module/game_state/game_state.gd`

## Kiểm chứng

- Unity đã import `WinPage.prefab`, `FailPage.prefab` và `GamePage.prefab`; `Root/PassPanel` cùng `Overlays/WinToast` đã được sinh và mọi presenter/UI/sprite reference đều serialize khác 0. Không có `m_Script: {fileID: 0}` hay lỗi import liên quan trong Editor log.
- `Meowdoku.Core`, `Meowdoku.Gameplay`, `Meowdoku.Editor` và `Meowdoku.EditModeTests` compile sạch bằng Unity Roslyn response files.
- Regression fixtures bao phủ config count/default, P90/rounding, PassText branches, persisted beat percent, free revive, fail restart, next Bank, `StepsUsed`, special/hard advance, fail–revive nhiều vòng và prefab/registry structure.
- Unity Test Runner và PlayMode end-to-end chưa chạy trong lượt này; không đánh dấu R13 hoàn thành.

## Còn lại của R13

- Chạy Unity Test Runner và PlayMode G1/G2; tên `Board` trong Godot là panel thống kê, không phải preview puzzle.
- PlayMode `Overlays/WinToast` bằng config override; default nguồn vẫn Off.
- Nối rewarded-ad adapter/mock để test nhánh revive có quảng cáo.
- Thay static cat sprite bằng Spine khi có runtime tương thích; hoàn thiện animation/VFX và pixel parity.
- PlayMode matrix: main/bank, normal/hard, win/fail, restart nhiều lần, revive nhiều lần, quit/resume tại mọi transition.
