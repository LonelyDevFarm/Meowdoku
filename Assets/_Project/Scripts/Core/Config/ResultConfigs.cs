using System.Collections.Generic;

namespace Meowdoku.Core.Config
{
    public sealed class ResultConfigSet
    {
        private readonly IAbConfig[] _all;

        public ResultConfigSet()
        {
            // Preserve ABTestManager registration order for source timing/dye.
            _all = new IAbConfig[]
            {
                FailText,
                PassText,
                ReviveFreeLogic,
                ReviveLife,
                WinToast,
                PassPage
            };
        }

        public FailTextConfig FailText { get; } = new();
        public PassTextConfig PassText { get; } = new();
        public ReviveFreeLogicConfig ReviveFreeLogic { get; } = new();
        public ReviveLifeConfig ReviveLife { get; } = new();
        public WinToastConfig WinToast { get; } = new();
        public PassPageConfig PassPage { get; } = new();
        public IReadOnlyList<IAbConfig> All => _all;
    }

    public sealed class WinToastConfig : AbConfigBase<int>
    {
        public const int ValueControl = 0;
        public const int ValueP5 = 1;
        public const int ValueP10 = 2;
        public const int ValueP20 = 3;

        public WinToastConfig()
            : base("win_toast", ValueControl, AbConfigTiming.GameStart) { }

        public bool IsEnabled() => Value != ValueControl;
        public bool CoversTier(int tier) =>
            tier >= 0 && Value >= System.Math.Max(1, tier);
    }

    public static class WinToastTierContract
    {
        public const int TierNone = -1;
        public const int TierPerfect = 0;
        public const int TierP5 = 1;
        public const int TierP10 = 2;
        public const int TierP20 = 3;

        public static int DetermineTier(int size, int stepsUsed)
        {
            if (size <= 0 || stepsUsed <= 0 || stepsUsed < size)
                return TierNone;
            if (!TryThresholds(size, out int p5, out int p10, out int p20))
                return TierNone;
            if (stepsUsed == size) return TierPerfect;
            if (stepsUsed <= p5) return TierP5;
            if (stepsUsed <= p10) return TierP10;
            if (stepsUsed <= p20) return TierP20;
            return TierNone;
        }

        public static string MessageKey(int tier, int randomIndex)
        {
            int count = tier == TierPerfect ? 9 : 7;
            if (tier < TierPerfect || tier > TierP20) return string.Empty;
            int index = ((randomIndex % count) + count) % count + 1;
            string group = tier switch
            {
                TierPerfect => "PERFECT",
                TierP5 => "P5",
                TierP10 => "P10",
                TierP20 => "P20",
                _ => string.Empty
            };
            return $"WIN_TOAST_{group}_{index:00}";
        }

        private static bool TryThresholds(
            int size,
            out int p5,
            out int p10,
            out int p20)
        {
            (p5, p10, p20) = size switch
            {
                6 => (16, 20, 30),
                7 => (30, 32, 36),
                8 => (25, 32, 40),
                9 => (28, 35, 45),
                10 => (30, 38, 50),
                11 => (60, 65, 71),
                12 => (70, 75, 80),
                _ => (0, 0, 0)
            };
            return p5 > 0;
        }
    }

    public sealed class PassPageConfig : AbConfigBase<int>
    {
        public const int ValueControl = 0;
        public const int ValueG1 = 1;
        public const int ValueG2 = 2;
        public const int ValueG4 = 4;

        public PassPageConfig()
            : base("pass_page", ValueControl, AbConfigTiming.GameStart) { }

        public bool IsG1() => Value == ValueG1;
        public bool IsG2() => Value == ValueG2;
        public bool IsG4() => Value == ValueG4;
    }

    public sealed class PassTextConfig : AbConfigBase<int>
    {
        public const int ValueControl = 0;
        public const int ValueBeatPercent = 1;
        public const int ValueV2 = 2;
        public const int ValueV3G1 = 3;
        public const int ValueV3G2 = 4;
        public const int ValueV3G3 = 5;

        public PassTextConfig()
            : base("pass_text", ValueControl, AbConfigTiming.GameStart) { }

        public bool ShouldShowBeatPercent() => Value == ValueBeatPercent;
    }

    public sealed class PassTextStrategyInput
    {
        public int Level { get; set; }
        public int Size { get; set; }
        public int RestartCount { get; set; }
        public int ReviveCount { get; set; }
        public int MistakeCount { get; set; }
        public double ElapsedSeconds { get; set; }
        public double LastWinBeatPercent { get; set; } = -1.0;
        public bool IsDaily { get; set; }
        public bool IsHard { get; set; }
    }

