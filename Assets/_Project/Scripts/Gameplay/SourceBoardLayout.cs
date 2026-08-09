using System;
using Meowdoku.Core.Config;

namespace Meowdoku.Gameplay
{
    /// <summary>
    /// Source-default measurements from board_view.gd and base_game_page.gd.
    /// Values stay in the board's local coordinate space; the complete board is
    /// uniformly scaled to the fixed visible width used by the Godot page.
    /// </summary>
    public static class SourceBoardLayout
    {
        public const int BoardPadding = 15;
        public const int CellPixels = 100;
        public const int CellGap = 4;
        public const int GridSlot = CellPixels + 2 * CellGap;
        public const int FixedBoardWidth = 1008;
        public const int BoardCornerRadius = 30;
        public const float BoardEnlargeFactor = 1.04167f;
        public const int BoardEnlargeMinimumSize = 8;

        public readonly struct GridLayout
        {
            public GridLayout(int padding, int gap, int backgroundCorner)
            {
                Padding = padding;
                Gap = gap;
                Slot = CellPixels + 2 * gap;
                BackgroundCorner = backgroundCorner;
            }

            public int Padding { get; }
            public int Gap { get; }
            public int Slot { get; }
            public int BackgroundCorner { get; }
            public int IntrinsicSizeFor(int puzzleSize) => Slot * puzzleSize + Padding * 2;
            public float ScaleFor(int puzzleSize, float targetWidth = FixedBoardWidth) =>
                targetWidth / IntrinsicSizeFor(puzzleSize);
        }

        public static GridLayout Resolve(int puzzleSize, GameGridUiConfig config)
        {
            int value = config?.Value ?? GameGridUiConfig.ValueNormal;
            int sourcePadding;
            int sourceGap;
            int backgroundCorner;
            switch (value)
            {
                case GameGridUiConfig.ValueSingleLine:
                    sourcePadding = 7;
                    sourceGap = 2;
                    backgroundCorner = 30;
                    break;
                case GameGridUiConfig.ValueReduceSpacing:
                    sourcePadding = 9;
                    sourceGap = 3;
                    backgroundCorner = 10;
                    break;
                case GameGridUiConfig.ValueDifferentCorners:
                    sourcePadding = 15;
                    sourceGap = 5;
                    backgroundCorner = 30;
                    break;
                default:
                    return new GridLayout(BoardPadding, CellGap, BoardCornerRadius);
            }

            if (puzzleSize <= 0)
                return new GridLayout(sourcePadding, sourceGap, backgroundCorner);

            float scale = (FixedBoardWidth - 2f * sourcePadding -
                           2f * puzzleSize * sourceGap) /
                          (CellPixels * (float)puzzleSize);
            if (scale <= 0f)
                return new GridLayout(sourcePadding, sourceGap, backgroundCorner);

            int localGap = (int)Math.Round(sourceGap / scale);
            int localPadding = (int)Math.Round(sourcePadding / scale);
            return new GridLayout(localPadding, localGap, backgroundCorner);
        }

        public static int IntrinsicSizeFor(int puzzleSize)
        {
            if (puzzleSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(puzzleSize));
            return GridSlot * puzzleSize + BoardPadding * 2;
        }

        public static float ScaleFor(int puzzleSize)
        {
            return FixedBoardWidth / (float)IntrinsicSizeFor(puzzleSize);
        }

        public static float TargetVisibleWidthFor(
            int puzzleSize,
            BoardSizeBigConfig config)
        {
            return config != null && config.IsEnlarged() &&
                   puzzleSize >= BoardEnlargeMinimumSize
                ? FixedBoardWidth * BoardEnlargeFactor
                : FixedBoardWidth;
        }
    }
}
