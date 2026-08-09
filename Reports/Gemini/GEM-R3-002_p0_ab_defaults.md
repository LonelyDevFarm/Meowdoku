REPORT_ID: GEM-R3-002
STATUS: COMPLETE
GENERATED_AT: 2026-08-08 15:40:00
SOURCE_ROOT: D:\Projects\_GameExtract\Main_Meokdoku

| Config file | Class | Key | Default expression | Resolved constant/value | Timing | Manager registration line |
|---|---|---|---|---|---|---|
| region_color_config.gd | RegionColorConfig | region_color | VALUE_NEW_CELL_ONLY | 2 | TIMING_APP_START | 151 |
| size_cycle_config.gd | SizeCycleConfig | size_cycle | VALUE_CONTROL | 2 | TIMING_GAME_START_NORMAL | 152 |
| rule_highlight_config.gd | RuleHighlightConfig | rule_highlight | VALUE_CONTROL | 0 | TIMING_GAME_START | 153 |
| goal_emphasis_config.gd | GoalEmphasisConfig | goal_emphasis | VALUE_CONTROL | 0 | TIMING_GAME_START_NORMAL_11 | 154 |
| auto_complete_config.gd | AutoCompleteConfig | auto_complete | VALUE_OFF | 0 | TIMING_GAME_START | 178 |
| error_feedback_config.gd | ErrorFeedbackConfig | error_feedback | VALUE_ALL | 0 | TIMING_GAME_START | 186 |
| swipe_protect_config.gd | SwipeProtectConfig | swipe_protect | VALUE_CONTROL | 0 | TIMING_GAME_START | 190 |
| dda_rank_config.gd | DdaRankConfig | dda_rank | VALUE_CONTROL | 0 | TIMING_GAME_START_NORMAL | 191 |
| revive_life_config.gd | ReviveLifeConfig | revive_life | VALUE_CONTROL | 0 | TIMING_GAME_START | 192 |
| life_icon_config.gd | LifeIconConfig | life_icon | VALUE_FISH | 1 | TIMING_APP_START | 163 |
| single_region_num_config.gd | SingleRegionNumConfig | single_region_num | VALUE_STRICT | 2 | TIMING_GAME_START_NORMAL | 196 |
| board_size_big_config.gd | BoardSizeBigConfig | board_size_big | VALUE_NORMAL | 0 | TIMING_GAME_START | 205 |
| score_encourage_config.gd | ScoreEncourageConfig | score_encourage | VALUE_DISABLED | 0 | TIMING_GAME_START | 206 |
| pre_cat_config.gd | PreCatConfig | pre_cat | VALUE_OFF | 0 | TIMING_GAME_START_NORMAL_21 | 208 |
| game_grid_ui_config.gd | GameGridUiConfig | game_grid_ui | VALUE_NOMAL | 0 | TIMING_APP_START | 211 |
| hint_cat_config.gd | HintCatConfig | hint_cat | VALUE_BULB | 0 | TIMING_APP_START | 213 |
| doubletap_protect_config.gd | DoubletapProtectConfig | doubletap_protect | VALUE_CONTROL | 0 | TIMING_APP_START | 214 |
| vibrate_combo_config.gd | VibrateComboConfig | vibrate_combo | VALUE_CONTROL | 0 | TIMING_GAME_START | 215 |
| combo_text_config.gd | ComboTextConfig | combo_text | VALUE_CONTROL | 0 | TIMING_GAME_START | 217 |
| combo_voice_config.gd | ComboVoiceConfig | combo_voice | VALUE_REAL_MALE_1 | 6 | TIMING_GAME_START | 218 |
| undo_btn_config.gd | UndoBtnConfig | undo_btn | VALUE_CONTROL | 0 | TIMING_GAME_START | N/A |
| game_auto_mark_config.gd | GameAutoMarkConfig | game_auto_mark | VALUE_CONTROL | 0 | TIMING_GAME_START | N/A |
| game_life_rule_config.gd | GameLifeRuleConfig | game_life_rule | VALUE_OFF | 0 | TIMING_APP_START | N/A |
| wrong_cat_effect_config.gd | WrongCatEffectConfig | wrong_cat_effect | VALUE_CONTROL | 0 | TIMING_GAME_START | N/A |