    public sealed class PassTextStrategySelection
    {
        public static readonly PassTextStrategySelection Empty = new();

        public string TitleKey { get; internal set; } = string.Empty;
        public string BodyKey { get; internal set; } = string.Empty;
        public double ShownPercent { get; internal set; } = -1.0;
        public double Percent { get; internal set; } = -1.0;
        public double DifferencePercent { get; internal set; } = -1.0;
    }

    /// <summary>
    /// Pure selection contract ported from pass_text_strategy_v0-v3_g3.gd.
    /// It returns localization keys so presentation remains locale-owned.
    /// </summary>
    public static class PassTextStrategyContract
    {
        private enum Pool
        {
            V2HardFirst,
            V2HardRetry,
            V2Perfect,
            V2Retry,
            V3HardFirst,
            V3HardRetry,
            V3Perfect,
            V3Retry,
            V3Plain,
            V3PlainG2
        }

        public static PassTextStrategySelection Select(
            int variant,
            PassTextStrategyInput input,
            int randomIndex = 0,
            double randomFraction = 0.0)
        {
            if (input == null || input.IsDaily ||
                variant == PassTextConfig.ValueControl)
                return PassTextStrategySelection.Empty;

            if (variant == PassTextConfig.ValueBeatPercent)
                return input.Size > 0
                    ? PercentOnly(input, randomFraction)
                    : PassTextStrategySelection.Empty;
            if (variant == PassTextConfig.ValueV2)
                return SelectV2(input, randomIndex, randomFraction);
            if (variant >= PassTextConfig.ValueV3G1 &&
                variant <= PassTextConfig.ValueV3G3)
                return SelectV3(variant, input, randomIndex, randomFraction);
            return PassTextStrategySelection.Empty;
        }

        private static PassTextStrategySelection SelectV2(
            PassTextStrategyInput input,
            int randomIndex,
            double randomFraction)
        {
            if (input.IsHard)
                return Pick(
                    input.RestartCount == 0
                        ? Pool.V2HardFirst
                        : Pool.V2HardRetry,
                    randomIndex);

            bool multipleAttempts = input.RestartCount > 0 || input.ReviveCount > 0;
            if (input.MistakeCount == 0 && !multipleAttempts)
                return Pick(Pool.V2Perfect, randomIndex);
            if (multipleAttempts) return Pick(Pool.V2Retry, randomIndex);
            if (input.Size <= 0) return PassTextStrategySelection.Empty;

            double percent = PassTextStatsContract.RoundNonZeroDecimal(
                PassTextStatsContract.BeatPercent(
                    input.ElapsedSeconds,
                    input.Size,
                    randomFraction));
            if (percent <= 75.0)
                return Fixed("WIN_V2_STRATEGIC_TITLE", "WIN_V2_STRATEGIC_BODY");
            if (input.LastWinBeatPercent >= 0.0 &&
                percent > input.LastWinBeatPercent)
            {
                double difference = PassTextStatsContract.RoundNonZeroDecimal(
                    percent - input.LastWinBeatPercent);
                return WithPercent(
                    "WIN_V2_AWESOME_TITLE",
                    "WIN_V2_AWESOME_BODY",
                    percent,
                    difference);
            }
            if (percent < 83.0)
                return WithPercent(
                    "WIN_V2_PERCEPTIVE_TITLE",
                    "WIN_V2_PERCEPTIVE_BODY",
                    percent);
            if (percent < 91.0)
                return WithPercent(
                    "WIN_V2_INTELLIGENT_TITLE",
                    "WIN_V2_INTELLIGENT_BODY",
                    percent);
            return WithPercent(
                "WIN_V2_BRILLIANT_TITLE",
                "WIN_V2_BRILLIANT_BODY",
                percent);
        }

