using System;
using System.Collections.Generic;
using UnityEngine;
using Meowdoku.Core;
using Meowdoku.Core.Ads;
using Meowdoku.Core.Config;
using Meowdoku.Core.Daily;
using Meowdoku.Core.Rank;
using Meowdoku.Core.Tracking;
using Meowdoku.Core.UI;
using Meowdoku.Gameplay.Input;
using Meowdoku.Services;

namespace Meowdoku.Gameplay
{
    public class GameplayManager : MonoBehaviour, IBoardStateReader
    {
        public event Action<GameToolKind> ToolRewardRequested;
        public event Action ToolPresentationChanged;
        public event Func<GameToolKind, bool> IdleToolHintPlayRequested;
        public event Action<GameToolKind> IdleToolHintStopRequested;
        public event Action<MainGameTransitionData> GameTransitioned;
        public event Action<GameplayTrackingStartData> GameTrackingStarted;
        public event Action<GameplaySessionMode, int> SessionLoadPreparing;
        public event Action<GameplaySessionMode, bool>
            SessionPresentationChanged;
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
        [SerializeField] private bool startAutomatically = true;

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
        private RuleHighlightConfig _ruleHighlightConfig;
        private VibrateComboConfig _vibrateComboConfig;
        private MeowFeedbackConfig _meowFeedbackConfig;
        private SizeCycleConfig _sizeCycleConfig;
        private SingleRegionNumConfig _singleRegionConfig;
        private NormalLevel10Config _normalLevel10Config;
        private ToolResourceCoordinator _toolResources;
        private IdleToolHintController _idleToolHint;
        private IGameTransitionCoordinator _transitions;
        private GameplaySessionMode _sessionMode = GameplaySessionMode.Unspecified;
        private DailyMetaRuntime _dailyMetaRuntime;
        private RankActivityRuntime _rankActivityRuntime;
        private TrackerService _tracker;
        private AdService _adService;
        private AbConfigRuntime _abConfigRuntime;
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
        private int _gestureHistoryCount = -1;
        private Vector2 _lastDragPosition;
        private bool _initialized;
        private bool _managedByPage;
        private bool _pageOpen;
        private double _elapsedPlaySeconds;
        private double _activePlayStartedAt;
        private int _toolsUsed;
        private int _meowPrefillCatCount;
        private GameplayTrackingStartData _trackingStartData;
        private bool _dailyClockPausedForAd;
        private bool _entryAdPending;
        private bool _applicationSuspended;
        private bool _gameplayEntryStarted;
        private const double SnapshotDebounceSeconds = 0.5;

        public QueendokuCore.Rule LastRuleViolation => _session != null
            ? _session.LastRuleViolation
            : QueendokuCore.Rule.None;
        public int CorrectCrossCount => _session != null ? _session.CorrectCrossCount : 0;
        public int FalseCrossCount => _session != null ? _session.FalseCrossCount : 0;
        public GameSessionState SessionState => _session != null
            ? _session.State
            : GameSessionState.Loading;
        public GameplaySessionMode SessionMode => _sessionMode;
        public bool IsDailySession => _sessionMode == GameplaySessionMode.Daily;
        public int CurrentPuzzleSize => _currentPuzzleSize;
        public int CurrentLevelNumber => _snapshotContext?.Level ?? 0;

#if UNITY_INCLUDE_TESTS
        internal int LivesForTests => _session?.Lives ?? 0;
        internal int RemainingCatsForTests => _session?.RemainingCats ?? 0;
        internal int ScoreForTests => _session?.Score.Score ?? 0;
        internal int ComboForTests => _session?.Score.Combo ?? 0;
        internal int MistakeCountForTests => _session?.MistakeCount ?? 0;
        internal int RestartCountForTests => _session?.RestartCount ?? 0;
        internal double SnapshotElapsedSecondsForTests =>
            _snapshotContext?.InGameSeconds ?? 0.0;
        internal string DailyDateForTests =>
            _snapshotContext?.DailyDate ?? string.Empty;
        internal int DailyIndexForTests =>
            _snapshotContext?.DailyIndex ?? 0;
        internal int BankIndexForTests => _snapshotContext?.BankIndex ?? 0;
        internal int BankTotalForTests =>
            _snapshotContext?.Entry?.BankTotal ?? 0;
        internal string PuzzleIdForTests =>
            _currentRegions != null && _currentPuzzleSize > 0
                ? LevelData.ComputePuzzleId(
                    _currentPuzzleSize,
                    _currentRegions)
                : string.Empty;
        internal BankPoolKind BankPoolForTests
        {
            get
            {
                LevelEntry entry = _snapshotContext?.Entry;
                if (_sessionMode != GameplaySessionMode.Bank || entry == null)
                    return BankPoolKind.None;
                if (entry.BankLk)
                    return entry.BankLkModified
                        ? BankPoolKind.LkModified
                        : BankPoolKind.Lk;
                if (entry.BankSp) return BankPoolKind.Special;
                if (entry.BankLkStyle) return BankPoolKind.LkStyle;
                if (entry.BankGc) return BankPoolKind.Gc;
                return BankPoolKind.Regular;
            }
        }

        internal int SolutionColumnForTests(int row)
        {
            return _currentSolutionColumns != null &&
                   row >= 0 && row < _currentSolutionColumns.Length
                ? _currentSolutionColumns[row]
                : -1;
        }

        internal int SwipeProtectValueForTests =>
            _swipeProtectConfig?.Value ?? int.MinValue;

        internal int DoubleTapProtectValueForTests =>
            _doubleTapProtectConfig?.Value ?? int.MinValue;

        internal int GameGridUiValueForTests =>
            _gameGridUiConfig?.Value ?? int.MinValue;

        internal int BoardSizeBigValueForTests =>
            _boardSizeBigConfig?.Value ?? int.MinValue;

        internal SessionActionResult DoubleTapForTests(int row, int column)
        {
            return ConsumeDoubleTap(row, column);
        }

        internal bool ApplyCellStateForTests(
            int row,
            int column,
            CellStateType state)
        {
            return ApplyCellState(row, column, state, false, true);
        }

        internal bool PlayIdleToolHintForTests(GameToolKind kind)
        {
            return DispatchIdleToolHintPlay(kind);
        }

