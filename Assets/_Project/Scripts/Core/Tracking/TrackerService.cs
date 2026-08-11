using System;
using System.Collections.Generic;

namespace Meowdoku.Core.Tracking
{
    public interface ITrackingSink
    {
        void SendEvent(
            string eventName,
            IReadOnlyDictionary<string, object> parameters);
        void SetUserProperty(string name, string value);
    }

    public sealed class NullTrackingSink : ITrackingSink
    {
        public static readonly NullTrackingSink Instance = new();
        private NullTrackingSink() { }
        public void SendEvent(
            string eventName,
            IReadOnlyDictionary<string, object> parameters) { }
        public void SetUserProperty(string name, string value) { }
    }

    public interface ITrackingIdProvider
    {
        string NewId();
    }

    public sealed class GuidTrackingIdProvider : ITrackingIdProvider
    {
        public static readonly GuidTrackingIdProvider Instance = new();
        private GuidTrackingIdProvider() { }
        public string NewId() => Guid.NewGuid().ToString("D");
    }

    public static class TrackerCatalog
    {
        public static class Event
        {
            public const string ScreenShow = "scr_show";
            public const string DialogShow = "dlg_show";
            public const string ButtonClick = "btn_click";
            public const string GameStart = "game_start";
            public const string GameEnd = "game_end";
            public const string PropGet = "prop_get";
            public const string PropUse = "prop_use";
            public const string AdShowTiming = "ad_show_timing";
            public const string InterstitialAdShow =
                "interstitial_ad_show";
            public const string RewardedAdShow = "rewarded_ad_show";
            public const string SwitchClick = "sw_click";
            public const string NewGuideShow = "new_guide_show";
            public const string NewGuideEnd = "new_guide_end";
            public const string NewGuideStep = "new_guide_step";
            public const string PerfMonitor = "perf_monitor";
            public const string RemoveAppStart = "remove_app_start";
            public const string SparkStreak = "spark_streak";
            public const string PushGuideResult = "push_guide_result";
            public const string RankStart = "rank_start";
            public const string RankData = "rank_data";
            public const string FrameGet = "frame_get";
        }

        public static class Screen
        {
            public const string Splash = "splash_scr";
            public const string Home = "homepage_scr";
            public const string NormalGame = "normal_game_scr";
            public const string NormalWin = "normal_game_success_scr";
            public const string NormalFail = "normal_game_fail_scr";
            public const string DailyGame = "daily_game_scr";
            public const string DailyWin = "daily_game_success_scr";
            public const string DailyFail = "daily_game_fail_scr";
            public const string Feedback = "feedback_scr";
            public const string Streak = "streak_scr";
            public const string GameStreak = "game_streak_scr";
            public const string ChallengeRank = "challenge_rank_scr";
        }

        public static class Dialog
        {
            public const string Privacy = "privacy_dlg";
            public const string PreAttGuide = "pre_att_guide_dlg";
            public const string Rate = "rate_dlg";
            public const string Feedback = "feedback_dlg";
            public const string Settings = "settings_dlg";
            public const string Options = "options_dlg";
            public const string NormalToast = "game_normal_toast_dlg";
            public const string HardToast = "game_hard_toast_dlg";
            public const string RewardFail = "reward_fail_dlg";
            public const string LanguagePicker = "language_picker_dlg";
            public const string PushGuide = "push_guide_dlg";
            public const string ChallengeGuide = "challenge_guide_dlg";
            public const string Avatar = "avatar_dlg";
            public const string ChallengeRank = "challenge_rank_dlg";
            public const string ChallengeReward =
                "challenge_reward_dlg";
            public const string ChallengeRewardGet =
                "challenge_reward_get_dlg";
        }

        public static class GameType
        {
            public const string Normal = "normal";
            public const string Daily = "daily";
        }

        public static class GameStatus
        {
            public const string New = "new";
            public const string Continue = "continue";
            public const string Restart = "restart";
        }

        public static class GameResult
        {
            public const string Win = "win";
            public const string Fail = "fail";
            public const string Quit = "quit";
        }

        public static class Prop
        {
            public const string Hint = "hint";
            public const string Locate = "locate";
            public const string Undo = "undo";
        }

        public static class PropSource
        {
            public const string HintRewardAd = "hint_reward_ad";
            public const string LocateRewardAd = "locate_reward_ad";
            public const string UndoRewardAd = "undo_reward_ad";
            public const string RewardFailDialog = "reward_fail_dlg";
            public const string StreakChest = "streak_chest";
            public const string StreakRewardAd = "streak_reward_ad";
            public const string SwitchGroup = "switch_group";
            public const string ChallengeGetDialog = "challenge_get_dlg";
            public const string ChallengeRewardGetDialog =
                "challenge_reward_get_dlg";
        }

