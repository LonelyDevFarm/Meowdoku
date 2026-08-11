using System;
using System.Collections.Generic;
using Meowdoku.Core.Config;

namespace Meowdoku.Core.Daily
{
    public enum DailyPuzzlePool
    {
        Regular,
        Gc,
        LkStyle
    }

    public sealed class DailyPuzzleSelection
    {
        public LevelEntry Entry { get; internal set; }
        public int Size { get; internal set; }
        public int Rank { get; internal set; }
        public string Tier { get; internal set; } = "N";
        public DailyPuzzlePool Pool { get; internal set; }
        public string BankSource { get; internal set; } = "regular";
        public int DailyIndex { get; internal set; }
        public int Transform { get; internal set; }
        public int EntryIndex { get; internal set; }
        public int BankIndex => EntryIndex + 1;
        public long Seed { get; internal set; }
        public bool IsSolutionValid { get; internal set; }
        public int[] StrategySteps { get; internal set; } = new int[5];
    }

    public sealed class DailyGameLaunchRequest
    {
        public string Date { get; internal set; } = string.Empty;
        public DailyPuzzleSelection Selection { get; internal set; }
        public IReadOnlyDictionary<string, object> Parameters { get; internal set; }
    }

    public readonly struct DailyPuzzlePoolPlan
    {
        public DailyPuzzlePoolPlan(
            int size,
            int rank,
            string tier,
            DailyPuzzlePool pool)
        {
            Size = size;
            Rank = rank;
            Tier = tier;
            Pool = pool;
        }

        public int Size { get; }
        public int Rank { get; }
        public string Tier { get; }
        public DailyPuzzlePool Pool { get; }
    }

    /// <summary>
    /// Deterministic port of DailyGamePage's date-to-bank selection. The same
    /// bank entry is reused for eight rotate/mirror variants before the cycle
    /// repeats.
    /// </summary>
    public static class DailyPuzzleSelector
    {
        private const int TransformCount = 8;
        private static readonly DateTime Epoch = new DateTime(2026, 4, 21);

        public static DailyGameLaunchRequest CreateLaunch(
            int currentLevel,
            DateTime localDate,
            DcLevelConfig config = null)
        {
            DailyPuzzleSelection selection = Select(
                currentLevel,
                DayOffset(localDate),
                config);
            return selection == null
                ? null
                : CreateLaunch(selection, DailyEntryStateContract.DateKey(localDate));
        }

        public static DailyGameLaunchRequest CreateLaunch(
            DailyPuzzleSelection selection,
            string date)
        {
            if (selection?.Entry == null) return null;
            LevelEntry entry = selection.Entry;
            int[] steps = selection.StrategySteps ?? Array.Empty<int>();
            var parameters = new Dictionary<string, object>
            {
                { "daily_mode", true },
                { "is_daily", true },
                { "daily_date", date ?? string.Empty },
                { "daily_index", selection.DailyIndex },
                { "daily_transform", selection.Transform },
                { "size", selection.Size },
                { "rank", selection.Rank },
                { "tier", selection.Tier ?? "N" },
                { "seed", selection.Seed },
                { "bank_source", selection.BankSource ?? "regular" },
                { "bank_idx", selection.BankIndex },
                { "bank_transform", selection.Transform },
                { "bank_size", selection.Size },
                { "bank_rank", selection.Rank },
                { "bank_index", selection.BankIndex },
                { "bank_tier", selection.Tier ?? "N" },
                { "level_seed", selection.Seed },
                { "prebuilt_regions", ToRows(entry.RegionMap) },
                { "prebuilt_solution", ToValues(entry.Solution) },
                { "custom_color_map", ToValues(entry.ColorMap) },
                { "prefill_positions", new List<object>() },
                { "r1_steps", Step(steps, 0) },
                { "r2_steps", Step(steps, 1) },
                { "r3_steps", Step(steps, 2) },
                { "r4_steps", Step(steps, 3) },
                { "r5_steps", Step(steps, 4) }
            };
            return new DailyGameLaunchRequest
            {
                Date = date ?? string.Empty,
                Selection = selection,
                Parameters = parameters
            };
        }

        public static int DayOffset(DateTime localDate)
        {
            int offset = JulianDay(localDate.Year, localDate.Month, localDate.Day) -
                         JulianDay(Epoch.Year, Epoch.Month, Epoch.Day);
            return Math.Max(0, offset);
        }

        public static DailyPuzzleSelection Select(
            int currentLevel,
            int dayOffset,
            DcLevelConfig config = null)
        {
            DailyPuzzlePoolPlan plan = ResolvePool(
                currentLevel,
                dayOffset,
                config);
            IReadOnlyList<LevelEntry> pool;
            switch (plan.Pool)
            {
                case DailyPuzzlePool.Gc:
                    pool = BankData.GetGcLevels(plan.Size, plan.Rank);
                    break;
                case DailyPuzzlePool.LkStyle:
                    pool = BankData.GetLkStyleLevelsByTier(
                        plan.Size,
                        plan.Rank,
                        plan.Tier);
                    break;
                default:
                    pool = BankData.GetLevels(plan.Size, plan.Rank);
                    break;
            }

            return SelectFromPool(
                pool,
                plan.Size,
                plan.Rank,
                plan.Tier,
                plan.Pool,
                dayOffset);
        }

