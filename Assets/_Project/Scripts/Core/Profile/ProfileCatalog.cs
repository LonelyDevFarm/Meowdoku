using System;
using System.Collections.Generic;

namespace Meowdoku.Core.Profile
{
    public static class ProfileCatalog
    {
        public const int AcquireThreshold = 100;
        public const int FirstPlaceFrameId = 100;

        private static readonly int[] AvatarIdValues =
            { 1, 2, 3, 4, 5, 6, 7, 8 };
        private static readonly int[] ClassicFrameIdValues =
            { 1, 2, 3, 4, 5, 6, 7, 8 };
        private static readonly int[] LeaderboardFrameIdValues = { 100 };

        public static IReadOnlyList<int> AvatarIds => AvatarIdValues;
        public static IReadOnlyList<int> ClassicFrameIds => ClassicFrameIdValues;
        public static IReadOnlyList<int> LeaderboardFrameIds => LeaderboardFrameIdValues;

        public static bool IsValidAvatar(int id) =>
            Array.IndexOf(AvatarIdValues, id) >= 0;

        public static bool IsValidFrame(int id) =>
            Array.IndexOf(ClassicFrameIdValues, id) >= 0 ||
            Array.IndexOf(LeaderboardFrameIdValues, id) >= 0;

        public static bool IsDefaultOwnedFrame(int id) =>
            id > 0 && id < AcquireThreshold;

        public static bool IsCountedFrame(int id) => id >= AcquireThreshold;

        public static int DefaultFrameCount(int id) =>
            IsCountedFrame(id) ? 0 : -1;

        public static int[] CopyAvatarIds() => (int[])AvatarIdValues.Clone();
        public static int[] CopyClassicFrameIds() =>
            (int[])ClassicFrameIdValues.Clone();
        public static int[] CopyLeaderboardFrameIds() =>
            (int[])LeaderboardFrameIdValues.Clone();
    }
}
