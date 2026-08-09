using System;
using System.Collections.Generic;

namespace Meowdoku.Core.Config
{
    /// <summary>Policy port of score_encourage_config.gd.</summary>
    public sealed class ScoreEncourageConfig : AbConfigBase<int>
    {
        public const int ValueDisabled = 0;
        public const int ValueFlyEffect = 1;
        public const int ValueNonRound = 2;
        public const int ValueMultiplier = 3;
        public const int ValueSkillScore = 4;
        public const int ValueDeduction = 5;
        public const int ValueLifeBonus = 6;
        public const int ValueMultiplierScroll = 7;

        public ScoreEncourageConfig()
            : base("score_encourage", ValueDisabled, AbConfigTiming.GameStart) { }

        public bool IsEnabled() { return Value != ValueDisabled; }
        public bool HasFlyEffect()
        {
            return (Value >= ValueFlyEffect && Value <= ValueMultiplier) ||
                   Value == ValueMultiplierScroll;
        }
        public bool HasCustomScoring() { return Value >= ValueNonRound; }
        public bool HasMultiplierDisplay()
        {
            return Value == ValueMultiplier || Value == ValueMultiplierScroll;
        }
        public bool HasScrollMultiplierAnimation() { return Value == ValueMultiplierScroll; }
        public bool HasAppear4MultiplierAnimation() { return Value == ValueMultiplier; }
        public bool HasSkillScore() { return Value == ValueSkillScore; }
        public bool HasDeduction() { return Value == ValueDeduction; }
        public bool HasLifeBonus() { return Value == ValueLifeBonus; }

        public int CalculateGain(int comboCount)
        {
            if (Value == ValueNonRound)
                return Math.Min(576 + Math.Max(0, comboCount - 1) * 96, 1440);
            if (Value == ValueMultiplier || Value == ValueMultiplierScroll)
                return 600;
            return Math.Min(600 + Math.Max(0, comboCount - 1) * 80, 1320);
        }

        public float CalculateMultiplier(int comboCount)
        {
            if (Value != ValueMultiplier && Value != ValueMultiplierScroll) return 1f;
            if (comboCount < 3) return 1f;
            return 1.2f + 0.1f * comboCount;
        }

        public int CalculateSkillBonus(int cellStrategy)
        {
            switch (cellStrategy)
            {
                case 2: return 20;
                case 3: return 30;
                case 4: return 50;
                case 5: return 100;
                case 6: return 200;
                case 7: return 300;
                default: return 0;
            }
        }

        public int DeductionPerMistake() { return 100; }

        public IReadOnlyList<int> CalculateLifeBonusSequence(int lives)
        {
            switch (lives)
            {
                case 1: return new[] { 100 };
                case 2: return new[] { 100, 100 };
                case 3: return new[] { 100, 100, 200 };
                default: return Array.Empty<int>();
            }
        }
    }
}
