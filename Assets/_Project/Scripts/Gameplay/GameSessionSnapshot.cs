using System;
using System.Collections;
using System.Collections.Generic;
using Meowdoku.Core;
using UnityEngine;

namespace Meowdoku.Gameplay
{
    public enum GameplaySessionMode
    {
        Unspecified = 0,
        Main = 1,
        Bank = 2,
        Daily = 3
    }

    public sealed class GameSessionSnapshotContext
    {
        public const int CurrentVersion = 2;

        public int Level { get; set; }
        public int BankIndex { get; set; }
        public LevelEntry Entry { get; set; }
        public GameplaySessionMode Mode { get; set; }
        public string DailyDate { get; set; } = string.Empty;
        public int DailyIndex { get; set; }
        public Dictionary<string, object> LaunchParameters { get; set; } =
            new Dictionary<string, object>();
        public string PreType { get; set; } = PreCatDecider.PreTypeNone;
        public double InGameSeconds { get; set; }
        public List<Vector2Int> PrefillPositions { get; } = new List<Vector2Int>();
        public Vector2Int PreCatPosition { get; set; } = new Vector2Int(-1, -1);

        public GameplaySessionMode ResolvedMode => Mode != GameplaySessionMode.Unspecified
            ? Mode
            : Level > 0
                ? GameplaySessionMode.Main
                : GameplaySessionMode.Bank;
    }

    public sealed class GameSessionSnapshotRestore
    {
        public LevelEntry Entry { get; internal set; }
        public GameSessionRestoreData Session { get; internal set; }
        public string PreType { get; internal set; } = PreCatDecider.PreTypeNone;
        public double InGameSeconds { get; internal set; }
        public List<Vector2Int> PrefillPositions { get; } = new List<Vector2Int>();
        public bool IsComplete { get; internal set; }
    }

    /// <summary>Schema/version/integrity port of GamePage endgame snapshot handling.</summary>
    public static class GameSessionSnapshot
    {
        public static Dictionary<string, object> Build(
            GameSession session,
            GameSessionSnapshotContext context)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (context?.Entry == null) throw new ArgumentNullException(nameof(context));
            LevelEntry entry = context.Entry;
            Dictionary<string, object> snapshot = session.CreateSnapshot();
            snapshot["version"] = GameSessionSnapshotContext.CurrentVersion;
            snapshot["level"] = context.Level;
            snapshot["size"] = entry.Size;
            snapshot["r"] = entry.Rank;
            snapshot["id"] = context.BankIndex;
            snapshot["seed"] = entry.Seed;
            snapshot["regionMap"] = ToRows(entry.RegionMap);
            snapshot["solution"] = ToValues(entry.Solution);
            snapshot["bank_source"] = entry.BankSource ?? string.Empty;
            snapshot["bank_source_main"] = entry.BankSourceMain ?? string.Empty;
            snapshot["bank_tier"] = entry.BankTier ?? string.Empty;
            snapshot["pre_type"] = context.PreType ?? PreCatDecider.PreTypeNone;
            snapshot["prefill_positions"] = ToPositions(context.PrefillPositions);
            snapshot["in_game_sec"] = Math.Max(0.0, context.InGameSeconds);
            return snapshot;
        }

