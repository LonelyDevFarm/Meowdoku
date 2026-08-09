# R4 Bank Pool Selection Test Report

- Date: 2026-08-08
- Source report consumed: `Reports/Gemini/GEM-R4-001_bank_pool_inventory.md`
- Source spot-check: `bank_data.gd` and `level_data.gd`
- Full EditMode regression: **130 passed, 0 failed**

## Implemented

- Lazy caches and source-shaped APIs for regular, LK Style and GC banks by size/rank/tier.
- Lazy SP, LK and LK Modified loaders, including SP reload and count APIs.
- `LevelEntry.maxR` plus runtime main-source/transform metadata.
- Cached entries remain immutable: selection clones an entry before attaching runtime metadata or transforming its board.
- Ordinary pool order is regular, LK Style, then GC; GC is enabled only for size 10/rank 1 or size 11.
- Main pool excludes reserved LK Modified indices, applies strict `r` versus fallback `maxR`, inserts LK Modified after four ordinary entries, and cycles transforms 0–7.
- Legacy bank index migration and separate main/LK Modified progress updates match the source flow.
- Special SP/LK entries use the original one-based mappings and rank metadata rules.

## Tests added

- All three JSON root shapes: rank dictionary, wrapped `levels`, and raw level array contract.
- Rank/tier filtering for regular, LK Style and GC.
- LK Modified `maxR` parsing.
- Four ordinary entries followed by one LK Modified entry.
- Runtime metadata does not mutate the cached bank entry.

## Remaining R4 work

`single_region_num`, recent-puzzle filtering, prefill/PreCat, canonical puzzle ID, and the full LevelGenerator color pipeline remain separate tasks. Unity resource loading still needs a PlayMode test against the encrypted assets, although compile and pure selection/parser regression are clean.
