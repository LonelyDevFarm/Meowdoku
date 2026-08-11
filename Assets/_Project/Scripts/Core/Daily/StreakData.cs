using System;
using System.Collections.Generic;

namespace Meowdoku.Core.Daily
{
    public sealed class StreakData
    {
        public const int BestStreakCap = 36500;

        public int CurrentStreak { get; set; }
        public int BestStreak { get; set; }
        public string LastCheckinDate { get; set; } = string.Empty;
        public int StreakStartWeekday { get; set; } = -1;
        public int RewardCycleDay { get; set; }
        public int LastGroup { get; set; } = -1;
        public int PendingSwitchPage { get; set; }
        public string PendingWinCheckinDate { get; set; } = string.Empty;

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["current_streak"] = CurrentStreak,
                ["best_streak"] = BestStreak,
                ["last_checkin_date"] = LastCheckinDate ?? string.Empty,
                ["streak_start_weekday"] = StreakStartWeekday,
                ["reward_cycle_day"] = RewardCycleDay,
                ["last_group"] = LastGroup,
                ["pending_switch_page"] = PendingSwitchPage,
                ["pending_win_checkin_date"] =
                    PendingWinCheckinDate ?? string.Empty
            };
        }

        public static StreakData FromDictionary(
            IReadOnlyDictionary<string, object> dictionary)
        {
            var data = new StreakData();
            if (dictionary == null) return data;

            data.CurrentStreak = ReadInt(dictionary, "current_streak");
            data.BestStreak = ReadInt(dictionary, "best_streak");
            data.LastCheckinDate = ReadString(
                dictionary,
                "last_checkin_date");
            data.StreakStartWeekday = ReadInt(
                dictionary,
                "streak_start_weekday",
                -1);
            data.RewardCycleDay = ReadInt(dictionary, "reward_cycle_day");
            data.LastGroup = ReadInt(dictionary, "last_group", -1);
            data.PendingSwitchPage = ReadInt(
                dictionary,
                "pending_switch_page");
            data.PendingWinCheckinDate = ReadString(
                dictionary,
                "pending_win_checkin_date");
            return data;
        }

        public static StreakData ResolveMerge(
            IReadOnlyDictionary<string, object> local,
            IReadOnlyDictionary<string, object> remote,
            int localJulianDay,
            int remoteJulianDay)
        {
            local ??= new Dictionary<string, object>();
            remote ??= new Dictionary<string, object>();

            int current = Math.Max(
                0,
                Math.Max(
                    ReadInt(local, "current_streak"),
                    ReadInt(remote, "current_streak")));
            int best = Math.Max(
                ReadInt(local, "best_streak"),
                ReadInt(remote, "best_streak"));
            int localCycle = ReadInt(
                local,
                "reward_cycle_day",
                ReadInt(local, "current_streak"));
            int remoteCycle = ReadInt(
                remote,
                "reward_cycle_day",
                ReadInt(remote, "current_streak"));
            int cycle = Math.Max(0, Math.Max(localCycle, remoteCycle));

            IReadOnlyDictionary<string, object> source =
                remoteJulianDay > localJulianDay ? remote : local;
            int weekday = ReadInt(source, "streak_start_weekday", -1);
            if (weekday < -1 || weekday > 6) weekday = -1;

            return new StreakData
            {
                CurrentStreak = current,
                BestStreak = Math.Clamp(
                    Math.Max(best, current),
                    0,
                    BestStreakCap),
                LastCheckinDate = ReadString(
                    source,
                    "last_checkin_date"),
                StreakStartWeekday = weekday,
                RewardCycleDay = cycle
            };
        }

        internal static int ReadInt(
            IReadOnlyDictionary<string, object> dictionary,
            string key,
            int fallback = 0)
        {
            if (dictionary == null ||
                !dictionary.TryGetValue(key, out object value) ||
                value == null)
                return fallback;
            try
            {
                return Convert.ToInt32(value);
            }
            catch (Exception)
            {
                return fallback;
            }
        }

        internal static string ReadString(
            IReadOnlyDictionary<string, object> dictionary,
            string key)
        {
            return dictionary != null &&
                   dictionary.TryGetValue(key, out object value) &&
                   value != null
                ? Convert.ToString(value) ?? string.Empty
                : string.Empty;
        }
    }
}
