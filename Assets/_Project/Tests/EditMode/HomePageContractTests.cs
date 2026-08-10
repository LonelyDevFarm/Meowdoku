using Meowdoku.Core.Config;
using Meowdoku.Core.UI;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class HomePageContractTests
    {
        [Test]
        public void DefaultPresentation_MatchesOfflineSourceConfiguration()
        {
            HomePresentationState state = HomePageContract.Resolve(1);

            Assert.That(state.Level, Is.EqualTo(1));
            Assert.That(state.EnglishLevelTitle, Is.EqualTo("Level 1"));
            Assert.That(state.IsHardLevel, Is.False);
            Assert.That(state.ShowDailyStreak, Is.True);
            Assert.That(state.ShowProfile, Is.False);
            Assert.That(state.HardEffectVariant, Is.EqualTo(HardButtonConfig.ValueDefault));
        }

        [Test]
        public void Presentation_UsesSourceHardLevelAndAbVariants()
        {
            var daily = new DailyStreakConfig();
            var leaderboard = new LeaderboardFuncConfig();
            var hardButton = new HardButtonConfig();
            daily.SetDebugOverride(DailyStreakConfig.ValueControl);
            leaderboard.SetDebugOverride(LeaderboardFuncConfig.ValueCatsProp);
            hardButton.SetDebugOverride(HardButtonConfig.ValueRed2);

            HomePresentationState state = HomePageContract.Resolve(
                30,
                daily,
                leaderboard,
                hardButton);

            Assert.That(state.IsHardLevel, Is.True);
            Assert.That(state.ShowDailyStreak, Is.False);
            Assert.That(state.ShowProfile, Is.True);
            Assert.That(state.HardEffectVariant, Is.EqualTo(HardButtonConfig.ValueRed2));
        }

        [Test]
        public void TransitionTiming_MatchesMainInterfaceAnimationMarkers()
        {
            Assert.That(HomePageContract.GamePageShowDelaySeconds,
                Is.EqualTo(0.000014f).Within(0.000002f));
            Assert.That(HomePageContract.HomeHideDelaySeconds,
                Is.EqualTo(0.10336f).Within(0.00001f));
            Assert.That(HomePageContract.RewardRestoreDelaySeconds, Is.EqualTo(0.4f));
            Assert.That(HomePageContract.StartRevealDurationSeconds,
                Is.EqualTo(0.36320632f).Within(0.000001f));
            Assert.That(HomePageContract.SettingsRevealDurationSeconds,
                Is.EqualTo(0.27336887f).Within(0.000001f));
            Assert.That(HomePageContract.ExitUiFadeDurationSeconds,
                Is.EqualTo(0.01664907f).Within(0.000001f));
            Assert.That(HomePageContract.LogoExitUnityYOffset,
                Is.EqualTo(70.765f).Within(0.0001f));
        }
    }
}