        internal void StopIdleToolHintForTests(GameToolKind kind)
        {
            IdleToolHintStopRequested?.Invoke(kind);
        }

        internal void SuspendApplicationForTests()
        {
            PersistLifecycleBoundary();
        }

        internal void ResumeApplicationForTests()
        {
            _applicationSuspended = false;
        }
#endif
        public float ElapsedPlaySeconds => (float)Math.Max(
            0.0,
            CurrentGameplayElapsed());

        public void BindDailyMetaRuntime(DailyMetaRuntime runtime)
        {
            _dailyMetaRuntime = runtime;
        }

        public void BindRankActivityRuntime(RankActivityRuntime runtime)
        {
            _rankActivityRuntime = runtime;
        }

        public void BindTracker(TrackerService tracker)
        {
            _tracker = tracker;
        }

        public void BindAdService(AdService service)
        {
            if (_adService == service) return;
            if (_adService != null)
            {
                _adService.AdShown -= HandleAdShown;
                _adService.AdClosed -= HandleAdClosed;
                _adService.AdError -= HandleAdError;
            }
            _adService = service;
            if (_adService != null)
            {
                _adService.AdShown += HandleAdShown;
                _adService.AdClosed += HandleAdClosed;
                _adService.AdError += HandleAdError;
            }
        }

        public void BindSoundService(SoundService service)
        {
            soundService = service;
            gameplayFeedbackPresenter?.BindSoundService(service);
        }

        public void BindAbConfigRuntime(AbConfigRuntime runtime)
        {
            _abConfigRuntime = runtime;
            BindLevelSelectionConfigs();
            BindBoardConfigs();
            BindInputConfigs();
            BindGameplayConfigs();
            gameplayFeedbackPresenter?.BindAbConfigRuntime(runtime);
        }

        public bool ShouldHighlightRuleViolation()
        {
            if (IsDailySession || _ruleHighlightConfig == null) return false;
            GameStateService state = _gameState ?? GameStateRuntime.Current;
            int level = _snapshotContext?.Level ?? state.CurrentLevel;
            return _ruleHighlightConfig.ShouldHighlight(
                state.TutorialDone,
                level);
        }

        public bool IsPatternModeAvailable =>
            _abConfigRuntime?.Settings.BlindMode.IsEnabled() == true;

        public bool IsSpecialBankSession =>
            _sessionMode == GameplaySessionMode.Bank &&
            _snapshotContext?.Entry?.BankSp == true;

        public bool IsToolFree(GameToolKind kind)
        {
            GameStateService state = _gameState ?? GameStateRuntime.Current;
            RewardUnlockLevelConfig config = _rewardUnlockConfig ??
                                             new RewardUnlockLevelConfig();
            int level = _snapshotContext?.Level ?? state.CurrentLevel;
            return !config.IsRewardRequiredAt(level);
        }

        public void ApplyPatternMode()
        {
            BlindModConfig config = _abConfigRuntime?.Settings.BlindMode;
            bool available = config?.IsEnabled() == true;
            boardView?.SetPatternMode(
                available && GameStateRuntime.Current.PatternModeOn,
                available && config.IsKeepOnFilled());
        }

        public bool GrantRewardedTool(GameToolKind kind)
        {
            if (_dailyMetaRuntime == null || _gameState == null) return false;
            string propName = kind == GameToolKind.Locate
                ? TrackerCatalog.Prop.Locate
                : TrackerCatalog.Prop.Hint;
            string reason = kind == GameToolKind.Locate
                ? TrackerCatalog.PropSource.LocateRewardAd
                : TrackerCatalog.PropSource.HintRewardAd;
            _gameState.IncrementGameTotalStat(
                CurrentTrackingGameType(),
                "rv_count");
            return _dailyMetaRuntime.Awards.Dispatch(
                new[] { AwardItem.Tool(propName, 1) },
                AwardDisplayType.Direct,
                reason) >= 0;
        }

        private void Start()
        {
            InitializeIfNeeded();
            if (startAutomatically && !_managedByPage)
            {
                LoadLevel(startingLevel);
                _pageOpen = _session != null;
            }
        }

        public void ConfigureForPageLifecycle()
        {
            _managedByPage = true;
            startAutomatically = false;
            InitializeIfNeeded();
        }

        public bool OpenPage(
            IReadOnlyDictionary<string, object> parameters)
        {
            InitializeIfNeeded();
            if (!_initialized) return false;

            bool dailyEntry = parameters != null &&
                (ReadParameterBool(parameters, "daily_mode") ||
                 ReadParameterBool(parameters, "is_daily"));
            bool directEntry = parameters != null &&
                (ReadParameterBool(parameters, "bank_mode") ||
                 parameters.ContainsKey("prebuilt_regions") ||
                 parameters.ContainsKey("prebuilt_solution"));
            int fallbackLevel = GameStateRuntime.Current.CurrentLevel;
            int level = ReadParameterInt(
                parameters,
                "level_index",
                fallbackLevel);
            IDictionary<string, object> directParameters = null;
            GameplaySessionMode mode = GameplaySessionMode.Main;
            if (dailyEntry)
            {
                mode = GameplaySessionMode.Daily;
                if (directEntry)
                {
                    directParameters = new Dictionary<string, object>(parameters);
                }
                else
                {
                    DailyGameLaunchRequest launch = DailyPuzzleSelector.CreateLaunch(
                        fallbackLevel,
                        DateTime.Now);
                    if (launch == null)
                    {
                        Debug.LogError("Cannot load today's Daily puzzle from the original banks.");
                        return false;
                    }
                    directParameters = new Dictionary<string, object>(launch.Parameters);
                }
                level = 0;
            }
            else if (directEntry)
            {
                mode = GameplaySessionMode.Bank;
                level = ReadParameterInt(parameters, "retry_level", 0);
                directParameters = new Dictionary<string, object>(parameters);
            }

            LoadLevel(
                level,
                directParameters,
                sessionMode: mode,
                trackingStatus: ReadParameterString(
                    parameters,
                    "_tracker_status",
                    string.Empty));
            _pageOpen = _session != null;
            return _pageOpen;
        }

