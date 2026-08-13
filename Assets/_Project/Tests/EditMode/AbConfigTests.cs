using System;
using System.Collections.Generic;
using System.Linq;
using Meowdoku.Core;
using Meowdoku.Core.Config;
using Meowdoku.Gameplay;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class AbConfigTests
    {
        [Test]
        public void DefaultProfile_ContainsAllPortedSourceConfigs()
        {
            Assert.That(DefaultConfigProfile.All.Count, Is.EqualTo(63));
            Assert.That(DefaultConfigProfile.All.Select(item => item.Key), Is.Unique);
            Assert.That(
                DefaultConfigProfile.All.Count(item => item.RegisteredBySource),
                Is.EqualTo(57));
        }

        [TestCase("region_color", 2, AbConfigTiming.AppStart, true)]
        [TestCase("size_cycle", 2, AbConfigTiming.GameStartNormal, true)]
        [TestCase("normal_level_10", 0, AbConfigTiming.GameStartNormal, true)]
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
        [TestCase("daily_first_level_difficulty", 0, AbConfigTiming.AppStart, true)]
        [TestCase("daily_streak", 1, AbConfigTiming.AppStart, true)]
        [TestCase("leaderboard_func", 0, AbConfigTiming.AppStart, true)]
        [TestCase("hard_button", 0, AbConfigTiming.AppStart, true)]
        [TestCase("settings_language", 0, AbConfigTiming.OpenSetting, true)]
        [TestCase("blind_mod", 0, AbConfigTiming.GameStart, true)]
        [TestCase("revive_life", 0, AbConfigTiming.GameStart, true)]
        [TestCase("revive_free_logic", 0, AbConfigTiming.AppStart, true)]
        [TestCase("pass_page", 0, AbConfigTiming.GameStart, true)]
        [TestCase("pass_text", 0, AbConfigTiming.GameStart, true)]
        [TestCase("fail_text", 0, AbConfigTiming.GameEnd, true)]
        [TestCase("win_toast", 0, AbConfigTiming.GameStart, true)]
        [TestCase("dc_level", 0, AbConfigTiming.GameStartDaily, true)]
        [TestCase("no_dc", 0, AbConfigTiming.AppStart, false)]
        [TestCase("dc_tag_ui", 0, AbConfigTiming.AppStart, false)]
        [TestCase("att_dlg_logic", 0, AbConfigTiming.AppStart, true)]
        [TestCase("push_permission", 0, AbConfigTiming.GameEndNormal20, true)]
        [TestCase("push_local_text", 0, AbConfigTiming.AppStart, true)]
        [TestCase("rate_us_pop", 0, AbConfigTiming.GameStart, true)]
        [TestCase("rate_us_pop_ui", 0, AbConfigTiming.GameStart, true)]
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

        [Test]
        public void LivingDays_UsesSourceExclusiveLocalCalendarSegments()
        {
            const int biasMinutes = 420;
            long first = (100L * 86400L - biasMinutes * 60L + 3600L) * 1000L;
            long current = (102L * 86400L - biasMinutes * 60L) * 1000L;
            var config = new LivingDaysConfig();

            LivingDaysSegment segment = config.Resolve(
                first,
                current,
                biasMinutes);

            Assert.That(segment.DaysSinceFirstOpen, Is.EqualTo(2));
            Assert.That(segment.Index, Is.EqualTo(1));
            Assert.That(segment.Count, Is.EqualTo(4));
            Assert.That(
                LivingDaysConfig.DaysSinceFirstOpen(0, current, biasMinutes),
                Is.EqualTo(-1));
        }

        [Test]
        public void GameStartReload_DyesOnceAndFeedsLivingDaysAdSegments()
        {
            var provider = new RecordingRuntimeProvider
            {
                IsInitializedValue = true
            };
            provider.StringValues["living_days"] =
                "{0,2},{2,7},{7,14},{14,31},{31,inf}";
            provider.StringValues["inter_cd_lc"] =
                "{120},{100},{90},{80},{60}";
            var configs = new AdConfigSet();
            using var service = new AbConfigService(provider, configs.All);

            service.Initialize();
            Assert.That(service.IsAppStartFinalized, Is.True);
            Assert.That(provider.DyedKeys, Is.Empty);

            service.ReloadTiming(AbConfigTiming.GameStart);

            Assert.That(provider.DyedKeys.Count, Is.EqualTo(configs.All.Count));
            Assert.That(configs.LivingDays.SegmentCount, Is.EqualTo(5));
            Assert.That(
                configs.InterCooldown.GetSeconds(2, 5),
                Is.EqualTo(90));
            Assert.That(
                configs.InterCooldown.GetSeconds(2, 4),
                Is.EqualTo(120));
        }

        [Test]
        public void AppStartFinalization_ReloadsOnlyMatchingTimingOnce()
        {
            var provider = new RecordingRuntimeProvider
            {
                IsInitializedValue = true,
                IsRemoteReadyValue = true
            };
            provider.IntValues["daily_streak"] = DailyStreakConfig.ValueNoReward;
            provider.IntValues["swipe_protect"] =
                SwipeProtectConfig.ValueHotzone20;
            provider.IntValues["tutorial_diagonal"] =
                TutorialDiagonalConfig.ValueDiagonalCopy;
            provider.IntValues["guide_feedback"] =
                GuideFeedbackConfig.ValueIq;
            var appStart = new DailyStreakConfig();
            var gameStart = new SwipeProtectConfig();
            using var service = new AbConfigService(
                provider,
                new IAbConfig[] { appStart, gameStart });

            service.Initialize();
            provider.RaiseInitialized();
            provider.RaiseRemoteReady();

            Assert.That(appStart.Value,
                Is.EqualTo(DailyStreakConfig.ValueNoReward));
            Assert.That(gameStart.IsValueLoaded, Is.False);
            Assert.That(
                provider.DyedKeys.Count(key => key == "daily_streak"),
                Is.EqualTo(1));

            service.ReloadTiming(AbConfigTiming.GameStart);
            Assert.That(gameStart.Value,
                Is.EqualTo(SwipeProtectConfig.ValueHotzone20));
        }

        [Test]
        public void FirstOpenTime_PersistsSourceKeyAndCannotBeOverwritten()
        {
            var data = new GameStateData();
            var state = new GameStateService(data);
            state.EnsureFirstOpenTime(123456789L, 999L);
            state.EnsureFirstOpenTime(222L, 333L);

            Dictionary<string, object> document = data.ToPlayerDocument();
            GameStateData restored = GameStateData.FromDocuments(document, null);

            Assert.That(data.FirstOpenTimeMs, Is.EqualTo(123456789L));
            Assert.That(restored.FirstOpenTimeMs, Is.EqualTo(123456789L));
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
        public void BoardConfigSet_LoadsSharedValuesAtSourceTimings()
        {
            var provider = new RecordingRuntimeProvider
            {
                IsInitializedValue = true,
                IsRemoteReadyValue = true
            };
            provider.IntValues["region_color"] =
                RegionColorConfig.ValuePaletteV8;
            provider.IntValues["game_grid_ui"] =
                GameGridUiConfig.ValueSingleLine;
            provider.IntValues["board_size_big"] =
                BoardSizeBigConfig.ValueEnlarged;
            var configs = new BoardConfigSet();
            using var service = new AbConfigService(provider, configs.All);

            service.Initialize();

            Assert.That(configs.RegionColor.Value,
                Is.EqualTo(RegionColorConfig.ValuePaletteV8));
            Assert.That(configs.GameGridUi.Value,
                Is.EqualTo(GameGridUiConfig.ValueSingleLine));
            Assert.That(configs.BoardSizeBig.IsValueLoaded, Is.False);

            service.ReloadTiming(AbConfigTiming.GameStart);

            Assert.That(configs.BoardSizeBig.Value,
                Is.EqualTo(BoardSizeBigConfig.ValueEnlarged));
            Assert.That(provider.DyedKeys,
                Is.EqualTo(new[]
                {
                    "region_color",
                    "game_grid_ui",
                    "board_size_big"
                }));
        }

        [Test]
        public void InputConfigSet_LoadsAppAndGameStartValuesFromOneCatalog()
        {
            var provider = new RecordingRuntimeProvider
            {
                IsInitializedValue = true,
                IsRemoteReadyValue = true
            };
            provider.IntValues["doubletap_protect"] =
                DoubleTapProtectConfig.ValueShorten;
            provider.IntValues["swipe_protect"] =
                SwipeProtectConfig.ValueHotzone20;
            provider.IntValues["tutorial_diagonal"] =
                TutorialDiagonalConfig.ValueDiagonalCopy;
            provider.IntValues["guide_feedback"] =
                GuideFeedbackConfig.ValueIq;
            var configs = new InputConfigSet();
            using var service = new AbConfigService(provider, configs.All);

            service.Initialize();

            Assert.That(configs.DoubleTapProtect.Value,
                Is.EqualTo(DoubleTapProtectConfig.ValueShorten));
            Assert.That(configs.TutorialDiagonal.IsDiagonalCopy(), Is.True);
            Assert.That(configs.GuideFeedback.IsIqGuide(), Is.True);
            Assert.That(configs.SwipeProtect.IsValueLoaded, Is.False);

            service.ReloadTiming(AbConfigTiming.GameStart);

            Assert.That(configs.SwipeProtect.Value,
                Is.EqualTo(SwipeProtectConfig.ValueHotzone20));
            Assert.That(provider.DyedKeys,
                Is.EqualTo(new[]
                {
                    "doubletap_protect",
                    "tutorial_diagonal",
                    "guide_feedback",
                    "swipe_protect"
                }));
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
        public void HomeConfigSet_ReloadsAllAppStartFeatureFlagsTogether()
        {
            var provider = new RecordingRuntimeProvider
            {
                IsInitializedValue = true,
                IsRemoteReadyValue = true
            };
            provider.IntValues["daily_streak"] = DailyStreakConfig.ValueControl;
            provider.IntValues["leaderboard_func"] =
                LeaderboardFuncConfig.ValueFishProp;
            provider.IntValues["hard_button"] = HardButtonConfig.ValueRed2;
            var configs = new HomeConfigSet();
            using var service = new AbConfigService(provider, configs.All);

            service.Initialize();

            Assert.That(configs.DailyStreak.IsEnabled(), Is.False);
            Assert.That(configs.Leaderboard.IsEnabled(), Is.True);
            Assert.That(configs.Leaderboard.GetGroup(),
                Is.EqualTo(LeaderboardFuncConfig.ValueFishProp));
            Assert.That(configs.HardButton.EffectVariant(),
                Is.EqualTo(HardButtonConfig.ValueRed2));
            Assert.That(provider.DyedKeys,
                Is.EqualTo(new[]
                {
                    "daily_streak",
                    "leaderboard_func",
                    "hard_button"
                }));
        }

        [Test]
        public void ResultConfigs_UseOfflineSourceDefaultsAndVariants()
        {
            var passPage = new PassPageConfig();
            var passText = new PassTextConfig();
            var reviveLife = new ReviveLifeConfig();
            var reviveFree = new ReviveFreeLogicConfig();
            var failText = new FailTextConfig();

            Assert.That(passPage.IsG1(), Is.False);
            Assert.That(passPage.IsG2(), Is.False);
            Assert.That(passPage.IsG4(), Is.False);
            Assert.That(passText.ShouldShowBeatPercent(), Is.False);
            Assert.That(reviveLife.LivesToRestore(), Is.EqualTo(1));
            Assert.That(reviveFree.ShouldFreeRevive(1, false), Is.False);
            Assert.That(failText.ShouldShowEncourage(), Is.False);

            reviveLife.SetDebugOverride(ReviveLifeConfig.ValueGroup2);
            reviveFree.SetDebugOverride(
                ReviveFreeLogicConfig.ValueFirstEverOnce);
            Assert.That(reviveLife.LivesToRestore(), Is.EqualTo(3));
            Assert.That(reviveLife.IsTwoLineButton(), Is.True);
            Assert.That(reviveFree.ShouldFreeRevive(20, false), Is.True);
            Assert.That(reviveFree.ShouldFreeRevive(20, true), Is.False);
        }

        [Test]
        public void PassTextStats_MatchSourceP90CurvesAndRounding()
        {
            Assert.That(
                PassTextStatsContract.BeatPercent(44, 4),
                Is.EqualTo(51.0).Within(0.0001));
            Assert.That(
                PassTextStatsContract.BeatPercent(0, 4),
                Is.EqualTo(99.0).Within(0.0001));
            Assert.That(
                PassTextStatsContract.BeatPercentGroup2(0, 4),
                Is.EqualTo(99.0).Within(0.0001));
            Assert.That(
                PassTextStatsContract.RoundNonZeroDecimal(83.0),
                Is.EqualTo(83.1).Within(0.0001));
        }

        [Test]
        public void WinToast_DefaultIsOffAndTierCoverageMatchesSource()
        {
            var config = new WinToastConfig();
            Assert.That(config.IsEnabled(), Is.False);
            Assert.That(config.CoversTier(0), Is.False);

            config.SetDebugOverride(WinToastConfig.ValueP10);
            Assert.That(config.IsEnabled(), Is.True);
            Assert.That(config.CoversTier(0), Is.True);
            Assert.That(config.CoversTier(1), Is.True);
            Assert.That(config.CoversTier(2), Is.True);
            Assert.That(config.CoversTier(3), Is.False);
        }

        [TestCase(6, 6, WinToastTierContract.TierPerfect)]
        [TestCase(6, 16, WinToastTierContract.TierP5)]
        [TestCase(6, 20, WinToastTierContract.TierP10)]
        [TestCase(6, 30, WinToastTierContract.TierP20)]
        [TestCase(6, 31, WinToastTierContract.TierNone)]
        [TestCase(5, 5, WinToastTierContract.TierNone)]
        [TestCase(10, 29, WinToastTierContract.TierP5)]
        public void WinToastTier_MatchesSourceThresholds(
            int size,
            int steps,
            int expectedTier)
        {
            Assert.That(
                WinToastTierContract.DetermineTier(size, steps),
                Is.EqualTo(expectedTier));
        }

        [Test]
        public void WinToastMessageKey_UsesSourcePoolsAndPadding()
        {
            Assert.That(
                WinToastTierContract.MessageKey(
                    WinToastTierContract.TierPerfect,
                    8),
                Is.EqualTo("WIN_TOAST_PERFECT_09"));
            Assert.That(
                WinToastTierContract.MessageKey(
                    WinToastTierContract.TierP20,
                    7),
                Is.EqualTo("WIN_TOAST_P20_01"));
            Assert.That(
                WinToastTierContract.MessageKey(
                    WinToastTierContract.TierNone,
                    0),
                Is.Empty);
        }

        [Test]
        public void WinToastMessage_ConvertsSourceBbCodeWithoutDamagingUnityTags()
        {
            Assert.That(
                GameplayWinToastPresenter.ConvertGodotBbCode(
                    "[b]Top[/b] <color=#FF83FB>20%</color>"),
                Is.EqualTo("<b>Top</b> <color=#FF83FB>20%</color>"));
        }

        [Test]
        public void PassTextStrategy_SelectsSourceBranchesDeterministically()
        {
            var input = new PassTextStrategyInput
            {
                Level = 20,
                Size = 4,
                ElapsedSeconds = 44,
                IsHard = true
            };
            PassTextStrategySelection hard = PassTextStrategyContract.Select(
                PassTextConfig.ValueV2,
                input,
                4,
                0.0);
            Assert.That(hard.TitleKey, Is.EqualTo("WIN_V2_HARD_FIRST_TITLE_4"));
            Assert.That(hard.ShownPercent, Is.EqualTo(-1.0));

            input.IsHard = false;
            input.Level = 21;
            PassTextStrategySelection perfect = PassTextStrategyContract.Select(
                PassTextConfig.ValueV3G1,
                input,
                13,
                0.0);
            Assert.That(perfect.TitleKey, Is.EqualTo("WIN_V3_PERFECT_TITLE_13"));

            input.MistakeCount = 1;
            input.ElapsedSeconds = 0;
            input.LastWinBeatPercent = 80.0;
            PassTextStrategySelection improved = PassTextStrategyContract.Select(
                PassTextConfig.ValueV2,
                input,
                0,
                0.0);
            Assert.That(improved.TitleKey, Is.EqualTo("WIN_V2_AWESOME_TITLE"));
            Assert.That(improved.ShownPercent, Is.EqualTo(99.1).Within(0.0001));
            Assert.That(improved.DifferencePercent, Is.EqualTo(19.1).Within(0.0001));

            input.IsDaily = true;
            Assert.That(
                PassTextStrategyContract.Select(
                    PassTextConfig.ValueBeatPercent,
                    input),
                Is.SameAs(PassTextStrategySelection.Empty));
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

        [Test]
        public void SettingsConfigSet_ReloadsSourceOpenAndGameStartTimings()
        {
            var provider = new RecordingRuntimeProvider
            {
                IsInitializedValue = true
            };
            provider.IntValues["settings_language"] =
                SettingsLanguageConfig.ValuePopup;
            provider.IntValues["blind_mod"] =
                BlindModConfig.ValueKeepOnFilled;
            provider.IntValues["rule_text"] =
                RuleTextConfig.ValueSettingEntry;
            var configs = new SettingsConfigSet();
            using var service = new AbConfigService(provider, configs.All);

            service.Initialize();
            service.ReloadTiming(AbConfigTiming.OpenSetting);

            Assert.That(configs.Language.IsPopupMode(), Is.True);
            Assert.That(configs.BlindMode.IsValueLoaded, Is.False);
            Assert.That(configs.RuleText.IsValueLoaded, Is.False);
            Assert.That(provider.DyedKeys,
                Is.EqualTo(new[] { "settings_language" }));

            service.ReloadTiming(AbConfigTiming.GameStart);

            Assert.That(configs.BlindMode.IsKeepOnFilled(), Is.True);
            Assert.That(configs.RuleText.IsSettingEntry(), Is.True);
            Assert.That(provider.DyedKeys,
                Is.EqualTo(new[]
                {
                    "settings_language",
                    "blind_mod",
                    "rule_text"
                }));
        }

        [Test]
        public void PlatformConfigSet_ReloadsAppStartAndLevel20WinSeparately()
        {
            var provider = new RecordingRuntimeProvider
            {
                IsInitializedValue = true
            };
            provider.IntValues["att_dlg_logic"] =
                AttDialogLogicConfig.ValueRestyledGuide;
            provider.IntValues["push_local_text"] =
                PushLocalTextConfig.ValueNewPool2;
            provider.IntValues["push_permission"] =
                PushPermissionConfig.ValueSessionStreak;
            var configs = new PlatformConfigSet();
            using var service = new AbConfigService(provider, configs.All);

            service.Initialize();

            Assert.That(configs.AttDialogLogic.IsCustomGuideRestyled(), Is.True);
            Assert.That(configs.PushLocalText.IsNewPool2(), Is.True);
            Assert.That(configs.PushPermission.IsValueLoaded, Is.False);

            service.ReloadTiming(AbConfigTiming.GameEndNormal20);

            Assert.That(
                configs.PushPermission.ShouldShowBySessionStreak(),
                Is.True);
            Assert.That(provider.DyedKeys, Is.EqualTo(new[]
            {
                "att_dlg_logic",
                "push_local_text",
                "push_permission"
            }));
        }

        [Test]
        public void LevelSelectionConfigSet_ReloadsPreCatOnlyAtNormalLevel21Timing()
        {
            var provider = new RecordingRuntimeProvider
            {
                IsInitializedValue = true
            };
            provider.IntValues["size_cycle"] = SizeCycleConfig.ValueCycleV3A;
            provider.IntValues["pre_cat"] = PreCatConfig.ValueAlways;
            var configs = new LevelSelectionConfigSet();
            using var service = new AbConfigService(provider, configs.All);

            service.Initialize();
            service.ReloadTiming(AbConfigTiming.GameStartNormal);

            Assert.That(configs.SizeCycle.Value,
                Is.EqualTo(SizeCycleConfig.ValueCycleV3A));
            Assert.That(configs.PreCat.IsValueLoaded, Is.False);

            service.ReloadTiming(AbConfigTiming.GameStartNormal21);

            Assert.That(configs.PreCat.Value,
                Is.EqualTo(PreCatConfig.ValueAlways));
            Assert.That(provider.DyedKeys,
                Is.EqualTo(new[] { "size_cycle", "single_region_num", "normal_level_10", "pre_cat" }));
        }

        [Test]
        public void GameplayConfigSet_ReloadsSharedValuesAtSourceTimings()
        {
            var provider = new RecordingRuntimeProvider
            {
                IsInitializedValue = true
            };
            provider.IntValues["daily_first_level_difficulty"] =
                DailyFirstLevelDifficultyConfig.ValueReduceOne;
            provider.IntValues["mark_sound"] = MarkSoundConfig.ValueSoft2;
            provider.IntValues["dda_rank"] = DdaRankConfig.ValueAnyAction;
            provider.IntValues["reward_unlock_level"] = 8;
            provider.IntValues["prop_highlight"] =
                PropHighlightConfig.ValueControlRepeatable;
            provider.IntValues["rule_highlight"] =
                RuleHighlightConfig.ValueHighlightAllLevels;
            provider.IntValues["vibrate_combo"] =
                VibrateComboConfig.ValueWeakerToStrong;
            provider.IntValues["combo_voice"] =
                ComboVoiceConfig.ValueRealFemaleMeowText3;
            provider.IntValues["meow_feedback"] =
                MeowFeedbackConfig.ValueCrescendo;
            provider.IntValues["thumb_up"] =
                ThumbUpConfig.ValueLikeOnly;
            var configs = new GameplayConfigSet();
            using var service = new AbConfigService(provider, configs.All);

            service.Initialize();

            Assert.That(configs.DailyFirstLevelDifficulty.IsEnabled(), Is.True);
            Assert.That(configs.MarkSound.IsSoftVariant2(), Is.True);
            Assert.That(configs.MeowFeedback.Value,
                Is.EqualTo(MeowFeedbackConfig.ValueCrescendo));
            Assert.That(configs.DdaRank.IsValueLoaded, Is.False);
            Assert.That(configs.RuleHighlight.IsValueLoaded, Is.False);

            service.ReloadTiming(AbConfigTiming.GameStart);
            Assert.That(configs.RewardUnlockLevel.Value, Is.EqualTo(8));
            Assert.That(configs.PropHighlight.IsRepeatable(), Is.True);
            Assert.That(configs.RuleHighlight.IsAllLevels(), Is.True);
            Assert.That(configs.VibrateCombo.Value,
                Is.EqualTo(VibrateComboConfig.ValueWeakerToStrong));
            Assert.That(configs.ComboVoice.Value,
                Is.EqualTo(ComboVoiceConfig.ValueRealFemaleMeowText3));
            Assert.That(configs.ThumbUp.Value,
                Is.EqualTo(ThumbUpConfig.ValueLikeOnly));

            service.ReloadTiming(AbConfigTiming.GameStartNormal);
            Assert.That(configs.DdaRank.IsAnyActionDemote(), Is.True);
            Assert.That(provider.DyedKeys, Is.EqualTo(new[]
            {
                "daily_first_level_difficulty",
                "mark_sound",
                "meow_feedback",
                "reward_unlock_level",
                "prop_highlight",
                "rule_highlight",
                "vibrate_combo",
                "combo_voice",
                "thumb_up",
                "dda_rank"
            }));
        }

        [Test]
        public void ResultConfigSet_ReloadsAppGameAndFailTimingsSeparately()
        {
            var provider = new RecordingRuntimeProvider
            {
                IsInitializedValue = true
            };
            provider.IntValues["revive_free_logic"] =
                ReviveFreeLogicConfig.ValueFirstEverOnce;
            provider.IntValues["revive_life"] =
                ReviveLifeConfig.ValueGroup3;
            provider.IntValues["win_toast"] = WinToastConfig.ValueP20;
            provider.IntValues["pass_page"] = PassPageConfig.ValueG4;
            provider.IntValues["pass_text"] = PassTextConfig.ValueV3G3;
            provider.IntValues["fail_text"] =
                FailTextConfig.ValueRevivePromote;
            var configs = new ResultConfigSet();
            using var service = new AbConfigService(provider, configs.All);

            service.Initialize();
            Assert.That(configs.ReviveFreeLogic.Value,
                Is.EqualTo(ReviveFreeLogicConfig.ValueFirstEverOnce));
            Assert.That(configs.ReviveLife.IsValueLoaded, Is.False);
            Assert.That(configs.FailText.IsValueLoaded, Is.False);

            service.ReloadTiming(AbConfigTiming.GameStart);
            Assert.That(configs.ReviveLife.IsAlternateButtonText(), Is.True);
            Assert.That(configs.WinToast.Value, Is.EqualTo(WinToastConfig.ValueP20));
            Assert.That(configs.PassPage.Value, Is.EqualTo(PassPageConfig.ValueG4));
            Assert.That(configs.PassText.Value, Is.EqualTo(PassTextConfig.ValueV3G3));
            Assert.That(configs.FailText.IsValueLoaded, Is.False);

            service.ReloadTiming(AbConfigTiming.GameEnd);
            Assert.That(configs.FailText.ShouldShowRevivePromote(), Is.True);
            Assert.That(provider.DyedKeys, Is.EqualTo(new[]
            {
                "revive_free_logic",
                "pass_text",
                "revive_life",
                "win_toast",
                "pass_page",
                "fail_text"
            }));
        }

        [TestCase(RuleHighlightConfig.ValueControl, false, 1, false)]
        [TestCase(RuleHighlightConfig.ValueHighlightViolated, false, 1, false)]
        [TestCase(RuleHighlightConfig.ValueHighlightViolated, true, 5, true)]
        [TestCase(RuleHighlightConfig.ValueHighlightViolated, true, 6, false)]
        [TestCase(RuleHighlightConfig.ValueHighlightAllLevels, false, 100, true)]
        public void RuleHighlight_PolicyMatchesSource(
            int value,
            bool tutorialDone,
            int level,
            bool expected)
        {
            var config = new RuleHighlightConfig();
            config.SetDebugOverride(value);

            Assert.That(
                config.ShouldHighlight(tutorialDone, level),
                Is.EqualTo(expected));
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

        private sealed class RecordingRuntimeProvider : IAbRuntimeProvider
        {
            public readonly Dictionary<string, int> IntValues = new();
            public readonly Dictionary<string, string> StringValues = new();
            public readonly List<string> DyedKeys = new();

            public event Action Initialized;
            public event Action RemoteReady;
            public event Action<string> ParamsUpdated;
            public bool IsInitializedValue;
            public bool IsRemoteReadyValue;
            public bool IsInitialized => IsInitializedValue;
            public bool IsRemoteReady => IsRemoteReadyValue;
            public long FirstOpenUnixMilliseconds { get; set; }

            public int GetInt(string key, int defaultValue) =>
                IntValues.TryGetValue(key, out int value) ? value : defaultValue;

            public string GetString(string key, string defaultValue) =>
                StringValues.TryGetValue(key, out string value)
                    ? value
                    : defaultValue;

            public void Dye(string key)
            {
                DyedKeys.Add(key);
            }

            public void RaiseInitialized()
            {
                IsInitializedValue = true;
                Initialized?.Invoke();
            }

            public void RaiseRemoteReady()
            {
                IsRemoteReadyValue = true;
                RemoteReady?.Invoke();
            }

            public void RaiseParamsUpdated(string updateType)
            {
                ParamsUpdated?.Invoke(updateType);
            }
        }
    }
}
