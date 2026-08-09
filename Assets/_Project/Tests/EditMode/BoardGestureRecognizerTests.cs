using Meowdoku.Core;
using Meowdoku.Core.Config;
using Meowdoku.Gameplay;
using Meowdoku.Gameplay.Input;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class BoardGestureRecognizerTests
    {
        [Test]
        public void SingleTap_IsReturnedImmediatelyLikeGodotSource()
        {
            var recognizer = CreateRecognizer();

            var actions = recognizer.OnDragStart(0, 0, 10f);
            Assert.That(actions, Has.Count.EqualTo(1));
            Assert.That(actions[0].Kind, Is.EqualTo(CellAction.ActionKind.SetState));
            Assert.That(actions[0].State, Is.EqualTo(CellStateType.MARK));
        }

        [Test]
        public void ErrorStart_IsTerminalAndCannotEraseNeighboringMarks()
        {
            var board = new MutableRowBoard(CellStateType.ERROR, CellStateType.MARK);
            var recognizer = new BoardGestureRecognizer(new BoardInputScheme(board));

            Assert.That(recognizer.OnDragStart(0, 0, 0f), Is.Empty);
            Assert.That(recognizer.OnDragOver(0, 1), Is.Empty);
        }

        [Test]
        public void SecondTapOnSameCell_WithinWindowEmitsDoubleTap()
        {
            var recognizer = CreateRecognizer();
            recognizer.OnDragStart(1, 2, 5f);

            var actions = recognizer.OnDragStart(1, 2, 5.1f);

            Assert.That(actions, Has.Count.EqualTo(1));
            Assert.That(actions[0].Kind, Is.EqualTo(CellAction.ActionKind.DoubleTap));
        }

        [Test]
        public void Swipe_ReturnsStartImmediatelyAndInterpolatesSkippedCells()
        {
            var recognizer = CreateRecognizer();
            var start = recognizer.OnDragStart(0, 0, 1f);

            var actions = recognizer.OnDragOver(0, 2);

            Assert.That(start, Has.Count.EqualTo(1));
            Assert.That(start[0].Column, Is.Zero);
            Assert.That(actions, Has.Count.EqualTo(2));
            Assert.That(actions[0].Column, Is.EqualTo(1));
            Assert.That(actions[1].Column, Is.EqualTo(2));
            Assert.That(actions, Has.All.Property("State").EqualTo(CellStateType.MARK));
        }

        [Test]
        public void NewCellTap_DoesNotWaitForPreviousDoubleTapWindow()
        {
            var recognizer = CreateRecognizer();
            var first = recognizer.OnDragStart(0, 0, 2f);

            var second = recognizer.OnDragStart(0, 1, 2.1f);

            Assert.That(first, Has.Count.EqualTo(1));
            Assert.That(second, Has.Count.EqualTo(1));
            Assert.That(second[0].Column, Is.EqualTo(1));
        }

        [Test]
        public void ConfiguredDoubleTapWindow_ReplacesDefaultExpiry()
        {
            var recognizer = new BoardGestureRecognizer(
                new BoardInputScheme(new EmptyBoard()),
                (row, column) => 0.25f);

            Assert.That(recognizer.OnDragStart(2, 3, 4f)[0].Kind,
                Is.EqualTo(CellAction.ActionKind.SetState));
            Assert.That(recognizer.OnDragStart(2, 3, 4.249f)[0].Kind,
                Is.EqualTo(CellAction.ActionKind.DoubleTap));

            var expired = new BoardGestureRecognizer(
                new BoardInputScheme(new EmptyBoard()),
                (row, column) => 0.25f);
            expired.OnDragStart(2, 3, 4f);
            Assert.That(expired.OnDragStart(2, 3, 4.251f)[0].Kind,
                Is.EqualTo(CellAction.ActionKind.SetState));
        }

        [Test]
        public void FastEraseAcrossThreeCells_ChangesStartMiddleAndEnd()
        {
            var board = new MutableRowBoard(CellStateType.MARK, CellStateType.MARK, CellStateType.MARK);
            var recognizer = new BoardGestureRecognizer(new BoardInputScheme(board));

            var start = recognizer.OnDragStart(0, 0, 1f);
            board.Apply(start);
            var rest = recognizer.OnDragOver(0, 2);

            Assert.That(start, Has.Count.EqualTo(1));
            Assert.That(rest, Has.Count.EqualTo(2));
            Assert.That(rest[0].Column, Is.EqualTo(1));
            Assert.That(rest[1].Column, Is.EqualTo(2));
            Assert.That(rest, Has.All.Property("State").EqualTo(CellStateType.EMPTY));
        }

        [Test]
        public void DoubleTapWindowProvider_ReceivesTappedRowAndColumn()
        {
            int receivedRow = -1;
            int receivedColumn = -1;
            var recognizer = new BoardGestureRecognizer(
                new BoardInputScheme(new EmptyBoard()),
                (row, column) =>
                {
                    receivedRow = row;
                    receivedColumn = column;
                    return 0.35f;
                });

            recognizer.OnDragStart(4, 5, 1f);

            Assert.That(receivedRow, Is.EqualTo(4));
            Assert.That(receivedColumn, Is.EqualTo(5));
        }

        [Test]
        public void StepHistory_RoundTripsSourceSerializationShape()
        {
            var history = new StepHistory();
            var step = new StepHistory.StepRecord
            {
                IsCatPlacement = true,
                IsWrongGuess = false
            };
            step.Cells.Add(new StepHistory.CellChange
            {
                Position = new UnityEngine.Vector2Int(2, 3),
                Before = CellStateType.MARK,
                After = CellStateType.CAT
            });
            history.Push(step);

            var restored = new StepHistory();
            restored.Deserialize(history.Serialize());

            Assert.That(restored.HasStep(), Is.True);
            Assert.That(restored.Count, Is.EqualTo(1));
            Assert.That(restored.PeekAt(-1), Is.Null);
            Assert.That(restored.PeekAt(1), Is.Null);
            Assert.That(restored.PeekLast().IsCatPlacement, Is.True);
            Assert.That(restored.PeekLast().IsWrongGuess, Is.False);
            Assert.That(restored.PeekLast().Cells[0].Position,
                Is.EqualTo(new UnityEngine.Vector2Int(2, 3)));
            Assert.That(restored.PeekLast().Cells[0].Before, Is.EqualTo(CellStateType.MARK));
            Assert.That(restored.PeekLast().Cells[0].After, Is.EqualTo(CellStateType.CAT));
        }

        [Test]
        public void GameSession_NewGameEntersThenUnlocksWithThreeLives()
        {
            GameSession session = CreateSession();

            Assert.That(session.State, Is.EqualTo(GameSessionState.Entering));
            Assert.That(session.CanAcceptInput, Is.False);
            Assert.That(session.Lives, Is.EqualTo(3));
            Assert.That(session.MistakeCount, Is.Zero);

            session.FinishEntering();

            Assert.That(session.State, Is.EqualTo(GameSessionState.Playing));
            Assert.That(session.CanAcceptInput, Is.True);
        }

        [Test]
        public void GameSession_WrongGuessLocksThenFailsAtZeroLivesAndCanRevive()
        {
            GameSession session = CreateSession();
            session.FinishEntering();
            int[][] wrongCells =
            {
                new[] { 0, 0 }, new[] { 1, 0 }, new[] { 2, 1 }
            };

            for (int i = 0; i < wrongCells.Length; i++)
            {
                SessionActionResult action = session.DoubleTap(wrongCells[i][0], wrongCells[i][1]);
                Assert.That(action.Kind, Is.EqualTo(SessionActionKind.WrongGuess));
                Assert.That(session.State, Is.EqualTo(GameSessionState.ResolvingWrongGuess));
                Assert.That(session.CanAcceptInput, Is.False);
                session.ResolveWrongGuess();
            }

            Assert.That(session.Lives, Is.Zero);
            Assert.That(session.MistakeCount, Is.EqualTo(3));
            Assert.That(session.State, Is.EqualTo(GameSessionState.Failed));
            Assert.That(session.History.Count, Is.EqualTo(3));
            Assert.That(session.History.PeekLast().IsWrongGuess, Is.True);

            Assert.That(session.Revive(1), Is.True);
            Assert.That(session.Lives, Is.EqualTo(1));
            Assert.That(session.ReviveCount, Is.EqualTo(1));
            Assert.That(session.State, Is.EqualTo(GameSessionState.Playing));
        }

        [Test]
        public void GameSession_WrongDoubleTapPersistsSourceErrorState()
        {
            GameSession session = CreateSession();
            session.FinishEntering();

            SessionActionResult result = session.DoubleTap(0, 0);

            Assert.That(result.Kind, Is.EqualTo(SessionActionKind.WrongGuess));
            Assert.That(session.GetCellState(0, 0), Is.EqualTo(CellStateType.ERROR));
            Assert.That(result.Changes[0].After, Is.EqualTo(CellStateType.ERROR));
        }

        [Test]
        public void GameSession_FinalCorrectCatTransitionsToWon()
        {
            GameSession session = CreateSession();
            session.FinishEntering();
            int[] solution = { 1, 3, 0, 2 };

            for (int row = 0; row < solution.Length; row++)
                Assert.That(session.DoubleTap(row, solution[row]).Accepted, Is.True);

            Assert.That(session.State, Is.EqualTo(GameSessionState.Won));
            Assert.That(session.CanAcceptInput, Is.False);
            Assert.That(session.RemainingCats, Is.Zero);
            Assert.That(session.Score.Score, Is.EqualTo(2880));
            Assert.That(session.Score.Combo, Is.EqualTo(4));
            Assert.That(session.History.Count, Is.EqualTo(4));
        }

        [Test]
        public void GameSession_RestoreUsesBoardListsSeparatelyFromUndoHistory()
        {
            var sourceHistory = new StepHistory();
            var historyStep = new StepHistory.StepRecord { IsCatPlacement = true };
            historyStep.Cells.Add(new StepHistory.CellChange
            {
                Position = new UnityEngine.Vector2Int(0, 1),
                Before = CellStateType.EMPTY,
                After = CellStateType.CAT
            });
            sourceHistory.Push(historyStep);
            var restore = new GameSessionRestoreData
            {
                Lives = 1,
                SuccessfulCatCount = 2,
                ReviveCount = 1,
                RestartCount = 3,
                Score = new System.Collections.Generic.Dictionary<string, int>
                {
                    { "score", 900 }, { "combo", 2 }, { "max_combo", 4 }
                },
                StepHistoryData = sourceHistory.Serialize()
            };
            restore.PlacedCats.Add(new UnityEngine.Vector2Int(0, 1));
            restore.Marks.Add(new UnityEngine.Vector2Int(1, 0));
            restore.Errors.Add(new UnityEngine.Vector2Int(2, 1));

            GameSession session = CreateSession(restore);

            Assert.That(session.GetCellState(0, 1), Is.EqualTo(CellStateType.CAT));
            Assert.That(session.GetCellState(1, 0), Is.EqualTo(CellStateType.MARK));
            Assert.That(session.GetCellState(2, 1), Is.EqualTo(CellStateType.ERROR));
            Assert.That(session.History.Count, Is.EqualTo(1));
            Assert.That(session.Lives, Is.EqualTo(1));
            Assert.That(session.Score.Score, Is.EqualTo(900));
            Assert.That(session.ReviveCount, Is.EqualTo(1));
            Assert.That(session.RestartCount, Is.EqualTo(3));
        }

        [Test]
        public void GameSession_SnapshotUsesSourceBoardAndScoreFields()
        {
            GameSession session = CreateSession();
            session.FinishEntering();
            session.DoubleTap(0, 1);

            System.Collections.Generic.Dictionary<string, object> snapshot =
                session.CreateSnapshot();

            Assert.That(snapshot["lives"], Is.EqualTo(3));
            Assert.That(snapshot["score"], Is.EqualTo(600));
            Assert.That(snapshot["combo"], Is.EqualTo(1));
            Assert.That((System.Collections.IList)snapshot["placed_cats"], Has.Count.EqualTo(1));
            Assert.That((System.Collections.IList)snapshot["marks"], Is.Empty);
            Assert.That((System.Collections.IList)snapshot["errors"], Is.Empty);
            Assert.That((System.Collections.IList)snapshot["step_history"], Has.Count.EqualTo(1));
        }

        [Test]
        public void GameSession_ClearOnlyRemovesPlainMarksWithoutCreatingHistory()
        {
            GameSession session = CreateSession();
            session.FinishEntering();
            session.TryApplyBoardEdit(0, 0, CellStateType.MARK, true, out _);
            session.TryApplyBoardEdit(1, 0, CellStateType.MARK, true, out _);
            session.CommitCurrentStep();
            session.Board.RestoreCellState(2, 1, CellStateType.ERROR, out _);
            int historyBefore = session.History.Count;

            SessionActionResult clear = session.ClearMarks();

            Assert.That(clear.Accepted, Is.True);
            Assert.That(clear.Changes, Has.Count.EqualTo(2));
            Assert.That(session.GetCellState(0, 0), Is.EqualTo(CellStateType.EMPTY));
            Assert.That(session.GetCellState(1, 0), Is.EqualTo(CellStateType.EMPTY));
            Assert.That(session.GetCellState(2, 1), Is.EqualTo(CellStateType.ERROR));
            Assert.That(session.History.Count, Is.EqualTo(historyBefore));
        }

        [Test]
        public void GameSession_LocateChoosesSolutionInSmallestRemainingRegion()
        {
            GameSession session = CreateSession();
            session.FinishEntering();
            session.TryApplyBoardEdit(1, 0, CellStateType.MARK, false, out _);
            session.TryApplyBoardEdit(1, 1, CellStateType.MARK, false, out _);
            session.TryApplyBoardEdit(1, 2, CellStateType.MARK, false, out _);

            SessionActionResult locate = session.Locate();

            Assert.That(locate.Accepted, Is.True);
            Assert.That(locate.Kind, Is.EqualTo(SessionActionKind.Locate));
            Assert.That(locate.Changes[0].Position,
                Is.EqualTo(new UnityEngine.Vector2Int(1, 3)));
            Assert.That(session.GetCellState(1, 3), Is.EqualTo(CellStateType.CAT));
            Assert.That(session.Score.Score, Is.EqualTo(600));
            Assert.That(session.History.PeekLast().IsCatPlacement, Is.True);
        }

        [Test]
        public void GameSession_HintRequestLocksInputAndR1MarkAppliesOneStep()
        {
            GameSession session = CreateSession();
            session.ApplyPrefill(0, 1, out _);
            session.FinishEntering();

            SessionHintRequest request = session.RequestHint();

            Assert.That(request.Found, Is.True);
            Assert.That(request.Hint.Strategy, Is.EqualTo("R1_mark"));
            Assert.That(session.HasPendingHint, Is.True);
            Assert.That(session.CanAcceptInput, Is.False);

            SessionActionResult applied = session.ApplyHint();

            Assert.That(applied.Accepted, Is.True);
            Assert.That(applied.Changes, Has.Count.EqualTo(8));
            Assert.That(session.History.Count, Is.EqualTo(1));
            Assert.That(session.History.PeekLast().Cells, Has.Count.EqualTo(8));
            Assert.That(session.History.PeekLast().IsCatPlacement, Is.False);
            Assert.That(session.CanAcceptInput, Is.True);
        }

        [Test]
        public void GameSession_HintRemovesWrongMarkBeforeOtherStrategies()
        {
            GameSession session = CreateSession();
            session.FinishEntering();
            session.TryApplyBoardEdit(0, 1, CellStateType.MARK, true, out _);
            session.CommitCurrentStep();

            SessionHintRequest request = session.RequestHint();
            SessionActionResult applied = session.ApplyHint();

            Assert.That(request.Found, Is.True);
            Assert.That(request.WrongMark, Is.True);
            Assert.That(request.WrongMarkCell,
                Is.EqualTo(new UnityEngine.Vector2Int(0, 1)));
            Assert.That(applied.Changes, Has.Count.EqualTo(1));
            Assert.That(session.GetCellState(0, 1), Is.EqualTo(CellStateType.EMPTY));
            Assert.That(session.History.Count, Is.EqualTo(2));
            Assert.That(session.History.PeekLast().Cells[0].Before,
                Is.EqualTo(CellStateType.MARK));
        }

        [Test]
        public void GameSession_AutoCompleteUsesDiagonalMarkOrderAndDoesNotCreateHistory()
        {
            GameSession session = CreateSession();
            session.FinishEntering();

            SessionActionResult result = session.AutoComplete();

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.Changes, Has.Count.EqualTo(16));
            Assert.That(result.Changes[0].Position,
                Is.EqualTo(new UnityEngine.Vector2Int(3, 0)));
            Assert.That(session.State, Is.EqualTo(GameSessionState.Won));
            Assert.That(session.Score.Score, Is.EqualTo(2880));
            Assert.That(session.History.Count, Is.Zero);
        }

        [Test]
        public void HintMutex_OnlyOwnerCanRelease()
        {
            var mutex = new HintMutex();

            Assert.That(mutex.TryAcquire("first"), Is.True);
            Assert.That(mutex.TryAcquire("second"), Is.False);
            mutex.Release("second");
            Assert.That(mutex.IsActive, Is.True);
            mutex.Release("first");
            Assert.That(mutex.IsActive, Is.False);
        }

        private static BoardGestureRecognizer CreateRecognizer()
        {
            return new BoardGestureRecognizer(new BoardInputScheme(new EmptyBoard()));
        }

        private static GameSession CreateSession(GameSessionRestoreData restore = null)
        {
            int[][] regions =
            {
                new[] { 0, 0, 0, 0 },
                new[] { 1, 1, 1, 1 },
                new[] { 2, 2, 2, 2 },
                new[] { 3, 3, 3, 3 }
            };
            return new GameSession(
                4,
                regions,
                new[] { 1, 3, 0, 2 },
                1,
                new ScoreEncourageConfig(),
                restore);
        }

        private sealed class EmptyBoard : IBoardStateReader
        {
            public CellStateType GetCellState(int row, int column)
            {
                return CellStateType.EMPTY;
            }
        }

        private sealed class MutableRowBoard : IBoardStateReader
        {
            private readonly CellStateType[] _states;

            public MutableRowBoard(params CellStateType[] states)
            {
                _states = states;
            }

            public CellStateType GetCellState(int row, int column)
            {
                return _states[column];
            }

            public void Apply(System.Collections.Generic.IReadOnlyList<CellAction> actions)
            {
                for (int i = 0; i < actions.Count; i++)
                    _states[actions[i].Column] = actions[i].State;
            }
        }
    }
}
