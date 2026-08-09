using Meowdoku.Core;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class CellStateTests
    {
        [TestCase(CellStateType.EMPTY)]
        [TestCase(CellStateType.DRAFT_CROSS)]
        [TestCase(CellStateType.DRAFT_CAT)]
        public void IsBlank_ReturnsTrueForBlankFamily(CellStateType state)
        {
            Assert.That(CellState.IsBlank(state), Is.True);
        }

        [TestCase(CellStateType.CAT)]
        [TestCase(CellStateType.MARK)]
        [TestCase(CellStateType.ERROR)]
        [TestCase(CellStateType.LOCKED_MARK)]
        public void IsBlank_ReturnsFalseForCommittedStates(CellStateType state)
        {
            Assert.That(CellState.IsBlank(state), Is.False);
        }

        [TestCase(CellStateType.MARK)]
        [TestCase(CellStateType.ERROR)]
        [TestCase(CellStateType.LOCKED_MARK)]
        public void IsCross_ReturnsTrueForCrossFamily(CellStateType state)
        {
            Assert.That(CellState.IsCross(state), Is.True);
        }
    }
}
