using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Meowdoku.Core.UI
{
    public enum BankPoolKind
    {
        None = 0,
        Regular = 1,
        Lk = 2,
        LkModified = 3,
        LkStyle = 4,
        Gc = 5,
        Special = 6
    }

    public enum BankBrowserPanel
    {
        Root = 0,
        RegularSize = 1,
        Tier = 2,
        LevelList = 3,
        LkList = 4,
        VariantSize = 5
    }

    public readonly struct BankBrowserState
    {
        public BankBrowserState(
            BankPoolKind pool,
            BankBrowserPanel panel,
            int size = BankBrowserContract.DefaultSize,
            int rank = 1,
            string tier = "")
        {
            Pool = pool;
            Panel = panel;
            Size = size;
            Rank = rank;
            Tier = tier ?? string.Empty;
        }

        public BankPoolKind Pool { get; }
        public BankBrowserPanel Panel { get; }
        public int Size { get; }
        public int Rank { get; }
        public string Tier { get; }
    }

    public readonly struct BankRankDefinition
    {
        public BankRankDefinition(int rank, string label, string description)
        {
            Rank = rank;
            Label = label;
            Description = description;
        }

        public int Rank { get; }
        public string Label { get; }
        public string Description { get; }
    }

    public readonly struct BankTierBucket
    {
        public BankTierBucket(
            BankRankDefinition definition,
            string tier,
            int count,
            bool isHardTier)
        {
            Definition = definition;
            Tier = tier ?? string.Empty;
            Count = count;
            IsHardTier = isHardTier;
        }

        public BankRankDefinition Definition { get; }
        public int Rank => Definition.Rank;
        public string Tier { get; }
        public int Count { get; }
        public bool IsHardTier { get; }
    }

    public sealed class BankLaunchRequest
    {
        internal BankLaunchRequest(
            BankPoolKind pool,
            int index,
            int total,
            Dictionary<string, object> parameters)
        {
            Pool = pool;
            Index = index;
            Total = total;
            Parameters = new ReadOnlyDictionary<string, object>(parameters);
        }

        public BankPoolKind Pool { get; }
        public int Index { get; }
        public int Total { get; }
        public IReadOnlyDictionary<string, object> Parameters { get; }
    }

    /// <summary>
    /// Pure page-state, filtering and Game launch contract ported from
    /// bank_page.gd. It deliberately preserves the six source bank branches
    /// rather than folding them into the normal level-selection pipeline.
    /// </summary>
    public static class BankBrowserContract
    {
        public const int DefaultSize = 7;

        private static readonly BankRankDefinition[] RankDefinitions =
        {
            new(1, "R1 Beginner", "唯一候选"),
            new(2, "R2 Easy", "区域-行列约束"),
            new(3, "R3 Medium", "集合锁定(K≤3)"),
            new(4, "R4 Hard", "高阶锁定/浅层推理"),
            new(5, "R5 Expert", "深层链式推理")
        };

        private static readonly BankRankDefinition HardRank4 =
            new(4, "R4H Hard+", "深度高阶推理");
        private static readonly BankRankDefinition HardRank5 =
            new(5, "R5H Expert+", "极深链式推理");

        private static readonly HashSet<int> HardTierKeys = new()
        {
            TierKey(7, 4), TierKey(8, 4), TierKey(9, 4),
            TierKey(10, 4), TierKey(11, 4), TierKey(12, 4),
            TierKey(8, 5), TierKey(9, 5), TierKey(10, 5),
            TierKey(11, 5)
        };

        public static IReadOnlyList<BankRankDefinition> Ranks =>
            RankDefinitions;

        public static BankBrowserState ResolveInitial(
            IReadOnlyDictionary<string, object> parameters)
        {
            int size = ReadInt(parameters, "sz", DefaultSize);
            if (ReadBool(parameters, "go_lk_style"))
                return new BankBrowserState(
                    BankPoolKind.LkStyle,
                    BankBrowserPanel.Tier,
                    size);
            if (ReadBool(parameters, "go_lk"))
                return new BankBrowserState(
                    BankPoolKind.Lk,
                    BankBrowserPanel.LkList,
                    size);
            if (ReadBool(parameters, "go_regular"))
                return new BankBrowserState(
                    BankPoolKind.Regular,
                    BankBrowserPanel.Tier,
                    size);
            return new BankBrowserState(
                BankPoolKind.None,
                BankBrowserPanel.Root,
                size);
        }

        public static BankBrowserState OpenRootPool(BankPoolKind pool)
        {
            return pool switch
            {
                BankPoolKind.Regular => new BankBrowserState(
                    pool, BankBrowserPanel.RegularSize),
                BankPoolKind.Lk or BankPoolKind.LkModified =>
                    new BankBrowserState(pool, BankBrowserPanel.LkList),
                BankPoolKind.LkStyle or BankPoolKind.Gc =>
                    new BankBrowserState(pool, BankBrowserPanel.VariantSize),
                BankPoolKind.Special => new BankBrowserState(
                    pool, BankBrowserPanel.LevelList),
                _ => new BankBrowserState(
                    BankPoolKind.None, BankBrowserPanel.Root)
            };
        }

        public static BankBrowserState OpenSize(
            BankPoolKind pool,
            int size)
        {
            if (pool != BankPoolKind.Regular &&
                pool != BankPoolKind.LkStyle &&
                pool != BankPoolKind.Gc)
                return OpenRootPool(pool);
            return new BankBrowserState(
                pool, BankBrowserPanel.Tier, size);
        }

        public static BankBrowserState OpenTier(
            BankPoolKind pool,
            int size,
            int rank,
            string tier)
        {
            return new BankBrowserState(
                pool,
                BankBrowserPanel.LevelList,
                size,
                rank,
                tier);
        }

        public static BankBrowserState PanelBack(BankBrowserState state)
        {
            switch (state.Panel)
            {
                case BankBrowserPanel.Tier:
                    if (state.Pool == BankPoolKind.Regular)
                        return new BankBrowserState(
                            state.Pool,
                            BankBrowserPanel.RegularSize,
                            state.Size);
                    if (state.Pool == BankPoolKind.LkStyle)
                        return new BankBrowserState(
                            state.Pool,
                            BankBrowserPanel.VariantSize,
                            state.Size);
                    return new BankBrowserState(
                        BankPoolKind.None,
                        BankBrowserPanel.Root,
                        state.Size);

                case BankBrowserPanel.LevelList:
                    if (state.Pool == BankPoolKind.Special)
                        return new BankBrowserState(
                            BankPoolKind.None,
                            BankBrowserPanel.Root,
                            state.Size);
                    return new BankBrowserState(
                        state.Pool,
                        BankBrowserPanel.Tier,
                        state.Size,
                        state.Rank,
                        state.Tier);

                case BankBrowserPanel.LkList:
                case BankBrowserPanel.RegularSize:
                case BankBrowserPanel.VariantSize:
                    return new BankBrowserState(
                        BankPoolKind.None,
                        BankBrowserPanel.Root,
                        state.Size);

                default:
                    return state;
            }
        }

        public static IReadOnlyList<int> GetSizes(BankPoolKind pool)
        {
            return pool switch
            {
                BankPoolKind.Regular => BankData.GetSizes(),
                BankPoolKind.LkStyle => BankData.GetLkStyleSizes(),
                BankPoolKind.Gc => BankData.GetGcSizes(),
                _ => Array.Empty<int>()
            };
        }

        public static IReadOnlyList<LevelEntry> GetLevels(
            BankPoolKind pool,
            int size = DefaultSize,
            int rank = 1,
            string tier = "")
        {
            bool filterTier = tier == "H" || tier == "N";
            return pool switch
            {
                BankPoolKind.Regular => filterTier
                    ? BankData.GetLevelsByTier(size, rank, tier)
                    : BankData.GetLevels(size, rank),
                BankPoolKind.Lk => BankData.GetLkLevels(),
                BankPoolKind.LkModified => BankData.GetLkModifiedLevels(),
                BankPoolKind.LkStyle => filterTier
                    ? BankData.GetLkStyleLevelsByTier(size, rank, tier)
                    : BankData.GetLkStyleLevels(size, rank),
                BankPoolKind.Gc => filterTier
                    ? BankData.GetGcLevelsByTier(size, rank, tier)
                    : BankData.GetGcLevels(size, rank),
                BankPoolKind.Special => BankData.GetSpecialLevels(),
                _ => Array.Empty<LevelEntry>()
            };
        }

        public static IReadOnlyList<BankTierBucket> GetTierBuckets(
            BankPoolKind pool,
            int size)
        {
            if (pool != BankPoolKind.Regular &&
                pool != BankPoolKind.LkStyle &&
                pool != BankPoolKind.Gc)
                return Array.Empty<BankTierBucket>();

            var result = new List<BankTierBucket>(7);
            foreach (BankRankDefinition definition in RankDefinitions)
            {
                int rank = definition.Rank;
                bool hasHardTier = HasHardTier(size, rank);
                string tier = string.Empty;
                int count;
                if (pool == BankPoolKind.Gc)
                {
                    bool gcHasHard = hasHardTier &&
                        GetLevels(pool, size, rank, "H").Count > 0;
                    tier = gcHasHard ? "N" : string.Empty;
                    count = GetLevels(pool, size, rank, tier).Count;
                }
                else if (hasHardTier)
                {
                    tier = "N";
                    count = GetLevels(pool, size, rank, tier).Count;
                }
                else
                {
                    count = GetLevels(pool, size, rank).Count;
                }

                if (count > 0)
                    result.Add(new BankTierBucket(
                        definition, tier, count, false));
            }

            foreach (int rank in new[] { 4, 5 })
            {
                if (!HasHardTier(size, rank)) continue;
                int count = GetLevels(pool, size, rank, "H").Count;
                if (count <= 0) continue;
                result.Add(new BankTierBucket(
                    rank == 4 ? HardRank4 : HardRank5,
                    "H",
                    count,
                    true));
            }

            return result;
        }

        public static bool TryCreateLaunch(
            BankPoolKind pool,
            int zeroBasedIndex,
            int size,
            int rank,
            string tier,
            out BankLaunchRequest request)
        {
            IReadOnlyList<LevelEntry> levels = GetLevels(
                pool, size, rank, tier);
            return TryCreateLaunch(
                pool,
                levels,
                zeroBasedIndex,
                size,
                rank,
                tier,
                out request);
        }

        public static bool TryCreateLaunch(
            BankPoolKind pool,
            IReadOnlyList<LevelEntry> levels,
            int zeroBasedIndex,
            int size,
            int rank,
            string tier,
            out BankLaunchRequest request)
        {
            request = null;
            if (levels == null || zeroBasedIndex < 0 ||
                zeroBasedIndex >= levels.Count)
                return false;

            LevelEntry entry = levels[zeroBasedIndex];
            if (entry == null) return false;
            int index = zeroBasedIndex + 1;
            var parameters = BaseParameters(
                entry,
                pool == BankPoolKind.Lk ||
                pool == BankPoolKind.LkModified ||
                pool == BankPoolKind.Special
                    ? entry.Size
                    : size,
                pool == BankPoolKind.Lk ||
                pool == BankPoolKind.LkModified
                    ? entry.MaxRank
                    : pool == BankPoolKind.Special
                        ? entry.Rank
                        : rank,
                index,
                levels.Count,
                pool == BankPoolKind.Lk ||
                pool == BankPoolKind.LkModified ||
                pool == BankPoolKind.Special
                    ? entry.Id
                    : entry.Seed);

            switch (pool)
            {
                case BankPoolKind.Lk:
                case BankPoolKind.LkModified:
                    parameters["bank_lk"] = true;
                    parameters["bank_lk_modified"] =
                        pool == BankPoolKind.LkModified;
                    break;

                case BankPoolKind.Special:
                    AddStrategySteps(parameters, entry);
                    parameters["bank_lk_style"] = false;
                    parameters["bank_sp"] = true;
                    parameters["custom_color_map"] =
                        Clone(entry.ColorMap);
                    break;

                case BankPoolKind.Regular:
                case BankPoolKind.LkStyle:
                case BankPoolKind.Gc:
                    AddStrategySteps(parameters, entry);
                    parameters["bank_lk_style"] =
                        pool == BankPoolKind.LkStyle;
                    parameters["bank_gc"] = pool == BankPoolKind.Gc;
                    parameters["bank_tier_h"] = tier == "H";
                    parameters["bank_tier"] = tier ?? string.Empty;
                    break;

                default:
                    return false;
            }

            request = new BankLaunchRequest(
                pool, index, levels.Count, parameters);
            return true;
        }

        public static bool HasHardTier(int size, int rank) =>
            HardTierKeys.Contains(TierKey(size, rank));

        public static string SizeTierLabel(int size)
        {
            return size switch
            {
                4 => "入门",
                5 => "进阶",
                6 => "挑战",
                7 => "高手",
                8 => "大师",
                9 => "宗师",
                10 => "传奇",
                _ => string.Empty
            };
        }

        private static Dictionary<string, object> BaseParameters(
            LevelEntry entry,
            int size,
            int rank,
            int index,
            int total,
            int seed)
        {
            return new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["from_bank_browser"] = true,
                ["bank_mode"] = true,
                ["bank_size"] = size,
                ["bank_rank"] = rank,
                ["bank_index"] = index,
                ["bank_total"] = total,
                ["prebuilt_regions"] = Clone(entry.RegionMap),
                ["prebuilt_solution"] = Clone(entry.Solution),
                ["level_seed"] = seed
            };
        }

        private static void AddStrategySteps(
            IDictionary<string, object> parameters,
            LevelEntry entry)
        {
            parameters["r1_steps"] = entry.R1Steps;
            parameters["r2_steps"] = entry.R2Steps;
            parameters["r3_steps"] = entry.R3Steps;
            parameters["r4_steps"] = entry.R4Steps;
            parameters["r5_steps"] = entry.R5Steps;
        }

        private static int[][] Clone(int[][] source)
        {
            if (source == null) return Array.Empty<int[]>();
            var result = new int[source.Length][];
            for (int index = 0; index < source.Length; index++)
            {
                result[index] = source[index] == null
                    ? Array.Empty<int>()
                    : (int[])source[index].Clone();
            }
            return result;
        }

        private static int[] Clone(int[] source) =>
            source == null ? Array.Empty<int>() : (int[])source.Clone();

        private static int TierKey(int size, int rank) => size * 10 + rank;

        private static bool ReadBool(
            IReadOnlyDictionary<string, object> parameters,
            string key)
        {
            if (parameters == null ||
                !parameters.TryGetValue(key, out object value) ||
                value == null)
                return false;
            return Convert.ToBoolean(value);
        }

        private static int ReadInt(
            IReadOnlyDictionary<string, object> parameters,
            string key,
            int fallback)
        {
            if (parameters == null ||
                !parameters.TryGetValue(key, out object value) ||
                value == null)
                return fallback;
            return Convert.ToInt32(value);
        }
    }
}
