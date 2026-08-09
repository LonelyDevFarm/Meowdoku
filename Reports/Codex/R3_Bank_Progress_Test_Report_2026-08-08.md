# R3 Bank Progress Test Report

- Date: 2026-08-08
- Source report consumed: `Reports/Gemini/GEM-R3-003_bank_progress_leveldata.md`
- Source spot-check: progress API in `game_state.gd`; ordinary/main selection and commit paths in `level_data.gd`
- Targeted result: **9 passed, 0 failed**
- Refreshed regression assembly: **94 passed, 0 failed**

## Implemented

- Source key shape: `size_rank` and `size_rank_H`; tier `N` intentionally shares the non-H key.
- Legacy bank index, main progress and LK-modified progress mutation APIs.
- `persist=false` batching followed by one explicit commit.
- Deep-copy progress snapshots.
- The source's legacy-shaped main-progress default is preserved so missing `idx` can trigger migration.
- `LevelData` no longer reads or writes `PlayerPrefs`.

## Verification note

The 9 targeted cases include 6 new `GameStateService` cases and 3 existing repository cases. The open Unity Editor has not yet refreshed the newest service files; after refresh, the expected combined EditMode count is 100. Core integration compiled successfully with Unity's bundled Roslyn compiler.

R4 still needs the full regular/LK-style/GC pool composition and level 51+ main/LK-modified interleave. This change only replaces temporary persistence without claiming that the entire level-selection pipeline is complete.
