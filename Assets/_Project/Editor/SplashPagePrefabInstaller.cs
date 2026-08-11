using Meowdoku.Core.Localization;
using Meowdoku.Core.UI;
using Meowdoku.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Editor
{
    [InitializeOnLoad]
    internal static class SplashPagePrefabInstaller
    {
        internal const string PrefabPath =
            "Assets/_Project/Prefabs/UI/SplashPage.prefab";
        private const string FontPath = "Assets/_Project/Fonts/Roboto.ttf";
        private const string LogoPath =
            "Assets/_Project/Sprites/common/logo.png";
        private const string CatFacePath =
            "Assets/_Project/Sprites/common/cat_face.png";
        private const string RoundedShaderPath =
            "Assets/_Project/Shaders/UIRoundedRect.shader";

        private static readonly Color Background =
            new(0.969f, 0.949f, 0.933f, 1f);
        private static readonly Color ProgressBackground =
            new(0.898f, 0.871f, 0.855f, 1f);
        private static readonly Color Accent =
            new(0.945f, 0.576f, 0.125f, 1f);
        private static readonly Color QuoteColor =
            new(0.576f, 0.353f, 0.353f, 1f);
        private static readonly Color AuthorColor =
            new(0.639f, 0.463f, 0.463f, 1f);

        static SplashPagePrefabInstaller()
        {
            EditorApplication.delayCall += InstallIfMissing;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                    EditorApplication.delayCall += InstallIfMissing;
            };
        }

        [MenuItem("Meowdoku/Port/Create Splash Page Prefab")]
        private static void InstallFromMenu()
        {
            InstallIfMissing();
            Selection.activeObject =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        internal static GameObject InstallIfReady()
        {
            InstallIfMissing();
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
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
            Sprite logo = LoadSprite(LogoPath);
            Sprite catFace = LoadSprite(CatFacePath);
            Shader rounded = AssetDatabase.LoadAssetAtPath<Shader>(
                RoundedShaderPath);
            LocalizationCatalog localization =
                LocalizationCatalogAssetInstaller.GetOrCreate();
            if (font == null || logo == null || catFace == null ||
                rounded == null || localization == null)
                return;

            EnsureFolder("Assets/_Project/Prefabs", "UI");
            GameObject page = Build(
                font,
                logo,
                catFace,
                rounded,
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
            Sprite logoSprite,
            Sprite catSprite,
            Shader rounded,
            LocalizationCatalog localization)
        {
            var page = new GameObject(
                "SplashPage",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(GraphicRaycaster),
                typeof(SplashPagePresenter));
            RectTransform pageRect = (RectTransform)page.transform;
            Stretch(pageRect);
            Canvas canvas = page.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasGroup group = page.GetComponent<CanvasGroup>();

            Image background = CreateImage("Background", pageRect, Background);
            Stretch(background.rectTransform);

            RectTransform root = CreateRect("Root", pageRect);
            root.anchorMin = new Vector2(0.5f, 0f);
            root.anchorMax = new Vector2(0.5f, 1f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(1080f, 0f);

            Text quote = CreateText(
                "QuoteLabel",
                root,
                font,
                80,
                "splash_slogan_0",
                QuoteColor,
                TextAnchor.UpperLeft);
            SetTopRect(quote.rectTransform, 0f, -362f, 900f, 540f);
            quote.horizontalOverflow = HorizontalWrapMode.Wrap;
            quote.verticalOverflow = VerticalWrapMode.Truncate;

            Text author = CreateText(
                "AuthorLabel",
                root,
                font,
                70,
                "- Meowdoku",
                AuthorColor,
                TextAnchor.MiddleRight);
            SetTopRect(author.rectTransform, 0f, -962f, 900f, 100f);

            Image progressBg = CreateImage(
                "ProgressBg",
                root,
                ProgressBackground);
            SetBottomRect(progressBg.rectTransform, 0f, 483f, 906f, 38f);
            progressBg.gameObject.AddComponent<RoundedImageView>()
                .Configure(progressBg, rounded, 22f);

            Image progress = CreateImage("ProgressFill", root, Accent);
            RectTransform progressRect = progress.rectTransform;
            progressRect.anchorMin = progressRect.anchorMax =
                new Vector2(0.5f, 0f);
            progressRect.pivot = new Vector2(0f, 0.5f);
            progressRect.anchoredPosition = new Vector2(-450f, 482f);
            progressRect.sizeDelta = new Vector2(0f, 32f);
            progress.gameObject.AddComponent<RoundedImageView>()
                .Configure(progress, rounded, 22f);

            Image logo = CreateImage("Logo", root, Color.white);
            logo.sprite = logoSprite;
            logo.preserveAspect = true;
            SetBottomRect(logo.rectTransform, 0f, 335f, 366f, 232f);

            Image cat = CreateImage("CatFace", root, Color.white);
            cat.sprite = catSprite;
            cat.preserveAspect = true;
            SetBottomRect(cat.rectTransform, -450f, 559f, 86f, 78f);

            SplashPagePresenter presenter =
                page.GetComponent<SplashPagePresenter>();
            SerializedObject data = new(presenter);
            data.FindProperty("uiLayer").intValue = (int)UiLayer.Loading;
            data.FindProperty("isFullscreen").boolValue = true;
            data.FindProperty("showMask").boolValue = false;
            data.FindProperty("rootCanvas").objectReferenceValue = canvas;
            data.FindProperty("rootCanvasGroup").objectReferenceValue = group;
            data.FindProperty("progressFill").objectReferenceValue = progressRect;
            data.FindProperty("catFace").objectReferenceValue = cat.rectTransform;
            data.FindProperty("quoteLabel").objectReferenceValue = quote;
            data.FindProperty("authorLabel").objectReferenceValue = author;
            data.FindProperty("localization").objectReferenceValue = localization;
            data.ApplyModifiedPropertiesWithoutUndo();
            return page;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static Image CreateImage(
            string name,
            Transform parent,
            Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
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
            TextAnchor alignment)
        {
            RectTransform rect = CreateRect(name, parent);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.text = value;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static void SetTopRect(
            RectTransform rect,
            float x,
            float y,
            float width,
            float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetBottomRect(
            RectTransform rect,
            float x,
            float y,
            float width,
            float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static Sprite LoadSprite(string path)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object asset in assets)
                if (asset is Sprite sprite)
                    return sprite;
            return null;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
