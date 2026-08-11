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
    public sealed class SettingsPagePresenter : UIFrameWindow
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

        private Action _onRestart;
        private Action _onPatternChanged;
        private Action _onClose;
        private Action _onFeedback;
        private Action _onCmp;
        private Action _onVibrationPreview;
        private bool _isGameMode;
        private bool _restartConsumed;
        private bool _skipNextCloseAnimation;
        private bool _suppressNextCloseCallback;
        private bool _waitingForHowToPlay;

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
            _onFeedback = Parameter<Action>(parameters, "on_feedback");
            _onCmp = Parameter<Action>(parameters, "on_cmp");
            _onVibrationPreview = Parameter<Action>(parameters, "on_vibration_preview");
            _restartConsumed = false;
            _skipNextCloseAnimation = false;
            _suppressNextCloseCallback = false;

            localization?.ApplySystemLocale(
                GameStateRuntime.Current,
                _languageConfig.IsLanguageSwitchEnabledPeek());

            SettingsPresentationState state = SettingsPageContract.Resolve(
                _isGameMode,
                LocalizationLocaleContract.ResolveCurrentSystemLocale(),
                GameStateRuntime.Current.TutorialDone,
                GameStateRuntime.Current.PatternSwitchDotDismissed,
                cmpRequired,
                _languageConfig,
                _blindModeConfig,
                _ruleTextConfig);
            ApplyLayout(state);
            if (state.ShowLanguageDropdown)
            {
                languageSwitchWidget?.Setup(
                    LocalizationLocaleContract.ResolveCurrentSystemLocale());
            }
            RefreshToggleValues();
            RefreshStaticText();
            RebuildAndCenterPanel();
            popupAnimator?.PlayOpen();
            soundService?.Play(SoundKind.DialogOpen);
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

        private void ApplyLayout(SettingsPresentationState state)
        {
            SetActive(musicToggle, state.ShowMusic);
            SetActive(soundToggle, state.ShowSound);
            SetActive(vibrationToggle, state.ShowVibration);
            SetActive(peopleToggle, state.ShowPeople);
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

        private void RebuildAndCenterPanel()
        {
            if (panel == null) return;
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
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
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
            if (value) soundService?.Play(SoundKind.ButtonClick);
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
                else if (Application.isMobilePlatform)
                    Handheld.Vibrate();
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
            if (page == null) return;
            _waitingForHowToPlay = true;
            Owner.Events.WindowHidden += HandleWindowHidden;
            _skipNextCloseAnimation = true;
            _suppressNextCloseCallback = true;
            Owner.Hide(UiName.Setting);
        }

        private void HandleWindowHidden(UiName name, UIFrameWindow _)
        {
            if (!_waitingForHowToPlay || name != UiName.HowToPlayPaged) return;
            DetachHowToPlayWait();
            _onClose?.Invoke();
        }

        private void DetachHowToPlayWait()
        {
            if (_waitingForHowToPlay && Owner != null)
                Owner.Events.WindowHidden -= HandleWindowHidden;
            _waitingForHowToPlay = false;
        }

        private void OpenFeedback()
        {
            TrackButton(TrackerCatalog.Button.Feedback);
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                ShowToast("NETWORK_ERROR", "Please check your network connection.");
                return;
            }
            _onFeedback?.Invoke();
        }

        private void OpenCmp()
        {
            TrackButton(TrackerCatalog.Button.PrivacyPreference);
            _onCmp?.Invoke();
        }

        private void OpenTerms()
        {
            TrackButton(TrackerCatalog.Button.Terms);
            Application.OpenURL("https://oakevergames.com/tos.html");
        }

        private void OpenPrivacy()
        {
            TrackButton(TrackerCatalog.Button.Privacy);
            Application.OpenURL("https://oakevergames.com/pp.html");
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
