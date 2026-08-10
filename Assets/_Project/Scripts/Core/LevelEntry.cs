using System;
using System.Collections;
using System.Collections.Generic;

namespace Meowdoku.Core
{
    [Serializable]
    public sealed class LevelEntry
    {
        public int Id { get; private set; }
        public int Seed { get; private set; }
        public int Size { get; private set; }
        public int Rank { get; private set; }
        public int MaxRank { get; private set; }
        public int Steps { get; private set; }
        public int R1Steps { get; private set; }
        public int R2Steps { get; private set; }
        public int R3Steps { get; private set; }
        public int R4Steps { get; private set; }
        public int R5Steps { get; private set; }
        public int SourceTransform { get; private set; }
        public int Sequence { get; private set; }
        public string Date { get; private set; }
        public string Label { get; private set; }
        public string Tier { get; private set; }
        public string Pattern { get; private set; }
        public int[][] RegionMap { get; private set; }
        public int[] Solution { get; private set; }
        public int[] ColorMap { get; private set; }
        public int[] PatternRegions { get; private set; }

        public string BankSource { get; internal set; }
        public int BankIndex { get; internal set; }
        public int BankRank { get; internal set; }
        public string BankTier { get; internal set; }
        public string BankSourceMain { get; internal set; }
        public int BankTransform { get; internal set; }
        public bool FromBankBrowser { get; private set; }
        public bool BankMode { get; private set; }
        public bool BankLk { get; private set; }
        public bool BankLkModified { get; private set; }
        public bool BankLkStyle { get; private set; }
        public bool BankGc { get; private set; }
        public bool BankSp { get; private set; }
        public bool BankTierH { get; private set; }
        public int BankTotal { get; private set; }

        public static LevelEntry FromDictionary(IDictionary<string, object> data)
        {
            if (data == null) return null;

            var entry = new LevelEntry
            {
                Id = ReadInt(data, "id"),
                Seed = ReadInt(data, "seed"),
                Size = ReadInt(data, "size"),
                Rank = ReadInt(data, "r", 1),
                MaxRank = ReadInt(data, "maxR", ReadInt(data, "r", 1)),
                Steps = ReadInt(data, "steps"),
                R1Steps = ReadInt(data, "r1"),
                R2Steps = ReadInt(data, "r2"),
                R3Steps = ReadInt(data, "r3"),
                R4Steps = ReadInt(data, "r4"),
                R5Steps = ReadInt(data, "r5"),
                SourceTransform = ReadInt(data, "transform"),
                Sequence = ReadInt(data, "seq"),
                Date = ReadString(data, "date"),
                Label = ReadString(data, "label"),
                Tier = ReadString(data, "tier"),
                Pattern = ReadString(data, "pattern"),
                RegionMap = ReadMatrix(data, "regionMap"),
                Solution = ReadIntArray(data, "solution"),
                ColorMap = ReadIntArray(data, data.ContainsKey("colorMap") ? "colorMap" : "custom_color_map"),
                PatternRegions = ReadIntArray(data, "patternRegions"),
                BankSource = ReadOptionalString(data, "bank_source"),
                BankIndex = ReadInt(data, "bank_index", ReadInt(data, "id")),
                BankRank = ReadInt(data, "bank_rank", ReadInt(data, "r", 1)),
                BankTier = ReadOptionalString(data, "bank_tier"),
                BankSourceMain = ReadOptionalString(data, "bank_source_main"),
                BankTransform = ReadInt(data, "bank_transform"),
                FromBankBrowser = ReadBool(data, "from_bank_browser"),
                BankMode = ReadBool(data, "bank_mode"),
                BankLk = ReadBool(data, "bank_lk"),
                BankLkModified = ReadBool(data, "bank_lk_modified"),
                BankLkStyle = ReadBool(data, "bank_lk_style"),
                BankGc = ReadBool(data, "bank_gc"),
                BankSp = ReadBool(data, "bank_sp"),
                BankTierH = ReadBool(data, "bank_tier_h"),
                BankTotal = ReadInt(data, "bank_total")
            };

            if (entry.Size <= 0 && entry.RegionMap != null)
            {
                entry.Size = entry.RegionMap.Length;
            }

            return entry;
        }

        internal LevelEntry Clone()
        {
            return new LevelEntry
            {
                Id = Id,
                Seed = Seed,
                Size = Size,
                Rank = Rank,
                MaxRank = MaxRank,
                Steps = Steps,
                R1Steps = R1Steps,
                R2Steps = R2Steps,
                R3Steps = R3Steps,
                R4Steps = R4Steps,
                R5Steps = R5Steps,
                SourceTransform = SourceTransform,
                Sequence = Sequence,
                Date = Date,
                Label = Label,
                Tier = Tier,
                Pattern = Pattern,
                RegionMap = CloneMatrix(RegionMap),
                Solution = Solution == null ? null : (int[])Solution.Clone(),
                ColorMap = ColorMap == null ? null : (int[])ColorMap.Clone(),
                PatternRegions = PatternRegions == null ? null : (int[])PatternRegions.Clone(),
                BankSource = BankSource,
                BankIndex = BankIndex,
                BankRank = BankRank,
                BankTier = BankTier,
                BankSourceMain = BankSourceMain,
                BankTransform = BankTransform,
                FromBankBrowser = FromBankBrowser,
                BankMode = BankMode,
                BankLk = BankLk,
                BankLkModified = BankLkModified,
                BankLkStyle = BankLkStyle,
                BankGc = BankGc,
                BankSp = BankSp,
                BankTierH = BankTierH,
                BankTotal = BankTotal
            };
        }

        internal LevelEntry CloneWithBoard(int[][] regionMap, int[] solution)
        {
            LevelEntry clone = Clone();
            clone.RegionMap = regionMap;
            clone.Solution = solution;
            return clone;
        }

        private static int[][] CloneMatrix(int[][] source)
        {
            if (source == null) return null;
            var result = new int[source.Length][];
            for (int i = 0; i < source.Length; i++) result[i] = source[i] == null ? null : (int[])source[i].Clone();
            return result;
        }

        private static int ReadInt(IDictionary<string, object> data, string key, int fallback = 0)
        {
            if (!data.TryGetValue(key, out object value) || value == null) return fallback;
            return Convert.ToInt32(value);
        }

        private static string ReadString(IDictionary<string, object> data, string key)
        {
            return data.TryGetValue(key, out object value) && value != null ? value.ToString() : string.Empty;
        }

        private static bool ReadBool(
            IDictionary<string, object> data,
            string key)
        {
            return data.TryGetValue(key, out object value) &&
                   value != null && Convert.ToBoolean(value);
        }

        private static string ReadOptionalString(IDictionary<string, object> data, string key)
        {
            return data.TryGetValue(key, out object value) && value != null ? value.ToString() : null;
        }

        private static int[] ReadIntArray(IDictionary<string, object> data, string key)
        {
            if (!data.TryGetValue(key, out object value) || !(value is IList list)) return null;
            var result = new int[list.Count];
            for (int i = 0; i < list.Count; i++) result[i] = Convert.ToInt32(list[i]);
            return result;
        }

        private static int[][] ReadMatrix(IDictionary<string, object> data, string key)
        {
            if (!data.TryGetValue(key, out object value) || !(value is IList rows)) return null;
            var result = new int[rows.Count][];
            for (int row = 0; row < rows.Count; row++)
            {
                if (!(rows[row] is IList columns)) return null;
                result[row] = new int[columns.Count];
                for (int column = 0; column < columns.Count; column++)
                {
                    result[row][column] = Convert.ToInt32(columns[column]);
                }
            }

            return result;
        }
    }
}
