# R12 Bank Selector + Back-stack

Ngày: 2026-08-11  
Unity: 6000.3.19f1

## Phạm vi nguồn đã đối chiếu

- `scripts/module/bank/view/bank_page.gd`
- Các hàm selector `_on_tier_minus`, `_on_tier_plus`, `_on_tier_go` và LK selector.
- Các handler Back `_on_back_btn_pressed`, `_on_tier_back_btn_pressed`, `_on_list_back_btn_pressed`, `_on_lk_back_btn_pressed`, `_on_lkss_back_btn_pressed`.

## Kết quả đối chiếu

- Tier và LK selector đều bắt đầu ở 1, clamp trong `[1, count]` và GO đổi sang index zero-based đúng nguồn.
- Regular Tier Back về Regular Size; LK Style Tier Back về Variant Size; GC Tier Back về Bank Root đúng hành vi nguồn.
- LK/LK Modified list Back về Root; SP list Back về Root; Level list thường quay về Tier tương ứng.
- `BankBrowserContract`, `BankBrowserPagePresenter` và `BankTierCardView` đã có hành vi production đúng; lát cắt này chỉ thêm accessor quan sát trạng thái và khóa regression bằng thao tác UI thật.

## Kiểm thử Unity thật

- Compile: Unity/Tundra cập nhật `Meowdoku.Gameplay.dll`, `Meowdoku.Editor.dll` và `Meowdoku.PlayModeTests.dll` không có C# error.
- PlayMode: `RESULT passed=9 failed=0 skipped=0 inconclusive=0 duration=120,052`.
- EditMode: `RESULT passed=512 failed=0 skipped=0 inconclusive=0 duration=52,825`.
- AppScene test bấm Minus tại cận 1, Plus tới cận trên, quay lại và launch entry #2 cho cả Tier selector lẫn LK selector.
- Test đi qua Back của Regular/LK/LK Modified/LK Style/GC/SP; 8 vòng Regular Root→Size→Tier→Back và reopen SP không tăng tổng row Size/Tier/Level.

## Hạ tầng Editor

- Named-event bridge không tồn tại sau một domain reload của phiên Editor hiện tại.
- Bổ sung phím tắt Editor-only làm fallback: Ctrl+Shift+Alt+R (Refresh), Ctrl+Shift+Alt+E (EditMode), Ctrl+Shift+Alt+P (PlayMode).
- Không thêm log runtime và không thay đổi player build.

## Còn lại

- Profiler/device soak dài cho pool và pixel parity của Bank thuộc vòng polish/device sau.
