using System;
using System.Collections.Generic;
using Meowdoku.Core.Daily;
using Meowdoku.Core.Profile;
using Meowdoku.Core.Robot;

namespace Meowdoku.Core.Rank
{
    public static class RankActivityConfig
    {
        public const int GroupCats = 1;
        public const int GroupFish = 2;
        public const int GroupFrameOnly = 3;
        public const int DefaultGroup = GroupCats;
        public const int UnlockLevel = 11;
        public const int PeriodDurationSeconds = 86400;
        public const int ReopenWins = 10;
        public const int RobotCount = 49;
        public const int MinimumScoringRobots = 5;

        public static RobotConfig BuildRobotConfig(int group)
        {
            var config = new RobotConfig
            {
                RobotCount = RobotCount,
                MinimumScoringRobots = MinimumScoringRobots
            };
            if (group != GroupFish) return config;
            config.BaseFloor = 32;
            config.Ceiling = 230;
            config.BotOffset = 1;
            config.ArrayValues.Clear();
            config.ArrayValues.AddRange(new[] { 1, 2, 3 });
            config.ArrayWeights.Clear();
            config.ArrayWeights.AddRange(new[] { 0.2f, 0.3f, 0.5f });
            config.ArrayStrategy = "fill_to_zero";
            config.StalkValues.Clear();
            config.StalkValues.AddRange(new[] { 1, 2, 3 });
            config.StalkDeltaTimeFactor = 1.5f;
            return config;
        }

        public static int MapCollect(
            int group,
            int levelCats,
            int remainingFish) => group == GroupFish
            ? remainingFish
            : levelCats;

        public static bool HasReward(int group, int rank)
        {
            if (rank <= 0) return false;
            return group == GroupFrameOnly ? rank == 1 : rank <= 3;
        }

        public static bool IsFirstPlace(int rank) => rank == 1;

        public static List<AwardItem> RewardItems(
            int group,
            int rank,
            IRobotRandom random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            var items = new List<AwardItem>();
            if (rank == 1)
                items.Add(AwardItem.Frame(
                    ProfileCatalog.FirstPlaceFrameId,
                    1));
            if (group == GroupFrameOnly) return items;
            switch (rank)
            {
                case 1:
                    items.Add(AwardItem.Tool("locate", 2));
                    items.Add(AwardItem.Tool("hint", 2));
                    break;
                case 2:
                    items.Add(AwardItem.Tool("locate", 1));
                    items.Add(AwardItem.Tool("hint", 1));
                    break;
                case 3:
                    items.Add(AwardItem.Tool(
                        random.NextInclusive(0, 1) == 0
                            ? "locate"
                            : "hint",
                        1));
                    break;
            }
            return items;
        }
    }
}
