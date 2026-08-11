# R15 Meta Persistence Durability Report — 2026-08-11

## Nguồn đối chiếu

- `scripts/module/profile/profile_service.gd`
- `scripts/module/profile/model/profile_data.gd`
- `scripts/module/robot/robot_service.gd`
- `scripts/module/robot/model/robot_pool.gd`
- `scripts/module/rank_activity/rank_activity_manager.gd`
- `scripts/module/rank_activity/model/rank_activity_data.gd`
- `scripts/module/rank_activity/core/rank_activity_period.gd`
- Award transaction/cold-start sweep đã port ở R14.

## Sai lệch đã tìm thấy

- Godot lưu profile bằng `ConfigFile.set_value("profile", "data", profile_dict)`. Unity cũ lại ghi các field profile trực tiếp trong section `profile`; tự round-trip được nhưng không giữ logical schema nguồn.
- Regression cũ chủ yếu dùng memory store giữ cùng object, nên chưa chứng minh serialization thật, process recreation, robot clock rollback hoặc Rank/Award recovery qua nhiều lần restart.

## Phần đã sửa

- `ProfileRepository.Save` nay ghi đúng envelope `profile/data` như nguồn.
- `ProfileRepository.Load` ưu tiên envelope nguồn và vẫn đọc flat schema do các bản Unity trước đã tạo; profile người dùng hiện có không bị bỏ.
- Thêm integration regression dùng thư mục tạm và các repository/`SaveStore` thật, không dùng object memory chung giữa các lần khởi tạo service.
- Scenario mở Rank group 1, commit 5.000 điểm, persist robot `last_seen_unix`, tái tạo toàn bộ runtime với clock bị lùi và xác nhận rank không tua ngược.
- Sau expiry, để Rank Gift transaction dở dang rồi tái tạo GameState/Profile/Robot/Award/Rank: cold sweep cấp +1 frame, +2 Locate, +2 Hint đúng một lần; Rank fold về `NotOpened`, pool cũ bị xóa.
- Home mở kỳ 2 theo nhánh `previous_awarded`; pool mới có key khác và vẫn tồn tại sau lần restart kế tiếp. Restart lần hai không cấp lại reward.

## Kiểm chứng

- Unity compile/domain reload sạch.
- Unity EditMode Test Runner: **532 passed, 0 failed, 0 skipped, 0 inconclusive**, duration **116,077 giây**.
- Unity PlayMode Test Runner: **17 passed, 0 failed, 0 skipped, 0 inconclusive**, duration **259,325 giây**.
- Không thêm runtime log và không ghi vào save thật của người dùng trong test.

## Còn lại

- Hard-kill/process termination, clock manipulation dài hạn và nhiều chu kỳ trên thiết bị thật thuộc R17.
- Backend Data Sync/merge conflict phụ thuộc auth/API/online provider của R16.
