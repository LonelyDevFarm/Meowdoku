# GEM-R9-001 Đặc tả Contract SoundManager / BGM / SFX

**STATUS**: COMPLETE  
**GENERATED_AT**: 2026-08-09 01:45:00  
**SOURCE_ROOT**: `D:\Projects\_GameExtract\Main_Meokdoku`

---

## 1. Cấu trúc Quản lý Âm thanh (SoundManager Contract)

Tài liệu này bóc tách `scripts/module/sound/sound_manager.gd` (đóng vai trò là Autoload Singleton).

### A. Enum và Mapping (Kind → Path)
Hệ thống sử dụng một bộ định danh tĩnh `Kind` (từ 0 đến 28) để gọi các SFX cơ bản.
Các tệp âm thanh (OGG) được load sẵn tại hàm `_ready()` qua mảng hằng số `_SOUND_PATHS`.
*Các loại âm thanh đáng chú ý:*
*   Gameplay: `MARK_X`, `UNMARK_X`, `MARK_CAT`, `MARK_WRONG`.
*   Tương tác UI: `BTN_CLICK`, `USE_HINT`, `DLG_OPEN`.
*   Sự kiện chính: `BOARD_ENTER`, `LEVEL_WIN`, `LEVEL_FAIL`, `ALL_CLEARED`.
*   Rank/Thưởng: Các hiệu ứng thu thập mèo, cá, hòm (`RANK_CAT_COLLECT`, `RANK_BOX_OPEN`...).

### B. Polyphony (Đa âm / Chồng lặp)
Godot hỗ trợ thuộc tính `max_polyphony` trên cùng một `AudioStreamPlayer`. Code gốc cấu hình sẵn mức polyphony ưu tiên tại `_POLYPHONY`:
*   **4 lớp (Cao nhất)**: `MARK_X`, `UNMARK_X`, `BTN_CLICK`, `MARK_X_SOFT_1/2`. (Các hành động spam liên tục).
*   **3 lớp**: `MARK_CAT`.
*   **2 lớp**: `MARK_WRONG`, `CLAP`, `BLOW_TRUMPET`, `COMBO`, `COMBO_VOICE`.
*   *Còn lại*: 1 lớp (Mặc định).

### C. Quản lý Động (Meow & Combo Voice)
Ngoài các âm thanh cơ bản load sẵn, hệ thống xử lý load động theo file path đối với tiếng mèo kêu và tiếng đọc Combo.
*   **`play_combo_voice_by_path(path)`**: Cấp phát player động, cache vào `_combo_voice_path_players` (polyphony=1).
*   **`play_meow_by_path(path)`**: Cache vào `_meow_path_players` (polyphony=2).
*   **Đặc tả Timing (Quan trọng)**: Tiếng Mèo kêu không phát ngay lập tức. Hàm `play_meow_by_path` sẽ `await` theo độ dài (seconds) của âm thanh `MARK_CAT` trước khi phát tiếng mèo (tránh bị đè âm thanh). Nguồn: `sound_manager.gd` dòng 198-204.

---

## 2. Quản lý BGM (Nhạc nền) và Ducking (Fade/Pause)

Mặc dù hệ thống có quản lý BGM (`_bgm_player`), tuy nhiên hàm `_should_play_bgm()` hiện đang **hardcode trả về `false`** (Dòng 311). Tức là nhạc nền trong dự án gốc đang bị **tắt hoàn toàn**. Nếu cần bật lại, logic của nó tuân theo nguyên tắc sau:

1. **Ducking (Nhường sóng)**:
    *   Mảng `_BGM_DUCK_KINDS = [Kind.BOARD_ENTER, Kind.LEVEL_WIN]`.
    *   Khi phát các SFX này, cờ `_bgm_ducking = true` được bật -> BGM bị gán `stream_paused = true`.
    *   Khi SFX kết thúc (callback `finished`), cờ tắt -> BGM tiếp tục.
2. **Ads Mute (Tắt khi xem Quảng cáo)**:
    *   Tự động lắng nghe `UniKitManager.ad_shown` và `UniKitManager.ad_closed` để bật/tắt cờ `_bgm_paused_for_ad`. Ngoại trừ quảng cáo "banner" thì không pause.
3. **Dialog Mute**:
    *   Bị điều khiển thủ công bởi hàm `set_bgm_paused(paused)` với cờ `_bgm_paused_for_dialog`. Thường được gọi khi bật popup (như ở màn `GameFailPage`).

---

## 3. Consumer và Check Trạng Thái
Mọi hàm `play` đều bắt buộc đi qua 2 lớp check:
*   `if _silent`: Check cờ silent cứng của app.
*   `if not GameState.is_sound_on()` (Cho SFX) hoặc `if not GameState.is_people_on()` (Riêng cho Combo Voice).

---

## 4. Unity Gaps (Sự khác biệt Kiến trúc cần xử lý)

| Domain | Godot (`sound_manager.gd`) | Unity (Đề xuất Port) | Phân loại |
| :--- | :--- | :--- | :--- |
| **Polyphony** | `AudioStreamPlayer.max_polyphony` tự động chồng tiếng. | Unity `AudioSource` mặc định ngắt tiếng cũ. Cần dùng `AudioSource.PlayOneShot()` hoặc dùng **AudioSource Object Pool**. | P0 (Bắt buộc) |
| **Assets Format** | `.ogg` | Đảm bảo chuyển đổi hoặc Import `.ogg` đúng nén (Vorbis) trên Unity. | P0 |
| **Dynamic Loading** | Dùng `ResourceLoader.load()` theo path string. | Chuyển thành `Resources.Load<AudioClip>` hoặc Addressables tùy kiến trúc Asset. | P1 |
| **Ducking/Pause** | Dùng biến boolean để chặn `stream_paused`. Đợi signal `finished` của SFX. | Unity có thể dùng Audio Mixer Snapshots để Ducking tự nhiên hơn, hoặc dùng Coroutine đo chiều dài Clip tương tự. | P2 |
| **BGM Status** | Bị hardcode tắt (`_should_play_bgm() -> false`) | Cân nhắc kiểm tra lại yêu cầu thiết kế xem có cần phục hồi BGM hay không. | P2 |

---

## 5. Fixture Kiểm Thử

| Fixture Tên | Input / Hành động | Kết quả mong đợi |
| :--- | :--- | :--- |
| **Polyphony Limit** | Gọi `play(Kind.MARK_X)` 5 lần cùng lúc. | Phát 4 âm đè lên nhau, âm thứ 5 bị bỏ qua hoặc đẩy âm cũ nhất ra (Do Polyphony=4). |
| **Meow Delay Timing** | Gọi `play_meow_by_path("meow.ogg")`. | Đợi chính xác thời lượng của `MARK_CAT` (khoảng 0.x giây) mới bắt đầu phát `meow.ogg`. Không phát nếu Sound Setting bị tắt giữa chừng. |
| **BGM Ad Pause** | Bắn sự kiện `ad_shown("reward")`. | Nhạc nền BGM bị pause (nếu BGM đang bật). |
| **BGM Ducking** | Gọi `play(Kind.LEVEL_WIN)`. | Nhạc nền BGM bị pause. Ngay khi âm thanh Win kết thúc, BGM tiếp tục phát. |
| **Settings Check** | Tắt `GameState.is_people_on()` và gọi `play_combo_voice_by_path`. | Không có âm thanh phát ra (Dù `is_sound_on` vẫn bật). |
