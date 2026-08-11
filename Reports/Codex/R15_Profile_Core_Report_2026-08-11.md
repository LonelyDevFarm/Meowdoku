# R15 — Profile core và Award frame boundary

Ngày: 2026-08-11  
Unity: 6000.3.19f1

## Đã port từ source

- `AvatarFrame`, `PlayerInfo`, `ProfileData`, `ProfileCatalog` với đúng key serialization, avatar/frame catalog và ngưỡng acquired frame 100.
- `ProfileNickname` sinh tên mặc định 6 ký tự từ alphabet `A-Z0-9`; nickname người dùng được trim và giới hạn 12 code point, không cắt vỡ surrogate pair.
- `ProfileService`: initialize idempotent, avatar/frame validation, unlock/equip, acquired count, red-dot, identity-customized, reset, player info và event tương ứng source.
- Remote contract `profile`: nickname UTF-8 base64 có prefix `b64:`, chỉ export/replace acquired frame id ≥100 và chỉ overwrite khi remote ahead.
- `ProfileRepository` giữ document/section/key source-shaped trong `profile.cfg`, dùng `SaveStore` atomic đã kiểm chứng của Unity project.
- `ProfileRuntime` là scene-owned composition boundary, được installer gắn trong `App/Systems` và serialize vào `DailyMetaRuntime.frameAwardSink`; Award frame vì vậy đi vào inventory thật thay vì null sink.
- Port `ProfileAvatarView`, `ProfileSelectionCell` và `ProfilePagePresenter`: pending avatar/frame không ghi trước Confirm, Close hủy thay đổi, rename select-all, Unicode 12 code point, tab Frame clear red-dot, locked frame shake 0,4 giây và tooltip tự đóng sau 3 giây. Nút Rank trong tooltip giữ ẩn tới khi RankActivityManager thật tồn tại.
- `ProfilePagePrefabInstaller` dựng ba prefab lồng từ sprite nguồn, giữ panel 900×1253, avatar/cell 185, grid bốn cột gap 6, group Leaderboard/Classic, GenericPopup, localization và route `UiName.Profile`; không sửa YAML.

## Kiểm chứng

- Core, Gameplay, Editor và EditModeTests compile sạch bằng Unity Roslyn.
- Reflection regression tổng đạt 31/31; 6 case Profile bao phủ initialize, Unicode nickname, validation, frame count/red-dot, remote merge, PlayerInfo round-trip và AwardManager → ProfileService.

## Còn lại

- Unity cần Refresh lần đầu để import script mới, chạy AppScene installer và tạo `.meta`.
- Unity Refresh để thực thi installer, sinh ba prefab/registry reference và kiểm tra serialized tree/missing script.
- PlayMode Profile: rename/IME, pending Close/Confirm, tab/red-dot, scroll-vs-tap, locked tooltip và hai aspect 1080×1920/2400.
- PlayMode restart, frame award cold-start và remote/backend sync; backend thuộc R16.