        public void ClosePage()
        {
            if (!_pageOpen) return;
            _idleToolHint?.Reset();
            _session?.CancelHint();
            HintPresentationClosed?.Invoke();
            if (_snapshotContext != null && _snapshotContext.Level > 0)
                FlushSnapshot();
            if (_session != null &&
                _session.State != GameSessionState.Leaving)
                _session.BeginLeaving();
            GameStateRuntime.FlushPendingWrites();
            _pageOpen = false;
            _entryAdPending = false;
        }

        private void InitializeIfNeeded()
        {
            if (_initialized) return;
            if (boardView == null)
            {
                Debug.LogError("GameplayManager requires a BoardView reference.");
                return;
            }
            _doubleTapProtectConfig = new DoubleTapProtectConfig();
            _swipeProtectConfig = new SwipeProtectConfig();
            _regionColorConfig = new RegionColorConfig();
            _gameGridUiConfig = new GameGridUiConfig();
            _boardSizeBigConfig = new BoardSizeBigConfig();
            _scoreEncourageConfig = new ScoreEncourageConfig();
            BindLevelSelectionConfigs();
            BindBoardConfigs();
            BindInputConfigs();
            BindGameplayConfigs();
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
            _initialized = true;
        }

        private void OnDestroy()
        {
            BindAdService(null);
            _idleToolHint?.Reset();
            FlushSnapshot();
            GameStateRuntime.FlushPendingWrites();
            if (_session != null) _session.BeginLeaving();
            if (!_initialized || boardView == null) return;
            boardView.OnGesturePointerStarted -= HandleGestureStarted;
            boardView.OnGesturePointerMoved -= HandleGestureMoved;
            boardView.OnGestureEnded -= HandleGestureEnded;
        }

        private void HandleAdShown(string placementId)
        {
            soundService?.NotifyAdShown(placementId);
            if (!IsDailySession ||
                placementId != TrackerCatalog.Placement.Reward ||
                _activePlayStartedAt <= 0.0)
                return;
            StopGameplayClock();
            _dailyClockPausedForAd = true;
        }

        private void HandleAdClosed(string placementId)
        {
            soundService?.NotifyAdClosed(placementId);
            if (placementId == TrackerCatalog.Placement.Interstitial)
                FinishPendingEntryAd();
            if (!_dailyClockPausedForAd ||
                placementId != TrackerCatalog.Placement.Reward)
                return;
            _dailyClockPausedForAd = false;
            if (_pageOpen && _session != null &&
                _session.State != GameSessionState.Won &&
                _session.State != GameSessionState.Failed &&
                _session.State != GameSessionState.Leaving)
                StartGameplayClock();
        }

        private void HandleAdError(string placementId, string message)
        {
            if (placementId == TrackerCatalog.Placement.Interstitial)
                FinishPendingEntryAd();
        }

        private void OnDisable()
        {
            _idleToolHint?.Reset();
        }

        private void LoadLevel(
            int levelNumber,
            IDictionary<string, object> directRetryParameters = null,
            int restartCount = 0,
            GameplaySessionMode sessionMode = GameplaySessionMode.Unspecified,
            string trackingStatus = "")
        {
            _idleToolHint?.Reset();
            _session?.CancelHint();
            HintPresentationClosed?.Invoke();
            _activeHint = null;
            _snapshotDirty = false;
            _dragInProgress = false;
            _gestureHistoryCount = -1;
            _wrongResolutionDeadline = 0;
            _hintCooldownDeadline = 0;
            _winSettlementDeadline = 0;
            _winSettlementPending = false;
            _inputLocked = true;
            _entryAdPending = false;
            _gameplayEntryStarted = false;
            _elapsedPlaySeconds = 0.0;
            _activePlayStartedAt = 0.0;
            _toolsUsed = 0;

            if (sessionMode == GameplaySessionMode.Unspecified)
                sessionMode = directRetryParameters == null
                    ? GameplaySessionMode.Main
                    : GameplaySessionMode.Bank;
            SessionLoadPreparing?.Invoke(sessionMode, levelNumber);
            _sessionMode = sessionMode;

            GameStateService state = GameStateRuntime.Current;
            _gameState = state;
            EnsureToolFlow(state);
            bool dailyAlreadyStarted = false;
            if (sessionMode == GameplaySessionMode.Daily)
            {
                string date = ReadParameterString(
                    directRetryParameters,
                    "daily_date",
                    DailyEntryStateContract.DateKey(DateTime.Now));
                dailyAlreadyStarted = string.Equals(
                    state.DailyStartedDate,
                    date,
                    StringComparison.Ordinal);
                if (!dailyAlreadyStarted)
                    state.SetDailyStartedDate(date);
            }
            GameSessionSnapshotRestore snapshotRestore = null;
            IDictionary<string, object> retryParameters = null;
            bool isDirectRetry = directRetryParameters != null;
            if (sessionMode == GameplaySessionMode.Main &&
                !isDirectRetry && _dailyFirstDifficultyConfig.IsEnabled())
            {
                state.EvaluateDailyFirstEasy();
                if (state.IsDailyFirstEasyAvailable &&
                    (LevelData.IsHardLevel(levelNumber) || LevelData.IsSpecialLevel(levelNumber)))
                    state.ConsumeDailyFirstEasy();
            }

            if (sessionMode == GameplaySessionMode.Main &&
                directRetryParameters == null)
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
                retryParameters = sessionMode == GameplaySessionMode.Main
                    ? directRetryParameters ?? state.GetRetryPuzzle(levelNumber)
                    : directRetryParameters;
                entry = TryReadCachedRetry(retryParameters);
            }
            if (entry == null && sessionMode == GameplaySessionMode.Main)
            {
                bool dedupRetried = false;
                while (true)
                {
                    entry = LevelData.GetLevelEntry(
                        levelNumber,
                        overrideSize: _sizeCycleConfig.ResolveSize(levelNumber),
                        gameState: state,
                        singleRegionConfig: _singleRegionConfig,
                        normalLevel10Config: _normalLevel10Config);
                    if (entry == null) break;

                    string puzzleId = LevelData.ComputePuzzleId(
                        entry.Size,
                        entry.RegionMap);
                    string source = string.IsNullOrEmpty(entry.BankSourceMain)
                        ? entry.BankSource
                        : entry.BankSourceMain;
                    Dictionary<string, object> previous = state.RecordPuzzle(
                        puzzleId,
                        levelNumber,
                        Application.version,
                        source ?? string.Empty);
                    bool crossLevelDuplicate = previous.Count > 0 &&
                        ReadParameterInt(
                            (IDictionary<string, object>)previous,
                            "level",
                            -1) != levelNumber;
                    if (!crossLevelDuplicate || dedupRetried) break;

                    // Source intentionally advances once more after the normal
                    // selector already advanced, then retries exactly once.
                    LevelData.AdvanceForEntry(entry, entry.Size, state);
                    dedupRetried = true;
                }
            }
            if (entry == null)
            {
                Debug.LogError($"Cannot load Meowdoku level {levelNumber} from the original bank.");
                _inputLocked = true;
                return;
            }
            if (sessionMode == GameplaySessionMode.Main &&
                !isDirectRetry &&
                _dailyFirstDifficultyConfig.IsEnabled() &&
                state.IsDailyFirstEasyAvailable)
                state.ConsumeDailyFirstEasy();

