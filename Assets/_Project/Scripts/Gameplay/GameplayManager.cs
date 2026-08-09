using System;
using System.Collections.Generic;
using UnityEngine;
using Meowdoku.Core;
using Meowdoku.Core.Config;
using Meowdoku.Gameplay.Input;
using Meowdoku.Services;

namespace Meowdoku.Gameplay
{
    public class GameplayManager : MonoBehaviour, IBoardStateReader
    {
        public event Action<GameToolKind> ToolRewardRequested;
        public event Func<GameToolKind, bool> IdleToolHintPlayRequested;
        public event Action<GameToolKind> IdleToolHintStopRequested;
        public event Action<MainGameTransitionData> GameTransitioned;
        public event Action<GameplayFeedbackData> GameplayFeedbackRequested;
        public event Action<IReadOnlyList<GameplayFeedbackData>> GameplayFeedbackBatchRequested;
        public event Action<GameplayHintPresentationData> HintPresentationRequested;
        public event Action HintPresentationClosed;
        public event Action<GameplayHudState> GameplayHudStateChanged;

        [Header("References")]
        public BoardView boardView;
        [SerializeField] private SoundService soundService;
        [SerializeField] private GameplayFeedbackPresenter gameplayFeedbackPresenter;
        [SerializeField] private GameplayLifeHudPresenter gameplayLifeHudPresenter;

        [Header("Level")]
        [Min(1)] public int startingLevel = 1;

        private GameSession _session;
        private SwipeGuardRecognizer _gestureRecognizer;
        private DoubleTapProtectConfig _doubleTapProtectConfig;
        private SwipeProtectConfig _swipeProtectConfig;
        private RegionColorConfig _regionColorConfig;
        private GameGridUiConfig _gameGridUiConfig;
        private BoardSizeBigConfig _boardSizeBigConfig;
        private DailyFirstLevelDifficultyConfig _dailyFirstDifficultyConfig;
        private ScoreEncourageConfig _scoreEncourageConfig;
        private PreCatConfig _preCatConfig;
        private RewardUnlockLevelConfig _rewardUnlockConfig;
        private PropHighlightConfig _propHighlightConfig;
        private MarkSoundConfig _markSoundConfig;
        private ToolResourceCoordinator _toolResources;
        private IdleToolHintController _idleToolHint;
        private MainGameTransitionCoordinator _transitions;
        private MainGameTransitionData _lastTransition;
        private GameSessionSnapshotContext _snapshotContext;
        private GameStateService _gameState;
        private int _currentPuzzleSize;
        private int[][] _currentRegions;
        private int[] _currentSolutionColumns;
        private bool _inputLocked;
        private double _wrongResolutionDeadline;
        private double _hintCooldownDeadline;
        private SessionHintRequest _activeHint;
        private bool _snapshotDirty;
        private double _snapshotDeadline;
        private double _winSettlementDeadline;
        private bool _winSettlementPending;
        private bool _dragInProgress;
        private Vector2 _lastDragPosition;
        private const double SnapshotDebounceSeconds = 0.5;

        public QueendokuCore.Rule LastRuleViolation => _session != null
            ? _session.LastRuleViolation
            : QueendokuCore.Rule.None;
        public int CorrectCrossCount => _session != null ? _session.CorrectCrossCount : 0;
        public int FalseCrossCount => _session != null ? _session.FalseCrossCount : 0;
        public GameSessionState SessionState => _session != null
            ? _session.State
            : GameSessionState.Loading;

        private void Start()
        {
            _doubleTapProtectConfig = new DoubleTapProtectConfig();
            _swipeProtectConfig = new SwipeProtectConfig();
            _regionColorConfig = new RegionColorConfig();
            _gameGridUiConfig = new GameGridUiConfig();
            _boardSizeBigConfig = new BoardSizeBigConfig();
            _dailyFirstDifficultyConfig = new DailyFirstLevelDifficultyConfig();
            _scoreEncourageConfig = new ScoreEncourageConfig();
            _preCatConfig = new PreCatConfig();
            _rewardUnlockConfig = new RewardUnlockLevelConfig();
            _propHighlightConfig = new PropHighlightConfig();
            _markSoundConfig = new MarkSoundConfig();
            var baseRecognizer = new BoardGestureRecognizer(
                new BoardInputScheme(this),
                DoubleTapWindowSeconds);
            _gestureRecognizer = new SwipeGuardRecognizer(
                baseRecognizer,
                _swipeProtectConfig,
                boardView.PointerToCell);

            boardView.OnGesturePointerStarted += HandleGestureStarted;
            boardView.OnGesturePointerMoved += HandleGestureMoved;
            boardView.OnGestureEnded += HandleGestureEnded;
            LoadLevel(startingLevel);
        }

