using System;
using System.Collections.Generic;
using UnityEngine;

namespace Meowdoku.Core
{
    /// <summary>
    /// Typed P0 persistence slice ported from game_state.gd.
    /// Serialized keys intentionally retain the source snake_case names.
    /// </summary>
    public sealed class GameStateData
    {
        public int CurrentLevel { get; set; } = 1;
        public bool IsFirstSession { get; set; } = true;
        public bool TutorialDone { get; set; }
        public int CurrentStrategy { get; set; } = 1;
        public int ConsecutiveCleanWins { get; set; }
        public bool LastLevelCleanWin { get; set; }
        public int ConsecutiveFails { get; set; }
        public int ConsecutiveRetryLevels { get; set; }
        public int RetryTrackingStrategy { get; set; }
        public int DailyIndex { get; set; }
        public string DailyCompletedDate { get; set; } = string.Empty;
        public string MaxDailyDate { get; set; } = string.Empty;
        public int DailyElapsedSeconds { get; set; }
        public float DailyBeatPercent { get; set; }
        public float DailyBestBeatPercent { get; set; }
        public string DailyStartedDate { get; set; } = string.Empty;
        public string DailyFirstEasyDate { get; set; } = string.Empty;
        public Dictionary<string, object> RecentWinCountsByDay { get; set; } =
            new Dictionary<string, object>();
        public int SessionCount { get; set; }
        public int TodaySessionCount { get; set; }
        public int LastDaySessionCount { get; set; }
        public int ActiveDays { get; set; }
        public int TodayPlayedCount { get; set; }
        public int TodayActiveSeconds { get; set; }
        public int TotalActiveSeconds { get; set; }
        public List<int> GrtLevelD90Reported { get; set; } = new();
        public List<string> GrtReportedEvents { get; set; } = new();
        public long FirstOpenTimeMs { get; set; }
        public string TodayDate { get; set; } = string.Empty;

        public Dictionary<string, object> BankProgress { get; set; } =
            new Dictionary<string, object>();
        public Dictionary<string, object> MainBankProgress { get; set; } =
            new Dictionary<string, object>();
        public Dictionary<string, object> LkModifiedProgress { get; set; } =
            new Dictionary<string, object>();

        public int ToolLocate { get; set; } = 5;
        public int ToolHint { get; set; } = 5;
        public int ToolUndo { get; set; } = 3;
        public string LastSplashDate { get; set; } = string.Empty;
        public bool HasUsedTool { get; set; }
        public bool PropHighlightShown { get; set; }
        public int PushAskCount { get; set; }
        public string PushGuideLastDate { get; set; } = string.Empty;
        public int PushGuideShownCount { get; set; }
        public int PushGuidePopupCount { get; set; }
        public bool HasShownAttGuide { get; set; }
        public string AppliedLocale { get; set; } = string.Empty;
        public bool MusicOn { get; set; } = true;
        public bool MusicUserModified { get; set; }
        public bool SoundOn { get; set; } = true;
        public bool VibrationOn { get; set; } = true;
        public bool PeopleOn { get; set; } = true;
        public bool PatternModeOn { get; set; }
        public bool PatternEntryDotDismissed { get; set; }
        public bool PatternSwitchDotDismissed { get; set; }
        public bool HasUsedReviveFree { get; set; }
        public bool InterstitialUnlocked { get; set; }
        public bool BannerUnlocked { get; set; }
        public float LastWinBeatPercent { get; set; } = -1f;

        public int RetryPuzzleLevel { get; set; }
        public Dictionary<string, object> RetryPuzzleParameters { get; set; } =
            new Dictionary<string, object>();

        public int PreCatFailLevel { get; set; }
        public int PreCatFailCount { get; set; }
        public bool PreCatRevivedThisLevel { get; set; }
        public bool PreCatPendingHard { get; set; }
        public bool PreCatPendingStruggle { get; set; }
        public bool PreCatPendingDemote { get; set; }
        public int PreCatLockLevel { get; set; }
        public string PreCatLockType { get; set; } = "0";
        public Vector2Int PreCatLockPosition { get; set; } = new Vector2Int(-1, -1);

        public List<object> RecentPuzzles { get; set; } = new List<object>();
        public List<object> InFlightAwards { get; set; } = new List<object>();
        public List<object> PendingRewards { get; set; } = new List<object>();
        public List<object> RewardHistoryTimestamps { get; set; } =
            new List<object>();
        public int RestoredTodayCount { get; set; }
        public int SavedGameAutoMark { get; set; } = -1;
        public Dictionary<string, object> SavedAbGroups { get; set; } =
            new Dictionary<string, object>();

