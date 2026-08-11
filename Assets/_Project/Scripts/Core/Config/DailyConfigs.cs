namespace Meowdoku.Core.Config
{
    public sealed class DcLevelConfig : AbConfigBase<int>
    {
        public const int ValueControl = 0;
        public const int ValueTiered10 = 1;
        public const int ValueTiered12Easy = 2;
        public const int ValueTiered12Hard = 3;
        public const int ValueRandom = 4;

        public DcLevelConfig()
            : base("dc_level", ValueControl, AbConfigTiming.GameStartDaily) { }

        public bool IsOverrideEnabled() => Value != ValueControl;

        public int GetPoolSize(int currentLevel, int daySeed)
        {
            switch (Value)
            {
                case ValueTiered10:
                    return currentLevel <= 200 ? 10 : 12;
                case ValueTiered12Easy:
                case ValueTiered12Hard:
                    return 12;
                case ValueRandom:
                    return daySeed % 2 == 0 ? 10 : 12;
                default:
                    return 10;
            }
        }

        public int GetPoolRank(int currentLevel, int daySeed)
        {
            switch (Value)
            {
                case ValueTiered10:
                case ValueTiered12Hard:
                    if (currentLevel <= 50) return 3;
                    return currentLevel <= 100 ? 4 : 5;
                case ValueTiered12Easy:
                    return currentLevel <= 100 ? 3 : 4;
                case ValueRandom:
                    int rankSeed = (daySeed + 1) % 10;
                    if (rankSeed < 4) return 3;
                    return rankSeed < 8 ? 4 : 5;
                default:
                    return 3;
            }
        }

        public bool UseGcBank(int size, int dayOffset)
        {
            return size == 10 || dayOffset % 2 != 0;
        }
    }

    public sealed class NoDcConfig : AbConfigBase<int>
    {
        public const int ValueShow = 0;
        public const int ValueHide = 1;

        public NoDcConfig()
            : base("no_dc", ValueShow, AbConfigTiming.AppStart) { }

        public bool ShouldShow() => Value == ValueShow;
    }

    public sealed class DcTagUiConfig : AbConfigBase<int>
    {
        public const int ValueControl = 0;
        public const int ValuePaw = 1;

        public DcTagUiConfig()
            : base("dc_tag_ui", ValueControl, AbConfigTiming.AppStart) { }

        public bool UsePawTag() => Value == ValuePaw;
    }
}