            _currentPuzzleSize = entry.Size;
            _currentRegions = entry.RegionMap;
            _currentSolutionColumns = entry.Solution;
            GameSessionRestoreData sessionRestore = snapshotRestore?.Session;
            _elapsedPlaySeconds = snapshotRestore?.InGameSeconds ?? 0.0;
            if (sessionRestore == null && restartCount > 0)
                sessionRestore = new GameSessionRestoreData { RestartCount = restartCount };
            _session = new GameSession(
                _currentPuzzleSize,
                _currentRegions,
                _currentSolutionColumns,
                entry.Rank,
                _scoreEncourageConfig,
                sessionRestore);
            _inputLocked = true;
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
            ApplyPatternMode();

            _snapshotContext = new GameSessionSnapshotContext
            {
                Level = levelNumber,
                BankIndex = entry.BankIndex > 0 ? entry.BankIndex : levelNumber,
                Entry = entry,
                Mode = sessionMode,
                DailyDate = ReadParameterString(
                    directRetryParameters,
                    "daily_date",
                    string.Empty),
                DailyIndex = ReadParameterInt(
                    directRetryParameters,
                    "daily_index",
                    0),
                LaunchParameters = directRetryParameters != null
                    ? new Dictionary<string, object>(directRetryParameters)
                    : new Dictionary<string, object>(),
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
                bool hasRetry = retryParameters != null && retryParameters.Count > 0;
                if (sessionMode == GameplaySessionMode.Main && !hasRetry)
                    ApplyTutorialPrefill(levelNumber, entry);
                ApplyInitialPrefills(retryParameters);
                if (sessionMode == GameplaySessionMode.Main)
                {
                    ResolvePreCat(levelNumber, entry, state);
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
            }
            _meowPrefillCatCount = _snapshotContext.PrefillPositions.Count;
            _transitions = sessionMode == GameplaySessionMode.Daily
                ? new DailyGameTransitionCoordinator(state)
                : new MainGameTransitionCoordinator(state);
            _lastTransition = null;
            gameplayFeedbackPresenter?.ResetPresenter(_session.Score.Score);
            gameplayLifeHudPresenter?.ResetLives(_session.Lives);
            PublishHudState();
            SessionPresentationChanged?.Invoke(
                sessionMode,
                ReadParameterBool(
                    _snapshotContext.LaunchParameters,
                    "from_bank_browser"));
            _gestureRecognizer.ConfigureBoard(
                boardView.PuzzleSize,
                boardView.GridSlotPixels,
                boardView.GridPaddingPixels,
                boardView.CellPixels);
            if (sessionMode != GameplaySessionMode.Daily)
                _rankActivityRuntime?.Manager?.NotifyLevelStart();
            string resolvedTrackingStatus = ResolveTrackingStatus(
                trackingStatus,
                sessionMode,
                restartCount,
                dailyAlreadyStarted,
                snapshotRestore != null,
                retryParameters,
                directRetryParameters);
            _trackingStartData = GameplayTrackingContract.BuildStart(
                _snapshotContext,
                resolvedTrackingStatus,
                state.CurrentLevel,
                LevelData.IsHardLevel(state.CurrentLevel),
                _rankActivityRuntime?.Manager?.IsRunning == true &&
                _rankActivityRuntime.Manager.IsJoined);
            GameTrackingStarted?.Invoke(_trackingStartData);
            ToolPresentationChanged?.Invoke();
            if (!TryStartEntryInterstitial(
                    resolvedTrackingStatus,
                    snapshotRestore != null))
                BeginGameplayEntry();
        }

        private void BindLevelSelectionConfigs()
        {
            LevelSelectionConfigSet configs =
                _abConfigRuntime?.LevelSelection;
            _sizeCycleConfig = configs?.SizeCycle ?? new SizeCycleConfig();
            _singleRegionConfig = configs?.SingleRegion ??
                                  new SingleRegionNumConfig();
            _normalLevel10Config = configs?.NormalLevel10 ??
                                   new NormalLevel10Config();
            _preCatConfig = configs?.PreCat ?? new PreCatConfig();
        }

        private void BindBoardConfigs()
        {
            BoardConfigSet configs = _abConfigRuntime?.Board;
            _regionColorConfig = configs?.RegionColor ??
                                 new RegionColorConfig();
            _gameGridUiConfig = configs?.GameGridUi ??
                                new GameGridUiConfig();
            _boardSizeBigConfig = configs?.BoardSizeBig ??
                                  new BoardSizeBigConfig();
        }

        private void BindInputConfigs()
        {
            InputConfigSet configs = _abConfigRuntime?.Input;
            _doubleTapProtectConfig = configs?.DoubleTapProtect ??
                                      new DoubleTapProtectConfig();
            _swipeProtectConfig = configs?.SwipeProtect ??
                                  new SwipeProtectConfig();
        }