        private static PassTextStrategySelection SelectV3(
            int variant,
            PassTextStrategyInput input,
            int randomIndex,
            double randomFraction)
        {
            if (input.IsHard)
                return Pick(
                    input.RestartCount == 0
                        ? Pool.V3HardFirst
                        : Pool.V3HardRetry,
                    randomIndex);

            bool multipleAttempts = input.RestartCount > 0 || input.ReviveCount > 0;
            if (input.MistakeCount == 0 && !multipleAttempts)
                return Pick(Pool.V3Perfect, randomIndex);
            if (multipleAttempts) return Pick(Pool.V3Retry, randomIndex);
            if (input.Size <= 0) return PassTextStrategySelection.Empty;

            double raw = variant == PassTextConfig.ValueV3G2
                ? PassTextStatsContract.BeatPercentGroup2(
                    input.ElapsedSeconds, input.Size, randomFraction)
                : PassTextStatsContract.BeatPercent(
                    input.ElapsedSeconds, input.Size, randomFraction);
            double percent = PassTextStatsContract.RoundNonZeroDecimal(raw);
            Pool plainPool = variant == PassTextConfig.ValueV3G2
                ? Pool.V3PlainG2
                : Pool.V3Plain;
            if (variant == PassTextConfig.ValueV3G3)
                return Pick(plainPool, randomIndex);

            double plainThreshold = variant == PassTextConfig.ValueV3G2
                ? 85.0
                : 75.0;
            if (percent <= plainThreshold) return Pick(plainPool, randomIndex);
            if (input.LastWinBeatPercent >= 0.0 &&
                percent > input.LastWinBeatPercent)
            {
                double difference = PassTextStatsContract.RoundNonZeroDecimal(
                    percent - input.LastWinBeatPercent);
                return WithPercent(
                    "WIN_V2_AWESOME_TITLE",
                    "WIN_V2_AWESOME_BODY",
                    percent,
                    difference);
            }

            double perceptiveThreshold = variant == PassTextConfig.ValueV3G2
                ? 91.0
                : 83.0;
            double intelligentThreshold = variant == PassTextConfig.ValueV3G2
                ? 95.0
                : 91.0;
            if (percent < perceptiveThreshold)
                return WithPercent(
                    "WIN_V2_PERCEPTIVE_TITLE",
                    "WIN_V2_PERCEPTIVE_BODY",
                    percent);
            if (percent < intelligentThreshold)
                return WithPercent(
                    "WIN_V2_INTELLIGENT_TITLE",
                    "WIN_V2_INTELLIGENT_BODY",
                    percent);
            return WithPercent(
                "WIN_V2_BRILLIANT_TITLE",
                "WIN_V2_BRILLIANT_BODY",
                percent);
        }

        private static PassTextStrategySelection PercentOnly(
            PassTextStrategyInput input,
            double randomFraction)
        {
            double percent = PassTextStatsContract.RoundNonZeroDecimal(
                PassTextStatsContract.BeatPercent(
                    input.ElapsedSeconds,
                    input.Size,
                    randomFraction));
            return WithPercent(string.Empty, "WIN_BEAT_PERCENT_TIP", percent);
        }

        private static PassTextStrategySelection WithPercent(
            string titleKey,
            string bodyKey,
            double percent,
            double difference = -1.0)
        {
            return new PassTextStrategySelection
            {
                TitleKey = titleKey,
                BodyKey = bodyKey,
                ShownPercent = percent,
                Percent = percent,
                DifferencePercent = difference
            };
        }

        private static PassTextStrategySelection Fixed(
            string titleKey,
            string bodyKey)
        {
            return new PassTextStrategySelection
            {
                TitleKey = titleKey,
                BodyKey = bodyKey
            };
        }

        private static PassTextStrategySelection Pick(Pool pool, int randomIndex)
        {
            int count = pool switch
            {
                Pool.V2HardFirst => 5,
                Pool.V2HardRetry => 5,
                Pool.V2Perfect => 4,
                Pool.V2Retry => 3,
                Pool.V3HardFirst => 15,
                Pool.V3HardRetry => 15,
                Pool.V3Perfect => 14,
                Pool.V3Retry => 13,
                Pool.V3Plain => 10,
                Pool.V3PlainG2 => 11,
                _ => 1
            };
            int index = ((randomIndex % count) + count) % count;
            return pool switch
            {
                Pool.V2HardFirst => Pair("WIN_V2_HARD_FIRST", index),
                Pool.V2HardRetry => Pair("WIN_V2_HARD_RETRY", index),
                Pool.V2Perfect => Pair("WIN_V2_PERFECT", index),
                Pool.V2Retry => Pair("WIN_V2_RETRY", index),
                Pool.V3HardFirst => index < 5
                    ? Pair("WIN_V2_HARD_FIRST", index)
                    : Pair("WIN_V3_HARD_FIRST", index),
                Pool.V3HardRetry => index < 5
                    ? Pair("WIN_V2_HARD_RETRY", index)
                    : Pair("WIN_V3_HARD_RETRY", index),
                Pool.V3Perfect => index < 4
                    ? Pair("WIN_V2_PERFECT", index)
                    : Pair("WIN_V3_PERFECT", index),
                Pool.V3Retry => index < 3
                    ? Pair("WIN_V2_RETRY", index)
                    : Pair("WIN_V3_RETRY", index),
                Pool.V3Plain => index == 0
                    ? Fixed("WIN_V2_STRATEGIC_TITLE", "WIN_V2_STRATEGIC_BODY")
                    : Pair("WIN_V3_PLAIN", index - 1),
                Pool.V3PlainG2 => index == 0
                    ? Fixed("WIN_V2_STRATEGIC_TITLE", "WIN_V2_STRATEGIC_BODY")
                    : Pair(
                        "WIN_V3_PLAIN",
                        index == 9 ? 9 : index == 10 ? 8 : index - 1),
                _ => PassTextStrategySelection.Empty
            };
        }

