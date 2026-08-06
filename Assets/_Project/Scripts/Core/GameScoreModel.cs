using System.Collections.Generic;

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
            if (d.TryGetValue("score", out int savedScore)) Score = savedScore;
            if (d.TryGetValue("combo", out int savedCombo)) Combo = savedCombo;
            if (d.TryGetValue("max_combo", out int savedMax)) MaxCombo = savedMax;
        }
    }
}
