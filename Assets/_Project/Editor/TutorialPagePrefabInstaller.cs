using Meowdoku.Core.UI;
using Meowdoku.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Editor
{
    /// <summary>
    /// Creates the first source-backed TutorialPage prefab through Unity's
    /// serialization API. Existing prefabs are never overwritten automatically.
    /// </summary>
    [InitializeOnLoad]
    internal static class TutorialPagePrefabInstaller
    {
        private const string PrefabFolder = "Assets/_Project/Prefabs/UI";
        private const string PrefabPath = PrefabFolder + "/TutorialPage.prefab";
        private const string CellPrefabPath = "Assets/_Project/Prefabs/Cell.prefab";
        private const string FontPath = "Assets/_Project/Fonts/Roboto.ttf";
        private const string RoundedShaderPath =
            "Assets/_Project/Shaders/UIRoundedRect.shader";
        private const string CheckCirclePath =
            "Assets/_Project/Sprites/common/success_check_circle.png";
        private const string CheckMarkPath =
            "Assets/_Project/Sprites/common/success_check_mark.png";
        private const string HintLampPath =
            "Assets/_Project/Sprites/game/icon_hint_lamp.png";
        private const string HandPath =
            "Assets/_Project/Sprites/Effects/ui_guide/ui_guide_hand.png";
        private const string PrimaryButtonPath =
            "Assets/_Project/Sprites/common/btn_primary.png";
        private const string FireworkLinePath =
            "Assets/_Project/Sprites/Effects/line/et_line_001.png";
        private const string FireworkRibbonPath =
            "Assets/_Project/Sprites/Effects/obj/et_ribbon_001.png";
        private const string FireworkStarPath =
            "Assets/_Project/Sprites/Effects/star/et_star_2.png";
        private const string FireworkGlowPath =
            "Assets/_Project/Sprites/Effects/glow/et_glow_003.png";

        private static readonly Color SourceBackground =
            new Color(0.969f, 0.949f, 0.933f, 1f);
        private static readonly Color SourceText =
            new Color(0.576f, 0.353f, 0.353f, 1f);
        private static readonly Color SourceAccent =
            new Color(0.945f, 0.576f, 0.125f, 1f);

        static TutorialPagePrefabInstaller()
        {
            EditorApplication.delayCall += InstallIfMissing;
            EditorApplication.playModeStateChanged += HandlePlayModeChanged;
        }

        internal static void InstallIfReady()
        {
            InstallIfMissing();
        }

        [MenuItem("Meowdoku/Port/Create Tutorial Page Prefab")]
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

            GameObject existing =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null)
            {
                UpgradeExisting();
                return;
            }

            GameObject cellPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(CellPrefabPath);
            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            Shader roundedShader =
                AssetDatabase.LoadAssetAtPath<Shader>(RoundedShaderPath);
            if (cellPrefab == null || font == null || roundedShader == null)
                return;

            EnsureFolder("Assets/_Project/Prefabs", "UI");
            GameObject page = Build(cellPrefab, font, roundedShader);
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
            GameObject cellPrefab,
            Font font,
            Shader roundedShader)
        {
            var page = new GameObject(
                "TutorialPage",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(GraphicRaycaster),
                typeof(TutorialPagePresenter));
            SetUiLayer(page);
            Stretch((RectTransform)page.transform);

            Canvas canvas = page.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            CanvasGroup pageGroup = page.GetComponent<CanvasGroup>();

            Image background = CreateImage("Background", page.transform);
            Stretch(background.rectTransform);
            background.color = SourceBackground;

            RectTransform root = CreateRect("Root", page.transform);
            root.anchorMin = new Vector2(0.5f, 0f);
            root.anchorMax = new Vector2(0.5f, 1f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(1080f, 0f);

            RectTransform boardContainer = CreateRect("Board", root);
            SetCenteredRect(boardContainer, Vector2.zero, new Vector2(920f, 920f));
            CanvasGroup boardInputGroup =
                boardContainer.gameObject.AddComponent<CanvasGroup>();

            Image boardImage = CreateImage("BoardView", boardContainer);
            SetCenteredRect(boardImage.rectTransform, Vector2.zero,
                new Vector2(462f, 462f));
            boardImage.color = Color.white;
            boardImage.raycastTarget = true;
            GridLayoutGroup grid =
                boardImage.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            BoardView board = boardImage.gameObject.AddComponent<BoardView>();
            SerializedObject boardData = new SerializedObject(board);
            boardData.FindProperty("cellPrefab").objectReferenceValue = cellPrefab;
            boardData.FindProperty("cellsContainer").objectReferenceValue =
                boardImage.transform;
            boardData.FindProperty("roundedBackgroundShader").objectReferenceValue =
                roundedShader;
            boardData.ApplyModifiedPropertiesWithoutUndo();

            RectTransform highlightOverlay =
                CreateRect("HighlightOverlay", boardContainer);
            Stretch(highlightOverlay);
            Image selectFrame = CreateImage("SelectFrame", highlightOverlay);
            SetCenteredRect(selectFrame.rectTransform, Vector2.zero,
                new Vector2(116f, 116f));
            selectFrame.color = new Color(0.851f, 0.282f, 0.282f, 0.18f);
            ConfigureRounded(selectFrame, roundedShader, 20f);

            RectTransform maskLayer = CreateRect("Mask", root);
            Stretch(maskLayer);
            CanvasGroup maskGroup = maskLayer.gameObject.AddComponent<CanvasGroup>();
            Image maskBackground = CreateImage("Background", maskLayer);
            Stretch(maskBackground.rectTransform);
            maskBackground.color = new Color(0f, 0f, 0f, 0.75f);
            RectTransform maskCells = CreateRect("Cells", maskLayer);
            Stretch(maskCells);

            RectTransform guidance = CreateRect("Guidance", root);
            Stretch(guidance);
            Image messagePanel = CreatePanel(
                "Message", guidance, roundedShader, 32f);
            SetCenteredRect(messagePanel.rectTransform, new Vector2(0f, 538f),
                new Vector2(930f, 190f));
            messagePanel.rectTransform.pivot = new Vector2(0.5f, 0f);
            AddPanelShadow(messagePanel.gameObject);
            Text messageText = CreateText(
                "Text", messagePanel.transform, font, 48, "Tutorial");
            Stretch(messageText.rectTransform, new Vector2(30f, 20f));

            Image subPanel = CreatePanel(
                "SubMessage", guidance, roundedShader, 32f);
            SetCenteredRect(subPanel.rectTransform, new Vector2(0f, -490f),
                new Vector2(800f, 190f));
            subPanel.rectTransform.pivot = new Vector2(0.5f, 1f);
            AddPanelShadow(subPanel.gameObject);
            Text subText = CreateText(
                "Text", subPanel.transform, font, 48, "Tutorial");
            Stretch(subText.rectTransform, new Vector2(30f, 20f));

            Button hintButton = CreatePanelButton(
                "Hint", guidance, roundedShader, 32f);
            SetCenteredRect((RectTransform)hintButton.transform,
                new Vector2(2f, -670f), new Vector2(930f, 190f));
            AddPanelShadow(hintButton.gameObject);
            Text hintText = CreateText(
                "Text", hintButton.transform, font, 44, "Tap here for a hint.");
            SetCenteredRect(hintText.rectTransform, new Vector2(-30f, 0f),
                new Vector2(700f, 110f));
            Image lamp = CreateImage("Lamp", hintButton.transform);
            SetCenteredRect(lamp.rectTransform, new Vector2(335f, 0f),
                new Vector2(96f, 104f));
            lamp.sprite = LoadSprite(HintLampPath, "icon_hint_lamp_0");
            lamp.preserveAspect = true;

            Button confirm = CreateSourceButton("Confirm", guidance, font);
            SetCenteredRect((RectTransform)confirm.transform,
                new Vector2(0f, -760f), new Vector2(784f, 258f));
            Text confirmText = confirm.transform.Find("Label")?.GetComponent<Text>();

            RectTransform hand = CreateRect("Hand", guidance);
            hand.anchorMin = hand.anchorMax = new Vector2(0.5f, 0.5f);
            hand.pivot = new Vector2(0f, 1f);
            hand.anchoredPosition = new Vector2(111f, 316f);
            hand.sizeDelta = new Vector2(110f, 120f);
            Image handImage = CreateImage("Static", hand);
            SetCenteredRect(handImage.rectTransform, new Vector2(52.5f, -55f),
                new Vector2(195f, 210f));
            handImage.sprite = LoadSprite(HandPath, "ui_guide_hand_0");
            handImage.preserveAspect = true;

            RectTransform feedback = CreateRect("Feedback", root);
            Stretch(feedback);
            RectTransform success = CreateRect("SuccessCheck", feedback);
            SetCenteredRect(success, new Vector2(0f, -711f),
                new Vector2(360f, 360f));
            CanvasGroup successGroup = success.gameObject.AddComponent<CanvasGroup>();
            Image circle = CreateImage("Circle", success);
            Stretch(circle.rectTransform);
            circle.sprite = LoadSprite(
                CheckCirclePath, "success_check_circle_0");
            circle.preserveAspect = true;
            Image check = CreateImage("Check", success);
            SetCenteredRect(check.rectTransform, new Vector2(1f, -1f),
                new Vector2(188f, 135f));
            check.sprite = LoadSprite(
                CheckMarkPath, "success_check_mark_0");
            check.preserveAspect = true;

            RectTransform iqBar = CreateRect("IqBar", feedback);
            iqBar.anchorMin = iqBar.anchorMax = new Vector2(0.5f, 1f);
            iqBar.pivot = new Vector2(0.5f, 0.5f);
            iqBar.anchoredPosition = new Vector2(0f, -270f);
            iqBar.sizeDelta = new Vector2(592f, 70f);
            iqBar.localScale = Vector3.one * 1.4f;
            Image iqBackground = CreatePanel(
                "Background", iqBar, roundedShader, 40f,
                new Color(0.898f, 0.871f, 0.855f, 1f));
            Stretch(iqBackground.rectTransform);
            Image iqFill = CreatePanel(
                "Fill", iqBar, roundedShader, 35f, SourceAccent);
            iqFill.rectTransform.anchorMin = iqFill.rectTransform.anchorMax =
                new Vector2(0f, 0.5f);
            iqFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            iqFill.rectTransform.anchoredPosition = Vector2.zero;
            iqFill.rectTransform.sizeDelta = new Vector2(285f, 70f);
            Text iqText = CreateText("Label", iqBar, font, 64, "IQ=60");
            SetCenteredRect(iqText.rectTransform, new Vector2(0f, 87f),
                new Vector2(600f, 70f));

            RectTransform effects = CreateRect("Effects", feedback);
            Stretch(effects);
            TutorialFinishEffects finishEffects =
                effects.gameObject.AddComponent<TutorialFinishEffects>();
            SerializedObject effectData = new SerializedObject(finishEffects);
            ConfigureFinishEffects(effectData, effects);
            effectData.ApplyModifiedPropertiesWithoutUndo();

            TutorialPagePresenter presenter =
                page.GetComponent<TutorialPagePresenter>();
            ConfigureWindow(presenter, canvas, pageGroup);
            SerializedObject data = new SerializedObject(presenter);
            SetReference(data, "boardView", board);
            SetReference(data, "layoutSpace", root);
            SetReference(data, "boardContainer", boardContainer);
            SetReference(data, "boardInputGroup", boardInputGroup);
            SetReference(data, "cellPrefab", cellPrefab);
            SetReference(data, "maskLayer", maskLayer.gameObject);
            SetReference(data, "maskGroup", maskGroup);
            SetReference(data, "maskCellLayer", maskCells);
            SetReference(data, "selectFrame", selectFrame.rectTransform);
            SetReference(data, "handHint", hand);
            SetReference(data, "handImage", handImage);
            SetReference(data, "messagePanel", messagePanel.gameObject);
            SetReference(data, "messageText", messageText);
            SetReference(data, "subMessagePanel", subPanel.gameObject);
            SetReference(data, "subMessageText", subText);
            SetReference(data, "hintPanel", hintButton.gameObject);
            SetReference(data, "hintText", hintText);
            SetReference(data, "hintButton", hintButton);
            SetReference(data, "confirmButton", confirm);
            SetReference(data, "confirmText", confirmText);
            SetReference(data, "successCheck", success.gameObject);
            SetReference(data, "successCheckGroup", successGroup);
            SetReference(data, "iqBar", iqBar.gameObject);
            SetReference(data, "iqFill", iqFill.rectTransform);
            SetReference(data, "iqText", iqText);
            SetReference(data, "finishEffects", finishEffects);
            data.ApplyModifiedPropertiesWithoutUndo();

            maskLayer.gameObject.SetActive(false);
            messagePanel.gameObject.SetActive(false);
            subPanel.gameObject.SetActive(false);
            hintButton.gameObject.SetActive(false);
            confirm.gameObject.SetActive(false);
            hand.gameObject.SetActive(false);
            selectFrame.gameObject.SetActive(false);
            success.gameObject.SetActive(false);
            iqBar.gameObject.SetActive(false);
            return page;
        }

        private static void ConfigureWindow(
            TutorialPagePresenter presenter,
            Canvas canvas,
            CanvasGroup group)
        {
            SerializedObject data = new SerializedObject(presenter);
            data.FindProperty("uiLayer").intValue = (int)UiLayer.Tutorial;
            data.FindProperty("isFullscreen").boolValue = true;
            data.FindProperty("showMask").boolValue = false;
            data.FindProperty("playOpenSound").boolValue = false;
            data.FindProperty("rootCanvas").objectReferenceValue = canvas;
            data.FindProperty("rootCanvasGroup").objectReferenceValue = group;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Image CreatePanel(
            string name,
            Transform parent,
            Shader shader,
            float radius,
            Color? color = null)
        {
            Image image = CreateImage(name, parent);
            image.color = color ?? Color.white;
            ConfigureRounded(image, shader, radius);
            return image;
        }

        private static Button CreatePanelButton(
            string name,
            Transform parent,
            Shader shader,
            float radius)
        {
            Image image = CreatePanel(name, parent, shader, radius);
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static Button CreateSourceButton(
            string name,
            Transform parent,
            Font font)
        {
            Image image = CreateImage(name, parent);
            image.sprite = LoadSprite(PrimaryButtonPath, "btn_primary_0");
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Shadow shadow = image.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.12f);
            shadow.effectDistance = new Vector2(0f, -10f);
            Text label = CreateText("Label", image.transform, font, 56, "Got it!");
            label.color = Color.white;
            Stretch(label.rectTransform, new Vector2(80f, 35f));
            return button;
        }

        private static Image CreateImage(string name, Transform parent)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            Font font,
            int fontSize,
            string value)
        {
            RectTransform rect = CreateRect(name, parent);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Normal;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = SourceText;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.lineSpacing = 1.18f;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            SetUiLayer(gameObject);
            RectTransform rect = (RectTransform)gameObject.transform;
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
            SerializedObject data = new SerializedObject(rounded);
            data.FindProperty("target").objectReferenceValue = image;
            data.FindProperty("roundedShader").objectReferenceValue = shader;
            data.FindProperty("cornerRadius").floatValue = radius;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddPanelShadow(GameObject target)
        {
            Shadow shadow = target.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.1f);
            shadow.effectDistance = new Vector2(0f, -4f);
        }

        private static void ConfigureWindowProperty(
            SerializedObject data,
            string name,
            Object value)
        {
            SerializedProperty property = data.FindProperty(name);
            if (property != null) property.objectReferenceValue = value;
        }

        private static void SetReference(
            SerializedObject data,
            string name,
            Object value)
        {
            ConfigureWindowProperty(data, name, value);
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

        private static Sprite LoadSprite(string path, string name = null)
        {
            if (string.IsNullOrEmpty(name))
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int index = 0; index < assets.Length; index++)
            {
                if (assets[index] is Sprite sprite && sprite.name == name)
                    return sprite;
            }
            return null;
        }

        private static void UpgradeExisting()
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                TutorialFinishEffects effects =
                    contents.GetComponentInChildren<TutorialFinishEffects>(true);
                if (effects == null) return;
                SerializedObject data = new SerializedObject(effects);
                RectTransform root = data.FindProperty("effectRoot")
                    .objectReferenceValue as RectTransform;
                if (root == null) root = effects.transform as RectTransform;
                ConfigureFinishEffects(data, root);
                if (!data.ApplyModifiedPropertiesWithoutUndo()) return;
                PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void ConfigureFinishEffects(
            SerializedObject data,
            RectTransform root)
        {
            data.FindProperty("effectRoot").objectReferenceValue = root;
            data.FindProperty("lineSprite").objectReferenceValue =
                LoadSprite(FireworkLinePath, "et_line_001_0");
            data.FindProperty("starSprite").objectReferenceValue =
                LoadSprite(FireworkStarPath, "et_star_2_0");
            data.FindProperty("glowSprite").objectReferenceValue =
                LoadSprite(FireworkGlowPath, "et_glow_003_0");

            Sprite[] ribbons = LoadSprites(FireworkRibbonPath);
            SerializedProperty ribbonProperty = data.FindProperty("ribbonSprites");
            ribbonProperty.arraySize = ribbons.Length;
            for (int index = 0; index < ribbons.Length; index++)
                ribbonProperty.GetArrayElementAtIndex(index).objectReferenceValue =
                    ribbons[index];
        }

        private static Sprite[] LoadSprites(string path)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            var sprites = new System.Collections.Generic.List<Sprite>();
            for (int index = 0; index < assets.Length; index++)
            {
                if (assets[index] is Sprite sprite) sprites.Add(sprite);
            }
            sprites.Sort((left, right) =>
                string.CompareOrdinal(left.name, right.name));
            return sprites.ToArray();
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }

        private static void HandlePlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += InstallIfMissing;
        }

        private static void SetUiLayer(GameObject target)
        {
            int layer = LayerMask.NameToLayer("UI");
            target.layer = layer >= 0 ? layer : 0;
        }
    }
}