        private static PassTextStrategySelection Pair(string prefix, int index)
        {
            return Fixed(
                prefix + "_TITLE_" + index,
                prefix + "_BODY_" + index);
        }
    }

    public sealed class ReviveLifeConfig : AbConfigBase<int>
    {
        public const int ValueControl = 0;
        public const int ValueGroup1 = 1;
        public const int ValueGroup2 = 2;
        public const int ValueGroup3 = 3;

        public ReviveLifeConfig()
            : base("revive_life", ValueControl, AbConfigTiming.GameStart) { }

        public int LivesToRestore() => Value == ValueControl ? 1 : 3;
        public bool IsTwoLineButton() => Value == ValueGroup2;
        public bool IsAlternateButtonText() => Value == ValueGroup3;
    }

    public sealed class ReviveFreeLogicConfig : AbConfigBase<int>
    {
        public const int ValueControl = 0;
        public const int ValueFirstLevelUnlimited = 1;
        public const int ValueFirstEverOnce = 2;

        public ReviveFreeLogicConfig()
            : base("revive_free_logic", ValueControl, AbConfigTiming.AppStart) { }

        public bool ShouldFreeRevive(int currentLevel, bool hasUsedFreeRevive)
        {
            return Value == ValueFirstLevelUnlimited && currentLevel == 1 ||
                   Value == ValueFirstEverOnce && !hasUsedFreeRevive;
        }

        public bool ShouldConsume() => Value == ValueFirstEverOnce;
    }

    public sealed class FailTextConfig : AbConfigBase<int>
    {
        public const int ValueControl = 0;
        public const int ValueProgressText = 1;
        public const int ValueRevivePromote = 2;

        public FailTextConfig()
            : base("fail_text", ValueControl, AbConfigTiming.GameEnd) { }

        public bool ShouldShowEncourage() => Value >= ValueProgressText;
        public bool ShouldShowRevivePromote() => Value == ValueRevivePromote;
    }

    public static class PassTextStatsContract
    {
        public static double BeatPercent(
            double elapsedSeconds,
            int size,
            double randomFraction = 0.0)
        {
            return Calculate(
                elapsedSeconds, size, 51.0, 48.0, randomFraction);
        }

        public static double BeatPercentGroup2(
            double elapsedSeconds,
            int size,
            double randomFraction = 0.0)
        {
            return Calculate(
                elapsedSeconds, size, 61.0, 38.0, randomFraction);
        }

        public static double RoundNonZeroDecimal(double percent)
        {
            double rounded = System.Math.Round(
                percent * 10.0,
                System.MidpointRounding.AwayFromZero) / 10.0;
            double integer = System.Math.Round(
                rounded,
                System.MidpointRounding.AwayFromZero);
            if (System.Math.Abs(rounded - integer) < 0.05)
                rounded = integer + 0.1;
            return rounded;
        }

        private static double Calculate(
            double elapsedSeconds,
            int size,
            double basePercent,
            double span,
            double randomFraction)
        {
            double p90 = P90(size);
            double delta = System.Math.Max(0.0, p90 - elapsedSeconds);
            return basePercent +
                   span * System.Math.Sqrt(delta / p90) +
                   randomFraction;
        }

        private static int P90(int size)
        {
            return size switch
            {
                <= 4 => 44,
                5 => 37,
                6 => 107,
                7 => 187,
                8 => 265,
                9 => 360,
                _ => 431
            };
        }
    }
}
