using System;
using System.Collections.Generic;
using UnityEngine;

namespace Meowdoku.Core
{
    public sealed class BoardStateChange
    {
        public Vector2Int Position { get; }
        public CellStateType Before { get; }
        public CellStateType After { get; }

        public BoardStateChange(
            int row,
            int column,
            CellStateType before,
            CellStateType after)
        {
            Position = new Vector2Int(row, column);
            Before = before;
            After = after;
        }
    }

    /// <summary>
    /// Authoritative board state ported from the state-owning parts of board_view.gd.
    /// Rendering and animation remain the responsibility of BoardView.
    /// </summary>
    public sealed class BoardStateModel
    {
        private readonly CellStateType[][] _board;
        private readonly int[][] _regions;
        private readonly int[] _solutionColumns;

        public int Size { get; }
        public int CatCount { get; private set; }
        public int RemainingCats => Size - CatCount;

        public BoardStateModel(int size, int[][] regions, int[] solutionColumns)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
            if (!HasSquareShape(regions, size))
                throw new ArgumentException("Regions must be a non-null size-by-size array.", nameof(regions));
            if (solutionColumns == null || solutionColumns.Length != size)
                throw new ArgumentException("Solution must contain one column per row.", nameof(solutionColumns));

            Size = size;
            _regions = Clone(regions);
            _solutionColumns = (int[])solutionColumns.Clone();
            _board = new CellStateType[size][];
            for (int row = 0; row < size; row++)
            {
                if (_solutionColumns[row] < 0 || _solutionColumns[row] >= size)
                    throw new ArgumentOutOfRangeException(nameof(solutionColumns));
                _board[row] = new CellStateType[size];
            }
        }

        public CellStateType GetCellState(int row, int column)
        {
            return IsInside(row, column) ? _board[row][column] : CellStateType.EMPTY;
        }

        public bool IsSolutionCell(int row, int column)
        {
            return IsInside(row, column) && _solutionColumns[row] == column;
        }

        public bool TrySetCellState(
            int row,
            int column,
            CellStateType state,
            out IReadOnlyList<BoardStateChange> changes)
        {
            var applied = new List<BoardStateChange>();
            changes = applied;
            if (!IsInside(row, column)) return false;

            CellStateType current = _board[row][column];
            if (current == CellStateType.LOCKED_MARK) return false;
            if (current == CellStateType.CAT && state != CellStateType.CAT) return false;
            if (state == CellStateType.CAT && !IsSolutionCell(row, column)) return false;
            if (current == state) return true;

            Apply(row, column, state, applied);
            if (state == CellStateType.CAT)
                HealIntegrity(row, column, applied);
            return true;
        }

        public bool TryMarkCellError(
            int row,
            int column,
            out IReadOnlyList<BoardStateChange> changes)
        {
            var applied = new List<BoardStateChange>();
            changes = applied;
            if (!IsInside(row, column)) return false;
            if (_board[row][column] == CellStateType.LOCKED_MARK || IsSolutionCell(row, column))
                return false;
            if (_board[row][column] != CellStateType.ERROR)
                Apply(row, column, CellStateType.ERROR, applied);
            return true;
        }

        public bool RestoreCellState(
            int row,
            int column,
            CellStateType state,
            out BoardStateChange change)
        {
            change = null;
            if (!IsInside(row, column)) return false;
            CellStateType current = _board[row][column];
            if (current == state) return true;
            change = new BoardStateChange(row, column, current, state);
            _board[row][column] = state;
            if (current == CellStateType.CAT) CatCount--;
            if (state == CellStateType.CAT) CatCount++;
            return true;
        }

        public bool WouldCatConflict(int row, int column)
        {
            if (!IsInside(row, column)) return false;
            return ClassifyViolation(row, column) != QueendokuCore.Rule.None;
        }

        public QueendokuCore.Rule ClassifyViolation(int row, int column)
        {
            if (!IsInside(row, column)) return QueendokuCore.Rule.None;
            return QueendokuCore.ClassifyViolation(
                row,
                column,
                GetPlacedCats(),
                _regions);
        }

        public List<Vector2Int> FindConflictingCats(int row, int column)
        {
            if (!IsInside(row, column)) return new List<Vector2Int>();
            return QueendokuCore.FindConflictingCats(
                row,
                column,
                GetPlacedCats(),
                _regions);
        }