### region_color_config.gd
CONSTANTS
| Name | Expression/value | Line |
|---|---|---|
| VALUE_CONTROL | 0 | 22 |
| VALUE_CUSTOM_PALETTE | 1 | 23 |
| VALUE_NEW_CELL_ONLY | 2 | 24 |
| VALUE_CELL_COLOR_V3 | 3 | 25 |
| VALUE_NEW_CELL_RECOMPUTE | 4 | 26 |
| VALUE_PALETTE_V5 | 5 | 27 |
| VALUE_PALETTE_V6 | 6 | 28 |
| VALUE_PALETTE_V7 | 7 | 29 |
| VALUE_PALETTE_V8 | 8 | 30 |
| VALUE_PALETTE_V9 | 9 | 31 |
| VALUE_ALL_WARM | 10 | 32 |
| VALUE_ALL_COOL | 11 | 33 |
| VALUE_TEMP_BALANCED | 12 | 34 |

PUBLIC METHODS
| Signature | Return/match/if expression | Line |
|---|---|---|
| is_custom_palette() -> bool: | value() == VALUE_CUSTOM_PALETTE | 42 |
| is_new_cell_only_palette() -> bool: | value() == VALUE_NEW_CELL_ONLY | 46 |
| is_cell_color_v3() -> bool: | value() == VALUE_CELL_COLOR_V3 | 50 |
| is_new_cell_recompute() -> bool: | value() == VALUE_NEW_CELL_RECOMPUTE | 54 |
| is_palette_v5() -> bool: | value() == VALUE_PALETTE_V5 | 58 |
| is_palette_v6() -> bool: | value() == VALUE_PALETTE_V6 | 62 |
| is_palette_v7() -> bool: | value() == VALUE_PALETTE_V7 | 66 |
| is_palette_v8() -> bool: | value() == VALUE_PALETTE_V8 | 70 |
| is_palette_v9() -> bool: | value() == VALUE_PALETTE_V9 | 74 |
| is_all_warm() -> bool: | value() == VALUE_ALL_WARM | 78 |
| is_all_cool() -> bool: | value() == VALUE_ALL_COOL | 82 |
| is_temp_balanced() -> bool: | value() == VALUE_TEMP_BALANCED | 86 |

### size_cycle_config.gd
CONSTANTS
| Name | Expression/value | Line |
|---|---|---|
| VALUE_CONTROL | 2 | 14 |
| VALUE_CYCLE_V3_A | 3 | 15 |
| VALUE_CYCLE_V3_B | 4 | 16 |
| VALUE_CYCLE_V3_C | 5 | 17 |
| VALUE_CYCLE_V3_D | 6 | 18 |
| VALUE_CYCLE_V3_E | 7 | 19 |
| VALUE_CYCLE_V3_F | 8 | 20 |

PUBLIC METHODS
| Signature | Return/match/if expression | Line |
|---|---|---|
| is_cycle_enabled() -> bool: | value() != VALUE_CONTROL | 28 |

### rule_highlight_config.gd
CONSTANTS
| Name | Expression/value | Line |
|---|---|---|
| VALUE_CONTROL | 0 | 10 |
| VALUE_HIGHLIGHT_VIOLATED | 1 | 11 |
| VALUE_HIGHLIGHT_ALL_LEVELS | 2 | 12 |

PUBLIC METHODS
| Signature | Return/match/if expression | Line |
|---|---|---|
| is_highlight_violated() -> bool: | value() != VALUE_CONTROL | 22 |
| is_all_levels() -> bool: | value() == VALUE_HIGHLIGHT_ALL_LEVELS | 27 |

### goal_emphasis_config.gd
CONSTANTS
| Name | Expression/value | Line |
|---|---|---|
| VALUE_CONTROL | 0 | 13 |
| VALUE_SPLIT_BY_LEVEL | 1 | 14 |
| LEVEL_THRESHOLD | 10 | 16 |

