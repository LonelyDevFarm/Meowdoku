# PROJECT: UNITY

## PATH: Assets\_Project\Scripts\Core\GameStateData.cs
``csharp
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

``

## PATH: Assets\_Project\Scripts\Core\GameStateService.cs
``csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using Meowdoku.Core.Config;
using Meowdoku.Core.Daily;
using Meowdoku.Core.Online;

namespace Meowdoku.Core
{
    public interface IVibrationStateSink
    {
        void SetEnabled(bool enabled);
    }

    public interface ICurrentDateProvider
    {
        string CurrentDate { get; }
    }

    public sealed class SystemCurrentDateProvider : ICurrentDateProvider
    {
        public static readonly SystemCurrentDateProvider Instance = new SystemCurrentDateProvider();
        private SystemCurrentDateProvider() { }
        public string CurrentDate => DateTime.Now.ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// Runtime mutation slice ported from the bank-progress API in game_state.gd.
    /// </summary>
    public sealed class GameStateService :
        IDataSyncSavable,
        IDataSyncMergeBasis
    {
        private const int RecentPuzzlesLimit = 100;
        private const long RewardHistoryRetainSeconds = 7 * 24 * 3600;
        private const long RestoreNormalLookbackSeconds = 3 * 24 * 3600;
        private const int RestoreMinimumNormalRewards = 3;
        private const int RestoreDailyMaximum = 3;
        private readonly IGameStatePlayerStore _store;
        private readonly IGameStateEndgameStore _endgameStore;
        private readonly IVibrationStateSink _vibrationSink;
        private readonly string _applicationVersion;
        private readonly ICurrentDateProvider _dateProvider;
        private readonly DdaRankConfig _ddaRankConfig;
        private bool _dailyFirstEasyAvailable;
        private bool _dailyFirstEasyEvaluated;
        private bool _isCurrentLevelDailyFirstEasy;
        private bool _currentLevelDirty;
        private bool _currentLevelRetried;
        private bool _ddaToolOrReviveUsed;
        private bool _ddaReviveUsed;
        private bool _demotedThisLevel;
        private bool _ddaPendingDemote;
        private int _sessionPlayedCount;
        private int _sessionConsecutiveWins;
        private bool _hasWonSinceColdStart;
        private int _sessionRewardViewCount;
        private bool _firstSessionRuntime;
        private readonly Dictionary<int, float> _failTextRevivePercent = new();

        public GameStateService(
            GameStateData data,
            IGameStatePlayerStore store = null,
            IVibrationStateSink vibrationSink = null,
            IGameStateEndgameStore endgameStore = null,
            string applicationVersion = "",
            ICurrentDateProvider dateProvider = null,
            DdaRankConfig ddaRankConfig = null)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            _store = store;
            _endgameStore = endgameStore ?? store as IGameStateEndgameStore;
            _vibrationSink = vibrationSink;
            _applicationVersion = applicationVersion ?? string.Empty;
            _dateProvider = dateProvider ?? SystemCurrentDateProvider.Instance;
            _ddaRankConfig = ddaRankConfig ?? new DdaRankConfig();
            _firstSessionRuntime = Data.IsFirstSession;
            _vibrationSink?.SetEnabled(Data.VibrationOn);
        }

        public GameStateData Data { get; }
        public event Action<string, int> ToolCountChanged;

        public int CurrentLevel => Data.CurrentLevel;
        public bool TutorialDone => Data.TutorialDone;
        public bool IsFirstSession => _firstSessionRuntime;
        public int CurrentStrategy => Data.CurrentStrategy;
        public string CurrentDate => _dateProvider.CurrentDate;
        public string LastSplashDate => Data.LastSplashDate;
        public string AppliedLocale => Data.AppliedLocale;
        public bool MusicOn => Data.MusicOn;
        public bool SoundOn => Data.SoundOn;
        public bool VibrationOn => Data.VibrationOn;
        public bool PeopleOn => Data.PeopleOn;
        public bool PatternModeOn => Data.PatternModeOn;
        public bool PatternEntryDotDismissed => Data.PatternEntryDotDismissed;
        public bool PatternSwitchDotDismissed => Data.PatternSwitchDotDismissed;
        public bool HasUsedReviveFree => Data.HasUsedReviveFree;
        public float LastWinBeatPercent => Data.LastWinBeatPercent;
        public int DailyIndex => Data.DailyIndex;
        public string DailyCompletedDate => Data.DailyCompletedDate;
        public string MaxDailyDate => Data.MaxDailyDate;
        public int DailyElapsedSeconds => Data.DailyElapsedSeconds;
        public float DailyBeatPercent => Data.DailyBeatPercent;
        public float DailyBestBeatPercent => Data.DailyBestBeatPercent;
        public string DailyStartedDate => Data.DailyStartedDate;
        public DailyEntryState CurrentDailyEntryState =>
            DailyEntryStateContract.Compute(
                Data.CurrentLevel,
                _dateProvider.CurrentDate,
                Data.DailyCompletedDate,
                Data.MaxDailyDate);
        public bool HasUsedTool => Data.HasUsedTool;
        public bool HasPropHighlightShown => Data.PropHighlightShown;
        public int PushAskCount => Data.PushAskCount;
        public string PushGuideLastDate => Data.PushGuideLastDate;
        public int PushGuideShownCount => Data.PushGuideShownCount;
        public int PushGuidePopupCount => Data.PushGuidePopupCount;
        public bool HasShownAttGuide => Data.HasShownAttGuide;
        public bool IsCurrentLevelDirty => _currentLevelDirty;
        public bool IsCurrentLevelRetried => _currentLevelRetried;
        public bool WasDdaToolOrReviveUsed => _ddaToolOrReviveUsed;
        public bool WasDdaReviveUsed => _ddaReviveUsed;
        public int SessionPlayedCount => _sessionPlayedCount;
        public int SessionConsecutiveWins => _sessionConsecutiveWins;
        public bool HasWonSinceColdStart => _hasWonSinceColdStart;
        public bool InterstitialUnlocked => Data.InterstitialUnlocked;
        public bool BannerUnlocked => Data.BannerUnlocked;
        public int SessionRewardViewCount => _sessionRewardViewCount;
        public bool IsDailyFirstEasyAvailable => _dailyFirstEasyAvailable;
        public bool IsCurrentLevelDailyFirstEasy => _isCurrentLevelDailyFirstEasy;
        public event Action<bool> LevelSettled;

        public void EnsureFirstOpenTime(
            long sdkValueMilliseconds,
            long fallbackNowMilliseconds = 0)
        {
            if (Data.FirstOpenTimeMs > 0) return;
            Data.FirstOpenTimeMs = sdkValueMilliseconds > 0
                ? sdkValueMilliseconds
                : fallbackNowMilliseconds > 0
                    ? fallbackNowMilliseconds
                    : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            SavePlayer();
        }

