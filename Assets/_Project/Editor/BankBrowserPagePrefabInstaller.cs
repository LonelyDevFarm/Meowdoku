using Meowdoku.Core.UI;
using Meowdoku.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Editor
{
    [InitializeOnLoad]
    internal static class BankBrowserPagePrefabInstaller
    {
        private const string PrefabPath =
            "Assets/_Project/Prefabs/UI/BankPage.prefab";
        private const string FontPath =
            "Assets/_Project/Fonts/NotoSourceHan-subset.ttf";
        private const string RoundedShaderPath =
            "Assets/_Project/Shaders/UIRoundedRect.shader";

        static BankBrowserPagePrefabInstaller()
        {
            EditorApplication.delayCall += InstallIfMissing;
            EditorApplication.playModeStateChanged += HandlePlayModeChanged;
        }

        [MenuItem("Meowdoku/Port/Create Bank Page Prefab")]
        private static void InstallFromMenu()
        {
            InstallIfMissing();
            Selection.activeObject =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        internal static bool InstallIfReady()
        {
            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
                return false;

            InstallIfMissing();
            return true;
        }

        private static void HandlePlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += InstallIfMissing;
        }

        private static void InstallIfMissing()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += InstallIfMissing;
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            GameObject existing =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null && !NeedsUpgrade(existing))
            {
                UIRegistryAssetInstaller.InstallIfReady();
                return;
            }

            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            Shader rounded =
                AssetDatabase.LoadAssetAtPath<Shader>(RoundedShaderPath);
            if (font == null || rounded == null) return;

            GameObject page = Build(font, rounded);
            try
            {
                PrefabUtility.SaveAsPrefabAsset(page, PrefabPath);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                Object.DestroyImmediate(page);
            }
            UIRegistryAssetInstaller.InstallIfReady();
        }

        private static bool NeedsUpgrade(GameObject prefab)
        {
            if (prefab == null ||
                prefab.GetComponent<BankBrowserPagePresenter>() == null)
                return true;

            Text title = prefab.transform.Find("Header/TitleLabel")
                ?.GetComponent<Text>();
            if (title == null || title.text != "Puzzle Bank")
                return true;

            Text regularCardTitle = prefab.transform.Find(
                    "RootPanel/RootScroll/Viewport/Content/RegularCard/Title")
                ?.GetComponent<Text>();
            return regularCardTitle == null ||
                   regularCardTitle.fontSize != 36 ||
                   regularCardTitle.verticalOverflow != VerticalWrapMode.Overflow;
        }

        private static GameObject Build(Font font, Shader rounded)
        {
            GameObject page = new("BankPage", typeof(RectTransform));
            RectTransform pageRect = page.GetComponent<RectTransform>();
            Stretch(pageRect);
            Canvas canvas = page.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = page.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            page.AddComponent<GraphicRaycaster>();
            CanvasGroup canvasGroup = page.AddComponent<CanvasGroup>();
            BankBrowserPagePresenter presenter =
                page.AddComponent<BankBrowserPagePresenter>();

            Image background = CreateImage("Background", pageRect);
            Stretch(background.rectTransform);
            background.color = new Color(0.941f, 0.929f, 0.91f, 1f);
            background.raycastTarget = false;

            RectTransform header = CreateRect("Header", pageRect);
            SetTopLeft(header, 0f, 0f, 1080f, 220f);
            Button homeBack = CreateButton(
                "BackBtn", header, rounded, 16f, new Color(0.3f, 0.3f, 0.3f));
            SetTopLeft((RectTransform)homeBack.transform,
                32f, 60f, 200f, 100f);
            Text homeBackText = CreateText(
                "Label", homeBack.transform, font, 30, Color.white,
                TextAnchor.MiddleCenter);
            Stretch(homeBackText.rectTransform);
            homeBackText.text = "Home";
            Text title = CreateText(
                "TitleLabel", header, font, 56,
                new Color(0.2f, 0.2f, 0.2f), TextAnchor.MiddleCenter);
            SetTopLeft(title.rectTransform, 240f, 60f, 600f, 100f);
            title.text = "Puzzle Bank";

            GameObject rootPanel = CreatePanel("RootPanel", pageRect, true);
            ScrollRect rootScroll = CreateScroll(
                rootPanel.transform,
                "RootScroll",
                50f, 40f, 980f, 1660f,
                24f,
                out RectTransform rootContent);
            BankRootCardView regularCard = CreateRootCard(
                "RegularCard", rootContent, font, rounded);
            BankRootCardView lkCard = CreateRootCard(
                "LKCard", rootContent, font, rounded);
            BankRootCardView lkModifiedCard = CreateRootCard(
                "LKModifiedCard", rootContent, font, rounded);
            BankRootCardView lkStyleCard = CreateRootCard(
                "LKStyleCard", rootContent, font, rounded);
            BankRootCardView gcCard = CreateRootCard(
                "GCCard", rootContent, font, rounded);
            BankRootCardView specialCard = CreateRootCard(
                "SPCard", rootContent, font, rounded);

            GameObject regularPanel = CreatePanel(
                "RegularSizePanel", pageRect, false);
            CreatePanelHeader(
                regularPanel.transform,
                "RegHeader",
                "Regular Puzzles",
                new Color(0.18f, 0.55f, 0.28f),
                font,
                rounded,
                out Button regularBack,
                out _);
            ScrollRect regularScroll = CreateScroll(
                regularPanel.transform,
                "RegScroll",
                50f, 160f, 980f, 1540f,
                24f,
                out RectTransform regularContent);
            BankSizeCardView regularTemplate = CreateSizeCard(
                "SizeCardTemplate", regularContent, font, rounded);
            regularTemplate.gameObject.SetActive(false);

            GameObject variantPanel = CreatePanel(
                "VariantSizePanel", pageRect, false);
            CreatePanelHeader(
                variantPanel.transform,
                "VariantHeader",
                "LK Style",
                new Color(0.38f, 0.18f, 0.72f),
                font,
                rounded,
                out Button variantBack,
                out Text variantTitle);
            ScrollRect variantScroll = CreateScroll(
                variantPanel.transform,
                "VariantScroll",
                50f, 160f, 980f, 1540f,
                24f,
                out RectTransform variantContent);
            BankSizeCardView variantTemplate = CreateSizeCard(
                "SizeCardTemplate", variantContent, font, rounded);
            variantTemplate.gameObject.SetActive(false);

            GameObject tierPanel = CreatePanel("TierPanel", pageRect, false);
            CreatePanelHeader(
                tierPanel.transform,
                "TierHeader",
                string.Empty,
                new Color(0.2f, 0.2f, 0.2f),
                font,
                rounded,
                out Button tierBack,
                out Text tierTitle);
            Text tierSubtitle = CreateText(
                "TierSubtitle", tierPanel.transform, font, 30,
                new Color(0.55f, 0.55f, 0.55f),
                TextAnchor.MiddleCenter);
            SetTopLeft(tierSubtitle.rectTransform, 24f, 148f, 1032f, 48f);
            tierSubtitle.text = "Choose difficulty";
            ScrollRect tierScroll = CreateScroll(
                tierPanel.transform,
                "TierScroll",
                24f, 200f, 1032f, 1500f,
                20f,
                out RectTransform tierContent);
            BankTierCardView tierTemplate = CreateTierCard(
                "TierCardTemplate", tierContent, font, rounded);
            tierTemplate.gameObject.SetActive(false);

            GameObject listPanel = CreatePanel("ListPanel", pageRect, false);
            CreatePanelHeader(
                listPanel.transform,
                "ListHeader",
                "SP Pattern Puzzles",
                new Color(0.2f, 0.2f, 0.2f),
                font,
                rounded,
                out Button listBack,
                out Text listTitle);
            ScrollRect listScroll = CreateScroll(
                listPanel.transform,
                "ListScroll",
                0f, 160f, 1080f, 1540f,
                2f,
                out RectTransform listContent);
            BankLevelRowView specialTemplate = CreateLevelRow(
                "SpecialRowTemplate", listContent, font, rounded, 140f);
            specialTemplate.gameObject.SetActive(false);

            GameObject lkPanel = CreatePanel("LKPanel", pageRect, false);
            CreatePanelHeader(
                lkPanel.transform,
                "LKHeader",
                "LK Archive",
                new Color(0.04f, 0.4f, 0.76f),
                font,
                rounded,
                out Button lkBack,
                out Text lkTitle);
            Text lkInfo = CreateText(
                "LKInfoLabel", lkPanel.transform, font, 30,
                new Color(0.55f, 0.55f, 0.55f),
                TextAnchor.MiddleCenter);
            SetTopLeft(lkInfo.rectTransform, 24f, 148f, 1032f, 48f);
            RectTransform selector = CreateRoundedPanel(
                "LKSelector", lkPanel.transform, rounded, 20f, Color.white);
            SetTopLeft(selector, 24f, 210f, 1032f, 180f);
            Text prompt = CreateText(
                "Prompt", selector, font, 32,
                new Color(0.2f, 0.2f, 0.2f), TextAnchor.MiddleLeft);
            SetTopLeft(prompt.rectTransform, 32f, 40f, 420f, 100f);
            prompt.text = "Enter level number";
            Button lkMinus = CreateSmallButton(
                "MinusBtn", selector, font, rounded, "-",
                new Color(0.04f, 0.4f, 0.76f));
            SetTopLeft((RectTransform)lkMinus.transform,
                500f, 45f, 90f, 90f);
            Text lkNumber = CreateText(
                "Number", selector, font, 40,
                new Color(0.2f, 0.2f, 0.2f), TextAnchor.MiddleCenter);
            SetTopLeft(lkNumber.rectTransform, 600f, 45f, 100f, 90f);
            lkNumber.text = "1";
            Button lkPlus = CreateSmallButton(
                "PlusBtn", selector, font, rounded, "+",
                new Color(0.04f, 0.4f, 0.76f));
            SetTopLeft((RectTransform)lkPlus.transform,
                710f, 45f, 90f, 90f);
            Button lkGo = CreateSmallButton(
                "GoBtn", selector, font, rounded, "GO",
                new Color(0.04f, 0.4f, 0.76f));
            SetTopLeft((RectTransform)lkGo.transform,
                820f, 45f, 160f, 90f);
            ScrollRect lkScroll = CreateScroll(
                lkPanel.transform,
                "LKScroll",
                0f, 400f, 1080f, 1300f,
                2f,
                out RectTransform lkContent);
            BankLevelRowView lkTemplate = CreateLevelRow(
                "LKRowTemplate", lkContent, font, rounded, 110f);
            lkTemplate.gameObject.SetActive(false);

            SerializedObject frame = new(presenter);
            Set(frame, "uiLayer", (int)UiLayer.Default);
            Set(frame, "isFullscreen", true);
            Set(frame, "showMask", false);
            Set(frame, "rootCanvas", canvas);
            Set(frame, "rootCanvasGroup", canvasGroup);
            Set(frame, "homeBackButton", homeBack);
            Set(frame, "rootPanel", rootPanel);
            Set(frame, "regularSizePanel", regularPanel);
            Set(frame, "tierPanel", tierPanel);
            Set(frame, "levelListPanel", listPanel);
            Set(frame, "lkPanel", lkPanel);
            Set(frame, "variantSizePanel", variantPanel);
            Set(frame, "regularCard", regularCard);
            Set(frame, "lkCard", lkCard);
            Set(frame, "lkModifiedCard", lkModifiedCard);
            Set(frame, "lkStyleCard", lkStyleCard);
            Set(frame, "gcCard", gcCard);
            Set(frame, "specialCard", specialCard);
            Set(frame, "regularSizeBackButton", regularBack);
            Set(frame, "regularSizeScroll", regularScroll);
            Set(frame, "regularSizeContent", regularContent);
            Set(frame, "regularSizeTemplate", regularTemplate);
            Set(frame, "variantSizeBackButton", variantBack);
            Set(frame, "variantSizeTitle", variantTitle);
            Set(frame, "variantSizeScroll", variantScroll);
            Set(frame, "variantSizeContent", variantContent);
            Set(frame, "variantSizeTemplate", variantTemplate);
            Set(frame, "tierBackButton", tierBack);
            Set(frame, "tierTitle", tierTitle);
            Set(frame, "tierScroll", tierScroll);
            Set(frame, "tierContent", tierContent);
            Set(frame, "tierTemplate", tierTemplate);
            Set(frame, "levelListBackButton", listBack);
            Set(frame, "levelListTitle", listTitle);
            Set(frame, "levelListScroll", listScroll);
            Set(frame, "levelListContent", listContent);
            Set(frame, "specialRowTemplate", specialTemplate);
            Set(frame, "lkBackButton", lkBack);
            Set(frame, "lkTitle", lkTitle);
            Set(frame, "lkInfoLabel", lkInfo);
            Set(frame, "lkMinusButton", lkMinus);
            Set(frame, "lkPlusButton", lkPlus);
            Set(frame, "lkGoButton", lkGo);
            Set(frame, "lkNumberLabel", lkNumber);
            Set(frame, "lkScroll", lkScroll);
            Set(frame, "lkContent", lkContent);
            Set(frame, "lkRowTemplate", lkTemplate);
            frame.ApplyModifiedPropertiesWithoutUndo();
            return page;
        }

        private static GameObject CreatePanel(
            string name,
            RectTransform parent,
            bool active)
        {
            RectTransform rect = CreateRect(name, parent);
            Stretch(rect);
            rect.offsetMax = new Vector2(0f, -220f);
            rect.gameObject.SetActive(active);
            return rect.gameObject;
        }

        private static void CreatePanelHeader(
            Transform parent,
            string name,
            string titleText,
            Color titleColor,
            Font font,
            Shader rounded,
            out Button back,
            out Text title)
        {
            RectTransform header = CreateRect(name, parent);
            SetTopLeft(header, 24f, 20f, 1032f, 120f);
            back = CreateButton(
                "BackBtn", header, rounded, 16f,
                new Color(0.25f, 0.45f, 0.75f));
            SetTopLeft((RectTransform)back.transform,
                0f, 10f, 180f, 100f);
            Text backText = CreateText(
                "Label", back.transform, font, 30, Color.white,
                TextAnchor.MiddleCenter);
            Stretch(backText.rectTransform);
            backText.text = "< Back";
            title = CreateText(
                "Title", header, font, 44, titleColor,
                TextAnchor.MiddleLeft);
            SetTopLeft(title.rectTransform, 200f, 10f, 832f, 100f);
            title.text = titleText;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 24;
        }

        private static BankRootCardView CreateRootCard(
            string name,
            Transform parent,
            Font font,
            Shader rounded)
        {
            Button button = CreateLayoutButton(
                name, parent, rounded, 24f, Color.white, 170f);
            Image image = button.GetComponent<Image>();
            Text title = CreateText(
                "Title", button.transform, font, 36,
                new Color(0.2f, 0.2f, 0.2f), TextAnchor.MiddleLeft);
            SetTopLeft(title.rectTransform, 48f, 10f, 650f, 70f);
            title.verticalOverflow = VerticalWrapMode.Overflow;
            Text subtitle = CreateText(
                "Subtitle", button.transform, font, 25,
                new Color(0.53f, 0.53f, 0.53f), TextAnchor.MiddleLeft);
            SetTopLeft(subtitle.rectTransform, 48f, 78f, 690f, 42f);
            Text metadata = CreateText(
                "Metadata", button.transform, font, 22,
                new Color(0.53f, 0.53f, 0.53f), TextAnchor.MiddleLeft);
            SetTopLeft(metadata.rectTransform, 48f, 120f, 690f, 34f);
            Text count = CreateText(
                "Count", button.transform, font, 31,
                new Color(0.53f, 0.53f, 0.53f), TextAnchor.MiddleRight);
            SetTopLeft(count.rectTransform, 740f, 45f, 150f, 50f);
            Text arrow = CreateText(
                "Arrow", button.transform, font, 56,
                new Color(0.53f, 0.53f, 0.53f), TextAnchor.MiddleRight);
            SetTopLeft(arrow.rectTransform, 900f, 38f, 50f, 80f);
            arrow.text = ">";
            BankRootCardView view =
                button.gameObject.AddComponent<BankRootCardView>();
            SerializedObject data = new(view);
            Set(data, "button", button);
            Set(data, "background", image);
            Set(data, "title", title);
            Set(data, "subtitle", subtitle);
            Set(data, "count", count);
            Set(data, "metadata", metadata);
            Set(data, "arrow", arrow);
            data.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static BankSizeCardView CreateSizeCard(
            string name,
            Transform parent,
            Font font,
            Shader rounded)
        {
            Button button = CreateLayoutButton(
                name, parent, rounded, 24f, Color.white, 170f);
            Text size = CreateText(
                "Size", button.transform, font, 56,
                new Color(0.2f, 0.2f, 0.2f), TextAnchor.MiddleLeft);
            SetTopLeft(size.rectTransform, 48f, 25f, 430f, 66f);
            Text tier = CreateText(
                "Tier", button.transform, font, 28,
                new Color(0.53f, 0.53f, 0.53f), TextAnchor.MiddleLeft);
            SetTopLeft(tier.rectTransform, 48f, 92f, 430f, 45f);
            Text count = CreateText(
                "Count", button.transform, font, 32,
                new Color(0.53f, 0.53f, 0.53f), TextAnchor.MiddleRight);
            SetTopLeft(count.rectTransform, 550f, 32f, 330f, 50f);
            Text ranks = CreateText(
                "Ranks", button.transform, font, 24,
                new Color(0.53f, 0.53f, 0.53f), TextAnchor.MiddleRight);
            SetTopLeft(ranks.rectTransform, 500f, 88f, 380f, 44f);
            Text arrow = CreateText(
                "Arrow", button.transform, font, 56,
                new Color(0.53f, 0.53f, 0.53f), TextAnchor.MiddleRight);
            SetTopLeft(arrow.rectTransform, 900f, 40f, 50f, 80f);
            arrow.text = ">";
            BankSizeCardView view =
                button.gameObject.AddComponent<BankSizeCardView>();
            SerializedObject data = new(view);
            Set(data, "button", button);
            Set(data, "sizeLabel", size);
            Set(data, "tierLabel", tier);
            Set(data, "countLabel", count);
            Set(data, "ranksLabel", ranks);
            data.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static BankTierCardView CreateTierCard(
            string name,
            Transform parent,
            Font font,
            Shader rounded)
        {
            RectTransform root = CreateRoundedPanel(
                name, parent, rounded, 20f, Color.white);
            LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 200f;
            Image background = root.GetComponent<Image>();
            RectTransform badge = CreateRoundedPanel(
                "Badge", root, rounded, 12f, Color.green);
            SetTopLeft(badge, 28f, 20f, 230f, 55f);
            Text badgeText = CreateText(
                "Label", badge, font, 27, Color.white,
                TextAnchor.MiddleCenter);
            Stretch(badgeText.rectTransform);
            Text description = CreateText(
                "Description", root, font, 30,
                new Color(0.2f, 0.2f, 0.2f), TextAnchor.MiddleLeft);
            SetTopLeft(description.rectTransform, 278f, 20f, 700f, 55f);
            description.resizeTextForBestFit = true;
            description.resizeTextMinSize = 20;
            Text count = CreateText(
                "Count", root, font, 26,
                new Color(0.53f, 0.53f, 0.53f), TextAnchor.MiddleLeft);
            SetTopLeft(count.rectTransform, 28f, 82f, 400f, 42f);
            Button minus = CreateSmallButton(
                "MinusBtn", root, font, rounded, "-", Color.gray);
            SetTopLeft((RectTransform)minus.transform,
                565f, 130f, 74f, 58f);
            Text number = CreateText(
                "Number", root, font, 38,
                new Color(0.2f, 0.2f, 0.2f), TextAnchor.MiddleCenter);
            SetTopLeft(number.rectTransform, 645f, 130f, 70f, 58f);
            number.text = "1";
            Button plus = CreateSmallButton(
                "PlusBtn", root, font, rounded, "+", Color.gray);
            SetTopLeft((RectTransform)plus.transform,
                720f, 130f, 74f, 58f);
            Button go = CreateSmallButton(
                "GoBtn", root, font, rounded, "GO", Color.green);
            SetTopLeft((RectTransform)go.transform,
                810f, 124f, 160f, 70f);
            BankTierCardView view =
                root.gameObject.AddComponent<BankTierCardView>();
            SerializedObject data = new(view);
            Set(data, "background", background);
            Set(data, "badgeBackground", badge.GetComponent<Image>());
            Set(data, "badgeLabel", badgeText);
            Set(data, "descriptionLabel", description);
            Set(data, "countLabel", count);
            Set(data, "numberLabel", number);
            Set(data, "minusButton", minus);
            Set(data, "plusButton", plus);
            Set(data, "goButton", go);
            Set(data, "goBackground", go.GetComponent<Image>());
            data.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static BankLevelRowView CreateLevelRow(
            string name,
            Transform parent,
            Font font,
            Shader rounded,
            float height)
        {
            Button button = CreateLayoutButton(
                name, parent, rounded, 0f, Color.white, height);
            Image background = button.GetComponent<Image>();
            Text index = CreateText(
                "Index", button.transform, font, 30,
                new Color(0.04f, 0.4f, 0.76f), TextAnchor.MiddleLeft);
            SetTopLeft(index.rectTransform, 36f, 0f, 100f, height);
            Text primary = CreateText(
                "Primary", button.transform, font, 36,
                new Color(0.2f, 0.2f, 0.2f), TextAnchor.MiddleLeft);
            SetTopLeft(primary.rectTransform, 140f, 12f, 350f, 58f);
            Text secondary = CreateText(
                "Secondary", button.transform, font, 26,
                new Color(0.53f, 0.53f, 0.53f), TextAnchor.MiddleLeft);
            SetTopLeft(secondary.rectTransform,
                140f, height > 120f ? 72f : 52f, 510f, 44f);
            RectTransform badge = CreateRoundedPanel(
                "Badge", button.transform, rounded, 10f, Color.gray);
            SetTopLeft(badge, 760f, (height - 54f) * 0.5f, 160f, 54f);
            Text badgeText = CreateText(
                "Label", badge, font, 25, Color.white,
                TextAnchor.MiddleCenter);
            Stretch(badgeText.rectTransform);
            Text arrow = CreateText(
                "Arrow", button.transform, font, 50,
                new Color(0.04f, 0.4f, 0.76f), TextAnchor.MiddleRight);
            SetTopLeft(arrow.rectTransform, 960f, 0f, 60f, height);
            arrow.text = ">";
            BankLevelRowView view =
                button.gameObject.AddComponent<BankLevelRowView>();
            SerializedObject data = new(view);
            Set(data, "button", button);
            Set(data, "background", background);
            Set(data, "indexLabel", index);
            Set(data, "primaryLabel", primary);
            Set(data, "secondaryLabel", secondary);
            Set(data, "badgeLabel", badgeText);
            Set(data, "badgeBackground", badge.GetComponent<Image>());
            Set(data, "arrowLabel", arrow);
            data.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static ScrollRect CreateScroll(
            Transform parent,
            string name,
            float left,
            float top,
            float width,
            float height,
            float spacing,
            out RectTransform content)
        {
            RectTransform root = CreateRect(name, parent);
            SetTopLeft(root, left, top, width, height);
            ScrollRect scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 50f;
            RectTransform viewport = CreateRect("Viewport", root);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            VerticalLayoutGroup layout =
                content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter =
                content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;
            return scroll;
        }

        private static Button CreateLayoutButton(
            string name,
            Transform parent,
            Shader rounded,
            float radius,
            Color color,
            float height)
        {
            RectTransform rect = CreateRoundedPanel(
                name, parent, rounded, radius, color);
            LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            return button;
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            Shader rounded,
            float radius,
            Color color)
        {
            RectTransform rect = CreateRoundedPanel(
                name, parent, rounded, radius, color);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            return button;
        }

        private static Button CreateSmallButton(
            string name,
            Transform parent,
            Font font,
            Shader rounded,
            string label,
            Color color)
        {
            Button button = CreateButton(
                name, parent, rounded, 12f, color);
            Text text = CreateText(
                "Label", button.transform, font, 34, Color.white,
                TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            text.text = label;
            return button;
        }

        private static RectTransform CreateRoundedPanel(
            string name,
            Transform parent,
            Shader rounded,
            float radius,
            Color color)
        {
            Image image = CreateImage(name, parent);
            image.color = color;
            if (radius > 0f)
            {
                RoundedImageView view =
                    image.gameObject.AddComponent<RoundedImageView>();
                view.Configure(image, rounded, radius);
            }
            return image.rectTransform;
        }

        private static Image CreateImage(string name, Transform parent)
        {
            RectTransform rect = CreateRect(name, parent);
            return rect.gameObject.AddComponent<Image>();
        }

        private static Text CreateText(
            string name,
            Transform parent,
            Font font,
            int size,
            Color color,
            TextAnchor alignment)
        {
            RectTransform rect = CreateRect(name, parent);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void SetTopLeft(
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
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Set(SerializedObject data, string name, Object value)
        {
            data.FindProperty(name).objectReferenceValue = value;
        }

        private static void Set(SerializedObject data, string name, bool value)
        {
            data.FindProperty(name).boolValue = value;
        }

        private static void Set(SerializedObject data, string name, int value)
        {
            data.FindProperty(name).intValue = value;
        }
    }
}
