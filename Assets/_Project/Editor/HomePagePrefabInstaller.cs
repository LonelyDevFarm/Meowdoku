using Meowdoku.Core.UI;
using Meowdoku.Core.Localization;
using Meowdoku.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Editor
{
    /// <summary>
    /// Builds the source-backed Home hierarchy without inventing Daily, Streak,
    /// Rank, Profile or Settings content that has not been ported yet.
    /// </summary>
    [InitializeOnLoad]
    internal static class HomePagePrefabInstaller
    {
        private const string PrefabFolder = "Assets/_Project/Prefabs/UI";
        private const string PrefabPath = PrefabFolder + "/HomePage.prefab";
        private const string MaterialFolder = "Assets/_Project/Materials";
        private const string FlowMaterialPath = MaterialFolder + "/HomeFlow.mat";
        private const string FontPath = "Assets/_Project/Fonts/Roboto.ttf";
        private const string EastAsianFontPath =
            "Assets/_Project/Fonts/NotoSourceHan-subset.ttf";
        private const string RoundedShaderPath =
            "Assets/_Project/Shaders/UIRoundedRect.shader";
        private const string FlowShaderPath =
            "Assets/_Project/Shaders/UIHomeFlow.shader";
        private const string LogoPath =
            "Assets/_Project/Sprites/common/logo.png";
        private const string FlowPath =
            "Assets/_Project/Sprites/Effects/ui/et_main_interface_flow.png";
        private const string FlowMaskPath =
            "Assets/_Project/Sprites/Effects/ui/et_main_interface_flow_mask.png";
        private const string SettingsIconPath =
            "Assets/_Project/Sprites/common/icon_settings.png";
        private const string BackIconPath =
            "Assets/_Project/Sprites/common/icon_back.png";
        private const string DifficultyBannerPath =
            "Assets/_Project/Sprites/home/difficulty_banner.png";

        private static readonly Color SourceBackground =
            new(0.969f, 0.949f, 0.933f, 1f);
        private static readonly Color SourceAccent =
            new(0.945f, 0.576f, 0.125f, 1f);
        private static readonly Color SourceText =
            new(0.576f, 0.353f, 0.353f, 1f);

        static HomePagePrefabInstaller()
        {
            EditorApplication.delayCall += InstallIfMissing;
            EditorApplication.playModeStateChanged += HandlePlayModeChanged;
        }

        [MenuItem("Meowdoku/Port/Create Home Page Prefab")]
        private static void InstallFromMenu()
        {
            InstallIfMissing();
            Selection.activeObject =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        private static void InstallIfMissing()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += InstallIfMissing;
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            LocalizationCatalog localization =
                LocalizationCatalogAssetInstaller.GetOrCreate();
            if (localization == null) return;
            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            Font eastAsian =
                AssetDatabase.LoadAssetAtPath<Font>(EastAsianFontPath);
            if (font == null || eastAsian == null) return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            {
                UpgradeLocalization(localization, font, eastAsian);
                return;
            }

            Shader roundedShader =
                AssetDatabase.LoadAssetAtPath<Shader>(RoundedShaderPath);
            Shader flowShader =
                AssetDatabase.LoadAssetAtPath<Shader>(FlowShaderPath);
            Texture2D logo = AssetDatabase.LoadAssetAtPath<Texture2D>(LogoPath);
            Texture2D flow = AssetDatabase.LoadAssetAtPath<Texture2D>(FlowPath);
            Texture2D flowMask =
                AssetDatabase.LoadAssetAtPath<Texture2D>(FlowMaskPath);
            if (font == null || roundedShader == null || flowShader == null ||
                logo == null || flow == null || flowMask == null)
                return;

            EnsureFolder("Assets/_Project/Prefabs", "UI");
            EnsureFolder("Assets/_Project", "Materials");
            Material flowMaterial = GetOrCreateFlowMaterial(flowShader, flowMask);
            if (flowMaterial == null) return;

            GameObject page = Build(
                font,
                eastAsian,
                localization,
                roundedShader,
                logo,
                flow,
                flowMaterial);
            try
            {
                PrefabUtility.SaveAsPrefabAsset(page, PrefabPath);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                Object.DestroyImmediate(page);
            }
        }

        private static void HandlePlayModeChanged(
            PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += InstallIfMissing;
        }

        private static GameObject Build(
            Font font,
            Font eastAsian,
            LocalizationCatalog localization,
            Shader roundedShader,
            Texture2D logoTexture,
            Texture2D flowTexture,
            Material flowMaterial)
        {
            var page = new GameObject(
                "HomePage",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(GraphicRaycaster),
                typeof(HomePagePresenter));
            SetUiLayer(page);
            Stretch((RectTransform)page.transform);

            Canvas canvas = page.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            CanvasGroup pageGroup = page.GetComponent<CanvasGroup>();

            Image background = CreateImage("Background", page.transform);
            Stretch(background.rectTransform);
            background.rectTransform.offsetMin = new Vector2(-7f, -7f);
            background.rectTransform.offsetMax = new Vector2(7f, 7f);
            background.color = SourceBackground;
            CanvasGroup backgroundGroup =
                background.gameObject.AddComponent<CanvasGroup>();

            RawImage gridFlow = CreateRawImage(
                "GridFlowLoop", background.transform);
            SetCenteredRect(gridFlow.rectTransform, new Vector2(-1f, 3f),
                new Vector2(1080f, 2400f));
            gridFlow.rectTransform.localScale = Vector3.one * 1.02f;
            gridFlow.texture = flowTexture;
            gridFlow.material = flowMaterial;
            gridFlow.color = new Color(1f, 1f, 1f, 0.2f);
            CanvasGroup gridGroup =
                gridFlow.gameObject.AddComponent<CanvasGroup>();

            RectTransform root = CreateRect("Root", page.transform);
            root.anchorMin = new Vector2(0.5f, 0f);
            root.anchorMax = new Vector2(0.5f, 1f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(1080f, 0f);

            RectTransform loge = CreateRect("Loge", root);
            SetCenteredRect(loge, new Vector2(-46f, -120f),
                new Vector2(520f, 330f));
            RawImage logo = CreateRawImage("LogoStaticAdapter", loge);
            SetCenteredRect(logo.rectTransform, Vector2.zero,
                new Vector2(520f, 321f));
            logo.texture = logoTexture;
            logo.color = Color.white;
            CanvasGroup logoGroup = logo.gameObject.AddComponent<CanvasGroup>();

            Button start = CreateRoundedButton(
                "StartBtn", root, roundedShader, 80f, SourceAccent);
            SetCenteredRect((RectTransform)start.transform,
                new Vector2(-19f, -555f),
                new Vector2(HomePageContract.StartButtonWidth,
                    HomePageContract.StartButtonHeight));
            Shadow startShadow = start.gameObject.AddComponent<Shadow>();
            startShadow.effectColor =
                new Color(SourceAccent.r, SourceAccent.g, SourceAccent.b, 0.7f);
            startShadow.effectDistance = new Vector2(0f, -14f);
            CanvasGroup startGroup =
                start.gameObject.AddComponent<CanvasGroup>();

            Text levelText = CreateText(
                "Text", start.transform, font, 80, "Level 1", Color.white);
            Stretch(levelText.rectTransform, new Vector2(45f, 18f));
            LocalizedText levelLocalized = ConfigureLocalizedText(
                levelText,
                localization,
                font,
                eastAsian,
                "GAME_LEVEL_TITLE",
                "Level %d");

            RectTransform hardBadge = CreateRect("HardBadge", start.transform);
            hardBadge.anchorMin = hardBadge.anchorMax = new Vector2(0.5f, 1f);
            hardBadge.pivot = new Vector2(0.5f, 0.5f);
            hardBadge.anchoredPosition = new Vector2(0f, 45f);
            hardBadge.sizeDelta = new Vector2(280f, 70f);
            Image hardBanner = CreateImage("Banner", hardBadge);
            Stretch(hardBanner.rectTransform);
            hardBanner.sprite = LoadSprite(
                DifficultyBannerPath, "difficulty_banner_0");
            hardBanner.preserveAspect = true;
            Text hardLabel = CreateText(
                "Label", hardBadge, font, 48, "Hard", Color.white);
            Stretch(hardLabel.rectTransform, new Vector2(10f, 4f));

            RectTransform daily = CreateRect("DailyStreakLayout", root);
            Stretch(daily);
            RectTransform dcSlot = CreateRect("DcEntrySlot", daily);
            SetCenteredRect(dcSlot, new Vector2(-230f, 495f),
                new Vector2(460f, 590f));
            RectTransform streakSlot = CreateRect("StreakEntrySlot", daily);
            SetCenteredRect(streakSlot, new Vector2(230f, 495f),
                new Vector2(460f, 590f));
            RectTransform streakSmallSlot =
                CreateRect("StreakSmallEntrySlot", daily);
            SetCenteredRect(streakSmallSlot, new Vector2(230f, 642.5f),
                new Vector2(420f, 255f));
            RectTransform rankSlot = CreateRect("RankEntrySlot", daily);
            SetCenteredRect(rankSlot, new Vector2(230f, 345.5f),
                new Vector2(420f, 255f));

            RectTransform vbox = CreateRect("VBoxContainer", root);
            Stretch(vbox);
            RectTransform headerAdapt =
                CreateRect("HeaderAdaptHolder", vbox);
            RectTransform header = CreateRect("Header", vbox);
            header.anchorMin = header.anchorMax = new Vector2(0.5f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.anchoredPosition = Vector2.zero;
            header.sizeDelta = new Vector2(1080f, 120f);

            Button back = CreateRoundIconButton(
                "BackBtn", header, roundedShader,
                BackIconPath, "icon_back_0");
            SetAnchoredRect(back.transform as RectTransform,
                new Vector2(0f, 0.5f), new Vector2(25f, -10f),
                new Vector2(120f, 120f), new Vector2(0f, 0.5f));

            RectTransform profile = CreateRect("ProfileEntry", header);
            SetAnchoredRect(profile,
                new Vector2(0f, 0.5f), new Vector2(27f, -31.5f),
                new Vector2(185f, 185f), new Vector2(0f, 0.5f));
            Image profileBackground = profile.gameObject.AddComponent<Image>();
            profileBackground.color = Color.white;
            profileBackground.raycastTarget = false;
            ConfigureRounded(profileBackground, roundedShader, 92.5f);
            RectTransform avatarSlot = CreateRect("AvatarSlot", profile);
            Stretch(avatarSlot);
            Button avatarButton = profile.gameObject.AddComponent<Button>();
            avatarButton.targetGraphic = profileBackground;

            Button settings = CreateRoundIconButton(
                "SettingsBtn", header, roundedShader,
                SettingsIconPath, "icon_settings_0");
            SetAnchoredRect(settings.transform as RectTransform,
                new Vector2(1f, 0.5f), new Vector2(-146f, -10f),
                new Vector2(120f, 120f), new Vector2(0f, 0.5f));
            CanvasGroup settingsGroup =
                settings.gameObject.AddComponent<CanvasGroup>();

            HomePagePresenter presenter = page.GetComponent<HomePagePresenter>();
            ConfigureWindow(presenter, canvas, pageGroup);
            SerializedObject data = new(presenter);
            SetReference(data, "layoutSpace", root);
            SetReference(data, "headerAdaptHolder", headerAdapt);
            SetReference(data, "header", header);
            SetReference(data, "logoVisual", logo.rectTransform);
            SetReference(data, "backgroundGroup", backgroundGroup);
            SetReference(data, "gridFlowGroup", gridGroup);
            SetReference(data, "logoGroup", logoGroup);
            SetReference(data, "startGroup", startGroup);
            SetReference(data, "settingsGroup", settingsGroup);
            SetReference(data, "startButton", start);
            SetReference(data, "levelText", levelText);
            SetReference(data, "levelLocalizedText", levelLocalized);
            SetReference(data, "hardBadge", hardBadge.gameObject);
            SetReference(data, "settingsButton", settings);
            SetReference(data, "profileEntry", profile.gameObject);
            SetReference(data, "profileButton", avatarButton);
            SetReference(data, "dailyStreakLayout", daily.gameObject);
            SetReference(data, "localization", localization);
            data.ApplyModifiedPropertiesWithoutUndo();

            back.gameObject.SetActive(false);
            profile.gameObject.SetActive(false);
            hardBadge.gameObject.SetActive(false);
            return page;
        }

        private static void UpgradeLocalization(
            LocalizationCatalog localization,
            Font font,
            Font eastAsian)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                HomePagePresenter presenter =
                    root.GetComponent<HomePagePresenter>();
                if (presenter == null) return;
                SerializedObject data = new(presenter);
                SerializedProperty property =
                    data.FindProperty("localization");
                bool changed = false;
                if (property != null &&
                    property.objectReferenceValue != localization)
                {
                    property.objectReferenceValue = localization;
                    changed = true;
                }

                SerializedProperty levelProperty =
                    data.FindProperty("levelText");
                Text level = levelProperty?.objectReferenceValue as Text;
                if (level != null)
                {
                    LocalizedText localized =
                        level.GetComponent<LocalizedText>();
                    if (localized == null)
                    {
                        localized = ConfigureLocalizedText(
                            level,
                            localization,
                            font,
                            eastAsian,
                            "GAME_LEVEL_TITLE",
                            "Level %d");
                        changed = true;
                    }
                    SerializedProperty localizedProperty =
                        data.FindProperty("levelLocalizedText");
                    if (localizedProperty != null &&
                        localizedProperty.objectReferenceValue != localized)
                    {
                        localizedProperty.objectReferenceValue = localized;
                        changed = true;
                    }
                }

                if (!changed) return;
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static LocalizedText ConfigureLocalizedText(
            Text target,
            LocalizationCatalog catalog,
            Font primary,
            Font eastAsian,
            string key,
            string fallback)
        {
            LocalizedText localized =
                target.GetComponent<LocalizedText>();
            if (localized == null)
                localized = target.gameObject.AddComponent<LocalizedText>();
            SerializedObject data = new(localized);
            SetReference(data, "catalog", catalog);
            SetReference(data, "target", target);
            SetReference(data, "primaryFont", primary);
            SetReference(data, "eastAsianFallbackFont", eastAsian);
            data.FindProperty("key").stringValue = key;
            data.FindProperty("fallbackText").stringValue = fallback;
            data.ApplyModifiedPropertiesWithoutUndo();
            localized.Refresh();
            return localized;
        }

        private static Material GetOrCreateFlowMaterial(
            Shader shader,
            Texture2D mask)
        {
            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(FlowMaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "HomeFlow" };
                material.SetTexture("_MaskTex", mask);
                material.SetVector("_ScrollSpeed",
                    new Vector4(0.015f, -0.015f, 0f, 0f));
                AssetDatabase.CreateAsset(material, FlowMaterialPath);
                return material;
            }

            material.shader = shader;
            material.SetTexture("_MaskTex", mask);
            material.SetVector("_ScrollSpeed",
                new Vector4(0.015f, -0.015f, 0f, 0f));
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Button CreateRoundedButton(
            string name,
            Transform parent,
            Shader shader,
            float radius,
            Color color)
        {
            Image image = CreateImage(name, parent);
            image.color = color;
            image.raycastTarget = true;
            ConfigureRounded(image, shader, radius);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static Button CreateRoundIconButton(
            string name,
            Transform parent,
            Shader shader,
            string iconPath,
            string spriteName)
        {
            Button button = CreateRoundedButton(
                name, parent, shader, 60f, Color.white);
            Shadow shadow = button.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.1f);
            shadow.effectDistance = new Vector2(0f, -4f);
            Image icon = CreateImage("Icon", button.transform);
            SetCenteredRect(icon.rectTransform, Vector2.zero,
                new Vector2(72f, 72f));
            icon.sprite = LoadSprite(iconPath, spriteName);
            icon.preserveAspect = true;
            return button;
        }

        private static Image CreateImage(string name, Transform parent)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static RawImage CreateRawImage(string name, Transform parent)
        {
            RectTransform rect = CreateRect(name, parent);
            RawImage image = rect.gameObject.AddComponent<RawImage>();
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
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var target = new GameObject(name, typeof(RectTransform));
            SetUiLayer(target);
            RectTransform rect = (RectTransform)target.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void ConfigureRounded(
            Image image,
            Shader shader,
            float radius)
        {
            RoundedImageView rounded =
                image.gameObject.AddComponent<RoundedImageView>();
            SerializedObject data = new(rounded);
            data.FindProperty("target").objectReferenceValue = image;
            data.FindProperty("roundedShader").objectReferenceValue = shader;
            data.FindProperty("cornerRadius").floatValue = radius;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureWindow(
            HomePagePresenter presenter,
            Canvas canvas,
            CanvasGroup group)
        {
            SerializedObject data = new(presenter);
            data.FindProperty("uiLayer").intValue = (int)UiLayer.Default;
            data.FindProperty("isFullscreen").boolValue = true;
            data.FindProperty("showMask").boolValue = false;
            data.FindProperty("playOpenSound").boolValue = false;
            data.FindProperty("rootCanvas").objectReferenceValue = canvas;
            data.FindProperty("rootCanvasGroup").objectReferenceValue = group;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetReference(
            SerializedObject data,
            string name,
            Object value)
        {
            SerializedProperty property = data.FindProperty(name);
            if (property != null) property.objectReferenceValue = value;
        }

        private static void SetCenteredRect(
            RectTransform rect,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetAnchoredRect(
            RectTransform rect,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            Vector2 pivot)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
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

        private static Sprite LoadSprite(string path, string name)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite && sprite.name == name)
                    return sprite;
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }

        private static void SetUiLayer(GameObject target)
        {
            int layer = LayerMask.NameToLayer("UI");
            target.layer = layer >= 0 ? layer : 0;
        }
    }
}
