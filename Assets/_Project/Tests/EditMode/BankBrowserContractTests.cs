using System.Collections.Generic;
using System.Linq;
using Meowdoku.Core;
using Meowdoku.Core.UI;
using Meowdoku.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Meowdoku.Tests.EditMode
{
    public sealed class BankBrowserContractTests
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

        [Test]
        public void LevelEntry_PreservesEveryScalarFieldUsedByBankPage()
        {
            LevelEntry entry = Entry(new Dictionary<string, object>
            {
                ["id"] = 151,
                ["seed"] = 42,
                ["size"] = 8,
                ["r"] = 4,
                ["maxR"] = 5,
                ["steps"] = 25,
                ["r1"] = 8,
                ["r2"] = 3,
                ["r3"] = 1,
                ["r4"] = 13,
                ["r5"] = 0,
                ["transform"] = 6,
                ["seq"] = 2,
                ["date"] = "2024/09/28",
                ["label"] = "R4 Hard",
                ["tier"] = "H",
                ["pattern"] = "123"
            });

            Assert.That(entry.Id, Is.EqualTo(151));
            Assert.That(entry.Seed, Is.EqualTo(42));
            Assert.That(entry.Size, Is.EqualTo(8));
            Assert.That(entry.Rank, Is.EqualTo(4));
            Assert.That(entry.MaxRank, Is.EqualTo(5));
            Assert.That(entry.Steps, Is.EqualTo(25));
            Assert.That(new[]
            {
                entry.R1Steps,
                entry.R2Steps,
                entry.R3Steps,
                entry.R4Steps,
                entry.R5Steps
            }, Is.EqualTo(new[] { 8, 3, 1, 13, 0 }));
            Assert.That(entry.SourceTransform, Is.EqualTo(6));
            Assert.That(entry.Sequence, Is.EqualTo(2));
            Assert.That(entry.Date, Is.EqualTo("2024/09/28"));
            Assert.That(entry.Label, Is.EqualTo("R4 Hard"));
            Assert.That(entry.Tier, Is.EqualTo("H"));
            Assert.That(entry.Pattern, Is.EqualTo("123"));

            LevelEntry clone = entry.Clone();
            Assert.That(clone.Id, Is.EqualTo(entry.Id));
            Assert.That(clone.R4Steps, Is.EqualTo(entry.R4Steps));
            Assert.That(clone.Date, Is.EqualTo(entry.Date));
            Assert.That(clone.SourceTransform,
                Is.EqualTo(entry.SourceTransform));
        }

        [Test]
        public void ResolveInitial_UsesSourcePriorityAndDefaultSize()
        {
            var all = new Dictionary<string, object>
            {
                ["go_lk_style"] = true,
                ["go_lk"] = true,
                ["go_regular"] = true,
                ["sz"] = 10
            };

            BankBrowserState state = BankBrowserContract.ResolveInitial(all);
            Assert.That(state.Pool, Is.EqualTo(BankPoolKind.LkStyle));
            Assert.That(state.Panel, Is.EqualTo(BankBrowserPanel.Tier));
            Assert.That(state.Size, Is.EqualTo(10));

            state = BankBrowserContract.ResolveInitial(null);
            Assert.That(state.Pool, Is.EqualTo(BankPoolKind.None));
            Assert.That(state.Panel, Is.EqualTo(BankBrowserPanel.Root));
            Assert.That(state.Size, Is.EqualTo(7));
        }

        [Test]
        public void PanelBack_MatchesEachSourcePanelBranch()
        {
            AssertBack(
                new BankBrowserState(
                    BankPoolKind.Regular,
                    BankBrowserPanel.Tier,
                    8),
                BankPoolKind.Regular,
                BankBrowserPanel.RegularSize);
            AssertBack(
                new BankBrowserState(
                    BankPoolKind.LkStyle,
                    BankBrowserPanel.Tier,
                    9),
                BankPoolKind.LkStyle,
                BankBrowserPanel.VariantSize);
            AssertBack(
                new BankBrowserState(
                    BankPoolKind.Gc,
                    BankBrowserPanel.Tier,
                    10),
                BankPoolKind.None,
                BankBrowserPanel.Root);
            AssertBack(
                new BankBrowserState(
                    BankPoolKind.Special,
                    BankBrowserPanel.LevelList),
                BankPoolKind.None,
                BankBrowserPanel.Root);
            AssertBack(
                new BankBrowserState(
                    BankPoolKind.Gc,
                    BankBrowserPanel.LevelList,
                    11,
                    4,
                    "H"),
                BankPoolKind.Gc,
                BankBrowserPanel.Tier);
            AssertBack(
                new BankBrowserState(
                    BankPoolKind.LkModified,
                    BankBrowserPanel.LkList),
                BankPoolKind.None,
                BankBrowserPanel.Root);
        }

        [Test]
        public void TierBuckets_SplitSourceHardTierFromNormalTier()
        {
            LevelBankIO.LoadOverride = filename => filename ==
                "bankData7x7.json"
                ? new Dictionary<string, object>
                {
                    ["4"] = new List<object>
                    {
                        EntryDictionary(7, 4, "N", seed: 10),
                        EntryDictionary(7, 4, "H", seed: 11)
                    }
                }
                : null;

            IReadOnlyList<BankTierBucket> buckets =
                BankBrowserContract.GetTierBuckets(
                    BankPoolKind.Regular, 7);

            Assert.That(buckets.Count, Is.EqualTo(2));
            Assert.That(buckets[0].Rank, Is.EqualTo(4));
            Assert.That(buckets[0].Tier, Is.EqualTo("N"));
            Assert.That(buckets[0].Count, Is.EqualTo(1));
            Assert.That(buckets[0].IsHardTier, Is.False);
            Assert.That(buckets[1].Tier, Is.EqualTo("H"));
            Assert.That(buckets[1].Definition.Label,
                Is.EqualTo("R4H Hard+"));
            Assert.That(buckets[1].Count, Is.EqualTo(1));
            Assert.That(buckets[1].IsHardTier, Is.True);
        }

        [Test]
        public void RegularLaunch_ContainsExactSourceParameterShape()
        {
            LevelEntry entry = Entry(EntryDictionary(
                7, 4, "H", seed: 99));
            Assert.That(BankBrowserContract.TryCreateLaunch(
                BankPoolKind.Regular,
                new[] { entry },
                0,
                7,
                4,
                "H",
                out BankLaunchRequest request), Is.True);

            CollectionAssert.AreEquivalent(new[]
            {
                "from_bank_browser", "bank_mode", "bank_size",
                "bank_rank", "bank_index", "bank_total",
                "prebuilt_regions", "prebuilt_solution", "level_seed",
                "r1_steps", "r2_steps", "r3_steps", "r4_steps",
                "r5_steps", "bank_lk_style", "bank_gc",
                "bank_tier_h", "bank_tier"
            }, request.Parameters.Keys);
            Assert.That(request.Parameters["level_seed"], Is.EqualTo(99));
            Assert.That(request.Parameters["r4_steps"], Is.EqualTo(13));
            Assert.That(request.Parameters["bank_tier_h"], Is.True);
            Assert.That(request.Parameters["bank_tier"], Is.EqualTo("H"));
            Assert.That(request.Parameters.ContainsKey("bank_lk"), Is.False);
            Assert.That(request.Parameters.ContainsKey("bank_sp"), Is.False);
            Assert.That(request.Parameters["prebuilt_regions"],
                Is.Not.SameAs(entry.RegionMap));
            Assert.That(request.Parameters["prebuilt_solution"],
                Is.Not.SameAs(entry.Solution));
        }

        [Test]
        public void LkLaunch_UsesIdAndMaxRankWithoutStrategyFields()
        {
            LevelEntry entry = Entry(new Dictionary<string, object>
            {
                ["id"] = 151,
                ["size"] = 8,
                ["maxR"] = 4,
                ["date"] = "2024/09/28",
                ["label"] = "R4 Hard"
            });

            Assert.That(BankBrowserContract.TryCreateLaunch(
                BankPoolKind.LkModified,
                new[] { entry },
                0,
                99,
                1,
                string.Empty,
                out BankLaunchRequest request), Is.True);

            Assert.That(request.Parameters["bank_size"], Is.EqualTo(8));
            Assert.That(request.Parameters["bank_rank"], Is.EqualTo(4));
            Assert.That(request.Parameters["level_seed"], Is.EqualTo(151));
            Assert.That(request.Parameters["bank_lk"], Is.True);
            Assert.That(request.Parameters["bank_lk_modified"], Is.True);
            Assert.That(request.Parameters.ContainsKey("r1_steps"), Is.False);
            Assert.That(request.Parameters.ContainsKey("bank_lk_style"),
                Is.False);
        }

        [Test]
        public void SpecialLaunch_UsesEntrySizeRankIdAndColorMap()
        {
            LevelEntry entry = Entry(new Dictionary<string, object>
            {
                ["id"] = 7,
                ["size"] = 9,
                ["r"] = 4,
                ["pattern"] = "123",
                ["colorMap"] = new[] { 1, 2, 3 },
                ["r1"] = 9,
                ["r2"] = 5,
                ["r3"] = 2,
                ["r4"] = 2,
                ["r5"] = 0
            });

            Assert.That(BankBrowserContract.TryCreateLaunch(
                BankPoolKind.Special,
                new[] { entry },
                0,
                4,
                1,
                string.Empty,
                out BankLaunchRequest request), Is.True);

            Assert.That(request.Parameters["bank_size"], Is.EqualTo(9));
            Assert.That(request.Parameters["bank_rank"], Is.EqualTo(4));
            Assert.That(request.Parameters["level_seed"], Is.EqualTo(7));
            Assert.That(request.Parameters["bank_sp"], Is.True);
            Assert.That(request.Parameters["bank_lk_style"], Is.False);
            Assert.That(request.Parameters["custom_color_map"],
                Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(request.Parameters.ContainsKey("bank_gc"), Is.False);
        }

        [Test]
        public void CreateLaunch_RejectsInvalidIndex()
        {
            Assert.That(BankBrowserContract.TryCreateLaunch(
                BankPoolKind.Regular,
                new[] { Entry(EntryDictionary(4, 1, string.Empty, 1)) },
                1,
                4,
                1,
                string.Empty,
                out BankLaunchRequest request), Is.False);
            Assert.That(request, Is.Null);
        }

        [Test]
        public void HardTierKeys_MatchSourceListExactly()
        {
            var actual = new List<string>();
            for (int size = 4; size <= 12; size++)
            for (int rank = 1; rank <= 5; rank++)
            {
                if (BankBrowserContract.HasHardTier(size, rank))
                    actual.Add($"{size}:{rank}");
            }

            Assert.That(actual, Is.EqualTo(new[]
            {
                "7:4", "8:4", "8:5", "9:4", "9:5",
                "10:4", "10:5", "11:4", "11:5", "12:4"
            }));
        }

        [Test]
        public void InstalledPrefab_HasSourcePanelBranchesAndNoMissingScripts()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/BankPage.prefab");
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<BankBrowserPagePresenter>(),
                Is.Not.Null);
            Assert.That(prefab.transform.Find("RootPanel"), Is.Not.Null);
            Assert.That(prefab.transform.Find("RegularSizePanel"), Is.Not.Null);
            Assert.That(prefab.transform.Find("TierPanel"), Is.Not.Null);
            Assert.That(prefab.transform.Find("ListPanel"), Is.Not.Null);
            Assert.That(prefab.transform.Find("LKPanel"), Is.Not.Null);
            Assert.That(prefab.transform.Find("VariantSizePanel"), Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<BankRootCardView>(true)
                .Length, Is.EqualTo(6));

            Component[] components =
                prefab.GetComponentsInChildren<Component>(true);
            Assert.That(components, Has.None.Null);
        }

        private static void AssertBack(
            BankBrowserState source,
            BankPoolKind expectedPool,
            BankBrowserPanel expectedPanel)
        {
            BankBrowserState result = BankBrowserContract.PanelBack(source);
            Assert.That(result.Pool, Is.EqualTo(expectedPool));
            Assert.That(result.Panel, Is.EqualTo(expectedPanel));
        }

        private static LevelEntry Entry(Dictionary<string, object> values)
        {
            values ??= new Dictionary<string, object>();
            if (!values.ContainsKey("regionMap"))
                values["regionMap"] = new[] { new[] { 0 } };
            if (!values.ContainsKey("solution"))
                values["solution"] = new[] { 0 };
            return LevelEntry.FromDictionary(values);
        }

        private static Dictionary<string, object> EntryDictionary(
            int size,
            int rank,
            string tier,
            int seed)
        {
            int[][] regions = Enumerable.Range(0, size)
                .Select(row => Enumerable.Repeat(row, size).ToArray())
                .ToArray();
            return new Dictionary<string, object>
            {
                ["seed"] = seed,
                ["size"] = size,
                ["r"] = rank,
                ["tier"] = tier,
                ["steps"] = 25,
                ["r1"] = 8,
                ["r2"] = 3,
                ["r3"] = 1,
                ["r4"] = 13,
                ["r5"] = 0,
                ["regionMap"] = regions,
                ["solution"] = Enumerable.Range(0, size).ToArray()
            };
        }
    }
}
