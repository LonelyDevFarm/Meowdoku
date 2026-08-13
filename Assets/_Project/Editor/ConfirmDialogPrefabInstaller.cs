using Meowdoku.Core.Localization;
using Meowdoku.Core.UI;
using Meowdoku.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Editor
{
    /// <summary>
    /// Materializes assets/prefab/confirm_dialog.tscn through Unity's Prefab
    /// API. The hierarchy and 900x900 source geometry remain deterministic.
    /// </summary>
    [InitializeOnLoad]
    internal static class ConfirmDialogPrefabInstaller
    {
        internal const string PrefabPath =
            "Assets/_Project/Prefabs/UI/ConfirmDialog.prefab";

        private const string FontPath =
            "Assets/_Project/Fonts/NotoSourceHan-subset.ttf";
        private const string RoundedShaderPath =
            "Assets/_Project/Shaders/UIRoundedRect.shader";
        private const string CloseSpritePath =
            "Assets/_Project/Sprites/common/btn_close.png";

        private static readonly Color PanelColor =
            new(1f, 0.984f, 0.969f, 1f);
        private static readonly Color TitlePanelColor =
            new(0.976f, 0.925f, 0.882f, 1f);
        private static readonly Color TitleColor =
            new(0.427f, 0.325f, 0.345f, 1f);
        private static readonly Color ContentColor =
            new(0.576f, 0.353f, 0.353f, 1f);
        private static readonly Color ActionColor =
            new(0.851f, 0.282f, 0.282f, 1f);

        static ConfirmDialogPrefabInstaller()
        {
            EditorApplication.delayCall += QueueInstall;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                    EditorApplication.delayCall += QueueInstall;
            };
        }

        [MenuItem("Meowdoku/Port/Install Confirm Dialog Prefab")]
        private static void InstallFromMenu()
        {
            InstallIfReady();
            Selection.activeObject =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        internal static bool InstallIfReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                QueueInstall();
                return false;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return false;

            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            Shader rounded = AssetDatabase.LoadAssetAtPath<Shader>(
                RoundedShaderPath);
            LocalizationCatalog localization =
                LocalizationCatalogAssetInstaller.GetOrCreate();
            if (font == null || rounded == null || localization == null)
            {
                QueueInstall();
                return false;
            }

            if (!HasValidPrefab())
            {
                GameObject page = Build(font, rounded, localization);
                try { PrefabUtility.SaveAsPrefabAsset(page, PrefabPath); }
                finally { Object.DestroyImmediate(page); }
                AssetDatabase.SaveAssets();
            }

            UIRegistryAssetInstaller.InstallIfReady();
            return true;
        }

        private static void QueueInstall()
        {
            EditorApplication.update -= InstallOnUpdate;
            EditorApplication.update += InstallOnUpdate;
        }

        private static void InstallOnUpdate()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;
            EditorApplication.update -= InstallOnUpdate;
            InstallIfReady();
        }

        private static bool HasValidPrefab()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            ConfirmDialogPresenter presenter =
                prefab != null
                    ? prefab.GetComponent<ConfirmDialogPresenter>()
                    : null;
            if (presenter == null) return false;
            var data = new SerializedObject(presenter);
            string[] properties =
            {
                "popupAnimator",
                "titleText",
                "contentText",
                "actionText",
                "actionButton",
                "confirmCloseButton",
                "localization"
            };
            foreach (string property in properties)
            {
                SerializedProperty value = data.FindProperty(property);
                if (value == null || value.objectReferenceValue == null)
                    return false;
            }
            return prefab.transform.Find(
                       "Root/Content/DialogRoot/CloseButton") != null &&
                   prefab.transform.Find(
                       "Root/Content/DialogRoot/ActionButton") != null;
        }

        private static GameObject Build(
            Font font,
            Shader rounded,
            LocalizationCatalog localization)
        {
            var page = new GameObject(
                "ConfirmDialog",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(GraphicRaycaster));
            SetUiLayer(page);
            Stretch((RectTransform)page.transform);
            Canvas canvas = page.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasGroup pageGroup = page.GetComponent<CanvasGroup>();
            ConfirmDialogPresenter presenter =
                page.AddComponent<ConfirmDialogPresenter>();

            RectTransform root = CreateRect("Root", page.transform);
            Stretch(root);

            Image overlay = CreateImage("Overlay", root);
            Stretch(overlay.rectTransform);
            overlay.color = new Color(0f, 0f, 0f, 0.75f);
            overlay.raycastTarget = true;
            CanvasGroup overlayGroup =
                overlay.gameObject.AddComponent<CanvasGroup>();

            RectTransform content = CreateRect("Content", root);
            Stretch(content);
            CanvasGroup contentGroup =
                content.gameObject.AddComponent<CanvasGroup>();
            RectTransform dialogRoot = CreateRect("DialogRoot", content);
            SetCentered(dialogRoot, Vector2.zero, new Vector2(900f, 900f));

            Image panel = CreateRounded(
                "DialogBg",
                dialogRoot,
                rounded,
                new Vector4(60f, 60f, 60f, 60f),
                PanelColor);
            Stretch(panel.rectTransform);

            Image titlePanel = CreateRounded(
                "TitleBgPanel",
                dialogRoot,
                rounded,
                new Vector4(60f, 60f, 0f, 0f),
                TitlePanelColor);
            SetTop(titlePanel.rectTransform, 0f, new Vector2(900f, 130f));

            Text title = CreateText(
                "TitleLabel",
                dialogRoot,
                font,
                86,
                "DIALOG_QUIT_TITLE",
                TitleColor,
                FontStyle.Bold);
            SetTop(title.rectTransform, -16f, new Vector2(900f, 100f));

            Button close = CreateIconButton(
                "CloseButton",
                dialogRoot,
                LoadSprite(CloseSpritePath));
            SetTopRight(
                close.transform as RectTransform,
                new Vector2(-20f, -20f),
                new Vector2(100f, 100f));

            Text message = CreateText(
                "ContentLabel",
                dialogRoot,
                font,
                80,
                "DIALOG_QUIT_MSG",
                ContentColor,
                FontStyle.Normal);
            SetCentered(
                message.rectTransform,
                new Vector2(0f, 54.5f),
                new Vector2(814f, 413f));
            message.horizontalOverflow = HorizontalWrapMode.Wrap;
            message.verticalOverflow = VerticalWrapMode.Truncate;
            message.resizeTextForBestFit = true;
            message.resizeTextMinSize = 48;

            Image shadow = CreateRounded(
                "ActionShadow",
                dialogRoot,
                rounded,
                new Vector4(52f, 52f, 52f, 52f),
                new Color(ActionColor.r, ActionColor.g, ActionColor.b, 0.6f));
            SetCentered(
                shadow.rectTransform,
                new Vector2(0f, -273f),
                new Vector2(784f, 258f));

            Button action = CreateTextButton(
                "ActionButton",
                dialogRoot,
                font,
                "DIALOG_QUIT_BTN",
                80,
                Color.white,
                ActionColor,
                rounded,
                52f,
                out Text actionText);
            SetCentered(
                action.transform as RectTransform,
                new Vector2(0f, -259f),
                new Vector2(784f, 258f));

            GenericPopupAnimator animator =
                page.AddComponent<GenericPopupAnimator>();
            var animatorData = new SerializedObject(animator);
            SetReference(animatorData, "content", content);
            SetReference(animatorData, "contentGroup", contentGroup);
            SetReference(animatorData, "overlayGroup", overlayGroup);
            animatorData.ApplyModifiedPropertiesWithoutUndo();

            var windowData = new SerializedObject(presenter);
            windowData.FindProperty("uiLayer").intValue =
                (int)UiLayer.Default;
            windowData.FindProperty("isFullscreen").boolValue = false;
            windowData.FindProperty("showMask").boolValue = false;
            windowData.FindProperty("maskOpacity").floatValue = 0f;
            windowData.FindProperty("playOpenSound").boolValue = true;
            SetReference(windowData, "rootCanvas", canvas);
            SetReference(windowData, "rootCanvasGroup", pageGroup);
            SetReference(windowData, "popupAnimator", animator);
            SetReference(windowData, "titleText", title);
            SetReference(windowData, "contentText", message);
            SetReference(windowData, "actionText", actionText);
            SetReference(windowData, "actionButton", action);
            SetReference(windowData, "confirmCloseButton", close);
            SetReference(windowData, "localization", localization);
            windowData.ApplyModifiedPropertiesWithoutUndo();
            return page;
        }

        private static Image CreateRounded(
            string name,
            Transform parent,
            Shader shader,
            Vector4 radii,
            Color color)
        {
            Image image = CreateImage(name, parent);
            image.color = color;
            image.raycastTarget = false;
            RoundedImageView view =
                image.gameObject.AddComponent<RoundedImageView>();
            var data = new SerializedObject(view);
            SetReference(data, "target", image);
            SetReference(data, "roundedShader", shader);
            data.FindProperty("usePerCornerRadii").boolValue = true;
            data.FindProperty("cornerRadii").vector4Value = radii;
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
            Image image = CreateRounded(
                name,
                parent,
                rounded,
                new Vector4(radius, radius, radius, radius),
                background);
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            label = CreateText(
                "Label",
                button.transform,
                font,
                size,
                value,
                textColor,
                FontStyle.Bold);
            Stretch(label.rectTransform);
            return button;
        }

        private static Button CreateIconButton(
            string name,
            Transform parent,
            Sprite sprite)
        {
            Image image = CreateImage(name, parent);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
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

        private static RectTransform CreateRect(
            string name,
            Transform parent)
        {
            var target = new GameObject(name, typeof(RectTransform));
            SetUiLayer(target);
            RectTransform rect = (RectTransform)target.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void SetReference(
            SerializedObject data,
            string property,
            Object value)
        {
            SerializedProperty target = data.FindProperty(property);
            if (target != null) target.objectReferenceValue = value;
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

        private static void SetUiLayer(GameObject target)
        {
            int layer = LayerMask.NameToLayer("UI");
            target.layer = layer >= 0 ? layer : 0;
        }
    }
}
