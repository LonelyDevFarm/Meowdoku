# R12 Settings core state và config — 2026-08-10

## Kết quả

Đã port lớp dữ liệu bắt buộc trước khi dựng Settings presenter. Bản Unity hiện phân biệt đúng Settings mở từ Home và Settings mở trong Game, đồng thời lưu được pattern mode và trạng thái red-dot theo đúng key Godot.

`GEM-R12-018` giúp xác nhận route, callback và việc `HomeSettingAnchor` thực tế thuộc Streak page. Báo cáo không liệt kê đầy đủ field pattern, config default, offset và layout condition nên các phần này được kiểm chứng trực tiếp từ source.

## Config đã port

- `SettingsLanguageConfig`: Hide 0, Popup 1, Dropdown 2; default Hide; timing `open_setting`.
- `BlindModConfig`: Control 0, HideOnFilled 1, KeepOnFilled 2; default Control; timing `game_start`.
- `RuleTextConfig`: bổ sung đủ predicate source, gồm `IsSettingEntry()`.
- `DefaultConfigProfile`: 37 config tổng, 33 config được source đăng ký.

## Persistence đã port

- `pattern_mode_on`, mặc định false.
- `pattern_entry_dot_dismissed`, mặc định false.
- `pattern_switch_dot_dismissed`, mặc định false.
- Setter pattern lưu ngay; hai hàm dismiss idempotent và không ghi save lặp.

Không chuyển các dòng `PATTERN_DBG` từ Godot vì đó là debug log rác, không phải hành vi game.

## Layout contract mặc định

### Outgame/Home

- Music ẩn; Sound, Vibration và People hiện.
- Ba toggle dùng separation 30, scale 1.
- Language ẩn vì `settings_language=0`.
- Pattern, Restart và How-to-play ẩn.
- Feedback, Terms/Privacy và Version hiện; CMP chỉ hiện khi platform boundary yêu cầu.

### Game mode

- Music vẫn ẩn; ba toggle còn lại hiện.
- Restart hiện; Terms/Privacy/Version ẩn.
- Pattern chỉ hiện khi `blind_mod!=0`.
- How-to-play chỉ hiện khi `rule_text=6`.

## Kiểm tra

- Core compile sạch bằng Unity Roslyn, gồm config/contract mới chưa có trong response file tại thời điểm kiểm tra.
- EditMode test assembly compile sạch.
- Fixture mới bao phủ defaults, popup/dropdown English suppression, game variants, persistence round-trip và dismissed-dot idempotence.
- Unity Test Runner chưa chạy; presenter/prefab/visual chưa được đánh dấu hoàn thành.

## Tiếp theo

Dựng `SettingsPagePresenter` và prefab theo cây nguồn; gắn toggle sprite thật, SoundService, vibration boundary, close animation và typed callback. Language/How-to-play chỉ mở khi page thật được port/registered.
