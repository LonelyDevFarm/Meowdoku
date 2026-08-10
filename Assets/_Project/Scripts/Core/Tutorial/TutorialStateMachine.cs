using System;
using System.Collections.Generic;
using Meowdoku.Core.Config;
using UnityEngine;

namespace Meowdoku.Core.Tutorial
{
    public enum TutorialPhase
    {
        PlaceFirstCat = 0,
        ConfirmOnePerColor = 1,
        MarkRowAndColumn = 2,
        PlaceSecondCat = 3,
        MarkNeighbors = 4,
        PlaceThirdCat = 5,
        FreePlay = 6,
        Feedback = 7,
        FinishConfirm = 8,
        Completed = 9
    }

    public enum TutorialFeedbackKind
    {
        None = 0,
        Check = 1,
        Iq = 2
    }

    public sealed class TutorialStateMachine
    {
        private static readonly Vector2Int FirstCat = new(0, 2);
        private static readonly Vector2Int SecondCat = new(3, 1);
        private static readonly Vector2Int ThirdCat = new(1, 0);
        private static readonly Vector2Int LastCat = new(2, 3);

        private static readonly Vector2Int[] RowColumnMarks =
        {
            new(0, 0), new(0, 1), new(0, 3),
            new(1, 2), new(2, 2), new(3, 2)
        };

        private static readonly Vector2Int[] NeighborMarks =
        {
            new(2, 0), new(2, 1), new(3, 0)
        };

        private readonly TutorialPuzzle _puzzle;
        private readonly GuideFeedbackConfig _feedbackConfig;
        private readonly TutorialDiagonalConfig _diagonalConfig;
        private readonly DoubleTapProtectConfig _doubleTapConfig;
        private readonly List<Vector2Int> _allowedCells = new();
        private readonly List<Vector2Int> _maskHintCells = new();
        private readonly List<Vector2Int> _mirrorCells = new();

        private BoardStateModel _board;
        private Vector2Int _dragStart = new(-1, -1);
        private bool _dragHadMove;
        private CellStateType _dragTargetState = CellStateType.MARK;
        private Vector2Int _lastTapCell = new(-1, -1);
        private double _lastTapExpiresAt;
        private int _requiredMarks;
        private int _markedCount;
        private TutorialPhase _phaseAfterFeedback;
        private int _iqAfterFeedback;

        public TutorialStateMachine(
            TutorialPuzzle puzzle,
            GuideFeedbackConfig feedbackConfig = null,
            TutorialDiagonalConfig diagonalConfig = null,
            DoubleTapProtectConfig doubleTapConfig = null)
        {
            _puzzle = puzzle ?? throw new ArgumentNullException(nameof(puzzle));
            _feedbackConfig = feedbackConfig ?? new GuideFeedbackConfig();
            _diagonalConfig = diagonalConfig ?? new TutorialDiagonalConfig();
            _doubleTapConfig = doubleTapConfig ?? new DoubleTapProtectConfig();
            Reset();
        }

        public event Action<TutorialPhase> PhaseChanged;
        public event Action<IReadOnlyList<BoardStateChange>> BoardChanged;
        public event Action<TutorialFeedbackKind, int, int> FeedbackRequested;
        public event Action PresentationChanged;

        public TutorialPhase Phase { get; private set; }
        public int HintPhase { get; private set; }
        public int IqValue { get; private set; }
        public TutorialFeedbackKind PendingFeedback { get; private set; }
        public bool UsesDiagonalCopy => _diagonalConfig.IsDiagonalCopy();
        public IReadOnlyList<Vector2Int> AllowedCells => _allowedCells;
        public IReadOnlyList<Vector2Int> MaskHintCells => _maskHintCells;
        public IReadOnlyList<Vector2Int> MirrorCells => _mirrorCells;
        public int MarkedCount => _markedCount;
        public int RequiredMarks => _requiredMarks;
        public bool IsComplete => Phase == TutorialPhase.Completed;

        public CellStateType GetCellState(int row, int column) =>
            _board.GetCellState(row, column);