        private void OnDestroy()
        {
            _idleToolHint?.Reset();
            FlushSnapshot();
            GameStateRuntime.FlushPendingWrites();
            if (_session != null) _session.BeginLeaving();
            if (boardView == null) return;
            boardView.OnGesturePointerStarted -= HandleGestureStarted;
            boardView.OnGesturePointerMoved -= HandleGestureMoved;
            boardView.OnGestureEnded -= HandleGestureEnded;
        }

        private void OnDisable()
        {
            _idleToolHint?.Reset();
        }

        private void LoadLevel(
            int levelNumber,
            IDictionary<string, object> directRetryParameters = null,
            int restartCount = 0)
        {
            _idleToolHint?.Reset();
            _session?.CancelHint();
            HintPresentationClosed?.Invoke();
            _activeHint = null;
            _snapshotDirty = false;
            _dragInProgress = false;
            _wrongResolutionDeadline = 0;
            _hintCooldownDeadline = 0;
            _winSettlementDeadline = 0;
            _winSettlementPending = false;
            _inputLocked = true;

            GameStateService state = GameStateRuntime.Current;
            _gameState = state;
            EnsureToolFlow(state);
            GameSessionSnapshotRestore snapshotRestore = null;
            IDictionary<string, object> retryParameters = null;
            bool isDirectRetry = directRetryParameters != null;
            if (!isDirectRetry && _dailyFirstDifficultyConfig.IsEnabled())
            {
                state.EvaluateDailyFirstEasy();
                if (state.IsDailyFirstEasyAvailable &&
                    (LevelData.IsHardLevel(levelNumber) || LevelData.IsSpecialLevel(levelNumber)))
                    state.ConsumeDailyFirstEasy();
            }

            if (directRetryParameters == null)
            {
                Dictionary<string, object> persistedSnapshot = state.GetEndgameSnapshot();
                if (persistedSnapshot.Count > 0 &&
                    !GameSessionSnapshot.TryRead(persistedSnapshot, levelNumber, out snapshotRestore))
                    state.ClearEndgameSnapshot();
            }
            if (snapshotRestore != null && snapshotRestore.IsComplete)
            {
                state.OnLevelWon(levelNumber);
                state.ClearEndgameSnapshot();
                LoadLevel(state.CurrentLevel);
                return;
            }

            LevelEntry entry = snapshotRestore?.Entry;
            if (entry == null)
            {
                retryParameters = directRetryParameters ?? state.GetRetryPuzzle(levelNumber);
                entry = TryReadCachedRetry(retryParameters);
            }
            if (entry == null)
                entry = LevelData.GetLevelEntry(levelNumber, gameState: state);
            if (entry == null)
            {
                Debug.LogError($"Cannot load Meowdoku level {levelNumber} from the original bank.");
                _inputLocked = true;
                return;
            }
            if (!isDirectRetry &&
                _dailyFirstDifficultyConfig.IsEnabled() &&
                state.IsDailyFirstEasyAvailable)
                state.ConsumeDailyFirstEasy();

            _currentPuzzleSize = entry.Size;
            _currentRegions = entry.RegionMap;
            _currentSolutionColumns = entry.Solution;
            GameSessionRestoreData sessionRestore = snapshotRestore?.Session;
            if (sessionRestore == null && restartCount > 0)
                sessionRestore = new GameSessionRestoreData { RestartCount = restartCount };
            _session = new GameSession(
                _currentPuzzleSize,
                _currentRegions,
                _currentSolutionColumns,
                entry.Rank,
                _scoreEncourageConfig,
                sessionRestore);
            _inputLocked = false;
            _gestureRecognizer.Reset();

            int[] colorMap = entry.ColorMap != null && entry.ColorMap.Length == _currentPuzzleSize
                ? entry.ColorMap
                : LevelGenerator.ComputeColorMapWithSeed(
                    _currentPuzzleSize,
                    _currentRegions,
                    entry.BankTransform);
            boardView.SetupBoard(
                _currentPuzzleSize,
                _currentRegions,
                colorMap,
                entry.PatternRegions,
                _regionColorConfig,
                _gameGridUiConfig,
                _boardSizeBigConfig);

            _snapshotContext = new GameSessionSnapshotContext
            {
                Level = levelNumber,
                BankIndex = entry.BankIndex > 0 ? entry.BankIndex : levelNumber,
                Entry = entry,
                PreType = snapshotRestore?.PreType ?? PreCatDecider.PreTypeNone
            };
            if (snapshotRestore != null)
            {
                _snapshotContext.PrefillPositions.AddRange(snapshotRestore.PrefillPositions);
                RenderSessionBoard();
                state.ClearCurrentLevelDirty();
            }
            else
            {
                ApplyInitialPrefills(retryParameters);
                ResolvePreCat(levelNumber, entry, state);
                bool hasRetry = retryParameters != null && retryParameters.Count > 0;
                if (!hasRetry)
                {
                    state.SetRetryPuzzle(
                        levelNumber,
                        GameRetryParameters.BuildInitial(_snapshotContext));
                    state.ClearCurrentLevelDirty();
                }
                else if (directRetryParameters == null)
                {
                    state.ClearCurrentLevelDirty();
                }
            }
            _transitions = new MainGameTransitionCoordinator(state);
            _lastTransition = null;
            gameplayFeedbackPresenter?.ResetPresenter(_session.Score.Score);
            gameplayLifeHudPresenter?.ResetLives(_session.Lives);
            PublishHudState();
            _session.FinishEntering();
            soundService?.Play(SoundKind.BoardEnter);
            boardView.PlayGridIntro();
            _gestureRecognizer.ConfigureBoard(
                boardView.PuzzleSize,
                boardView.GridSlotPixels,
                boardView.GridPaddingPixels,
                boardView.CellPixels);
        }

