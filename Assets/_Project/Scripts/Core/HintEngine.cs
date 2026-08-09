using System;
using System.Collections.Generic;
using UnityEngine;

namespace Meowdoku.Core
{
    public sealed class HintResult
    {
        public bool Found { get; internal set; }
        public string Strategy { get; internal set; } = string.Empty;
        public Vector2Int Cell { get; internal set; } = new Vector2Int(-1, -1);
        public Vector2Int CatCell { get; internal set; } = new Vector2Int(-1, -1);
        public string UnitType { get; internal set; } = string.Empty;
        public int UnitIndex { get; internal set; } = -1;
        public string Mode { get; internal set; } = string.Empty;
        public int Region { get; internal set; } = -1;
        public int Row { get; internal set; } = -1;
        public int Column { get; internal set; } = -1;
        public List<Vector2Int> UnitCells { get; } = new List<Vector2Int>();
        public List<Vector2Int> HighlightCells { get; } = new List<Vector2Int>();
        public List<int> Regions { get; } = new List<int>();
        public List<int> LockedRows { get; } = new List<int>();
        public List<int> LockedColumns { get; } = new List<int>();
        public HintChainDetail Chain { get; internal set; }

        internal static HintResult Missing() { return new HintResult(); }
    }

    public sealed class HintChainDetail
    {
        public int Depth { get; internal set; }
        public string ContradictionType { get; internal set; } = string.Empty;
        public int ContradictionIndex { get; internal set; } = -1;
        public List<Vector2Int> Steps { get; } = new List<Vector2Int>();
    }

    /// <summary>Pure port of gameplay/core/hint_engine.gd.</summary>
    public static class HintEngine
    {
        public static HintResult FindR1Hint(CellStateType[][] board, int size, int[][] regions)
        {
            BuildPlaced(board, size, regions, out bool[] rowPiece, out bool[] colPiece, out bool[] regPiece);

            // Preserve the source's special full-line region check before ordinary singles.
            for (int row = 0; row < size; row++)
            {
                if (rowPiece[row]) continue;
                int rowRegion = regions[row][0];
                bool rowUniform = true;
                for (int column = 1; column < size; column++)
                {
                    if (regions[row][column] == rowRegion) continue;
                    rowUniform = false;
                    break;
                }
                if (!rowUniform || regPiece[rowRegion]) continue;

                for (int column = 0; column < size; column++)
                {
                    if (!CanPlace(row, column, board, size, regions, rowPiece, colPiece, regPiece))
                        continue;
                    bool columnUniform = true;
                    for (int otherRow = 0; otherRow < size; otherRow++)
                    {
                        if (regions[otherRow][column] == rowRegion) continue;
                        columnUniform = false;
                        break;
                    }
                    if (!columnUniform) continue;

                    var result = new HintResult
                    {
                        Found = true,
                        Cell = new Vector2Int(row, column),
                        UnitType = "full_line",
                        UnitIndex = rowRegion
                    };
                    for (int r = 0; r < size; r++)
                        for (int c = 0; c < size; c++)
                            if (regions[r][c] == rowRegion)
                                result.UnitCells.Add(new Vector2Int(r, c));
                    return result;
                }
            }

            for (int row = 0; row < size; row++)
            {
                if (rowPiece[row]) continue;
                var candidates = new List<Vector2Int>();
                for (int column = 0; column < size; column++)
                    if (CanPlace(row, column, board, size, regions, rowPiece, colPiece, regPiece))
                        candidates.Add(new Vector2Int(row, column));
                if (candidates.Count == 1)
                    return Single(candidates[0], "row", row, RowCells(row, size));
            }

            for (int column = 0; column < size; column++)
            {
                if (colPiece[column]) continue;
                var candidates = new List<Vector2Int>();
                for (int row = 0; row < size; row++)
                    if (CanPlace(row, column, board, size, regions, rowPiece, colPiece, regPiece))
                        candidates.Add(new Vector2Int(row, column));
                if (candidates.Count == 1)
                    return Single(candidates[0], "col", column, ColumnCells(column, size));
            }

            for (int region = 0; region < size; region++)
            {
                if (regPiece[region]) continue;
                var candidates = new List<Vector2Int>();
                var unit = new List<Vector2Int>();
                for (int row = 0; row < size; row++)
                {
                    for (int column = 0; column < size; column++)
                    {
                        if (regions[row][column] != region) continue;
                        unit.Add(new Vector2Int(row, column));
                        if (CanPlace(row, column, board, size, regions, rowPiece, colPiece, regPiece))
                            candidates.Add(new Vector2Int(row, column));
                    }
                }
                if (candidates.Count == 1)
                    return Single(candidates[0], "region", region, unit);
            }
            return HintResult.Missing();
        }

