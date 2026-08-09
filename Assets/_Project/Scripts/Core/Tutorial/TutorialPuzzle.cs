using System;
using System.Collections.Generic;

namespace Meowdoku.Core.Tutorial
{
    public sealed class TutorialPuzzle
    {
        public const int SourceGuideId = 51;
        public const int SourceSize = 4;
        public const float SourceBoardWidth = 919f;

        public TutorialPuzzle(
            int id,
            string pattern,
            int[][] regions,
            int[] solutionColumns,
            int[] colorMap)
        {
            if (regions == null || regions.Length != SourceSize)
                throw new ArgumentException("Tutorial regions must be 4x4.", nameof(regions));
            for (int row = 0; row < SourceSize; row++)
            {
                if (regions[row] == null || regions[row].Length != SourceSize)
                    throw new ArgumentException("Tutorial regions must be 4x4.", nameof(regions));
            }
            if (solutionColumns == null || solutionColumns.Length != SourceSize)
                throw new ArgumentException("Tutorial solution must contain four columns.", nameof(solutionColumns));
            if (colorMap == null || colorMap.Length != SourceSize)
                throw new ArgumentException("Tutorial color map must contain four entries.", nameof(colorMap));

            Id = id;
            Pattern = pattern ?? string.Empty;
            Regions = Clone(regions);
            SolutionColumns = (int[])solutionColumns.Clone();
            ColorMap = (int[])colorMap.Clone();
        }

        public int Id { get; }
        public string Pattern { get; }
        public int[][] Regions { get; }
        public int[] SolutionColumns { get; }
        public int[] ColorMap { get; }

        public static bool TryLoadFromBank(out TutorialPuzzle puzzle)
        {
            return TryFind(BankData.GetSpecialLevels(), out puzzle);
        }

        internal static bool TryFind(
            IReadOnlyList<LevelEntry> entries,
            out TutorialPuzzle puzzle)
        {
            puzzle = null;
            if (entries == null) return false;
            foreach (LevelEntry entry in entries)
            {
                if (entry == null || entry.Size != SourceSize ||
                    !string.Equals(entry.Pattern, "guide", StringComparison.Ordinal))
                    continue;
                puzzle = new TutorialPuzzle(
                    entry.BankIndex,
                    entry.Pattern,
                    entry.RegionMap,
                    entry.Solution,
                    entry.ColorMap);
                return true;
            }
            return false;
        }

        private static int[][] Clone(int[][] source)
        {
            var result = new int[source.Length][];
            for (int row = 0; row < source.Length; row++)
                result[row] = (int[])source[row].Clone();
            return result;
        }
    }
}
