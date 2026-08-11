using System;
using System.Collections.Generic;
using Meowdoku.Core.Profile;

namespace Meowdoku.Core.Robot
{
    public interface IRobotTimeProvider
    {
        long UnixNow { get; }
    }

    public sealed class SystemRobotTimeProvider : IRobotTimeProvider
    {
        public long UnixNow => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    public interface IRobotRandomFactory
    {
        IRobotRandom Create();
    }

    public sealed class SystemRobotRandomFactory : IRobotRandomFactory
    {
        public IRobotRandom Create() => new SystemRobotRandom();
    }

    /// <summary>
    /// Source-shaped owner of simulated leaderboard pools. Time, randomness
    /// and persistence are injected so the same rules can be verified without
    /// a scene singleton.
    /// </summary>
    public sealed class RobotService
    {
        private readonly IRobotPoolStore _store;
        private readonly IRobotTimeProvider _time;
        private readonly IRobotRandomFactory _randomFactory;
        private readonly Dictionary<string, RobotPool> _pools =
            new(StringComparer.Ordinal);
        private int _keySequence;

        public RobotService(
            IRobotPoolStore store,
            IRobotTimeProvider time = null,
            IRobotRandomFactory randomFactory = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _time = time ?? new SystemRobotTimeProvider();
            _randomFactory = randomFactory ?? new SystemRobotRandomFactory();
            IReadOnlyDictionary<string, RobotPool> loaded = _store.LoadAll();
            if (loaded == null) return;
            foreach (KeyValuePair<string, RobotPool> pair in loaded)
                if (!string.IsNullOrEmpty(pair.Key) && pair.Value != null)
                    _pools[pair.Key] = pair.Value;
        }

        public string CreatePool(
            RobotConfig config,
            int previousScore,
            int previousRank,
            IReadOnlyList<int> avatarIds,
            IReadOnlyList<int> frameIds,
            int firstPlaceFrameId,
            IReadOnlyList<string> nicknamePool,
            int openPeriod,
            long endUnix)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            IRobotRandom random = CreateRandom();
            var pool = new RobotPool
            {
                Key = GenerateKey(),
                Config = config,
                CreatedUnix = _time.UnixNow,
                EndUnix = endUnix,
                BaseScore = RobotScoreGenerator.PlayerBase(
                    previousScore,
                    previousRank,
                    config)
            };

            for (int index = 0; index < config.RobotCount; index++)
            {
                var robot = new RobotData { Id = index };
                int finalScore = RobotScoreGenerator.BotFinalScore(
                    pool.BaseScore,
                    config,
                    random);
                List<int> values = RobotScoreGenerator.ScoreArray(
                    finalScore,
                    config,
                    random);
                int realized = 0;
                for (int valueIndex = 0;
                     valueIndex < values.Count;
                     valueIndex++)
                    realized += values[valueIndex];
                robot.FinalScore = realized;
                robot.Timeline.AddRange(
                    RobotScoreGenerator.DistributeTimeline(
                        values,
                        config,
                        random));
                pool.Robots.Add(robot);
            }

            RobotScoreGenerator.EnsureMinimumScoring(
                pool.Robots,
                config.MinimumScoringRobots);
            RobotIdentity.Assign(
                pool.Robots,
                avatarIds,
                frameIds,
                nicknamePool,
                openPeriod,
                firstPlaceFrameId,
                random);
            _pools[pool.Key] = pool;
            Persist();
            return pool.Key;
        }

        public List<RobotRankEntry> GetRanking(
            string key,
            int playerScore,
            long playerUnix,
            long? nowUnix = null,
            bool dropZeroRobots = false)
        {
            if (!TryGetPool(key, out RobotPool pool))
                return new List<RobotRankEntry>();
            long effectiveNow = EffectiveNow(
                pool,
                nowUnix ?? _time.UnixNow);
            return RobotRanking.Rank(
                pool,
                effectiveNow,
                playerScore,
                playerUnix,
                dropZeroRobots);
        }

        public int GetPlayerRank(
            string key,
            int playerScore,
            long playerUnix,
            long? nowUnix = null,
            bool dropZeroRobots = false)
        {
            if (!TryGetPool(key, out RobotPool pool)) return -1;
            long effectiveNow = EffectiveNow(
                pool,
                nowUnix ?? _time.UnixNow);
            return RobotRanking.PlayerRank(
                pool,
                effectiveNow,
                playerScore,
                playerUnix,
                dropZeroRobots);
        }

        public List<RankInfo> GetRankList(
            string key,
            PlayerInfo playerInfo,
            int playerScore,
            long playerUnix,
            IReadOnlyList<int> rankAwards = null,
            long? nowUnix = null,
            bool dropZeroRobots = false)
        {
            if (!TryGetPool(key, out RobotPool pool))
                return new List<RankInfo>();
            long effectiveNow = EffectiveNow(
                pool,
                nowUnix ?? _time.UnixNow);
            List<RobotRankEntry> entries = RobotRanking.Rank(
                pool,
                effectiveNow,
                playerScore,
                playerUnix,
                dropZeroRobots);
            var result = new List<RankInfo>(entries.Count);
            for (int index = 0; index < entries.Count; index++)
            {
                RobotRankEntry entry = entries[index];
                result.Add(new RankInfo
                {
                    Rank = index + 1,
                    Score = entry.Score,
                    PlayerInfo = entry.IsPlayer
                        ? playerInfo
                        : RobotToPlayerInfo(entry.Robot),
                    AwardId = rankAwards != null && index < rankAwards.Count
                        ? rankAwards[index]
                        : 0
                });
            }
            return result;
        }

