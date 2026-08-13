using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Meowdoku.Core.Config;
using UnityEngine;

namespace Meowdoku.Core
{
    public interface IInclusiveRandom
    {
        int RangeInclusive(int minimum, int maximum);
    }

    public sealed class UnityInclusiveRandom : IInclusiveRandom
    {
        public static readonly UnityInclusiveRandom Instance = new UnityInclusiveRandom();
        private UnityInclusiveRandom() { }
        public int RangeInclusive(int minimum, int maximum)
        {
            return Random.Range(minimum, maximum + 1);
        }
    }

    // Chứa dữ liệu tĩnh và logic sinh cấp độ (Level).
    public static class LevelData
    {
        public const int LEVEL_COUNT = 0;
        
        // Mảng quy định kích thước bảng (Board Size) cho 100 level đầu tiên.
        public static readonly int[] SIZES = new int[] {
            4, 4, 5, 5, 6, 5, 5, 6, 6, 7, 
            6, 6, 6, 6, 7, 6, 7, 6, 7, 8, 
            6, 7, 6, 7, 8, 6, 7, 8, 7, 8, 
            6, 7, 6, 7, 8, 6, 7, 8, 7, 8, 
            6, 7, 6, 7, 8, 6, 7, 8, 7, 8, 
            6, 7, 8, 7, 9, 6, 7, 8, 9, 10, 
            6, 7, 8, 7, 9, 6, 7, 8, 9, 10, 
            6, 7, 8, 7, 9, 6, 7, 8, 9, 10, 
            6, 7, 8, 7, 9, 6, 7, 8, 9, 10, 
            6, 7, 8, 7, 9, 6, 7, 8, 9, 10
        };

        // Kích thước bảng lặp lại cho các level từ 101 trở đi.
        private static readonly int[] _SIZES_101_PLUS = new int[] { 7, 8, 7, 9, 10, 7, 8, 9, 8, 10 };

        // Trả về kích thước mảng (NxN) dựa vào cấp độ hiện tại.
        public static int GetSize(int levelNum)
        {
            if (levelNum < 1) return 0;
            if (levelNum <= 100) return SIZES[levelNum - 1];
            return _SIZES_101_PLUS[(levelNum - 101) % 10];
        }

        // Xác định xem cấp độ hiện tại có phải là cấp độ khó hay không.
        public static bool IsHardLevel(int levelNum)
        {
            return levelNum >= 21 && levelNum % 10 == 0;
        }

        public static int StrategyToRank(int strategy)
        {
            switch (strategy)
            {
                case 5: return 4;
                case 6: return 5;
                case 7: return 5;
                default: return strategy;
            }
        }

        public static string StrategyToTier(int strategy)
        {
            switch (strategy)
            {
                case 5: return "H";
                case 7: return "H";
                default: return "N";
            }
        }

        public static int GetStrategy(int levelNumber, GameStateService gameState)
        {
            if (levelNumber <= 5) return 1;
            if (gameState == null) throw new System.ArgumentNullException(nameof(gameState));

            int strategy = gameState.CurrentStrategy;
            if (levelNumber >= 51 && strategy < 2)
            {
                gameState.SetCurrentStrategy(2);
                strategy = 2;
            }
            return strategy;
        }

        private static readonly Dictionary<int, (string source, int index)> SpecialLevels =
            new Dictionary<int, (string source, int index)>
            {
                { 10, ("sp", 44) }, { 20, ("sp", 45) }, { 30, ("sp", 36) },
                { 40, ("sp", 9) }, { 50, ("sp", 37) }, { 55, ("sp", 7) },
                { 60, ("sp", 34) }, { 62, ("sp", 8) }, { 70, ("sp", 32) },
                { 75, ("sp", 6) }, { 80, ("sp", 43) }, { 90, ("sp", 33) },
                { 100, ("sp", 5) }, { 123, ("sp", 1) }, { 200, ("lk", 30) },
                { 250, ("lk", 75) }, { 314, ("lk", 141) }, { 456, ("sp", 2) }
            };

