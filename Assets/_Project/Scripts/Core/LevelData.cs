using System.Collections.Generic;
using UnityEngine;

namespace Meowdoku.Core
{
    // Chứa dữ liệu tĩnh và logic sinh cấp độ (Level).
    public static class LevelData
    {
        public const int LEVEL_COUNT = 0;
        
        // Mảng quy định kích thước bảng (Board Size) cho 100 level đầu tiên.
        public static readonly int[] SIZES = new int[] {
            4, 4, 5, 5, 6, 5, 5, 6, 6, 7, 
            6, 6, 6, 6, 7, 6, 7, 6, 7, 8, 
            6, 7, 6, 7, 8, 6, 7, 8, 7, 8, 
            6, 7, 6, 7, 8, 6, 7, 8, 7, 8, 
            6, 7, 6, 7, 8, 6, 7, 8, 7, 8, 
            6, 7, 8, 7, 9, 6, 7, 8, 9, 10, 
            6, 7, 8, 7, 9, 6, 7, 8, 9, 10, 
            6, 7, 8, 7, 9, 6, 7, 8, 9, 10, 
            6, 7, 8, 7, 9, 6, 7, 8, 9, 10, 
            6, 7, 8, 7, 9, 6, 7, 8, 9, 10
        };

        // Kích thước bảng lặp lại cho các level từ 101 trở đi.
        private static readonly int[] _SIZES_101_PLUS = new int[] { 7, 8, 7, 9, 10, 7, 8, 9, 8, 10 };

        // Trả về kích thước mảng (NxN) dựa vào cấp độ hiện tại.
        public static int GetSize(int levelNum)
        {
            if (levelNum < 1) return 0;
            if (levelNum <= 100) return SIZES[levelNum - 1];
            return _SIZES_101_PLUS[(levelNum - 101) % 10];
        }

        // Xác định xem cấp độ hiện tại có phải là cấp độ khó hay không.
        public static bool IsHardLevel(int levelNum)
        {
            return levelNum >= 21 && levelNum % 10 == 0;
        }

        public static int StrategyToRank(int strategy)
        {
            switch (strategy)
            {
                case 5: return 4;
                case 6: return 5;
                case 7: return 5;
                default: return strategy;
            }
        }

        public static string StrategyToTier(int strategy)
        {
            switch (strategy)
            {
                case 5: return "H";
                case 7: return "H";
                default: return "N";
            }
        }

        // Xoay (Rotate) và Lật (Mirror) ma trận bản đồ để tạo biến thể mới.
        // Trả về tuple gồm Bản đồ và Đáp án đã được Xoay/Lật.
        public static (int[][] rm, int[] sol) ApplyTransform(int[][] regionMap, int[] solution, int sz, int t)
        {
            int[][] rm = Clone2DArray(regionMap, sz);
            int[] sol = (int[])solution.Clone();
            
            int mirror = t / 4;  // Trục lật
            int rot = t % 4;     // Góc xoay

            // Xử lý Lật (Mirror)
            if (mirror == 1)
            {
                int[][] newRm = new int[sz][];
                for (int r = 0; r < sz; r++)
                {
                    newRm[r] = new int[sz];
                    for (int c = 0; c < sz; c++)
                    {
                        newRm[r][c] = rm[r][sz - 1 - c];
                    }
                }
                int[] newSol = new int[sz];
                for (int r = 0; r < sz; r++) newSol[r] = sz - 1 - sol[r];
                rm = newRm; sol = newSol;
            }
            else if (mirror == 2)
            {
                int[][] newRm = new int[sz][];
                for (int r = 0; r < sz; r++)
                {
                    newRm[r] = (int[])rm[sz - 1 - r].Clone();
                }
                int[] newSol = (int[])sol.Clone();
                System.Array.Reverse(newSol);
                rm = newRm; sol = newSol;
            }

            // Xử lý Xoay (Rotate)
            for (int i = 0; i < rot; i++)
            {
                int[][] newRm = new int[sz][];
                for (int r2 = 0; r2 < sz; r2++)
                {
                    newRm[r2] = new int[sz];
                    for (int c2 = 0; c2 < sz; c2++)
                    {
                        newRm[r2][c2] = rm[sz - 1 - c2][r2];
                    }
                }
                int[] newSol = new int[sz];
                for (int r2 = 0; r2 < sz; r2++) newSol[sol[r2]] = sz - 1 - r2;
                rm = newRm; sol = newSol;
            }

            return (rm, sol);
        }

        // Sao chép (clone) mảng 2 chiều để tránh lỗi Reference trong C#.
        private static int[][] Clone2DArray(int[][] source, int sz)
        {
            int[][] dest = new int[sz][];
            for (int i = 0; i < sz; i++)
            {
                dest[i] = (int[])source[i].Clone();
            }
            return dest;
        }
    }
}
