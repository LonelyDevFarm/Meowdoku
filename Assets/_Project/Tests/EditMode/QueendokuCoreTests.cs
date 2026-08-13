using System.Collections.Generic;
using Meowdoku.Core;
using NUnit.Framework;
using UnityEngine;

namespace Meowdoku.Tests.EditMode
{
    public sealed class QueendokuCoreTests
    {
        private static readonly int[][] RegionsByRow =
        {
            new[] { 0, 0, 0, 0 },
            new[] { 1, 1, 1, 1 },
            new[] { 2, 2, 2, 2 },
            new[] { 3, 3, 3, 3 }
        };

        [Test]
        public void ValidateSolutionEntry_AcceptsKnownFourByFourSolution()
        {
            Assert.That(
                QueendokuCore.ValidateSolutionEntry(
                    RegionsByRow,
                    new[] { 1, 3, 0, 2 },
                    4),
                Is.True);
        }

        [Test]
        public void ValidateSolutionEntry_RejectsDuplicateColumn()
        {
            Assert.That(
                QueendokuCore.ValidateSolutionEntry(
                    RegionsByRow,
                    new[] { 1, 1, 0, 2 },
                    4),
                Is.False);
        }

        [Test]
        public void ValidateSolutionEntry_RejectsMalformedAndOutOfRangeDataSafely()
        {
            int[][] malformedRows =
            {
                new[] { 0, 0, 0, 0 },
                new[] { 1, 1, 1 },
                new[] { 2, 2, 2, 2 },
                null
            };

            Assert.That(
                QueendokuCore.ValidateSolutionEntry(
                    null, new[] { 1, 3, 0, 2 }, 4),
                Is.False);
            Assert.That(
                QueendokuCore.ValidateSolutionEntry(
                    malformedRows, new[] { 1, 3, 0, 2 }, 4),
                Is.False);
            Assert.That(
                QueendokuCore.ValidateSolutionEntry(
                    RegionsByRow, null, 4),
                Is.False);
            Assert.That(
                QueendokuCore.ValidateSolutionEntry(
                    RegionsByRow, new[] { 1, 3, 0 }, 4),
                Is.False);
            Assert.That(
                QueendokuCore.ValidateSolutionEntry(
                    RegionsByRow, new[] { -1, 3, 0, 2 }, 4),
                Is.False);
            Assert.That(
                QueendokuCore.ValidateSolutionEntry(
                    RegionsByRow, new[] { 1, 4, 0, 2 }, 4),
                Is.False);
        }

        [Test]
        public void FindConflicts_ReturnsEveryConflictingCatAndOnlyCats()
        {
            int[][] regions =
            {
                new[] { 0, 0, 0, 0, 0 },
                new[] { 1, 1, 1, 1, 1 },
                new[] { 2, 2, 2, 2, 2 },
                new[] { 3, 3, 3, 3, 3 },
                new[] { 4, 4, 4, 4, 4 }
            };
            CellStateType[][] board = EmptyBoard(5);
            board[0][0] = CellStateType.CAT;
            board[0][4] = CellStateType.CAT;
            board[2][2] = CellStateType.CAT;
            board[4][0] = CellStateType.CAT;
            board[4][4] = CellStateType.CAT;
            board[2][0] = CellStateType.MARK;
            board[2][4] = CellStateType.ERROR;

            Assert.That(
                QueendokuCore.FindConflicts(board, 5, regions),
                Is.EquivalentTo(new[]
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(0, 4),
                    new Vector2Int(4, 0),
                    new Vector2Int(4, 4)
                }));
        }

        [Test]
        public void CellsExcludedByCat_MatchesSourceRowMajorUnion()
        {
            int[][] regions =
            {
                new[] { 0, 1, 2, 3 },
                new[] { 1, 1, 2, 3 },
                new[] { 2, 2, 3, 3 },
                new[] { 3, 0, 0, 0 }
            };

            Assert.That(
                QueendokuCore.CellsExcludedByCat(
                    new Vector2Int(0, 0), 4, regions),
                Is.EqualTo(new[]
                {
                    new Vector2Int(0, 1),
                    new Vector2Int(0, 2),
                    new Vector2Int(0, 3),
                    new Vector2Int(1, 0),
                    new Vector2Int(1, 1),
                    new Vector2Int(2, 0),
                    new Vector2Int(3, 0),
                    new Vector2Int(3, 1),
                    new Vector2Int(3, 2),
                    new Vector2Int(3, 3)
                }));
        }

