using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Meowdoku.Core;
using Meowdoku.Core.Ads;
using Meowdoku.Core.Config;
using Meowdoku.Core.Daily;
using Meowdoku.Core.Localization;
using Meowdoku.Core.Online;
using Meowdoku.Core.Profile;
using Meowdoku.Core.Rank;
using Meowdoku.Core.Tracking;
using Meowdoku.Core.UI;
using Meowdoku.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class HomePagePresenter : UIFrameWindow,
        IClockTickConsumer,
        IDailyMetaConsumer,
        IProfileConsumer,
        IRankActivityConsumer,
        IAbConfigRuntimeConsumer,
        IDataSyncConsumer,
        ISoundServiceConsumer
    {
        public override string GetTrackingScreenName() =>
            TrackerCatalog.Screen.Home;

        private static readonly UiName[] HomeAndGame =
            { UiName.Home, UiName.Game };
        public const string ReturnFromGameplayParameter =
            nameof(ReturnFromGameplayParameter);

        [Header("Source hierarchy")]
        [SerializeField] private RectTransform layoutSpace;
        [SerializeField] private RectTransform headerAdaptHolder;
        [SerializeField] private RectTransform header;
        [SerializeField] private RectTransform logoVisual;
        [SerializeField] private CanvasGroup backgroundGroup;
        [SerializeField] private CanvasGroup gridFlowGroup;
        [SerializeField] private CanvasGroup logoGroup;
        [SerializeField] private CanvasGroup startGroup;
        [SerializeField] private CanvasGroup settingsGroup;

        [Header("Home controls")]
        [SerializeField] private Button startButton;
        [SerializeField] private Text levelText;
        [SerializeField] private LocalizedText levelLocalizedText;
        [SerializeField] private GameObject hardBadge;
        [SerializeField] private Button settingsButton;
        [SerializeField] private GameObject profileEntry;
        [SerializeField] private Button profileButton;
        [SerializeField] private ProfileAvatarView profileAvatar;
        [SerializeField] private GameObject dailyStreakLayout;
        [SerializeField] private DailyChallengeEntryPresenter dailyEntry;
        [SerializeField] private StreakEntryPresenter streakEntry;
        [SerializeField] private StreakEntryPresenter streakMiniEntry;
        [SerializeField] private RankActivityEntryPresenter rankEntry;
        [SerializeField] private TextAsset dialogPriorityConfig;
        [SerializeField] private TextAsset abSwitchPopupConfig;

        [Header("Scene-owned services")]
        [SerializeField] private LocalizationCatalog localization;
        [SerializeField] private SoundService soundService;

        private readonly DailyStreakConfig _dailyStreak = new();
        private readonly LeaderboardFuncConfig _leaderboard = new();
        private readonly HardButtonConfig _hardButton = new();
        private readonly UIPopupQueue _popupQueue = new();

        private Sequence _transition;
        private Sequence _logoIdle;
        private Sequence _profileShake;
        private Vector3 _logoBaseScale = Vector3.one;
        private Vector2 _logoBasePosition;
        private UIFrameWindow _newPage;
        private bool _isExiting;
        private DailyMetaRuntime _dailyMetaRuntime;
        private ProfileRuntime _profileRuntime;
        private ProfileService _subscribedProfileService;
        private RankActivityRuntime _rankActivityRuntime;
        private AbConfigRuntime _abConfigRuntime;
        private DataSyncRuntime _dataSyncRuntime;
        private ClockTicker _clockTicker;
        private bool _rankPopupPending;
        private Action _quitAction = Application.Quit;

        public bool IsExiting => _isExiting;
        public float SettingsButtonCenterY => settingsButton != null
            ? ((RectTransform)settingsButton.transform).position.y
            : 0f;
        public RectTransform ProfileEntryRect =>
            profileEntry != null
                ? profileEntry.transform as RectTransform
                : null;

        protected override void OnCreate()
        {
            if (logoVisual != null)
            {
                _logoBaseScale = logoVisual.localScale;
                _logoBasePosition = logoVisual.anchoredPosition;
            }

            if (startButton != null) startButton.onClick.AddListener(StartGame);
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OpenSettings);
            if (profileButton != null)
                profileButton.onClick.AddListener(OpenProfile);
            if (dailyEntry != null)
                dailyEntry.PlayRequested += StartDaily;
            if (streakEntry != null)
                streakEntry.OpenRequested += OpenStreak;
            if (streakMiniEntry != null)
                streakMiniEntry.OpenRequested += OpenStreak;
            if (rankEntry != null)
                rankEntry.OpenRequested += OpenRankEntry;
            if (localization != null)
                localization.LocaleChanged += RefreshPresentation;
            ApplyHeaderLayout();
        }

        protected override void OnShow(
            IReadOnlyDictionary<string, object> parameters)
        {
            _isExiting = false;
            _newPage = null;
            SettingsLanguageConfig languageConfig =
                _abConfigRuntime != null
                    ? _abConfigRuntime.Settings.Language
                    : new SettingsLanguageConfig();
            localization?.ApplySystemLocale(
                GameStateRuntime.Current,
                languageConfig.IsLanguageSwitchEnabledPeek(
                    _abConfigRuntime?.ValueProvider));
            soundService?.StartBgm();
            GameStateRuntime.Current.AdvanceMaxDailyDate(
                DailyEntryStateContract.DateKey(CurrentLocalNow));
            RefreshPresentation();
            SubscribeProfileService();
            dailyEntry?.Show();
            streakEntry?.Show();
            streakMiniEntry?.Show();
            _rankActivityRuntime?.Manager?.OnHomeShown();
            rankEntry?.Show();
            ApplyMetaEntryLayout();
            ApplyHeaderLayout();
            bool returningFromGameplay = parameters != null &&
                parameters.TryGetValue(
                    ReturnFromGameplayParameter,
                    out object returnValue) &&
                returnValue is true;
            if (returningFromGameplay)
                ShowImmediate();
            else
                PlayAppear();
            BuildPopupQueue();
        }

        protected override IEnumerator OnHide()
        {
            UnsubscribeProfileService();
            KillTransition();
            KillProfileShake();
            _popupQueue.Abort();
            _isExiting = false;
            _newPage = null;
            dailyEntry?.Hide();
            streakEntry?.Hide();
            streakMiniEntry?.Hide();
            rankEntry?.Hide();
            _rankPopupPending = false;
            yield break;
        }

        protected override bool OnBackRequest()
        {
            if (_isExiting || Owner == null) return true;
            UIFrameWindow settings = Owner.Get(UiName.Setting);
            if (settings != null && settings.IsShowing) return true;

            var parameters = new Dictionary<string, object>(1)
            {
                ["on_confirm"] = _quitAction ?? (Action)Application.Quit
            };
            Owner.Show(UiName.Confirm, parameters);
            return true;
        }

        protected override void OnStackTop()
        {
            if (!IsShowing) return;
            bool controlsReady =
                (startButton == null || startButton.interactable) &&
                (settingsButton == null || settingsButton.interactable) &&
                (profileButton == null || profileButton.interactable);
            if (!_isExiting && controlsReady) return;

            // Game is shown at the source entry marker, slightly before Home's
            // exit animation finishes. A very fast Game -> Home transition can
            // therefore expose this still-showing page with its buttons locked.
            // Becoming the top window is the authoritative point at which Home
            // must be interactive again, regardless of the interrupted tween.
            _isExiting = false;
            _newPage = null;
            PlayAppear();
        }

