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
            Assert.That(SourceRankActivityLayout.PageWidth,
                Is.EqualTo(1080f));
        }

        [Test]
        public void RankPage_RuntimeLayoutRestoresSourceHeaderCoordinateSpace()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/RankActivityPage.prefab");
            Assert.That(prefab, Is.Not.Null);
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                RankActivityPageLayoutPresenter layout =
                    instance.GetComponent<RankActivityPageLayoutPresenter>();
                RectTransform header = instance.transform.Find(
                    "Root/Header") as RectTransform;
                RectTransform podium = instance.transform.Find(
                    "Root/Podium") as RectTransform;
                Assert.That(layout, Is.Not.Null);
                Assert.That(header, Is.Not.Null);
                Assert.That(podium, Is.Not.Null);

                layout.ApplyLayoutForTests(1920f, 0f, 0f);

                Assert.That(header.anchorMin,
                    Is.EqualTo(new Vector2(0.5f, 1f)));
                Assert.That(header.anchorMax,
                    Is.EqualTo(new Vector2(0.5f, 1f)));
                Assert.That(header.sizeDelta,
                    Is.EqualTo(new Vector2(1080f, 184f)));
                Assert.That(podium.sizeDelta,
                    Is.EqualTo(new Vector2(1080f, 521f)));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
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

            RectTransform rowContent = row.transform.Find(
                "VisualRoot/CanvasGroup/Content") as RectTransform;
            RectTransform avatar = rowContent?.Find(
                "AvatarSlot") as RectTransform;
            RectTransform name = rowContent?.Find(
                "NameLabel") as RectTransform;
            RectTransform score = rowContent?.Find("Score") as RectTransform;
            RectTransform chest = rowContent?.Find("Chest") as RectTransform;
            Assert.That(rowContent, Is.Not.Null);
            Assert.That(rowContent.anchoredPosition,
                Is.EqualTo(new Vector2(110f, -10f)));
            Assert.That(rowContent.sizeDelta,
                Is.EqualTo(new Vector2(838f, 160f)));
            Assert.That(avatar.sizeDelta,
                Is.EqualTo(new Vector2(185f, 185f)));
            Assert.That(avatar.localScale.x,
                Is.EqualTo(146f / 185f).Within(0.0001f));
            Assert.That(avatar.localScale.y,
                Is.EqualTo(146f / 185f).Within(0.0001f));
            Assert.That(avatar.anchoredPosition,
                Is.EqualTo(new Vector2(7f, -7f)));
            Assert.That(row.transform.Find(
                "VisualRoot/CanvasGroup/FloatingOccluder"), Is.Not.Null);
            Assert.That(name.anchoredPosition,
                Is.EqualTo(new Vector2(190f, -5f)));
            Assert.That(score.anchoredPosition,
                Is.EqualTo(new Vector2(490f, -40f)));
            Assert.That(chest.anchoredPosition,
                Is.EqualTo(new Vector2(723f, -19f)));

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
            RectTransform header = page.transform.Find("Root/Header") as RectTransform;
            RectTransform list = page.transform.Find("Root/List") as RectTransform;
            RectTransform cta = page.transform.Find("Root/CtaButton") as RectTransform;
            RectTransform viewport = page.transform.Find(
                "Root/List/Viewport") as RectTransform;
            VerticalLayoutGroup pageRows = page.transform.Find(
                    "Root/List/Viewport/Rows")
                .GetComponent<VerticalLayoutGroup>();
            ScrollRect pageScroll = list.GetComponent<ScrollRect>();
            Assert.That(podium.sizeDelta.y, Is.EqualTo(521f));
            Assert.That(header, Is.Not.Null);
            Assert.That(page.transform.Find("Root/Header/LeftFish"), Is.Not.Null);
            Assert.That(page.transform.Find("Root/Header/RightFish"), Is.Not.Null);
            RectTransform back = page.transform.Find(
                "Root/Header/BackBtn") as RectTransform;
            RectTransform settings = page.transform.Find(
                "Root/Header/SettingsBtn") as RectTransform;
            Assert.That(back.sizeDelta, Is.EqualTo(new Vector2(120f, 120f)));
            Assert.That(settings.sizeDelta,
                Is.EqualTo(new Vector2(120f, 120f)));
            Assert.That(back.Find("Base")?.GetComponent<RawImage>(), Is.Not.Null);
            Assert.That(back.Find("Icon")?.GetComponent<RawImage>(), Is.Not.Null);
            Assert.That(settings.Find("Base")?.GetComponent<RawImage>(),
                Is.Not.Null);
            Assert.That(settings.Find("Icon")?.GetComponent<RawImage>(),
                Is.Not.Null);
            Assert.That(
                back.Find("Icon").GetComponent<RawImage>().texture.name,
                Is.EqualTo("icon_back"));
            Assert.That(
                settings.Find("Icon").GetComponent<RawImage>().texture.name,
                Is.EqualTo("icon_info"));
            for (int place = 1; place <= 3; place++)
            {
                string branch = place == 1
                    ? "First"
                    : place == 2 ? "Second" : "Third";
                Transform podiumBranch = page.transform.Find(
                    "Root/Podium/" + branch);
                Assert.That(podiumBranch, Is.Not.Null);
                Assert.That(podiumBranch.Find("AvatarGroup"), Is.Not.Null);
                Assert.That(podiumBranch.Find(
                    "MedalBadge/RankNumber"), Is.Not.Null);
                Assert.That(podiumBranch.Find(
                    "Info/Score/CountBg"), Is.Not.Null);
            }
            Assert.That(list.sizeDelta, Is.EqualTo(new Vector2(1008f, -1183f)));
            Assert.That(cta.sizeDelta, Is.EqualTo(new Vector2(784f, 258f)));
            Image ctaImage = cta.GetComponent<Image>();
            Assert.That(ctaImage.sprite, Is.Not.Null);
            Assert.That(ctaImage.sprite.name, Is.EqualTo("btn_primary_0"));
            Assert.That(ctaImage.preserveAspect, Is.True);
            Assert.That(viewport.offsetMin, Is.EqualTo(new Vector2(0f, 18f)));
            Assert.That(viewport.offsetMax, Is.EqualTo(new Vector2(0f, -20f)));
            Assert.That(viewport.GetComponent<RectMask2D>(),
                Is.Not.Null.And.Property("enabled").True);
            Mask pageLegacyMask = viewport.GetComponent<Mask>();
            Assert.That(pageLegacyMask == null || !pageLegacyMask.enabled,
                Is.True);
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
            RectTransform changeViewport = change.transform.Find(
                "Root/ListGroup/RankCellMask") as RectTransform;
            VerticalLayoutGroup changeRows = change.transform.Find(
                    "Root/ListGroup/RankCellMask/RowList")
                .GetComponent<VerticalLayoutGroup>();
            ScrollRect changeScroll = changeList.GetComponent<ScrollRect>();
            Assert.That(changeViewport, Is.Not.Null);
            Assert.That(changeViewport.GetComponent<RectMask2D>(),
                Is.Not.Null.And.Property("enabled").True);
            Mask changeLegacyMask = changeViewport.GetComponent<Mask>();
            Assert.That(changeLegacyMask == null || !changeLegacyMask.enabled,
                Is.True);
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

        [Test]
        public void GeneratedRankHowToPlay_UsesSourceZigZagComposition()
        {
            GameObject page = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/RankActivityHowToPlay.prefab");
            Assert.That(page, Is.Not.Null);

            RectTransform step = page.transform.Find(
                "Root/Content/Step") as RectTransform;
            RectTransform cat = page.transform.Find(
                "Root/Content/CollectVisual/IconCat") as RectTransform;
            RectTransform rankList = page.transform.Find(
                "Root/Content/RankList") as RectTransform;
            RectTransform reward = page.transform.Find(
                "Root/Content/RewardFull/TreasureBox") as RectTransform;
            RectTransform arrowToCollect = page.transform.Find(
                "Root/Content/Arrow/ArrowToCollect") as RectTransform;
            RectTransform arrowToRank = page.transform.Find(
                "Root/Content/Arrow/ArrowToRank") as RectTransform;
            RectTransform arrowToReward = page.transform.Find(
                "Root/Content/Arrow/ArrowToReward") as RectTransform;

            Assert.That(step.anchoredPosition,
                Is.EqualTo(new Vector2(153f, -486f)));
            Assert.That(cat.anchoredPosition,
                Is.EqualTo(new Vector2(647f, -791f)));
            Assert.That(rankList.anchoredPosition,
                Is.EqualTo(new Vector2(140f, -1192f)));
            Assert.That(reward.anchoredPosition,
                Is.EqualTo(new Vector2(609f, -1478f)));
            Assert.That(arrowToCollect, Is.Not.Null);
            Assert.That(arrowToRank, Is.Not.Null);
            Assert.That(arrowToReward, Is.Not.Null);
            Assert.That(arrowToCollect.localEulerAngles.z,
                Is.EqualTo(342.8113f).Within(0.01f));
            Assert.That(arrowToRank.localScale.x, Is.EqualTo(-1f));
            Assert.That(page.transform.Find(
                "Root/Content/Step/Cell_9"), Is.Not.Null);
        }
    }
}