        [TestCase(0, 0, 0, 2, QueendokuCore.Rule.SameColor)]
        [TestCase(0, 0, 1, 0, QueendokuCore.Rule.SameLine)]
        [TestCase(0, 0, 1, 1, QueendokuCore.Rule.NoTouch)]
        [TestCase(0, 0, 1, 2, QueendokuCore.Rule.None)]
        public void ClassifyViolation_UsesSourceRulePriority(
            int row,
            int column,
            int placedRow,
            int placedColumn,
            QueendokuCore.Rule expected)
        {
            var placed = new List<Vector2Int>
            {
                new Vector2Int(placedRow, placedColumn)
            };

            Assert.That(
                QueendokuCore.ClassifyViolation(row, column, placed, RegionsByRow),
                Is.EqualTo(expected));
        }

        [Test]
        public void IsComplete_RequiresExactlyFourNonConflictingCats()
        {
            CellStateType[][] board = EmptyBoard(4);
            int[] solution = { 1, 3, 0, 2 };
            for (int row = 0; row < solution.Length; row++)
            {
                board[row][solution[row]] = CellStateType.CAT;
            }

            Assert.That(QueendokuCore.IsComplete(board, 4, RegionsByRow), Is.True);

            board[3][2] = CellStateType.EMPTY;
            Assert.That(QueendokuCore.IsComplete(board, 4, RegionsByRow), Is.False);
        }

        [Test]
        public void BoardStateModel_RejectsWrongCatAndProtectsPlacedCat()
        {
            var model = new BoardStateModel(4, RegionsByRow, new[] { 1, 3, 0, 2 });

            Assert.That(model.TrySetCellState(0, 0, CellStateType.CAT, out _), Is.False);
            Assert.That(model.TrySetCellState(0, 1, CellStateType.CAT, out _), Is.True);
            Assert.That(model.TrySetCellState(0, 1, CellStateType.EMPTY, out _), Is.False);
            Assert.That(model.GetCellState(0, 1), Is.EqualTo(CellStateType.CAT));
            Assert.That(model.CatCount, Is.EqualTo(1));
            Assert.That(model.RemainingCats, Is.EqualTo(3));
        }

        [Test]
        public void BoardStateModel_RestoreSupportsUndoOfCatPlacement()
        {
            var model = new BoardStateModel(4, RegionsByRow, new[] { 1, 3, 0, 2 });
            model.TrySetCellState(0, 1, CellStateType.CAT, out _);

            Assert.That(
                model.RestoreCellState(0, 1, CellStateType.MARK, out BoardStateChange change),
                Is.True);
            Assert.That(change.Before, Is.EqualTo(CellStateType.CAT));
            Assert.That(change.After, Is.EqualTo(CellStateType.MARK));
            Assert.That(model.CatCount, Is.Zero);
        }

        [Test]
        public void BoardStateModel_MarkErrorRejectsSolutionAndLockedMark()
        {
            var model = new BoardStateModel(4, RegionsByRow, new[] { 1, 3, 0, 2 });

            Assert.That(model.TryMarkCellError(0, 1, out _), Is.False);
            model.RestoreCellState(0, 0, CellStateType.LOCKED_MARK, out _);
            Assert.That(model.TryMarkCellError(0, 0, out _), Is.False);
            Assert.That(model.GetCellState(0, 0), Is.EqualTo(CellStateType.LOCKED_MARK));
        }

        [Test]
        public void BoardStateModel_PlacingCatHealsInvalidCatAndErrorLikeSource()
        {
            var model = new BoardStateModel(4, RegionsByRow, new[] { 1, 3, 0, 2 });
            model.RestoreCellState(1, 0, CellStateType.CAT, out _);
            model.RestoreCellState(2, 0, CellStateType.ERROR, out _);

            Assert.That(
                model.TrySetCellState(0, 1, CellStateType.CAT, out var changes),
                Is.True);
            Assert.That(changes, Has.Count.EqualTo(3));
            Assert.That(model.GetCellState(1, 0), Is.EqualTo(CellStateType.EMPTY));
            Assert.That(model.GetCellState(2, 0), Is.EqualTo(CellStateType.EMPTY));
            Assert.That(model.CatCount, Is.EqualTo(1));
        }

        [Test]
        public void BoardStateModel_ClassifiesWrongGuessAgainstPlacedCats()
        {
            var model = new BoardStateModel(4, RegionsByRow, new[] { 1, 3, 0, 2 });
            model.TrySetCellState(0, 1, CellStateType.CAT, out _);

            Assert.That(model.ClassifyViolation(0, 3), Is.EqualTo(QueendokuCore.Rule.SameColor));
            Assert.That(model.ClassifyViolation(1, 1), Is.EqualTo(QueendokuCore.Rule.SameLine));
            Assert.That(model.ClassifyViolation(1, 2), Is.EqualTo(QueendokuCore.Rule.NoTouch));
            Assert.That(model.FindConflictingCats(1, 2),
                Is.EqualTo(new[] { new Vector2Int(0, 1) }));
        }