        public static HintResult FindMarkHint(CellStateType[][] board, int size, int[][] regions)
        {
            Validate(board, size, regions);
            for (int row = 0; row < size; row++)
            {
                for (int column = 0; column < size; column++)
                {
                    if (board[row][column] != CellStateType.CAT) continue;
                    var marks = new List<Vector2Int>();
                    for (int c = 0; c < size; c++)
                        if (c != column && board[row][c] == CellStateType.EMPTY)
                            marks.Add(new Vector2Int(row, c));
                    for (int r = 0; r < size; r++)
                        if (r != row && board[r][column] == CellStateType.EMPTY)
                            marks.Add(new Vector2Int(r, column));
                    for (int dr = -1; dr <= 1; dr++)
                    {
                        for (int dc = -1; dc <= 1; dc++)
                        {
                            if (dr == 0 && dc == 0) continue;
                            int r = row + dr;
                            int c = column + dc;
                            var cell = new Vector2Int(r, c);
                            if (Inside(r, c, size) && board[r][c] == CellStateType.EMPTY && !marks.Contains(cell))
                                marks.Add(cell);
                        }
                    }
                    if (marks.Count == 0) continue;
                    var result = new HintResult
                    {
                        Found = true,
                        Strategy = "R1_mark",
                        Cell = marks[0],
                        CatCell = new Vector2Int(row, column)
                    };
                    result.UnitCells.AddRange(marks);
                    return result;
                }
            }
            return HintResult.Missing();
        }