        public static class Placement
        {
            public const string Interstitial = "interstitial";
            public const string Reward = "reward";
            public const string Banner = "banner";
            public const string AppOpen = "appopen";
        }

        public static class AdPosition
        {
            public const string NormalGameFail = "normal_game_fail";
            public const string DailyGameFail = "daily_game_fail";
            public const string PropsNormalHint = "props_normal_hint";
            public const string PropsNormalLocate = "props_normal_locate";
            public const string PropsDailyHint = "props_daily_hint";
            public const string PropsDailyLocate = "props_daily_locate";
            public const string StreakDoubleReward = "streak_x2_reward";
            public const string StreakReviveReward = "streak_revive_reward";
            public const string RankReward = "rank_reward";
            public const string NormalStart = "normal_start";
            public const string NormalSuccess = "normal_success";
            public const string NormalRestart = "normal_restart";
            public const string NormalContinue = "normal_continue";
        }

        public static class Button
        {
            public const string NormalPlay = "normal_play";
            public const string DailyPlay = "daily_play";
            public const string Settings = "settings";
            public const string Streak = "streak";
            public const string GoToPlay = "gotoplay";
            public const string Back = "back";
            public const string Hint = "hint";
            public const string Locate = "locate";
            public const string Clear = "clear";
            public const string Coordinate = "coord";
            public const string HintApply = "hint_apply";
            public const string HintStop = "hint_stop";
            public const string HintDetail = "hint_detail";
            public const string Options = "options";
            public const string LevelPlay = "level_play";
            public const string Revive = "revive";
            public const string Restart = "restart";
            public const string TryAgain = "try_again";
            public const string Continue = "continue";
            public const string Close = "close";
            public const string Feedback = "feedback";
            public const string Terms = "terms";
            public const string Policy = "policy";
            public const string Privacy = "privacy";
            public const string PrivacyPreference = "privacy_preference";
            public const string Language = "language";
            public const string LanguageConfirm = "language_confirm";
            public const string LanguageCancel = "language_cancel";
            public const string Submit = "submit";
            public const string FeedbackRecord = "feedback_record";
            public const string Accept = "accept";
            public const string AttContinue = "att_continue";
            public const string RateUs = "rate_us";
            public const string Collect = "collect";
            public const string PushGuideJoin = "push_guide_join";
            public const string PushGuideClose = "push_guide_close";
            public const string Play = "play";
            public const string Save = "save";
            public const string CollectDouble = "collect_double";
            public const string ChallengeInfo = "challenge_info";
            public const string SelfInfo = "self_info";
            public const string ChallengeEntrance = "challenge_entrance";
        }

        public static class Switch
        {
            public const string Music = "music_sw";
            public const string Sound = "sound_sw";
            public const string Vibration = "vibration_sw";
            public const string Pattern = "pattern_sw";
        }

        public static class UserProperty
        {
            public const string UiLanguage = "ui_language";
        }
    }

    public sealed class TrackerService
    {
        private static readonly string[] RoundStatKeys =
        {
            "hint_used", "locate_used", "hint_apply_used",
            "hint_stop_used", "hint_detail_used", "clear_used",
            "step_used", "erase_count", "hint_cross_count"
        };

        private readonly GameStateService _gameState;
        private readonly ITrackingSink _sink;
        private readonly ITrackingIdProvider _ids;
        private readonly Dictionary<string, object> _mainStats = new();
        private readonly Dictionary<string, object> _dailyStats = new();
        private readonly List<string> _sourceStack = new();
        private readonly Dictionary<string, string> _pendingAdIds = new();
        private string _currentGameId = string.Empty;
        private string _activeGameType = string.Empty;

        public TrackerService(
            GameStateService gameState,
            ITrackingSink sink = null,
            ITrackingIdProvider ids = null)
        {
            _gameState = gameState ??
                         throw new ArgumentNullException(nameof(gameState));
            _sink = sink ?? NullTrackingSink.Instance;
            _ids = ids ?? GuidTrackingIdProvider.Instance;
        }

        public string CurrentGameId => _currentGameId;
        public string CurrentSource => _sourceStack.Count > 0
            ? _sourceStack[_sourceStack.Count - 1]
            : string.Empty;

        public void SetActiveGameType(string gameType)
        {
            _activeGameType = gameType ?? string.Empty;
            _currentGameId =
                _gameState.GetPersistedGameId(_activeGameType);
            Dictionary<string, object> active = ActiveStats();
            if (active.Count != 0) return;
            Dictionary<string, object> persisted =
                _gameState.GetGameRoundStats(_activeGameType);
            foreach (KeyValuePair<string, object> pair in persisted)
                active[pair.Key] = pair.Value;
        }

