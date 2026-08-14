using Meowdoku.Core;

namespace Meowdoku.Gameplay
{
    public static class PortfolioFeatureUnlockPolicy
    {
        public const int PreviewUnlockTriggerLevel = 7;
        public const int PreviewUnlockTargetLevel = 21;

        public static bool Apply(GameStateService state)
        {
            if (state == null ||
                !state.TutorialDone ||
                state.CurrentLevel < PreviewUnlockTriggerLevel ||
                state.CurrentLevel >= PreviewUnlockTargetLevel)
                return false;

            state.SetCurrentLevel(PreviewUnlockTargetLevel);
            return true;
        }
    }
}
