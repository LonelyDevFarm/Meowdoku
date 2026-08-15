using System;
using System.Collections.Generic;
using System.IO;
using Meowdoku.Core;
using Meowdoku.Core.Daily;
using Meowdoku.Core.Profile;
using Meowdoku.Core.Rank;
using Meowdoku.Core.Robot;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class MetaPersistenceDurabilityTests
    {
        private string _directory;

        [SetUp]
        public void SetUp()
        {
            GlobalUniqueId.ResetForTests();
            _directory = Path.Combine(
                Path.GetTempPath(),
                "MeowdokuMetaDurabilityTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
        }

        [Test]
        public void ProfileRepository_WritesSourceEnvelopeAndReadsLegacyUnityShape()
        {
            var data = InitializedProfile();
            data.Nickname = "SourceCat";
            var repository = new ProfileRepository(_directory);

            Assert.That(repository.Save(data), Is.True);
            var rawStore = new SaveStore(
                ProfileRepository.SavePassword,
                _directory,
                false,
                Path.Combine(_directory, "profile.cfg"));
            Dictionary<string, object> document = rawStore.LoadConfig();
            Assert.That(document, Is.Not.Null);
            Assert.That(document["profile"],
                Is.InstanceOf<IReadOnlyDictionary<string, object>>());
            var section = (IReadOnlyDictionary<string, object>)
                document["profile"];
            Assert.That(section.Keys, Is.EquivalentTo(new[] { "data" }));
            Assert.That(section["data"],
                Is.InstanceOf<IReadOnlyDictionary<string, object>>());
            Assert.That(repository.Load().Nickname, Is.EqualTo("SourceCat"));

            string legacyDirectory = Path.Combine(_directory, "legacy");
            Directory.CreateDirectory(legacyDirectory);
            var legacyStore = new SaveStore(
                ProfileRepository.SavePassword,
                legacyDirectory,
                false,
                Path.Combine(legacyDirectory, "profile.cfg"));
            Assert.That(legacyStore.SaveConfig(new Dictionary<string, object>
            {
                ["profile"] = data.ToDictionary()
            }), Is.True);
            Assert.That(
                new ProfileRepository(legacyDirectory).Load().Nickname,
                Is.EqualTo("SourceCat"));
        }

        [Test]
        public void BackgroundProfileWrites_CoalesceAndFlushLatestImmutableState()
        {
            var repository = new ProfileRepository(
                _directory,
                useBackgroundWrites: true);
            ProfileData data = InitializedProfile();
            data.Nickname = "First";
            Assert.That(repository.Save(data), Is.True);
            data.Nickname = "Latest";
            Assert.That(repository.Save(data), Is.True);
            data.Nickname = "NotQueued";

            Assert.That(repository.FlushPendingWrites(), Is.True);
            Assert.That(
                new ProfileRepository(_directory).Load().Nickname,
                Is.EqualTo("Latest"));
        }

        [Test]
        public void RankReward_ProcessRestartAndClockRollback_RemainIdempotent()
        {
            var time = new MutableTime(10_000);
            var environment = new MutableEnvironment();
            var randomFactory = new FixedRobotRandomFactory();

            Runtime first = OpenRuntime(time, environment, randomFactory);
            Assert.That(first.Rank.MaybeOpen(true), Is.True);
            first.Rank.ConfirmParticipation();
            first.Rank.NotifyLevelStart();
            first.Rank.SetLevelCollect(5_000);
            time.UnixNow = 10_600;
            first.Rank.NotifyLevelWin();
            int rankBeforeRestart = first.Rank.GetPlayerRank();
            Assert.That(rankBeforeRestart, Is.EqualTo(1));

            time.UnixNow = 10_100;
            Runtime rolledBack = OpenRuntime(
                time,
                environment,
                randomFactory);
            Assert.That(rolledBack.Rank.State,
                Is.EqualTo(RankActivityState.OpenJoined));
            Assert.That(rolledBack.Rank.CollectTotal, Is.EqualTo(5_000));
            Assert.That(rolledBack.Robots.HasPool(
                rolledBack.RankStore.Load().RobotKey), Is.True);
            Assert.That(rolledBack.Rank.GetPlayerRank(),
                Is.EqualTo(rankBeforeRestart),
                "Robot ranking must not rewind after a process restart with a rolled-back clock.");

            time.UnixNow = 10_000 +
                           RankActivityConfig.PeriodDurationSeconds + 1;
            rolledBack.Rank.Tick();
            Assert.That(rolledBack.Rank.GetPendingReward(), Is.Not.Null);
            string previousPoolKey = rolledBack.RankStore.Load().RobotKey;
            int uid = rolledBack.Rank.ClaimReward(atHome: false);
            Assert.That(uid, Is.GreaterThan(0));
            Assert.That(rolledBack.GameState.GetInFlightAwards(), Has.Count.EqualTo(1));
            Assert.That(rolledBack.Profile.GetFrameCount(
                ProfileCatalog.FirstPlaceFrameId), Is.Zero);

            Runtime recovered = OpenRuntime(
                time,
                environment,
                randomFactory);
            Assert.That(recovered.Rank.State,
                Is.EqualTo(RankActivityState.NotOpened));
            Assert.That(recovered.GameState.GetInFlightAwards(), Is.Empty);
            Assert.That(recovered.Profile.GetFrameCount(
                ProfileCatalog.FirstPlaceFrameId), Is.EqualTo(1));
            Assert.That(recovered.GameState.GetToolCount("locate"),
                Is.EqualTo(101));
            Assert.That(recovered.GameState.GetToolCount("hint"),
                Is.EqualTo(101));
            Assert.That(recovered.Robots.HasPool(previousPoolKey), Is.False);
            Assert.That(recovered.Rank.OnHomeShown(), Is.True);
            Assert.That(recovered.Rank.State,
                Is.EqualTo(RankActivityState.OpenNotJoined));
            Assert.That(recovered.Rank.PeriodCount, Is.EqualTo(2));
            string nextPoolKey = recovered.RankStore.Load().RobotKey;
            Assert.That(nextPoolKey, Is.Not.EqualTo(previousPoolKey));
            Assert.That(recovered.Robots.HasPool(nextPoolKey), Is.True);

            Runtime restartedAgain = OpenRuntime(
                time,
                environment,
                randomFactory);
            Assert.That(restartedAgain.Rank.State,
                Is.EqualTo(RankActivityState.OpenNotJoined));
            Assert.That(restartedAgain.Rank.PeriodCount, Is.EqualTo(2));
            Assert.That(restartedAgain.Robots.HasPool(nextPoolKey), Is.True);
            Assert.That(restartedAgain.GameState.GetInFlightAwards(), Is.Empty);
            Assert.That(restartedAgain.Profile.GetFrameCount(
                ProfileCatalog.FirstPlaceFrameId), Is.EqualTo(1));
            Assert.That(restartedAgain.GameState.GetToolCount("locate"),
                Is.EqualTo(101));
            Assert.That(restartedAgain.GameState.GetToolCount("hint"),
                Is.EqualTo(101));
        }

        private Runtime OpenRuntime(
            MutableTime time,
            MutableEnvironment environment,
            IRobotRandomFactory randomFactory)
        {
            var gameRepository = new GameStateRepository(_directory);
            var gameState = new GameStateService(
                gameRepository.Load(),
                gameRepository);
            var profile = new ProfileService(
                new ProfileRepository(_directory),
                new FixedProfileRandom());
            var robots = new RobotService(
                new RobotRepository(_directory),
                time,
                randomFactory);
            var awards = new AwardManager(gameState, profile);
            var rankStore = new RankActivityRepository(_directory);
            var rank = new RankActivityManager(
                rankStore,
                robots,
                profile,
                awards,
                environment,
                time,
                randomFactory);
            return new Runtime(
                gameState,
                profile,
                robots,
                rankStore,
                rank);
        }

        private static ProfileData InitializedProfile()
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
            return data;
        }

        private sealed class Runtime
        {
            public Runtime(
                GameStateService gameState,
                ProfileService profile,
                RobotService robots,
                RankActivityRepository rankStore,
                RankActivityManager rank)
            {
                GameState = gameState;
                Profile = profile;
                Robots = robots;
                RankStore = rankStore;
                Rank = rank;
            }

            public GameStateService GameState { get; }
            public ProfileService Profile { get; }
            public RobotService Robots { get; }
            public RankActivityRepository RankStore { get; }
            public RankActivityManager Rank { get; }
        }

        private sealed class MutableEnvironment : IRankActivityEnvironment
        {
            public bool LeaderboardEnabled => true;
            public int LeaderboardGroup => RankActivityConfig.GroupCats;
            public int CurrentLevel => RankActivityConfig.UnlockLevel;
        }

        private sealed class MutableTime : IRobotTimeProvider
        {
            public MutableTime(long unixNow) { UnixNow = unixNow; }
            public long UnixNow { get; set; }
        }

        private sealed class FixedProfileRandom : IProfileRandom
        {
            public int NextInclusive(int minimum, int maximum) => minimum;
        }

        private sealed class FixedRobotRandomFactory : IRobotRandomFactory
        {
            public IRobotRandom Create() => new FixedRobotRandom();
        }

        private sealed class FixedRobotRandom : IRobotRandom
        {
            public int NextInclusive(int minimum, int maximum) => minimum;
            public float NextFloat() => 1f;
        }
    }
}
