using System;
using System.Collections.Generic;

namespace Meowdoku.Core.Robot
{
    public interface IRobotRandom
    {
        int NextInclusive(int minimum, int maximum);
        float NextFloat();
    }

    public sealed class SystemRobotRandom : IRobotRandom
    {
        private readonly Random _random;

        public SystemRobotRandom() : this(new Random()) { }
        public SystemRobotRandom(Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public int NextInclusive(int minimum, int maximum)
        {
            if (maximum < minimum)
                throw new ArgumentOutOfRangeException(nameof(maximum));
            return _random.Next(minimum, maximum + 1);
        }

        public float NextFloat() => (float)_random.NextDouble();
    }

    public static class RobotScoreGenerator
    {
        public static int PlayerBase(
            int previousScore,
            int previousRank,
            RobotConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            float alpha = previousRank <= 1
                ? config.AlphaFirst
                : previousRank <= 10
                    ? config.AlphaTop10
                    : previousRank <= 30
                        ? config.AlphaTop30
                        : config.AlphaRest;
            float raw = previousScore * (1f + alpha);
            return (int)Math.Max(config.BaseFloor,
                Math.Min(raw, config.Ceiling));
        }

        public static int BotFinalScore(
            int baseScore,
            RobotConfig config,
            IRobotRandom random)
        {
            Validate(config, random);
            double value = Math.Pow(random.NextFloat(), config.RandomPower);
            return config.BotOffset + (int)Math.Round(
                baseScore * value,
                MidpointRounding.AwayFromZero);
        }

        public static List<int> ScoreArray(
            int botScore,
            RobotConfig config,
            IRobotRandom random)
        {
            Validate(config, random);
            return string.Equals(
                config.ArrayStrategy,
                "closest_approach",
                StringComparison.Ordinal)
                ? ClosestApproach(botScore, config, random)
                : string.Equals(
                    config.ArrayStrategy,
                    "fill_to_zero",
                    StringComparison.Ordinal)
                    ? FillToZero(botScore, config, random)
                    : new List<int>();
        }

        public static List<RobotTimelinePoint> DistributeTimeline(
            IReadOnlyList<int> values,
            RobotConfig config,
            IRobotRandom random)
        {
            Validate(config, random);
            var result = new List<RobotTimelinePoint>(values?.Count ?? 0);
            if (values == null) return result;
            List<Bucket> buckets = ParseFormat(
                config.TimelineFormat,
                config.TotalMinutes);
            for (int index = 0; index < values.Count; index++)
            {
                int minute = index < config.FirstHourForcedCount
                    ? random.NextInclusive(0, config.FirstHourMinutes)
                    : PickMinute(buckets, random);
                result.Add(new RobotTimelinePoint
                {
                    Minute = minute,
                    Delta = values[index]
                });
            }
            result.Sort((left, right) => left.Minute.CompareTo(right.Minute));
            for (int index = 1; index < result.Count; index++)
                if (result[index].Minute - result[index - 1].Minute <
                    config.CooldownMinutes)
                    result[index].Minute = result[index - 1].Minute +
                                           config.CooldownMinutes;
            int last = result.Count - 1;
            if (last >= 0 && result[last].Minute >= config.TotalMinutes)
            {
                result[last].Minute = config.TotalMinutes - 1;
                for (int index = last - 1; index >= 0; index--)
                    if (result[index + 1].Minute - result[index].Minute <
                        config.CooldownMinutes)
                        result[index].Minute = result[index + 1].Minute -
                                               config.CooldownMinutes;
            }
            return result;
        }

        public static void EnsureMinimumScoring(
            IReadOnlyList<RobotData> robots,
            int count)
        {
            if (robots == null) return;
            int total = Math.Min(count, robots.Count);
            for (int index = 0; index < total; index++)
                if (robots[index].Timeline.Count > 0 &&
                    robots[index].Timeline[0].Minute > 0)
                    robots[index].Timeline[0].Minute = 0;
        }

        private static List<int> ClosestApproach(
            int target,
            RobotConfig config,
            IRobotRandom random)
        {
            var result = new List<int>();
            int current = 0;
            while (true)
            {
                int value = WeightedPick(
                    config.ArrayValues,
                    config.ArrayWeights,
                    random);
                if (current + value <= target)
                {
                    result.Add(value);
                    current += value;
                }
                else
                {
                    int before = Math.Abs(target - current);
                    int after = Math.Abs(target - (current + value));
                    if (after < before) result.Add(value);
                    break;
                }
            }
            Shuffle(result, random);
            return result;
        }

