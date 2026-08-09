using System;
using System.Collections.Generic;

namespace Meowdoku.Core
{
    public static class BankData
    {
        private static readonly int[] KnownSizes = { 4, 5, 6, 7, 8, 9, 10, 12 };
        private static readonly int[] LkStyleKnownSizes = { 7, 8, 9, 10, 11, 12 };
        private static readonly int[] GcKnownSizes = { 6, 7, 8, 9, 10, 11, 12 };

        private static readonly Dictionary<string, List<LevelEntry>> Cache = new Dictionary<string, List<LevelEntry>>();
        private static readonly HashSet<int> LoadedSizes = new HashSet<int>();
        private static readonly Dictionary<string, List<LevelEntry>> LkStyleCache = new Dictionary<string, List<LevelEntry>>();
        private static readonly HashSet<int> LkStyleLoadedSizes = new HashSet<int>();
        private static readonly Dictionary<int, List<LevelEntry>> GcCache = new Dictionary<int, List<LevelEntry>>();
        private static readonly HashSet<int> GcLoadedSizes = new HashSet<int>();

        private static List<LevelEntry> _specialLevels;
        private static List<LevelEntry> _lkLevels;
        private static List<LevelEntry> _lkModifiedLevels;

        public static IReadOnlyList<int> GetSizes()
        {
            var result = new List<int>();
            foreach (int size in KnownSizes)
            {
                if (GetRanks(size).Count > 0) result.Add(size);
            }
            return result;
        }

        public static IReadOnlyList<int> GetRanks(int size) { return RanksFor(size, GetLevels); }
        public static IReadOnlyList<LevelEntry> GetLevels(int size, int rank)
        {
            LoadSize(size);
            return Cache.TryGetValue(Key(size, rank), out List<LevelEntry> levels) ? levels : Array.Empty<LevelEntry>();
        }
        public static IReadOnlyList<LevelEntry> GetLevelsByTier(int size, int rank, string tier) { return ByTier(GetLevels(size, rank), tier); }
        public static int GetLevelCount(int size, int rank) { return GetLevels(size, rank).Count; }
        public static int GetLevelCountByTier(int size, int rank, string tier) { return GetLevelsByTier(size, rank, tier).Count; }

        public static IReadOnlyList<int> GetLkStyleSizes()
        {
            var result = new List<int>();
            foreach (int size in LkStyleKnownSizes)
            {
                LoadLkStyleSize(size);
                if (GetLkStyleRanks(size).Count > 0) result.Add(size);
            }
            return result;
        }
        public static IReadOnlyList<int> GetLkStyleRanks(int size) { return RanksFor(size, GetLkStyleLevels); }
        public static IReadOnlyList<LevelEntry> GetLkStyleLevels(int size, int rank)
        {
            LoadLkStyleSize(size);
            return LkStyleCache.TryGetValue(Key(size, rank), out List<LevelEntry> levels) ? levels : Array.Empty<LevelEntry>();
        }
        public static IReadOnlyList<LevelEntry> GetLkStyleLevelsByTier(int size, int rank, string tier) { return ByTier(GetLkStyleLevels(size, rank), tier); }
        public static int GetLkStyleLevelCount(int size, int rank) { return GetLkStyleLevels(size, rank).Count; }
        public static int GetLkStyleLevelCountByTier(int size, int rank, string tier) { return GetLkStyleLevelsByTier(size, rank, tier).Count; }

        public static IReadOnlyList<int> GetGcSizes()
        {
            var result = new List<int>();
            foreach (int size in GcKnownSizes)
            {
                LoadGcSize(size);
                if (GcCache.TryGetValue(size, out List<LevelEntry> levels) && levels.Count > 0) result.Add(size);
            }
            return result;
        }
        public static IReadOnlyList<int> GetGcRanks(int size) { return RanksFor(size, GetGcLevels); }
        public static IReadOnlyList<LevelEntry> GetGcLevels(int size, int rank)
        {
            LoadGcSize(size);
            if (!GcCache.TryGetValue(size, out List<LevelEntry> all)) return Array.Empty<LevelEntry>();
            var result = new List<LevelEntry>();
            foreach (LevelEntry entry in all) if (entry.Rank == rank) result.Add(entry);
            return result;
        }
        public static IReadOnlyList<LevelEntry> GetGcLevelsByTier(int size, int rank, string tier) { return ByTier(GetGcLevels(size, rank), tier); }
        public static int GetGcLevelCount(int size, int rank) { return GetGcLevels(size, rank).Count; }
        public static int GetGcLevelCountByTier(int size, int rank, string tier) { return GetGcLevelsByTier(size, rank, tier).Count; }