        public void OnPlayerScore(string key, int score, long scoreUnix)
        {
            if (!TryGetPool(key, out RobotPool pool)) return;
            long now = EffectiveNow(pool, _time.UnixNow);
            float elapsedMinute = (now - pool.CreatedUnix) / 60f;
            pool.PlayerLastScore = score;
            pool.PlayerLastUnix = scoreUnix;

            if (!pool.StalkActive && score > pool.BaseScore)
            {
                pool.StalkActive = true;
                for (int index = 0; index < pool.Robots.Count; index++)
                    FreezeFuture(pool.Robots[index], elapsedMinute);
            }
            if (pool.StalkActive)
                ApplyCatchUp(pool, score, scoreUnix, elapsedMinute);
            Persist();
        }

        public bool HasPool(string key) =>
            !string.IsNullOrEmpty(key) && _pools.ContainsKey(key);

        public void DiscardPool(string key)
        {
            if (!string.IsNullOrEmpty(key) && _pools.Remove(key)) Persist();
        }

        public void Reset()
        {
            _pools.Clear();
            _store.Reset();
        }

        private void ApplyCatchUp(
            RobotPool pool,
            int playerScore,
            long playerUnix,
            float elapsedMinute)
        {
            RobotConfig config = pool.Config;
            int capacity = Math.Min(
                RobotStalking.Capacity(playerScore, config),
                config.StalkTopPool);
            if (capacity <= 0) return;
            IRobotRandom random = CreateRandom();
            var scored = new List<ScoredRobot>(pool.Robots.Count);
            for (int index = 0; index < pool.Robots.Count; index++)
            {
                RobotData robot = pool.Robots[index];
                scored.Add(new ScoredRobot(
                    robot,
                    RobotRanking.RobotScoreAt(robot, elapsedMinute)));
            }
            scored.Sort((left, right) => right.Score.CompareTo(left.Score));

            int currentMinute = (int)elapsedMinute;
            int total = Math.Min(capacity, scored.Count);
            for (int index = 0; index < total; index++)
            {
                RobotData robot = scored[index].Robot;
                int scoreGap = playerScore - scored[index].Score;
                float lastMinute = robot.LastUpdateMinute >= 0f
                    ? robot.LastUpdateMinute
                    : elapsedMinute;
                float deltaTime = Math.Max(
                    0f,
                    elapsedMinute - lastMinute);
                int delta = RobotStalking.CatchUpDelta(
                    scoreGap,
                    deltaTime,
                    config,
                    random);
                if (delta <= 0) continue;
                robot.Timeline.Add(new RobotTimelinePoint
                {
                    Minute = currentMinute,
                    Delta = delta,
                    Timestamp = RobotStalking.OvertakeGuardUnix(
                        playerUnix,
                        config,
                        random)
                });
                robot.Stalking = true;
                robot.LastUpdateMinute = elapsedMinute;
            }
        }

        private static void FreezeFuture(
            RobotData robot,
            float elapsedMinute)
        {
            var kept = new List<RobotTimelinePoint>();
            float lastMinute = -1f;
            for (int index = 0; index < robot.Timeline.Count; index++)
            {
                RobotTimelinePoint point = robot.Timeline[index];
                if (point.Minute > elapsedMinute) continue;
                kept.Add(point);
                if (point.Minute > lastMinute) lastMinute = point.Minute;
            }
            robot.Timeline.Clear();
            robot.Timeline.AddRange(kept);
            robot.LastUpdateMinute = lastMinute < 0f
                ? elapsedMinute
                : lastMinute;
        }

        private static PlayerInfo RobotToPlayerInfo(RobotData robot)
        {
            if (robot == null) return null;
            return new PlayerInfo
            {
                Nickname = robot.Nickname,
                AvatarId = robot.AvatarId,
                Frame = new AvatarFrame(
                    robot.FrameId,
                    robot.IsFirstFrame ? robot.FrameBadge : -1),
                PlayerId = $"robot_{robot.Id}",
                IsRobot = true
            };
        }

        private long EffectiveNow(RobotPool pool, long input)
        {
            long effective = input;
            if (pool.EndUnix > 0)
                effective = Math.Min(effective, pool.EndUnix);
            effective = Math.Max(effective, pool.LastSeenUnix);
            pool.LastSeenUnix = effective;
            return effective;
        }

        private bool TryGetPool(string key, out RobotPool pool)
        {
            if (!string.IsNullOrEmpty(key) &&
                _pools.TryGetValue(key, out pool))
                return true;
            pool = null;
            return false;
        }

        private IRobotRandom CreateRandom()
        {
            return _randomFactory.Create() ??
                   throw new InvalidOperationException(
                       "Robot random factory returned null.");
        }

        private string GenerateKey()
        {
            _keySequence++;
            return $"rb_{_time.UnixNow}_{_keySequence}";
        }

        private void Persist() => _store.SaveAll(_pools);

        private readonly struct ScoredRobot
        {
            public ScoredRobot(RobotData robot, int score)
            {
                Robot = robot;
                Score = score;
            }

            public RobotData Robot { get; }
            public int Score { get; }
        }
    }
}