        private void BindGameplayConfigs()
        {
            GameplayConfigSet configs = _abConfigRuntime?.Gameplay;
            _dailyFirstDifficultyConfig = configs?.DailyFirstLevelDifficulty ??
                                          new DailyFirstLevelDifficultyConfig();
            _rewardUnlockConfig = configs?.RewardUnlockLevel ??
                                  new RewardUnlockLevelConfig();
            _propHighlightConfig = configs?.PropHighlight ??
                                   new PropHighlightConfig();
            _markSoundConfig = configs?.MarkSound ?? new MarkSoundConfig();
            _ruleHighlightConfig = configs?.RuleHighlight ??
                                   new RuleHighlightConfig();
            _vibrateComboConfig = configs?.VibrateCombo ??
                                  new VibrateComboConfig();
            _meowFeedbackConfig = configs?.MeowFeedback ??
                                  new MeowFeedbackConfig();
        }

        private bool TryStartEntryInterstitial(
            string trackingStatus,
            bool endgameRestore)
        {
            if (_adService == null || _gameState == null) return false;
            string position = trackingStatus == TrackerCatalog.GameStatus.Restart
                ? TrackerCatalog.AdPosition.NormalRestart
                : trackingStatus == TrackerCatalog.GameStatus.Continue
                    ? TrackerCatalog.AdPosition.NormalContinue
                    : TrackerCatalog.AdPosition.NormalStart;
            _entryAdPending = true;
            var policy = new InterstitialPolicy(
                _gameState,
                _adService,
                level: _abConfigRuntime?.Ads.InterUnlockLevel,
                session: _abConfigRuntime?.Ads.InterUnlockSession,
                memory: _abConfigRuntime?.Ads.InterUnlockMemory,
                cooldown: _abConfigRuntime?.Ads.InterCooldown,
                protection: _abConfigRuntime?.Ads.InterExtraProtection);
            LivingDaysSegment segment =
                _abConfigRuntime?.CurrentLivingDaysSegment() ??
                new LivingDaysSegment(-1, 0, -1);
            InterstitialPolicyResult result = policy.TryShow(
                position,
                new InterstitialContext(
                    endgameRestore,
                    SystemInfo.systemMemorySize,
                    _adService.SessionActiveSeconds,
                    _abConfigRuntime?.FirstOpenUnixMilliseconds ?? 0,
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    segment.Index,
                    segment.Count));
            if (result.Shown) return true;
            _entryAdPending = false;
            return false;
        }

        private void FinishPendingEntryAd()
        {
            if (!_entryAdPending) return;
            _entryAdPending = false;
            BeginGameplayEntry();
        }

        private void BeginGameplayEntry()
        {
            if (_gameplayEntryStarted || _session == null) return;
            _gameplayEntryStarted = true;
            _inputLocked = false;
            _session.FinishEntering();
            StartGameplayClock();
            soundService?.Play(SoundKind.BoardEnter);
            boardView?.PlayGridIntro();
        }

        public IReadOnlyDictionary<string, object> BuildTrackingEndParameters(
            MainGameTransitionData transition,
            string result,
            TrackerService tracker)
        {
            if (transition == null || tracker == null || _gameState == null ||
                _trackingStartData == null)
                return null;
            int timeSeconds = Math.Max(0, (int)transition.ElapsedSeconds);
            _gameState.IncrementGameTotalStat(
                _trackingStartData.GameType,
                "time_total",
                timeSeconds);
            return GameplayTrackingContract.BuildEnd(
                _trackingStartData,
                transition,
                result,
                tracker,
                _gameState);
        }

        private LevelEntry TryReadCachedRetry(IDictionary<string, object> cached)
        {
            if (cached == null || cached.Count == 0) return null;
            var normalized = new Dictionary<string, object>(cached);
            CopyIfPresent(cached, normalized, "bank_size", "size");
            CopyIfPresent(cached, normalized, "bank_rank", "r");
            CopyIfPresent(cached, normalized, "bank_index", "id");
            CopyIfPresent(cached, normalized, "level_seed", "seed");
            if (ReadBool(cached, "bank_lk") || ReadBool(cached, "bank_sp"))
                CopyIfPresent(cached, normalized, "level_seed", "id");
            CopyIfPresent(cached, normalized, "r1_steps", "r1");
            CopyIfPresent(cached, normalized, "r2_steps", "r2");
            CopyIfPresent(cached, normalized, "r3_steps", "r3");
            CopyIfPresent(cached, normalized, "r4_steps", "r4");
            CopyIfPresent(cached, normalized, "r5_steps", "r5");
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
                if (scenarios.Count == 0) return;
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

            _snapshotContext.PreType = preType;
            if (position.x < 0) return;
            if (_session.ApplyPrefill(position.x, position.y, out IReadOnlyList<BoardStateChange> changes))
            {
                _snapshotContext.PreType = preType;
                _snapshotContext.PreCatPosition = position;
                _snapshotContext.PrefillPositions.Add(position);
                ApplyViewChanges(changes, false);
            }
        }

