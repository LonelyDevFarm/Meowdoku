namespace Meowdoku.Core.Config
{
    using System.Collections.Generic;

    public sealed class DailyStreakConfig : AbConfigBase<int>
    {
        public const int ValueControl = 0;
        public const int ValueBasic = 1;
        public const int ValueChallengeOnly = 2;
        public const int ValueNoReward = 3;
        public const int ValueNoLit = 4;
        public const int ValueEntry = 6;
        public const int ValueEntryReorder = 7;

        public DailyStreakConfig()
            : base("daily_streak", ValueBasic, AbConfigTiming.AppStart) { }

        public bool IsEnabled() => Value != ValueControl;

        // These two policies intentionally mirror the current source implementation,
        // including variants whose names suggest behavior that is not currently active.
        public bool IsChallengeOnly() => false;
        public bool HasReward() => Value != ValueControl;
        public bool IsSkipLit() => false;

        public bool HasPlayEntry()
        {
            return Value == ValueEntry || Value == ValueEntryReorder;
        }

        public bool IsSettleReorder() => Value == ValueEntryReorder;
    }

    public sealed class LeaderboardFuncConfig : AbConfigBase<int>
    {
        public const int ValueControl = 0;
        public const int ValueCatsProp = 1;
        public const int ValueFishProp = 2;
        public const int ValueCatsFrameOnly = 3;

        public LeaderboardFuncConfig()
            : base("leaderboard_func", ValueControl, AbConfigTiming.AppStart) { }

        public bool IsEnabled() => Value != ValueControl;
        public int GetGroup() => Value;
    }

    public sealed class HardButtonConfig : AbConfigBase<int>
    {
        public const int ValueDefault = 0;
        public const int ValueStarsFire = 1;
        public const int ValueYellowFire = 2;
        public const int ValueRed1 = 3;
        public const int ValueRed2 = 4;

        public HardButtonConfig()
            : base("hard_button", ValueDefault, AbConfigTiming.AppStart) { }

        public int EffectVariant() => Value;
    }

    public sealed class HomeConfigSet
    {
        private readonly IAbConfig[] _all;

        public HomeConfigSet()
        {
            _all = new IAbConfig[]
            {
                DailyStreak,
                Leaderboard,
                HardButton
            };
        }

        public DailyStreakConfig DailyStreak { get; } = new();
        public LeaderboardFuncConfig Leaderboard { get; } = new();
        public HardButtonConfig HardButton { get; } = new();
        public IReadOnlyList<IAbConfig> All => _all;
    }
}
