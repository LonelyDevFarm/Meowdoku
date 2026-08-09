REPORT_ID: GEM-R3-001
STATUS: COMPLETE
GENERATED_AT: 2026-08-08 15:24:22
SOURCE_ROOT: D:\Projects\_GameExtract\Main_Meokdoku

THỜI_GIAN: 2026-08-08 15:24:22
PHẠM_VI ĐÃ QUÉT: scripts/module/game_state/game_state.gd, scripts/module/game_state/save_store.gd
PHẠM_VI KHÔNG THỂ QUÉT: N/A

CÔNG CỤ/LỆNH ĐÃ DÙNG:
- C# Script Csc Compiler v5.0 (System.IO.File.ReadAllLines, Regex)

TỔNG KẾT SỐ LIỆU:
- Tổng số file: 2
- Matched: 102
- Missing: 0
- Extra: 0
- Duplicate: 0
- Error: 0

KẾT QUẢ CHI TIẾT:
### 1. Hằng số Save
- const SAVE_DIR: = "user://save_store/"
- const SAVE_PATH_A: = "user://save_store/save_a.cfg"
- const SAVE_PATH_B: = "user://save_store/save_b.cfg"
- const SAVE_FLAG: = "user://save_store/flag.txt"
- const SAVE_PATH_OLD: = "user://save.cfg"
- const SAVE_PATH_ENDGAME: = "user://save_store/endgame.cfg"
- const SAVE_PASSWORD: = "qd_x9K3mPv7RtN2sLwH8jFcZyA5eBkM1n"
- const _LOAD_ATTEMPTS: int = 3
- const _RETRY_DELAY_MS: int = 60