        private LevelEntry TryReadCachedRetry(IDictionary<string, object> cached)
        {
            if (cached == null || cached.Count == 0) return null;
            var normalized = new Dictionary<string, object>(cached);
            CopyIfPresent(cached, normalized, "bank_size", "size");
            CopyIfPresent(cached, normalized, "bank_rank", "r");
            CopyIfPresent(cached, normalized, "bank_index", "id");
            CopyIfPresent(cached, normalized, "level_seed", "seed");
            CopyIfPresent(cached, normalized, "prebuilt_regions", "regionMap");
            CopyIfPresent(cached, normalized, "prebuilt_solution", "solution");
            LevelEntry entry = LevelEntry.FromDictionary(normalized);
            return entry != null && entry.Size > 0 && entry.RegionMap != null && entry.Solution != null
                ? entry
                : null;
        }

        private static void CopyIfPresent(
            IDictionary<string, object> source,
            IDictionary<string, object> destination,
            string sourceKey,
            string destinationKey)
        {
            if (!destination.ContainsKey(destinationKey) && source.TryGetValue(sourceKey, out object value))
                destination[destinationKey] = value;
        }

        private void ResolvePreCat(int level, LevelEntry entry, GameStateService state)
        {
            Dictionary<string, object> pending = state.ConsumePreCatPending();
            if (level <= 20) return;

            Dictionary<string, object> locked = state.GetPreCatLock(level);
            string preType = PreCatDecider.PreTypeNone;
            Vector2Int position = new Vector2Int(-1, -1);
            if (locked.TryGetValue("locked", out object isLocked) && (bool)isLocked)
            {
                preType = locked["pre_type"].ToString();
                position = (Vector2Int)locked["position"];
                if (!IsSolutionPosition(position, entry))
                {
                    position = PreCatDecider.PickPrefillCell(
                        entry.Size,
                        entry.RegionMap,
                        HintEngine.SolutionMatrix(entry.Size, entry.Solution));
                    state.SetPreCatLock(level, preType, position);
                }
            }
            else if (_preCatConfig.Value != PreCatConfig.ValueOff)
            {
                List<int> scenarios = PreCatDecider.HitScenarios(
                    ReadBool(pending, "hard"),
                    entry.Rank,
                    ReadBool(pending, "struggle"),
                    ReadBool(pending, "demote"));
                PreCatDecision decision = PreCatDecider.Decide(
                    _preCatConfig.Value,
                    scenarios,
                    entry.Size,
                    entry.RegionMap,
                    HintEngine.SolutionMatrix(entry.Size, entry.Solution));
                preType = decision.PreType;
                position = decision.Position;
                state.SetPreCatLock(level, preType, position);
            }

            if (position.x < 0) return;
            if (_session.ApplyPrefill(position.x, position.y, out IReadOnlyList<BoardStateChange> changes))
            {
                _snapshotContext.PreType = preType;
                _snapshotContext.PreCatPosition = position;
                _snapshotContext.PrefillPositions.Add(position);
                ApplyViewChanges(changes, false);
            }
        }

