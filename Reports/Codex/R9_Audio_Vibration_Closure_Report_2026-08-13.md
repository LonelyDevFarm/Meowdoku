# R9 — Audio và vibration contract closure

Ngày: 2026-08-13  
Unity: 6000.3.19f1

## Kết quả

- Đối chiếu trực tiếp toàn bộ `sound_manager.gd` và `vibrate_manager.gd`, không dựa vào báo cáo cũ.
- Giữ đúng 29 `SoundKind`, 27 fixed path, 39 dynamic combo/meow path và hai Kind không có path là no-op.
- Xác minh `SoundCatalog.asset` chứa đủ clip thật; playback không dùng `Resources.Load` hoặc lookup asset trong hot path.
- Sửa BGM duck bị kẹt: mỗi duck SFX nay có generation guard và tự nhả theo độ dài clip; disable hủy coroutine/reset state. BGM vẫn hard-off/path rỗng đúng nguồn.
- Đưa `vibrate_combo`, `combo_voice`, `meow_feedback`, `thumb_up` vào shared `GameplayConfigSet`, đúng AppStart/GameStart timing. `GameplayFeedbackPresenter` không còn giữ ComboVoiceConfig cục bộ đứng ngoài runtime.
- Port `VibrationLevel`, low/high-RAM duration/amplitude, enable/cancel sink và Android `VibrationEffect`. Editor/desktop/thiết bị không có vibrator no-op an toàn; iOS dùng `Handheld.Vibrate` vì Unity không cung cấp native haptic-level API tương đương plugin Godot.
- Nối mức rung nguồn vào tap/swipe, correct cat/combo, wrong guess và hint xóa MARK. Nối meow-by-path với prefill/final-cat guard; default `meow_feedback=0` vẫn im lặng.
- Không thêm log runtime.

## Kiểm tra Unity

- Batch compile: thành công; chỉ còn warning cũ `FakeAuthProvider.LoginError` không được dùng.
- `SoundContractTests`: **17/17 passed**.
- `VibrationContractTests`: **9/9 passed**.
- `GameplayConfigSet_ReloadsSharedValuesAtSourceTimings`: **1/1 passed**.
- `SoundServicePlayModeTests`: **1/1 passed**; khóa pool size, silent/sound gate, dialog, banner/non-banner ad và duck release.

## Ranh giới còn lại

- Nghe trực tiếp toàn bộ clip, cảm nhận cường độ rung trên Android/iOS thật và audio/video timing comparison vẫn thuộc device QA R17.
- iOS parity cường độ chính xác cần native haptic adapter riêng; fallback hiện tại có chủ đích và không ảnh hưởng gameplay logic.
- BGM không được tự sáng tạo hay gắn nhạc thay thế vì source hard-code `_should_play_bgm() == false` và path rỗng.