        public void EvaluateDailyFirstEasy()
        {
            if (_dailyFirstEasyEvaluated) return;
            _dailyFirstEasyEvaluated = true;
            string today = _dateProvider.CurrentDate;
            if (string.CompareOrdinal(Data.DailyFirstEasyDate, today) >= 0)
            {
                _dailyFirstEasyAvailable = false;
                return;
            }

            Dictionary<string, object> snapshot = Data.EndgameSnapshot;
            if (snapshot.Count > 0 &&
                ReadObjectInt(snapshot, "level", 0) == Data.CurrentLevel &&
                ReadObjectInt(snapshot, "lives", 0) > 0)
            {
                if (HasValidPrefill(snapshot))
                {
                    Data.DailyFirstEasyDate = today;
                    _dailyFirstEasyAvailable = false;
                    SavePlayer();
                    return;
                }
                int prefill = CollectionCount(snapshot, "prefill_positions");
                int userCats = CollectionCount(snapshot, "placed_cats") - prefill;
                int marks = CollectionCount(snapshot, "marks");
                int errors = CollectionCount(snapshot, "errors");
                if (userCats > 0 || marks > 0 || errors > 0)
                {
                    Data.DailyFirstEasyDate = today;
                    _dailyFirstEasyAvailable = false;
                    SavePlayer();
                    return;
                }
            }
            _dailyFirstEasyAvailable = true;
        }

        public void ConsumeDailyFirstEasy(bool markCurrentLevel = false)
        {
            Data.DailyFirstEasyDate = _dateProvider.CurrentDate;
            _dailyFirstEasyAvailable = false;
            if (markCurrentLevel) _isCurrentLevelDailyFirstEasy = true;
            SavePlayer();
        }

        public void AdvanceDailyFirstEasyDate()
        {
            string today = _dateProvider.CurrentDate;
            if (string.CompareOrdinal(Data.DailyFirstEasyDate, today) >= 0) return;
            Data.DailyFirstEasyDate = today;
            _dailyFirstEasyAvailable = false;
            SavePlayer();
        }

        public void ResetCurrentLevelDailyFirstEasy()
        {
            _isCurrentLevelDailyFirstEasy = false;
        }

        public void SetDailyIndex(int value)
        {
            Data.DailyIndex = value;
            SavePlayer();
        }

        public void SetDailyStartedDate(string date)
        {
            Data.DailyStartedDate = date ?? string.Empty;
            SavePlayer();
        }

        public void AdvanceMaxDailyDate(string date = null)
        {
            string target = date ?? _dateProvider.CurrentDate;
            if (string.CompareOrdinal(target, Data.MaxDailyDate) <= 0) return;
            Data.MaxDailyDate = target;
            SavePlayer();
        }

        public void MarkDailyCompleted(
            string date,
            int elapsedSeconds,
            float beatPercent)
        {
            Data.DailyCompletedDate = date ?? string.Empty;
            Data.DailyElapsedSeconds = elapsedSeconds;
            Data.DailyBeatPercent = beatPercent;
            if (beatPercent > Data.DailyBestBeatPercent)
                Data.DailyBestBeatPercent = beatPercent;
            _hasWonSinceColdStart = true;
            SavePlayer();
        }

        public void ClearDailyCompletion()
        {
            Data.DailyCompletedDate = string.Empty;
            Data.DailyElapsedSeconds = 0;
            Data.DailyBeatPercent = 0f;
            Data.DailyBestBeatPercent = 0f;
            SavePlayer();
        }

        public void SetCurrentLevel(int value)
        {
            Data.CurrentLevel = value;
            SavePlayer();
        }

        public void SetTutorialDone(bool value)
        {
            Data.TutorialDone = value;
            SavePlayer();
        }

        public void ConsumeFirstSessionPersist()
        {
            if (!Data.IsFirstSession) return;
            Data.IsFirstSession = false;
            SavePlayer();
        }

        public void MarkFirstSessionDone()
        {
            _firstSessionRuntime = false;
        }

        public void SetCurrentStrategy(int value)
        {
            Data.CurrentStrategy = value;
            SavePlayer();
        }

        public bool MarkSplashShownToday()
        {
            string today = _dateProvider.CurrentDate;
            bool firstToday = !string.Equals(
                Data.LastSplashDate,
                today,
                StringComparison.Ordinal);
            if (!firstToday) return false;
            Data.LastSplashDate = today;
            SavePlayer();
            return true;
        }

        public void SetAppliedLocale(string value)
        {
            Data.AppliedLocale = value ?? string.Empty;
            SavePlayer();
        }

        public void SetMusicOn(bool value)
        {
            Data.MusicOn = value;
            Data.MusicUserModified = true;
            SavePlayer();
        }

        public void InitMusicDefault(bool defaultOn)
        {
            if (Data.MusicUserModified || Data.MusicOn == defaultOn) return;
            Data.MusicOn = defaultOn;
            SavePlayer();
        }

        public void SetSoundOn(bool value)
        {
            Data.SoundOn = value;
            SavePlayer();
        }

        public void SetVibrationOn(bool value)
        {
            Data.VibrationOn = value;
            _vibrationSink?.SetEnabled(value);
            SavePlayer();
        }

        public void SetPeopleOn(bool value)
        {
            Data.PeopleOn = value;
            SavePlayer();
        }

        public void SetPatternModeOn(bool value)
        {
            Data.PatternModeOn = value;
            SavePlayer();
        }

        public void MarkPatternEntryDotDismissed()
        {
            if (Data.PatternEntryDotDismissed) return;
            Data.PatternEntryDotDismissed = true;
            SavePlayer();
        }

        public void MarkPatternSwitchDotDismissed()
        {
            if (Data.PatternSwitchDotDismissed) return;
            Data.PatternSwitchDotDismissed = true;
            SavePlayer();
        }

        public void MarkReviveFreeUsed()
        {
            if (Data.HasUsedReviveFree) return;
            Data.HasUsedReviveFree = true;
            SavePlayer();
        }

        public void SetLastWinBeatPercent(float value)
        {
            if (Math.Abs(Data.LastWinBeatPercent - value) < 0.0001f) return;
            Data.LastWinBeatPercent = value;
            SavePlayer();
        }

        public float GetFailTextRevivePercent(int level)
        {
            return _failTextRevivePercent.TryGetValue(level, out float value)
                ? value
                : -1f;
        }

        public void SetFailTextRevivePercent(int level, float value)
        {
            _failTextRevivePercent[level] = value;
        }

        public int GetToolCount(string kind)
        {
            switch (kind)
            {
                case "locate": return Data.ToolLocate;
                case "hint": return Data.ToolHint;
                default: return 0;
            }
        }

        public void SetToolCount(string kind, int count)
        {
            int previous = GetToolCount(kind);
            switch (kind)
            {
                case "locate": Data.ToolLocate = count; break;
                case "hint": Data.ToolHint = count; break;
                default: return;
            }

            if (count < previous && !Data.HasUsedTool)
                Data.HasUsedTool = true;
            SavePlayer();
            ToolCountChanged?.Invoke(kind, count);
        }

        public List<object> GetInFlightAwards()
        {
            return new List<object>(Data.InFlightAwards);
        }

