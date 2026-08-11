using System;
using System.Collections;
using System.Collections.Generic;
using Meowdoku.Core.Profile;

namespace Meowdoku.Core.Robot
{
    public sealed class RankInfo
    {
        public PlayerInfo PlayerInfo { get; set; }
        public int Rank { get; set; }
        public int Score { get; set; }
        public int AwardId { get; set; }
        public bool IsSelf => PlayerInfo != null && PlayerInfo.IsSelf;
    }

    public sealed class RobotTimelinePoint
    {
        public int Minute { get; set; }
        public int Delta { get; set; }
        public long? Timestamp { get; set; }

        public Dictionary<string, object> ToDictionary()
        {
            var result = new Dictionary<string, object>
            {
                ["min"] = Minute,
                ["delta"] = Delta
            };
            if (Timestamp.HasValue) result["ts"] = Timestamp.Value;
            return result;
        }

        public static RobotTimelinePoint FromDictionary(
            IReadOnlyDictionary<string, object> dictionary)
        {
            var point = new RobotTimelinePoint
            {
                Minute = RobotValue.ReadInt(dictionary, "min"),
                Delta = RobotValue.ReadInt(dictionary, "delta")
            };
            if (dictionary != null && dictionary.ContainsKey("ts"))
                point.Timestamp = RobotValue.ReadLong(dictionary, "ts");
            return point;
        }
    }

    public sealed class RobotData
    {
        public int Id { get; set; }
        public string Nickname { get; set; } = string.Empty;
        public int AvatarId { get; set; }
        public int FrameId { get; set; }
        public bool IsFirstFrame { get; set; }
        public int FrameBadge { get; set; }
        public int FinalScore { get; set; }
        public List<RobotTimelinePoint> Timeline { get; } = new();
        public bool Stalking { get; set; }
        public float LastUpdateMinute { get; set; } = -1f;

        public Dictionary<string, object> ToDictionary()
        {
            var timeline = new List<object>(Timeline.Count);
            for (int index = 0; index < Timeline.Count; index++)
                timeline.Add(Timeline[index].ToDictionary());
            return new Dictionary<string, object>
            {
                ["id"] = Id,
                ["nickname"] = Nickname ?? string.Empty,
                ["avatar_id"] = AvatarId,
                ["frame_id"] = FrameId,
                ["is_first_frame"] = IsFirstFrame,
                ["frame_badge"] = FrameBadge,
                ["final_score"] = FinalScore,
                ["timeline"] = timeline,
                ["stalking"] = Stalking,
                ["last_update_min"] = LastUpdateMinute
            };
        }

        public static RobotData FromDictionary(
            IReadOnlyDictionary<string, object> dictionary)
        {
            var robot = new RobotData
            {
                Id = RobotValue.ReadInt(dictionary, "id"),
                Nickname = RobotValue.ReadString(dictionary, "nickname"),
                AvatarId = RobotValue.ReadInt(dictionary, "avatar_id"),
                FrameId = RobotValue.ReadInt(dictionary, "frame_id"),
                IsFirstFrame = RobotValue.ReadBool(
                    dictionary,
                    "is_first_frame"),
                FrameBadge = RobotValue.ReadInt(dictionary, "frame_badge"),
                FinalScore = RobotValue.ReadInt(dictionary, "final_score"),
                Stalking = RobotValue.ReadBool(dictionary, "stalking"),
                LastUpdateMinute = RobotValue.ReadFloat(
                    dictionary,
                    "last_update_min",
                    -1f)
            };
            if (dictionary != null &&
                dictionary.TryGetValue("timeline", out object timeline) &&
                timeline is IList list)
            {
                for (int index = 0; index < list.Count; index++)
                    if (list[index] is
                        IReadOnlyDictionary<string, object> point)
                        robot.Timeline.Add(
                            RobotTimelinePoint.FromDictionary(point));
            }
            return robot;
        }
    }