        public static HintResult FindR2Hint(CellStateType[][] board, int size, int[][] regions)
        {
            BuildPlaced(board, size, regions, out bool[] rowPiece, out bool[] colPiece, out bool[] regPiece);
            var regionCandidates = new List<Vector2Int>[size];
            for (int region = 0; region < size; region++)
            {
                regionCandidates[region] = new List<Vector2Int>();
                if (regPiece[region]) continue;
                for (int row = 0; row < size; row++)
                    for (int column = 0; column < size; column++)
                        if (regions[row][column] == region && board[row][column] == CellStateType.EMPTY &&
                            CanPlace(row, column, board, size, regions, rowPiece, colPiece, regPiece))
                            regionCandidates[region].Add(new Vector2Int(row, column));
            }

            for (int region = 0; region < size; region++)
            {
                List<Vector2Int> candidates = regionCandidates[region];
                if (regPiece[region] || candidates.Count <= 1) continue;
                var rows = new HashSet<int>();
                for (int i = 0; i < candidates.Count; i++) rows.Add(candidates[i].x);
                if (rows.Count != 1) continue;
                int row = candidates[0].x;
                bool hasNew = false;
                for (int column = 0; column < size; column++)
                    if (regions[row][column] != region && board[row][column] == CellStateType.EMPTY && !rowPiece[row])
                    { hasNew = true; break; }
                if (hasNew) return R2("r2a_row", region, row, -1, candidates);
            }

            for (int region = 0; region < size; region++)
            {
                List<Vector2Int> candidates = regionCandidates[region];
                if (regPiece[region] || candidates.Count <= 1) continue;
                var columns = new HashSet<int>();
                for (int i = 0; i < candidates.Count; i++) columns.Add(candidates[i].y);
                if (columns.Count != 1) continue;
                int column = candidates[0].y;
                bool hasNew = false;
                for (int row = 0; row < size; row++)
                    if (regions[row][column] != region && board[row][column] == CellStateType.EMPTY && !colPiece[column])
                    { hasNew = true; break; }
                if (hasNew) return R2("r2a_col", region, -1, column, candidates);
            }

            for (int row = 0; row < size; row++)
            {
                if (rowPiece[row]) continue;
                var candidates = new List<Vector2Int>();
                var candidateRegions = new HashSet<int>();
                for (int column = 0; column < size; column++)
                {
                    if (board[row][column] != CellStateType.EMPTY ||
                        !CanPlace(row, column, board, size, regions, rowPiece, colPiece, regPiece)) continue;
                    candidates.Add(new Vector2Int(row, column));
                    candidateRegions.Add(regions[row][column]);
                }
                if (candidates.Count <= 1 || candidateRegions.Count != 1) continue;
                int region = regions[candidates[0].x][candidates[0].y];
                bool hasNew = false;
                for (int i = 0; i < regionCandidates[region].Count; i++)
                    if (regionCandidates[region][i].x != row) { hasNew = true; break; }
                if (hasNew) return R2("r2b_row", region, row, -1, candidates);
            }

            for (int column = 0; column < size; column++)
            {
                if (colPiece[column]) continue;
                var candidates = new List<Vector2Int>();
                var candidateRegions = new HashSet<int>();
                for (int row = 0; row < size; row++)
                {
                    if (board[row][column] != CellStateType.EMPTY ||
                        !CanPlace(row, column, board, size, regions, rowPiece, colPiece, regPiece)) continue;
                    candidates.Add(new Vector2Int(row, column));
                    candidateRegions.Add(regions[row][column]);
                }
                if (candidates.Count <= 1 || candidateRegions.Count != 1) continue;
                int region = regions[candidates[0].x][candidates[0].y];
                bool hasNew = false;
                for (int i = 0; i < regionCandidates[region].Count; i++)
                    if (regionCandidates[region][i].y != column) { hasNew = true; break; }
                if (hasNew) return R2("r2b_col", region, -1, column, candidates);
            }
            return HintResult.Missing();
        }

        public static HintResult FindR3R4Hint(CellStateType[][] board, int size, int[][] regions)
        {
            BuildPlaced(board, size, regions, out bool[] rowPiece, out bool[] colPiece, out bool[] regPiece);
            var unplaced = new List<int>();
            var regionRows = new HashSet<int>[size];
            var regionColumns = new HashSet<int>[size];
            for (int region = 0; region < size; region++)
            {
                regionRows[region] = new HashSet<int>();
                regionColumns[region] = new HashSet<int>();
                if (regPiece[region]) continue;
                unplaced.Add(region);
                for (int row = 0; row < size; row++)
                    for (int column = 0; column < size; column++)
                        if (regions[row][column] == region &&
                            CanPlace(row, column, board, size, regions, rowPiece, colPiece, regPiece))
                        {
                            regionRows[region].Add(row);
                            regionColumns[region].Add(column);
                        }
            }

            int maxK = Math.Min(unplaced.Count - 1, 6);
            for (int k = 2; k <= maxK; k++)
            {
                List<List<int>> subsets = GenerateSubsets(unplaced, k);
                for (int i = 0; i < subsets.Count; i++)
                {
                    List<int> subset = subsets[i];
                    var regionSet = new HashSet<int>(subset);
                    var allRows = new List<int>();
                    bool validRows = true;
                    for (int j = 0; j < subset.Count; j++)
                    {
                        int region = subset[j];
                        if (regionRows[region].Count > k) { validRows = false; break; }
                        AddOrderedUnique(allRows, regionRows[region]);
                    }
                    if (validRows && allRows.Count == k && HasNewRowMark(allRows, regionSet, board, regions, rowPiece, size))
                        return SubsetResult(k, subset, allRows, null, board, size, regions, rowPiece, colPiece, regPiece);

                    var allColumns = new List<int>();
                    bool validColumns = true;
                    for (int j = 0; j < subset.Count; j++)
                    {
                        int region = subset[j];
                        if (regionColumns[region].Count > k) { validColumns = false; break; }
                        AddOrderedUnique(allColumns, regionColumns[region]);
                    }
                    if (validColumns && allColumns.Count == k && HasNewColumnMark(allColumns, regionSet, board, regions, colPiece, size))
                        return SubsetResult(k, subset, null, allColumns, board, size, regions, rowPiece, colPiece, regPiece);
                }
            }
            return HintResult.Missing();
        }

