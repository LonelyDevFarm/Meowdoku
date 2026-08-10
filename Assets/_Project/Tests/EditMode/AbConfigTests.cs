using System.Linq;
using Meowdoku.Core.Config;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class AbConfigTests
    {
        [Test]
        public void DefaultProfile_ContainsAllPortedSourceConfigs()
        {
            Assert.That(DefaultConfigProfile.All.Count, Is.EqualTo(37));
            Assert.That(DefaultConfigProfile.All.Select(item => item.Key), Is.Unique);
            Assert.That(DefaultConfigProfile.All.Count(item => item.RegisteredBySource), Is.EqualTo(33));
        }

        [TestCase("region_color", 2, AbConfigTiming.AppStart, true)]
        [TestCase("size_cycle", 2, AbConfigTiming.GameStartNormal, true)]
        [TestCase("swipe_protect", 0, AbConfigTiming.GameStart, true)]
        [TestCase("doubletap_protect", 0, AbConfigTiming.AppStart, true)]
        [TestCase("tutorial_diagonal", 0, AbConfigTiming.AppStart, true)]
        [TestCase("guide_feedback", 0, AbConfigTiming.AppStart, true)]
        [TestCase("combo_voice", 6, AbConfigTiming.GameStart, true)]
        [TestCase("undo_btn", 0, AbConfigTiming.GameStart, false)]
        [TestCase("game_auto_mark", 0, AbConfigTiming.GameStart, false)]
        [TestCase("game_life_rule", 0, AbConfigTiming.AppStart, false)]
        [TestCase("wrong_cat_effect", 0, AbConfigTiming.GameStart, false)]
        [TestCase("reward_unlock_level", 0, AbConfigTiming.GameStart, true)]
        [TestCase("prop_highlight", 2, AbConfigTiming.GameStart, true)]
        [TestCase("mark_sound", 0, AbConfigTiming.AppStart, true)]
        [TestCase("rule_text", 0, AbConfigTiming.GameStart, true)]
        [TestCase("meow_feedback", 0, AbConfigTiming.AppStart, true)]
        [TestCase("thumb_up", 0, AbConfigTiming.GameStart, true)]
        [TestCase("daily_streak", 1, AbConfigTiming.AppStart, true)]
        [TestCase("leaderboard_func", 0, AbConfigTiming.AppStart, true)]
        [TestCase("hard_button", 0, AbConfigTiming.AppStart, true)]
        [TestCase("settings_language", 0, AbConfigTiming.OpenSetting, true)]
        [TestCase("blind_mod", 0, AbConfigTiming.GameStart, true)]
        public void DefaultProfile_MatchesSource(
            string key,
            int expectedDefault,
            string expectedTiming,
            bool expectedRegistration)
        {
            AbConfigDefinition definition = DefaultConfigProfile.Get(key);

            Assert.That(definition.DefaultValue, Is.EqualTo(expectedDefault));
            Assert.That(definition.Timing, Is.EqualTo(expectedTiming));
            Assert.That(definition.RegisteredBySource, Is.EqualTo(expectedRegistration));
        }

        [Test]
        public void BaseConfig_UsesDefaultUntilReloaded()
        {
            var config = new SwipeProtectConfig();

            Assert.That(config.Value, Is.EqualTo(SwipeProtectConfig.ValueControl));
            Assert.That(config.IsValueLoaded, Is.False);

            config.ReloadValue(new FixedIntProvider(SwipeProtectConfig.ValueHotzone20));

            Assert.That(config.Value, Is.EqualTo(SwipeProtectConfig.ValueHotzone20));
            Assert.That(config.IsValueLoaded, Is.True);
        }

        [Test]
        public void BaseConfig_DebugOverrideWinsAndCanBeCleared()
        {
            var config = new SwipeProtectConfig();
            config.ReloadValue(new FixedIntProvider(SwipeProtectConfig.ValueHotzone10));

            config.SetDebugOverride(SwipeProtectConfig.ValueDynamicIntent);
            Assert.That(config.Value, Is.EqualTo(SwipeProtectConfig.ValueDynamicIntent));

            config.ClearDebugOverride();
            Assert.That(config.Value, Is.EqualTo(SwipeProtectConfig.ValueHotzone10));
        }

        [TestCase(SwipeProtectConfig.ValueControl, false, 0.0, 4)]
        [TestCase(SwipeProtectConfig.ValueHotzone10, true, 0.1, 4)]
        [TestCase(SwipeProtectConfig.ValueHotzone20, true, 0.2, 4)]
        [TestCase(SwipeProtectConfig.ValueHotzone30, true, 0.3, 4)]
        [TestCase(SwipeProtectConfig.ValueHotzone40, true, 0.4, 4)]
        [TestCase(SwipeProtectConfig.ValueHotzone50, true, 0.5, 4)]
        [TestCase(SwipeProtectConfig.ValueHotzoneRaised, true, 0.4, 5)]
        [TestCase(SwipeProtectConfig.ValueDynamicIntent, true, 0.4, 4)]
        public void SwipeProtect_PolicyMatchesSource(
            int value,
            bool enabled,
            double tolerance,
            int thresholdAtSizeSeven)
        {
            Assert.That(SwipeProtectConfig.IsEnabledFor(value), Is.EqualTo(enabled));
            Assert.That(SwipeProtectConfig.TolerancePercentFor(value), Is.EqualTo(tolerance));
            Assert.That(SwipeProtectConfig.ThresholdForValue(value, 7), Is.EqualTo(thresholdAtSizeSeven));
        }

        [TestCase(DoubleTapProtectConfig.ValueControl, true, false, 0.35)]
        [TestCase(DoubleTapProtectConfig.ValueShorten, true, false, 0.25)]
        [TestCase(DoubleTapProtectConfig.ValueByTruth, true, false, 0.35)]
        [TestCase(DoubleTapProtectConfig.ValueByTruth, false, false, 0.25)]
        [TestCase(DoubleTapProtectConfig.ValueByConflict, false, true, 0.25)]
        [TestCase(DoubleTapProtectConfig.ValueByConflict, false, false, 0.35)]
        public void DoubleTapProtect_WindowMatchesSource(
            int value,
            bool truthHasCat,
            bool wouldConflict,
            double expectedSeconds)
        {
            var config = new DoubleTapProtectConfig();
            config.SetDebugOverride(value);

            Assert.That(
                config.WindowSeconds(truthHasCat, wouldConflict),
                Is.EqualTo(expectedSeconds));
        }

        [Test]
        public void RegionColorAndSizeCycle_UseSourceDefaults()
        {
            var regionColor = new RegionColorConfig();
            var sizeCycle = new SizeCycleConfig();

            Assert.That(regionColor.IsNewCellOnlyPalette(), Is.True);
            Assert.That(sizeCycle.IsCycleEnabled(), Is.False);
        }

        [Test]
        public void TutorialConfigs_UseOfflineSourceDefaultsAndVariants()
        {
            var diagonal = new TutorialDiagonalConfig();
            var feedback = new GuideFeedbackConfig();

            Assert.That(diagonal.IsDiagonalCopy(), Is.False);
            Assert.That(feedback.IsCheckGuide(), Is.False);
            Assert.That(feedback.IsIqGuide(), Is.False);

            diagonal.SetDebugOverride(TutorialDiagonalConfig.ValueDiagonalCopy);
            feedback.SetDebugOverride(GuideFeedbackConfig.ValueIq);

            Assert.That(diagonal.IsDiagonalCopy(), Is.True);
            Assert.That(feedback.IsCheckGuide(), Is.False);
            Assert.That(feedback.IsIqGuide(), Is.True);
        }

        [Test]
        public void HomeConfigs_UseOfflineSourceDefaults()
        {
            var daily = new DailyStreakConfig();
            var leaderboard = new LeaderboardFuncConfig();
            var hardButton = new HardButtonConfig();

            Assert.That(daily.Value, Is.EqualTo(DailyStreakConfig.ValueBasic));
            Assert.That(daily.IsEnabled(), Is.True);
            Assert.That(daily.IsChallengeOnly(), Is.False);
            Assert.That(daily.HasReward(), Is.True);
            Assert.That(daily.IsSkipLit(), Is.False);
            Assert.That(daily.HasPlayEntry(), Is.False);
            Assert.That(daily.IsSettleReorder(), Is.False);
            Assert.That(leaderboard.IsEnabled(), Is.False);
            Assert.That(leaderboard.GetGroup(), Is.EqualTo(LeaderboardFuncConfig.ValueControl));
            Assert.That(hardButton.EffectVariant(), Is.EqualTo(HardButtonConfig.ValueDefault));
        }

        [Test]
        public void DailyStreak_VariantsMatchCurrentSourcePolicies()
        {
            var daily = new DailyStreakConfig();

            daily.SetDebugOverride(DailyStreakConfig.ValueControl);
            Assert.That(daily.IsEnabled(), Is.False);
            Assert.That(daily.HasReward(), Is.False);

            daily.SetDebugOverride(DailyStreakConfig.ValueNoReward);
            Assert.That(daily.IsEnabled(), Is.True);
            Assert.That(daily.HasReward(), Is.True);

            daily.SetDebugOverride(DailyStreakConfig.ValueEntryReorder);
            Assert.That(daily.HasPlayEntry(), Is.True);
            Assert.That(daily.IsSettleReorder(), Is.True);
        }

        [Test]
        public void SettingsConfigs_UseOfflineSourceDefaultsAndVariants()
        {
            var language = new SettingsLanguageConfig();
            var blindMode = new BlindModConfig();
            var ruleText = new RuleTextConfig();

            Assert.That(language.IsLanguageSwitchEnabled(), Is.False);
            Assert.That(language.IsPopupMode(), Is.False);
            Assert.That(language.IsDropdownMode(), Is.False);
            Assert.That(blindMode.IsEnabled(), Is.False);
            Assert.That(blindMode.IsKeepOnFilled(), Is.False);
            Assert.That(ruleText.IsSettingEntry(), Is.False);

            language.SetDebugOverride(SettingsLanguageConfig.ValueDropdown);
            blindMode.SetDebugOverride(BlindModConfig.ValueKeepOnFilled);
            ruleText.SetDebugOverride(RuleTextConfig.ValueSettingEntry);

            Assert.That(language.IsLanguageSwitchEnabled(), Is.True);
            Assert.That(language.IsDropdownMode(), Is.True);
            Assert.That(blindMode.IsEnabled(), Is.True);
            Assert.That(blindMode.IsKeepOnFilled(), Is.True);
            Assert.That(ruleText.IsSettingEntry(), Is.True);
        }

        [TestCase(SingleRegionNumConfig.ValueDefault, 200, 5, -1)]
        [TestCase(SingleRegionNumConfig.ValueStrict, 20, 2, -1)]
        [TestCase(SingleRegionNumConfig.ValueStrict, 21, 2, 1)]
        [TestCase(SingleRegionNumConfig.ValueAllOne, 1, 1, 1)]
        [TestCase(SingleRegionNumConfig.ValueZero51, 51, 1, 1)]
        [TestCase(SingleRegionNumConfig.ValueZero51, 51, 2, 0)]
        [TestCase(SingleRegionNumConfig.ValueZero101, 100, 2, 1)]
        [TestCase(SingleRegionNumConfig.ValueZero101, 101, 2, 0)]
        public void SingleRegionPolicy_MatchesSourceThresholds(
            int value,
            int level,
            int rank,
            int expected)
        {
            var config = new SingleRegionNumConfig();
            config.SetDebugOverride(value);

            Assert.That(config.SingleLimitAt(level, rank), Is.EqualTo(expected));
            Assert.That(config.IsCoarseLimited(),
                Is.EqualTo(value != SingleRegionNumConfig.ValueDefault));
        }

        [Test]
        public void DdaAndDailyFirstConfigs_UseSourceControlDefaults()
        {
            var dda = new DdaRankConfig();
            var daily = new DailyFirstLevelDifficultyConfig();

            Assert.That(dda.Value, Is.EqualTo(DdaRankConfig.ValueControl));
            Assert.That(dda.IsRetryOnceDemote(), Is.False);
            Assert.That(dda.IsToolReviveDemote(), Is.False);
            Assert.That(dda.IsAnyActionDemote(), Is.False);
            Assert.That(daily.Value, Is.EqualTo(DailyFirstLevelDifficultyConfig.ValueControl));
            Assert.That(daily.IsEnabled(), Is.False);
        }

        [TestCase(PropHighlightConfig.ValueControl, "control", false, false)]
        [TestCase(PropHighlightConfig.ValueLocateOnce, "locate", true, false)]
        [TestCase(PropHighlightConfig.ValueHintOnce, "hint", true, false)]
        [TestCase(PropHighlightConfig.ValueNone, "none", false, false)]
        [TestCase(PropHighlightConfig.ValueControlRepeatable, "random", false, true)]
        public void PropHighlight_PolicyMatchesSource(
            int value,
            string target,
            bool once,
            bool repeatable)
        {
            var config = new PropHighlightConfig();
            config.SetDebugOverride(value);

            Assert.That(config.TargetProp(), Is.EqualTo(target));
            Assert.That(config.IsOncePerLifetime(), Is.EqualTo(once));
            Assert.That(config.IsRepeatable(), Is.EqualTo(repeatable));
        }

        [TestCase(0, 0, true)]
        [TestCase(5, 4, false)]
        [TestCase(5, 5, true)]
        public void RewardUnlockLevel_UsesInclusiveSourceThreshold(
            int unlockLevel,
            int currentLevel,
            bool expected)
        {
            var config = new RewardUnlockLevelConfig();
            config.SetDebugOverride(unlockLevel);

            Assert.That(config.IsRewardRequiredAt(currentLevel), Is.EqualTo(expected));
        }

        [Test]
        public void AudioFeedbackConfigs_UseSourceDefaultsAndPaths()
        {
            var combo = new ComboVoiceConfig();
            var meow = new MeowFeedbackConfig();
            var thumb = new ThumbUpConfig();

            Assert.That(combo.GetComboVoice(3),
                Is.EqualTo("res://assets/audio/sfx/combo_nice_s6.ogg"));
            Assert.That(combo.GetComboVoice(12),
                Is.EqualTo("res://assets/audio/sfx/combo_unbelievable_s6.ogg"));
            Assert.That(meow.IsEnabled(), Is.False);
            Assert.That(meow.GetMeowPath(1), Is.Empty);
            Assert.That(thumb.IsAnyFeedbackEnabled(), Is.False);
        }

        private sealed class FixedIntProvider : IAbValueProvider
        {
            private readonly int _value;

            public FixedIntProvider(int value)
            {
                _value = value;
            }

            public int GetInt(string key, int defaultValue) => _value;
            public string GetString(string key, string defaultValue) => defaultValue;
        }
    }
}
