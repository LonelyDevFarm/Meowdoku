using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.Config;
using Meowdoku.Core.Tutorial;
using NUnit.Framework;
using UnityEngine;

namespace Meowdoku.Tests.EditMode
{
    public sealed class TutorialStateMachineTests
    {
        private static readonly Vector2Int[] RowColumnMarks =
        {
            new(0, 0), new(0, 1), new(0, 3),
            new(1, 2), new(2, 2), new(3, 2)
        };

        private static readonly Vector2Int[] NeighborMarks =
        {
            new(2, 0), new(2, 1), new(3, 0)
        };

        [Test]
        public void GuidePuzzle_MatchesDecodedGodotBankEntryExactly()
        {
            TutorialPuzzle puzzle = CreatePuzzle();

            Assert.That(puzzle.Id, Is.EqualTo(51));
            Assert.That(puzzle.Pattern, Is.EqualTo("guide"));
            Assert.That(puzzle.Regions, Is.EqualTo(new[]
            {
                new[] { 0, 1, 2, 1 },
                new[] { 0, 1, 1, 1 },
                new[] { 0, 0, 3, 1 },
                new[] { 0, 3, 3, 1 }
            }));
            Assert.That(puzzle.SolutionColumns, Is.EqualTo(new[] { 2, 0, 3, 1 }));
            Assert.That(puzzle.ColorMap, Is.EqualTo(new[] { 8, 4, 10, 1 }));
            Assert.That(TutorialPuzzle.SourceBoardWidth, Is.EqualTo(919f));
        }

        [Test]
        public void TryFind_UsesPatternFieldAndRejectsOrdinaryFourByFourEntry()
        {
            LevelEntry ordinary = CreateEntry(50, string.Empty);
            LevelEntry guide = CreateEntry(51, "guide");

            Assert.That(
                TutorialPuzzle.TryFind(new[] { ordinary }, out TutorialPuzzle missing),
                Is.False);
            Assert.That(missing, Is.Null);
            Assert.That(
                TutorialPuzzle.TryFind(new[] { ordinary, guide }, out TutorialPuzzle found),
                Is.True);
            Assert.That(found.Id, Is.EqualTo(51));
            Assert.That(found.Pattern, Is.EqualTo("guide"));
        }

        [Test]
        public void PlaceCatSteps_RequireSameCellDoubleTapWithinSourceWindow()
        {
            var machine = CreateMachine();

            Assert.That(machine.Tap(0, 1, 0.00), Is.False);
            Assert.That(machine.Tap(0, 2, 0.00), Is.True);
            Assert.That(machine.GetCellState(0, 2), Is.EqualTo(CellStateType.EMPTY));
            Assert.That(machine.Tap(0, 2, 0.36), Is.True);
            Assert.That(machine.GetCellState(0, 2), Is.EqualTo(CellStateType.EMPTY));
            Assert.That(machine.Tap(0, 2, 0.50), Is.True);

            Assert.That(machine.GetCellState(0, 2), Is.EqualTo(CellStateType.CAT));
            Assert.That(machine.Phase, Is.EqualTo(TutorialPhase.ConfirmOnePerColor));
        }

        [Test]
        public void DefaultFlow_UsesAllSevenSourceInteractionsAndFinalConfirm()
        {
            var machine = CreateMachine();
            double time = 0;

            DoubleTap(machine, 0, 2, ref time);
            Assert.That(machine.Phase, Is.EqualTo(TutorialPhase.ConfirmOnePerColor));
            Assert.That(machine.Confirm(), Is.True);
            Assert.That(machine.Phase, Is.EqualTo(TutorialPhase.MarkRowAndColumn));
            Assert.That(machine.RequiredMarks, Is.EqualTo(6));

            TapAll(machine, RowColumnMarks, ref time);
            Assert.That(machine.Phase, Is.EqualTo(TutorialPhase.PlaceSecondCat));
            DoubleTap(machine, 3, 1, ref time);
            Assert.That(machine.Phase, Is.EqualTo(TutorialPhase.MarkNeighbors));
            Assert.That(machine.RequiredMarks, Is.EqualTo(3));

            TapAll(machine, NeighborMarks, ref time);
            Assert.That(machine.Phase, Is.EqualTo(TutorialPhase.PlaceThirdCat));
            DoubleTap(machine, 1, 0, ref time);
            Assert.That(machine.Phase, Is.EqualTo(TutorialPhase.FreePlay));

            Assert.That(machine.Tap(1, 1, time += 0.5), Is.True);
            Assert.That(machine.GetCellState(1, 1), Is.EqualTo(CellStateType.MARK));
            Assert.That(machine.Tap(1, 1, time += 0.5), Is.True);
            Assert.That(machine.GetCellState(1, 1), Is.EqualTo(CellStateType.EMPTY));
            DoubleTap(machine, 2, 3, ref time);

            Assert.That(machine.Phase, Is.EqualTo(TutorialPhase.FinishConfirm));
            Assert.That(machine.Confirm(), Is.True);
            Assert.That(machine.IsComplete, Is.True);
        }