PUBLIC METHODS
| Signature | Return/match/if expression | Line |
|---|---|---|
| should_emphasize_cat_score(level: int) -> bool: | value() == VALUE_SPLIT_BY_LEVEL and level > LEVEL_THRESHOLD | 26 |

### auto_complete_config.gd
CONSTANTS
| Name | Expression/value | Line |
|---|---|---|
| VALUE_OFF | 0 | 14 |
| VALUE_ON | 1 | 15 |
| VALUE_LAST_CAT_ONLY | 2 | 16 |

PUBLIC METHODS
(None)

### error_feedback_config.gd
CONSTANTS
| Name | Expression/value | Line |
|---|---|---|
| VALUE_ALL | 0 | 10 |
| VALUE_CONFLICT_ONLY | 1 | 11 |
| VALUE_NONE | 2 | 12 |

PUBLIC METHODS
| Signature | Return/match/if expression | Line |
|---|---|---|
| only_conflicting_cats_react() -> bool: | value() == VALUE_CONFLICT_ONLY | 20 |
| no_cats_react() -> bool: | value() == VALUE_NONE | 24 |

### swipe_protect_config.gd
CONSTANTS
| Name | Expression/value | Line |
|---|---|---|
| VALUE_CONTROL | 0 | 26 |
| VALUE_HOTZONE_40 | 1 | 27 |
| VALUE_HOTZONE_10 | 2 | 28 |
| VALUE_HOTZONE_RAISED | 3 | 29 |
| VALUE_HOTZONE_30 | 4 | 30 |
| VALUE_HOTZONE_20 | 5 | 31 |
| VALUE_HOTZONE_50 | 6 | 32 |
| VALUE_DYNAMIC_INTENT | 7 | 33 |
| DYNAMIC_WINDOW_MS | 100 | 36 |
| DYNAMIC_VELOCITY_THRESHOLD_PX_PER_MS | 1.2 | 37 |

PUBLIC METHODS
| Signature | Return/match/if expression | Line |
|---|---|---|
| is_enabled() -> bool: | is_enabled_for(int(value())) | 52 |
| min_size() -> int: | min_size_for(int(value())) | 55 |
| tolerance_pct() -> float: | tolerance_pct_for(int(value())) | 58 |
| threshold_for(n: int) -> int: | threshold_for_value(int(value()), n) | 61 |
| is_dynamic_intent() -> bool: | int(value()) == VALUE_DYNAMIC_INTENT | 64 |
| velocity_window_ms() -> int: | effective_velocity_window_ms() | 67 |
| velocity_threshold_px_per_ms() -> float: | effective_velocity_threshold_px_per_ms() | 70 |

### dda_rank_config.gd
CONSTANTS
| Name | Expression/value | Line |
|---|---|---|
| VALUE_CONTROL | 0 | 11 |
| VALUE_RETRY_ONCE | 1 | 12 |
| VALUE_TOOL_REVIVE | 2 | 13 |
| VALUE_ANY_ACTION | 3 | 14 |

PUBLIC METHODS
| Signature | Return/match/if expression | Line |
|---|---|---|
| is_retry_once_demote() -> bool: | value() == VALUE_RETRY_ONCE | 22 |
| is_tool_revive_demote() -> bool: | value() == VALUE_TOOL_REVIVE | 26 |
| is_any_action_demote() -> bool: | value() == VALUE_ANY_ACTION | 30 |

### revive_life_config.gd
CONSTANTS
| Name | Expression/value | Line |
|---|---|---|
| VALUE_CONTROL | 0 | 20 |
| VALUE_GROUP_1 | 1 | 21 |
| VALUE_GROUP_2 | 2 | 22 |
| VALUE_GROUP_3 | 3 | 23 |

