using System;

namespace Meowdoku.Gameplay
{
    public readonly struct SourceGameplayPageLayoutResult
    {
        public SourceGameplayPageLayoutResult(
            float headerCenterY,
            float catHeartCenterY,
            float ruleCenterY,
            float boardCenterY,
            float bottomToolsCenterY)
        {
            HeaderCenterY = headerCenterY;
            CatHeartCenterY = catHeartCenterY;
            RuleCenterY = ruleCenterY;
            BoardCenterY = boardCenterY;
            BottomToolsCenterY = bottomToolsCenterY;
        }

        public float HeaderCenterY { get; }
        public float CatHeartCenterY { get; }
        public float RuleCenterY { get; }
        public float BoardCenterY { get; }
        public float BottomToolsCenterY { get; }
    }

    /// <summary>
    /// Vertical positions produced by the source board_no_fuction VBox profile.
    /// Coordinates returned here use Unity's centered, positive-up convention.
    /// </summary>
    public static class SourceGameplayPageLayout
    {
        public const float RuleBarHeight = 170f;
        public const float BoardHeight = 1008f;
        public const float DesignWidth = 1080f;
        public const float DesignHeight = 2400f;
        public const float MinimumAdaptiveHeight = 1920f;

        private const float HeaderHeight = 120f;
        private const float HeaderAdaptMaximum = 65f;
        private const float CatAdaptMinimum = 4f;
        private const float CatHeartHeight = 88f;
        private const float RuleAdaptMinimum = 4f;
        private const float BoardAdaptMinimum = 4f;
        private const float BottomToolsHeight = 200f;
        private const float AdBannerHeight = 180f;

        private const float HeaderAdaptRatio = 65f;
        private const float CatAdaptRatio = 91f;
        private const float RuleAdaptRatio = 34f;
        private const float BoardAdaptRatio = 128f;
        private const float FunctionAdaptRatio = 16f;
        private const float BottomAdaptRatio = 190f;
        private const float AdAdaptRatio = 70f;
        private const float AdDownAdaptRatio = 40f;

        public static SourceGameplayPageLayoutResult Calculate(float viewportHeight)
        {
            return Calculate(viewportHeight, 0f, 0f, BoardHeight, false);
        }

        public static SourceGameplayPageLayoutResult Calculate(
            float viewportHeight,
            float topInset,
            float bottomInset,
            float visibleBoardHeight,
            bool enlargedProfile)
        {
            if (viewportHeight <= 0f)
                throw new ArgumentOutOfRangeException(nameof(viewportHeight));

            topInset = Math.Max(0f, topInset);
            bottomInset = Math.Max(0f, bottomInset);
            float safeHeight = Math.Max(0f, viewportHeight - topInset - bottomInset);
            bool collapseHeaderAdapt = topInset > 0f;
            float headerAdaptMinimum = collapseHeaderAdapt
                ? 0f
                : HeaderAdaptiveMinimumFor(viewportHeight);
            float headerRatio = collapseHeaderAdapt
                ? 0f
                : enlargedProfile ? 66f : HeaderAdaptRatio;
            float catRatio = enlargedProfile ? 93f : CatAdaptRatio;
            float ruleRatio = enlargedProfile ? 35f : RuleAdaptRatio;
            float boardRatio = enlargedProfile ? 107f : BoardAdaptRatio;
            float bottomRatio = enlargedProfile ? 169f : BottomAdaptRatio;
            float ruleMinimum = enlargedProfile ? 0f : RuleAdaptMinimum;
            float functionMinimum = enlargedProfile ? 4f : 0f;
            float bottomMinimum = enlargedProfile ? 2f : 0f;
            float boardContainerHeight = Math.Max(BoardHeight, visibleBoardHeight);

            float fixedMinimumHeight = headerAdaptMinimum + HeaderHeight +
                CatAdaptMinimum + CatHeartHeight + ruleMinimum + RuleBarHeight +
                BoardAdaptMinimum + boardContainerHeight + functionMinimum +
                bottomMinimum + BottomToolsHeight + AdBannerHeight;
            float totalStretchRatio = headerRatio + catRatio + ruleRatio + boardRatio +
                FunctionAdaptRatio + bottomRatio + AdAdaptRatio + AdDownAdaptRatio;
            float extra = Math.Max(0f, safeHeight - fixedMinimumHeight);
            float unit = totalStretchRatio > 0f ? extra / totalStretchRatio : 0f;
            float headerAdapt = headerAdaptMinimum + headerRatio * unit;
            float catAdapt = CatAdaptMinimum + catRatio * unit;
            float ruleAdapt = ruleMinimum + ruleRatio * unit;
            float boardAdapt = BoardAdaptMinimum + boardRatio * unit;
            float functionAdapt = functionMinimum + FunctionAdaptRatio * unit;
            float bottomAdapt = bottomMinimum + bottomRatio * unit;

            float ruleTop = headerAdapt + HeaderHeight + catAdapt +
                            CatHeartHeight + ruleAdapt;
            float headerCenterFromTop = headerAdapt + HeaderHeight * 0.5f;
            float catHeartTop = headerAdapt + HeaderHeight + catAdapt;
            float boardTop = ruleTop + RuleBarHeight + boardAdapt;
            float halfViewport = viewportHeight * 0.5f;
            float headerCenterY = halfViewport - topInset - headerCenterFromTop;
            float catHeartCenterY = halfViewport - topInset -
                                    (catHeartTop + CatHeartHeight * 0.5f);
            float ruleCenterY = halfViewport - topInset -
                                (ruleTop + RuleBarHeight * 0.5f);
            float boardCenterY = halfViewport - topInset -
                                 (boardTop + boardContainerHeight * 0.5f);
            float bottomToolsTop = boardTop + boardContainerHeight +
                                   functionAdapt + bottomAdapt;
            float bottomToolsCenterY = halfViewport - topInset -
                                       (bottomToolsTop + BottomToolsHeight * 0.5f);
            return new SourceGameplayPageLayoutResult(
                headerCenterY,
                catHeartCenterY,
                ruleCenterY,
                boardCenterY,
                bottomToolsCenterY);
        }

        public static float HeaderAdaptiveMinimumFor(float viewportHeight)
        {
            float t = (viewportHeight - MinimumAdaptiveHeight) /
                      (DesignHeight - MinimumAdaptiveHeight);
            t = Math.Max(0f, Math.Min(1f, t));
            return HeaderAdaptMaximum * t;
        }
    }
}