        private static List<int> FillToZero(
            int target,
            RobotConfig config,
            IRobotRandom random)
        {
            var result = new List<int>();
            int remaining = target;
            while (remaining > 0)
            {
                int value = remaining >= 3
                    ? WeightedPick(
                        config.ArrayValues,
                        config.ArrayWeights,
                        random)
                    : remaining;
                result.Add(value);
                remaining -= value;
            }
            Shuffle(result, random);
            return result;
        }

        private static int WeightedPick(
            IReadOnlyList<int> values,
            IReadOnlyList<float> weights,
            IRobotRandom random)
        {
            if (values == null || weights == null || values.Count == 0 ||
                values.Count != weights.Count)
                return 0;
            float total = 0f;
            for (int index = 0; index < weights.Count; index++)
                total += weights[index];
            float roll = random.NextFloat() * total;
            float accumulated = 0f;
            for (int index = 0; index < values.Count; index++)
            {
                accumulated += weights[index];
                if (roll <= accumulated) return values[index];
            }
            return values[values.Count - 1];
        }

        private static List<Bucket> ParseFormat(string format, int totalMinutes)
        {
            var result = new List<Bucket>();
            int previous = 0;
            string[] segments = (format ?? string.Empty).Split(
                new[] { ',' },
                StringSplitOptions.RemoveEmptyEntries);
            for (int index = 0; index < segments.Length; index++)
            {
                string[] values = segments[index].Split(';');
                if (values.Length < 2 ||
                    !int.TryParse(values[0], out int end) ||
                    !float.TryParse(
                        values[1],
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out float weight))
                    continue;
                result.Add(new Bucket(previous, end, weight));
                previous = end;
            }
            if (result.Count == 0)
                result.Add(new Bucket(0, totalMinutes, 1f));
            return result;
        }

        private static int PickMinute(
            IReadOnlyList<Bucket> buckets,
            IRobotRandom random)
        {
            float total = 0f;
            for (int index = 0; index < buckets.Count; index++)
                total += buckets[index].Weight;
            float roll = random.NextFloat() * total;
            float accumulated = 0f;
            for (int index = 0; index < buckets.Count; index++)
            {
                Bucket bucket = buckets[index];
                accumulated += bucket.Weight;
                if (roll <= accumulated)
                    return random.NextInclusive(bucket.Start, bucket.End);
            }
            Bucket last = buckets[buckets.Count - 1];
            return random.NextInclusive(last.Start, last.End);
        }

        internal static void Shuffle<T>(IList<T> values, IRobotRandom random)
        {
            for (int index = values.Count - 1; index > 0; index--)
            {
                int swap = random.NextInclusive(0, index);
                (values[index], values[swap]) = (values[swap], values[index]);
            }
        }

        private static void Validate(
            RobotConfig config,
            IRobotRandom random)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (random == null) throw new ArgumentNullException(nameof(random));
        }

        private readonly struct Bucket
        {
            public Bucket(int start, int end, float weight)
            {
                Start = start;
                End = end;
                Weight = weight;
            }
            public int Start { get; }
            public int End { get; }
            public float Weight { get; }
        }
    }

    public static class RobotIdentity
    {
        public static void Assign(
            IReadOnlyList<RobotData> robots,
            IReadOnlyList<int> avatarIds,
            IReadOnlyList<int> frameIds,
            IReadOnlyList<string> nicknames,
            int openPeriod,
            int firstPlaceFrameId,
            IRobotRandom random)
        {
            if (robots == null || random == null) return;
            for (int index = 0; index < robots.Count; index++)
            {
                RobotData robot = robots[index];
                if (nicknames != null && nicknames.Count > 0)
                    robot.Nickname = nicknames[random.NextInclusive(
                        0,
                        nicknames.Count - 1)];
                if (avatarIds != null && avatarIds.Count > 0)
                    robot.AvatarId = avatarIds[random.NextInclusive(
                        0,
                        avatarIds.Count - 1)];
                if (frameIds != null && frameIds.Count > 0)
                    robot.FrameId = frameIds[random.NextInclusive(
                        0,
                        frameIds.Count - 1)];
                robot.IsFirstFrame = false;
                robot.FrameBadge = 0;
            }
            if (robots.Count == 0) return;

            int count = random.NextInclusive(1, 3);
            var order = new List<int>(robots.Count);
            for (int index = 0; index < robots.Count; index++) order.Add(index);
            order.Sort((left, right) =>
                robots[right].FinalScore.CompareTo(robots[left].FinalScore));
            var chosen = new HashSet<int>();
            for (int index = 0; index < Math.Min(count, order.Count); index++)
                chosen.Add(order[index]);
            var rest = new List<int>();
            for (int index = 0; index < order.Count; index++)
                if (!chosen.Contains(order[index])) rest.Add(order[index]);
            RobotScoreGenerator.Shuffle(rest, random);
            for (int index = 0; index < Math.Min(count + 2, rest.Count); index++)
                chosen.Add(rest[index]);

            foreach (int index in chosen)
            {
                RobotData robot = robots[index];
                robot.IsFirstFrame = true;
                robot.FrameBadge = random.NextInclusive(1, Math.Max(1, openPeriod));
                if (firstPlaceFrameId != 0)
                    robot.FrameId = firstPlaceFrameId;
            }
        }
    }