        public void AddInFlightAward(Dictionary<string, object> entry)
        {
            if (entry == null) return;
            Data.InFlightAwards.Add(entry);
            SavePlayer();
        }

        public bool RemoveInFlightAward(int uid)
        {
            for (int index = Data.InFlightAwards.Count - 1;
                 index >= 0;
                 index--)
            {
                if (Data.InFlightAwards[index] is not
                        Dictionary<string, object> entry ||
                    ReadObjectInt(entry, "uid", -1) != uid)
                    continue;
                Data.InFlightAwards.RemoveAt(index);
                SavePlayer();
                return true;
            }
            return false;
        }

        public Dictionary<string, object> FindInFlightAward(int uid)
        {
            foreach (object value in Data.InFlightAwards)
            {
                if (value is Dictionary<string, object> entry &&
                    ReadObjectInt(entry, "uid", -1) == uid)
                    return entry;
            }
            return null;
        }

        public void MarkPropHighlightShown()
        {
            if (Data.PropHighlightShown) return;
            Data.PropHighlightShown = true;
            SavePlayer();
        }

        public void IncrementPushAskCount()
        {
            Data.PushAskCount++;
            SavePlayer();
        }

        public void MarkPushGuideTriggered()
        {
            Data.PushGuideLastDate = _dateProvider.CurrentDate;
            Data.PushGuideShownCount++;
            SavePlayer();
        }

        public void MarkPushGuidePopupShown()
        {
            Data.PushGuidePopupCount++;
            SavePlayer();
        }

        public bool IsPushGuideCooldownElapsed()
        {
            if (string.IsNullOrEmpty(Data.PushGuideLastDate)) return true;
            if (!DateTime.TryParseExact(
                    Data.PushGuideLastDate,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime lastDate))
                return true;
            if (!DateTime.TryParseExact(
                    _dateProvider.CurrentDate,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime today))
                return true;
            return (today - lastDate).TotalDays >= 5d;
        }

        public int GetRecentThreeDayWinCount()
        {
            RollDayIfNeeded();
            int total = 0;
            foreach (object value in Data.RecentWinCountsByDay.Values)
            {
                try { total += Convert.ToInt32(value); }
                catch (Exception exception) when (
                    exception is FormatException ||
                    exception is InvalidCastException ||
                    exception is OverflowException) { }
            }
            return total;
        }

        public void MarkAttGuideShown()
        {
            if (Data.HasShownAttGuide) return;
            Data.HasShownAttGuide = true;
            SavePlayer();
        }

        public void MarkCurrentLevelDirty()
        {
            _currentLevelDirty = true;
        }

        public void ClearCurrentLevelDirty()
        {
            _currentLevelDirty = false;
        }

        public void MarkDdaToolOrReviveUsed()
        {
            _ddaToolOrReviveUsed = true;
        }

        public void MarkDdaReviveUsed()
        {
            _ddaReviveUsed = true;
        }

        public void ResetCurrentLevelRuntimeFlags()
        {
            _currentLevelDirty = false;
            _ddaToolOrReviveUsed = false;
            _ddaReviveUsed = false;
        }

        public void OnSessionStarted()
        {
            RollDayIfNeeded();
            Data.SessionCount++;
            Data.TodaySessionCount++;
            _sessionPlayedCount = 0;
            _sessionConsecutiveWins = 0;
            _sessionRewardViewCount = 0;
            SavePlayer();
        }

        public void IncrementSessionRewardViewCount()
        {
            _sessionRewardViewCount++;
        }

        public void ResetSessionRewardViewCount()
        {
            _sessionRewardViewCount = 0;
        }

        public void MarkInterstitialUnlocked()
        {
            if (Data.InterstitialUnlocked) return;
            Data.InterstitialUnlocked = true;
            SavePlayer();
        }

        public void MarkBannerUnlocked()
        {
            if (Data.BannerUnlocked) return;
            Data.BannerUnlocked = true;
            SavePlayer();
        }

        public bool HasPendingRewards() => Data.PendingRewards.Count > 0;

        public List<object> GetPendingRewards() =>
            new(Data.PendingRewards);

        public void AddPendingReward(Dictionary<string, object> reward)
        {
            if (reward == null) return;
            Data.PendingRewards.Add(reward);
            SavePlayer();
        }

        public List<object> PopAllPendingRewards()
        {
            var result = new List<object>(Data.PendingRewards);
            if (result.Count == 0) return result;
            Data.PendingRewards.Clear();
            SavePlayer();
            return result;
        }

        public void RemovePendingRewards(IReadOnlyCollection<string> showIds)
        {
            if (showIds == null || showIds.Count == 0) return;
            bool changed = false;
            for (int index = Data.PendingRewards.Count - 1; index >= 0; index--)
            {
                if (Data.PendingRewards[index] is not
                        Dictionary<string, object> entry ||
                    !Contains(showIds, ReadString(entry, "show_id")))
                    continue;
                Data.PendingRewards.RemoveAt(index);
                changed = true;
            }
            if (changed) SavePlayer();
        }

        public void RemovePendingRewardEntries(
            IReadOnlyCollection<object> entries)
        {
            if (entries == null || entries.Count == 0) return;
            bool changed = false;
            foreach (object entry in entries)
                changed |= Data.PendingRewards.Remove(entry);
            if (changed) SavePlayer();
        }

        public void RecordNormalReward(long unixTimestamp)
        {
            Data.RewardHistoryTimestamps.Add(unixTimestamp);
            long cutoff = unixTimestamp - RewardHistoryRetainSeconds;
            for (int index = Data.RewardHistoryTimestamps.Count - 1;
                 index >= 0;
                 index--)
            {
                if (ReadLong(Data.RewardHistoryTimestamps[index]) < cutoff)
                    Data.RewardHistoryTimestamps.RemoveAt(index);
            }
            SavePlayer();
        }

        public int GetRestoreRemainingToday(long unixTimestamp)
        {
            RollDayIfNeeded();
            long cutoff = unixTimestamp - RestoreNormalLookbackSeconds;
            int recent = 0;
            for (int index = 0;
                 index < Data.RewardHistoryTimestamps.Count;
                 index++)
            {
                if (ReadLong(Data.RewardHistoryTimestamps[index]) >= cutoff)
                    recent++;
            }
            if (recent < RestoreMinimumNormalRewards) return 0;
            return Math.Max(
                0,
                RestoreDailyMaximum - Data.RestoredTodayCount);
        }

        public int RestoredTodayCount
        {
            get
            {
                RollDayIfNeeded();
                return Data.RestoredTodayCount;
            }
        }

        public void AddRestoredTodayCount(int count)
        {
            if (count <= 0) return;
            RollDayIfNeeded();
            Data.RestoredTodayCount += count;
            SavePlayer();
        }

        public void AddActiveSeconds(int seconds)
        {
            if (seconds <= 0) return;
            RollDayIfNeeded();
            Data.TodayActiveSeconds += seconds;
            Data.TotalActiveSeconds += seconds;
            SavePlayer();
        }