        public static HintResult FindChainHint(CellStateType[][] board, int size, int[][] regions)
        {
            Validate(board, size, regions);
            ChainState baseline = BuildChainState(board, size, regions);
            int bestDepth = int.MaxValue;
            int bestRow = -1;
            int bestColumn = -1;
            HintChainDetail bestDetail = null;
            for (int row = 0; row < size; row++)
            {
                if (baseline.Placed[row] != -1) continue;
                for (int column = 0; column < size; column++)
                {
                    if (!baseline.Candidates[row][column]) continue;
                    HintChainDetail detail = TryChainContradiction(row, column, size, regions, baseline);
                    if (detail == null || detail.Depth >= bestDepth) continue;
                    bestDepth = detail.Depth;
                    bestRow = row;
                    bestColumn = column;
                    bestDetail = detail;
                }
            }
            if (bestRow < 0) return HintResult.Missing();
            var result = new HintResult
            {
                Found = true,
                Strategy = bestDepth <= 2 ? "R4_chain" : "R5_chain",
                Cell = new Vector2Int(bestRow, bestColumn),
                Chain = bestDetail
            };
            result.UnitCells.Add(result.Cell);
            return result;
        }

        public static Dictionary<Vector2Int, int> ComputeCellRanks(
            CellStateType[][] board,
            int size,
            int[][] regions,
            bool[][] solution,
            int fallbackStrategy = 4)
        {
            ValidateSolution(solution, size);
            CellStateType[][] work = Clone(board, size, regions);
            var ranks = new Dictionary<Vector2Int, int>();
            for (int row = 0; row < size; row++)
                for (int column = 0; column < size; column++)
                    if (work[row][column] == CellStateType.CAT && solution[row][column])
                        ranks[new Vector2Int(row, column)] = 1;

            int currentMax = 1;
            while (true)
            {
                HintResult hint = FindMarkHint(work, size, regions);
                if (hint.Found) { ApplyMarkHint(work, hint); continue; }
                hint = FindR1Hint(work, size, regions);
                if (hint.Found)
                {
                    work[hint.Cell.x][hint.Cell.y] = CellStateType.CAT;
                    if (solution[hint.Cell.x][hint.Cell.y]) ranks[hint.Cell] = currentMax;
                    currentMax = 1;
                    continue;
                }
                hint = FindR2Hint(work, size, regions);
                if (hint.Found) { ApplyR2Marks(work, hint, size, regions); currentMax = Math.Max(currentMax, 2); continue; }
                hint = FindR3R4Hint(work, size, regions);
                if (hint.Found && hint.Strategy == "R3")
                { ApplyR3Marks(work, hint, size, regions); currentMax = Math.Max(currentMax, 3); continue; }
                break;
            }
            for (int row = 0; row < size; row++)
                for (int column = 0; column < size; column++)
                {
                    var cell = new Vector2Int(row, column);
                    if (solution[row][column] && !ranks.ContainsKey(cell)) ranks[cell] = fallbackStrategy;
                }
            return ranks;
        }

