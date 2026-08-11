using System;
using System.Collections.Generic;
using Meowdoku.Core.Config;

namespace Meowdoku.Core.Daily
{
    public enum StreakCheckinSource
    {
        Main,
        Challenge
    }

    public enum StreakReviveAnimationKind
    {
        None,
        Resume,
        Backfill
    }

    public readonly struct StreakWeekSlot
    {
        public StreakWeekSlot(int weekday, bool isChecked)
        {
            Weekday = weekday;
            IsChecked = isChecked;
        }

        public int Weekday { get; }
        public bool IsChecked { get; }
    }

    public readonly struct StreakCheckinResult
    {
        public StreakCheckinResult(
            int streak,
            int bestStreak,
            bool hasReward,
            bool isNewStreak)
        {
            Streak = streak;
            BestStreak = bestStreak;
            HasReward = hasReward;
            IsNewStreak = isNewStreak;
        }

        public int Streak { get; }
        public int BestStreak { get; }
        public bool HasReward { get; }
        public bool IsNewStreak { get; }
    }

    public readonly struct StreakReviveInfo
    {
        public StreakReviveInfo(
            int brokenStreak,
            int missedDays,
            bool isResume)
        {
            BrokenStreak = brokenStreak;
            MissedDays = missedDays;
            IsResume = isResume;
        }

        public int BrokenStreak { get; }
        public int MissedDays { get; }
        public bool IsResume { get; }
    }

    public readonly struct StreakReviveResult
    {
        public StreakReviveResult(
            int streak,
            int bestStreak,
            bool hasReward,
            int missedDays,
            bool isResume)
        {
            Streak = streak;
            BestStreak = bestStreak;
            HasReward = hasReward;
            MissedDays = missedDays;
            IsResume = isResume;
        }

        public int Streak { get; }
        public int BestStreak { get; }
        public bool HasReward { get; }
        public int MissedDays { get; }
        public bool IsResume { get; }
    }

    public readonly struct StreakReviveAnimation
    {
        public StreakReviveAnimation(
            StreakReviveAnimationKind kind,
            int preCycle,
            int preWeekday,
            int gained)
        {
            Kind = kind;
            PreCycle = preCycle;
            PreWeekday = preWeekday;
            Gained = gained;
        }

        public StreakReviveAnimationKind Kind { get; }
        public int PreCycle { get; }
        public int PreWeekday { get; }
        public int Gained { get; }
    }

    public interface IStreakRewardBoundary
    {
        int DispatchStreakChest(IReadOnlyDictionary<string, int> rewards);
        void DispatchSwitchGift(IReadOnlyDictionary<string, int> rewards);
        void ShowAward(int uid);
    }

    internal sealed class NullStreakRewardBoundary : IStreakRewardBoundary
    {
        public static readonly NullStreakRewardBoundary Instance = new();
        private NullStreakRewardBoundary() { }
        public int DispatchStreakChest(
            IReadOnlyDictionary<string, int> rewards) => 0;
        public void DispatchSwitchGift(
            IReadOnlyDictionary<string, int> rewards) { }
        public void ShowAward(int uid) { }
    }

    public sealed class StreakFeature
    {
        public const int CycleLength = 7;

        private static readonly IReadOnlyDictionary<string, int> RewardBase =
            new Dictionary<string, int>
            {
                ["hint"] = 2,
                ["locate"] = 2
            };

        private readonly IStreakDataStore _store;
        private readonly ICurrentDateProvider _dateProvider;
        private readonly DailyStreakConfig _streakConfig;
        private readonly StreakProtectConfig _protectConfig;
        private readonly IStreakRewardBoundary _rewardBoundary;
        private StreakData _data;
        private int _pendingShowUid = -1;
        private bool _pendingSwitchEligible;
        private int _lastSeenJulianDay;
        private StreakReviveAnimation _reviveAnimation;

        public StreakFeature(
            IStreakDataStore store = null,
            ICurrentDateProvider dateProvider = null,
            DailyStreakConfig streakConfig = null,
            StreakProtectConfig protectConfig = null,
            IStreakRewardBoundary rewardBoundary = null,
            StreakData initialData = null)
        {
            _store = store;
            _dateProvider = dateProvider ?? SystemCurrentDateProvider.Instance;
            _streakConfig = streakConfig ?? new DailyStreakConfig();
            _protectConfig = protectConfig ?? new StreakProtectConfig();
            _rewardBoundary = rewardBoundary ??
                              NullStreakRewardBoundary.Instance;
            _data = initialData ?? _store?.Load() ?? new StreakData();
            ResolvePendingWinCheckin();
            _pendingSwitchEligible = _data.PendingSwitchPage > 0;
            _lastSeenJulianDay = TodayJulianDay;
        }

