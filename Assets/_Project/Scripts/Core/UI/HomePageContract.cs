using Meowdoku.Core.Config;

namespace Meowdoku.Core.UI
{
    public readonly struct HomePresentationState
    {
        public HomePresentationState(
            int level,
            bool isHardLevel,
            bool showDailyStreak,
            bool showProfile,
            int hardEffectVariant)
        {
            Level = level;
            IsHardLevel = isHardLevel;
            ShowDailyStreak = showDailyStreak;
            ShowProfile = showProfile;
            HardEffectVariant = hardEffectVariant;
        }

        public int Level { get; }
        public bool IsHardLevel { get; }
        public bool ShowDailyStreak { get; }
        public bool ShowProfile { get; }
        public int HardEffectVariant { get; }

        // GAME_LEVEL_TITLE in the English source localization is "Level %d".
        // The presenter will replace this fallback through the localization service.
        public string EnglishLevelTitle => $"Level {Level}";
    }

    public static class HomePageContract
    {
        public const float SourceReferenceWidth = 1080f;
        public const float StartButtonWidth = 750f;
        public const float StartButtonHeight = 160f;

        public const float MainInterfaceDurationSeconds = 0.93333334f;
        public const float DisappearMarkerSeconds = 0.8299737f;
        public const float EntryMarkerSeconds = 0.8299877f;
        public const float RewardRestoreDelaySeconds = 0.4f;

        public const float GridRevealSeconds = 0.04075222f;
        public const float StartRevealDelaySeconds = 0.16433388f;
        public const float StartRevealEndSeconds = 0.5275402f;
        public const float SettingsRevealDelaySeconds = 0.1774595f;
        public const float SettingsRevealEndSeconds = 0.45082837f;
        public const float LogoRevealSeconds = 0.23333335f;
        public const float LogoScaleEndSeconds = 0.8300394f;
        public const float ExitUiFadeEndSeconds = 0.84662277f;
        public const float LogoExitFadeEndSeconds = 0.932f;
        public const float LogoExitScaleEndSeconds = 0.9326443f;
        public const float LogoAppearScaleRatio = 0.96f / 0.88f;
        public const float LogoExitScaleRatio = 0.634f / 0.88f;
        public const float LogoExitUnityYOffset = 241f - 170.235f;

        public static float GamePageShowDelaySeconds =>
            EntryMarkerSeconds - DisappearMarkerSeconds;

        public static float HomeHideDelaySeconds =>
            MainInterfaceDurationSeconds - DisappearMarkerSeconds;

        public static float StartRevealDurationSeconds =>
            StartRevealEndSeconds - StartRevealDelaySeconds;

        public static float SettingsRevealDurationSeconds =>
            SettingsRevealEndSeconds - SettingsRevealDelaySeconds;

        public static float ExitUiFadeDurationSeconds =>
            ExitUiFadeEndSeconds - DisappearMarkerSeconds;

        public static float LogoExitFadeDurationSeconds =>
            LogoExitFadeEndSeconds - DisappearMarkerSeconds;

        public static float LogoExitScaleDurationSeconds =>
            LogoExitScaleEndSeconds - DisappearMarkerSeconds;

        public static HomePresentationState Resolve(
            int currentLevel,
            DailyStreakConfig dailyStreak = null,
            LeaderboardFuncConfig leaderboard = null,
            HardButtonConfig hardButton = null)
        {
            dailyStreak ??= new DailyStreakConfig();
            leaderboard ??= new LeaderboardFuncConfig();
            hardButton ??= new HardButtonConfig();

            return new HomePresentationState(
                currentLevel,
                LevelData.IsHardLevel(currentLevel),
                dailyStreak.IsEnabled(),
                leaderboard.IsEnabled(),
                hardButton.EffectVariant());
        }
    }
}
