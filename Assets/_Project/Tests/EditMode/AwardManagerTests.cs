using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.Daily;
using Meowdoku.Core.Tracking;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class AwardManagerTests
    {
        [SetUp]
        public void SetUp()
        {
            GlobalUniqueId.ResetForTests();
        }

        [Test]
        public void DirectAward_PersistsImmediatelyAndLeavesNoInFlightEntry()
        {
            GameStateService state = State();
            var manager = new AwardManager(state);

            int uid = manager.Dispatch(
                new[] { AwardItem.Tool("hint", 2) },
                AwardDisplayType.Direct,
                "direct_test");

            Assert.That(uid, Is.EqualTo(1));
            Assert.That(state.GetToolCount("hint"), Is.EqualTo(7));
            Assert.That(state.GetInFlightAwards(), Is.Empty);
            Assert.That(manager.ActiveRenderCount, Is.Zero);
        }

        [Test]
        public void DirectToolAward_TracksPropGetAfterInventoryMutation()
        {
            GameStateService state = State();
            var sink = new RecordingSink();
            var tracker = new TrackerService(state, sink);
            var manager = new AwardManager(state, tracker: tracker);

            manager.Dispatch(
                new[] { AwardItem.Tool("locate", 2) },
                AwardDisplayType.Direct,
                TrackerCatalog.PropSource.StreakChest);

            Assert.That(sink.Name, Is.EqualTo(TrackerCatalog.Event.PropGet));
            Assert.That(sink.Parameters["prop_name"], Is.EqualTo("locate"));
            Assert.That(sink.Parameters["source"],
                Is.EqualTo("streak_chest"));
            Assert.That(sink.Parameters["prop_num"], Is.EqualTo(2));
            Assert.That(sink.Parameters["prop_left"], Is.EqualTo(7));
        }

        [Test]
        public void StreakGift_StaysInFlightUntilPageCompletion()
        {
            GameStateService state = State();
            var manager = new AwardManager(state);
            AwardPresentationRequest request = null;
            manager.AwardPresentationRequested += value => request = value;

            int uid = manager.Dispatch(
                new[] { AwardItem.Tool("locate", 2) },
                AwardDisplayType.StreakGift,
                "streak_chest",
                "streak_reward_ad");

            Assert.That(state.GetToolCount("locate"), Is.EqualTo(5));
            Assert.That(state.GetInFlightAwards().Count, Is.EqualTo(1));
            Assert.That(manager.ShowAward(uid), Is.True);
            Assert.That(request, Is.Not.Null);
            Assert.That(request.Uid, Is.EqualTo(uid));
            Assert.That(request.Items[0].Count, Is.EqualTo(2));
            Assert.That(manager.CompleteAward(uid), Is.True);
            Assert.That(state.GetToolCount("locate"), Is.EqualTo(7));
            Assert.That(state.GetInFlightAwards(), Is.Empty);
            Assert.That(manager.CompleteAward(uid), Is.False);
            Assert.That(state.GetToolCount("locate"), Is.EqualTo(7));
        }

        [Test]
        public void DoubleAward_DoublesToolsButNeverFrames()
        {
            GameStateService state = State();
            var frames = new FrameSink();
            var manager = new AwardManager(state, frames);
            int uid = manager.Dispatch(
                new[]
                {
                    AwardItem.Tool("hint", 2),
                    AwardItem.Frame(9, 1)
                },
                AwardDisplayType.StreakGift,
                "streak_chest",
                "streak_reward_ad");

            Assert.That(manager.DoubleAward(uid), Is.True);
            Assert.That(manager.CompleteAward(uid), Is.True);
            Assert.That(state.GetToolCount("hint"), Is.EqualTo(9));
            Assert.That(frames.TotalGranted, Is.EqualTo(1));
            Assert.That(frames.LastFrameId, Is.EqualTo(9));
        }

        [Test]
        public void ColdStartSweep_GrantsPersistedEntryOnlyOnce()
        {
            var data = new GameStateData();
            data.InFlightAwards.Add(Entry(
                77,
                AwardDisplayType.StreakGift,
                AwardItem.Tool("hint", 3)));
            GameStateService state = State(data);

            _ = new AwardManager(state);
            Assert.That(state.GetToolCount("hint"), Is.EqualTo(8));
            Assert.That(state.GetInFlightAwards(), Is.Empty);

            _ = new AwardManager(state);
            Assert.That(state.GetToolCount("hint"), Is.EqualTo(8));
        }

        [Test]
        public void InvalidBatch_IsRejectedAtomically()
        {
            GameStateService state = State();
            var manager = new AwardManager(state);
            int uid = manager.Dispatch(
                new[]
                {
                    AwardItem.Tool("hint", 2),
                    AwardItem.Tool(string.Empty, 1)
                },
                AwardDisplayType.Direct,
                "invalid_test");

            Assert.That(uid, Is.EqualTo(-1));
            Assert.That(state.GetToolCount("hint"), Is.EqualTo(5));
            Assert.That(state.GetInFlightAwards(), Is.Empty);
        }

        [Test]
        public void InFlightAwards_RoundTripThroughPlayerDocument()
        {
            var data = new GameStateData();
            data.InFlightAwards.Add(Entry(
                13,
                AwardDisplayType.RankGift,
                AwardItem.Frame(4)));

            GameStateData loaded = GameStateData.FromDocuments(
                data.ToPlayerDocument(),
                null);

            Assert.That(loaded.InFlightAwards.Count, Is.EqualTo(1));
            var entry = (Dictionary<string, object>)loaded.InFlightAwards[0];
            Assert.That(AwardItem.ReadInt(entry, "uid"), Is.EqualTo(13));
        }

        [Test]
        public void SeventhDayStreak_DispatchesDurableGiftBeforeClaim()
        {
            GameStateService state = State();
            var awards = new AwardManager(state);
            var streak = new StreakFeature(
                dateProvider: new FixedDate("2026-08-10"),
                rewardBoundary: awards,
                initialData: new StreakData
                {
                    CurrentStreak = 6,
                    BestStreak = 6,
                    RewardCycleDay = 6,
                    LastCheckinDate = "2026-08-09",
                    StreakStartWeekday = 1
                });

            StreakCheckinResult result = streak.DoCheckin();
            Assert.That(result.HasReward, Is.True);
            Assert.That(streak.PendingShowUid, Is.GreaterThan(0));
            Assert.That(state.GetInFlightAwards().Count, Is.EqualTo(1));
            Assert.That(state.GetToolCount("hint"), Is.EqualTo(5));

            Assert.That(
                awards.CompleteAward(streak.PendingShowUid),
                Is.True);
            Assert.That(state.GetToolCount("hint"), Is.EqualTo(7));
            Assert.That(state.GetToolCount("locate"), Is.EqualTo(7));
        }

        private static GameStateService State(GameStateData data = null)
        {
            return new GameStateService(
                data ?? new GameStateData(),
                new MemoryPlayerStore());
        }

        private static Dictionary<string, object> Entry(
            int uid,
            AwardDisplayType displayType,
            AwardItem item)
        {
            return new Dictionary<string, object>
            {
                ["uid"] = uid,
                ["items"] = new List<object> { item.ToDictionary() },
                ["display_type"] = (int)displayType,
                ["reason"] = "test",
                ["bonus_reason"] = string.Empty
            };
        }

        private sealed class MemoryPlayerStore : IGameStatePlayerStore
        {
            public bool SavePlayer(GameStateData data) => true;
        }

        private sealed class FrameSink : IFrameAwardSink
        {
            public int TotalGranted { get; private set; }
            public int LastFrameId { get; private set; }
            public bool GrantFrame(int frameId, int count)
            {
                LastFrameId = frameId;
                TotalGranted += count;
                return true;
            }
        }

        private sealed class RecordingSink : ITrackingSink
        {
            public string Name { get; private set; }
            public IReadOnlyDictionary<string, object> Parameters
            {
                get;
                private set;
            }
            public void SendEvent(
                string eventName,
                IReadOnlyDictionary<string, object> parameters)
            {
                Name = eventName;
                Parameters = new Dictionary<string, object>(parameters);
            }
            public void SetUserProperty(string name, string value) { }
        }

        private sealed class FixedDate : ICurrentDateProvider
        {
            public FixedDate(string date) { CurrentDate = date; }
            public string CurrentDate { get; }
        }
    }
}