        [Test]
        public void FreePlay_OnlyLastSolutionCellCanBecomeCat()
        {
            TutorialStateMachine machine = AdvanceDefaultToFreePlay(out double time);

            Assert.That(machine.Tap(1, 1, time += 0.1), Is.True);
            Assert.That(machine.Tap(1, 1, time += 0.1), Is.True);
            Assert.That(machine.GetCellState(1, 1), Is.EqualTo(CellStateType.EMPTY));
            Assert.That(machine.Phase, Is.EqualTo(TutorialPhase.FreePlay));

            DoubleTap(machine, 2, 3, ref time);
            Assert.That(machine.GetCellState(2, 3), Is.EqualTo(CellStateType.CAT));
            Assert.That(machine.Phase, Is.EqualTo(TutorialPhase.FinishConfirm));
        }

        [Test]
        public void HintFlow_RevealsThenAppliesTwoRowsAndLastCatInSixPresses()
        {
            TutorialStateMachine machine = AdvanceDefaultToFreePlay(out _);
            int presentationChanges = 0;
            machine.PresentationChanged += () => presentationChanges++;

            Assert.That(machine.PressHint(), Is.True);
            Assert.That(machine.HintPhase, Is.EqualTo(1));
            Assert.That(presentationChanges, Is.EqualTo(1));
            AssertCells(machine.AllowedCells, new Vector2Int(1, 1), new Vector2Int(1, 3));
            AssertCells(machine.MirrorCells, new Vector2Int(1, 0));

            Assert.That(machine.PressHint(), Is.True);
            Assert.That(machine.HintPhase, Is.Zero);
            Assert.That(presentationChanges, Is.EqualTo(2));
            Assert.That(machine.GetCellState(1, 1), Is.EqualTo(CellStateType.MARK));
            Assert.That(machine.GetCellState(1, 3), Is.EqualTo(CellStateType.MARK));

            Assert.That(machine.PressHint(), Is.True);
            Assert.That(machine.HintPhase, Is.EqualTo(2));
            Assert.That(presentationChanges, Is.EqualTo(3));
            AssertCells(machine.AllowedCells, new Vector2Int(3, 3));
            AssertCells(machine.MirrorCells, new Vector2Int(3, 1));

            Assert.That(machine.PressHint(), Is.True);
            Assert.That(machine.HintPhase, Is.Zero);
            Assert.That(presentationChanges, Is.EqualTo(4));
            Assert.That(machine.GetCellState(3, 3), Is.EqualTo(CellStateType.MARK));

            Assert.That(machine.PressHint(), Is.True);
            Assert.That(machine.HintPhase, Is.EqualTo(3));
            Assert.That(presentationChanges, Is.EqualTo(5));
            AssertCells(machine.AllowedCells, new Vector2Int(2, 3));

            Assert.That(machine.PressHint(), Is.True);
            Assert.That(machine.GetCellState(2, 3), Is.EqualTo(CellStateType.CAT));
            Assert.That(machine.Phase, Is.EqualTo(TutorialPhase.FinishConfirm));
            Assert.That(presentationChanges, Is.EqualTo(5));
        }

        [Test]
        public void CheckFlow_SkipsFirstConfirmAndGatesEveryInteractionWithFeedback()
        {
            var feedback = new GuideFeedbackConfig();
            feedback.SetDebugOverride(GuideFeedbackConfig.ValueCheck);
            var machine = CreateMachine(feedback);
            double time = 0;
            int requested = 0;
            machine.FeedbackRequested += (kind, before, after) =>
            {
                Assert.That(kind, Is.EqualTo(TutorialFeedbackKind.Check));
                Assert.That(before, Is.EqualTo(60));
                Assert.That(after, Is.EqualTo(60));
                requested++;
            };

            DoubleTap(machine, 0, 2, ref time);

            Assert.That(machine.Phase, Is.EqualTo(TutorialPhase.Feedback));
            Assert.That(machine.PendingFeedback, Is.EqualTo(TutorialFeedbackKind.Check));
            Assert.That(machine.CompleteFeedback(), Is.True);
            Assert.That(machine.Phase, Is.EqualTo(TutorialPhase.MarkRowAndColumn));
            Assert.That(machine.IqValue, Is.EqualTo(60));
            Assert.That(requested, Is.EqualTo(1));
        }

        [Test]
        public void IqFlow_IncrementsTwentyAfterEachOfSixInteractiveActions()
        {
            var feedback = new GuideFeedbackConfig();
            feedback.SetDebugOverride(GuideFeedbackConfig.ValueIq);
            var machine = CreateMachine(feedback);
            double time = 0;

            DoubleTap(machine, 0, 2, ref time);
            CompleteIqFeedback(machine, 80);
            TapAll(machine, RowColumnMarks, ref time);
            CompleteIqFeedback(machine, 100);
            DoubleTap(machine, 3, 1, ref time);
            CompleteIqFeedback(machine, 120);
            TapAll(machine, NeighborMarks, ref time);
            CompleteIqFeedback(machine, 140);
            DoubleTap(machine, 1, 0, ref time);
            CompleteIqFeedback(machine, 160);
            DoubleTap(machine, 2, 3, ref time);
            CompleteIqFeedback(machine, 180);

            Assert.That(machine.Phase, Is.EqualTo(TutorialPhase.FinishConfirm));
            Assert.That(machine.IqValue, Is.EqualTo(180));
        }