        public bool HasGrtLevelD90Reported(int level) =>
            Data.GrtLevelD90Reported.Contains(level);

        public void MarkGrtLevelD90Reported(int level)
        {
            if (level <= 0 || HasGrtLevelD90Reported(level)) return;
            Data.GrtLevelD90Reported.Add(level);
            SavePlayer();
        }

        public bool HasGrtEventReported(string eventName) =>
            !string.IsNullOrEmpty(eventName) &&
            Data.GrtReportedEvents.Contains(eventName);

        public void MarkGrtEventReported(string eventName)
        {
            if (string.IsNullOrEmpty(eventName) ||
                HasGrtEventReported(eventName))
                return;
            Data.GrtReportedEvents.Add(eventName);
            SavePlayer();
        }

        public void OnGameFinished()
        {
            RollDayIfNeeded();
            _sessionPlayedCount++;
            Data.TodayPlayedCount++;
            SavePlayer();
        }

        public void OnLevelWon(int levelNumber)
        {
            int nextLevel = levelNumber + 1;
            if (nextLevel > Data.CurrentLevel) Data.CurrentLevel = nextLevel;

            int strategyBefore = Data.CurrentStrategy;
            Data.PreCatPendingStruggle =
                (Data.PreCatFailLevel == levelNumber && Data.PreCatFailCount >= 2) ||
                Data.PreCatRevivedThisLevel;
            Data.PreCatFailCount = 0;
            Data.PreCatFailLevel = 0;
            Data.PreCatRevivedThisLevel = false;
            Data.PreCatLockLevel = 0;
            Data.PreCatLockType = "0";
            Data.PreCatLockPosition = new UnityEngine.Vector2Int(-1, -1);
            Data.PreCatPendingHard = LevelData.IsHardLevel(levelNumber);

            if (levelNumber >= 6)
            {
                int maxStrategy;
                if (levelNumber >= 201) maxStrategy = 6;
                else if (levelNumber >= 101) maxStrategy = 5;
                else if (levelNumber >= 51) maxStrategy = 4;
                else if (levelNumber >= 21) maxStrategy = 3;
                else maxStrategy = 2;

                int winThreshold = levelNumber >= 51 ? 1 : 2;
                int minStrategy = levelNumber >= 101 ? 2 : 1;
                bool cleanWin = !_currentLevelDirty;
                if (cleanWin)
                {
                    Data.ConsecutiveCleanWins++;
                    if (Data.ConsecutiveCleanWins >= winThreshold &&
                        Data.CurrentStrategy < maxStrategy)
                    {
                        Data.CurrentStrategy++;
                        Data.ConsecutiveCleanWins = 0;
                    }
                }
                else
                {
                    Data.ConsecutiveCleanWins = 0;
                }

                int failThreshold = levelNumber >= 21 ? 2 : 1;
                if (Data.ConsecutiveFails >= failThreshold &&
                    Data.CurrentStrategy > minStrategy &&
                    !_demotedThisLevel)
                {
                    Data.CurrentStrategy--;
                    _demotedThisLevel = true;
                }
                Data.ConsecutiveFails = 0;

                if (levelNumber >= 21)
                {
                    if (_currentLevelRetried)
                    {
                        if (Data.CurrentStrategy == Data.RetryTrackingStrategy)
                        {
                            Data.ConsecutiveRetryLevels++;
                            int retryMinimum = levelNumber >= 101 ? 2 : 1;
                            if (Data.ConsecutiveRetryLevels >= 2 &&
                                Data.CurrentStrategy > retryMinimum &&
                                !_demotedThisLevel)
                            {
                                Data.CurrentStrategy--;
                                Data.ConsecutiveRetryLevels = 0;
                                Data.RetryTrackingStrategy = 0;
                            }
                        }
                        else
                        {
                            Data.ConsecutiveRetryLevels = 1;
                            Data.RetryTrackingStrategy = Data.CurrentStrategy;
                        }
                    }
                    else
                    {
                        Data.ConsecutiveRetryLevels = 0;
                        Data.RetryTrackingStrategy = 0;
                    }
                }

                ApplyDdaDemoteOnWon(levelNumber, minStrategy);
            }

            Data.LastLevelCleanWin = !_currentLevelDirty;
            _currentLevelRetried = false;
            _currentLevelDirty = false;
            _ddaToolOrReviveUsed = false;
            _ddaReviveUsed = false;
            _isCurrentLevelDailyFirstEasy = false;
            _demotedThisLevel = false;
            Data.RetryPuzzleLevel = 0;
            Data.RetryPuzzleParameters = new Dictionary<string, object>();
            _hasWonSinceColdStart = true;
            _sessionConsecutiveWins++;
            IncrementTodayWinCount();
            if (Data.CurrentStrategy < strategyBefore)
                Data.PreCatPendingDemote = true;

            SavePlayer();
            LevelSettled?.Invoke(true);
        }

        public void OnLevelFailed(int levelNumber)
        {
            _currentLevelRetried = true;
            _currentLevelDirty = true;
            Data.LastLevelCleanWin = false;
            _sessionConsecutiveWins = 0;

            if (levelNumber != Data.PreCatFailLevel)
            {
                Data.PreCatFailLevel = levelNumber;
                Data.PreCatFailCount = 0;
                Data.PreCatRevivedThisLevel = false;
            }
            Data.PreCatFailCount++;

            if (levelNumber >= 6)
            {
                Data.ConsecutiveCleanWins = 0;
                Data.ConsecutiveFails++;
            }
            if (_ddaRankConfig.IsAnyActionDemote())
                _ddaToolOrReviveUsed = true;

            SavePlayer();
            LevelSettled?.Invoke(false);
        }

        private void ApplyDdaDemoteOnWon(int levelNumber, int minimumStrategy)
        {
            if (!_ddaRankConfig.IsRetryOnceDemote() &&
                !_ddaRankConfig.IsToolReviveDemote() &&
                !_ddaRankConfig.IsAnyActionDemote())
                return;
            if (_isCurrentLevelDailyFirstEasy) return;

            bool triggered;
            if (_ddaRankConfig.IsRetryOnceDemote())
                triggered = _currentLevelRetried || _ddaReviveUsed;
            else
                triggered = _ddaToolOrReviveUsed;

            int nextLevel = levelNumber + 1;
            bool nextIsSkip = LevelData.IsHardLevel(nextLevel) ||
                              LevelData.IsSpecialLevel(nextLevel);

            if (_ddaPendingDemote && !_demotedThisLevel)
            {
                Data.CurrentStrategy = Math.Max(minimumStrategy, Data.CurrentStrategy - 1);
                _ddaPendingDemote = false;
                _demotedThisLevel = true;
            }
            if (!triggered || _demotedThisLevel) return;

            if (nextIsSkip)
                _ddaPendingDemote = true;
            else
            {
                Data.CurrentStrategy = Math.Max(minimumStrategy, Data.CurrentStrategy - 1);
                _demotedThisLevel = true;
            }
        }

