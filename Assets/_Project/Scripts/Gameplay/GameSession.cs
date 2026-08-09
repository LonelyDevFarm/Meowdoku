using System;
using System.Collections;
using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.Config;
using Meowdoku.Gameplay.Input;
using UnityEngine;

namespace Meowdoku.Gameplay
{
    public enum GameSessionState
    {
        Loading,
        Entering,
        Playing,
        ResolvingWrongGuess,
        Won,
        Failed,
        Leaving
    }

    public enum SessionActionKind
    {
        None,
        BoardEdit,
        CorrectCat,
        WrongGuess,
        Undo,
        Clear,
        Locate,
        Hint,
        AutoComplete
    }

    public sealed class SessionActionResult
    {
        public bool Accepted { get; internal set; }
        public SessionActionKind Kind { get; internal set; }
        public IReadOnlyList<BoardStateChange> Changes { get; internal set; } =
            Array.Empty<BoardStateChange>();
        public QueendokuCore.Rule RuleViolation { get; internal set; }
        public IReadOnlyList<Vector2Int> ConflictingCats { get; internal set; } =
            Array.Empty<Vector2Int>();
        public ScoreGainResult ScoreGain { get; internal set; }
        public int Deduction { get; internal set; }
        public int LivesBefore { get; internal set; }
        public int LivesAfter { get; internal set; }
        public bool IsComplete { get; internal set; }
        public IReadOnlyList<GameplayFeedbackData> Feedback { get; internal set; } =
            Array.Empty<GameplayFeedbackData>();
    }

    public sealed class GameSessionRestoreData
    {
        public int Lives { get; set; } = 3;
        public int ReviveCount { get; set; }
        public int RestartCount { get; set; }
        public int SuccessfulCatCount { get; set; }
        public Dictionary<string, int> Score { get; set; } = new Dictionary<string, int>();
        public IList StepHistoryData { get; set; }
        public List<Vector2Int> PlacedCats { get; } = new List<Vector2Int>();
        public List<Vector2Int> Marks { get; } = new List<Vector2Int>();
        public List<Vector2Int> Errors { get; } = new List<Vector2Int>();
    }

    public sealed class SessionHintRequest
    {
        public bool Found { get; internal set; }
        public bool WrongMark { get; internal set; }
        public bool RequiresLocateFallback { get; internal set; }
        public HintResult Hint { get; internal set; }
        public Vector2Int WrongMarkCell { get; internal set; } = new Vector2Int(-1, -1);
    }

    /// <summary>
    /// Pure per-game controller extracted from BaseGamePage/GamePage. Timers,
    /// animation, persistence scheduling and feedback remain outside this class.
    /// </summary>
    public sealed class GameSession : IBoardStateReader
    {
        private readonly List<StepHistory.CellChange> _currentStep =
            new List<StepHistory.CellChange>();
        private readonly ScoreEncourageConfig _scoreConfig;
        private readonly Dictionary<Vector2Int, int> _cellRanks;
        private readonly int _size;
        private readonly int[][] _regions;
        private readonly int[] _solutionColumns;
        private readonly HintMutex _hintMutex = new HintMutex();
        private SessionHintRequest _pendingHint;
        private int _successfulCatCount;
        private bool _winLifeBonusApplied;

        public GameSessionState State { get; private set; } = GameSessionState.Loading;
        public BoardStateModel Board { get; }
        public GameScoreModel Score { get; } = new GameScoreModel();
        public StepHistory History { get; } = new StepHistory();
        public int Lives { get; private set; }
        public int MistakeCount { get; private set; }
        public int ReviveCount { get; private set; }
        public int RestartCount { get; private set; }
        public QueendokuCore.Rule LastRuleViolation { get; private set; }
        public bool CanAcceptInput => State == GameSessionState.Playing && !_hintMutex.IsActive;
        public bool HasPendingHint => _pendingHint != null;
        public int CorrectCrossCount => Board.CountCorrectCrosses();
        public int FalseCrossCount => Board.CountFalseCrosses();
        public int RemainingCats => Board.RemainingCats;