PUBLIC METHODS
| Signature | Return/match/if expression | Line |
|---|---|---|
| get_lives_to_restore() -> int: | 1 if value() == VALUE_CONTROL else 3 | 32 |
| is_two_line_button() -> bool: | value() == VALUE_GROUP_2 | 36 |
| is_alt_button_text() -> bool: | value() == VALUE_GROUP_3 | 40 |

### life_icon_config.gd
CONSTANTS
| Name | Expression/value | Line |
|---|---|---|
| VALUE_FISH | 1 | 15 |
| VALUE_FISH_C1 | 3 | 16 |
| VALUE_FISH_C2 | 4 | 17 |
| VALUE_FISH_C3 | 5 | 18 |
| VALUE_HEART | 0 | 20 |
| VALUE_LIGHTNING | 2 | 22 |

PUBLIC METHODS
| Signature | Return/match/if expression | Line |
|---|---|---|
| fish_full_texture() -> Texture2D: | match value() | 44 |
| fish_dim_texture() -> Texture2D: | match value() | 55 |
| fish_debris_texture() -> Texture2D: | match value() | 66 |
| effective_fish_full() -> Texture2D: | v if v != null else _FISH_FULL_BASE | 78 |
| fish_variant_index() -> int: | match value() | 84 |

### single_region_num_config.gd
CONSTANTS
| Name | Expression/value | Line |
|---|---|---|
| VALUE_DEFAULT | 0 | 19 |
| VALUE_LIMITED | 1 | 20 |
| VALUE_STRICT | 2 | 21 |
| VALUE_ALL_ONE | 3 | 22 |
| VALUE_ZERO_51 | 4 | 23 |
| VALUE_ZERO_101 | 5 | 24 |

PUBLIC METHODS
| Signature | Return/match/if expression | Line |
|---|---|---|
| is_coarse_limited() -> bool: | value() != VALUE_DEFAULT | 36 |
| single_limit_at(level_num: int, rank: int) -> int: | match value() | 43 |
| is_single_region_limited() -> bool: | value() == VALUE_LIMITED | 61 |
| is_strict_limited_at(level_num: int) -> bool: | value() == VALUE_STRICT and level_num >= 21 | 65 |

### board_size_big_config.gd
CONSTANTS
| Name | Expression/value | Line |
|---|---|---|
| VALUE_NORMAL | 0 | 10 |
| VALUE_ENLARGED | 1 | 11 |

PUBLIC METHODS
| Signature | Return/match/if expression | Line |
|---|---|---|
| is_enlarged() -> bool: | value() == VALUE_ENLARGED | 21 |

### score_encourage_config.gd
CONSTANTS
| Name | Expression/value | Line |
|---|---|---|
| VALUE_DISABLED | 0 | 16 |
| VALUE_FLY_EFFECT | 1 | 17 |
| VALUE_NON_ROUND | 2 | 18 |
| VALUE_MULTIPLIER | 3 | 19 |
| VALUE_SKILL_SCORE | 4 | 20 |
| VALUE_DEDUCTION | 5 | 21 |
| VALUE_LIFE_BONUS | 6 | 22 |
| VALUE_MULTIPLIER_SCROLL | 7 | 23 |

PUBLIC METHODS
| Signature | Return/match/if expression | Line |
|---|---|---|
| is_enabled() -> bool: | value() != VALUE_DISABLED | 30 |
| has_fly_effect() -> bool: | (v >= VALUE_FLY_EFFECT and v <= VALUE_MULTIPLIER) or v == VALUE_MULTIPLIER_SCROLL | 33 |
| has_custom_scoring() -> bool: | value() >= VALUE_NON_ROUND | 38 |
| has_multiplier_display() -> bool: | v == VALUE_MULTIPLIER or v == VALUE_MULTIPLIER_SCROLL | 41 |
| has_scroll_multiplier_anim() -> bool: | value() == VALUE_MULTIPLIER_SCROLL | 45 |
| has_appear4_multiplier_anim() -> bool: | value() == VALUE_MULTIPLIER | 48 |
| calc_gain(combo_count: int) -> int: | match value() | 52 |
| calc_multiplier(combo_count: int) -> float: | 1.2 + 0.1 * combo_count | 68 |
| has_skill_score() -> bool: | value() == VALUE_SKILL_SCORE | 77 |
| has_deduction() -> bool: | value() == VALUE_DEDUCTION | 80 |
| has_life_bonus() -> bool: | value() == VALUE_LIFE_BONUS | 83 |
| calc_skill_bonus(cell_strategy: int) -> int: | match cell_strategy | 88 |
| get_deduction_per_mistake() -> int: | 100 | 100 |
| calc_life_bonus_sequence(lives: int) -> Array[int]: | match lives | 104 |