        public event Action<StreakData> StreakUpdated;
        public event Action<StreakCheckinResult> CheckinCompleted;

        public StreakData Data => _data;
        public StreakReviveAnimation ReviveAnimation => _reviveAnimation;
        public int PendingShowUid => _pendingShowUid;
        public bool HasPendingShow => _pendingShowUid >= 0;
        public bool HasPendingReviveDecision =>
            !string.IsNullOrEmpty(_data.PendingWinCheckinDate);
        public bool IsEnabled => _streakConfig.IsEnabled();
        public bool HasReward => _streakConfig.HasReward();
        public bool ShouldSkipLit => _streakConfig.IsSkipLit();
        public bool HasPlayEntry => _streakConfig.HasPlayEntry();
        public bool IsSettleReorder => _streakConfig.IsSettleReorder();

        private string Today => _dateProvider.CurrentDate ?? string.Empty;
        private int TodayJulianDay => StreakDateMath.DateToJulianDay(Today);

        public bool IsUnlocked(bool tutorialDone)
        {
            return IsEnabled && tutorialDone;
        }

        public bool CanCheckinToday()
        {
            if (string.IsNullOrEmpty(_data.LastCheckinDate)) return true;
            return TodayJulianDay >
                   StreakDateMath.DateToJulianDay(_data.LastCheckinDate);
        }

        public bool IsWinQualified(StreakCheckinSource source)
        {
            if (!IsEnabled) return false;
            if (_streakConfig.IsChallengeOnly())
                return source == StreakCheckinSource.Challenge;
            return source == StreakCheckinSource.Main ||
                   source == StreakCheckinSource.Challenge;
        }

        public bool NotifyWin(
            StreakCheckinSource source,
            bool tutorialDone,
            out StreakCheckinResult result)
        {
            result = default;
            if (!IsUnlocked(tutorialDone) || !CanCheckinToday() ||
                !IsWinQualified(source))
                return false;
            result = DoCheckin();
            return true;
        }

        public StreakCheckinResult DoCheckin()
        {
            bool wasBroken = IsBroken();
            _reviveAnimation = default;
            _data.PendingWinCheckinDate = string.Empty;
            _data.LastCheckinDate = Today;

            if (wasBroken)
            {
                _data.CurrentStreak = 1;
                _data.RewardCycleDay = 1;
                _data.StreakStartWeekday = TodayWeekday;
            }
            else
            {
                _data.CurrentStreak++;
                _data.RewardCycleDay++;
                if (_data.StreakStartWeekday < 0)
                    _data.StreakStartWeekday = TodayWeekday;
            }

            if (_data.CurrentStreak > _data.BestStreak)
                _data.BestStreak = _data.CurrentStreak;

            _pendingShowUid = 0;
            bool hasReward = false;
            if (_data.RewardCycleDay > 0 &&
                _data.RewardCycleDay % CycleLength == 0 &&
                _streakConfig.HasReward())
            {
                hasReward = true;
                _pendingShowUid =
                    _rewardBoundary.DispatchStreakChest(RewardBase);
            }

            Save();
            var result = new StreakCheckinResult(
                _data.CurrentStreak,
                _data.BestStreak,
                hasReward,
                _data.CurrentStreak == 1);
            CheckinCompleted?.Invoke(result);
            StreakUpdated?.Invoke(_data);
            return result;
        }

        public bool ShouldOfferRevive(
            StreakCheckinSource source,
            bool tutorialDone)
        {
            if (!IsUnlocked(tutorialDone) || !CanCheckinToday() ||
                !IsWinQualified(source) || !IsBroken() ||
                _data.CurrentStreak < 2)
                return false;
            int maximum = _protectConfig.ReviveMaxDays();
            return maximum > 0 && MissedDays() <= maximum;
        }

        public void SettleWin(
            StreakCheckinSource source,
            bool tutorialDone)
        {
            if (ShouldOfferRevive(source, tutorialDone))
                MarkPendingWinCheckin();
            else
                NotifyWin(source, tutorialDone, out _);
        }

        public void MarkPendingWinCheckin()
        {
            _data.PendingWinCheckinDate = Today;
            Save();
        }

        public StreakReviveInfo GetReviveInfo()
        {
            return new StreakReviveInfo(
                _data.CurrentStreak,
                MissedDays(),
                _protectConfig.IsResume());
        }