#if UNITY_INCLUDE_TESTS
        internal void ConfigureQuitForTests(Action quit)
        {
            _quitAction = quit ?? Application.Quit;
        }

        internal string LevelTextForTests => levelText != null
            ? levelText.text
            : string.Empty;
        internal LocalizationCatalog LocalizationForTests => localization;
        internal bool PopupQueueRunningForTests => _popupQueue.IsRunning;
#endif

        protected override void OnDestroyWindow()
        {
            UnsubscribeProfileService();
            KillTransition();
            KillProfileShake();
            if (startButton != null) startButton.onClick.RemoveListener(StartGame);
            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(OpenSettings);
            if (profileButton != null)
                profileButton.onClick.RemoveListener(OpenProfile);
            if (dailyEntry != null)
                dailyEntry.PlayRequested -= StartDaily;
            if (streakEntry != null)
                streakEntry.OpenRequested -= OpenStreak;
            if (streakMiniEntry != null)
                streakMiniEntry.OpenRequested -= OpenStreak;
            if (rankEntry != null)
                rankEntry.OpenRequested -= OpenRankEntry;
            if (localization != null)
                localization.LocaleChanged -= RefreshPresentation;
            BindDataSyncRuntime(null);
            base.OnDestroyWindow();
        }

        public void BindSoundService(SoundService service)
        {
            soundService = service;
        }

        public void BindClockTicker(ClockTicker ticker)
        {
            _clockTicker = ticker;
            dailyEntry?.BindClockTicker(ticker);
        }

        private DateTime CurrentLocalNow => _clockTicker != null
            ? _clockTicker.LocalNow
            : DateTime.Now;

        public void BindDailyMetaRuntime(DailyMetaRuntime runtime)
        {
            _dailyMetaRuntime = runtime;
            streakEntry?.BindDailyMetaRuntime(runtime);
            streakMiniEntry?.BindDailyMetaRuntime(runtime);
        }

        public void BindProfileRuntime(ProfileRuntime runtime)
        {
            UnsubscribeProfileService();
            _profileRuntime = runtime;
            if (IsShowing) SubscribeProfileService();
            RefreshProfileAvatar();
        }

        public void BindAbConfigRuntime(AbConfigRuntime runtime)
        {
            _abConfigRuntime = runtime;
            if (IsShowing) RefreshPresentation();
        }

        public void BindRankActivityRuntime(RankActivityRuntime runtime)
        {
            _rankActivityRuntime = runtime;
            rankEntry?.BindRankActivityRuntime(runtime);
            if (IsShowing)
                ApplyMetaEntryLayout();
        }

        public void BindDataSyncRuntime(DataSyncRuntime runtime)
        {
            if (_dataSyncRuntime == runtime) return;
            if (_dataSyncRuntime != null)
                _dataSyncRuntime.DataSyncCompleted -=
                    HandleDataSyncCompleted;
            _dataSyncRuntime = runtime;
            if (_dataSyncRuntime != null)
                _dataSyncRuntime.DataSyncCompleted +=
                    HandleDataSyncCompleted;
        }

        private void HandleDataSyncCompleted(bool changed)
        {
            if (changed && IsShowing) RefreshPresentation();
        }

        public void BindLocalization(LocalizationCatalog catalog)
        {
            if (localization == catalog) return;
            if (localization != null)
                localization.LocaleChanged -= RefreshPresentation;
            localization = catalog;
            dailyEntry?.BindLocalization(catalog);
            streakEntry?.BindLocalization(catalog);
            streakMiniEntry?.BindLocalization(catalog);
            if (localization != null)
                localization.LocaleChanged += RefreshPresentation;
            RefreshPresentation();
        }

        public void RefreshPresentation()
        {
            HomePresentationState state = HomePageContract.Resolve(
                GameStateRuntime.Current.CurrentLevel,
                CurrentDailyStreakConfig,
                CurrentLeaderboardConfig,
                CurrentHardButtonConfig);
            if (levelText != null)
            {
                if (levelLocalizedText != null)
                    levelLocalizedText.SetArguments(state.Level);
                else
                {
                    string format = localization != null
                        ? localization.Translate("GAME_LEVEL_TITLE")
                        : "Level %d";
                    if (format == "GAME_LEVEL_TITLE") format = "Level %d";
                    levelText.text = format.Replace(
                        "%d",
                        state.Level.ToString());
                }
            }
            if (hardBadge != null) hardBadge.SetActive(state.IsHardLevel);
            if (dailyStreakLayout != null)
                dailyStreakLayout.SetActive(state.ShowDailyStreak);
            dailyEntry?.RefreshNow();
            streakEntry?.RefreshNow();
            streakMiniEntry?.RefreshNow();
            rankEntry?.RefreshNow();
            ApplyMetaEntryLayout(false);
            if (profileEntry != null)
                profileEntry.SetActive(state.ShowProfile);
            RefreshProfileAvatar();
        }

        private void ApplyMetaEntryLayout(bool refreshRankEntry = true)
        {
            if (refreshRankEntry)
                rankEntry?.RefreshNow();
            bool rankOn =
                _rankActivityRuntime?.Manager?.HasHomeEntry == true;
            if (rankEntry != null)
                rankEntry.gameObject.SetActive(rankOn);
            if (streakEntry != null)
                streakEntry.gameObject.SetActive(!rankOn);
            if (streakMiniEntry != null)
                streakMiniEntry.gameObject.SetActive(rankOn);
        }

        private void SubscribeProfileService()
        {
            ProfileService service = _profileRuntime?.Service;
            if (service == null || _subscribedProfileService == service) return;
            UnsubscribeProfileService();
            service.AvatarFrameChanged += HandleAvatarFrameChanged;
            _subscribedProfileService = service;
        }

        private void UnsubscribeProfileService()
        {
            if (_subscribedProfileService == null) return;
            _subscribedProfileService.AvatarFrameChanged -=
                HandleAvatarFrameChanged;
            _subscribedProfileService = null;
        }

        private void HandleAvatarFrameChanged()
        {
            RefreshProfileAvatar();
        }

        private void RefreshProfileAvatar()
        {
            ProfileService service = _profileRuntime?.Service;
            if (profileAvatar == null || service == null) return;
            profileAvatar.Apply(service.GetPlayerInfo());
            profileAvatar.SetRedDot(service.HasFrameRedDot);
        }

        public void PlayProfileShake()
        {
            RectTransform target = ProfileEntryRect;
            if (target == null) return;
            KillProfileShake();
            target.localScale = Vector3.one;
            _profileShake = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(target.gameObject, LinkBehaviour.KillOnDisable);
            _profileShake.Append(
                target.DOScale(1.15f, 0.06666667f)
                    .SetEase(Ease.OutQuad));
            _profileShake.Append(
                target.DOScale(1f, 0.13333335f)
                    .SetEase(Ease.InOutQuad));
            _profileShake.OnComplete(() => _profileShake = null);
        }

        private void StartGame()
        {
            if (_isExiting || Owner == null) return;
            Tracking?.TrackButtonClick(
                TrackerCatalog.Button.NormalPlay,
                GetTrackingScreenName());
            _isExiting = true;
            SetButtonsInteractable(false);
            Owner.HideAllExcept(HomeAndGame);
            PlayExitToGame();
        }

        private void OpenSettings()
        {
            if (_isExiting || Owner == null) return;
            Tracking?.TrackButtonClick(
                TrackerCatalog.Button.Settings,
                GetTrackingScreenName());
            Owner.Show(
                UiName.Setting,
                new Dictionary<string, object>(1)
                {
                    ["on_level_selected"] =
                        (Action<int>)HandlePortfolioLevelSelected
                });
        }

        private void HandlePortfolioLevelSelected(int _)
        {
            RefreshPresentation();
        }

        private void StartDaily()
        {
            if (_isExiting || Owner == null) return;
            Tracking?.TrackButtonClick(
                TrackerCatalog.Button.DailyPlay,
                GetTrackingScreenName());
            var parameters = new Dictionary<string, object>(1)
            {
                ["daily_mode"] = true
            };
            UIFrameWindow page = Owner.Show(UiName.DailyGame, parameters);
            if (page == null) return;
            _isExiting = true;
            Owner.Hide(UiName.Home);
        }

        private void OpenProfile()
        {
            if (_isExiting || Owner == null ||
                profileEntry == null || !profileEntry.activeInHierarchy)
                return;
            Tracking?.TrackButtonClick(
                TrackerCatalog.Button.SelfInfo,
                GetTrackingScreenName());
            Owner.Show(UiName.Profile);
        }

        private void OpenStreak()
        {
            if (_isExiting || Owner == null) return;
            Tracking?.TrackButtonClick(
                TrackerCatalog.Button.Streak,
                GetTrackingScreenName());
            Owner.Show(
                UiName.Streak,
                new Dictionary<string, object>
                {
                    [StreakPagePresenter.StateParameter] =
                        (int)StreakDisplayState.Main
                });
        }

        private void BuildPopupQueue()
        {
            if (_popupQueue.IsRunning || dialogPriorityConfig == null)
                return;
            _popupQueue.Clear();
            if (_dailyMetaRuntime != null)
                _dailyMetaRuntime.Streak.NotifyGroupDyed(
                    CurrentDailyStreakConfig.Value);
            var handlers =
                new Dictionary<string, Func<IEnumerator>>(
                    StringComparer.Ordinal)
                {
                    ["ab_switch_popup"] = ShowAbSwitchPopup,
                    ["rank_reward_and_tryopen_popup"] =
                        ShowRankRewardAndTryOpenPopup,
                    ["ad_reward_restored"] =
                        ShowAdRewardRestored,
                    ["rank_open_popup"] = ShowRankOpenPopup
                };
            UIPopupConfig.BuildQueueForScene(
                UIPopupConfig.ParsePriorities(
                    dialogPriorityConfig.text),
                "home",
                handlers,
                _popupQueue);
            StartManagedCoroutine(_popupQueue.Flush());
        }

        private IEnumerator ShowAbSwitchPopup()
        {
            if (_dailyMetaRuntime == null ||
                abSwitchPopupConfig == null)
                yield break;
            int targetPage =
                _dailyMetaRuntime.Streak.PendingSwitchPage;
            if (targetPage <= 0) yield break;

            IReadOnlyList<AbSwitchPopupRule> rules =
                UIPopupConfig.ParseAbSwitchRules(
                    abSwitchPopupConfig.text);
            AbSwitchPopupRule matched =
                UIPopupConfig.FindSwitchRule(
                    rules,
                    "daily_streak",
                    targetPage);
            if (matched == null) yield break;

            yield return new WaitForSecondsRealtime(0.1f);
            if (!IsShowing || _isExiting || Owner == null)
                yield break;
            var parameters = new Dictionary<string, object>(
                matched.Parameters,
                StringComparer.Ordinal);
            UIFrameWindow popup = Owner.Show(
                UiName.AbSwitchPopup,
                parameters);
            if (popup == null) yield break;

            _dailyMetaRuntime.Streak.ConsumePendingSwitch();
            yield return Owner.AwaitHidden(UiName.AbSwitchPopup);
            ApplySwitchRewards(parameters);
        }

        private void OpenRankEntry()
        {
            if (_isExiting || Owner == null || _rankPopupPending) return;
            Tracking?.TrackButtonClick(
                TrackerCatalog.Button.ChallengeEntrance,
                GetTrackingScreenName());
            RankActivityManager manager = _rankActivityRuntime?.Manager;
            if (manager == null) return;
            StartManagedCoroutine(manager.IsOpenNotJoined
                ? ShowRankOpenPopup()
                : ShowRankPageThenTryOpen());
        }

        public void RequestRankOpenPopup()
        {
            if (_isExiting || Owner == null || _rankPopupPending) return;
            StartManagedCoroutine(ShowRankOpenPopup());
        }

        private IEnumerator ShowRankPageThenTryOpen()
        {
            if (Owner == null) yield break;
            UIFrameWindow page = Owner.Show(UiName.RankActivityPage);
            if (page == null) yield break;
            yield return Owner.AwaitHidden(UiName.RankActivityPage);
            yield return null;
            yield return null;
            yield return ShowRankOpenPopup();
        }

        private IEnumerator ShowRankRewardAndTryOpenPopup()
        {
            if (_rankActivityRuntime?.Manager?.GetPendingReward() == null)
                yield break;
            yield return ShowRankPageThenTryOpen();
        }

        private IEnumerator ShowAdRewardRestored()
        {
            yield return new WaitForSecondsRealtime(
                HomePageContract.RewardRestoreDelaySeconds);
            if (_isExiting || !IsShowing || Owner == null)
                yield break;

            GameStateService state = GameStateRuntime.Current;
            var restore = new RewardRestoreService(state);
            RewardRestoreBatch batch = restore.BuildBatch(
                DateTimeOffset.Now.ToUnixTimeSeconds());
            if (batch == null) yield break;

            var parameters = new Dictionary<string, object>(1)
            {
                ["batch"] = batch
            };
            var page = Owner.Show(
                UiName.AdRewardRestored,
                parameters) as AdRewardRestoredPagePresenter;
            if (page == null) yield break;

            bool done = false;
            bool collected = false;
            void HandleCollected()
            {
                collected = true;
                done = true;
            }
            void HandleClosed() => done = true;
            page.Collected += HandleCollected;
            page.Closed += HandleClosed;
            try
            {
                while (!done && IsShowing && !_isExiting)
                    yield return null;
            }
            finally
            {
                page.Collected -= HandleCollected;
                page.Closed -= HandleClosed;
            }
            if (!done) yield break;
            restore.Complete(batch, collected);
        }

        private IEnumerator ShowRankOpenPopup()
        {
            RankActivityManager manager = _rankActivityRuntime?.Manager;
            if (manager == null || !manager.IsOpenNotJoined ||
                _rankPopupPending || Owner == null)
                yield break;
            UIFrameWindow existing = Owner.Get(UiName.RankActivityOpenPopup);
            if (existing != null && existing.IsShowing) yield break;

            _rankPopupPending = true;
            yield return new WaitForSecondsRealtime(0.1f);
            for (int frame = 0; frame < 60 && !_isExiting && !IsShowing;
                 frame++)
                yield return null;
            if (_isExiting || !IsShowing || Owner == null)
            {
                _rankPopupPending = false;
                yield break;
            }

            var popup = Owner.Show(UiName.RankActivityOpenPopup) as
                RankActivityOpenPopupPresenter;
            if (popup == null)
            {
                _rankPopupPending = false;
                yield break;
            }

            yield return Owner.AwaitHidden(UiName.RankActivityOpenPopup);
            _rankPopupPending = false;
            manager.ConfirmParticipation();
            rankEntry?.RefreshNow();
            if (_isExiting || !IsShowing) yield break;

            bool firstPeriod = manager.PeriodCount == 1;
            if (!firstPeriod && !popup.WasStarted) yield break;
            if (firstPeriod &&
                _profileRuntime?.Service?.IsIdentityDefault == true)
            {
                UIFrameWindow profile = Owner.Show(
                    UiName.Profile,
                    new Dictionary<string, object>(1)
                    {
                        ["from_rank_open_guide"] = true
                    });
                if (profile != null)
                    yield return Owner.AwaitHidden(UiName.Profile);
                if (_isExiting || !IsShowing) yield break;
            }

            EnterMainLevelCovering();
        }

        private void EnterMainLevelCovering()
        {
            if (_isExiting || Owner == null) return;
            Tracking?.TrackButtonClick(
                TrackerCatalog.Button.NormalPlay,
                GetTrackingScreenName());
            var parameters = new Dictionary<string, object>(1)
            {
                ["level_index"] = GameStateRuntime.Current.CurrentLevel
            };
            UIFrameWindow game = Owner.Show(UiName.Game, parameters);
            if (game == null) return;
            _isExiting = true;
            Owner.Hide(UiName.Home);
        }

        private static void ApplySwitchRewards(
            IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters == null ||
                !parameters.TryGetValue("reward", out object raw) ||
                raw is not IReadOnlyDictionary<string, object> rewards)
                return;
            GameStateService state = GameStateRuntime.Current;
            foreach (KeyValuePair<string, object> pair in rewards)
            {
                int count;
                try
                {
                    count = Convert.ToInt32(pair.Value);
                }
                catch (Exception)
                {
                    continue;
                }
                if (count <= 0) continue;
                state.SetToolCount(
                    pair.Key,
                    state.GetToolCount(pair.Key) + count);
            }
        }

        private DailyStreakConfig CurrentDailyStreakConfig =>
            _abConfigRuntime != null
                ? _abConfigRuntime.Home.DailyStreak
                : _dailyStreak;

        private LeaderboardFuncConfig CurrentLeaderboardConfig =>
            _abConfigRuntime != null
                ? _abConfigRuntime.Home.Leaderboard
                : _leaderboard;

        private HardButtonConfig CurrentHardButtonConfig =>
            _abConfigRuntime != null
                ? _abConfigRuntime.Home.HardButton
                : _hardButton;

        private void PlayAppear()
        {
            KillTransition();
            SetButtonsInteractable(true);
            SetAlpha(backgroundGroup, 1f);
            SetAlpha(gridFlowGroup, 0f);
            SetAlpha(logoGroup, 0f);
            SetAlpha(startGroup, 0f);
            SetAlpha(settingsGroup, 0f);
            ResetLogo(HomePageContract.LogoAppearScaleRatio);

            _transition = DOTween.Sequence().SetLink(gameObject);
            FadeAt(_transition, gridFlowGroup, 0f,
                HomePageContract.GridRevealSeconds, 1f);
            FadeAt(_transition, logoGroup, 0f,
                HomePageContract.LogoRevealSeconds, 1f);
            FadeAt(_transition, startGroup,
                HomePageContract.StartRevealDelaySeconds,
                HomePageContract.StartRevealDurationSeconds, 1f);
            FadeAt(_transition, settingsGroup,
                HomePageContract.SettingsRevealDelaySeconds,
                HomePageContract.SettingsRevealDurationSeconds, 1f);
            if (logoVisual != null)
            {
                _transition.Insert(0f, logoVisual
                    .DOScale(_logoBaseScale,
                        HomePageContract.LogoScaleEndSeconds)
                    .SetEase(Ease.Linear));
            }
            _transition.AppendInterval(Mathf.Max(
                0f,
                HomePageContract.DisappearMarkerSeconds -
                _transition.Duration()));
            _transition.OnComplete(() =>
            {
                _transition = null;
                PlayLogoIdle();
            });
        }

        private void ShowImmediate()
        {
            KillTransition();
            SetButtonsInteractable(true);
            SetAlpha(backgroundGroup, 1f);
            SetAlpha(gridFlowGroup, 1f);
            SetAlpha(logoGroup, 1f);
            SetAlpha(startGroup, 1f);
            SetAlpha(settingsGroup, 1f);
            ResetLogo(1f);
            PlayLogoIdle();
        }

        private void PlayExitToGame()
        {
            KillTransition();
            _transition = DOTween.Sequence().SetLink(gameObject);
            FadeAt(_transition, backgroundGroup, 0f,
                HomePageContract.ExitUiFadeDurationSeconds, 0f);
            FadeAt(_transition, gridFlowGroup, 0f,
                HomePageContract.ExitUiFadeDurationSeconds, 0f);
            FadeAt(_transition, startGroup, 0f,
                HomePageContract.ExitUiFadeDurationSeconds, 0f);
            FadeAt(_transition, settingsGroup, 0f,
                HomePageContract.ExitUiFadeDurationSeconds, 0f);
            FadeAt(_transition, logoGroup, 0f,
                HomePageContract.LogoExitFadeDurationSeconds, 0f);

            if (logoVisual != null)
            {
                _transition.Insert(0f, logoVisual
                    .DOScale(
                        _logoBaseScale * HomePageContract.LogoExitScaleRatio,
                        HomePageContract.LogoExitScaleDurationSeconds)
                    .SetEase(Ease.Linear));
                _transition.Insert(0f, logoVisual
                    .DOAnchorPos(
                        _logoBasePosition +
                        Vector2.up * HomePageContract.LogoExitUnityYOffset,
                        HomePageContract.LogoExitScaleDurationSeconds)
                    .SetEase(Ease.Linear));
            }

            _transition.InsertCallback(
                HomePageContract.GamePageShowDelaySeconds,
                ShowGamePage);
            _transition.AppendInterval(Mathf.Max(
                0f,
                HomePageContract.HomeHideDelaySeconds -
                _transition.Duration()));
            _transition.OnComplete(FinishExit);
        }

        private void ShowGamePage()
        {
            if (!_isExiting || Owner == null) return;
            var parameters = new Dictionary<string, object>(1)
            {
                ["level_index"] = GameStateRuntime.Current.CurrentLevel
            };
            _newPage = Owner.Show(UiName.Game, parameters);
        }

        private void FinishExit()
        {
            _transition = null;
            if (_newPage != null && Owner != null)
            {
                Owner.Hide(UiName.Home);
                return;
            }

            // A missing registry entry is an invalid integration state. Recover
            // the visible Home page so an editor preview cannot become stuck.
            _isExiting = false;
            PlayAppear();
        }

        private void ResetLogo(float scaleRatio)
        {
            if (logoVisual == null) return;
            logoVisual.anchoredPosition = _logoBasePosition;
            logoVisual.localScale = _logoBaseScale * scaleRatio;
        }

        private void PlayLogoIdle()
        {
            KillLogoIdle();
            if (logoVisual == null || !IsShowing || _isExiting) return;

            ResetLogo(1f);
            _logoIdle = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(logoVisual.gameObject, LinkBehaviour.KillOnDisable);
            _logoIdle.AppendInterval(0.35f);
            _logoIdle.Append(
                logoVisual.DOScale(_logoBaseScale * 1.035f, 0.42f)
                    .SetEase(Ease.OutSine));
            _logoIdle.Join(
                logoVisual.DOAnchorPos(
                        _logoBasePosition + Vector2.up * 5f,
                        0.42f)
                    .SetEase(Ease.OutSine));
            _logoIdle.Append(
                logoVisual.DOScale(_logoBaseScale, 0.75f)
                    .SetEase(Ease.InOutSine));
            _logoIdle.Join(
                logoVisual.DOAnchorPos(_logoBasePosition, 0.75f)
                    .SetEase(Ease.InOutSine));
            _logoIdle.AppendInterval(1.1f);
            _logoIdle.SetLoops(-1, LoopType.Restart);
        }

        private void KillLogoIdle()
        {
            if (_logoIdle != null && _logoIdle.IsActive())
                _logoIdle.Kill(false);
            _logoIdle = null;
        }

        private void KillTransition()
        {
            KillLogoIdle();
            if (_transition != null)
            {
                _transition.Kill(false);
                _transition = null;
            }
        }

        private void KillProfileShake()
        {
            if (_profileShake != null && _profileShake.IsActive())
                _profileShake.Kill(false);
            _profileShake = null;
            if (ProfileEntryRect != null)
                ProfileEntryRect.localScale = Vector3.one;
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (startButton != null) startButton.interactable = interactable;
            if (settingsButton != null)
                settingsButton.interactable = interactable;
            if (profileButton != null)
                profileButton.interactable = interactable;
        }

        private static void FadeAt(
            Sequence sequence,
            CanvasGroup group,
            float start,
            float duration,
            float target)
        {
            if (sequence == null || group == null) return;
            sequence.Insert(start, group.DOFade(target, duration)
                .SetEase(Ease.Linear));
        }

        private static void SetAlpha(CanvasGroup group, float alpha)
        {
            if (group != null) group.alpha = alpha;
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled) ApplyHeaderLayout();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) ApplyHeaderLayout();
        }

        private void ApplyHeaderLayout()
        {
            if (layoutSpace == null || header == null ||
                layoutSpace.rect.height <= 0f)
                return;

            float topInset = GetTopSafeInset();
            float adapt = topInset > 0f
                ? 0f
                : SourceGameplayPageLayout.HeaderAdaptiveMinimumFor(
                    layoutSpace.rect.height);
            if (headerAdaptHolder != null)
            {
                headerAdaptHolder.anchorMin = headerAdaptHolder.anchorMax =
                    new Vector2(0.5f, 1f);
                headerAdaptHolder.pivot = new Vector2(0.5f, 1f);
                headerAdaptHolder.anchoredPosition = new Vector2(0f, -topInset);
                headerAdaptHolder.sizeDelta = new Vector2(1080f, adapt);
            }

            header.anchorMin = header.anchorMax = new Vector2(0.5f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.anchoredPosition = new Vector2(0f, -topInset - adapt);
            header.sizeDelta = new Vector2(1080f, 120f);
        }

        private float GetTopSafeInset()
        {
            if (!Application.isMobilePlatform || Screen.width <= 0 ||
                Screen.height <= 0 || layoutSpace == null)
                return 0f;
            return Mathf.Max(0f, Screen.height - Screen.safeArea.yMax) *
                   (layoutSpace.rect.width / Screen.width);
        }
    }
}
