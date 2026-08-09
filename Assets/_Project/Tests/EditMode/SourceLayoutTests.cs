using Meowdoku.Gameplay;
using Meowdoku.Core.Config;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class SourceLayoutTests
    {
        [TestCase(4, 462)]
        [TestCase(5, 570)]
        [TestCase(8, 894)]
        [TestCase(9, 1002)]
        [TestCase(10, 1110)]
        public void BoardIntrinsicSize_MatchesGodotFormula(int size, int expected)
        {
            Assert.That(SourceBoardLayout.IntrinsicSizeFor(size), Is.EqualTo(expected));
        }

        [TestCase(4)]
        [TestCase(7)]
        [TestCase(10)]
        public void BoardScale_AlwaysProducesFixedVisibleWidth(int size)
        {
            float visible = SourceBoardLayout.IntrinsicSizeFor(size) *
                            SourceBoardLayout.ScaleFor(size);
            Assert.That(visible, Is.EqualTo(SourceBoardLayout.FixedBoardWidth).Within(0.001f));
        }

        [TestCase(GameGridUiConfig.ValueNormal, 15, 4, 30, 462)]
        [TestCase(GameGridUiConfig.ValueSingleLine, 3, 1, 30, 414)]
        [TestCase(GameGridUiConfig.ValueReduceSpacing, 4, 1, 10, 416)]
        [TestCase(GameGridUiConfig.ValueDifferentCorners, 6, 2, 30, 428)]
        public void GridUiLayout_MatchesSourceSolveLocalLayout(
            int value,
            int padding,
            int gap,
            int backgroundCorner,
            int intrinsicSize)
        {
            var config = new GameGridUiConfig();
            config.SetDebugOverride(value);
            SourceBoardLayout.GridLayout layout = SourceBoardLayout.Resolve(4, config);

            Assert.That(layout.Padding, Is.EqualTo(padding));
            Assert.That(layout.Gap, Is.EqualTo(gap));
            Assert.That(layout.BackgroundCorner, Is.EqualTo(backgroundCorner));
            Assert.That(layout.IntrinsicSizeFor(4), Is.EqualTo(intrinsicSize));
        }

        [TestCase(4, 12)]
        [TestCase(8, 8)]
        [TestCase(12, 4)]
        [TestCase(13, 10)]
        public void DifferentCornerRadius_MatchesSourceSizeMap(int size, int expected)
        {
            Assert.That(new GameGridUiConfig().DifferenceSizeCellCorners(size),
                Is.EqualTo(expected));
        }

        [TestCase(1920f, 0f)]
        [TestCase(2160f, 32.5f)]
        [TestCase(2400f, 65f)]
        [TestCase(2600f, 65f)]
        public void HeaderAdaptiveMinimum_MatchesSourceHeightInterpolation(
            float viewportHeight,
            float expected)
        {
            Assert.That(SourceGameplayPageLayout.HeaderAdaptiveMinimumFor(viewportHeight),
                Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void SafeArea_KeepsPlacedElementsInsideTopAndBottomInsets()
        {
            const float viewportHeight = 2400f;
            const float topInset = 120f;
            const float bottomInset = 80f;
            SourceGameplayPageLayoutResult result = SourceGameplayPageLayout.Calculate(
                viewportHeight, topInset, bottomInset,
                SourceGameplayPageLayout.BoardHeight, false);

            float safeTop = viewportHeight * 0.5f - topInset;
            float safeBottom = -viewportHeight * 0.5f + bottomInset;
            Assert.That(result.HeaderCenterY + 60f, Is.LessThanOrEqualTo(safeTop + 0.001f));
            Assert.That(result.BoardCenterY - 504f, Is.GreaterThanOrEqualTo(safeBottom - 0.001f));
        }

        [TestCase(7, false, 1008f)]
        [TestCase(8, false, 1008f)]
        [TestCase(7, true, 1008f)]
        [TestCase(8, true, 1050.003f)]
        [TestCase(10, true, 1050.003f)]
        public void BoardEnlarge_UsesSourceThresholdAndFactor(
            int size,
            bool enlarged,
            float expected)
        {
            var config = new BoardSizeBigConfig();
            if (enlarged) config.SetDebugOverride(BoardSizeBigConfig.ValueEnlarged);
            Assert.That(SourceBoardLayout.TargetVisibleWidthFor(size, config),
                Is.EqualTo(expected).Within(0.01f));
        }

        [Test]
        public void DefaultPageLayout_MatchesSourceVBoxAt1080By1920()
        {
            SourceGameplayPageLayoutResult result =
                SourceGameplayPageLayout.Calculate(1920f);

            Assert.That(result.HeaderCenterY, Is.EqualTo(885.44f).Within(0.02f));
            Assert.That(result.CatHeartCenterY, Is.EqualTo(757.06f).Within(0.02f));
            Assert.That(result.RuleCenterY, Is.EqualTo(616.45f).Within(0.02f));
            Assert.That(result.BoardCenterY, Is.EqualTo(-5.22f).Within(0.02f));
            Assert.That(result.RuleCenterY -
                        (result.BoardCenterY + SourceGameplayPageLayout.BoardHeight * 0.5f),
                Is.EqualTo(117.67f).Within(0.02f));
        }
    }
}