        public StreakReviveResult ReviveStreak()
        {
            int missed = MissedDays();
            bool hasReward = false;
            _pendingShowUid = 0;

            if (_protectConfig.IsResume())
            {
                _reviveAnimation = new StreakReviveAnimation(
                    StreakReviveAnimationKind.Resume,
                    _data.RewardCycleDay,
                    _data.StreakStartWeekday,
                    0);
                _data.CurrentStreak++;
                _data.RewardCycleDay = 1;
                _data.StreakStartWeekday = TodayWeekday;
            }
            else
            {
                int add = missed + 1;
                int beforeCycle = _data.RewardCycleDay;
                _data.CurrentStreak += add;
                _data.RewardCycleDay += add;
                if (_data.StreakStartWeekday < 0)
                    _data.StreakStartWeekday = TodayWeekday;

                if (_streakConfig.HasReward() &&
                    _data.RewardCycleDay / CycleLength >
                    beforeCycle / CycleLength)
                {
                    hasReward = true;
                    _pendingShowUid =
                        _rewardBoundary.DispatchStreakChest(RewardBase);
                }
                _reviveAnimation = new StreakReviveAnimation(
                    StreakReviveAnimationKind.Backfill,
                    beforeCycle,
                    -1,
                    add);
            }

            _data.LastCheckinDate = Today;
            _data.PendingWinCheckinDate = string.Empty;
            if (_data.CurrentStreak > _data.BestStreak)
                _data.BestStreak = _data.CurrentStreak;
            Save();
            var result = new StreakReviveResult(
                _data.CurrentStreak,
                _data.BestStreak,
                hasReward,
                missed,
                _protectConfig.IsResume());
            StreakUpdated?.Invoke(_data);
            return result;
        }

        public StreakCheckinResult GiveUpRevive()
        {
            return DoCheckin();
        }

        public int DisplayStreak => IsBroken() ? 0 : _data.CurrentStreak;

        public IReadOnlyList<StreakWeekSlot> GetWeekSlots()
        {
            int startWeekday = _data.StreakStartWeekday >= 0
                ? _data.StreakStartWeekday
                : TodayWeekday;
            int filled = 0;
            if (!IsBroken() && _data.RewardCycleDay > 0)
            {
                int mod = _data.RewardCycleDay % CycleLength;
                if (mod == 0)
                {
                    if (!CanCheckinToday()) filled = CycleLength;
                }
                else
                {
                    filled = mod;
                }
            }

            var slots = new StreakWeekSlot[CycleLength];
            for (int index = 0; index < CycleLength; index++)
                slots[index] = new StreakWeekSlot(
                    (startWeekday + index) % CycleLength,
                    index < filled);
            return slots;
        }

        public bool ClaimReward()
        {
            if (_pendingShowUid < 0) return false;
            if (_pendingShowUid > 0)
                _rewardBoundary.ShowAward(_pendingShowUid);
            StreakUpdated?.Invoke(_data);
            return true;
        }

        public void ConsumePendingShow()
        {
            _pendingShowUid = -1;
            _reviveAnimation = default;
        }

        public bool IsBroken()
        {
            if (string.IsNullOrEmpty(_data.LastCheckinDate)) return false;
            return TodayJulianDay -
                   StreakDateMath.DateToJulianDay(
                       _data.LastCheckinDate) > 1;
        }

        public int MissedDays()
        {
            if (string.IsNullOrEmpty(_data.LastCheckinDate)) return 0;
            return Math.Max(
                0,
                TodayJulianDay -
                StreakDateMath.DateToJulianDay(
                    _data.LastCheckinDate) - 1);
        }

        public bool TickDayWatch()
        {
            int today = TodayJulianDay;
            if (today == _lastSeenJulianDay) return false;
            _lastSeenJulianDay = today;
            StreakUpdated?.Invoke(_data);
            return true;
        }

        public void NotifyGroupDyed(int currentGroup)
        {
            if (_data.LastGroup == -1)
            {
                if (currentGroup != 0)
                {
                    _data.LastGroup = currentGroup;
                    Save();
                }
                return;
            }
            if (currentGroup == _data.LastGroup) return;

            int page = MapSwitchPage(_data.LastGroup, currentGroup);
            if (page > 0)
            {
                _data.PendingSwitchPage = page;
                _pendingSwitchEligible = true;
            }
            _data.LastGroup = currentGroup != 0 ? currentGroup : -1;
            Save();
        }

        public int PendingSwitchPage => _pendingSwitchEligible
            ? _data.PendingSwitchPage
            : 0;