        public string NewGameId(string gameType)
        {
            _activeGameType = gameType ?? string.Empty;
            _currentGameId = _ids.NewId();
            ActiveStats().Clear();
            _gameState.ResetGameTotalStats(_activeGameType);
            _gameState.ResetGameRoundStats(_activeGameType);
            _gameState.SetPersistedGameId(
                _activeGameType,
                _currentGameId);
            return _currentGameId;
        }

        public void IncrementStat(string key, int delta = 1)
        {
            if (string.IsNullOrEmpty(key)) return;
            Dictionary<string, object> stats = ActiveStats();
            stats[key] = ReadInt(stats, key) + delta;
            _gameState.PersistGameRoundStats(_activeGameType, stats);
        }

        public int GetStat(string key) => string.IsNullOrEmpty(key)
            ? 0
            : ReadInt(ActiveStats(), key);

        public void ResetRoundStats()
        {
            Dictionary<string, object> stats = ActiveStats();
            for (int index = 0; index < RoundStatKeys.Length; index++)
                stats.Remove(RoundStatKeys[index]);
            _gameState.PersistGameRoundStats(_activeGameType, stats);
        }

        public void OnRestart()
        {
            ResetRoundStats();
            IncrementStat("restart_count");
        }

        public void NotifyDialogClosed(string dialogName)
        {
            if (string.IsNullOrEmpty(dialogName)) return;
            int index = _sourceStack.LastIndexOf(dialogName);
            if (index < 0) return;
            _sourceStack.RemoveRange(
                index,
                _sourceStack.Count - index);
        }

        public void TrackScreenShown(string screenName, string source = "")
        {
            if (string.IsNullOrEmpty(screenName)) return;
            string previous = string.IsNullOrEmpty(source)
                ? CurrentSource
                : source;
            var parameters = new Dictionary<string, object>
            {
                ["scr_name"] = screenName
            };
            AddSource(parameters, previous);
            Send(TrackerCatalog.Event.ScreenShow, parameters);
            _sourceStack.Clear();
            _sourceStack.Add(screenName);
        }

        public void TrackDialogShown(
            string dialogName,
            string source = "",
            IReadOnlyDictionary<string, object> extra = null)
        {
            if (string.IsNullOrEmpty(dialogName)) return;
            string previous = string.IsNullOrEmpty(source)
                ? CurrentSource
                : source;
            var parameters = new Dictionary<string, object>
            {
                ["dlg_name"] = dialogName
            };
            AddSource(parameters, previous);
            Merge(parameters, extra);
            Send(TrackerCatalog.Event.DialogShow, parameters);
            _sourceStack.Add(dialogName);
        }

        public void TrackButtonClick(
            string buttonName,
            string source = "",
            IReadOnlyDictionary<string, object> extra = null)
        {
            if (string.IsNullOrEmpty(buttonName)) return;
            var parameters = new Dictionary<string, object>
            {
                ["btn_name"] = buttonName
            };
            AddSource(
                parameters,
                string.IsNullOrEmpty(source) ? CurrentSource : source);
            Merge(parameters, extra);
            Send(TrackerCatalog.Event.ButtonClick, parameters);
        }

        public void TrackGameStart(
            string qid,
            string qrotate,
            string status,
            string gameType,
            int difficulty,
            int level,
            int strategyLayer,
            int scale,
            int isChallenge,
            string preType = "0")
        {
            Send(TrackerCatalog.Event.GameStart,
                new Dictionary<string, object>
                {
                    ["qid"] = qid,
                    ["qrotate"] = qrotate,
                    ["status"] = status,
                    ["game_type"] = gameType,
                    ["diffi"] = difficulty,
                    ["level"] = level,
                    ["strategy_layer"] = strategyLayer,
                    ["is_challenge"] = isChallenge,
                    ["scale"] = scale,
                    ["pre_type"] = preType
                });
        }

        public void TrackGameEnd(
            IReadOnlyDictionary<string, object> values) =>
            Send(TrackerCatalog.Event.GameEnd, Copy(values));

        public void TrackProp(
            bool acquired,
            string propName,
            string source,
            int propNum,
            int propLeft)
        {
            Send(
                acquired
                    ? TrackerCatalog.Event.PropGet
                    : TrackerCatalog.Event.PropUse,
                new Dictionary<string, object>
                {
                    ["prop_name"] = propName,
                    ["source"] = source,
                    ["prop_num"] = propNum,
                    ["prop_left"] = propLeft
                });
        }

        public string GenerateAdShowId() => _ids.NewId();

        public void RememberAdShowId(string placementType, string id)
        {
            if (!string.IsNullOrEmpty(placementType))
                _pendingAdIds[placementType] = id ?? string.Empty;
        }

