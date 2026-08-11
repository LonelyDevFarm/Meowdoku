using System;
using System.Collections.Generic;
using System.Globalization;
using Meowdoku.Core.Config;
using Meowdoku.Core.Daily;

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
    public sealed class GameStateService
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