        public void ConsumePendingSwitch()
        {
            _data.PendingSwitchPage = 0;
            _pendingSwitchEligible = false;
            Save();
        }

        public void GrantSwitchGift()
        {
            _rewardBoundary.DispatchSwitchGift(RewardBase);
        }

        public void MergeRemote(IReadOnlyDictionary<string, object> remote)
        {
            if (remote == null || remote.Count == 0) return;
            Dictionary<string, object> local = _data.ToDictionary();
            int localDay = string.IsNullOrEmpty(_data.LastCheckinDate)
                ? 0
                : StreakDateMath.DateToJulianDay(
                    _data.LastCheckinDate);
            string remoteDate = StreakData.ReadString(
                remote,
                "last_checkin_date");
            int remoteDay = string.IsNullOrEmpty(remoteDate)
                ? 0
                : StreakDateMath.DateToJulianDay(remoteDate);
            StreakData merged = StreakData.ResolveMerge(
                local,
                remote,
                localDay,
                remoteDay);
            _data.CurrentStreak = merged.CurrentStreak;
            _data.BestStreak = merged.BestStreak;
            _data.LastCheckinDate = merged.LastCheckinDate;
            _data.StreakStartWeekday = merged.StreakStartWeekday;
            _data.RewardCycleDay = merged.RewardCycleDay;
            Save();
            StreakUpdated?.Invoke(_data);
        }

        public void Reset()
        {
            _data = new StreakData();
            _pendingShowUid = -1;
            _reviveAnimation = default;
            _pendingSwitchEligible = false;
            _store?.Reset();
            Save();
            StreakUpdated?.Invoke(_data);
        }

        public static int MapSwitchPage(int oldGroup, int newGroup)
        {
            oldGroup = NormalizeSwitchGroup(oldGroup);
            newGroup = NormalizeSwitchGroup(newGroup);
            if ((oldGroup == 1 || oldGroup == 2 || oldGroup == 4) &&
                (newGroup == 0 || newGroup == 3))
                return 1;
            if (oldGroup == 3 && newGroup == 0) return 2;
            if (oldGroup == 3 &&
                (newGroup == 1 || newGroup == 2 || newGroup == 4))
                return 3;
            return 0;
        }

        private int TodayWeekday => StreakDateMath.Weekday(Today);

        private void ResolvePendingWinCheckin()
        {
            string date = _data.PendingWinCheckinDate;
            if (string.IsNullOrEmpty(date)) return;
            _data.PendingWinCheckinDate = string.Empty;
            _data.CurrentStreak = 1;
            _data.RewardCycleDay = 1;
            _data.LastCheckinDate = date;
            int weekday = StreakDateMath.Weekday(date);
            _data.StreakStartWeekday = weekday >= 0
                ? weekday
                : TodayWeekday;
            if (_data.BestStreak < 1) _data.BestStreak = 1;
            Save();
        }

        private void Save()
        {
            _store?.Save(_data);
        }

        private static int NormalizeSwitchGroup(int group)
        {
            return group == 1 || group == 6 || group == 7 ? 1 : group;
        }
    }

    public static class StreakDateMath
    {
        public static int DateToJulianDay(string date)
        {
            if (!TryParse(date, out int year, out int month, out int day))
                return 0;
            int a = (14 - month) / 12;
            int y = year + 4800 - a;
            int m = month + 12 * a - 3;
            return day + (153 * m + 2) / 5 + 365 * y + y / 4 -
                   y / 100 + y / 400 - 32045;
        }

        public static int Weekday(string date)
        {
            if (!TryParse(date, out int year, out int month, out int day))
                return -1;
            try
            {
                return (int)new DateTime(year, month, day).DayOfWeek;
            }
            catch (ArgumentOutOfRangeException)
            {
                return -1;
            }
        }

        public static string Offset(string date, int days)
        {
            if (!TryParse(date, out int year, out int month, out int day))
                return date ?? string.Empty;
            try
            {
                return new DateTime(year, month, day)
                    .AddDays(days)
                    .ToString("yyyy-MM-dd");
            }
            catch (ArgumentOutOfRangeException)
            {
                return date ?? string.Empty;
            }
        }

        private static bool TryParse(
            string date,
            out int year,
            out int month,
            out int day)
        {
            year = month = day = 0;
            if (string.IsNullOrEmpty(date)) return false;
            string[] parts = date.Split('-');
            return parts.Length >= 3 &&
                   int.TryParse(parts[0], out year) &&
                   int.TryParse(parts[1], out month) &&
                   int.TryParse(parts[2], out day);
        }
    }
}