        public GameSession(
            int size,
            int[][] regions,
            int[] solutionColumns,
            int fallbackRank,
            ScoreEncourageConfig scoreConfig,
            GameSessionRestoreData restore = null)
        {
            _scoreConfig = scoreConfig ?? throw new ArgumentNullException(nameof(scoreConfig));
            _size = size;
            _regions = Clone(regions);
            _solutionColumns = (int[])solutionColumns.Clone();
            Board = new BoardStateModel(size, regions, solutionColumns);
            State = GameSessionState.Entering;
            Lives = 3;

            _cellRanks = _scoreConfig.HasSkillScore()
                ? HintEngine.ComputeCellRanks(
                    Board.GetBoardSnapshot(),
                    size,
                    regions,
                    HintEngine.SolutionMatrix(size, solutionColumns),
                    fallbackRank)
                : null;

            if (restore != null) Restore(restore);
        }

        public void FinishEntering()
        {
            if (State == GameSessionState.Entering) State = GameSessionState.Playing;
        }

        public CellStateType GetCellState(int row, int column)
        {
            return Board.GetCellState(row, column);
        }

        public bool TryApplyBoardEdit(
            int row,
            int column,
            CellStateType state,
            bool record,
            out SessionActionResult result)
        {
            result = Rejected();
            if (!CanAcceptInput || !Board.TrySetCellState(row, column, state, out IReadOnlyList<BoardStateChange> changes))
                return false;

            if (record && changes.Count > 0)
            {
                BoardStateChange primary = changes[0];
                _currentStep.Add(new StepHistory.CellChange
                {
                    Position = primary.Position,
                    Before = primary.Before,
                    After = primary.After
                });
            }
            result = new SessionActionResult
            {
                Accepted = true,
                Kind = SessionActionKind.BoardEdit,
                Changes = changes,
                LivesBefore = Lives,
                LivesAfter = Lives
            };
            return true;
        }

        public SessionActionResult DoubleTap(int row, int column)
        {
            if (!CanAcceptInput || Board.GetCellState(row, column) == CellStateType.CAT)
                return Rejected();

            CellStateType before = Board.GetCellState(row, column);
            bool correct = Board.IsSolutionCell(row, column);
            // Source BoardView.play_error_feedback keeps a wrong double-tap in
            // ERROR. It is intentionally folded to MARK only for hint solving.
            CellStateType after = correct ? CellStateType.CAT : CellStateType.ERROR;
            if (!Board.TrySetCellState(row, column, after, out IReadOnlyList<BoardStateChange> changes))
                return Rejected();

            _currentStep.Add(new StepHistory.CellChange
            {
                Position = new Vector2Int(row, column),
                Before = before,
                After = after
            });

            int livesBefore = Lives;
            var result = new SessionActionResult
            {
                Accepted = true,
                Kind = correct ? SessionActionKind.CorrectCat : SessionActionKind.WrongGuess,
                Changes = changes,
                LivesBefore = livesBefore
            };

            if (correct)
            {
                LastRuleViolation = QueendokuCore.Rule.None;
                int rank = _cellRanks != null && _cellRanks.TryGetValue(
                    new Vector2Int(row, column), out int value) ? value : 1;
                int scoreBefore = Score.Score;
                result.ScoreGain = GameScoringRules.ApplyCorrectCat(
                    Score,
                    _scoreConfig,
                    ref _successfulCatCount,
                    rank);
                var feedback = new List<GameplayFeedbackData>
                {
                    BuildCorrectFeedback(
                        new Vector2Int(row, column),
                        GameplayFeedbackSource.UserAction,
                        scoreBefore,
                        result.ScoreGain)
                };
                CommitCurrentStep(true, false);
                if (Board.IsComplete()) State = GameSessionState.Won;
                AppendWinLifeBonusFeedback(feedback);
                result.Feedback = feedback;
            }
            else
            {
                LastRuleViolation = Board.ClassifyViolation(row, column);
                result.RuleViolation = LastRuleViolation;
                result.ConflictingCats = Board.FindConflictingCats(row, column);
                MistakeCount++;
                Lives = Math.Max(Lives - 1, 0);
                int scoreBefore = Score.Score;
                result.Deduction = GameScoringRules.ApplyWrongGuess(
                    Score,
                    _scoreConfig,
                    ref _successfulCatCount);
                result.Feedback = new[]
                {
                    new GameplayFeedbackData
                    {
                        Kind = GameplayFeedbackKind.WrongGuess,
                        Source = GameplayFeedbackSource.UserAction,
                        Position = new Vector2Int(row, column),
                        ScoreBefore = scoreBefore,
                        ScoreAfter = Score.Score,
                        Deduction = result.Deduction,
                        LivesBefore = livesBefore,
                        LivesAfter = Lives,
                        RuleViolation = LastRuleViolation,
                        ConflictingCats = result.ConflictingCats
                    }
                };
                CommitCurrentStep(false, true);
                State = GameSessionState.ResolvingWrongGuess;
            }

            result.LivesAfter = Lives;
            result.IsComplete = State == GameSessionState.Won;
            return result;
        }

