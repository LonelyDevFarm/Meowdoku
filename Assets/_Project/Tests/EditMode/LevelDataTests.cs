using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.Config;
using NUnit.Framework;
using UnityEngine;

namespace Meowdoku.Tests.EditMode
{
    public sealed class LevelDataTests
    {
        [SetUp]
        public void SetUp()
        {
            BankData.ResetForTests();
            LevelBankIO.LoadOverride = null;
        }

        [TearDown]
        public void TearDown()
        {
            LevelBankIO.LoadOverride = null;
            BankData.ResetForTests();
        }

        [TestCase(1, 4)]
        [TestCase(10, 7)]
        [TestCase(20, 8)]
        [TestCase(55, 9)]
        [TestCase(60, 10)]
        [TestCase(100, 10)]
        [TestCase(101, 7)]
        [TestCase(110, 10)]
        [TestCase(111, 7)]
        public void GetSize_FollowsSourceSchedule(int level, int expectedSize)
        {
            Assert.That(LevelData.GetSize(level), Is.EqualTo(expectedSize));
        }

        [TestCase(10)]
        [TestCase(55)]
        [TestCase(200)]
        [TestCase(456)]
        public void IsSpecialLevel_RecognizesMappedLevels(int level)
        {
            Assert.That(LevelData.IsSpecialLevel(level), Is.True);
        }

        [TestCase(1)]
        [TestCase(21)]
        [TestCase(199)]
        public void IsSpecialLevel_RejectsOrdinaryLevels(int level)
        {
            Assert.That(LevelData.IsSpecialLevel(level), Is.False);
        }

        [Test]
        public void GetStrategy_FirstFiveLevelsAlwaysUseStrategyOneWithoutSaving()
        {
            var data = new GameStateData { CurrentStrategy = 4 };
            var store = new StrategyStore();
            var service = new GameStateService(data, store);

            Assert.That(LevelData.GetStrategy(5, service), Is.EqualTo(1));
            Assert.That(data.CurrentStrategy, Is.EqualTo(4));
            Assert.That(store.SaveCount, Is.Zero);
        }

