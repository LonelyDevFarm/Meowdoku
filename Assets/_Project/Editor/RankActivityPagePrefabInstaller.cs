using System;
using Meowdoku.Core.Localization;
using Meowdoku.Core.UI;
using Meowdoku.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Editor
{
    /// <summary>
    /// Source-backed composition for the Rank Activity home entry and opening
    /// dialog. The installer only writes prefab assets through Unity APIs, so
    /// GUIDs and serialized references remain stable across upgrades.
    /// </summary>
    [InitializeOnLoad]
    internal static class RankActivityPagePrefabInstaller
    {
        internal const string OpenPopupPath =
            "Assets/_Project/Prefabs/UI/RankActivityOpenPopup.prefab";
        internal const string PagePath =
            "Assets/_Project/Prefabs/UI/RankActivityPage.prefab";
        internal const string HowToPlayPath =
            "Assets/_Project/Prefabs/UI/RankActivityHowToPlay.prefab";
        internal const string ChangePath =
            "Assets/_Project/Prefabs/UI/RankActivityChange.prefab";
        private const string RowPath =
            "Assets/_Project/Prefabs/UI/RankActivityRow.prefab";
        private const string AvatarPath =
            "Assets/_Project/Prefabs/UI/ProfileAvatarView.prefab";
        private const string HomePath =
            "Assets/_Project/Prefabs/UI/HomePage.prefab";
        private const string RankRoot =
            "Assets/_Project/Sprites/rank_activity/";
        private const string GameRoot = "Assets/_Project/Sprites/game/";
        private const string CommonRoot = "Assets/_Project/Sprites/common/";
        private const string FontPath = "Assets/_Project/Fonts/Roboto.ttf";
        private const string EastAsianFontPath =
            "Assets/_Project/Fonts/NotoSourceHan-subset.ttf";
        private const string RoundedShaderPath =
            "Assets/_Project/Shaders/UIRoundedRect.shader";

        private static readonly Color Brown =
            new(0.576f, 0.353f, 0.353f, 1f);
        private static readonly Color Cream =
            new(1f, 0.984f, 0.969f, 1f);
        private static readonly Color TitleCream =
            new(0.976f, 0.925f, 0.882f, 1f);

        static RankActivityPagePrefabInstaller()
        {
            EditorApplication.delayCall += QueueInstall;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                    EditorApplication.delayCall += QueueInstall;
            };
        }

        [MenuItem("Meowdoku/Port/Install Rank Activity Entry and Popup")]
        private static void InstallFromMenu()
        {
            InstallIfReady();
            Selection.activeObject =
                AssetDatabase.LoadAssetAtPath<GameObject>(OpenPopupPath);
        }

        internal static void InstallIfReady()
        {
            if (!CanEdit())
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    EditorApplication.delayCall += QueueInstall;
                return;
            }

            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            Font eastAsian =
                AssetDatabase.LoadAssetAtPath<Font>(EastAsianFontPath);
            Shader rounded = AssetDatabase.LoadAssetAtPath<Shader>(
                RoundedShaderPath);
            LocalizationCatalog localization =
                LocalizationCatalogAssetInstaller.GetOrCreate();
            if (font == null || eastAsian == null || rounded == null ||
                localization == null || LoadSprite(RankRoot + "entry_open.png") == null)
                return;

            EnsureFolder("Assets/_Project/Prefabs", "UI");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(OpenPopupPath) == null)
            {
                GameObject popup = BuildOpenPopup(font, localization, rounded);
                PrefabUtility.SaveAsPrefabAsset(popup, OpenPopupPath);
                UnityEngine.Object.DestroyImmediate(popup);
            }

            GameObject avatar =
                AssetDatabase.LoadAssetAtPath<GameObject>(AvatarPath);
            GameObject row = AssetDatabase.LoadAssetAtPath<GameObject>(RowPath);
            if (row == null && avatar != null)
                row = BuildRowPrefab(font, avatar);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PagePath) == null &&
                avatar != null && row != null)
            {
                GameObject page = BuildRankPage(
                    font,
                    localization,
                    avatar,
                    row);
                PrefabUtility.SaveAsPrefabAsset(page, PagePath);
                UnityEngine.Object.DestroyImmediate(page);
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(HowToPlayPath) == null)
            {
                GameObject page = BuildHowToPlay(font, localization);
                PrefabUtility.SaveAsPrefabAsset(page, HowToPlayPath);
                UnityEngine.Object.DestroyImmediate(page);
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ChangePath) == null &&
                row != null)
            {
                GameObject page = BuildChangePage(font, localization, row);
                PrefabUtility.SaveAsPrefabAsset(page, ChangePath);
                UnityEngine.Object.DestroyImmediate(page);
            }
            DailyMetaPagePrefabInstaller.InstallIfReady();
            if (avatar != null)
                UpgradeAwardForRankGift(font, localization, avatar);

            UpgradeHome(font, eastAsian, localization, rounded);
            AssetDatabase.SaveAssets();
            UIRegistryAssetInstaller.InstallIfReady();
        }

        private static void QueueInstall()
        {
            InstallIfReady();
        }

        private static void UpgradeHome(
            Font font,
            Font eastAsian,
            LocalizationCatalog localization,
            Shader rounded)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(HomePath) == null)
                return;
            GameObject root = PrefabUtility.LoadPrefabContents(HomePath);
            try
            {
                HomePagePresenter home = root.GetComponent<HomePagePresenter>();
                Transform slot = root.transform.Find(
                    "Root/DailyStreakLayout/RankEntrySlot");
                if (home == null || slot == null) return;

                RankActivityEntryPresenter entry =
                    slot.GetComponentInChildren<RankActivityEntryPresenter>(true);
                bool changed = false;
                if (entry == null)
                {
                    entry = BuildEntry(
                        (RectTransform)slot,
                        font,
                        eastAsian,
                        localization,
                        rounded);
                    changed = entry != null;
                }

                SerializedObject data = new(home);
                SerializedProperty property = data.FindProperty("rankEntry");
                if (property != null &&
                    property.objectReferenceValue != entry)
                {
                    property.objectReferenceValue = entry;
                    changed = true;
                }
                if (!changed) return;
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, HomePath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static RankActivityEntryPresenter BuildEntry(
            RectTransform parent,
            Font font,
            Font eastAsian,
            LocalizationCatalog localization,
            Shader rounded)
        {
            RectTransform root = CreateRect("RankActivityEntry", parent);
            Stretch(root);
            var presenter =
                root.gameObject.AddComponent<RankActivityEntryPresenter>();

            RectTransform pending = CreateRect("StateOpen", root);
            Stretch(pending);
            Image pendingVisual = CreateImage(
                "Visual", pending, LoadSprite(RankRoot + "entry_open.png"));
            Stretch(pendingVisual.rectTransform, new Vector2(-20f, -14f));
            pendingVisual.rectTransform.offsetMax = new Vector2(20f, 14f);
            pendingVisual.preserveAspect = true;

            RectTransform chestSwitch = CreateRect("ChestSwitch", pending);
            SetCentered(chestSwitch, new Vector2(0f, 31f),
                new Vector2(150f, 150f));
            var tiers = new GameObject[3];
            for (int index = 0; index < tiers.Length; index++)
            {
                Image chest = CreateImage(
                    $"RankBox{index + 1}",
                    chestSwitch,
                    LoadSprite(RankRoot + $"chest_tier{index + 1}.png"));
                Stretch(chest.rectTransform);
                chest.preserveAspect = true;
                chest.gameObject.SetActive(false);
                tiers[index] = chest.gameObject;
            }
            Image frameOnly = CreateImage(
                "FrameOnlyBox",
                chestSwitch,
                LoadSprite(RankRoot + "rank_reward_box.png"));
            Stretch(frameOnly.rectTransform);
            frameOnly.preserveAspect = true;
            frameOnly.gameObject.SetActive(false);

            RectTransform openBand = CreateRect("Countdown", pending);
            SetCentered(openBand, new Vector2(0f, -82.5f),
                new Vector2(370f, 90f));
            Image openBackground = CreateImage("Background", openBand, null);
            SetCentered(openBackground.rectTransform, new Vector2(0f, -1f),
                new Vector2(300f, 56f));
            openBackground.color = new Color(0.96f, 0.913f, 1f, 1f);
            openBackground.gameObject.AddComponent<RoundedImageView>()
                .Configure(openBackground, rounded, 28f);
            Text openText = CreateText(
                "Open", openBand, font, 32, "OPEN", Brown);
            SetCentered(openText.rectTransform, Vector2.zero,
                new Vector2(300f, 56f));
            ConfigureLocalized(
                openText,
                localization,
                font,
                eastAsian,
                "RANK_ENTRY_OPEN",
                "OPEN");

            RectTransform active = CreateRect("StateActive", root);
            Stretch(active);
            Image activeVisual = CreateImage(
                "Visual", active, LoadSprite(RankRoot + "entry_open.png"));
            Stretch(activeVisual.rectTransform, new Vector2(-20f, -14f));
            activeVisual.rectTransform.offsetMax = new Vector2(20f, 14f);
            activeVisual.preserveAspect = true;
            Image activityArt = CreateImage(
                "ActivityArt", activeVisual.rectTransform,
                LoadSprite(RankRoot + "entry_active1.png"));
            SetCentered(activityArt.rectTransform, new Vector2(16.5f, 7f),
                new Vector2(265f, 149f));
            activityArt.preserveAspect = true;

            RectTransform countdown = CreateRect("Countdown", active);
            SetCentered(countdown, new Vector2(0f, -82.5f),
                new Vector2(370f, 90f));
            Image countdownBackground = CreateImage(
                "Background", countdown, null);
            SetCentered(countdownBackground.rectTransform,
                new Vector2(0f, -1f), new Vector2(300f, 56f));
            countdownBackground.color = new Color(0.96f, 0.913f, 1f, 1f);
            countdownBackground.gameObject.AddComponent<RoundedImageView>()
                .Configure(countdownBackground, rounded, 28f);
            Image timer = CreateImage(
                "CountdownIcon", countdown,
                LoadSprite(GameRoot + "icon_timer.png"));
            SetCentered(timer.rectTransform, new Vector2(-95f, 0f),
                new Vector2(42f, 47f));
            timer.preserveAspect = true;
            Text countdownText = CreateText(
                "CountdownText", countdown, font, 40,
                "21:19:33", Brown);
            SetCentered(countdownText.rectTransform, new Vector2(38f, 0f),
                new Vector2(205f, 56f));

            RectTransform rank = CreateRect("Rank", active);
            SetCentered(rank, new Vector2(160f, 82f),
                new Vector2(82f, 100f));
            Image medal = CreateImage(
                "Medal", rank, LoadSprite(RankRoot + "medal.png"));
            Stretch(medal.rectTransform);
            medal.preserveAspect = true;
            Text rankText = CreateText(
                "RankText", rank, font, 40, "99", Color.white);
            SetCentered(rankText.rectTransform, new Vector2(0f, -8f),
                new Vector2(56f, 50f));

            Button click = CreateButton("ClickBtn", root, null, Color.clear);
            Stretch((RectTransform)click.transform);

            SerializedObject data = new(presenter);
            SetRef(data, "contentRoot", root.gameObject);
            SetRef(data, "pendingRewardState", pending.gameObject);
            SetRef(data, "activeState", active.gameObject);
            SetRef(data, "rankMedal", rank.gameObject);
            SetRef(data, "rankText", rankText);
            SetRef(data, "countdownText", countdownText);
            SetObjectArray(data, "chestTiers", tiers);
            SetRef(data, "frameOnlyChest", frameOnly.gameObject);
            SetRef(data, "clickButton", click);
            data.ApplyModifiedPropertiesWithoutUndo();

            pending.gameObject.SetActive(false);
            rank.gameObject.SetActive(false);
            root.gameObject.SetActive(false);
            return presenter;
        }

        private static GameObject BuildOpenPopup(
            Font font,
            LocalizationCatalog localization,
            Shader rounded)
        {
            var page = new GameObject(
                "RankActivityOpenPopup",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(GraphicRaycaster),
                typeof(RankActivityOpenPopupPresenter));
            Stretch((RectTransform)page.transform);
            page.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            RectTransform root = CreateRect("Root", page.transform);
            Stretch(root);
            RectTransform content = CreateRect("Content", root);
            Stretch(content);
            CanvasGroup contentGroup = content.gameObject.AddComponent<CanvasGroup>();
            GenericPopupAnimator animator =
                content.gameObject.AddComponent<GenericPopupAnimator>();

            RectTransform dialog = CreateRect("DialogRoot", content);
            SetCentered(dialog, Vector2.zero, new Vector2(900f, 1180f));
            Image dialogBackground = CreateImage("DialogBg", dialog, null);
            Stretch(dialogBackground.rectTransform);
            dialogBackground.color = Cream;
            dialogBackground.gameObject.AddComponent<RoundedImageView>()
                .Configure(dialogBackground, rounded, 60f);

            Image titlePanel = CreateImage("TitleBgPanel", dialog, null);
            SetTop(titlePanel.rectTransform, 0f, 0f, 900f, 130f);
            titlePanel.color = TitleCream;
            titlePanel.gameObject.AddComponent<RoundedImageView>()
                .Configure(titlePanel, rounded, 60f);
            Text title = CreateText(
                "TitleLabel", dialog, font, 86, "New Session", Brown);
            SetTop(title.rectTransform, 0f, 0f, 900f, 130f);

            Button close = CreateButton(
                "CloseBtn", dialog,
                LoadSprite(CommonRoot + "btn_close.png"), Color.white);
            SetTop((RectTransform)close.transform, 780f, 20f, 100f, 100f);

            RectTransform banner = CreateRect("Banner", dialog);
            SetTop(banner, 50f, 180f, 800f, 426f);
            Image bannerImage = CreateImage(
                "BannerImg", banner, LoadSprite(RankRoot + "layer553.png"));
            Stretch(bannerImage.rectTransform);
            bannerImage.preserveAspect = true;
            Image countdownBackground = CreateImage(
                "CountdownBg", banner, null);
            SetTop(countdownBackground.rectTransform, 269f, 0f, 262f, 64f);
            countdownBackground.color = Color.black;
            countdownBackground.gameObject.AddComponent<RoundedImageView>()
                .Configure(countdownBackground, rounded, 26f);
            Image timer = CreateImage(
                "CountdownIcon", banner,
                LoadSprite(GameRoot + "icon_timer.png"));
            SetTop(timer.rectTransform, 294f, 6f, 42f, 47f);
            timer.preserveAspect = true;
            Text countdown = CreateText(
                "CountdownLabel", banner, font, 40,
                "21:29:33", Color.white);
            SetTop(countdown.rectTransform, 335f, 1f, 174f, 62f);

            RectTransform copyArea = CreateRect("Body", dialog);
            SetTop(copyArea, 46f, 675f, 808f, 175f);
            Image cat = CreateImage(
                "CatIcon", copyArea,
                LoadSprite(GameRoot + "tool_cat_item.png"));
            SetCentered(cat.rectTransform, new Vector2(-305f, 0f),
                new Vector2(66f, 64f));
            cat.preserveAspect = true;
            Image fish = CreateImage(
                "FishIcon", copyArea,
                LoadSprite(GameRoot + "fish_full.png"));
            SetCentered(fish.rectTransform, new Vector2(-305f, 0f),
                new Vector2(80f, 80f));
            fish.preserveAspect = true;
            Text body = CreateText(
                "BodyText", copyArea, font, 54,
                "Play games to find and rank up during each event. " +
                "Aim for higher ranks!", Brown);
            SetCentered(body.rectTransform, new Vector2(45f, 0f),
                new Vector2(700f, 175f));
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Truncate;
            body.resizeTextMinSize = 30;

            Button action = CreateButton(
                "ActionButton", dialog,
                LoadSprite(CommonRoot + "btn_primary.png"), Color.white);
            SetTop((RectTransform)action.transform,
                56f, 880f, 784f, 190f);
            Text actionText = CreateText(
                "Text", action.transform, font, 64,
                "Got it", Color.white);
            Stretch(actionText.rectTransform, new Vector2(45f, 20f));

            SerializedObject animatorData = new(animator);
            SetRef(animatorData, "content", content);
            SetRef(animatorData, "contentGroup", contentGroup);
            animatorData.ApplyModifiedPropertiesWithoutUndo();

            RankActivityOpenPopupPresenter presenter =
                page.GetComponent<RankActivityOpenPopupPresenter>();
            ConfigureWindow(presenter, page, UiLayer.Popup, false, true);
            SerializedObject data = new(presenter);
            SetRef(data, "popupAnimator", animator);
            SetRef(data, "titleText", title);
            SetRef(data, "bodyText", body);
            SetRef(data, "countdownText", countdown);
            SetRef(data, "actionText", actionText);
            SetRef(data, "catVisual", cat.gameObject);
            SetRef(data, "fishVisual", fish.gameObject);
            SetRef(data, "actionButton", action);
            SetRef(data, "actionCloseButton", close);
            SetRef(data, "localization", localization);
            data.ApplyModifiedPropertiesWithoutUndo();
            fish.gameObject.SetActive(false);
            return page;
        }

        private static GameObject BuildRowPrefab(Font font, GameObject avatarPrefab)
        {
            var root = new GameObject(
                "RankActivityRow",
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(RankActivityRowView));
            root.layer = LayerMask.NameToLayer("UI");
            RectTransform rootRect = (RectTransform)root.transform;
            rootRect.sizeDelta = new Vector2(968f, 180f);
            LayoutElement layout = root.GetComponent<LayoutElement>();
            layout.preferredWidth = 968f;
            layout.preferredHeight = 180f;

            Image background = CreateImage(
                "Background", rootRect,
                LoadSprite(RankRoot + "rank_row_bg.png"));
            Stretch(background.rectTransform, new Vector2(-14f, -4f));
            background.rectTransform.offsetMax = new Vector2(14f, 24f);

            Image bigMedal = CreateImage(
                "BigMedal", rootRect,
                LoadSprite(RankRoot + "rank_medal_gold.png"));
            SetTop(bigMedal.rectTransform, -14f, -4f, 332f, 208f);
            bigMedal.preserveAspect = true;

            RectTransform content = CreateRect("Content", rootRect);
            SetTop(content, 110f, 10f, 838f, 160f);
            GameObject avatar = (GameObject)PrefabUtility.InstantiatePrefab(
                avatarPrefab);
            avatar.name = "AvatarSlot";
            avatar.transform.SetParent(content, false);
            SetTop((RectTransform)avatar.transform, 0f, 0f, 160f, 160f);

            Text name = CreateText(
                "NameLabel", content, font, 50,
                "ID123456...", Brown);
            SetTop(name.rectTransform, 190f, 5f, 270f, 150f);
            name.alignment = TextAnchor.MiddleLeft;
            name.horizontalOverflow = HorizontalWrapMode.Wrap;

            RectTransform scoreGroup = CreateRect("Score", content);
            SetTop(scoreGroup, 490f, 40f, 223f, 80f);
            Image scoreBackground = CreateImage(
                "CountBg", scoreGroup,
                LoadSprite(RankRoot + "fish_count_bg.png"));
            SetTop(scoreBackground.rectTransform, 5f, 6f, 218f, 88f);
            Image fish = CreateImage(
                "FishIcon", scoreGroup,
                LoadSprite(RankRoot + "htp_fish.png"));
            SetTop(fish.rectTransform, 0f, 0f, 80f, 80f);
            fish.preserveAspect = true;
            Image cat = CreateImage(
                "CatIcon", scoreGroup,
                LoadSprite(GameRoot + "tool_cat_item.png"));
            SetTop(cat.rectTransform, 0f, 0f, 80f, 80f);
            cat.preserveAspect = true;
            Text score = CreateText(
                "CountLabel", scoreGroup, font, 50, "999", Brown);
            SetTop(score.rectTransform, 90f, 9f, 102f, 64f);

            Text rank = CreateText(
                "RankPlain", content, font, 58, "4", Brown);
            SetTop(rank.rectTransform, -69f, 53f, 50f, 58f);

            RectTransform chest = CreateRect("Chest", content);
            SetTop(chest, 723f, 19f, 109f, 122f);
            Image chestImage = CreateImage(
                "Image", chest,
                LoadSprite(RankRoot + "chest_tier3.png"));
            Stretch(chestImage.rectTransform);
            chestImage.preserveAspect = true;

            RectTransform badgeRoot = CreateRect("MedalBadge", rootRect);
            SetTop(badgeRoot, 20f, 43f, 76f, 88f);
            Image badge = CreateImage(
                "BadgeBg", badgeRoot,
                LoadSprite(RankRoot + "rank_badge_gold.png"));
            Stretch(badge.rectTransform);
            badge.preserveAspect = true;
            Text badgeText = CreateText(
                "BadgeNum", badgeRoot, font, 50, "1", Color.white);
            SetTop(badgeText.rectTransform, 20f, 18f, 38f, 58f);

            Button selfButton = CreateButton(
                "SelfButton", avatar.transform, null, Color.clear);
            Stretch((RectTransform)selfButton.transform);

            RankActivityRowView view = root.GetComponent<RankActivityRowView>();
            SerializedObject data = new(view);
            SetRef(data, "background", background);
            SetRef(data, "normalBackground",
                LoadSprite(RankRoot + "rank_row_bg.png"));
            SetRef(data, "selfBackground",
                LoadSprite(RankRoot + "rank_row_bg_self_noshadow.png"));
            SetRef(data, "bigMedal", bigMedal);
            SetSpriteArray(data, "bigMedals", new[]
            {
                LoadSprite(RankRoot + "rank_medal_gold.png"),
                LoadSprite(RankRoot + "rank_medal_silver.png"),
                LoadSprite(RankRoot + "rank_medal_bronze.png")
            });
            SetRef(data, "badge", badge);
            SetSpriteArray(data, "badges", new[]
            {
                LoadSprite(RankRoot + "rank_badge_gold.png"),
                LoadSprite(RankRoot + "rank_badge_silver.png"),
                LoadSprite(RankRoot + "rank_badge_bronze.png")
            });
            SetRef(data, "badgeText", badgeText);
            SetRef(data, "rankText", rank);
            SetRef(data, "avatar", avatar.GetComponent<ProfileAvatarView>());
            SetRef(data, "nameText", name);
            SetRef(data, "scoreText", score);
            SetRef(data, "scoreBackground", scoreBackground);
            SetRef(data, "normalScoreBackground",
                LoadSprite(RankRoot + "fish_count_bg.png"));
            SetRef(data, "selfScoreBackground",
                LoadSprite(RankRoot + "fish_count_bg_self.png"));
            SetRef(data, "catIcon", cat.gameObject);
            SetRef(data, "fishIcon", fish.gameObject);
            SetRef(data, "chest", chest.gameObject);
            SetRef(data, "chestImage", chestImage);
            SetSpriteArray(data, "chestTiers", ChestSprites());
            SetRef(data, "selfButton", selfButton);
            data.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, RowPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildRankPage(
            Font font,
            LocalizationCatalog localization,
            GameObject avatarPrefab,
            GameObject rowPrefab)
        {
            var page = new GameObject(
                "RankActivityPage",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(GraphicRaycaster),
                typeof(RankActivityPagePresenter));
            Stretch((RectTransform)page.transform);
            page.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            Image background = CreateImage(
                "Background", page.transform,
                LoadSprite(RankRoot + "rankpage_background.png"));
            Stretch(background.rectTransform);
            background.type = Image.Type.Sliced;

            RectTransform root = CreateRect("Root", page.transform);
            root.anchorMin = new Vector2(0.5f, 0f);
            root.anchorMax = new Vector2(0.5f, 1f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(1080f, 0f);
            root.anchoredPosition = Vector2.zero;

            RectTransform header = CreateRect("Header", root);
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.anchoredPosition = Vector2.zero;
            header.sizeDelta = new Vector2(0f, 184f);
            Button back = CreateButton(
                "BackBtn", header,
                LoadSprite(CommonRoot + "icon_back.png"), Color.white);
            SetTop((RectTransform)back.transform, 25f, 20f, 100f, 100f);
            Button info = CreateButton(
                "SettingsBtn", header,
                LoadSprite(CommonRoot + "icon_info.png"), Color.white);
            SetTop((RectTransform)info.transform, 955f, 20f, 100f, 100f);

            Image titleBase = CreateImage(
                "TitleBase", header,
                LoadSprite(RankRoot + "rankpage_title_base.png"));
            SetTop(titleBase.rectTransform, 233f, 9f, 615f, 128f);
            titleBase.preserveAspect = true;
            Text title = CreateText(
                "Title", header, font, 58, "Leaderboard", Color.white);
            SetTop(title.rectTransform, 364f, 9f, 354f, 90f);

            Image countdownBackground = CreateImage(
                "CountdownBg", header,
                LoadSprite(RankRoot + "rankpage_countdown.png"));
            SetTop(countdownBackground.rectTransform, 405f, 117f, 274f, 66f);
            countdownBackground.preserveAspect = true;
            Image timer = CreateImage(
                "CountdownIcon", header,
                LoadSprite(GameRoot + "icon_timer.png"));
            SetTop(timer.rectTransform, 433f, 122f, 42f, 47f);
            timer.preserveAspect = true;
            Text countdown = CreateText(
                "CountdownText", header, font, 40,
                "22:55:44", Color.white);
            SetTop(countdown.rectTransform, 475f, 119f, 180f, 60f);

            RectTransform podiumArea = CreateRect("Podium", root);
            podiumArea.anchorMin = podiumArea.anchorMax = new Vector2(0.5f, 1f);
            podiumArea.pivot = new Vector2(0.5f, 1f);
            podiumArea.anchoredPosition = new Vector2(0f, -245f);
            podiumArea.sizeDelta = new Vector2(1080f, 470f);
            RankActivityPodiumView[] podiums =
            {
                BuildPodium("First", podiumArea, avatarPrefab, font, 1,
                    new Vector2(0f, 15f)),
                BuildPodium("Second", podiumArea, avatarPrefab, font, 2,
                    new Vector2(-315f, -35f)),
                BuildPodium("Third", podiumArea, avatarPrefab, font, 3,
                    new Vector2(315f, -35f))
            };

            RectTransform listGroup = CreateRect("List", root);
            listGroup.anchorMin = new Vector2(0.5f, 0f);
            listGroup.anchorMax = new Vector2(0.5f, 1f);
            listGroup.pivot = new Vector2(0.5f, 0.5f);
            listGroup.anchoredPosition = new Vector2(0f, -185f);
            listGroup.sizeDelta = new Vector2(1008f, -900f);
            Image listBackground = CreateImage(
                "Background", listGroup,
                LoadSprite(RankRoot + "rankpage_list_bg.png"));
            Stretch(listBackground.rectTransform);
            listBackground.type = Image.Type.Sliced;

            RectTransform viewport = CreateRect("Viewport", listGroup);
            Stretch(viewport, new Vector2(20f, 20f));
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            RectTransform rows = CreateRect("Rows", viewport);
            rows.anchorMin = new Vector2(0.5f, 1f);
            rows.anchorMax = new Vector2(0.5f, 1f);
            rows.pivot = new Vector2(0.5f, 1f);
            rows.anchoredPosition = Vector2.zero;
            rows.sizeDelta = new Vector2(968f, 0f);
            VerticalLayoutGroup vertical =
                rows.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.spacing = 10f;
            vertical.childAlignment = TextAnchor.UpperCenter;
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;
            ContentSizeFitter fitter =
                rows.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ScrollRect scroll = listGroup.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = rows;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.1f;

            Button cta = CreateButton(
                "CtaButton", root,
                LoadSprite(RankRoot + "rankpage_cta.png"), Color.white);
            RectTransform ctaRect = (RectTransform)cta.transform;
            ctaRect.anchorMin = ctaRect.anchorMax = new Vector2(0.5f, 0f);
            ctaRect.pivot = new Vector2(0.5f, 0f);
            ctaRect.anchoredPosition = new Vector2(0f, 35f);
            ctaRect.sizeDelta = new Vector2(820f, 180f);
            Text ctaText = CreateText(
                "Text", cta.transform, font, 58,
                "Go to Collect", Color.white);
            Stretch(ctaText.rectTransform, new Vector2(60f, 25f));

            RankActivityPagePresenter presenter =
                page.GetComponent<RankActivityPagePresenter>();
            ConfigureWindow(presenter, page, UiLayer.Default, true, false);
            SerializedObject data = new(presenter);
            SetRef(data, "backButton", back);
            SetRef(data, "infoButton", info);
            SetRef(data, "ctaButton", cta);
            SetRef(data, "titleText", title);
            SetRef(data, "countdownText", countdown);
            SetRef(data, "ctaText", ctaText);
            SetRef(data, "scroll", scroll);
            SetRef(data, "rowList", rows);
            SetRef(data, "rowPrefab",
                rowPrefab.GetComponent<RankActivityRowView>());
            SetComponentArray(data, "podiums", podiums);
            SetRef(data, "localization", localization);
            data.ApplyModifiedPropertiesWithoutUndo();
            return page;
        }

        private static RankActivityPodiumView BuildPodium(
            string name,
            Transform parent,
            GameObject avatarPrefab,
            Font font,
            int place,
            Vector2 position)
        {
            RectTransform root = CreateRect(name, parent);
            SetCentered(root, position, new Vector2(300f, 430f));
            var view = root.gameObject.AddComponent<RankActivityPodiumView>();
            string medal = place == 1 ? "gold" : place == 2 ? "silver" : "bronze";
            Image baseImage = CreateImage(
                "Base", root,
                LoadSprite(RankRoot + $"top3_{medal}_base.png"));
            SetCentered(baseImage.rectTransform, new Vector2(0f, -115f),
                new Vector2(300f, 235f));
            baseImage.preserveAspect = true;
            GameObject avatar = (GameObject)PrefabUtility.InstantiatePrefab(
                avatarPrefab);
            avatar.name = "Avatar";
            avatar.transform.SetParent(root, false);
            SetCentered((RectTransform)avatar.transform,
                new Vector2(0f, 80f), new Vector2(210f, 210f));
            Text displayName = CreateText(
                "Name", root, font, 36, "Player", Color.white);
            SetCentered(displayName.rectTransform, new Vector2(0f, -42f),
                new Vector2(260f, 48f));
            Text score = CreateText(
                "Score", root, font, 40, "999", Color.white);
            SetCentered(score.rectTransform, new Vector2(25f, -100f),
                new Vector2(150f, 55f));
            Image cat = CreateImage(
                "CatIcon", root,
                LoadSprite(GameRoot + "tool_cat_item.png"));
            SetCentered(cat.rectTransform, new Vector2(-65f, -100f),
                new Vector2(56f, 56f));
            cat.preserveAspect = true;
            Image fish = CreateImage(
                "FishIcon", root,
                LoadSprite(RankRoot + "htp_fish.png"));
            SetCentered(fish.rectTransform, new Vector2(-65f, -100f),
                new Vector2(56f, 56f));
            fish.preserveAspect = true;
            RectTransform chest = CreateRect("Chest", root);
            SetCentered(chest, new Vector2(0f, -180f),
                new Vector2(100f, 100f));
            Image chestImage = CreateImage(
                "Image", chest,
                LoadSprite(RankRoot + $"chest_tier{4 - place}.png"));
            Stretch(chestImage.rectTransform);
            chestImage.preserveAspect = true;
            Button selfButton = CreateButton(
                "SelfButton", avatar.transform, null, Color.clear);
            Stretch((RectTransform)selfButton.transform);

            SerializedObject data = new(view);
            SetRef(data, "avatar", avatar.GetComponent<ProfileAvatarView>());
            SetRef(data, "nameText", displayName);
            SetRef(data, "scoreText", score);
            SetRef(data, "catIcon", cat.gameObject);
            SetRef(data, "fishIcon", fish.gameObject);
            SetRef(data, "chest", chest.gameObject);
            SetRef(data, "chestImage", chestImage);
            SetSpriteArray(data, "chestTiers", ChestSprites());
            SetRef(data, "selfButton", selfButton);
            data.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static GameObject BuildHowToPlay(
            Font font,
            LocalizationCatalog localization)
        {
            var page = new GameObject(
                "RankActivityHowToPlay",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(GraphicRaycaster),
                typeof(RankActivityHowToPlayPresenter));
            Stretch((RectTransform)page.transform);
            page.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            RectTransform root = CreateRect("Root", page.transform);
            Stretch(root);
            RectTransform content = CreateRect("Content", root);
            content.anchorMin = content.anchorMax = new Vector2(0.5f, 0.5f);
            content.pivot = new Vector2(0.5f, 0.5f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(1080f, 2400f);

            Text title = CreateText(
                "Leaderboard", content, font, 90,
                "Leaderboard", Color.white);
            SetTop(title.rectTransform, 264f, 280f, 552f, 100f);

            RectTransform steps = CreateRect("Steps", content);
            SetTop(steps, 100f, 470f, 880f, 1450f);

            RectTransform clear = BuildHtpStep(
                "ClearLevel", steps, font, 0f,
                LoadSprite(RankRoot + "htp_rect1.png"),
                "Clear main levels",
                out Text clearText);
            Image clearDecor = CreateImage(
                "Board", clear,
                LoadSprite(RankRoot + "htp_layer2.png"));
            SetCentered(clearDecor.rectTransform, new Vector2(0f, 25f),
                new Vector2(260f, 260f));
            clearDecor.preserveAspect = true;

            RectTransform collect = BuildHtpStep(
                "Collect", steps, font, 390f,
                LoadSprite(RankRoot + "htp_layer344_copy.png"),
                "Find cats to increase your rank",
                out Text collectText);
            Image cat = CreateImage(
                "IconCat", collect,
                LoadSprite(RankRoot + "htp_prop_cat.png"));
            SetCentered(cat.rectTransform, new Vector2(0f, 20f),
                new Vector2(250f, 250f));
            cat.preserveAspect = true;
            Image fish = CreateImage(
                "IconFish", collect,
                LoadSprite(RankRoot + "htp_fish.png"));
            SetCentered(fish.rectTransform, new Vector2(0f, 20f),
                new Vector2(250f, 250f));
            fish.preserveAspect = true;

            RectTransform top = BuildHtpStep(
                "TopLeaderboard", steps, font, 780f,
                LoadSprite(RankRoot + "htp_rank_list.png"),
                "Top the Leaderboard",
                out Text topText);
            Image rankList = CreateImage(
                "RankList", top,
                LoadSprite(RankRoot + "htp_rank_list.png"));
            SetCentered(rankList.rectTransform, new Vector2(0f, 25f),
                new Vector2(300f, 270f));
            rankList.preserveAspect = true;

            Image arrow1 = CreateImage(
                "Arrow1", steps, LoadSprite(RankRoot + "htp_arrow.png"));
            SetTop(arrow1.rectTransform, 410f, 330f, 60f, 90f);
            arrow1.preserveAspect = true;
            Image arrow2 = CreateImage(
                "Arrow2", steps, LoadSprite(RankRoot + "htp_arrow.png"));
            SetTop(arrow2.rectTransform, 410f, 720f, 60f, 90f);
            arrow2.preserveAspect = true;

            RectTransform reward = CreateRect("Reward", content);
            SetTop(reward, 100f, 1900f, 880f, 360f);
            RectTransform full = CreateRect("RewardFull", reward);
            Stretch(full);
            Image fullAvatar = CreateImage(
                "Avatar", full,
                LoadSprite(RankRoot + "htp_avatar.png"));
            SetCentered(fullAvatar.rectTransform, new Vector2(-160f, 55f),
                new Vector2(230f, 230f));
            fullAvatar.preserveAspect = true;
            Image fullFrame = CreateImage(
                "Frame", full,
                LoadSprite(RankRoot + "htp_first_place_frame.png"));
            SetCentered(fullFrame.rectTransform, new Vector2(10f, 55f),
                new Vector2(230f, 230f));
            fullFrame.preserveAspect = true;
            Image fullBox = CreateImage(
                "Box", full,
                LoadSprite(RankRoot + "htp_tier3_box.png"));
            SetCentered(fullBox.rectTransform, new Vector2(190f, 55f),
                new Vector2(230f, 230f));
            fullBox.preserveAspect = true;

            RectTransform frameOnly = CreateRect("RewardFrameOnly", reward);
            Stretch(frameOnly);
            Image foAvatar = CreateImage(
                "Avatar", frameOnly,
                LoadSprite(RankRoot + "htp_fo_avatar.png"));
            SetCentered(foAvatar.rectTransform, new Vector2(-120f, 55f),
                new Vector2(250f, 250f));
            foAvatar.preserveAspect = true;
            Image foFrame = CreateImage(
                "Frame", frameOnly,
                LoadSprite(RankRoot + "htp_fo_first_place_frame.png"));
            SetCentered(foFrame.rectTransform, new Vector2(120f, 55f),
                new Vector2(250f, 250f));
            foFrame.preserveAspect = true;
            Text rewardText = CreateText(
                "RewardText", reward, font, 48,
                "Win exclusive frames and rewards", Color.white);
            SetCentered(rewardText.rectTransform, new Vector2(0f, -125f),
                new Vector2(780f, 80f));

            Text continueText = CreateText(
                "TapToContinue", content, font, 44,
                "Tap to Continue", Color.white);
            SetTop(continueText.rectTransform, 300f, 2290f, 480f, 70f);
            Button dismiss = CreateButton(
                "DismissButton", page.transform, null, Color.clear);
            Stretch((RectTransform)dismiss.transform);

            RankActivityHowToPlayPresenter presenter =
                page.GetComponent<RankActivityHowToPlayPresenter>();
            ConfigureWindow(presenter, page, UiLayer.Default, false, true);
            SerializedObject data = new(presenter);
            SetRef(data, "catIcon", cat.gameObject);
            SetRef(data, "fishIcon", fish.gameObject);
            SetRef(data, "fullReward", full.gameObject);
            SetRef(data, "frameOnlyReward", frameOnly.gameObject);
            SetRef(data, "titleText", title);
            SetRef(data, "clearText", clearText);
            SetRef(data, "collectText", collectText);
            SetRef(data, "topText", topText);
            SetRef(data, "rewardText", rewardText);
            SetRef(data, "continueText", continueText);
            SetRef(data, "dismissButton", dismiss);
            SetRef(data, "localization", localization);
            data.ApplyModifiedPropertiesWithoutUndo();
            fish.gameObject.SetActive(false);
            frameOnly.gameObject.SetActive(false);
            return page;
        }

        private static GameObject BuildChangePage(
            Font font,
            LocalizationCatalog localization,
            GameObject rowPrefab)
        {
            var page = new GameObject(
                "RankActivityChange",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(GraphicRaycaster),
                typeof(RankActivityChangePresenter));
            Stretch((RectTransform)page.transform);
            page.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            Image mask = CreateImage("Mask", page.transform, null);
            Stretch(mask.rectTransform);
            mask.color = new Color(0f, 0f, 0f, 0.85f);
            Button maskButton = mask.gameObject.AddComponent<Button>();
            mask.raycastTarget = true;
            maskButton.targetGraphic = mask;
            maskButton.transition = Selectable.Transition.None;

            RectTransform root = CreateRect("Root", page.transform);
            root.anchorMin = new Vector2(0.5f, 0f);
            root.anchorMax = new Vector2(0.5f, 1f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(1080f, 0f);

            RectTransform encourage = CreateRect("EncourageTopBar", root);
            encourage.anchorMin = encourage.anchorMax = new Vector2(0.5f, 1f);
            encourage.pivot = new Vector2(0.5f, 1f);
            encourage.anchoredPosition = new Vector2(0f, 9f);
            encourage.sizeDelta = new Vector2(630f, 226f);
            Image glow = CreateImage(
                "BannerGlow", encourage,
                LoadSprite(RankRoot + "change_banner_glow.png"));
            SetTop(glow.rectTransform, 36f, 46.5f, 615.7f, 163.5f);
            glow.preserveAspect = true;
            Image bar = CreateImage(
                "BannerBar", encourage,
                LoadSprite(RankRoot + "change_banner_bar.png"));
            SetTop(bar.rectTransform, 46f, 76f, 540f, 124f);
            bar.preserveAspect = true;
            Text progress = CreateText(
                "ProgressLabel", encourage, font, 40,
                string.Empty, Brown);
            SetTop(progress.rectTransform, 79f, 76f, 473f, 124f);

            Text title = CreateText(
                "Leaderboard", root, font, 90,
                "Leaderboard", TitleCream);
            title.rectTransform.anchorMin =
                title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -248f);
            title.rectTransform.sizeDelta = new Vector2(700f, 120f);

            RectTransform countdownRoot = CreateRect("Countdown", root);
            countdownRoot.anchorMin =
                countdownRoot.anchorMax = new Vector2(0.5f, 1f);
            countdownRoot.pivot = new Vector2(0.5f, 1f);
            countdownRoot.anchoredPosition = new Vector2(0f, -428f);
            countdownRoot.sizeDelta = new Vector2(320f, 84f);
            Image countdownBackground = CreateImage(
                "CountdownBg", countdownRoot,
                LoadSprite(RankRoot + "change_countdown_bg.png"));
            Stretch(countdownBackground.rectTransform);
            countdownBackground.preserveAspect = true;
            Image countdownIcon = CreateImage(
                "CountdownIcon", countdownRoot,
                LoadSprite(RankRoot + "change_countdown_icon.png"));
            SetTop(countdownIcon.rectTransform, 27f, 8f, 58f, 65f);
            countdownIcon.preserveAspect = true;
            Text countdown = CreateText(
                "CountdownText", countdownRoot, font, 50,
                "12:05:05", Color.white);
            SetTop(countdown.rectTransform, 97.5f, 18f, 197f, 52f);

            RectTransform listGroup = CreateRect("ListGroup", root);
            listGroup.anchorMin = new Vector2(0.5f, 0f);
            listGroup.anchorMax = new Vector2(0.5f, 1f);
            listGroup.pivot = new Vector2(0.5f, 0.5f);
            listGroup.anchoredPosition = Vector2.zero;
            listGroup.sizeDelta = new Vector2(1008f, -1240f);
            RectTransform viewport = CreateRect("RankCellMask", listGroup);
            Stretch(viewport);
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
            Mask viewportMask = viewport.gameObject.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            RectTransform rows = CreateRect("RowList", viewport);
            rows.anchorMin = new Vector2(0.5f, 1f);
            rows.anchorMax = new Vector2(0.5f, 1f);
            rows.pivot = new Vector2(0.5f, 1f);
            rows.anchoredPosition = Vector2.zero;
            rows.sizeDelta = new Vector2(968f, 0f);
            VerticalLayoutGroup vertical =
                rows.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.spacing = 20f;
            vertical.childAlignment = TextAnchor.UpperCenter;
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;
            ContentSizeFitter fitter =
                rows.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ScrollRect scroll = listGroup.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = rows;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.1f;

            Button tap = CreateButton(
                "TapToContinue", root, null, Color.clear);
            RectTransform tapRect = (RectTransform)tap.transform;
            tapRect.anchorMin = tapRect.anchorMax = new Vector2(0.5f, 0f);
            tapRect.pivot = new Vector2(0.5f, 0f);
            tapRect.anchoredPosition = new Vector2(0f, 245f);
            tapRect.sizeDelta = new Vector2(480f, 70f);
            tap.transition = Selectable.Transition.None;
            Text tapText = CreateText(
                "Text", tap.transform, font, 44,
                "Tap to Continue",
                new Color(1f, 0.892f, 0.458f, 1f));
            Stretch(tapText.rectTransform);

            RankActivityChangePresenter presenter =
                page.GetComponent<RankActivityChangePresenter>();
            ConfigureWindow(presenter, page, UiLayer.Popup, false, false);
            SerializedObject data = new(presenter);
            SetRef(data, "titleText", title);
            SetRef(data, "countdownText", countdown);
            SetRef(data, "encouragementRoot", encourage.gameObject);
            SetRef(data, "encouragementText", progress);
            SetRef(data, "scroll", scroll);
            SetRef(data, "rowList", rows);
            SetRef(data, "rowPrefab",
                rowPrefab.GetComponent<RankActivityRowView>());
            SetRef(data, "maskButton", maskButton);
            SetRef(data, "tapButton", tap);
            SetRef(data, "tapText", tapText);
            SetRef(data, "localization", localization);
            data.ApplyModifiedPropertiesWithoutUndo();
            return page;
        }

        private static void UpgradeAwardForRankGift(
            Font font,
            LocalizationCatalog localization,
            GameObject avatarPrefab)
        {
            string path = DailyMetaPagePrefabInstaller.AwardPrefabPath;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                return;
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                AwardPagePresenter presenter =
                    root.GetComponent<AwardPagePresenter>();
                Transform panel = root.transform.Find("AwardPanel");
                if (presenter == null || panel == null) return;
                bool changed = false;
                SerializedObject presenterData = new(presenter);
                SerializedProperty regular =
                    presenterData.FindProperty("regularRoot");
                if (regular != null &&
                    regular.objectReferenceValue != panel.gameObject)
                {
                    regular.objectReferenceValue = panel.gameObject;
                    changed = true;
                }

                AwardItemView[] items =
                    root.GetComponentsInChildren<AwardItemView>(true);
                for (int index = 0; index < items.Length; index++)
                {
                    Transform frameRoot =
                        items[index].transform.Find("FrameReward");
                    if (frameRoot == null)
                    {
                        frameRoot = BuildFrameReward(items[index].transform);
                        changed = true;
                    }
                    SerializedObject itemData = new(items[index]);
                    SerializedProperty frame =
                        itemData.FindProperty("frameRoot");
                    if (frame != null &&
                        frame.objectReferenceValue != frameRoot.gameObject)
                    {
                        frame.objectReferenceValue = frameRoot.gameObject;
                        changed = true;
                    }
                    itemData.ApplyModifiedPropertiesWithoutUndo();
                    frameRoot.gameObject.SetActive(false);
                }

                Transform giftRoot =
                    root.transform.Find("RankGiftRoot");
                RankGiftView gift;
                if (giftRoot == null)
                {
                    gift = BuildRankGift(
                        root.transform,
                        font,
                        localization,
                        avatarPrefab);
                    giftRoot = gift.transform;
                    changed = true;
                }
                else
                {
                    gift = giftRoot.GetComponent<RankGiftView>();
                }
                SerializedProperty giftProperty =
                    presenterData.FindProperty("rankGiftView");
                if (giftProperty != null &&
                    giftProperty.objectReferenceValue != gift)
                {
                    giftProperty.objectReferenceValue = gift;
                    changed = true;
                }
                presenterData.ApplyModifiedPropertiesWithoutUndo();
                giftRoot.gameObject.SetActive(false);
                if (!changed) return;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Transform BuildFrameReward(Transform parent)
        {
            RectTransform root = CreateRect("FrameReward", parent);
            Stretch(root);
            Image avatar = CreateImage(
                "Avatar", root,
                LoadSprite(RankRoot + "htp_avatar.png"));
            SetCentered(avatar.rectTransform, Vector2.zero,
                new Vector2(185f, 185f));
            avatar.preserveAspect = true;
            Image frame = CreateImage(
                "FirstPlaceFrame", root,
                LoadSprite(RankRoot + "htp_first_place_frame.png"));
            SetCentered(frame.rectTransform, Vector2.zero,
                new Vector2(236f, 236f));
            frame.preserveAspect = true;
            return root;
        }

        private static RankGiftView BuildRankGift(
            Transform parent,
            Font font,
            LocalizationCatalog localization,
            GameObject avatarPrefab)
        {
            RectTransform root = CreateRect("RankGiftRoot", parent);
            Stretch(root);
            RankGiftView view = root.gameObject.AddComponent<RankGiftView>();

            Text win = CreateText(
                "WinText", root, font, 60,
                "You've won 1st place 1 times!",
                new Color(1f, 0.892f, 0.458f, 1f));
            win.rectTransform.anchorMin =
                win.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            win.rectTransform.pivot = new Vector2(0.5f, 1f);
            win.rectTransform.anchoredPosition = new Vector2(0f, -256f);
            win.rectTransform.sizeDelta = new Vector2(857f, 100f);

            RectTransform chestRoot = CreateRect("Box", root);
            chestRoot.anchorMin =
                chestRoot.anchorMax = new Vector2(0.5f, 1f);
            chestRoot.pivot = new Vector2(0.5f, 1f);
            chestRoot.anchoredPosition = new Vector2(0f, -390f);
            chestRoot.sizeDelta = new Vector2(520f, 520f);
            Image glow = CreateImage(
                "Glow", chestRoot,
                LoadSprite(RankRoot + "rank_reward_glow.png"));
            Stretch(glow.rectTransform);
            glow.preserveAspect = true;
            Image chest = CreateImage(
                "Chest", chestRoot,
                LoadSprite(RankRoot + "chest_tier3.png"));
            SetCentered(chest.rectTransform, Vector2.zero,
                new Vector2(350f, 350f));
            chest.preserveAspect = true;

            RectTransform podium = CreateRect("Podium", root);
            podium.anchorMin =
                podium.anchorMax = new Vector2(0.5f, 1f);
            podium.pivot = new Vector2(0.5f, 1f);
            podium.anchoredPosition = new Vector2(0f, -930f);
            podium.sizeDelta = new Vector2(1080f, 347f);
            ProfileAvatarView[] avatars =
            {
                BuildAwardPodium(
                    "GoldSofa", podium, avatarPrefab,
                    LoadSprite(RankRoot + "award_podium_gold.png"),
                    new Vector2(0f, 0f), 210f),
                BuildAwardPodium(
                    "SilverSofa", podium, avatarPrefab,
                    LoadSprite(RankRoot + "award_podium_silver.png"),
                    new Vector2(-352f, -25f), 185f),
                BuildAwardPodium(
                    "BronzeSofa", podium, avatarPrefab,
                    LoadSprite(RankRoot + "award_podium_bronze.png"),
                    new Vector2(352f, -37f), 185f)
            };

            Button collect = CreateButton(
                "CollectBtn", root,
                LoadSprite(CommonRoot + "btn_primary.png"),
                Color.white);
            collect.transform.GetComponent<RectTransform>().anchorMin =
                collect.transform.GetComponent<RectTransform>().anchorMax =
                    new Vector2(0.5f, 0f);
            collect.transform.GetComponent<RectTransform>().pivot =
                new Vector2(0.5f, 0f);
            collect.transform.GetComponent<RectTransform>().anchoredPosition =
                new Vector2(0f, 510f);
            collect.transform.GetComponent<RectTransform>().sizeDelta =
                new Vector2(784f, 258f);
            Text collectText = CreateText(
                "Text", collect.transform, font, 64,
                "Collect", Color.white);
            Stretch(collectText.rectTransform, new Vector2(50f, 35f));

            SerializedObject data = new(view);
            SetRef(data, "winText", win);
            SetRef(data, "chestRoot", chestRoot.gameObject);
            SetRef(data, "chestImage", chest);
            SetSpriteArray(data, "chestTiers", ChestSprites());
            SetComponentArray(data, "podiumAvatars", avatars);
            SetRef(data, "collectButton", collect);
            SetRef(data, "collectText", collectText);
            SetRef(data, "localization", localization);
            data.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static ProfileAvatarView BuildAwardPodium(
            string name,
            Transform parent,
            GameObject avatarPrefab,
            Sprite baseSprite,
            Vector2 position,
            float avatarSize)
        {
            RectTransform root = CreateRect(name, parent);
            SetCentered(root, position, new Vector2(376f, 347f));
            Image baseImage = CreateImage("Base", root, baseSprite);
            SetCentered(baseImage.rectTransform, new Vector2(0f, -75f),
                new Vector2(376f, 190f));
            baseImage.preserveAspect = true;
            GameObject avatar = (GameObject)PrefabUtility.InstantiatePrefab(
                avatarPrefab);
            avatar.name = "AvatarSlot";
            avatar.transform.SetParent(root, false);
            SetCentered((RectTransform)avatar.transform,
                new Vector2(0f, 55f),
                new Vector2(avatarSize, avatarSize));
            return avatar.GetComponent<ProfileAvatarView>();
        }

        private static RectTransform BuildHtpStep(
            string name,
            Transform parent,
            Font font,
            float top,
            Sprite background,
            string label,
            out Text labelText)
        {
            RectTransform root = CreateRect(name, parent);
            SetTop(root, 0f, top, 880f, 340f);
            Image panel = CreateImage("Panel", root, background);
            SetCentered(panel.rectTransform, new Vector2(0f, 25f),
                new Vector2(300f, 300f));
            panel.preserveAspect = true;
            labelText = CreateText(
                "Text", root, font, 48, label, Color.white);
            SetCentered(labelText.rectTransform, new Vector2(0f, -145f),
                new Vector2(820f, 70f));
            return root;
        }

        private static Sprite[] ChestSprites() => new[]
        {
            LoadSprite(RankRoot + "chest_tier1.png"),
            LoadSprite(RankRoot + "chest_tier2.png"),
            LoadSprite(RankRoot + "chest_tier3.png")
        };

        private static void ConfigureWindow(
            UIFrameWindow presenter,
            GameObject page,
            UiLayer layer,
            bool fullscreen,
            bool mask)
        {
            SerializedObject data = new(presenter);
            SetRef(data, "rootCanvas", page.GetComponent<Canvas>());
            SetRef(data, "rootCanvasGroup", page.GetComponent<CanvasGroup>());
            data.FindProperty("uiLayer").intValue = (int)layer;
            data.FindProperty("isFullscreen").boolValue = fullscreen;
            data.FindProperty("showMask").boolValue = mask;
            data.FindProperty("maskOpacity").floatValue = 0.8f;
            data.FindProperty("playOpenSound").boolValue = true;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureLocalized(
            Text text,
            LocalizationCatalog catalog,
            Font primary,
            Font eastAsian,
            string key,
            string fallback)
        {
            LocalizedText localized =
                text.gameObject.AddComponent<LocalizedText>();
            SerializedObject data = new(localized);
            SetRef(data, "catalog", catalog);
            SetRef(data, "target", text);
            data.FindProperty("key").stringValue = key;
            data.FindProperty("fallbackText").stringValue = fallback;
            SetRef(data, "primaryFont", primary);
            SetRef(data, "eastAsianFallbackFont", eastAsian);
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var target = new GameObject(name, typeof(RectTransform));
            target.layer = LayerMask.NameToLayer("UI");
            RectTransform rect = (RectTransform)target.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static Image CreateImage(
            string name,
            Transform parent,
            Sprite sprite)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            Font font,
            int size,
            string value,
            Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.text = value;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Math.Max(16, size / 2);
            text.resizeTextMaxSize = size;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            Sprite sprite,
            Color color)
        {
            Image image = CreateImage(name, parent, sprite);
            image.raycastTarget = true;
            image.color = color;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static void SetObjectArray(
            SerializedObject data,
            string name,
            GameObject[] values)
        {
            SerializedProperty property = data.FindProperty(name);
            if (property == null) return;
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
                property.GetArrayElementAtIndex(index).objectReferenceValue =
                    values[index];
        }

        private static void SetComponentArray<T>(
            SerializedObject data,
            string name,
            T[] values) where T : Component
        {
            SerializedProperty property = data.FindProperty(name);
            if (property == null) return;
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
                property.GetArrayElementAtIndex(index).objectReferenceValue =
                    values[index];
        }

        private static void SetSpriteArray(
            SerializedObject data,
            string name,
            Sprite[] values)
        {
            SerializedProperty property = data.FindProperty(name);
            if (property == null) return;
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
                property.GetArrayElementAtIndex(index).objectReferenceValue =
                    values[index];
        }

        private static void SetRef(
            SerializedObject data,
            string name,
            UnityEngine.Object value)
        {
            SerializedProperty property = data.FindProperty(name);
            if (property != null) property.objectReferenceValue = value;
        }

        private static void SetCentered(
            RectTransform rect,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetTop(
            RectTransform rect,
            float left,
            float top,
            float width,
            float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, -top);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect)
        {
            Stretch(rect, Vector2.zero);
        }

        private static void Stretch(RectTransform rect, Vector2 inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = inset;
            rect.offsetMax = -inset;
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int index = 0; index < assets.Length; index++)
                if (assets[index] is Sprite value) return value;
            return null;
        }

        private static bool CanEdit() =>
            !EditorApplication.isCompiling &&
            !EditorApplication.isUpdating &&
            !EditorApplication.isPlayingOrWillChangePlaymode;

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
