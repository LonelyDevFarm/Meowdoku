using System;

namespace Meowdoku.Core.Config
{
    public sealed class SwipeProtectConfig : AbConfigBase<int>
    {
        public const int ValueControl = 0;
        public const int ValueHotzone40 = 1;
        public const int ValueHotzone10 = 2;
        public const int ValueHotzoneRaised = 3;
        public const int ValueHotzone30 = 4;
        public const int ValueHotzone20 = 5;
        public const int ValueHotzone50 = 6;
        public const int ValueDynamicIntent = 7;
        public const int DynamicWindowMilliseconds = 100;
        public const double DynamicVelocityThresholdPixelsPerMillisecond = 1.2;

        public SwipeProtectConfig()
            : base("swipe_protect", ValueControl, AbConfigTiming.GameStart) { }

        public bool IsEnabled() => IsEnabledFor(Value);
        public int MinSize() => MinSizeFor(Value);
        public double TolerancePercent() => TolerancePercentFor(Value);
        public int ThresholdFor(int size) => ThresholdForValue(Value, size);
        public bool IsDynamicIntent() => Value == ValueDynamicIntent;

        public static bool IsEnabledFor(int value)
        {
            return value >= ValueHotzone40 && value <= ValueDynamicIntent;
        }

        public static int MinSizeFor(int value)
        {
            return value == ValueHotzoneRaised ? 7 : 0;
        }

        public static double TolerancePercentFor(int value)
        {
            switch (value)
            {
                case ValueHotzone10: return 0.1;
                case ValueHotzone20: return 0.2;
                case ValueHotzone30: return 0.3;
                case ValueHotzone50: return 0.5;
                case ValueHotzone40:
                case ValueHotzoneRaised:
                case ValueDynamicIntent: return 0.4;
                default: return 0.0;
            }
        }

        public static int ThresholdForValue(int value, int size)
        {
            return value == ValueHotzoneRaised ? (int)Math.Ceiling(size * 0.6) : 4;
        }
    }

    public sealed class DoubleTapProtectConfig : AbConfigBase<int>
    {
        public const int ValueControl = 0;
        public const int ValueShorten = 1;
        public const int ValueByTruth = 2;
        public const int ValueByConflict = 3;
        public const double LongSeconds = 0.35;
        public const double ShortSeconds = 0.25;

        public DoubleTapProtectConfig()
            : base("doubletap_protect", ValueControl, AbConfigTiming.AppStart) { }

        public double WindowSeconds(bool truthHasCat, bool wouldConflict)
        {
            switch (Value)
            {
                case ValueShorten: return ShortSeconds;
                case ValueByTruth: return truthHasCat ? LongSeconds : ShortSeconds;
                case ValueByConflict: return wouldConflict ? ShortSeconds : LongSeconds;
                default: return LongSeconds;
            }
        }

        public bool NeedsTruth() => Value == ValueByTruth;
        public bool NeedsConflict() => Value == ValueByConflict;
    }

    public sealed class TutorialDiagonalConfig : AbConfigBase<int>
    {
        public const int ValueAdjacent = 0;
        public const int ValueDiagonalCopy = 2;

        public TutorialDiagonalConfig()
            : base("tutorial_diagonal", ValueAdjacent, AbConfigTiming.AppStart) { }

        public bool IsDiagonalCopy() => Value == ValueDiagonalCopy;
    }

    public sealed class GuideFeedbackConfig : AbConfigBase<int>
    {
        public const int ValueCurrent = 0;
        public const int ValueCheck = 1;
        public const int ValueIq = 2;

        public GuideFeedbackConfig()
            : base("guide_feedback", ValueCurrent, AbConfigTiming.AppStart) { }

        public bool IsCheckGuide() => Value == ValueCheck;
        public bool IsIqGuide() => Value == ValueIq;
    }

    public sealed class RegionColorConfig : AbConfigBase<int>
    {
        public const int ValueControl = 0;
        public const int ValueCustomPalette = 1;
        public const int ValueNewCellOnly = 2;
        public const int ValueCellColorV3 = 3;
        public const int ValueNewCellRecompute = 4;
        public const int ValuePaletteV5 = 5;
        public const int ValuePaletteV6 = 6;
        public const int ValuePaletteV7 = 7;
        public const int ValuePaletteV8 = 8;
        public const int ValuePaletteV9 = 9;
        public const int ValueAllWarm = 10;
        public const int ValueAllCool = 11;
        public const int ValueTempBalanced = 12;