        private static readonly HashSet<int> LkModifiedReserved = new HashSet<int>
        {
            20, 30, 53, 71, 72, 75, 114, 141, 164
        };
        private const int TransformCount = 8;
        private static readonly string[] TransformSuffixes =
        {
            "", "r90", "r180", "r270", "h", "hr90", "hr180", "hr270"
        };

        public static bool IsSpecialLevel(int levelNumber)
        {
            return SpecialLevels.ContainsKey(levelNumber);
        }

        public static LevelEntry GetLevelEntry(
            int levelNumber,
            int currentStrategy = -1,
            int overrideSize = 0,
            GameStateService gameState = null,
            SingleRegionNumConfig singleRegionConfig = null,
            IInclusiveRandom random = null,
            NormalLevel10Config normalLevel10Config = null)
        {
            bool useSp57 = levelNumber == 10 &&
                           normalLevel10Config?.IsSp57AtLevel10() == true;
            (string source, int index) special = default;
            bool isMappedSpecial = SpecialLevels.TryGetValue(
                levelNumber,
                out special);
            if (useSp57 || isMappedSpecial)
            {
                if (useSp57) special = ("sp", 57);
                IReadOnlyList<LevelEntry> levels = special.source == "lk"
                    ? BankData.GetLkLevels()
                    : BankData.GetSpecialLevels();
                int index = special.index - 1;
                if (index < 0 || index >= levels.Count) return null;
                LevelEntry specialEntry = levels[index].Clone();
                specialEntry.BankSource = special.source;
                specialEntry.BankIndex = index + 1;
                specialEntry.BankRank = special.source == "lk" ? specialEntry.MaxRank : specialEntry.Rank;
                specialEntry.BankTier = string.Empty;
                return specialEntry;
            }

            int size = overrideSize > 0 ? overrideSize : GetSize(levelNumber);
            if (size <= 0) return null;
            GameStateService state = gameState ?? GameStateRuntime.Current;

            LevelDifficultySelection difficulty = ResolveDifficulty(
                levelNumber,
                currentStrategy,
                state,
                random ?? UnityInclusiveRandom.Instance);
            int rank = difficulty.Rank;
            string tier = difficulty.Tier;

            if (tier == "N" &&
                BankData.GetLevelsByTier(size, rank, tier).Count == 0 &&
                BankData.GetLkStyleLevelsByTier(size, rank, tier).Count == 0)
            {
                tier = string.Empty;
            }

            LevelEntry selected =
                GetFilteredEntry(size, rank, tier, levelNumber, state, singleRegionConfig);
            if (selected != null) state.CommitBankProgress();
            return selected;
        }

        internal static LevelDifficultySelection ResolveDifficulty(
            int levelNumber,
            int currentStrategy,
            GameStateService state,
            IInclusiveRandom random)
        {
            if (state == null) throw new System.ArgumentNullException(nameof(state));
            if (random == null) throw new System.ArgumentNullException(nameof(random));
            if (IsHardLevel(levelNumber)) return new LevelDifficultySelection(5, "N", 5);

            int strategy = currentStrategy >= 0
                ? (levelNumber <= 5 ? 1 : currentStrategy)
                : GetStrategy(levelNumber, state);
            if (levelNumber >= 51 && strategy < 2)
            {
                state.SetCurrentStrategy(2);
                strategy = 2;
            }
            if (levelNumber >= 51) strategy = Mathf.Min(strategy, 4);
            else if (levelNumber >= 21) strategy = Mathf.Min(strategy, 3);
            else strategy = Mathf.Min(strategy, 2);

            if (strategy >= 3) strategy = random.RangeInclusive(2, strategy);
            if (state.IsDailyFirstEasyAvailable && strategy > 1)
            {
                strategy--;
                state.ConsumeDailyFirstEasy(true);
            }
            return new LevelDifficultySelection(
                StrategyToRank(strategy),
                StrategyToTier(strategy),
                strategy);
        }

