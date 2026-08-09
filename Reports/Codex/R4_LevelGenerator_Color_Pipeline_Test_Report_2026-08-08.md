# R4 LevelGenerator Color Pipeline Test Report

- Date: 2026-08-08
- Source report consumed: `Reports/Gemini/GEM-R4-003_level_generator_color_pipeline.md`
- Source spot-check: `level_generator.gd`, `board_view.gd`, `cell.tscn`, `game_page.gd`
- Full EditMode regression: **153 passed, 0 failed**

## Implemented

- Default 12-color RGB greedy map and arbitrary RGB palette variant.
- Seeded LCG/Fisher–Yates order using constants `1664525`, `1013904223` and mask `2147483647`.
- CIE L*a*b* conversion, D65 matrix and Euclidean Lab selection.
- RGB and Lab pattern variants with luminance-sorted dark/light pools.
- All RegionColor values 0–12, including custom/new-cell/V3/V5–V9 and warm/cool/balanced palette composition.
- `patternRegions` parsing in `LevelEntry`.
- Runtime wiring uses custom bank colorMap when present; otherwise it seeds from `_bank_transform`, then applies the same BoardView config override branch as Godot.

## Source discrepancy caught by spot-check

The report describes degree priority as descending. The actual Godot comparator returns `db > da` when degrees differ, which orders lower-degree regions first under `sort_custom`. Unity now follows the executable expression, not the prose interpretation. A fixed stripe fixture locks the resulting map.

## Coverage added

- Exact default stripe map showing comparator direction.
- Exact LCG results for seeds 1, 2 and 123.
- Pattern dark-pool reservation.
- Default config preserving caller colorMap and V3 forcing recomputation.
- Warm, cool and balanced palette sizes.

No runtime logging was added.