        public RegionColorConfig()
            : base("region_color", ValueNewCellOnly, AbConfigTiming.AppStart) { }

        public bool IsCustomPalette() => Value == ValueCustomPalette;
        public bool IsNewCellOnlyPalette() => Value == ValueNewCellOnly;
        public bool IsCellColorV3() => Value == ValueCellColorV3;
        public bool IsNewCellRecompute() => Value == ValueNewCellRecompute;
        public bool IsPaletteV5() => Value == ValuePaletteV5;
        public bool IsPaletteV6() => Value == ValuePaletteV6;
        public bool IsPaletteV7() => Value == ValuePaletteV7;
        public bool IsPaletteV8() => Value == ValuePaletteV8;
        public bool IsPaletteV9() => Value == ValuePaletteV9;
        public bool IsAllWarm() => Value == ValueAllWarm;
        public bool IsAllCool() => Value == ValueAllCool;
        public bool IsTempBalanced() => Value == ValueTempBalanced;
    }

    public sealed class GameGridUiConfig : AbConfigBase<int>
    {
        public const int ValueNormal = 0;
        public const int ValueSingleLine = 1;
        public const int ValueReduceSpacing = 2;
        public const int ValueDifferentCorners = 3;

        private static readonly int[] CornerRadiusBySize =
        {
            0, 0, 0, 0, 12, 11, 10, 9, 8, 7, 6, 5, 4
        };

        public GameGridUiConfig()
            : base("game_grid_ui", ValueNormal, AbConfigTiming.AppStart) { }

        public bool IsSingleLine() => Value == ValueSingleLine;
        public bool IsReduceSpacing() => Value == ValueReduceSpacing;
        public bool IsDifferentCorners() => Value == ValueDifferentCorners;

        public int DifferenceSizeCellCorners(int size)
        {
            return size >= 4 && size < CornerRadiusBySize.Length
                ? CornerRadiusBySize[size]
                : 10;
        }
    }

    public sealed class BoardSizeBigConfig : AbConfigBase<int>
    {
        public const int ValueNormal = 0;
        public const int ValueEnlarged = 1;

        public BoardSizeBigConfig()
            : base("board_size_big", ValueNormal, AbConfigTiming.GameStart) { }

        public bool IsEnlarged() => Value == ValueEnlarged;
    }

    public sealed class SizeCycleConfig : AbConfigBase<int>
    {
        public const int ValueControl = 2;
        public const int ValueCycleV3A = 3;
        public const int ValueCycleV3B = 4;
        public const int ValueCycleV3C = 5;
        public const int ValueCycleV3D = 6;
        public const int ValueCycleV3E = 7;
        public const int ValueCycleV3F = 8;

        public SizeCycleConfig()
            : base("size_cycle", ValueControl, AbConfigTiming.GameStartNormal) { }

        public bool IsCycleEnabled() => Value != ValueControl;
    }

    public sealed class SingleRegionNumConfig : AbConfigBase<int>
    {
        public const int ValueDefault = 0;
        public const int ValueLimited = 1;
        public const int ValueStrict = 2;
        public const int ValueAllOne = 3;
        public const int ValueZero51 = 4;
        public const int ValueZero101 = 5;

        public SingleRegionNumConfig()
            : base("single_region_num", ValueStrict, AbConfigTiming.GameStartNormal) { }

        public bool IsCoarseLimited() => Value != ValueDefault;
        public bool IsSingleRegionLimited() => Value == ValueLimited;
        public bool IsStrictLimitedAt(int levelNumber) => Value == ValueStrict && levelNumber >= 21;

        public int SingleLimitAt(int levelNumber, int rank)
        {
            switch (Value)
            {
                case ValueStrict:
                    return levelNumber >= 21 ? 1 : -1;
                case ValueAllOne:
                    return 1;
                case ValueZero51:
                    if (levelNumber >= 51) return rank == 1 ? 1 : 0;
                    return levelNumber >= 21 ? 1 : -1;
                case ValueZero101:
                    if (levelNumber >= 101) return rank == 1 ? 1 : 0;
                    return levelNumber >= 21 ? 1 : -1;
                default:
                    return -1;
            }
        }
    }

