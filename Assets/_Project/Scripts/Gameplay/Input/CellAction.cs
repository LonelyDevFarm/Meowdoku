using Meowdoku.Core;

namespace Meowdoku.Gameplay.Input
{
    public sealed class CellAction
    {
        public enum ActionKind { SetState, DoubleTap }

        public ActionKind Kind { get; private set; }
        public int Row { get; private set; }
        public int Column { get; private set; }
        public CellStateType State { get; private set; }
        public CellStateType Before { get; private set; }
        public bool PlayAnimation { get; private set; } = true;
        public bool ShowCatVisual { get; private set; } = true;
        public bool Record { get; private set; } = true;
        public int Vibrate { get; private set; } = -1;

        public static CellAction SetCell(
            int row, int column, CellStateType before, CellStateType target,
            int vibrate = -1, bool record = true)
        {
            return new CellAction
            {
                Kind = ActionKind.SetState,
                Row = row,
                Column = column,
                Before = before,
                State = target,
                Vibrate = vibrate,
                Record = record
            };
        }

        public static CellAction DoubleTap(int row, int column)
        {
            return new CellAction { Kind = ActionKind.DoubleTap, Row = row, Column = column };
        }
    }
}
