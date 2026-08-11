using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Meowdoku.Core.Profile;
using Meowdoku.Core.Robot;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class RobotCoreTests
    {
        [Test]
        public void NicknameCatalog_MatchesExactSourceSnapshot()
        {
            string joined = string.Join("\n", RobotNicknameCatalog.Names);
            byte[] digest = SHA256.Create().ComputeHash(
                Encoding.UTF8.GetBytes(joined));
            string hash = BitConverter.ToString(digest)
                .Replace("-", string.Empty)
                .ToLowerInvariant();

            Assert.That(RobotNicknameCatalog.Names,
                Has.Count.EqualTo(1699));
            Assert.That(RobotNicknameCatalog.Names[0], Is.EqualTo("P738J1"));
            Assert.That(RobotNicknameCatalog.Names[1698], Is.EqualTo("Souta"));
            Assert.That(hash, Is.EqualTo(
                "f864d2094a2587d4c030371373120fd44c693061edde7af3c54292d3dac7e4fd"));
        }

        [Test]
        public void ConfigAndPool_RoundTripSourceKeys()
        {
            var pool = new RobotPool
            {
                Key = "rb_100_1",
                CreatedUnix = 100,
                EndUnix = 500,
                BaseScore = 118,
                LastSeenUnix = 200,
                PlayerLastScore = 32,
                PlayerLastUnix = 190,
                StalkActive = true
            };
            var robot = new RobotData
            {
                Id = 2,
                Nickname = "MEO",
                AvatarId = 3,
                FrameId = 100,
                IsFirstFrame = true,
                FrameBadge = 4,
                FinalScore = 42,
                Stalking = true,
                LastUpdateMinute = 2.5f
            };
            robot.Timeline.Add(new RobotTimelinePoint
            {
                Minute = 2,
                Delta = 9,
                Timestamp = 222
            });
            pool.Robots.Add(robot);

            RobotPool restored = RobotPool.FromDictionary(
                pool.ToDictionary());

            Assert.That(restored.Key, Is.EqualTo("rb_100_1"));
            Assert.That(restored.Config.RobotCount, Is.EqualTo(30));
            Assert.That(restored.Robots, Has.Count.EqualTo(1));
            Assert.That(restored.Robots[0].Nickname, Is.EqualTo("MEO"));
            Assert.That(restored.Robots[0].Timeline[0].Timestamp,
                Is.EqualTo(222));
            Assert.That(restored.StalkActive, Is.True);
        }

        [Test]
        public void PlayerBase_UsesSourceRankBandsAndBounds()
        {
            var config = new RobotConfig();

            Assert.That(RobotScoreGenerator.PlayerBase(500, 1, config),
                Is.EqualTo(540));
            Assert.That(RobotScoreGenerator.PlayerBase(500, 10, config),
                Is.EqualTo(515));
            Assert.That(RobotScoreGenerator.PlayerBase(500, 30, config),
                Is.EqualTo(450));
            Assert.That(RobotScoreGenerator.PlayerBase(500, 31, config),
                Is.EqualTo(400));
            Assert.That(RobotScoreGenerator.PlayerBase(10, 1, config),
                Is.EqualTo(118));
            Assert.That(RobotScoreGenerator.PlayerBase(2000, 1, config),
                Is.EqualTo(960));
        }

        [Test]
        public void ScoreArrayAndTimeline_KeepOvershootAndCooldownRules()
        {
            var config = new RobotConfig
            {
                TotalMinutes = 20,
                FirstHourMinutes = 0,
                FirstHourForcedCount = 2,
                CooldownMinutes = 3,
                TimelineFormat = "20;1"
            };
            config.ArrayValues.Clear();
            config.ArrayValues.Add(8);
            config.ArrayWeights.Clear();
            config.ArrayWeights.Add(1f);
            var random = new FixedRandom(0, 0f);

            List<int> values = RobotScoreGenerator.ScoreArray(
                14,
                config,
                random);
            List<RobotTimelinePoint> timeline =
                RobotScoreGenerator.DistributeTimeline(
                    new[] { 8, 8, 8 },
                    config,
                    random);

            Assert.That(values, Is.EqualTo(new[] { 8, 8 }));
            Assert.That(timeline.ConvertAll(point => point.Minute),
                Is.EqualTo(new[] { 0, 3, 6 }));
        }

        [Test]
        public void Ranking_UsesEarlierTimestampToBreakEqualScore()
        {
            RobotPool pool = PoolWithRobot(
                createdUnix: 100,
                pointMinute: 0,
                pointScore: 10);

            List<RobotRankEntry> laterPlayer = RobotRanking.Rank(
                pool,
                100,
                10,
                101);
            List<RobotRankEntry> earlierPlayer = RobotRanking.Rank(
                pool,
                100,
                10,
                99);

            Assert.That(laterPlayer[0].IsPlayer, Is.False);
            Assert.That(earlierPlayer[0].IsPlayer, Is.True);
        }

        [Test]
        public void Service_CreatePersistsPoolAndBuildsRankInfo()
        {
            var store = new MemoryPoolStore();
            var time = new FixedTime(1000);
            var service = new RobotService(
                store,
                time,
                new FixedRandomFactory(0, 1f));
            RobotConfig config = SmallConfig(robotCount: 2);

            string key = service.CreatePool(
                config,
                previousScore: 20,
                previousRank: 1,
                avatarIds: new[] { 3 },
                frameIds: new[] { 4 },
                firstPlaceFrameId: 100,
                nicknamePool: new[] { "BOT" },
                openPeriod: 7,
                endUnix: 2000);
            List<RankInfo> ranks = service.GetRankList(
                key,
                new PlayerInfo
                {
                    PlayerId = PlayerInfo.LocalPlayerId,
                    Nickname = "ME"
                },
                playerScore: 0,
                playerUnix: 1000,
                rankAwards: new[] { 11, 12 },
                nowUnix: 2000);

            Assert.That(key, Is.EqualTo("rb_1000_1"));
            Assert.That(store.SaveCount, Is.EqualTo(1));
            Assert.That(service.HasPool(key), Is.True);
            Assert.That(ranks, Has.Count.EqualTo(3));
            Assert.That(ranks[0].PlayerInfo.IsRobot, Is.True);
            Assert.That(ranks[0].PlayerInfo.Nickname, Is.EqualTo("BOT"));
            Assert.That(ranks[0].PlayerInfo.Frame.Id, Is.EqualTo(100));
            Assert.That(ranks[0].AwardId, Is.EqualTo(11));
            Assert.That(ranks[2].IsSelf, Is.True);
        }

        [Test]
        public void EffectiveNow_ClampsAtEndAndNeverMovesBackward()
        {
            RobotPool pool = PoolWithRobot(
                createdUnix: 1000,
                pointMinute: 10,
                pointScore: 8);
            pool.Key = "pool";
            pool.EndUnix = 1600;
            pool.LastSeenUnix = 1500;
            var store = new MemoryPoolStore(pool);
            var service = new RobotService(
                store,
                new FixedTime(0),
                new FixedRandomFactory(0, 0f));

            List<RobotRankEntry> beforeEnd = service.GetRanking(
                "pool",
                0,
                0,
                nowUnix: 1200);
            List<RobotRankEntry> atEnd = service.GetRanking(
                "pool",
                0,
                0,
                nowUnix: 2000);
            List<RobotRankEntry> afterRollback = service.GetRanking(
                "pool",
                0,
                0,
                nowUnix: 1100);

            Assert.That(beforeEnd[0].Score, Is.EqualTo(0));
            Assert.That(atEnd[0].Score, Is.EqualTo(8));
            Assert.That(afterRollback[0].Score, Is.EqualTo(8));
            Assert.That(pool.LastSeenUnix, Is.EqualTo(1600));
        }

        [Test]
        public void PlayerCrossingBase_FreezesFutureAndAppliesSourceCatchUp()
        {
            RobotPool pool = PoolWithRobot(
                createdUnix: 1000,
                pointMinute: 1,
                pointScore: 8);
            pool.Key = "pool";
            pool.BaseScore = 10;
            pool.Config = SmallConfig(robotCount: 1);
            pool.Config.Ceiling = 100;
            pool.Config.StalkTopPool = 1;
            pool.Config.StalkSlotDivisor = 25;
            pool.Config.StalkMinimumGap = 2;
            pool.Config.StalkDeltaTimeFactor = 8f;
            pool.Config.StalkValues.Clear();
            pool.Config.StalkValues.Add(8);
            pool.Robots[0].Timeline.Add(new RobotTimelinePoint
            {
                Minute = 10,
                Delta = 8
            });
            var store = new MemoryPoolStore(pool);
            var service = new RobotService(
                store,
                new FixedTime(1120),
                new FixedRandomFactory(0, 0f));

            service.OnPlayerScore("pool", 20, 1100);

            RobotData robot = pool.Robots[0];
            Assert.That(pool.StalkActive, Is.True);
            Assert.That(robot.Timeline, Has.Count.EqualTo(2));
            Assert.That(robot.Timeline[0].Minute, Is.EqualTo(1));
            Assert.That(robot.Timeline[1].Minute, Is.EqualTo(2));
            Assert.That(robot.Timeline[1].Delta, Is.EqualTo(8));
            Assert.That(robot.Timeline[1].Timestamp, Is.EqualTo(1105));
            Assert.That(robot.Stalking, Is.True);
            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        private static RobotConfig SmallConfig(int robotCount)
        {
            var config = new RobotConfig
            {
                RobotCount = robotCount,
                BaseFloor = 20,
                Ceiling = 20,
                BotOffset = 0,
                RandomPower = 1f,
                FirstHourForcedCount = 10,
                FirstHourMinutes = 0,
                TotalMinutes = 60,
                CooldownMinutes = 0
            };
            config.ArrayValues.Clear();
            config.ArrayValues.Add(10);
            config.ArrayWeights.Clear();
            config.ArrayWeights.Add(1f);
            return config;
        }

        private static RobotPool PoolWithRobot(
            long createdUnix,
            int pointMinute,
            int pointScore)
        {
            var pool = new RobotPool
            {
                Key = "pool",
                CreatedUnix = createdUnix,
                Config = new RobotConfig()
            };
            var robot = new RobotData { Id = 0 };
            robot.Timeline.Add(new RobotTimelinePoint
            {
                Minute = pointMinute,
                Delta = pointScore
            });
            pool.Robots.Add(robot);
            return pool;
        }

        private sealed class FixedRandom : IRobotRandom
        {
            private readonly int _integer;
            private readonly float _float;
            public FixedRandom(int integer, float value)
            {
                _integer = integer;
                _float = value;
            }
            public int NextInclusive(int minimum, int maximum) =>
                Math.Clamp(_integer, minimum, maximum);
            public float NextFloat() => _float;
        }

        private sealed class FixedRandomFactory : IRobotRandomFactory
        {
            private readonly int _integer;
            private readonly float _float;
            public FixedRandomFactory(int integer, float value)
            {
                _integer = integer;
                _float = value;
            }
            public IRobotRandom Create() =>
                new FixedRandom(_integer, _float);
        }

        private sealed class FixedTime : IRobotTimeProvider
        {
            public FixedTime(long unixNow) { UnixNow = unixNow; }
            public long UnixNow { get; set; }
        }

        private sealed class MemoryPoolStore : IRobotPoolStore
        {
            private readonly Dictionary<string, RobotPool> _pools = new();
            public MemoryPoolStore(params RobotPool[] pools)
            {
                for (int index = 0; index < pools.Length; index++)
                    _pools[pools[index].Key] = pools[index];
            }
            public int SaveCount { get; private set; }
            public IReadOnlyDictionary<string, RobotPool> LoadAll() => _pools;
            public bool SaveAll(IReadOnlyDictionary<string, RobotPool> pools)
            {
                _pools.Clear();
                foreach (KeyValuePair<string, RobotPool> pair in pools)
                    _pools[pair.Key] = pair.Value;
                SaveCount++;
                return true;
            }
            public void Reset() { _pools.Clear(); }
        }
    }
}
