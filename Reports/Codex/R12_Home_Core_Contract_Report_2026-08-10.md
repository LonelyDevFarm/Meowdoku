# R12 Home core contract — 2026-08-10

## Kết quả

Đã bắt đầu R12 bằng phần có thể chuyển nguyên trạng từ Godot: ba cấu hình A/B mà Home đọc, trạng thái trình bày level/hard và các mốc timing của animation `MainInterface`. Chưa dựng Home prefab hoặc nút Daily/Settings/Profile khi các page/entry phụ thuộc chưa được port đầy đủ.

`GEM-R12-017` chưa có file báo cáo khi phần code bắt đầu và xuất hiện ở bước kiểm tra cuối. Báo cáo hữu ích để xác nhận đường dẫn, route, cleanup và asset chính, nhưng thiếu các offset/size/timing chi tiết và dependency Daily/Streak đã yêu cầu. Vì vậy phần đã port vẫn được kiểm chứng trực tiếp từ mã nguồn Godot; báo cáo không được dùng để suy đoán hoặc dựng UI còn thiếu.

## Phần đã port

- `DailyStreakConfig`: key `daily_streak`, default Basic (1), timing AppStart và toàn bộ policy hiện có trong source.
- `LeaderboardFuncConfig`: key `leaderboard_func`, default Control (0), Profile mặc định ẩn.
- `HardButtonConfig`: key `hard_button`, default variant 0 và ánh xạ nguyên giá trị 0–4.
- `HomePresentationState`: current level, hard-level, Daily/Profile visibility và hard effect variant.
- `HomePageContract`: reference width 1080, Start 750×160, animation duration/marker, Home→Game show/hide offset và reward restore delay 0,4 giây.
- `DefaultConfigProfile`: từ 32 lên 35 config, registered-by-source từ 28 lên 31.

## Điểm parity được giữ nguyên

- `DailyStreakConfig.ValueNoReward` hiện vẫn trả `HasReward() == true` vì Godot chỉ kiểm tra khác Control.
- `IsChallengeOnly()` và `IsSkipLit()` hiện luôn false trong source dù có variant mang tên tương ứng.
- Home offline mặc định hiện Daily Streak, ẩn Profile, dùng hard-button variant 0.
- English fallback của `GAME_LEVEL_TITLE` là `Level {n}`; presenter sau này phải lấy localization service thay vì hard-code chuỗi này cho mọi locale.

## Kiểm tra

- `Meowdoku.Core`: compile sạch bằng Unity Roslyn/reference response file, có chèn hai source mới chưa được AssetDatabase refresh.
- `Meowdoku.EditModeTests`: compile sạch, gồm test config mới và `HomePageContractTests`.
- Unity Test Runner/PlayMode: chưa chạy; người dùng sẽ kiểm tra gộp ở lượt sau.

## Chưa làm trong báo cáo này

- Home presenter/prefab và animation visual.
- Logo Spine adapter.
- Daily/Streak/Rank entry thực tế.
- Settings, localization, How-to-play và Bank page.
- Startup registry nối đầy đủ Splash/Tutorial/Home/Game.