        public int CountCorrectCrosses()
        {
            int count = 0;
            for (int row = 0; row < Size; row++)
                for (int column = 0; column < Size; column++)
                    if (CellState.IsCross(_board[row][column]) && !IsSolutionCell(row, column))
                        count++;
            return count;
        }

        public int CountFalseCrosses()
        {
            int count = 0;
            for (int row = 0; row < Size; row++)
                for (int column = 0; column < Size; column++)
                    if (CellState.IsCross(_board[row][column]) && IsSolutionCell(row, column))
                        count++;
            return count;
        }

        public bool IsComplete()
        {
            return QueendokuCore.IsComplete(_board, Size, _regions);
        }

        public CellStateType[][] GetBoardSnapshot()
        {
            return Clone(_board);
        }

        private List<Vector2Int> GetPlacedCats()
        {
            var result = new List<Vector2Int>();
            for (int row = 0; row < Size; row++)
                for (int column = 0; column < Size; column++)
                    if (_board[row][column] == CellStateType.CAT)
                        result.Add(new Vector2Int(row, column));
            return result;
        }

        private void HealIntegrity(
            int placedRow,
            int placedColumn,
            List<BoardStateChange> changes)
        {
            for (int row = 0; row < Size; row++)
            {
                for (int column = 0; column < Size; column++)
                {
                    if (row == placedRow && column == placedColumn) continue;
                    CellStateType state = _board[row][column];
                    bool invalidCat = state == CellStateType.CAT && !IsSolutionCell(row, column);
                    bool invalidError = state == CellStateType.ERROR && IsSolutionCell(row, column);
                    if (invalidCat || invalidError)
                        Apply(row, column, CellStateType.EMPTY, changes);
                }
            }
        }

        private void Apply(
            int row,
            int column,
            CellStateType state,
            List<BoardStateChange> changes)
        {
            CellStateType before = _board[row][column];
            _board[row][column] = state;
            if (before == CellStateType.CAT) CatCount--;
            if (state == CellStateType.CAT) CatCount++;
            changes.Add(new BoardStateChange(row, column, before, state));
        }

        private bool IsInside(int row, int column)
        {
            return row >= 0 && row < Size && column >= 0 && column < Size;
        }

        private static bool HasSquareShape<T>(T[][] values, int size)
        {
            if (values == null || values.Length != size) return false;
            for (int row = 0; row < size; row++)
                if (values[row] == null || values[row].Length != size)
                    return false;
            return true;
        }

