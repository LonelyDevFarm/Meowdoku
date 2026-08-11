using System;
using System.Collections.Generic;

namespace Meowdoku.Core.Rank
{
    public enum RankActivityState
    {
        NotOpened = 0,
        OpenNotJoined = 1,
        OpenJoined = 2,
        Settling = 3
    }

    public sealed class RankActivityData
    {
        public int PeriodCount { get; set; }
        public int PreviousScore { get; set; }
        public int PreviousRank { get; set; }
        public bool PreviousAwarded { get; set; }
        public RankActivityState State { get; set; }
        public int Group { get; set; }
        public long StartUnix { get; set; }
        public long EndUnix { get; set; }
        public string RobotKey { get; set; } = string.Empty;
        public bool Joined { get; set; }
        public int CollectTotal { get; set; }
        public long PlayerScoreUnix { get; set; }
        public bool Settled { get; set; }
        public int FinalRank { get; set; }
        public bool RewardClaimed { get; set; }
        public int WinsSinceClose { get; set; }
        public int LevelCache { get; set; }
        public bool LevelCacheActive { get; set; }
        public int BestEncouragedRank { get; set; }
        public int Place2Wins { get; set; }
        public int Place3Wins { get; set; }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["period_count"] = PeriodCount,
                ["prev_score"] = PreviousScore,
                ["prev_rank"] = PreviousRank,
                ["prev_awarded"] = PreviousAwarded,
                ["state"] = (int)State,
                ["group"] = Group,
                ["start_unix"] = StartUnix,
                ["end_unix"] = EndUnix,
                ["robot_key"] = RobotKey ?? string.Empty,
                ["joined"] = Joined,
                ["collect_total"] = CollectTotal,
                ["player_score_unix"] = PlayerScoreUnix,
                ["settled"] = Settled,
                ["final_rank"] = FinalRank,
                ["reward_claimed"] = RewardClaimed,
                ["wins_since_close"] = WinsSinceClose,
                ["level_cache"] = LevelCache,
                ["level_cache_active"] = LevelCacheActive,
                ["best_encouraged_rank"] = BestEncouragedRank,
                ["place2_wins"] = Place2Wins,
                ["place3_wins"] = Place3Wins
            };
        }

        public static RankActivityData FromDictionary(
            IReadOnlyDictionary<string, object> values)
        {
            var data = new RankActivityData();
            if (values == null) return data;
            data.PeriodCount = RankValue.Int(values, "period_count");
            data.PreviousScore = RankValue.Int(values, "prev_score");
            data.PreviousRank = RankValue.Int(values, "prev_rank");
            data.PreviousAwarded = RankValue.Bool(values, "prev_awarded");
            data.State = (RankActivityState)RankValue.Int(values, "state");
            data.Group = RankValue.Int(values, "group");
            data.StartUnix = RankValue.Long(values, "start_unix");
            data.EndUnix = RankValue.Long(values, "end_unix");
            data.RobotKey = RankValue.String(values, "robot_key");
            data.Joined = RankValue.Bool(values, "joined");
            data.CollectTotal = RankValue.Int(values, "collect_total");
            data.PlayerScoreUnix = RankValue.Long(
                values,
                "player_score_unix");
            data.Settled = RankValue.Bool(values, "settled");
            data.FinalRank = RankValue.Int(values, "final_rank");
            data.RewardClaimed = RankValue.Bool(values, "reward_claimed");
            data.WinsSinceClose = RankValue.Int(values, "wins_since_close");
            data.LevelCache = RankValue.Int(values, "level_cache");
            data.LevelCacheActive = RankValue.Bool(
                values,
                "level_cache_active");
            data.BestEncouragedRank = RankValue.Int(
                values,
                "best_encouraged_rank");
            data.Place2Wins = RankValue.Int(values, "place2_wins");
            data.Place3Wins = RankValue.Int(values, "place3_wins");
            return data;
        }
    }

    public sealed class RankSettlementResult
    {
        public int Rank { get; set; }
        public bool Awarded { get; set; }
        public bool IsFirst { get; set; }
        public int CollectTotal { get; set; }
        public int Group { get; set; }

        public Dictionary<string, object> ToDictionary() => new()
        {
            ["rank"] = Rank,
            ["awarded"] = Awarded,
            ["is_first"] = IsFirst,
            ["collect_total"] = CollectTotal,
            ["group"] = Group
        };

        public RankSettlementResult Clone() => new()
        {
            Rank = Rank,
            Awarded = Awarded,
            IsFirst = IsFirst,
            CollectTotal = CollectTotal,
            Group = Group
        };
    }

    public enum RankEncouragementKind
    {
        None,
        Reach,
        Climb
    }

    public readonly struct RankProgressEncouragement
    {
        public RankProgressEncouragement(
            RankEncouragementKind kind,
            int rank = 0,
            int advance = 0)
        {
            Kind = kind;
            Rank = rank;
            Advance = advance;
        }

        public RankEncouragementKind Kind { get; }
        public int Rank { get; }
        public int Advance { get; }
    }

    internal static class RankValue
    {
        public static int Int(
            IReadOnlyDictionary<string, object> values,
            string key,
            int fallback = 0) => ConvertValue(
                values,
                key,
                fallback,
                Convert.ToInt32);

        public static long Long(
            IReadOnlyDictionary<string, object> values,
            string key,
            long fallback = 0) => ConvertValue(
                values,
                key,
                fallback,
                Convert.ToInt64);

        public static bool Bool(
            IReadOnlyDictionary<string, object> values,
            string key,
            bool fallback = false) => ConvertValue(
                values,
                key,
                fallback,
                Convert.ToBoolean);

        public static string String(
            IReadOnlyDictionary<string, object> values,
            string key,
            string fallback = "")
        {
            return values != null && values.TryGetValue(key, out object raw) &&
                   raw != null
                ? Convert.ToString(raw) ?? fallback
                : fallback;
        }

        private static T ConvertValue<T>(
            IReadOnlyDictionary<string, object> values,
            string key,
            T fallback,
            Func<object, T> convert)
        {
            if (values == null || !values.TryGetValue(key, out object raw) ||
                raw == null)
                return fallback;
            try { return convert(raw); }
            catch (Exception) { return fallback; }
        }
    }
}
