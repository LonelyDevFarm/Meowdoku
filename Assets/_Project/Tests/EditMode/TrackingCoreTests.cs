using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.Tracking;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class TrackingCoreTests
    {
        [Test]
        public void SourceStack_FollowsScreenDialogButtonAndCloseRules()
        {
            var sink = new RecordingSink();
            var tracker = new TrackerService(
                State(),
                sink,
                new SequentialIds());

            tracker.TrackScreenShown(TrackerCatalog.Screen.Home);
            tracker.TrackDialogShown(TrackerCatalog.Dialog.Settings);
            tracker.TrackButtonClick("close");

            Assert.That(sink.Events[0].Name,
                Is.EqualTo(TrackerCatalog.Event.ScreenShow));
            Assert.That(sink.Events[1].Parameters["source"],
                Is.EqualTo(TrackerCatalog.Screen.Home));
            Assert.That(sink.Events[2].Parameters["source"],
                Is.EqualTo(TrackerCatalog.Dialog.Settings));
            tracker.NotifyDialogClosed(TrackerCatalog.Dialog.Settings);
            Assert.That(tracker.CurrentSource,
                Is.EqualTo(TrackerCatalog.Screen.Home));
        }

        [Test]
        public void GameIdAndRoundStats_PersistAndRestartMatchesSource()
        {
            GameStateService state = State();
            var sink = new RecordingSink();
            var tracker = new TrackerService(
                state,
                sink,
                new SequentialIds());
            Assert.That(
                tracker.NewGameId(TrackerCatalog.GameType.Normal),
                Is.EqualTo("id-1"));
            tracker.IncrementStat("hint_used", 2);
            tracker.IncrementStat("custom_total", 3);
            tracker.OnRestart();
            tracker.TrackGameEnd(new Dictionary<string, object>
            {
                ["result"] = TrackerCatalog.GameResult.Quit
            });

            Assert.That(tracker.GetStat("hint_used"), Is.Zero);
            Assert.That(tracker.GetStat("custom_total"), Is.EqualTo(3));
            Assert.That(tracker.GetStat("restart_count"), Is.EqualTo(1));
            Assert.That(sink.Events[0].Parameters["game_id"],
                Is.EqualTo("id-1"));
            Assert.That(state.GetPersistedGameId("normal"),
                Is.EqualTo("id-1"));
        }

        [Test]
        public void QuestionRotation_MatchesGodotTransformEncoding()
        {
            string[] expected =
            {
                "0", "90", "180", "270",
                "H0", "H90", "H180", "H270",
                "V0", "V90", "V180", "V270"
            };
            for (int index = 0; index < expected.Length; index++)
                Assert.That(
                    TrackerService.TransformToQuestionRotation(index),
                    Is.EqualTo(expected[index]));
        }

        [Test]
        public void Session_FlushesActiveTimeAndRefreshesOnlyAfterThirtyMinutes()
        {
            GameStateService state = State();
            var clock = new FakeClock
            {
                UnixNow = 1000,
                MonotonicMilliseconds = 5000
            };
            var session = new SessionService(
                state,
                true,
                clock,
                new SequentialIds());
            Assert.That(session.SessionId, Is.EqualTo("id-1"));
            Assert.That(state.Data.SessionCount, Is.EqualTo(1));

            clock.MonotonicMilliseconds += 65000;
            Assert.That(session.FlushActiveSegment(), Is.EqualTo(65));
            Assert.That(state.Data.TodayActiveSeconds, Is.EqualTo(65));
            Assert.That(state.Data.TotalActiveSeconds, Is.EqualTo(65));

            session.OnFocusOut();
            clock.UnixNow += 600;
            Assert.That(session.OnFocusIn(), Is.False);
            Assert.That(session.SessionRecord, Is.EqualTo(2));

            session.OnFocusOut();
            clock.UnixNow +=
                SessionService.SessionRefreshIntervalSeconds + 1;
            Assert.That(session.OnFocusIn(), Is.True);
            Assert.That(session.SessionId, Is.EqualTo("id-2"));
            Assert.That(session.SessionRecord, Is.EqualTo(1));
            Assert.That(state.Data.SessionCount, Is.EqualTo(2));
        }

        [Test]
        public void GrtMilestones_RoundTripWithoutDuplicates()
        {
            GameStateService state = State();
            state.MarkGrtLevelD90Reported(10);
            state.MarkGrtLevelD90Reported(10);
            state.MarkGrtEventReported("grt_level6_d0");
            state.MarkGrtEventReported("grt_level6_d0");

            Dictionary<string, object> document =
                state.Data.ToPlayerDocument();
            GameStateData restored =
                GameStateData.FromDocuments(document, null);

            Assert.That(restored.GrtLevelD90Reported,
                Is.EqualTo(new[] { 10 }));
            Assert.That(restored.GrtReportedEvents,
                Is.EqualTo(new[] { "grt_level6_d0" }));
        }

        [Test]
        public void AdAndPropEvents_PreserveExactSourcePayload()
        {
            var sink = new RecordingSink();
            var tracker = new TrackerService(
                State(),
                sink,
                new SequentialIds());

            tracker.TrackProp(
                false,
                TrackerCatalog.Prop.Hint,
                TrackerCatalog.Screen.NormalGame,
                1,
                4);
            tracker.TrackAdShowTiming(
                "show-1",
                TrackerCatalog.Placement.Reward,
                TrackerCatalog.Placement.Reward,
                TrackerCatalog.AdPosition.PropsNormalHint);
            tracker.TrackRewardedAdShow(
                "show-1",
                12,
                TrackerCatalog.AdPosition.PropsNormalHint);
            tracker.RememberAdShowId(
                TrackerCatalog.Placement.Reward,
                "show-1");

            Assert.That(sink.Events[0].Name,
                Is.EqualTo(TrackerCatalog.Event.PropUse));
            Assert.That(sink.Events[0].Parameters["prop_name"],
                Is.EqualTo("hint"));
            Assert.That(sink.Events[0].Parameters["prop_left"],
                Is.EqualTo(4));
            Assert.That(sink.Events[1].Name,
                Is.EqualTo(TrackerCatalog.Event.AdShowTiming));
            Assert.That(sink.Events[1].Parameters["position"],
                Is.EqualTo("props_normal_hint"));
            Assert.That(sink.Events[2].Name,
                Is.EqualTo(TrackerCatalog.Event.RewardedAdShow));
            Assert.That(sink.Events[2].Parameters["level"],
                Is.EqualTo(12));
            Assert.That(
                tracker.ConsumeAdShowId(TrackerCatalog.Placement.Reward),
                Is.EqualTo("show-1"));
            Assert.That(
                tracker.ConsumeAdShowId(TrackerCatalog.Placement.Reward),
                Is.Empty);
        }

        private static GameStateService State() =>
            new(new GameStateData(), new MemoryStore());

        private sealed class MemoryStore : IGameStatePlayerStore
        {
            public bool SavePlayer(GameStateData data) => true;
        }

        private sealed class SequentialIds : ITrackingIdProvider
        {
            private int _value;
            public string NewId() => $"id-{++_value}";
        }

        private sealed class FakeClock : ITrackingClock
        {
            public long UnixNow { get; set; }
            public long MonotonicMilliseconds { get; set; }
        }

        private sealed class RecordingSink : ITrackingSink
        {
            public readonly List<Entry> Events = new();
            public void SendEvent(
                string eventName,
                IReadOnlyDictionary<string, object> parameters)
            {
                Events.Add(new Entry(
                    eventName,
                    new Dictionary<string, object>(parameters)));
            }
            public void SetUserProperty(string name, string value) { }
        }

        private sealed class Entry
        {
            public Entry(
                string name,
                Dictionary<string, object> parameters)
            {
                Name = name;
                Parameters = parameters;
            }
            public string Name { get; }
            public Dictionary<string, object> Parameters { get; }
        }
    }
}
