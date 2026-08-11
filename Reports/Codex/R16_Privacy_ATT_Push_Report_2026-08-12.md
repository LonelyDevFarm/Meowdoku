# R16 Privacy, ATT and Push Report — 2026-08-12

## Phạm vi

Port lát cắt product-platform từ nguồn Godot sang Unity, gồm Privacy/CMP, ATT, startup push permission, post-win push guide và local notification.

Nguồn đối chiếu chính:

- `scripts/module/ui/panel/launcher.gd`
- `scripts/common/att_guide_helper.gd`
- `scripts/common/unikit_manager.gd`
- `scripts/module/splash/view/privacy_dialog.gd`
- `scripts/module/splash/view/pre_att_guide_page.gd`
- `scripts/module/splash/view/pre_push_guide_page.gd`
- `scripts/module/splash/view/push_guide_flow.gd`
- `scripts/module/result/view/game_win_page.gd`
- bốn scene Privacy/Pre-ATT/V2/Pre-Push và ba A/B config `att_dlg_logic`, `push_permission`, `push_local_text`

## Kết quả

- Bổ sung đúng schema save nguồn cho push ask/guide count, popup count, last date và ATT-guide shown; service giữ cooldown 5 ngày cùng recent-three-day/session-win policy.
- `PrivacyPermissionRuntime` nằm dưới `App/Systems`, dùng `IPlatformPermissionProvider`; AppBootstrap, UIManager, Settings và Win page cùng nhận một serialized instance.
- Startup giữ thứ tự Privacy blocking → initialize ATT → startup push request → enable/remove/register local notifications.
- CMP/ATT giữ nhánh Android fire-and-return, iOS/new-user timeout 2 giây, A/B skip/normal/restyled, status `NotDetermined` và post-ATT delay 1 giây.
- Push guide giữ minimum level 20, threshold/cap/cooldown, delay Win 2,467 giây, `SystemAndSetting`/`Setting`, tracker result và persisted counter/date.
- Local notification giữ hai ID noon/evening, legacy 4+4 content, pool mới shuffle 100 lấy 5, local hour 12:00/20:00 và repeat 86.400.000 ms vô hạn.
- Unity đã sinh và registry bốn prefab `PrivacyDialog`, `PreAttGuidePage`, `PreAttGuidePageV2`, `PrePushGuidePage`; hierarchy nằm theo nhánh chức năng và không missing script.
- Runtime không thêm debug log. Offline provider hoàn tất callback an toàn nhưng không giả đồng ý consent hoặc giả SDK state.

## Lỗi composition đã bắt và sửa

Lượt full EditMode đầu tiên chạy 561 test: 558 test ngoài phạm vi pass, ba composition test mới fail vì bốn `MonoBehaviour` được đặt chung trong `PlatformGuidePresenters.cs`. Unity compile được nhưng prefab serialize `m_Script: {fileID: 0}`.

Đã tách `PrivacyDialogPresenter`, `PreAttGuidePresenter`, `PrePushGuidePresenter` và `PushGuidePopupAnimator` sang file trùng tên class, giữ `.meta` riêng. Hai prefab cũ có duplicate `closeButton` sau migration được rebuild qua Prefab API thành base `closeButton` + `guideCloseButton`; không sửa YAML bằng suy đoán.

Refresh bridge nay settle generated platform prefab, registry và AppScene trước `AssetDatabase.Refresh`. EditMode/PlayMode bridge có event hậu tố `.Platform` để chạy đúng fixture liên quan thay vì lặp toàn bộ suite.

## Kiểm thử cuối

- Compile: Unity Tundra build thành công, không có C# error.
- Targeted EditMode: **17 passed, 0 failed**, 7,365 giây.
- Targeted PlayMode: **3 passed, 0 failed**, 34,639 giây.
- PlayMode thật bao phủ:
  - Privacy Accept trước startup push và daily notification registration.
  - Pre-ATT Continue trước system ATT request.
  - Post-win Pre-Push Allow, request type/position và persisted counters.
- Theo chiến lược test theo phạm vi, 558 test đã pass ở lượt full không bị chạy lại sau migration; chỉ nhóm chịu ảnh hưởng được rerun.

Kết quả máy đọc:

- `Temp/MeowdokuPlatformEditModeTestResult.xml`
- `Temp/MeowdokuPlatformPlayModeTestResult.xml`

## Chuyển thể Unity

- Godot UniKit singleton/signal được thay bằng provider interface và scene-owned coordinator; policy/order vẫn ở core game, native adapter chỉ sở hữu OS/SDK call.
- Godot local notification plugin được mô hình hóa thành `DailyLocalNotification`; provider native sẽ chịu trách nhiệm persistence/schedule trên iOS/Android.
- Godot popup scene trở thành UGUI prefab có serialized presenter/animator; UI registry giữ cùng route/lifecycle thay vì runtime lookup.

## Còn lại

- Native iOS/Android adapter cho Privacy/CMP, ATT status/dialog, push permission và notification scheduler.
- Kiểm thử callback khi app mất/lấy focus quanh OS dialog trên thiết bị thật.
- Xác nhận notification persistence/timezone/DST/app-kill.
- Pixel/video parity của bốn popup tại 1080×1920 và 1080×2400.
