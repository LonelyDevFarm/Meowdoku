# R12 Locale Cold Start — 2026-08-13

## Kết quả

Đã khép phần cold restart còn thiếu của `P-SET-007` bằng AppScene PlayMode thật.

- Platform PlayMode: **18/18**, 0 fail, 0 skip, 166,951 giây.
- Không sửa production; chỉ bổ sung fixture tại đúng ranh giới scene startup.
- Full EditMode ổn định gần nhất vẫn là **678/678** và không chạy lại vì không có production/domain change liên quan.

## Đối chiếu nguồn

`language_manager.gd::apply_system_locale` chỉ dùng `GameState.apply_locale` khi `settings_language` đang bật; ngược lại dùng locale hệ thống. Fixture giữ nguyên điều kiện này:

1. Tạo state trước scene với `apply_locale=vi`.
2. Gắn provider `settings_language=Popup` qua `SceneManager.sceneLoaded`, tức sau `AbConfigRuntime.Awake` nhưng trước `AppBootstrap.Start`.
3. Chờ startup hoàn tất rồi kiểm tra cùng catalog được Home và Settings dùng.

Kết quả catalog có `Locale=vi`, `TranslationColumn=vi`, Home hiện `Màn 1` và Language entry hoạt động ngay lần mở đầu. Persistence file của `AppliedLocale` đã được fixture repository hiện hữu khóa riêng.

## Còn mở

`P-SET-007` vẫn `[~]` vì glyph/font zh/ja/ko và các locale dài cần kiểm tra trên thiết bị thật. Popup/dropdown, persistence, refresh trong phiên và cold startup đã có bằng chứng tự động.
