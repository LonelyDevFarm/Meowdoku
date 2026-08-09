# R4 Random, DDA Selection and Sequence Test Report

- Date: 2026-08-08
- Source report consumed: `Reports/Gemini/GEM-R4-004_random_dda_level_sequence.md`
- Source spot-check: `level_data.gd`, `game_state.gd`, `game_page.gd`, `dda_rank_config.gd`, `daily_first_level_difficulty_config.gd`
- Full EditMode regression: **160 passed, 0 failed**

## Implemented

- Injectable inclusive random contract matching Godot `randi_range(from, to)`.
- Unity runtime adapter uses `Random.Range(minimum, maximum + 1)`.
- Difficulty resolution preserves level caps 2/3/4, random cooling after clamp, and Daily First Easy reduction after RNG.
- Persisted `daily_first_easy_date`, injectable current-date provider, once-per-cold-start evaluation, played-snapshot invalidation, consume/mark and date advancement APIs.
- Typed default-control policies for `daily_first_level_difficulty` and `dda_rank`.
- Gameplay runtime evaluates/consumes Daily First Easy only when its source config is enabled.

## Report discrepancies corrected

- Although GameState may raise stored strategy to 5 at level 101 and 6 at level 201, `LevelData.get_level_entry` clamps every level from 51 onward to 4 before RNG. The selector therefore does not emit strategy 5/6 in this source build.
- `is_hard_level` requires `level >= 21`; level 20 is reached through the SP special mapping, not the hard branch.

## Coverage

- Upper-bound RNG across every level 1–250.
- Lower-bound inclusive RNG at levels 21 and 101.
- Daily reduction after RNG and current-level marking.
- Same-day/snapshot opportunity behavior and persisted date round-trip.
- Full `GetLevelEntry` run for levels 1–250 using representative valid banks, including all special mappings inside that range, ordinary/main pool routing, transforms and hard ranks.

The win/fail aggregate mutations that raise or demote stored strategy remain part of the future GameSession transition. R4 now consumes that strategy exactly as the source selector does.

No runtime logging was added.