        public CellStateType[][] GetBoardSnapshot() => _board.GetBoardSnapshot();

        public void Reset()
        {
            _board = new BoardStateModel(
                TutorialPuzzle.SourceSize,
                _puzzle.Regions,
                _puzzle.SolutionColumns);
            _dragStart = new Vector2Int(-1, -1);
            _dragHadMove = false;
            _lastTapCell = new Vector2Int(-1, -1);
            _lastTapExpiresAt = 0d;
            HintPhase = 0;
            IqValue = 60;
            PendingFeedback = TutorialFeedbackKind.None;
            EnterPhase(TutorialPhase.PlaceFirstCat, false);
        }

        public bool BeginGesture(int row, int column)
        {
            if (!Inside(row, column) || Phase == TutorialPhase.Feedback ||
                Phase == TutorialPhase.ConfirmOnePerColor ||
                Phase == TutorialPhase.FinishConfirm ||
                Phase == TutorialPhase.Completed ||
                _board.GetCellState(row, column) == CellStateType.CAT)
                return false;

            if (Phase == TutorialPhase.FreePlay)
            {
                if (HintPhase > 0 && !IsAllowed(row, column)) return false;
            }
            else if (!IsAllowed(row, column))
            {
                return false;
            }

            _dragStart = new Vector2Int(row, column);
            _dragHadMove = false;
            if (Phase == TutorialPhase.FreePlay &&
                (HintPhase == 0 || HintPhase == 3))
            {
                CellStateType current = _board.GetCellState(row, column);
                _dragTargetState = CellState.IsBlank(current)
                    ? CellStateType.MARK
                    : CellStateType.EMPTY;
            }
            return true;
        }

        public bool DragOver(int row, int column)
        {
            if (!Inside(row, column) || _dragStart.x < 0) return false;
            if (_dragStart != new Vector2Int(row, column)) _dragHadMove = true;

            if (Phase == TutorialPhase.MarkRowAndColumn ||
                Phase == TutorialPhase.MarkNeighbors)
            {
                if (!IsAllowed(row, column) ||
                    !CellState.IsBlank(_board.GetCellState(row, column)))
                    return false;
                if (!SetCell(row, column, CellStateType.MARK)) return false;
                _markedCount++;
                if (_markedCount >= _requiredMarks)
                    CompleteInteractivePhase(Phase);
                return true;
            }

            if (Phase != TutorialPhase.FreePlay) return false;
            if (HintPhase == 1 || HintPhase == 2)
            {
                if (!IsAllowed(row, column) ||
                    !CellState.IsBlank(_board.GetCellState(row, column)))
                    return false;
                bool changed = SetCell(row, column, CellStateType.MARK);
                if (changed) CheckHintMarkPhaseComplete();
                return changed;
            }

            CellStateType current = _board.GetCellState(row, column);
            if (current == CellStateType.CAT || current == _dragTargetState)
                return false;
            return SetCell(row, column, _dragTargetState);
        }

        public bool EndGesture(double nowSeconds)
        {
            ExpireTap(nowSeconds, onlyIfExpired: true);
            if (_dragStart.x < 0) return false;
            Vector2Int start = _dragStart;
            bool moved = _dragHadMove;
            _dragStart = new Vector2Int(-1, -1);
            _dragHadMove = false;
            if (moved) return true;

            switch (Phase)
            {
                case TutorialPhase.MarkRowAndColumn:
                case TutorialPhase.MarkNeighbors:
                    return HandleMarkTap(start.x, start.y);
                case TutorialPhase.PlaceFirstCat:
                case TutorialPhase.PlaceSecondCat:
                case TutorialPhase.PlaceThirdCat:
                    return HandlePlaceCatTap(start.x, start.y, nowSeconds);
                case TutorialPhase.FreePlay:
                    return HandleFreePlayTap(start.x, start.y, nowSeconds);
                default:
                    return false;
            }
        }

        public bool Tap(int row, int column, double nowSeconds)
        {
            return BeginGesture(row, column) && EndGesture(nowSeconds);
        }

