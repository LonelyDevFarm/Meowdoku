using System;
using System.Collections.Generic;

namespace Meowdoku.Core.Config
{
    public static class AbConfigTiming
    {
        public const string AppStart = "app_start";
        public const string OpenSetting = "open_setting";
        public const string GameStart = "game_start";
        public const string GameStartNormal = "game_start_normal";
        public const string GameStartNormal11 = "game_start_normal_11";
        public const string GameStartNormal21 = "game_start_normal_21";
        public const string GameStartDaily = "game_start_dc";
        public const string GameEnd = "game_end";
    }

    public sealed class AbConfigDefinition
    {
        public AbConfigDefinition(
            string key,
            object defaultValue,
            string timing,
            bool registeredBySource = true)
        {
            Key = key;
            DefaultValue = defaultValue;
            Timing = timing;
            RegisteredBySource = registeredBySource;
        }

        public string Key { get; }
        public object DefaultValue { get; }
        public string Timing { get; }
        public bool RegisteredBySource { get; }
    }

    public static class DefaultConfigProfile
    {
        private static readonly AbConfigDefinition[] Definitions =
        {
            new AbConfigDefinition("region_color", 2, AbConfigTiming.AppStart),
            new AbConfigDefinition("size_cycle", 2, AbConfigTiming.GameStartNormal),
            new AbConfigDefinition("rule_highlight", 0, AbConfigTiming.GameStart),
            new AbConfigDefinition("rule_text", 0, AbConfigTiming.GameStart),
            new AbConfigDefinition("goal_emphasis", 0, AbConfigTiming.GameStartNormal11),
            new AbConfigDefinition("auto_complete", 0, AbConfigTiming.GameStart),
            new AbConfigDefinition("error_feedback", 0, AbConfigTiming.GameStart),
            new AbConfigDefinition("swipe_protect", 0, AbConfigTiming.GameStart),
            new AbConfigDefinition("dda_rank", 0, AbConfigTiming.GameStartNormal),
            new AbConfigDefinition("revive_life", 0, AbConfigTiming.GameStart),
            new AbConfigDefinition("revive_free_logic", 0, AbConfigTiming.AppStart),
            new AbConfigDefinition("life_icon", 1, AbConfigTiming.AppStart),
            new AbConfigDefinition("single_region_num", 2, AbConfigTiming.GameStartNormal),
            new AbConfigDefinition("board_size_big", 0, AbConfigTiming.GameStart),
            new AbConfigDefinition("score_encourage", 0, AbConfigTiming.GameStart),
            new AbConfigDefinition("pre_cat", 0, AbConfigTiming.GameStartNormal21),
            new AbConfigDefinition("game_grid_ui", 0, AbConfigTiming.AppStart),
            new AbConfigDefinition("hint_cat", 0, AbConfigTiming.AppStart),
            new AbConfigDefinition("doubletap_protect", 0, AbConfigTiming.AppStart),
            new AbConfigDefinition("tutorial_diagonal", 0, AbConfigTiming.AppStart),
            new AbConfigDefinition("guide_feedback", 0, AbConfigTiming.AppStart),
            new AbConfigDefinition("vibrate_combo", 0, AbConfigTiming.GameStart),
            new AbConfigDefinition("combo_text", 0, AbConfigTiming.GameStart),
            new AbConfigDefinition("combo_voice", 6, AbConfigTiming.GameStart),
            new AbConfigDefinition("meow_feedback", 0, AbConfigTiming.AppStart),
            new AbConfigDefinition("thumb_up", 0, AbConfigTiming.GameStart),
            new AbConfigDefinition("daily_streak", 1, AbConfigTiming.AppStart),
            new AbConfigDefinition("leaderboard_func", 0, AbConfigTiming.AppStart),
            new AbConfigDefinition("hard_button", 0, AbConfigTiming.AppStart),
            new AbConfigDefinition("settings_language", 0, AbConfigTiming.OpenSetting),
            new AbConfigDefinition("blind_mod", 0, AbConfigTiming.GameStart),
            new AbConfigDefinition(
                "living_days",
                LivingDaysConfig.DefaultSegments,
                AbConfigTiming.GameStart),
            new AbConfigDefinition("undo_btn", 0, AbConfigTiming.GameStart, false),
            new AbConfigDefinition("game_auto_mark", 0, AbConfigTiming.GameStart, false),
            new AbConfigDefinition("game_life_rule", 0, AbConfigTiming.AppStart, false),
            new AbConfigDefinition("wrong_cat_effect", 0, AbConfigTiming.GameStart, false),
            new AbConfigDefinition("reward_unlock_level", 0, AbConfigTiming.GameStart),
            new AbConfigDefinition("prop_highlight", 2, AbConfigTiming.GameStart),
            new AbConfigDefinition("mark_sound", 0, AbConfigTiming.AppStart),
            new AbConfigDefinition("pass_page", 0, AbConfigTiming.GameStart),
            new AbConfigDefinition("pass_text", 0, AbConfigTiming.GameStart),
            new AbConfigDefinition("fail_text", 0, AbConfigTiming.GameEnd),
            new AbConfigDefinition("win_toast", 0, AbConfigTiming.GameStart),
            new AbConfigDefinition("dc_level", 0, AbConfigTiming.GameStartDaily),
            new AbConfigDefinition("no_dc", 0, AbConfigTiming.AppStart, false),
            new AbConfigDefinition("dc_tag_ui", 0, AbConfigTiming.AppStart, false)
            ,new AbConfigDefinition("inter_unlock_level", 11, AbConfigTiming.GameStart)
            ,new AbConfigDefinition("inter_unlock_session", 2, AbConfigTiming.GameStart)
            ,new AbConfigDefinition("inter_unlock_memory", 300, AbConfigTiming.GameStart)
            ,new AbConfigDefinition("inter_cd_lc", "{60}", AbConfigTiming.GameStart)
            ,new AbConfigDefinition("inter_extra_protect_lc", "{session_game_2}", AbConfigTiming.GameStart)
            ,new AbConfigDefinition("banner_unlock_session", 2, AbConfigTiming.GameStart)
            ,new AbConfigDefinition("banner_unlock_level", 11, AbConfigTiming.GameStart)
            ,new AbConfigDefinition("banner_extra_protect_lc", "{no}", AbConfigTiming.GameStart)
            ,new AbConfigDefinition("banner_unlock_diff_lc", "{all}", AbConfigTiming.GameStart)
            ,new AbConfigDefinition("common_rewardad_logic", 0, AbConfigTiming.GameStart)
        };

        private static readonly Dictionary<string, AbConfigDefinition> ByKey = BuildIndex();

        public static IReadOnlyList<AbConfigDefinition> All => Definitions;

        public static bool TryGet(string key, out AbConfigDefinition definition)
        {
            return ByKey.TryGetValue(key, out definition);
        }

        public static AbConfigDefinition Get(string key)
        {
            if (!TryGet(key, out AbConfigDefinition definition))
                throw new KeyNotFoundException($"Unknown source AB config key: {key}");
            return definition;
        }

        private static Dictionary<string, AbConfigDefinition> BuildIndex()
        {
            var result = new Dictionary<string, AbConfigDefinition>(StringComparer.Ordinal);
            for (int i = 0; i < Definitions.Length; i++)
                result.Add(Definitions[i].Key, Definitions[i]);
            return result;
        }
    }
}