        public void CommitCurrentStep(bool isCatPlacement = false, bool isWrongGuess = false)
        {
            if (_currentStep.Count == 0) return;
            var step = new StepHistory.StepRecord
            {
                IsCatPlacement = isCatPlacement,
                IsWrongGuess = isWrongGuess
            };
            step.Cells.AddRange(_currentStep);
            History.Push(step);
            _currentStep.Clear();
        }

        public GameSessionState ResolveWrongGuess()
        {
            if (State == GameSessionState.ResolvingWrongGuess)
                State = Lives <= 0 ? GameSessionState.Failed : GameSessionState.Playing;
            return State;
        }

        public bool Revive(int livesToRestore)
        {
            if (State != GameSessionState.Failed || livesToRestore <= 0) return false;
            Lives = Math.Min(Lives + livesToRestore, 3);
            ReviveCount++;
            State = GameSessionState.Playing;
            return true;
        }

        public SessionActionResult Undo()
        {
            if (!CanAcceptInput) return Rejected();
            StepHistory.StepRecord step = History.PopLast();
            if (step == null) return Rejected();
            var changes = new List<BoardStateChange>();
            for (int i = step.Cells.Count - 1; i >= 0; i--)
            {
                StepHistory.CellChange entry = step.Cells[i];
                if (Board.RestoreCellState(
                        entry.Position.x,
                        entry.Position.y,
                        entry.Before,
                        out BoardStateChange change) && change != null)
                    changes.Add(change);
            }
            if (step.IsCatPlacement) Score.ResetCombo();
            return new SessionActionResult
            {
                Accepted = true,
                Kind = SessionActionKind.Undo,
                Changes = changes,
                LivesBefore = Lives,
                LivesAfter = Lives
            };
        }

        public SessionActionResult ClearMarks()
        {
            if (!CanAcceptInput) return Rejected();
            var changes = new List<BoardStateChange>();
            for (int row = 0; row < _size; row++)
            {
                for (int column = 0; column < _size; column++)
                {
                    if (Board.GetCellState(row, column) != CellStateType.MARK) continue;
                    if (Board.RestoreCellState(row, column, CellStateType.EMPTY, out BoardStateChange change) && change != null)
                        changes.Add(change);
                }
            }
            return new SessionActionResult
            {
                Accepted = true,
                Kind = SessionActionKind.Clear,
                Changes = changes,
                LivesBefore = Lives,
                LivesAfter = Lives
            };
        }

        public SessionActionResult Locate()
        {
            if (!CanAcceptInput) return Rejected();
            var regionRemaining = new Dictionary<int, int>();
            for (int row = 0; row < _size; row++)
            {
                for (int column = 0; column < _size; column++)
                {
                    CellStateType state = Board.GetCellState(row, column);
                    if (state == CellStateType.MARK || state == CellStateType.ERROR) continue;
                    int region = _regions[row][column];
                    regionRemaining[region] = regionRemaining.TryGetValue(region, out int count)
                        ? count + 1
                        : 1;
                }
            }

            Vector2Int best = new Vector2Int(-1, -1);
            int bestSize = int.MaxValue;
            for (int row = 0; row < _size; row++)
            {
                int column = _solutionColumns[row];
                if (Board.GetCellState(row, column) == CellStateType.CAT) continue;
                int region = _regions[row][column];
                int remaining = regionRemaining.TryGetValue(region, out int count)
                    ? count
                    : _size * _size;
                if (remaining >= bestSize) continue;
                bestSize = remaining;
                best = new Vector2Int(row, column);
            }
            if (best.x < 0 || !Board.TrySetCellState(
                    best.x, best.y, CellStateType.CAT, out IReadOnlyList<BoardStateChange> changes))
                return Rejected();

            BoardStateChange primary = changes[0];
            _currentStep.Add(new StepHistory.CellChange
            {
                Position = primary.Position,
                Before = primary.Before,
                After = primary.After
            });
            int scoreBefore = Score.Score;
            ScoreGainResult gain = GameScoringRules.ApplyCorrectCat(
                Score,
                _scoreConfig,
                ref _successfulCatCount,
                1,
                true);
            CommitCurrentStep(true, false);
            if (Board.IsComplete()) State = GameSessionState.Won;
            var feedback = new List<GameplayFeedbackData>
            {
                BuildCorrectFeedback(best, GameplayFeedbackSource.Locate, scoreBefore, gain)
            };
            AppendWinLifeBonusFeedback(feedback);
            return new SessionActionResult
            {
                Accepted = true,
                Kind = SessionActionKind.Locate,
                Changes = changes,
                ScoreGain = gain,
                LivesBefore = Lives,
                LivesAfter = Lives,
                IsComplete = State == GameSessionState.Won,
                Feedback = feedback
            };
        }