        public Dictionary<string, object> EndgameSnapshot { get; set; } =
            new Dictionary<string, object>();
        public Dictionary<string, object> MainGameTotalStats { get; set; } =
            new Dictionary<string, object>();
        public Dictionary<string, object> DailyGameTotalStats { get; set; } =
            new Dictionary<string, object>();
        public Dictionary<string, object> MainGameRoundStats { get; set; } =
            new Dictionary<string, object>();
        public Dictionary<string, object> DailyGameRoundStats { get; set; } =
            new Dictionary<string, object>();
        public string MainGameId { get; set; } = string.Empty;
        public string DailyGameId { get; set; } = string.Empty;

        public Dictionary<string, object> ToPlayerDocument()
        {
            var progress = new Dictionary<string, object>
            {
                { "current_level", CurrentLevel },
                { "is_first_session", IsFirstSession },
                { "tutorial_done", TutorialDone },
                { "current_strategy", CurrentStrategy },
                { "consecutive_clean_wins", ConsecutiveCleanWins },
                { "last_level_clean_win", LastLevelCleanWin },
                { "consecutive_fails", ConsecutiveFails },
                { "consecutive_retry_levels", ConsecutiveRetryLevels },
                { "retry_tracking_strategy", RetryTrackingStrategy },
                { "daily_index", DailyIndex },
                { "daily_completed_date", DailyCompletedDate },
                { "max_daily_date", MaxDailyDate },
                { "daily_elapsed_sec", DailyElapsedSeconds },
                { "daily_beat_percent", DailyBeatPercent },
                { "daily_best_beat_percent", DailyBestBeatPercent },
                { "daily_started_date", DailyStartedDate },
                { "daily_first_easy_date", DailyFirstEasyDate },
                { "recent_win_counts_by_day", RecentWinCountsByDay },
                { "session_count", SessionCount },
                { "today_session_count", TodaySessionCount },
                { "last_day_session_count", LastDaySessionCount },
                { "active_days", ActiveDays },
                { "today_played_count", TodayPlayedCount },
                { "today_active_sec", TodayActiveSeconds },
                { "total_active_sec", TotalActiveSeconds },
                { "grt_level_d90_reported", ToObjects(GrtLevelD90Reported) },
                { "grt_reported_events", ToObjects(GrtReportedEvents) },
                { "first_open_time_ms", FirstOpenTimeMs },
                { "today_date", TodayDate },
                { "bank_progress", BankProgress },
                { "main_bank_progress", MainBankProgress },
                { "lkmod_progress", LkModifiedProgress },
                { "tool_locate", ToolLocate },
                { "tool_hint", ToolHint },
                { "tool_undo", ToolUndo },
                { "last_splash_date", LastSplashDate },
                { "has_used_tool", HasUsedTool },
                { "prop_highlight_shown", PropHighlightShown },
                { "push_ask_count", PushAskCount },
                { "push_guide_last_date", PushGuideLastDate },
                { "push_guide_shown_count", PushGuideShownCount },
                { "push_guide_popup_count", PushGuidePopupCount },
                { "has_shown_att_guide", HasShownAttGuide },
                { "apply_locale", AppliedLocale },
                { "music_on", MusicOn },
                { "music_user_modified", MusicUserModified },
                { "sound_on", SoundOn },
                { "vibration_on", VibrationOn },
                { "people_on", PeopleOn },
                { "pattern_mode_on", PatternModeOn },
                { "pattern_entry_dot_dismissed", PatternEntryDotDismissed },
                { "pattern_switch_dot_dismissed", PatternSwitchDotDismissed },
                { "has_used_revive_free", HasUsedReviveFree },
                { "interstitial_unlocked", InterstitialUnlocked },
                { "banner_unlocked", BannerUnlocked },
                { "last_win_beat_percent", LastWinBeatPercent },
                { "retry_puzzle_level", RetryPuzzleLevel },
                { "retry_puzzle_params", RetryPuzzleParameters },
                { "pre_cat_fail_lv", PreCatFailLevel },
                { "pre_cat_fail_count", PreCatFailCount },
                { "pre_cat_revived_this_level", PreCatRevivedThisLevel },
                { "pre_cat_pending_hard", PreCatPendingHard },
                { "pre_cat_pending_struggle", PreCatPendingStruggle },
                { "pre_cat_pending_demote", PreCatPendingDemote },
                { "pre_cat_lock_lv", PreCatLockLevel },
                { "pre_cat_lock_pre_type", PreCatLockType },
                {
                    "pre_cat_lock_pos",
                    new Dictionary<string, object>
                    {
                        { "x", PreCatLockPosition.x },
                        { "y", PreCatLockPosition.y }
                    }
                },
                { "recent_puzzles", RecentPuzzles },
                { "in_flight_awards", InFlightAwards },
                { "pending_rewards", PendingRewards },
                { "reward_history_ts", RewardHistoryTimestamps },
                { "restored_today_count", RestoredTodayCount },
                { "endgame_snapshot", new Dictionary<string, object>() },
                { "saved_game_auto_mark", SavedGameAutoMark },
                { "saved_ab_groups", SavedAbGroups },

                // The source keeps these legacy player-save keys empty because
                // live values are stored in the separate endgame file.
                { "main_game_total_stats", new Dictionary<string, object>() },
                { "daily_game_total_stats", new Dictionary<string, object>() },
                { "main_game_round_stats", new Dictionary<string, object>() },
                { "daily_game_round_stats", new Dictionary<string, object>() },
                { "main_game_id", string.Empty },
                { "daily_game_id", string.Empty }
            };

            return new Dictionary<string, object> { { "progress", progress } };
        }

