# R4 Filter, Prefill and Puzzle ID Test Report

- Date: 2026-08-08
- Source report consumed: `Reports/Gemini/GEM-R4-002_transform_filter_prefill.md`
- Source spot-check: `level_data.gd`, `single_region_num_config.gd`, `game_state.gd`, `game_page.gd`, `pre_cat_decider.gd`
- Full EditMode regression: **144 passed, 0 failed**

## Implemented

- `SingleRegionNumConfig` values 0–5, coarse gate and exact strict thresholds for STRICT, ALL_ONE, ZERO_51 and ZERO_101.
- Coarse rejection inside ordinary/main selection, strict outer filter, seen-entry protection and the source fallback budgets.
- Exemptions for LK Modified, SP and LK sources.
- Tutorial prefill rules: levels 1–6 prefer a solution cell in a multi-cell region; levels 7–10 prefer a single-cell region; later levels have no tutorial prefill.
- Region transform, first-seen label normalization, comma serialization and canonical SHA-256 puzzle IDs with Godot suffixes.
- `GameStateService.RecordPuzzle` duplicate lookup, bank/main/LK-modified progress snapshots, deep-copy reads and source limit of 100 records.

No repeated runtime diagnostics were added.

## Boundary preserved

The source performs duplicate retry in `GamePage`, not in `LevelData`. Core now records and detects the duplicate, but retry wiring remains for the future GameSession layer.

`PreCatDecider.pick_prefill_cell` depends on `HintEngine.compute_cell_ranks`. Its state schema already exists and tutorial prefill is complete, but creating a disconnected replacement before HintEngine would only produce placeholder code, so that branch remains explicitly partial until R5.

## Coverage added

- All single-region config thresholds and coarse activation.
- Coarse skip and ZERO_51 outer strict skip through real bank progress.
- Tutorial prefill ranges.
- Exact known canonical hash and label-invariance.
- Previous duplicate return, immutable progress snapshots and 100-entry trimming.
