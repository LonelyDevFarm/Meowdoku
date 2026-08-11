using Meowdoku.Core.Localization;
using Meowdoku.Core.UI;
using Meowdoku.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Editor
{
    [InitializeOnLoad]
    internal static class AdRewardRestoredPagePrefabInstaller
    {
        internal const string PrefabPath =
            "Assets/_Project/Prefabs/UI/AdRewardRestoredPage.prefab";
        private const string RoundedShaderPath =
            "Assets/_Project/Shaders/UIRoundedRect.shader";

        static AdRewardRestoredPagePrefabInstaller()
        {
            EditorApplication.delayCall += InstallIfPossible;
        }

        [MenuItem("Meowdoku/Port/Rebuild Ad Reward Restored Page")]
        private static void Rebuild()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                AssetDatabase.DeleteAsset(PrefabPath);
            InstallIfPossible();
        }

        private static void InstallIfPossible()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += InstallIfPossible;
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                return;

            GameObject page = Build();
            PrefabUtility.SaveAsPrefabAsset(page, PrefabPath);
            Object.DestroyImmediate(page);
            AssetDatabase.SaveAssets();
            UIRegistryAssetInstaller.InstallIfReady();
        }

        private static GameObject Build()
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(
                "Assets/_Project/Fonts/Roboto.ttf");
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(
                RoundedShaderPath);
            Sprite closeSprite = LoadSprite(
                "Assets/_Project/Sprites/common/btn_close.png");
            Sprite locateSprite = LoadSprite(
                "Assets/_Project/Sprites/game/tool_cat_item.png");
            Sprite hintSprite = LoadSprite(
                "Assets/_Project/Sprites/game/icon_hint_lamp.png");
            LocalizationCatalog localization =
                LocalizationCatalogAssetInstaller.GetOrCreate();

            GameObject page = new(
                "AdRewardRestoredPage",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(GraphicRaycaster),
                typeof(AdRewardRestoredPagePresenter));
            RectTransform root = (RectTransform)page.transform;
            Stretch(root);
            Canvas canvas = page.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasGroup rootGroup = page.GetComponent<CanvasGroup>();

            RectTransform content = CreateRect("Content", root);
            Stretch(content);

            RectTransform dialog = CreateRect("DialogRoot", content);
            SetCentered(dialog, Vector2.zero, new Vector2(900f, 1180f));
            Image background = CreateRounded(
                "DialogBg",
                dialog,
                shader,
                new Vector4(60f, 60f, 60f, 60f),
                new Color(1f, 0.984f, 0.969f, 1f));
            Stretch(background.rectTransform);

            Image titleBackground = CreateRounded(
                "TitleBgPanel",
                dialog,
                shader,
                new Vector4(60f, 60f, 0f, 0f),
                new Color(0.976f, 0.925f, 0.882f, 1f));
            SetTop(titleBackground.rectTransform, 0f, 130f, 900f);

            Text title = CreateText(
                "TitleLabel",
                dialog,
                font,
                68,
                "Reward restored",
                new Color(0.427f, 0.325f, 0.345f, 1f));
            SetTop(title.rectTransform, 16f, 100f, 680f);

            Button close = CreateImageButton(
                "CloseButton",
                dialog,
                closeSprite);
            SetTop((RectTransform)close.transform, 20f, 100f, 100f, 770f);

            RectTransform toolGroup = CreateRect("ToolGroup", dialog);
            SetTop(toolGroup, 187f, 250f, 540f);
            AwardItemView locate = CreateAwardItem(
                "RevealBtn",
                toolGroup,
                font,
                shader,
                locateSprite,
                hintSprite,
                new Vector2(-157f, 0f));
            AwardItemView hint = CreateAwardItem(
                "HintBtn",
                toolGroup,
                font,
                shader,
                locateSprite,
                hintSprite,
                new Vector2(157f, 0f));

            Image contentBackground = CreateRounded(
                "ContentBg",
                dialog,
                shader,
                new Vector4(20f, 20f, 20f, 20f),
                new Color(0.976f, 0.925f, 0.882f, 1f));
            SetTop(contentBackground.rectTransform, 491f, 380f, 800f);
            Text body = CreateText(
                "ContentLabel",
                dialog,
                font,
                54,
                "Your missing ad reward has been restored.",
                new Color(0.576f, 0.353f, 0.353f, 1f));
            SetTop(body.rectTransform, 517f, 325f, 720f);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Truncate;

            Button collect = CreateRoundedButton(
                "CollectButton",
                dialog,
                shader,
                44f,
                new Color(0.945f, 0.435f, 0.31f, 1f));
            SetTop((RectTransform)collect.transform, 890f, 160f, 750f);
            Text collectText = CreateText(
                "Text",
                collect.transform,
                font,
                60,
                "Collect",
                Color.white);
            Stretch(collectText.rectTransform, new Vector2(35f, 16f));

            AdRewardRestoredPagePresenter presenter =
                page.GetComponent<AdRewardRestoredPagePresenter>();
            SerializedObject data = new(presenter);
            data.FindProperty("uiLayer").intValue = (int)UiLayer.Popup;
            data.FindProperty("isFullscreen").boolValue = false;
            data.FindProperty("showMask").boolValue = true;
            data.FindProperty("maskOpacity").floatValue = 0.8f;
            data.FindProperty("playOpenSound").boolValue = true;
            SetRef(data, "rootCanvas", canvas);
            SetRef(data, "rootCanvasGroup", rootGroup);
            SetRef(data, "content", content);
            SetRef(data, "titleText", title);
            SetRef(data, "bodyText", body);
            SetRef(data, "collectText", collectText);
            SetRef(data, "collectButton", collect);
            SetRef(data, "actionCloseButton", close);
            SetRef(data, "locateReward", locate);
            SetRef(data, "hintReward", hint);
            SetRef(data, "localization", localization);
            data.ApplyModifiedPropertiesWithoutUndo();
            return page;
        }

        private static AwardItemView CreateAwardItem(
            string name,
            Transform parent,
            Font font,
            Shader shader,
            Sprite locateSprite,
            Sprite hintSprite,
            Vector2 position)
        {
            Image background = CreateRounded(
                name,
                parent,
                shader,
                new Vector4(28f, 28f, 28f, 28f),
                Color.white);
            SetCentered(
                background.rectTransform,
                position,
                new Vector2(210f, 250f));
            Image icon = CreateImage("Icon", background.transform, null);
            SetCentered(
                icon.rectTransform,
                new Vector2(0f, 28f),
                new Vector2(116f, 116f));
            icon.preserveAspect = true;
            Text count = CreateText(
                "Count",
                background.transform,
                font,
                70,
                "x1",
                new Color(0.576f, 0.353f, 0.353f, 1f));
            SetCentered(
                count.rectTransform,
                new Vector2(0f, -82f),
                new Vector2(210f, 80f));
            AwardItemView view =
                background.gameObject.AddComponent<AwardItemView>();
            SerializedObject data = new(view);
            SetRef(data, "background", background);
            SetRef(data, "icon", icon);
            SetRef(data, "countText", count);
            SetRef(data, "locateIcon", locateSprite);
            SetRef(data, "hintIcon", hintSprite);
            data.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static Button CreateRoundedButton(
            string name,
            Transform parent,
            Shader shader,
            float radius,
            Color color)
        {
            Image image = CreateRounded(
                name,
                parent,
                shader,
                new Vector4(radius, radius, radius, radius),
                color);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static Button CreateImageButton(
            string name,
            Transform parent,
            Sprite sprite)
        {
            Image image = CreateImage(name, parent, sprite);
            image.preserveAspect = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static Image CreateRounded(
            string name,
            Transform parent,
            Shader shader,
            Vector4 radii,
            Color color)
        {
            Image image = CreateImage(name, parent, null);
            image.color = color;
            RoundedImageView rounded =
                image.gameObject.AddComponent<RoundedImageView>();
            SerializedObject data = new(rounded);
            SetRef(data, "target", image);
            SetRef(data, "roundedShader", shader);
            data.FindProperty("usePerCornerRadii").boolValue = true;
            data.FindProperty("cornerRadii").vector4Value = radii;
            data.ApplyModifiedPropertiesWithoutUndo();
            return image;
        }

        private static Image CreateImage(
            string name,
            Transform parent,
            Sprite sprite)
        {
            GameObject target = new(name, typeof(RectTransform), typeof(Image));
            RectTransform rect = (RectTransform)target.transform;
            rect.SetParent(parent, false);
            Image image = target.GetComponent<Image>();
            image.sprite = sprite;
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
            GameObject target = new(name, typeof(RectTransform), typeof(Text));
            RectTransform rect = (RectTransform)target.transform;
            rect.SetParent(parent, false);
            Text text = target.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.text = value;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 26;
            text.resizeTextMaxSize = size;
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject target = new(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)target.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(
            RectTransform rect,
            Vector2 inset = default)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = inset;
            rect.offsetMax = -inset;
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
            float top,
            float height,
            float width,
            float left = 0f)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(
                left == 0f ? 0f : left + width * 0.5f - 450f,
                -top);
        }

        private static void SetRef(
            SerializedObject data,
            string name,
            Object value)
        {
            SerializedProperty property = data.FindProperty(name);
            if (property != null) property.objectReferenceValue = value;
        }

        private static Sprite LoadSprite(string path) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