        public static IReadOnlyList<LevelEntry> GetSpecialLevels()
        {
            if (_specialLevels == null) _specialLevels = LoadNamedLevelArray("bankDataSP.json", "levels");
            return _specialLevels;
        }
        public static int GetSpecialLevelCount() { return GetSpecialLevels().Count; }
        public static void ReloadSpecialLevels() { _specialLevels = null; GetSpecialLevels(); }

        public static IReadOnlyList<LevelEntry> GetLkLevels()
        {
            if (_lkLevels == null) _lkLevels = LoadNamedLevelArray("bankDataLK.json", null);
            return _lkLevels;
        }

        public static IReadOnlyList<LevelEntry> GetLkModifiedLevels()
        {
            if (_lkModifiedLevels == null) _lkModifiedLevels = LoadNamedLevelArray("bankDataLKModified.json", "levels");
            return _lkModifiedLevels;
        }
        public static int GetLkModifiedLevelCount() { return GetLkModifiedLevels().Count; }

        private static void LoadSize(int size)
        {
            if (!LoadedSizes.Add(size)) return;
            LoadRankedFile($"bankData{size}x{size}.json", size, Cache);
        }

        private static void LoadLkStyleSize(int size)
        {
            if (!LkStyleLoadedSizes.Add(size)) return;
            LoadRankedFile($"bankDataLKStyle{size}x{size}.json", size, LkStyleCache);
        }

        private static void LoadGcSize(int size)
        {
            if (!GcLoadedSizes.Add(size)) return;
            object parsed = LevelBankIO.LoadJson($"bankDataGC{size}x{size}.json");
            if (!(parsed is IDictionary<string, object> root) || !root.TryGetValue("levels", out object raw)) return;
            List<LevelEntry> levels = ParseLevelArray(raw);
            if (levels.Count > 0) GcCache[size] = levels;
        }

        private static void LoadRankedFile(string filename, int size, Dictionary<string, List<LevelEntry>> target)
        {
            object parsed = LevelBankIO.LoadJson(filename);
            if (!(parsed is IDictionary<string, object> root)) return;
            foreach (KeyValuePair<string, object> pair in root)
            {
                if (!int.TryParse(pair.Key, out int rank)) continue;
                List<LevelEntry> levels = ParseLevelArray(pair.Value);
                if (levels.Count > 0) target[Key(size, rank)] = levels;
            }
        }

        private static List<LevelEntry> LoadNamedLevelArray(string filename, string rootProperty)
        {
            object parsed = LevelBankIO.LoadJson(filename);
            object raw = parsed;
            if (!string.IsNullOrEmpty(rootProperty))
            {
                if (!(parsed is IDictionary<string, object> root) || !root.TryGetValue(rootProperty, out raw)) return new List<LevelEntry>();
            }
            return ParseLevelArray(raw);
        }

        private static List<LevelEntry> ParseLevelArray(object raw)
        {
            if (!(raw is IList<object> rawLevels)) return new List<LevelEntry>();
            var result = new List<LevelEntry>(rawLevels.Count);
            foreach (object item in rawLevels)
            {
                if (!(item is IDictionary<string, object> rawEntry)) continue;
                LevelEntry entry = LevelEntry.FromDictionary(rawEntry);
                if (entry != null) result.Add(entry);
            }
            return result;
        }

        private static IReadOnlyList<int> RanksFor(int size, Func<int, int, IReadOnlyList<LevelEntry>> getter)
        {
            var result = new List<int>();
            for (int rank = 1; rank <= 5; rank++) if (getter(size, rank).Count > 0) result.Add(rank);
            return result;
        }

        private static IReadOnlyList<LevelEntry> ByTier(IReadOnlyList<LevelEntry> all, string tier)
        {
            var result = new List<LevelEntry>();
            foreach (LevelEntry entry in all) if (entry.Tier == tier) result.Add(entry);
            return result;
        }

        private static string Key(int size, int rank) { return $"{size}_{rank}"; }

        internal static void ResetForTests()
        {
            Cache.Clear(); LoadedSizes.Clear();
            LkStyleCache.Clear(); LkStyleLoadedSizes.Clear();
            GcCache.Clear(); GcLoadedSizes.Clear();
            _specialLevels = null; _lkLevels = null; _lkModifiedLevels = null;
        }
    }
}
