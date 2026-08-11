using System.Collections.Generic;

namespace Meowdoku.Core.Config
{
    public readonly struct LivingDaysSegment
    {
        public LivingDaysSegment(int index, int count, int daysSinceFirstOpen)
        {
            Index = index;
            Count = count;
            DaysSinceFirstOpen = daysSinceFirstOpen;
        }

        public int Index { get; }
        public int Count { get; }
        public int DaysSinceFirstOpen { get; }
    }

    /// <summary>
    /// Port of living_days_config.gd. Segment upper bounds are exclusive and
    /// an upper bound of -1 represents the source "inf" token.
    /// </summary>
    public sealed class LivingDaysConfig : AbConfigBase<string>
    {
        public const string DefaultSegments = "{0,2},{2,4},{4,7},{7,inf}";

        public LivingDaysConfig()
            : base("living_days", DefaultSegments, AbConfigTiming.GameStart) { }

        public int SegmentCount => ParseSegments(Value).Count;

        public LivingDaysSegment Resolve(
            long firstOpenUnixMilliseconds,
            long nowUnixMilliseconds,
            int localUtcBiasMinutes)
        {
            List<(int Lower, int Upper)> segments = ParseSegments(Value);
            int days = DaysSinceFirstOpen(
                firstOpenUnixMilliseconds,
                nowUnixMilliseconds,
                localUtcBiasMinutes);
            int index = -1;
            if (days >= 0)
            {
                for (int i = 0; i < segments.Count; i++)
                {
                    (int lower, int upper) = segments[i];
                    if (days >= lower && (upper < 0 || days < upper))
                    {
                        index = i;
                        break;
                    }
                }
            }
            return new LivingDaysSegment(index, segments.Count, days);
        }

        public static int DaysSinceFirstOpen(
            long firstOpenUnixMilliseconds,
            long nowUnixMilliseconds,
            int localUtcBiasMinutes)
        {
            if (firstOpenUnixMilliseconds <= 0 || nowUnixMilliseconds <= 0)
                return -1;
            long biasSeconds = localUtcBiasMinutes * 60L;
            long firstLocalDay =
                (firstOpenUnixMilliseconds / 1000L + biasSeconds) / 86400L;
            long currentLocalDay =
                (nowUnixMilliseconds / 1000L + biasSeconds) / 86400L;
            long result = currentLocalDay - firstLocalDay;
            return result < int.MinValue || result > int.MaxValue
                ? -1
                : (int)result;
        }

        internal static List<(int Lower, int Upper)> ParseSegments(string raw)
        {
            var result = new List<(int, int)>();
            foreach (string segment in InterCdLcConfig.ParseSegments(raw))
            {
                string[] parts = segment.Split(',');
                if (parts.Length != 2 ||
                    !int.TryParse(parts[0].Trim(), out int lower))
                    continue;
                string upperText = parts[1].Trim();
                int upper;
                if (upperText == "inf") upper = -1;
                else if (!int.TryParse(upperText, out upper)) continue;
                result.Add((lower, upper));
            }
            return result;
        }
    }
}
