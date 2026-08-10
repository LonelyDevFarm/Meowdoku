using Meowdoku.Core.UI;
using Meowdoku.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Meowdoku.Tests.EditMode
{
    public sealed class HowToPlayContractTests
    {
        [Test]
        public void FullPage_DemoMatrixAndSourceTimingsMatch()
        {
            Assert.That(HowToPlayContract.FullDemos.Count, Is.EqualTo(3));
            Assert.That(HowToPlayContract.FullRows, Is.EqualTo(3));
            Assert.That(HowToPlayContract.FullColumns, Is.EqualTo(5));
            Assert.That(HowToPlayContract.FullDemos[0].Colors[0],
                Is.EqualTo("BBBBP"));
            Assert.That(HowToPlayContract.FullDemos[0].HasError, Is.True);
            Assert.That(HowToPlayContract.FullDemos[0].ErrorFrame,
                Is.EqualTo(72));
            Assert.That(HowToPlayContract.FullDemos[0].Waves[0].StartFrame,
                Is.EqualTo(134));
            Assert.That(HowToPlayContract.FullDemos[2].Waves[0].Cells.Count,
                Is.EqualTo(8));
            Assert.That(HowToPlayContract.FullCrossStepFrames, Is.EqualTo(5));
            Assert.That(HowToPlayContract.FullStartDelayFrames, Is.EqualTo(6));
            Assert.That(HowToPlayContract.FullGapFrames, Is.EqualTo(12));
            Assert.That(HowToPlayContract.FullLastGapFrames, Is.EqualTo(24));
            Assert.That(HowToPlayContract.DemoDisappearSeconds,
                Is.EqualTo(0.1f));
        }

        [Test]
        public void PagedPage_ThreeSourceRulesAndTimingMatch()
        {
            Assert.That(HowToPlayContract.PagedDemos.Count, Is.EqualTo(3));
            Assert.That(HowToPlayContract.PagedDemos[0].CaptionKey,
                Is.EqualTo("GAME_RULE_ONE_PER_COLOR"));
            Assert.That(HowToPlayContract.PagedDemos[1].CaptionKey,
                Is.EqualTo("GAME_RULE_ONE_PER_LINE"));
            Assert.That(HowToPlayContract.PagedDemos[2].CaptionKey,
                Is.EqualTo("GAME_RULE_NO_TOUCH"));
            Assert.That(HowToPlayContract.PagedDemos[0].Waves[0].StartFrame,
                Is.EqualTo(163));
            Assert.That(HowToPlayContract.PagedCrossStepFrames, Is.EqualTo(6));
            Assert.That(HowToPlayContract.PagedStartDelayFrames, Is.EqualTo(6));
            Assert.That(HowToPlayContract.PagedHoldAfterSeconds,
                Is.EqualTo(1.6f));
            Assert.That(HowToPlayContract.PagedSlideSeconds,
                Is.EqualTo(16f / 60f));
        }

        [Test]
        public void PagedBoardScale_UsesLargestDimensionLikeSource()
        {
            Assert.That(HowToPlayContract.PagedBoardScale(4, 4),
                Is.EqualTo(1.875f));
            Assert.That(HowToPlayContract.PagedBoardScale(5, 5),
                Is.EqualTo(1.5f));
        }

        [Test]
        public void PaletteCodesAndRuleHighlightsMatchSource()
        {
            Assert.That(HowToPlayContract.PaletteIndex('B'), Is.EqualTo(8));
            Assert.That(HowToPlayContract.PaletteIndex('P'), Is.EqualTo(1));
            Assert.That(HowToPlayContract.PaletteIndex('Y'), Is.EqualTo(5));
            Assert.That(HowToPlayContract.HighlightKeyword(
                    "GAME_RULE_ONE_PER_LINE",
                    "en-US"),
                Is.EqualTo("column and row"));
            Assert.That(HowToPlayContract.HighlightKeyword(
                    "GAME_RULE_NO_TOUCH",
                    "zh_TW"),
                Is.EqualTo("相邻"));
            Assert.That(HowToPlayContract.HighlightKeyword(
                    "GAME_RULE_NO_TOUCH",
                    "vi"),
                Is.Empty);
        }

        [Test]
        public void GeneratedPrefabs_ContainFixedSourceBoardCounts()
        {
            GameObject full = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/HowToPlayPage.prefab");
            GameObject paged = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/HowToPlayPagedPage.prefab");
            Assert.That(full, Is.Not.Null);
            Assert.That(paged, Is.Not.Null);
            Assert.That(full.GetComponent<HowToPlayPagePresenter>(), Is.Not.Null);
            Assert.That(
                paged.GetComponent<HowToPlayPagedPagePresenter>(),
                Is.Not.Null);

            HowToPlayDemoBoardView[] fullBoards =
                full.GetComponentsInChildren<HowToPlayDemoBoardView>(true);
            HowToPlayDemoBoardView[] pagedBoards =
                paged.GetComponentsInChildren<HowToPlayDemoBoardView>(true);
            Assert.That(fullBoards.Length, Is.EqualTo(3));
            Assert.That(pagedBoards.Length, Is.EqualTo(3));
            Assert.That(TotalCells(fullBoards), Is.EqualTo(45));
            Assert.That(TotalCells(pagedBoards), Is.EqualTo(57));
        }

        private static int TotalCells(HowToPlayDemoBoardView[] boards)
        {
            int count = 0;
            for (int index = 0; index < boards.Length; index++)
                count += boards[index].CellCount;
            return count;
        }
    }
}
