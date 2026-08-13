using System;
using System.Collections.Generic;
using System.IO;
using Meowdoku.Core.Ads;
using Meowdoku.Core.Config;
using Meowdoku.Core.Daily;
using Meowdoku.Core.Localization;
using Meowdoku.Core.Online;
using Meowdoku.Core.Platform;
using Meowdoku.Core.Profile;
using Meowdoku.Core.Rank;
using Meowdoku.Core.Robot;
using Meowdoku.Core.UI;
using Meowdoku.Gameplay;
using Meowdoku.Services;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Meowdoku.Editor
{
    /// <summary>
    /// Builds the runtime page composition through Unity serialization APIs.
    /// GameplayScene remains an untouched standalone test scene; AppScene is
    /// the source-shaped UIManager entry point used for integrated PlayMode.
    /// </summary>
    [InitializeOnLoad]
    internal static class AppRuntimeSceneInstaller
    {
        internal const string GamePagePath =
            "Assets/_Project/Prefabs/UI/GamePage.prefab";
        internal const string AppScenePath =
            "Assets/_Project/Scenes/AppScene.unity";
        private const string GameplayScenePath =
            "Assets/_Project/Scenes/GameplayScene.unity";
        private const string EastAsianFontPath =
            "Assets/_Project/Fonts/NotoSourceHan-subset.ttf";
        private const string InfoSpritePath =
            "Assets/_Project/Sprites/game/btn_info2.png";
        private const string RoundedShaderPath =
            "Assets/_Project/Shaders/UIRoundedRect.shader";
        private const string DailyTimerIconPath =
            "Assets/_Project/Sprites/game/icon_timer.png";
        private const string WinToastBackgroundPath =
            "Assets/_Project/Sprites/game/win_toast/bg.png";
        private static readonly string[] WinToastIconPaths =
        {
            "Assets/_Project/Sprites/game/game_win_toast04/union_86.png",
            "Assets/_Project/Sprites/game/game_win_toast03/union_86.png",
            "Assets/_Project/Sprites/game/game_win_toast02/union_86.png",
            "Assets/_Project/Sprites/game/game_win_toast01/union_86.png"
        };

        static AppRuntimeSceneInstaller()
        {
            EditorApplication.delayCall += InstallIfReady;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                    EditorApplication.delayCall += InstallIfReady;
            };
        }

        [MenuItem("Meowdoku/Port/Install App Runtime Scene")]
        private static void InstallFromMenu()
        {
            InstallIfReady();
            Selection.activeObject =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(AppScenePath);
        }

        [MenuItem("Meowdoku/Port/Rebuild Game Page Prefab")]
        private static void RebuildGamePageFromMenu()
        {
            if (!CanEdit()) return;
            BuildGamePage();
            UIRegistryAssetInstaller.InstallIfReady();
            Selection.activeObject =
                AssetDatabase.LoadAssetAtPath<GameObject>(GamePagePath);
        }

        public static void InstallIfReady()
        {
            if (!CanEdit())
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    EditorApplication.delayCall += InstallIfReady;
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GameplayScenePath) ==
                null)
                return;
            NormalizeAppSceneUiScale();
            if (SplashPagePrefabInstaller.InstallIfReady() == null) return;
            if (!PlatformGuidePrefabInstaller.InstallIfReady()) return;
            if (!ProductServicePrefabInstaller.InstallIfReady()) return;
            GameObject gamePrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(GamePagePath);
            if ((gamePrefab == null ||
                 gamePrefab.transform.Find("Overlays/WinToast") == null) &&
                !BuildGamePage())
                return;
            UpgradeGamePageDailyPresentation();
            UpgradeGamePageSharedAudio();
            UpgradeGamePageLifeEffects();

            UIRegistry registry = UIRegistryAssetInstaller.InstallIfReady();
            LocalizationCatalog localization =
                LocalizationCatalogAssetInstaller.GetOrCreate();
            if (registry == null || localization == null) return;
            // Persist the source-shaped entry point before scene generation.
            // SaveScene can trigger an asset refresh/domain reload, which may
            // interrupt the remainder of this delay callback in the Editor.
            EnsureBuildSettings();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(AppScenePath) == null)
                BuildAppScene(registry, localization);
            UpgradeAppSceneClockTicker();
        }

        private static bool CanEdit()
        {
            return !EditorApplication.isCompiling &&
                   !EditorApplication.isUpdating &&
                   !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        internal static void NormalizeAppSceneUiScale()
        {
            if (!CanEdit()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(AppScenePath) == null)
                return;

            Scene app = SceneManager.GetSceneByPath(AppScenePath);
            bool openedForUpgrade = !app.IsValid() || !app.isLoaded;
            List<Behaviour> suspendedSceneLights = null;
            try
            {
                if (openedForUpgrade)
                {
                    suspendedSceneLights = SuspendLoadedSceneLights();
                    app = EditorSceneManager.OpenScene(
                        AppScenePath,
                        OpenSceneMode.Additive);
                }
                if (!app.IsValid() || !app.isLoaded) return;

                GameObject appRoot = FindRoot(app, "App");
                Transform ui = appRoot != null
                    ? appRoot.transform.Find("UI")
                    : null;
                if (ui == null) return;

                bool changed = NormalizeScale(ui);
                changed |= NormalizeScale(ui.Find("Windows"));
                changed |= NormalizeScale(ui.Find("SharedOverlays"));
                if (!changed) return;

                EditorSceneManager.MarkSceneDirty(app);
                EditorSceneManager.SaveScene(app, AppScenePath);
            }
            finally
            {
                if (openedForUpgrade && app.IsValid() && app.isLoaded)
                    EditorSceneManager.CloseScene(app, true);
                RestoreSceneLights(suspendedSceneLights);
            }
        }

        private static bool BuildGamePage()
        {
            Scene preview = default;
            try
            {
                preview = EditorSceneManager.OpenPreviewScene(GameplayScenePath);
                GameObject canvasObject = FindRoot(preview, "Canvas");
                GameObject systems = FindRoot(preview, "Systems");
                if (canvasObject == null || systems == null) return false;

                Canvas canvas = canvasObject.GetComponent<Canvas>();
                GameplayManager manager =
                    systems.GetComponentInChildren<GameplayManager>(true);
                if (canvas == null || manager == null) return false;
                GameplayPresentationSceneInstaller.ConfigureBoardPatterns(
                    manager.boardView);
                GameplayFeedbackSceneInstaller.ConfigureLifeEffects(
                    canvasObject.transform);

                canvasObject.name = "GamePage";
                systems.transform.SetParent(canvasObject.transform, false);
                CanvasGroup group = canvasObject.GetComponent<CanvasGroup>();
                if (group == null) group = canvasObject.AddComponent<CanvasGroup>();
                GameplayPagePresenter presenter =
                    canvasObject.GetComponent<GameplayPagePresenter>();
                if (presenter == null)
                    presenter = canvasObject.AddComponent<GameplayPagePresenter>();

                Button back = FindButton(canvasObject.transform, "BackBtn");
                Button settings = FindButton(
                    canvasObject.transform,
                    "SettingsBtn");
                Button info = FindButton(canvasObject.transform, "InfoBtn") ??
                              CreateInfoButton(settings);
                Button returnBank =
                    FindButton(canvasObject.transform, "ReturnBankBtn") ??
                    CreateReturnBankButton(canvasObject.transform);
                Transform overlays = canvasObject.transform.Find("Overlays") ??
                                     canvasObject.transform;
                GameplayWinToastPresenter winToast =
                    CreateWinToast(overlays);
                DailyPresentationRefs daily = EnsureDailyPresentation(
                    canvasObject.transform);
                if (back == null || settings == null || info == null ||
                    returnBank == null || winToast == null)
                    return false;

                SerializedObject managerData = new(manager);
                SerializedProperty auto =
                    managerData.FindProperty("startAutomatically");
                if (auto != null) auto.boolValue = false;
                managerData.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject pageData = new(presenter);
                pageData.FindProperty("uiLayer").intValue =
                    (int)UiLayer.Default;
                pageData.FindProperty("isFullscreen").boolValue = true;
                pageData.FindProperty("showMask").boolValue = false;
                pageData.FindProperty("rootCanvas").objectReferenceValue = canvas;
                pageData.FindProperty("rootCanvasGroup").objectReferenceValue =
                    group;
                pageData.FindProperty("gameplayManager").objectReferenceValue =
                    manager;
                pageData.FindProperty("backButton").objectReferenceValue = back;
                pageData.FindProperty("settingsButton").objectReferenceValue =
                    settings;
                pageData.FindProperty("infoButton").objectReferenceValue = info;
                pageData.FindProperty("returnBankButton").objectReferenceValue =
                    returnBank;
                pageData.FindProperty("winToast").objectReferenceValue =
                    winToast;
                SetReference(pageData, "mainLevelDisplay", daily.MainLevel);
                SetReference(pageData, "mainScoreDisplay", daily.MainScore);
                SetReference(pageData, "dailyDateDisplay", daily.DateRoot);
                SetReference(pageData, "dailyDateText", daily.DateText);
                SetReference(pageData, "dailyTimerDisplay", daily.TimerRoot);
                SetReference(pageData, "dailyTimerText", daily.TimerText);
                SetReference(
                    pageData,
                    "localization",
                    LocalizationCatalogAssetInstaller.GetOrCreate());
                pageData.ApplyModifiedPropertiesWithoutUndo();

                EnsureFolder("Assets/_Project/Prefabs", "UI");
                return PrefabUtility.SaveAsPrefabAsset(
                           canvasObject,
                           GamePagePath) != null;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }
            finally
            {
                if (preview.IsValid())
                    EditorSceneManager.ClosePreviewScene(preview);
                AssetDatabase.SaveAssets();
            }
        }

        private static Button CreateInfoButton(Button settings)
        {
            if (settings == null || settings.transform.parent == null)
                return null;
            GameObject clone = UnityEngine.Object.Instantiate(
                settings.gameObject,
                settings.transform.parent);
            clone.name = "InfoBtn";
            RectTransform rect = clone.transform as RectTransform;
            if (rect != null)
            {
                Vector2 position = rect.anchoredPosition;
                position.x = 794f;
                rect.anchoredPosition = position;
            }

            Sprite sprite = LoadSprite(InfoSpritePath);
            if (sprite != null)
            {
                Image[] images = clone.GetComponentsInChildren<Image>(true);
                Image target = clone.GetComponent<Button>().targetGraphic as Image;
                for (int index = images.Length - 1; index >= 0; index--)
                {
                    if (images[index] == target) continue;
                    images[index].sprite = sprite;
                    images[index].preserveAspect = true;
                    break;
                }
            }
            clone.SetActive(false);
            return clone.GetComponent<Button>();
        }

        private static Button CreateReturnBankButton(Transform page)
        {
            var gameObject = new GameObject(
                "ReturnBankBtn",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(page, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(25f, -175f);
            rect.sizeDelta = new Vector2(170f, 56f);
            Image background = gameObject.GetComponent<Image>();
            background.color = new Color(0.945f, 0.576f, 0.125f, 1f);
            Shader rounded = AssetDatabase.LoadAssetAtPath<Shader>(
                RoundedShaderPath);
            if (rounded != null)
                gameObject.AddComponent<RoundedImageView>()
                    .Configure(background, rounded, 18f);

            var labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            var labelRect = (RectTransform)labelObject.transform;
            labelRect.SetParent(rect, false);
            Stretch(labelRect);
            Text label = labelObject.GetComponent<Text>();
            label.font = AssetDatabase.LoadAssetAtPath<Font>(
                EastAsianFontPath);
            label.fontSize = 28;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = "返回题库";
            label.raycastTarget = false;
            gameObject.SetActive(false);
            return gameObject.GetComponent<Button>();
        }

        private static GameplayWinToastPresenter CreateWinToast(Transform parent)
        {
            Transform old = parent.Find("WinToast");
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            Sprite background = LoadSprite(WinToastBackgroundPath);
            Sprite[] icons = new Sprite[WinToastIconPaths.Length];
            for (int i = 0; i < icons.Length; i++)
                icons[i] = LoadSprite(WinToastIconPaths[i]);
            LocalizationCatalog localization =
                LocalizationCatalogAssetInstaller.GetOrCreate();
            Font font = AssetDatabase.LoadAssetAtPath<Font>(EastAsianFontPath);
            if (background == null || localization == null || font == null ||
                Array.Exists(icons, icon => icon == null))
                return null;

            var toastObject = new GameObject(
                "WinToast",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GameplayWinToastPresenter),
                typeof(GenericPopupAnimator));
            RectTransform toast = (RectTransform)toastObject.transform;
            toast.SetParent(parent, false);
            Stretch(toast);
            Canvas toastCanvas = toastObject.GetComponent<Canvas>();
            toastCanvas.overrideSorting = true;
            toastCanvas.sortingOrder = 11;

            RectTransform content = CreateRect("Content", toast);
            SetCentered(content, new Vector2(0f, 21.5f),
                new Vector2(900f, 365f));
            CanvasGroup group = content.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            content.localScale = Vector3.one * 0.7f;

            Image bg = content.gameObject.AddComponent<Image>();
            bg.sprite = background;
            bg.preserveAspect = false;
            bg.raycastTarget = false;

            RectTransform iconRect = CreateRect("TierIcon", content);
            SetCentered(iconRect, new Vector2(0f, 123.5f),
                new Vector2(103f, 87f));
            Image iconImage = iconRect.gameObject.AddComponent<Image>();
            iconImage.sprite = icons[0];
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            RectTransform messageRect = CreateRect("Message", content);
            SetCentered(messageRect, new Vector2(0f, -32.5f),
                new Vector2(720f, 180f));
            Text message = messageRect.gameObject.AddComponent<Text>();
            message.font = font;
            message.fontSize = 60;
            message.resizeTextForBestFit = true;
            message.resizeTextMinSize = 28;
            message.resizeTextMaxSize = 60;
            message.alignment = TextAnchor.MiddleCenter;
            message.color = Color.white;
            message.supportRichText = true;
            message.raycastTarget = false;

            GenericPopupAnimator animator =
                toastObject.GetComponent<GenericPopupAnimator>();
            SerializedObject animatorData = new(animator);
            animatorData.FindProperty("content").objectReferenceValue = content;
            animatorData.FindProperty("contentGroup").objectReferenceValue = group;
            animatorData.ApplyModifiedPropertiesWithoutUndo();

            GameplayWinToastPresenter presenter =
                toastObject.GetComponent<GameplayWinToastPresenter>();
            SerializedObject toastData = new(presenter);
            toastData.FindProperty("content").objectReferenceValue = content;
            toastData.FindProperty("contentGroup").objectReferenceValue = group;
            toastData.FindProperty("tierIcon").objectReferenceValue = iconImage;
            toastData.FindProperty("messageText").objectReferenceValue = message;
            toastData.FindProperty("popupAnimator").objectReferenceValue = animator;
            toastData.FindProperty("localization").objectReferenceValue =
                localization;
            toastData.FindProperty("perfectIcon").objectReferenceValue = icons[0];
            toastData.FindProperty("p5Icon").objectReferenceValue = icons[1];
            toastData.FindProperty("p10Icon").objectReferenceValue = icons[2];
            toastData.FindProperty("p20Icon").objectReferenceValue = icons[3];
            toastData.ApplyModifiedPropertiesWithoutUndo();
            toastObject.SetActive(false);
            return presenter;
        }

        private static void BuildAppScene(
            UIRegistry registry,
            LocalizationCatalog localization)
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene source = default;
            Scene app = default;
            try
            {
                source = EditorSceneManager.OpenPreviewScene(GameplayScenePath);
                app = EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Additive);
                SceneManager.SetActiveScene(app);
                CloneRoot(source, app, "Main Camera");
                CloneRoot(source, app, "Global Light 2D");
                CloneRoot(source, app, "EventSystem");

                var appRoot = new GameObject("App");
                SceneManager.MoveGameObjectToScene(appRoot, app);
                var systems = new GameObject("Systems");
                systems.transform.SetParent(appRoot.transform, false);
                ClockTicker clockTicker = systems.AddComponent<ClockTicker>();
                ProfileRuntime profileRuntime =
                    systems.AddComponent<ProfileRuntime>();
                RobotRuntime robotRuntime =
                    systems.AddComponent<RobotRuntime>();
                Meowdoku.Core.Tracking.TrackingRuntime trackingRuntime =
                    systems.AddComponent<
                        Meowdoku.Core.Tracking.TrackingRuntime>();
                AbConfigRuntime abConfigRuntime =
                    systems.AddComponent<AbConfigRuntime>();
                PrivacyPermissionRuntime platformRuntime =
                    systems.AddComponent<PrivacyPermissionRuntime>();
                ProductServiceRuntime productServiceRuntime =
                    systems.AddComponent<ProductServiceRuntime>();
                AuthRuntime authRuntime =
                    systems.AddComponent<AuthRuntime>();
                AdRuntime adRuntime = systems.AddComponent<AdRuntime>();
                SerializedObject adData = new(adRuntime);
                adData.FindProperty("trackingRuntime").objectReferenceValue =
                    trackingRuntime;
                adData.FindProperty("abConfigRuntime").objectReferenceValue =
                    abConfigRuntime;
                adData.ApplyModifiedPropertiesWithoutUndo();
                DailyMetaRuntime dailyMetaRuntime =
                    systems.AddComponent<DailyMetaRuntime>();
                SerializedObject dailyMetaData = new(dailyMetaRuntime);
                dailyMetaData.FindProperty("clockTicker")
                    .objectReferenceValue = clockTicker;
                dailyMetaData.FindProperty("frameAwardSink")
                    .objectReferenceValue = profileRuntime;
                dailyMetaData.ApplyModifiedPropertiesWithoutUndo();
                RankActivityRuntime rankRuntime =
                    systems.AddComponent<RankActivityRuntime>();
                SerializedObject rankData = new(rankRuntime);
                rankData.FindProperty("clockTicker").objectReferenceValue =
                    clockTicker;
                rankData.FindProperty("robotRuntime").objectReferenceValue =
                    robotRuntime;
                rankData.FindProperty("profileRuntime").objectReferenceValue =
                    profileRuntime;
                rankData.FindProperty("dailyMetaRuntime").objectReferenceValue =
                    dailyMetaRuntime;
                rankData.ApplyModifiedPropertiesWithoutUndo();
                DataSyncHttpApi dataSyncApi =
                    systems.AddComponent<DataSyncHttpApi>();
                SerializedObject dataSyncApiData = new(dataSyncApi);
                dataSyncApiData.FindProperty("authRuntime")
                    .objectReferenceValue = authRuntime;
                dataSyncApiData.ApplyModifiedPropertiesWithoutUndo();
                DataSyncRuntime dataSyncRuntime =
                    systems.AddComponent<DataSyncRuntime>();
                SerializedObject dataSyncData = new(dataSyncRuntime);
                dataSyncData.FindProperty("authRuntime")
                    .objectReferenceValue = authRuntime;
                dataSyncData.FindProperty("apiAdapter")
                    .objectReferenceValue = dataSyncApi;
                dataSyncData.FindProperty("dailyMetaRuntime")
                    .objectReferenceValue = dailyMetaRuntime;
                dataSyncData.FindProperty("profileRuntime")
                    .objectReferenceValue = profileRuntime;
                dataSyncData.FindProperty("rankActivityRuntime")
                    .objectReferenceValue = rankRuntime;
                dataSyncData.ApplyModifiedPropertiesWithoutUndo();

                SoundRuntime soundRuntime = CreateSharedAudio(
                    systems.transform,
                    null);

                var uiObject = new GameObject(
                    "UI",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster),
                    typeof(UIManager));
                RectTransform uiRect = (RectTransform)uiObject.transform;
                uiRect.SetParent(appRoot.transform, false);
                uiRect.localScale = Vector3.one;
                Stretch(uiRect);
                Canvas rootCanvas = uiObject.GetComponent<Canvas>();
                rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = uiObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.screenMatchMode =
                    CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0f;

                RectTransform windows = CreateRect("Windows", uiRect);
                Stretch(windows);
                RectTransform overlays = CreateRect("SharedOverlays", uiRect);
                Stretch(overlays);
                CreateMask(
                    "ModalMask",
                    overlays,
                    out Canvas maskCanvas,
                    out CanvasGroup maskGroup,
                    out _);
                CreateMask(
                    "InputGuard",
                    overlays,
                    out Canvas blockerCanvas,
                    out _,
                    out Image blocker,
                    true);

                UIManager uiManager = uiObject.GetComponent<UIManager>();
                soundRuntime.BindUIManager(uiManager);
                SerializedObject soundRuntimeData = new(soundRuntime);
                soundRuntimeData.FindProperty("uiManager").objectReferenceValue =
                    uiManager;
                soundRuntimeData.ApplyModifiedPropertiesWithoutUndo();
                SerializedObject platformData = new(platformRuntime);
                platformData.FindProperty("uiManager").objectReferenceValue =
                    uiManager;
                platformData.FindProperty("localization")
                    .objectReferenceValue = localization;
                platformData.FindProperty("abConfigRuntime")
                    .objectReferenceValue = abConfigRuntime;
                platformData.FindProperty("trackingRuntime")
                    .objectReferenceValue = trackingRuntime;
                platformData.ApplyModifiedPropertiesWithoutUndo();
                SerializedObject productData = new(productServiceRuntime);
                productData.FindProperty("uiManager").objectReferenceValue =
                    uiManager;
                productData.FindProperty("localization")
                    .objectReferenceValue = localization;
                productData.FindProperty("abConfigRuntime")
                    .objectReferenceValue = abConfigRuntime;
                productData.FindProperty("trackingRuntime")
                    .objectReferenceValue = trackingRuntime;
                productData.ApplyModifiedPropertiesWithoutUndo();
                dailyMetaData.FindProperty("uiManager")
                    .objectReferenceValue = uiManager;
                dailyMetaData.ApplyModifiedPropertiesWithoutUndo();
                SerializedObject managerData = new(uiManager);
                managerData.FindProperty("registry").objectReferenceValue = registry;
                managerData.FindProperty("windowRoot").objectReferenceValue =
                    windows;
                managerData.FindProperty("clockTicker").objectReferenceValue =
                    clockTicker;
                managerData.FindProperty("dailyMetaRuntime")
                    .objectReferenceValue = dailyMetaRuntime;
                managerData.FindProperty("profileRuntime")
                    .objectReferenceValue = profileRuntime;
                managerData.FindProperty("rankActivityRuntime")
                    .objectReferenceValue = rankRuntime;
                managerData.FindProperty("trackingRuntime")
                    .objectReferenceValue = trackingRuntime;
                managerData.FindProperty("adRuntime")
                    .objectReferenceValue = adRuntime;
                managerData.FindProperty("abConfigRuntime")
                    .objectReferenceValue = abConfigRuntime;
                managerData.FindProperty("dataSyncRuntime")
                    .objectReferenceValue = dataSyncRuntime;
                managerData.FindProperty("platformRuntime")
                    .objectReferenceValue = platformRuntime;
                managerData.FindProperty("productServiceRuntime")
                    .objectReferenceValue = productServiceRuntime;
                managerData.FindProperty("maskCanvas").objectReferenceValue =
                    maskCanvas;
                managerData.FindProperty("maskGroup").objectReferenceValue =
                    maskGroup;
                managerData.FindProperty("inputBlocker").objectReferenceValue =
                    blocker;
                managerData.FindProperty("inputBlockerCanvas")
                    .objectReferenceValue = blockerCanvas;
                managerData.ApplyModifiedPropertiesWithoutUndo();

                AppBootstrap bootstrap = systems.AddComponent<AppBootstrap>();
                SerializedObject bootstrapData = new(bootstrap);
                bootstrapData.FindProperty("uiManager").objectReferenceValue =
                    uiManager;
                bootstrapData.FindProperty("localizationCatalog")
                    .objectReferenceValue = localization;
                bootstrapData.FindProperty("abConfigRuntime")
                    .objectReferenceValue = abConfigRuntime;
                bootstrapData.FindProperty("dataSyncRuntime")
                    .objectReferenceValue = dataSyncRuntime;
                bootstrapData.FindProperty("platformRuntime")
                    .objectReferenceValue = platformRuntime;
                bootstrapData.FindProperty("productServiceRuntime")
                    .objectReferenceValue = productServiceRuntime;
                bootstrapData.FindProperty("runOnStart").boolValue = true;
                bootstrapData.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.SaveScene(app, AppScenePath);
            }
            finally
            {
                if (source.IsValid())
                    EditorSceneManager.ClosePreviewScene(source);
                if (app.IsValid() && app.isLoaded)
                    EditorSceneManager.CloseScene(app, true);
                if (previous.IsValid() && previous.isLoaded)
                    SceneManager.SetActiveScene(previous);
                AssetDatabase.SaveAssets();
            }
        }

        private static void UpgradeAppSceneClockTicker()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(AppScenePath) == null)
                return;

            Scene app = SceneManager.GetSceneByPath(AppScenePath);
            bool openedForUpgrade = !app.IsValid() || !app.isLoaded;
            List<Behaviour> suspendedSceneLights = null;
            try
            {
                if (openedForUpgrade)
                {
                    suspendedSceneLights = SuspendLoadedSceneLights();
                    app = EditorSceneManager.OpenScene(
                        AppScenePath,
                        OpenSceneMode.Additive);
                }
                if (!app.IsValid() || !app.isLoaded) return;

                GameObject appRoot = FindRoot(app, "App");
                Transform systems = appRoot != null
                    ? appRoot.transform.Find("Systems")
                    : null;
                UIManager manager = appRoot != null
                    ? appRoot.GetComponentInChildren<UIManager>(true)
                    : null;
                if (systems == null || manager == null) return;

                bool changed = false;
                changed |= NormalizeScale(manager.transform);
                Transform windows = manager.transform.Find("Windows");
                changed |= NormalizeScale(windows);
                Transform overlays = manager.transform.Find("SharedOverlays");
                changed |= NormalizeScale(overlays);
                if (overlays != null)
                {
                    changed |= NormalizeScale(overlays.Find("ModalMask"));
                    changed |= NormalizeScale(overlays.Find("InputGuard"));
                }
                ClockTicker ticker = systems.GetComponent<ClockTicker>();
                if (ticker == null)
                {
                    ticker = systems.gameObject.AddComponent<ClockTicker>();
                    changed = true;
                }

                ProfileRuntime profileRuntime =
                    systems.GetComponent<ProfileRuntime>();
                if (profileRuntime == null)
                {
                    profileRuntime = systems.gameObject
                        .AddComponent<ProfileRuntime>();
                    changed = true;
                }

                RobotRuntime robotRuntime =
                    systems.GetComponent<RobotRuntime>();
                if (robotRuntime == null)
                {
                    systems.gameObject.AddComponent<RobotRuntime>();
                    changed = true;
                }

                Meowdoku.Core.Tracking.TrackingRuntime trackingRuntime =
                    systems.GetComponent<
                        Meowdoku.Core.Tracking.TrackingRuntime>();
                if (trackingRuntime == null)
                {
                    trackingRuntime = systems.gameObject.AddComponent<
                        Meowdoku.Core.Tracking.TrackingRuntime>();
                    changed = true;
                }

                AbConfigRuntime abConfigRuntime =
                    systems.GetComponent<AbConfigRuntime>();
                if (abConfigRuntime == null)
                {
                    abConfigRuntime = systems.gameObject
                        .AddComponent<AbConfigRuntime>();
                    changed = true;
                }

                PrivacyPermissionRuntime platformRuntime =
                    systems.GetComponent<PrivacyPermissionRuntime>();
                if (platformRuntime == null)
                {
                    platformRuntime = systems.gameObject
                        .AddComponent<PrivacyPermissionRuntime>();
                    changed = true;
                }

                ProductServiceRuntime productServiceRuntime =
                    systems.GetComponent<ProductServiceRuntime>();
                if (productServiceRuntime == null)
                {
                    productServiceRuntime = systems.gameObject
                        .AddComponent<ProductServiceRuntime>();
                    changed = true;
                }

                AuthRuntime authRuntime =
                    systems.GetComponent<AuthRuntime>();
                if (authRuntime == null)
                {
                    authRuntime = systems.gameObject
                        .AddComponent<AuthRuntime>();
                    changed = true;
                }

                AdRuntime adRuntime = systems.GetComponent<AdRuntime>();
                if (adRuntime == null)
                {
                    adRuntime = systems.gameObject.AddComponent<AdRuntime>();
                    changed = true;
                }

                SoundRuntime soundRuntime =
                    systems.GetComponentInChildren<SoundRuntime>(true);
                if (soundRuntime == null)
                {
                    soundRuntime = CreateSharedAudio(systems, manager);
                    changed = soundRuntime != null;
                }
                else
                {
                    SerializedObject soundRuntimeData = new(soundRuntime);
                    changed |= SetReference(
                        soundRuntimeData,
                        "uiManager",
                        manager);
                    changed |= SetReference(
                        soundRuntimeData,
                        "soundService",
                        soundRuntime.GetComponent<SoundService>());
                    soundRuntimeData.ApplyModifiedPropertiesWithoutUndo();
                }
                SerializedObject adData = new(adRuntime);
                changed |= SetReference(
                    adData,
                    "trackingRuntime",
                    trackingRuntime);
                changed |= SetReference(
                    adData,
                    "abConfigRuntime",
                    abConfigRuntime);
                adData.ApplyModifiedPropertiesWithoutUndo();

                DailyMetaRuntime dailyMeta =
                    systems.GetComponent<DailyMetaRuntime>();
                if (dailyMeta == null)
                {
                    dailyMeta = systems.gameObject
                        .AddComponent<DailyMetaRuntime>();
                    changed = true;
                }

                RankActivityRuntime rankRuntime =
                    systems.GetComponent<RankActivityRuntime>();
                if (rankRuntime == null)
                {
                    rankRuntime = systems.gameObject
                        .AddComponent<RankActivityRuntime>();
                    changed = true;
                }

                SerializedObject rankData = new(rankRuntime);
                changed |= SetReference(rankData, "clockTicker", ticker);
                changed |= SetReference(
                    rankData,
                    "robotRuntime",
                    robotRuntime);
                changed |= SetReference(
                    rankData,
                    "profileRuntime",
                    profileRuntime);
                changed |= SetReference(
                    rankData,
                    "dailyMetaRuntime",
                    dailyMeta);
                rankData.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject dailyMetaData = new(dailyMeta);
                SerializedProperty dailyClockProperty =
                    dailyMetaData.FindProperty("clockTicker");
                if (dailyClockProperty != null &&
                    dailyClockProperty.objectReferenceValue != ticker)
                {
                    dailyClockProperty.objectReferenceValue = ticker;
                    dailyMetaData.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }

                SerializedProperty frameSinkProperty =
                    dailyMetaData.FindProperty("frameAwardSink");
                if (frameSinkProperty != null &&
                    frameSinkProperty.objectReferenceValue != profileRuntime)
                {
                    frameSinkProperty.objectReferenceValue = profileRuntime;
                    dailyMetaData.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }

                DataSyncHttpApi dataSyncApi =
                    systems.GetComponent<DataSyncHttpApi>();
                if (dataSyncApi == null)
                {
                    dataSyncApi = systems.gameObject
                        .AddComponent<DataSyncHttpApi>();
                    changed = true;
                }
                SerializedObject dataSyncApiData = new(dataSyncApi);
                changed |= SetReference(
                    dataSyncApiData,
                    "authRuntime",
                    authRuntime);
                dataSyncApiData.ApplyModifiedPropertiesWithoutUndo();

                DataSyncRuntime dataSyncRuntime =
                    systems.GetComponent<DataSyncRuntime>();
                if (dataSyncRuntime == null)
                {
                    dataSyncRuntime = systems.gameObject
                        .AddComponent<DataSyncRuntime>();
                    changed = true;
                }
                SerializedObject dataSyncData = new(dataSyncRuntime);
                changed |= SetReference(
                    dataSyncData,
                    "authRuntime",
                    authRuntime);
                changed |= SetReference(
                    dataSyncData,
                    "apiAdapter",
                    dataSyncApi);
                changed |= SetReference(
                    dataSyncData,
                    "dailyMetaRuntime",
                    dailyMeta);
                changed |= SetReference(
                    dataSyncData,
                    "profileRuntime",
                    profileRuntime);
                changed |= SetReference(
                    dataSyncData,
                    "rankActivityRuntime",
                    rankRuntime);
                dataSyncData.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject platformData = new(platformRuntime);
                changed |= SetReference(
                    platformData,
                    "uiManager",
                    manager);
                changed |= SetReference(
                    platformData,
                    "localization",
                    LocalizationCatalogAssetInstaller.GetOrCreate());
                changed |= SetReference(
                    platformData,
                    "abConfigRuntime",
                    abConfigRuntime);
                changed |= SetReference(
                    platformData,
                    "trackingRuntime",
                    trackingRuntime);
                platformData.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject productData = new(productServiceRuntime);
                changed |= SetReference(productData, "uiManager", manager);
                changed |= SetReference(
                    productData,
                    "localization",
                    LocalizationCatalogAssetInstaller.GetOrCreate());
                changed |= SetReference(
                    productData,
                    "abConfigRuntime",
                    abConfigRuntime);
                changed |= SetReference(
                    productData,
                    "trackingRuntime",
                    trackingRuntime);
                productData.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject managerData = new(manager);
                SerializedProperty tickerProperty =
                    managerData.FindProperty("clockTicker");
                if (tickerProperty != null &&
                    tickerProperty.objectReferenceValue != ticker)
                {
                    tickerProperty.objectReferenceValue = ticker;
                    managerData.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }

                SerializedProperty dailyMetaProperty =
                    managerData.FindProperty("dailyMetaRuntime");
                if (dailyMetaProperty != null &&
                    dailyMetaProperty.objectReferenceValue != dailyMeta)
                {
                    dailyMetaProperty.objectReferenceValue = dailyMeta;
                    managerData.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }

                SerializedProperty profileProperty =
                    managerData.FindProperty("profileRuntime");
                if (profileProperty != null &&
                    profileProperty.objectReferenceValue != profileRuntime)
                {
                    profileProperty.objectReferenceValue = profileRuntime;
                    managerData.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }

                SerializedProperty rankProperty =
                    managerData.FindProperty("rankActivityRuntime");
                if (rankProperty != null &&
                    rankProperty.objectReferenceValue != rankRuntime)
                {
                    rankProperty.objectReferenceValue = rankRuntime;
                    managerData.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }

                SerializedProperty trackingProperty =
                    managerData.FindProperty("trackingRuntime");
                if (trackingProperty != null &&
                    trackingProperty.objectReferenceValue != trackingRuntime)
                {
                    trackingProperty.objectReferenceValue = trackingRuntime;
                    managerData.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }

                changed |= SetReference(
                    managerData,
                    "adRuntime",
                    adRuntime);
                changed |= SetReference(
                    managerData,
                    "abConfigRuntime",
                    abConfigRuntime);
                changed |= SetReference(
                    managerData,
                    "dataSyncRuntime",
                    dataSyncRuntime);
                changed |= SetReference(
                    managerData,
                    "platformRuntime",
                    platformRuntime);
                changed |= SetReference(
                    managerData,
                    "productServiceRuntime",
                    productServiceRuntime);
                managerData.ApplyModifiedPropertiesWithoutUndo();

                AppBootstrap bootstrap =
                    systems.GetComponent<AppBootstrap>();
                if (bootstrap == null)
                {
                    bootstrap = systems.gameObject.AddComponent<AppBootstrap>();
                    changed = true;
                }
                SerializedObject bootstrapData = new(bootstrap);
                changed |= SetReference(
                    bootstrapData,
                    "uiManager",
                    manager);
                changed |= SetReference(
                    bootstrapData,
                    "abConfigRuntime",
                    abConfigRuntime);
                changed |= SetReference(
                    bootstrapData,
                    "dataSyncRuntime",
                    dataSyncRuntime);
                changed |= SetReference(
                    bootstrapData,
                    "platformRuntime",
                    platformRuntime);
                changed |= SetReference(
                    bootstrapData,
                    "productServiceRuntime",
                    productServiceRuntime);
                bootstrapData.ApplyModifiedPropertiesWithoutUndo();

                SerializedProperty dailyUiProperty =
                    dailyMetaData.FindProperty("uiManager");
                if (dailyUiProperty != null &&
                    dailyUiProperty.objectReferenceValue != manager)
                {
                    dailyUiProperty.objectReferenceValue = manager;
                    dailyMetaData.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }

                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(app);
                    EditorSceneManager.SaveScene(app, AppScenePath);
                }
            }
            finally
            {
                if (openedForUpgrade && app.IsValid() && app.isLoaded)
                    EditorSceneManager.CloseScene(app, true);
                RestoreSceneLights(suspendedSceneLights);
            }
        }

        private static List<Behaviour> SuspendLoadedSceneLights()
        {
            Type lightType = Type.GetType(
                "UnityEngine.Rendering.Universal.Light2D, " +
                "Unity.RenderPipelines.Universal.2D.Runtime");
            if (lightType == null) return null;
            UnityEngine.Object[] lights =
                Resources.FindObjectsOfTypeAll(lightType);
            var suspended = new List<Behaviour>();
            for (int index = 0; index < lights.Length; index++)
            {
                if (lights[index] is not Behaviour behaviour ||
                    !behaviour.enabled ||
                    !behaviour.gameObject.scene.IsValid() ||
                    !behaviour.gameObject.scene.isLoaded)
                    continue;
                behaviour.enabled = false;
                suspended.Add(behaviour);
            }
            return suspended;
        }

        private static void RestoreSceneLights(
            IReadOnlyList<Behaviour> suspended)
        {
            if (suspended == null) return;
            for (int index = 0; index < suspended.Count; index++)
            {
                Behaviour behaviour = suspended[index];
                if (behaviour != null &&
                    behaviour.gameObject.scene.IsValid() &&
                    behaviour.gameObject.scene.isLoaded)
                    behaviour.enabled = true;
            }
        }

        private static SoundRuntime CreateSharedAudio(
            Transform systems,
            UIManager manager)
        {
            if (systems == null) return null;
            SoundCatalog catalog =
                GameplayAudioSceneInstaller.GetOrCreateCatalog();
            if (catalog == null) return null;

            Transform audio = systems.Find("Audio");
            if (audio == null)
            {
                var audioObject = new GameObject("Audio");
                audio = audioObject.transform;
                audio.SetParent(systems, false);
            }

            SoundService service = audio.GetComponent<SoundService>();
            if (service == null)
                service = audio.gameObject.AddComponent<SoundService>();
            SoundRuntime runtime = audio.GetComponent<SoundRuntime>();
            if (runtime == null)
                runtime = audio.gameObject.AddComponent<SoundRuntime>();

            Transform bgmTransform = audio.Find("Bgm");
            if (bgmTransform == null)
            {
                var bgmObject = new GameObject("Bgm");
                bgmTransform = bgmObject.transform;
                bgmTransform.SetParent(audio, false);
            }
            AudioSource bgm = bgmTransform.GetComponent<AudioSource>();
            if (bgm == null) bgm = bgmTransform.gameObject.AddComponent<AudioSource>();
            bgm.playOnAwake = false;
            bgm.loop = true;

            SerializedObject serviceData = new(service);
            SetReference(serviceData, "catalog", catalog);
            SetReference(serviceData, "bgmSource", bgm);
            serviceData.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject runtimeData = new(runtime);
            SetReference(runtimeData, "uiManager", manager);
            SetReference(runtimeData, "soundService", service);
            runtimeData.ApplyModifiedPropertiesWithoutUndo();
            return runtime;
        }

        private static void UpgradeGamePageSharedAudio()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(GamePagePath);
            bool changed = false;
            try
            {
                SoundService[] embedded =
                    root.GetComponentsInChildren<SoundService>(true);
                for (int index = embedded.Length - 1; index >= 0; index--)
                {
                    SoundService service = embedded[index];
                    if (service == null) continue;
                    UnityEngine.Object.DestroyImmediate(service.gameObject);
                    changed = true;
                }

                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(root, GamePagePath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void UpgradeGamePageLifeEffects()
        {
            if (!File.Exists(GamePagePath)) return;
            GameObject root = PrefabUtility.LoadPrefabContents(GamePagePath);
            try
            {
                if (!GameplayFeedbackSceneInstaller.ConfigureLifeEffects(root.transform))
                    return;
                PrefabUtility.SaveAsPrefabAsset(root, GamePagePath);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void CreateMask(
            string name,
            Transform parent,
            out Canvas canvas,
            out CanvasGroup group,
            out Image image,
            bool transparent = false)
        {
            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(GraphicRaycaster),
                typeof(Image));
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            Stretch(rect);
            canvas = gameObject.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            group = gameObject.GetComponent<CanvasGroup>();
            image = gameObject.GetComponent<Image>();
            image.color = transparent
                ? new Color(0f, 0f, 0f, 0f)
                : Color.black;
            image.raycastTarget = true;
        }

        private static void UpgradeGamePageDailyPresentation()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(GamePagePath);
            try
            {
                GameplayPagePresenter presenter =
                    root.GetComponent<GameplayPagePresenter>();
                if (presenter == null) return;
                GameplayManager gameplay =
                    root.GetComponentInChildren<GameplayManager>(true);
                bool patternChanged =
                    GameplayPresentationSceneInstaller.ConfigureBoardPatterns(
                        gameplay != null ? gameplay.boardView : null);
                bool toolBarChanged =
                    GameplayPresentationSceneInstaller.ConfigureToolBar(
                        root.transform,
                        gameplay);
                DailyPresentationRefs daily = EnsureDailyPresentation(
                    root.transform);
                if (daily.DateRoot == null || daily.TimerRoot == null)
                {
                    if (patternChanged || toolBarChanged)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, GamePagePath);
                        AssetDatabase.SaveAssets();
                    }
                    return;
                }

                SerializedObject data = new(presenter);
                bool changed = daily.Created || patternChanged || toolBarChanged;
                changed |= SetReference(data, "mainLevelDisplay", daily.MainLevel);
                changed |= SetReference(data, "mainScoreDisplay", daily.MainScore);
                changed |= SetReference(data, "dailyDateDisplay", daily.DateRoot);
                changed |= SetReference(data, "dailyDateText", daily.DateText);
                changed |= SetReference(data, "dailyTimerDisplay", daily.TimerRoot);
                changed |= SetReference(data, "dailyTimerText", daily.TimerText);
                changed |= SetReference(
                    data,
                    "localization",
                    LocalizationCatalogAssetInstaller.GetOrCreate());
                if (!changed) return;
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, GamePagePath);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        internal static void UpgradeGamePagePatternAssets()
        {
            if (!CanEdit() || !File.Exists(GamePagePath)) return;
            GameObject root = PrefabUtility.LoadPrefabContents(GamePagePath);
            try
            {
                GameplayManager gameplay =
                    root.GetComponentInChildren<GameplayManager>(true);
                if (!GameplayPresentationSceneInstaller.ConfigureBoardPatterns(
                        gameplay != null ? gameplay.boardView : null))
                    return;
                PrefabUtility.SaveAsPrefabAsset(root, GamePagePath);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static DailyPresentationRefs EnsureDailyPresentation(
            Transform root)
        {
            var result = new DailyPresentationRefs();
            RectTransform header = root.Find("HUD/Header") as RectTransform;
            RectTransform row = root.Find("HUD/CatHeartRow") as RectTransform;
            if (header == null || row == null) return result;

            result.MainLevel = header.Find("LevelDisplay")?.gameObject;
            result.MainScore = header.Find("ScoreDisplay")?.gameObject;
            Font font = AssetDatabase.LoadAssetAtPath<Font>(
                "Assets/_Project/Fonts/Roboto.ttf");

            RectTransform dateRoot =
                header.Find("DailyDateDisplay") as RectTransform;
            if (dateRoot == null)
            {
                dateRoot = CreateRect("DailyDateDisplay", header);
                result.Created = true;
            }
            dateRoot.anchorMin = dateRoot.anchorMax = new Vector2(0.5f, 1f);
            dateRoot.pivot = new Vector2(0.5f, 1f);
            dateRoot.anchoredPosition = new Vector2(0f, -18f);
            dateRoot.sizeDelta = new Vector2(660f, 82f);
            Text date = EnsureText("DateLabel", dateRoot, font, 56, "Dec 26");
            Stretch(date.rectTransform);
            date.color = new Color(0.576f, 0.353f, 0.353f, 1f);
            date.alignment = TextAnchor.MiddleCenter;

            RectTransform timerRoot =
                row.Find("DailyTimeDisplay") as RectTransform;
            if (timerRoot == null)
            {
                timerRoot = CreateRect("DailyTimeDisplay", row);
                result.Created = true;
            }
            SetCentered(timerRoot, new Vector2(310f, 0f), new Vector2(260f, 84f));
            Image background = EnsureImage("Background", timerRoot);
            Stretch(background.rectTransform);
            background.color = new Color(1f, 1f, 1f, 0.92f);
            background.raycastTarget = false;
            Image icon = EnsureImage("TimerIcon", timerRoot);
            SetCentered(icon.rectTransform,
                new Vector2(-82f, 0f), new Vector2(58f, 66f));
            icon.sprite = LoadSprite(DailyTimerIconPath);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            Text timer = EnsureText("TimerLabel", timerRoot, font, 50, "00:00");
            SetCentered(timer.rectTransform,
                new Vector2(38f, 0f), new Vector2(150f, 72f));
            timer.color = new Color(0.576f, 0.353f, 0.353f, 1f);
            timer.alignment = TextAnchor.MiddleCenter;

            if (result.Created)
            {
                dateRoot.gameObject.SetActive(false);
                timerRoot.gameObject.SetActive(false);
            }
            result.DateRoot = dateRoot.gameObject;
            result.DateText = date;
            result.TimerRoot = timerRoot.gameObject;
            result.TimerText = timer;
            return result;
        }

        private static Text EnsureText(
            string name,
            Transform parent,
            Font font,
            int size,
            string value)
        {
            Transform existing = parent.Find(name);
            Text text = existing != null ? existing.GetComponent<Text>() : null;
            if (text == null)
            {
                RectTransform rect = existing as RectTransform ??
                                     CreateRect(name, parent);
                text = rect.gameObject.AddComponent<Text>();
            }
            text.font = font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.text = value;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Image EnsureImage(string name, Transform parent)
        {
            Transform existing = parent.Find(name);
            Image image = existing != null ? existing.GetComponent<Image>() : null;
            if (image != null) return image;
            RectTransform rect = existing as RectTransform ??
                                 CreateRect(name, parent);
            return rect.gameObject.AddComponent<Image>();
        }

        private static bool SetReference(
            SerializedObject data,
            string name,
            UnityEngine.Object value)
        {
            SerializedProperty property = data.FindProperty(name);
            if (property == null || property.objectReferenceValue == value)
                return false;
            property.objectReferenceValue = value;
            return true;
        }

        private sealed class DailyPresentationRefs
        {
            public GameObject MainLevel;
            public GameObject MainScore;
            public GameObject DateRoot;
            public Text DateText;
            public GameObject TimerRoot;
            public Text TimerText;
            public bool Created;
        }

        private static void CloneRoot(Scene source, Scene target, string name)
        {
            GameObject original = FindRoot(source, name);
            if (original == null) return;
            GameObject clone = UnityEngine.Object.Instantiate(original);
            clone.name = name;
            SceneManager.MoveGameObjectToScene(clone, target);
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            if (!scene.IsValid()) return null;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == name)
                    return root;
            return null;
        }

        private static Button FindButton(Transform root, string name)
        {
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
                if (button.name == name)
                    return button;
            return null;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        private static bool NormalizeScale(Transform value)
        {
            if (value == null || value.localScale == Vector3.one) return false;
            value.localScale = Vector3.one;
            return true;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
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

        private static Sprite LoadSprite(string path)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (UnityEngine.Object asset in assets)
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

        private static void EnsureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(AppScenePath, true)
            };
        }
    }
}