        public SessionHintRequest RequestHint(bool allowLocateFallback = false)
        {
            if (!CanAcceptInput || !_hintMutex.TryAcquire("game_hint"))
                return new SessionHintRequest();

            CellStateType[][] board = FoldedBoard();
            HintResult hint = HintEngine.FindMarkHint(board, _size, _regions);
            bool wrongMark = false;
            if (!hint.Found)
            {
                for (int row = 0; row < _size && !wrongMark; row++)
                {
                    int column = _solutionColumns[row];
                    if (Board.GetCellState(row, column) != CellStateType.MARK) continue;
                    wrongMark = true;
                    _pendingHint = new SessionHintRequest
                    {
                        Found = true,
                        WrongMark = true,
                        WrongMarkCell = new Vector2Int(row, column)
                    };
                }
            }
            if (!wrongMark && !hint.Found) hint = HintEngine.FindR1Hint(board, _size, _regions);
            if (!wrongMark && !hint.Found) hint = HintEngine.FindR2Hint(board, _size, _regions);
            if (!wrongMark && !hint.Found) hint = HintEngine.FindR3R4Hint(board, _size, _regions);
            if (!wrongMark && !hint.Found) hint = HintEngine.FindChainHint(board, _size, _regions);

            if (!wrongMark && !hint.Found)
            {
                _hintMutex.Release("game_hint");
                return new SessionHintRequest
                {
                    Found = false,
                    RequiresLocateFallback = allowLocateFallback
                };
            }
            if (!wrongMark)
                _pendingHint = new SessionHintRequest { Found = true, Hint = hint };
            return _pendingHint;
        }

        public SessionActionResult ApplyHint()
        {
            if (State != GameSessionState.Playing || _pendingHint == null) return Rejected();
            SessionHintRequest request = _pendingHint;
            HintResult hint = request.Hint;
            var changes = new List<BoardStateChange>();
            bool catStep = false;
            ScoreGainResult scoreGain = null;
            int scoreBefore = Score.Score;
            Vector2Int catPosition = new Vector2Int(-1, -1);

            if (request.WrongMark)
                ApplyHintState(request.WrongMarkCell, CellStateType.EMPTY, changes, CellStateType.MARK);
            else if (hint.Strategy == "R1_mark")
                ApplyHintMarks(hint.UnitCells, changes);
            else if (hint.Strategy == "R2")
                ApplyHintMarks(R2TargetCells(hint), changes);
            else if (hint.Strategy == "R3" || hint.Strategy == "R4")
                ApplyHintMarks(R3TargetCells(hint), changes);
            else if (hint.Strategy == "R4_chain" || hint.Strategy == "R5_chain")
                ApplyHintMarks(new[] { hint.Cell }, changes);
            else
            {
                ApplyHintState(hint.Cell, CellStateType.CAT, changes, Board.GetCellState(hint.Cell.x, hint.Cell.y));
                if (changes.Count > 0)
                {
                    scoreGain = GameScoringRules.ApplyCorrectCat(
                        Score, _scoreConfig, ref _successfulCatCount, 1, true);
                    catStep = true;
                    catPosition = hint.Cell;
                }
            }

            CommitCurrentStep(catStep, false);
            _pendingHint = null;
            _hintMutex.Release("game_hint");
            if (Board.IsComplete()) State = GameSessionState.Won;
            var feedback = new List<GameplayFeedbackData>();
            if (scoreGain != null)
                feedback.Add(BuildCorrectFeedback(
                    catPosition,
                    GameplayFeedbackSource.Hint,
                    scoreBefore,
                    scoreGain));
            AppendWinLifeBonusFeedback(feedback);
            return new SessionActionResult
            {
                Accepted = true,
                Kind = SessionActionKind.Hint,
                Changes = changes,
                ScoreGain = scoreGain,
                LivesBefore = Lives,
                LivesAfter = Lives,
                IsComplete = State == GameSessionState.Won,
                Feedback = feedback
            };
        }

