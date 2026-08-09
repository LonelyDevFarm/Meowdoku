# R6 Input Config Wiring Test Report

- Date: 2026-08-08
- Source report consumed: `Reports/Gemini/GEM-R6-001_input_config_wiring.md`
- Source spot-check: board/swipe recognizers and double-tap provider/conflict callbacks in `base_game_page.gd`
- Targeted input result: **25 passed, 0 failed**
- Last refreshed full regression assembly: **122 passed, 0 failed**

## Implemented

- `DoubleTapProtectConfig` now provides the per-cell 0.25/0.35 second window.
- Truth and conflict callbacks only run for the variants that require them.
- `SwipeGuardRecognizer` owns axis guard and velocity gate in the same start/over/end order as source.
- Swipe thresholds, minimum size, tolerance and dynamic velocity come from source config.
- Pointer positions are converted to board-local top-left pixels before hit testing.
- Runtime geometry uses actual Unity cell size, spacing and padding instead of temporary visual constants.
- Grid layout is explicitly fixed-column and upper-left aligned for stable N×N coordinates.

## Preserved Unity adaptation

Godot emits the first single-tap action immediately, while this Unity prototype defers it until the double-tap window closes. The user already verified that this removes the X-then-cat flash. A-005 now documents this deliberate difference; only the timing policy was replaced with source config.

No swipe A/B variant is enabled by invention: the source default remains control (`0`).

## Remaining gate

EditMode coverage validates config windows, row/column conversion, interpolation, row locking and minimum board size. PlayMode tests on real pointer/touch events at multiple framerates and visual verification of the final cat state are still required.

After Unity refreshes the five newly added input cases, the expected combined EditMode count is 127.