    public sealed class RobotConfig
    {
        public int RobotCount { get; set; } = 30;
        public int MinimumScoringRobots { get; set; }
        public string ArrayStrategy { get; set; } = "closest_approach";
        public int BaseFloor { get; set; } = 118;
        public int Ceiling { get; set; } = 960;
        public int BotOffset { get; set; } = 8;
        public float RandomPower { get; set; } = 1f;
        public float AlphaFirst { get; set; } = 0.08f;
        public float AlphaTop10 { get; set; } = 0.03f;
        public float AlphaTop30 { get; set; } = -0.1f;
        public float AlphaRest { get; set; } = -0.2f;
        public List<int> ArrayValues { get; } = new() { 8, 9, 10 };
        public List<float> ArrayWeights { get; } = new() { 0.1f, 0.2f, 0.7f };
        public int TotalMinutes { get; set; } = 1440;
        public string TimelineFormat { get; set; } =
            "30;2,60;1,120;1,240;1,1320;5,1440;1";
        public int FirstHourForcedCount { get; set; } = 2;
        public int FirstHourMinutes { get; set; } = 60;
        public int CooldownMinutes { get; set; } = 3;
        public int StalkTopPool { get; set; } = 10;
        public int StalkSlotDivisor { get; set; } = 25;
        public int StalkMinimumGap { get; set; } = 2;
        public float StalkDeltaTimeFactor { get; set; } = 8f;
        public List<int> StalkValues { get; } = new() { 8, 9, 10 };
        public int StalkOvertakeDelayMinimum { get; set; } = 5;
        public int StalkOvertakeDelayMaximum { get; set; } = 30;

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["robot_count"] = RobotCount,
                ["min_scoring_robots"] = MinimumScoringRobots,
                ["array_strategy"] = ArrayStrategy ?? string.Empty,
                ["base_floor"] = BaseFloor,
                ["ceiling"] = Ceiling,
                ["bot_offset"] = BotOffset,
                ["rand_power"] = RandomPower,
                ["alpha_first"] = AlphaFirst,
                ["alpha_top10"] = AlphaTop10,
                ["alpha_top30"] = AlphaTop30,
                ["alpha_rest"] = AlphaRest,
                ["arr_values"] = new List<int>(ArrayValues),
                ["arr_weights"] = new List<float>(ArrayWeights),
                ["total_minutes"] = TotalMinutes,
                ["timeline_format"] = TimelineFormat ?? string.Empty,
                ["first_hour_forced_count"] = FirstHourForcedCount,
                ["first_hour_minutes"] = FirstHourMinutes,
                ["cooldown_minutes"] = CooldownMinutes,
                ["stalk_top_pool"] = StalkTopPool,
                ["stalk_slot_div"] = StalkSlotDivisor,
                ["stalk_min_gap"] = StalkMinimumGap,
                ["stalk_dt_factor"] = StalkDeltaTimeFactor,
                ["stalk_values"] = new List<int>(StalkValues),
                ["stalk_overtake_delay_min"] =
                    StalkOvertakeDelayMinimum,
                ["stalk_overtake_delay_max"] =
                    StalkOvertakeDelayMaximum
            };
        }

        public static RobotConfig FromDictionary(
            IReadOnlyDictionary<string, object> dictionary)
        {
            var config = new RobotConfig();
            if (dictionary == null) return config;
            config.RobotCount = RobotValue.ReadInt(
                dictionary,
                "robot_count",
                config.RobotCount);
            config.MinimumScoringRobots = RobotValue.ReadInt(
                dictionary,
                "min_scoring_robots",
                config.MinimumScoringRobots);
            config.ArrayStrategy = RobotValue.ReadString(
                dictionary,
                "array_strategy",
                config.ArrayStrategy);
            config.BaseFloor = RobotValue.ReadInt(
                dictionary,
                "base_floor",
                config.BaseFloor);
            config.Ceiling = RobotValue.ReadInt(
                dictionary,
                "ceiling",
                config.Ceiling);
            config.BotOffset = RobotValue.ReadInt(
                dictionary,
                "bot_offset",
                config.BotOffset);
            config.RandomPower = RobotValue.ReadFloat(
                dictionary,
                "rand_power",
                config.RandomPower);
            config.AlphaFirst = RobotValue.ReadFloat(
                dictionary,
                "alpha_first",
                config.AlphaFirst);
            config.AlphaTop10 = RobotValue.ReadFloat(
                dictionary,
                "alpha_top10",
                config.AlphaTop10);
            config.AlphaTop30 = RobotValue.ReadFloat(
                dictionary,
                "alpha_top30",
                config.AlphaTop30);
            config.AlphaRest = RobotValue.ReadFloat(
                dictionary,
                "alpha_rest",
                config.AlphaRest);
            ReplaceInts(config.ArrayValues, dictionary, "arr_values");
            ReplaceFloats(config.ArrayWeights, dictionary, "arr_weights");
            config.TotalMinutes = RobotValue.ReadInt(
                dictionary,
                "total_minutes",
                config.TotalMinutes);
            config.TimelineFormat = RobotValue.ReadString(
                dictionary,
                "timeline_format",
                config.TimelineFormat);
            config.FirstHourForcedCount = RobotValue.ReadInt(
                dictionary,
                "first_hour_forced_count",
                config.FirstHourForcedCount);
            config.FirstHourMinutes = RobotValue.ReadInt(
                dictionary,
                "first_hour_minutes",
                config.FirstHourMinutes);
            config.CooldownMinutes = RobotValue.ReadInt(
                dictionary,
                "cooldown_minutes",
                config.CooldownMinutes);
            config.StalkTopPool = RobotValue.ReadInt(
                dictionary,
                "stalk_top_pool",
                config.StalkTopPool);
            config.StalkSlotDivisor = RobotValue.ReadInt(
                dictionary,
                "stalk_slot_div",
                config.StalkSlotDivisor);
            config.StalkMinimumGap = RobotValue.ReadInt(
                dictionary,
                "stalk_min_gap",
                config.StalkMinimumGap);
            config.StalkDeltaTimeFactor = RobotValue.ReadFloat(
                dictionary,
                "stalk_dt_factor",
                config.StalkDeltaTimeFactor);
            ReplaceInts(config.StalkValues, dictionary, "stalk_values");
            config.StalkOvertakeDelayMinimum = RobotValue.ReadInt(
                dictionary,
                "stalk_overtake_delay_min",
                config.StalkOvertakeDelayMinimum);
            config.StalkOvertakeDelayMaximum = RobotValue.ReadInt(
                dictionary,
                "stalk_overtake_delay_max",
                config.StalkOvertakeDelayMaximum);
            return config;
        }

        private static void ReplaceInts(
            List<int> target,
            IReadOnlyDictionary<string, object> dictionary,
            string key)
        {
            if (!dictionary.TryGetValue(key, out object value) ||
                value is not IEnumerable enumerable)
                return;
            target.Clear();
            foreach (object item in enumerable)
                try { target.Add(Convert.ToInt32(item)); }
                catch (Exception) { }
        }

        private static void ReplaceFloats(
            List<float> target,
            IReadOnlyDictionary<string, object> dictionary,
            string key)
        {
            if (!dictionary.TryGetValue(key, out object value) ||
                value is not IEnumerable enumerable)
                return;
            target.Clear();
            foreach (object item in enumerable)
                try { target.Add(Convert.ToSingle(item)); }
                catch (Exception) { }
        }
    }

    public sealed class RobotPool
    {
        public string Key { get; set; } = string.Empty;
        public RobotConfig Config { get; set; } = new();
        public long CreatedUnix { get; set; }
        public long EndUnix { get; set; }
        public int BaseScore { get; set; }
        public long LastSeenUnix { get; set; }
        public List<RobotData> Robots { get; } = new();
        public int PlayerLastScore { get; set; }
        public long PlayerLastUnix { get; set; }
        public bool StalkActive { get; set; }

        public Dictionary<string, object> ToDictionary()
        {
            var robots = new List<object>(Robots.Count);
            for (int index = 0; index < Robots.Count; index++)
                robots.Add(Robots[index].ToDictionary());
            return new Dictionary<string, object>
            {
                ["key"] = Key ?? string.Empty,
                ["config"] = Config?.ToDictionary() ??
                             new Dictionary<string, object>(),
                ["created_unix"] = CreatedUnix,
                ["end_unix"] = EndUnix,
                ["x_base"] = BaseScore,
                ["last_seen_unix"] = LastSeenUnix,
                ["robots"] = robots,
                ["player_last_score"] = PlayerLastScore,
                ["player_last_unix"] = PlayerLastUnix,
                ["stalk_active"] = StalkActive
            };
        }

        public static RobotPool FromDictionary(
            IReadOnlyDictionary<string, object> dictionary)
        {
            var pool = new RobotPool
            {
                Key = RobotValue.ReadString(dictionary, "key"),
                CreatedUnix = RobotValue.ReadLong(dictionary, "created_unix"),
                EndUnix = RobotValue.ReadLong(dictionary, "end_unix"),
                BaseScore = RobotValue.ReadInt(dictionary, "x_base"),
                LastSeenUnix = RobotValue.ReadLong(
                    dictionary,
                    "last_seen_unix"),
                PlayerLastScore = RobotValue.ReadInt(
                    dictionary,
                    "player_last_score"),
                PlayerLastUnix = RobotValue.ReadLong(
                    dictionary,
                    "player_last_unix"),
                StalkActive = RobotValue.ReadBool(dictionary, "stalk_active")
            };
            if (dictionary != null &&
                dictionary.TryGetValue("config", out object config) &&
                config is IReadOnlyDictionary<string, object> configDictionary)
                pool.Config = RobotConfig.FromDictionary(configDictionary);
            if (dictionary != null &&
                dictionary.TryGetValue("robots", out object robots) &&
                robots is IList list)
            {
                for (int index = 0; index < list.Count; index++)
                    if (list[index] is
                        IReadOnlyDictionary<string, object> robotDictionary)
                        pool.Robots.Add(
                            RobotData.FromDictionary(robotDictionary));
            }
            return pool;
        }
    }

    public sealed class RobotRankEntry
    {
        public bool IsPlayer { get; set; }
        public RobotData Robot { get; set; }
        public int Score { get; set; }
        public long TimestampUnix { get; set; }
    }

    internal static class RobotValue
    {
        public static int ReadInt(
            IReadOnlyDictionary<string, object> values,
            string key,
            int fallback = 0) => ConvertValue(values, key, fallback,
                Convert.ToInt32);
        public static long ReadLong(
            IReadOnlyDictionary<string, object> values,
            string key,
            long fallback = 0) => ConvertValue(values, key, fallback,
                Convert.ToInt64);
        public static float ReadFloat(
            IReadOnlyDictionary<string, object> values,
            string key,
            float fallback = 0f) => ConvertValue(values, key, fallback,
                Convert.ToSingle);
        public static bool ReadBool(
            IReadOnlyDictionary<string, object> values,
            string key,
            bool fallback = false) => ConvertValue(values, key, fallback,
                Convert.ToBoolean);
        public static string ReadString(
            IReadOnlyDictionary<string, object> values,
            string key,
            string fallback = "")
        {
            return values != null && values.TryGetValue(key, out object value) &&
                   value != null
                ? Convert.ToString(value) ?? fallback
                : fallback;
        }

        private static T ConvertValue<T>(
            IReadOnlyDictionary<string, object> values,
            string key,
            T fallback,
            Func<object, T> convert)
        {
            if (values == null || !values.TryGetValue(key, out object value) ||
                value == null)
                return fallback;
            try { return convert(value); }
            catch (Exception) { return fallback; }
        }
    }
}
