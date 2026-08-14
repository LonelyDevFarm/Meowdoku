# R17 Gameplay Presentation Closure — 2026-08-14

## Phạm vi

Đóng phần trình bày board/gameplay cùng Result VFX cho Fail/Revive/Win và objective audio QA của F2. Nghe trực tiếp để đánh giá âm lượng, chồng âm và cảm giác trên thiết bị vẫn là `USER QA`.

## Đối chiếu nguồn Godot

- `BoardContainerCell_01.gd`, `game_page.tscn` và `daily_game_page.tscn`: cell enter theo ring `column + (N - 1 - row)`, đi từ bottom-left lên top-right; normal, reduce-spacing và single-line giữ curve/timing riêng.
- `combo_feedback_view.gd` và `base_game_page.gd`: `score_encourage` reload ở GameStart; score fly delay là `0.8`, `1.367` hoặc `1.45` giây theo variant; completion chỉ chờ mốc launch.
- `tool_button.tscn`: `tool_loop` dài `1.5s` và cleanup bằng RESET. `base_game_page.gd`: RuleBar violation pulse chu kỳ `0.6s`, alpha floor `0.4`, lặp hai vòng.
- `game_fail_page`: timeline overlay/cat/title/remaining/encourage/CTA, input unlock `1.5s` và close `0.1s`.
- `game_win_page`: timeline ray/title/cat/glow/body/CTA; one-shot `0.394s`, ribbon loop `0.535s`, dùng các effect asset line/ribbon/star/glow nguồn.

## Thay đổi Unity

- `BoardView`/`CellView` giữ diagonal delay, ba curve nguồn, CanvasGroup intro và lifecycle stop/restart an toàn.
- `GameplayConfigSet` đăng ký shared `ScoreEncourageConfig`; `GameplayManager` reload đúng shared GameStart instance. Score bubble/multiplier/flight, RuleBar và ToolButton giữ sorting cùng pool lifecycle.
- `GameFailPagePresenter` dùng CanvasGroup riêng cho từng nhóm, áp đúng timing nguồn, khóa CTA `1.5s`, close `0.1s`, đồng thời cleanup/reopen không giữ tween hoặc state cũ.
- `GameWinPagePresenter` kết hợp `ResultCelebrationEffects` để dựng UGUI line/ribbon/star/glow theo pool cố định; one-shot, ribbon loop và confetti đều được thu hồi khi đóng/reopen.
- `ResultPagePrefabInstaller` nâng cấp prefab idempotent. `TweenRuntimeConfiguration` preallocate `512` tweeners và `128` sequences; không xuất hiện resize warning.
- Bridge có thể tái tạo event resilient sau lifecycle reset. `RateUsStarPointerView` được tách thành MonoScript riêng và hai prefab RateUs được rebuild qua Unity API; không còn missing script.

## Kiểm thử

- Unity compile: clean.
- Platform EditMode: `88/88`.
- Platform PlayMode: `27/27`.
- Portfolio Visual PlayMode: `1/1`.
- Cả `66` clip serialized decode thành sample hữu hạn, không im lặng; luồng thật khóa play-count cho `BoardEnter`, `MarkCat`, `Wrong`, `Fail`, `AllCleared` và `Win`.
- Ảnh bằng chứng: `04c_CatBurst`, `05_Fail`, `06_Win` và `06b_Win_1080x2400`.

## Sai khác được chấp nhận

- UGUI, CanvasGroup và DOTween là adapter Unity cho animation/control Godot.
- Asset Spine không có runtime tương thích dùng static sprite + DOTween theo quyết định portfolio.
- BGM hard-off là contract chủ ý để khớp nguồn.
- Cảm nhận audio và input/vibration trên thiết bị chờ `USER QA`.

## Trạng thái

Board/gameplay presentation và Result VFX: `DONE`. F2 chỉ còn `ACTIVE` để người dùng nghiệm thu nghe trực tiếp.
