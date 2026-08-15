using System;
using System.Collections;
using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.Config;
using Meowdoku.Core.Localization;
using Meowdoku.Core.Tracking;
using Meowdoku.Core.UI;
using Meowdoku.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SettingsPagePresenter : UIFrameWindow,
        IAbConfigRuntimeConsumer,
        ISettingsExternalServicesConsumer,
        ISoundServiceConsumer
    {
        public override string GetTrackingDialogName() => _isGameMode
            ? TrackerCatalog.Dialog.Options
            : TrackerCatalog.Dialog.Settings;

        [Header("Popup hierarchy")]
        [SerializeField] private RectTransform panel;
        [SerializeField] private GenericPopupAnimator popupAnimator;
        [SerializeField] private Text titleText;
        [SerializeField] private LocalizedText versionLocalizedText;

        [Header("Toggle grid")]
        [SerializeField] private RectTransform toggleGrid;
        [SerializeField] private HorizontalLayoutGroup toggleGridLayout;
        [SerializeField] private SettingsToggleView musicToggle;
        [SerializeField] private SettingsToggleView soundToggle;
        [SerializeField] private SettingsToggleView vibrationToggle;
        [SerializeField] private SettingsToggleView peopleToggle;

        [Header("Portfolio demo")]
        [SerializeField] private GameObject levelSelectorRow;
        [SerializeField] private Button previousLevelButton;
        [SerializeField] private InputField levelInput;
        [SerializeField] private Button nextLevelButton;

        [Header("Optional switch rows")]
        [SerializeField] private GameObject optionalSwitchSpacer;
        [SerializeField] private GameObject optionalSwitchContainer;
        [SerializeField] private LanguageSwitchWidget languageSwitchWidget;
        [SerializeField] private GameObject patternSwitch;
        [SerializeField] private Button patternButton;
        [SerializeField] private GameObject patternOn;
        [SerializeField] private GameObject patternOff;
        [SerializeField] private GameObject patternDot;

        [Header("Action rows")]
        [SerializeField] private GameObject actionSpacer;
        [SerializeField] private Button languageButton;
        [SerializeField] private Button feedbackButton;
        [SerializeField] private Button howToPlayButton;
        [SerializeField] private GameObject restartRow;
        [SerializeField] private Button restartButton;
        [SerializeField] private LayoutElement afterActionsSpacer;
        [SerializeField] private GameObject cmpRow;
        [SerializeField] private Button cmpButton;
        [SerializeField] private GameObject termsRow;
        [SerializeField] private Button termsButton;
        [SerializeField] private Button privacyButton;
        [SerializeField] private GameObject versionRow;
        [SerializeField] private Text versionText;
        [SerializeField] private LayoutElement bottomSpacer;

        [Header("Feedback boundaries")]
        [SerializeField] private SourceToastView toast;
        [SerializeField] private LocalizationCatalog localization;
        [SerializeField] private SoundService soundService;
        [SerializeField] private bool cmpRequired;

        private readonly SettingsLanguageConfig _languageConfig = new();
        private readonly BlindModConfig _blindModeConfig = new();
        private readonly RuleTextConfig _ruleTextConfig = new();
        private AbConfigRuntime _abConfigRuntime;
        private ISettingsExternalServices _externalServices =
            OfflineSettingsExternalServices.Instance;

        private Action _onRestart;
        private Action _onPatternChanged;
        private Action _onClose;
        private Action _onCmp;
        private Action _onVibrationPreview;
        private Action<int> _onLevelSelected;
        private bool _isGameMode;
        private bool _restartConsumed;
        private bool _skipNextCloseAnimation;
        private bool _suppressNextCloseCallback;
        private bool _waitingForHowToPlay;
        private HowToPlayPagedPagePresenter _howToPlayPage;
        // Interview/demo range; level generation remains valid beyond the
        // original authored progression, while three digits keeps mobile input compact.
        private const int MinimumPortfolioLevel = 1;
        private const int MaximumPortfolioLevel = 999;
#if UNITY_INCLUDE_TESTS
        private string _systemLocaleOverrideForTests = string.Empty;
#endif

        public bool IsGameMode => _isGameMode;

        protected override void OnCreate()
        {
            BindToggle(musicToggle, ToggleMusic);
            BindToggle(soundToggle, ToggleSound);
            BindToggle(vibrationToggle, ToggleVibration);
            BindToggle(peopleToggle, TogglePeople);
            AddListener(patternButton, TogglePattern);
            AddListener(languageButton, OpenLanguage);
            AddListener(feedbackButton, OpenFeedback);
            AddListener(howToPlayButton, OpenHowToPlay);
            AddListener(restartButton, RestartGame);
            AddListener(cmpButton, OpenCmp);
            AddListener(termsButton, OpenTerms);
            AddListener(privacyButton, OpenPrivacy);
            AddListener(previousLevelButton, SelectPreviousLevel);
            AddListener(nextLevelButton, SelectNextLevel);
            if (levelInput != null)
                levelInput.onEndEdit.AddListener(CommitLevelInput);
            if (languageSwitchWidget != null)
            {
                languageSwitchWidget.LanguagePicked += ApplyLanguageAndClose;
                languageSwitchWidget.DropdownOpened += HandleLanguageDropdownOpened;
                languageSwitchWidget.DropdownClosed += HandleLanguageDropdownClosed;
            }
            if (localization != null)
                localization.LocaleChanged += RefreshStaticText;
            RefreshStaticText();
        }

        protected override void OnShow(
            IReadOnlyDictionary<string, object> parameters)
        {
            popupAnimator?.Stop();
            DetachHowToPlayWait();
            _isGameMode = Parameter(parameters, "is_game_mode", false);
            _onRestart = Parameter<Action>(parameters, "on_restart");
            _onPatternChanged = Parameter<Action>(parameters, "on_pattern_changed");
            _onClose = Parameter<Action>(parameters, "on_close");
            _onCmp = Parameter<Action>(parameters, "on_cmp");
            _onVibrationPreview = Parameter<Action>(parameters, "on_vibration_preview");
            _onLevelSelected = Parameter<Action<int>>(
                parameters,
                "on_level_selected");
            _restartConsumed = false;
            _skipNextCloseAnimation = false;
            _suppressNextCloseCallback = false;

            _abConfigRuntime?.ReloadTiming(AbConfigTiming.OpenSetting);
            SettingsLanguageConfig languageConfig = LanguageConfig;
            string systemLocale = ResolveSystemLocale();
            localization?.ApplySystemLocale(
                GameStateRuntime.Current,
                languageConfig.IsLanguageSwitchEnabled(),
                systemLocale);

            SettingsPresentationState state = SettingsPageContract.Resolve(
                _isGameMode,
                systemLocale,
                GameStateRuntime.Current.TutorialDone,
                GameStateRuntime.Current.PatternSwitchDotDismissed,
                cmpRequired || _externalServices.IsConsentManagementRequired,
                languageConfig,
                BlindModeConfig,
                RuleTextConfig);
            ApplyLayout(state);
            if (state.ShowLanguageDropdown)
            {
                languageSwitchWidget?.Setup(
                    systemLocale);
            }
            RefreshToggleValues();
            RefreshLevelSelector();
            RefreshStaticText();
            RebuildAndCenterPanel(state);
            popupAnimator?.PlayOpen();
        }

        protected override IEnumerator PlayCloseAnimation()
        {
            if (_skipNextCloseAnimation)
            {
                _skipNextCloseAnimation = false;
                yield break;
            }

            if (popupAnimator != null)
                yield return popupAnimator.PlayClose();
        }

        protected override IEnumerator OnHide()
        {
            popupAnimator?.Stop();
            languageSwitchWidget?.ForceClose();
            bool shouldCall = !_suppressNextCloseCallback;
            _suppressNextCloseCallback = false;
            if (shouldCall) _onClose?.Invoke();
            yield break;
        }

        protected override void OnCloseButtonPressed()
        {
            TrackButton(TrackerCatalog.Button.Close);
        }

        protected override void OnDestroyWindow()
        {
            popupAnimator?.Stop();
            DetachHowToPlayWait();
            UnbindToggle(musicToggle, ToggleMusic);
            UnbindToggle(soundToggle, ToggleSound);
            UnbindToggle(vibrationToggle, ToggleVibration);
            UnbindToggle(peopleToggle, TogglePeople);
            RemoveListener(patternButton, TogglePattern);
            RemoveListener(languageButton, OpenLanguage);
            RemoveListener(feedbackButton, OpenFeedback);
            RemoveListener(howToPlayButton, OpenHowToPlay);
            RemoveListener(restartButton, RestartGame);
            RemoveListener(cmpButton, OpenCmp);
            RemoveListener(termsButton, OpenTerms);
            RemoveListener(privacyButton, OpenPrivacy);
            RemoveListener(previousLevelButton, SelectPreviousLevel);
            RemoveListener(nextLevelButton, SelectNextLevel);
            if (levelInput != null)
                levelInput.onEndEdit.RemoveListener(CommitLevelInput);
            if (languageSwitchWidget != null)
            {
                languageSwitchWidget.LanguagePicked -= ApplyLanguageAndClose;
                languageSwitchWidget.DropdownOpened -= HandleLanguageDropdownOpened;
                languageSwitchWidget.DropdownClosed -= HandleLanguageDropdownClosed;
            }
            if (localization != null)
                localization.LocaleChanged -= RefreshStaticText;
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
                localization.LocaleChanged -= RefreshStaticText;
            localization = catalog;
            if (localization != null)
                localization.LocaleChanged += RefreshStaticText;
            RefreshStaticText();
        }

        public void BindAbConfigRuntime(AbConfigRuntime runtime)
        {
            _abConfigRuntime = runtime;
        }

        public void BindSettingsExternalServices(
            ISettingsExternalServices services)
        {
            _externalServices = services ??
                OfflineSettingsExternalServices.Instance;
        }

#if UNITY_INCLUDE_TESTS
        internal void OverrideSystemLocaleForTests(string locale)
        {
            _systemLocaleOverrideForTests = locale ?? string.Empty;
        }
#endif

        private SettingsLanguageConfig LanguageConfig =>
            _abConfigRuntime?.Settings.Language ?? _languageConfig;

        private BlindModConfig BlindModeConfig =>
            _abConfigRuntime?.Settings.BlindMode ?? _blindModeConfig;

        private RuleTextConfig RuleTextConfig =>
            _abConfigRuntime?.Settings.RuleText ?? _ruleTextConfig;

        private void ApplyLayout(SettingsPresentationState state)
        {
            SetActive(musicToggle, state.ShowMusic);
            SetActive(soundToggle, state.ShowSound);
            SetActive(vibrationToggle, state.ShowVibration);
            SetActive(peopleToggle, state.ShowPeople);
            if (levelSelectorRow != null)
                levelSelectorRow.SetActive(
                    state.ShowPortfolioLevelSelector);
            if (toggleGridLayout != null)
                toggleGridLayout.spacing = state.ToggleHorizontalSeparation;

            if (languageSwitchWidget != null)
                languageSwitchWidget.gameObject.SetActive(
                    state.ShowLanguageDropdown);
            if (patternSwitch != null)
                patternSwitch.SetActive(state.ShowPattern);
            if (optionalSwitchContainer != null)
                optionalSwitchContainer.SetActive(state.ShowToggleContainer);
            if (optionalSwitchSpacer != null)
                optionalSwitchSpacer.SetActive(state.ShowToggleContainer);
            if (patternDot != null) patternDot.SetActive(state.ShowPatternDot);

            SetActive(languageButton, state.ShowLanguageButton);
            SetActive(feedbackButton, state.ShowFeedback);
            SetActive(howToPlayButton, state.ShowHowToPlay);
            if (restartRow != null) restartRow.SetActive(state.ShowRestart);
            if (actionSpacer != null) actionSpacer.SetActive(true);
            if (afterActionsSpacer != null)
                afterActionsSpacer.preferredHeight = state.IsGameMode ? 0f : 50f;
            if (cmpRow != null) cmpRow.SetActive(state.ShowCmp);
            if (termsRow != null) termsRow.SetActive(state.ShowTerms);
            if (versionRow != null) versionRow.SetActive(state.ShowVersion);
            if (bottomSpacer != null)
                bottomSpacer.preferredHeight = state.BottomSpacerMinimum;

            ConfigureActionRows(state);
            ConfigureLegalLinks();
        }

        private void ConfigureActionRows(SettingsPresentationState state)
        {
            RectTransform container = feedbackButton != null
                ? feedbackButton.transform.parent as RectTransform
                : null;
            if (container == null) return;

            int visibleRows = 0;
            if (state.ShowLanguageButton) visibleRows++;
            if (state.ShowFeedback) visibleRows++;
            if (state.ShowHowToPlay) visibleRows++;
            if (state.ShowRestart) visibleRows++;

            VerticalLayoutGroup layout =
                container.GetComponent<VerticalLayoutGroup>();
            float spacing = layout != null ? layout.spacing : 0f;
            float height = visibleRows > 0
                ? visibleRows * SettingsPageContract.MainButtonHeight +
                  (visibleRows - 1) * spacing
                : 0f;
            LayoutElement element = container.GetComponent<LayoutElement>();
            if (element == null)
                element = container.gameObject.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
        }

        private void ConfigureLegalLinks()
        {
            if (termsRow == null) return;
            HorizontalLayoutGroup horizontal =
                termsRow.GetComponent<HorizontalLayoutGroup>();
            if (horizontal != null) horizontal.enabled = false;

            VerticalLayoutGroup layout =
                termsRow.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
                layout = termsRow.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 0f;
            layout.padding = new RectOffset(40, 40, 0, 0);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            LayoutElement row = termsRow.GetComponent<LayoutElement>();
            if (row != null)
            {
                row.minHeight = 140f;
                row.preferredHeight = 140f;
            }
            ConfigureLegalLink(termsButton);
            ConfigureLegalLink(privacyButton);
        }

        private static void ConfigureLegalLink(Button button)
        {
            if (button == null) return;
            LayoutElement element = button.GetComponent<LayoutElement>();
            if (element != null)
            {
                element.minWidth = 0f;
                element.preferredWidth = 750f;
                element.flexibleWidth = 1f;
                element.minHeight = 65f;
                element.preferredHeight = 65f;
                element.flexibleHeight = 0f;
            }

            Text label = button.GetComponentInChildren<Text>(true);
            if (label == null) return;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 30;
            label.resizeTextMaxSize = 40;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private void RefreshToggleValues()
        {
            GameStateService state = GameStateRuntime.Current;
            musicToggle?.SetValue(state.MusicOn);
            soundToggle?.SetValue(state.SoundOn);
            vibrationToggle?.SetValue(state.VibrationOn);
            peopleToggle?.SetValue(state.PeopleOn);
            SetPatternVisual(state.PatternModeOn);
        }

        private void RefreshLevelSelector()
        {
            if (levelInput == null) return;
            levelInput.SetTextWithoutNotify(
                Mathf.Clamp(
                    GameStateRuntime.Current.CurrentLevel,
                    MinimumPortfolioLevel,
                    MaximumPortfolioLevel)
                .ToString());
        }

        private void SelectPreviousLevel()
        {
            SelectPortfolioLevel(ReadLevelInput() - 1);
        }

        private void SelectNextLevel()
        {
            SelectPortfolioLevel(ReadLevelInput() + 1);
        }

        private void CommitLevelInput(string value)
        {
            if (!int.TryParse(value, out int level))
            {
                RefreshLevelSelector();
                return;
            }
            SelectPortfolioLevel(level);
        }

        private int ReadLevelInput()
        {
            return levelInput != null &&
                   int.TryParse(levelInput.text, out int level)
                ? Mathf.Clamp(
                    level,
                    MinimumPortfolioLevel,
                    MaximumPortfolioLevel)
                : GameStateRuntime.Current.CurrentLevel;
        }

        private void SelectPortfolioLevel(int requestedLevel)
        {
            int level = Mathf.Clamp(
                requestedLevel,
                MinimumPortfolioLevel,
                MaximumPortfolioLevel);
            GameStateService state = GameStateRuntime.Current;
            levelInput?.SetTextWithoutNotify(level.ToString());
            if (state.CurrentLevel == level) return;

            // A manually selected portfolio level must never restore the
            // unfinished board or retry seed belonging to another level.
            state.ClearEndgameSnapshot();
            state.SetRetryPuzzle(0, null);
            state.ResetCurrentLevelRuntimeFlags();
            state.SetCurrentLevel(level);
            _onLevelSelected?.Invoke(level);
        }

        private void RefreshStaticText()
        {
            if (titleText != null)
                titleText.text = Translate("SETTING_TITLE", "Settings");
            if (versionText != null)
            {
                if (versionLocalizedText != null)
                    versionLocalizedText.SetArguments(Application.version);
                else
                {
                    string format = Translate("SETTING_VERSION", "Version %s");
                    versionText.text = format.Replace("%s", Application.version);
                }
            }
        }

        private void RebuildAndCenterPanel(SettingsPresentationState state)
        {
            if (panel == null) return;
            RectTransform titleBar = titleText != null
                ? titleText.transform.parent as RectTransform
                : null;
            if (titleBar != null)
                titleBar.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    SettingsPageContract.TitleBarHeight);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            if (panel.childCount > 0 &&
                panel.GetChild(0) is RectTransform layoutRoot)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
                float preferredHeight =
                    LayoutUtility.GetPreferredHeight(layoutRoot);
                if (preferredHeight > 0f)
                    panel.SetSizeWithCurrentAnchors(
                        RectTransform.Axis.Vertical,
                        preferredHeight);
                EnsurePanelContainsLastRow(layoutRoot, state);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        }

        private void EnsurePanelContainsLastRow(
            RectTransform layoutRoot,
            SettingsPresentationState state)
        {
            RectTransform lastRow = state.IsGameMode
                ? restartRow != null
                    ? restartRow.transform as RectTransform
                    : null
                : versionRow != null
                    ? versionRow.transform as RectTransform
                    : null;
            if (lastRow == null || !lastRow.gameObject.activeSelf) return;

            float padding = state.IsGameMode ? 70f : 45f;
            for (int pass = 0; pass < 2; pass++)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
                Canvas.ForceUpdateCanvases();
                Bounds bounds =
                    RectTransformUtility.CalculateRelativeRectTransformBounds(
                        panel,
                        lastRow);
                float missing =
                    panel.rect.yMin - (bounds.min.y - padding);
                if (missing <= 0.5f) break;
                panel.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    panel.rect.height + missing);
            }
        }

        private void ToggleMusic()
        {
            bool value = !GameStateRuntime.Current.MusicOn;
            GameStateRuntime.Current.SetMusicOn(value);
            soundService?.RefreshBgm();
            musicToggle?.SetValue(value);
            ShowToast(
                value ? "SETTING_MUSIC_ON" : "SETTING_MUSIC_OFF",
                value ? "Music On" : "Music Off");
            TrackSwitch(TrackerCatalog.Switch.Music, value);
        }

        private void ToggleSound()
        {
            bool value = !GameStateRuntime.Current.SoundOn;
            GameStateRuntime.Current.SetSoundOn(value);
            soundToggle?.SetValue(value);
            ShowToast(
                value ? "SETTING_SOUND_ON" : "SETTING_SOUND_OFF",
                value ? "Sound On" : "Sound Off");
            TrackSwitch(TrackerCatalog.Switch.Sound, value);
        }

        private void ToggleVibration()
        {
            bool value = !GameStateRuntime.Current.VibrationOn;
            GameStateRuntime.Current.SetVibrationOn(value);
            vibrationToggle?.SetValue(value);
            ShowToast(
                value ? "SETTING_VIBRATION_ON" : "SETTING_VIBRATION_OFF",
                value ? "Vibration On" : "Vibration Off");
            if (value)
            {
                if (_onVibrationPreview != null)
                    _onVibrationPreview.Invoke();
                else
                    VibrationRuntime.Current.Play(VibrationLevel.Level3);
            }
            TrackSwitch(TrackerCatalog.Switch.Vibration, value);
        }

        private void TogglePeople()
        {
            bool value = !GameStateRuntime.Current.PeopleOn;
            GameStateRuntime.Current.SetPeopleOn(value);
            peopleToggle?.SetValue(value);
            ShowToast(
                value ? "SETTING_PEOPLE_ON" : "SETTING_PEOPLE_OFF",
                value ? "Voice On" : "Voice Off");
        }

        private void TogglePattern()
        {
            bool value = !GameStateRuntime.Current.PatternModeOn;
            GameStateRuntime.Current.SetPatternModeOn(value);
            SetPatternVisual(value);
            if (!GameStateRuntime.Current.PatternSwitchDotDismissed)
            {
                GameStateRuntime.Current.MarkPatternSwitchDotDismissed();
                if (patternDot != null) patternDot.SetActive(false);
            }
            ShowToast(
                value ? "SETTING_PATTERN_ON" : "SETTING_PATTERN_OFF",
                value ? "Pattern Mode On" : "Pattern Mode Off");
            Tracking?.TrackSwitchClick(
                TrackerCatalog.Switch.Pattern,
                value ? 1 : 0,
                TrackerCatalog.Dialog.Options);
            _onPatternChanged?.Invoke();
        }

        private void SetPatternVisual(bool value)
        {
            if (patternOn != null) patternOn.SetActive(value);
            if (patternOff != null) patternOff.SetActive(!value);
        }

        private void RestartGame()
        {
            if (_restartConsumed) return;
            _restartConsumed = true;
            TrackButton(TrackerCatalog.Button.Restart);
            _onRestart?.Invoke();
            Owner?.Hide(UiName.Setting);
        }

        private void OpenLanguage()
        {
            TrackButton(TrackerCatalog.Button.Language);
            Owner?.Show(UiName.Language);
        }

        private void ApplyLanguageAndClose(string locale)
        {
            if (string.IsNullOrWhiteSpace(locale)) return;
            TrackButton(TrackerCatalog.Button.LanguageConfirm);
            Tracking?.TrackUiLanguage(locale);
            localization?.SetLocale(locale);
            GameStateRuntime.Current.SetAppliedLocale(locale);
            Owner?.Hide(UiName.Setting);
        }

        private void OpenHowToPlay()
        {
            if (Owner == null || _waitingForHowToPlay) return;
            UIFrameWindow page = Owner.Show(UiName.HowToPlayPaged);
            HowToPlayPagedPagePresenter paged =
                page as HowToPlayPagedPagePresenter;
            if (paged == null) return;
            _waitingForHowToPlay = true;
            _howToPlayPage = paged;
            _howToPlayPage.Closed += HandleHowToPlayClosed;
            Owner.Events.WindowHidden += HandleWindowHidden;
            _skipNextCloseAnimation = true;
            _suppressNextCloseCallback = true;
            Owner.Hide(UiName.Setting);
        }

        private void HandleHowToPlayClosed()
        {
            if (!_waitingForHowToPlay) return;
            Action callback = _onClose;
            DetachHowToPlayWait();
            callback?.Invoke();
        }

        private void HandleWindowHidden(UiName name, UIFrameWindow _)
        {
            if (!_waitingForHowToPlay || name != UiName.HowToPlayPaged) return;
            DetachHowToPlayWait();
        }

        private void DetachHowToPlayWait()
        {
            if (_howToPlayPage != null)
                _howToPlayPage.Closed -= HandleHowToPlayClosed;
            if (_waitingForHowToPlay && Owner != null)
                Owner.Events.WindowHidden -= HandleWindowHidden;
            _waitingForHowToPlay = false;
            _howToPlayPage = null;
        }

        private void OpenFeedback()
        {
            TrackButton(TrackerCatalog.Button.Feedback);
            _externalServices.OpenLocalizedPrivacyUrl(
                PortfolioLinks.GitHub);
        }

        private void OpenCmp()
        {
            TrackButton(TrackerCatalog.Button.PrivacyPreference);
            if (_onCmp != null) _onCmp.Invoke();
            else _externalServices.ShowConsentManagement();
        }

        private void OpenTerms()
        {
            TrackButton(TrackerCatalog.Button.Terms);
            _externalServices.OpenLocalizedPrivacyUrl(
                PortfolioLinks.GitHub);
        }

        private void OpenPrivacy()
        {
            TrackButton(TrackerCatalog.Button.Privacy);
            _externalServices.OpenLocalizedPrivacyUrl(
                PortfolioLinks.GitHub);
        }

        private void HandleLanguageDropdownOpened()
        {
            TrackButton(TrackerCatalog.Button.Language);
            Tracking?.TrackDialogShown(
                TrackerCatalog.Dialog.LanguagePicker);
        }

        private void HandleLanguageDropdownClosed()
        {
            Tracking?.NotifyDialogClosed(
                TrackerCatalog.Dialog.LanguagePicker);
        }

        private void TrackButton(string name)
        {
            Tracking?.TrackButtonClick(name, GetTrackingDialogName());
        }

        private void TrackSwitch(string name, bool value)
        {
            Tracking?.TrackSwitchClick(
                name,
                value ? 1 : 0,
                GetTrackingDialogName());
        }

        private void ShowToast(string key, string fallback)
        {
            toast?.Show(Translate(key, fallback));
        }

        private string Translate(string key, string fallback)
        {
            if (localization == null) return fallback;
            string value = localization.Translate(key);
            return string.Equals(value, key, StringComparison.Ordinal)
                ? fallback
                : value;
        }

        private string ResolveSystemLocale()
        {
#if UNITY_INCLUDE_TESTS
            if (!string.IsNullOrWhiteSpace(_systemLocaleOverrideForTests))
                return LocalizationLocaleContract.NormalizeLocale(
                    _systemLocaleOverrideForTests);
#endif
            return LocalizationLocaleContract.ResolveCurrentSystemLocale();
        }

        private static void BindToggle(
            SettingsToggleView view,
            UnityEngine.Events.UnityAction action)
        {
            if (view?.Button != null) view.Button.onClick.AddListener(action);
        }

        private static void UnbindToggle(
            SettingsToggleView view,
            UnityEngine.Events.UnityAction action)
        {
            if (view?.Button != null) view.Button.onClick.RemoveListener(action);
        }

        private static void AddListener(
            Button button,
            UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.AddListener(action);
        }

        private static void RemoveListener(
            Button button,
            UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.RemoveListener(action);
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null) component.gameObject.SetActive(active);
        }

        private static void SetActive(Button button, bool active)
        {
            if (button != null) button.gameObject.SetActive(active);
        }

        private static T Parameter<T>(
            IReadOnlyDictionary<string, object> parameters,
            string key) where T : class
        {
            return parameters != null && parameters.TryGetValue(key, out object raw)
                ? raw as T
                : null;
        }

        private static bool Parameter(
            IReadOnlyDictionary<string, object> parameters,
            string key,
            bool fallback)
        {
            return parameters != null && parameters.TryGetValue(key, out object raw) &&
                   raw is bool value
                ? value
                : fallback;
        }
    }
}
