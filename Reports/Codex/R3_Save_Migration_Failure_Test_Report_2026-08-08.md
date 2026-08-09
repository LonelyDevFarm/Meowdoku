# R3 Save Migration and Failure Test Report

- Date: 2026-08-08
- Source report consumed: `Reports/Gemini/GEM-R3-006_save_migration_failures.md`
- Source spot-check: complete `save_store.gd`, `_migrate_legacy_save` and version references in `game_state.gd`
- Targeted result: **17 passed, 0 failed**
- Last refreshed full regression assembly: **108 passed, 0 failed**

## Implemented and verified

- Proactive migration runs only when the flag file is absent and a readable legacy file exists.
- First migration creates slot A and flag A; it does not fabricate slot B.
- Legacy input is preserved after successful migration.
- Corrupt legacy input fails without creating a slot or flag.
- Corrupt/missing flag follows the source order: slot A, then B, then legacy.
- Both slots corrupt without legacy returns no document, allowing typed defaults at repository level.
- Orphan `.tmp` files are never treated as committed saves.
- Wrong-typed player fields fall back independently without discarding valid sibling fields.

## Source corrections

The Gemini report described migration as populating A/B, but the source calls `save_config` once and therefore creates only A on the first migration. It also associated `ENDGAME_SNAPSHOT_VERSION = 2` with migration; source search confirms that constant has no call site and no schema-version migration exists.

## Remaining platform gate

Atomic/app-kill behavior still requires an actual Unity player test on target filesystems. Pure EditMode tests cover deterministic file states but cannot simulate power loss during an OS rename or flush.

The current two unrefreshed increments add seven endgame tests and seven migration/failure tests to the last 108-case Unity assembly. After refresh, the expected combined EditMode count is 122.