        public static Dictionary<Vector2Int, bool> ComputeR4PlusCells(
            CellStateType[][] board, int size, int[][] regions, bool[][] solution)
        {
            ValidateSolution(solution, size);
            CellStateType[][] work = Clone(board, size, regions);
            while (true)
            {
                HintResult hint = FindMarkHint(work, size, regions);
                if (hint.Found) { ApplyMarkHint(work, hint); continue; }
                hint = FindR1Hint(work, size, regions);
                if (hint.Found) { work[hint.Cell.x][hint.Cell.y] = CellStateType.CAT; continue; }
                hint = FindR2Hint(work, size, regions);
                if (hint.Found) { ApplyR2Marks(work, hint, size, regions); continue; }
                hint = FindR3R4Hint(work, size, regions);
                if (hint.Found && hint.Strategy == "R3") { ApplyR3Marks(work, hint, size, regions); continue; }
                break;
            }
            var result = new Dictionary<Vector2Int, bool>();
            for (int row = 0; row < size; row++)
                for (int column = 0; column < size; column++)
                    if (solution[row][column] && work[row][column] != CellStateType.CAT)
                        result[new Vector2Int(row, column)] = true;
            return result;
        }

        public static bool[][] SolutionMatrix(int size, int[] solutionColumns)
        {
            if (solutionColumns == null || solutionColumns.Length != size)
                throw new ArgumentException("Solution must contain one column per row.", nameof(solutionColumns));
            var result = new bool[size][];
            for (int row = 0; row < size; row++)
            {
                result[row] = new bool[size];
                if (!Inside(row, solutionColumns[row], size)) throw new ArgumentOutOfRangeException(nameof(solutionColumns));
                result[row][solutionColumns[row]] = true;
            }
            return result;
        }

        private static HintResult Single(Vector2Int cell, string type, int index, List<Vector2Int> unit)
        {
            var result = new HintResult { Found = true, Cell = cell, UnitType = type, UnitIndex = index };
            result.UnitCells.AddRange(unit);
            return result;
        }

        private static HintResult R2(string mode, int region, int row, int column, List<Vector2Int> highlights)
        {
            var result = new HintResult
            { Found = true, Strategy = "R2", Mode = mode, Region = region, Row = row, Column = column };
            result.HighlightCells.AddRange(highlights);
            return result;
        }

        private static HintResult SubsetResult(
            int k, List<int> subset, List<int> rows, List<int> columns,
            CellStateType[][] board, int size, int[][] regions,
            bool[] rowPiece, bool[] colPiece, bool[] regPiece)
        {
            var result = new HintResult { Found = true, Strategy = k <= 3 ? "R3" : "R4" };
            result.Regions.AddRange(subset);
            if (rows != null) result.LockedRows.AddRange(rows);
            if (columns != null) result.LockedColumns.AddRange(columns);
            for (int i = 0; i < subset.Count; i++)
            {
                int region = subset[i];
                for (int row = 0; row < size; row++)
                    for (int column = 0; column < size; column++)
                        if (regions[row][column] == region &&
                            CanPlace(row, column, board, size, regions, rowPiece, colPiece, regPiece))
                            result.HighlightCells.Add(new Vector2Int(row, column));
            }
            return result;
        }