        public Dictionary<string, object> ToEndgameDocument()
        {
            return new Dictionary<string, object>
            {
                {
                    "snapshot",
                    new Dictionary<string, object> { { "data", EndgameSnapshot } }
                },
                {
                    "stats",
                    new Dictionary<string, object>
                    {
                        { "main_total", MainGameTotalStats },
                        { "daily_total", DailyGameTotalStats },
                        { "main_round", MainGameRoundStats },
                        { "daily_round", DailyGameRoundStats },
                        { "main_id", MainGameId },
                        { "daily_id", DailyGameId }
                    }
                }
            };
        }

        public bool IsEndgameStoreEmpty()
        {
            return EndgameSnapshot.Count == 0 &&
                   MainGameTotalStats.Count == 0 &&
                   DailyGameTotalStats.Count == 0 &&
                   MainGameRoundStats.Count == 0 &&
                   DailyGameRoundStats.Count == 0 &&
                   string.IsNullOrEmpty(MainGameId) &&
                   string.IsNullOrEmpty(DailyGameId);
        }

        public static GameStateData FromDocuments(
            Dictionary<string, object> playerDocument,
            Dictionary<string, object> endgameDocument)
        {
            var data = new GameStateData();
            Dictionary<string, object> progress = Section(playerDocument, "progress");
            if (progress != null)
            {
                data.CurrentLevel = Int(progress, "current_level", 1);
                data.IsFirstSession = Bool(progress, "is_first_session", true);
                data.TutorialDone = Bool(progress, "tutorial_done", false);
                data.CurrentStrategy = Int(progress, "current_strategy", 1);
                data.ConsecutiveCleanWins = Int(progress, "consecutive_clean_wins", 0);
                data.LastLevelCleanWin = Bool(progress, "last_level_clean_win", false);
                data.ConsecutiveFails = Int(progress, "consecutive_fails", 0);
                data.ConsecutiveRetryLevels = Int(progress, "consecutive_retry_levels", 0);
                data.RetryTrackingStrategy = Int(progress, "retry_tracking_strategy", 0);
                data.DailyIndex = Int(progress, "daily_index", 0);
                data.DailyCompletedDate = String(
                    progress,
                    "daily_completed_date",
                    string.Empty);
                data.MaxDailyDate = String(
                    progress,
                    "max_daily_date",
                    string.Empty);
                data.DailyElapsedSeconds = Int(progress, "daily_elapsed_sec", 0);
                data.DailyBeatPercent = Float(progress, "daily_beat_percent", 0f);
                data.DailyBestBeatPercent = Float(
                    progress,
                    "daily_best_beat_percent",
                    0f);
                data.DailyStartedDate = String(
                    progress,
                    "daily_started_date",
                    string.Empty);
                data.DailyFirstEasyDate = String(progress, "daily_first_easy_date", string.Empty);
                data.RecentWinCountsByDay = Dictionary(progress, "recent_win_counts_by_day");
                data.SessionCount = Int(progress, "session_count", 0);
                data.TodaySessionCount = Int(progress, "today_session_count", 0);
                data.LastDaySessionCount = Int(progress, "last_day_session_count", 0);
                data.ActiveDays = Int(progress, "active_days", 0);
                data.TodayPlayedCount = Int(progress, "today_played_count", 0);
                data.TodayActiveSeconds = Int(progress, "today_active_sec", 0);
                data.TotalActiveSeconds = Int(progress, "total_active_sec", 0);
                data.GrtLevelD90Reported =
                    IntList(progress, "grt_level_d90_reported");
                data.GrtReportedEvents =
                    StringList(progress, "grt_reported_events");
                data.FirstOpenTimeMs = Long(
                    progress,
                    "first_open_time_ms",
                    0L);
                data.TodayDate = String(progress, "today_date", string.Empty);
                data.BankProgress = Dictionary(progress, "bank_progress");
                data.MainBankProgress = Dictionary(progress, "main_bank_progress");
                data.LkModifiedProgress = Dictionary(progress, "lkmod_progress");
                data.ToolLocate = Int(progress, "tool_locate", 5);
                data.ToolHint = Int(progress, "tool_hint", 5);
                data.ToolUndo = Int(progress, "tool_undo", 3);
                data.LastSplashDate = String(
                    progress,
                    "last_splash_date",
                    string.Empty);
                data.HasUsedTool = Bool(progress, "has_used_tool", false);
                data.PropHighlightShown = Bool(progress, "prop_highlight_shown", false);
                data.PushAskCount = Int(progress, "push_ask_count", 0);
                data.PushGuideLastDate = String(
                    progress,
                    "push_guide_last_date",
                    string.Empty);
                data.PushGuideShownCount = Int(
                    progress,
                    "push_guide_shown_count",
                    0);
                data.PushGuidePopupCount = Int(
                    progress,
                    "push_guide_popup_count",
                    0);
                data.HasShownAttGuide = Bool(
                    progress,
                    "has_shown_att_guide",
                    false);
                data.AppliedLocale = String(progress, "apply_locale", string.Empty);
                data.MusicOn = Bool(progress, "music_on", true);
                data.MusicUserModified = Bool(progress, "music_user_modified", false);
                data.SoundOn = Bool(progress, "sound_on", true);
                data.VibrationOn = Bool(progress, "vibration_on", true);
                data.PeopleOn = Bool(progress, "people_on", true);
                data.PatternModeOn = Bool(progress, "pattern_mode_on", false);
                data.PatternEntryDotDismissed = Bool(
                    progress,
                    "pattern_entry_dot_dismissed",
                    false);
                data.PatternSwitchDotDismissed = Bool(
                    progress,
                    "pattern_switch_dot_dismissed",
                    false);
                data.HasUsedReviveFree = Bool(
                    progress,
                    "has_used_revive_free",
                    false);
                data.InterstitialUnlocked = Bool(
                    progress,
                    "interstitial_unlocked",
                    false);
                data.BannerUnlocked = Bool(
                    progress,
                    "banner_unlocked",
                    false);
                data.LastWinBeatPercent = Float(
                    progress,
                    "last_win_beat_percent",
                    -1f);
                data.RetryPuzzleLevel = Int(progress, "retry_puzzle_level", 0);
                data.RetryPuzzleParameters = Dictionary(progress, "retry_puzzle_params");
                data.PreCatFailLevel = Int(progress, "pre_cat_fail_lv", 0);
                data.PreCatFailCount = Int(progress, "pre_cat_fail_count", 0);
                data.PreCatRevivedThisLevel = Bool(
                    progress,
                    "pre_cat_revived_this_level",
                    false);
                data.PreCatPendingHard = Bool(progress, "pre_cat_pending_hard", false);
                data.PreCatPendingStruggle = Bool(
                    progress,
                    "pre_cat_pending_struggle",
                    false);
                data.PreCatPendingDemote = Bool(progress, "pre_cat_pending_demote", false);
                data.PreCatLockLevel = Int(progress, "pre_cat_lock_lv", 0);
                data.PreCatLockType = String(progress, "pre_cat_lock_pre_type", "0");
                data.PreCatLockPosition = Position(progress, "pre_cat_lock_pos");
                data.RecentPuzzles = List(progress, "recent_puzzles");
                data.InFlightAwards = List(progress, "in_flight_awards");
                data.PendingRewards = List(progress, "pending_rewards");
                data.RewardHistoryTimestamps =
                    List(progress, "reward_history_ts");
                data.RestoredTodayCount = Int(
                    progress,
                    "restored_today_count",
                    0);
                data.SavedGameAutoMark = Int(progress, "saved_game_auto_mark", -1);
                data.SavedAbGroups = Dictionary(progress, "saved_ab_groups");
            }

            Dictionary<string, object> snapshot = Section(endgameDocument, "snapshot");
            Dictionary<string, object> stats = Section(endgameDocument, "stats");
            if (snapshot != null)
            {
                data.EndgameSnapshot = Dictionary(snapshot, "data");
            }
            if (stats != null)
            {
                data.MainGameTotalStats = Dictionary(stats, "main_total");
                data.DailyGameTotalStats = Dictionary(stats, "daily_total");
                data.MainGameRoundStats = Dictionary(stats, "main_round");
                data.DailyGameRoundStats = Dictionary(stats, "daily_round");
                data.MainGameId = String(stats, "main_id", string.Empty);
                data.DailyGameId = String(stats, "daily_id", string.Empty);
            }

            return data;
        }