        private void ApplyInitialPrefills(IDictionary<string, object> parameters)
        {
            if (parameters == null || !parameters.TryGetValue("prefill_positions", out object raw) ||
                !(raw is System.Collections.IList positions)) return;
            for (int i = 0; i < positions.Count; i++)
            {
                if (!(positions[i] is System.Collections.IList position) || position.Count < 2) continue;
                int row = System.Convert.ToInt32(position[0]);
                int column = System.Convert.ToInt32(position[1]);
                if (!IsSolutionPosition(new Vector2Int(row, column), _snapshotContext.Entry)) continue;
                if (_session.ApplyPrefill(row, column, out IReadOnlyList<BoardStateChange> changes))
                {
                    _snapshotContext.PrefillPositions.Add(new Vector2Int(row, column));
                    ApplyViewChanges(changes, false);
                }
            }
        }

        private static bool ReadBool(IDictionary<string, object> data, string key)
        {
            return data.TryGetValue(key, out object value) && value != null && (bool)value;
        }

        private static bool IsSolutionPosition(Vector2Int position, LevelEntry entry)
        {
            return position.x >= 0 && position.x < entry.Size &&
                   position.y >= 0 && position.y < entry.Size &&
                   entry.Solution[position.x] == position.y;
        }

        private void RenderSessionBoard()
        {
            CellStateType[][] board = _session.Board.GetBoardSnapshot();
            for (int row = 0; row < board.Length; row++)
                for (int column = 0; column < board[row].Length; column++)
                    if (board[row][column] != CellStateType.EMPTY)
                        boardView.SetCellState(row, column, board[row][column], false);
        }

        private void HandleGestureStarted(
            Vector2 position,
            Vector2Int startCell,
            int nowMilliseconds)
        {
            _idleToolHint?.Reset();
            if (!CanAcceptInput()) return;
            ConsumeActions(_gestureRecognizer.OnDragStart(
                position,
                startCell,
                nowMilliseconds));
            _dragInProgress = true;
            _lastDragPosition = position;
        }

        private void Update()
        {
            if (_dragInProgress)
                _gestureRecognizer.OnDragTick(
                    _lastDragPosition,
                    (int)(Time.unscaledTimeAsDouble * 1000.0));
            if (_snapshotDirty && Time.unscaledTimeAsDouble >= _snapshotDeadline)
                FlushSnapshot();
            if (_session != null &&
                _session.State == GameSessionState.ResolvingWrongGuess &&
                Time.unscaledTimeAsDouble >= _wrongResolutionDeadline)
            {
                GameSessionState resolved = _session.ResolveWrongGuess();
                if (resolved == GameSessionState.Failed) TrySettleFail();
            }
            if (_winSettlementPending &&
                Time.unscaledTimeAsDouble >= _winSettlementDeadline)
            {
                _winSettlementPending = false;
                TrySettleWin();
            }

            if (_session != null)
            {
                bool terminal = _session.State == GameSessionState.Won ||
                                _session.State == GameSessionState.Failed ||
                                _session.State == GameSessionState.Leaving;
                _idleToolHint?.Tick(
                    Time.unscaledDeltaTime,
                    isActiveAndEnabled && gameObject.activeInHierarchy,
                    terminal,
                    _session.State == GameSessionState.ResolvingWrongGuess,
                    _activeHint != null);
            }

        }