        private void ApplyTutorialPrefill(int level, LevelEntry entry)
        {
            Vector2Int? position = LevelData.ComputePrefill(
                level,
                entry.RegionMap,
                entry.Solution,
                entry.Size);
            if (!position.HasValue) return;

            Vector2Int value = position.Value;
            if (_session.ApplyPrefill(
                    value.x,
                    value.y,
                    out IReadOnlyList<BoardStateChange> changes))
            {
                _snapshotContext.PrefillPositions.Add(value);
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

        private static bool ReadParameterBool(
            IReadOnlyDictionary<string, object> data,
            string key)
        {
            if (data == null || !data.TryGetValue(key, out object value) ||
                value == null)
                return false;
            try
            {
                return Convert.ToBoolean(value);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static int ReadParameterInt(
            IReadOnlyDictionary<string, object> data,
            string key,
            int fallback)
        {
            if (data == null || !data.TryGetValue(key, out object value) ||
                value == null)
                return fallback;
            try
            {
                return Convert.ToInt32(value);
            }
            catch (Exception)
            {
                return fallback;
            }
        }

        private static int ReadParameterInt(
            IDictionary<string, object> data,
            string key,
            int fallback)
        {
            if (data == null || !data.TryGetValue(key, out object value) ||
                value == null)
                return fallback;
            try
            {
                return Convert.ToInt32(value);
            }
            catch (Exception)
            {
                return fallback;
            }
        }

        private static string ReadParameterString(
            IReadOnlyDictionary<string, object> data,
            string key,
            string fallback)
        {
            return data != null && data.TryGetValue(key, out object value) &&
                   value != null
                ? value.ToString()
                : fallback;
        }

        private static string ReadParameterString(
            IDictionary<string, object> data,
            string key,
            string fallback)
        {
            return data != null && data.TryGetValue(key, out object value) &&
                   value != null
                ? value.ToString()
                : fallback;
        }

        private static string ResolveTrackingStatus(
            string requested,
            GameplaySessionMode mode,
            int restartCount,
            bool dailyAlreadyStarted,
            bool restoredSnapshot,
            IDictionary<string, object> retryParameters,
            IDictionary<string, object> directParameters)
        {
            if (requested == TrackerCatalog.GameStatus.New ||
                requested == TrackerCatalog.GameStatus.Continue ||
                requested == TrackerCatalog.GameStatus.Restart)
                return requested;
            if (restartCount > 0)
                return TrackerCatalog.GameStatus.Restart;
            if (mode == GameplaySessionMode.Daily)
                return dailyAlreadyStarted
                    ? TrackerCatalog.GameStatus.Continue
                    : TrackerCatalog.GameStatus.New;
            if (restoredSnapshot)
                return TrackerCatalog.GameStatus.Continue;
            if (mode == GameplaySessionMode.Main && directParameters == null &&
                retryParameters != null && retryParameters.Count > 0)
                return TrackerCatalog.GameStatus.Continue;
            return TrackerCatalog.GameStatus.New;
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
            _gestureHistoryCount = _session.History.Count;
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
                if (resolved == GameSessionState.Failed)
                {
                    StopGameplayClock();
                    TrySettleFail();
                }
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
            if (_session != null)
            {
                _session.CommitCurrentStep();
                if (_gestureHistoryCount >= 0 &&
                    _session.History.Count > _gestureHistoryCount)
                    CountBoardStep();
            }
            _gestureHistoryCount = -1;
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

                bool applied = ApplyCellState(
                    action.Row,
                    action.Column,
                    action.State,
                    action.PlayAnimation,
                    action.Record);
                if (applied && action.Before == CellStateType.MARK &&
                    action.State == CellStateType.EMPTY)
                    _tracker?.IncrementStat("erase_count");
                if (applied && action.Vibrate >= 0)
                    VibrationRuntime.Current.Play(action.Vibrate);
            }
        }

        private SessionActionResult ConsumeDoubleTap(int row, int column)
        {
            SessionActionResult result = _session.DoubleTap(row, column);
            if (!result.Accepted) return result;
            ApplyActionResult(result, true);
            if (result.Kind == SessionActionKind.WrongGuess)
            {
                _gameState?.IncrementGameTotalStat(
                    CurrentTrackingGameType(),
                    "invalid_sign_total");
                double delay = result.LivesAfter <= 0 ? 0.6 : 0.4;
                _wrongResolutionDeadline = Time.unscaledTimeAsDouble + delay;
            }
            return result;
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
            if (IsEntryBlocked()) return;
            _tracker?.TrackButtonClick(TrackerCatalog.Button.Clear);
            _tracker?.IncrementStat("clear_used");
            _gameState?.IncrementGameTotalStat(
                CurrentTrackingGameType(),
                "clear_used_total");
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
            _tracker?.TrackButtonClick(TrackerCatalog.Button.Locate);
            _idleToolHint?.Reset();
            if (!CanAcceptInput() || _toolResources == null || _gameState == null)
                return ToolResourceDecision.Rejected;

            ToolResourceDecision decision = _toolResources.TryConsume(
                GameToolKind.Locate,
                _gameState.CurrentLevel,
                NowMilliseconds());
            TrackToolConsumption(GameToolKind.Locate, decision);
            if (decision == ToolResourceDecision.RewardRequired)
                ToolRewardRequested?.Invoke(GameToolKind.Locate);

            // This ordering is intentional: BaseGamePage marks Locate dirty/DDA
            // after _consume_tool even when the reward path was required.
            _gameState.MarkCurrentLevelDirty();
            _gameState.MarkDdaToolOrReviveUsed();
            if (!IsAuthorized(decision)) return decision;

            _tracker?.IncrementStat("locate_used");
            _gameState.IncrementGameTotalStat(
                CurrentTrackingGameType(),
                "locate_used_total");
            action = ApplyAuthorizedLocate();
            if (action.Accepted)
            {
                _toolsUsed++;
                CountBoardStep();
            }
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
            _tracker?.TrackButtonClick(TrackerCatalog.Button.Hint);
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

            _tracker?.IncrementStat("hint_used");
            _gameState.IncrementGameTotalStat(
                CurrentTrackingGameType(),
                "hint_used_total");
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
            TrackToolConsumption(GameToolKind.Hint, consumed);
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
            _tracker?.TrackButtonClick(TrackerCatalog.Button.HintApply);
            _tracker?.IncrementStat("hint_apply_used");
            SessionHintRequest appliedRequest = _activeHint;
            SessionActionResult result = _session.ApplyHint();
            if (result.Accepted)
            {
                _toolsUsed++;
                int hintCrossCount = 0;
                for (int index = 0; index < result.Changes.Count; index++)
                    if (CellState.IsCross(result.Changes[index].After))
                        hintCrossCount++;
                if (hintCrossCount > 0)
                    _tracker?.IncrementStat("hint_cross_count", hintCrossCount);
                ApplyActionResult(result, true);
                CountBoardStep();
            }
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

        public void DismissRequestedHint()
        {
            if (_activeHint == null) return;
            _tracker?.TrackButtonClick(TrackerCatalog.Button.HintStop);
            _tracker?.IncrementStat("hint_stop_used");
            CancelRequestedHint();
        }

        public void NotifyHintDetailRequested()
        {
            HintChainDetail chain = _activeHint?.Hint?.Chain;
            if (chain == null || chain.Steps == null || chain.Steps.Count == 0)
                return;
            _tracker?.TrackButtonClick(TrackerCatalog.Button.HintDetail);
            _tracker?.IncrementStat("hint_detail_used");
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
            RefreshSnapshotElapsedTime();
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
            StartGameplayClock();
            return true;
        }

        public bool RestartLevel()
        {
            if (_session != null &&
                _session.State == GameSessionState.Failed &&
                _transitions != null &&
                _transitions.TryRestartAfterFail(
                    _session,
                    _snapshotContext,
                    out MainGameTransitionData failedRestart))
            {
                StopGameplayClock();
                if (_sessionMode != GameplaySessionMode.Daily)
                    _rankActivityRuntime?.Manager?.NotifyLevelRestart();
                PublishTransition(failedRestart);
                _session.BeginLeaving();
                LoadLevel(
                    failedRestart.Level,
                    failedRestart.RetryParameters,
                    failedRestart.RestartCount,
                    _sessionMode);
                return true;
            }

            if (_transitions == null || !_transitions.TryRestart(
                    _session,
                    _snapshotContext,
                    out MainGameTransitionData transition))
                return false;

            StopGameplayClock();
            if (_sessionMode != GameplaySessionMode.Daily)
                _rankActivityRuntime?.Manager?.NotifyLevelRestart();
            PublishTransition(transition);
            _session.BeginLeaving();
            LoadLevel(
                transition.Level,
                transition.RetryParameters,
                transition.RestartCount,
                _sessionMode);
            return true;
        }

        public bool QuitLevel()
        {
            RefreshSnapshotElapsedTime();
            if (_transitions == null || !_transitions.TryQuit(
                    _session,
                    _snapshotContext,
                    out MainGameTransitionData transition))
                return false;

            _snapshotDirty = false;
            StopGameplayClock();
            if (_sessionMode != GameplaySessionMode.Daily)
                _rankActivityRuntime?.Manager?.NotifyLevelExit();
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

            if (_lastTransition.IsDailySession)
            {
                _session.BeginLeaving();
                LoadLevel(
                    _gameState.CurrentLevel,
                    sessionMode: GameplaySessionMode.Main);
            }
            else if (_lastTransition.IsBankSession)
            {
                if (!TryCreateNextBankLaunch(out BankLaunchRequest request))
                    return false;
                _session.BeginLeaving();
                LoadLevel(
                    0,
                    new Dictionary<string, object>(request.Parameters),
                    sessionMode: GameplaySessionMode.Bank);
            }
            else
            {
                _session.BeginLeaving();
                LoadLevel(_gameState.CurrentLevel);
            }
            return true;
        }

        private bool TryCreateNextBankLaunch(out BankLaunchRequest request)
        {
            return BankBrowserContract.TryCreateNextLaunch(
                _snapshotContext?.Entry,
                out request);
        }

        private void TrySettleFail()
        {
            if (_transitions != null && _transitions.TrySettleFail(
                    _session,
                    _snapshotContext,
                    out MainGameTransitionData transition))
            {
                _tracker?.IncrementStat("gamedie_count");
                PublishTransition(transition);
            }
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
            if (transition != null)
            {
                transition.ElapsedSeconds = (float)Math.Max(
                    0.0,
                    CurrentGameplayElapsed());
                transition.ToolsUsed = _toolsUsed;
                bool dailyCommitted = false;
                if (transition.IsDailySession &&
                    transition.Kind == MainGameTransitionKind.Won &&
                    _gameState != null)
                {
                    string date = string.IsNullOrEmpty(transition.DailyDate)
                        ? DailyEntryStateContract.DateKey(DateTime.Now)
                        : transition.DailyDate;
                    transition.DailyDate = date;
                    dailyCommitted = DailyWinSettlement.Commit(
                        _gameState,
                        transition,
                        Math.Max(0, (int)transition.ElapsedSeconds),
                        _snapshotContext?.Entry?.Rank ?? 4,
                        _snapshotContext?.Entry?.Size ?? 12);
                }
                if (transition.Kind == MainGameTransitionKind.Won &&
                    !transition.IsDailySession &&
                    _rankActivityRuntime?.Manager != null)
                {
                    RankActivityManager rank = _rankActivityRuntime.Manager;
                    rank.SetLevelCollect(RankActivityConfig.MapCollect(
                        rank.Group,
                        _currentPuzzleSize,
                        _session?.Lives ?? 0));
                    rank.NotifyLevelWin();
                }
                if (transition.Kind == MainGameTransitionKind.Won &&
                    !transition.StreakSettlementCommitted &&
                    _dailyMetaRuntime != null &&
                    (!transition.IsDailySession || dailyCommitted))
                {
                    _dailyMetaRuntime.SettleWin(
                        transition.IsDailySession
                            ? StreakCheckinSource.Challenge
                            : StreakCheckinSource.Main);
                    transition.StreakSettlementCommitted = true;
                }
            }
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
            ApplyViewChanges(
                result.Changes,
                playAnimation,
                playSounds,
                result.Kind == SessionActionKind.Undo);
            PlayActionVibrationAndMeow(result);
            PublishHudState();
            if (result.IsComplete) StopGameplayClock();
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

        private void PlayActionVibrationAndMeow(SessionActionResult result)
        {
            if (result == null || !result.Accepted) return;
            if (result.Kind == SessionActionKind.WrongGuess)
                VibrationRuntime.Current.Play(VibrationLevel.Level3);
            else if (result.Kind == SessionActionKind.Hint)
            {
                for (int index = 0; index < result.Changes.Count; index++)
                {
                    BoardStateChange change = result.Changes[index];
                    if (change.Before != CellStateType.MARK ||
                        change.After != CellStateType.EMPTY)
                        continue;
                    VibrationRuntime.Current.Play(VibrationLevel.Level2);
                    break;
                }
            }

            int catChangeCount = 0;
            for (int index = 0; index < result.Changes.Count; index++)
                if (result.Changes[index].After == CellStateType.CAT &&
                    result.Changes[index].Before != CellStateType.CAT)
                    catChangeCount++;
            if (catChangeCount == 0) return;

            int catsAfter = _currentPuzzleSize - (_session?.RemainingCats ?? 0);
            int catChangeIndex = 0;
            for (int index = 0; index < result.Changes.Count; index++)
            {
                BoardStateChange change = result.Changes[index];
                if (change.After != CellStateType.CAT ||
                    change.Before == CellStateType.CAT)
                    continue;

                int combo = ComboForPosition(result.Feedback, change.Position);
                int vibrationLevel = combo >= 1 &&
                                     _vibrateComboConfig?.IsEnabled() == true
                    ? _vibrateComboConfig.ComboVibrationLevel(combo)
                    : (int)VibrationLevel.Level3;
                if (vibrationLevel >= 0)
                    VibrationRuntime.Current.Play(vibrationLevel);

                int catOnBoard = catsAfter - catChangeCount + (++catChangeIndex);
                if (_meowFeedbackConfig?.IsEnabled() != true ||
                    catOnBoard >= _currentPuzzleSize)
                    continue;
                int meowIndex = catOnBoard - _meowPrefillCatCount;
                if (meowIndex <= 0) continue;
                soundService?.PlayMeowByPath(_meowFeedbackConfig.GetMeowPath(
                    meowIndex,
                    UnityEngine.Random.Range(1, 8)));
            }
        }

        private static int ComboForPosition(
            IReadOnlyList<GameplayFeedbackData> feedback,
            Vector2Int position)
        {
            if (feedback == null) return -1;
            for (int index = 0; index < feedback.Count; index++)
            {
                GameplayFeedbackData item = feedback[index];
                if (item != null &&
                    item.Kind == GameplayFeedbackKind.CorrectCat &&
                    item.Position == position)
                    return item.ComboCount;
            }
            return -1;
        }

        public void SetResultBgmPaused(bool paused)
        {
            soundService?.SetBgmPaused(paused);
        }

        public void PlayResultSound(SoundKind kind)
        {
            soundService?.Play(kind);
        }

        private void StartGameplayClock()
        {
            if (_activePlayStartedAt > 0.0) return;
            _activePlayStartedAt = Time.unscaledTimeAsDouble;
        }

        private void StopGameplayClock()
        {
            if (_activePlayStartedAt <= 0.0) return;
            _elapsedPlaySeconds += Math.Max(
                0.0,
                Time.unscaledTimeAsDouble - _activePlayStartedAt);
            _activePlayStartedAt = 0.0;
        }

        private double CurrentGameplayElapsed()
        {
            return _activePlayStartedAt > 0.0
                ? _elapsedPlaySeconds + Math.Max(
                    0.0,
                    Time.unscaledTimeAsDouble - _activePlayStartedAt)
                : _elapsedPlaySeconds;
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
            bool playSounds = true,
            bool restoreAuthoritativeState = false)
        {
            for (int i = 0; i < changes.Count; i++)
            {
                BoardStateChange change = changes[i];
                if (restoreAuthoritativeState)
                    boardView.RestoreCellState(
                        change.Position.x,
                        change.Position.y,
                        change.After,
                        playAnimation);
                else
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
            if (_snapshotContext.Level <= 0) return;
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

        private void FlushSnapshot(bool force = false)
        {
            if ((!_snapshotDirty && !force) ||
                _session == null ||
                _snapshotContext == null ||
                _gameState == null)
                return;
            if (_snapshotContext.Level <= 0)
            {
                _snapshotDirty = false;
                return;
            }
            if (_session.State == GameSessionState.Won ||
                _session.State == GameSessionState.Leaving)
            {
                _snapshotDirty = false;
                return;
            }
            _snapshotDirty = false;
            RefreshSnapshotElapsedTime();
            _gameState.SetEndgameSnapshot(GameSessionSnapshot.Build(_session, _snapshotContext));
        }

        private void RefreshSnapshotElapsedTime()
        {
            if (_snapshotContext != null)
                _snapshotContext.InGameSeconds = CurrentGameplayElapsed();
        }

        private void PersistLifecycleBoundary()
        {
            if (_applicationSuspended) return;
            _applicationSuspended = true;
            // Godot rebuilds the whole snapshot on focus-out even when the
            // debounce timer is idle, so the latest in-game time is durable.
            FlushSnapshot(force: true);
            GameStateRuntime.FlushPendingWrites();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                PersistLifecycleBoundary();
            else
                _applicationSuspended = false;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                _applicationSuspended = false;
                FinishPendingEntryAd();
                _idleToolHint?.ResetElapsed();
                return;
            }
            PersistLifecycleBoundary();
        }

        private void OnApplicationQuit()
        {
            PersistLifecycleBoundary();
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

        private string CurrentTrackingGameType()
        {
            if (!string.IsNullOrEmpty(_trackingStartData?.GameType))
                return _trackingStartData.GameType;
            return _sessionMode == GameplaySessionMode.Daily
                ? TrackerCatalog.GameType.Daily
                : TrackerCatalog.GameType.Normal;
        }

        private void CountBoardStep()
        {
            _tracker?.IncrementStat("step_used");
            _gameState?.IncrementGameTotalStat(
                CurrentTrackingGameType(),
                "step_total");
        }

        private void TrackToolConsumption(
            GameToolKind kind,
            ToolResourceDecision decision)
        {
            if (_tracker == null || _toolResources == null ||
                decision != ToolResourceDecision.Consumed)
                return;
            string propName = kind == GameToolKind.Locate
                ? TrackerCatalog.Prop.Locate
                : TrackerCatalog.Prop.Hint;
            _tracker.TrackProp(
                false,
                propName,
                _tracker.CurrentSource,
                1,
                _toolResources.GetCount(kind));
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
                return _owner.DispatchIdleToolHintPlay(kind);
            }

            public void Stop(GameToolKind kind)
            {
                _owner.IdleToolHintStopRequested?.Invoke(kind);
            }
        }

        private bool DispatchIdleToolHintPlay(GameToolKind kind)
        {
            Func<GameToolKind, bool> handler = IdleToolHintPlayRequested;
            if (handler == null) return false;
            bool played = false;
            Delegate[] subscribers = handler.GetInvocationList();
            for (int index = 0; index < subscribers.Length; index++)
                played |= ((Func<GameToolKind, bool>)subscribers[index]).Invoke(kind);
            return played;
        }

        private bool CanAcceptInput()
        {
            return !_inputLocked && _session != null && _session.CanAcceptInput;
        }
    }
}
