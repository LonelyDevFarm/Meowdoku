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
        private const string EffectsRoot = "Assets/_Project/Sprites/Effects/";
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
            UpgradeRankRowVisuals();
            row = AssetDatabase.LoadAssetAtPath<GameObject>(RowPath);
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
            UpgradeRankPageLayout();
            UpgradeRankChangeLayout();
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

        private static void UpgradeRankPageLayout()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PagePath) == null) return;
            GameObject page = PrefabUtility.LoadPrefabContents(PagePath);
            try
            {
                if (page.GetComponent<RankActivityPageLayoutPresenter>() != null)
                    return;
                RectTransform root = page.transform.Find("Root") as RectTransform;
                RectTransform header = page.transform.Find("Root/Header") as RectTransform;
                RectTransform podium = page.transform.Find("Root/Podium") as RectTransform;
                RectTransform list = page.transform.Find("Root/List") as RectTransform;
                RectTransform viewport = page.transform.Find(
                    "Root/List/Viewport") as RectTransform;
                RectTransform rows = page.transform.Find(
                    "Root/List/Viewport/Rows") as RectTransform;
                RectTransform cta = page.transform.Find(
                    "Root/CtaButton") as RectTransform;
                RankActivityPagePresenter presenter =
                    page.GetComponent<RankActivityPagePresenter>();
                if (root == null || header == null || podium == null || list == null ||
                    viewport == null || rows == null || cta == null || presenter == null)
                    return;

                podium.anchoredPosition = new Vector2(0f, -245f);
                podium.sizeDelta = new Vector2(1080f, 521f);
                list.anchoredPosition = new Vector2(0f, -203.5f);
                list.sizeDelta = new Vector2(1008f, -1183f);
                Stretch(viewport);
                viewport.offsetMin = new Vector2(0f, 18f);
                viewport.offsetMax = new Vector2(0f, -20f);
                VerticalLayoutGroup vertical = rows.GetComponent<VerticalLayoutGroup>();
                if (vertical != null) vertical.spacing = 20f;
                ScrollRect scroll = list.GetComponent<ScrollRect>();
                if (scroll != null)
                    scroll.movementType = ScrollRect.MovementType.Clamped;
                cta.anchoredPosition = new Vector2(0f, 130f);
                cta.sizeDelta = new Vector2(784f, 258f);

                RectTransform floating = page.transform.Find(
                    "Root/FloatRow") as RectTransform;
                if (floating == null)
                {
                    floating = CreateRect("FloatRow", root);
                    Stretch(floating);
                }
                floating.SetAsLastSibling();

                SerializedObject presenterData = new(presenter);
                SetRef(presenterData, "floatingRowLayer", floating);
                presenterData.ApplyModifiedPropertiesWithoutUndo();

                RankActivityPageLayoutPresenter layout =
                    page.AddComponent<RankActivityPageLayoutPresenter>();
                SerializedObject layoutData = new(layout);
                SetRef(layoutData, "layoutSpace", root);
                SetRef(layoutData, "header", header);
                SetRef(layoutData, "podium", podium);
                SetRef(layoutData, "list", list);
                SetRef(layoutData, "cta", cta);
                layoutData.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(page, PagePath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(page);
            }
        }

        private static void UpgradeRankRowVisuals()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RowPath) == null)
                return;
            GameObject root = PrefabUtility.LoadPrefabContents(RowPath);
            try
            {
                RankActivityRowView view =
                    root.GetComponent<RankActivityRowView>();
                RectTransform rootRect = root.transform as RectTransform;
                if (view == null || rootRect == null) return;

                bool changed = false;
                RectTransform visual = root.transform.Find(
                    "VisualRoot") as RectTransform;
                if (visual == null)
                {
                    visual = CreateRect("VisualRoot", rootRect);
                    Stretch(visual);
                    changed = true;
                }

                RectTransform content = visual.Find(
                    "CanvasGroup") as RectTransform;
                if (content == null)
                {
                    content = CreateRect("CanvasGroup", visual);
                    Stretch(content);
                    changed = true;
                }
                CanvasGroup contentGroup =
                    content.GetComponent<CanvasGroup>();
                if (contentGroup == null)
                {
                    contentGroup = content.gameObject.AddComponent<CanvasGroup>();
                    changed = true;
                }
                contentGroup.alpha = 1f;

                Transform[] directChildren =
                    new Transform[root.transform.childCount];
                for (int index = 0; index < directChildren.Length; index++)
                    directChildren[index] = root.transform.GetChild(index);
                for (int index = 0; index < directChildren.Length; index++)
                {
                    Transform child = directChildren[index];
                    if (child == visual) continue;
                    child.SetParent(content, false);
                    changed = true;
                }

                Transform[] visualChildren =
                    new Transform[visual.childCount];
                for (int index = 0; index < visualChildren.Length; index++)
                    visualChildren[index] = visual.GetChild(index);
                for (int index = 0; index < visualChildren.Length; index++)
                {
                    Transform child = visualChildren[index];
                    if (child == content || child.name == "Shadow" ||
                        child.name == "Effects")
                        continue;
                    child.SetParent(content, false);
                    changed = true;
                }

                Image shadow = visual.Find("Shadow")?.GetComponent<Image>();
                if (shadow == null)
                {
                    shadow = CreateImage(
                        "Shadow",
                        visual,
                        LoadSprite(RankRoot + "rank_row_self_shadow.png"));
                    changed = true;
                }
                Sprite shadowSprite = LoadSprite(
                    RankRoot + "rank_row_self_shadow.png");
                if (shadow.sprite != shadowSprite)
                {
                    shadow.sprite = shadowSprite;
                    changed = true;
                }
                shadow.raycastTarget = false;
                shadow.type = Image.Type.Simple;
                RectTransform shadowRect = shadow.rectTransform;
                Vector2 shadowAnchor = new(0f, 1f);
                Vector2 shadowPivot = new(0.5f, 0.5f);
                Vector2 shadowPosition = new(484.5f, -93.55f);
                Vector2 shadowSize = new(1033f, 270f);
                Vector3 shadowScale = new(1f, -1f, 1f);
                if (shadowRect.anchorMin != shadowAnchor ||
                    shadowRect.anchorMax != shadowAnchor ||
                    shadowRect.pivot != shadowPivot ||
                    shadowRect.anchoredPosition != shadowPosition ||
                    shadowRect.sizeDelta != shadowSize ||
                    shadowRect.localScale != shadowScale)
                {
                    shadowRect.anchorMin = shadowAnchor;
                    shadowRect.anchorMax = shadowAnchor;
                    shadowRect.pivot = shadowPivot;
                    shadowRect.anchoredPosition = shadowPosition;
                    shadowRect.sizeDelta = shadowSize;
                    shadowRect.localScale = shadowScale;
                    changed = true;
                }
                Color shadowColor = shadow.color;
                if (!Mathf.Approximately(shadowColor.a, 0f))
                {
                    shadowColor.a = 0f;
                    shadow.color = shadowColor;
                    changed = true;
                }
                if (shadow.gameObject.activeSelf)
                {
                    shadow.gameObject.SetActive(false);
                    changed = true;
                }
                if (shadowRect.GetSiblingIndex() != 0)
                {
                    shadowRect.SetAsFirstSibling();
                    changed = true;
                }
                RankActivityRowCelebrationView celebration =
                    EnsureRankRowCelebration(visual, ref changed);
                RectTransform effects = visual.Find("Effects") as RectTransform;
                if (effects != null &&
                    effects.GetSiblingIndex() != visual.childCount - 1)
                {
                    effects.SetAsLastSibling();
                    changed = true;
                }
                int contentIndex = effects != null
                    ? effects.GetSiblingIndex() - 1
                    : visual.childCount - 1;
                if (content.GetSiblingIndex() != contentIndex)
                {
                    content.SetSiblingIndex(Mathf.Max(1, contentIndex));
                    changed = true;
                }

                SerializedObject data = new(view);
                SerializedProperty visualProperty =
                    data.FindProperty("visualRoot");
                SerializedProperty groupProperty =
                    data.FindProperty("contentGroup");
                SerializedProperty shadowProperty =
                    data.FindProperty("selfShadow");
                SerializedProperty celebrationProperty =
                    data.FindProperty("celebration");
                if (visualProperty.objectReferenceValue != visual)
                {
                    visualProperty.objectReferenceValue = visual;
                    changed = true;
                }
                if (groupProperty.objectReferenceValue != contentGroup)
                {
                    groupProperty.objectReferenceValue = contentGroup;
                    changed = true;
                }
                if (shadowProperty.objectReferenceValue != shadow)
                {
                    shadowProperty.objectReferenceValue = shadow;
                    changed = true;
                }
                if (celebrationProperty.objectReferenceValue != celebration)
                {
                    celebrationProperty.objectReferenceValue = celebration;
                    changed = true;
                }
                if (!changed) return;
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, RowPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void UpgradeRankChangeLayout()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ChangePath) == null) return;
            GameObject page = PrefabUtility.LoadPrefabContents(ChangePath);
            try
            {
                if (page.GetComponent<RankActivityChangeLayoutPresenter>() != null)
                    return;
                RectTransform root = page.transform.Find("Root") as RectTransform;
                RectTransform encourage = page.transform.Find(
                    "Root/EncourageTopBar") as RectTransform;
                RectTransform title = page.transform.Find(
                    "Root/Leaderboard") as RectTransform;
                RectTransform countdown = page.transform.Find(
                    "Root/Countdown") as RectTransform;
                RectTransform countdownText = page.transform.Find(
                    "Root/Countdown/CountdownText") as RectTransform;
                RectTransform list = page.transform.Find(
                    "Root/ListGroup") as RectTransform;
                RectTransform rows = page.transform.Find(
                    "Root/ListGroup/RankCellMask/RowList") as RectTransform;
                RectTransform tap = page.transform.Find(
                    "Root/TapToContinue") as RectTransform;
                RankActivityChangePresenter presenter =
                    page.GetComponent<RankActivityChangePresenter>();
                if (root == null || encourage == null || title == null ||
                    countdown == null || countdownText == null || list == null ||
                    rows == null || tap == null || presenter == null)
                    return;

                SetTop(countdownText, 97.5f, 24f, 197f, 40f);
                VerticalLayoutGroup vertical = rows.GetComponent<VerticalLayoutGroup>();
                if (vertical != null)
                {
                    vertical.spacing = 20f;
                    vertical.padding = new RectOffset(0, 0, 200, 200);
                }
                ScrollRect scroll = list.GetComponent<ScrollRect>();
                if (scroll != null)
                    scroll.movementType = ScrollRect.MovementType.Clamped;

                RectTransform celebrate = page.transform.Find(
                    "Root/PlayerCelebrate") as RectTransform;
                if (celebrate == null)
                {
                    celebrate = CreateRect("PlayerCelebrate", root);
                    Stretch(celebrate);
                    celebrate.SetSiblingIndex(tap.GetSiblingIndex());
                }

                SerializedObject presenterData = new(presenter);
                SetRef(presenterData, "celebrateLayer", celebrate);
                presenterData.ApplyModifiedPropertiesWithoutUndo();

                RankActivityChangeLayoutPresenter layout =
                    page.AddComponent<RankActivityChangeLayoutPresenter>();
                SerializedObject layoutData = new(layout);
                SetRef(layoutData, "layoutSpace", root);
                SetRef(layoutData, "encourage", encourage);
                SetRef(layoutData, "title", title);
                SetRef(layoutData, "countdown", countdown);
                SetRef(layoutData, "list", list);
                SetRef(layoutData, "tap", tap);
                layoutData.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(page, ChangePath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(page);
            }
        }

        private static RankActivityRowCelebrationView
            EnsureRankRowCelebration(
                RectTransform visual,
                ref bool changed)
        {
            RectTransform effects = visual.Find("Effects") as RectTransform;
            if (effects == null)
            {
                effects = CreateRect("Effects", visual);
                Stretch(effects);
                changed = true;
            }
            RankActivityRowCelebrationView existing =
                effects.GetComponent<RankActivityRowCelebrationView>();
            if (existing != null) return existing;

            RankActivityRowCelebrationView celebration =
                effects.gameObject.AddComponent<RankActivityRowCelebrationView>();
            Sprite fish = LoadSprite(RankRoot + "collect_fish.png");
            Sprite cat = LoadSprite(GameRoot + "tool_cat_item.png");
            Sprite glowSprite = LoadSprite(
                EffectsRoot + "glow/et_glow_002.png");
            Sprite maskSprite = LoadSprite(
                EffectsRoot + "mask/et_mask_015.png");
            Sprite star1 = LoadSprite(
                EffectsRoot + "star/et_star_1.png");
            Sprite star2 = LoadSprite(
                EffectsRoot + "star/et_star_003.png");
            Sprite arrowSprite = LoadSprite(
                EffectsRoot + "ui/et_change_arrow.png");

            RectTransform collection = CreateRect("Collection", effects);
            SetTop(collection, 888f, 90f, 40f, 40f);
            RectTransform target = CreateRect("Target", collection);
            SetSourcePoint(target, new Vector2(-250f, 1f),
                new Vector2(1f, 1f));
            var itemPositions = new[]
            {
                new Vector2(-5f, -52f),
                new Vector2(40f, -30f),
                new Vector2(-25f, 35f),
                new Vector2(35f, 30f),
                new Vector2(-40f, -15f),
                new Vector2(3f, -2f)
            };
            var items = new Image[itemPositions.Length];
            var collectionGlows = new Image[itemPositions.Length];
            var collectionStars = new Image[itemPositions.Length];
            for (int index = 0; index < itemPositions.Length; index++)
            {
                items[index] = CreateImage(
                    $"CollectItem_{index + 1}",
                    collection,
                    fish);
                SetSourcePoint(
                    items[index].rectTransform,
                    itemPositions[index],
                    new Vector2(54f, 54f));
                items[index].preserveAspect = true;
                items[index].gameObject.SetActive(false);

                collectionGlows[index] = CreateImage(
                    $"Burst_{index + 1}_Glow",
                    collection,
                    glowSprite);
                SetSourcePoint(
                    collectionGlows[index].rectTransform,
                    new Vector2(-250f, 1f),
                    new Vector2(120f, 120f));
                collectionGlows[index].preserveAspect = true;
                collectionGlows[index].color =
                    new Color(1f, 0.78f, 0.35f, 0f);
                collectionGlows[index].gameObject.SetActive(false);

                collectionStars[index] = CreateImage(
                    $"Burst_{index + 1}_Star",
                    collection,
                    index % 2 == 0 ? star1 : star2);
                SetSourcePoint(
                    collectionStars[index].rectTransform,
                    new Vector2(-250f, 1f),
                    new Vector2(52f, 52f));
                collectionStars[index].preserveAspect = true;
                collectionStars[index].gameObject.SetActive(false);
            }

            RectTransform arrowRoot = CreateRect("Arrow", effects);
            Stretch(arrowRoot);
            CanvasGroup arrowGroup =
                arrowRoot.gameObject.AddComponent<CanvasGroup>();
            arrowGroup.alpha = 0f;
            arrowGroup.interactable = false;
            arrowGroup.blocksRaycasts = false;
            float[] arrowX = { 150f, 375f, 600f, 825f };
            var arrows = new Image[arrowX.Length];
            for (int index = 0; index < arrows.Length; index++)
            {
                arrows[index] = CreateImage(
                    $"ArrowParticle_{index + 1}",
                    arrowRoot,
                    arrowSprite);
                SetSourcePoint(
                    arrows[index].rectTransform,
                    new Vector2(arrowX[index], 200f),
                    new Vector2(90f, 90f));
                arrows[index].preserveAspect = true;
                Color arrowColor = arrows[index].color;
                arrowColor.a = 0f;
                arrows[index].color = arrowColor;
            }
            arrowRoot.gameObject.SetActive(false);

            RectTransform riseBurst = CreateRect("RiseBurst", effects);
            Stretch(riseBurst);
            Image riseGlow = CreateImage("Glow", riseBurst, maskSprite);
            SetCentered(riseGlow.rectTransform, Vector2.zero,
                new Vector2(240f, 240f));
            riseGlow.preserveAspect = true;
            riseGlow.color = new Color(1f, 0.92f, 0.2f, 0f);
            riseGlow.gameObject.SetActive(false);
            Vector2[] starPositions =
            {
                new(-360f, 90f), new(-120f, 90f),
                new(120f, 90f), new(360f, 90f),
                new(-360f, -90f), new(-120f, -90f),
                new(120f, -90f), new(360f, -90f),
                new(-480f, 45f), new(-480f, -45f),
                new(480f, 45f), new(480f, -45f)
            };
            var riseStars = new Image[starPositions.Length];
            for (int index = 0; index < riseStars.Length; index++)
            {
                riseStars[index] = CreateImage(
                    $"EdgeStar_{index + 1}",
                    riseBurst,
                    index % 2 == 0 ? star1 : star2);
                SetCentered(
                    riseStars[index].rectTransform,
                    starPositions[index],
                    new Vector2(42f, 42f));
                riseStars[index].preserveAspect = true;
                riseStars[index].color = index % 2 == 0
                    ? new Color(0.55f, 0.482f, 0.275f, 0f)
                    : new Color(1f, 0.92f, 0.2f, 0f);
                riseStars[index].gameObject.SetActive(false);
            }

            SerializedObject data = new(celebration);
            SetRef(data, "collectionTarget", target);
            SetComponentArray(data, "collectionItems", items);
            SetComponentArray(data, "collectionGlows", collectionGlows);
            SetComponentArray(data, "collectionStars", collectionStars);
            SetRef(data, "fishSprite", fish);
            SetRef(data, "catSprite", cat);
            SetRef(data, "arrowGroup", arrowGroup);
            SetComponentArray(data, "arrowItems", arrows);
            SetRef(data, "riseGlow", riseGlow);
            SetComponentArray(data, "riseStars", riseStars);
            data.ApplyModifiedPropertiesWithoutUndo();
            effects.SetAsLastSibling();
            changed = true;
            return celebration;
        }

        private static void SetSourcePoint(
            RectTransform rect,
            Vector2 sourcePosition,
            Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(
                sourcePosition.x,
                -sourcePosition.y);
            rect.sizeDelta = size;
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

            RectTransform visualRoot = CreateRect("VisualRoot", rootRect);
            Stretch(visualRoot);
            Image selfShadow = CreateImage(
                "Shadow",
                visualRoot,
                LoadSprite(RankRoot + "rank_row_self_shadow.png"));
            RectTransform shadowRect = selfShadow.rectTransform;
            shadowRect.anchorMin = shadowRect.anchorMax = new Vector2(0f, 1f);
            shadowRect.pivot = new Vector2(0.5f, 0.5f);
            shadowRect.anchoredPosition = new Vector2(484.5f, -93.55f);
            shadowRect.sizeDelta = new Vector2(1033f, 270f);
            shadowRect.localScale = new Vector3(1f, -1f, 1f);
            Color shadowColor = selfShadow.color;
            shadowColor.a = 0f;
            selfShadow.color = shadowColor;
            selfShadow.gameObject.SetActive(false);
            RectTransform canvasRoot = CreateRect("CanvasGroup", visualRoot);
            Stretch(canvasRoot);
            CanvasGroup contentGroup =
                canvasRoot.gameObject.AddComponent<CanvasGroup>();

            Image background = CreateImage(
                "Background", canvasRoot,
                LoadSprite(RankRoot + "rank_row_bg.png"));
            Stretch(background.rectTransform, new Vector2(-14f, -4f));
            background.rectTransform.offsetMax = new Vector2(14f, 24f);

            Image bigMedal = CreateImage(
                "BigMedal", canvasRoot,
                LoadSprite(RankRoot + "rank_medal_gold.png"));
            SetTop(bigMedal.rectTransform, -14f, -4f, 332f, 208f);
            bigMedal.preserveAspect = true;

            RectTransform content = CreateRect("Content", canvasRoot);
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

            RectTransform badgeRoot = CreateRect("MedalBadge", canvasRoot);
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
            SetRef(data, "visualRoot", visualRoot);
            SetRef(data, "contentGroup", contentGroup);
            SetRef(data, "selfShadow", selfShadow);
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
                typeof(RankActivityPagePresenter),
                typeof(RankActivityPageLayoutPresenter));
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
            podiumArea.sizeDelta = new Vector2(1080f, 521f);
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
            listGroup.anchoredPosition = new Vector2(0f, -203.5f);
            listGroup.sizeDelta = new Vector2(1008f, -1183f);
            Image listBackground = CreateImage(
                "Background", listGroup,
                LoadSprite(RankRoot + "rankpage_list_bg.png"));
            Stretch(listBackground.rectTransform);
            listBackground.type = Image.Type.Sliced;

            RectTransform viewport = CreateRect("Viewport", listGroup);
            Stretch(viewport);
            viewport.offsetMin = new Vector2(0f, 18f);
            viewport.offsetMax = new Vector2(0f, -20f);
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
            scroll.movementType = ScrollRect.MovementType.Clamped;

            Button cta = CreateButton(
                "CtaButton", root,
                LoadSprite(RankRoot + "rankpage_cta.png"), Color.white);
            RectTransform ctaRect = (RectTransform)cta.transform;
            ctaRect.anchorMin = ctaRect.anchorMax = new Vector2(0.5f, 0f);
            ctaRect.pivot = new Vector2(0.5f, 0f);
            ctaRect.anchoredPosition = new Vector2(0f, 130f);
            ctaRect.sizeDelta = new Vector2(784f, 258f);
            Text ctaText = CreateText(
                "Text", cta.transform, font, 58,
                "Go to Collect", Color.white);
            Stretch(ctaText.rectTransform, new Vector2(60f, 25f));

            RectTransform floatingRowLayer = CreateRect("FloatRow", root);
            Stretch(floatingRowLayer);
            floatingRowLayer.SetAsLastSibling();

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
            SetRef(data, "floatingRowLayer", floatingRowLayer);
            SetRef(data, "rowPrefab",
                rowPrefab.GetComponent<RankActivityRowView>());
            SetComponentArray(data, "podiums", podiums);
            SetRef(data, "localization", localization);
            data.ApplyModifiedPropertiesWithoutUndo();

            RankActivityPageLayoutPresenter layout =
                page.GetComponent<RankActivityPageLayoutPresenter>();
            SerializedObject layoutData = new(layout);
            SetRef(layoutData, "layoutSpace", root);
            SetRef(layoutData, "header", header);
            SetRef(layoutData, "podium", podiumArea);
            SetRef(layoutData, "list", listGroup);
            SetRef(layoutData, "cta", ctaRect);
            layoutData.ApplyModifiedPropertiesWithoutUndo();
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
                typeof(RankActivityChangePresenter),
                typeof(RankActivityChangeLayoutPresenter));
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
            SetTop(countdown.rectTransform, 97.5f, 24f, 197f, 40f);

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
            vertical.padding = new RectOffset(0, 0, 200, 200);
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
            scroll.movementType = ScrollRect.MovementType.Clamped;

            RectTransform celebrateLayer = CreateRect("PlayerCelebrate", root);
            Stretch(celebrateLayer);

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
            SetRef(data, "celebrateLayer", celebrateLayer);
            SetRef(data, "rowPrefab",
                rowPrefab.GetComponent<RankActivityRowView>());
            SetRef(data, "maskButton", maskButton);
            SetRef(data, "tapButton", tap);
            SetRef(data, "tapText", tapText);
            SetRef(data, "localization", localization);
            data.ApplyModifiedPropertiesWithoutUndo();

            RankActivityChangeLayoutPresenter layout =
                page.GetComponent<RankActivityChangeLayoutPresenter>();
            SerializedObject layoutData = new(layout);
            SetRef(layoutData, "layoutSpace", root);
            SetRef(layoutData, "encourage", encourage);
            SetRef(layoutData, "title", title.rectTransform);
            SetRef(layoutData, "countdown", countdownRoot);
            SetRef(layoutData, "list", listGroup);
            SetRef(layoutData, "tap", tapRect);
            layoutData.ApplyModifiedPropertiesWithoutUndo();
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
                bool rebuildGift = giftRoot == null ||
                                   giftRoot.Find("Effects") == null;
                if (rebuildGift)
                {
                    if (giftRoot != null)
                        UnityEngine.Object.DestroyImmediate(
                            giftRoot.gameObject);
                    Image backdrop = root.transform.Find("Overlay")
                        ?.GetComponent<Image>();
                    gift = BuildRankGift(
                        root.transform,
                        font,
                        localization,
                        avatarPrefab,
                        backdrop);
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

                Transform effectRoot = root.transform.Find("FrameAddEffect");
                FrameAwardEffectView frameEffect;
                bool rebuildFrameEffect = effectRoot == null ||
                                          effectRoot.Find("Flight") == null;
                if (rebuildFrameEffect)
                {
                    if (effectRoot != null)
                        UnityEngine.Object.DestroyImmediate(
                            effectRoot.gameObject);
                    frameEffect = BuildFrameAddEffect(
                        root.transform,
                        avatarPrefab);
                    effectRoot = frameEffect.transform;
                    changed = true;
                }
                else
                {
                    frameEffect =
                        effectRoot.GetComponent<FrameAwardEffectView>();
                }
                SerializedProperty effectProperty =
                    presenterData.FindProperty("frameAddEffect");
                if (effectProperty != null &&
                    effectProperty.objectReferenceValue != frameEffect)
                {
                    effectProperty.objectReferenceValue = frameEffect;
                    changed = true;
                }
                presenterData.ApplyModifiedPropertiesWithoutUndo();
                giftRoot.gameObject.SetActive(false);
                effectRoot.gameObject.SetActive(false);
                if (!changed) return;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static FrameAwardEffectView BuildFrameAddEffect(
            Transform parent,
            GameObject avatarPrefab)
        {
            RectTransform root = CreateRect("FrameAddEffect", parent);
            Stretch(root);
            root.SetAsLastSibling();
            FrameAwardEffectView view =
                root.gameObject.AddComponent<FrameAwardEffectView>();

            RectTransform ray = CreateRect("EffectRayLight", root);
            SetCentered(ray, Vector2.zero, new Vector2(900f, 900f));
            CanvasGroup rayGroup = ray.gameObject.AddComponent<CanvasGroup>();
            Image glow = CreateImage(
                "Glow",
                ray,
                LoadSprite(RankRoot + "rank_reward_glow.png"));
            Stretch(glow.rectTransform);
            glow.preserveAspect = true;
            glow.raycastTarget = false;

            GameObject avatarObject =
                (GameObject)PrefabUtility.InstantiatePrefab(avatarPrefab);
            avatarObject.name = "AvatarCell";
            avatarObject.transform.SetParent(root, false);
            RectTransform avatarRect =
                (RectTransform)avatarObject.transform;
            SetCentered(avatarRect, Vector2.zero, new Vector2(185f, 185f));
            CanvasGroup avatarGroup =
                avatarObject.GetComponent<CanvasGroup>();
            if (avatarGroup == null)
                avatarGroup = avatarObject.AddComponent<CanvasGroup>();
            ProfileAvatarView avatar =
                avatarObject.GetComponent<ProfileAvatarView>();

            FrameAwardFlightView flight = BuildFrameFlight(root);

            SerializedObject data = new(view);
            SetRef(data, "rayGroup", rayGroup);
            SetRef(data, "rayVisual", ray);
            SetRef(data, "avatarGroup", avatarGroup);
            SetRef(data, "avatarVisual", avatarRect);
            SetRef(data, "avatar", avatar);
            SetRef(data, "flight", flight);
            data.ApplyModifiedPropertiesWithoutUndo();
            root.gameObject.SetActive(false);
            return view;
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
            GameObject avatarPrefab,
            Image backdrop)
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
            CanvasGroup winGroup = win.gameObject.AddComponent<CanvasGroup>();

            RectTransform chestRoot = CreateRect("Box", root);
            chestRoot.anchorMin =
                chestRoot.anchorMax = new Vector2(0.5f, 1f);
            chestRoot.pivot = new Vector2(0.5f, 1f);
            chestRoot.anchoredPosition = new Vector2(0f, -390f);
            chestRoot.sizeDelta = new Vector2(520f, 520f);
            CanvasGroup chestGroup =
                chestRoot.gameObject.AddComponent<CanvasGroup>();
            RectTransform animatedBox = CreateRect("AnimatedBox", chestRoot);
            SetCentered(animatedBox, Vector2.zero, new Vector2(520f, 520f));
            Image glow = CreateImage(
                "Glow", animatedBox,
                LoadSprite(RankRoot + "rank_reward_glow.png"));
            Stretch(glow.rectTransform);
            glow.preserveAspect = true;
            Image chest = CreateImage(
                "Chest", animatedBox,
                LoadSprite(RankRoot + "chest_tier3.png"));
            SetCentered(chest.rectTransform, Vector2.zero,
                new Vector2(466f, 466f));
            chest.preserveAspect = true;

            RectTransform podium = CreateRect("Podium", root);
            podium.anchorMin =
                podium.anchorMax = new Vector2(0.5f, 1f);
            podium.pivot = new Vector2(0.5f, 1f);
            podium.anchoredPosition = new Vector2(0f, -930f);
            podium.sizeDelta = new Vector2(1080f, 347f);
            CanvasGroup podiumGroup =
                podium.gameObject.AddComponent<CanvasGroup>();
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
            var seats = new RectTransform[avatars.Length];
            var seatGroups = new CanvasGroup[avatars.Length];
            var avatarVisuals = new RectTransform[avatars.Length];
            var avatarGroups = new CanvasGroup[avatars.Length];
            for (int index = 0; index < avatars.Length; index++)
            {
                avatarVisuals[index] = avatars[index].transform as RectTransform;
                seats[index] = avatarVisuals[index]?.parent as RectTransform;
                if (seats[index] != null)
                    seatGroups[index] = seats[index].gameObject
                        .AddComponent<CanvasGroup>();
                if (avatarVisuals[index] != null)
                {
                    avatarGroups[index] = avatarVisuals[index]
                        .GetComponent<CanvasGroup>();
                    if (avatarGroups[index] == null)
                        avatarGroups[index] = avatarVisuals[index]
                            .gameObject.AddComponent<CanvasGroup>();
                }
            }

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
            CanvasGroup collectGroup =
                collect.gameObject.AddComponent<CanvasGroup>();

            BuildRankGiftEffects(
                root,
                out RectTransform[] burstRoots,
                out Image[] burstGlows,
                out Image[] burstStars);

            SerializedObject data = new(view);
            SetRef(data, "backdrop", backdrop);
            SetRef(data, "winText", win);
            SetRef(data, "winGroup", winGroup);
            SetRef(data, "chestRoot", chestRoot.gameObject);
            SetRef(data, "chestImage", chest);
            SetRef(data, "chestGroup", chestGroup);
            SetRef(data, "chestVisual", animatedBox);
            SetRef(data, "chestGlow", glow);
            SetSpriteArray(data, "chestTiers", ChestSprites());
            SetRef(data, "podiumVisual", podium);
            SetRef(data, "podiumGroup", podiumGroup);
            SetComponentArray(data, "podiumSeats", seats);
            SetComponentArray(data, "podiumSeatGroups", seatGroups);
            SetComponentArray(data, "podiumAvatars", avatars);
            SetComponentArray(data, "podiumAvatarVisuals", avatarVisuals);
            SetComponentArray(data, "podiumAvatarGroups", avatarGroups);
            SetRef(data, "collectButton", collect);
            SetRef(data, "collectText", collectText);
            SetRef(data, "collectGroup", collectGroup);
            SetComponentArray(data, "burstRoots", burstRoots);
            SetComponentArray(data, "burstGlows", burstGlows);
            SetComponentArray(data, "burstStars", burstStars);
            SetRef(data, "localization", localization);
            data.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static void BuildRankGiftEffects(
            RectTransform parent,
            out RectTransform[] roots,
            out Image[] glows,
            out Image[] stars)
        {
            RectTransform effects = CreateRect("Effects", parent);
            Stretch(effects);
            effects.SetAsLastSibling();
            Vector2[] positions =
            {
                new(0f, 300f),
                new(300f, 318f),
                new(-276f, 274f),
                new(164f, -169f)
            };
            roots = new RectTransform[positions.Length];
            glows = new Image[positions.Length];
            stars = new Image[positions.Length * 8];
            Sprite glowSprite = LoadSprite(
                EffectsRoot + "glow/et_glow_002.png");
            Sprite starSprite = LoadSprite(
                EffectsRoot + "star/et_star_1.png");
            for (int group = 0; group < positions.Length; group++)
            {
                RectTransform burst = CreateRect(
                    $"Firework_{group + 1}",
                    effects);
                SetCentered(burst, positions[group], new Vector2(40f, 40f));
                roots[group] = burst;
                Image glow = CreateImage("Glow", burst, glowSprite);
                SetCentered(glow.rectTransform, Vector2.zero,
                    new Vector2(300f, 300f));
                glow.preserveAspect = true;
                glows[group] = glow;
                for (int index = 0; index < 8; index++)
                {
                    Image star = CreateImage(
                        $"Star_{index + 1}",
                        burst,
                        starSprite);
                    SetCentered(star.rectTransform, Vector2.zero,
                        new Vector2(42f, 42f));
                    star.preserveAspect = true;
                    stars[group * 8 + index] = star;
                }
            }
        }

        private static FrameAwardFlightView BuildFrameFlight(
            RectTransform parent)
        {
            RectTransform root = CreateRect("Flight", parent);
            Stretch(root);
            root.SetAsLastSibling();
            FrameAwardFlightView view =
                root.gameObject.AddComponent<FrameAwardFlightView>();

            Sprite trailSprite = LoadSprite(
                EffectsRoot + "trail/et_trail_001.png");
            Sprite pointSprite = LoadSprite(
                EffectsRoot + "glow/et_glow_005.png");
            Sprite glowSprite = LoadSprite(
                EffectsRoot + "glow/et_glow_002.png");
            Sprite starSprite = LoadSprite(
                EffectsRoot + "star/et_star_1.png");
            Image[] trail = new Image[16];
            for (int index = trail.Length - 1; index >= 0; index--)
            {
                Image segment = CreateImage(
                    $"Trail_{index + 1}",
                    root,
                    trailSprite);
                SetCentered(segment.rectTransform, Vector2.zero,
                    new Vector2(3f, 30f));
                segment.type = Image.Type.Sliced;
                segment.color = new Color(1f, 0.86f, 0.25f, 0f);
                trail[index] = segment;
            }
            Image point = CreateImage("Point", root, pointSprite);
            SetCentered(point.rectTransform, Vector2.zero,
                new Vector2(82f, 82f));
            point.preserveAspect = true;
            point.color = new Color(1f, 0.933f, 0.6f, 1f);

            Image burstGlow = CreateImage("BurstGlow", root, glowSprite);
            SetCentered(burstGlow.rectTransform, Vector2.zero,
                new Vector2(300f, 300f));
            burstGlow.preserveAspect = true;
            var burstStars = new Image[12];
            for (int index = 0; index < burstStars.Length; index++)
            {
                Image star = CreateImage(
                    $"BurstStar_{index + 1}",
                    root,
                    starSprite);
                SetCentered(star.rectTransform, Vector2.zero,
                    new Vector2(42f, 42f));
                star.preserveAspect = true;
                burstStars[index] = star;
            }

            SerializedObject data = new(view);
            SetRef(data, "point", point);
            SetComponentArray(data, "trailSegments", trail);
            SetRef(data, "burstGlow", burstGlow);
            SetComponentArray(data, "burstStars", burstStars);
            data.ApplyModifiedPropertiesWithoutUndo();
            root.gameObject.SetActive(false);
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