    public static class RobotRanking
    {
        public static int RobotScoreAt(RobotData robot, float elapsedMinute)
        {
            if (robot == null) return 0;
            int score = 0;
            for (int index = 0; index < robot.Timeline.Count; index++)
                if (robot.Timeline[index].Minute <= elapsedMinute)
                    score += robot.Timeline[index].Delta;
            return score;
        }

        public static long RobotLastUnix(
            RobotData robot,
            float elapsedMinute,
            long createdUnix)
        {
            int bestMinute = -1;
            long bestTimestamp = createdUnix;
            if (robot == null) return bestTimestamp;
            for (int index = 0; index < robot.Timeline.Count; index++)
            {
                RobotTimelinePoint point = robot.Timeline[index];
                if (point.Minute > elapsedMinute || point.Minute < bestMinute)
                    continue;
                bestMinute = point.Minute;
                bestTimestamp = point.Timestamp ??
                                createdUnix + point.Minute * 60L;
            }
            return bestTimestamp;
        }

        public static List<RobotRankEntry> Rank(
            RobotPool pool,
            long nowUnix,
            int playerScore,
            long playerUnix,
            bool dropZeroRobots = false)
        {
            var entries = new List<RobotRankEntry>();
            if (pool == null) return entries;
            float elapsed = (nowUnix - pool.CreatedUnix) / 60f;
            for (int index = 0; index < pool.Robots.Count; index++)
            {
                RobotData robot = pool.Robots[index];
                int score = RobotScoreAt(robot, elapsed);
                if (dropZeroRobots && score <= 0) continue;
                entries.Add(new RobotRankEntry
                {
                    Robot = robot,
                    Score = score,
                    TimestampUnix = RobotLastUnix(
                        robot,
                        elapsed,
                        pool.CreatedUnix)
                });
            }
            entries.Add(new RobotRankEntry
            {
                IsPlayer = true,
                Score = playerScore,
                TimestampUnix = playerUnix
            });
            entries.Sort((left, right) =>
            {
                int score = right.Score.CompareTo(left.Score);
                return score != 0
                    ? score
                    : left.TimestampUnix.CompareTo(right.TimestampUnix);
            });
            return entries;
        }

        public static int PlayerRank(
            RobotPool pool,
            long nowUnix,
            int playerScore,
            long playerUnix,
            bool dropZeroRobots = false)
        {
            List<RobotRankEntry> entries = Rank(
                pool,
                nowUnix,
                playerScore,
                playerUnix,
                dropZeroRobots);
            for (int index = 0; index < entries.Count; index++)
                if (entries[index].IsPlayer) return index + 1;
            return entries.Count;
        }
    }

    public static class RobotStalking
    {
        public static int Capacity(int playerScore, RobotConfig config)
        {
            if (config == null || playerScore >= config.Ceiling) return 0;
            return (int)Math.Floor(
                (config.Ceiling - playerScore) /
                (float)config.StalkSlotDivisor);
        }

        public static int CatchUpDelta(
            int scoreGap,
            float elapsedMinute,
            RobotConfig config,
            IRobotRandom random)
        {
            if (config == null || random == null ||
                config.StalkValues.Count == 0)
                return 0;
            int maximum = (int)Math.Max(
                0f,
                Math.Min(
                    scoreGap - config.StalkMinimumGap,
                    elapsedMinute * config.StalkDeltaTimeFactor));
            if (maximum <= 0) return 0;
            int total = 0;
            while (true)
            {
                int value = config.StalkValues[random.NextInclusive(
                    0,
                    config.StalkValues.Count - 1)];
                if (total + value > maximum) break;
                total += value;
            }
            return total;
        }

        public static long OvertakeGuardUnix(
            long playerLastUnix,
            RobotConfig config,
            IRobotRandom random)
        {
            // Source adds this minute-named config directly to unix seconds.
            return playerLastUnix + random.NextInclusive(
                config.StalkOvertakeDelayMinimum,
                config.StalkOvertakeDelayMaximum);
        }
    }
}