        [Test]
        public void GetStrategy_Level51MigratesStrategyOneToTwoAndPersists()
        {
            var data = new GameStateData { CurrentStrategy = 1 };
            var store = new StrategyStore();
            var service = new GameStateService(data, store);

            Assert.That(LevelData.GetStrategy(51, service), Is.EqualTo(2));
            Assert.That(data.CurrentStrategy, Is.EqualTo(2));
            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void BankData_LoadsEachSourceShapeAndFiltersRankAndTier()
        {
            LevelBankIO.LoadOverride = filename =>
            {
                switch (filename)
                {
                    case "bankData4x4.json":
                        return RankedBank(1, Entry(4, 1, "N"), Entry(4, 1, "H"));
                    case "bankDataLKStyle7x7.json":
                        return RankedBank(2, Entry(7, 2, "N"));
                    case "bankDataGC11x11.json":
                        return WrappedBank(Entry(11, 3, "N"), Entry(11, 4, "H"));
                    case "bankDataLKModified.json":
                        return WrappedBank(Entry(7, 1, "", 2));
                    default:
                        return null;
                }
            };

            Assert.That(BankData.GetLevels(4, 1).Count, Is.EqualTo(2));
            Assert.That(BankData.GetLevelsByTier(4, 1, "H").Count, Is.EqualTo(1));
            Assert.That(BankData.GetLkStyleLevels(7, 2).Count, Is.EqualTo(1));
            Assert.That(BankData.GetGcRanks(11), Is.EqualTo(new[] { 3, 4 }));
            Assert.That(BankData.GetGcLevelsByTier(11, 4, "H").Count, Is.EqualTo(1));
            Assert.That(BankData.GetLkModifiedLevels()[0].MaxRank, Is.EqualTo(2));
        }

        [Test]
        public void GetLevelEntry_MainPoolInsertsLkModifiedAfterFourOrdinaryEntries()
        {
            var regular = new List<object>();
            for (int i = 0; i < 5; i++) regular.Add(Entry(7, 2, "N"));
            LevelBankIO.LoadOverride = filename =>
            {
                if (filename == "bankData7x7.json")
                    return new Dictionary<string, object> { { "2", regular } };
                if (filename == "bankDataLKModified.json")
                    return WrappedBank(Entry(7, 1, "", 2));
                return null;
            };

            var state = new GameStateService(new GameStateData { CurrentStrategy = 2 }, new StrategyStore());
            for (int i = 0; i < 4; i++)
            {
                LevelEntry regularEntry = LevelData.GetLevelEntry(51, 2, 7, state);
                Assert.That(regularEntry.BankSourceMain, Is.EqualTo("regular"));
                Assert.That(regularEntry.BankIndex, Is.EqualTo(i + 1));
            }

            LevelEntry inserted = LevelData.GetLevelEntry(51, 2, 7, state);
            Assert.That(inserted.BankSource, Is.EqualTo("lk_mod"));
            Assert.That(inserted.BankSourceMain, Is.EqualTo("lk_mod"));
            Assert.That(inserted.BankIndex, Is.EqualTo(1));
        }

        [Test]
        public void GetLevelEntry_OrdinaryPoolDoesNotMutateCachedEntryMetadata()
        {
            LevelBankIO.LoadOverride = filename => filename == "bankData4x4.json"
                ? RankedBank(1, Entry(4, 1, "N"))
                : null;
            var state = new GameStateService(new GameStateData(), new StrategyStore());

            LevelEntry selected = LevelData.GetLevelEntry(1, 1, 4, state);
            LevelEntry cached = BankData.GetLevels(4, 1)[0];

            Assert.That(selected.BankSource, Is.EqualTo("regular"));
            Assert.That(cached.BankSource, Is.Null);
            Assert.That(selected, Is.Not.SameAs(cached));
        }

        [Test]
        public void GetLevelEntry_DefaultSingleRegionPolicySkipsCoarseViolation()
        {
            LevelBankIO.LoadOverride = filename => filename == "bankData4x4.json"
                ? RankedBank(1, EntryWithSingleRegions(4, 1, "N"), Entry(4, 1, "N"))
                : null;
            var state = new GameStateService(new GameStateData(), new StrategyStore());

            LevelEntry selected = LevelData.GetLevelEntry(1, 1, 4, state);

            Assert.That(selected.BankIndex, Is.EqualTo(2));
            Assert.That(LevelData.CountSingleCellRegions(selected.RegionMap, 4), Is.Zero);
        }

        [Test]
        public void GetLevelEntry_Zero51PolicyAppliesOuterStrictThreshold()
        {
            LevelBankIO.LoadOverride = filename => filename == "bankData7x7.json"
                ? RankedBank(2, EntryWithOneSingleRegion(), Entry(7, 2, "N"))
                : filename == "bankDataLKModified.json" ? WrappedBank() : null;
            var state = new GameStateService(new GameStateData { CurrentStrategy = 2 }, new StrategyStore());
            var config = new SingleRegionNumConfig();
            config.SetDebugOverride(SingleRegionNumConfig.ValueZero51);

            LevelEntry selected = LevelData.GetLevelEntry(51, 2, 7, state, config);

            Assert.That(selected.BankIndex, Is.EqualTo(2));
            Assert.That(LevelData.CountSingleCellRegions(selected.RegionMap, 7), Is.Zero);
        }

        [Test]
        public void ComputePrefill_UsesSourceTutorialRegionRules()
        {
            int[][] multiCellRows = RowRegions(4);
            int[][] oneSingle = RowRegions(4);
            oneSingle[0][1] = 99;
            int[] solution = { 1, 3, 0, 2 };

            Assert.That(LevelData.ComputePrefill(1, multiCellRows, solution, 4),
                Is.EqualTo(new Vector2Int(0, 1)));
            Assert.That(LevelData.ComputePrefill(7, oneSingle, solution, 4),
                Is.EqualTo(new Vector2Int(0, 1)));
            Assert.That(LevelData.ComputePrefill(11, multiCellRows, solution, 4), Is.Null);
        }

        [Test]
        public void ComputePuzzleId_NormalizesLabelsAndUsesGodotSha256Format()
        {
            int[][] normalized = RowRegions(4);
            int[][] relabeled = RowRegions(4);
            for (int row = 0; row < 4; row++)
                for (int column = 0; column < 4; column++)
                    relabeled[row][column] = 20 + row * 7;

            Assert.That(LevelData.ComputePuzzleId(4, normalized),
                Is.EqualTo("4_688d7f4c8def909d"));
            Assert.That(LevelData.ComputePuzzleId(4, relabeled),
                Is.EqualTo("4_688d7f4c8def909d"));
        }

        [Test]
        public void LevelGenerator_DefaultMapKeepsGodotComparatorDirection()
        {
            int[][] regions = VerticalStripeRegions(4);

            Assert.That(LevelGenerator.ComputeColorMap(4, regions),
                Is.EqualTo(new[] { 0, 9, 3, 1 }));
        }

        [TestCase(1, new[] { 4, 9, 0, 11 })]
        [TestCase(2, new[] { 11, 0, 9, 4 })]
        [TestCase(123, new[] { 1, 8, 0, 9 })]
        public void LevelGenerator_SeededLcgMatchesSourceFixture(int seed, int[] expected)
        {
            Assert.That(LevelGenerator.ComputeColorMapWithSeed(4, VerticalStripeRegions(4), seed),
                Is.EqualTo(expected));
        }

        [Test]
        public void LevelGenerator_PatternReservesDarkPoolBeforeBackground()
        {
            int[][] grayscale =
            {
                new[] { 10, 10, 10 }, new[] { 20, 20, 20 },
                new[] { 200, 200, 200 }, new[] { 240, 240, 240 }
            };

            int[] result = LevelGenerator.ComputeColorMapForRgbWithPattern(
                4,
                VerticalStripeRegions(4),
                grayscale,
                new[] { 0 });

            Assert.That(result[0], Is.EqualTo(0));
            Assert.That(result[1], Is.Not.EqualTo(0));
            Assert.That(result[2], Is.Not.EqualTo(0));
            Assert.That(result[3], Is.Not.EqualTo(0));
        }

        [Test]
        public void RegionColorPipeline_DefaultPreservesCallerMapButV3Recomputes()
        {
            int[][] regions = VerticalStripeRegions(4);
            int[] supplied = { 3, 2, 1, 0 };
            var defaultConfig = new RegionColorConfig();
            RegionColorResult defaultResult = RegionColorPipeline.Resolve(
                4, regions, supplied, null, defaultConfig);
            Assert.That(defaultResult.ColorMap, Is.SameAs(supplied));
            Assert.That(defaultResult.Palette[0],
                Is.EqualTo(new Color(205f / 255f, 164f / 255f, 0f, 1f)));

            var v3Config = new RegionColorConfig();
            v3Config.SetDebugOverride(RegionColorConfig.ValueCellColorV3);
            RegionColorResult v3Result = RegionColorPipeline.Resolve(
                4, regions, supplied, null, v3Config);
            Assert.That(v3Result.ColorMap,
                Is.EqualTo(LevelGenerator.ComputeColorMapForRgb(
                    4, regions, RegionColorPipeline.PaletteFor(RegionColorConfig.ValueCellColorV3, 4))));
        }

        [TestCase(RegionColorConfig.ValueAllWarm, 10, 10)]
        [TestCase(RegionColorConfig.ValueAllCool, 8, 8)]
        [TestCase(RegionColorConfig.ValueTempBalanced, 7, 7)]
        public void RegionColorPipeline_TemperaturePalettesMatchBoardViewSizes(
            int value,
            int size,
            int expectedCount)
        {
            Assert.That(RegionColorPipeline.PaletteFor(value, size).Length, Is.EqualTo(expectedCount));
        }

        [Test]
        public void ResolveDifficulty_UpperRngMatchesSourceCapsAcrossLevelsOneTo250()
        {
            var state = new GameStateService(new GameStateData { CurrentStrategy = 6 });
            var random = new BoundRandom(useUpper: true);

            for (int level = 1; level <= 250; level++)
            {
                LevelData.LevelDifficultySelection result =
                    LevelData.ResolveDifficulty(level, 6, state, random);
                int expected = LevelData.IsHardLevel(level)
                    ? 5
                    : level <= 5 ? 1
                    : level <= 20 ? 2
                    : level <= 50 ? 3
                    : 4;
                Assert.That(result.Strategy, Is.EqualTo(expected), "level " + level);
            }
        }

        [Test]
        public void ResolveDifficulty_LowerRngUsesInclusiveLowerBound()
        {
            var state = new GameStateService(new GameStateData { CurrentStrategy = 6 });
            var random = new BoundRandom(useUpper: false);

            Assert.That(LevelData.ResolveDifficulty(21, 6, state, random).Strategy, Is.EqualTo(2));
            Assert.That(LevelData.ResolveDifficulty(101, 6, state, random).Strategy, Is.EqualTo(2));
            Assert.That(random.LastMinimum, Is.EqualTo(2));
            Assert.That(random.LastMaximum, Is.EqualTo(4));
            Assert.That(LevelData.IsHardLevel(20), Is.False);
            Assert.That(LevelData.IsSpecialLevel(20), Is.True);
        }

        [Test]
        public void ResolveDifficulty_DailyFirstEasyReducesAfterRandomAndMarksLevel()
        {
            var data = new GameStateData { CurrentStrategy = 4 };
            var state = new GameStateService(
                data,
                dateProvider: new FixedDateProvider("2026-08-08"));
            state.EvaluateDailyFirstEasy();

            LevelData.LevelDifficultySelection result =
                LevelData.ResolveDifficulty(51, 4, state, new BoundRandom(useUpper: true));

            Assert.That(result.Strategy, Is.EqualTo(3));
            Assert.That(state.IsDailyFirstEasyAvailable, Is.False);
            Assert.That(state.IsCurrentLevelDailyFirstEasy, Is.True);
            Assert.That(data.DailyFirstEasyDate, Is.EqualTo("2026-08-08"));
        }

        [Test]
        public void GetLevelEntry_RepresentativeBanksCoverSequenceOneTo250()
        {
            LevelBankIO.LoadOverride = BuildSequenceBank;
            var state = new GameStateService(new GameStateData { CurrentStrategy = 6 });
            var random = new BoundRandom(useUpper: false);

            for (int level = 1; level <= 250; level++)
            {
                LevelEntry entry = LevelData.GetLevelEntry(
                    level,
                    gameState: state,
                    random: random);

                Assert.That(entry, Is.Not.Null, "level " + level);
                if (LevelData.IsSpecialLevel(level))
                    Assert.That(entry.BankSource, Is.EqualTo(level == 200 || level == 250 ? "lk" : "sp"));
                else
                {
                    Assert.That(entry.Size, Is.EqualTo(LevelData.GetSize(level)), "size at level " + level);
                    Assert.That(entry.BankSource, Is.EqualTo("regular"), "source at level " + level);
                }
            }
        }

        private static Dictionary<string, object> RankedBank(int rank, params Dictionary<string, object>[] entries)
        {
            return new Dictionary<string, object> { { rank.ToString(), new List<object>(entries) } };
        }

        private static Dictionary<string, object> WrappedBank(params Dictionary<string, object>[] entries)
        {
            return new Dictionary<string, object> { { "levels", new List<object>(entries) } };
        }

        private static object BuildSequenceBank(string filename)
        {
            if (filename == "bankDataSP.json") return WrappedBank(RepeatedEntries(60, 4, 1));
            if (filename == "bankDataLK.json") return new List<object>(RepeatedEntries(170, 7, 1));
            if (filename == "bankDataLKModified.json") return WrappedBank();
            const string prefix = "bankData";
            if (!filename.StartsWith(prefix) || filename.Length <= prefix.Length ||
                !char.IsDigit(filename[prefix.Length])) return null;

            int separator = filename.IndexOf('x', prefix.Length);
            if (separator < 0 || !int.TryParse(
                    filename.Substring(prefix.Length, separator - prefix.Length),
                    out int size)) return null;
            var root = new Dictionary<string, object>();
            for (int rank = 1; rank <= 5; rank++)
                root[rank.ToString()] = new List<object> { Entry(size, rank, "N") };
            return root;
        }

        private static Dictionary<string, object>[] RepeatedEntries(int count, int size, int rank)
        {
            var result = new Dictionary<string, object>[count];
            for (int index = 0; index < count; index++) result[index] = Entry(size, rank, "N", rank);
            return result;
        }

        private static Dictionary<string, object> Entry(int size, int rank, string tier, int maxRank = -1)
        {
            int[] solution = SolutionFor(size);
            var regionMap = new List<object>();
            for (int row = 0; row < size; row++)
            {
                var values = new List<object>();
                for (int column = 0; column < size; column++) values.Add(row);
                regionMap.Add(values);
            }
            var rawSolution = new List<object>();
            foreach (int value in solution) rawSolution.Add(value);
            var result = new Dictionary<string, object>
            {
                { "size", size }, { "r", rank }, { "tier", tier },
                { "regionMap", regionMap }, { "solution", rawSolution }
            };
            if (maxRank >= 0) result["maxR"] = maxRank;
            return result;
        }

        private static Dictionary<string, object> EntryWithSingleRegions(int size, int rank, string tier)
        {
            Dictionary<string, object> result = Entry(size, rank, tier);
            var regionMap = new List<object>();
            int next = 0;
            for (int row = 0; row < size; row++)
            {
                var values = new List<object>();
                for (int column = 0; column < size; column++) values.Add(next++);
                regionMap.Add(values);
            }
            result["regionMap"] = regionMap;
            return result;
        }

        private static Dictionary<string, object> EntryWithOneSingleRegion()
        {
            Dictionary<string, object> result = Entry(7, 2, "N");
            var regionMap = new List<object>();
            for (int row = 0; row < 7; row++)
            {
                var values = new List<object>();
                for (int column = 0; column < 7; column++)
                    values.Add(row == 0 && column > 0 ? 1 : row);
                regionMap.Add(values);
            }
            result["regionMap"] = regionMap;
            return result;
        }

        private static int[][] RowRegions(int size)
        {
            var result = new int[size][];
            for (int row = 0; row < size; row++)
            {
                result[row] = new int[size];
                for (int column = 0; column < size; column++) result[row][column] = row;
            }
            return result;
        }

        private static int[][] VerticalStripeRegions(int size)
        {
            var result = new int[size][];
            for (int row = 0; row < size; row++)
            {
                result[row] = new int[size];
                for (int column = 0; column < size; column++) result[row][column] = column;
            }
            return result;
        }

        private static int[] SolutionFor(int size)
        {
            if (size == 4) return new[] { 1, 3, 0, 2 };
            if (size == 7) return new[] { 0, 2, 4, 6, 1, 3, 5 };
            var result = new int[size];
            Assert.That(PlaceQueen(0, size, result), Is.True, "queen fixture size " + size);
            return result;
        }

        private static bool PlaceQueen(int row, int size, int[] columns)
        {
            if (row == size) return true;
            for (int column = 0; column < size; column++)
            {
                bool allowed = true;
                for (int previous = 0; previous < row; previous++)
                {
                    if (columns[previous] == column ||
                        System.Math.Abs(columns[previous] - column) == row - previous)
                    {
                        allowed = false;
                        break;
                    }
                }
                if (!allowed) continue;
                columns[row] = column;
                if (PlaceQueen(row + 1, size, columns)) return true;
            }
            return false;
        }

        private sealed class StrategyStore : IGameStatePlayerStore
        {
            public int SaveCount { get; private set; }

            public bool SavePlayer(GameStateData data)
            {
                SaveCount++;
                return true;
            }
        }

        private sealed class BoundRandom : IInclusiveRandom
        {
            private readonly bool _useUpper;
            public int LastMinimum { get; private set; }
            public int LastMaximum { get; private set; }

            public BoundRandom(bool useUpper) { _useUpper = useUpper; }
            public int RangeInclusive(int minimum, int maximum)
            {
                LastMinimum = minimum;
                LastMaximum = maximum;
                return _useUpper ? maximum : minimum;
            }
        }

        private sealed class FixedDateProvider : ICurrentDateProvider
        {
            public FixedDateProvider(string date) { CurrentDate = date; }
            public string CurrentDate { get; }
        }
    }
}
