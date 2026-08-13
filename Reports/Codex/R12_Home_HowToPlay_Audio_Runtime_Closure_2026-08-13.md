# R12 Home, How-to-play và Global Audio Runtime — 2026-08-13

## Kết quả

Đã khép `P-HOME-002`, `P-HOME-006`, `P-SET-009` và `P-SET-011` bằng AppScene PlayMode. Home cập nhật level/locale đúng lúc đang mở và khi reopen; hai How-to-play chạy demo thật rồi cleanup/reopen mà không giữ cell, coroutine, tween hoặc silence cũ.

Unity 6000.3.19f1 đạt:

- Full EditMode: **678/678**, 0 fail, 0 skip, 142,741 giây.
- Platform PlayMode: **16/16**, 0 fail, 0 skip, 156,914 giây.
- Refresh/installer idempotent: không có `error CS`, missing script hoặc installer exception mới.

## Gap production phát hiện

Godot dùng một `SoundManager` autoload cho toàn app. Unity trước lượt này chỉ có `SoundService` nằm trong GamePage prefab, trong khi `HomePagePresenter`, `SettingsPagePresenter`, `HowToPlayPagePresenter` và `HowToPlayPagedPagePresenter` đều serialize `soundService: null`. Vì vậy Home không thực sự gọi SoundManager, Settings không phát preview/dialog và HTP không thực sự bật silence dù code nhìn có vẻ đầy đủ.

Đã sửa bằng composition nguồn:

```text
App
└─ Systems
   └─ Audio
      ├─ SoundRuntime
      ├─ SoundService
      └─ Bgm (AudioSource)
```

- `SoundRuntime` nghe `UIManager.Events.WindowCreated` và bind một service cho mọi `ISoundServiceConsumer`.
- Home, Settings, Full HTP, Paged HTP và GamePage đều là consumer.
- GamePage forward service tới `GameplayManager` và `GameplayFeedbackPresenter`.
- Installer loại SoundService/voice pool nhúng khỏi GamePage prefab; EditMode gate bắt buộc AppScene có đúng một runtime/service và GamePage có 0 service.
- BGM vẫn hard-off/path rỗng đúng source; thay đổi này không tự tạo nhạc nền.

## Bằng chứng Home/HTP

- Home: level text 1→7 khi state đổi, refresh ngay khi locale đổi sang `vi`, hide/set level 9/reopen cùng instance hiển thị đúng; popup queue kết thúc và số presenter không tăng.
- Full HTP: ba board 3×5, demo đi qua start delay, `Silent=true` khi show; hide trả toàn bộ cell EMPTY, dừng coroutine, `Silent=false`; ba vòng reopen giữ nguyên cell population.
- Paged HTP: show bắt đầu page 0, chuyển page 1, hide cleanup; reopen cùng instance về page 0, demo chạy lại, cell population không đổi và silence được trả.
- 15 Platform flow cũ vẫn xanh sau migration, gồm Tutorial, input, level selection, rule highlight, game/result, toolbar và permission startup.

## Phần còn mở

- `P-SET-010` vẫn `[~]` vì VFX/pixel parity và soak/device dài chưa được chứng minh, dù navigation/reopen logic đã đạt.
- Home/Settings/HTP pixel/animation ở 1080×1920/2400 và nghe/cảm nhận audio trên thiết bị thuộc R17.
- Bảng phần trăm tổng quan giữ nguyên vì đây là closure chi tiết trong khoảng UI/audio hiện tại.
