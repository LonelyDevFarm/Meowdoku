using System;

namespace Meowdoku.Core.Rank
{
    public static class RankActivityPeriod
    {
        public static bool ShouldOpen(
            int periodCount,
            int level,
            bool previousAwarded,
            int winsSinceClose,
            bool atHome,
            bool isNewSession,
            int unlockLevel,
            int reopenWins)
        {
            if (periodCount == 0) return level >= unlockLevel;
            if (previousAwarded)
                return atHome || winsSinceClose >= reopenWins;
            return winsSinceClose >= reopenWins || isNewSession;
        }

        public static bool IsExpired(long nowUnix, long endUnix) =>
            endUnix > 0 && nowUnix >= endUnix;

        public static long ComputeEnd(long nowUnix, int durationSeconds) =>
            nowUnix + durationSeconds;

        public static int RemainingSeconds(long nowUnix, long endUnix)
        {
            long remaining = Math.Max(0L, endUnix - nowUnix);
            return remaining > int.MaxValue ? int.MaxValue : (int)remaining;
        }
    }
}
