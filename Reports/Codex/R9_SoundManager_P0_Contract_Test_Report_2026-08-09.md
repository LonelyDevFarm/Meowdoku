# R9 SoundManager P0 Contract Test Report

- Date: 2026-08-09
- Unity: 6000.3.19f1
- Result: **244 passed, 0 failed**

## Scope

- Tạo assembly `Meowdoku.Services` và port `SoundManager.Kind` theo đúng thứ tự 29 giá trị 0–28.
- Port bảng 27 fixed SFX path, polyphony, SFX/people setting gates, silent flag, stop, dynamic combo voice/meow cache và meow delay theo độ dài `MARK_CAT`.
- Unity voice pool cắt voice cũ nhất khi vượt giới hạn như `AudioStreamPlayer.max_polyphony` của Godot.
- Tạo `SoundCatalog` dùng serialized `AudioClip`, không dùng `Resources.Load` trong playback path.
- Port trạng thái BGM start/dialog/ad/duck/refresh nhưng giữ `_should_play_bgm() == false` đúng nguồn.
- Port `mark_sound` config bị thiếu khỏi default profile; profile tăng từ 26 lên 27 config, 23 config được source đăng ký.
- Nối cell-change sound contract cho CAT/ERROR/MARK/UNMARK và Hint request khi `GameplayManager` được gán `SoundService`.
- Clear phát một `UNMARK_X` sau khi im lặng toàn bộ cell changes; AutoComplete audio vẫn được hoãn vì sequence/timing nguồn chưa được port.

## Source corrections

`GEM-R9-001` được dùng để dò đường rồi kiểm tra toàn bộ `sound_manager.gd`, config và call sites:

- Enum có 29 kind, từ 0 đến 28.
- `MARK_WRONG_LOW` và `LEVEL_FAIL_LOW` tồn tại trong enum nhưng không có trong `_SOUND_PATHS`; gọi hai kind này là no-op và Unity giữ nguyên.
- Fixed `COMBO_VOICE` có polyphony 2; player combo voice tạo động theo path không đặt `max_polyphony`, nên giữ mặc định 1.
- Vượt fixed polyphony phải cắt voice cũ nhất, không phải bỏ âm mới hoặc chọn tùy ý.
- BGM không chỉ “mặc định tắt”: source hard-code `_should_play_bgm()` trả `false` và còn để path rỗng, nên không được tự phục hồi nhạc ở bản port.

## Asset audit

- 27/27 fixed clip được mapping đều tồn tại dưới `Assets/_Project/Audio/sfx`.
- Tổng cộng có 66 file OGG Unity, gồm meow và combo voice variants cho serialized path catalog sau này.
- Không chuyển đổi định dạng: Unity 6 import trực tiếp OGG hiện có.

## Remaining boundary

- `SoundCatalog.asset`, bootstrap `SoundService` và serialized scene reference chưa được tạo vì UI/service bootstrap nguồn tương đương thuộc R10. Do đó bước này khóa code/contract và call sites nhưng chưa tạo thay đổi âm thanh nghe được trong scene hiện tại.
- Dynamic meow path còn phụ thuộc cat visual/presenter chọn đúng clip.
- Board-enter, all-cleared, result, clap/trumpet và combo voice sẽ được nối tại đúng presenter/call site tương ứng, không phát sớm từ domain.
- AutoComplete cần sequence bất đồng bộ nguồn trước khi bật audio; bulk changes hiện được cố ý suppress để tránh phát ồ ạt sai timing.
