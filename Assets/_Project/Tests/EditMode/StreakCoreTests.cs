using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.Config;
using Meowdoku.Core.Daily;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class StreakCoreTests
    {
        [Test]
        public void DefaultConfig_RunsBeforeResultWithRewardAndLit()
        {
            var feature = Feature(
                "2026-08-10",
                new StreakData());

            Assert.That(feature.IsEnabled, Is.True);
            Assert.That(feature.HasReward, Is.True);
            Assert.That(feature.ShouldSkipLit, Is.False);
            Assert.That(feature.HasPlayEntry, Is.False);
            Assert.That(feature.IsSettleReorder, Is.False);

            feature.DoCheckin();
            Assert.That(feature.HasPendingShow, Is.True);
            Assert.That(feature.PendingShowUid, Is.Zero);
            Assert.That(feature.Data.CurrentStreak, Is.EqualTo(1));
        }

        [Test]
        public void Checkin_IsIdempotentPerLocalDayAndRewardsEverySevenDays()
        {
            var date = new MutableDate("2026-08-10");
            var store = new MemoryStore(new StreakData
            {
                CurrentStreak = 6,
                BestStreak = 6,
                RewardCycleDay = 6,
                LastCheckinDate = "2026-08-09",
                StreakStartWeekday = 1
            });
            var rewards = new RewardBoundary();
            var feature = new StreakFeature(
                store,
                date,
                rewardBoundary: rewards);

            Assert.That(
                feature.NotifyWin(
                    StreakCheckinSource.Main,
                    true,
                    out StreakCheckinResult first),
                Is.True);
            Assert.That(first.Streak, Is.EqualTo(7));
            Assert.That(first.HasReward, Is.True);
            Assert.That(feature.PendingShowUid, Is.EqualTo(41));
            Assert.That(rewards.StreakDispatches, Is.EqualTo(1));
            Assert.That(
                feature.NotifyWin(
                    StreakCheckinSource.Challenge,
                    true,
                    out _),
                Is.False);
            Assert.That(feature.Data.CurrentStreak, Is.EqualTo(7));
            Assert.That(rewards.StreakDispatches, Is.EqualTo(1));
        }

        [Test]
        public void BrokenCheckin_ResetsToOneWhenProtectIsNotOffered()
        {
            var feature = Feature(
                "2026-08-10",
                new StreakData
                {
                    CurrentStreak = 5,
                    BestStreak = 8,
                    RewardCycleDay = 5,
                    LastCheckinDate = "2026-08-07"
                });

            Assert.That(feature.IsBroken(), Is.True);
            Assert.That(feature.MissedDays(), Is.EqualTo(2));
            StreakCheckinResult result = feature.DoCheckin();
            Assert.That(result.Streak, Is.EqualTo(1));
            Assert.That(result.BestStreak, Is.EqualTo(8));
            Assert.That(result.IsNewStreak, Is.True);
            Assert.That(feature.Data.RewardCycleDay, Is.EqualTo(1));
        }

        [Test]
        public void Backfill_AddsMissedDaysAndCurrentWinAndCrossesRewardCycle()
        {
            var protect = new StreakProtectConfig();
            protect.SetDebugOverride(StreakProtectConfig.ValueBackfill2);
            var rewards = new RewardBoundary();
            var feature = Feature(
                "2026-08-10",
                new StreakData
                {
                    CurrentStreak = 5,
                    BestStreak = 5,
                    RewardCycleDay = 5,
                    LastCheckinDate = "2026-08-07",
                    StreakStartWeekday = 2
                },
                protect,
                rewards);

            Assert.That(
                feature.ShouldOfferRevive(
                    StreakCheckinSource.Challenge,
                    true),
                Is.True);
            feature.SettleWin(StreakCheckinSource.Challenge, true);
            Assert.That(feature.HasPendingReviveDecision, Is.True);
            StreakReviveResult result = feature.ReviveStreak();

            Assert.That(result.MissedDays, Is.EqualTo(2));
            Assert.That(result.Streak, Is.EqualTo(8));
            Assert.That(result.HasReward, Is.True);
            Assert.That(feature.Data.RewardCycleDay, Is.EqualTo(8));
            Assert.That(
                feature.ReviveAnimation.Kind,
                Is.EqualTo(StreakReviveAnimationKind.Backfill));
            Assert.That(feature.ReviveAnimation.Gained, Is.EqualTo(3));
            Assert.That(rewards.StreakDispatches, Is.EqualTo(1));
        }

        [Test]
        public void Resume_IncrementsStreakButRestartsRewardCycleAtOne()
        {
            var protect = new StreakProtectConfig();
            protect.SetDebugOverride(StreakProtectConfig.ValueResume);
            var feature = Feature(
                "2026-08-10",
                new StreakData
                {
                    CurrentStreak = 12,
                    BestStreak = 12,
                    RewardCycleDay = 5,
                    LastCheckinDate = "2026-08-06",
                    StreakStartWeekday = 4
                },
                protect);

            Assert.That(
                feature.ShouldOfferRevive(StreakCheckinSource.Main, true),
                Is.True);
            feature.MarkPendingWinCheckin();
            StreakReviveResult result = feature.ReviveStreak();

            Assert.That(result.IsResume, Is.True);
            Assert.That(result.Streak, Is.EqualTo(13));
            Assert.That(feature.Data.RewardCycleDay, Is.EqualTo(1));
            Assert.That(
                feature.ReviveAnimation.Kind,
                Is.EqualTo(StreakReviveAnimationKind.Resume));
            Assert.That(feature.ReviveAnimation.PreCycle, Is.EqualTo(5));
            Assert.That(feature.ReviveAnimation.PreWeekday, Is.EqualTo(4));
        }

        [Test]
        public void PendingWin_OnNextConstructionResolvesToNewOneDayStreak()
        {
            var store = new MemoryStore(new StreakData
            {
                CurrentStreak = 9,
                BestStreak = 9,
                RewardCycleDay = 4,
                LastCheckinDate = "2026-08-06",
                PendingWinCheckinDate = "2026-08-10"
            });

            var feature = new StreakFeature(
                store,
                new MutableDate("2026-08-10"));

            Assert.That(feature.HasPendingReviveDecision, Is.False);
            Assert.That(feature.Data.CurrentStreak, Is.EqualTo(1));
            Assert.That(feature.Data.RewardCycleDay, Is.EqualTo(1));
            Assert.That(
                feature.Data.LastCheckinDate,
                Is.EqualTo("2026-08-10"));
            Assert.That(feature.Data.StreakStartWeekday, Is.EqualTo(1));
            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        private static StreakFeature Feature(
            string today,
            StreakData data,
            StreakProtectConfig protect = null,
            IStreakRewardBoundary rewards = null)
        {
            return new StreakFeature(
                new MemoryStore(data),
                new MutableDate(today),
                protectConfig: protect,
                rewardBoundary: rewards);
        }

        private sealed class MutableDate : ICurrentDateProvider
        {
            public MutableDate(string value) { CurrentDate = value; }
            public string CurrentDate { get; set; }
        }

        private sealed class MemoryStore : IStreakDataStore
        {
            private StreakData _data;
            public MemoryStore(StreakData data) { _data = data; }
            public int SaveCount { get; private set; }
            public StreakData Load() => _data;
            public bool Save(StreakData data)
            {
                _data = data;
                SaveCount++;
                return true;
            }
            public void Reset() { _data = new StreakData(); }
        }

        private sealed class RewardBoundary : IStreakRewardBoundary
        {
            public int StreakDispatches { get; private set; }
            public int DispatchStreakChest(
                IReadOnlyDictionary<string, int> rewards)
            {
                StreakDispatches++;
                Assert.That(rewards["hint"], Is.EqualTo(2));
                Assert.That(rewards["locate"], Is.EqualTo(2));
                return 41;
            }
            public void DispatchSwitchGift(
                IReadOnlyDictionary<string, int> rewards) { }
            public void ShowAward(int uid) { }
        }
    }
}
