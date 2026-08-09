# R3 Config Defaults Test Report

- Date: 2026-08-08
- Source report consumed: `Reports/Gemini/GEM-R3-002_p0_ab_defaults.md`
- Source spot-check: `ab_config_base.gd`, `swipe_protect_config.gd`, `doubletap_protect_config.gd`, `region_color_config.gd`, `size_cycle_config.gd`, and timing/registration lines in `abtest_manager.gd`
- Result: **27 passed, 0 failed**

## Scope

- `AbConfigBase<T>` default, reload and debug-override behavior
- P0 default catalog count, unique keys, timing and source-registration flags
- Swipe protect enable/tolerance/threshold variants
- Double-tap window variants
- Region-color and size-cycle source defaults

## Verification method

The new Core sources compiled successfully with Unity 6000.3.19f1's bundled Roslyn compiler. The isolated pure config suite was then compiled and executed with Unity's bundled Mono runtime and project NUnit framework.

The open Unity Editor had not refreshed its `Library/ScriptAssemblies` when this report was written, so the existing 67-case full suite was not relabeled as a new full-regression result. The user should refresh Unity once; Codex can verify the complete combined suite on the next turn.

## Preserved source anomaly

`undo_btn`, `game_auto_mark`, `game_life_rule`, and `wrong_cat_effect` exist in source but are not registered by the scanned Godot manager. The Unity catalog records them with `RegisteredBySource = false`; no registration behavior was invented.
