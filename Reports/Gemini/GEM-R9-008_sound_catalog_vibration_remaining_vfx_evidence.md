# Báo cáo bằng chứng: GEM-R9-008 (Sound Catalog, Vibration, R9 VFX)

**Nguồn đối chiếu:** `D:\Projects\_GameExtract\Main_Meokdoku`
**Mục tiêu:** Liệt kê các bằng chứng mã nguồn liên quan đến Sound Bootstrap, Vibration Manager, R9 VFX và sự phụ thuộc Plugin/Asset.

---

## 1. Khởi tạo SoundManager và Quản lý Audio Paths

- **Khởi tạo và Preload (Bootstrap):**
  - **File:** `scripts/module/sound/sound_manager.gd`
  - **Hàm khởi tạo:** `func _ready() -> void:`
  - **Logic:** Duyệt qua từ điển hằng số `_SOUND_PATHS` và tự động tạo các `AudioStreamPlayer` làm con (child node) tương ứng với từng enum `Kind`.
  ```gdscript
  func _ready() -> void :
      for kind in _SOUND_PATHS.keys():
          var path: String = _SOUND_PATHS[kind]
          # ... logic sinh node AudioStreamPlayer và nạp path
  ```
- **Dynamic Audio (Call site không nằm trong _SOUND_PATHS cố định):**
  - **Hàm 1:** `func play_combo_voice_by_path(path: String) -> void :`
  - **Hàm 2:** `func play_meow_by_path(path: String) -> void :`
  - **Điều kiện:** Nếu cờ `_silent == true`, lập tức return.
- **Polyphony:** Được định nghĩa tại hằng `const _POLYPHONY: Dictionary`. Ví dụ `Kind.MARK_X: 4` (cho phép phát đè tối đa 4 âm thanh X cùng lúc, nếu vượt quá sẽ bị chặn hoặc reuse tuỳ pool).

## 2. Ducking, Silent, và Dialog/Ad State
- **Duck BGM (Ép nhỏ BGM khi SFX quan trọng phát lên):**
  - **Hằng số:** `const _BGM_DUCK_KINDS: Array = [Kind.BOARD_ENTER, Kind.LEVEL_WIN]`
  - **Bằng chứng hàm:** `func play(kind: int) -> void :` gọi `_duck_bgm_during(player)` nếu kind thuộc danh sách duck.
  - **Hoạt động ducking:**
  ```gdscript
  func _duck_bgm_during(sfx_player: AudioStreamPlayer) -> void :
      _bgm_ducking = true
      _apply_bgm_playback()
      if not sfx_player.finished.is_connected(_on_duck_sfx_finished):
          sfx_player.finished.connect(_on_duck_sfx_finished, CONNECT_ONE_SHOT)
  ```
- **Silent & Pause:**
  - Logic dừng tạm thời (stream_paused):
  ```gdscript
  _bgm_player.stream_paused = _bgm_paused_for_dialog or _bgm_ducking or _bgm_paused_for_ad
  ```

## 3. VibrateManager (Quản lý Rung)
- **File:** `scripts/module/common/vibrate_manager.gd`
- **Các mốc cường độ (Levels):**
  ```gdscript
  enum Level{ LEVEL1, LEVEL2, LEVEL3, LEVEL4, LEVEL5, LEVEL6, LEVEL7, LEVEL10 }
  ```
- **Phân tách nền tảng (Platform Gates):**
  - **iOS:** Dùng map Haptic Engine của iOS (`_map_ios`), ví dụ `selectionChanged`, `feedbackMedium`, `notificationSuccess`.
  - **Android/Low-end:** Dùng tham số duration (d) và amplitude (a). Ví dụ: `_map_low` và `_map_high` (phân chia bằng hằng số `_RAM_4G_MB = 3800` để check ram máy).
  - Không có thiết bị hỗ trợ: Bỏ qua (không crash).

## 4. Các VFX R9 còn lại (Visual Effects)
Các Prefab Particle/Hiệu ứng phát sáng hoặc nổ (Burst):
- **Thu thập Cá (Fish Glow/Burst):**
  - **File Scene:** `assets/prefab/collect_fish_burst.tscn`, `assets/prefab/effect_collect_glow.tscn`
  - **Thành phần cốt lõi:** Sử dụng Node `CPUParticles2D`.
  - **Asset Dependency (Texture):** `res://assets/effect/texture/star/et_star_1.png` và `res://assets/effect/texture/mask/et_mask_016.png`.
  - **CanvasItemMaterial:** Sử dụng Blend Mode `Add` hoặc `Mix` (được ghi nhận qua node `GlowAdd` và `GlowAlp`).
- **Score Burst (Điểm số nổ):**
  - **File Scene:** `assets/prefab/effect_score_burst.tscn`.
- **Trail (Đuôi bay):**
  - **File Scene:** `assets/prefab/effect_score_trail.tscn`.
- **All-Cleared / Win / Fail / Board Enter:** Nằm chủ yếu qua AnimationPlayer của `board_view.gd` (như đã phân tích ở các báo cáo trước) và âm thanh từ SoundManager.

## 5. Mặc định Offline (Default) vs AB Variant
- Trong `cell.tscn`, animation nứt vỡ mặc định là `ErrorAppear`.
- Các nhánh A/B test (Ví dụ `IconCrashConfig.VALUE_NO_CRASH` hoặc `VALUE_FISH_CRASH`) sẽ trỏ tới các animation biến thể như `ErrorAppear1`, `ErrorAppear2`.
- **Kết luận:** File Godot lưu toàn bộ Variant vào trong cùng một Scene `.tscn` thông qua nhiều Track Animation.

## 6. Yêu cầu Runtime/Plugin (Copy Asset sang Unity)
**Những Asset Unity CÓ THỂ COPY trực tiếp 1:1:**
- File âm thanh đuôi `.ogg` (`assets/audio/sfx/`).
- File texture PNG cho VFX (`assets/effect/texture/star/`).
- File UI Sprite (`assets/ui/`).
- File export Spine (gồm `.json` hoặc `.skel`, `.atlas`, `.png`) trong thư mục Spine gốc.

**Những Runtime bắt buộc phải có trên Unity để dịch mã (Plugin Dependencies):**
- **Spine-Unity Runtime:** Bắt buộc phải có để đọc các node `SpineSprite` và `SpineAnimationTrack` (vì Spine Godot dùng track nhúng thẳng).
- Tích hợp Haptic (như `NiceVibrations` hoặc tương đương) để map lại các string iOS Haptic (`selectionChanged`, `feedbackHeavy`) từ `VibrateManager`.

---
### Checklist Bàn Giao
1. [x] **Trích xuất logic SoundManager:** Hoàn thành (có bằng chứng Ducking).
2. [x] **Trích xuất VibrateManager:** Hoàn thành (enum và iOS/Android branch).
3. [x] **Khảo sát VFX:** CPUParticles2D và Blend Mode Add được ghi chú rõ ràng.
4. [x] **Xác nhận:** KHÔNG có bất kỳ thay đổi nào tác động lên code/scene Unity/Godot ngoài báo cáo này. Mọi thông tin 100% khách quan từ Godot files.
