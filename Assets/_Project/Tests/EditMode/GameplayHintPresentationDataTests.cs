using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Gameplay;
using Meowdoku.Gameplay.Input;
using NUnit.Framework;
using UnityEngine;

namespace Meowdoku.Tests.EditMode
{
    public sealed class GameplayHintPresentationDataTests
    {
        [Test]
        public void WrongMark_UsesSourceDescriptionAndCell()
        {
            var request = new SessionHintRequest
            {
                Found = true,
                WrongMark = true,
                WrongMarkCell = new Vector2Int(2, 1)
            };

            GameplayHintPresentationData data = GameplayHintPresentationData.Build(
                request, 4, null, null);

            Assert.That(data.WrongMark, Is.True);
            Assert.That(data.DescriptionKey, Is.EqualTo("HINT_WRONG_MARK"));
            Assert.That(data.HighlightCells, Is.EqualTo(new[] { new Vector2Int(2, 1) }));
        }

        [Test]
        public void R2_PreviewsAreSortedAndUsePointOneSecondStagger()
        {
            int[][] regions =
            {
                new[] { 0, 0, 1, 1 },
                new[] { 0, 2, 2, 1 },
                new[] { 3, 2, 2, 1 },
                new[] { 3, 3, 3, 0 }
            };
            var hint = new HintResult
            {
                Found = true,
                Strategy = "R2",
                Mode = "r2a_row",
                Row = 0,
                Region = 0,
                Cell = new Vector2Int(0, 1)
            };
            var request = new SessionHintRequest { Found = true, Hint = hint };

            GameplayHintPresentationData data = GameplayHintPresentationData.Build(
                request, 4, regions, new BlankBoard());

            Assert.That(data.StrategyLabel, Is.EqualTo("R2"));
            Assert.That(data.DescriptionKey, Is.EqualTo("HINT_REGION_CONSTRAINT"));
            Assert.That(data.MarkPreviews.Count, Is.EqualTo(2));
            Assert.That(data.MarkPreviews[0].Position, Is.EqualTo(new Vector2Int(0, 2)));
            Assert.That(data.MarkPreviews[1].DelaySeconds, Is.EqualTo(0.1f).Within(0.0001f));
        }

        [Test]
        public void ChainDetail_IsOnlyAvailableWhenSourceHasSteps()
        {
            var chain = new HintChainDetail();
            chain.Steps.Add(new Vector2Int(1, 1));
            var hint = new HintResult
            {
                Found = true,
                Strategy = "R5_chain",
                Chain = chain,
                Cell = new Vector2Int(1, 1)
            };

            GameplayHintPresentationData data = GameplayHintPresentationData.Build(
                new SessionHintRequest { Found = true, Hint = hint },
                4,
                null,
                null);

            Assert.That(data.StrategyLabel, Is.EqualTo("R5"));
            Assert.That(data.HasChainDetail, Is.True);
            Assert.That(data.DescriptionKey, Is.EqualTo("HINT_CONTRADICTION"));
        }

        [Test]
        public void EnglishFallback_UsesSourceTranslationInsteadOfDisplayingKey()
        {
            Assert.That(
                GameplayHintOverlayPresenter.ResolveEnglishSourceText("HINT_WRONG_MARK"),
                Is.EqualTo(
                    "You've incorrectly marked this cell! Tap to remove the X mark."));
            Assert.That(
                GameplayHintOverlayPresenter.ResolveEnglishSourceText(
                    "HINT_REGION_CONSTRAINT"),
                Is.EqualTo("Region Constraint: Exclude related cells"));
        }

        private sealed class BlankBoard : IBoardStateReader
        {
            public CellStateType GetCellState(int row, int column)
            {
                return CellStateType.EMPTY;
            }
        }
    }
}
