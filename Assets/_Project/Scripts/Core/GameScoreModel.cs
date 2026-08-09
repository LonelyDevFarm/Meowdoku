using System.Collections.Generic;
using Meowdoku.Core.Config;

namespace Meowdoku.Core
{
    // Quản lý điểm số và chuỗi combo của người chơi.
    public class GameScoreModel
    {
        public int Score { get; private set; } = 0;
        public int Combo { get; private set; } = 0;
        public int MaxCombo { get; private set; } = 0;

        // Tăng combo hiện tại lên 1. Cập nhật MaxCombo nếu cần.
        public void AddCombo()
        {
            Combo += 1;
            if (Combo > MaxCombo)
            {
                MaxCombo = Combo;
            }
        }

        // Đứt chuỗi combo, đưa về 0.
        public void ResetCombo()
        {
            Combo = 0;
        }

        // Cộng thêm điểm.
        public void AddScore(int gain)
        {
            Score += gain;
        }

        // Bị trừ điểm (phạt) khi làm sai.
        public void ApplyDeduction(int amount)
        {
            Score -= amount;
        }

        // Khôi phục mọi chỉ số về 0 (ví dụ: ván mới).
        public void ResetAll()
        {
            Score = 0;
            Combo = 0;
            MaxCombo = 0;
        }

        // Đóng gói dữ liệu để Save game.
        public Dictionary<string, int> ToDict()
        {
            return new Dictionary<string, int>
            {
                { "score", Score },
                { "combo", Combo },
                { "max_combo", MaxCombo }
            };
        }

        // Tải dữ liệu từ Save game.
        public void Restore(Dictionary<string, int> d)
        {
            Score = d != null && d.TryGetValue("score", out int savedScore) ? savedScore : 0;
            Combo = d != null && d.TryGetValue("combo", out int savedCombo) ? savedCombo : 0;
            MaxCombo = d != null && d.TryGetValue("max_combo", out int savedMax)
                ? savedMax
                : Combo;
        }
    }

    public sealed class ScoreGainResult
    {
        public int BaseGain { get; internal set; }
        public float Multiplier { get; internal set; }
        public int SkillBonus { get; internal set; }
        public int TotalGain { get; internal set; }
    }

    /// <summary>Pure scoring sequence extracted from BaseGamePage signal handlers.</summary>
    public static class GameScoringRules
    {
        public static ScoreGainResult ApplyCorrectCat(
            GameScoreModel model,
            ScoreEncourageConfig config,
            ref int successfulCatCount,
            int cellRank = 1,
            bool isToolSource = false)
        {
            model.AddCombo();
            successfulCatCount++;

            int baseGain = config.CalculateGain(
                config.IsEnabled() ? successfulCatCount : model.Combo);
            float multiplier = config.IsEnabled()
                ? config.CalculateMultiplier(successfulCatCount)
                : 1f;
            int skillBonus = config.HasSkillScore() && !isToolSource
                ? config.CalculateSkillBonus(cellRank)
                : 0;
            int total = (int)(baseGain * multiplier) + skillBonus;
            model.AddScore(total);
            return new ScoreGainResult
            {
                BaseGain = baseGain,
                Multiplier = multiplier,
                SkillBonus = skillBonus,
                TotalGain = total
            };
        }

        public static int ApplyWrongGuess(
            GameScoreModel model,
            ScoreEncourageConfig config,
            ref int successfulCatCount)
        {
            model.ResetCombo();
            successfulCatCount = 0;
            if (!config.HasDeduction()) return 0;
            int deduction = config.DeductionPerMistake();
            model.ApplyDeduction(deduction);
            return deduction;
        }

        public static int ApplyLifeBonus(
            GameScoreModel model,
            ScoreEncourageConfig config,
            int lives)
        {
            if (!config.HasLifeBonus()) return 0;
            IReadOnlyList<int> sequence = config.CalculateLifeBonusSequence(lives);
            int total = 0;
            for (int i = 0; i < sequence.Count; i++)
            {
                model.AddScore(sequence[i]);
                total += sequence[i];
            }
            return total;
        }
    }
}
