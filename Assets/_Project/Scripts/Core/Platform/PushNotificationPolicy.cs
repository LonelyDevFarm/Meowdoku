using System;
using System.Collections.Generic;
using Meowdoku.Core.Config;

namespace Meowdoku.Core.Platform
{
    public static class PushGuidePolicy
    {
        public const int MinimumLevel = 20;
        public const int ThreeDayWinMinimum = 20;
        public const int SessionWinStreakMinimum = 5;
        public const int MaximumPopupCount = 5;

        public static bool IsEligible(
            int level,
            GameStateService gameState,
            PushPermissionConfig config)
        {
            if (gameState == null || config == null || level < MinimumLevel)
                return false;
            if (config.ShouldShowByThreeDayProgress())
            {
                if (gameState.GetRecentThreeDayWinCount() < ThreeDayWinMinimum)
                    return false;
            }
            else if (config.ShouldShowBySessionStreak())
            {
                if (gameState.SessionConsecutiveWins < SessionWinStreakMinimum)
                    return false;
            }
            else
                return false;

            return gameState.PushGuidePopupCount < MaximumPopupCount &&
                   gameState.IsPushGuideCooldownElapsed();
        }
    }

    public static class DailyLocalNotificationFactory
    {
        public const string NoonId = "daily_noon";
        public const string EveningId = "daily_evening";

        public static IReadOnlyList<DailyLocalNotification> Build(
            PushLocalTextConfig config,
            Func<string, string> translate,
            DateTime localNow,
            Random random = null)
        {
            config ??= new PushLocalTextConfig();
            translate ??= key => key;
            random ??= new Random();

            IReadOnlyList<LocalNotificationContent> noon;
            IReadOnlyList<LocalNotificationContent> evening;
            if (config.IsNewPool2())
            {
                noon = BuildShuffled(
                    "PUSH2_NOON_TITLE_",
                    "PUSH2_NOON_BODY_",
                    translate,
                    random);
                evening = BuildShuffled(
                    "PUSH2_EVE_TITLE_",
                    "PUSH2_EVE_BODY_",
                    translate,
                    random);
            }
            else if (config.IsNewPool())
            {
                noon = BuildShuffled(
                    "PUSH_NOON_TITLE_",
                    "PUSH_NOON_BODY_",
                    translate,
                    random);
                evening = BuildShuffled(
                    "PUSH_EVE_TITLE_",
                    "PUSH_EVE_BODY_",
                    translate,
                    random);
            }
            else
            {
                string title = translate("PUSH_TITLE");
                noon = BuildLegacy(title, 1, 4, translate);
                evening = BuildLegacy(title, 5, 8, translate);
            }

            return new[]
            {
                new DailyLocalNotification(NoonId, localNow, 12, 0, noon),
                new DailyLocalNotification(EveningId, localNow, 20, 0, evening)
            };
        }

        private static IReadOnlyList<LocalNotificationContent> BuildLegacy(
            string title,
            int first,
            int last,
            Func<string, string> translate)
        {
            var result = new List<LocalNotificationContent>(last - first + 1);
            for (int index = first; index <= last; index++)
                result.Add(new LocalNotificationContent(
                    title,
                    translate("PUSH_CONTENT_" + index)));
            return result;
        }

        private static IReadOnlyList<LocalNotificationContent> BuildShuffled(
            string titlePrefix,
            string bodyPrefix,
            Func<string, string> translate,
            Random random)
        {
            var pool = new List<LocalNotificationContent>(100);
            for (int index = 1; index <= 100; index++)
                pool.Add(new LocalNotificationContent(
                    translate(titlePrefix + index),
                    translate(bodyPrefix + index)));
            for (int index = pool.Count - 1; index > 0; index--)
            {
                int swap = random.Next(index + 1);
                (pool[index], pool[swap]) = (pool[swap], pool[index]);
            }
            return pool.GetRange(0, 5);
        }
    }
}
