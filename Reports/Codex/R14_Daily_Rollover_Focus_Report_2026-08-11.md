# R14 Daily Rollover/Focus Report — 2026-08-11

## Nguồn đối chiếu

- `scripts/module/ui/clock_ticker.gd`
- `scripts/module/daily/model/daily_entry_state.gd`
- `scripts/module/daily/view/daily_challenge_entry_cell.gd`
- `scripts/module/home/view/home_page.gd`
- `scripts/module/daily_streak/core/feature_daily_streak.gd`

## Contract nguồn

- `ClockTicker` căn tick đầu theo `ceil(now + 0,001)` rồi phát mỗi giây.
- Daily entry tính lại trạng thái và countdown ở mỗi tick; hoàn thành hôm trước trở lại Normal khi sang ngày mới.
- Streak có day-watch độc lập page; đổi Julian day chỉ phát `streak_updated`, không tự thay đổi streak.
- Home chỉ advance `max_daily_date` khi `on_show`; ngày nhỏ hơn mốc lớn nhất vẫn bị coi là Done để chống quay lùi đồng hồ.

## Sai lệch và thay đổi

- Daily/Streak entry Unity đã dùng `ClockTicker`, nhưng `HomePagePresenter.OnShow` vẫn đọc `DateTime.Now` riêng khi ghi `max_daily_date`. Hai consumer vì vậy không dùng cùng nguồn thời gian khi resume/đổi múi giờ và không thể kiểm chứng nhất quán.
- `HomePagePresenter` nay giữ ClockTicker được inject và dùng `ClockTicker.LocalNow` cho cùng ranh giới `max_daily_date`; fallback `DateTime.Now` chỉ dùng khi scene không bind clock.
- Thêm test seam dưới `UNITY_INCLUDE_TESTS` để đọc trạng thái presentation; không ảnh hưởng player build và không thêm runtime log.

## AppScene PlayMode matrix

1. Khởi động Home ở level 21, ngày 10/08, Daily đã hoàn thành và Streak đã check-in: Daily hiện Done, Streak hiện checked; Home advance max date từ ngày 09 lên ngày 10.
2. Đổi clock/date provider sang 11/08 rồi gửi callback focus-in: tick kế tiếp đổi Daily sang Normal và Streak sang unchecked, giữ streak bằng 3 và chưa advance max date khi Home vẫn đang mở.
3. Hide/show lại cùng instance Home: max date được persist thành 11/08 đúng điểm gọi nguồn.
4. Xóa completion, quay clock về 10/08 rồi gửi callback pause-resume: entry cập nhật về Done nhờ `today < max_daily_date`; Streak trở lại checked và max date không giảm.

## Kết quả

- Unity compile/domain reload sạch.
- EditMode: **515 passed, 0 failed, 0 skipped, 0 inconclusive** (`60,707 s`).
- PlayMode: **14 passed, 0 failed, 0 skipped, 0 inconclusive** (`206,003 s`).

## Còn lại

- Hard-kill/resume, thay timezone và đổi ngày trên Android/iOS thật thuộc R17 device matrix.
- Daily ad-time compensation trên provider thiết bị thật.
- Streak nhiều ngày và Rank period tiếp theo là hai meta matrix kế tiếp.
