using Meowdoku.Core;
using UnityEngine;

namespace Meowdoku.Gameplay.Input
{
    public sealed class BoardStrokeContext
    {
        private static readonly Vector2Int InvalidCell = new Vector2Int(-1, -1);

        public Vector2Int StartCell { get; set; } = InvalidCell;
        public Vector2Int LastCell { get; set; } = InvalidCell;
        public CellStateType TargetState { get; set; } = CellStateType.EMPTY;
        public bool TargetPending { get; set; }
        public bool HadMove { get; set; }
        public bool Changed { get; set; }
        public bool WantsDoubleTapWindow { get; set; }

        public bool IsActive => StartCell != InvalidCell;

        public void Reset()
        {
            StartCell = InvalidCell;
            LastCell = InvalidCell;
            TargetState = CellStateType.EMPTY;
            TargetPending = false;
            HadMove = false;
            Changed = false;
            WantsDoubleTapWindow = false;
        }
    }
}