        public void CancelHint()
        {
            _pendingHint = null;
            _hintMutex.Release("game_hint");
        }

        public SessionActionResult AutoComplete()
        {
            if (!CanAcceptInput) return Rejected();
            var changes = new List<BoardStateChange>();
            var feedback = new List<GameplayFeedbackData>();
            ScoreGainResult lastGain = null;
            for (int ring = 0; ring <= (_size - 1) * 2; ring++)
            {
                for (int row = 0; row < _size; row++)
                {
                    for (int column = 0; column < _size; column++)
                    {
                        if (column + (_size - 1 - row) != ring ||
                            Board.IsSolutionCell(row, column) ||
                            !CellState.IsBlank(Board.GetCellState(row, column))) continue;
                        if (Board.TrySetCellState(row, column, CellStateType.MARK, out IReadOnlyList<BoardStateChange> markChanges))
                            changes.AddRange(markChanges);
                    }
                }
            }
            for (int ring = 0; ring <= (_size - 1) * 2; ring++)
            {
                for (int row = 0; row < _size; row++)
                {
                    int column = _solutionColumns[row];
                    if (column + (_size - 1 - row) != ring || Board.GetCellState(row, column) == CellStateType.CAT)
                        continue;
                    if (!Board.TrySetCellState(row, column, CellStateType.CAT, out IReadOnlyList<BoardStateChange> catChanges))
                        continue;
                    changes.AddRange(catChanges);
                    int rank = _cellRanks != null && _cellRanks.TryGetValue(
                        new Vector2Int(row, column), out int value) ? value : 1;
                    int scoreBefore = Score.Score;
                    lastGain = GameScoringRules.ApplyCorrectCat(
                        Score, _scoreConfig, ref _successfulCatCount, rank);
                    feedback.Add(BuildCorrectFeedback(
                        new Vector2Int(row, column),
                        GameplayFeedbackSource.AutoComplete,
                        scoreBefore,
                        lastGain));
                }
            }
            if (Board.IsComplete()) State = GameSessionState.Won;
            AppendWinLifeBonusFeedback(feedback);
            return new SessionActionResult
            {
                Accepted = true,
                Kind = SessionActionKind.AutoComplete,
                Changes = changes,
                ScoreGain = lastGain,
                LivesBefore = Lives,
                LivesAfter = Lives,
                IsComplete = State == GameSessionState.Won,
                Feedback = feedback
            };
        }

        private GameplayFeedbackData BuildCorrectFeedback(
            Vector2Int position,
            GameplayFeedbackSource source,
            int scoreBefore,
            ScoreGainResult gain)
        {
            float previousMultiplier = _scoreConfig.IsEnabled()
                ? _scoreConfig.CalculateMultiplier(_successfulCatCount - 1)
                : 1f;
            int displayGain = gain.Multiplier > 1f
                ? gain.BaseGain
                : (int)(gain.BaseGain * gain.Multiplier);
            return new GameplayFeedbackData
            {
                Kind = GameplayFeedbackKind.CorrectCat,
                Source = source,
                Position = position,
                ComboCount = Score.Combo,
                SuccessfulCatCount = _successfulCatCount,
                ScoreBefore = scoreBefore,
                ScoreAfter = Score.Score,
                BaseGain = gain.BaseGain,
                DisplayGain = displayGain,
                Multiplier = gain.Multiplier,
                PreviousMultiplier = previousMultiplier,
                SkillBonus = gain.SkillBonus,
                TotalGain = gain.TotalGain,
                LivesBefore = Lives,
                LivesAfter = Lives,
                UsesScoreEncourage = _scoreConfig.IsEnabled(),
                ShowsMultiplier = _scoreConfig.HasMultiplierDisplay() &&
                                  gain.Multiplier > 1f,
                UsesScrollMultiplierAnimation =
                    _scoreConfig.HasScrollMultiplierAnimation(),
                HasFlyEffect = _scoreConfig.HasFlyEffect(),
                FlyDelaySeconds = ScoreFlyDelaySeconds(gain.Multiplier)
            };
        }

