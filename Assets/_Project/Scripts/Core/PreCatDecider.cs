using System;
using System.Collections.Generic;
using UnityEngine;

namespace Meowdoku.Core
{
    public sealed class PreCatDecision
    {
        public string PreType { get; internal set; } = PreCatDecider.PreTypeNone;
        public Vector2Int Position { get; internal set; } = new Vector2Int(-1, -1);
        public bool HasPlacement => Position.x >= 0;
    }

    /// <summary>Pure port of gameplay/core/pre_cat_decider.gd.</summary>
    public static class PreCatDecider
    {
        public const int SceneHardNext = 1;
        public const int SceneConsecutiveFail = 2;
        public const int SceneDemote = 3;
        public const int ValueOff = 0;
        public const int ValueAlways = 1;
        public const int ValueHalf = 2;
        public const string PreTypeNone = "0";

        public static List<int> HitScenarios(
            bool isPreviousHard,
            int currentRank,
            bool pendingStruggle,
            bool pendingDemote)
        {
            var result = new List<int>();
            if (isPreviousHard && currentRank >= 3) result.Add(SceneHardNext);
            if (pendingStruggle) result.Add(SceneConsecutiveFail);
            if (pendingDemote) result.Add(SceneDemote);
            return result;
        }

        public static string ScenesToPreType(IReadOnlyList<int> scenes)
        {
            if (scenes == null || scenes.Count == 0) return PreTypeNone;
            var parts = new string[scenes.Count];
            for (int i = 0; i < scenes.Count; i++) parts[i] = scenes[i].ToString();
            return string.Join("&", parts);
        }

        public static Vector2Int PickPrefillCell(
            int size,
            int[][] regions,
            bool[][] solution,
            IInclusiveRandom random = null)
        {
            random = random ?? UnityInclusiveRandom.Instance;
            var board = new CellStateType[size][];
            for (int row = 0; row < size; row++) board[row] = new CellStateType[size];
            Dictionary<Vector2Int, int> ranks = HintEngine.ComputeCellRanks(
                board, size, regions, solution);
            var candidates = new List<Vector2Int>();
            foreach (KeyValuePair<Vector2Int, int> rank in ranks)
                if (rank.Value >= 3) candidates.Add(rank.Key);
            if (candidates.Count == 0) return new Vector2Int(-1, -1);
            return candidates[random.RangeInclusive(0, candidates.Count - 1)];
        }

        public static PreCatDecision Decide(
            int group,
            IReadOnlyList<int> scenes,
            int size,
            int[][] regions,
            bool[][] solution,
            IInclusiveRandom random = null)
        {
            var noPlacement = new PreCatDecision();
            if (group == ValueOff || scenes == null || scenes.Count == 0) return noPlacement;
            random = random ?? UnityInclusiveRandom.Instance;
            if (group == ValueHalf && random.RangeInclusive(0, 1) == 1) return noPlacement;
            Vector2Int position = PickPrefillCell(size, regions, solution, random);
            if (position.x < 0) return noPlacement;
            return new PreCatDecision
            {
                PreType = ScenesToPreType(scenes),
                Position = position
            };
        }
    }
}
