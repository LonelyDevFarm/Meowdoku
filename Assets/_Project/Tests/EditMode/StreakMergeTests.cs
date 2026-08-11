using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.Daily;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class StreakMergeTests
    {
        [Test]
        public void WeekSlots_ShowPartialAndFullSevenDayCycle()
        {
            var feature = new StreakFeature(
                new MemoryStore(new StreakData
                {
                    CurrentStreak = 3,
                    RewardCycleDay = 3,
                    LastCheckinDate = "2026-08-10",
                    StreakStartWeekday = 1
                }),
                new FixedDate("2026-08-10"));

            IReadOnlyList<StreakWeekSlot> partial = feature.GetWeekSlots();
            Assert.That(partial.Count, Is.EqualTo(7));
            Assert.That(partial[0].IsChecked, Is.True);
            Assert.That(partial[2].IsChecked, Is.True);
            Assert.That(partial[3].IsChecked, Is.False);

            feature.Data.RewardCycleDay = 7;
            IReadOnlyList<StreakWeekSlot> full = feature.GetWeekSlots();
            Assert.That(full[6].IsChecked, Is.True);
        }

        [Test]
        public void Merge_MatchesIndependentMaximumAndRecentDateRules()
        {
            const int today = 1000;
            StreakData merged = StreakData.ResolveMerge(
                Record(10, 10, "today", 1, 3),
                Record(5, 5, "old", 2, 5),
                today,
                today - 4);
            Assert.That(merged.CurrentStreak, Is.EqualTo(10));
            Assert.That(merged.RewardCycleDay, Is.EqualTo(5));
            Assert.That(merged.LastCheckinDate, Is.EqualTo("today"));

            StreakData missingCycle = StreakData.ResolveMerge(
                Record(3, 3, "local", 1, 3),
                new Dictionary<string, object>
                {
                    ["current_streak"] = 6,
                    ["best_streak"] = 6,
                    ["last_checkin_date"] = "remote",
                    ["streak_start_weekday"] = 2
                },
                today - 1,
                today);
            Assert.That(missingCycle.CurrentStreak, Is.EqualTo(6));
            Assert.That(missingCycle.RewardCycleDay, Is.EqualTo(6));
            Assert.That(missingCycle.LastCheckinDate, Is.EqualTo("remote"));
        }

        [Test]
        public void Merge_CapsBestAndKeepsBestAtLeastCurrent()
        {
            StreakData capped = StreakData.ResolveMerge(
                Record(3, 999999999, "today", 1, 3),
                Record(8, 0, string.Empty, -1, 8),
                1000,
                0);
            Assert.That(
                capped.BestStreak,
                Is.EqualTo(StreakData.BestStreakCap));
            Assert.That(
                capped.BestStreak,
                Is.GreaterThanOrEqualTo(capped.CurrentStreak));
        }

        [Test]
        public void GroupSwitchMapping_MatchesSourceAliasesAndPages()
        {
            Assert.That(StreakFeature.MapSwitchPage(6, 0), Is.EqualTo(1));
            Assert.That(StreakFeature.MapSwitchPage(3, 0), Is.EqualTo(2));
            Assert.That(StreakFeature.MapSwitchPage(3, 7), Is.EqualTo(3));
            Assert.That(StreakFeature.MapSwitchPage(2, 4), Is.Zero);
        }

        [Test]
        public void DateMath_UsesLocalCalendarAcrossMonthAndLeapDay()
        {
            int feb28 = StreakDateMath.DateToJulianDay("2028-02-28");
            int feb29 = StreakDateMath.DateToJulianDay("2028-02-29");
            int mar1 = StreakDateMath.DateToJulianDay("2028-03-01");
            Assert.That(feb29 - feb28, Is.EqualTo(1));
            Assert.That(mar1 - feb29, Is.EqualTo(1));
            Assert.That(
                StreakDateMath.Offset("2028-02-28", 2),
                Is.EqualTo("2028-03-01"));
            Assert.That(
                StreakDateMath.Weekday("2026-08-10"),
                Is.EqualTo(1));
        }

        private static Dictionary<string, object> Record(
            int current,
            int best,
            string date,
            int weekday,
            int cycle)
        {
            return new Dictionary<string, object>
            {
                ["current_streak"] = current,
                ["best_streak"] = best,
                ["last_checkin_date"] = date,
                ["streak_start_weekday"] = weekday,
                ["reward_cycle_day"] = cycle
            };
        }

        private sealed class FixedDate : ICurrentDateProvider
        {
            public FixedDate(string value) { CurrentDate = value; }
            public string CurrentDate { get; }
        }

        private sealed class MemoryStore : IStreakDataStore
        {
            private StreakData _data;
            public MemoryStore(StreakData data) { _data = data; }
            public StreakData Load() => _data;
            public bool Save(StreakData data)
            {
                _data = data;
                return true;
            }
            public void Reset() { _data = new StreakData(); }
        }
    }
}