        private void RollDayIfNeeded()
        {
            string today = _dateProvider.CurrentDate;
            if (Data.TodayDate == today) return;

            Data.LastDaySessionCount = Data.TodaySessionCount;
            Data.TodaySessionCount = 0;
            Data.TodayPlayedCount = 0;
            Data.TodayActiveSeconds = 0;
            Data.RestoredTodayCount = 0;
            Data.ActiveDays++;

            if (DateTime.TryParseExact(
                    today,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime currentDate))
            {
                DateTime cutoff = currentDate.AddDays(-2);
                var stale = new List<string>();
                foreach (string key in Data.RecentWinCountsByDay.Keys)
                {
                    if (DateTime.TryParseExact(
                            key,
                            "yyyy-MM-dd",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out DateTime value) && value < cutoff)
                        stale.Add(key);
                }
                for (int index = 0; index < stale.Count; index++)
                    Data.RecentWinCountsByDay.Remove(stale[index]);
            }
            Data.TodayDate = today;
        }

        private void IncrementTodayWinCount()
        {
            string today = _dateProvider.CurrentDate;
            Data.RecentWinCountsByDay[today] =
                ReadInt(Data.RecentWinCountsByDay, today, 0) + 1;
        }

        public void SetRetryPuzzle(int level, Dictionary<string, object> parameters)
        {
            Data.RetryPuzzleLevel = level;
            Data.RetryPuzzleParameters = parameters ?? new Dictionary<string, object>();
            SavePlayer();
        }

        public Dictionary<string, object> GetRetryPuzzle(int level)
        {
            return Data.RetryPuzzleLevel == level && Data.RetryPuzzleParameters.Count > 0
                ? Data.RetryPuzzleParameters
                : new Dictionary<string, object>();
        }

        public int GetPreCatFailCount(int level)
        {
            return Data.PreCatFailLevel == level ? Data.PreCatFailCount : 0;
        }

        public void MarkPreCatRevived()
        {
            if (Data.PreCatRevivedThisLevel) return;
            Data.PreCatRevivedThisLevel = true;
            SavePlayer();
        }

        public Dictionary<string, object> ConsumePreCatPending()
        {
            var result = new Dictionary<string, object>
            {
                { "hard", Data.PreCatPendingHard },
                { "struggle", Data.PreCatPendingStruggle },
                { "demote", Data.PreCatPendingDemote }
            };

            if (!Data.PreCatPendingHard &&
                !Data.PreCatPendingStruggle &&
                !Data.PreCatPendingDemote)
                return result;

            Data.PreCatPendingHard = false;
            Data.PreCatPendingStruggle = false;
            Data.PreCatPendingDemote = false;
            SavePlayer();
            return result;
        }

        public Dictionary<string, object> GetPreCatLock(int level)
        {
            if (level > 0 && Data.PreCatLockLevel == level)
            {
                return new Dictionary<string, object>
                {
                    { "locked", true },
                    { "pre_type", Data.PreCatLockType },
                    { "position", Data.PreCatLockPosition }
                };
            }

            return new Dictionary<string, object>
            {
                { "locked", false },
                { "pre_type", "0" },
                { "position", new UnityEngine.Vector2Int(-1, -1) }
            };
        }

        public void SetPreCatLock(
            int level,
            string preType,
            UnityEngine.Vector2Int position)
        {
            Data.PreCatLockLevel = level;
            Data.PreCatLockType = preType ?? "0";
            Data.PreCatLockPosition = position;
            SavePlayer();
        }

        public Dictionary<string, object> RecordPuzzle(
            string puzzleId,
            int level,
            string version = "",
            string source = "")
        {
            Dictionary<string, object> previous = null;
            for (int index = Data.RecentPuzzles.Count - 1; index >= 0; index--)
            {
                if (!(Data.RecentPuzzles[index] is Dictionary<string, object> entry)) continue;
                if (ReadString(entry, "puzzle_id") == (puzzleId ?? string.Empty))
                {
                    previous = DeepClone(entry);
                    break;
                }
            }

            Data.RecentPuzzles.Add(new Dictionary<string, object>
            {
                { "puzzle_id", puzzleId ?? string.Empty },
                { "level", level },
                { "v", version ?? string.Empty },
                { "src", source ?? string.Empty },
                { "ts", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                { "bank_progress", DeepClone(Data.BankProgress) },
                { "main_bank_progress", DeepClone(Data.MainBankProgress) },
                { "lkmod_progress", DeepClone(Data.LkModifiedProgress) }
            });
            while (Data.RecentPuzzles.Count > RecentPuzzlesLimit) Data.RecentPuzzles.RemoveAt(0);
            SavePlayer();
            return previous ?? new Dictionary<string, object>();
        }

        public List<object> GetRecentPuzzles()
        {
            return (List<object>)DeepCloneValue(Data.RecentPuzzles);
        }

        public Dictionary<string, object> GetEndgameSnapshot()
        {
            return Data.EndgameSnapshot;
        }

        public bool SetEndgameSnapshot(Dictionary<string, object> snapshot)
        {
            snapshot = snapshot ?? new Dictionary<string, object>();
            if (snapshot.Count > 0) snapshot["app_version"] = _applicationVersion;
            Data.EndgameSnapshot = snapshot;
            return SaveEndgameNow();
        }

        public bool ClearEndgameSnapshot()
        {
            if (Data.EndgameSnapshot.Count == 0) return true;
            Data.EndgameSnapshot = new Dictionary<string, object>();
            return SaveEndgameNow();
        }

        public int GetGameTotalStat(string gameType, string key)
        {
            return ReadInt(TotalStats(gameType), key, 0);
        }

        public bool IncrementGameTotalStat(string gameType, string key, int delta = 1)
        {
            Dictionary<string, object> stats = TotalStats(gameType);
            stats[key] = ReadInt(stats, key, 0) + delta;
            return RequestEndgameSave();
        }

        public string GetPersistedGameId(string gameType)
        {
            return gameType == "daily" ? Data.DailyGameId : Data.MainGameId;
        }

        public bool SetPersistedGameId(string gameType, string value)
        {
            if (gameType == "daily") Data.DailyGameId = value ?? string.Empty;
            else Data.MainGameId = value ?? string.Empty;
            return SaveEndgameNow();
        }

        public bool ResetGameTotalStats(string gameType)
        {
            Dictionary<string, object> stats = TotalStats(gameType);
            if (stats.Count == 0) return true;
            stats.Clear();
            return SaveEndgameNow();
        }

        public Dictionary<string, object> GetGameRoundStats(string gameType)
        {
            return new Dictionary<string, object>(RoundStats(gameType));
        }

        public bool PersistGameRoundStats(
            string gameType,
            Dictionary<string, object> stats)
        {
            Dictionary<string, object> copy = stats == null
                ? new Dictionary<string, object>()
                : new Dictionary<string, object>(stats);
            if (gameType == "daily") Data.DailyGameRoundStats = copy;
            else Data.MainGameRoundStats = copy;
            return RequestEndgameSave();
        }

        public bool ResetGameRoundStats(string gameType)
        {
            Dictionary<string, object> stats = RoundStats(gameType);
            if (stats.Count == 0) return true;
            stats.Clear();
            return SaveEndgameNow();
        }

        public int GetBankIndex(int size, int rank, string tier = "")
        {
            string key = ProgressKey(size, rank, tier);
            return ReadInt(Data.BankProgress, key, 0);
        }

        public void AdvanceBankIndex(
            int size,
            int rank,
            string tier = "",
            bool persist = true)
        {
            string key = ProgressKey(size, rank, tier);
            Data.BankProgress[key] = ReadInt(Data.BankProgress, key, 0) + 1;
            if (persist) SavePlayer();
        }

        public Dictionary<string, object> GetMainProgress(
            int size,
            int rank,
            string tier = "")
        {
            string key = ProgressKey(size, rank, tier);
            if (!Data.MainBankProgress.TryGetValue(key, out object raw) ||
                !(raw is Dictionary<string, object> progress))
            {
                // This legacy-shaped default is intentional. get_next_entry_main in
                // the source detects the absent "idx" and migrates bank_progress.
                progress = new Dictionary<string, object>
                {
                    { "lk_mod", 0 },
                    { "regular", 0 },
                    { "lkstyle", 0 },
                    { "transform", 0 }
                };
                Data.MainBankProgress[key] = progress;
            }
            return progress;
        }

        public void SetMainProgress(
            int size,
            int rank,
            string tier,
            Dictionary<string, object> progress,
            bool persist = true)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            Data.MainBankProgress[ProgressKey(size, rank, tier)] = progress;
            if (persist) SavePlayer();
        }

