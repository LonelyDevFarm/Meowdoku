# R12 — Settings toggle/mode-layout closure

Ngày kiểm tra: 2026-08-13  
Unity: 6000.3.19f1

## Phạm vi nguồn

- `scripts/module/setting/view/setting_page.gd`
- `scripts/module/setting/ui/setting_page.tscn`
- `settings_language_config.gd`, `blind_mod_config.gd`, `rule_text_config.gd`

## Matrix runtime

- Mở Settings từ Home, đóng/mở lại, vào Game rồi mở/đóng lại hai lần trên cùng cached presenter.
- Xác nhận Music luôn ẩn; Sound, Vibration và People luôn hiện, spacing 30.
- Bấm ba toggle qua pointer down/up thật; model, ToggleOn/ToggleOff và icon đổi đồng bộ, giữ nguyên qua reopen và khi chuyển sang Game mode.
- Home mode: Language/Feedback/Terms/Version hiện theo config; Pattern/HTP/Restart/CMP ẩn; spacer 50/30.
- Game mode: Language/CMP/Terms/Version ẩn; Pattern/HTP/Restart hiện; spacer 0/90.
- Pattern red-dot hiện khi tutorial đã xong và chưa dismiss; click bật pattern, dismiss dot bền vững, cập nhật Board ngay và giữ đúng khi reopen.
- Panel được rebuild và giữ tâm ở cả hai mode; hierarchy theo nhánh chức năng không bị nhân đôi.

## Kết quả

- Không phát hiện sai lệch production; không cần sửa runtime.
- Platform PlayMode: **23 passed, 0 failed, 0 skipped**, thời lượng **356,896 giây**.
- Full EditMode ổn định gần nhất: **679/679**; không chạy lại vì thay đổi chỉ là PlayMode fixture và Unity compile sạch.
- Không thêm runtime log, không commit và không ghi save thật.

## Trạng thái

- `P-HOME-004`: hoàn thành.
- `P-SET-001`: hoàn thành về runtime; cảm nhận native vẫn thuộc Audio/Device QA.