        public bool Confirm()
        {
            if (Phase == TutorialPhase.ConfirmOnePerColor)
            {
                EnterPhase(TutorialPhase.MarkRowAndColumn);
                return true;
            }
            if (Phase == TutorialPhase.FinishConfirm)
            {
                EnterPhase(TutorialPhase.Completed);
                return true;
            }
            return false;
        }

        public bool CompleteFeedback()
        {
            if (Phase != TutorialPhase.Feedback ||
                PendingFeedback == TutorialFeedbackKind.None)
                return false;
            if (PendingFeedback == TutorialFeedbackKind.Iq)
                IqValue = _iqAfterFeedback;
            PendingFeedback = TutorialFeedbackKind.None;
            EnterPhase(_phaseAfterFeedback);
            return true;
        }

        public bool PressHint()
        {
            if (Phase != TutorialPhase.FreePlay) return false;
            if (HintPhase == 1 || HintPhase == 2)
            {
                Vector2Int[] cells = _allowedCells.ToArray();
                foreach (Vector2Int cell in cells)
                {
                    if (CellState.IsBlank(_board.GetCellState(cell.x, cell.y)))
                        SetCell(cell.x, cell.y, CellStateType.MARK);
                }
                CheckHintMarkPhaseComplete();
                return true;
            }
            if (HintPhase == 3)
            {
                PlaceCat(LastCat.x, LastCat.y);
                HintPhase = 0;
                ConfigureFreePlayBasePresentation();
                CheckFreePlayComplete();
                if (Phase == TutorialPhase.FreePlay)
                    PresentationChanged?.Invoke();
                return true;
            }

            List<Vector2Int> blueEmpty = BlankCellsInCatRow(ThirdCat);
            if (blueEmpty.Count > 0)
            {
                HintPhase = 1;
                ConfigureHintPhase(blueEmpty, ThirdCat);
                PresentationChanged?.Invoke();
                return true;
            }
            List<Vector2Int> pinkEmpty = BlankCellsInCatRow(SecondCat);
            if (pinkEmpty.Count > 0)
            {
                HintPhase = 2;
                ConfigureHintPhase(pinkEmpty, SecondCat);
                PresentationChanged?.Invoke();
                return true;
            }

            HintPhase = 3;
            SetCells(_allowedCells, LastCat);
            SetCells(_maskHintCells, LastCat);
            _mirrorCells.Clear();
            PresentationChanged?.Invoke();
            return true;
        }

        private bool HandleMarkTap(int row, int column)
        {
            if (!CellState.IsBlank(_board.GetCellState(row, column))) return false;
            if (!SetCell(row, column, CellStateType.MARK)) return false;
            _markedCount++;
            if (_markedCount >= _requiredMarks)
                CompleteInteractivePhase(Phase);
            return true;
        }

        private bool HandlePlaceCatTap(int row, int column, double nowSeconds)
        {
            if (IsSecondTap(row, column, nowSeconds))
            {
                ClearPendingTap();
                if (!PlaceCat(row, column)) return false;
                CompleteInteractivePhase(Phase);
                return true;
            }
            OpenTapWindow(row, column, true, nowSeconds);
            return true;
        }

        private bool HandleFreePlayTap(int row, int column, double nowSeconds)
        {
            if (HintPhase == 1 || HintPhase == 2)
            {
                if (!IsAllowed(row, column)) return false;
                CellStateType state = _board.GetCellState(row, column);
                bool hintCellChanged = false;
                if (CellState.IsBlank(state))
                    hintCellChanged = SetCell(row, column, CellStateType.MARK);
                else if (state == CellStateType.MARK)
                    hintCellChanged = SetCell(row, column, CellStateType.EMPTY);
                CheckHintMarkPhaseComplete();
                return hintCellChanged;
            }

            if (IsAllowed(row, column))
            {
                if (IsSecondTap(row, column, nowSeconds))
                {
                    ClearPendingTap();
                    bool placed = PlaceCat(row, column);
                    if (placed) CheckFreePlayComplete();
                    return placed;
                }
                OpenTapWindow(row, column, true, nowSeconds);
                return true;
            }

            CellStateType current = _board.GetCellState(row, column);
            bool changed = false;
            if (CellState.IsBlank(current))
                changed = SetCell(row, column, CellStateType.MARK);
            else if (current == CellStateType.MARK)
                changed = SetCell(row, column, CellStateType.EMPTY);
            OpenTapWindow(row, column, false, nowSeconds);
            return changed;
        }

