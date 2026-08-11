# R13 Bank Pool Matrix Report — 2026-08-11

## Phạm vi

Mở rộng AppScene PlayMode từ SP sang năm pool còn lại của `bank_page.gd`: Regular, LK, LK Modified, LK Style và GC. Test dùng đúng hierarchy động của `BankBrowserPagePresenter`, không gọi thẳng `BankBrowserContract.TryCreateLaunch`.

## Luồng đã khóa

- Regular: `RegularCard → SizeCard → TierCard/GoBtn → Game`.
- LK và LK Modified: root card → `LKRow` đầu tiên → Game.
- LK Style và GC: root card → variant `SizeCard → TierCard/GoBtn → Game`.
- Mỗi launch tạo `GameplaySessionMode.Bank`, pool đúng cờ nguồn, index #1, total hợp lệ và hiện direct `ReturnBankBtn`.
- Mỗi session được AutoComplete để đi qua settlement/Win thật; `Next` tải index kế tiếp theo modulo trong cùng pool và bỏ `from_bank_browser`, vì vậy direct-return ẩn.
- Sau mỗi nhánh, Bank cùng instance được show lại và Game được hide; root state được render lại trước nhánh kế tiếp.
- Reopen Regular sau khi đã đi qua các pool giữ nguyên tổng `BankSizeCardView`, xác nhận dynamic row pool được tái sử dụng trong vòng kiểm thử này.

SP vẫn được giữ bởi hai ca trước: launch/Return Bank và Win #1→Next #2→Fail→Restart đúng metadata.

## Test boundary

- `BankPoolForTests`, `BankTotalForTests` và `BankIndexForTests` chỉ compile với `UNITY_INCLUDE_TESTS`.
- Pool được suy ra từ chính `LevelEntry` đã load (`bank_lk_modified`, `bank_sp`, `bank_lk_style`, `bank_gc`), không đọc lại UI label.
- Không ghi save thật nhờ `GameStateRuntime.OverrideForTests`; không thêm log runtime.

## Kết quả

- Unity compile/reload sạch.
- PlayMode: **8 passed, 0 failed, 0 skipped, 0 inconclusive**, duration **120,906 s**.
- EditMode gần nhất sau thay đổi production trước đó: **511 passed, 0 failed**; thay đổi Bank sau run này chỉ thêm accessor dưới `UNITY_INCLUDE_TESTS` và PlayMode fixture.

## Còn lại

- Selector `+/-/Go`, back-stack spam và pool soak dài có profiler.
- App pause/focus/kill quanh Bank Win/Fail/Next.
- Pixel/scroll/touch parity trên hai aspect và thiết bị thật.