        [Test]
        public void BoardStateModel_CountsCorrectAndFalseCrossesUsingSourcePredicate()
        {
            var model = new BoardStateModel(4, RegionsByRow, new[] { 1, 3, 0, 2 });
            model.RestoreCellState(0, 0, CellStateType.MARK, out _);
            model.RestoreCellState(1, 0, CellStateType.ERROR, out _);
            model.RestoreCellState(2, 1, CellStateType.LOCKED_MARK, out _);
            model.RestoreCellState(3, 2, CellStateType.MARK, out _);
            model.RestoreCellState(0, 2, CellStateType.DRAFT_CROSS, out _);

            Assert.That(model.CountCorrectCrosses(), Is.EqualTo(3));
            Assert.That(model.CountFalseCrosses(), Is.EqualTo(1));
        }

        [Test]
        public void HintEngine_MarkHintUsesSourceOrderingAndDeduplicatesDiagonal()
        {
            int[][] regions =
            {
                new[] { 0, 0, 0 },
                new[] { 1, 1, 2 },
                new[] { 1, 2, 2 }
            };
            CellStateType[][] board = EmptyBoard(3);
            board[0][0] = CellStateType.CAT;

            HintResult hint = HintEngine.FindMarkHint(board, 3, regions);

            Assert.That(hint.Found, Is.True);
            Assert.That(hint.Strategy, Is.EqualTo("R1_mark"));
            Assert.That(hint.CatCell, Is.EqualTo(new Vector2Int(0, 0)));
            Assert.That(hint.Cell, Is.EqualTo(new Vector2Int(0, 1)));
            Assert.That(hint.UnitCells, Is.EqualTo(new[]
            {
                new Vector2Int(0, 1), new Vector2Int(0, 2),
                new Vector2Int(1, 0), new Vector2Int(2, 0),
                new Vector2Int(1, 1)
            }));
        }

        [Test]
        public void HintEngine_R1FindsOnlyEmptyCandidateAndRejectsOtherStates()
        {
            int[][] regions =
            {
                new[] { 0, 0, 0 },
                new[] { 1, 1, 2 },
                new[] { 1, 2, 2 }
            };
            CellStateType[][] board = EmptyBoard(3);
            board[0][0] = CellStateType.DRAFT_CROSS;
            board[0][1] = CellStateType.LOCKED_MARK;

            HintResult hint = HintEngine.FindR1Hint(board, 3, regions);

            Assert.That(hint.Found, Is.True);
            Assert.That(hint.UnitType, Is.EqualTo("row"));
            Assert.That(hint.UnitIndex, Is.Zero);
            Assert.That(hint.Cell, Is.EqualTo(new Vector2Int(0, 2)));
        }

        [Test]
        public void HintEngine_R2FindsRegionCandidatesLockedToRow()
        {
            int[][] regions =
            {
                new[] { 0, 0, 1 },
                new[] { 0, 2, 1 },
                new[] { 2, 2, 1 }
            };
            CellStateType[][] board = EmptyBoard(3);
            board[1][0] = CellStateType.MARK;

            HintResult hint = HintEngine.FindR2Hint(board, 3, regions);

            Assert.That(hint.Found, Is.True);
            Assert.That(hint.Mode, Is.EqualTo("r2a_row"));
            Assert.That(hint.Region, Is.Zero);
            Assert.That(hint.Row, Is.Zero);
            Assert.That(hint.HighlightCells, Is.EqualTo(new[]
            {
                new Vector2Int(0, 0), new Vector2Int(0, 1)
            }));
        }

        [Test]
        public void HintEngine_R3UsesValidFourRegionSubset()
        {
            int[][] regions =
            {
                new[] { 0, 0, 2, 2 },
                new[] { 1, 1, 2, 2 },
                new[] { 3, 3, 3, 3 },
                new[] { 3, 3, 3, 3 }
            };

            HintResult hint = HintEngine.FindR3R4Hint(EmptyBoard(4), 4, regions);

            Assert.That(hint.Found, Is.True);
            Assert.That(hint.Strategy, Is.EqualTo("R3"));
            Assert.That(hint.Regions, Is.EqualTo(new[] { 0, 1 }));
            Assert.That(hint.LockedRows, Is.EqualTo(new[] { 0, 1 }));
            Assert.That(hint.LockedColumns, Is.Empty);
        }