        public Dictionary<string, object> GetLkModifiedProgress(int size, int rank)
        {
            string key = LkModifiedProgressKey(size, rank);
            if (!Data.LkModifiedProgress.TryGetValue(key, out object raw) ||
                !(raw is Dictionary<string, object> progress))
            {
                progress = new Dictionary<string, object> { { "idx", 0 } };
                Data.LkModifiedProgress[key] = progress;
            }
            return progress;
        }

        public void SetLkModifiedProgress(
            int size,
            int rank,
            Dictionary<string, object> progress,
            bool persist = true)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            Data.LkModifiedProgress[LkModifiedProgressKey(size, rank)] = progress;
            if (persist) SavePlayer();
        }

        public bool CommitBankProgress()
        {
            return SavePlayer();
        }

        public Dictionary<string, object> GetBankProgressSnapshot()
        {
            return DeepClone(Data.BankProgress);
        }

        public Dictionary<string, object> GetMainBankProgressSnapshot()
        {
            return DeepClone(Data.MainBankProgress);
        }

        public Dictionary<string, object> GetLkModifiedProgressSnapshot()
        {
            return DeepClone(Data.LkModifiedProgress);
        }

        public static string ProgressKey(int size, int rank, string tier = "")
        {
            return $"{size}_{rank}{(tier == "H" ? "_H" : string.Empty)}";
        }

        public static string LkModifiedProgressKey(int size, int rank)
        {
            return $"{size}_{rank}";
        }

        public string RemoteSaveId => "core";

        public bool IsRemoteAhead(
            IReadOnlyDictionary<string, object> remote)
        {
            return DataSyncValues.Int(remote, "current_level") >
                   Data.CurrentLevel;
        }

        public Dictionary<string, object> ExportRemote()
        {
            return new Dictionary<string, object>
            {
                ["current_level"] = Data.CurrentLevel,
                ["tool_locate"] = Data.ToolLocate,
                ["tool_hint"] = Data.ToolHint,
                ["current_strategy"] = Data.CurrentStrategy
            };
        }

        public bool MergeRemote(
            IReadOnlyDictionary<string, object> remote,
            DataSyncMergeContext context)
        {
            if (remote == null || remote.Count == 0 ||
                !context.RemoteAhead)
                return false;

            Data.CurrentLevel = DataSyncValues.Int(
                remote,
                "current_level",
                Data.CurrentLevel);
            Data.CurrentStrategy = DataSyncValues.Int(
                remote,
                "current_strategy",
                Data.CurrentStrategy);
            if (Data.CurrentLevel > 1 && !Data.TutorialDone)
                Data.TutorialDone = true;

            int locate = DataSyncValues.Int(
                remote,
                "tool_locate",
                Data.ToolLocate);
            int hint = DataSyncValues.Int(
                remote,
                "tool_hint",
                Data.ToolHint);
            bool locateChanged = locate != Data.ToolLocate;
            bool hintChanged = hint != Data.ToolHint;
            Data.ToolLocate = locate;
            Data.ToolHint = hint;
            SavePlayer();

            if (locateChanged)
                ToolCountChanged?.Invoke("locate", Data.ToolLocate);
            if (hintChanged)
                ToolCountChanged?.Invoke("hint", Data.ToolHint);
            return true;
        }

        private bool SavePlayer()
        {
            return _store == null || _store.SavePlayer(Data);
        }

        private bool SaveEndgameNow()
        {
            return _endgameStore == null || _endgameStore.SaveEndgame(Data);
        }

        private bool RequestEndgameSave()
        {
            return _endgameStore == null || _endgameStore.RequestSaveEndgame(Data);
        }

        private Dictionary<string, object> TotalStats(string gameType)
        {
            return gameType == "daily"
                ? Data.DailyGameTotalStats
                : Data.MainGameTotalStats;
        }

        private Dictionary<string, object> RoundStats(string gameType)
        {
            return gameType == "daily"
                ? Data.DailyGameRoundStats
                : Data.MainGameRoundStats;
        }

        private static int ReadInt(
            Dictionary<string, object> values,
            string key,
            int fallback)
        {
            if (!values.TryGetValue(key, out object raw) || raw == null) return fallback;
            try { return Convert.ToInt32(raw); }
            catch (Exception exception) when (
                exception is FormatException ||
                exception is InvalidCastException ||
                exception is OverflowException)
            {
                return fallback;
            }
        }

        private static string ReadString(Dictionary<string, object> values, string key)
        {
            return values.TryGetValue(key, out object raw) && raw != null ? raw.ToString() : string.Empty;
        }

        private static int ReadObjectInt(Dictionary<string, object> values, string key, int fallback)
        {
            if (!values.TryGetValue(key, out object raw) || raw == null) return fallback;
            try { return Convert.ToInt32(raw); }
            catch (Exception exception) when (
                exception is FormatException ||
                exception is InvalidCastException ||
                exception is OverflowException)
            {
                return fallback;
            }
        }

        private static long ReadLong(object value)
        {
            if (value == null) return 0;
            try { return Convert.ToInt64(value); }
            catch (Exception exception) when (
                exception is FormatException ||
                exception is InvalidCastException ||
                exception is OverflowException)
            {
                return 0;
            }
        }

        private static bool Contains(
            IReadOnlyCollection<string> values,
            string target)
        {
            foreach (string value in values)
                if (string.Equals(value, target, StringComparison.Ordinal))
                    return true;
            return false;
        }

