using System.Collections.Generic;
using Meowdoku.Core;

namespace Meowdoku.Gameplay.Input
{
    public interface IBoardStateReader
    {
        CellStateType GetCellState(int row, int column);
    }

    public sealed class NormalTapOperation
    {
        private readonly IBoardStateReader _board;
        public NormalTapOperation(IBoardStateReader board) { _board = board; }

        public List<CellAction> OnTap(int row, int column, BoardStrokeContext stroke)
        {
            var result = new List<CellAction>();
            CellStateType current = _board.GetCellState(row, column);
            if (current == CellStateType.CAT)
            {
                stroke.TargetPending = true;
                stroke.TargetState = CellStateType.EMPTY;
                return result;
            }

            stroke.TargetPending = false;
            stroke.TargetState = CellState.IsBlank(current) ? CellStateType.MARK : CellStateType.EMPTY;
            if (CellState.IsBlank(current))
                result.Add(CellAction.SetCell(row, column, CellStateType.EMPTY, CellStateType.MARK, 2));
            else if (current == CellStateType.MARK)
                result.Add(CellAction.SetCell(row, column, CellStateType.MARK, CellStateType.EMPTY, 2));
            return result;
        }
    }

    public sealed class NormalDoubleTapOperation
    {
        private readonly IBoardStateReader _board;
        public NormalDoubleTapOperation(IBoardStateReader board) { _board = board; }

        public List<CellAction> OnDoubleTap(int row, int column)
        {
            var result = new List<CellAction>();
            if (_board.GetCellState(row, column) != CellStateType.CAT)
                result.Add(CellAction.DoubleTap(row, column));
            return result;
        }
    }

    public sealed class NormalSwipeOperation
    {
        private readonly IBoardStateReader _board;
        public NormalSwipeOperation(IBoardStateReader board) { _board = board; }

        public CellAction OnPaint(int row, int column, BoardStrokeContext stroke, bool isCurrent)
        {
            CellStateType state = _board.GetCellState(row, column);
            if (state == CellStateType.CAT || state == CellStateType.ERROR) return null;

            if (stroke.TargetPending)
            {
                stroke.TargetState = CellState.IsBlank(state) ? CellStateType.MARK : CellStateType.EMPTY;
                stroke.TargetPending = false;
            }
            if (state == stroke.TargetState) return null;
            return CellAction.SetCell(row, column, state, stroke.TargetState, isCurrent ? 2 : -1);
        }
    }

    public sealed class BoardInputScheme
    {
        private readonly IBoardStateReader _board;
        public NormalTapOperation Tap { get; }
        public NormalDoubleTapOperation DoubleTap { get; }
        public NormalSwipeOperation Swipe { get; }

        public BoardInputScheme(IBoardStateReader board)
        {
            _board = board;
            Tap = new NormalTapOperation(board);
            DoubleTap = new NormalDoubleTapOperation(board);
            Swipe = new NormalSwipeOperation(board);
        }

        public bool IsTerminalError(int row, int column)
        {
            return _board.GetCellState(row, column) == CellStateType.ERROR;
        }
    }
}
