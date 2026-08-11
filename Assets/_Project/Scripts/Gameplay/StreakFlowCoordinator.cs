using System.Collections;
using System.Collections.Generic;
using Meowdoku.Core.Daily;
using Meowdoku.Core.UI;

namespace Meowdoku.Gameplay
{
    public static class StreakFlowCoordinator
    {
        public static bool HasPendingFlow(DailyMetaRuntime runtime)
        {
            return runtime != null &&
                   (runtime.Streak.HasPendingReviveDecision ||
                    runtime.Streak.HasPendingShow);
        }

        public static IEnumerator RunBeforeResult(
            UIManager owner,
            DailyMetaRuntime runtime)
        {
            if (owner == null || runtime == null ||
                runtime.Streak.IsSettleReorder)
                yield break;
            yield return RunPending(owner, runtime.Streak);
        }

        public static IEnumerator RunAfterResult(
            UIManager owner,
            DailyMetaRuntime runtime)
        {
            if (owner == null || runtime == null ||
                !runtime.Streak.IsSettleReorder)
                yield break;
            yield return RunPending(owner, runtime.Streak);
        }

        private static IEnumerator RunPending(
            UIManager owner,
            StreakFeature streak)
        {
            if (streak.HasPendingReviveDecision)
            {
                StreakReviveInfo info = streak.GetReviveInfo();
                UiName pageName = info.IsResume
                    ? UiName.StreakResume
                    : UiName.StreakBackfill;
                var parameters = new Dictionary<string, object>
                {
                    ["from_streak"] = info.BrokenStreak,
                    ["to_streak"] = streak.DisplayStreak,
                    ["info_days"] = info.BrokenStreak
                };
                UIFrameWindow page = owner.Show(pageName, parameters);
                if (page == null)
                    streak.GiveUpRevive();
                else
                    yield return owner.AwaitHidden(pageName);
            }

            if (!streak.HasPendingShow) yield break;
            StreakDisplayState state =
                streak.Data.CurrentStreak == 1 &&
                !streak.ShouldSkipLit
                    ? StreakDisplayState.Lit
                    : StreakDisplayState.Settle;
            UIFrameWindow streakPage = owner.Show(
                UiName.Streak,
                new Dictionary<string, object>
                {
                    [StreakPagePresenter.StateParameter] = (int)state
                });
            if (streakPage != null)
                yield return owner.AwaitHidden(UiName.Streak);
        }
    }
}