        private void HandleGestureMoved(Vector2 position, int nowMilliseconds)
        {
            if (!CanAcceptInput()) return;
            _lastDragPosition = position;
            ConsumeActions(_gestureRecognizer.OnDragOver(position, nowMilliseconds));
        }

        private void HandleGestureEnded()
        {
            _dragInProgress = false;
            if (_gestureRecognizer == null) return;
            _gestureRecognizer.OnDragEnd();
            if (_session != null) _session.CommitCurrentStep();
        }

        private void ConsumeActions(IReadOnlyList<CellAction> actions)
        {
            for (int i = 0; i < actions.Count; i++)
            {
                CellAction action = actions[i];
                if (action.Kind == CellAction.ActionKind.DoubleTap)
                {
                    ConsumeDoubleTap(action.Row, action.Column);
                    continue;
                }

                ApplyCellState(
                    action.Row,
                    action.Column,
                    action.State,
                    action.PlayAnimation,
                    action.Record);
            }
        }

        private void ConsumeDoubleTap(int row, int column)
        {
            SessionActionResult result = _session.DoubleTap(row, column);
            if (!result.Accepted) return;
            ApplyActionResult(result, true);
            if (result.Kind == SessionActionKind.WrongGuess)
            {
                double delay = result.LivesAfter <= 0 ? 0.6 : 0.4;
                _wrongResolutionDeadline = Time.unscaledTimeAsDouble + delay;
            }
        }

        [ContextMenu("Undo Last Step")]
        public void Undo()
        {
            if (!CanAcceptInput()) return;
            SessionActionResult result = _session.Undo();
            if (result.Accepted) ApplyActionResult(result, false);
        }

        public void ClearMarks()
        {
            if (!CanAcceptInput()) return;
            SessionActionResult result = _session.ClearMarks();
            if (!result.Accepted) return;
            ApplyActionResult(result, true, false);
            if (result.Changes.Count > 0) soundService?.Play(SoundKind.UnmarkX);
        }

        private SessionActionResult ApplyAuthorizedLocate()
        {
            if (!CanAcceptInput()) return new SessionActionResult();
            SessionActionResult result = _session.Locate();
            if (result.Accepted) ApplyActionResult(result, true);
            return result;
        }

        public ToolResourceDecision TryUseLocate(out SessionActionResult action)
        {
            action = new SessionActionResult();
            if (IsEntryBlocked()) return ToolResourceDecision.Rejected;
            _idleToolHint?.Reset();
            if (!CanAcceptInput() || _toolResources == null || _gameState == null)
                return ToolResourceDecision.Rejected;

            ToolResourceDecision decision = _toolResources.TryConsume(
                GameToolKind.Locate,
                _gameState.CurrentLevel,
                NowMilliseconds());
            if (decision == ToolResourceDecision.RewardRequired)
                ToolRewardRequested?.Invoke(GameToolKind.Locate);

            // This ordering is intentional: BaseGamePage marks Locate dirty/DDA
            // after _consume_tool even when the reward path was required.
            _gameState.MarkCurrentLevelDirty();
            _gameState.MarkDdaToolOrReviveUsed();
            if (!IsAuthorized(decision)) return decision;

            action = ApplyAuthorizedLocate();
            return action.Accepted ? decision : ToolResourceDecision.NoAction;
        }

        private bool TryRequestAuthorizedHint(
            bool allowLocateFallback,
            out SessionHintRequest request)
        {
            request = null;
            if (!CanAcceptInput() || Time.unscaledTimeAsDouble < _hintCooldownDeadline)
                return false;
            request = _session.RequestHint(allowLocateFallback);
            if (!request.Found) return false;
            _activeHint = request;
            return true;
        }