        [Test]
        public void HintEngine_R4UsesFourRegionSubsetWithoutEarlierPairOrTriple()
        {
            // Regions 0..3 each span all first four rows and columns. No pair
            // or triple can lock exactly k units; all four together lock rows
            // 0..3 and expose the region-4 cells in column 4 for marking.
            int[][] regions =
            {
                new[] { 0, 1, 2, 3, 4 },
                new[] { 1, 2, 3, 0, 4 },
                new[] { 2, 3, 0, 1, 4 },
                new[] { 3, 0, 1, 2, 4 },
                new[] { 4, 4, 4, 4, 4 }
            };

            HintResult hint = HintEngine.FindR3R4Hint(
                EmptyBoard(5),
                5,
                regions);

            Assert.That(hint.Found, Is.True);
            Assert.That(hint.Strategy, Is.EqualTo("R4"));
            Assert.That(hint.Regions, Is.EqualTo(new[] { 0, 1, 2, 3 }));
            Assert.That(hint.LockedRows, Is.EqualTo(new[] { 0, 1, 2, 3 }));
            Assert.That(hint.LockedColumns, Is.Empty);
            Assert.That(hint.HighlightCells, Has.Count.EqualTo(16));
        }

        [Test]
        public void HintEngine_ChainReportsDirectContradiction()
        {
            int[][] regions =
            {
                new[] { 0, 0, 0 },
                new[] { 1, 1, 2 },
                new[] { 1, 2, 2 }
            };
            CellStateType[][] board = EmptyBoard(3);
            board[1][2] = CellStateType.MARK;

            HintResult hint = HintEngine.FindChainHint(board, 3, regions);

            Assert.That(hint.Found, Is.True);
            Assert.That(hint.Strategy, Is.EqualTo("R4_chain"));
            Assert.That(hint.Cell, Is.EqualTo(new Vector2Int(0, 0)));
            Assert.That(hint.Chain.Depth, Is.Zero);
            Assert.That(hint.Chain.ContradictionType, Is.EqualTo("row"));
            Assert.That(hint.Chain.ContradictionIndex, Is.EqualTo(1));
        }

        [Test]
        public void HintEngine_CellRanksFallBackWhenR1ToR3CannotProgress()
        {
            bool[][] solution = HintEngine.SolutionMatrix(4, new[] { 1, 3, 0, 2 });

            Dictionary<Vector2Int, int> ranks = HintEngine.ComputeCellRanks(
                EmptyBoard(4), 4, RegionsByRow, solution, 7);

            Assert.That(ranks, Has.Count.EqualTo(4));
            Assert.That(ranks[new Vector2Int(0, 1)], Is.EqualTo(7));
            Assert.That(ranks[new Vector2Int(1, 3)], Is.EqualTo(7));
            Assert.That(ranks[new Vector2Int(2, 0)], Is.EqualTo(7));
            Assert.That(ranks[new Vector2Int(3, 2)], Is.EqualTo(7));
        }

        [Test]
        public void PreCatDecider_UsesRankedCellsAndSourceSceneOrder()
        {
            List<int> scenes = PreCatDecider.HitScenarios(true, 3, true, true);
            bool[][] solution = HintEngine.SolutionMatrix(4, new[] { 1, 3, 0, 2 });

            PreCatDecision decision = PreCatDecider.Decide(
                PreCatDecider.ValueAlways,
                scenes,
                4,
                RegionsByRow,
                solution,
                new FixedInclusiveRandom(2));

            Assert.That(PreCatDecider.ScenesToPreType(scenes), Is.EqualTo("1&2&3"));
            Assert.That(decision.HasPlacement, Is.True);
            Assert.That(decision.PreType, Is.EqualTo("1&2&3"));
            Assert.That(decision.Position, Is.EqualTo(new Vector2Int(2, 0)));
        }

        [Test]
        public void PreCatDecider_HalfGroupCanSkipPlacement()
        {
            PreCatDecision decision = PreCatDecider.Decide(
                PreCatDecider.ValueHalf,
                new[] { PreCatDecider.SceneConsecutiveFail },
                4,
                RegionsByRow,
                HintEngine.SolutionMatrix(4, new[] { 1, 3, 0, 2 }),
                new FixedInclusiveRandom(1));

            Assert.That(decision.HasPlacement, Is.False);
            Assert.That(decision.PreType, Is.EqualTo(PreCatDecider.PreTypeNone));
        }

        private static CellStateType[][] EmptyBoard(int size)
        {
            var board = new CellStateType[size][];
            for (int row = 0; row < size; row++)
            {
                board[row] = new CellStateType[size];
            }
            return board;
        }

        private sealed class FixedInclusiveRandom : IInclusiveRandom
        {
            private readonly int _value;
            public FixedInclusiveRandom(int value) { _value = value; }
            public int RangeInclusive(int minimum, int maximum)
            {
                return Mathf.Clamp(_value, minimum, maximum);
            }
        }
    }
}
