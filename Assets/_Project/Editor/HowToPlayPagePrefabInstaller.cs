using System.Collections.Generic;
using Meowdoku.Core.Localization;
using Meowdoku.Core.UI;
using Meowdoku.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Editor
{
    /// <summary>
    /// Builds both source How-to-play scenes through Unity serialization APIs.
    /// Demo cells are prefab children, so page loops allocate no GameObjects.
    /// </summary>
    [InitializeOnLoad]
    internal static class HowToPlayPagePrefabInstaller
    {
        private const string PrefabFolder = "Assets/_Project/Prefabs/UI";
        private const string FullPrefabPath =
            PrefabFolder + "/HowToPlayPage.prefab";
        private const string PagedPrefabPath =
            PrefabFolder + "/HowToPlayPagedPage.prefab";
        private const string CellPrefabPath =
            "Assets/_Project/Prefabs/Cell.prefab";
        private const string FontPath = "Assets/_Project/Fonts/Roboto.ttf";
        private const string EastAsianFontPath =
            "Assets/_Project/Fonts/NotoSourceHan-subset.ttf";
        private const string RoundedShaderPath =
            "Assets/_Project/Shaders/UIRoundedRect.shader";
        private const string DividerPath =
            "Assets/_Project/Sprites/game/rule_divider.png";
        private const string ClosePath =
            "Assets/_Project/Sprites/common/btn_close.png";
        private const string BackButtonPath =
            "Assets/_Project/Sprites/common/btn_orange_round.png";
        private const string BackIconPath =
            "Assets/_Project/Sprites/common/btn_back_white.svg";
        private const string MainButtonPath =
            "Assets/_Project/Sprites/common/btn_orange_capsule.png";

        private static readonly Color CardColor =
            new(1f, 1f, 0.992157f, 1f);
        private static readonly Color DialogColor =
            new(1f, 0.984f, 0.969f, 1f);
        private static readonly Color TitleBarColor =
            new(0.976f, 0.925f, 0.882f, 1f);
        private static readonly Color TitleTextColor =
            new(0.427f, 0.325f, 0.345f, 1f);
        private static readonly Color BodyTextColor =
            new(0.576f, 0.353f, 0.353f, 1f);

        static HowToPlayPagePrefabInstaller()
        {
            EditorApplication.delayCall += InstallIfMissing;
            EditorApplication.playModeStateChanged += HandlePlayModeChanged;
        }

        [MenuItem("Meowdoku/Port/Create How-to-play Page Prefabs")]
        private static void InstallFromMenu()
        {
            InstallIfMissing();
            Selection.activeObject =
                AssetDatabase.LoadAssetAtPath<GameObject>(PagedPrefabPath);
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

            bool needsFull =
                AssetDatabase.LoadAssetAtPath<GameObject>(FullPrefabPath) == null;
            bool needsPaged =
                AssetDatabase.LoadAssetAtPath<GameObject>(PagedPrefabPath) == null;
            if (!needsFull && !needsPaged)
            {
                UIRegistryAssetInstaller.InstallIfReady();
                return;
            }

            GameObject cellPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(CellPrefabPath);
            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            Font eastAsian =
                AssetDatabase.LoadAssetAtPath<Font>(EastAsianFontPath);
            Shader rounded =
                AssetDatabase.LoadAssetAtPath<Shader>(RoundedShaderPath);
            LocalizationCatalog localization =
                LocalizationCatalogAssetInstaller.GetOrCreate();
            if (cellPrefab == null || font == null || eastAsian == null ||
                rounded == null || localization == null)
                return;

            EnsureFolder("Assets/_Project/Prefabs", "UI");
            if (needsFull)
            {
                GameObject full = BuildFull(
                    cellPrefab,
                    font,
                    eastAsian,
                    rounded,
                    localization);
                SaveAndDestroy(full, FullPrefabPath);
            }
            if (needsPaged)
            {
                GameObject paged = BuildPaged(
                    cellPrefab,
                    font,
                    eastAsian,
                    rounded,
                    localization);
                SaveAndDestroy(paged, PagedPrefabPath);
            }
            AssetDatabase.SaveAssets();
            UIRegistryAssetInstaller.InstallIfReady();
        }

        private static GameObject BuildFull(
            GameObject cellPrefab,
            Font font,
            Font eastAsian,
            Shader rounded,
            LocalizationCatalog localization)
        {
            GameObject page = CreatePageRoot<HowToPlayPagePresenter>(
                "HowToPlayPage",
                out Canvas canvas,
                out CanvasGroup pageGroup);
            HowToPlayPagePresenter presenter =
                page.GetComponent<HowToPlayPagePresenter>();

            RectTransform root = CreateFixedRoot(page.transform);
            Image overlay = CreateImage("Overlay", root);
            Stretch(overlay.rectTransform);
            overlay.color = new Color(0f, 0f, 0f, 0.85f);
            overlay.raycastTarget = false;

            RectTransform content = CreateRect("Content", root);
            Stretch(content);
            CanvasGroup contentGroup =
                content.gameObject.AddComponent<CanvasGroup>();
            GenericPopupAnimator popup =
                page.AddComponent<GenericPopupAnimator>();
            ConfigurePopup(popup, content, contentGroup);

            var boards = new HowToPlayDemoBoardView[
                HowToPlayContract.FullDemos.Count];
            for (int index = 0; index < boards.Length; index++)
            {
                float top = HowToPlayContract.FullCardTops[index];
                Image card = CreateRoundedImage(
                    "Card" + (index + 1),
                    content,
                    rounded,
                    new Vector4(19f, 19f, 19f, 19f),
                    CardColor);
                SetTopLeft(
                    card.rectTransform,
                    new Vector2(HowToPlayContract.FullCardLeft, -top),
                    new Vector2(
                        HowToPlayContract.FullCardWidth,
                        HowToPlayContract.FullCardHeight));
                Shadow shadow = card.gameObject.AddComponent<Shadow>();
                shadow.effectColor =
                    new Color(0.898039f, 0.827451f, 0.764706f, 0.1f);
                shadow.effectDistance = new Vector2(0f, -10f);

                Text title = CreateText(
                    "Title" + (index + 1),
                    content,
                    font,
                    54,
                    HowToPlayContract.PagedDemos[index].CaptionKey,
                    CardColor,
                    FontStyle.Bold);
                SetTopLeft(
                    title.rectTransform,
                    new Vector2(
                        0f,
                        -(top +
                          HowToPlayContract.FullTitleCenterDeltaY - 40f)),
                    new Vector2(1080f, 80f));
                Localize(
                    title,
                    localization,
                    font,
                    eastAsian,
                    HowToPlayContract.PagedDemos[index].CaptionKey,
                    HowToPlayContract.PagedDemos[index].CaptionKey);

                HowToPlayFullDemo demo = HowToPlayContract.FullDemos[index];
                float slot = HowToPlayContract.FullRenderSlotPixels /
                             HowToPlayContract.FullBoardScale;
                float width =
                    (HowToPlayContract.FullColumns - 1) * slot +
                    HowToPlayContract.FullCellPixels;
                float boardRenderWidth =
                    (HowToPlayContract.FullColumns - 1) *
                    HowToPlayContract.FullRenderSlotPixels +
                    HowToPlayContract.FullCellPixels *
                    HowToPlayContract.FullBoardScale;
                float left = HowToPlayContract.FullCardLeft +
                             (HowToPlayContract.FullCardWidth - boardRenderWidth) /
                             2f;
                RectTransform boardRoot = CreateRect(
                    "Board" + (index + 1),
                    content);
                SetTopLeft(
                    boardRoot,
                    new Vector2(
                        left,
                        -(top + HowToPlayContract.FullBoardMarginY)),
                    new Vector2(
                        width,
                        (HowToPlayContract.FullRows - 1) * slot +
                        HowToPlayContract.FullCellPixels));
                boardRoot.localScale = Vector3.one *
                                       HowToPlayContract.FullBoardScale;
                boards[index] = CreateDemoBoard(
                    boardRoot,
                    cellPrefab,
                    demo.Colors.Count,
                    demo.Colors[0].Length,
                    slot,
                    HowToPlayContract.FullCellPixels,
                    0f,
                    7f);
            }

            Texture2D dividerTexture =
                AssetDatabase.LoadAssetAtPath<Texture2D>(DividerPath);
            for (int index = 0;
                 index < HowToPlayContract.FullDividerTops.Count;
                 index++)
            {
                RawImage divider = CreateRawImage(
                    "Divider" + (index + 1),
                    content);
                divider.texture = dividerTexture;
                SetTopLeft(
                    divider.rectTransform,
                    new Vector2(
                        59f,
                        -HowToPlayContract.FullDividerTops[index]),
                    new Vector2(969f, 16f));
            }

            Button tap = CreateTransparentButton("TapCatcher", root);
            Stretch((RectTransform)tap.transform);
            ConfigureWindow(presenter, canvas, pageGroup, null);
            SerializedObject data = new(presenter);
            SetReference(data, "popupAnimator", popup);
            SetReference(data, "tapCatcher", tap);
            SetArray(data, "boards", boards);
            data.ApplyModifiedPropertiesWithoutUndo();
            return page;
        }

        private static GameObject BuildPaged(
            GameObject cellPrefab,
            Font font,
            Font eastAsian,
            Shader rounded,
            LocalizationCatalog localization)
        {
            GameObject page = CreatePageRoot<HowToPlayPagedPagePresenter>(
                "HowToPlayPagedPage",
                out Canvas canvas,
                out CanvasGroup pageGroup);
            HowToPlayPagedPagePresenter presenter =
                page.GetComponent<HowToPlayPagedPagePresenter>();
            RectTransform root = CreateFixedRoot(page.transform);

            Image overlay = CreateImage("Overlay", root);
            Stretch(overlay.rectTransform);
            overlay.color = new Color(0f, 0f, 0f, 0.6f);
            overlay.raycastTarget = true;

            RectTransform content = CreateRect("Content", root);
            Stretch(content);
            CanvasGroup contentGroup =
                content.gameObject.AddComponent<CanvasGroup>();
            GenericPopupAnimator popup =
                page.AddComponent<GenericPopupAnimator>();
            ConfigurePopup(popup, content, contentGroup);

            Image dialog = CreateRoundedImage(
                "DialogBg",
                content,
                rounded,
                new Vector4(60f, 60f, 60f, 60f),
                DialogColor);
            SetTopLeft(dialog.rectTransform,
                new Vector2(90f, -475f), new Vector2(900f, 1450f));

            Image titleBar = CreateRoundedImage(
                "TitleBar",
                content,
                rounded,
                new Vector4(60f, 60f, 0f, 0f),
                TitleBarColor);
            SetTopLeft(titleBar.rectTransform,
                new Vector2(90f, -475f), new Vector2(900f, 133f));

            Text title = CreateText(
                "TitleLabel",
                content,
                font,
                86,
                "How to play",
                TitleTextColor,
                FontStyle.Bold);
            SetTopLeft(title.rectTransform,
                new Vector2(90f, -475f), new Vector2(900f, 133f));
            Localize(
                title,
                localization,
                font,
                eastAsian,
                "HOW_TO_PLAY_TITLE",
                "How to play");

            Button close = CreateTransparentButton("CloseBtn", content);
            SetTopLeft((RectTransform)close.transform,
                new Vector2(870f, -495f), new Vector2(100f, 102f));
            RawImage closeIcon = CreateRawImage("CloseIcon", close.transform);
            Stretch(closeIcon.rectTransform);
            closeIcon.texture =
                AssetDatabase.LoadAssetAtPath<Texture2D>(ClosePath);

            RectTransform clip = CreateRect("BoardClip", content);
            SetTopLeft(clip,
                new Vector2(90f, -608f), new Vector2(900f, 861f));
            clip.gameObject.AddComponent<RectMask2D>();

            var boards = new HowToPlayDemoBoardView[
                HowToPlayContract.PagedDemos.Count];
            var boardRects = new RectTransform[boards.Length];
            for (int index = 0; index < boards.Length; index++)
            {
                HowToPlayPagedDemo demo =
                    HowToPlayContract.PagedDemos[index];
                int rows = demo.Colors.Count;
                int columns = demo.Colors[0].Length;
                float scale = HowToPlayContract.PagedBoardScale(rows, columns);
                float native = Mathf.Max(rows, columns) *
                               HowToPlayContract.PagedSlotPixels;
                float contentWidth = columns *
                                     HowToPlayContract.PagedSlotPixels * scale;
                RectTransform boardRoot = CreateRect(
                    "Board" + (index + 1),
                    clip);
                SetTopLeft(
                    boardRoot,
                    new Vector2(
                        (HowToPlayContract.PagedClipWidth - contentWidth) / 2f,
                        -(HowToPlayContract.PagedBoardTop -
                          HowToPlayContract.PagedClipTop)),
                    new Vector2(native, native));
                boardRoot.localScale = Vector3.one * scale;

                float cardPad = 14f / scale;
                Image card = CreateRoundedImage(
                    "Card",
                    boardRoot,
                    rounded,
                    new Vector4(
                        19f / scale,
                        19f / scale,
                        19f / scale,
                        19f / scale),
                    CardColor);
                SetTopLeft(
                    card.rectTransform,
                    new Vector2(-cardPad, cardPad),
                    new Vector2(
                        native + 2f * cardPad,
                        native + 2f * cardPad));
                Shadow shadow = card.gameObject.AddComponent<Shadow>();
                shadow.effectColor =
                    new Color(0.898039f, 0.827451f, 0.764706f, 0.1f);
                shadow.effectDistance = new Vector2(0f, -10f / scale);

                boards[index] = CreateDemoBoard(
                    boardRoot,
                    cellPrefab,
                    rows,
                    columns,
                    HowToPlayContract.PagedSlotPixels,
                    HowToPlayContract.FullCellPixels,
                    HowToPlayContract.PagedCellGapPixels,
                    8f);
                boardRects[index] = boardRoot;
                boardRoot.gameObject.SetActive(index == 0);
            }

            Text caption = CreateText(
                "Caption",
                content,
                font,
                60,
                "1 Cat per color",
                BodyTextColor,
                FontStyle.Normal);
            caption.supportRichText = true;
            SetTopLeft(caption.rectTransform,
                new Vector2(150f, -1469f), new Vector2(780f, 180f));

            RectTransform buttonRow = CreateRect("ButtonRow", content);
            Stretch(buttonRow);
            Button back = CreateTransparentButton("BackBtn", buttonRow);
            SetTopLeft((RectTransform)back.transform,
                new Vector2(155f, -1675f), new Vector2(160f, 160f));
            RawImage backBackground = CreateRawImage("Bg", back.transform);
            SetCentered(backBackground.rectTransform,
                Vector2.zero, new Vector2(220f, 220f));
            backBackground.texture =
                AssetDatabase.LoadAssetAtPath<Texture2D>(BackButtonPath);
            Image backIcon = CreateImage("BackIcon", back.transform);
            SetCentered(backIcon.rectTransform,
                new Vector2(-2f, 0f), new Vector2(50f, 83f));
            backIcon.sprite =
                AssetDatabase.LoadAssetAtPath<Sprite>(BackIconPath);
            backIcon.preserveAspect = true;

            Button main = CreateTransparentButton("MainBtn", buttonRow);
            SetTopLeft((RectTransform)main.transform,
                new Vector2(260f, -1675f), new Vector2(560f, 160f));
            RawImage mainBackground = CreateRawImage("Bg", main.transform);
            SetCentered(mainBackground.rectTransform,
                new Vector2(0f, -10f), new Vector2(620f, 220f));
            mainBackground.texture =
                AssetDatabase.LoadAssetAtPath<Texture2D>(MainButtonPath);
            Text mainText = CreateText(
                "Label",
                main.transform,
                font,
                80,
                "Next",
                Color.white,
                FontStyle.Bold);
            Stretch(mainText.rectTransform);
            LocalizedText mainLocalized = Localize(
                mainText,
                localization,
                font,
                eastAsian,
                "HOW_TO_PLAY_NEXT",
                "Next");

            ConfigureWindow(presenter, canvas, pageGroup, close);
            SerializedObject data = new(presenter);
            SetReference(data, "popupAnimator", popup);
            SetArray(data, "boards", boards);
            SetArray(data, "boardRects", boardRects);
            SetReference(data, "caption", caption);
            SetReference(data, "backButton", back);
            SetReference(data, "mainButton", main);
            SetReference(data, "mainLabel", mainLocalized);
            SetReference(data, "localization", localization);
            data.ApplyModifiedPropertiesWithoutUndo();
            return page;
        }

        private static HowToPlayDemoBoardView CreateDemoBoard(
            RectTransform boardRoot,
            GameObject cellPrefab,
            int rows,
            int columns,
            float slot,
            float cellPixels,
            float inset,
            float radius)
        {
            HowToPlayDemoBoardView board =
                boardRoot.gameObject.AddComponent<HowToPlayDemoBoardView>();
            var cells = new List<CellView>(rows * columns);
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    GameObject instance = PrefabUtility.InstantiatePrefab(
                        cellPrefab,
                        boardRoot) as GameObject;
                    if (instance == null) continue;
                    instance.name = $"Cell_{row}_{column}";
                    RectTransform rect = instance.transform as RectTransform;
                    if (rect != null)
                    {
                        SetTopLeft(
                            rect,
                            new Vector2(
                                column * slot + inset,
                                -(row * slot + inset)),
                            new Vector2(cellPixels, cellPixels));
                        rect.localScale = Vector3.one;
                    }
                    CellView view = instance.GetComponent<CellView>();
                    if (view != null) cells.Add(view);
                }
            }

            SerializedObject data = new(board);
            data.FindProperty("rows").intValue = rows;
            data.FindProperty("columns").intValue = columns;
            data.FindProperty("cornerRadius").floatValue = radius;
            SetArray(data, "cells", cells.ToArray());
            data.ApplyModifiedPropertiesWithoutUndo();
            return board;
        }

        private static GameObject CreatePageRoot<T>(
            string name,
            out Canvas canvas,
            out CanvasGroup group) where T : UIFrameWindow
        {
            var page = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(GraphicRaycaster),
                typeof(T));
            Stretch((RectTransform)page.transform);
            canvas = page.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            group = page.GetComponent<CanvasGroup>();
            return page;
        }

        private static RectTransform CreateFixedRoot(Transform parent)
        {
            RectTransform root = CreateRect("Root", parent);
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(1080f, 2400f);
            return root;
        }

        private static void ConfigurePopup(
            GenericPopupAnimator popup,
            RectTransform content,
            CanvasGroup group)
        {
            SerializedObject data = new(popup);
            SetReference(data, "content", content);
            SetReference(data, "contentGroup", group);
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureWindow(
            UIFrameWindow presenter,
            Canvas canvas,
            CanvasGroup group,
            Button closeButton)
        {
            SerializedObject data = new(presenter);
            data.FindProperty("uiLayer").intValue = (int)UiLayer.Popup;
            data.FindProperty("isFullscreen").boolValue = false;
            data.FindProperty("showMask").boolValue = false;
            data.FindProperty("maskOpacity").floatValue = 0.8f;
            data.FindProperty("playOpenSound").boolValue = true;
            SetReference(data, "rootCanvas", canvas);
            SetReference(data, "rootCanvasGroup", group);
            SetReference(data, "closeButton", closeButton);
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static LocalizedText Localize(
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
            return localized;
        }

        private static Image CreateRoundedImage(
            string name,
            Transform parent,
            Shader shader,
            Vector4 radii,
            Color color)
        {
            Image image = CreateImage(name, parent);
            image.color = color;
            RoundedImageView rounded =
                image.gameObject.AddComponent<RoundedImageView>();
            SerializedObject data = new(rounded);
            SetReference(data, "target", image);
            SetReference(data, "roundedShader", shader);
            data.FindProperty("usePerCornerRadii").boolValue = true;
            data.FindProperty("cornerRadii").vector4Value = radii;
            data.ApplyModifiedPropertiesWithoutUndo();
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
            text.text = value;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateTransparentButton(
            string name,
            Transform parent)
        {
            Image image = CreateImage(name, parent);
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
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

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var child = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)child.transform;
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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

        private static void SetReference(
            SerializedObject target,
            string property,
            Object value)
        {
            SerializedProperty field = target.FindProperty(property);
            if (field != null) field.objectReferenceValue = value;
        }

        private static void SetArray<T>(
            SerializedObject target,
            string property,
            T[] values) where T : Object
        {
            SerializedProperty array = target.FindProperty(property);
            if (array == null) return;
            array.arraySize = values?.Length ?? 0;
            for (int index = 0; index < array.arraySize; index++)
                array.GetArrayElementAtIndex(index).objectReferenceValue =
                    values[index];
        }

        private static void SaveAndDestroy(GameObject root, string path)
        {
            try
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                Object.DestroyImmediate(root);
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