        private static int CollectionCount(Dictionary<string, object> values, string key)
        {
            return values.TryGetValue(key, out object raw) && raw is System.Collections.ICollection collection
                ? collection.Count
                : 0;
        }

        private static bool HasValidPrefill(Dictionary<string, object> snapshot)
        {
            if (!snapshot.TryGetValue("prefill_positions", out object rawPositions) ||
                !(rawPositions is System.Collections.IList positions) || positions.Count == 0 ||
                !snapshot.TryGetValue("solution", out object rawSolution) ||
                !(rawSolution is System.Collections.IList solution) || solution.Count == 0)
                return false;
            for (int i = 0; i < positions.Count; i++)
            {
                if (!(positions[i] is System.Collections.IList position) || position.Count < 2) return false;
                int row = Convert.ToInt32(position[0]);
                int column = Convert.ToInt32(position[1]);
                if (row < 0 || row >= solution.Count || Convert.ToInt32(solution[row]) != column) return false;
            }
            return true;
        }

        private static Dictionary<string, object> DeepClone(
            Dictionary<string, object> source)
        {
            var clone = new Dictionary<string, object>(source.Count);
            foreach (KeyValuePair<string, object> pair in source)
            {
                clone[pair.Key] = DeepCloneValue(pair.Value);
            }
            return clone;
        }

        private static object DeepCloneValue(object value)
        {
            if (value is Dictionary<string, object> dictionary) return DeepClone(dictionary);
            if (value is List<object> list)
            {
                var clone = new List<object>(list.Count);
                foreach (object item in list) clone.Add(DeepCloneValue(item));
                return clone;
            }
            return value;
        }
    }

    public static class GameStateRuntime
    {
        private static GameStateService _current;
        private static GameStateRepository _repository;
        private static bool _quittingHookRegistered;

        public static GameStateService Current
        {
            get
            {
                if (_current != null) return _current;
                GameStateRepository repository = GameStateRepository.CreateDefault();
                _repository = repository;
                _current = new GameStateService(
                    repository.Load(),
                    repository,
                    null,
                    repository,
                    UnityEngine.Application.version);
                RegisterQuittingHook();
                return _current;
            }
        }

        public static void Configure(GameStateService service)
        {
            FlushPendingWrites();
            _repository = null;
            _current = service ?? throw new ArgumentNullException(nameof(service));
        }

        public static bool FlushPendingWrites()
        {
            return _repository == null || _repository.FlushEndgameWrites();
        }

#if UNITY_INCLUDE_TESTS
        /// <summary>
        /// Temporarily replaces the process-wide runtime state without
        /// flushing, replacing, or otherwise touching the repository that
        /// owns the player's real save. The exact previous references are
        /// restored when the returned scope is disposed.
        /// </summary>
        internal static IDisposable OverrideForTests(GameStateService service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            var scope = new TestOverrideScope(
                _current,
                _repository,
                service);
            _current = service;
            _repository = null;
            return scope;
        }

        private sealed class TestOverrideScope : IDisposable
        {
            private readonly GameStateService _previousCurrent;
            private readonly GameStateRepository _previousRepository;
            private readonly GameStateService _replacement;
            private bool _disposed;

            public TestOverrideScope(
                GameStateService previousCurrent,
                GameStateRepository previousRepository,
                GameStateService replacement)
            {
                _previousCurrent = previousCurrent;
                _previousRepository = previousRepository;
                _replacement = replacement;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                if (!ReferenceEquals(_current, _replacement))
                    throw new InvalidOperationException(
                        "GameStateRuntime test overrides must be disposed in order.");
                _current = _previousCurrent;
                _repository = _previousRepository;
            }
        }
#endif

        private static void RegisterQuittingHook()
        {
            if (_quittingHookRegistered) return;
            UnityEngine.Application.quitting += HandleApplicationQuitting;
            _quittingHookRegistered = true;
        }

        private static void HandleApplicationQuitting()
        {
            FlushPendingWrites();
        }
    }
}

``

## PATH: Assets\_Project\Scripts\Core\Config\AbConfigRuntime.cs
``csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Meowdoku.Core.Config
{
    public interface IAbConfigRuntimeConsumer
    {
        void BindAbConfigRuntime(AbConfigRuntime runtime);
    }

    [DisallowMultipleComponent]
    public sealed class AbConfigRuntime : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour providerAdapter;

        private readonly AdConfigSet _adConfigs = new();
        private readonly SettingsConfigSet _settingsConfigs = new();
        private readonly HomeConfigSet _homeConfigs = new();
        private readonly PlatformConfigSet _platformConfigs = new();
        private AbConfigService _service;
        private GameStateService _gameState;

        public event Action<string> ParamsUpdated;
        public AdConfigSet Ads => _adConfigs;
        public SettingsConfigSet Settings => _settingsConfigs;
        public HomeConfigSet Home => _homeConfigs;
        public PlatformConfigSet Platform => _platformConfigs;
        public IAbValueProvider ValueProvider
        {
            get
            {
                Initialize(_gameState ?? GameStateRuntime.Current);
                return _service?.Provider ?? OfflineAbRuntimeProvider.Instance;
            }
        }
        public bool IsRemoteReady => _service?.IsRemoteReady == true;
        public bool IsAppStartFinalized =>
            _service?.IsAppStartFinalized == true;
        public long FirstOpenUnixMilliseconds =>
            (_gameState ?? GameStateRuntime.Current).Data.FirstOpenTimeMs;

        private void Awake()
        {
            Initialize(GameStateRuntime.Current);
        }

        public void Initialize(GameStateService gameState)
        {
            _gameState = gameState ?? GameStateRuntime.Current;
            if (_service != null) return;
            IAbRuntimeProvider provider =
                providerAdapter as IAbRuntimeProvider ??
                OfflineAbRuntimeProvider.Instance;
            _service = new AbConfigService(provider, BuildConfigCatalog());
            _service.ProviderReady += EnsureFirstOpenTime;
            _service.ParamsUpdated += HandleParamsUpdated;
            if (provider.IsInitialized || provider.IsRemoteReady)
                EnsureFirstOpenTime();
            _service.Initialize();
        }

        public IEnumerator AwaitRemoteReady(float maximumSeconds = 2f)
        {
            Initialize(_gameState ?? GameStateRuntime.Current);
            float deadline = Time.realtimeSinceStartup +
                             Mathf.Max(0f, maximumSeconds);
            while (!IsRemoteReady && Time.realtimeSinceStartup < deadline)
                yield return null;
            if (!IsRemoteReady)
            {
                EnsureFirstOpenTime();
                _service.FinalizeRemoteFallback();
            }
        }

        public void ReloadTiming(string timing)
        {
            Initialize(_gameState ?? GameStateRuntime.Current);
            if (!IsAppStartFinalized)
            {
                EnsureFirstOpenTime();
                _service.FinalizeRemoteFallback();
            }
            _service.ReloadTiming(timing);
        }

        public LivingDaysSegment CurrentLivingDaysSegment()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            int bias = (int)TimeZoneInfo.Local
                .GetUtcOffset(DateTimeOffset.FromUnixTimeMilliseconds(now))
                .TotalMinutes;
            return _adConfigs.LivingDays.Resolve(
                FirstOpenUnixMilliseconds,
                now,
                bias);
        }

        public void BindProvider(MonoBehaviour adapter)
        {
            if (providerAdapter == adapter) return;
            providerAdapter = adapter;
            RebuildService();
        }

        private void EnsureFirstOpenTime()
        {
            GameStateService state = _gameState ?? GameStateRuntime.Current;
            long sdkValue = _service?.Provider.FirstOpenUnixMilliseconds ?? 0;
            state.EnsureFirstOpenTime(sdkValue);
        }

        private void RebuildService()
        {
            if (_service != null)
            {
                _service.ProviderReady -= EnsureFirstOpenTime;
                _service.ParamsUpdated -= HandleParamsUpdated;
                _service.Dispose();
                _service = null;
            }
            if (isActiveAndEnabled)
                Initialize(_gameState ?? GameStateRuntime.Current);
        }

        private void OnDestroy()
        {
            if (_service != null)
            {
                _service.ProviderReady -= EnsureFirstOpenTime;
                _service.ParamsUpdated -= HandleParamsUpdated;
                _service.Dispose();
                _service = null;
            }
            ParamsUpdated = null;
        }

        private void HandleParamsUpdated(string updateType)
        {
            ParamsUpdated?.Invoke(updateType ?? string.Empty);
        }

        private IReadOnlyList<IAbConfig> BuildConfigCatalog()
        {
            var configs = new List<IAbConfig>(
                _adConfigs.All.Count +
                _settingsConfigs.All.Count +
                _homeConfigs.All.Count +
                _platformConfigs.All.Count);
            configs.AddRange(_adConfigs.All);
            configs.AddRange(_settingsConfigs.All);
            configs.AddRange(_homeConfigs.All);
            configs.AddRange(_platformConfigs.All);
            return configs;
        }
    }
}

