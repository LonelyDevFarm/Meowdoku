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

        [Test]
        public void SizeCycle_ControlMatchesSourceGameplayScheduleOneTo250()
        {
            var config = new SizeCycleConfig();
            for (int level = 1; level <= 250; level++)
            {
                Assert.That(
                    config.ResolveSize(level),
                    Is.EqualTo(ExpectedControlSize(level)),
                    "level " + level);
            }
            Assert.That(config.ResolveSize(0), Is.Zero);
        }

        [TestCase(SizeCycleConfig.ValueCycleV3A, 22, 10)]
        [TestCase(SizeCycleConfig.ValueCycleV3B, 15, 9)]
        [TestCase(SizeCycleConfig.ValueCycleV3B, 24, 8)]
        [TestCase(SizeCycleConfig.ValueCycleV3C, 100, 10)]
        [TestCase(SizeCycleConfig.ValueCycleV3C, 102, 10)]
        [TestCase(SizeCycleConfig.ValueCycleV3D, 2, 5)]
        [TestCase(SizeCycleConfig.ValueCycleV3D, 11, 8)]
        [TestCase(SizeCycleConfig.ValueCycleV3E, 22, 10)]
        [TestCase(SizeCycleConfig.ValueCycleV3E, 53, 11)]
        [TestCase(SizeCycleConfig.ValueCycleV3F, 2, 5)]
        [TestCase(SizeCycleConfig.ValueCycleV3F, 53, 11)]
        public void SizeCycle_VariantsMatchSourceBoundaries(
            int value,
            int level,
            int expectedSize)
        {
            var config = new SizeCycleConfig();
            config.SetDebugOverride(value);
            Assert.That(config.ResolveSize(level), Is.EqualTo(expectedSize));
        }

        [Test]
        public void GetLevelEntry_ControlCycleOverridesBaseLevelSize()
        {
            LevelBankIO.LoadOverride = BuildSequenceBank;
            var state = new GameStateService(new GameStateData());
            var config = new SizeCycleConfig();

            LevelEntry entry = LevelData.GetLevelEntry(
                3,
                currentStrategy: 1,
                overrideSize: config.ResolveSize(3),
                gameState: state);

            Assert.That(LevelData.GetSize(3), Is.EqualTo(5));
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.Size, Is.EqualTo(6),
                "Main gameplay must use _get_ab_size(), not LevelData.SIZES directly.");
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
        public void GetLevelEntry_SpecialMapMatchesEverySourceEntry()
        {
            LevelBankIO.LoadOverride = BuildSequenceBank;
            var expected = new Dictionary<int, (string source, int index)>
            {
                { 10, ("sp", 44) }, { 20, ("sp", 45) },
                { 30, ("sp", 36) }, { 40, ("sp", 9) },
                { 50, ("sp", 37) }, { 55, ("sp", 7) },
                { 60, ("sp", 34) }, { 62, ("sp", 8) },
                { 70, ("sp", 32) }, { 75, ("sp", 6) },
                { 80, ("sp", 43) }, { 90, ("sp", 33) },
                { 100, ("sp", 5) }, { 123, ("sp", 1) },
                { 200, ("lk", 30) }, { 250, ("lk", 75) },
                { 314, ("lk", 141) }, { 456, ("sp", 2) }
            };

            foreach (KeyValuePair<int, (string source, int index)> pair in expected)
            {
                LevelEntry entry = LevelData.GetLevelEntry(pair.Key);
                Assert.That(entry, Is.Not.Null, "level " + pair.Key);
                Assert.That(entry.BankSource, Is.EqualTo(pair.Value.source),
                    "source at level " + pair.Key);
                Assert.That(entry.BankIndex, Is.EqualTo(pair.Value.index),
                    "index at level " + pair.Key);
            }
        }

        [Test]
        public void GetLevelEntry_Level10VariantSelectsSp57()
        {
            LevelBankIO.LoadOverride = BuildSequenceBank;
            var config = new NormalLevel10Config();

            Assert.That(LevelData.GetLevelEntry(10,
                    normalLevel10Config: config).BankIndex,
                Is.EqualTo(44));

            config.SetDebugOverride(NormalLevel10Config.ValueSp57);
            LevelEntry variant = LevelData.GetLevelEntry(
                10,
                normalLevel10Config: config);
            Assert.That(variant.BankSource, Is.EqualTo("sp"));
            Assert.That(variant.BankIndex, Is.EqualTo(57));
        }

        [TestCase(110)]
        [TestCase(120)]
        [TestCase(310)]
        public void ResolveDifficulty_OrdinaryHardLevelUsesRankFiveNormalTier(
            int level)
        {
            var state = new GameStateService(
                new GameStateData { CurrentStrategy = 7 });
            LevelData.LevelDifficultySelection result =
                LevelData.ResolveDifficulty(
                    level,
                    7,
                    state,
                    new BoundRandom(useUpper: false));

            Assert.That(LevelData.IsHardLevel(level), Is.True);
            Assert.That(LevelData.IsSpecialLevel(level), Is.False);
            Assert.That(result.Rank, Is.EqualTo(5));
            Assert.That(result.Tier, Is.EqualTo("N"));
            Assert.That(result.Strategy, Is.EqualTo(5));
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
        public void BankData_RealVariantBanksMatchSourceInventory()
        {
            var lkStyleCounts = new Dictionary<int, int[]>
            {
                { 7, new[] { 0, 481, 488, 493, 10 } },
                { 8, new[] { 0, 500, 500, 868, 500 } },
                { 9, new[] { 0, 500, 500, 1002, 500 } },
                { 10, new[] { 0, 500, 500, 809, 597 } },
                { 11, new[] { 10, 10, 10, 20, 20 } },
                { 12, new[] { 0, 4, 5, 700, 17 } }
            };
            var gcCounts = new Dictionary<int, int[]>
            {
                { 6, new[] { 10, 6, 5, 100, 0 } },
                { 8, new[] { 28, 300, 300, 150, 100 } },
                { 9, new[] { 30, 301, 300, 110, 20 } },
                { 10, new[] { 100, 300, 418, 135, 210 } },
                { 11, new[] { 0, 100, 500, 500, 400 } },
                { 12, new[] { 0, 100, 100, 400, 302 } }
            };

            Assert.That(BankData.GetLkStyleSizes(),
                Is.EqualTo(new[] { 7, 8, 9, 10, 11, 12 }));
            foreach (KeyValuePair<int, int[]> pair in lkStyleCounts)
            {
                for (int rank = 1; rank <= 5; rank++)
                    Assert.That(
                        BankData.GetLkStyleLevelCount(pair.Key, rank),
                        Is.EqualTo(pair.Value[rank - 1]),
                        $"LK Style {pair.Key}x{pair.Key} rank {rank}");
            }

            Assert.That(BankData.GetGcSizes(),
                Is.EqualTo(new[] { 6, 8, 9, 10, 11, 12 }));
            foreach (KeyValuePair<int, int[]> pair in gcCounts)
            {
                for (int rank = 1; rank <= 5; rank++)
                    Assert.That(
                        BankData.GetGcLevelCount(pair.Key, rank),
                        Is.EqualTo(pair.Value[rank - 1]),
                        $"GC {pair.Key}x{pair.Key} rank {rank}");
            }

            IReadOnlyList<LevelEntry> lkModified =
                BankData.GetLkModifiedLevels();
            Assert.That(lkModified.Count, Is.EqualTo(169));
            Assert.That(lkModified[0].Id, Is.EqualTo(151));
            Assert.That(lkModified[19].Id, Is.EqualTo(189));
            Assert.That(lkModified[168].Id, Is.EqualTo(353));
        }

        [Test]
        public void BankData_RealVariantTierFiltersMatchSourceInventory()
        {
            Assert.That(BankData.GetLkStyleLevelCount(12, 4), Is.EqualTo(700));
            Assert.That(BankData.GetLkStyleLevelCountByTier(12, 4, "N"), Is.EqualTo(429));
            Assert.That(BankData.GetLkStyleLevelCountByTier(12, 4, "H"), Is.EqualTo(200));
            Assert.That(BankData.GetLkStyleLevelCountByTier(12, 4, string.Empty), Is.EqualTo(71));

            Assert.That(BankData.GetGcLevelCount(10, 3), Is.EqualTo(418));
            Assert.That(BankData.GetGcLevelCountByTier(10, 3, "N"), Is.EqualTo(10));
            Assert.That(BankData.GetGcLevelCountByTier(10, 3, "H"), Is.EqualTo(11));
            Assert.That(BankData.GetGcLevelCountByTier(10, 3, string.Empty), Is.EqualTo(397));

            Assert.That(BankData.GetGcLevelCount(11, 5), Is.EqualTo(400));
            Assert.That(BankData.GetGcLevelCountByTier(11, 5, "N"), Is.EqualTo(177));
            Assert.That(BankData.GetGcLevelCountByTier(11, 5, "H"), Is.EqualTo(20));
            Assert.That(BankData.GetGcLevelCountByTier(11, 5, string.Empty), Is.EqualTo(203));
        }

        [Test]
        public void GetLevelEntry_OrdinaryPoolOrderIncludesLkStyleThenEligibleGc()
        {
            LevelBankIO.LoadOverride = filename => filename switch
            {
                "bankData10x10.json" => RankedBank(1, Entry(10, 1, "N")),
                "bankDataLKStyle10x10.json" => RankedBank(1, Entry(10, 1, "N")),
                "bankDataGC10x10.json" => WrappedBank(Entry(10, 1, "N")),
                _ => null
            };
            var state = new GameStateService(
                new GameStateData { CurrentStrategy = 1 },
                new StrategyStore());

            LevelEntry regular = LevelData.GetLevelEntry(21, 1, 10, state);
            LevelEntry lkStyle = LevelData.GetLevelEntry(21, 1, 10, state);
            LevelEntry gc = LevelData.GetLevelEntry(21, 1, 10, state);

            Assert.That(regular.BankSource, Is.EqualTo("regular"));
            Assert.That(lkStyle.BankSource, Is.EqualTo("lkstyle"));
            Assert.That(gc.BankSource, Is.EqualTo("gc"));
            Assert.That(regular.BankIndex, Is.EqualTo(1));
            Assert.That(lkStyle.BankIndex, Is.EqualTo(1));
            Assert.That(gc.BankIndex, Is.EqualTo(1));
        }

        [Test]
        public void GetLevelEntry_MainSkipsReservedLkModifiedAndDoesNotTransformIt()
        {
            var lkModified = new List<object>();
            for (int index = 1; index <= 21; index++)
            {
                Dictionary<string, object> entry = Entry(7, 2, string.Empty, 2);
                entry["id"] = index;
                lkModified.Add(entry);
            }
            LevelBankIO.LoadOverride = filename => filename switch
            {
                "bankData7x7.json" => RankedBank(2, Entry(7, 2, "N")),
                "bankDataLKModified.json" =>
                    new Dictionary<string, object> { { "levels", lkModified } },
                _ => null
            };
            var data = new GameStateData { CurrentStrategy = 2 };
            data.MainBankProgress[GameStateService.ProgressKey(7, 2, "N")] =
                new Dictionary<string, object>
                {
                    { "idx", 0 }, { "since_lk", 4 }, { "transform", 5 }
                };
            data.LkModifiedProgress[GameStateService.LkModifiedProgressKey(7, 2)] =
                new Dictionary<string, object> { { "idx", 19 } };
            var store = new StrategyStore();
            var state = new GameStateService(data, store);

            LevelEntry selected = LevelData.GetLevelEntry(51, 2, 7, state);

            Assert.That(selected.BankSourceMain, Is.EqualTo("lk_mod"));
            Assert.That(selected.Id, Is.EqualTo(21),
                "Source position 20 is reserved, so filtered index 20 maps to source position 21.");
            Assert.That(selected.BankIndex, Is.EqualTo(20));
            Assert.That(selected.BankTransform, Is.EqualTo(5));
            Assert.That(selected.RegionMap[0], Is.EqualTo(new[] { 0, 0, 0, 0, 0, 0, 0 }),
                "LK Modified stores the selected transform metadata but is not transformed.");
            Assert.That(System.Convert.ToInt32(
                state.GetLkModifiedProgress(7, 2)["idx"]), Is.EqualTo(20));
            Assert.That(System.Convert.ToInt32(
                state.GetMainProgress(7, 2, "N")["since_lk"]), Is.Zero);
            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void GetLevelEntry_HardMainRelaxesLkModifiedRankAfterInvalidEntry()
        {
            Dictionary<string, object> lkEntry = Entry(7, 4, string.Empty, 5);
            lkEntry["id"] = 500;
            LevelBankIO.LoadOverride = filename => filename switch
            {
                "bankData7x7.json" => RankedBank(5,
                    InvalidEntry(7, 5, "N"),
                    Entry(7, 5, "N")),
                "bankDataLKModified.json" => WrappedBank(lkEntry),
                _ => null
            };
            var data = new GameStateData { CurrentStrategy = 5 };
            data.MainBankProgress[GameStateService.ProgressKey(7, 5, "N")] =
                new Dictionary<string, object>
                {
                    { "idx", 0 }, { "since_lk", 4 }, { "transform", 0 }
                };
            var state = new GameStateService(data, new StrategyStore());

            LevelEntry selected = LevelData.GetLevelEntry(110, 5, 7, state);

            Assert.That(selected.BankSourceMain, Is.EqualTo("lk_mod"));
            Assert.That(selected.Id, Is.EqualTo(500));
            Assert.That(selected.Rank, Is.EqualTo(4));
            Assert.That(selected.MaxRank, Is.EqualTo(5));
            Assert.That(selected.BankRank, Is.EqualTo(5));
            Assert.That(System.Convert.ToInt32(
                state.GetMainProgress(7, 5, "N")["idx"]), Is.EqualTo(1));
            Assert.That(System.Convert.ToInt32(
                state.GetLkModifiedProgress(7, 5)["idx"]), Is.EqualTo(1));
        }

        [Test]
        public void GetLevelEntry_MainTenRankThreeExcludesRegularPool()
        {
            LevelBankIO.LoadOverride = filename => filename switch
            {
                "bankData10x10.json" => RankedBank(3, Entry(10, 3, "N")),
                "bankDataLKStyle10x10.json" => RankedBank(3, Entry(10, 3, "N")),
                "bankDataLKModified.json" => WrappedBank(),
                _ => null
            };
            var state = new GameStateService(
                new GameStateData { CurrentStrategy = 3 },
                new StrategyStore());

            LevelEntry selected = LevelData.GetLevelEntry(
                51,
                currentStrategy: 3,
                overrideSize: 10,
                gameState: state,
                random: new BoundRandom(useUpper: true));

            Assert.That(selected.BankSourceMain, Is.EqualTo("lkstyle"));
            Assert.That(selected.BankRank, Is.EqualTo(3));
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
        public void GetLevelEntry_ValidOrdinaryAdvancesOnceAndCommitsOnce()
        {
            LevelBankIO.LoadOverride = filename => filename == "bankData4x4.json"
                ? RankedBank(1, Entry(4, 1, "N"), Entry(4, 1, "N"))
                : null;
            var store = new StrategyStore();
            var state = new GameStateService(new GameStateData(), store);

            LevelEntry selected = LevelData.GetLevelEntry(
                1,
                currentStrategy: 1,
                overrideSize: 4,
                gameState: state);

            Assert.That(selected.BankIndex, Is.EqualTo(1));
            Assert.That(state.GetBankIndex(4, 1, "N"), Is.EqualTo(1));
            Assert.That(store.SaveCount, Is.EqualTo(1),
                "Selection must batch progress and commit once.");
        }

        [Test]
        public void GetLevelEntry_InvalidOrdinaryIsSkippedBeforeValidEntry()
        {
            LevelBankIO.LoadOverride = filename => filename == "bankData4x4.json"
                ? RankedBank(1,
                    InvalidEntry(4, 1, "N"),
                    Entry(4, 1, "N"))
                : null;
            var store = new StrategyStore();
            var state = new GameStateService(new GameStateData(), store);

            LevelEntry selected = LevelData.GetLevelEntry(
                1,
                currentStrategy: 1,
                overrideSize: 4,
                gameState: state);

            Assert.That(selected.BankIndex, Is.EqualTo(2));
            Assert.That(
                QueendokuCore.ValidateSolutionEntry(
                    selected.RegionMap,
                    selected.Solution,
                    4),
                Is.True);
            Assert.That(state.GetBankIndex(4, 1, "N"), Is.EqualTo(2),
                "Rejected and accepted entries must each consume one index.");
            Assert.That(store.SaveCount, Is.EqualTo(1),
                "Rejected entries must not trigger intermediate disk writes.");
        }

        [Test]
        public void GetLevelEntry_AllInvalidOrdinaryReturnsLastWithoutAdvancingIt()
        {
            LevelBankIO.LoadOverride = filename => filename == "bankData4x4.json"
                ? RankedBank(1,
                    InvalidEntry(4, 1, "N"),
                    InvalidEntry(4, 1, "N"))
                : null;
            var store = new StrategyStore();
            var state = new GameStateService(new GameStateData(), store);

            LevelEntry selected = LevelData.GetLevelEntry(
                1,
                currentStrategy: 1,
                overrideSize: 4,
                gameState: state);

            Assert.That(selected.BankIndex, Is.EqualTo(2));
            Assert.That(
                QueendokuCore.ValidateSolutionEntry(
                    selected.RegionMap,
                    selected.Solution,
                    4),
                Is.False);
            Assert.That(state.GetBankIndex(4, 1, "N"), Is.EqualTo(1),
                "Source fallback does not advance the final invalid entry.");
            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void GetLevelEntry_InvalidMainEntryAdvancesThenCommitsAcceptedEntry()
        {
            LevelBankIO.LoadOverride = filename => filename switch
            {
                "bankData7x7.json" => RankedBank(2,
                    InvalidEntry(7, 2, "N"),
                    Entry(7, 2, "N")),
                "bankDataLKModified.json" => WrappedBank(),
                _ => null
            };
            var store = new StrategyStore();
            var state = new GameStateService(
                new GameStateData { CurrentStrategy = 2 },
                store);

            LevelEntry selected = LevelData.GetLevelEntry(
                51,
                currentStrategy: 2,
                overrideSize: 7,
                gameState: state);

            Dictionary<string, object> progress =
                state.GetMainProgress(7, 2, "N");
            Assert.That(selected.BankSourceMain, Is.EqualTo("regular"));
            Assert.That(selected.BankIndex, Is.EqualTo(2));
            Assert.That(System.Convert.ToInt32(progress["idx"]), Is.EqualTo(2));
            Assert.That(System.Convert.ToInt32(progress["since_lk"]), Is.EqualTo(1),
                "Invalid skip changes idx only; accepted ordinary increments since_lk.");
            Assert.That(System.Convert.ToInt32(progress["transform"]), Is.Zero);
            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void GetLevelEntry_SpecialDoesNotMutateSequentialProgress()
        {
            LevelBankIO.LoadOverride = BuildSequenceBank;
            var store = new StrategyStore();
            var state = new GameStateService(new GameStateData(), store);

            LevelEntry selected = LevelData.GetLevelEntry(
                10,
                gameState: state);

            Assert.That(selected.BankSource, Is.EqualTo("sp"));
            Assert.That(selected.BankIndex, Is.EqualTo(44));
            Assert.That(state.Data.BankProgress, Is.Empty);
            Assert.That(state.Data.MainBankProgress, Is.Empty);
            Assert.That(store.SaveCount, Is.Zero);
        }

        [Test]
        public void AdvanceForEntry_DedupSkipAdvancesOrdinaryASecondTime()
        {
            LevelBankIO.LoadOverride = filename => filename == "bankData4x4.json"
                ? RankedBank(1,
                    Entry(4, 1, "N"),
                    Entry(4, 1, "N"),
                    Entry(4, 1, "N"))
                : null;
            var store = new StrategyStore();
            var state = new GameStateService(new GameStateData(), store);
            LevelEntry first = LevelData.GetLevelEntry(1, 1, 4, state);

            LevelData.AdvanceForEntry(first, 4, state);
            LevelEntry retried = LevelData.GetLevelEntry(1, 1, 4, state);

            Assert.That(first.BankIndex, Is.EqualTo(1));
            Assert.That(retried.BankIndex, Is.EqualTo(3),
                "Dedup skips one additional entry before its single retry.");
            Assert.That(state.GetBankIndex(4, 1, "N"), Is.EqualTo(3));
            Assert.That(store.SaveCount, Is.EqualTo(3),
                "First select, persistent dedup advance and retry each save once.");
        }

        [Test]
        public void AdvanceForEntry_DedupSkipUsesMainOrdinaryProgressBranch()
        {
            LevelBankIO.LoadOverride = filename => filename switch
            {
                "bankData7x7.json" => RankedBank(2,
                    Entry(7, 2, "N"),
                    Entry(7, 2, "N"),
                    Entry(7, 2, "N")),
                "bankDataLKModified.json" => WrappedBank(),
                _ => null
            };
            var store = new StrategyStore();
            var state = new GameStateService(
                new GameStateData { CurrentStrategy = 2 },
                store);
            LevelEntry first = LevelData.GetLevelEntry(51, 2, 7, state);

            LevelData.AdvanceForEntry(first, 7, state);
            Dictionary<string, object> progress =
                state.GetMainProgress(7, 2, "N");

            Assert.That(System.Convert.ToInt32(progress["idx"]), Is.EqualTo(2));
            Assert.That(System.Convert.ToInt32(progress["since_lk"]), Is.EqualTo(2));
            Assert.That(store.SaveCount, Is.EqualTo(2));
        }

        [Test]
        public void AdvanceForEntry_DedupSkipUsesSourceLkModifiedSaveOrdering()
        {
            LevelEntry entry = LevelEntry.FromDictionary(Entry(7, 2, string.Empty, 2));
            entry.BankRank = 2;
            entry.BankTier = "N";
            entry.BankSourceMain = "lk_mod";
            var data = new GameStateData();
            data.MainBankProgress[GameStateService.ProgressKey(7, 2, "N")] =
                new Dictionary<string, object>
                {
                    { "idx", 4 }, { "since_lk", 4 }, { "transform", 3 }
                };
            var store = new StrategyStore();
            var state = new GameStateService(data, store);

            LevelData.AdvanceForEntry(entry, 7, state);

            Assert.That(System.Convert.ToInt32(
                state.GetLkModifiedProgress(7, 2)["idx"]), Is.EqualTo(1));
            Assert.That(System.Convert.ToInt32(
                state.GetMainProgress(7, 2, "N")["since_lk"]), Is.Zero);
            Assert.That(store.SaveCount, Is.EqualTo(2),
                "Source persists LK progress and Main since_lk separately.");
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

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        public void ApplyTransform_AllEightVariantsMatchSourceRegionMapAndSolution(
            int transform)
        {
            int[][] regionMap =
            {
                new[] { 0, 1, 2, 3 },
                new[] { 4, 5, 6, 7 },
                new[] { 8, 9, 10, 11 },
                new[] { 12, 13, 14, 15 }
            };
            int[] solution = { 0, 2, 3, 1 };
            int[][][] expectedMaps =
            {
                new[]
                {
                    new[] { 0, 1, 2, 3 }, new[] { 4, 5, 6, 7 },
                    new[] { 8, 9, 10, 11 }, new[] { 12, 13, 14, 15 }
                },
                new[]
                {
                    new[] { 12, 8, 4, 0 }, new[] { 13, 9, 5, 1 },
                    new[] { 14, 10, 6, 2 }, new[] { 15, 11, 7, 3 }
                },
                new[]
                {
                    new[] { 15, 14, 13, 12 }, new[] { 11, 10, 9, 8 },
                    new[] { 7, 6, 5, 4 }, new[] { 3, 2, 1, 0 }
                },
                new[]
                {
                    new[] { 3, 7, 11, 15 }, new[] { 2, 6, 10, 14 },
                    new[] { 1, 5, 9, 13 }, new[] { 0, 4, 8, 12 }
                },
                new[]
                {
                    new[] { 3, 2, 1, 0 }, new[] { 7, 6, 5, 4 },
                    new[] { 11, 10, 9, 8 }, new[] { 15, 14, 13, 12 }
                },
                new[]
                {
                    new[] { 15, 11, 7, 3 }, new[] { 14, 10, 6, 2 },
                    new[] { 13, 9, 5, 1 }, new[] { 12, 8, 4, 0 }
                },
                new[]
                {
                    new[] { 12, 13, 14, 15 }, new[] { 8, 9, 10, 11 },
                    new[] { 4, 5, 6, 7 }, new[] { 0, 1, 2, 3 }
                },
                new[]
                {
                    new[] { 0, 4, 8, 12 }, new[] { 1, 5, 9, 13 },
                    new[] { 2, 6, 10, 14 }, new[] { 3, 7, 11, 15 }
                }
            };
            int[][] expectedSolutions =
            {
                new[] { 0, 2, 3, 1 },
                new[] { 3, 0, 2, 1 },
                new[] { 2, 0, 1, 3 },
                new[] { 2, 1, 3, 0 },
                new[] { 3, 1, 0, 2 },
                new[] { 1, 2, 0, 3 },
                new[] { 1, 3, 2, 0 },
                new[] { 0, 3, 1, 2 }
            };

            (int[][] transformedMap, int[] transformedSolution) =
                LevelData.ApplyTransform(regionMap, solution, 4, transform);

            for (int row = 0; row < 4; row++)
                Assert.That(transformedMap[row], Is.EqualTo(expectedMaps[transform][row]),
                    $"region row {row}, transform {transform}");
            Assert.That(transformedSolution, Is.EqualTo(expectedSolutions[transform]),
                $"solution, transform {transform}");
            Assert.That(regionMap[0], Is.EqualTo(new[] { 0, 1, 2, 3 }),
                "Transform must not mutate the cached bank entry.");
            Assert.That(solution, Is.EqualTo(new[] { 0, 2, 3, 1 }),
                "Transform must not mutate the cached bank solution.");
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
        public void LevelGenerator_RgbAndLabMatchSourceDistanceFixtures()
        {
            int[][] palette =
            {
                new[] { 0, 0, 0 },
                new[] { 255, 255, 255 },
                new[] { 255, 0, 0 },
                new[] { 0, 0, 255 }
            };
            int[][] regions = VerticalStripeRegions(4);

            Assert.That(
                LevelGenerator.ComputeColorMapForRgb(4, regions, palette),
                Is.EqualTo(new[] { 0, 2, 3, 1 }));
            Assert.That(
                LevelGenerator.ComputeColorMapForLab(4, regions, palette),
                Is.EqualTo(new[] { 0, 3, 2, 1 }));
        }

        [Test]
        public void LevelGenerator_RgbAndLabPatternMatchSourceDarkPoolFixtures()
        {
            int[][] palette =
            {
                new[] { 0, 0, 0 },
                new[] { 255, 255, 255 },
                new[] { 255, 0, 0 },
                new[] { 0, 0, 255 }
            };
            int[][] regions = VerticalStripeRegions(4);
            int[] patternRegions = { 1, 2 };

            Assert.That(
                LevelGenerator.ComputeColorMapForRgbWithPattern(
                    4, regions, palette, patternRegions),
                Is.EqualTo(new[] { 1, 0, 3, 2 }));
            Assert.That(
                LevelGenerator.ComputeColorMapForLabWithPattern(
                    4, regions, palette, patternRegions),
                Is.EqualTo(new[] { 2, 0, 3, 1 }));
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

        [TestCase(0, 4, 12, 230, 168, 193, 171, 109, 70)]
        [TestCase(1, 4, 12, 203, 203, 36, 170, 113, 70)]
        [TestCase(2, 4, 12, 205, 164, 0, 168, 109, 74)]
        [TestCase(3, 4, 12, 201, 179, 91, 176, 121, 89)]
        [TestCase(4, 4, 12, 205, 164, 0, 168, 109, 74)]
        [TestCase(5, 4, 12, 172, 113, 71, 137, 196, 230)]
        [TestCase(6, 4, 12, 201, 167, 121, 219, 188, 72)]
        [TestCase(7, 4, 12, 182, 124, 84, 228, 105, 156)]
        [TestCase(8, 4, 12, 211, 213, 81, 166, 123, 99)]
        [TestCase(9, 4, 12, 220, 158, 124, 220, 91, 106)]
        [TestCase(10, 10, 10, 248, 155, 229, 42, 140, 83)]
        [TestCase(11, 8, 8, 137, 121, 218, 205, 164, 0)]
        [TestCase(12, 7, 7, 248, 155, 229, 56, 169, 192)]
        public void RegionColorPipeline_PaletteOrderMatchesSourceFixtures(
            int value,
            int size,
            int expectedCount,
            int firstRed,
            int firstGreen,
            int firstBlue,
            int lastRed,
            int lastGreen,
            int lastBlue)
        {
            int[][] palette = RegionColorPipeline.PaletteFor(value, size);

            Assert.That(palette, Has.Length.EqualTo(expectedCount));
            Assert.That(palette[0],
                Is.EqualTo(new[] { firstRed, firstGreen, firstBlue }));
            Assert.That(palette[palette.Length - 1],
                Is.EqualTo(new[] { lastRed, lastGreen, lastBlue }));
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

        private static Dictionary<string, object> InvalidEntry(
            int size,
            int rank,
            string tier)
        {
            Dictionary<string, object> result = Entry(size, rank, tier);
            var invalid = new List<object>();
            for (int row = 0; row < size; row++) invalid.Add(0);
            result["solution"] = invalid;
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

        private static int ExpectedControlSize(int level)
        {
            int[] first10 = { 4, 4, 6, 6, 8, 6, 6, 8, 8, 7 };
            int[] second10 = { 6, 6, 8, 8, 10, 8, 9, 10, 9, 8 };
            int[] level21To50 = { 8, 9, 10, 9, 10, 8, 9, 10, 9, 10 };
            int[] level51Plus = { 8, 10, 10, 9, 10, 10, 9, 10, 10, 10 };
            if (level <= 10) return first10[level - 1];
            if (level <= 20) return second10[level - 11];
            if (level <= 50) return level21To50[(level - 21) % 10];
            return level51Plus[(level - 51) % 10];
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