        internal readonly struct LevelDifficultySelection
        {
            public readonly int Rank;
            public readonly string Tier;
            public readonly int Strategy;

            public LevelDifficultySelection(int rank, string tier, int strategy)
            {
                Rank = rank;
                Tier = tier;
                Strategy = strategy;
            }
        }

        private static LevelEntry GetFilteredEntry(
            int size,
            int rank,
            string tier,
            int levelNumber,
            GameStateService state,
            SingleRegionNumConfig config)
        {
            config = config ?? new SingleRegionNumConfig();
            int remaining = -1;
            var seen = new HashSet<string>();
            while (true)
            {
                LevelEntry entry = levelNumber >= 51
                    ? GetNextMainEntry(size, rank, tier, state, IsHardLevel(levelNumber), config.IsCoarseLimited())
                    : GetNextEntry(size, rank, tier, state, config.IsCoarseLimited());
                if (entry == null) return null;

                int threshold = config.SingleLimitAt(levelNumber, rank);
                string source = string.IsNullOrEmpty(entry.BankSourceMain) ? entry.BankSource : entry.BankSourceMain;
                if (threshold >= 0 && !IsSingleRegionExempt(source) &&
                    CountSingleCellRegions(entry.RegionMap, size) > threshold)
                {
                    if (remaining < 0) remaining = size * size * 12;
                    string key = $"{entry.BankSource}:{entry.BankIndex}";
                    if (!seen.Contains(key) && remaining > 1)
                    {
                        seen.Add(key);
                        remaining--;
                        continue;
                    }
                }
                return entry;
            }
        }

        private static LevelEntry GetNextEntry(
            int size,
            int rank,
            string tier,
            GameStateService state,
            bool coarseLimit)
        {
            PoolSet pools = BuildOrdinaryPools(size, rank, tier, false);
            int total = pools.Total;
            if (total == 0) return null;

            LevelEntry last = null;
            for (int attempt = 0; attempt < total; attempt++)
            {
                int realIndex = PositiveModulo(state.GetBankIndex(size, rank, tier), total);
                last = SelectOrdinary(pools, realIndex, rank, tier);
                if (coarseLimit && CountSingleCellRegions(last.RegionMap, size) > 2)
                {
                    if (attempt + 1 < total) state.AdvanceBankIndex(size, rank, tier, false);
                    continue;
                }
                if (IsValid(last, size))
                {
                    state.AdvanceBankIndex(size, rank, tier, false);
                    return last;
                }
                if (attempt + 1 < total) state.AdvanceBankIndex(size, rank, tier, false);
            }
            return last;
        }

