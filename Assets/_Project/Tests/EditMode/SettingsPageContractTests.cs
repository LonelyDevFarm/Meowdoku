using Meowdoku.Core.Config;
using Meowdoku.Core.UI;
using Meowdoku.Gameplay;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class SettingsPageContractTests
    {
        [Test]
        public void OfflineOutgameLayout_MatchesSourceDefaults()
        {
            SettingsPresentationState state = SettingsPageContract.Resolve(
                false,
                "en_US");

            Assert.That(state.ShowMusic, Is.False);
            Assert.That(state.ShowSound, Is.True);
            Assert.That(state.ShowVibration, Is.True);
            Assert.That(state.ShowPeople, Is.True);
            Assert.That(state.VisibleToggleCount, Is.EqualTo(3));
            Assert.That(state.ToggleHorizontalSeparation, Is.EqualTo(30));
            Assert.That(state.ToggleScale, Is.EqualTo(1f));
            Assert.That(state.ShowLanguageButton, Is.False);
            Assert.That(state.ShowLanguageDropdown, Is.False);
            Assert.That(state.ShowPattern, Is.False);
            Assert.That(state.ShowFeedback, Is.True);
            Assert.That(state.ShowRestart, Is.False);
            Assert.That(state.ShowTerms, Is.True);
            Assert.That(state.ShowVersion, Is.True);
            Assert.That(state.BottomSpacerMinimum, Is.EqualTo(30f));
        }

        [Test]
        public void OfflineGameLayout_HidesOutgameRowsAndUsesGameSpacer()
        {
            SettingsPresentationState state = SettingsPageContract.Resolve(
                true,
                "en_US");

            Assert.That(state.ShowPattern, Is.False);
            Assert.That(state.ShowHowToPlay, Is.False);
            Assert.That(state.ShowRestart, Is.True);
            Assert.That(state.ShowTerms, Is.False);
            Assert.That(state.ShowVersion, Is.False);
            Assert.That(state.ShowCmp, Is.False);
            Assert.That(state.BottomSpacerMinimum, Is.EqualTo(90f));
        }

        [Test]
        public void PopupLanguage_RemainsVisibleForEnglishSystemLocale()
        {
            var language = new SettingsLanguageConfig();
            language.SetDebugOverride(SettingsLanguageConfig.ValuePopup);

            SettingsPresentationState state = SettingsPageContract.Resolve(
                false,
                "en_US",
                language: language);

            Assert.That(state.ShowLanguageButton, Is.True);
            Assert.That(state.ShowLanguageDropdown, Is.False);
        }

        [TestCase("en_US", false)]
        [TestCase("en-US", false)]
        [TestCase("ja_JP", true)]
        public void DropdownLanguage_UsesSourceEnglishSuppression(
            string locale,
            bool expectedVisible)
        {
            var language = new SettingsLanguageConfig();
            language.SetDebugOverride(SettingsLanguageConfig.ValueDropdown);

            SettingsPresentationState state = SettingsPageContract.Resolve(
                false,
                locale,
                language: language);

            Assert.That(state.ShowLanguageDropdown, Is.EqualTo(expectedVisible));
            Assert.That(state.ShowToggleContainer, Is.EqualTo(expectedVisible));
        }

        [Test]
        public void GameVariants_ShowPatternHowToPlayAndUnreadDot()
        {
            var blindMode = new BlindModConfig();
            var ruleText = new RuleTextConfig();
            blindMode.SetDebugOverride(BlindModConfig.ValueHideOnFilled);
            ruleText.SetDebugOverride(RuleTextConfig.ValueSettingEntry);

            SettingsPresentationState state = SettingsPageContract.Resolve(
                true,
                "vi_VN",
                tutorialDone: true,
                patternSwitchDotDismissed: false,
                blindMode: blindMode,
                ruleText: ruleText);

            Assert.That(state.ShowPattern, Is.True);
            Assert.That(state.ShowPatternDot, Is.True);
            Assert.That(state.ShowHowToPlay, Is.True);
            Assert.That(state.ShowRestart, Is.True);
            Assert.That(state.ShowToggleContainer, Is.True);
        }

        [Test]
        public void GenericPopupTiming_MatchesSourceAnimationResource()
        {
            Assert.That(SettingsPageContract.PopupMarkerSeconds,
                Is.EqualTo(0.3f));
            Assert.That(SettingsPageContract.PopupLengthSeconds,
                Is.EqualTo(0.6192876f));
            Assert.That(SettingsPageContract.PopupOpenOvershootSeconds,
                Is.EqualTo(0.09963459f));
            Assert.That(SettingsPageContract.PopupOpenFadeSeconds,
                Is.EqualTo(0.05483741f));
            Assert.That(SettingsPageContract.PopupCloseOvershootSeconds,
                Is.EqualTo(0.1492851f));
            Assert.That(SettingsPageContract.PopupCloseFadeStartSeconds,
                Is.EqualTo(0.2666667f));
        }

        [Test]
        public void ToastTimingAndPlacement_MatchSourceScript()
        {
            Assert.That(SourceToastView.MaximumWidth, Is.EqualTo(870f));
            Assert.That(SourceToastView.SourceTopY, Is.EqualTo(750f));
            Assert.That(SourceToastView.FloatDistance, Is.EqualTo(50f));
            Assert.That(SourceToastView.FadeInSeconds, Is.EqualTo(0.15f));
            Assert.That(SourceToastView.HoldSeconds, Is.EqualTo(1.2f));
            Assert.That(SourceToastView.FadeOutSeconds, Is.EqualTo(0.2f));
            Assert.That(SourceToastView.MoveSeconds, Is.EqualTo(1.55f));
        }
    }
}
