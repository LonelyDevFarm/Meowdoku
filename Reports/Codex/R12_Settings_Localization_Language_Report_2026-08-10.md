# R12 Settings, Localization và Language Report — 2026-08-10

## Phạm vi

Lượt này hoàn thiện nhánh R12 theo thứ tự phụ thuộc:

1. Settings presenter/prefab và phản hồi toggle.
2. `GenericPopup.res` và `toast.gd` adapter dùng chung.
3. Bảng dịch/locale runtime từ nguồn Godot.
4. Language popup và language dropdown trong Settings.
5. Registry asset installer chỉ đăng ký page đã có thật.

Không port giả How-to-play, Bank, Daily, Streak hay Rank.

## Nguồn Godot đã đối chiếu trực tiếp

- `scripts/module/setting/view/setting_page.gd`
- `scripts/module/setting/ui/setting_page.tscn`
- `scripts/module/setting/view/language_switch_widget.gd`
- `assets/prefab/language_switch_widget.tscn`
- `scripts/module/language/language_manager.gd`
- `scripts/module/language/view/language_page.gd`
- `scripts/module/language/view/language_option.gd`
- `scripts/module/language/ui/language_page.tscn`
- `scripts/module/language/ui/language_option.tscn`
- `scripts/module/ui/common/toast.gd`
- `assets/animation/GenericPopup.res`
- `assets/localization/translations.csv`

## Kết quả triển khai

### Settings

- `SettingsPagePresenter` áp dụng đúng outgame/game-mode contract đã port ở lượt trước.
- Music ẩn theo default; Sound, Vibration và People hiện.
- Toggle cập nhật state, icon, ON/OFF panel và toast ngay lập tức.
- Sound bật phát preview; vibration bật gọi boundary được inject hoặc mobile fallback.
- Restart idempotent; Terms/Privacy dùng URL nguồn; feedback offline hiện `NETWORK_ERROR`.
- Pattern mode/dismissed dot giữ đúng persistence và callback nguồn.
- How-to-play giữ skip-close/suppress-close-callback contract, nhưng route chỉ hoạt động khi page thật được đăng ký ở lượt tiếp theo.
- Cây prefab gom theo nhánh `Root/Content/PanelContainer/VBoxContainer`, mỗi toggle và action tự quản lý con của mình.

### Animation và toast

- Tách `GenericPopupAnimator` dùng chung thay vì lặp tween trong từng presenter.
- Khóa đúng các mốc nguồn:
  - marker `0.3 s`
  - tổng `0.6192876 s`
  - open overshoot `0.09963459 s`
  - open fade `0.05483741 s`
  - close overshoot `0.1492851 s`
  - close fade start tương đối `0.2666667 s`
- `SourceToastView` giữ 870 px, Y 750, float 50 px, nhịp `0.15 + 1.2 + 0.2 s`, move `1.55 s` OutQuad và replace toast hiện hành.

### Localization

- Copy nguyên trạng `translations.csv`; SHA-256 nguồn và Unity cùng là `BFC5F71E72BCED300DCBCAE21511854DD13E4D607AAAB93CC95F7535CDC86573`.
- Parser hỗ trợ dấu phẩy, escaped quote và newline trong quoted field.
- Smoke parse trực tiếp file thật cho kết quả:
  - 76 cột locale
  - 1.695 record
  - 1.645 key duy nhất
  - `SETTING_TITLE` tiếng Việt: `Cài Đặt`
  - `SETTING_SOUND_ON` tiếng Việt: `Đã bật âm`
  - `GAME_LEVEL_TITLE` tiếng Việt: `Màn %d`
- Runtime chỉ giữ dictionary của locale đang dùng; đổi ngôn ngữ mới parse lại bảng, không giữ hơn 70 locale trong dictionary cùng lúc.
- Port đầy đủ supported language, alias `tl→fil`, `in→id`, `iw→he`, `no→nb`, fallback `en` và canonical `zh_CN/zh_TW`.
- `LocalizedText` refresh theo event, hỗ trợ `%s`/`%d` nguồn và chọn NotoSourceHan cho zh/ja/ko trong giới hạn legacy UGUI.
- Home level và Settings static/dynamic text đã dùng catalog chung.

### Language UI

- `LanguageSelectionContract` giữ đúng chín option cơ sở và đưa system locale lên đầu; locale ngoài danh sách/Chinese tạo option thứ mười.
- Exact-then-main selection và scroll-tap tolerance 6 px khớp nguồn.
- `LanguagePage.prefab` có đúng 10 option tái sử dụng, không instantiate khi scroll.
- Language dropdown giữ hai lựa chọn System/English, canonical Chinese, outside-close blocker và animation 0,1 giây/508 px theo source resource.
- Chọn locale cập nhật catalog, `GameState.applied_locale`, refresh text rồi đóng page tương ứng.

### Asset sinh bởi Unity

- `Assets/_Project/Settings/LocalizationCatalog.asset`
- `Assets/_Project/Prefabs/UI/SettingsPage.prefab`
- `Assets/_Project/Prefabs/UI/LanguagePage.prefab`
- `HomePage.prefab` được nâng cấp bằng serialized catalog và dynamic level binding.
- `UIRegistryAssetInstaller` chỉ đăng ký Home, Tutorial, Setting và Language khi prefab thật tồn tại; Unity đã sinh `Assets/_Project/Settings/UIRegistry.asset` với đúng bốn entry này.

Kiểm tra YAML read-only trên prefab do Unity sinh:

- 0 missing script ở Home/Settings/Language.
- Settings có `GenericPopupAnimator`, `LanguageSwitchWidget`, outside blocker và 11 `LocalizedText`.
- Language có 10 `LanguageOptionView`.
- Home có một `LocalizedText` cho level và serialized catalog reference.

## Kiểm chứng

- `Meowdoku.Core`: compile sạch bằng Unity Roslyn response file mới.
- `Meowdoku.Gameplay`: compile sạch.
- `Meowdoku.Editor`: compile sạch sau khi sửa nhánh return null của installer.
- `Meowdoku.EditModeTests`: compile sạch, gồm fixture locale/CSV/order/placeholder/dropdown/registry mới.
- Parser thật đã chạy ngoài Unity trên chính implementation `LocalizationCsvReader`, không chỉ kiểm tra bằng parser khác.

## Còn chờ

- Unity Test Runner chưa được chạy trong lượt không điều khiển được GUI Editor.
- PlayMode cần xác nhận Settings open/close, từng toggle, Language popup, dropdown outside-click, locale persist sau restart và font ở thiết bị.
- Scene composition còn cần gắn registry vào `UIManager`; Splash/Game/HTP/Bank chưa được thêm giả.
- Pixel/video parity ở 1080×1920 và aspect dài thuộc phần cuối R12/R17.