        private void AppendWinLifeBonusFeedback(List<GameplayFeedbackData> feedback)
        {
            if (State != GameSessionState.Won || _winLifeBonusApplied) return;
            _winLifeBonusApplied = true;
            if (!_scoreConfig.HasLifeBonus()) return;

            IReadOnlyList<int> sequence = _scoreConfig.CalculateLifeBonusSequence(Lives);
            for (int index = 0; index < sequence.Count; index++)
            {
                int scoreBefore = Score.Score;
                int bonus = sequence[index];
                Score.AddScore(bonus);
                feedback.Add(new GameplayFeedbackData
                {
                    Kind = GameplayFeedbackKind.LifeBonus,
                    Source = GameplayFeedbackSource.UserAction,
                    ScoreBefore = scoreBefore,
                    ScoreAfter = Score.Score,
                    TotalGain = bonus,
                    DisplayGain = bonus,
                    LivesBefore = Lives,
                    LivesAfter = Lives,
                    LifeSlotIndex = index,
                    UsesScoreEncourage = true,
                    HasFlyEffect = true
                });
            }
        }

        private float ScoreFlyDelaySeconds(float multiplier)
        {
            if (!_scoreConfig.HasFlyEffect()) return 0f;
            if (!_scoreConfig.HasMultiplierDisplay()) return 0.8f;
            if (multiplier <= 1f) return 0.8f;
            if (_scoreConfig.HasScrollMultiplierAnimation()) return 1.367f;
            if (_scoreConfig.HasAppear4MultiplierAnimation()) return 1.45f;
            return 0.8f;
        }

        private CellStateType[][] FoldedBoard()
        {
            CellStateType[][] board = Board.GetBoardSnapshot();
            for (int row = 0; row < _size; row++)
                for (int column = 0; column < _size; column++)
                    if (board[row][column] == CellStateType.ERROR ||
                        board[row][column] == CellStateType.LOCKED_MARK)
                        board[row][column] = CellStateType.MARK;
            return board;
        }

        private void ApplyHintMarks(
            IEnumerable<Vector2Int> cells,
            List<BoardStateChange> result)
        {
            foreach (Vector2Int cell in cells)
            {
                if (!CellState.IsBlank(Board.GetCellState(cell.x, cell.y))) continue;
                ApplyHintState(cell, CellStateType.MARK, result, CellStateType.EMPTY);
            }
        }

        private void ApplyHintState(
            Vector2Int cell,
            CellStateType state,
            List<BoardStateChange> result,
            CellStateType recordedBefore)
        {
            if (cell.x < 0 || !Board.TrySetCellState(
                    cell.x,
                    cell.y,
                    state,
                    out IReadOnlyList<BoardStateChange> changes) ||
                changes.Count == 0)
                return;
            result.AddRange(changes);
            _currentStep.Add(new StepHistory.CellChange
            {
                Position = cell,
                Before = recordedBefore,
                After = state
            });
        }

        private List<Vector2Int> R2TargetCells(HintResult hint)
        {
            var result = new List<Vector2Int>();
            if (hint.Mode == "r2a_row")
            {
                for (int column = 0; column < _size; column++)
                    if (_regions[hint.Row][column] != hint.Region &&
                        CellState.IsBlank(Board.GetCellState(hint.Row, column)))
                        result.Add(new Vector2Int(hint.Row, column));
            }
            else if (hint.Mode == "r2a_col")
            {
                for (int row = 0; row < _size; row++)
                    if (_regions[row][hint.Column] != hint.Region &&
                        CellState.IsBlank(Board.GetCellState(row, hint.Column)))
                        result.Add(new Vector2Int(row, hint.Column));
            }
            else if (hint.Mode == "r2b_row")
            {
                for (int row = 0; row < _size; row++)
                    for (int column = 0; column < _size; column++)
                        if (_regions[row][column] == hint.Region && row != hint.Row &&
                            CellState.IsBlank(Board.GetCellState(row, column)))
                            result.Add(new Vector2Int(row, column));
            }
            else if (hint.Mode == "r2b_col")
            {
                for (int row = 0; row < _size; row++)
                    for (int column = 0; column < _size; column++)
                        if (_regions[row][column] == hint.Region && column != hint.Column &&
                            CellState.IsBlank(Board.GetCellState(row, column)))
                            result.Add(new Vector2Int(row, column));
            }
            return result;
        }