### 2 & 3. Dữ liệu cfg.set_value và cfg.get_value
| Section | Key | Expr Saved | Default Loaded | Save Line | Load Line |
|---|---|---|---|---|---|
| snapshot | data | _endgame_snapshot | {} | 1599 | 1623 |
| stats | main_total | _main_game_total_stats | {} | 1600 | 1624 |
| stats | daily_total | _daily_game_total_stats | {} | 1601 | 1625 |
| stats | main_round | _main_game_round_stats | {} | 1602 | 1626 |
| stats | daily_round | _daily_game_round_stats | {} | 1603 | 1627 |
| stats | main_id | _main_game_id | "" | 1604 | 1628 |
| stats | daily_id | _daily_game_id | "" | 1605 | 1629 |
| progress | current_level | _current_level | 1 | 1946 | 2052 |
| progress | tutorial_done | _tutorial_done | false | 1947 | 2053 |
| progress | current_strategy | _current_strategy | 1 | 1948 | 2054 |
| progress | consecutive_clean_wins | _consecutive_clean_wins | 0 | 1949 | 2055 |
| progress | last_level_clean_win | _last_level_clean_win | false | 1950 | 2056 |
| progress | consecutive_fails | _consecutive_fails | 0 | 1951 | 2057 |
| progress | consecutive_retry_levels | _consecutive_retry_levels | 0 | 1952 | 2058 |
| progress | retry_tracking_strategy | _retry_tracking_strategy | 0 | 1953 | 2059 |
| progress | bank_progress | _bank_progress | {} | 1954 | 2060 |
| progress | main_bank_progress | _main_bank_progress | {} | 1955 | 2061 |
| progress | lkmod_progress | _lkmod_progress | {} | 1956 | 2062 |
| progress | has_shown_rate_us | _has_shown_rate_us | false | 1957 | 2063 |
| progress | has_used_revive_free | _has_used_revive_free | false | 1958 | 2064 |
| progress | warn_life_shown | _warn_life_shown | false | 1959 | 2065 |
| progress | life_plus_first_done | _life_plus_first_done | false | 1960 | 2066 |
| progress | daily_index | _daily_index | 0 | 1961 | 2067 |
| progress | daily_completed_date | _daily_completed_date | "" | 1962 | 2068 |
| progress | max_daily_date | _max_daily_date | "" | 1963 | 2069 |
| progress | daily_elapsed_sec | _daily_elapsed_sec | 0 | 1964 | 2070 |
| progress | daily_beat_percent | _daily_beat_percent | 0.0 | 1965 | 2071 |
| progress | daily_best_beat_percent | _daily_best_beat_percent | 0.0 | 1966 | 2072 |
| progress | daily_started_date | _daily_started_date | "" | 1967 | 2073 |
| progress | daily_first_easy_date | _daily_first_easy_date | "" | 1968 | 2074 |
| progress | game_total_stats | _game_total_stats | {} | 1969 | 2075 |
| progress | main_game_total_stats | {} | {} | 1971 | 2076 |
| progress | daily_game_total_stats | {} | {} | 1972 | 2077 |
| progress | main_game_round_stats | {} | {} | 1973 | 2078 |
| progress | daily_game_round_stats | {} | {} | 1974 | 2079 |
| progress | main_game_id | "" | "" | 1975 | 2080 |
| progress | daily_game_id | "" | "" | 1976 | 2081 |
| progress | tool_locate | _tool_locate | 5 | 1977 | 2082 |
| progress | tool_hint | _tool_hint | 5 | 1978 | 2083 |
| progress | tool_undo | _tool_undo | 3 | 1979 | 2084 |
| progress | last_splash_date | _last_splash_date | "" | 1980 | 2085 |
| progress | apply_locale | _apply_locale | "" | 1981 | 2086 |
| progress | is_first_session | _is_first_session | true | 1982 | 2087 |
| progress | last_first_level_date | _last_first_level_date | "" | 1983 | 2088 |
| progress | music_on | _music_on | true | 1984 | 2089 |
| progress | music_user_modified | _music_user_modified | false | 1985 | 2090 |
| progress | sound_on | _sound_on | true | 1986 | 2091 |
| progress | vibration_on | _vibration_on | true | 1987 | 2092 |
| progress | people_on | _people_on | true | 1988 | 2093 |
| progress | has_used_tool | _has_used_tool | false | 1989 | 2094 |
| progress | prop_highlight_shown | _prop_highlight_shown | false | 1990 | 2095 |
| progress | push_ask_count | _push_ask_count | 0 | 1991 | 2096 |
| progress | push_guide_last_date | _push_guide_last_date | "" | 1992 | 2097 |
| progress | push_guide_shown_count | _push_guide_shown_count | 0 | 1993 | 2098 |
| progress | push_guide_popup_count | _push_guide_popup_count | 0 | 1994 | 2099 |
| progress | recent_win_counts_by_day | _recent_win_counts_by_day | {} | 1995 | 2100 |
| progress | retry_puzzle_level | _retry_puzzle_level | 0 | 1996 | 2101 |
| progress | retry_puzzle_params | _retry_puzzle_params | {} | 1997 | 2102 |
| progress | pre_cat_fail_lv | _pre_cat_fail_lv | 0 | 1998 | 2103 |
| progress | pre_cat_fail_count | _pre_cat_fail_count | 0 | 1999 | 2104 |
| progress | pre_cat_revived_this_level | _pre_cat_revived_this_level | false | 2000 | 2105 |
| progress | pre_cat_pending_hard | _pre_cat_pending_hard | false | 2001 | 2106 |
| progress | pre_cat_pending_struggle | _pre_cat_pending_struggle | false | 2002 | 2107 |
| progress | pre_cat_pending_demote | _pre_cat_pending_demote | false | 2003 | 2108 |
| progress | pre_cat_lock_lv | _pre_cat_lock_lv | 0 | 2004 | 2109 |
| progress | pre_cat_lock_pre_type | _pre_cat_lock_pre_type | "0" | 2005 | 2110 |
| progress | pre_cat_lock_pos | _pre_cat_lock_pos | Vector2i(-1, -1 | 2006 | 2111 |
| progress | has_shown_att_guide | _has_shown_att_guide | false | 2007 | 2112 |
| progress | interstitial_unlocked | _interstitial_unlocked | false | 2008 | 2113 |
| progress | banner_unlocked | _banner_unlocked | false | 2009 | 2114 |
| progress | has_shown_draft_onboarding | _has_shown_draft_onboarding | false | 2010 | 2115 |
| progress | auto_mark_tutorial_done | _auto_mark_tutorial_done | false | 2011 | 2116 |
| progress | rule_info_bar_collapsed | _rule_info_bar_collapsed | false | 2012 | 2117 |
| progress | pattern_mode_on | _pattern_mode_on | false | 2013 | 2118 |
| progress | pattern_entry_dot_dismissed | _pattern_entry_dot_dismissed | false | 2015 | 2120 |
| progress | pattern_switch_dot_dismissed | _pattern_switch_dot_dismissed | false | 2016 | 2121 |
| progress | grt_level_d90_reported | _grt_level_d90_reported | [] | 2017 | 2122 |
| progress | grt_reported_events | _grt_reported_events | [] | 2018 | 2123 |
| progress | first_open_time_ms | _first_open_time_ms | 0 | 2019 | 2124 |
| progress | recent_puzzles | _recent_puzzles | [] | 2020 | 2125 |
| progress | endgame_snapshot | {} | {} | 2021 | 2126 |
| progress | session_count | _session_count | 0 | 2022 | 2127 |
| progress | today_session_count | _today_session_count | 0 | 2023 | 2128 |
| progress | last_day_session_count | _last_day_session_count | 0 | 2024 | 2129 |
| progress | active_days | _active_days | 0 | 2025 | 2130 |
| progress | today_played_count | _today_played_count | 0 | 2026 | 2131 |
| progress | today_active_sec | _today_active_sec | 0 | 2027 | 2132 |
| progress | total_active_sec | _total_active_sec | 0 | 2028 | 2133 |
| progress | today_date | _today_date | "" | 2029 | 2134 |
| progress | pending_rewards | _pending_rewards | [] | 2030 | 2135 |
| progress | in_flight_awards | _in_flight_awards | [] | 2031 | 2136 |
| progress | reward_history_ts | _reward_history_ts | [] | 2032 | 2137 |
| progress | restored_today_count | _restored_today_count | 0 | 2033 | 2138 |
| progress | daily_auto_mark_enabled | _daily_auto_mark_enabled | false | 2034 | 2139 |
| progress | daily_auto_mark_free_consumed | _daily_auto_mark_free_consumed | false | 2035 | 2140 |
| progress | saved_game_auto_mark | _saved_game_auto_mark | -1 | 2036 | 2141 |
| progress | saved_ab_groups | _saved_ab_groups | {} | 2037 | 2142 |
| progress | last_win_beat_percent | _last_win_beat_percent | -1.0 | 2038 | 2143 |
| progress | help_last_open_time | _help_last_open_time | 0 | 2039 | 2144 |
| progress | install_version | _install_version | "" | 2040 | 2145 |
| progress | toast_hold_text_sig | _toast_hold_text_sig | -999 | 2041 | 2146 |
| progress | toast_shown_keys | _toast_shown_keys | PackedStringArray( | 2042 | 2147 |

### 4. P0 Keys
| Key | Expr Saved | Default Loaded | Save Line | Load Line |
|---|---|---|---|---|
| current_level | _current_level | 1 | 1946 | 2052 |
| tutorial_done | _tutorial_done | false | 1947 | 2053 |
| current_strategy | _current_strategy | 1 | 1948 | 2054 |
| consecutive_clean_wins | _consecutive_clean_wins | 0 | 1949 | 2055 |
| last_level_clean_win | _last_level_clean_win | false | 1950 | 2056 |
| consecutive_fails | _consecutive_fails | 0 | 1951 | 2057 |
| consecutive_retry_levels | _consecutive_retry_levels | 0 | 1952 | 2058 |
| retry_tracking_strategy | _retry_tracking_strategy | 0 | 1953 | 2059 |
| bank_progress | _bank_progress | {} | 1954 | 2060 |
| main_bank_progress | _main_bank_progress | {} | 1955 | 2061 |
| lkmod_progress | _lkmod_progress | {} | 1956 | 2062 |
| tool_locate | _tool_locate | 5 | 1977 | 2082 |
| tool_hint | _tool_hint | 5 | 1978 | 2083 |
| tool_undo | _tool_undo | 3 | 1979 | 2084 |
| apply_locale | _apply_locale | "" | 1981 | 2086 |
| music_on | _music_on | true | 1984 | 2089 |
| music_user_modified | _music_user_modified | false | 1985 | 2090 |
| sound_on | _sound_on | true | 1986 | 2091 |
| vibration_on | _vibration_on | true | 1987 | 2092 |
| people_on | _people_on | true | 1988 | 2093 |
| retry_puzzle_level | _retry_puzzle_level | 0 | 1996 | 2101 |
| retry_puzzle_params | _retry_puzzle_params | {} | 1997 | 2102 |
| pre_cat_fail_lv | _pre_cat_fail_lv | 0 | 1998 | 2103 |
| pre_cat_fail_count | _pre_cat_fail_count | 0 | 1999 | 2104 |
| pre_cat_revived_this_level | _pre_cat_revived_this_level | false | 2000 | 2105 |
| pre_cat_pending_hard | _pre_cat_pending_hard | false | 2001 | 2106 |
| pre_cat_pending_struggle | _pre_cat_pending_struggle | false | 2002 | 2107 |
| pre_cat_pending_demote | _pre_cat_pending_demote | false | 2003 | 2108 |
| pre_cat_lock_lv | _pre_cat_lock_lv | 0 | 2004 | 2109 |
| pre_cat_lock_pre_type | _pre_cat_lock_pre_type | "0" | 2005 | 2110 |
| pre_cat_lock_pos | _pre_cat_lock_pos | Vector2i(-1, -1 | 2006 | 2111 |
| recent_puzzles | _recent_puzzles | [] | 2020 | 2125 |
| endgame_snapshot | {} | {} | 2021 | 2126 |
| saved_game_auto_mark | _saved_game_auto_mark | -1 | 2036 | 2141 |
| saved_ab_groups | _saved_ab_groups | {} | 2037 | 2142 |

### 5. P0 Getters/Setters
- `current_level`: func get_current_level() -> int: (Line 453)
- `current_level`: func set_current_level(value: int) -> void : (Line 456)
- `tutorial_done`: func set_tutorial_done(value: bool) -> void : (Line 463)
- `current_strategy`: func get_current_strategy() -> int: (Line 725)
- `current_strategy`: func set_current_strategy(value: int) -> void : (Line 728)
- `apply_locale`: func get_apply_locale() -> String: (Line 1151)
- `apply_locale`: func set_apply_locale(value: String) -> void : (Line 1154)
- `music_on`: func set_music_on(value: bool) -> void : (Line 1198)
- `sound_on`: func set_sound_on(value: bool) -> void : (Line 1216)
- `vibration_on`: func set_vibration_on(value: bool) -> void : (Line 1223)
- `people_on`: func set_people_on(value: bool) -> void : (Line 1231)
- `pre_cat_fail_count`: func get_pre_cat_fail_count(lv: int) -> int: (Line 1286)
- `consecutive_clean_wins`: func get_consecutive_clean_wins() -> int: (Line 1460)
- `lkmod_progress`: func get_lkmod_progress(sz: int, rank: int) -> Dictionary: (Line 1497)
- `lkmod_progress`: func set_lkmod_progress(sz: int, rank: int, progress: Dictionary, persist: bool = true) -> void : (Line 1504)
- `bank_progress`: func get_bank_progress_snapshot() -> Dictionary: (Line 1517)
- `bank_progress`: func get_main_bank_progress_snapshot() -> Dictionary: (Line 1520)
- `main_bank_progress`: func get_main_bank_progress_snapshot() -> Dictionary: (Line 1520)
- `lkmod_progress`: func get_lkmod_progress_snapshot() -> Dictionary: (Line 1523)
- `recent_puzzles`: func get_recent_puzzles() -> Array: (Line 1555)
- `endgame_snapshot`: func get_endgame_snapshot() -> Dictionary: (Line 1560)
- `endgame_snapshot`: func set_endgame_snapshot(snapshot: Dictionary) -> void : (Line 1565)

### 6. Call sites of _save_data()
- `_save_data()` called in `func set_current_level(value: int) -> void :` at line 458
- `_save_data()` called in `func set_tutorial_done(value: bool) -> void :` at line 465
- `_save_data()` called in `func mark_revive_free_used() -> void :` at line 477
- `_save_data()` called in `func evaluate_daily_first_easy() -> void :` at line 512
- `_save_data()` called in `func consume_daily_first_easy() -> void :` at line 523
- `_save_data()` called in `func advance_daily_first_easy_date() -> void :` at line 538
- `_save_data()` called in `func cheat_reset_daily_first_easy() -> void :` at line 544
- `_save_data()` called in `func mark_rate_us_shown() -> void :` at line 573
- `_save_data()` called in `func reset_rate_us_shown() -> void :` at line 580
- `_save_data()` called in `func mark_warn_life_shown() -> void :` at line 590
- `_save_data()` called in `func reset_warn_life_shown() -> void :` at line 597
- `_save_data()` called in `func mark_att_guide_shown() -> void :` at line 607
- `_save_data()` called in `func mark_interstitial_unlocked() -> void :` at line 617
- `_save_data()` called in `func mark_banner_unlocked() -> void :` at line 627
- `_save_data()` called in `func set_pattern_mode_on(value: bool) -> void :` at line 636
- `_save_data()` called in `func mark_pattern_entry_dot_dismissed() -> void :` at line 647
- `_save_data()` called in `func mark_pattern_switch_dot_dismissed() -> void :` at line 657
- `_save_data()` called in `func set_saved_ab_group(key: String, v: int) -> void :` at line 669
- `_save_data()` called in `func set_rule_info_bar_collapsed(value: bool) -> void :` at line 678
- `_save_data()` called in `func mark_grt_level_d90_reported(level: int) -> void :` at line 691
- `_save_data()` called in `func mark_grt_event_reported(event_name: String) -> void :` at line 704
- `_save_data()` called in `func ensure_first_open_time(sdk_value_ms: int) -> void :` at line 723
- `_save_data()` called in `func set_current_strategy(value: int) -> void :` at line 730
- `_save_data()` called in `func set_daily_started_date(date: String) -> void :` at line 742
- `_save_data()` called in `func advance_max_daily_date(date: String) -> void :` at line 820
- `_save_data()` called in `func mark_daily_completed(date: String, elapsed_sec: int, beat_percent: float) -> void :` at line 840
- `_save_data()` called in `func clear_daily_completion() -> void :` at line 848
- `_save_data()` called in `func on_session_started() -> void :` at line 887
- `_save_data()` called in `func on_game_finished() -> void :` at line 916
- `_save_data()` called in `func add_today_active_sec(delta_sec: int) -> void :` at line 933
- `_save_data()` called in `func add_pending_reward(reward: Dictionary) -> void :` at line 968
- `_save_data()` called in `func pop_all_pending_rewards() -> Array:` at line 974
- `_save_data()` called in `func record_normal_reward(ts: int) -> void :` at line 987
- `_save_data()` called in `func add_restored_today_count(n: int) -> void :` at line 1018
- `_save_data()` called in `func remove_pending_rewards(entries: Array) -> void :` at line 1025
- `_save_data()` called in `func mark_prop_highlight_shown() -> void :` at line 1055
- `_save_data()` called in `func inc_push_ask_count() -> void :` at line 1062
- `_save_data()` called in `func mark_push_guide_triggered() -> void :` at line 1070
- `_save_data()` called in `func mark_push_guide_popup_shown() -> void :` at line 1081
- `_save_data()` called in `func set_tool_count(kind: String, count: int) -> void :` at line 1111
- `_save_data()` called in `func add_in_flight_award(entry: Dictionary) -> void :` at line 1126
- `_save_data()` called in `func remove_in_flight_award(uid: int) -> void :` at line 1133
- `_save_data()` called in `func set_last_splash_date(value: String) -> void :` at line 1148
- `_save_data()` called in `func set_apply_locale(value: String) -> void :` at line 1156
- `_save_data()` called in `func consume_first_session_persist() -> void :` at line 1180
- `_save_data()` called in `func consume_today_first_level() -> void :` at line 1193
- `_save_data()` called in `func set_music_on(value: bool) -> void :` at line 1201
- `_save_data()` called in `func init_music_default(default_on: bool) -> void :` at line 1211
- `_save_data()` called in `func set_sound_on(value: bool) -> void :` at line 1218
- `_save_data()` called in `func set_vibration_on(value: bool) -> void :` at line 1226
- `_save_data()` called in `func set_people_on(value: bool) -> void :` at line 1233
- `_save_data()` called in `func set_retry_puzzle(level: int, params: Dictionary) -> void :` at line 1276
- `_save_data()` called in `func mark_pre_cat_revived() -> void :` at line 1295
- `_save_data()` called in `func consume_pre_cat_pending() -> Dictionary:` at line 1309
- `_save_data()` called in `func set_pre_cat_lock(lv: int, pre_type: String, position: Vector2i) -> void :` at line 1323
- `_save_data()` called in `func set_toast_hold_text_sig(v: int) -> void :` at line 1399
- `_save_data()` called in `func add_toast_shown_key(key: String) -> void :` at line 1409
- `_save_data()` called in `func clear_toast_shown_keys() -> void :` at line 1413
- `_save_data()` called in `func set_last_win_beat_percent(pct: float) -> void :` at line 1434
- `_save_data()` called in `func set_help_last_open_time(value: int) -> void :` at line 1441
- `_save_data()` called in `func ensure_install_version(version: String) -> void :` at line 1455
- `_save_data()` called in `func advance_bank_index(sz: int, rank: int, tier: String = "", persist: bool = true) -> void :` at line 1480
- `_save_data()` called in `func set_main_progress(sz: int, rank: int, tier: String, progress: Dictionary, persist: bool = true) -> void :` at line 1494
- `_save_data()` called in `func set_lkmod_progress(sz: int, rank: int, progress: Dictionary, persist: bool = true) -> void :` at line 1508
- `_save_data()` called in `func commit_bank_progress() -> void :` at line 1514
- `_save_data()` called in `func record_puzzle(puzzle_id: String, level: int, version: String = "", src: String = "") -> Dictionary:` at line 1552
- `_save_data()` called in `func _apply_level_won(level_num: int) -> void :` at line 1746
- `_save_data()` called in `func _apply_level_failed(level_num: int) -> void :` at line 1777
- `_save_data()` called in `func cheat_jump_to_level(level: int) -> void :` at line 1878
- `_save_data()` called in `func merge_remote(remote: Dictionary, ctx: Dictionary = {}) -> void :` at line 1934
- `_save_data()` called in `func reset_all() -> void :` at line 2283

### 7. SaveStore Methods
- `_init(password: String, dir: String, dual_slot: bool, path_a: String, path_b: String = "", flag_path: String = "", legacy_path: String = "") -> void :` (Line 22)
- `save_config(cfg: ConfigFile) -> bool:` (Line 32)
- `load_config() -> ConfigFile:` (Line 45)
- `remove() -> void :` (Line 55)
- `_load_once() -> ConfigFile:` (Line 60)
- `_atomic_write(cfg: ConfigFile, final_path: String) -> bool:` (Line 89)
- `_read_flag() -> String:` (Line 99)
- `_write_flag(slot: String) -> void :` (Line 108)

BẤT THƯỜNG:
- Không có bất thường

GIỚI HẠN CỦA BÁO CÁO:
- Lọc theo phương pháp Regex đơn giản, các setter không chứa trực tiếp tiền tố `set_` theo tên key sẽ không được liệt kê hết (yêu cầu phân tích AST đầy đủ của GDScript).

KHÔNG BAO GỒM:
- Không viết code.
- Không sửa file.
- Không đề xuất kiến trúc.
- Không tự đánh dấu roadmap hoàn thành.
