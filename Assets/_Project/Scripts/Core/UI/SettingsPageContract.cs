using System;
using Meowdoku.Core.Config;

namespace Meowdoku.Core.UI
{
    public readonly struct SettingsPresentationState
    {
        public SettingsPresentationState(
            bool isGameMode,
            bool showLanguageButton,
            bool showLanguageDropdown,
            bool showPattern,
            bool showPatternDot,
            bool showHowToPlay,
            bool showRestart,
            bool showCmp,
            bool showTerms,
            bool showVersion)
        {
            IsGameMode = isGameMode;
            ShowLanguageButton = showLanguageButton;
            ShowLanguageDropdown = showLanguageDropdown;
            ShowPattern = showPattern;
            ShowPatternDot = showPatternDot;
            ShowHowToPlay = showHowToPlay;
            ShowRestart = showRestart;
            ShowCmp = showCmp;
            ShowTerms = showTerms;
            ShowVersion = showVersion;
        }

        public bool IsGameMode { get; }
        public bool ShowMusic => false;
        public bool ShowSound => true;
        public bool ShowVibration => true;
        public bool ShowPeople => true;
        public int VisibleToggleCount => 3;
        public int ToggleHorizontalSeparation => 30;
        public float ToggleScale => 1f;
        public bool ShowLanguageButton { get; }
        public bool ShowLanguageDropdown { get; }
        public bool ShowToggleContainer => ShowLanguageDropdown || ShowPattern;
        public bool ShowPattern { get; }
        public bool ShowPatternDot { get; }
        public bool ShowHowToPlay { get; }
        public bool ShowRestart { get; }
        public bool ShowFeedback => true;
        public bool ShowCmp { get; }
        public bool ShowTerms { get; }
        public bool ShowVersion { get; }
        public float BottomSpacerMinimum => IsGameMode ? 90f : 30f;
    }

    public static class SettingsPageContract
    {
        public const float SourceReferenceWidth = 1080f;
        public const float PanelWidth = 900f;
        public const float TitleBarHeight = 130f;
        public const float ToggleButtonSize = 250f;
        public const float MainButtonWidth = 750f;
        public const float MainButtonHeight = 160f;

        // assets/animation/GenericPopup.res (marker "Mark" starts close track).
        public const float PopupMarkerSeconds = 0.3f;
        public const float PopupLengthSeconds = 0.6192876f;
        public const float PopupOpenOvershootSeconds = 0.09963459f;
        public const float PopupOpenFadeSeconds = 0.05483741f;
        public const float PopupCloseOvershootSeconds = 0.1492851f;
        public const float PopupCloseFadeStartSeconds = 0.2666667f;

        public static SettingsPresentationState Resolve(
            bool isGameMode,
            string systemLocale,
            bool tutorialDone = false,
            bool patternSwitchDotDismissed = false,
            bool cmpRequired = false,
            SettingsLanguageConfig language = null,
            BlindModConfig blindMode = null,
            RuleTextConfig ruleText = null)
        {
            language ??= new SettingsLanguageConfig();
            blindMode ??= new BlindModConfig();
            ruleText ??= new RuleTextConfig();

            bool dropdownMode = language.IsDropdownMode();
            bool showLanguage = !isGameMode &&
                                language.IsLanguageSwitchEnabled();
            if (showLanguage && dropdownMode &&
                string.Equals(MainLocale(systemLocale), "en",
                    StringComparison.OrdinalIgnoreCase))
                showLanguage = false;

            bool showPattern = isGameMode && blindMode.IsEnabled();
            return new SettingsPresentationState(
                isGameMode,
                showLanguage && !dropdownMode,
                showLanguage && dropdownMode,
                showPattern,
                showPattern && tutorialDone && !patternSwitchDotDismissed,
                isGameMode && ruleText.IsSettingEntry(),
                isGameMode,
                !isGameMode && cmpRequired,
                !isGameMode,
                !isGameMode);
        }

        private static string MainLocale(string locale)
        {
            if (string.IsNullOrWhiteSpace(locale)) return string.Empty;
            int separator = locale.IndexOfAny(new[] { '_', '-' });
            return separator > 0 ? locale.Substring(0, separator) : locale;
        }
    }
}