        public static bool TryRead(
            IDictionary<string, object> snapshot,
            int expectedLevel,
            out GameSessionSnapshotRestore restore)
        {
            restore = null;
            try
            {
                if (snapshot == null || ReadInt(snapshot, "version") != GameSessionSnapshotContext.CurrentVersion)
                    return false;
                string[] required = { "size", "r", "id", "regionMap", "solution", "level", "lives", "placed_cats", "marks", "errors" };
                for (int i = 0; i < required.Length; i++)
                    if (!snapshot.ContainsKey(required[i])) return false;

                int level = ReadInt(snapshot, "level");
                int size = ReadInt(snapshot, "size");
                int lives = ReadInt(snapshot, "lives");
                if (level != expectedLevel || size <= 0 || lives <= 0) return false;

                LevelEntry entry = LevelEntry.FromDictionary(snapshot);
                if (entry?.RegionMap == null || entry.RegionMap.Length != size ||
                    entry.Solution == null || entry.Solution.Length != size)
                    return false;
                for (int row = 0; row < size; row++)
                    if (entry.RegionMap[row] == null || entry.RegionMap[row].Length != size ||
                        entry.Solution[row] < 0 || entry.Solution[row] >= size)
                        return false;

                var session = new GameSessionRestoreData
                {
                    Lives = lives,
                    ReviveCount = ReadInt(snapshot, "revive_count"),
                    RestartCount = ReadInt(snapshot, "restart_count"),
                    SuccessfulCatCount = ReadInt(snapshot, "se_count"),
                    StepHistoryData = ReadList(snapshot, "step_history"),
                    Score = new Dictionary<string, int>
                    {
                        { "score", ReadScore(snapshot) },
                        { "combo", ReadInt(snapshot, "combo", ReadInt(snapshot, "combo_count")) },
                        { "max_combo", ReadInt(snapshot, "max_combo", ReadInt(snapshot, "combo_count")) }
                    }
                };
                if (!ReadPositions(snapshot, "placed_cats", size, session.PlacedCats, entry.Solution, true) ||
                    !ReadPositions(snapshot, "marks", size, session.Marks, null, false) ||
                    !ReadPositions(snapshot, "errors", size, session.Errors, null, false))
                    return false;

                var result = new GameSessionSnapshotRestore
                {
                    Entry = entry,
                    Session = session,
                    PreType = ReadString(snapshot, "pre_type", PreCatDecider.PreTypeNone),
                    InGameSeconds = Math.Max(0.0, ReadDouble(snapshot, "in_game_sec")),
                    IsComplete = session.PlacedCats.Count == size
                };
                ReadPositions(snapshot, "prefill_positions", size, result.PrefillPositions, entry.Solution, true);
                restore = result;
                return true;
            }
            catch (Exception exception) when (exception is InvalidCastException || exception is FormatException || exception is OverflowException)
            {
                return false;
            }
        }

        public static bool HasUserProgress(IDictionary<string, object> snapshot, int level)
        {
            if (!TryRead(snapshot, level, out GameSessionSnapshotRestore restore)) return false;
            if (restore.PrefillPositions.Count > 0) return true;
            return restore.Session.PlacedCats.Count > 0 || restore.Session.Marks.Count > 0 || restore.Session.Errors.Count > 0;
        }

        private static int ReadScore(IDictionary<string, object> data)
        {
            if (data.ContainsKey("score")) return ReadInt(data, "score");
            return ReadInt(data, ReadBool(data, "se_enabled") ? "se_score" : "combo_score");
        }

        private static bool ReadPositions(
            IDictionary<string, object> data,
            string key,
            int size,
            ICollection<Vector2Int> destination,
            int[] solution,
            bool requireSolution)
        {
            IList list = ReadList(data, key);
            if (list == null) return false;
            for (int i = 0; i < list.Count; i++)
            {
                if (!(list[i] is IList position) || position.Count < 2) return false;
                int row = Convert.ToInt32(position[0]);
                int column = Convert.ToInt32(position[1]);
                if (row < 0 || row >= size || column < 0 || column >= size) return false;
                if (requireSolution && (solution == null || solution[row] != column)) return false;
                destination.Add(new Vector2Int(row, column));
            }
            return true;
        }

        private static IList ReadList(IDictionary<string, object> data, string key)
        {
            return data.TryGetValue(key, out object value) ? value as IList : null;
        }

        private static int ReadInt(IDictionary<string, object> data, string key, int fallback = 0)
        {
            return data.TryGetValue(key, out object value) && value != null ? Convert.ToInt32(value) : fallback;
        }

        private static double ReadDouble(
            IDictionary<string, object> data,
            string key,
            double fallback = 0.0)
        {
            if (!data.TryGetValue(key, out object value) || value == null)
                return fallback;
            double result = Convert.ToDouble(value);
            return double.IsNaN(result) || double.IsInfinity(result)
                ? fallback
                : result;
        }

        private static bool ReadBool(IDictionary<string, object> data, string key)
        {
            return data.TryGetValue(key, out object value) && value != null && Convert.ToBoolean(value);
        }

        private static string ReadString(IDictionary<string, object> data, string key, string fallback)
        {
            return data.TryGetValue(key, out object value) && value != null ? value.ToString() : fallback;
        }

        private static List<object> ToValues(int[] values)
        {
            var result = new List<object>();
            if (values != null) for (int i = 0; i < values.Length; i++) result.Add(values[i]);
            return result;
        }

        private static List<object> ToRows(int[][] rows)
        {
            var result = new List<object>();
            if (rows != null) for (int i = 0; i < rows.Length; i++) result.Add(ToValues(rows[i]));
            return result;
        }

        private static List<object> ToPositions(IReadOnlyList<Vector2Int> positions)
        {
            var result = new List<object>();
            for (int i = 0; positions != null && i < positions.Count; i++)
                result.Add(new List<object> { positions[i].x, positions[i].y });
            return result;
        }
    }
}
