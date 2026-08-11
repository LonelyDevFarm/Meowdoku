using Meowdoku.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Tests.EditMode
{
    public sealed class SourceRankActivityLayoutTests
    {
        [Test]
        public void RankPage_At1920_MatchesSourceVBox()
        {
            SourceRankActivityPageLayoutResult value =
                SourceRankActivityLayout.CalculatePage(1920f);

            Assert.That(value.HeaderAdaptiveHeight, Is.Zero);
            Assert.That(value.HeaderTop, Is.Zero);
            Assert.That(value.PodiumTop, Is.EqualTo(245f));
            Assert.That(value.ListTop, Is.EqualTo(795f));
            Assert.That(value.ListBottomInset, Is.EqualTo(388f));
            Assert.That(1920f - value.ListTop - value.ListBottomInset,
                Is.EqualTo(737f));
            Assert.That(value.CtaBottomInset, Is.EqualTo(130f));
        }

        [Test]
        public void RankPage_At2400_UsesSourceHeaderAdaptHolder()
        {
            SourceRankActivityPageLayoutResult value =
                SourceRankActivityLayout.CalculatePage(2400f);

            Assert.That(value.HeaderAdaptiveHeight, Is.EqualTo(65f));
            Assert.That(value.HeaderTop, Is.EqualTo(65f));
            Assert.That(value.PodiumTop, Is.EqualTo(310f));
            Assert.That(value.ListTop, Is.EqualTo(860f));
            Assert.That(2400f - value.ListTop - value.ListBottomInset,
                Is.EqualTo(1152f));
        }

        [Test]
        public void RankPage_SafeAreaCollapsesHeaderAdaptAndInsetsBothEnds()
        {
            SourceRankActivityPageLayoutResult value =
                SourceRankActivityLayout.CalculatePage(2400f, 120f, 80f);

            Assert.That(value.HeaderAdaptiveHeight, Is.Zero);
            Assert.That(value.HeaderTop, Is.EqualTo(120f));
            Assert.That(value.ListTop, Is.EqualTo(915f));
            Assert.That(value.ListBottomInset, Is.EqualTo(468f));
            Assert.That(value.CtaBottomInset, Is.EqualTo(210f));
        }

        [Test]
        public void RankChange_SafeGroupsMatchSourceOffsets()
        {
            SourceRankActivityChangeLayoutResult value =
                SourceRankActivityLayout.CalculateChange(2400f, 120f, 80f);

            Assert.That(value.EncourageTop, Is.EqualTo(111f));
            Assert.That(value.TitleTop, Is.EqualTo(368f));
            Assert.That(value.CountdownTop, Is.EqualTo(548f));
            Assert.That(value.ListTop, Is.EqualTo(740f));
            Assert.That(value.ListBottomInset, Is.EqualTo(700f));
            Assert.That(value.TapBottomInset, Is.EqualTo(325f));
        }

        [TestCase(0f, 680f, 0f)]
        [TestCase(1800f, 680f, 1750f)]
        public void RankChange_CenterOffsetIncludesSourceVerticalPadding(
            float rowTop,
            float viewportHeight,
            float expected)
        {
            Assert.That(SourceRankActivityLayout.CenteredScrollOffset(
                    rowTop,
                    SourceRankActivityLayout.RowHeight,
                    viewportHeight),
                Is.EqualTo(expected));
        }

        [TestCase(0, 1f)]
        [TestCase(4, 1.2f)]
        [TestCase(50, 3f)]
        public void RankChange_RiseDurationMatchesSourceClamp(
            int advance,
            float expected)
        {
            Assert.That(SourceRankActivityLayout.RiseDuration(advance),
                Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void RankRow_IntroAndShadowTimingMatchesSourceAnimations()
        {
            Assert.That(RankActivityRowView.Appear1Duration,
                Is.EqualTo(0.36666667f).Within(0.000001f));
            Assert.That(RankActivityRowView.Appear2Duration,
                Is.EqualTo(0.42151412f).Within(0.000001f));
            Assert.That(RankActivityRowView.Appear3Duration,
                Is.EqualTo(0.3f).Within(0.000001f));
            Assert.That(RankActivityRowView.IntroFadeDuration,
                Is.EqualTo(0.06666667f).Within(0.000001f));
            Assert.That(RankActivityRowView.PopFadeDuration,
                Is.EqualTo(0.16666667f).Within(0.000001f));
            Assert.That(RankActivityRowView.ShadowFadeDuration,
                Is.EqualTo(0.15f).Within(0.000001f));
            Assert.That(RankActivityRowCelebrationView.CollectionDuration,
                Is.EqualTo(1.6666701f).Within(0.000001f));
            Assert.That(RankActivityRowCelebrationView.RiseUpDuration,
                Is.EqualTo(0.23333333f).Within(0.000001f));
            Assert.That(RankActivityRowCelebrationView.RiseDownDuration,
                Is.EqualTo(0.33333334f).Within(0.000001f));
            Assert.That(RankActivityRowCelebrationView.RiseDownBurstTime,
                Is.EqualTo(0.23333335f).Within(0.000001f));
        }

        [Test]
        public void GeneratedRankRow_HasIndependentVisualRootAndStickyShadow()
        {
            GameObject row = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/RankActivityRow.prefab");
            Assert.That(row, Is.Not.Null);
            RectTransform visual = row.transform.Find(
                "VisualRoot") as RectTransform;
            Image shadow = row.transform.Find(
                    "VisualRoot/Shadow")
                ?.GetComponent<Image>();
            CanvasGroup content = row.transform.Find(
                    "VisualRoot/CanvasGroup")
                ?.GetComponent<CanvasGroup>();
            RankActivityRowCelebrationView celebration = row.transform.Find(
                    "VisualRoot/Effects")
                ?.GetComponent<RankActivityRowCelebrationView>();
            Assert.That(visual, Is.Not.Null);
            Assert.That(shadow, Is.Not.Null);
            Assert.That(content, Is.Not.Null);
            Assert.That(celebration, Is.Not.Null);
            Assert.That(row.transform.Find(
                "VisualRoot/Effects/Collection/CollectItem_6"), Is.Not.Null);
            Assert.That(row.transform.Find(
                "VisualRoot/Effects/Arrow/ArrowParticle_4"), Is.Not.Null);
            Assert.That(row.transform.Find(
                "VisualRoot/Effects/RiseBurst/EdgeStar_12"), Is.Not.Null);
            Assert.That(shadow.raycastTarget, Is.False);
            Assert.That(shadow.rectTransform.sizeDelta,
                Is.EqualTo(new Vector2(1033f, 270f)));
            Assert.That(shadow.rectTransform.localScale.y, Is.EqualTo(-1f));

            SerializedObject data = new(
                row.GetComponent<RankActivityRowView>());
            Assert.That(data.FindProperty("visualRoot").objectReferenceValue,
                Is.EqualTo(visual));
            Assert.That(data.FindProperty("contentGroup").objectReferenceValue,
                Is.EqualTo(content));
            Assert.That(data.FindProperty("selfShadow").objectReferenceValue,
                Is.EqualTo(shadow));
            Assert.That(data.FindProperty("celebration").objectReferenceValue,
                Is.EqualTo(celebration));
            SerializedObject effectData = new(celebration);
            Assert.That(effectData.FindProperty("collectionItems").arraySize,
                Is.EqualTo(6));
            Assert.That(effectData.FindProperty("arrowItems").arraySize,
                Is.EqualTo(4));
            Assert.That(effectData.FindProperty("riseStars").arraySize,
                Is.EqualTo(12));
        }

        [Test]
        public void GeneratedRankPrefabs_KeepSourceGeometryAndScrollComposition()
        {
            GameObject page = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/RankActivityPage.prefab");
            Assert.That(page, Is.Not.Null);
            Assert.That(page.GetComponent<RankActivityPageLayoutPresenter>(),
                Is.Not.Null);
            RectTransform podium = page.transform.Find("Root/Podium") as RectTransform;
            RectTransform list = page.transform.Find("Root/List") as RectTransform;
            RectTransform cta = page.transform.Find("Root/CtaButton") as RectTransform;
            RectTransform viewport = page.transform.Find(
                "Root/List/Viewport") as RectTransform;
            VerticalLayoutGroup pageRows = page.transform.Find(
                    "Root/List/Viewport/Rows")
                .GetComponent<VerticalLayoutGroup>();
            ScrollRect pageScroll = list.GetComponent<ScrollRect>();
            Assert.That(podium.sizeDelta.y, Is.EqualTo(521f));
            Assert.That(list.sizeDelta, Is.EqualTo(new Vector2(1008f, -1183f)));
            Assert.That(cta.sizeDelta, Is.EqualTo(new Vector2(784f, 258f)));
            Assert.That(viewport.offsetMin, Is.EqualTo(new Vector2(0f, 18f)));
            Assert.That(viewport.offsetMax, Is.EqualTo(new Vector2(0f, -20f)));
            Assert.That(pageRows.spacing, Is.EqualTo(20f));
            Assert.That(pageScroll.movementType,
                Is.EqualTo(ScrollRect.MovementType.Clamped));
            Assert.That(page.transform.Find("Root/FloatRow"), Is.Not.Null);
            SerializedObject pagePresenter = new(
                page.GetComponent<RankActivityPagePresenter>());
            Assert.That(pagePresenter.FindProperty("floatingRowLayer")
                .objectReferenceValue, Is.Not.Null);

            GameObject change = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/RankActivityChange.prefab");
            Assert.That(change, Is.Not.Null);
            Assert.That(change.GetComponent<RankActivityChangeLayoutPresenter>(),
                Is.Not.Null);
            RectTransform changeList = change.transform.Find(
                "Root/ListGroup") as RectTransform;
            VerticalLayoutGroup changeRows = change.transform.Find(
                    "Root/ListGroup/RankCellMask/RowList")
                .GetComponent<VerticalLayoutGroup>();
            ScrollRect changeScroll = changeList.GetComponent<ScrollRect>();
            Assert.That(changeRows.padding.top, Is.EqualTo(200));
            Assert.That(changeRows.padding.bottom, Is.EqualTo(200));
            Assert.That(changeRows.spacing, Is.EqualTo(20f));
            Assert.That(changeScroll.movementType,
                Is.EqualTo(ScrollRect.MovementType.Clamped));
            Assert.That(change.transform.Find("Root/PlayerCelebrate"), Is.Not.Null);
            SerializedObject changePresenter = new(
                change.GetComponent<RankActivityChangePresenter>());
            Assert.That(changePresenter.FindProperty("celebrateLayer")
                .objectReferenceValue, Is.Not.Null);
        }
    }
}
