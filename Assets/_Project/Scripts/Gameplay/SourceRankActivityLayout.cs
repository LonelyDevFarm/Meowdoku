using System;

namespace Meowdoku.Gameplay
{
    public readonly struct SourceRankActivityPageLayoutResult
    {
        public SourceRankActivityPageLayoutResult(
            float headerTop,
            float podiumTop,
            float listTop,
            float listBottomInset,
            float ctaBottomInset,
            float headerAdaptiveHeight)
        {
            HeaderTop = headerTop;
            PodiumTop = podiumTop;
            ListTop = listTop;
            ListBottomInset = listBottomInset;
            CtaBottomInset = ctaBottomInset;
            HeaderAdaptiveHeight = headerAdaptiveHeight;
        }

        public float HeaderTop { get; }
        public float PodiumTop { get; }
        public float ListTop { get; }
        public float ListBottomInset { get; }
        public float CtaBottomInset { get; }
        public float HeaderAdaptiveHeight { get; }
    }

    public readonly struct SourceRankActivityChangeLayoutResult
    {
        public SourceRankActivityChangeLayoutResult(
            float encourageTop,
            float titleTop,
            float countdownTop,
            float listTop,
            float listBottomInset,
            float tapBottomInset)
        {
            EncourageTop = encourageTop;
            TitleTop = titleTop;
            CountdownTop = countdownTop;
            ListTop = listTop;
            ListBottomInset = listBottomInset;
            TapBottomInset = tapBottomInset;
        }

        public float EncourageTop { get; }
        public float TitleTop { get; }
        public float CountdownTop { get; }
        public float ListTop { get; }
        public float ListBottomInset { get; }
        public float TapBottomInset { get; }
    }

    /// <summary>
    /// Pure geometry and scrolling values copied from the source Rank scenes.
    /// Values use top/bottom distances in the 1080-wide canvas space.
    /// </summary>
    public static class SourceRankActivityLayout
    {
        public const float PageHeaderHeight = 184f;
        public const float PagePodiumHeight = 521f;
        public const float PageListWidth = 1008f;
        public const float PageCtaWidth = 784f;
        public const float PageCtaHeight = 258f;
        public const float RowHeight = 180f;
        public const float RowSpacing = 20f;
        public const float ChangeListVerticalPadding = 200f;

        public static SourceRankActivityPageLayoutResult CalculatePage(
            float viewportHeight,
            float topInset = 0f,
            float bottomInset = 0f)
        {
            ValidateViewport(viewportHeight);
            topInset = Math.Max(0f, topInset);
            bottomInset = Math.Max(0f, bottomInset);
            float adaptive = topInset > 0f
                ? 0f
                : SourceGameplayPageLayout.HeaderAdaptiveMinimumFor(viewportHeight);
            float headerTop = topInset + adaptive;
            return new SourceRankActivityPageLayoutResult(
                headerTop,
                headerTop + 245f,
                headerTop + 795f,
                bottomInset + 388f,
                bottomInset + 130f,
                adaptive);
        }

        public static SourceRankActivityChangeLayoutResult CalculateChange(
            float viewportHeight,
            float topInset = 0f,
            float bottomInset = 0f)
        {
            ValidateViewport(viewportHeight);
            topInset = Math.Max(0f, topInset);
            bottomInset = Math.Max(0f, bottomInset);
            return new SourceRankActivityChangeLayoutResult(
                topInset - 9f,
                topInset + 248f,
                topInset + 428f,
                topInset + 620f,
                bottomInset + 620f,
                bottomInset + 245f);
        }

        public static float CenteredScrollOffset(
            float rowTopWithoutPadding,
            float rowHeight,
            float viewportHeight)
        {
            return Math.Max(
                0f,
                rowTopWithoutPadding + ChangeListVerticalPadding +
                rowHeight * 0.5f - viewportHeight * 0.5f);
        }

        public static float RiseDuration(int advance)
        {
            return Math.Max(1f, Math.Min(3f, 1f + 0.05f * Math.Max(0, advance)));
        }

        private static void ValidateViewport(float viewportHeight)
        {
            if (viewportHeight <= 0f)
                throw new ArgumentOutOfRangeException(nameof(viewportHeight));
        }
    }
}
