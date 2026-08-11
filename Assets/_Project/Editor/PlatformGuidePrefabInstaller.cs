using System.IO;
using Meowdoku.Core.Localization;
using Meowdoku.Core.UI;
using Meowdoku.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Editor
{
    [InitializeOnLoad]
    internal static partial class PlatformGuidePrefabInstaller
    {
        internal const string PrivacyPath =
            "Assets/_Project/Prefabs/UI/PrivacyDialog.prefab";
        internal const string PreAttPath =
            "Assets/_Project/Prefabs/UI/PreAttGuidePage.prefab";
        internal const string PreAttV2Path =
            "Assets/_Project/Prefabs/UI/PreAttGuidePageV2.prefab";
        internal const string PrePushPath =
            "Assets/_Project/Prefabs/UI/PrePushGuidePage.prefab";

        private const string FontPath =
            "Assets/_Project/Fonts/NotoSourceHan-subset.ttf";
        private const string RoundedShaderPath =
            "Assets/_Project/Shaders/UIRoundedRect.shader";
        private const string LogoPath =
            "Assets/_Project/Sprites/common/logo.png";
        private const string ClosePath =
            "Assets/_Project/Sprites/common/btn_close.png";

        private static readonly Color PanelColor =
            new(1f, 0.984f, 0.969f, 1f);
        private static readonly Color HeaderColor =
            new(0.976f, 0.925f, 0.882f, 1f);
        private static readonly Color TextColor =
            new(0.576f, 0.353f, 0.353f, 1f);
        private static readonly Color TitleColor =
            new(0.427f, 0.325f, 0.345f, 1f);
        private static readonly Color AccentColor =
            new(0.945f, 0.576f, 0.125f, 1f);

        static PlatformGuidePrefabInstaller()
        {
            EditorApplication.delayCall += QueueInstall;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                    EditorApplication.delayCall += QueueInstall;
            };
        }

        [MenuItem("Meowdoku/Port/Install Platform Guide Prefabs")]
        private static void InstallFromMenu()
        {
            InstallIfReady();
            Selection.activeObject =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrivacyPath);
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
            if (!HasPresenter<PrivacyDialogPresenter>(PrivacyPath))
            {
                Save(BuildPrivacy(font, rounded, localization), PrivacyPath);
                changed = true;
            }
            if (!HasPresenter<PreAttGuidePresenter>(PreAttPath))
            {
                Save(BuildPreAtt(font, rounded, localization, false), PreAttPath);
                changed = true;
            }
            if (!HasPresenter<PreAttGuidePresenter>(PreAttV2Path) ||
                HasLegacyDuplicateCloseField(PreAttV2Path) ||
                !HasAssignedReference<PreAttGuidePresenter>(
                    PreAttV2Path,
                    "guideCloseButton"))
            {
                Save(BuildPreAtt(font, rounded, localization, true), PreAttV2Path);
                changed = true;
            }
            if (!HasPresenter<PrePushGuidePresenter>(PrePushPath) ||
                HasLegacyDuplicateCloseField(PrePushPath) ||
                !HasAssignedReference<PrePushGuidePresenter>(
                    PrePushPath,
                    "guideCloseButton"))
            {
                Save(BuildPrePush(font, rounded, localization), PrePushPath);
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

        private static GameObject BuildPrivacy(
            Font font,
            Shader rounded,
            LocalizationCatalog localization)
        {
            GameObject page = CreatePage<PrivacyDialogPresenter>(
                "PrivacyDialog",
                out PrivacyDialogPresenter presenter,
                out Canvas canvas,
                out CanvasGroup pageGroup);
            RectTransform root = CreateRect("Root", page.transform);
            Stretch(root);
            RectTransform content = CreateRect("Content", root);
            Stretch(content);
            CanvasGroup contentGroup = content.gameObject.AddComponent<CanvasGroup>();
            GenericPopupAnimator animator = ConfigureGenericAnimator(
                page,
                content,
                contentGroup);

            Image panel = CreateRounded(
                "Panel",
                content,
                rounded,
                60f,
                PanelColor);
            SetCentered(panel.rectTransform, Vector2.zero, new Vector2(900f, 900f));
            Image header = CreateRounded(
                "TitleHeader",
                panel.transform,
                rounded,
                60f,
                HeaderColor);
            SetTop(header.rectTransform, 0f, new Vector2(900f, 130f));

            Text title = CreateText(
                "TitleLabel", panel.transform, font, 86, "Welcome",
                TitleColor, FontStyle.Bold);
            SetTop(title.rectTransform, -16f, new Vector2(838f, 100f));
            Text body = CreateText(
                "ContentLabel", panel.transform, font, 58,
                "Please read and accept our Terms of Service and Privacy Policy.",
                TextColor, FontStyle.Normal);
            SetCentered(body.rectTransform, new Vector2(0f, 75f),
                new Vector2(780f, 350f));
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Truncate;
            body.resizeTextForBestFit = true;
            body.resizeTextMinSize = 36;
            body.resizeTextMaxSize = 58;

            Button terms = CreateTextButton(
                "TermsButton", panel.transform, font,
                "Terms of Service", 38, TextColor, Color.clear,
                out _);
            SetCentered((RectTransform)terms.transform,
                new Vector2(-205f, -145f), new Vector2(350f, 70f));
            Button privacy = CreateTextButton(
                "PrivacyButton", panel.transform, font,
                "Privacy Policy", 38, TextColor, Color.clear,
                out _);
            SetCentered((RectTransform)privacy.transform,
                new Vector2(205f, -145f), new Vector2(350f, 70f));
            Button accept = CreateTextButton(
                "AcceptButton", panel.transform, font,
                "Accept", 70, Color.white, AccentColor,
                out Text acceptLabel,
                rounded,
                100f);
            SetCentered((RectTransform)accept.transform,
                new Vector2(0f, -310f), new Vector2(784f, 180f));

            ConfigureWindow(presenter, canvas, pageGroup, true, 0.8f);
            SerializedObject data = new(presenter);
            SetReference(data, "popupAnimator", animator);
            SetReference(data, "titleText", title);
            SetReference(data, "contentText", body);
            SetReference(data, "acceptText", acceptLabel);
            SetReference(data, "acceptButton", accept);
            SetReference(data, "termsButton", terms);
            SetReference(data, "privacyButton", privacy);
            SetReference(data, "localization", localization);
            data.ApplyModifiedPropertiesWithoutUndo();
            return page;
        }

        private static GameObject BuildPreAtt(
            Font font,
            Shader rounded,
            LocalizationCatalog localization,
            bool restyled)
        {
            string pageName = restyled
                ? "PreAttGuidePageV2"
                : "PreAttGuidePage";
            GameObject page = CreatePage<PreAttGuidePresenter>(
                pageName,
                out PreAttGuidePresenter presenter,
                out Canvas canvas,
                out CanvasGroup pageGroup);
            RectTransform root = CreateRect("Root", page.transform);
            Stretch(root);
            RectTransform content = CreateRect("Content", root);
            Stretch(content);
            CanvasGroup contentGroup = content.gameObject.AddComponent<CanvasGroup>();
            GenericPopupAnimator animator = ConfigureGenericAnimator(
                page,
                content,
                contentGroup);

            Transform textParent = content;
            Button close = null;
            if (restyled)
            {
                Image panel = CreateRounded(
                    "Panel", content, rounded, 60f, PanelColor);
                SetCentered(panel.rectTransform, Vector2.zero,
                    new Vector2(900f, 1170f));
                Image header = CreateRounded(
                    "TitleHeader", panel.transform, rounded, 60f, HeaderColor);
                SetTop(header.rectTransform, 0f, new Vector2(900f, 130f));
                textParent = panel.transform;
                close = CreateIconButton(
                    "CloseButton",
                    panel.transform,
                    LoadSprite(ClosePath));
                SetTopRight((RectTransform)close.transform,
                    new Vector2(-20f, -20f), new Vector2(100f, 100f));
            }
            else
            {
                Image background = content.gameObject.AddComponent<Image>();
                background.color = new Color(0.969f, 0.949f, 0.933f, 1f);
                background.raycastTarget = false;
                Image logo = CreateImage(
                    "Logo", content, LoadSprite(LogoPath));
                SetCentered(logo.rectTransform, new Vector2(0f, 640f),
                    new Vector2(369f, 234f));
            }

            Text title = CreateText(
                "TitleLabel", textParent, font, restyled ? 61 : 68,
                "Please Allow Tracking", TextColor, FontStyle.Bold);
            SetCentered(title.rectTransform,
                new Vector2(0f, restyled ? 500f : 320f),
                new Vector2(restyled ? 620f : 792f, 150f));
            Text description = CreateText(
                "DescriptionLabel", textParent, font,
                restyled ? 54 : 64,
                "Help support our ability to offer this app for free.",
                TextColor, FontStyle.Normal);
            SetCentered(description.rectTransform,
                new Vector2(0f, restyled ? 80f : -90f),
                new Vector2(restyled ? 760f : 900f,
                    restyled ? 580f : 850f));
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            description.verticalOverflow = VerticalWrapMode.Truncate;
            description.resizeTextForBestFit = true;
            description.resizeTextMinSize = 34;
            description.resizeTextMaxSize = restyled ? 54 : 64;

            Button proceed = CreateTextButton(
                "ContinueButton", textParent, font, "Continue", 70,
                Color.white, AccentColor, out Text proceedText,
                rounded, 100f);
            SetCentered((RectTransform)proceed.transform,
                new Vector2(0f, restyled ? -455f : -740f),
                new Vector2(784f, 180f));

            ConfigureWindow(presenter, canvas, pageGroup, true, 0.8f);
            SerializedObject data = new(presenter);
            SetReference(data, "popupAnimator", animator);
            SetReference(data, "titleText", title);
            SetReference(data, "descriptionText", description);
            SetReference(data, "continueText", proceedText);
            SetReference(data, "continueButton", proceed);
            SetReference(data, "guideCloseButton", close);
            SetReference(data, "localization", localization);
            data.FindProperty("restyled").boolValue = restyled;
            data.ApplyModifiedPropertiesWithoutUndo();
            return page;
        }

        private static GameObject CreatePage<T>(
            string name,
            out T presenter,
            out Canvas canvas,
            out CanvasGroup group) where T : UIFrameWindow
        {
            var page = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(GraphicRaycaster));
            SetUiLayer(page);
            Stretch((RectTransform)page.transform);
            canvas = page.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            group = page.GetComponent<CanvasGroup>();
            presenter = page.AddComponent<T>();
            return page;
        }

        private static GenericPopupAnimator ConfigureGenericAnimator(
            GameObject page,
            RectTransform content,
            CanvasGroup group)
        {
            GenericPopupAnimator animator =
                page.AddComponent<GenericPopupAnimator>();
            SerializedObject data = new(animator);
            SetReference(data, "content", content);
            SetReference(data, "contentGroup", group);
            data.ApplyModifiedPropertiesWithoutUndo();
            return animator;
        }

        private static void ConfigureWindow(
            UIFrameWindow presenter,
            Canvas canvas,
            CanvasGroup group,
            bool showMask,
            float opacity)
        {
            SerializedObject data = new(presenter);
            data.FindProperty("uiLayer").intValue = (int)UiLayer.Popup;
            data.FindProperty("isFullscreen").boolValue = false;
            data.FindProperty("showMask").boolValue = showMask;
            data.FindProperty("maskOpacity").floatValue = opacity;
            data.FindProperty("playOpenSound").boolValue = true;
            SetReference(data, "rootCanvas", canvas);
            SetReference(data, "rootCanvasGroup", group);
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Save(GameObject page, string path)
        {
            try { PrefabUtility.SaveAsPrefabAsset(page, path); }
            finally { Object.DestroyImmediate(page); }
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
            SerializedProperty property =
                new SerializedObject(component).FindProperty(propertyName);
            return property != null && property.objectReferenceValue != null;
        }

        private static bool HasLegacyDuplicateCloseField(string path)
        {
            if (!File.Exists(path)) return false;
            const string token = "\n  closeButton:";
            string yaml = File.ReadAllText(path);
            int first = yaml.IndexOf(token, System.StringComparison.Ordinal);
            return first >= 0 && yaml.IndexOf(
                token,
                first + token.Length,
                System.StringComparison.Ordinal) >= 0;
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
            RoundedImageView view =
                rect.gameObject.AddComponent<RoundedImageView>();
            SerializedObject data = new(view);
            SetReference(data, "target", image);
            SetReference(data, "roundedShader", shader);
            data.FindProperty("cornerRadius").floatValue = radius;
            data.ApplyModifiedPropertiesWithoutUndo();
            return image;
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

        private static Button CreateIconButton(
            string name,
            Transform parent,
            Sprite sprite)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static Button CreateTextButton(
            string name,
            Transform parent,
            Font font,
            string value,
            int size,
            Color textColor,
            Color background,
            out Text label,
            Shader rounded = null,
            float radius = 0f)
        {
            Image image = rounded != null
                ? CreateRounded(name, parent, rounded, radius, background)
                : CreateImage(name, parent, null);
            image.color = background;
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            label = CreateText(
                "Label", button.transform, font, size, value,
                textColor, FontStyle.Bold);
            Stretch(label.rectTransform);
            return button;
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
            string propertyName,
            Object value)
        {
            SerializedProperty property = data.FindProperty(propertyName);
            if (property != null) property.objectReferenceValue = value;
        }

        private static Sprite LoadSprite(string path)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object asset in assets)
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
            float y,
            Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = size;
        }

        private static void SetTopRight(
            RectTransform rect,
            Vector2 position,
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