        private void CompleteInteractivePhase(TutorialPhase completedPhase)
        {
            TutorialPhase next;
            switch (completedPhase)
            {
                case TutorialPhase.PlaceFirstCat:
                    next = _feedbackConfig.Value == GuideFeedbackConfig.ValueCurrent
                        ? TutorialPhase.ConfirmOnePerColor
                        : TutorialPhase.MarkRowAndColumn;
                    break;
                case TutorialPhase.MarkRowAndColumn:
                    next = TutorialPhase.PlaceSecondCat;
                    break;
                case TutorialPhase.PlaceSecondCat:
                    next = TutorialPhase.MarkNeighbors;
                    break;
                case TutorialPhase.MarkNeighbors:
                    next = TutorialPhase.PlaceThirdCat;
                    break;
                case TutorialPhase.PlaceThirdCat:
                    next = TutorialPhase.FreePlay;
                    break;
                case TutorialPhase.FreePlay:
                    next = TutorialPhase.FinishConfirm;
                    break;
                default:
                    return;
            }

            if (_feedbackConfig.Value == GuideFeedbackConfig.ValueCurrent)
            {
                EnterPhase(next);
                return;
            }

            _phaseAfterFeedback = next;
            PendingFeedback = _feedbackConfig.IsIqGuide()
                ? TutorialFeedbackKind.Iq
                : TutorialFeedbackKind.Check;
            int before = IqValue;
            _iqAfterFeedback = PendingFeedback == TutorialFeedbackKind.Iq
                ? Math.Min(180, before + 20)
                : before;
            EnterPhase(TutorialPhase.Feedback);
            FeedbackRequested?.Invoke(PendingFeedback, before, _iqAfterFeedback);
        }

        private void EnterPhase(TutorialPhase phase, bool notify = true)
        {
            Phase = phase;
            _requiredMarks = 0;
            _markedCount = 0;
            if (phase != TutorialPhase.Feedback)
                HintPhase = 0;
            _allowedCells.Clear();
            _maskHintCells.Clear();
            _mirrorCells.Clear();

            switch (phase)
            {
                case TutorialPhase.PlaceFirstCat:
                    SetCells(_allowedCells, FirstCat);
                    SetCells(_maskHintCells, FirstCat);
                    break;
                case TutorialPhase.MarkRowAndColumn:
                    AddCells(_allowedCells, RowColumnMarks);
                    AddCells(_maskHintCells, RowColumnMarks);
                    SetCells(_mirrorCells, FirstCat);
                    _requiredMarks = RowColumnMarks.Length;
                    break;
                case TutorialPhase.PlaceSecondCat:
                    SetCells(_allowedCells, SecondCat);
                    SetCells(_maskHintCells, SecondCat);
                    AddCells(_mirrorCells, new[] { new Vector2Int(2, 2), new Vector2Int(3, 2) });
                    break;
                case TutorialPhase.MarkNeighbors:
                    AddCells(_allowedCells, NeighborMarks);
                    AddCells(_maskHintCells, NeighborMarks);
                    SetCells(_mirrorCells, SecondCat);
                    _requiredMarks = NeighborMarks.Length;
                    break;
                case TutorialPhase.PlaceThirdCat:
                    SetCells(_allowedCells, ThirdCat);
                    SetCells(_maskHintCells, ThirdCat);
                    AddCells(_mirrorCells, new[]
                    {
                        new Vector2Int(0, 0), new Vector2Int(2, 0),
                        new Vector2Int(2, 1), new Vector2Int(3, 0)
                    });
                    break;
                case TutorialPhase.FreePlay:
                    ConfigureFreePlayBasePresentation();
                    break;
            }
            if (notify) PhaseChanged?.Invoke(phase);
        }