        public string ConsumeAdShowId(string placementType)
        {
            if (string.IsNullOrEmpty(placementType) ||
                !_pendingAdIds.TryGetValue(placementType, out string id))
                return string.Empty;
            _pendingAdIds.Remove(placementType);
            return id;
        }

        public void TrackAdShowTiming(
            string adShowId,
            string placement,
            string placementType,
            string position)
        {
            Send(TrackerCatalog.Event.AdShowTiming,
                new Dictionary<string, object>
                {
                    ["ad_show_id"] = adShowId ?? string.Empty,
                    ["placement"] = placement ?? string.Empty,
                    ["placement_type"] = placementType ?? string.Empty,
                    ["position"] = position ?? string.Empty
                });
        }

        public void TrackInterstitialAdShow(
            string adShowId,
            int level,
            string position)
        {
            Send(TrackerCatalog.Event.InterstitialAdShow,
                new Dictionary<string, object>
                {
                    ["ad_show_id"] = adShowId ?? string.Empty,
                    ["level"] = level,
                    ["position"] = position ?? string.Empty
                });
        }

        public void TrackRewardedAdShow(
            string adShowId,
            int level,
            string position)
        {
            Send(TrackerCatalog.Event.RewardedAdShow,
                new Dictionary<string, object>
                {
                    ["ad_show_id"] = adShowId ?? string.Empty,
                    ["level"] = level,
                    ["position"] = position ?? string.Empty
                });
        }

        public void TrackSwitchClick(
            string switchName,
            int state,
            string source) =>
            Send(TrackerCatalog.Event.SwitchClick,
                new Dictionary<string, object>
                {
                    ["sw_name"] = switchName,
                    ["state"] = state,
                    ["source"] = source
                });

        public void TrackRankStart(int rankId) =>
            Send(TrackerCatalog.Event.RankStart,
                new Dictionary<string, object> { ["rank_id"] = rankId });

        public void TrackRankData(
            int rankId,
            string source,
            int rank,
            int normalNum,
            string nickname,
            int avatar,
            int frameId,
            int frameLevel,
            string resultDetail)
        {
            var parameters = new Dictionary<string, object>
            {
                ["rank_id"] = rankId,
                ["rank"] = rank,
                ["normal_num"] = normalNum,
                ["nick"] = nickname,
                ["avatar"] = avatar,
                ["frame_id"] = frameId,
                ["frame_level"] = frameLevel,
                ["result_detail"] = resultDetail
            };
            AddSource(parameters, source);
            Send(TrackerCatalog.Event.RankData, parameters);
        }

        public void TrackFrameGet(int frameId, string source)
        {
            var parameters = new Dictionary<string, object>
            {
                ["frame_id"] = frameId
            };
            AddSource(parameters, source);
            Send(TrackerCatalog.Event.FrameGet, parameters);
        }

        public void TrackUiLanguage(string languageCode) =>
            _sink.SetUserProperty(
                TrackerCatalog.UserProperty.UiLanguage,
                languageCode ?? string.Empty);

        public static string TransformToQuestionRotation(int transform)
        {
            int normalized = Math.Max(0, transform);
            string rotation = new[] { "0", "90", "180", "270" }
                [normalized % 4];
            if (normalized >= 8) return "V" + rotation;
            return normalized >= 4 ? "H" + rotation : rotation;
        }

        private Dictionary<string, object> ActiveStats() =>
            _activeGameType == TrackerCatalog.GameType.Daily
                ? _dailyStats
                : _mainStats;

        private void Send(
            string eventName,
            Dictionary<string, object> parameters)
        {
            if (!string.IsNullOrEmpty(_currentGameId) &&
                !parameters.ContainsKey("game_id"))
                parameters["game_id"] = _currentGameId;
            _sink.SendEvent(eventName, parameters);
        }

        private static void AddSource(
            Dictionary<string, object> parameters,
            string source)
        {
            if (!string.IsNullOrEmpty(source))
                parameters["source"] = source;
        }

        private static void Merge(
            Dictionary<string, object> target,
            IReadOnlyDictionary<string, object> values)
        {
            if (values == null) return;
            foreach (KeyValuePair<string, object> pair in values)
                target[pair.Key] = pair.Value;
        }

        private static Dictionary<string, object> Copy(
            IReadOnlyDictionary<string, object> values)
        {
            var result = new Dictionary<string, object>();
            Merge(result, values);
            return result;
        }

        private static int ReadInt(
            IReadOnlyDictionary<string, object> values,
            string key)
        {
            if (values == null ||
                !values.TryGetValue(key, out object value))
                return 0;
            try { return Convert.ToInt32(value); }
            catch (Exception) { return 0; }
        }
    }
}
