# R17 Mobile Lifecycle Audit Report — 2026-08-11

## Phạm vi

- Audit gameplay/UI runtime theo tiêu chí mobile: per-frame loop, pointer hot path, component lookup, cấp phát hình học, event/tween/coroutine cleanup và ownership pool.
- Không đổi luật, timing, coordinate hay presentation contract của nguồn Godot.
- Không xóa prototype/scene legacy khi Build Settings và đường test chưa được khóa.

## Thay đổi

- `BoardView` cache Canvas của hierarchy và tự làm mới khi đổi parent.
- `BoardView` cache `GridLayoutGroup` theo `cellsContainer`; nếu container được thay ở runtime, cache tự invalidation.
- Quy đổi pointer-to-cell không còn component lookup theo từng pointer move.
- Các API lấy cell/board bounds tái sử dụng một buffer bốn world-corner thay vì tạo mảng mới.
- `BoardGridOverlayGraphic` tái sử dụng path của rounded frame trong các lần rebuild intro; việc mesh dirty vẫn chỉ diễn ra trong animation nguồn.

## Kết quả audit lifecycle

- `BoardView` tháo Input System callback ở `OnDisable`; gesture đang chạy được hủy khi mất focus/disable.
- Cell và các feedback view chính đều kill tween khi disable/release; UI window sở hữu và dừng coroutine đã đăng ký.
- `PoolManager` singleton prototype không còn code consumer trong runtime chính, nhưng vẫn là component serialized của `LoadingScene` legacy. Giữ nguyên ở lát cắt này để tránh xóa nhầm test path.
- `SceneLoader` là adapter scene-per-page cũ; AppScene/UIManager là runtime chính. Việc loại file/scene cũ thuộc bước release cleanup sau khi khóa Build Settings.

## Xác minh

- Core: 91 source, compile sạch bằng Unity Roslyn.
- Gameplay: 86 source, compile sạch.
- Editor: 21 source, compile sạch.
- EditModeTests: 41 source, compile sạch.
- Regression runner: **90 passed, 0 failed**.
- Unity refresh bridge trả `REFRESH_SIGNAL_SENT`.
- Unity Editor: `Tundra build success`, sau đó `Begin MonoManager ReloadAssembly`; không có C# compile error trong phần log mới.

## Còn lại

- Chạy Unity Profiler trên thiết bị Android/iOS: CPU, GC/frame, GPU, batch/draw call, memory và startup.
- Soak test nhiều vòng game/restart/exit/resume và xác nhận pool không tăng vô hạn.
- Touch/multi-pointer, safe-area/notch và app pause/resume trên thiết bị thật.
- Chỉ xóa `PoolManager`, `SceneLoader` và scene prototype sau khi xác minh không còn trong Build Settings/test workflow.
