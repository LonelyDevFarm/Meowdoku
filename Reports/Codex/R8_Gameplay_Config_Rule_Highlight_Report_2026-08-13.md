# R8 Shared Gameplay Config + RuleHighlight — 2026-08-13

## Kết quả

Đã đóng `P-GAME-004` và sửa ownership của sáu A/B config gameplay vốn đã
có class nhưng chưa nằm trong shared runtime catalog. Default offline vẫn tắt
rule highlight như nguồn; các remote/debug variant nay thực sự đi tới consumer.

## Đối chiếu nguồn

- `scripts/module/abtest/abtest_manager.gd`
- `scripts/module/abtest/config/rule_highlight_config.gd`
- `scripts/module/game/view/base_game_page.gd::_try_emit_rule_violation`
- `scripts/module/game/view/game_page.gd::_on_rule_violated`

## Sai lệch đã sửa

- Unity trước đây chỉ có Off/On, thiếu `VALUE_HIGHLIGHT_ALL_LEVELS=2`.
- `GameplayRuleBarPresenter` tự tạo `RuleHighlightConfig`, vì vậy GameStart
  reload từ provider không thể thay đổi hành vi thật.
- `daily_first_level_difficulty`, `dda_rank`, `reward_unlock_level`,
  `prop_highlight` và `mark_sound` cũng dùng instance cục bộ hoặc chưa được
  đăng ký trong catalog dù nguồn sở hữu chúng qua `ABTestManager`.
- `daily_first_level_difficulty` còn thiếu trong `DefaultConfigProfile`.

## Hành vi đã khóa

- Control (`0`) không hiện highlight.
- Highlight violated (`1`) chỉ hiện khi Tutorial đã xong và level `<= 5`.
- Highlight all levels (`2`) bỏ gate Tutorial/level.
- Daily giữ hành vi base page, không tự thêm override highlight.
- Rule classification vẫn dùng priority nguồn; presenter chỉ quyết định có
  phát pulse hay không, không thay đổi wrong-guess/lives/score.
- `GameplayConfigSet` tải AppStart/GameStart/GameStartNormal đúng timing và
  truyền cùng instance tới GameplayManager, RuleBar và GameState DDA consumer.

## Bằng chứng Unity

- Full EditMode: **665 passed, 0 failed** — 144,341 giây.
- Platform PlayMode: **12 passed, 0 failed** — 129,238 giây.
- AppScene fixture chứng minh provider → `AbConfigRuntime.Gameplay` →
  Gameplay/GameState và RuleBar thật; control giữ ba highlight ẩn, all-level
  bật đúng highlight được phân loại sau wrong double-tap.
- Không thêm runtime log.

## Còn mở

Các mục `P-GAME-001..003`, `005..010` tiếp tục được audit theo bằng chứng
source + AppScene. RuleBar `rule_text` variant/collapse và pixel/video parity
thuộc R9, không được suy diễn là hoàn thành từ test policy này.