        private static bool HasNewRowMark(List<int> rows, HashSet<int> regionSet,
            CellStateType[][] board, int[][] regions, bool[] rowPiece, int size)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                int row = rows[i];
                if (rowPiece[row]) continue;
                for (int column = 0; column < size; column++)
                    if (!regionSet.Contains(regions[row][column]) && board[row][column] == CellStateType.EMPTY)
                        return true;
            }
            return false;
        }

        private static bool HasNewColumnMark(List<int> columns, HashSet<int> regionSet,
            CellStateType[][] board, int[][] regions, bool[] colPiece, int size)
        {
            for (int i = 0; i < columns.Count; i++)
            {
                int column = columns[i];
                if (colPiece[column]) continue;
                for (int row = 0; row < size; row++)
                    if (!regionSet.Contains(regions[row][column]) && board[row][column] == CellStateType.EMPTY)
                        return true;
            }
            return false;
        }

        private static void AddOrderedUnique(List<int> target, HashSet<int> values)
        {
            foreach (int value in values) if (!target.Contains(value)) target.Add(value);
        }

        private static List<List<int>> GenerateSubsets(List<int> values, int count)
        {
            var result = new List<List<int>>();
            GenerateSubset(values, count, 0, new List<int>(), result);
            return result;
        }

        private static void GenerateSubset(List<int> values, int count, int start,
            List<int> current, List<List<int>> result)
        {
            if (current.Count == count) { result.Add(new List<int>(current)); return; }
            for (int i = start; i < values.Count; i++)
            {
                current.Add(values[i]);
                GenerateSubset(values, count, i + 1, current, result);
                current.RemoveAt(current.Count - 1);
            }
        }

        private sealed class ChainState
        {
            public bool[][] Candidates;
            public int[] Placed;
            public bool[] ColumnPlaced;
            public bool[] RegionPlaced;

            public ChainState Clone()
            {
                var candidates = new bool[Candidates.Length][];
                for (int row = 0; row < Candidates.Length; row++)
                    candidates[row] = (bool[])Candidates[row].Clone();
                return new ChainState
                {
                    Candidates = candidates,
                    Placed = (int[])Placed.Clone(),
                    ColumnPlaced = (bool[])ColumnPlaced.Clone(),
                    RegionPlaced = (bool[])RegionPlaced.Clone()
                };
            }
        }

        private static ChainState BuildChainState(CellStateType[][] board, int size, int[][] regions)
        {
            var state = new ChainState
            {
                Candidates = new bool[size][],
                Placed = new int[size],
                ColumnPlaced = new bool[size],
                RegionPlaced = new bool[size]
            };
            for (int row = 0; row < size; row++)
            {
                state.Candidates[row] = new bool[size];
                for (int column = 0; column < size; column++) state.Candidates[row][column] = true;
                state.Placed[row] = -1;
            }
            for (int row = 0; row < size; row++)
                for (int column = 0; column < size; column++)
                    if (board[row][column] == CellStateType.CAT)
                        ChainPlace(row, column, size, regions, state);
            for (int row = 0; row < size; row++)
                for (int column = 0; column < size; column++)
                    if (board[row][column] == CellStateType.MARK)
                        state.Candidates[row][column] = false;
            return state;
        }

        private static void ChainPlace(int row, int column, int size, int[][] regions, ChainState state)
        {
            state.Placed[row] = column;
            state.ColumnPlaced[column] = true;
            state.RegionPlaced[regions[row][column]] = true;
            for (int c = 0; c < size; c++) if (c != column) state.Candidates[row][c] = false;
            for (int r = 0; r < size; r++) if (r != row) state.Candidates[r][column] = false;
            for (int dr = -1; dr <= 1; dr++)
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0) continue;
                    int r = row + dr;
                    int c = column + dc;
                    if (Inside(r, c, size)) state.Candidates[r][c] = false;
                }
            int region = regions[row][column];
            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                    if (regions[r][c] == region && (r != row || c != column))
                        state.Candidates[r][c] = false;
        }

        private static HintChainDetail TryChainContradiction(
            int initialRow, int initialColumn, int size, int[][] regions, ChainState baseline)
        {
            ChainState state = baseline.Clone();
            ChainPlace(initialRow, initialColumn, size, regions, state);
            var steps = new List<Vector2Int>();
            int extra = 0;
            bool progress = true;
            while (progress)
            {
                progress = false;
                for (int row = 0; row < size; row++)
                    if (state.Placed[row] == -1 && CountRow(state.Candidates, row, size) == 0)
                        return ChainDetail(extra, steps, "row", row);
                for (int column = 0; column < size; column++)
                    if (!state.ColumnPlaced[column] && CountColumn(state.Candidates, column, size) == 0)
                        return ChainDetail(extra, steps, "col", column);
                for (int region = 0; region < size; region++)
                    if (!state.RegionPlaced[region] && CountRegion(state.Candidates, regions, region, size) == 0)
                        return ChainDetail(extra, steps, "region", region);

                for (int row = 0; row < size; row++)
                {
                    if (state.Placed[row] != -1) continue;
                    int only = OnlyInRow(state.Candidates, row, size);
                    if (only < 0) continue;
                    ChainPlace(row, only, size, regions, state);
                    steps.Add(new Vector2Int(row, only)); extra++; progress = true; break;
                }
                if (progress) continue;
                for (int column = 0; column < size; column++)
                {
                    if (state.ColumnPlaced[column]) continue;
                    int only = OnlyInColumn(state.Candidates, column, size);
                    if (only < 0) continue;
                    ChainPlace(only, column, size, regions, state);
                    steps.Add(new Vector2Int(only, column)); extra++; progress = true; break;
                }
                if (progress) continue;
                for (int region = 0; region < size; region++)
                {
                    if (state.RegionPlaced[region]) continue;
                    Vector2Int only = OnlyInRegion(state.Candidates, regions, region, size);
                    if (only.x < 0) continue;
                    ChainPlace(only.x, only.y, size, regions, state);
                    steps.Add(only); extra++; progress = true; break;
                }
            }
            return null;
        }

        private static HintChainDetail ChainDetail(int depth, List<Vector2Int> steps, string type, int index)
        {
            var detail = new HintChainDetail
            { Depth = depth, ContradictionType = type, ContradictionIndex = index };
            detail.Steps.AddRange(steps);
            return detail;
        }

        private static int CountRow(bool[][] candidates, int row, int size)
        { int count = 0; for (int c = 0; c < size; c++) if (candidates[row][c]) count++; return count; }
        private static int CountColumn(bool[][] candidates, int column, int size)
        { int count = 0; for (int r = 0; r < size; r++) if (candidates[r][column]) count++; return count; }
        private static int CountRegion(bool[][] candidates, int[][] regions, int region, int size)
        { int count = 0; for (int r = 0; r < size; r++) for (int c = 0; c < size; c++) if (regions[r][c] == region && candidates[r][c]) count++; return count; }
        private static int OnlyInRow(bool[][] candidates, int row, int size)
        { int found = -1; for (int c = 0; c < size; c++) if (candidates[row][c]) { if (found >= 0) return -1; found = c; } return found; }
        private static int OnlyInColumn(bool[][] candidates, int column, int size)
        { int found = -1; for (int r = 0; r < size; r++) if (candidates[r][column]) { if (found >= 0) return -1; found = r; } return found; }
        private static Vector2Int OnlyInRegion(bool[][] candidates, int[][] regions, int region, int size)
        {
            var found = new Vector2Int(-1, -1);
            for (int r = 0; r < size; r++) for (int c = 0; c < size; c++)
                if (regions[r][c] == region && candidates[r][c])
                { if (found.x >= 0) return new Vector2Int(-1, -1); found = new Vector2Int(r, c); }
            return found;
        }

        private static void ApplyMarkHint(CellStateType[][] board, HintResult hint)
        {
            for (int i = 0; i < hint.UnitCells.Count; i++)
            {
                Vector2Int cell = hint.UnitCells[i];
                if (board[cell.x][cell.y] == CellStateType.EMPTY) board[cell.x][cell.y] = CellStateType.MARK;
            }
        }

        private static void ApplyR2Marks(CellStateType[][] board, HintResult hint, int size, int[][] regions)
        {
            if (hint.Mode == "r2a_row")
            {
                for (int c = 0; c < size; c++) if (regions[hint.Row][c] != hint.Region && board[hint.Row][c] == CellStateType.EMPTY) board[hint.Row][c] = CellStateType.MARK;
            }
            else if (hint.Mode == "r2a_col")
            {
                for (int r = 0; r < size; r++) if (regions[r][hint.Column] != hint.Region && board[r][hint.Column] == CellStateType.EMPTY) board[r][hint.Column] = CellStateType.MARK;
            }
            else if (hint.Mode == "r2b_row")
            {
                for (int r = 0; r < size; r++) for (int c = 0; c < size; c++) if (regions[r][c] == hint.Region && r != hint.Row && board[r][c] == CellStateType.EMPTY) board[r][c] = CellStateType.MARK;
            }
            else if (hint.Mode == "r2b_col")
            {
                for (int r = 0; r < size; r++) for (int c = 0; c < size; c++) if (regions[r][c] == hint.Region && c != hint.Column && board[r][c] == CellStateType.EMPTY) board[r][c] = CellStateType.MARK;
            }
        }

        private static void ApplyR3Marks(CellStateType[][] board, HintResult hint, int size, int[][] regions)
        {
            var regionSet = new HashSet<int>(hint.Regions);
            for (int i = 0; i < hint.LockedRows.Count; i++)
            {
                int row = hint.LockedRows[i];
                for (int c = 0; c < size; c++) if (!regionSet.Contains(regions[row][c]) && board[row][c] == CellStateType.EMPTY) board[row][c] = CellStateType.MARK;
            }
            for (int i = 0; i < hint.LockedColumns.Count; i++)
            {
                int column = hint.LockedColumns[i];
                for (int r = 0; r < size; r++) if (!regionSet.Contains(regions[r][column]) && board[r][column] == CellStateType.EMPTY) board[r][column] = CellStateType.MARK;
            }
        }

        private static void BuildPlaced(CellStateType[][] board, int size, int[][] regions,
            out bool[] rowPiece, out bool[] colPiece, out bool[] regPiece)
        {
            Validate(board, size, regions);
            rowPiece = new bool[size]; colPiece = new bool[size]; regPiece = new bool[size];
            for (int row = 0; row < size; row++) for (int column = 0; column < size; column++)
                if (board[row][column] == CellStateType.CAT)
                { rowPiece[row] = true; colPiece[column] = true; regPiece[regions[row][column]] = true; }
        }

        private static bool CanPlace(int row, int column, CellStateType[][] board, int size,
            int[][] regions, bool[] rowPiece, bool[] colPiece, bool[] regPiece)
        {
            if (board[row][column] != CellStateType.EMPTY || rowPiece[row] || colPiece[column] || regPiece[regions[row][column]]) return false;
            for (int dr = -1; dr <= 1; dr++) for (int dc = -1; dc <= 1; dc++)
            {
                if (dr == 0 && dc == 0) continue;
                int r = row + dr; int c = column + dc;
                if (Inside(r, c, size) && board[r][c] == CellStateType.CAT) return false;
            }
            return true;
        }

        private static List<Vector2Int> RowCells(int row, int size)
        { var result = new List<Vector2Int>(); for (int c = 0; c < size; c++) result.Add(new Vector2Int(row, c)); return result; }
        private static List<Vector2Int> ColumnCells(int column, int size)
        { var result = new List<Vector2Int>(); for (int r = 0; r < size; r++) result.Add(new Vector2Int(r, column)); return result; }

        private static CellStateType[][] Clone(CellStateType[][] board, int size, int[][] regions)
        {
            Validate(board, size, regions);
            var result = new CellStateType[size][];
            for (int row = 0; row < size; row++) result[row] = (CellStateType[])board[row].Clone();
            return result;
        }

        private static void Validate(CellStateType[][] board, int size, int[][] regions)
        {
            if (size <= 0 || board == null || board.Length != size || regions == null || regions.Length != size)
                throw new ArgumentException("Board and regions must be size-by-size arrays.");
            for (int row = 0; row < size; row++)
            {
                if (board[row] == null || board[row].Length != size || regions[row] == null || regions[row].Length != size)
                    throw new ArgumentException("Board and regions must be size-by-size arrays.");
                for (int column = 0; column < size; column++)
                    if (regions[row][column] < 0 || regions[row][column] >= size)
                        throw new ArgumentOutOfRangeException(nameof(regions));
            }
        }

        private static void ValidateSolution(bool[][] solution, int size)
        {
            if (solution == null || solution.Length != size) throw new ArgumentException("Solution must be size-by-size.", nameof(solution));
            for (int row = 0; row < size; row++) if (solution[row] == null || solution[row].Length != size) throw new ArgumentException("Solution must be size-by-size.", nameof(solution));
        }

        private static bool Inside(int row, int column, int size)
        { return row >= 0 && row < size && column >= 0 && column < size; }
    }
}