        private static LevelEntry GetNextMainEntry(
            int size,
            int rank,
            string tier,
            GameStateService state,
            bool strictRank,
            bool coarseLimit)
        {
            bool currentStrict = strictRank;
            int remaining = -1;
            LevelEntry last = null;

            while (true)
            {
                List<LevelEntry> lkModified = FilterLkModified(size, rank, currentStrict);
                PoolSet pools = BuildOrdinaryPools(size, rank, tier, true);
                int total = lkModified.Count + pools.Total;
                if (total == 0) return null;
                if (remaining < 0) remaining = total * TransformCount;

                Dictionary<string, object> progress = state.GetMainProgress(size, rank, tier);
                int transform = ReadProgressInt(progress, "transform", 0);
                int index = ReadProgressInt(progress, "idx", -1);
                int sinceLk = ReadProgressInt(progress, "since_lk", 0);
                if (index < 0)
                {
                    int legacyIndex = state.GetBankIndex(size, rank, tier);
                    int denominator = pools.Regular.Count + pools.LkStyle.Count;
                    index = legacyIndex > 0 && denominator > 0 ? legacyIndex % denominator : 0;
                    sinceLk = 0;
                    progress["idx"] = index;
                    progress["since_lk"] = sinceLk;
                    state.SetMainProgress(size, rank, tier, progress, false);
                }

                Dictionary<string, object> lkProgress = state.GetLkModifiedProgress(size, rank);
                int lkIndex = ReadProgressInt(lkProgress, "idx", 0);
                if (index >= pools.Total)
                {
                    transform = (transform + 1) % TransformCount;
                    index = 0;
                    sinceLk = 0;
                    progress = new Dictionary<string, object>
                    {
                        { "idx", 0 }, { "since_lk", 0 }, { "transform", transform }
                    };
                    state.SetMainProgress(size, rank, tier, progress, false);
                }

                if (sinceLk >= 4 && lkIndex < lkModified.Count)
                {
                    last = PrepareEntry(lkModified[lkIndex], "lk_mod", lkIndex + 1, rank, tier, "lk_mod", transform);
                }
                else if (pools.Total > 0)
                {
                    last = SelectOrdinary(pools, index, rank, tier);
                    last.BankSourceMain = last.BankSource;
                    last.BankTransform = transform;
                    if (transform > 0)
                    {
                        (int[][] regionMap, int[] solution) = ApplyTransform(last.RegionMap, last.Solution, size, transform);
                        last = last.CloneWithBoard(regionMap, solution);
                    }
                }
                else
                {
                    return null;
                }

                if (coarseLimit && !IsSingleRegionExempt(last.BankSourceMain) &&
                    CountSingleCellRegions(last.RegionMap, size) > 2)
                {
                    if (remaining <= 1) return last;
                    AdvanceRejectedMainEntry(last, size, rank, tier, state, progress, lkProgress, index, lkIndex);
                    remaining--;
                    continue;
                }

                if (IsValid(last, size))
                {
                    AdvanceForEntry(last, size, state, false);
                    return last;
                }
                if (remaining <= 1) return last;

                AdvanceRejectedMainEntry(last, size, rank, tier, state, progress, lkProgress, index, lkIndex);
                remaining--;
                currentStrict = false;
            }
        }

        private static PoolSet BuildOrdinaryPools(int size, int rank, string tier, bool mainPool)
        {
            IReadOnlyList<LevelEntry> regular = tier.Length > 0
                ? BankData.GetLevelsByTier(size, rank, tier)
                : BankData.GetLevels(size, rank);
            IReadOnlyList<LevelEntry> lkStyle = tier.Length > 0
                ? BankData.GetLkStyleLevelsByTier(size, rank, tier)
                : BankData.GetLkStyleLevels(size, rank);
            if (tier.Length > 0 && regular.Count == 0 && lkStyle.Count == 0)
            {
                regular = BankData.GetLevels(size, rank);
                lkStyle = BankData.GetLkStyleLevels(size, rank);
            }
            if (mainPool && size == 10 && (rank == 3 || rank == 4)) regular = System.Array.Empty<LevelEntry>();

            IReadOnlyList<LevelEntry> gc = System.Array.Empty<LevelEntry>();
            if ((size == 10 && rank == 1) || size == 11)
            {
                gc = tier.Length > 0
                    ? BankData.GetGcLevelsByTier(size, rank, tier)
                    : BankData.GetGcLevels(size, rank);
                if (tier.Length > 0 && gc.Count == 0) gc = BankData.GetGcLevels(size, rank);
            }
            return new PoolSet(regular, lkStyle, gc);
        }

        private static List<LevelEntry> FilterLkModified(int size, int rank, bool strictRank)
        {
            IReadOnlyList<LevelEntry> all = BankData.GetLkModifiedLevels();
            var result = new List<LevelEntry>();
            for (int index = 0; index < all.Count; index++)
            {
                LevelEntry entry = all[index];
                if (LkModifiedReserved.Contains(index + 1) || entry.Size != size) continue;
                int entryRank = strictRank ? entry.Rank : entry.MaxRank;
                if (entryRank == rank) result.Add(entry);
            }
            return result;
        }

