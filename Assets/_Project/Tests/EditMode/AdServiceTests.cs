using System;
using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.Ads;
using Meowdoku.Core.Config;
using Meowdoku.Core.Tracking;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class AdServiceTests
    {
        [Test]
        public void NullProvider_IsUnavailableAndDoesNotCreateTracking()
        {
            var sink = new RecordingSink();
            var tracker = new TrackerService(State(), sink);
            using var service = new AdService(
                State(),
                tracker,
                NullAdProvider.Instance);

            Assert.That(service.IsAvailable, Is.False);
            Assert.That(service.GenerateShowId(), Is.Empty);
            Assert.That(service.TryShowReward("source", null), Is.False);
            Assert.That(sink.Events, Is.Empty);
        }

        [Test]
        public void Impression_ConsumesPreparedIdAndTracksActualShowOnce()
        {
            GameStateService state = State();
            state.SetCurrentLevel(12);
            var sink = new RecordingSink();
            var tracker = new TrackerService(state, sink);
            var provider = new FakeProvider();
            using var service = new AdService(state, tracker, provider);

            Assert.That(service.TryShowReward(
                TrackerCatalog.AdPosition.PropsNormalHint,
                null), Is.True);
            Assert.That(service.IsRewardRequestActive, Is.True);
            Assert.That(service.TryShowReward(
                TrackerCatalog.AdPosition.PropsNormalLocate,
                null), Is.False);
            Assert.That(sink.Events.Count, Is.EqualTo(1));
            Assert.That(sink.Events[0].Name,
                Is.EqualTo(TrackerCatalog.Event.AdShowTiming));

            provider.RaiseImpression(
                TrackerCatalog.Placement.Reward,
                "provider_fallback");

            Assert.That(sink.Events.Count, Is.EqualTo(2));
            Assert.That(sink.Events[1].Name,
                Is.EqualTo(TrackerCatalog.Event.RewardedAdShow));
            Assert.That(sink.Events[1].Parameters["ad_show_id"],
                Is.EqualTo("show-1"));
            Assert.That(sink.Events[1].Parameters["level"], Is.EqualTo(12));
            Assert.That(sink.Events[1].Parameters["position"],
                Is.EqualTo(TrackerCatalog.AdPosition.PropsNormalHint));
        }

        [Test]
        public void RewardedAndClosed_AreDistinctAndCompleteRequestOnlyOnce()
        {
            GameStateService state = State();
            var provider = new FakeProvider();
            using var service = new AdService(
                state,
                new TrackerService(state),
                provider);
            var results = new List<bool>();
            int shown = 0;
            int closed = 0;
            service.AdShown += _ => shown++;
            service.AdClosed += _ => closed++;

            Assert.That(service.TryShowReward(
                TrackerCatalog.AdPosition.NormalGameFail,
                value => results.Add(value)), Is.True);
            provider.RaiseShown(TrackerCatalog.Placement.Reward);
            provider.RaiseRewarded(TrackerCatalog.Placement.Reward);
            provider.RaiseClosed(TrackerCatalog.Placement.Reward);

            Assert.That(shown, Is.EqualTo(1));
            Assert.That(closed, Is.EqualTo(1));
            Assert.That(results, Is.EqualTo(new[] { true }));
            Assert.That(service.IsRewardRequestActive, Is.False);

            Assert.That(service.TryShowReward(
                TrackerCatalog.AdPosition.NormalGameFail,
                value => results.Add(value)), Is.True);
            provider.RaiseClosed(TrackerCatalog.Placement.Reward);
            Assert.That(results, Is.EqualTo(new[] { true, false }));
        }

        [Test]
        public void Dispose_UnsubscribesProviderCallbacksAndCancelsPendingReward()
        {
            GameStateService state = State();
            var provider = new FakeProvider();
            var service = new AdService(
                state,
                new TrackerService(state),
                provider);
            var results = new List<bool>();
            int shown = 0;
            service.AdShown += _ => shown++;
            Assert.That(service.TryShowReward(
                TrackerCatalog.AdPosition.NormalGameFail,
                value => results.Add(value)), Is.True);

            service.Dispose();
            provider.RaiseShown(TrackerCatalog.Placement.Reward);
            provider.RaiseRewarded(TrackerCatalog.Placement.Reward);

            Assert.That(results, Is.EqualTo(new[] { false }));
            Assert.That(shown, Is.Zero);
        }

        [Test]
        public void InterstitialPolicy_PreservesDefaultUnlockProtectionAndCooldownOrder()
        {
            GameStateService state = State();
            state.SetCurrentLevel(11);
            state.Data.SessionCount = 2;
            state.OnGameFinished();
            var provider = new FakeProvider();
            var clock = new FakeAdClock { UnixNow = 1000 };
            using var service = new AdService(
                state,
                new TrackerService(state),
                provider,
                clock);
            var policy = new InterstitialPolicy(
                state,
                service,
                new FixedRandom(0));

            InterstitialPolicyResult first = policy.TryShow(
                TrackerCatalog.AdPosition.NormalStart,
                new InterstitialContext(false, 512));
            Assert.That(first.Shown, Is.True);
            Assert.That(first.Reason, Is.EqualTo(InterstitialBlockReason.None));
            Assert.That(state.InterstitialUnlocked, Is.True);
            Assert.That(provider.ShowCount, Is.EqualTo(1));

            provider.RaiseClosed(TrackerCatalog.Placement.Interstitial);
            clock.UnixNow = 1059;
            InterstitialPolicyResult cooldown = policy.TryShow(
                TrackerCatalog.AdPosition.NormalContinue,
                new InterstitialContext(false, 512));
            Assert.That(cooldown.Shown, Is.False);
            Assert.That(cooldown.Reason,
                Is.EqualTo(InterstitialBlockReason.Cooldown));
            Assert.That(provider.ShowCount, Is.EqualTo(1));
        }

        [Test]
        public void RewardViewProbability_IsConsumedBeforeLaterInterstitialGates()
        {
            GameStateService state = State();
            state.IncrementSessionRewardViewCount();
            var provider = new FakeProvider();
            using var service = new AdService(
                state,
                new TrackerService(state),
                provider);
            var policy = new InterstitialPolicy(
                state,
                service,
                new FixedRandom(99));

            InterstitialPolicyResult result = policy.TryShow(
                TrackerCatalog.AdPosition.NormalStart,
                new InterstitialContext(true, 64));

            Assert.That(result.Reason,
                Is.EqualTo(InterstitialBlockReason.RewardViewProbability));
            Assert.That(state.SessionRewardViewCount, Is.Zero);
            Assert.That(provider.ShowCount, Is.Zero);
        }

        [Test]
        public void RewardWatchdog_PersistsOnlyAfterShownClosedAndThirtySeconds()
        {
            GameStateService state = State();
            var provider = new FakeProvider();
            var clock = new FakeAdClock { UnixNow = 1000 };
            var restore = new CommonRewardAdLogicConfig();
            restore.SetDebugOverride(CommonRewardAdLogicConfig.ValueRestore);
            using var service = new AdService(
                state,
                new TrackerService(state),
                provider,
                clock,
                rewardRestoreConfig: restore);

            Assert.That(service.TryShowReward(
                TrackerCatalog.AdPosition.PropsNormalHint,
                null), Is.True);
            provider.RaiseShown(TrackerCatalog.Placement.Reward);
            provider.RaiseClosed(TrackerCatalog.Placement.Reward);
            Assert.That(service.PendingRewardWatchdogCount, Is.EqualTo(1));

            clock.UnixNow = 1029;
            service.Tick();
            Assert.That(state.HasPendingRewards(), Is.False);
            clock.UnixNow = 1030;
            service.Tick();

            Assert.That(service.PendingRewardWatchdogCount, Is.Zero);
            Assert.That(state.HasPendingRewards(), Is.True);
            var entry = (Dictionary<string, object>)
                state.GetPendingRewards()[0];
            Assert.That(entry["show_id"], Is.EqualTo("show-1"));
            Assert.That(entry["source"], Is.EqualTo(
                TrackerCatalog.AdPosition.PropsNormalHint));
            Assert.That(entry["ts"], Is.EqualTo(1030));
        }

        [Test]
        public void LateRewardCallback_CancelsEarliestWatchdog()
        {
            GameStateService state = State();
            var provider = new FakeProvider();
            var clock = new FakeAdClock { UnixNow = 1000 };
            var restore = new CommonRewardAdLogicConfig();
            restore.SetDebugOverride(CommonRewardAdLogicConfig.ValueRestore);
            using var service = new AdService(
                state,
                new TrackerService(state),
                provider,
                clock,
                rewardRestoreConfig: restore);

            service.TryShowReward(
                TrackerCatalog.AdPosition.PropsNormalLocate,
                null);
            provider.RaiseShown(TrackerCatalog.Placement.Reward);
            provider.RaiseClosed(TrackerCatalog.Placement.Reward);
            clock.UnixNow = 1010;
            provider.RaiseRewarded(TrackerCatalog.Placement.Reward);
            clock.UnixNow = 1040;
            service.Tick();

            Assert.That(service.PendingRewardWatchdogCount, Is.Zero);
            Assert.That(state.HasPendingRewards(), Is.False);
            Assert.That(state.Data.RewardHistoryTimestamps.Count,
                Is.EqualTo(1));
        }

        [Test]
        public void BannerPolicy_PreservesSourceUnlockAndProviderLifecycle()
        {
            GameStateService state = State();
            state.SetCurrentLevel(11);
            state.Data.SessionCount = 2;
            var provider = new FakeProvider();
            using var service = new AdService(
                state,
                new TrackerService(state),
                provider);
            var policy = new BannerPolicy(state, service);

            BannerPolicyResult result = policy.TryShow(
                "game",
                new BannerContext(true, 11, 4));

            Assert.That(result.Shown, Is.True);
            Assert.That(state.BannerUnlocked, Is.True);
            Assert.That(provider.BannerShowCount, Is.EqualTo(1));
            Assert.That(provider.BannerHeight, Is.EqualTo(180));
            service.DestroyBanner();
            Assert.That(provider.DestroyCount, Is.EqualTo(1));
            Assert.That(service.IsBannerActive, Is.False);
        }

        [Test]
        public void BannerPolicy_RejectsLevelBeforeUnlockAndConfiguredSize()
        {
            GameStateService state = State();
            state.SetCurrentLevel(10);
            state.Data.SessionCount = 2;
            var provider = new FakeProvider();
            using var service = new AdService(
                state,
                new TrackerService(state),
                provider);
            var policy = new BannerPolicy(state, service);

            BannerPolicyResult level = policy.TryShow(
                "game",
                new BannerContext(true, 10, 4));
            Assert.That(level.Reason, Is.EqualTo(BannerBlockReason.LevelLocked));

            state.SetCurrentLevel(11);
            var sizes = new BannerUnlockDiffLcConfig();
            sizes.SetDebugOverride("{6}");
            policy = new BannerPolicy(
                state,
                service,
                difficulty: sizes);
            BannerPolicyResult size = policy.TryShow(
                "game",
                new BannerContext(true, 11, 4));
            Assert.That(size.Reason, Is.EqualTo(BannerBlockReason.SizeLocked));
            Assert.That(provider.BannerShowCount, Is.Zero);
        }

        [Test]
        public void RewardRestoreBatch_UsesNewestThreeAndRemovesPresentedEntries()
        {
            GameStateService state = State();
            state.RecordNormalReward(900);
            state.RecordNormalReward(950);
            state.RecordNormalReward(990);
            AddPending(state, "old", TrackerCatalog.AdPosition.PropsNormalHint);
            AddPending(state, "two", TrackerCatalog.AdPosition.PropsNormalLocate);
            AddPending(state, "three", TrackerCatalog.AdPosition.PropsDailyHint);
            AddPending(state, "new", TrackerCatalog.AdPosition.StreakDoubleReward);
            var restore = new RewardRestoreService(state);

            RewardRestoreBatch batch = restore.BuildBatch(1000);

            Assert.That(batch, Is.Not.Null);
            Assert.That(batch.RewardedAdCount, Is.EqualTo(3));
            Assert.That(Count(batch, "hint"), Is.EqualTo(3));
            Assert.That(Count(batch, "locate"), Is.EqualTo(3));
            restore.Complete(batch, true);
            Assert.That(state.RestoredTodayCount, Is.EqualTo(3));
            Assert.That(state.GetPendingRewards().Count, Is.EqualTo(1));
            var remaining = (Dictionary<string, object>)
                state.GetPendingRewards()[0];
            Assert.That(remaining["show_id"], Is.EqualTo("old"));
        }

        [Test]
        public void RewardRestoreBatch_ClearsUnsupportedPendingQueue()
        {
            GameStateService state = State();
            AddPending(state, "fail", TrackerCatalog.AdPosition.NormalGameFail);
            var restore = new RewardRestoreService(state);

            Assert.That(restore.BuildBatch(1000), Is.Null);
            Assert.That(state.HasPendingRewards(), Is.False);
        }

        [Test]
        public void AdRestoreState_RoundTripsSourcePersistenceKeys()
        {
            var source = new GameStateData
            {
                BannerUnlocked = true,
                RestoredTodayCount = 2
            };
            source.PendingRewards.Add(new Dictionary<string, object>
            {
                ["show_id"] = "show-9",
                ["source"] = TrackerCatalog.AdPosition.PropsDailyHint,
                ["ts"] = 900L
            });
            source.RewardHistoryTimestamps.Add(800L);

            GameStateData restored = GameStateData.FromDocuments(
                source.ToPlayerDocument(),
                source.ToEndgameDocument());

            Assert.That(restored.BannerUnlocked, Is.True);
            Assert.That(restored.RestoredTodayCount, Is.EqualTo(2));
            Assert.That(restored.PendingRewards.Count, Is.EqualTo(1));
            Assert.That(restored.RewardHistoryTimestamps,
                Is.EqualTo(new object[] { 800L }));
        }

        private static void AddPending(
            GameStateService state,
            string showId,
            string source)
        {
            state.AddPendingReward(new Dictionary<string, object>
            {
                ["show_id"] = showId,
                ["source"] = source,
                ["ts"] = 1
            });
        }

        private static int Count(RewardRestoreBatch batch, string kind)
        {
            for (int index = 0; index < batch.Items.Count; index++)
                if (batch.Items[index].Kind == kind)
                    return batch.Items[index].Count;
            return 0;
        }

        private static GameStateService State() =>
            new(new GameStateData(), new MemoryStore());

        private sealed class MemoryStore : IGameStatePlayerStore
        {
            public bool SavePlayer(GameStateData data) => true;
        }

        private sealed class FakeProvider : IAdProvider
        {
            private int _ids;
            public int ShowCount { get; private set; }
            public int BannerShowCount { get; private set; }
            public int BannerHeight { get; private set; }
            public int DestroyCount { get; private set; }
            public bool IsAvailable => true;
            public event Action<string> AdShown;
            public event Action<string> AdClosed;
            public event Action<string> AdRewarded;
            public event Action<string, string> AdError;
            public event Action<AdImpression> AdImpression;
            public string CreateShowId() => $"show-{++_ids}";
            public bool IsReady(
                string placementId,
                string position,
                string showId) => true;
            public bool IsValid(string placementId, string position) => true;
            public void Show(
                string placementId,
                string position,
                string showId) => ShowCount++;
            public void ShowBanner(
                string placementId,
                string position,
                bool anchorBottom,
                int offsetBase,
                int heightBase)
            {
                BannerShowCount++;
                BannerHeight = heightBase;
            }
            public void Destroy(string placementId) => DestroyCount++;
            public void RaiseShown(string placementId) =>
                AdShown?.Invoke(placementId);
            public void RaiseClosed(string placementId) =>
                AdClosed?.Invoke(placementId);
            public void RaiseRewarded(string placementId) =>
                AdRewarded?.Invoke(placementId);
            public void RaiseError(string placementId, string message) =>
                AdError?.Invoke(placementId, message);
            public void RaiseImpression(string placementId, string position) =>
                AdImpression?.Invoke(new AdImpression(placementId, position));
        }

        private sealed class FakeAdClock : AdService.IClock
        {
            public long UnixNow { get; set; }
        }

        private sealed class FixedRandom : IAdRandom
        {
            private readonly int _value;
            public FixedRandom(int value) { _value = value; }
            public int Range(int minimumInclusive, int maximumExclusive) =>
                _value;
        }

        private sealed class RecordingSink : ITrackingSink
        {
            public readonly List<Entry> Events = new();
            public void SendEvent(
                string eventName,
                IReadOnlyDictionary<string, object> parameters) =>
                Events.Add(new Entry(
                    eventName,
                    new Dictionary<string, object>(parameters)));
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
