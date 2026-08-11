namespace Meowdoku.Core.Config
{
    public sealed class StreakProtectConfig : AbConfigBase<int>
    {
        public const int ValueControl = 0;
        public const int ValueNumberRoll = 1;
        public const int ValueBackfill1 = 2;
        public const int ValueBackfill2 = 3;
        public const int ValueResume = 4;

        public StreakProtectConfig()
            : base("streak_protect", ValueControl, AbConfigTiming.AppStart) { }

        public int ReviveMaxDays()
        {
            return Value switch
            {
                ValueBackfill1 => 1,
                ValueBackfill2 => 2,
                ValueResume => 3,
                _ => 0
            };
        }

        public bool IsBackfill()
        {
            return Value == ValueBackfill1 || Value == ValueBackfill2;
        }

        public bool IsResume() => Value == ValueResume;
        public bool HasNumberRoll() => Value != ValueControl;
    }
}
