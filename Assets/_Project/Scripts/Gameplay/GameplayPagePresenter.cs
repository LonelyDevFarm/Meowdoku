using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Meowdoku.Core;
using Meowdoku.Core.Ads;
using Meowdoku.Core.Config;
using Meowdoku.Core.Daily;
using Meowdoku.Core.Localization;
using Meowdoku.Core.Rank;
using Meowdoku.Core.Tracking;
using Meowdoku.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameplayPagePresenter : UIFrameWindow,
        IStartupGamePrewarm,
        IClockTickConsumer,
        IDailyMetaConsumer,
        IRankActivityConsumer,
        IAdServiceConsumer,
        IAbConfigRuntimeConsumer
    {
        public override string GetTrackingScreenName() => _dailyPresentation
            ? TrackerCatalog.Screen.DailyGame
            : TrackerCatalog.Screen.NormalGame;

        private static readonly UiName[] KeepHome = { UiName.Home };

        [SerializeField] private GameplayManager gameplayManager;
        [SerializeField] private Button backButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button infoButton;
        [SerializeField] private Button returnBankButton;
        [SerializeField] private GameplayWinToastPresenter winToast;
        [Header("Daily presentation")]
        [SerializeField] private GameObject mainLevelDisplay;
        [SerializeField] private GameObject mainScoreDisplay;
        [SerializeField] private GameObject dailyDateDisplay;
        [SerializeField] private Text dailyDateText;
        [SerializeField] private GameObject dailyTimerDisplay;
        [SerializeField] private Text dailyTimerText;
        [SerializeField] private LocalizationCatalog localization;

        private readonly RuleTextConfig _ruleTextConfig = new();
        private int _resultGeneration;
        private bool _dailyPresentation;
        private double _nextDailyClockRefresh;
        private ClockTicker _clockTicker;
        private DailyMetaRuntime _dailyMetaRuntime;
        private RankActivityRuntime _rankActivityRuntime;
        private AdService _adService;
        private AbConfigRuntime _abConfigRuntime;

        private void Update()
        {
            if (_clockTicker != null || !_dailyPresentation ||
                Time.unscaledTimeAsDouble < _nextDailyClockRefresh)
                return;
            RefreshDailyPresentation();
        }

        protected override void OnCreate()
        {
            gameplayManager?.BindTracker(Tracking);
            gameplayManager?.ConfigureForPageLifecycle();
            if (gameplayManager != null)
            {
                gameplayManager.GameTransitioned += HandleGameTransition;
                gameplayManager.GameTrackingStarted += HandleGameTrackingStarted;
                gameplayManager.SessionPresentationChanged +=
                    HandleSessionPresentationChanged;
                gameplayManager.ToolRewardRequested += HandleToolRewardRequested;
                gameplayManager.HintPresentationRequested +=
                    HandleHintPresentationRequested;
                gameplayManager.HintPresentationClosed +=
                    HandleHintPresentationClosed;
            }
            Add(backButton, ExitToHome);
            Add(settingsButton, OpenSettings);
            Add(infoButton, OpenHowToPlay);
            Add(returnBankButton, ReturnToBank);
        }

        protected override void OnShow(
            IReadOnlyDictionary<string, object> parameters)
        {
            _resultGeneration++;
            Owner?.Hide(UiName.Win);
            Owner?.Hide(UiName.Fail);
            Owner?.Hide(UiName.DailyWin);
            Owner?.Hide(UiName.DailyFail);
            winToast?.HideImmediate();
            bool fromBank = ReadBool(parameters, "from_bank_browser");
            _dailyPresentation = ReadBool(parameters, "daily_mode") ||
                                 ReadBool(parameters, "is_daily");
            ReloadGameStartConfigs();
            SubscribeClock();
            ApplySessionPresentation();
            if (returnBankButton != null)
                returnBankButton.gameObject.SetActive(fromBank);
            if (infoButton != null)
                infoButton.gameObject.SetActive(_ruleTextConfig.IsInfoPopup());
            gameplayManager?.BindTracker(Tracking);
            gameplayManager?.OpenPage(parameters);
            if (_dailyPresentation) RefreshDailyPresentation();
        }

        protected override IEnumerator OnHide()
        {
            _resultGeneration++;
            Owner?.Hide(UiName.Win);
            Owner?.Hide(UiName.Fail);
            Owner?.Hide(UiName.DailyWin);
            Owner?.Hide(UiName.DailyFail);
            winToast?.HideImmediate();
            DestroyBanner();
            UnsubscribeClock();
            _dailyPresentation = false;
            ApplySessionPresentation();
            gameplayManager?.ClosePage();
            yield break;
        }

        protected override bool OnBackRequest()
        {
            ExitToHome();
            return true;
        }

        protected override void OnDestroyWindow()
        {
            UnsubscribeClock();
            Remove(backButton, ExitToHome);
            Remove(settingsButton, OpenSettings);
            Remove(infoButton, OpenHowToPlay);
            Remove(returnBankButton, ReturnToBank);
            if (gameplayManager != null)
            {
                gameplayManager.GameTransitioned -= HandleGameTransition;
                gameplayManager.GameTrackingStarted -= HandleGameTrackingStarted;
                gameplayManager.SessionPresentationChanged -=
                    HandleSessionPresentationChanged;
                gameplayManager.ToolRewardRequested -= HandleToolRewardRequested;
                gameplayManager.HintPresentationRequested -=
                    HandleHintPresentationRequested;
                gameplayManager.HintPresentationClosed -=
                    HandleHintPresentationClosed;
                gameplayManager.BindTracker(null);
                gameplayManager.BindAdService(null);
            }
            base.OnDestroyWindow();
        }

        public void BindClockTicker(ClockTicker ticker)
        {
            if (_clockTicker == ticker) return;
            UnsubscribeClock();
            _clockTicker = ticker;
            SubscribeClock();
        }

        public void BindDailyMetaRuntime(DailyMetaRuntime runtime)
        {
            _dailyMetaRuntime = runtime;
            gameplayManager?.BindDailyMetaRuntime(runtime);
        }

        public void BindRankActivityRuntime(RankActivityRuntime runtime)
        {
            _rankActivityRuntime = runtime;
            gameplayManager?.BindRankActivityRuntime(runtime);
        }

        public void BindAdService(AdService service)
        {
            _adService = service;
            gameplayManager?.BindAdService(service);
        }

        public void BindAbConfigRuntime(AbConfigRuntime runtime)
        {
            _abConfigRuntime = runtime;
            gameplayManager?.BindAbConfigRuntime(runtime);
        }

        private void HandleToolRewardRequested(GameToolKind kind)
        {
            if (_adService == null || gameplayManager == null) return;
            DestroyBanner();
            string position = kind == GameToolKind.Locate
                ? gameplayManager.IsDailySession
                    ? TrackerCatalog.AdPosition.PropsDailyLocate
                    : TrackerCatalog.AdPosition.PropsNormalLocate
                : gameplayManager.IsDailySession
                    ? TrackerCatalog.AdPosition.PropsDailyHint
                    : TrackerCatalog.AdPosition.PropsNormalHint;
            int generation = _resultGeneration;
            bool started = _adService.TryShowReward(position, rewarded =>
            {
                if (generation != _resultGeneration || !IsShowing)
                    return;
                if (rewarded) gameplayManager.GrantRewardedTool(kind);
                ShowBannerIfEligible();
            });
            if (!started) ShowBannerIfEligible();
        }

        private void HandleHintPresentationRequested(
            GameplayHintPresentationData _)
        {
            DestroyBanner();
        }

        private void HandleHintPresentationClosed()
        {
            if (IsShowing) ShowBannerIfEligible();
        }

        public IEnumerator PrewarmBoard(int boardSize)
        {
            gameplayManager?.ConfigureForPageLifecycle();
            if (gameplayManager?.boardView != null)
                yield return gameplayManager.boardView.PrewarmCells(boardSize);
        }

        private void ExitToHome()
        {
            if (Owner == null || gameplayManager == null)
                return;
            Tracking?.TrackButtonClick(
                TrackerCatalog.Button.Back,
                GetTrackingScreenName());
            if (!gameplayManager.QuitLevel()) return;
            Owner.HideAllExcept(KeepHome);
            Owner.Show(UiName.Home);
        }

        private void OpenSettings()
        {
            if (Owner == null || gameplayManager == null) return;
            Tracking?.TrackButtonClick(
                TrackerCatalog.Button.Options,
                GetTrackingScreenName());
            var parameters = new Dictionary<string, object>
            {
                ["is_game_mode"] = true,
                ["on_restart"] = (Action)(() =>
                    gameplayManager.RestartLevel())
            };
            Owner.Show(UiName.Setting, parameters);
        }

        private void OpenHowToPlay()
        {
            Owner?.Show(UiName.HowToPlay);
        }

        private void ReturnToBank()
        {
            if (Owner == null) return;
            Owner.Show(UiName.Bank);
            Owner.Hide(UiName.Game);
        }

        private void ApplySessionPresentation()
        {
            SetActive(mainLevelDisplay, !_dailyPresentation);
            SetActive(mainScoreDisplay, !_dailyPresentation);
            SetActive(dailyDateDisplay, _dailyPresentation);
            SetActive(dailyTimerDisplay, _dailyPresentation);
        }

        private void RefreshDailyPresentation()
        {
            DateTime now = _clockTicker != null
                ? _clockTicker.LocalNow
                : DateTime.Now;
            string monthKey = DailyEntryStateContract.MonthLocalizationKey(
                now.Month);
            string month = localization != null
                ? localization.Translate(monthKey)
                : monthKey;
            if (string.IsNullOrEmpty(month) || month == monthKey)
                month = now.ToString("MMM", CultureInfo.InvariantCulture);
            if (dailyDateText != null)
                dailyDateText.text = DailyEntryStateContract.TodayDateText(
                    month,
                    now.Day);

            int elapsed = gameplayManager != null
                ? Math.Max(0, (int)gameplayManager.ElapsedPlaySeconds)
                : 0;
            if (dailyTimerText != null)
            {
                dailyTimerText.text = elapsed >= 3600
                    ? string.Format(
                        CultureInfo.InvariantCulture,
                        "{0:00}:{1:00}:{2:00}",
                        elapsed / 3600,
                        elapsed % 3600 / 60,
                        elapsed % 60)
                    : string.Format(
                        CultureInfo.InvariantCulture,
                        "{0:00}:{1:00}",
                        elapsed / 60,
                        elapsed % 60);
            }
            _nextDailyClockRefresh = Time.unscaledTimeAsDouble + 1.0;
        }

        private void SubscribeClock()
        {
            if (!_dailyPresentation || _clockTicker == null) return;
            _clockTicker.SecondTick -= RefreshDailyPresentation;
            _clockTicker.SecondTick += RefreshDailyPresentation;
        }

        private void UnsubscribeClock()
        {
            if (_clockTicker != null)
                _clockTicker.SecondTick -= RefreshDailyPresentation;
        }

        private void HandleGameTransition(MainGameTransitionData transition)
        {
            if (!IsShowing || Owner == null || transition == null) return;
            TrackGameTransition(transition);
            if (transition.Kind == MainGameTransitionKind.Revived)
                ShowBannerIfEligible();
            else
                DestroyBanner();
            var parameters = new Dictionary<string, object>
            {
                ["transition"] = transition,
                ["gameplay_manager"] = gameplayManager
            };
            switch (transition.Kind)
            {
                case MainGameTransitionKind.Failed:
                    Owner.Show(
                        transition.IsDailySession
                            ? UiName.DailyFail
                            : UiName.Fail,
                        parameters);
                    break;
                case MainGameTransitionKind.Won:
                    int generation = ++_resultGeneration;
                    StartManagedCoroutine(ShowWinAfterDelay(
                        generation,
                        parameters));
                    break;
                case MainGameTransitionKind.Revived:
                    Owner.Hide(
                        transition.IsDailySession
                            ? UiName.DailyFail
                            : UiName.Fail);
                    break;
                case MainGameTransitionKind.Restart:
                    Owner.Hide(UiName.Fail);
                    Owner.Hide(UiName.Win);
                    Owner.Hide(UiName.DailyFail);
                    Owner.Hide(UiName.DailyWin);
                    break;
            }
        }

        private void HandleGameTrackingStarted(GameplayTrackingStartData data)
        {
            if (data == null || Tracking == null) return;
            Tracking.SetActiveGameType(data.GameType);
            if (data.Status == TrackerCatalog.GameStatus.New)
                Tracking.NewGameId(data.GameType);
            Tracking.TrackGameStart(
                data.Qid,
                data.QuestionRotation,
                data.Status,
                data.GameType,
                data.Difficulty,
                data.Level,
                data.StrategyLayer,
                data.Scale,
                data.IsChallenge,
                data.PreType);
            ShowBannerIfEligible();
        }

        private void HandleSessionPresentationChanged(
            GameplaySessionMode mode,
            bool fromBankBrowser)
        {
            UnsubscribeClock();
            _dailyPresentation = mode == GameplaySessionMode.Daily;
            ApplySessionPresentation();
            if (returnBankButton != null)
                returnBankButton.gameObject.SetActive(fromBankBrowser);
            SubscribeClock();
            if (_dailyPresentation) RefreshDailyPresentation();
        }

        private void ShowBannerIfEligible()
        {
            if (_adService == null || gameplayManager == null ||
                !IsShowing || gameplayManager.CurrentPuzzleSize <= 0)
                return;
            string position = gameplayManager.IsDailySession
                ? "daily"
                : "game";
            var policy = new BannerPolicy(
                GameStateRuntime.Current,
                _adService,
                _abConfigRuntime?.Ads.BannerUnlockSession,
                _abConfigRuntime?.Ads.BannerUnlockLevel,
                _abConfigRuntime?.Ads.BannerExtraProtection,
                _abConfigRuntime?.Ads.BannerUnlockDifficulty);
            LivingDaysSegment segment =
                _abConfigRuntime?.CurrentLivingDaysSegment() ??
                new LivingDaysSegment(-1, 0, -1);
            policy.TryShow(
                position,
                new BannerContext(
                    gameplayManager.CurrentLevelNumber > 0,
                    gameplayManager.CurrentLevelNumber,
                    gameplayManager.CurrentPuzzleSize,
                    _adService.SessionActiveSeconds,
                    _abConfigRuntime?.FirstOpenUnixMilliseconds ?? 0,
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    segment.Index,
                    segment.Count));
        }

        private void ReloadGameStartConfigs()
        {
            if (_abConfigRuntime == null) return;
            _abConfigRuntime.ReloadTiming(AbConfigTiming.GameStart);
            if (_dailyPresentation)
            {
                _abConfigRuntime.ReloadTiming(AbConfigTiming.GameStartDaily);
                return;
            }
            _abConfigRuntime.ReloadTiming(AbConfigTiming.GameStartNormal);
            int level = GameStateRuntime.Current.CurrentLevel;
            if (level >= 11)
                _abConfigRuntime.ReloadTiming(
                    AbConfigTiming.GameStartNormal11);
            if (level >= 21)
                _abConfigRuntime.ReloadTiming(
                    AbConfigTiming.GameStartNormal21);
        }

        private void DestroyBanner()
        {
            _adService?.DestroyBanner();
        }

        private void TrackGameTransition(MainGameTransitionData transition)
        {
            if (Tracking == null || gameplayManager == null) return;
            string result = transition.Kind switch
            {
                MainGameTransitionKind.Failed => TrackerCatalog.GameResult.Fail,
                MainGameTransitionKind.Won => TrackerCatalog.GameResult.Win,
                MainGameTransitionKind.Restart => TrackerCatalog.GameResult.Quit,
                _ => string.Empty
            };
            if (string.IsNullOrEmpty(result)) return;
            IReadOnlyDictionary<string, object> values =
                gameplayManager.BuildTrackingEndParameters(
                    transition,
                    result,
                    Tracking);
            if (values != null) Tracking.TrackGameEnd(values);
            if (transition.Kind == MainGameTransitionKind.Restart)
                Tracking.OnRestart();
        }

        private IEnumerator ShowWinAfterDelay(
            int generation,
            IReadOnlyDictionary<string, object> parameters)
        {
            MainGameTransitionData transition =
                parameters != null &&
                parameters.TryGetValue("transition", out object value)
                    ? value as MainGameTransitionData
                    : null;
            bool toastWasShown = winToast != null &&
                                 winToast.TryShow(transition);
            var resultParameters = new Dictionary<string, object>();
            if (parameters != null)
                foreach (KeyValuePair<string, object> pair in parameters)
                    resultParameters[pair.Key] = pair.Value;
            resultParameters["toast_was_shown"] = toastWasShown;

            bool daily = transition != null && transition.IsDailySession;
            bool pendingStreak =
                StreakFlowCoordinator.HasPendingFlow(_dailyMetaRuntime) &&
                !_dailyMetaRuntime.Streak.IsSettleReorder;
            float delay = daily
                ? pendingStreak && !toastWasShown
                    ? DailyResultContract.AppearDelaySeconds
                    : DailyResultContract.PageShowDelaySeconds(
                        toastWasShown)
                : toastWasShown ? 1.5f : 1.2f;
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);
            if (generation != _resultGeneration || !IsShowing)
                yield break;
            if (!daily)
                yield return RunRankFlowAfterWin(generation, transition);
            if (generation != _resultGeneration || !IsShowing)
                yield break;
            if (pendingStreak)
                yield return StreakFlowCoordinator.RunBeforeResult(
                    Owner,
                    _dailyMetaRuntime);
            if (generation != _resultGeneration || !IsShowing)
                yield break;
            Owner?.Show(
                daily
                    ? UiName.DailyWin
                    : UiName.Win,
                resultParameters);
        }

        private IEnumerator RunRankFlowAfterWin(
            int generation,
            MainGameTransitionData transition)
        {
            RankActivityManager rank = _rankActivityRuntime?.Manager;
            if (rank == null || transition == null ||
                transition.IsDailySession || transition.IsBankSession ||
                !rank.IsJoined)
                yield break;
            bool settling = rank.State == RankActivityState.Settling;
            if (rank.State != RankActivityState.OpenJoined && !settling)
                yield break;
            bool changed = rank.DidLastWinScore;
            if (!changed && !settling) yield break;

            if (changed && Owner != null)
            {
                UIFrameWindow page = Owner.Show(
                    UiName.RankActivityChange,
                    new Dictionary<string, object>(1)
                    {
                        ["advance_places"] = rank.LastWinAdvance
                    });
                if (page != null)
                    yield return Owner.AwaitHidden(UiName.RankActivityChange);
                if (generation != _resultGeneration || !IsShowing)
                    yield break;
            }

            if (rank.State != RankActivityState.Settling) yield break;
            if (rank.GetPendingReward() != null)
            {
                int uid = rank.ClaimReward(false);
                if (uid >= 0 && Owner != null)
                {
                    yield return null;
                    yield return Owner.AwaitHidden(UiName.Award);
                }
                if (generation != _resultGeneration || !IsShowing)
                    yield break;
                if (rank.IsOpenNotJoined && Owner != null)
                {
                    UIFrameWindow popup = Owner.Show(
                        UiName.RankActivityOpenPopup);
                    if (popup != null)
                        yield return Owner.AwaitHidden(
                            UiName.RankActivityOpenPopup);
                    rank.ConfirmParticipation();
                }
            }
            else
            {
                rank.NotifySettlementDone();
            }
        }

        private static bool ReadBool(
            IReadOnlyDictionary<string, object> parameters,
            string key)
        {
            if (parameters == null ||
                !parameters.TryGetValue(key, out object value) ||
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

        private static void Add(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.AddListener(action);
        }

        private static void Remove(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.RemoveListener(action);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
