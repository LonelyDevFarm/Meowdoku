using Meowdoku.Core.Localization;
using Meowdoku.Core.UI;
using Meowdoku.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Editor
{
    /// <summary>
    /// Builds language_page.tscn and ten reusable language_option.tscn rows
    /// through Unity APIs. No option is instantiated while scrolling.
    /// </summary>
    [InitializeOnLoad]
    internal static class LanguagePagePrefabInstaller
    {
        private const string PrefabFolder = "Assets/_Project/Prefabs/UI";
        private const string PrefabPath = PrefabFolder + "/LanguagePage.prefab";
        private const string FontPath = "Assets/_Project/Fonts/Roboto.ttf";
        private const string EastAsianFontPath =
            "Assets/_Project/Fonts/NotoSourceHan-subset.ttf";
        private const string RoundedShaderPath =
            "Assets/_Project/Shaders/UIRoundedRect.shader";
        private const string CloseIconPath =
            "Assets/_Project/Sprites/common/btn_close.png";
        private const string PrimaryButtonPath =
            "Assets/_Project/Sprites/common/btn_primary.png";
        private const string CheckPath =
            "Assets/_Project/Sprites/language/language_setting_v2/vector_3.png";

        private static readonly Color PanelColor =
            new(0.99607843f, 0.9843137f, 0.9647059f, 1f);
        private static readonly Color TitleColor =
            new(0.9764706f, 0.9254902f, 0.88235295f, 1f);
        private static readonly Color TitleTextColor =
            new(0.426923f, 0.3251181f, 0.34547916f, 1f);
        private static readonly Color OptionColor =
            new(0.9764706f, 0.9254902f, 0.88235295f, 1f);
        private static readonly Color OptionTextColor =
            new(0.5769231f, 0.3522559f, 0.3522559f, 1f);
        private static readonly Color SubtitleColor =
            new(0.8156863f, 0.69803923f, 0.67058825f, 1f);

        static LanguagePagePrefabInstaller()
        {
            EditorApplication.delayCall += InstallIfMissing;
        }

        [MenuItem("Meowdoku/Port/Create Language Page Prefab")]
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
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                return;

            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            Font eastAsian =
                AssetDatabase.LoadAssetAtPath<Font>(EastAsianFontPath);
            Shader rounded =
                AssetDatabase.LoadAssetAtPath<Shader>(RoundedShaderPath);
            Texture2D close =
                AssetDatabase.LoadAssetAtPath<Texture2D>(CloseIconPath);
            Texture2D primary =
                AssetDatabase.LoadAssetAtPath<Texture2D>(PrimaryButtonPath);
            Texture2D check =
                AssetDatabase.LoadAssetAtPath<Texture2D>(CheckPath);
            LocalizationCatalog localization =
                LocalizationCatalogAssetInstaller.GetOrCreate();
            if (font == null || eastAsian == null || rounded == null ||
                close == null || primary == null || check == null ||
                localization == null)
                return;

            EnsureFolder("Assets/_Project/Prefabs", "UI");
            GameObject page = Build(
                font,
                eastAsian,
                rounded,
                close,
                primary,
                check,
                localization);
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

        private static GameObject Build(
            Font font,
            Font eastAsian,
            Shader rounded,
            Texture2D closeTexture,
            Texture2D primaryTexture,
            Texture2D checkTexture,
            LocalizationCatalog localization)
        {
            var page = new GameObject(
                "LanguagePage",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(GraphicRaycaster),
                typeof(LanguagePagePresenter));
            SetUiLayer(page);
            Stretch((RectTransform)page.transform);
            Canvas canvas = page.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasGroup pageGroup = page.GetComponent<CanvasGroup>();

            RectTransform root = CreateRect("Root", page.transform);
            root.anchorMin = new Vector2(0.5f, 0f);
            root.anchorMax = new Vector2(0.5f, 1f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(1080f, 0f);

            RectTransform content = CreateRect("Content", root);
            SetAnchored(
                content,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(900f, 1299f),
                new Vector2(0.5f, 0.5f));
            CanvasGroup contentGroup =
                content.gameObject.AddComponent<CanvasGroup>();
            GenericPopupAnimator popup =
                page.AddComponent<GenericPopupAnimator>();
            SerializedObject popupData = new(popup);
            SetReference(popupData, "content", content);
            SetReference(popupData, "contentGroup", contentGroup);
            popupData.ApplyModifiedPropertiesWithoutUndo();

            Image frame = CreateRoundedImage(
                "PopupFrame", content, rounded, 60f, PanelColor);
            Stretch(frame.rectTransform);

            Image title = CreateRoundedImage(
                "TitleBg", content, rounded, 60f, TitleColor);
            SetTopLeft(title.rectTransform,
                Vector2.zero, new Vector2(900f, 130f));
            Text titleLabel = CreateText(
                "TitleLabel", content, font, 86, "Language",
                TitleTextColor, FontStyle.Bold);
            SetTopLeft(titleLabel.rectTransform,
                new Vector2(150f, 0f), new Vector2(600f, 125f));
            Localize(
                titleLabel, localization, font, eastAsian,
                "SETTING_LANGUAGE", "Language");

            Button close = CreateTransparentButton("CloseBtn", title.transform);
            SetAnchored(
                (RectTransform)close.transform,
                new Vector2(1f, 0.5f),
                new Vector2(-69f, 5f),
                new Vector2(100f, 100f),
                new Vector2(0.5f, 0.5f));
            RawImage closeIcon = CreateRawImage("CloseIcon", close.transform);
            Stretch(closeIcon.rectTransform);
            closeIcon.texture = closeTexture;

            RectTransform scrollRoot = CreateRect("ScrollContainer", content);
            SetTopLeft(scrollRoot,
                new Vector2(50f, -200f), new Vector2(830f, 789f));
            ScrollRect scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.inertia = true;
            scroll.decelerationRate = 0.135f;
            scroll.scrollSensitivity = 60f;

            RectTransform viewport = CreateRect("Viewport", scrollRoot);
            Stretch(viewport);
            viewport.offsetMax = new Vector2(-24f, 0f);
            Image viewportGraphic = viewport.gameObject.AddComponent<Image>();
            viewportGraphic.color = new Color(1f, 1f, 1f, 0f);
            viewportGraphic.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = viewport;

            RectTransform options = CreateRect("OptionsList", viewport);
            options.anchorMin = new Vector2(0f, 1f);
            options.anchorMax = new Vector2(1f, 1f);
            options.pivot = new Vector2(0.5f, 1f);
            options.anchoredPosition = Vector2.zero;
            options.sizeDelta = Vector2.zero;
            VerticalLayoutGroup optionLayout =
                options.gameObject.AddComponent<VerticalLayoutGroup>();
            optionLayout.spacing = 24f;
            optionLayout.childAlignment = TextAnchor.UpperCenter;
            optionLayout.childControlWidth = true;
            optionLayout.childControlHeight = false;
            optionLayout.childForceExpandWidth = true;
            optionLayout.childForceExpandHeight = false;
            ContentSizeFitter fitter =
                options.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = options;

            Scrollbar scrollbar = CreateScrollbar(scrollRoot, rounded);
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility =
                ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scroll.verticalScrollbarSpacing = 12f;

            var optionViews = new LanguageOptionView[
                LanguageSelectionContract.MaximumVisibleOptions];
            for (int index = 0; index < optionViews.Length; index++)
            {
                optionViews[index] = CreateOption(
                    "LanguageOption" + (index + 1),
                    options,
                    scroll,
                    font,
                    eastAsian,
                    rounded,
                    checkTexture);
            }

            RectTransform yesRoot = CreateRect("YesBtn", content);
            SetTopLeft(yesRoot,
                new Vector2(75f, -1048f), new Vector2(750f, 160f));
            RawImage visual = CreateRawImage("Visual", yesRoot);
            visual.rectTransform.anchorMin = Vector2.zero;
            visual.rectTransform.anchorMax = Vector2.one;
            visual.rectTransform.offsetMin = new Vector2(-30f, -40f);
            visual.rectTransform.offsetMax = new Vector2(30f, 20f);
            visual.texture = primaryTexture;
            Button confirm = CreateTransparentButton("Bg", yesRoot);
            Stretch((RectTransform)confirm.transform);
            Text confirmLabel = CreateText(
                "Label", confirm.transform, font, 80, "Confirm",
                Color.white, FontStyle.Bold);
            Stretch(confirmLabel.rectTransform);
            Localize(
                confirmLabel, localization, font, eastAsian,
                "LANGUAGE_CONFIRM", "Confirm");

            LanguagePagePresenter presenter =
                page.GetComponent<LanguagePagePresenter>();
            ConfigureWindow(presenter, canvas, pageGroup, close);
            SerializedObject data = new(presenter);
            SetReference(data, "popupAnimator", popup);
            SetReference(data, "scrollRect", scroll);
            SetReference(data, "confirmButton", confirm);
            SetReference(data, "localization", localization);
            SerializedProperty optionsProperty =
                data.FindProperty("optionViews");
            optionsProperty.arraySize = optionViews.Length;
            for (int index = 0; index < optionViews.Length; index++)
            {
                optionsProperty.GetArrayElementAtIndex(index)
                    .objectReferenceValue = optionViews[index];
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            return page;
        }

        private static LanguageOptionView CreateOption(
            string name,
            Transform parent,
            ScrollRect scroll,
            Font font,
            Font eastAsian,
            Shader rounded,
            Texture2D checkTexture)
        {
            Image background = CreateRoundedImage(
                name, parent, rounded, 30f, OptionColor);
            SetPreferred(background.gameObject, 0f, 150f);
            background.raycastTarget = true;
            Button button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;

            RectTransform vbox = CreateRect("VBox", background.transform);
            Stretch(vbox);
            vbox.offsetMin = new Vector2(50f, 4f);
            vbox.offsetMax = new Vector2(-130f, -4f);
            Text nativeLabel = CreateText(
                "NativeLabel", vbox, font, 60, string.Empty,
                OptionTextColor, FontStyle.Normal);
            nativeLabel.alignment = TextAnchor.MiddleLeft;
            SetTopLeft(nativeLabel.rectTransform,
                Vector2.zero, new Vector2(650f, 80f));
            Text subtitle = CreateText(
                "SubLabel", vbox, font, 36, string.Empty,
                SubtitleColor, FontStyle.Normal);
            subtitle.alignment = TextAnchor.MiddleLeft;
            SetTopLeft(subtitle.rectTransform,
                new Vector2(0f, -75f), new Vector2(650f, 62f));

            RectTransform checkRoot = CreateRect("CheckMark", background.transform);
            SetAnchored(
                checkRoot,
                new Vector2(1f, 0.5f),
                new Vector2(-72f, 0f),
                new Vector2(82f, 82f),
                new Vector2(0.5f, 0.5f));
            Image circle = CreateRoundedImage(
                "Ellipse1", checkRoot, rounded, 41f, Color.white);
            Stretch(circle.rectTransform);
            RawImage check = CreateRawImage("Visual", checkRoot);
            SetAnchored(
                check.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, -4.25f),
                new Vector2(55f, 39.5f),
                new Vector2(0.5f, 0.5f));
            check.texture = checkTexture;

            LanguageOptionView view =
                background.gameObject.AddComponent<LanguageOptionView>();
            SerializedObject data = new(view);
            SetReference(data, "button", button);
            SetReference(data, "scrollRect", scroll);
            SetReference(data, "background", background);
            SetReference(data, "nativeLabel", nativeLabel);
            SetReference(data, "subtitleLabel", subtitle);
            SetReference(data, "checkMark", checkRoot.gameObject);
            SetReference(data, "primaryFont", font);
            SetReference(data, "eastAsianFallbackFont", eastAsian);
            data.ApplyModifiedPropertiesWithoutUndo();
            checkRoot.gameObject.SetActive(false);
            return view;
        }

        private static Scrollbar CreateScrollbar(
            Transform parent,
            Shader rounded)
        {
            RectTransform root = CreateRect("Scrollbar Vertical", parent);
            root.anchorMin = new Vector2(1f, 0f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(1f, 0.5f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(12f, 0f);
            Image rootImage = root.gameObject.AddComponent<Image>();
            rootImage.color = new Color(1f, 1f, 1f, 0f);
            Scrollbar bar = root.gameObject.AddComponent<Scrollbar>();
            bar.direction = Scrollbar.Direction.BottomToTop;
            bar.targetGraphic = rootImage;

            RectTransform area = CreateRect("Sliding Area", root);
            Stretch(area);
            Image handle = CreateRoundedImage(
                "Handle", area, rounded, 6f,
                new Color(0.9372549f, 0.92156863f, 0.9098039f, 1f));
            Stretch(handle.rectTransform);
            bar.handleRect = handle.rectTransform;
            bar.targetGraphic = handle;
            return bar;
        }

        private static void ConfigureWindow(
            LanguagePagePresenter presenter,
            Canvas canvas,
            CanvasGroup group,
            Button closeButton)
        {
            SerializedObject data = new(presenter);
            data.FindProperty("uiLayer").intValue = (int)UiLayer.Popup;
            data.FindProperty("isFullscreen").boolValue = false;
            data.FindProperty("showMask").boolValue = true;
            data.FindProperty("maskOpacity").floatValue = 0.8f;
            data.FindProperty("playOpenSound").boolValue = true;
            SetReference(data, "rootCanvas", canvas);
            SetReference(data, "rootCanvasGroup", group);
            SetReference(data, "closeButton", closeButton);
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Localize(
            Text target,
            LocalizationCatalog catalog,
            Font primary,
            Font eastAsian,
            string key,
            string fallback)
        {
            LocalizedText localized =
                target.gameObject.AddComponent<LocalizedText>();
            SerializedObject data = new(localized);
            SetReference(data, "catalog", catalog);
            SetReference(data, "target", target);
            SetReference(data, "primaryFont", primary);
            SetReference(data, "eastAsianFallbackFont", eastAsian);
            data.FindProperty("key").stringValue = key;
            data.FindProperty("fallbackText").stringValue = fallback;
            data.ApplyModifiedPropertiesWithoutUndo();
            localized.Refresh();
        }

        private static Image CreateRoundedImage(
            string name,
            Transform parent,
            Shader shader,
            float radius,
            Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            RoundedImageView rounded =
                image.gameObject.AddComponent<RoundedImageView>();
            SerializedObject data = new(rounded);
            SetReference(data, "target", image);
            SetReference(data, "roundedShader", shader);
            data.FindProperty("cornerRadius").floatValue = radius;
            data.ApplyModifiedPropertiesWithoutUndo();
            return image;
        }

        private static Button CreateTransparentButton(
            string name,
            Transform parent)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
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
            Color color,
            FontStyle style)
        {
            RectTransform rect = CreateRect(name, parent);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
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

        private static void SetReference(
            SerializedObject data,
            string name,
            Object value)
        {
            SerializedProperty property = data.FindProperty(name);
            if (property != null) property.objectReferenceValue = value;
        }

        private static void SetPreferred(
            GameObject target,
            float width,
            float height)
        {
            LayoutElement layout = target.GetComponent<LayoutElement>();
            if (layout == null) layout = target.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;
        }

        private static void SetTopLeft(
            RectTransform rect,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetAnchored(
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
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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