        private static Dictionary<string, object> Section(
            Dictionary<string, object> document,
            string name)
        {
            if (document != null &&
                document.TryGetValue(name, out object value) &&
                value is Dictionary<string, object> section)
            {
                return section;
            }
            return null;
        }

        private static int Int(Dictionary<string, object> values, string key, int fallback)
        {
            if (!values.TryGetValue(key, out object value) || value == null) return fallback;
            try { return Convert.ToInt32(value); }
            catch (Exception exception) when (
                exception is FormatException ||
                exception is InvalidCastException ||
                exception is OverflowException)
            {
                return fallback;
            }
        }

        private static long Long(
            Dictionary<string, object> values,
            string key,
            long fallback)
        {
            if (!values.TryGetValue(key, out object value) || value == null)
                return fallback;
            try { return Convert.ToInt64(value); }
            catch (Exception exception) when (
                exception is FormatException ||
                exception is InvalidCastException ||
                exception is OverflowException)
            {
                return fallback;
            }
        }

        private static bool Bool(Dictionary<string, object> values, string key, bool fallback)
        {
            return values.TryGetValue(key, out object value) && value is bool result
                ? result
                : fallback;
        }

        private static float Float(
            Dictionary<string, object> values,
            string key,
            float fallback)
        {
            if (!values.TryGetValue(key, out object value) || value == null)
                return fallback;
            try { return Convert.ToSingle(value); }
            catch (Exception exception) when (
                exception is FormatException ||
                exception is InvalidCastException ||
                exception is OverflowException)
            {
                return fallback;
            }
        }

