using System;
using System.IO;
using Meowdoku.Core.Localization;
using Meowdoku.Core.UI;
using Meowdoku.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Editor
{
    /// <summary>
    /// Materializes the source feedback/rate_us pages as ordinary prefabs.
    /// References are assigned through SerializedObject so scene YAML is never
    /// edited by hand and the UI registry can remain deterministic.
    /// </summary>
    [InitializeOnLoad]
    internal static class ProductServicePrefabInstaller
    {
        internal const string FeedbackPath =
            "Assets/_Project/Prefabs/UI/FeedbackPage.prefab";
        internal const string RateUsPath =
            "Assets/_Project/Prefabs/UI/RateUsPage.prefab";
        internal const string RateUsV2Path =
            "Assets/_Project/Prefabs/UI/RateUsPageV2.prefab";

        private const string FontPath =
            "Assets/_Project/Fonts/NotoSourceHan-subset.ttf";
        private const string RoundedShaderPath =
            "Assets/_Project/Shaders/UIRoundedRect.shader";
        private const string ClosePath =
            "Assets/_Project/Sprites/common/btn_close.png";
        private const string StarPath =
            "Assets/_Project/Sprites/rate_us/star.png";
        private const string DimStarPath =
            "Assets/_Project/Sprites/rate_us/star_dim.png";
        private const string CatWavePath =
            "Assets/_Project/Sprites/rate_us/cat_wave.png";

        private static readonly Color PanelColor =
            new(1f, 0.984f, 0.969f, 1f);
        private static readonly Color TextColor =
            new(0.576f, 0.353f, 0.353f, 1f);
        private static readonly Color AccentColor =
            new(0.945f, 0.576f, 0.125f, 1f);

        static ProductServicePrefabInstaller()
        {
            EditorApplication.delayCall += QueueInstall;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                    EditorApplication.delayCall += QueueInstall;
            };
        }

        [MenuItem("Meowdoku/Port/Install Feedback and Rate Us Prefabs")]
        private static void InstallFromMenu()
        {
            InstallIfReady();
            Selection.activeObject =
                AssetDatabase.LoadAssetAtPath<GameObject>(FeedbackPath);
        }

        internal static bool InstallIfReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += QueueInstall;
                return false;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return false;

            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            Shader rounded = AssetDatabase.LoadAssetAtPath<Shader>(
                RoundedShaderPath);
            LocalizationCatalog localization =
                LocalizationCatalogAssetInstaller.GetOrCreate();
            if (font == null || rounded == null || localization == null)
                return false;

            EnsureFolder("Assets/_Project/Prefabs", "UI");
            bool changed = false;
            if (!HasPresenter<FeedbackPagePresenter>(FeedbackPath) ||
                !HasSerializedToken(FeedbackPath, "frameCloseButton:") ||
                !HasSerializedToken(FeedbackPath, "feedbackCloseButton:") ||
                !HasAssignedReference<FeedbackPagePresenter>(
                    FeedbackPath, "feedbackCloseButton"))
            {
                Save(BuildFeedback(font, rounded, localization), FeedbackPath);
                changed = true;
            }
            if (!HasPresenter<RateUsPagePresenter>(RateUsPath) ||
                !HasSerializedToken(RateUsPath, "frameCloseButton:") ||
                !HasSerializedToken(RateUsPath, "rateCloseButton:") ||
                !HasAssignedReference<RateUsPagePresenter>(
                    RateUsPath, "rateCloseButton"))
            {
                Save(BuildRateUs(font, rounded, localization, false), RateUsPath);
                changed = true;
            }
            if (!HasPresenter<RateUsPagePresenter>(RateUsV2Path) ||
                HasSerializedToken(
                    RateUsV2Path,
                    "Meowdoku.Gameplay::Meowdoku.Gameplay.RateUsPagePresenterV2") ||
                !HasSerializedToken(RateUsV2Path, "frameCloseButton:") ||
                !HasSerializedToken(RateUsV2Path, "rateCloseButton:") ||
                !HasAssignedReference<RateUsPagePresenter>(
                    RateUsV2Path, "rateCloseButton"))
            {
                Save(BuildRateUs(font, rounded, localization, true), RateUsV2Path);
                changed = true;
            }
            if (changed) AssetDatabase.SaveAssets();
            UIRegistryAssetInstaller.InstallIfReady();
            return true;
        }

        private static void QueueInstall()
        {
            EditorApplication.update -= InstallOnEditorUpdate;
            EditorApplication.update += InstallOnEditorUpdate;
        }

        private static void InstallOnEditorUpdate()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;
            EditorApplication.update -= InstallOnEditorUpdate;
            InstallIfReady();
        }

        private static GameObject BuildFeedback(
            Font font,
            Shader rounded,
            LocalizationCatalog localization)
        {
            GameObject page = CreatePage<FeedbackPagePresenter>(
                "FeedbackPage", out FeedbackPagePresenter presenter,
                out Canvas canvas, out CanvasGroup pageGroup);
            RectTransform root = CreateRect("Root", page.transform);
            Stretch(root);
            RectTransform content = CreateRect("Content", root);
            Stretch(content);
            CanvasGroup contentGroup = content.gameObject.AddComponent<CanvasGroup>();
            GenericPopupAnimator animator = ConfigureAnimator(
                page, content, contentGroup);

            Image panel = CreateRounded("Panel", content, rounded, 60f, PanelColor);
            SetCentered(panel.rectTransform, Vector2.zero, new Vector2(900f, 1040f));
            Text title = CreateText("TitleLabel", panel.transform, font, 64,
                "Feedback", TextColor, FontStyle.Bold);
            SetTop(title.rectTransform, -20f, new Vector2(790f, 120f));
            Text description = CreateText("DescriptionLabel", panel.transform, font,
                42, "Tell us what happened", TextColor, FontStyle.Normal);
            SetCentered(description.rectTransform, new Vector2(0f, 320f),
                new Vector2(780f, 110f));
            description.resizeTextForBestFit = true;
            description.resizeTextMinSize = 28;

            InputField input = CreateInput("FeedbackInput", panel.transform, font);
            SetCentered(input.transform as RectTransform, new Vector2(0f, 60f),
                new Vector2(780f, 430f));
            Button submit = CreateTextButton("SubmitButton", panel.transform, font,
                "Submit", 50, Color.white, AccentColor, rounded, 38f,
                out Text submitText);
            SetCentered(submit.transform as RectTransform, new Vector2(0f, -300f),
                new Vector2(620f, 130f));
            Text thanks = CreateText("ThanksLabel", panel.transform, font, 34,
                "Thank you!", AccentColor, FontStyle.Bold);
            SetCentered(thanks.rectTransform, new Vector2(0f, -190f),
                new Vector2(700f, 70f));
            thanks.gameObject.SetActive(false);
            Button close = CreateIconButton("CloseButton", panel.transform,
                LoadSprite(ClosePath));
            SetTopRight(close.transform as RectTransform,
                new Vector2(-20f, -20f), new Vector2(100f, 100f));

            ConfigureWindow(presenter, canvas, pageGroup);
            SerializedObject data = new(presenter);
            SetReference(data, "popupAnimator", animator);
            SetReference(data, "titleText", title);
            SetReference(data, "descriptionText", description);
            SetReference(data, "submitText", submitText);
            SetReference(data, "thanksText", thanks);
            SetReference(data, "inputField", input);
            SetReference(data, "submitButton", submit);
            SetReference(data, "feedbackCloseButton", close);
            SetReference(data, "localization", localization);
            data.ApplyModifiedPropertiesWithoutUndo();
            return page;
        }

        private static GameObject BuildRateUs(
            Font font,
            Shader rounded,
            LocalizationCatalog localization,
            bool restyled)
        {
            string pageName = restyled ? "RateUsPageV2" : "RateUsPage";
            RateUsPagePresenter presenter;
            Canvas canvas;
            CanvasGroup pageGroup;
            GameObject page = CreatePage<RateUsPagePresenter>(pageName,
                out presenter, out canvas, out pageGroup);
            UIFrameWindow targetWindow = presenter;
            RectTransform root = CreateRect("Root", page.transform);
            Stretch(root);
            RectTransform content = CreateRect("Content", root);
            Stretch(content);
            CanvasGroup contentGroup = content.gameObject.AddComponent<CanvasGroup>();
            GenericPopupAnimator animator = ConfigureAnimator(
                page, content, contentGroup);

            Image panel = CreateRounded("Panel", content, rounded, 60f, PanelColor);
            SetCentered(panel.rectTransform, Vector2.zero,
                new Vector2(900f, restyled ? 1050f : 920f));
            Text title = CreateText("TitleLabel", panel.transform, font, 60,
                "Rate us", TextColor, FontStyle.Bold);
            SetTop(title.rectTransform, -20f, new Vector2(800f, 120f));
            Text question = CreateText("QuestionLabel", panel.transform, font, 45,
                "How do you like Meowdoku?", TextColor, FontStyle.Normal);
            SetCentered(question.rectTransform, new Vector2(0f, 275f),
                new Vector2(780f, 120f));

            RectTransform starRow = CreateRect("StarRow", panel.transform);
            SetCentered(starRow, new Vector2(0f, 65f), new Vector2(720f, 150f));
            Image[] starImages = new Image[5];
            Button[] starButtons = new Button[5];
            Sprite lit = LoadSprite(StarPath);
            Sprite dim = LoadSprite(DimStarPath);
            for (int index = 0; index < 5; index++)
            {
                RectTransform starRect = CreateRect("Star_" + (index + 1), starRow);
                SetCentered(starRect, new Vector2(-288f + index * 144f, 0f),
                    new Vector2(128f, 128f));
                Image image = starRect.gameObject.AddComponent<Image>();
                image.sprite = restyled ? dim : lit;
                image.preserveAspect = true;
                image.raycastTarget = true;
                Button button = starRect.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                RateUsStarPointerView pointer =
                    starRect.gameObject.AddComponent<RateUsStarPointerView>();
                SerializedObject pointerData = new(pointer);
                pointerData.FindProperty("starIndex").intValue = index + 1;
                pointerData.ApplyModifiedPropertiesWithoutUndo();
                starImages[index] = image;
                starButtons[index] = button;
            }
            if (restyled)
            {
                Image wave = CreateImage("CatWave", panel.transform,
                    LoadSprite(CatWavePath));
                SetCentered(wave.rectTransform, new Vector2(0f, -110f),
                    new Vector2(320f, 160f));
            }
            Button rate = CreateTextButton("RateButton", panel.transform, font,
                "Rate", 50, Color.white, AccentColor, rounded, 38f,
                out _);
            SetCentered(rate.transform as RectTransform,
                new Vector2(0f, restyled ? -350f : -270f),
                new Vector2(620f, 130f));
            Button close = CreateIconButton("CloseButton", panel.transform,
                LoadSprite(ClosePath));
            SetTopRight(close.transform as RectTransform,
                new Vector2(-20f, -20f), new Vector2(100f, 100f));

            ConfigureWindow(targetWindow, canvas, pageGroup);
            SerializedObject data = new(targetWindow);
            SetReference(data, "popupAnimator", animator);
            SetReference(data, "titleText", title);
            SetReference(data, "questionText", question);
            SetArray(data, "stars", starImages);
            SetArray(data, "starButtons", starButtons);
            SetReference(data, "litStar", lit);
            SetReference(data, "dimStar", dim);
            SetReference(data, "rateButton", rate);
            SetReference(data, "rateCloseButton", close);
            SetReference(data, "localization", localization);
            SerializedProperty restyledProperty = data.FindProperty("restyled");
            if (restyledProperty != null) restyledProperty.boolValue = restyled;
            data.ApplyModifiedPropertiesWithoutUndo();
            return page;
        }

        private static GameObject CreatePage<T>(
            string name,
            out T presenter,
            out Canvas canvas,
            out CanvasGroup group) where T : UIFrameWindow
        {
            var page = new GameObject(name, typeof(RectTransform), typeof(Canvas),
                typeof(CanvasGroup), typeof(GraphicRaycaster));
            SetUiLayer(page);
            Stretch((RectTransform)page.transform);
            canvas = page.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            group = page.GetComponent<CanvasGroup>();
            presenter = page.AddComponent<T>();
            return page;
        }

        private static GameObject CreatePage<T>(
            string name,
            out T presenter,
            out Canvas canvas,
            out CanvasGroup group,
            out UIFrameWindow window) where T : UIFrameWindow
        {
            GameObject page = CreatePage(name, out presenter, out canvas, out group);
            window = presenter;
            return page;
        }

        private static GenericPopupAnimator ConfigureAnimator(
            GameObject page,
            RectTransform content,
            CanvasGroup group)
        {
            GenericPopupAnimator animator = page.AddComponent<GenericPopupAnimator>();
            SerializedObject data = new(animator);
            SetReference(data, "content", content);
            SetReference(data, "contentGroup", group);
            data.ApplyModifiedPropertiesWithoutUndo();
            return animator;
        }

        private static void ConfigureWindow(
            UIFrameWindow window,
            Canvas canvas,
            CanvasGroup group)
        {
            SerializedObject data = new(window);
            data.FindProperty("uiLayer").intValue = (int)UiLayer.Popup;
            data.FindProperty("isFullscreen").boolValue = false;
            data.FindProperty("showMask").boolValue = true;
            data.FindProperty("maskOpacity").floatValue = 0.8f;
            data.FindProperty("playOpenSound").boolValue = true;
            SetReference(data, "rootCanvas", canvas);
            SetReference(data, "rootCanvasGroup", group);
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static InputField CreateInput(
            string name,
            Transform parent,
            Font font)
        {
            RectTransform rect = CreateRect(name, parent);
            Image background = rect.gameObject.AddComponent<Image>();
            background.color = new Color(0.98f, 0.95f, 0.92f, 1f);
            InputField input = rect.gameObject.AddComponent<InputField>();
            input.lineType = InputField.LineType.MultiLineNewline;
            input.characterLimit = 2000;
            RectTransform textRect = CreateRect("Text", rect);
            textRect.offsetMin = new Vector2(24f, 20f);
            textRect.offsetMax = new Vector2(-24f, -20f);
            Text text = textRect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = 36;
            text.color = TextColor;
            text.alignment = TextAnchor.UpperLeft;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform placeholderRect = CreateRect("Placeholder", rect);
            placeholderRect.offsetMin = textRect.offsetMin;
            placeholderRect.offsetMax = textRect.offsetMax;
            Text placeholder = placeholderRect.gameObject.AddComponent<Text>();
            placeholder.font = font;
            placeholder.fontSize = 36;
            placeholder.color = new Color(TextColor.r, TextColor.g, TextColor.b, 0.45f);
            placeholder.text = "Write your feedback";
            placeholder.alignment = TextAnchor.UpperLeft;
            input.textComponent = text;
            input.placeholder = placeholder;
            return input;
        }

        private static Image CreateRounded(
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
            RoundedImageView view = rect.gameObject.AddComponent<RoundedImageView>();
            SerializedObject data = new(view);
            SetReference(data, "target", image);
            SetReference(data, "roundedShader", shader);
            data.FindProperty("cornerRadius").floatValue = radius;
            data.ApplyModifiedPropertiesWithoutUndo();
            return image;
        }

        private static Button CreateTextButton(
            string name,
            Transform parent,
            Font font,
            string value,
            int size,
            Color textColor,
            Color background,
            Shader rounded,
            float radius,
            out Text label)
        {
            Image image = CreateRounded(name, parent, rounded, radius, background);
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            label = CreateText("Label", button.transform, font, size, value,
                textColor, FontStyle.Bold);
            Stretch(label.rectTransform);
            return button;
        }

        private static Button CreateIconButton(
            string name,
            Transform parent,
            Sprite sprite)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static Image CreateImage(
            string name,
            Transform parent,
            Sprite sprite)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
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
            text.text = value;
            text.raycastTarget = false;
            text.supportRichText = true;
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject target = new(name, typeof(RectTransform));
            SetUiLayer(target);
            RectTransform rect = (RectTransform)target.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void SetReference(SerializedObject data, string property,
            UnityEngine.Object value)
        {
            SerializedProperty target = data.FindProperty(property);
            if (target != null) target.objectReferenceValue = value;
        }

        private static void SetArray(
            SerializedObject data,
            string property,
            UnityEngine.Object[] values)
        {
            SerializedProperty target = data.FindProperty(property);
            if (target == null) return;
            target.arraySize = values?.Length ?? 0;
            for (int index = 0; index < target.arraySize; index++)
                target.GetArrayElementAtIndex(index).objectReferenceValue =
                    values[index];
        }

        private static void Save(GameObject page, string path)
        {
            try { PrefabUtility.SaveAsPrefabAsset(page, path); }
            finally { UnityEngine.Object.DestroyImmediate(page); }
        }

        private static bool HasPresenter<T>(string path) where T : Component
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefab != null && prefab.GetComponent<T>() != null;
        }

        private static bool HasAssignedReference<T>(
            string path,
            string propertyName) where T : Component
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            T component = prefab != null ? prefab.GetComponent<T>() : null;
            if (component == null) return false;
            SerializedProperty property = new SerializedObject(component)
                .FindProperty(propertyName);
            return property != null && property.objectReferenceValue != null;
        }

        private static bool HasSerializedToken(string path, string token)
        {
            return File.Exists(path) &&
                File.ReadAllText(path).IndexOf(
                    token, StringComparison.Ordinal) >= 0;
        }

        private static Sprite LoadSprite(string path)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (UnityEngine.Object asset in assets)
                if (asset is Sprite sprite) return sprite;
            return null;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetCentered(RectTransform rect, Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetTop(RectTransform rect, float y, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = size;
        }

        private static void SetTopRight(RectTransform rect, Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
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