        private static LevelEntry SelectOrdinary(PoolSet pools, int index, int rank, string tier)
        {
            if (index < pools.Regular.Count)
                return PrepareEntry(pools.Regular[index], "regular", index + 1, rank, tier);
            index -= pools.Regular.Count;
            if (index < pools.LkStyle.Count)
                return PrepareEntry(pools.LkStyle[index], "lkstyle", index + 1, rank, tier);
            index -= pools.LkStyle.Count;
            return PrepareEntry(pools.Gc[index], "gc", index + 1, rank, tier);
        }

        private static LevelEntry PrepareEntry(
            LevelEntry source,
            string bankSource,
            int bankIndex,
            int rank,
            string tier,
            string mainSource = "",
            int transform = 0)
        {
            LevelEntry entry = source.Clone();
            entry.BankSource = bankSource;
            entry.BankIndex = bankIndex;
            entry.BankRank = rank;
            entry.BankTier = tier;
            entry.BankSourceMain = mainSource;
            entry.BankTransform = transform;
            return entry;
        }

        public static void AdvanceForEntry(
            LevelEntry entry,
            int size,
            GameStateService state = null,
            bool persist = true)
        {
            if (entry == null)
                throw new System.ArgumentNullException(nameof(entry));
            state = state ?? GameStateRuntime.Current;
            if (string.IsNullOrEmpty(entry.BankSourceMain))
            {
                state.AdvanceBankIndex(
                    size,
                    entry.BankRank,
                    entry.BankTier,
                    persist);
                return;
            }
            if (entry.BankSourceMain == "lk_mod")
            {
                Dictionary<string, object> lkProgress = state.GetLkModifiedProgress(size, entry.BankRank);
                lkProgress["idx"] = ReadProgressInt(lkProgress, "idx", 0) + 1;
                state.SetLkModifiedProgress(
                    size,
                    entry.BankRank,
                    lkProgress,
                    persist);
                Dictionary<string, object> progress = state.GetMainProgress(size, entry.BankRank, entry.BankTier);
                progress["since_lk"] = 0;
                state.SetMainProgress(
                    size,
                    entry.BankRank,
                    entry.BankTier,
                    progress,
                    persist);
                return;
            }
            Dictionary<string, object> mainProgress = state.GetMainProgress(size, entry.BankRank, entry.BankTier);
            mainProgress["idx"] = ReadProgressInt(mainProgress, "idx", 0) + 1;
            mainProgress["since_lk"] = ReadProgressInt(mainProgress, "since_lk", 0) + 1;
            state.SetMainProgress(
                size,
                entry.BankRank,
                entry.BankTier,
                mainProgress,
                persist);
        }

        private static void AdvanceRejectedMainEntry(
            LevelEntry entry,
            int size,
            int rank,
            string tier,
            GameStateService state,
            Dictionary<string, object> progress,
            Dictionary<string, object> lkProgress,
            int index,
            int lkIndex)
        {
            if (entry.BankSourceMain == "lk_mod")
            {
                lkProgress["idx"] = lkIndex + 1;
                state.SetLkModifiedProgress(size, rank, lkProgress, false);
            }
            else
            {
                progress["idx"] = index + 1;
                state.SetMainProgress(size, rank, tier, progress, false);
            }
        }

        private static bool IsSingleRegionExempt(string source)
        {
            return source == "lk_mod" || source == "sp" || source == "lk";
        }

        public static int CountSingleCellRegions(int[][] regionMap, int size)
        {
            if (regionMap == null || regionMap.Length != size) return 0;
            var counts = new Dictionary<int, int>();
            for (int row = 0; row < size; row++)
            {
                if (regionMap[row] == null || regionMap[row].Length != size) return 0;
                for (int column = 0; column < size; column++)
                {
                    int region = regionMap[row][column];
                    counts[region] = counts.TryGetValue(region, out int count) ? count + 1 : 1;
                }
            }
            int singles = 0;
            foreach (int count in counts.Values) if (count == 1) singles++;
            return singles;
        }