``

## PATH: UI Registry Search (UiName.Feedback, UiName.RateUs, UiName.RateUsV2)
``text

Assets\_Project\Scripts\Core\UI\UIContracts.cs:20:        RateUs = 11,
Assets\_Project\Scripts\Core\UI\UIContracts.cs:21:        RateUsV2 = 12,
Assets\_Project\Scripts\Core\Tracking\TrackerService.cs:202:            public const string RateUs = "rate_us";



``

NOT FOUND trong toàn bộ Assets/_Project cho UI registry data của UiName.Feedback (Prefab / Presenter).

## PATH: D:\Projects\Meowdoku\Assets\_Project\Scripts\Gameplay\AbSwitchPopupPresenter.cs (Sample Popup Presenter)
``csharp
using System;
using System.Collections;
using System.Collections.Generic;
using Meowdoku.Core.Localization;
using Meowdoku.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class AbSwitchPopupPresenter : UIFrameWindow
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text actionText;
        [SerializeField] private Text feedbackText;
        [SerializeField] private Button actionButton;
        [SerializeField] private Button actionCloseButton;
        [SerializeField] private Button feedbackButton;
        [SerializeField] private GameObject toolGroup;
        [SerializeField] private GameObject locateReward;
        [SerializeField] private Text locateCountText;
        [SerializeField] private GameObject hintReward;
        [SerializeField] private Text hintCountText;
        [SerializeField] private LocalizationCatalog localization;

        protected override void OnCreate()
        {
            Add(actionButton, Close);
            Add(actionCloseButton, Close);
            // Support/FAQ belongs to the external services boundary. Keep the
            // source control visible when requested but non-interactive until
            // that boundary exists.
            if (feedbackButton != null)
                feedbackButton.interactable = false;
        }

        protected override void OnShow(
            IReadOnlyDictionary<string, object> parameters)
        {
            SetText(titleText, TranslateParameter(
                parameters,
                "title",
                "DAILY_STREAK_MAJOR_UPDATE",
                "Major Update"));
            SetText(bodyText, TranslateParameter(
                parameters,
                "body",
                "DAILY_STREAK_SWITCH3_DESC",
                "Good news! Daily Streak has been upgraded."));
            SetText(actionText, TranslateParameter(
                parameters,
                "btn_text",
                "DAILY_STREAK_GET_IT",
                "Get it"));
            SetText(feedbackText, Translate(
                "FEEDBACK_TITLE",
                "Feedback"));

            bool feedback = ReadString(parameters, "feedback") == "1";
            SetActive(feedbackButton, feedback);
            ApplyRewards(parameters);
        }

        protected override IEnumerator OnHide()
        {
            yield break;
        }

        protected override bool OnBackRequest()
        {
            Close();
            return true;
        }

        protected override void OnDestroyWindow()
        {
            Remove(actionButton, Close);
            Remove(actionCloseButton, Close);
            base.OnDestroyWindow();
        }

        private void ApplyRewards(
            IReadOnlyDictionary<string, object> parameters)
        {
            IReadOnlyDictionary<string, object> rewards = null;
            if (parameters != null &&
                parameters.TryGetValue("reward", out object raw))
                rewards = raw as IReadOnlyDictionary<string, object>;
            int locate = ReadCount(rewards, "locate");
            int hint = ReadCount(rewards, "hint");
            SetActive(toolGroup, locate > 0 || hint > 0);
            SetActive(locateReward, locate > 0);
            SetActive(hintReward, hint > 0);
            SetText(locateCountText, "x" + locate);
            SetText(hintCountText, "x" + hint);
        }

        private void Close()
        {
            Owner?.Hide(UiName.AbSwitchPopup);
        }

        private string TranslateParameter(
            IReadOnlyDictionary<string, object> parameters,
            string name,
            string defaultKey,
            string fallback)
        {
            string key = ReadString(parameters, name);
            if (string.IsNullOrEmpty(key)) key = defaultKey;
            return Translate(key, fallback);
        }

        private string Translate(string key, string fallback)
        {
            if (localization == null) return fallback;
            string value = localization.Translate(key);
            return string.IsNullOrEmpty(value) || value == key
                ? fallback
                : value;
        }

        private static string ReadString(
            IReadOnlyDictionary<string, object> parameters,
            string key)
        {
            return parameters != null &&
                   parameters.TryGetValue(key, out object value) &&
                   value != null
                ? Convert.ToString(value) ?? string.Empty
                : string.Empty;
        }

        private static int ReadCount(
            IReadOnlyDictionary<string, object> rewards,
            string key)
        {
            if (rewards == null ||
                !rewards.TryGetValue(key, out object value))
                return 0;
            try
            {
                return Math.Max(0, Convert.ToInt32(value));
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private static void Add(
            Button button,
            UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.AddListener(action);
        }

        private static void Remove(
            Button button,
            UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.RemoveListener(action);
        }

        private static void SetText(Text target, string value)
        {
            if (target != null) target.text = value ?? string.Empty;
        }

        private static void SetActive(Component target, bool active)
        {
            if (target != null &&
                target.gameObject.activeSelf != active)
                target.gameObject.SetActive(active);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }
}

``

