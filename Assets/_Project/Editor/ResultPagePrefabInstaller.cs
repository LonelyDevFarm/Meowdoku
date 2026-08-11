using Meowdoku.Core.Localization;
using Meowdoku.Core.UI;
using Meowdoku.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Editor
{
    [InitializeOnLoad]
    internal static class ResultPagePrefabInstaller
    {
        internal const string WinPrefabPath =
            "Assets/_Project/Prefabs/UI/WinPage.prefab";
        internal const string FailPrefabPath =
            "Assets/_Project/Prefabs/UI/FailPage.prefab";
        private const string FontPath = "Assets/_Project/Fonts/Roboto.ttf";
        private const string RoundedShaderPath =
            "Assets/_Project/Shaders/UIRoundedRect.shader";
        private const string WinCatPath =
            "Assets/_Project/Sprites/win/cat_victory.png";
        private const string RayPath =
            "Assets/_Project/Sprites/win/ray_light.png";
        private const string FailCatPath =
            "Assets/_Project/Sprites/fail/cat_crying.png";
        private const string FailFacePath =
            "Assets/_Project/Sprites/fail/cat_face.png";
        private const string PassPanelPath =
            "Assets/_Project/Sprites/result/pass_page_g1/panel_bg_v2.png";
        private const string CompletionIconPath =
            "Assets/_Project/Sprites/result/pass_page_g2/completion_rate.png";
        private const string MistakeIconPath =
            "Assets/_Project/Sprites/result/pass_page_g2/error_count.png";
        private const string ToolIconPath =
            "Assets/_Project/Sprites/result/pass_page_g2/hint_count.png";

        private static readonly Color Cream =
            new(1f, 0.965f, 0.925f, 1f);
        private static readonly Color Brown =
            new(0.455f, 0.31f, 0.22f, 1f);
        private static readonly Color Orange =
            new(1f, 0.541f, 0.016f, 1f);
        private static readonly Color Blue =
            new(0.447f, 0.655f, 0.859f, 1f);
        private static readonly Color PaleBlue =
            new(0.875f, 0.937f, 1f, 1f);

        static ResultPagePrefabInstaller()
        {
            EditorApplication.delayCall += InstallIfReady;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                    EditorApplication.delayCall += InstallIfReady;
            };
        }

        [MenuItem("Meowdoku/Port/Create Result Page Prefabs")]
        private static void InstallFromMenu()
        {
            InstallIfReady();
            Selection.activeObject =
                AssetDatabase.LoadAssetAtPath<GameObject>(WinPrefabPath);
        }

        internal static void InstallIfReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += InstallIfReady;
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            Shader rounded = AssetDatabase.LoadAssetAtPath<Shader>(
                RoundedShaderPath);
            LocalizationCatalog localization =
                LocalizationCatalogAssetInstaller.GetOrCreate();
            Sprite winCat = LoadSprite(WinCatPath);
            Sprite ray = LoadSprite(RayPath);
            Sprite failCat = LoadSprite(FailCatPath);
            Sprite failFace = LoadSprite(FailFacePath);
            Sprite passPanel = LoadSprite(PassPanelPath);
            Sprite completionIcon = LoadSprite(CompletionIconPath);
            Sprite mistakeIcon = LoadSprite(MistakeIconPath);
            Sprite toolIcon = LoadSprite(ToolIconPath);
            if (font == null || rounded == null || localization == null ||
                winCat == null || ray == null || failCat == null ||
                failFace == null || passPanel == null ||
                completionIcon == null || mistakeIcon == null ||
                toolIcon == null)
                return;

            EnsureFolder("Assets/_Project/Prefabs", "UI");
            GameObject winPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(WinPrefabPath);
            if (winPrefab == null ||
                winPrefab.transform.Find("Root/PassPanel") == null ||
                winPrefab.transform.Find("Root/DailyVisuals") == null)
                Save(BuildWin(
                        font,
                        rounded,
                        localization,
                        winCat,
                        ray,
                        passPanel,
                        completionIcon,
                        mistakeIcon,
                        toolIcon),
                    WinPrefabPath);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(FailPrefabPath) == null)
                Save(BuildFail(font, rounded, localization, failCat, failFace),
                    FailPrefabPath);
            UIRegistryAssetInstaller.InstallIfReady();
        }

        private static GameObject BuildWin(
            Font font,
            Shader rounded,
            LocalizationCatalog localization,
            Sprite catSprite,
            Sprite raySprite,
            Sprite passPanelSprite,
            Sprite completionIcon,
            Sprite mistakeIcon,
            Sprite toolIcon)
        {
            GameObject page = CreatePage<GameWinPagePresenter>("WinPage");
            Canvas canvas = page.GetComponent<Canvas>();
            CanvasGroup pageGroup = page.GetComponent<CanvasGroup>();
            RectTransform root = CreateReferenceRoot(page.transform);

            RectTransform visuals = CreateRect("Visuals", root);
            Stretch(visuals);
            Image ray = CreateImage("RayLight", visuals, raySprite, Color.white);
            SetCentered(ray.rectTransform, new Vector2(0f, 80f),
                new Vector2(1250f, 1250f));
            Image cat = CreateImage(
                "VictoryCat", visuals, catSprite, Color.white);
            SetCentered(cat.rectTransform, new Vector2(0f, 165f),
                new Vector2(500f, 500f));

            RectTransform content = CreateRect("Content", root);
            Stretch(content);
            CanvasGroup contentGroup =
                content.gameObject.AddComponent<CanvasGroup>();
            Text title = CreateText(
                "Title", content, font, 120, "WIN_TITLE",
                Color.white, FontStyle.Bold);
            SetCentered(title.rectTransform, new Vector2(0f, 555f),
                new Vector2(900f, 180f));

            RectTransform bodyRoot = CreateRect("Body", content);
            Text body = CreateText(
                "BeatPercent", bodyRoot, font, 60, string.Empty,
                Color.white, FontStyle.Bold);
            SetCentered(body.rectTransform, new Vector2(0f, -120f),
                new Vector2(860f, 150f));

            Image stats = CreateRoundedImage(
                "Statistics", content, rounded, 30f,
                new Color(Brown.r, Brown.g, Brown.b, 0.72f));
            SetCentered(stats.rectTransform, new Vector2(0f, -120f),
                new Vector2(804f, 204f));
            CanvasGroup statsGroup =
                stats.gameObject.AddComponent<CanvasGroup>();
            Text time = CreateText(
                "Time", stats.transform, font, 48, "Time  00:00",
                Cream, FontStyle.Bold);
            SetCentered(time.rectTransform, new Vector2(-260f, 0f),
                new Vector2(250f, 150f));
            Text score = CreateText(
                "Score", stats.transform, font, 48, "Score  0",
                Cream, FontStyle.Bold);
            SetCentered(score.rectTransform, Vector2.zero,
                new Vector2(260f, 150f));
            Text combo = CreateText(
                "Combo", stats.transform, font, 48, "Combo  0",
                Cream, FontStyle.Bold);
            SetCentered(combo.rectTransform, new Vector2(260f, 0f),
                new Vector2(250f, 150f));

            RectTransform actions = CreateRect("Actions", content);
            SetCentered(actions, new Vector2(0f, -390f),
                new Vector2(820f, 180f));
            Button next = CreateRoundedButton(
                "Next", actions, font, rounded, "Level 2", Orange,
                Color.white, new Vector2(750f, 160f));

            RectTransform passPanelRoot = CreateRect("PassPanel", root);
            Stretch(passPanelRoot);
            CanvasGroup passPanelGroup =
                passPanelRoot.gameObject.AddComponent<CanvasGroup>();
            Image passPopup = CreateImage(
                "Popup", passPanelRoot, passPanelSprite, Color.white);
            passPopup.type = Image.Type.Sliced;
            passPopup.preserveAspect = false;
            SetCentered(passPopup.rectTransform, new Vector2(0f, 372f),
                new Vector2(900f, 912f));

            Text passTitle = CreateText(
                "Title", passPopup.transform, font, 80, "Perfect!",
                Brown, FontStyle.Bold);
            SetCentered(passTitle.rectTransform, new Vector2(0f, 172f),
                new Vector2(550f, 80f));

            Image passStats = CreateRoundedImage(
                "Statistics", passPopup.transform, rounded, 20f,
                new Color(0.996f, 0.945f, 0.824f, 1f));
            SetCentered(passStats.rectTransform, new Vector2(0f, -150f),
                new Vector2(800f, 512f));
            CreatePassStatRow(
                passStats.transform, font, 0, "Size", "4\u00D74",
                out Text passSizeKey, out Text passSize);
            CreatePassStatRow(
                passStats.transform, font, 1, "Time", "00:00",
                out Text passTimeKey, out Text passTime);
            CreatePassStatRow(
                passStats.transform, font, 2, "Score", "0",
                out Text passScoreKey, out Text passScore);
            CreatePassStatRow(
                passStats.transform, font, 3, "Combo", "0",
                out Text passComboKey, out Text passCombo);

            Image passExtra = CreateRoundedImage(
                "ExtraStatistics", passPopup.transform, rounded, 20f,
                new Color(0.996f, 0.945f, 0.824f, 1f));
            SetCentered(passExtra.rectTransform, new Vector2(0f, -422f),
                new Vector2(798f, 128f));
            Text passCompletion = CreatePassInfoItem(
                "CompletionRate", passExtra.transform, font,
                completionIcon, -266f, "0%");
            Text passMistake = CreatePassInfoItem(
                "MistakeCount", passExtra.transform, font,
                mistakeIcon, 0f, "0");
            Text passTools = CreatePassInfoItem(
                "ToolsUsed", passExtra.transform, font,
                toolIcon, 266f, "0");

            Text passPraise = CreateText(
                "Praise", passPanelRoot, font, 76, string.Empty,
                Brown, FontStyle.Bold);
            passPraise.supportRichText = true;
            SetCentered(passPraise.rectTransform, new Vector2(0f, -274f),
                new Vector2(880f, 200f));

            RectTransform passActions = CreateRect("Actions", passPanelRoot);
            SetCentered(passActions, new Vector2(0f, -544f),
                new Vector2(1080f, 160f));
            Button passNext = CreateRoundedButton(
                "Next", passActions, font, rounded, "Level 2", Orange,
                Color.white, new Vector2(784f, 160f));
            passPanelRoot.gameObject.SetActive(false);

            RectTransform dailyVisuals = CreateRect("DailyVisuals", root);
            Stretch(dailyVisuals);
            CanvasGroup dailyGroup =
                dailyVisuals.gameObject.AddComponent<CanvasGroup>();
            Image dailyRay = CreateImage(
                "RayLight", dailyVisuals, raySprite, Color.white);
            SetCentered(
                dailyRay.rectTransform,
                new Vector2(0f, 80f),
                new Vector2(1250f, 1250f));
            Image dailyCat = CreateImage(
                "VictoryCatStaticAdapter",
                dailyVisuals,
                catSprite,
                Color.white);
            SetCentered(
                dailyCat.rectTransform,
                new Vector2(0f, 120f),
                new Vector2(560f, 560f));

            Text dailyTitle = CreateText(
                "Title", dailyVisuals, font, 120, "DAILY_WIN_TITLE",
                Color.white, FontStyle.Bold);
            SetCentered(
                dailyTitle.rectTransform,
                new Vector2(0f, 690f),
                new Vector2(900f, 180f));
            Outline dailyTitleOutline =
                dailyTitle.gameObject.AddComponent<Outline>();
            dailyTitleOutline.effectColor =
                new Color(0.8f, 0.349f, 0f, 1f);
            dailyTitleOutline.effectDistance = new Vector2(6f, -6f);

            Text dailyTime = CreateText(
                "Time", dailyVisuals, font, 70, string.Empty,
                Cream, FontStyle.Normal);
            dailyTime.supportRichText = true;
            SetCentered(
                dailyTime.rectTransform,
                new Vector2(0f, -360f),
                new Vector2(880f, 115f));
            Text dailyBeat = CreateText(
                "Beat", dailyVisuals, font, 70, string.Empty,
                new Color(1f, 0.89f, 0.459f, 1f), FontStyle.Normal);
            dailyBeat.supportRichText = true;
            SetCentered(
                dailyBeat.rectTransform,
                new Vector2(0f, -520f),
                new Vector2(900f, 200f));

            RectTransform dailyActions = CreateRect(
                "Actions",
                dailyVisuals);
            SetCentered(
                dailyActions,
                new Vector2(0f, -730f),
                new Vector2(820f, 180f));
            Button dailyContinue = CreateRoundedButton(
                "Continue", dailyActions, font, rounded, "WIN_CONTINUE",
                Orange, Color.white, new Vector2(784f, 160f));
            dailyVisuals.gameObject.SetActive(false);

            SerializedObject data =
                new(page.GetComponent<GameWinPagePresenter>());
            ConfigureFrame(data, canvas, pageGroup, true, 0.85f);
            SetReference(data, "content", content);
            SetReference(data, "contentGroup", contentGroup);
            SetReference(data, "defaultVisuals", visuals.gameObject);
            SetReference(data, "rayLight", ray.rectTransform);
            SetReference(data, "victoryCat", cat.rectTransform);
            SetReference(data, "titleText", title);
            SetReference(data, "bodyRoot", bodyRoot.gameObject);
            SetReference(data, "bodyText", body);
            SetReference(data, "statisticsRoot", stats.gameObject);
            SetReference(data, "statisticsGroup", statsGroup);
            SetReference(data, "timeText", time);
            SetReference(data, "scoreText", score);
            SetReference(data, "comboText", combo);
            SetReference(data, "nextButtonText",
                next.GetComponentInChildren<Text>(true));
            SetReference(data, "nextButton", next);
            SetReference(data, "passPanelRoot", passPanelRoot.gameObject);
            SetReference(data, "passPanelPopup", passPopup.rectTransform);
            SetReference(data, "passPanelGroup", passPanelGroup);
            SetReference(data, "passTitleText", passTitle);
            SetReference(data, "passPraiseText", passPraise);
            SetReference(data, "passPraiseRect", passPraise.rectTransform);
            SetReference(data, "passStatsRoot", passStats.rectTransform);
            SetReference(data, "passActionsRect", passActions);
            SetReference(data, "passSizeKeyText", passSizeKey);
            SetReference(data, "passTimeKeyText", passTimeKey);
            SetReference(data, "passScoreKeyText", passScoreKey);
            SetReference(data, "passComboKeyText", passComboKey);
            SetReference(data, "passSizeText", passSize);
            SetReference(data, "passTimeText", passTime);
            SetReference(data, "passScoreText", passScore);
            SetReference(data, "passComboText", passCombo);
            SetReference(data, "passExtraRoot", passExtra.gameObject);
            SetReference(data, "passCompletionText", passCompletion);
            SetReference(data, "passMistakeText", passMistake);
            SetReference(data, "passToolsText", passTools);
            SetReference(data, "passNextButtonText",
                passNext.GetComponentInChildren<Text>(true));
            SetReference(data, "passNextButton", passNext);
            SetReference(data, "dailyVisuals", dailyVisuals.gameObject);
            SetReference(data, "dailyContent", dailyVisuals);
            SetReference(data, "dailyContentGroup", dailyGroup);
            SetReference(data, "dailyRayLight", dailyRay.rectTransform);
            SetReference(data, "dailyVictoryCat", dailyCat.rectTransform);
            SetReference(data, "dailyTitleText", dailyTitle);
            SetReference(data, "dailyTimeText", dailyTime);
            SetReference(data, "dailyBeatText", dailyBeat);
            SetReference(
                data,
                "dailyContinueText",
                dailyContinue.GetComponentInChildren<Text>(true));
            SetReference(data, "dailyContinueButton", dailyContinue);
            SetReference(data, "localization", localization);
            data.ApplyModifiedPropertiesWithoutUndo();
            return page;
        }

        private static GameObject BuildFail(
            Font font,
            Shader rounded,
            LocalizationCatalog localization,
            Sprite catSprite,
            Sprite faceSprite)
        {
            GameObject page = CreatePage<GameFailPagePresenter>("FailPage");
            Canvas canvas = page.GetComponent<Canvas>();
            CanvasGroup pageGroup = page.GetComponent<CanvasGroup>();

            Image overlay = CreateImage(
                "Overlay", page.transform, null, Color.black);
            Stretch(overlay.rectTransform);
            overlay.raycastTarget = true;
            CanvasGroup overlayGroup =
                overlay.gameObject.AddComponent<CanvasGroup>();

            RectTransform root = CreateReferenceRoot(page.transform);
            RectTransform visuals = CreateRect("Visuals", root);
            Stretch(visuals);
            Image cat = CreateImage(
                "CryingCat", visuals, catSprite, Color.white);
            SetCentered(cat.rectTransform, new Vector2(0f, 260f),
                new Vector2(520f, 520f));

            RectTransform content = CreateRect("Content", root);
            Stretch(content);
            CanvasGroup contentGroup =
                content.gameObject.AddComponent<CanvasGroup>();
            Text title = CreateText(
                "Title", content, font, 120, "FAIL_TITLE_FISH",
                Color.white, FontStyle.Bold);
            SetCentered(title.rectTransform, new Vector2(0f, 675f),
                new Vector2(900f, 180f));

            RectTransform remainingRoot = CreateRect("Remaining", content);
            SetCentered(remainingRoot, new Vector2(0f, -15f),
                new Vector2(760f, 100f));
            Image face = CreateImage(
                "CatFace", remainingRoot, faceSprite, Color.white);
            SetCentered(face.rectTransform, new Vector2(-190f, 0f),
                new Vector2(86f, 86f));
            Text remaining = CreateText(
                "Count", remainingRoot, font, 56, "Remaining: 0",
                Color.white, FontStyle.Bold);
            SetCentered(remaining.rectTransform, new Vector2(90f, 0f),
                new Vector2(520f, 90f));

            RectTransform encourageRoot = CreateRect("Encourage", content);
            SetCentered(encourageRoot, new Vector2(0f, -155f),
                new Vector2(900f, 120f));
            Text encourage = CreateText(
                "Label", encourageRoot, font, 54, string.Empty,
                Color.white, FontStyle.Bold);
            Stretch(encourage.rectTransform);

            RectTransform actions = CreateRect("Actions", content);
            Stretch(actions);
            RectTransform reviveRoot = CreateRect("Revive", actions);
            SetCentered(reviveRoot, new Vector2(0f, -345f),
                new Vector2(820f, 190f));
            Button revive = CreateRoundedButton(
                "ReviveButton", reviveRoot, font, rounded, "Revive", Blue,
                Color.white, new Vector2(780f, 160f));
            Text reviveText = revive.GetComponentInChildren<Text>(true);
            Text reviveSubtitle = CreateText(
                "Subtitle", revive.transform, font, 36, string.Empty,
                Color.white, FontStyle.Normal);
            SetCentered(reviveSubtitle.rectTransform, new Vector2(0f, -49f),
                new Vector2(700f, 45f));

            RectTransform restartRoot = CreateRect("Restart", actions);
            SetCentered(restartRoot, new Vector2(0f, -555f),
                new Vector2(820f, 190f));
            Button restart = CreateRoundedButton(
                "RestartButton", restartRoot, font, rounded, "Restart",
                PaleBlue, Brown, new Vector2(780f, 160f));

            SerializedObject data =
                new(page.GetComponent<GameFailPagePresenter>());
            ConfigureFrame(data, canvas, pageGroup, false, 0f);
            SetReference(data, "overlayGroup", overlayGroup);
            SetReference(data, "content", content);
            SetReference(data, "contentGroup", contentGroup);
            SetReference(data, "titleText", title);
            SetReference(data, "remainingText", remaining);
            SetReference(data, "encourageRoot", encourageRoot.gameObject);
            SetReference(data, "encourageText", encourage);
            SetReference(data, "reviveRoot", reviveRoot.gameObject);
            SetReference(data, "reviveText", reviveText);
            SetReference(data, "reviveSubtitleText", reviveSubtitle);
            SetReference(data, "reviveButton", revive);
            SetReference(data, "restartText",
                restart.GetComponentInChildren<Text>(true));
            SetReference(data, "restartButton", restart);
            SetReference(data, "localization", localization);
            data.ApplyModifiedPropertiesWithoutUndo();
            return page;
        }

        private static GameObject CreatePage<T>(string name)
            where T : Component
        {
            var page = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(GraphicRaycaster),
                typeof(T));
            Stretch((RectTransform)page.transform);
            page.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            return page;
        }

        private static RectTransform CreateReferenceRoot(Transform parent)
        {
            RectTransform root = CreateRect("Root", parent);
            root.anchorMin = new Vector2(0.5f, 0f);
            root.anchorMax = new Vector2(0.5f, 1f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(1080f, 0f);
            return root;
        }

        private static void CreatePassStatRow(
            Transform parent,
            Font font,
            int index,
            string keyValue,
            string displayValue,
            out Text keyText,
            out Text valueText)
        {
            RectTransform row = CreateRect("Row" + (index + 1), parent);
            SetCentered(row, new Vector2(0f, 192f - index * 128f),
                new Vector2(800f, 128f));
            keyText = CreateText(
                "Key", row, font, 56, keyValue, Brown, FontStyle.Bold);
            keyText.alignment = TextAnchor.MiddleLeft;
            SetCentered(keyText.rectTransform, new Vector2(-190f, 0f),
                new Vector2(300f, 128f));
            valueText = CreateText(
                "Value", row, font, 56, displayValue, Brown,
                FontStyle.Bold);
            valueText.alignment = TextAnchor.MiddleRight;
            SetCentered(valueText.rectTransform, new Vector2(210f, 0f),
                new Vector2(300f, 128f));

            if (index >= 3) return;
            Image divider = CreateImage(
                "Divider", row, null,
                new Color(Brown.r, Brown.g, Brown.b, 0.05f));
            SetCentered(divider.rectTransform, new Vector2(0f, -62f),
                new Vector2(720f, 4f));
        }

        private static Text CreatePassInfoItem(
            string name,
            Transform parent,
            Font font,
            Sprite iconSprite,
            float x,
            string value)
        {
            RectTransform item = CreateRect(name, parent);
            SetCentered(item, new Vector2(x, 0f), new Vector2(266f, 128f));
            Image icon = CreateImage("Icon", item, iconSprite, Color.white);
            SetCentered(icon.rectTransform, new Vector2(-45f, 0f),
                new Vector2(80f, 80f));
            Text text = CreateText(
                "Value", item, font, 52, value, Brown, FontStyle.Bold);
            text.alignment = TextAnchor.MiddleLeft;
            SetCentered(text.rectTransform, new Vector2(65f, 0f),
                new Vector2(130f, 100f));
            return text;
        }

        private static Button CreateRoundedButton(
            string name,
            Transform parent,
            Font font,
            Shader shader,
            string label,
            Color background,
            Color foreground,
            Vector2 size)
        {
            Image image = CreateRoundedImage(
                name, parent, shader, size.y * 0.5f, background);
            SetCentered(image.rectTransform, Vector2.zero, size);
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Text text = CreateText(
                "Label", image.transform, font, 70, label,
                foreground, FontStyle.Bold);
            Stretch(text.rectTransform);
            return button;
        }

        private static Image CreateRoundedImage(
            string name,
            Transform parent,
            Shader shader,
            float radius,
            Color color)
        {
            Image image = CreateImage(name, parent, null, color);
            image.gameObject.AddComponent<RoundedImageView>()
                .Configure(image, shader, radius);
            return image;
        }

        private static Image CreateImage(
            string name,
            Transform parent,
            Sprite sprite,
            Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = sprite != null;
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            Font font,
            int size,
            string value,
            Color color,
            FontStyle style)
        {
            RectTransform rect = CreateRect(name, parent);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 20;
            text.resizeTextMaxSize = size;
            text.text = value;
            text.color = color;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void ConfigureFrame(
            SerializedObject data,
            Canvas canvas,
            CanvasGroup canvasGroup,
            bool showMask,
            float opacity)
        {
            data.FindProperty("uiLayer").intValue = (int)UiLayer.Default;
            data.FindProperty("isFullscreen").boolValue = false;
            data.FindProperty("showMask").boolValue = showMask;
            data.FindProperty("maskOpacity").floatValue = opacity;
            data.FindProperty("rootCanvas").objectReferenceValue = canvas;
            data.FindProperty("rootCanvasGroup").objectReferenceValue = canvasGroup;
        }

        private static void SetReference(
            SerializedObject data,
            string propertyName,
            Object value)
        {
            SerializedProperty property = data.FindProperty(propertyName);
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

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static Sprite LoadSprite(string path)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite) return sprite;
            }
            return null;
        }

        private static void Save(GameObject page, string path)
        {
            try
            {
                PrefabUtility.SaveAsPrefabAsset(page, path);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                Object.DestroyImmediate(page);
            }
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