        public ToolResourceDecision TryUseHint(
            bool allowLocateFallback,
            out SessionHintRequest request)
        {
            request = null;
            if (IsEntryBlocked() || Time.unscaledTimeAsDouble < _hintCooldownDeadline)
                return ToolResourceDecision.Rejected;
            _idleToolHint?.Reset();
            if (!CanAcceptInput() || _toolResources == null || _gameState == null)
                return ToolResourceDecision.Rejected;

            ToolResourceDecision availability = _toolResources.Inspect(
                GameToolKind.Hint,
                _gameState.CurrentLevel,
                NowMilliseconds());
            if (availability == ToolResourceDecision.RewardRequired)
                ToolRewardRequested?.Invoke(GameToolKind.Hint);
            if (!IsAvailable(availability)) return availability;

            if (!TryRequestAuthorizedHint(allowLocateFallback, out request))
            {
                if (request != null && request.RequiresLocateFallback)
                    return TryUseLocate(out _);
                return ToolResourceDecision.NoAction;
            }

            ToolResourceDecision consumed = _toolResources.TryConsume(
                GameToolKind.Hint,
                _gameState.CurrentLevel,
                NowMilliseconds());
            if (!IsAuthorized(consumed))
            {
                CancelRequestedHint();
                if (consumed == ToolResourceDecision.RewardRequired)
                    ToolRewardRequested?.Invoke(GameToolKind.Hint);
                return consumed;
            }

            _gameState.MarkCurrentLevelDirty();
            _gameState.MarkDdaToolOrReviveUsed();
            soundService?.Play(SoundKind.UseHint);
            GameplayHintPresentationData presentation = GameplayHintPresentationData.Build(
                request,
                _currentPuzzleSize,
                _currentRegions,
                this);
            if (presentation != null) HintPresentationRequested?.Invoke(presentation);
            return consumed;
        }

        public SessionActionResult ApplyRequestedHint()
        {
            if (_activeHint == null) return new SessionActionResult();
            SessionHintRequest appliedRequest = _activeHint;
            SessionActionResult result = _session.ApplyHint();
            if (result.Accepted) ApplyActionResult(result, true);
            string strategy = appliedRequest.Hint != null
                ? appliedRequest.Hint.Strategy
                : string.Empty;
            bool catHint = !appliedRequest.WrongMark &&
                           (strategy == string.Empty || strategy == "R1");
            _hintCooldownDeadline = Time.unscaledTimeAsDouble + (catHint ? 0.8 : 0.5);
            _activeHint = null;
            HintPresentationClosed?.Invoke();
            return result;
        }

        public void CancelRequestedHint()
        {
            _activeHint = null;
            if (_session != null) _session.CancelHint();
            HintPresentationClosed?.Invoke();
        }

        public SessionActionResult RunAutoComplete()
        {
            if (!CanAcceptInput()) return new SessionActionResult();
            SessionActionResult result = _session.AutoComplete();
            if (result.Accepted) ApplyActionResult(result, true, false);
            return result;
        }

        public bool ReviveFromFail(int livesToRestore)
        {
            if (_transitions == null || !_transitions.TryRevive(
                    _session,
                    _snapshotContext,
                    livesToRestore,
                    out MainGameTransitionData transition))
                return false;

            _snapshotDirty = false;
            gameplayLifeHudPresenter?.PlayRevive(
                _session.Lives,
                livesToRestore >= 3);
            PublishTransition(transition);
            return true;
        }

        public bool RestartLevel()
        {
            if (_session != null &&
                _session.State == GameSessionState.Failed &&
                _lastTransition != null &&
                _lastTransition.Kind == MainGameTransitionKind.Failed &&
                _lastTransition.RetryParameters.Count > 0)
            {
                int level = _lastTransition.Level;
                var retry = new Dictionary<string, object>(_lastTransition.RetryParameters);
                _session.BeginLeaving();
                LoadLevel(level, retry);
                return true;
            }

            if (_transitions == null || !_transitions.TryRestart(
                    _session,
                    _snapshotContext,
                    out MainGameTransitionData transition))
                return false;

            PublishTransition(transition);
            _session.BeginLeaving();
            LoadLevel(
                transition.Level,
                transition.RetryParameters,
                transition.RestartCount);
            return true;
        }

        public bool QuitLevel()
        {
            if (_transitions == null || !_transitions.TryQuit(
                    _session,
                    _snapshotContext,
                    out MainGameTransitionData transition))
                return false;

            _snapshotDirty = false;
            PublishTransition(transition);
            _session.BeginLeaving();
            GameStateRuntime.FlushPendingWrites();
            return true;
        }

        public bool ContinueToNextLevel()
        {
            if (_lastTransition == null ||
                _lastTransition.Kind != MainGameTransitionKind.Won ||
                _gameState == null)
                return false;

            int nextLevel = _gameState.CurrentLevel;
            _session.BeginLeaving();
            LoadLevel(nextLevel);
            return true;
        }

