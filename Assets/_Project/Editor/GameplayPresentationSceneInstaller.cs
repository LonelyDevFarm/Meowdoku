using Meowdoku.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Meowdoku.Editor
{
    /// <summary>
    /// Installs the source-default rule bar, hint overlay and Cell hint visuals.
    /// All prefab/scene changes go through Unity serialization APIs and remain idempotent.
    /// </summary>
    [InitializeOnLoad]
    internal static class GameplayPresentationSceneInstaller
    {
        private const string ScenePath = "Assets/_Project/Scenes/GameplayScene.unity";
        private const string CellPrefabPath = "Assets/_Project/Prefabs/Cell.prefab";
        private const string FontPath = "Assets/_Project/Fonts/Roboto.ttf";
        private const string RoundedShaderPath =
            "Assets/_Project/Shaders/UIRoundedRect.shader";
        private static readonly string[] PatternSpritePaths =
        {
            "Assets/_Project/Sprites/game/pattern_icon/pattern_claw.png",
            "Assets/_Project/Sprites/game/pattern_icon/pattern_bell.png",
            "Assets/_Project/Sprites/game/pattern_icon/pattern_fishbone.png",
            "Assets/_Project/Sprites/game/pattern_icon/pattern_whisker.png",
            "Assets/_Project/Sprites/game/pattern_icon/pattern_sparkle.png",
            "Assets/_Project/Sprites/game/pattern_icon/pattern_ear.png",
            "Assets/_Project/Sprites/game/pattern_icon/pattern_heart.png",
            "Assets/_Project/Sprites/game/pattern_icon/pattern_yarn.png",
            "Assets/_Project/Sprites/game/pattern_icon/pattern_sprout.png",
            "Assets/_Project/Sprites/game/pattern_icon/pattern_paw.png",
            "Assets/_Project/Sprites/game/pattern_icon/pattern_triangle.png",
            "Assets/_Project/Sprites/game/pattern_icon/pattern_dot.png"
        };
        private const int MaxInstallAttempts = 300;
        private static readonly Color Brown = new Color(0.576f, 0.353f, 0.353f, 1f);
        private static int _remainingInstallAttempts;

        static GameplayPresentationSceneInstaller()
        {
            QueueInstall();
            EditorSceneManager.sceneOpened += HandleSceneOpened;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        private static void HandleSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.path == ScenePath) QueueInstall();
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode) QueueInstall();
        }

        private static void QueueInstall()
        {
            _remainingInstallAttempts = MaxInstallAttempts;
            EditorApplication.update -= TryInstallWhenReady;
            EditorApplication.update += TryInstallWhenReady;
        }

        private static void TryInstallWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlaying ||
                EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Scene scene = SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.path == ScenePath)
            {
                EditorApplication.update -= TryInstallWhenReady;
                InstallIfNeeded();
                return;
            }

            if (scene.IsValid() && !string.IsNullOrEmpty(scene.path) ||
                --_remainingInstallAttempts <= 0)
                EditorApplication.update -= TryInstallWhenReady;
        }

        [MenuItem("Meowdoku/Port/Install Rule Bar And Hint Overlay")]
        private static void InstallFromMenu()
        {
            InstallIfNeeded();
        }

        private static void InstallIfNeeded()
        {
            if (EditorApplication.isPlaying ||
                EditorApplication.isPlayingOrWillChangePlaymode) return;
            UpgradeCellPrefab();
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath) return;
            GameplayManager manager = Object.FindFirstObjectByType<GameplayManager>();
            BoardView board = Object.FindFirstObjectByType<BoardView>();
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (manager == null || board == null || canvas == null) return;
            ConfigureCanvasScaler(canvas);

            RectTransform hud = EnsureRect("HUD", canvas.transform, true);
            RectTransform overlays = EnsureRect("Overlays", canvas.transform, true);
            RectTransform ruleBar = InstallRuleBar(hud, manager);
            InstallPageLayout(hud, ruleBar, board);
            InstallHintOverlay(overlays, manager, board);
            SerializedObject boardData = new SerializedObject(board);
            boardData.FindProperty("roundedBackgroundShader").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Shader>(RoundedShaderPath);
            boardData.ApplyModifiedPropertiesWithoutUndo();
            ConfigureBoardPatterns(board);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AppRuntimeSceneInstaller.UpgradeGamePagePatternAssets();
        }

        private static void UpgradeCellPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(CellPrefabPath);
            if (root == null) return;
            try
            {
                CellView view = root.GetComponent<CellView>();
                if (view == null) return;
                Image pattern = EnsureImage("Pattern", root.transform);
                SetRect(pattern.rectTransform, Vector2.zero, new Vector2(95f, 95f));
                pattern.sprite = null;
                pattern.color = Color.white;
                pattern.preserveAspect = true;
                pattern.raycastTarget = false;
                int backgroundIndex = view.bgImage != null
                    ? view.bgImage.transform.GetSiblingIndex()
                    : 0;
                pattern.transform.SetSiblingIndex(Mathf.Min(
                    backgroundIndex + 1,
                    root.transform.childCount - 1));
                pattern.gameObject.SetActive(false);
                RectTransform hintRoot = EnsureRect("HintVisuals", root.transform, true);
                Image light = EnsureImage("HintLight", hintRoot);
                Image frame = EnsureImage("PromptFrame", hintRoot);
                Image cross = EnsureImage("PromptCross", hintRoot);
                Sprite frameSprite = LoadSprite(
                    "Assets/_Project/Sprites/game/icon_prompt_frame.png");
                Sprite crossSprite = LoadSprite(
                    "Assets/_Project/Sprites/game/icon_mark_white_3.png");
                ConfigureCellVisual(light, frameSprite);
                ConfigureCellVisual(frame, frameSprite);
                ConfigureCellVisual(cross, crossSprite);
                light.gameObject.SetActive(false);
                frame.gameObject.SetActive(false);
                cross.gameObject.SetActive(false);

                Image error = root.transform.Find("ErrorIcon")?.GetComponent<Image>();
                if (error != null)
                    error.color = new Color(0.99215686f, 0.41568628f, 0.18039216f, 1f);
                SerializedObject data = new SerializedObject(view);
                data.FindProperty("patternImage").objectReferenceValue = pattern;
                data.FindProperty("hintLight").objectReferenceValue = light;
                data.FindProperty("promptFrame").objectReferenceValue = frame;
                data.FindProperty("promptCross").objectReferenceValue = cross;
                data.FindProperty("roundedBackgroundShader").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Shader>(RoundedShaderPath);
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, CellPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static RectTransform InstallRuleBar(
            RectTransform hud,
            GameplayManager manager)
        {
            RectTransform root = EnsureRect("RuleBar", hud, false);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(1080f, 170f);

            RectTransform effects = EnsureRect("Effects", root, true);
            effects.SetAsFirstSibling();
            Image glow = EnsureImage("Glow", effects);
            SetRect(glow.rectTransform, new Vector2(3f, 2f),
                new Vector2(1110f, 250f));
            glow.sprite = LoadNineSliceSprite(
                "Assets/_Project/Sprites/Effects/mask/et_mask_001.png",
                new Vector4(120f, 116f, 116f, 117f));
            glow.type = Image.Type.Sliced;
            glow.preserveAspect = false;
            glow.color = new Color(1f, 0.58695984f, 0f, 1f);
            glow.raycastTarget = false;

            Image outer = EnsureImage("Background", root);
            SetRect(outer.rectTransform, Vector2.zero, new Vector2(1008f, 162f));
            ConfigureRoundedImage(outer, Color.white);

            RectTransform rules = EnsureRect("Rules", root, true);
            RectTransform highlights = EnsureRect("Highlights", root, true);
            string[] labels =
            {
                "1 Cat per color",
                "1 Cat per column and row",
                "Cats can't be adjacent"
            };
            float[] x = { -329f, 0f, 329f };
            Image[] highlightImages = new Image[3];
            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            Sprite highlightSprite = LoadSprite(
                "Assets/_Project/Sprites/game/rule_highlight.png");
            for (int index = 0; index < labels.Length; index++)
            {
                Image pill = EnsureImage($"Rule{index + 1}", rules);
                SetRect(pill.rectTransform, new Vector2(x[index], 0f),
                    new Vector2(310f, 120f));
                ConfigureRoundedImage(pill, new Color(0.9843f, 0.95686f, 0.93333f, 1f));
                Text text = EnsureText("Label", pill.transform, font, 36, labels[index]);
                Stretch(text.rectTransform, new Vector2(12f, 4f));
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Truncate;
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 12;
                text.resizeTextMaxSize = 36;
                // Source uses line_spacing = -10 at font size 36.
                text.lineSpacing = 0.72f;

                Image highlight = EnsureImage($"Rule{index + 1}", highlights);
                SetRect(highlight.rectTransform, new Vector2(x[index], 0f),
                    new Vector2(330f, 140f));
                highlight.sprite = highlightSprite;
                highlight.preserveAspect = true;
                highlight.raycastTarget = false;
                highlight.color = new Color(1f, 1f, 1f, 0f);
                highlight.gameObject.SetActive(false);
                highlightImages[index] = highlight;
            }

            GameplayRuleBarPresenter presenter =
                root.GetComponent<GameplayRuleBarPresenter>();
            if (presenter == null)
                presenter = root.gameObject.AddComponent<GameplayRuleBarPresenter>();
            SerializedObject serialized = new SerializedObject(presenter);
            serialized.FindProperty("gameplayManager").objectReferenceValue = manager;
            SetObjectArray(serialized.FindProperty("highlights"), highlightImages);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private static void InstallPageLayout(
            RectTransform hud,
            RectTransform ruleBar,
            BoardView board)
        {
            GameplayPageLayoutPresenter presenter =
                hud.GetComponent<GameplayPageLayoutPresenter>();
            if (presenter == null)
                presenter = hud.gameObject.AddComponent<GameplayPageLayoutPresenter>();
            SerializedObject serialized = new SerializedObject(presenter);
            serialized.FindProperty("layoutSpace").objectReferenceValue = hud;
            serialized.FindProperty("header").objectReferenceValue =
                hud.Find("Header") as RectTransform;
            serialized.FindProperty("catHeartRow").objectReferenceValue =
                hud.Find("CatHeartRow") as RectTransform;
            serialized.FindProperty("ruleBar").objectReferenceValue = ruleBar;
            serialized.FindProperty("board").objectReferenceValue =
                board.cellsContainer as RectTransform;
            serialized.FindProperty("boardView").objectReferenceValue = board;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            presenter.ApplyLayout();
        }

        private static void ConfigureCanvasScaler(Canvas canvas)
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 2400f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            scaler.referencePixelsPerUnit = 100f;
        }

        private static void InstallHintOverlay(
            RectTransform overlays,
            GameplayManager manager,
            BoardView board)
        {
            GameplayHintOverlayPresenter presenter =
                overlays.GetComponent<GameplayHintOverlayPresenter>();
            if (presenter == null)
                presenter = overlays.gameObject.AddComponent<GameplayHintOverlayPresenter>();

            RectTransform visualRoot = EnsureRect("HintOverlay", overlays, true);
            Image dim = EnsureImage("Dim", visualRoot);
            Stretch(dim.rectTransform, Vector2.zero);
            dim.color = new Color(0f, 0f, 0f, 0.749f);
            dim.raycastTarget = true;

            RectTransform highlightLayer = EnsureRect("Highlights", visualRoot, true);
            RectTransform banner = EnsureRect("Banner", visualRoot, false);
            banner.anchorMin = banner.anchorMax = new Vector2(0.5f, 0.5f);
            banner.pivot = new Vector2(0.5f, 0f);
            banner.sizeDelta = new Vector2(900f, 190f);
            Image bannerBackground = EnsureImage("Background", banner);
            Stretch(bannerBackground.rectTransform, Vector2.zero);
            ConfigureRoundedImage(bannerBackground, new Color(1f, 0.984f, 0.969f, 1f));

            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            Text strategy = EnsureText("Strategy", banner, font, 56, "R4");
            strategy.color = new Color(0.851f, 0.282f, 0.282f, 1f);
            strategy.alignment = TextAnchor.MiddleCenter;
            SetAnchoredRect(strategy.rectTransform,
                new Vector2(30f, 0f), new Vector2(200f, 190f));
            Text description = EnsureText(
                "Description", banner, font, 36, "HINT_CONTRADICTION");
            description.fontStyle = FontStyle.Normal;
            description.alignment = TextAnchor.MiddleLeft;
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            SetAnchoredRect(description.rectTransform,
                new Vector2(50f, 0f), new Vector2(850f, 190f));

            Button detail = EnsureButton(
                "Detail", banner, font, 38, "Deduce steps",
                new Color(0.851f, 0.282f, 0.282f, 1f), Color.white);
            SetAnchoredRect(detail.transform as RectTransform,
                new Vector2(620f, 45f), new Vector2(850f, 145f));

            RectTransform buttons = EnsureRect("Buttons", visualRoot, false);
            buttons.anchorMin = buttons.anchorMax = new Vector2(0.5f, 0.5f);
            buttons.pivot = new Vector2(0.5f, 1f);
            buttons.sizeDelta = new Vector2(750f, 360f);
            Button apply = EnsureButton(
                "Apply", buttons, font, 42, "Apply",
                new Color(0.851f, 0.282f, 0.282f, 1f), Color.white);
            SetRect(apply.transform as RectTransform, new Vector2(0f, -80f),
                new Vector2(750f, 160f));
            Button dismiss = EnsureButton(
                "Dismiss", buttons, font, 42, "Cancel",
                new Color(1f, 0.8117647f, 0.58431375f, 1f),
                new Color(0.7529412f, 0.45490196f, 0.25490198f, 1f));
            SetRect(dismiss.transform as RectTransform, new Vector2(0f, -280f),
                new Vector2(750f, 160f));

            SerializedObject serialized = new SerializedObject(presenter);
            serialized.FindProperty("gameplayManager").objectReferenceValue = manager;
            serialized.FindProperty("boardView").objectReferenceValue = board;
            serialized.FindProperty("cellPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(CellPrefabPath);
            serialized.FindProperty("visualRoot").objectReferenceValue = visualRoot.gameObject;
            serialized.FindProperty("layoutSpace").objectReferenceValue = overlays;
            serialized.FindProperty("highlightLayer").objectReferenceValue = highlightLayer;
            serialized.FindProperty("banner").objectReferenceValue = banner;
            serialized.FindProperty("buttonGroup").objectReferenceValue = buttons;
            serialized.FindProperty("dimImage").objectReferenceValue = dim;
            serialized.FindProperty("strategyLabel").objectReferenceValue = strategy;
            serialized.FindProperty("descriptionLabel").objectReferenceValue = description;
            serialized.FindProperty("applyButton").objectReferenceValue = apply;
            serialized.FindProperty("dismissButton").objectReferenceValue = dismiss;
            serialized.FindProperty("detailButton").objectReferenceValue = detail;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            visualRoot.gameObject.SetActive(false);
        }

        private static Button EnsureButton(
            string name,
            Transform parent,
            Font font,
            int fontSize,
            string label,
            Color background,
            Color foreground)
        {
            RectTransform rect = EnsureRect(name, parent, false);
            Image image = rect.GetComponent<Image>();
            if (image == null) image = rect.gameObject.AddComponent<Image>();
            ConfigureRoundedImage(image, background);
            image.raycastTarget = true;
            Button button = rect.GetComponent<Button>();
            if (button == null) button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Text text = EnsureText("Label", rect, font, fontSize, label);
            text.color = foreground;
            Stretch(text.rectTransform, new Vector2(20f, 10f));
            return button;
        }

        private static void ConfigureRoundedImage(Image image, Color color)
        {
            // Godot StyleBox corners are completed in R7. Do not substitute an
            // unrelated button texture while the exact Unity mesh adapter is pending.
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = color;
            image.raycastTarget = false;
        }

        private static void ConfigureCellVisual(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;
            Stretch(image.rectTransform, Vector2.zero);
        }

        private static RectTransform EnsureRect(string name, Transform parent, bool stretch)
        {
            Transform existing = parent.Find(name);
            RectTransform rect = existing as RectTransform;
            if (rect == null)
            {
                var gameObject = new GameObject(name, typeof(RectTransform));
                gameObject.layer = LayerMask.NameToLayer("UI");
                rect = (RectTransform)gameObject.transform;
                rect.SetParent(parent, false);
            }
            if (stretch) Stretch(rect, Vector2.zero);
            return rect;
        }

        private static Image EnsureImage(string name, Transform parent)
        {
            RectTransform rect = EnsureRect(name, parent, false);
            Image image = rect.GetComponent<Image>();
            if (image == null) image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static Text EnsureText(
            string name,
            Transform parent,
            Font font,
            int fontSize,
            string value)
        {
            RectTransform rect = EnsureRect(name, parent, false);
            Text text = rect.GetComponent<Text>();
            if (text == null) text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Brown;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = value;
            return text;
        }

        private static void SetObjectArray(SerializedProperty property, Object[] values)
        {
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }

        internal static bool ConfigureBoardPatterns(BoardView board)
        {
            if (board == null) return false;
            var sprites = new Object[PatternSpritePaths.Length];
            for (int index = 0; index < PatternSpritePaths.Length; index++)
                sprites[index] = LoadSprite(PatternSpritePaths[index]);

            SerializedObject data = new SerializedObject(board);
            SerializedProperty property = data.FindProperty("patternIcons");
            if (property == null) return false;
            bool changed = property.arraySize != sprites.Length;
            if (!changed)
            {
                for (int index = 0; index < sprites.Length; index++)
                {
                    if (property.GetArrayElementAtIndex(index)
                            .objectReferenceValue == sprites[index])
                        continue;
                    changed = true;
                    break;
                }
            }
            if (!changed) return false;
            SetObjectArray(property, sprites);
            data.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        private static void SetRect(RectTransform rect, Vector2 center, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = center;
            rect.sizeDelta = size;
        }

        private static void SetAnchoredRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = min;
            rect.sizeDelta = max - min;
        }

        private static void Stretch(RectTransform rect, Vector2 inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = inset;
            rect.offsetMax = -inset;
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite LoadNineSliceSprite(string path, Vector4 border)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null &&
                (importer.spriteImportMode != SpriteImportMode.Single ||
                 importer.spriteBorder != border))
            {
                // Godot NinePatchRect uses the complete 256x256 texture. The
                // extracted Unity import was auto-trimmed as Multiple, which
                // distorts the glow and cannot carry the source patch margins.
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = border;
                importer.SaveAndReimport();
            }
            return LoadSprite(path);
        }
    }
}
