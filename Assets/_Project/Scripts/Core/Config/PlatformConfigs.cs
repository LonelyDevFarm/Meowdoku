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

    public sealed class PlatformConfigSet
    {
        private readonly IAbConfig[] _all;

        public PlatformConfigSet()
        {
            _all = new IAbConfig[]
            {
                AttDialogLogic,
                PushPermission,
                PushLocalText
            };
        }

        public AttDialogLogicConfig AttDialogLogic { get; } = new();
        public PushPermissionConfig PushPermission { get; } = new();
        public PushLocalTextConfig PushLocalText { get; } = new();
        public IReadOnlyList<IAbConfig> All => _all;
    }
}