### pre_cat_config.gd
CONSTANTS
| Name | Expression/value | Line |
|---|---|---|
| VALUE_OFF | 0 | 13 |
| VALUE_ALWAYS | 1 | 14 |
| VALUE_HALF | 2 | 15 |

PUBLIC METHODS
| Signature | Return/match/if expression | Line |
|---|---|---|
| is_enabled() -> bool: | value() != VALUE_OFF | 23 |
| should_always_prefill() -> bool: | value() == VALUE_ALWAYS | 27 |
| should_half_prefill() -> bool: | value() == VALUE_HALF | 31 |
| peek_group() -> int: | int(peek_value()) | 37 |

### game_grid_ui_config.gd
CONSTANTS
| Name | Expression/value | Line |
|---|---|---|
| VALUE_NOMAL | 0 | 5 |
| VALUE_SINGLE_LINE | 1 | 6 |
| VALUE_REDUCE_SPACING | 2 | 7 |
| VALUE_DIFFERENT_CORNERS | 3 | 8 |

PUBLIC METHODS
| Signature | Return/match/if expression | Line |
|---|---|---|
| is_single_line() -> bool: | value() == VALUE_SINGLE_LINE | 27 |
| is_reduce_spacing() -> bool: | value() == VALUE_REDUCE_SPACING | 30 |
| is_different_corners() -> bool: | value() == VALUE_DIFFERENT_CORNERS | 33 |
| get_difference_size_cell_corners(size: int) -> int: | CORNER_RADIUS_BY_SIZE.get(size, 10) | 36 |
| get_board_layout() -> Dictionary: | LAYOUT_BY_GROUP[g] | 48 |
| solve_local_layout(sz: int) -> Dictionary: | calculation block (scale) | 75 |

### hint_cat_config.gd
CONSTANTS
| Name | Expression/value | Line |
|---|---|---|
| VALUE_BULB | 0 | 17 |
| VALUE_BULB_PAW | 1 | 18 |
| VALUE_BULB_EAR | 2 | 19 |
| VALUE_YARN_BULB | 3 | 20 |
| VALUE_BULB_EAR2 | 4 | 21 |

PUBLIC METHODS
| Signature | Return/match/if expression | Line |
|---|---|---|
| is_cat_hint() -> bool: | value() != VALUE_BULB | 36 |
| icon_texture() -> Texture2D: | match value() | 42 |

### doubletap_protect_config.gd
CONSTANTS
| Name | Expression/value | Line |
|---|---|---|
| VALUE_CONTROL | 0 | 17 |
| VALUE_SHORTEN | 1 | 18 |
| VALUE_BY_TRUTH | 2 | 19 |
| VALUE_BY_CONFLICT | 3 | 20 |
| LONG_SEC | 0.35 | 23 |
| SHORT_SEC | 0.25 | 24 |

PUBLIC METHODS
| Signature | Return/match/if expression | Line |
|---|---|---|
| window_sec(truth_has_cat: bool, would_conflict: bool) -> float: | match value() | 34 |
| needs_truth() -> bool: | value() == VALUE_BY_TRUTH | 45 |
| needs_conflict() -> bool: | value() == VALUE_BY_CONFLICT | 49 |

### vibrate_combo_config.gd
CONSTANTS
| Name | Expression/value | Line |
|---|---|---|
| VALUE_CONTROL | 0 | 14 |
| VALUE_STRONG | 1 | 15 |
| VALUE_STRONGER | 2 | 16 |
| VALUE_WEAK_TO_STRONG | 3 | 17 |
| VALUE_WEAKER_TO_STRONG | 4 | 18 |

