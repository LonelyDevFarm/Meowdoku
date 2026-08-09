using System;
using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Gameplay.Input;
using UnityEngine;

namespace Meowdoku.Gameplay
{
    public readonly struct HintPreviewCell
    {
        public HintPreviewCell(Vector2Int position, float delaySeconds)
        {
            Position = position;
            DelaySeconds = delaySeconds;
        }

        public Vector2Int Position { get; }
        public float DelaySeconds { get; }
    }

    /// <summary>
    /// View contract extracted from HintOverlay and _build_hint_highlights.
    /// Localization stays key-based until the source translation table is ported.
    /// </summary>
    public sealed class GameplayHintPresentationData
    {
        public string Strategy { get; private set; } = string.Empty;
        public string StrategyLabel { get; private set; } = "R1";
        public string DescriptionKey { get; private set; } = string.Empty;
        public bool WrongMark { get; private set; }
        public bool HasChainDetail { get; private set; }
        public Vector2Int KeyCell { get; private set; } = new Vector2Int(-1, -1);
        public Vector2Int CatCell { get; private set; } = new Vector2Int(-1, -1);
        public IReadOnlyList<Vector2Int> HighlightCells { get; private set; } =
            Array.Empty<Vector2Int>();
        public IReadOnlyList<HintPreviewCell> MarkPreviews { get; private set; } =
            Array.Empty<HintPreviewCell>();
        public HintChainDetail Chain { get; private set; }

        public static GameplayHintPresentationData Build(
            SessionHintRequest request,
            int size,
            int[][] regions,
            IBoardStateReader board)
        {
            if (request == null || !request.Found)
                return null;
            if (request.WrongMark)
            {
                return new GameplayHintPresentationData
                {
                    WrongMark = true,
                    DescriptionKey = "HINT_WRONG_MARK",
                    KeyCell = request.WrongMarkCell,
                    HighlightCells = new[] { request.WrongMarkCell }
                };
            }

            HintResult hint = request.Hint;
            if (hint == null) return null;
            string strategy = hint.Strategy ?? string.Empty;
            var highlights = new List<Vector2Int>();
            IReadOnlyList<Vector2Int> sourceHighlights = hint.UnitCells.Count > 0
                ? hint.UnitCells
                : hint.HighlightCells;
            AddUnique(highlights, sourceHighlights);
            if (hint.Cell.x >= 0) AddUnique(highlights, hint.Cell);
            if (hint.CatCell.x >= 0) AddUnique(highlights, hint.CatCell);

            var previews = new List<HintPreviewCell>();
            if (strategy == "R1_mark")
                AddPreviews(previews, hint.UnitCells, 0.06f);
            else if (strategy == "R2")
                AddPreviews(previews, R2Targets(hint, size, regions, board), 0.1f);
            else if (strategy == "R3" || strategy == "R4")
                AddPreviews(previews, R3Targets(hint, size, regions, board), 0.06f);

            return new GameplayHintPresentationData
            {
                Strategy = strategy,
                StrategyLabel = ResolveStrategyLabel(strategy),
                DescriptionKey = ResolveDescriptionKey(strategy, hint.UnitType),
                KeyCell = hint.Cell,
                CatCell = hint.CatCell,
                HighlightCells = highlights,
                MarkPreviews = previews,
                Chain = hint.Chain,
                HasChainDetail = (strategy == "R4_chain" || strategy == "R5_chain") &&
                                 hint.Chain != null && hint.Chain.Steps.Count > 0
            };
        }

        private static string ResolveStrategyLabel(string strategy)
        {
            if (strategy.StartsWith("R5", StringComparison.Ordinal)) return "R5";
            if (strategy.StartsWith("R4", StringComparison.Ordinal)) return "R4";
            if (strategy == "R3") return "R3";
            if (strategy == "R2") return "R2";
            return "R1";
        }

        private static string ResolveDescriptionKey(string strategy, string unitType)
        {
            switch (strategy)
            {
                case "R1_mark": return "HINT_R1_MARK";
                case "R2": return "HINT_REGION_CONSTRAINT";
                case "R3": return "HINT_SET_LOCKING";
                case "R4": return "HINT_LARGE_SET_LOCKING";
                case "R4_chain":
                case "R5_chain": return "HINT_CONTRADICTION";
                default: return unitType == "full_line"
                    ? "HINT_INTERSECTION"
                    : "HINT_ONLY_ONE_CELL";
            }
        }

        private static List<Vector2Int> R2Targets(
            HintResult hint,
            int size,
            int[][] regions,
            IBoardStateReader board)
        {
            var result = new List<Vector2Int>();
            if (regions == null || board == null) return result;
            if (hint.Mode == "r2a_row")
            {
                for (int column = 0; column < size; column++)
                    TryAddBlank(result, hint.Row, column,
                        regions[hint.Row][column] != hint.Region, board);
            }
            else if (hint.Mode == "r2a_col")
            {
                for (int row = 0; row < size; row++)
                    TryAddBlank(result, row, hint.Column,
                        regions[row][hint.Column] != hint.Region, board);
            }
            else if (hint.Mode == "r2b_row" || hint.Mode == "r2b_col")
            {
                for (int row = 0; row < size; row++)
                for (int column = 0; column < size; column++)
                {
                    bool outsideLockedUnit = hint.Mode == "r2b_row"
                        ? row != hint.Row
                        : column != hint.Column;
                    TryAddBlank(result, row, column,
                        regions[row][column] == hint.Region && outsideLockedUnit, board);
                }
            }
            result.Sort(CompareCells);
            return result;
        }

        private static List<Vector2Int> R3Targets(
            HintResult hint,
            int size,
            int[][] regions,
            IBoardStateReader board)
        {
            var result = new List<Vector2Int>();
            if (regions == null || board == null) return result;
            var lockedRegions = new HashSet<int>(hint.Regions);
            for (int index = 0; index < hint.LockedRows.Count; index++)
            {
                int row = hint.LockedRows[index];
                for (int column = 0; column < size; column++)
                    TryAddBlank(result, row, column,
                        !lockedRegions.Contains(regions[row][column]), board);
            }
            for (int index = 0; index < hint.LockedColumns.Count; index++)
            {
                int column = hint.LockedColumns[index];
                for (int row = 0; row < size; row++)
                    TryAddBlank(result, row, column,
                        !lockedRegions.Contains(regions[row][column]), board);
            }
            return result;
        }

        private static void TryAddBlank(
            List<Vector2Int> result,
            int row,
            int column,
            bool condition,
            IBoardStateReader board)
        {
            if (!condition || !CellState.IsBlank(board.GetCellState(row, column))) return;
            AddUnique(result, new Vector2Int(row, column));
        }

        private static void AddPreviews(
            List<HintPreviewCell> destination,
            IReadOnlyList<Vector2Int> cells,
            float interval)
        {
            for (int index = 0; index < cells.Count; index++)
                destination.Add(new HintPreviewCell(cells[index], index * interval));
        }

        private static void AddUnique(List<Vector2Int> destination, IReadOnlyList<Vector2Int> cells)
        {
            for (int index = 0; index < cells.Count; index++)
                AddUnique(destination, cells[index]);
        }

        private static void AddUnique(List<Vector2Int> destination, Vector2Int cell)
        {
            if (!destination.Contains(cell)) destination.Add(cell);
        }

        private static int CompareCells(Vector2Int left, Vector2Int right)
        {
            int row = left.x.CompareTo(right.x);
            return row != 0 ? row : left.y.CompareTo(right.y);
        }
    }
}
