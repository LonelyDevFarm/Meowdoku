using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Meowdoku.Core;
using Meowdoku.Core.Config;
using Meowdoku.Core.Localization;
using Meowdoku.Core.UI;
using Meowdoku.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class HomePagePresenter : UIFrameWindow
    {
        private static readonly UiName[] HomeAndGame =
            { UiName.Home, UiName.Game };

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
        [SerializeField] private GameObject dailyStreakLayout;

        [Header("Scene-owned services")]
        [SerializeField] private LocalizationCatalog localization;
        [SerializeField] private SoundService soundService;

        private readonly DailyStreakConfig _dailyStreak = new();
        private readonly LeaderboardFuncConfig _leaderboard = new();
        private readonly HardButtonConfig _hardButton = new();
        private readonly UIPopupQueue _popupQueue = new();

        private Sequence _transition;
        private Vector3 _logoBaseScale = Vector3.one;
        private Vector2 _logoBasePosition;
        private UIFrameWindow _newPage;
        private bool _isExiting;

        public bool IsExiting => _isExiting;
        public float SettingsButtonCenterY => settingsButton != null
            ? ((RectTransform)settingsButton.transform).position.y
            : 0f;

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
            if (localization != null)
                localization.LocaleChanged += RefreshPresentation;
            ApplyHeaderLayout();
        }

        protected override void OnShow(
            IReadOnlyDictionary<string, object> parameters)
        {
            _isExiting = false;
            _newPage = null;
            localization?.ApplySystemLocale(
                GameStateRuntime.Current,
                new SettingsLanguageConfig().IsLanguageSwitchEnabledPeek());
            soundService?.StartBgm();
            RefreshPresentation();
            ApplyHeaderLayout();
            PlayAppear();
        }

        protected override IEnumerator OnHide()
        {
            KillTransition();
            _popupQueue.Clear();
            _isExiting = false;
            _newPage = null;
            yield break;
        }

        protected override bool OnBackRequest()
        {
            if (_isExiting || Owner == null) return true;
            UIFrameWindow settings = Owner.Get(UiName.Setting);
            if (settings != null && settings.IsShowing) return true;

            var parameters = new Dictionary<string, object>(1)
            {
                ["on_confirm"] = (Action)Application.Quit
            };
            Owner.Show(UiName.Confirm, parameters);
            return true;
        }

        protected override void OnDestroyWindow()
        {
            KillTransition();
            if (startButton != null) startButton.onClick.RemoveListener(StartGame);
            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(OpenSettings);
            if (profileButton != null)
                profileButton.onClick.RemoveListener(OpenProfile);
            if (localization != null)
                localization.LocaleChanged -= RefreshPresentation;
            base.OnDestroyWindow();
        }

        public void BindSoundService(SoundService service)
        {
            soundService = service;
        }

        public void BindLocalization(LocalizationCatalog catalog)
        {
            if (localization == catalog) return;
            if (localization != null)
                localization.LocaleChanged -= RefreshPresentation;
            localization = catalog;
            if (localization != null)
                localization.LocaleChanged += RefreshPresentation;
            RefreshPresentation();
        }

        public void RefreshPresentation()
        {
            HomePresentationState state = HomePageContract.Resolve(
                GameStateRuntime.Current.CurrentLevel,
                _dailyStreak,
                _leaderboard,
                _hardButton);
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
            if (profileEntry != null)
                profileEntry.SetActive(state.ShowProfile);
        }

        private void StartGame()
        {
            if (_isExiting || Owner == null) return;
            _isExiting = true;
            SetButtonsInteractable(false);
            Owner.HideAllExcept(HomeAndGame);
            PlayExitToGame();
        }

        private void OpenSettings()
        {
            if (_isExiting || Owner == null) return;
            Owner.Show(UiName.Setting);
        }

        private void OpenProfile()
        {
            if (_isExiting || Owner == null ||
                profileEntry == null || !profileEntry.activeInHierarchy)
                return;
            Owner.Show(UiName.Profile);
        }

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

        private void KillTransition()
        {
            if (_transition == null) return;
            _transition.Kill(false);
            _transition = null;
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