        private bool SetCell(int row, int column, CellStateType state)
        {
            if (!_board.TrySetCellState(row, column, state,
                    out IReadOnlyList<BoardStateChange> changes) || changes.Count == 0)
                return false;
            BoardChanged?.Invoke(changes);
            return true;
        }

        private bool PlaceCat(int row, int column)
        {
            return SetCell(row, column, CellStateType.CAT);
        }

        private void CheckFreePlayComplete()
        {
            if (_board.IsComplete())
                CompleteInteractivePhase(TutorialPhase.FreePlay);
        }

        private void CheckHintMarkPhaseComplete()
        {
            foreach (Vector2Int cell in _allowedCells)
            {
                if (_board.GetCellState(cell.x, cell.y) != CellStateType.MARK)
                    return;
            }
            HintPhase = 0;
            ConfigureFreePlayBasePresentation();
            PresentationChanged?.Invoke();
        }

        private List<Vector2Int> BlankCellsInCatRow(Vector2Int cat)
        {
            var result = new List<Vector2Int>();
            for (int column = 0; column < TutorialPuzzle.SourceSize; column++)
            {
                if (column != cat.y &&
                    CellState.IsBlank(_board.GetCellState(cat.x, column)))
                    result.Add(new Vector2Int(cat.x, column));
            }
            return result;
        }

        private void ConfigureHintPhase(
            IEnumerable<Vector2Int> cells,
            Vector2Int mirrorCat)
        {
            _allowedCells.Clear();
            _maskHintCells.Clear();
            _mirrorCells.Clear();
            AddCells(_allowedCells, cells);
            AddCells(_maskHintCells, cells);
            SetCells(_mirrorCells, mirrorCat);
        }

        private void ConfigureFreePlayBasePresentation()
        {
            _allowedCells.Clear();
            _maskHintCells.Clear();
            _mirrorCells.Clear();
            SetCells(_allowedCells, LastCat);
        }

        private bool IsSecondTap(int row, int column, double nowSeconds)
        {
            ExpireTap(nowSeconds, onlyIfExpired: true);
            return _lastTapCell == new Vector2Int(row, column);
        }

        private void OpenTapWindow(
            int row,
            int column,
            bool onSolutionCat,
            double nowSeconds)
        {
            bool truthHasCat = _doubleTapConfig.NeedsTruth() && onSolutionCat;
            bool wouldConflict = _doubleTapConfig.NeedsConflict() &&
                                 _board.WouldCatConflict(row, column);
            _lastTapCell = new Vector2Int(row, column);
            _lastTapExpiresAt = nowSeconds +
                                _doubleTapConfig.WindowSeconds(
                                    truthHasCat, wouldConflict);
        }

        private void ExpireTap(double nowSeconds, bool onlyIfExpired)
        {
            if (_lastTapCell.x < 0) return;
            if (onlyIfExpired && nowSeconds <= _lastTapExpiresAt) return;
            ClearPendingTap();
        }

        private void ClearPendingTap()
        {
            _lastTapCell = new Vector2Int(-1, -1);
            _lastTapExpiresAt = 0d;
        }

        private bool IsAllowed(int row, int column)
        {
            return _allowedCells.Contains(new Vector2Int(row, column));
        }

        private static bool Inside(int row, int column)
        {
            return row >= 0 && row < TutorialPuzzle.SourceSize &&
                   column >= 0 && column < TutorialPuzzle.SourceSize;
        }

        private static void SetCells(List<Vector2Int> target, Vector2Int cell)
        {
            target.Clear();
            target.Add(cell);
        }

        private static void AddCells(
            List<Vector2Int> target,
            IEnumerable<Vector2Int> cells)
        {
            target.AddRange(cells);
        }
    }

    public sealed class TutorialCompletionCommitter
    {
        private bool _committed;

        public bool Commit(GameStateService gameState)
        {
            if (_committed) return false;
            if (gameState == null) throw new ArgumentNullException(nameof(gameState));
            gameState.SetTutorialDone(true);
            _committed = true;
            return true;
        }
    }
}