PUBLIC METHODS
| Signature | Return/match/if expression | Line |
|---|---|---|
| is_enabled() -> bool: | value() != VALUE_CONTROL | 26 |
| combo_vibrate_level(combo: int) -> int: | match value() | 30 |

### combo_text_config.gd
CONSTANTS
| Name | Expression/value | Line |
|---|---|---|
| VALUE_CONTROL | 0 | 14 |
| VALUE_VARIANT_ORDINARY_1 | 1 | 15 |
| VALUE_VARIANT_ORDINARY_2 | 2 | 16 |
| VALUE_VARIANT_MEOW | 3 | 17 |

PUBLIC METHODS
| Signature | Return/match/if expression | Line |
|---|---|---|
| is_enabled() -> bool: | value() != VALUE_CONTROL | 25 |
| get_animation_player_suffix() -> String: | match value() | 31 |
| get_max_encourage_level() -> int: | if value() == VALUE_CONTROL: 6 else 10 | 44 |

### combo_voice_config.gd
CONSTANTS
| Name | Expression/value | Line |
|---|---|---|
| VALUE_REAL_MALE_1 | 6 | 14 |
| VALUE_REAL_MALE_A_TEXT1 | 9 | 15 |
| VALUE_REAL_MALE_B_TEXT2 | 10 | 16 |
| VALUE_REAL_FEMALE_MEOW_TEXT3 | 11 | 17 |

PUBLIC METHODS
| Signature | Return/match/if expression | Line |
|---|---|---|
| get_combo_voice(combo_count: int) -> String: | lookup Dictionary | 78 |

### undo_btn_config.gd
CONSTANTS
| Name | Expression/value | Line |
|---|---|---|
| VALUE_CONTROL | 0 | 11 |
| VALUE_UNDO_PAID | 1 | 12 |
| VALUE_HIGHLIGHT_PAID | 2 | 13 |
| VALUE_UNDO_FREE | 3 | 14 |
| VALUE_HIGHLIGHT_FREE | 4 | 15 |

PUBLIC METHODS
(None)

### game_auto_mark_config.gd
CONSTANTS
| Name | Expression/value | Line |
|---|---|---|
| VALUE_CONTROL | 0 | 18 |
| VALUE_AUTO_CROSS | 1 | 19 |
| VALUE_DOT_TOGGLE | 2 | 20 |
| VALUE_AUTO_CROSS_LV6 | 3 | 21 |
| VALUE_LOCK_X | 4 | 22 |
| VALUE_PROP_AD | 5 | 23 |

PUBLIC METHODS
(None)

### game_life_rule_config.gd
CONSTANTS
| Name | Expression/value | Line |
|---|---|---|
| VALUE_OFF | 0 | 11 |
| VALUE_PLUS_ONE | 1 | 12 |

PUBLIC METHODS
(None)

### wrong_cat_effect_config.gd
CONSTANTS
| Name | Expression/value | Line |
|---|---|---|
| VALUE_CONTROL | 0 | 13 |
| VALUE_NO_SHAKE | 1 | 14 |
| VALUE_LOW_WRONG_VOLUME | 2 | 15 |
| VALUE_NO_RED_FILL | 3 | 16 |
| VALUE_LOW_FAIL_VOLUME | 4 | 17 |
| VALUE_ALL_REDUCED | 5 | 18 |

PUBLIC METHODS
(None)

BẤT THƯỜNG:
- File tồn tại nhưng không được manager đăng ký: `undo_btn_config.gd`, `game_auto_mark_config.gd`, `game_life_rule_config.gd`, `wrong_cat_effect_config.gd`.

GIỚI HẠN CỦA BÁO CÁO:
- (Không có)

KHÔNG BAO GỒM:
- Không viết code.
- Không sửa file.
- Không đề xuất kiến trúc.
- Không tự đánh dấu roadmap hoàn thành.
