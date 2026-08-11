using System;
using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.Config;
using Meowdoku.Core.Daily;
using Meowdoku.Core.UI;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class DailyCoreTests
    {
        [TestCase(20, "2026-08-10", "", "", DailyEntryState.Locked)]
        [TestCase(21, "2026-08-10", "", "", DailyEntryState.Normal)]
        [TestCase(21, "2026-08-10", "2026-08-10", "", DailyEntryState.Done)]
        [TestCase(21, "2026-08-10", "", "2026-08-11", DailyEntryState.Done)]
        [TestCase(21, "2026-08-10", "", "2026-08-10", DailyEntryState.Normal)]
        public void EntryState_MatchesUnlockAndDateRules(
            int level,
            string today,
            string completed,
            string maxDate,
            DailyEntryState expected)
        {
            Assert.That(
                DailyEntryStateContract.Compute(
                    level,
                    today,
                    completed,
                    maxDate),
                Is.EqualTo(expected));
        }

        [Test]
        public void EntryText_UsesSourceDateCountdownAndDoneFormats()
        {
            var midnight = new DateTime(2026, 8, 10, 0, 0, 0);
            var late = new DateTime(2026, 8, 10, 23, 59, 59);

            Assert.That(
                DailyEntryStateContract.DateKey(midnight),
                Is.EqualTo("2026-08-10"));
            Assert.That(
                DailyEntryStateContract.MonthLocalizationKey(8),
                Is.EqualTo("MONTH_ABBR_8"));
            Assert.That(
                DailyEntryStateContract.TodayDateText("Aug", 10),
                Is.EqualTo("Aug 10"));
            Assert.That(
                DailyEntryStateContract.CountdownText(midnight),
                Is.EqualTo("24:00:00"));
            Assert.That(
                DailyEntryStateContract.CountdownText(late),
                Is.EqualTo("00:00:01"));
            Assert.That(
                DailyEntryStateContract.DoneTimeText(754),
                Is.EqualTo("12:34"));
            Assert.That(
                DailyEntryStateContract.DoneTopPercent(73.46f),
                Is.EqualTo(26.5f));
            Assert.That(
                DailyEntryStateContract.DoneTopPercentDecimals(26.5f),
                Is.EqualTo(1));
            Assert.That(
                DailyEntryStateContract.DoneTopPercentDecimals(26f),
                Is.Zero);
        }

        [Test]
        public void ClockTicker_FirstTickMatchesSourceWallClockBoundary()
        {
            Assert.That(
                ClockTickerContract.SecondsUntilFirstTick(100.0),
                Is.EqualTo(1.0).Within(0.000001));
            Assert.That(
                ClockTickerContract.SecondsUntilFirstTick(100.25),
                Is.EqualTo(0.75).Within(0.000001));
            Assert.That(
                ClockTickerContract.SecondsUntilFirstTick(100.9995),
                Is.EqualTo(1.0005).Within(0.000001));
            Assert.That(
                ClockTickerContract.LocalDateKey(
                    new DateTime(2026, 12, 31, 23, 59, 59)),
                Is.EqualTo("2026-12-31"));
        }

        [Test]
        public void DailyResult_UsesSourceToastAppearAndTimeFormatting()
        {
            Assert.That(
                DailyResultContract.PageShowDelaySeconds(false),
                Is.Zero);
            Assert.That(
                DailyResultContract.ResultAnimationDelaySeconds(false),
                Is.EqualTo(0.8f));
            Assert.That(
                DailyResultContract.PageShowDelaySeconds(true),
                Is.EqualTo(1.5f));
            Assert.That(
                DailyResultContract.ResultAnimationDelaySeconds(true),
                Is.Zero);
            Assert.That(
                DailyResultContract.FormatElapsedSeconds(3599.9f),
                Is.EqualTo("59:59"));
            Assert.That(
                DailyResultContract.FormatElapsedSeconds(3600f),
                Is.EqualTo("60:00"));
            Assert.That(
                DailyResultContract.FormatBeatPercent(95f),
                Is.EqualTo("95.0%"));
        }

        [Test]
        public void DailyStats_MatchesSourceReferenceAndClamp()
        {
            Assert.That(DailyStats.BeatPercent(0, 3, 10), Is.EqualTo(99f));
            Assert.That(DailyStats.BeatPercent(75, 3, 10), Is.EqualTo(92.6f));
            Assert.That(DailyStats.BeatPercent(100000, 5, 12), Is.EqualTo(49f));
        }

        [TestCase(100, 10, 3, DailyPuzzlePool.Regular)]
        [TestCase(101, 10, 4, DailyPuzzlePool.Regular)]
        [TestCase(200, 10, 4, DailyPuzzlePool.Regular)]
        [TestCase(201, 12, 4, DailyPuzzlePool.LkStyle)]
        public void PoolPlan_ControlMatchesSourceLevelBands(
            int currentLevel,
            int size,
            int rank,
            DailyPuzzlePool pool)
        {
            DailyPuzzlePoolPlan plan = DailyPuzzleSelector.ResolvePool(
                currentLevel,
                0);

            Assert.That(plan.Size, Is.EqualTo(size));
            Assert.That(plan.Rank, Is.EqualTo(rank));
            Assert.That(plan.Pool, Is.EqualTo(pool));
            Assert.That(plan.Tier, Is.EqualTo("N"));
        }

        [Test]
        public void DcLevelVariants_MatchSourceSizeRankAndGcRules()
        {
            var config = new DcLevelConfig();
            config.SetDebugOverride(DcLevelConfig.ValueTiered10);
            DailyPuzzlePoolPlan tiered = DailyPuzzleSelector.ResolvePool(
                150,
                0,
                config);
            Assert.That(tiered.Size, Is.EqualTo(10));
            Assert.That(tiered.Rank, Is.EqualTo(5));
            Assert.That(tiered.Pool, Is.EqualTo(DailyPuzzlePool.Gc));

            config.SetDebugOverride(DcLevelConfig.ValueRandom);
            DailyPuzzlePoolPlan even = DailyPuzzleSelector.ResolvePool(1, 0, config);
            DailyPuzzlePoolPlan odd = DailyPuzzleSelector.ResolvePool(1, 1, config);
            Assert.That(even.Size, Is.EqualTo(10));
            Assert.That(odd.Size, Is.EqualTo(12));
            Assert.That(even.Rank, Is.EqualTo(3));
            Assert.That(odd.Rank, Is.EqualTo(3));
            Assert.That(even.Pool, Is.EqualTo(DailyPuzzlePool.Gc));
            Assert.That(odd.Pool, Is.EqualTo(DailyPuzzlePool.Gc));
        }

        [Test]
        public void DayOffset_UsesSourceEpochAndClampsEarlierDates()
        {
            Assert.That(
                DailyPuzzleSelector.DayOffset(new DateTime(2026, 4, 20)),
                Is.Zero);
            Assert.That(
                DailyPuzzleSelector.DayOffset(new DateTime(2026, 4, 21)),
                Is.Zero);
            Assert.That(
                DailyPuzzleSelector.DayOffset(new DateTime(2026, 4, 22)),
                Is.EqualTo(1));
            Assert.That(
                DailyPuzzleSelector.DayOffset(new DateTime(2027, 4, 21)),
                Is.EqualTo(365));
        }

        [Test]
        public void Selector_CyclesEntriesThenEightTransformsAndPreservesExplicitZeroSeed()
        {
            LevelEntry first = Entry(101, false, 1);
            LevelEntry second = Entry(202, true, 0);

            DailyPuzzleSelection selection = DailyPuzzleSelector.SelectFromPool(
                new[] { first, second },
                4,
                3,
                "N",
                DailyPuzzlePool.Regular,
                5);

            Assert.That(selection, Is.Not.Null);
            Assert.That(selection.DailyIndex, Is.EqualTo(5));
            Assert.That(selection.EntryIndex, Is.EqualTo(1));
            Assert.That(selection.Transform, Is.EqualTo(2));
            Assert.That(selection.BankIndex, Is.EqualTo(2));
            Assert.That(selection.Seed, Is.Zero);
            Assert.That(selection.IsSolutionValid, Is.True);
            Assert.That(selection.StrategySteps, Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
            Assert.That(selection.Entry.BankTransform, Is.EqualTo(2));
        }

        [Test]
        public void Selector_SkipsInvalidVirtualEntryAndFallsBackToNextValidOne()
        {
            LevelEntry invalid = Entry(1, false, 0, new[] { 0, 0, 0, 0 });
            LevelEntry valid = Entry(2, false, 0);

            DailyPuzzleSelection selection = DailyPuzzleSelector.SelectFromPool(
                new[] { invalid, valid },
                4,
                3,
                "N",
                DailyPuzzlePool.Regular,
                0);

            Assert.That(selection.IsSolutionValid, Is.True);
            Assert.That(selection.DailyIndex, Is.EqualTo(1));
            Assert.That(selection.EntryIndex, Is.EqualTo(1));
            Assert.That(selection.Seed, Is.EqualTo(2));
        }

        [Test]
        public void LaunchContract_CarriesSourceDailyMetadataAndPrebuiltBoard()
        {
            DailyPuzzleSelection selection = DailyPuzzleSelector.SelectFromPool(
                new[] { Entry(7, true, 42) },
                4,
                3,
                "N",
                DailyPuzzlePool.Regular,
                3);

            DailyGameLaunchRequest launch = DailyPuzzleSelector.CreateLaunch(
                selection,
                "2026-08-10");

            Assert.That(launch, Is.Not.Null);
            Assert.That(launch.Date, Is.EqualTo("2026-08-10"));
            Assert.That(launch.Parameters["daily_mode"], Is.True);
            Assert.That(launch.Parameters["is_daily"], Is.True);
            Assert.That(launch.Parameters["daily_index"], Is.EqualTo(3));
            Assert.That(launch.Parameters["daily_transform"], Is.EqualTo(3));
            Assert.That(launch.Parameters["level_seed"], Is.EqualTo(42));
            Assert.That(
                ((List<object>)launch.Parameters["prebuilt_regions"]).Count,
                Is.EqualTo(4));
            Assert.That(
                ((List<object>)launch.Parameters["prebuilt_solution"]).Count,
                Is.EqualTo(4));
            Assert.That(
                (List<object>)launch.Parameters["prefill_positions"],
                Is.Empty);
        }

        [Test]
        public void GameState_DailyProgressPersistsBestAndMaxDateMonotonically()
        {
            var store = new CountingStore();
            var data = new GameStateData { CurrentLevel = 21 };
            var service = new GameStateService(
                data,
                store,
                dateProvider: new DateProvider("2026-08-10"));

            Assert.That(
                service.CurrentDailyEntryState,
                Is.EqualTo(DailyEntryState.Normal));
            service.SetDailyStartedDate("2026-08-10");
            service.MarkDailyCompleted("2026-08-10", 321, 72.4f);
            service.MarkDailyCompleted("2026-08-10", 400, 60f);

            Assert.That(service.DailyStartedDate, Is.EqualTo("2026-08-10"));
            Assert.That(service.DailyElapsedSeconds, Is.EqualTo(400));
            Assert.That(service.DailyBeatPercent, Is.EqualTo(60f));
            Assert.That(service.DailyBestBeatPercent, Is.EqualTo(72.4f));
            Assert.That(
                service.CurrentDailyEntryState,
                Is.EqualTo(DailyEntryState.Done));
            Assert.That(service.HasWonSinceColdStart, Is.True);

            service.AdvanceMaxDailyDate("2026-08-11");
            int saves = store.SaveCount;
            service.AdvanceMaxDailyDate("2026-08-10");
            service.ClearDailyCompletion();

            Assert.That(service.MaxDailyDate, Is.EqualTo("2026-08-11"));
            Assert.That(service.DailyBestBeatPercent, Is.Zero);
            Assert.That(
                service.CurrentDailyEntryState,
                Is.EqualTo(DailyEntryState.Done));
            Assert.That(store.SaveCount, Is.EqualTo(saves + 1));
        }

        private static LevelEntry Entry(
            int id,
            bool includeSeed,
            int seed,
            int[] solution = null)
        {
            var data = new Dictionary<string, object>
            {
                { "id", id },
                { "size", 4 },
                { "r", 3 },
                { "r1", 1 },
                { "r2", 2 },
                { "r3", 3 },
                { "r4", 4 },
                { "r5", 5 },
                {
                    "regionMap",
                    new[]
                    {
                        new[] { 0, 0, 0, 0 },
                        new[] { 1, 1, 1, 1 },
                        new[] { 2, 2, 2, 2 },
                        new[] { 3, 3, 3, 3 }
                    }
                },
                { "solution", solution ?? new[] { 1, 3, 0, 2 } }
            };
            if (includeSeed) data["seed"] = seed;
            return LevelEntry.FromDictionary(data);
        }

        private sealed class DateProvider : ICurrentDateProvider
        {
            public DateProvider(string date) { CurrentDate = date; }
            public string CurrentDate { get; }
        }

        private sealed class CountingStore : IGameStatePlayerStore
        {
            public int SaveCount { get; private set; }
            public bool SavePlayer(GameStateData data)
            {
                SaveCount++;
                return true;
            }
        }
    }
}
