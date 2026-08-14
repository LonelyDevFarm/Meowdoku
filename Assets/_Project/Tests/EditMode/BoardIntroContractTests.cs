using Meowdoku.Gameplay;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class BoardIntroContractTests
    {
        private const float Tolerance = 0.0001f;

        [TestCase(4, 0.82f)]
        [TestCase(10, 1.30f)]
        public void InputReadyDuration_MatchesSourceBoundary(int size, float expected)
        {
            Assert.That(BoardView.CalculateGridIntroInputReadyDuration(size),
                Is.EqualTo(expected).Within(Tolerance));
        }

        [TestCase(4, 3, 0, 0.18f)]
        [TestCase(4, 2, 0, 0.22f)]
        [TestCase(4, 3, 1, 0.22f)]
        [TestCase(4, 0, 3, 0.42f)]
        public void CellDelay_MatchesSourceBottomLeftToTopRightDiagonalOrder(
            int size, int row, int column, float expected)
        {
            Assert.That(BoardView.CalculateGridIntroCellDelay(size, row, column),
                Is.EqualTo(expected).Within(Tolerance));
        }

        [TestCase(4, false, 1.42f)]
        [TestCase(4, true, 0.82f)]
        [TestCase(10, false, 1.90f)]
        public void VisualDuration_MatchesSourceCompletion(
            int size, bool singleLine, float expected)
        {
            Assert.That(BoardView.CalculateGridIntroVisualDuration(size, singleLine),
                Is.EqualTo(expected).Within(Tolerance));
        }
    }
}
