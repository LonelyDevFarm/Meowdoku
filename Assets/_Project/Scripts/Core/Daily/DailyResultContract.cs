using System;
using System.Globalization;

namespace Meowdoku.Core.Daily
{
    public static class DailyResultContract
    {
        public const float AppearDelaySeconds = 0.8f;
        public const float ToastDurationSeconds = 1.5f;
        public const float InputBlockSeconds = 2f;

        public static float PageShowDelaySeconds(bool toastWasShown)
        {
            return toastWasShown ? ToastDurationSeconds : 0f;
        }

        public static float ResultAnimationDelaySeconds(bool toastWasShown)
        {
            return toastWasShown ? 0f : AppearDelaySeconds;
        }

        public static string FormatElapsedSeconds(float elapsedSeconds)
        {
            int total = Math.Max(0, (int)Math.Floor(elapsedSeconds));
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:00}:{1:00}",
                total / 60,
                total % 60);
        }

        public static string FormatBeatPercent(float beatPercent)
        {
            return beatPercent.ToString(
                       "0.0",
                       CultureInfo.InvariantCulture) + "%";
        }
    }
}