        private List<Vector2Int> R3TargetCells(HintResult hint)
        {
            var result = new List<Vector2Int>();
            var regions = new HashSet<int>(hint.Regions);
            for (int i = 0; i < hint.LockedRows.Count; i++)
            {
                int row = hint.LockedRows[i];
                for (int column = 0; column < _size; column++)
                    if (!regions.Contains(_regions[row][column]) &&
                        CellState.IsBlank(Board.GetCellState(row, column)))
                        result.Add(new Vector2Int(row, column));
            }
            for (int i = 0; i < hint.LockedColumns.Count; i++)
            {
                int column = hint.LockedColumns[i];
                for (int row = 0; row < _size; row++)
                {
                    var cell = new Vector2Int(row, column);
                    if (!regions.Contains(_regions[row][column]) &&
                        CellState.IsBlank(Board.GetCellState(row, column)) &&
                        !result.Contains(cell))
                        result.Add(cell);
                }
            }
            return result;
        }

        public bool ApplyPrefill(int row, int column, out IReadOnlyList<BoardStateChange> changes)
        {
            changes = Array.Empty<BoardStateChange>();
            return State == GameSessionState.Entering &&
                   Board.TrySetCellState(row, column, CellStateType.CAT, out changes);
        }

        public void BeginLeaving()
        {
            State = GameSessionState.Leaving;
        }

        public Dictionary<string, object> CreateSnapshot()
        {
            var cats = new List<object>();
            var marks = new List<object>();
            var errors = new List<object>();
            CellStateType[][] board = Board.GetBoardSnapshot();
            for (int row = 0; row < board.Length; row++)
            {
                for (int column = 0; column < board[row].Length; column++)
                {
                    List<object> position = null;
                    if (board[row][column] == CellStateType.CAT) position = new List<object> { row, column };
                    else if (board[row][column] == CellStateType.MARK) position = new List<object> { row, column };
                    else if (board[row][column] == CellStateType.ERROR) position = new List<object> { row, column };
                    if (position == null) continue;
                    if (board[row][column] == CellStateType.CAT) cats.Add(position);
                    else if (board[row][column] == CellStateType.MARK) marks.Add(position);
                    else errors.Add(position);
                }
            }
            return new Dictionary<string, object>
            {
                { "lives", Lives },
                { "placed_cats", cats },
                { "marks", marks },
                { "errors", errors },
                { "step_history", History.Serialize() },
                { "score", Score.Score },
                { "combo", Score.Combo },
                { "max_combo", Score.MaxCombo },
                { "se_count", _successfulCatCount },
                { "restart_count", RestartCount },
                { "revive_count", ReviveCount }
            };
        }

        private void Restore(GameSessionRestoreData restore)
        {
            Lives = Mathf.Clamp(restore.Lives, 0, 3);
            ReviveCount = Math.Max(restore.ReviveCount, 0);
            RestartCount = Math.Max(restore.RestartCount, 0);
            _successfulCatCount = Math.Max(restore.SuccessfulCatCount, 0);
            Score.Restore(restore.Score);
            History.Deserialize(restore.StepHistoryData);
            RestoreCells(restore.PlacedCats, CellStateType.CAT);
            RestoreCells(restore.Marks, CellStateType.MARK);
            RestoreCells(restore.Errors, CellStateType.ERROR);
        }

        private void RestoreCells(IReadOnlyList<Vector2Int> cells, CellStateType state)
        {
            for (int i = 0; i < cells.Count; i++)
                Board.RestoreCellState(cells[i].x, cells[i].y, state, out _);
        }

        private static SessionActionResult Rejected()
        {
            return new SessionActionResult { Accepted = false, Kind = SessionActionKind.None };
        }

        private static int[][] Clone(int[][] source)
        {
            var result = new int[source.Length][];
            for (int row = 0; row < source.Length; row++)
                result[row] = (int[])source[row].Clone();
            return result;
        }
    }
}
