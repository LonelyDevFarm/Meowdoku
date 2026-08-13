using System.Collections.Generic;

namespace Meowdoku.Core.Config
{
    public sealed class AttDialogLogicConfig : AbConfigBase<int>
    {
        public const int ValueDefault = 0;
        public const int ValueSkipCustomGuide = 1;
        public const int ValueRestyledGuide = 2;

        public AttDialogLogicConfig()
            : base("att_dlg_logic", ValueDefault, AbConfigTiming.AppStart) { }

        public bool ShouldSkipCustomGuide() =>
            Value == ValueSkipCustomGuide;

        public bool IsCustomGuideRestyled() =>
            Value == ValueRestyledGuide;
    }

    public sealed class PushPermissionConfig : AbConfigBase<int>
    {
        public const int ValueControl = 0;
        public const int ValueThreeDayProgress = 1;
        public const int ValueSessionStreak = 2;

        public PushPermissionConfig()
            : base(
                "push_permission",
                ValueControl,
                AbConfigTiming.GameEndNormal20) { }

        public bool ShouldShowByThreeDayProgress() =>
            Value == ValueThreeDayProgress;

        public bool ShouldShowBySessionStreak() =>
            Value == ValueSessionStreak;
    }

    public sealed class PushLocalTextConfig : AbConfigBase<int>
    {
        public const int ValueLegacy = 0;
        public const int ValueNewPool = 1;
        public const int ValueNewPool2 = 2;

        public PushLocalTextConfig()
            : base("push_local_text", ValueLegacy, AbConfigTiming.AppStart) { }

        public bool IsNewPool() => Value == ValueNewPool;
        public bool IsNewPool2() => Value == ValueNewPool2;
    }

    public sealed class RateUsPopConfig : AbConfigBase<int>
    {
        public const int ValueGateLevel8 = 0;
        public const int ValueGateLevel15 = 1;
        public const int ValueHomeAfterWin = 2;
        public const int ValueWinStreak5 = 3;

        public RateUsPopConfig()
            : base("rate_us_pop", ValueGateLevel8, AbConfigTiming.GameStart) { }

        public bool IsEligibleAtGameWin(
            int level,
            int sessionConsecutiveWins)
        {
            return Value switch
            {
                ValueGateLevel8 => level >= 8,
                ValueWinStreak5 => level >= 15 && sessionConsecutiveWins >= 5,
                _ => false
            };
        }
    }

    public sealed class RateUsPopUiConfig : AbConfigBase<int>
    {
        public const int ValueOldUi = 0;
        public const int ValueNewUi = 1;

        public RateUsPopUiConfig()
            : base("rate_us_pop_ui", ValueOldUi, AbConfigTiming.GameStart) { }

        public bool IsNewUi() => Value == ValueNewUi;
    }

    public sealed class PlatformConfigSet
    {
        private readonly IAbConfig[] _all;

        public PlatformConfigSet()
        {
            _all = new IAbConfig[]
            {
                AttDialogLogic,
                PushPermission,
                PushLocalText,
                RateUsPop,
                RateUsPopUi
            };
        }

        public AttDialogLogicConfig AttDialogLogic { get; } = new();
        public PushPermissionConfig PushPermission { get; } = new();
        public PushLocalTextConfig PushLocalText { get; } = new();
        public RateUsPopConfig RateUsPop { get; } = new();
        public RateUsPopUiConfig RateUsPopUi { get; } = new();
        public IReadOnlyList<IAbConfig> All => _all;
    }
}