        public static DailyPuzzlePoolPlan ResolvePool(
            int currentLevel,
            int dayOffset,
            DcLevelConfig config = null)
        {
            config ??= new DcLevelConfig();
            if (config.IsOverrideEnabled())
            {
                int size = config.GetPoolSize(currentLevel, dayOffset);
                int rank = config.GetPoolRank(currentLevel, dayOffset);
                DailyPuzzlePool pool = config.UseGcBank(size, dayOffset)
                    ? DailyPuzzlePool.Gc
                    : DailyPuzzlePool.Regular;
                return new DailyPuzzlePoolPlan(size, rank, "N", pool);
            }

            if (currentLevel <= 100)
                return new DailyPuzzlePoolPlan(
                    10,
                    3,
                    "N",
                    DailyPuzzlePool.Regular);
            if (currentLevel <= 200)
                return new DailyPuzzlePoolPlan(
                    10,
                    4,
                    "N",
                    DailyPuzzlePool.Regular);
            return new DailyPuzzlePoolPlan(
                12,
                4,
                "N",
                DailyPuzzlePool.LkStyle);
        }

        public static DailyPuzzleSelection SelectFromPool(
            IReadOnlyList<LevelEntry> pool,
            int size,
            int rank,
            string tier,
            DailyPuzzlePool poolKind,
            int dayOffset)
        {
            if (pool == null || pool.Count == 0) return null;
            int totalVirtual = pool.Count * TransformCount;
            int virtualIndex = PositiveModulo(dayOffset, totalVirtual);
            DailyPuzzleSelection fallback = null;

            for (int attempts = 0; attempts < totalVirtual; attempts++)
            {
                int transform = virtualIndex / pool.Count;
                int entryIndex = virtualIndex % pool.Count;
                LevelEntry source = pool[entryIndex];
                if (source != null &&
                    source.RegionMap != null &&
                    source.Solution != null)
                {
                    (int[][] regions, int[] solution) = LevelData.ApplyTransform(
                        source.RegionMap,
                        source.Solution,
                        size,
                        transform);
                    bool valid = QueendokuCore.ValidateSolutionEntry(
                        regions,
                        solution,
                        size);
                    fallback = Build(
                        source,
                        regions,
                        solution,
                        size,
                        rank,
                        tier,
                        poolKind,
                        virtualIndex,
                        transform,
                        entryIndex,
                        valid);
                    if (valid) return fallback;
                }
                virtualIndex = (virtualIndex + 1) % totalVirtual;
            }

            // Source deliberately continues with the final attempted entry if
            // a malformed bank somehow contains no valid board.
            return fallback;
        }

        private static DailyPuzzleSelection Build(
            LevelEntry source,
            int[][] regions,
            int[] solution,
            int size,
            int rank,
            string tier,
            DailyPuzzlePool pool,
            int virtualIndex,
            int transform,
            int entryIndex,
            bool valid)
        {
            string bankSource = pool switch
            {
                DailyPuzzlePool.Gc => "gc",
                DailyPuzzlePool.LkStyle => "lkstyle",
                _ => "regular"
            };
            LevelEntry entry = source.CloneWithBoard(regions, solution);
            entry.BankSource = bankSource;
            entry.BankSourceMain = bankSource;
            entry.BankIndex = entryIndex + 1;
            entry.BankRank = rank;
            entry.BankTier = tier ?? "N";
            entry.BankTransform = transform;

            return new DailyPuzzleSelection
            {
                Entry = entry,
                Size = size,
                Rank = rank,
                Tier = tier ?? "N",
                Pool = pool,
                BankSource = bankSource,
                DailyIndex = virtualIndex,
                Transform = transform,
                EntryIndex = entryIndex,
                Seed = source.HasSeed ? source.Seed : source.Id,
                IsSolutionValid = valid,
                StrategySteps = new[]
                {
                    source.R1Steps,
                    source.R2Steps,
                    source.R3Steps,
                    source.R4Steps,
                    source.R5Steps
                }
            };
        }

        private static int JulianDay(int year, int month, int day)
        {
            int a = (14 - month) / 12;
            int y = year + 4800 - a;
            int m = month + 12 * a - 3;
            return day +
                   (153 * m + 2) / 5 +
                   365 * y +
                   y / 4 -
                   y / 100 +
                   y / 400 -
                   32045;
        }

        private static int PositiveModulo(int value, int modulus)
        {
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }

        private static int Step(IReadOnlyList<int> steps, int index)
        {
            return steps != null && index >= 0 && index < steps.Count
                ? steps[index]
                : 0;
        }

        private static List<object> ToValues(int[] values)
        {
            var result = new List<object>();
            if (values == null) return result;
            for (int index = 0; index < values.Length; index++)
                result.Add(values[index]);
            return result;
        }

        private static List<object> ToRows(int[][] rows)
        {
            var result = new List<object>();
            if (rows == null) return result;
            for (int index = 0; index < rows.Length; index++)
                result.Add(ToValues(rows[index]));
            return result;
        }
    }
}