    public sealed class DailyFirstLevelDifficultyConfig : AbConfigBase<int>
    {
        public const int ValueControl = 0;
        public const int ValueReduceOne = 1;

        public DailyFirstLevelDifficultyConfig()
            : base("daily_first_level_difficulty", ValueControl, AbConfigTiming.AppStart) { }

        public bool IsEnabled() => Value == ValueReduceOne;
    }

    public sealed class DdaRankConfig : AbConfigBase<int>
    {
        public const int ValueControl = 0;
        public const int ValueRetryOnce = 1;
        public const int ValueToolRevive = 2;
        public const int ValueAnyAction = 3;

        public DdaRankConfig()
            : base("dda_rank", ValueControl, AbConfigTiming.GameStartNormal) { }

        public bool IsRetryOnceDemote() => Value == ValueRetryOnce;
        public bool IsToolReviveDemote() => Value == ValueToolRevive;
        public bool IsAnyActionDemote() => Value == ValueAnyAction;
    }

    public sealed class PreCatConfig : AbConfigBase<int>
    {
        public const int ValueOff = 0;
        public const int ValueAlways = 1;
        public const int ValueHalf = 2;

        public PreCatConfig()
            : base("pre_cat", ValueOff, AbConfigTiming.GameStartNormal21) { }
    }

    public sealed class RewardUnlockLevelConfig : AbConfigBase<int>
    {
        public const int DefaultUnlockLevel = 0;

        public RewardUnlockLevelConfig()
            : base(
                "reward_unlock_level",
                DefaultUnlockLevel,
                AbConfigTiming.GameStart) { }

        public bool IsRewardRequiredAt(int level) => level >= Value;
    }

    public sealed class PropHighlightConfig : AbConfigBase<int>
    {
        public const int ValueControl = 0;
        public const int ValueLocateOnce = 1;
        public const int ValueHintOnce = 2;
        public const int ValueNone = 3;
        public const int ValueControlRepeatable = 4;

        public PropHighlightConfig()
            : base("prop_highlight", ValueHintOnce, AbConfigTiming.GameStart) { }

        public string TargetProp()
        {
            switch (Value)
            {
                case ValueLocateOnce: return "locate";
                case ValueHintOnce: return "hint";
                case ValueNone: return "none";
                case ValueControlRepeatable: return "random";
                default: return "control";
            }
        }

        public bool IsOncePerLifetime()
        {
            return Value == ValueLocateOnce || Value == ValueHintOnce;
        }

        public bool IsRepeatable() => Value == ValueControlRepeatable;
    }

    public sealed class MarkSoundConfig : AbConfigBase<int>
    {
        public const int ValueControl = 0;
        public const int ValueSoft1 = 1;
        public const int ValueSoft2 = 2;

        public MarkSoundConfig()
            : base("mark_sound", ValueControl, AbConfigTiming.AppStart) { }

        public bool IsSoftVariant1() => Value == ValueSoft1;
        public bool IsSoftVariant2() => Value == ValueSoft2;
    }

    public sealed class RuleHighlightConfig : AbConfigBase<int>
    {
        public const int ValueOff = 0;
        public const int ValueOn = 1;

        public RuleHighlightConfig()
            : base("rule_highlight", ValueOff, AbConfigTiming.GameStart) { }

        public bool IsEnabled() => Value == ValueOn;
    }

    public sealed class RuleTextConfig : AbConfigBase<int>
    {
        public const int ValueText = 0;
        public const int ValueThirdImage = 1;
        public const int ValueAllImage = 2;
        public const int ValueIconText = 3;
        public const int ValueCollapse10 = 4;
        public const int ValueInfoPopup = 5;
        public const int ValueSettingEntry = 6;
        public const int ValueSingleSwipe = 7;

        public RuleTextConfig()
            : base("rule_text", ValueText, AbConfigTiming.GameStart) { }

        public bool UsesDefaultTextBar() => Value == ValueText;
        public bool IsThirdImage() => Value == ValueThirdImage;
        public bool IsAllImage() => Value == ValueAllImage;
        public bool IsIconText() => Value == ValueIconText;
        public bool IsCollapse10() => Value == ValueCollapse10;
        public bool IsInfoPopup() => Value == ValueInfoPopup;
        public bool IsSettingEntry() => Value == ValueSettingEntry;
        public bool IsSingleSwipe() => Value == ValueSingleSwipe;
    }
}