        [Test]
        public void ResetAndNewSession_ReturnToFirstStepWithoutPersistingProgress()
        {
            var machine = CreateMachine();
            double time = 0;
            DoubleTap(machine, 0, 2, ref time);
            machine.Confirm();
            machine.Tap(0, 0, time += 0.5);

            machine.Reset();
            var recreated = CreateMachine();

            Assert.That(machine.Phase, Is.EqualTo(TutorialPhase.PlaceFirstCat));
            Assert.That(machine.GetCellState(0, 2), Is.EqualTo(CellStateType.EMPTY));
            Assert.That(recreated.Phase, Is.EqualTo(TutorialPhase.PlaceFirstCat));
            Assert.That(recreated.MarkedCount, Is.Zero);
        }

        [Test]
        public void CompletionCommitter_SavesTutorialDoneExactlyOnce()
        {
            var store = new CountingStore();
            var service = new GameStateService(new GameStateData(), store);
            var committer = new TutorialCompletionCommitter();

            Assert.That(committer.Commit(service), Is.True);
            Assert.That(committer.Commit(service), Is.False);
            Assert.That(service.TutorialDone, Is.True);
            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void DiagonalVariant_OnlyChangesPresentationContract()
        {
            var diagonal = new TutorialDiagonalConfig();
            diagonal.SetDebugOverride(TutorialDiagonalConfig.ValueDiagonalCopy);
            var machine = new TutorialStateMachine(
                CreatePuzzle(),
                diagonalConfig: diagonal);

            Assert.That(machine.UsesDiagonalCopy, Is.True);
            AssertCells(machine.AllowedCells, new Vector2Int(0, 2));
        }

        private static TutorialStateMachine AdvanceDefaultToFreePlay(out double time)
        {
            var machine = CreateMachine();
            time = 0;
            DoubleTap(machine, 0, 2, ref time);
            machine.Confirm();
            TapAll(machine, RowColumnMarks, ref time);
            DoubleTap(machine, 3, 1, ref time);
            TapAll(machine, NeighborMarks, ref time);
            DoubleTap(machine, 1, 0, ref time);
            Assert.That(machine.Phase, Is.EqualTo(TutorialPhase.FreePlay));
            return machine;
        }

        private static void CompleteIqFeedback(TutorialStateMachine machine, int expectedIq)
        {
            Assert.That(machine.Phase, Is.EqualTo(TutorialPhase.Feedback));
            Assert.That(machine.PendingFeedback, Is.EqualTo(TutorialFeedbackKind.Iq));
            Assert.That(machine.CompleteFeedback(), Is.True);
            Assert.That(machine.IqValue, Is.EqualTo(expectedIq));
        }

        private static void DoubleTap(
            TutorialStateMachine machine,
            int row,
            int column,
            ref double time)
        {
            time += 0.5;
            Assert.That(machine.Tap(row, column, time), Is.True);
            time += 0.1;
            Assert.That(machine.Tap(row, column, time), Is.True);
        }

        private static void TapAll(
            TutorialStateMachine machine,
            IEnumerable<Vector2Int> cells,
            ref double time)
        {
            foreach (Vector2Int cell in cells)
            {
                time += 0.5;
                Assert.That(machine.Tap(cell.x, cell.y, time), Is.True);
            }
        }

        private static void AssertCells(
            IReadOnlyList<Vector2Int> actual,
            params Vector2Int[] expected)
        {
            Assert.That(actual, Is.EquivalentTo(expected));
        }

        private static TutorialStateMachine CreateMachine(GuideFeedbackConfig feedback = null)
        {
            return new TutorialStateMachine(CreatePuzzle(), feedback);
        }

        private static TutorialPuzzle CreatePuzzle()
        {
            return new TutorialPuzzle(
                51,
                "guide",
                CreateRegions(),
                new[] { 2, 0, 3, 1 },
                new[] { 8, 4, 10, 1 });
        }

        private static LevelEntry CreateEntry(int id, string pattern)
        {
            return LevelEntry.FromDictionary(new Dictionary<string, object>
            {
                { "id", id },
                { "pattern", pattern },
                { "size", 4 },
                { "regionMap", CreateRegions() },
                { "solution", new[] { 2, 0, 3, 1 } },
                { "colorMap", new[] { 8, 4, 10, 1 } }
            });
        }

        private static int[][] CreateRegions()
        {
            return new[]
            {
                new[] { 0, 1, 2, 1 },
                new[] { 0, 1, 1, 1 },
                new[] { 0, 0, 3, 1 },
                new[] { 0, 3, 3, 1 }
            };
        }

        private sealed class CountingStore : IGameStatePlayerStore
        {
            public int SaveCount { get; private set; }

            public bool SavePlayer(GameStateData data)
            {
                SaveCount++;
                return true;
            }
        }
    }
}
