# R8/R9 ToolButton UI Closure — 2026-08-13

## Phạm vi

- Đối chiếu trực tiếp `tool_button.gd/.tscn`, `game_ad_badge.gd/.tscn`,
  `game_page.tscn` và Tool handlers trong `base_game_page.gd`.
- Đóng `P-GAME-007` và `P-GAME-013` ở phạm vi runtime offline của dự án.

## Kết quả

- Thêm `ToolButtonView` và `GameplayToolBarPresenter`.
- Dựng serialized hierarchy `HUD/BottomTools/{Locate,Hint}` cho cả
  `GameplayScene` và `GamePage.prefab`; Clear giữ ẩn như scene Main gốc.
- Port ba trạng thái NO_TOOL/HAS_TOOL/FREE, count `99+`, plus/Free badge,
  press/release scale, obtain hook và animation `tool_loop`/`RESET` bằng DOTween.
- Nối click tới Locate/Hint domain, tool-count event và reward boundary hiện có.
- Sửa lifecycle config: ToolBar refresh sau mọi `LoadLevel`, tránh frame đầu dùng
  giá trị trước khi `GameStart` config reload.
- Count và badge giữ đúng qua Game → Home → Game; tween/event được cleanup khi
  page bị disable.

SDK quảng cáo/backend thật không được thêm. Reward tiếp tục dùng provider
offline/no-op an toàn theo quyết định phạm vi R16.

## Kiểm thử Unity 6000.3.19f1

- Batch compile/import/installer: thành công, process exit code 0.
- Targeted EditMode `SourceLayoutTests`: **29/29 passed**, 0 failed.
- Targeted Platform PlayMode
  `PlatformToolBar_SourceBadgeClickAndPulseStayCoherent`: **1/1 passed**,
  0 failed; bao phủ hierarchy, FREE/HAS_TOOL/NO_TOOL, click decrement,
  persistence Home→Game và idle pulse start/stop.
- Không thêm runtime log.

## Còn ngoài phạm vi closure này

- Pixel/video parity và particle obtain đầy đủ thuộc khối Visual/VFX.
- Provider quảng cáo/native callback/device QA thật ngoài phạm vi bản học tập.