        public static Vector2Int? ComputePrefill(
            int levelNumber,
            int[][] regionMap,
            int[] solution,
            int size)
        {
            if (levelNumber < 1 || levelNumber > 10) return null;
            bool wantSizeOne = levelNumber >= 7;
            var regionAreas = new Dictionary<int, int>();
            for (int row = 0; row < size; row++)
            {
                for (int column = 0; column < size; column++)
                {
                    int region = regionMap[row][column];
                    regionAreas[region] = regionAreas.TryGetValue(region, out int area) ? area + 1 : 1;
                }
            }
            for (int row = 0; row < size; row++)
            {
                int column = solution[row];
                int area = regionAreas[regionMap[row][column]];
                if ((wantSizeOne && area == 1) || (!wantSizeOne && area > 1))
                    return new Vector2Int(row, column);
            }
            return size > 0 && solution != null && solution.Length > 0
                ? new Vector2Int(0, solution[0])
                : (Vector2Int?)null;
        }

        public static string ComputePuzzleId(int size, int[][] regionMap)
        {
            string inputNormalized = SerializeRegionMap(NormalizeRegionMap(regionMap, size));
            var transformedMaps = new int[TransformCount][][];
            var transformedStrings = new string[TransformCount];
            for (int transform = 0; transform < TransformCount; transform++)
            {
                transformedMaps[transform] = ApplyRegionTransform(regionMap, size, transform);
                transformedStrings[transform] = SerializeRegionMap(NormalizeRegionMap(transformedMaps[transform], size));
            }

            string canonical = transformedStrings[0];
            int canonicalIndex = 0;
            for (int transform = 1; transform < TransformCount; transform++)
            {
                if (string.CompareOrdinal(transformedStrings[transform], canonical) < 0)
                {
                    canonical = transformedStrings[transform];
                    canonicalIndex = transform;
                }
            }

            int suffixTransform = 0;
            for (int transform = 0; transform < TransformCount; transform++)
            {
                int[][] candidate = ApplyRegionTransform(transformedMaps[canonicalIndex], size, transform);
                if (SerializeRegionMap(NormalizeRegionMap(candidate, size)) == inputNormalized)
                {
                    suffixTransform = transform;
                    break;
                }
            }

            string hash;
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical));
                var builder = new StringBuilder(bytes.Length * 2);
                foreach (byte value in bytes) builder.Append(value.ToString("x2"));
                hash = builder.ToString().Substring(0, 16);
            }
            return suffixTransform == 0
                ? $"{size}_{hash}"
                : $"{size}_{hash}_{TransformSuffixes[suffixTransform]}";
        }

        private static int[][] ApplyRegionTransform(int[][] regionMap, int size, int transform)
        {
            int[][] map = Clone2DArray(regionMap, size);
            int mirror = transform / 4;
            int rotations = transform % 4;
            if (mirror == 1)
            {
                var mirrored = new int[size][];
                for (int row = 0; row < size; row++)
                {
                    mirrored[row] = new int[size];
                    for (int column = 0; column < size; column++)
                        mirrored[row][column] = map[row][size - 1 - column];
                }
                map = mirrored;
            }
            for (int rotation = 0; rotation < rotations; rotation++)
            {
                var rotated = new int[size][];
                for (int row = 0; row < size; row++)
                {
                    rotated[row] = new int[size];
                    for (int column = 0; column < size; column++)
                        rotated[row][column] = map[size - 1 - column][row];
                }
                map = rotated;
            }
            return map;
        }

        private static int[][] NormalizeRegionMap(int[][] regionMap, int size)
        {
            var remap = new Dictionary<int, int>();
            int nextId = 0;
            var result = new int[size][];
            for (int row = 0; row < size; row++)
            {
                result[row] = new int[size];
                for (int column = 0; column < size; column++)
                {
                    int value = regionMap[row][column];
                    if (!remap.TryGetValue(value, out int normalized))
                    {
                        normalized = nextId++;
                        remap[value] = normalized;
                    }
                    result[row][column] = normalized;
                }
            }
            return result;
        }

        private static string SerializeRegionMap(int[][] regionMap)
        {
            var builder = new StringBuilder();
            for (int row = 0; row < regionMap.Length; row++)
            {
                for (int column = 0; column < regionMap[row].Length; column++)
                {
                    if (builder.Length > 0) builder.Append(',');
                    builder.Append(regionMap[row][column]);
                }
            }
            return builder.ToString();
        }

        private static bool IsValid(LevelEntry entry, int size)
        {
            return entry != null && QueendokuCore.ValidateSolutionEntry(entry.RegionMap, entry.Solution, size);
        }

        private static int ReadProgressInt(Dictionary<string, object> progress, string key, int fallback)
        {
            return progress.TryGetValue(key, out object value) && value != null ? System.Convert.ToInt32(value) : fallback;
        }

        private static int PositiveModulo(int value, int modulus)
        {
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }

        private sealed class PoolSet
        {
            public readonly IReadOnlyList<LevelEntry> Regular;
            public readonly IReadOnlyList<LevelEntry> LkStyle;
            public readonly IReadOnlyList<LevelEntry> Gc;
            public int Total => Regular.Count + LkStyle.Count + Gc.Count;

            public PoolSet(
                IReadOnlyList<LevelEntry> regular,
                IReadOnlyList<LevelEntry> lkStyle,
                IReadOnlyList<LevelEntry> gc)
            {
                Regular = regular;
                LkStyle = lkStyle;
                Gc = gc;
            }
        }

        // Xoay (Rotate) và Lật (Mirror) ma trận bản đồ để tạo biến thể mới.
        // Trả về tuple gồm Bản đồ và Đáp án đã được Xoay/Lật.
        public static (int[][] rm, int[] sol) ApplyTransform(int[][] regionMap, int[] solution, int sz, int t)
        {
            int[][] rm = Clone2DArray(regionMap, sz);
            int[] sol = (int[])solution.Clone();
            
            int mirror = t / 4;  // Trục lật
            int rot = t % 4;     // Góc xoay

            // Xử lý Lật (Mirror)
            if (mirror == 1)
            {
                int[][] newRm = new int[sz][];
                for (int r = 0; r < sz; r++)
                {
                    newRm[r] = new int[sz];
                    for (int c = 0; c < sz; c++)
                    {
                        newRm[r][c] = rm[r][sz - 1 - c];
                    }
                }
                int[] newSol = new int[sz];
                for (int r = 0; r < sz; r++) newSol[r] = sz - 1 - sol[r];
                rm = newRm; sol = newSol;
            }
            else if (mirror == 2)
            {
                int[][] newRm = new int[sz][];
                for (int r = 0; r < sz; r++)
                {
                    newRm[r] = (int[])rm[sz - 1 - r].Clone();
                }
                int[] newSol = (int[])sol.Clone();
                System.Array.Reverse(newSol);
                rm = newRm; sol = newSol;
            }

            // Xử lý Xoay (Rotate)
            for (int i = 0; i < rot; i++)
            {
                int[][] newRm = new int[sz][];
                for (int r2 = 0; r2 < sz; r2++)
                {
                    newRm[r2] = new int[sz];
                    for (int c2 = 0; c2 < sz; c2++)
                    {
                        newRm[r2][c2] = rm[sz - 1 - c2][r2];
                    }
                }
                int[] newSol = new int[sz];
                for (int r2 = 0; r2 < sz; r2++) newSol[sol[r2]] = sz - 1 - r2;
                rm = newRm; sol = newSol;
            }

            return (rm, sol);
        }

        // Sao chép (clone) mảng 2 chiều để tránh lỗi Reference trong C#.
        private static int[][] Clone2DArray(int[][] source, int sz)
        {
            int[][] dest = new int[sz][];
            for (int i = 0; i < sz; i++)
            {
                dest[i] = (int[])source[i].Clone();
            }
            return dest;
        }
    }
}
