# R16 Auth/API Provider Boundary Report — 2026-08-11

## Phạm vi

Port lát cắt Auth/API từ nguồn Godot sang lõi Unity provider-neutral, nối lifecycle vào `AppScene`, giữ bản offline không phụ thuộc SDK native và khóa hành vi bằng regression.

Nguồn đối chiếu chính:

- `scripts/common/auth_manager.gd`
- `scripts/core/net/api_config.gd`
- thứ tự autoload/startup liên quan Auth và Data Sync

## Kết quả

- Thêm `ApiConfig` với app ID, dev/prod endpoint, sign secret, sync/account path, response code và platform mapping đúng nguồn.
- Thêm `AuthService` giữ nguyên gate Analytics + LUID, init payload, guest login, JSON callback boundary, profile/device query và đầy đủ error code.
- Token request dùng clock monotonic, hỗ trợ force-refresh và hết hạn đúng 12.000 ms.
- Login-expired phát signal trước auto relogin; relogin được debounce 60.000 ms, giới hạn 5 lần liên tiếp và reset khi login thành công.
- Thêm `AuthRuntime` scene-owned, cleanup event trong `Dispose`; `AppRuntimeSceneInstaller` đặt component dưới `App/Systems` bằng Unity serialization.
- Khi chưa có native provider, startup offline tiếp tục bình thường và không phát log runtime rác.
- Sửa `UnityRefreshBridge`: request tới trong lúc compile không còn làm `_refreshPending` kẹt vĩnh viễn; Editor polling thực hiện refresh ngay khi compiler/import rảnh.

## Kiểm thử

- Unity compile: `Meowdoku.Core`, `Meowdoku.Gameplay`, `Meowdoku.Editor`, `Meowdoku.EditModeTests` và `Meowdoku.PlayModeTests` đều được tái biên dịch sạch.
- EditMode: **537 passed, 0 failed, 0 skipped, 0 inconclusive**, 112,575 giây.
- PlayMode: **17 passed, 0 failed, 0 skipped, 0 inconclusive**, 250,814 giây.
- Regression mới bao phủ exact API/payload, prerequisite gate/start-once, token callback/timeout, relogin debounce/cap/reset và missing-provider degradation.

## Chuyển thể Unity

Godot dùng `Engine.get_singleton("AuthPlugin")` và signal động. Unity dùng `IAuthProvider` cùng `IAuthPrerequisiteProvider`; JSON và policy vẫn ở ranh giới tương đương, còn lifecycle do `AuthRuntime` sở hữu. Đây là adaptation bắt buộc, không thay luật nguồn.

## Còn lại

- Native iOS/Android adapter cho AuthPlugin/UniKit Analytics, LUID và device identity.
- Data Sync, merge conflict, access-token retry và startup timeout.
- Callback/network/device parity trên thiết bị thật.