        private static T[][] Clone<T>(T[][] source)
        {
            var result = new T[source.Length][];
            for (int row = 0; row < source.Length; row++)
                result[row] = (T[])source[row].Clone();
            return result;
        }
    }

    /// <summary>
    /// Pure puzzle rules ported from queendoku_core.gd.
    /// Coordinates use Vector2Int.x as row and Vector2Int.y as column to match
    /// the original Godot implementation.
    /// </summary>
    public static class QueendokuCore
    {
        public enum Rule
        {
            None = 0,
            SameColor = 1,
            SameLine = 2,
            NoTouch = 3
        }

        public static HashSet<Vector2Int> FindConflicts(
            CellStateType[][] board,
            int size,
            int[][] regions)
        {
            ValidateBoardArguments(board, size, regions);

            var errors = new HashSet<Vector2Int>();
            var pieces = new List<Vector2Int>();

            for (int row = 0; row < size; row++)
            {
                for (int column = 0; column < size; column++)
                {
                    if (board[row][column] == CellStateType.CAT)
                    {
                        pieces.Add(new Vector2Int(row, column));
                    }
                }
            }

            for (int i = 0; i < pieces.Count; i++)
            {
                for (int j = i + 1; j < pieces.Count; j++)
                {
                    Vector2Int a = pieces[i];
                    Vector2Int b = pieces[j];
                    if (ClassifyPair(a, b, regions) == Rule.None)
                    {
                        continue;
                    }

                    errors.Add(a);
                    errors.Add(b);
                }
            }

            return errors;
        }

        public static Rule ClassifyViolation(
            int row,
            int column,
            IReadOnlyList<Vector2Int> placedCats,
            int[][] regions)
        {
            ValidateCell(row, column, regions);
            if (placedCats == null) throw new ArgumentNullException(nameof(placedCats));

            var here = new Vector2Int(row, column);
            Rule best = Rule.None;

            for (int i = 0; i < placedCats.Count; i++)
            {
                Rule violation = ClassifyPair(here, placedCats[i], regions);
                if (violation != Rule.None && (best == Rule.None || violation < best))
                {
                    best = violation;
                    if (best == Rule.SameColor)
                    {
                        return best;
                    }
                }
            }

            return best;
        }

        public static List<Vector2Int> FindConflictingCats(
            int row,
            int column,
            IReadOnlyList<Vector2Int> placedCats,
            int[][] regions)
        {
            ValidateCell(row, column, regions);
            if (placedCats == null) throw new ArgumentNullException(nameof(placedCats));

            var here = new Vector2Int(row, column);
            var result = new List<Vector2Int>();
            for (int i = 0; i < placedCats.Count; i++)
            {
                if (ClassifyPair(here, placedCats[i], regions) != Rule.None)
                {
                    result.Add(placedCats[i]);
                }
            }

            return result;
        }

        public static List<Vector2Int> CellsExcludedByCat(
            Vector2Int cat,
            int size,
            int[][] regions)
        {
            ValidateSizeAndRegions(size, regions);
            if (!IsInside(cat.x, cat.y, size))
            {
                throw new ArgumentOutOfRangeException(nameof(cat));
            }

            var result = new List<Vector2Int>();
            for (int row = 0; row < size; row++)
            {
                for (int column = 0; column < size; column++)
                {
                    if (row == cat.x && column == cat.y)
                    {
                        continue;
                    }

                    var cell = new Vector2Int(row, column);
                    if (ClassifyPair(cell, cat, regions) != Rule.None)
                    {
                        result.Add(cell);
                    }
                }
            }

            return result;
        }

        public static bool IsComplete(CellStateType[][] board, int size, int[][] regions)
        {
            ValidateBoardArguments(board, size, regions);

            int pieceCount = 0;
            for (int row = 0; row < size; row++)
            {
                for (int column = 0; column < size; column++)
                {
                    if (board[row][column] == CellStateType.CAT)
                    {
                        pieceCount++;
                    }
                }
            }

            return pieceCount == size && FindConflicts(board, size, regions).Count == 0;
        }

        public static bool ValidateSolutionEntry(int[][] regions, int[] solution, int size)
        {
            if (!HasSquareShape(regions, size) || solution == null || solution.Length != size)
            {
                return false;
            }

            var board = new CellStateType[size][];
            for (int row = 0; row < size; row++)
            {
                board[row] = new CellStateType[size];
            }

            for (int row = 0; row < size; row++)
            {
                int column = solution[row];
                if (!IsInside(row, column, size))
                {
                    return false;
                }

                board[row][column] = CellStateType.CAT;
            }

            return IsComplete(board, size, regions);
        }

        private static Rule ClassifyPair(Vector2Int a, Vector2Int b, int[][] regions)
        {
            if (regions[a.x][a.y] == regions[b.x][b.y])
            {
                return Rule.SameColor;
            }

            if (a.x == b.x || a.y == b.y)
            {
                return Rule.SameLine;
            }

            if (Math.Abs(a.x - b.x) <= 1 && Math.Abs(a.y - b.y) <= 1)
            {
                return Rule.NoTouch;
            }

            return Rule.None;
        }

        private static void ValidateBoardArguments(
            CellStateType[][] board,
            int size,
            int[][] regions)
        {
            ValidateSizeAndRegions(size, regions);
            if (!HasSquareShape(board, size))
            {
                throw new ArgumentException("Board must be a non-null size-by-size array.", nameof(board));
            }
        }

        private static void ValidateSizeAndRegions(int size, int[][] regions)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
            if (!HasSquareShape(regions, size))
            {
                throw new ArgumentException("Regions must be a non-null size-by-size array.", nameof(regions));
            }
        }

        private static void ValidateCell(int row, int column, int[][] regions)
        {
            if (regions == null) throw new ArgumentNullException(nameof(regions));
            int size = regions.Length;
            ValidateSizeAndRegions(size, regions);
            if (!IsInside(row, column, size))
            {
                throw new ArgumentOutOfRangeException($"Cell ({row}, {column}) is outside the board.");
            }
        }

        private static bool HasSquareShape<T>(T[][] values, int size)
        {
            if (values == null || values.Length != size)
            {
                return false;
            }

            for (int row = 0; row < size; row++)
            {
                if (values[row] == null || values[row].Length != size)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsInside(int row, int column, int size)
        {
            return row >= 0 && row < size && column >= 0 && column < size;
        }
    }
}