        private static string String(
            Dictionary<string, object> values,
            string key,
            string fallback)
        {
            return values.TryGetValue(key, out object value) && value is string result
                ? result
                : fallback;
        }

        private static Dictionary<string, object> Dictionary(
            Dictionary<string, object> values,
            string key)
        {
            return values.TryGetValue(key, out object value) &&
                   value is Dictionary<string, object> result
                ? result
                : new Dictionary<string, object>();
        }

        private static List<object> List(Dictionary<string, object> values, string key)
        {
            return values.TryGetValue(key, out object value) && value is List<object> result
                ? result
                : new List<object>();
        }

        private static List<int> IntList(
            Dictionary<string, object> values,
            string key)
        {
            var result = new List<int>();
            List<object> source = List(values, key);
            for (int index = 0; index < source.Count; index++)
            {
                try { result.Add(Convert.ToInt32(source[index])); }
                catch (Exception) { }
            }
            return result;
        }

        private static List<string> StringList(
            Dictionary<string, object> values,
            string key)
        {
            var result = new List<string>();
            List<object> source = List(values, key);
            for (int index = 0; index < source.Count; index++)
                if (source[index] is string value &&
                    !string.IsNullOrEmpty(value))
                    result.Add(value);
            return result;
        }

        private static List<object> ToObjects<T>(IReadOnlyList<T> values)
        {
            var result = new List<object>();
            if (values == null) return result;
            for (int index = 0; index < values.Count; index++)
                result.Add(values[index]);
            return result;
        }

        private static Vector2Int Position(Dictionary<string, object> values, string key)
        {
            Dictionary<string, object> position = Dictionary(values, key);
            return position.Count == 0
                ? new Vector2Int(-1, -1)
                : new Vector2Int(Int(position, "x", -1), Int(position, "y", -1));
        }
    }
}