        private void TrySettleFail()
        {
            if (_transitions != null && _transitions.TrySettleFail(
                    _session,
                    _snapshotContext,
                    out MainGameTransitionData transition))
                PublishTransition(transition);
        }

        private void TrySettleWin()
        {
            if (_transitions != null && _transitions.TrySettleWin(
                    _session,
                    _snapshotContext,
                    out MainGameTransitionData transition))
                PublishTransition(transition);
        }

        private void PublishTransition(MainGameTransitionData transition)
        {
            _lastTransition = transition;
            GameTransitioned?.Invoke(transition);
        }

        private bool IsSolutionCell(int row, int column)
        {
            return _session != null && _session.Board.IsSolutionCell(row, column);
        }

        private float DoubleTapWindowSeconds(int row, int column)
        {
            bool truthHasCat = _doubleTapProtectConfig.NeedsTruth() &&
                               IsSolutionCell(row, column);
            bool wouldConflict = _doubleTapProtectConfig.NeedsConflict() &&
                                 WouldCatConflict(row, column);
            return (float)_doubleTapProtectConfig.WindowSeconds(
                truthHasCat,
                wouldConflict);
        }

        private bool WouldCatConflict(int row, int column)
        {
            return _session != null && _session.Board.WouldCatConflict(row, column);
        }

        public CellStateType GetCellState(int row, int column)
        {
            return _session != null
                ? _session.GetCellState(row, column)
                : CellStateType.EMPTY;
        }

        private bool ApplyCellState(
            int row,
            int column,
            CellStateType state,
            bool playAnimation,
            bool recordPrimary)
        {
            if (_session == null || !_session.TryApplyBoardEdit(
                    row,
                    column,
                    state,
                    recordPrimary,
                    out SessionActionResult result))
                return false;
            ApplyViewChanges(result.Changes, playAnimation);
            return true;
        }

        private void ApplyActionResult(
            SessionActionResult result,
            bool playAnimation,
            bool playSounds = true)
        {
            ApplyViewChanges(result.Changes, playAnimation, playSounds);
            PublishHudState();
            if (result.IsComplete && playSounds && soundService != null)
            {
                soundService.Stop(SoundKind.MarkCat);
                soundService.Play(SoundKind.AllCleared);
            }
            IReadOnlyList<GameplayFeedbackData> feedback = result.Feedback;
            GameplayFeedbackBatchRequested?.Invoke(feedback);
            for (int index = 0; index < feedback.Count; index++)
                GameplayFeedbackRequested?.Invoke(feedback[index]);
            RequestWinSettlement();
        }

        private void PublishHudState()
        {
            if (_session == null || _currentPuzzleSize <= 0 || _snapshotContext == null)
                return;
            GameplayHudStateChanged?.Invoke(new GameplayHudState(
                _snapshotContext.Level,
                _currentPuzzleSize - _session.RemainingCats,
                _currentPuzzleSize));
        }

        /// <summary>
        /// Called synchronously by the serialized gameplay presenter while a
        /// feedback batch is being published. Multiple visual layers may safely
        /// extend the same deadline.
        /// </summary>
        public void DelayWinSettlement(float seconds)
        {
            if (_session == null || _session.State != GameSessionState.Won) return;
            double deadline = Time.unscaledTimeAsDouble + Math.Max(0f, seconds);
            if (deadline > _winSettlementDeadline) _winSettlementDeadline = deadline;
        }

        private void RequestWinSettlement()
        {
            if (_session == null || _session.State != GameSessionState.Won) return;
            if (_winSettlementDeadline <= Time.unscaledTimeAsDouble)
            {
                TrySettleWin();
                return;
            }
            _winSettlementPending = true;
        }

        private void ApplyViewChanges(
            IReadOnlyList<BoardStateChange> changes,
            bool playAnimation,
            bool playSounds = true)
        {
            for (int i = 0; i < changes.Count; i++)
            {
                BoardStateChange change = changes[i];
                boardView.SetCellState(
                    change.Position.x,
                    change.Position.y,
                    change.After,
                    playAnimation);
                if (playSounds) PlayCellChangeSound(change, playAnimation);
            }
            ScheduleSnapshot(changes);
        }

