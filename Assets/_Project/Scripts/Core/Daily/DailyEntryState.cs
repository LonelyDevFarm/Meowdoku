using System;
using System.Globalization;
using Meowdoku.Core.UI;

namespace Meowdoku.Core.Daily
{
    public enum DailyEntryState
    {
        Locked,
        Normal,
        Done
    }

    public static class DailyEntryStateContract
    {
        public const int UnlockLevel = 21;

        public static DailyEntryState Compute(
            int currentLevel,
            string today,
            string completedDate,
            string maxDailyDate)
        {
            if (currentLevel < UnlockLevel) return DailyEntryState.Locked;
            today ??= string.Empty;
            completedDate ??= string.Empty;
            maxDailyDate ??= string.Empty;
            return completedDate == today ||
                   string.CompareOrdinal(today, maxDailyDate) < 0
                ? DailyEntryState.Done
                : DailyEntryState.Normal;
        }

        public static string DateKey(DateTime localDate)
        {
            return ClockTickerContract.LocalDateKey(localDate);
        }

        public static string MonthLocalizationKey(int month)
        {
            int clamped = Math.Max(1, Math.Min(12, month));
            return "MONTH_ABBR_" + clamped.ToString(CultureInfo.InvariantCulture);
        }

        public static string TodayDateText(string translatedMonth, int day)
        {
            return (translatedMonth ?? string.Empty) + " " +
                   day.ToString(CultureInfo.InvariantCulture);
        }

        public static string CountdownText(DateTime localDateTime)
        {
            int remaining = 86400 -
                            localDateTime.Hour * 3600 -
                            localDateTime.Minute * 60 -
                            localDateTime.Second;
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:00}:{1:00}:{2:00}",
                remaining / 3600,
                remaining % 3600 / 60,
                remaining % 60);
        }

        public static string DoneTimeText(int elapsedSeconds)
        {
            int seconds = Math.Max(0, elapsedSeconds);
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:00}:{1:00}",
                seconds / 60,
                seconds % 60);
        }

        public static float DoneTopPercent(float beatPercent)
        {
            return (float)Math.Round(
                (100f - beatPercent) * 10f,
                MidpointRounding.AwayFromZero) / 10f;
        }

        public static int DoneTopPercentDecimals(float topPercent)
        {
            return Math.Abs(topPercent - MathF.Round(topPercent)) < 0.00001f
                ? 0
                : 1;
        }
    }
}
