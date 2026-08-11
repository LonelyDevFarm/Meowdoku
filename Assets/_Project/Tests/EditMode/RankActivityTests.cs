using System;
using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.Daily;
using Meowdoku.Core.Profile;
using Meowdoku.Core.Rank;
using Meowdoku.Core.Robot;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class RankActivityTests
    {
        [SetUp]
        public void SetUp()
        {
            GlobalUniqueId.ResetForTests();
        }

        [Test]
        public void Config_PreservesThreeSourceGroupsAndRewardTable()
        {
            RobotConfig cats = RankActivityConfig.BuildRobotConfig(
                RankActivityConfig.GroupCats);
            RobotConfig fish = RankActivityConfig.BuildRobotConfig(
                RankActivityConfig.GroupFish);
            var random = new FixedRandom(1, 0f);

            Assert.That(cats.RobotCount, Is.EqualTo(49));
            Assert.That(cats.BaseFloor, Is.EqualTo(118));
            Assert.That(fish.BaseFloor, Is.EqualTo(32));
            Assert.That(fish.Ceiling, Is.EqualTo(230));
            Assert.That(fish.ArrayStrategy, Is.EqualTo("fill_to_zero"));
            Assert.That(fish.StalkDeltaTimeFactor, Is.EqualTo(1.5f));
            Assert.That(RankActivityConfig.MapCollect(1, 4, 2), Is.EqualTo(4));
            Assert.That(RankActivityConfig.MapCollect(2, 4, 2), Is.EqualTo(2));
            Assert.That(RankActivityConfig.HasReward(1, 3), Is.True);
            Assert.That(RankActivityConfig.HasReward(3, 2), Is.False);

            List<AwardItem> first = RankActivityConfig.RewardItems(1, 1, random);
            List<AwardItem> third = RankActivityConfig.RewardItems(1, 3, random);
            Assert.That(first, Has.Count.EqualTo(3));
            Assert.That(first[0].FrameId,
                Is.EqualTo(ProfileCatalog.FirstPlaceFrameId));
            Assert.That(third, Has.Count.EqualTo(1));
            Assert.That(third[0].Kind, Is.EqualTo("hint"));
            Assert.That(RankPresentationContract.EntryChestTier(1),
                Is.EqualTo(3));
            Assert.That(RankPresentationContract.EntryChestTier(3),
                Is.EqualTo(1));
        }

        [Test]
        public void Presentation_StripsGodotInlineImageButKeepsSourceCopy()
        {
            const string source =
                "[center]Play games to find [img=66x64]res://cat.png[/img] " +
                "and rank up.[/center]";

            Assert.That(
                RankPresentationContract.GodotRichTextToPlainText(source),
                Is.EqualTo("Play games to find and rank up."));
            Assert.That(
                RankPresentationContract.GodotRichTextToPlainText(null),
                Is.Empty);
        }

        [Test]
        public void PeriodRules_MatchFirstOpenAndAwardedReopenBranches()
        {
            Assert.That(RankActivityPeriod.ShouldOpen(
                0, 10, false, 0, true, true, 11, 10), Is.False);
            Assert.That(RankActivityPeriod.ShouldOpen(
                0, 11, false, 0, false, false, 11, 10), Is.True);
            Assert.That(RankActivityPeriod.ShouldOpen(
                1, 99, true, 0, true, false, 11, 10), Is.True);
            Assert.That(RankActivityPeriod.ShouldOpen(
                1, 99, true, 9, false, true, 11, 10), Is.False);
            Assert.That(RankActivityPeriod.ShouldOpen(
                1, 99, false, 0, false, true, 11, 10), Is.True);
            Assert.That(RankActivityPeriod.RemainingSeconds(100, 90),
                Is.Zero);
        }

        [Test]
        public void Data_RoundTripsEverySourceField()
        {
            var data = new RankActivityData
            {
                PeriodCount = 3,
                PreviousScore = 88,
                PreviousRank = 2,
                PreviousAwarded = true,
                State = RankActivityState.Settling,
                Group = 2,
                StartUnix = 10,
                EndUnix = 20,
                RobotKey = "rb",
                Joined = true,
                CollectTotal = 7,
                PlayerScoreUnix = 18,
                Settled = true,
                FinalRank = 2,
                RewardClaimed = true,
                WinsSinceClose = 4,
                LevelCache = 3,
                LevelCacheActive = true,
                BestEncouragedRank = 2,
                Place2Wins = 5,
                Place3Wins = 6
            };

            RankActivityData restored = RankActivityData.FromDictionary(
                data.ToDictionary());

            Assert.That(restored.PeriodCount, Is.EqualTo(3));
            Assert.That(restored.State, Is.EqualTo(RankActivityState.Settling));
            Assert.That(restored.RobotKey, Is.EqualTo("rb"));
            Assert.That(restored.LevelCacheActive, Is.True);
            Assert.That(restored.Place3Wins, Is.EqualTo(6));
        }

        [Test]
        public void FirstPeriod_OpensAtLevelElevenAndCommitsOnlyOnWin()
        {
            Fixture fixture = CreateFixture(group: 1);
            int rankingChanges = 0;
            fixture.Manager.RankingChanged += () => rankingChanges++;

            Assert.That(fixture.Manager.MaybeOpen(true), Is.True);
            Assert.That(fixture.Manager.State,
                Is.EqualTo(RankActivityState.OpenNotJoined));
            Assert.That(fixture.Manager.PeriodCount, Is.EqualTo(1));
            fixture.Manager.ConfirmParticipation();
            fixture.Manager.NotifyLevelStart();
            fixture.Manager.SetLevelCollect(4);
            Assert.That(fixture.Manager.CollectTotal, Is.Zero);

            fixture.Manager.NotifyLevelWin();

            Assert.That(fixture.Manager.CollectTotal, Is.EqualTo(4));
            Assert.That(fixture.Manager.LastWinIncrement, Is.EqualTo(4));
            Assert.That(fixture.Manager.DidLastWinScore, Is.True);
            Assert.That(rankingChanges, Is.EqualTo(1));
            Assert.That(fixture.RankStore.Current.LevelCacheActive, Is.False);
        }

        [Test]
        public void ExpiryInLevel_DefersSettleThenRankOneRewardOpensNextPeriod()
        {
            Fixture fixture = CreateFixture(group: 1);
            fixture.Manager.MaybeOpen(true);
            fixture.Manager.ConfirmParticipation();
            fixture.Manager.NotifyLevelStart();
            fixture.Manager.SetLevelCollect(5000);
            fixture.Time.UnixNow += RankActivityConfig.PeriodDurationSeconds + 1;

            fixture.Manager.Tick();
            Assert.That(fixture.Manager.State,
                Is.EqualTo(RankActivityState.Settling));
            Assert.That(fixture.RankStore.Current.Settled, Is.False);

            fixture.Manager.NotifyLevelWin();
            RankSettlementResult pending = fixture.Manager.GetPendingReward();
            Assert.That(pending, Is.Not.Null);
            Assert.That(pending.Rank, Is.EqualTo(1));
            Assert.That(fixture.RankStore.Current.Settled, Is.True);

            AwardPresentationRequest request = null;
            fixture.Awards.AwardPresentationRequested += value => request = value;
            int uid = fixture.Manager.ClaimReward(true);
            Assert.That(uid, Is.GreaterThan(0));
            Assert.That(request, Is.Not.Null);
            Assert.That(request.DisplayType, Is.EqualTo(AwardDisplayType.RankGift));
            Assert.That(fixture.Awards.CompleteAward(uid), Is.True);

            Assert.That(fixture.Profile.GetFrameCount(
                ProfileCatalog.FirstPlaceFrameId), Is.EqualTo(1));
            Assert.That(fixture.GameState.GetToolCount("locate"), Is.EqualTo(7));
            Assert.That(fixture.GameState.GetToolCount("hint"), Is.EqualTo(7));
            Assert.That(fixture.Manager.State,
                Is.EqualTo(RankActivityState.OpenNotJoined));
            Assert.That(fixture.Manager.PeriodCount, Is.EqualTo(2));
        }

        [Test]
        public void NoRewardSettlement_StaysForChangePageUntilAcknowledged()
        {
            Fixture fixture = CreateFixture(group: 3);
            fixture.Manager.MaybeOpen(true);
            fixture.Manager.ConfirmParticipation();
            fixture.Manager.NotifyLevelStart();
            fixture.Manager.SetLevelCollect(0);
            fixture.Time.UnixNow += RankActivityConfig.PeriodDurationSeconds + 1;
            fixture.Manager.Tick();

            fixture.Manager.NotifyLevelWin();

            Assert.That(fixture.RankStore.Current.Settled, Is.True);
            Assert.That(fixture.RankStore.Current.FinalRank,
                Is.GreaterThan(1));
            Assert.That(fixture.Manager.State,
                Is.EqualTo(RankActivityState.Settling));
            Assert.That(fixture.Manager.GetPendingReward(), Is.Null);

            fixture.Manager.NotifySettlementDone();
            Assert.That(fixture.Manager.State,
                Is.EqualTo(RankActivityState.NotOpened));
        }

        [Test]
        public void DisabledFeatureAtHome_DiscardsActivePoolAndResetsData()
        {
            Fixture fixture = CreateFixture(group: 1);
            fixture.Manager.MaybeOpen(true);
            string key = fixture.RankStore.Current.RobotKey;
            Assert.That(fixture.Robots.HasPool(key), Is.True);
            fixture.Environment.Enabled = false;

            fixture.Manager.OnHomeShown();

            Assert.That(fixture.Manager.State,
                Is.EqualTo(RankActivityState.NotOpened));
            Assert.That(fixture.Manager.PeriodCount, Is.Zero);
            Assert.That(fixture.Robots.HasPool(key), Is.False);
        }

        [Test]
        public void InterruptedClaim_ColdStartFoldsWithoutRedispatch()
        {
            Fixture fixture = CreateFixture(group: 1);
            fixture.RankStore.Current = new RankActivityData
            {
                State = RankActivityState.Settling,
                Settled = true,
                RewardClaimed = true,
                FinalRank = 1,
                Group = 1,
                PeriodCount = 1
            };

            var restored = new RankActivityManager(
                fixture.RankStore,
                fixture.Robots,
                fixture.Profile,
                fixture.Awards,
                fixture.Environment,
                fixture.Time,
                fixture.RandomFactory);

            Assert.That(restored.State,
                Is.EqualTo(RankActivityState.NotOpened));
            Assert.That(restored.GetPendingReward(), Is.Null);
            Assert.That(fixture.GameState.GetInFlightAwards(), Is.Empty);
        }

        private static Fixture CreateFixture(int group)
        {
            var time = new FixedTime(1000);
            var randomFactory = new FixedRandomFactory(0, 1f);
            var robotStore = new MemoryRobotStore();
            var robots = new RobotService(
                robotStore,
                time,
                randomFactory);
            var profile = new ProfileService(
                InitializedProfileStore(),
                new FixedProfileRandom());
            var gameState = new GameStateService(
                new GameStateData(),
                new MemoryPlayerStore());
            var awards = new AwardManager(gameState, profile);
            var environment = new MutableEnvironment
            {
                Enabled = true,
                Group = group,
                Level = 11
            };
            var rankStore = new MemoryRankStore();
            var manager = new RankActivityManager(
                rankStore,
                robots,
                profile,
                awards,
                environment,
                time,
                randomFactory);
            return new Fixture(
                manager,
                rankStore,
                robots,
                profile,
                gameState,
                awards,
                environment,
                time,
                randomFactory);
        }

        private static MemoryProfileStore InitializedProfileStore()
        {
            var data = new ProfileData
            {
                Nickname = "ABC123",
                AvatarId = 1,
                FrameId = 1,
                Initialized = true
            };
            foreach (int id in ProfileCatalog.ClassicFrameIds)
                data.OwnedFrames[id] = new AvatarFrame(id, -1);
            return new MemoryProfileStore(data);
        }

        private sealed class Fixture
        {
            public Fixture(
                RankActivityManager manager,
                MemoryRankStore rankStore,
                RobotService robots,
                ProfileService profile,
                GameStateService gameState,
                AwardManager awards,
                MutableEnvironment environment,
                FixedTime time,
                FixedRandomFactory randomFactory)
            {
                Manager = manager;
                RankStore = rankStore;
                Robots = robots;
                Profile = profile;
                GameState = gameState;
                Awards = awards;
                Environment = environment;
                Time = time;
                RandomFactory = randomFactory;
            }
            public RankActivityManager Manager { get; }
            public MemoryRankStore RankStore { get; }
            public RobotService Robots { get; }
            public ProfileService Profile { get; }
            public GameStateService GameState { get; }
            public AwardManager Awards { get; }
            public MutableEnvironment Environment { get; }
            public FixedTime Time { get; }
            public FixedRandomFactory RandomFactory { get; }
        }

        private sealed class MutableEnvironment : IRankActivityEnvironment
        {
            public bool Enabled { get; set; }
            public int Group { get; set; }
            public int Level { get; set; }
            public bool LeaderboardEnabled => Enabled;
            public int LeaderboardGroup => Group;
            public int CurrentLevel => Level;
        }

        private sealed class FixedTime : IRobotTimeProvider
        {
            public FixedTime(long value) { UnixNow = value; }
            public long UnixNow { get; set; }
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

        private sealed class FixedProfileRandom : IProfileRandom
        {
            public int NextInclusive(int minimum, int maximum) => minimum;
        }

        private sealed class MemoryRankStore : IRankActivityStore
        {
            public RankActivityData Current { get; set; } = new();
            public int SaveCount { get; private set; }
            public RankActivityData Load() => Current;
            public bool Save(RankActivityData data)
            {
                Current = data;
                SaveCount++;
                return true;
            }
            public void Reset() { Current = new RankActivityData(); }
        }

        private sealed class MemoryRobotStore : IRobotPoolStore
        {
            private readonly Dictionary<string, RobotPool> _pools = new();
            public IReadOnlyDictionary<string, RobotPool> LoadAll() => _pools;
            public bool SaveAll(IReadOnlyDictionary<string, RobotPool> pools)
            {
                _pools.Clear();
                foreach (KeyValuePair<string, RobotPool> pair in pools)
                    _pools[pair.Key] = pair.Value;
                return true;
            }
            public void Reset() { _pools.Clear(); }
        }

        private sealed class MemoryProfileStore : IProfileDataStore
        {
            private ProfileData _data;
            public MemoryProfileStore(ProfileData data) { _data = data; }
            public ProfileData Load() => _data;
            public bool Save(ProfileData data) { _data = data; return true; }
            public void Reset() { _data = new ProfileData(); }
        }

        private sealed class MemoryPlayerStore : IGameStatePlayerStore
        {
            public bool SavePlayer(GameStateData data) => true;
        }
    }
}