        private void PlayCellChangeSound(BoardStateChange change, bool playAnimation)
        {
            if (!playAnimation || soundService == null) return;
            if (change.After == CellStateType.CAT)
            {
                soundService.Play(SoundKind.MarkCat);
                return;
            }
            if (change.After == CellStateType.ERROR)
            {
                soundService.Play(SoundKind.MarkWrong);
                return;
            }
            if (change.After == CellStateType.MARK && change.Before == CellStateType.EMPTY)
            {
                SoundKind kind = _markSoundConfig != null && _markSoundConfig.IsSoftVariant1()
                    ? SoundKind.MarkXSoft1
                    : _markSoundConfig != null && _markSoundConfig.IsSoftVariant2()
                        ? SoundKind.MarkXSoft2
                        : SoundKind.MarkX;
                soundService.Play(kind);
                return;
            }
            if (change.After == CellStateType.EMPTY && change.Before == CellStateType.MARK)
                soundService.Play(SoundKind.UnmarkX);
        }

        private void ScheduleSnapshot(IReadOnlyList<BoardStateChange> changes)
        {
            if (_session == null || _snapshotContext == null || changes == null || changes.Count == 0) return;
            if (_session.State == GameSessionState.Won)
            {
                _snapshotDirty = false;
                return;
            }

            bool immediate = false;
            for (int i = 0; i < changes.Count; i++)
                if (changes[i].After == CellStateType.CAT || changes[i].After == CellStateType.ERROR)
                    immediate = true;
            _snapshotDirty = true;
            _snapshotDeadline = Time.unscaledTimeAsDouble + SnapshotDebounceSeconds;
            if (immediate) FlushSnapshot();
        }

        private void FlushSnapshot()
        {
            if (!_snapshotDirty || _session == null || _snapshotContext == null || _gameState == null) return;
            if (_session.State == GameSessionState.Won)
            {
                _snapshotDirty = false;
                return;
            }
            _snapshotDirty = false;
            _gameState.SetEndgameSnapshot(GameSessionSnapshot.Build(_session, _snapshotContext));
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            FlushSnapshot();
            GameStateRuntime.FlushPendingWrites();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                _idleToolHint?.ResetElapsed();
                return;
            }
            FlushSnapshot();
            GameStateRuntime.FlushPendingWrites();
        }

        private void EnsureToolFlow(GameStateService state)
        {
            if (_toolResources == null)
                _toolResources = new ToolResourceCoordinator(state, _rewardUnlockConfig);
            if (_idleToolHint == null)
                _idleToolHint = new IdleToolHintController(
                    state,
                    _propHighlightConfig,
                    new GameplayIdleToolHintSink(this));
        }

        private static bool IsAvailable(ToolResourceDecision decision)
        {
            return decision == ToolResourceDecision.Available ||
                   decision == ToolResourceDecision.Free;
        }

        private static bool IsAuthorized(ToolResourceDecision decision)
        {
            return decision == ToolResourceDecision.Consumed ||
                   decision == ToolResourceDecision.Free;
        }

        private static long NowMilliseconds()
        {
            return (long)(Time.unscaledTimeAsDouble * 1000.0);
        }

        private bool IsEntryBlocked()
        {
            return _session == null ||
                   _session.State == GameSessionState.Loading ||
                   _session.State == GameSessionState.Entering;
        }

        private sealed class GameplayIdleToolHintSink : IIdleToolHintSink
        {
            private readonly GameplayManager _owner;

            public GameplayIdleToolHintSink(GameplayManager owner)
            {
                _owner = owner;
            }

            public bool TryPlay(GameToolKind kind)
            {
                Func<GameToolKind, bool> handler = _owner.IdleToolHintPlayRequested;
                if (handler == null) return false;
                bool played = false;
                Delegate[] subscribers = handler.GetInvocationList();
                for (int index = 0; index < subscribers.Length; index++)
                    played |= ((Func<GameToolKind, bool>)subscribers[index]).Invoke(kind);
                return played;
            }

            public void Stop(GameToolKind kind)
            {
                _owner.IdleToolHintStopRequested?.Invoke(kind);
            }
        }

        private bool CanAcceptInput()
        {
            return !_inputLocked && _session != null && _session.CanAcceptInput;
        }
    }
}
