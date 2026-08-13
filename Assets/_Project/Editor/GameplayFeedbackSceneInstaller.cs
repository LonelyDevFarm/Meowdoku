using System.Collections.Generic;
using Meowdoku.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Meowdoku.Editor
{
    /// <summary>
    /// Idempotent editor-time scene port. It creates serialized UGUI objects
    /// through Unity APIs; no scene YAML is guessed or patched.
    /// </summary>
    [InitializeOnLoad]
    internal static class GameplayFeedbackSceneInstaller
    {
        private const string ScenePath = "Assets/_Project/Scenes/GameplayScene.unity";
        private const int MaxInstallAttempts = 300;
        private static int _remainingInstallAttempts;

        static GameplayFeedbackSceneInstaller()
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

        [MenuItem("Meowdoku/Port/Install Gameplay Feedback UI")]
        private static void InstallFromMenu()
        {
            InstallIfNeeded();
        }

        private static void InstallIfNeeded()
        {
            if (EditorApplication.isPlaying ||
                EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath) return;
            GameplayManager manager = Object.FindFirstObjectByType<GameplayManager>();
            BoardView board = Object.FindFirstObjectByType<BoardView>();
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (manager == null || board == null || canvas == null) return;

            EnsureSceneTree(
                scene,
                canvas,
                manager,
                board,
                out RectTransform topBar,
                out RectTransform catHeartRow,
                out RectTransform feedbackRoot,
                out RectTransform hudRoot);

            GameplayFeedbackPresenter presenter =
                canvas.GetComponentInChildren<GameplayFeedbackPresenter>(true);
            if (presenter == null)
                presenter = feedbackRoot.gameObject.AddComponent<GameplayFeedbackPresenter>();
            else if (presenter.transform != feedbackRoot)
            {
                RectTransform legacy = presenter.transform as RectTransform;
                MoveChildren(legacy, feedbackRoot, "ScoreDisplay");
                Object.DestroyImmediate(presenter);
                presenter = feedbackRoot.gameObject.AddComponent<GameplayFeedbackPresenter>();
                if (legacy != null && legacy.childCount == 0)
                    Object.DestroyImmediate(legacy.gameObject);
            }

            Text scoreValue = EnsureScoreHeader(topBar, feedbackRoot);
            Text levelValue = EnsureHeader(topBar);
            GameplayLifeHudPresenter lifeHud = EnsureLifeHud(catHeartRow, hudRoot, manager);
            EnsureHudPresenter(hudRoot, catHeartRow, manager, levelValue);
            EnsurePageLayout(hudRoot, topBar, catHeartRow, board);

            RectTransform scoreGroup = EnsurePoolGroup(
                feedbackRoot, "ScoreBubbles", "ScoreBubble_");
            RectTransform deductionGroup = EnsurePoolGroup(
                feedbackRoot, "DeductionBubbles", "DeductionBubble_");
            RectTransform skillGroup = EnsurePoolGroup(
                feedbackRoot, "SkillBubbles", "SkillBubble_");
            RectTransform multiplierGroup = EnsurePoolGroup(
                feedbackRoot, "Multipliers", "Multiplier_");
            RectTransform flightGroup = EnsurePoolGroup(
                feedbackRoot, "ScoreFlights", "ScoreFlight_");

            Image encourage = feedbackRoot.Find("Encourage")?.GetComponent<Image>();
            if (encourage == null) encourage = CreateImage("Encourage", feedbackRoot);
            RectTransform encourageRect = encourage.rectTransform;
            encourageRect.anchorMin = encourageRect.anchorMax = new Vector2(0.5f, 1f);
            encourageRect.anchoredPosition = new Vector2(0f, -245f);
            encourage.raycastTarget = false;
            encourage.gameObject.SetActive(false);

            GameplayFeedbackBubbleView[] scorePool = EnsureBubblePool(
                scoreGroup, "ScoreBubble", 8, "score_font", "ui_mao_sz_pic_", -18f);
            GameplayFeedbackBubbleView[] deductionPool = EnsureBubblePool(
                deductionGroup, "DeductionBubble", 4, "deduct_font", "ui_mao_jf_pic_", -6f);
            GameplayFeedbackBubbleView[] skillPool = EnsureBubblePool(
                skillGroup, "SkillBubble", 4, "multiplier_font", "ui_mao_cf_pic_", -6f);
            GameplayMultiplierView[] multiplierPool = EnsureMultiplierPool(
                multiplierGroup, 4);
            GameplayScoreFlightView[] flightPool = EnsureScoreFlightPool(
                flightGroup, 6);

            SerializedObject serialized = new SerializedObject(presenter);
            serialized.FindProperty("gameplayManager").objectReferenceValue = manager;
            serialized.FindProperty("boardView").objectReferenceValue = board;
            serialized.FindProperty("feedbackArea").objectReferenceValue = feedbackRoot;
            serialized.FindProperty("scoreValue").objectReferenceValue = scoreValue;
            serialized.FindProperty("lifeHudPresenter").objectReferenceValue = lifeHud;
            serialized.FindProperty("encourageImage").objectReferenceValue = encourage;
            SetObjectArray(serialized.FindProperty("encourageSprites"), LoadSprites(
                "Assets/_Project/Sprites/encourage/ui_mao_glc_pic_", 1, 6));
            SetObjectArray(serialized.FindProperty("scoreBubbles"), scorePool);
            SetObjectArray(serialized.FindProperty("deductionBubbles"), deductionPool);
            SetObjectArray(serialized.FindProperty("skillBubbles"), skillPool);
            SetObjectArray(serialized.FindProperty("multiplierViews"), multiplierPool);
            SetObjectArray(serialized.FindProperty("scoreFlights"), flightPool);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            WirePresenters(manager, presenter, lifeHud);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        internal static bool ConfigureLifeEffects(Transform scope)
        {
            if (scope == null) return false;
            GameplayLifeHudPresenter presenter =
                scope.GetComponentInChildren<GameplayLifeHudPresenter>(true);
            if (presenter == null) return false;

            Sprite[] fishFrames = LoadAllSprites(
                "Assets/_Project/Sprites/Effects/obj/et_fish_full_001.png");
            Sprite fishGlow = LoadFirstSprite(
                "Assets/_Project/Sprites/Effects/glow/et_glow_fish.png");
            Sprite reviveGlow = LoadFirstSprite(
                "Assets/_Project/Sprites/Effects/ui/et_broken_fish_glow.png");
            if (fishFrames.Length == 0 || fishGlow == null || reviveGlow == null)
                return false;

            SerializedObject data = new(presenter);
            SerializedProperty slots = data.FindProperty("slots");
            if (slots == null || slots.arraySize == 0) return false;
            for (int index = 0; index < slots.arraySize; index++)
            {
                GameplayLifeSlotView slot = slots.GetArrayElementAtIndex(index)
                    .objectReferenceValue as GameplayLifeSlotView;
                EnsureLifeEffects(slot, fishFrames, fishGlow, reviveGlow);
            }
            return true;
        }

        private static void EnsureSceneTree(
            Scene scene,
            Canvas canvas,
            GameplayManager manager,
            BoardView board,
            out RectTransform topBar,
            out RectTransform catHeartRow,
            out RectTransform feedbackRoot,
            out RectTransform hudRoot)
        {
            RectTransform gameplay = EnsureRect("Gameplay", canvas.transform);
            hudRoot = EnsureRect("HUD", canvas.transform);
            EnsureRect("Overlays", canvas.transform);
            topBar = EnsureHeaderRoot(hudRoot);
            catHeartRow = EnsureRect("CatHeartRow", hudRoot);
            catHeartRow.sizeDelta = new Vector2(1080f, 88f);

            GameplayFeedbackPresenter existing =
                canvas.GetComponentInChildren<GameplayFeedbackPresenter>(true);
            RectTransform feedback = existing != null
                ? existing.transform as RectTransform
                : null;
            if (feedback == null)
                feedback = EnsureRect("Feedback", hudRoot);
            else
            {
                feedback.name = "Feedback";
                feedback.SetParent(hudRoot, false);
                Stretch(feedback);
            }
            feedbackRoot = feedback;

            RectTransform boardRect = board.transform as RectTransform;
            if (boardRect != null && boardRect.parent != gameplay)
                boardRect.SetParent(gameplay, false);

            Transform systems = FindSceneRoot(scene, "Systems");
            if (systems == null)
            {
                var systemsObject = new GameObject("Systems");
                SceneManager.MoveGameObjectToScene(systemsObject, scene);
                systems = systemsObject.transform;
            }
            if (manager.transform.parent != systems)
                manager.transform.SetParent(systems, false);
        }

        private static Text EnsureScoreHeader(RectTransform topBar, RectTransform feedbackRoot)
        {
            Transform displayTransform = topBar.Find("ScoreDisplay");
            if (displayTransform == null)
            {
                Transform legacy = feedbackRoot.Find("ScoreDisplay");
                if (legacy != null)
                {
                    legacy.SetParent(topBar, false);
                    displayTransform = legacy;
                }
            }
            Text value = displayTransform != null
                ? displayTransform.Find("Value")?.GetComponent<Text>()
                : null;
            if (value == null) value = CreateScoreHeader(topBar);
            ConfigureScoreHeader(value);
            return value;
        }

        private static RectTransform EnsureHeaderRoot(RectTransform hudRoot)
        {
            RectTransform header = hudRoot.Find("Header") as RectTransform;
            RectTransform legacy = hudRoot.Find("TopBar") as RectTransform;
            if (header == null && legacy != null)
            {
                legacy.name = "Header";
                header = legacy;
            }
            if (header == null) header = EnsureRect("Header", hudRoot);
            if (legacy != null && legacy != header)
            {
                MoveChildren(legacy, header, string.Empty);
                Object.DestroyImmediate(legacy.gameObject);
            }
            header.sizeDelta = new Vector2(1080f, 120f);
            return header;
        }

        private static Text EnsureHeader(RectTransform header)
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/Fonts/Roboto.ttf");
            EnsureRoundHeaderButton(
                "BackBtn", header, new Vector2(25f, 10f),
                "Assets/_Project/Sprites/common/icon_back.png");
            EnsureRoundHeaderButton(
                "SettingsBtn", header, new Vector2(934f, 10f),
                "Assets/_Project/Sprites/common/icon_settings.png");

            RectTransform level = EnsureRect("LevelDisplay", header);
            SetTopLeftRect(level, 274f, -12f, 256f, 118f);
            Text title = EnsureText("Title", level, font, 50, "Level");
            SetTopLeftRect(title.rectTransform, 0f, 3f, 256f, 60f);
            Text value = EnsureText("Value", level, font, 58, "1");
            SetTopLeftRect(value.rectTransform, 0f, 58f, 256f, 60f);
            return value;
        }

        private static void EnsureRoundHeaderButton(
            string name,
            RectTransform header,
            Vector2 topLeft,
            string iconPath)
        {
            RectTransform root = EnsureRect(name, header);
            SetTopLeftRect(root, topLeft.x, topLeft.y, 120f, 120f);
            Image baseImage = EnsureImage("Base", root);
            SetCenteredRect(baseImage.rectTransform, Vector2.zero, new Vector2(152f, 152f));
            baseImage.sprite = LoadSprite("Assets/_Project/Sprites/common/round_btn_base.png");
            baseImage.preserveAspect = true;
            baseImage.raycastTarget = true;
            Image icon = EnsureImage("Icon", root);
            SetCenteredRect(icon.rectTransform, Vector2.zero, new Vector2(100f, 100f));
            icon.sprite = LoadSprite(iconPath);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            Button button = root.GetComponent<Button>();
            if (button == null) button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = baseImage;
            button.transition = Selectable.Transition.ColorTint;
        }

        private static GameplayHudPresenter EnsureHudPresenter(
            RectTransform hudRoot,
            RectTransform catHeartRow,
            GameplayManager manager,
            Text levelValue)
        {
            RectTransform target = EnsureRect("Target", catHeartRow);
            SetTopLeftRect(target, 236f, -18f, 283f, 128f);
            Image background = EnsureImage("CatCountBg", target);
            SetTopLeftRect(background.rectTransform, 19f, 20f, 260f, 84f);
            background.sprite = null;
            background.color = Color.white;

            Image catFace = EnsureImage("CatFaceIcon", target);
            SetTopLeftRect(catFace.rectTransform, 53f, 27f, 77.44f, 70.4f);
            catFace.sprite = LoadSprite("Assets/_Project/Sprites/common/cat_face.png");
            catFace.preserveAspect = true;

            Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/Fonts/Roboto.ttf");
            Text count = EnsureText("CatCountLabel", target, font, 56, "0/4");
            SetTopLeftRect(count.rectTransform, 113f, 21f, 156f, 84f);
            count.supportRichText = true;

            GameplayHudPresenter presenter = hudRoot.GetComponent<GameplayHudPresenter>();
            if (presenter == null)
                presenter = hudRoot.gameObject.AddComponent<GameplayHudPresenter>();
            SerializedObject data = new SerializedObject(presenter);
            data.FindProperty("gameplayManager").objectReferenceValue = manager;
            data.FindProperty("levelValue").objectReferenceValue = levelValue;
            data.FindProperty("catCountLabel").objectReferenceValue = count;
            data.FindProperty("catTarget").objectReferenceValue = target;
            data.ApplyModifiedPropertiesWithoutUndo();
            return presenter;
        }

        private static void EnsurePageLayout(
            RectTransform hudRoot,
            RectTransform header,
            RectTransform catHeartRow,
            BoardView board)
        {
            GameplayPageLayoutPresenter presenter =
                hudRoot.GetComponent<GameplayPageLayoutPresenter>();
            if (presenter == null)
                presenter = hudRoot.gameObject.AddComponent<GameplayPageLayoutPresenter>();
            SerializedObject data = new SerializedObject(presenter);
            data.FindProperty("layoutSpace").objectReferenceValue = hudRoot;
            data.FindProperty("header").objectReferenceValue = header;
            data.FindProperty("catHeartRow").objectReferenceValue = catHeartRow;
            data.FindProperty("ruleBar").objectReferenceValue =
                hudRoot.Find("RuleBar") as RectTransform;
            data.FindProperty("board").objectReferenceValue =
                board.cellsContainer as RectTransform;
            data.FindProperty("boardView").objectReferenceValue = board;
            data.ApplyModifiedPropertiesWithoutUndo();
            presenter.ApplyLayout();
        }

        private static void ConfigureScoreHeader(Text value)
        {
            if (value == null) return;
            RectTransform display = value.transform.parent as RectTransform;
            if (display != null)
            {
                display.anchorMin = display.anchorMax = new Vector2(0f, 1f);
                display.pivot = new Vector2(0.5f, 1f);
                // Source default two-column header: offsets -502..-302 from right.
                display.anchoredPosition = new Vector2(678f, 12f);
                display.sizeDelta = new Vector2(200f, 118f);
            }
            Text title = display != null ? display.Find("Title")?.GetComponent<Text>() : null;
            if (title != null)
            {
                title.horizontalOverflow = HorizontalWrapMode.Overflow;
                title.verticalOverflow = VerticalWrapMode.Overflow;
            }
            value.horizontalOverflow = HorizontalWrapMode.Overflow;
            // Godot Label does not discard the 58 px glyph when its font metrics
            // exceed the 60 px control. Unity Truncate did, hiding the whole value.
            value.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private static GameplayLifeHudPresenter EnsureLifeHud(
            RectTransform catHeartRow,
            RectTransform hudRoot,
            GameplayManager manager)
        {
            GameplayLifeHudPresenter existing =
                hudRoot.GetComponentInChildren<GameplayLifeHudPresenter>(true);
            RectTransform root = existing != null
                ? existing.transform as RectTransform
                : null;
            if (root == null) root = CreateRect("HeartBg", catHeartRow);
            root.name = "HeartBg";
            root.SetParent(catHeartRow, false);
            SetTopLeftRect(root, 565f, 2f, 260f, 84f);
            Image heartBackground = root.GetComponent<Image>();
            if (heartBackground == null) heartBackground = root.gameObject.AddComponent<Image>();
            heartBackground.sprite = null;
            heartBackground.color = Color.white;
            heartBackground.raycastTarget = false;
            GameplayLifeHudPresenter presenter = existing != null
                ? existing
                : root.gameObject.AddComponent<GameplayLifeHudPresenter>();

            Sprite full = LoadSprite("Assets/_Project/Sprites/game/fish_full.png");
            Sprite dim = LoadSprite("Assets/_Project/Sprites/game/fish_dim.png");
            Sprite[] fishFrames = LoadAllSprites(
                "Assets/_Project/Sprites/Effects/obj/et_fish_full_001.png");
            Sprite fishGlow = LoadFirstSprite(
                "Assets/_Project/Sprites/Effects/glow/et_glow_fish.png");
            Sprite reviveGlow = LoadFirstSprite(
                "Assets/_Project/Sprites/Effects/ui/et_broken_fish_glow.png");

            if (existing != null)
            {
                SerializedObject existingData = new SerializedObject(existing);
                existingData.FindProperty("gameplayManager").objectReferenceValue = manager;
                SerializedProperty existingSlots = existingData.FindProperty("slots");
                for (int index = 0; index < existingSlots.arraySize; index++)
                {
                    GameplayLifeSlotView slotView = existingSlots
                        .GetArrayElementAtIndex(index).objectReferenceValue as
                        GameplayLifeSlotView;
                    EnsureLifeEffects(
                        slotView, fishFrames, fishGlow, reviveGlow);
                }
                existingData.ApplyModifiedPropertiesWithoutUndo();
                return existing;
            }

            var slots = new GameplayLifeSlotView[3];
            float[] centers = { -68f, 0f, 69f };
            for (int index = 0; index < slots.Length; index++)
            {
                RectTransform slot = CreateRect($"LifeSlot{index + 1}", root);
                slot.anchorMin = slot.anchorMax = new Vector2(0.5f, 0.5f);
                slot.anchoredPosition = new Vector2(centers[index], 0f);
                slot.sizeDelta = new Vector2(60f, 60f);
                Image dimImage = CreateImage("Dim", slot);
                ConfigureLifeImage(dimImage, dim);
                Image fullImage = CreateImage("Full", slot);
                ConfigureLifeImage(fullImage, full);
                GameplayLifeSlotView view = slot.gameObject.AddComponent<GameplayLifeSlotView>();
                SerializedObject slotData = new SerializedObject(view);
                slotData.FindProperty("dimImage").objectReferenceValue = dimImage;
                slotData.FindProperty("fullImage").objectReferenceValue = fullImage;
                slotData.ApplyModifiedPropertiesWithoutUndo();
                EnsureLifeEffects(view, fishFrames, fishGlow, reviveGlow);
                view.ShowAlive();
                slots[index] = view;
            }

            SerializedObject data = new SerializedObject(presenter);
            data.FindProperty("gameplayManager").objectReferenceValue = manager;
            SetObjectArray(data.FindProperty("slots"), slots);
            data.ApplyModifiedPropertiesWithoutUndo();
            return presenter;
        }

        private static void EnsureLifeEffects(
            GameplayLifeSlotView view,
            Sprite[] fishFrames,
            Sprite fishGlowSprite,
            Sprite reviveGlowSprite)
        {
            if (view == null) return;
            RectTransform effects = EnsureRect("Effects", view.transform);
            Stretch(effects);
            effects.SetAsLastSibling();

            Image reviveGlow = EnsureImage("ReviveGlow", effects);
            SetCenteredRect(
                reviveGlow.rectTransform, Vector2.zero, new Vector2(84f, 84f));
            reviveGlow.sprite = reviveGlowSprite;
            reviveGlow.color = Color.white;
            reviveGlow.gameObject.SetActive(false);

            var fishParticles = new Image[6];
            var glowParticles = new Image[6];
            for (int index = 0; index < 6; index++)
            {
                Image fish = EnsureImage($"FishParticle{index + 1}", effects);
                SetCenteredRect(
                    fish.rectTransform, Vector2.zero, new Vector2(26f, 26f));
                fish.sprite = fishFrames != null && fishFrames.Length > 0
                    ? fishFrames[index % fishFrames.Length]
                    : null;
                fish.color = Color.white;
                fish.gameObject.SetActive(false);
                fishParticles[index] = fish;

                Image glow = EnsureImage($"GlowParticle{index + 1}", effects);
                SetCenteredRect(
                    glow.rectTransform, Vector2.zero, new Vector2(18f, 18f));
                glow.sprite = fishGlowSprite;
                glow.color = Color.white;
                glow.gameObject.SetActive(false);
                glowParticles[index] = glow;
            }

            SerializedObject data = new(view);
            data.FindProperty("reviveGlow").objectReferenceValue = reviveGlow;
            SetObjectArray(data.FindProperty("fishParticles"), fishParticles);
            SetObjectArray(data.FindProperty("glowParticles"), glowParticles);
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureLifeImage(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static RectTransform EnsureRect(string name, Transform parent)
        {
            Transform existing = parent.Find(name);
            RectTransform rect = existing as RectTransform;
            if (rect == null) rect = CreateRect(name, parent);
            Stretch(rect);
            return rect;
        }

        private static Transform FindSceneRoot(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
                if (roots[index].name == name) return roots[index].transform;
            return null;
        }

        private static void MoveChildren(
            RectTransform source,
            RectTransform destination,
            string excludedName)
        {
            if (source == null) return;
            for (int index = source.childCount - 1; index >= 0; index--)
            {
                Transform child = source.GetChild(index);
                if (child.name == excludedName) continue;
                child.SetParent(destination, false);
            }
        }

        private static Text CreateScoreHeader(RectTransform root)
        {
            RectTransform display = CreateRect("ScoreDisplay", root);
            display.anchorMin = display.anchorMax = new Vector2(0f, 1f);
            display.pivot = new Vector2(0.5f, 1f);
            // Source: right-anchored offsets -502..-302 on a 1080-wide header.
            display.anchoredPosition = new Vector2(678f, -53f);
            display.sizeDelta = new Vector2(200f, 118f);
            Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/Fonts/Roboto.ttf");
            Text title = CreateText("Title", display, font, 50, "Score");
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -3f);
            title.rectTransform.sizeDelta = new Vector2(0f, 60f);
            Text value = CreateText("Value", display, font, 58, "0");
            value.rectTransform.anchorMin = new Vector2(0f, 1f);
            value.rectTransform.anchorMax = new Vector2(1f, 1f);
            value.rectTransform.pivot = new Vector2(0.5f, 1f);
            value.rectTransform.anchoredPosition = new Vector2(0f, -58f);
            value.rectTransform.sizeDelta = new Vector2(0f, 60f);
            ConfigureScoreHeader(value);
            return value;
        }

        private static RectTransform EnsurePoolGroup(
            RectTransform feedbackRoot,
            string groupName,
            string childPrefix)
        {
            RectTransform group = EnsureRect(groupName, feedbackRoot);
            for (int index = feedbackRoot.childCount - 1; index >= 0; index--)
            {
                Transform child = feedbackRoot.GetChild(index);
                if (child == group || !child.name.StartsWith(childPrefix)) continue;
                child.SetParent(group, false);
            }
            return group;
        }

        private static GameplayFeedbackBubbleView[] EnsureBubblePool(
            RectTransform root,
            string prefix,
            int count,
            string folder,
            string filePrefix,
            float separation)
        {
            var result = new GameplayFeedbackBubbleView[count];
            Sprite sign = LoadSprite($"Assets/_Project/Sprites/game/{folder}/{filePrefix}10.png");
            Sprite[] digits = LoadSprites(
                $"Assets/_Project/Sprites/game/{folder}/{filePrefix}", 0, 10);
            for (int index = 0; index < count; index++)
            {
                string objectName = $"{prefix}_{index + 1}";
                RectTransform bubbleRoot = root.Find(objectName) as RectTransform;
                GameplayFeedbackBubbleView existing =
                    bubbleRoot != null
                        ? bubbleRoot.GetComponent<GameplayFeedbackBubbleView>()
                        : null;
                if (existing != null)
                {
                    result[index] = existing;
                    continue;
                }
                if (bubbleRoot != null) Object.DestroyImmediate(bubbleRoot.gameObject);
                bubbleRoot = CreateRect(objectName, root);
                bubbleRoot.anchorMin = bubbleRoot.anchorMax = new Vector2(0.5f, 0.5f);
                bubbleRoot.sizeDelta = new Vector2(305f, 83f);
                RectTransform visual = CreateRect("Visual", bubbleRoot);
                Stretch(visual);
                CanvasGroup group = visual.gameObject.AddComponent<CanvasGroup>();
                SpriteNumberView number = visual.gameObject.AddComponent<SpriteNumberView>();
                Image signImage = CreateImage("Sign", visual);
                Image[] digitImages = new Image[4];
                for (int digit = 0; digit < digitImages.Length; digit++)
                    digitImages[digit] = CreateImage($"Digit{digit + 1}", visual);
                ConfigureNumber(number, visual, signImage, digitImages, sign, digits, separation);
                GameplayFeedbackBubbleView bubble =
                    bubbleRoot.gameObject.AddComponent<GameplayFeedbackBubbleView>();
                SerializedObject bubbleData = new SerializedObject(bubble);
                bubbleData.FindProperty("visual").objectReferenceValue = visual;
                bubbleData.FindProperty("canvasGroup").objectReferenceValue = group;
                bubbleData.FindProperty("number").objectReferenceValue = number;
                bubbleData.ApplyModifiedPropertiesWithoutUndo();
                bubbleRoot.gameObject.SetActive(false);
                result[index] = bubble;
            }
            return result;
        }

        private static GameplayMultiplierView[] EnsureMultiplierPool(
            RectTransform root,
            int count)
        {
            var result = new GameplayMultiplierView[count];
            string fontRoot = "Assets/_Project/Sprites/game/multiplier_font/ui_mao_cf_pic_";
            Sprite[] digits = LoadSprites(fontRoot, 0, 10);
            Sprite xSprite = LoadSprite(fontRoot + "11.png");
            Sprite dotSprite = LoadSprite(fontRoot + "12.png");
            for (int index = 0; index < count; index++)
            {
                string objectName = $"Multiplier_{index + 1}";
                RectTransform itemRoot = root.Find(objectName) as RectTransform;
                GameplayMultiplierView existing =
                    itemRoot != null ? itemRoot.GetComponent<GameplayMultiplierView>() : null;
                if (existing != null)
                {
                    result[index] = existing;
                    continue;
                }
                if (itemRoot != null) Object.DestroyImmediate(itemRoot.gameObject);
                itemRoot = CreateRect(objectName, root);
                itemRoot.anchorMin = itemRoot.anchorMax = new Vector2(0.5f, 0.5f);
                itemRoot.sizeDelta = new Vector2(141f, 69f);
                RectTransform visual = CreateRect("Visual", itemRoot);
                Stretch(visual);
                CanvasGroup group = visual.gameObject.AddComponent<CanvasGroup>();
                Image xMark = CreateImage("X", visual);
                Image integer = CreateImage("Integer", visual);
                Image dot = CreateImage("Dot", visual);
                Image decimalDigit = CreateImage("Decimal", visual);
                GameplayMultiplierView view =
                    itemRoot.gameObject.AddComponent<GameplayMultiplierView>();
                SerializedObject data = new SerializedObject(view);
                data.FindProperty("visual").objectReferenceValue = visual;
                data.FindProperty("canvasGroup").objectReferenceValue = group;
                data.FindProperty("xMark").objectReferenceValue = xMark;
                data.FindProperty("integerDigit").objectReferenceValue = integer;
                data.FindProperty("dot").objectReferenceValue = dot;
                data.FindProperty("decimalDigit").objectReferenceValue = decimalDigit;
                data.FindProperty("xSprite").objectReferenceValue = xSprite;
                data.FindProperty("dotSprite").objectReferenceValue = dotSprite;
                SetObjectArray(data.FindProperty("digitSprites"), digits);
                data.FindProperty("separation").floatValue = -6f;
                data.ApplyModifiedPropertiesWithoutUndo();
                itemRoot.gameObject.SetActive(false);
                result[index] = view;
            }
            return result;
        }

        private static GameplayScoreFlightView[] EnsureScoreFlightPool(
            RectTransform root,
            int count)
        {
            var result = new GameplayScoreFlightView[count];
            Sprite trailSprite = LoadSprite(
                "Assets/_Project/Sprites/Effects/trail/et_trail_001.png");
            Sprite pointSprite = LoadSprite(
                "Assets/_Project/Sprites/Effects/glow/et_glow_005.png");
            Sprite burstSprite = LoadSprite(
                "Assets/_Project/Sprites/Effects/glow/et_glow_002.png");
            Sprite starAlpha = LoadSprite(
                "Assets/_Project/Sprites/Effects/star/et_star_1.png");
            Sprite starAdd = LoadSprite(
                "Assets/_Project/Sprites/Effects/star/et_star_003.png");
            for (int index = 0; index < count; index++)
            {
                string objectName = $"ScoreFlight_{index + 1}";
                RectTransform itemRoot = root.Find(objectName) as RectTransform;
                GameplayScoreFlightView existing =
                    itemRoot != null ? itemRoot.GetComponent<GameplayScoreFlightView>() : null;
                if (existing != null)
                {
                    result[index] = existing;
                    continue;
                }
                if (itemRoot != null) Object.DestroyImmediate(itemRoot.gameObject);
                itemRoot = CreateRect(objectName, root);
                Stretch(itemRoot);
                Image[] trail = new Image[12];
                for (int segment = trail.Length - 1; segment >= 0; segment--)
                {
                    trail[segment] = CreateImage($"Trail_{segment + 1}", itemRoot);
                    ConfigureEffectImage(trail[segment], trailSprite, new Vector2(80f, 80f));
                }
                Image point = CreateImage("Point", itemRoot);
                ConfigureEffectImage(point, pointSprite, new Vector2(48f, 48f));
                Image glow = CreateImage("BurstGlow", itemRoot);
                ConfigureEffectImage(glow, burstSprite, new Vector2(180f, 180f));
                Image[] stars = new Image[24];
                for (int star = 0; star < stars.Length; star++)
                {
                    stars[star] = CreateImage($"BurstStar_{star + 1}", itemRoot);
                    ConfigureEffectImage(
                        stars[star],
                        star % 2 == 0 ? starAlpha : starAdd,
                        new Vector2(36f, 36f));
                }
                GameplayScoreFlightView view =
                    itemRoot.gameObject.AddComponent<GameplayScoreFlightView>();
                SerializedObject data = new SerializedObject(view);
                data.FindProperty("point").objectReferenceValue = point;
                SetObjectArray(data.FindProperty("trailSegments"), trail);
                data.FindProperty("burstGlow").objectReferenceValue = glow;
                SetObjectArray(data.FindProperty("burstStars"), stars);
                data.ApplyModifiedPropertiesWithoutUndo();
                itemRoot.gameObject.SetActive(false);
                result[index] = view;
            }
            return result;
        }

        private static void ConfigureEffectImage(
            Image image,
            Sprite sprite,
            Vector2 size)
        {
            image.sprite = sprite;
            image.raycastTarget = false;
            image.preserveAspect = true;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
        }

        private static void ConfigureNumber(
            SpriteNumberView number,
            RectTransform content,
            Image sign,
            Image[] digits,
            Sprite signSprite,
            Sprite[] digitSprites,
            float separation)
        {
            SerializedObject data = new SerializedObject(number);
            data.FindProperty("content").objectReferenceValue = content;
            data.FindProperty("sign").objectReferenceValue = sign;
            SetObjectArray(data.FindProperty("digits"), digits);
            data.FindProperty("signSprite").objectReferenceValue = signSprite;
            SetObjectArray(data.FindProperty("digitSprites"), digitSprites);
            data.FindProperty("separation").floatValue = separation;
            data.ApplyModifiedPropertiesWithoutUndo();
            sign.raycastTarget = false;
            for (int index = 0; index < digits.Length; index++) digits[index].raycastTarget = false;
        }

        private static void WirePresenters(
            GameplayManager manager,
            GameplayFeedbackPresenter presenter,
            GameplayLifeHudPresenter lifeHud)
        {
            if (presenter == null) return;
            SerializedObject serialized = new SerializedObject(manager);
            SerializedProperty property = serialized.FindProperty("gameplayFeedbackPresenter");
            if (property != null) property.objectReferenceValue = presenter;
            SerializedProperty lifeProperty = serialized.FindProperty("gameplayLifeHudPresenter");
            if (lifeProperty != null) lifeProperty.objectReferenceValue = lifeHud;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        }

        private static Text CreateText(
            string name,
            Transform parent,
            Font font,
            int size,
            string value)
        {
            RectTransform rect = CreateRect(name, parent);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.576f, 0.353f, 0.353f, 1f);
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = value;
            return text;
        }

        private static Text EnsureText(
            string name,
            Transform parent,
            Font font,
            int size,
            string value)
        {
            RectTransform rect = EnsureRect(name, parent);
            Text text = rect.GetComponent<Text>();
            if (text == null) text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.576f, 0.353f, 0.353f, 1f);
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = value;
            return text;
        }

        private static Image CreateImage(string name, Transform parent)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            return image;
        }

        private static Image EnsureImage(string name, Transform parent)
        {
            RectTransform rect = EnsureRect(name, parent);
            Image image = rect.GetComponent<Image>();
            if (image == null) image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            return image;
        }

        private static void SetTopLeftRect(
            RectTransform rect,
            float x,
            float y,
            float width,
            float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetCenteredRect(
            RectTransform rect,
            Vector2 center,
            Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = center;
            rect.sizeDelta = size;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.layer = LayerMask.NameToLayer("UI");
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite[] LoadSprites(string prefix, int first, int count)
        {
            var sprites = new Sprite[count];
            for (int index = 0; index < count; index++)
                sprites[index] = LoadSprite($"{prefix}{first + index:00}.png");
            return sprites;
        }

        private static Sprite[] LoadAllSprites(string path)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            var sprites = new List<Sprite>();
            for (int index = 0; index < assets.Length; index++)
            {
                if (assets[index] is Sprite sprite) sprites.Add(sprite);
            }
            sprites.Sort((left, right) =>
                string.CompareOrdinal(left.name, right.name));
            return sprites.ToArray();
        }

        private static Sprite LoadFirstSprite(string path)
        {
            Sprite[] sprites = LoadAllSprites(path);
            return sprites.Length > 0 ? sprites[0] : null;
        }

        private static void SetObjectArray<T>(SerializedProperty property, IReadOnlyList<T> values)
            where T : Object
        {
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index++)
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }
    }
}
