using System;
using System.Collections;
using Meowdoku.Core.Ads;
using Meowdoku.Core.Config;
using Meowdoku.Core.Online;
using Meowdoku.Core.Platform;
using Meowdoku.Core.UI;
using Meowdoku.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Meowdoku.Tests.EditMode
{
    public sealed class AppRuntimeCompositionTests
    {
        private const string RegistryPath =
            "Assets/_Project/Settings/UIRegistry.asset";
        private const string SplashPath =
            "Assets/_Project/Prefabs/UI/SplashPage.prefab";
        private const string HomePath =
            "Assets/_Project/Prefabs/UI/HomePage.prefab";
        private const string TutorialPath =
            "Assets/_Project/Prefabs/UI/TutorialPage.prefab";
        private const string SettingPath =
            "Assets/_Project/Prefabs/UI/SettingsPage.prefab";
        private const string LanguagePath =
            "Assets/_Project/Prefabs/UI/LanguagePage.prefab";
        private const string HowToPlayPath =
            "Assets/_Project/Prefabs/UI/HowToPlayPage.prefab";
        private const string HowToPlayPagedPath =
            "Assets/_Project/Prefabs/UI/HowToPlayPagedPage.prefab";
        private const string BankPath =
            "Assets/_Project/Prefabs/UI/BankPage.prefab";
        private const string GamePath =
            "Assets/_Project/Prefabs/UI/GamePage.prefab";
        private const string CellPath =
            "Assets/_Project/Prefabs/Cell.prefab";
        private const string WinPath =
            "Assets/_Project/Prefabs/UI/WinPage.prefab";
        private const string FailPath =
            "Assets/_Project/Prefabs/UI/FailPage.prefab";
        private const string PrivacyPath =
            "Assets/_Project/Prefabs/UI/PrivacyDialog.prefab";
        private const string PreAttPath =
            "Assets/_Project/Prefabs/UI/PreAttGuidePage.prefab";
        private const string PreAttV2Path =
            "Assets/_Project/Prefabs/UI/PreAttGuidePageV2.prefab";
        private const string PrePushPath =
            "Assets/_Project/Prefabs/UI/PrePushGuidePage.prefab";
        private const string AppScenePath =
            "Assets/_Project/Scenes/AppScene.unity";

        private static readonly PrimaryPageSpec[] PrimaryPages =
        {
            new(UiName.Home, HomePath, typeof(HomePagePresenter)),
            new(UiName.Tutorial, TutorialPath, typeof(TutorialPagePresenter)),
            new(UiName.Setting, SettingPath, typeof(SettingsPagePresenter)),
            new(UiName.Language, LanguagePath, typeof(LanguagePagePresenter)),
            new(UiName.HowToPlay, HowToPlayPath,
                typeof(HowToPlayPagePresenter)),
            new(UiName.HowToPlayPaged, HowToPlayPagedPath,
                typeof(HowToPlayPagedPagePresenter)),
            new(UiName.Bank, BankPath, typeof(BankBrowserPagePresenter)),
            new(UiName.Game, GamePath, typeof(GameplayPagePresenter))
        };

        [Test]
        public void Registry_ContainsStartupAndGamePages()
        {
            UIRegistry registry =
                AssetDatabase.LoadAssetAtPath<UIRegistry>(RegistryPath);
            Assert.That(registry, Is.Not.Null);
            Assert.That(registry.TryGetPrefab(UiName.Splash, out _), Is.True);
            Assert.That(registry.TryGetPrefab(UiName.Game, out _), Is.True);
            Assert.That(registry.TryGetPrefab(UiName.Win, out _), Is.True);
            Assert.That(registry.TryGetPrefab(UiName.Fail, out _), Is.True);
        }

        [Test]
        public void Registry_ContainsEveryPrimaryNavigationPage()
        {
            UIRegistry registry =
                AssetDatabase.LoadAssetAtPath<UIRegistry>(RegistryPath);
            Assert.That(registry, Is.Not.Null);
            Assert.That(registry.ValidateEntries(), Is.Empty);

            foreach (PrimaryPageSpec spec in PrimaryPages)
            {
                Assert.That(
                    registry.TryGetPrefab(spec.Name, out UIFrameWindow window),
                    Is.True,
                    spec.Name + " is missing from UIRegistry.");
                Assert.That(
                    AssetDatabase.GetAssetPath(window),
                    Is.EqualTo(spec.Path),
                    spec.Name + " points to the wrong prefab.");
                Assert.That(
                    window.GetComponent(spec.PresenterType),
                    Is.Not.Null,
                    spec.Path + " is missing " + spec.PresenterType.Name + ".");
                Assert.That(
                    GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(
                        window.gameObject),
                    Is.Zero,
                    spec.Path + " contains a missing script.");
            }
        }

        [Test]
        public void Registry_ContainsEveryPlatformGuidePage()
        {
            UIRegistry registry =
                AssetDatabase.LoadAssetAtPath<UIRegistry>(RegistryPath);
            Assert.That(registry, Is.Not.Null);

            AssertRegistryPage<PrivacyDialogPresenter>(
                registry, UiName.Privacy, PrivacyPath);
            AssertRegistryPage<PreAttGuidePresenter>(
                registry, UiName.PreAttGuide, PreAttPath);
            AssertRegistryPage<PreAttGuidePresenter>(
                registry, UiName.PreAttGuideV2, PreAttV2Path);
            AssertRegistryPage<PrePushGuidePresenter>(
                registry, UiName.PrePushGuide, PrePushPath);
        }

        [Test]
        public void PlatformGuidePrefabs_HaveRequiredBindingsAndHierarchy()
        {
            AssertBindings<PrivacyDialogPresenter>(
                PrivacyPath,
                "popupAnimator",
                "titleText",
                "contentText",
                "acceptText",
                "acceptButton",
                "termsButton",
                "privacyButton",
                "localization");
            AssertBindings<PreAttGuidePresenter>(
                PreAttPath,
                "popupAnimator",
                "titleText",
                "descriptionText",
                "continueText",
                "continueButton",
                "localization");
            AssertBindings<PreAttGuidePresenter>(
                PreAttV2Path,
                "popupAnimator",
                "titleText",
                "descriptionText",
                "continueText",
                "continueButton",
                "guideCloseButton",
                "localization");
            AssertBindings<PrePushGuidePresenter>(
                PrePushPath,
                "popupAnimator",
                "titleText",
                "descriptionText",
                "allowText",
                "allowButton",
                "guideCloseButton",
                "localization");

            GameObject privacy =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrivacyPath);
            GameObject preAtt =
                AssetDatabase.LoadAssetAtPath<GameObject>(PreAttPath);
            GameObject preAttV2 =
                AssetDatabase.LoadAssetAtPath<GameObject>(PreAttV2Path);
            GameObject prePush =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrePushPath);
            Assert.That(privacy.transform.Find("Root/Content/Panel/AcceptButton"),
                Is.Not.Null);
            Assert.That(preAtt.transform.Find("Root/Content/ContinueButton"),
                Is.Not.Null);
            Assert.That(
                preAttV2.transform.Find("Root/Content/Panel/CloseButton"),
                Is.Not.Null);
            Assert.That(prePush.transform.Find("Popup/Cat/Group957Img"),
                Is.Not.Null);
            Assert.That(prePush.transform.Find("Popup/AllowButton"),
                Is.Not.Null);
        }

        [Test]
        public void PrimaryNavigationPresenters_HaveRequiredBindings()
        {
            AssertBindings<HomePagePresenter>(
                HomePath,
                "layoutSpace",
                "startButton",
                "settingsButton",
                "dailyEntry",
                "streakEntry",
                "rankEntry");
            AssertBindings<TutorialPagePresenter>(
                TutorialPath,
                "boardView",
                "layoutSpace",
                "boardInputGroup",
                "cellPrefab",
                "hintButton",
                "confirmButton");
            AssertBindings<SettingsPagePresenter>(
                SettingPath,
                "popupAnimator",
                "musicToggle",
                "soundToggle",
                "vibrationToggle",
                "peopleToggle",
                "languageButton",
                "howToPlayButton");
            GameObject settingsPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(SettingPath);
            LanguageSwitchWidget languageSwitch =
                settingsPrefab.GetComponentInChildren<LanguageSwitchWidget>(true);
            Assert.That(languageSwitch, Is.Not.Null);
            var languageSwitchData = new SerializedObject(languageSwitch);
            SerializedProperty outsideBlocker =
                languageSwitchData.FindProperty("outsideBlocker");
            Assert.That(outsideBlocker, Is.Not.Null);
            Graphic outsideGraphic =
                outsideBlocker.objectReferenceValue as Graphic;
            Assert.That(outsideGraphic, Is.Not.Null,
                "Language outside blocker must be a pointer-down Graphic.");
            Assert.That(outsideGraphic.raycastTarget, Is.True);
            Assert.That(outsideGraphic.GetComponent<Button>(), Is.Null,
                "A Button would close on release instead of source pointer-down.");
            AssertBindings<LanguagePagePresenter>(
                LanguagePath,
                "popupAnimator",
                "scrollRect",
                "confirmButton");
            AssertArraySize<LanguagePagePresenter>(
                LanguagePath,
                "optionViews",
                10);
            AssertBindings<HowToPlayPagePresenter>(
                HowToPlayPath,
                "popupAnimator",
                "tapCatcher");
            AssertArraySize<HowToPlayPagePresenter>(
                HowToPlayPath,
                "boards",
                3);
            AssertBindings<HowToPlayPagedPagePresenter>(
                HowToPlayPagedPath,
                "popupAnimator",
                "caption",
                "backButton",
                "mainButton");
            AssertArraySize<HowToPlayPagedPagePresenter>(
                HowToPlayPagedPath,
                "boards",
                3);
            AssertArraySize<HowToPlayPagedPagePresenter>(
                HowToPlayPagedPath,
                "boardRects",
                3);
            AssertBindings<BankBrowserPagePresenter>(
                BankPath,
                "homeBackButton",
                "rootPanel",
                "regularCard",
                "lkCard",
                "lkModifiedCard",
                "lkStyleCard",
                "gcCard",
                "specialCard");
            AssertBindings<GameplayPagePresenter>(
                GamePath,
                "gameplayManager",
                "backButton",
                "settingsButton",
                "infoButton",
                "returnBankButton",
                "winToast");
        }

        [Test]
        public void GameplayPatternAssets_AreSerializedFromTheSourcePalette()
        {
            AssertBindings<CellView>(CellPath, "patternImage");

            GameObject game =
                AssetDatabase.LoadAssetAtPath<GameObject>(GamePath);
            Assert.That(game, Is.Not.Null, GamePath);
            BoardView board = game.GetComponentInChildren<BoardView>(true);
            Assert.That(board, Is.Not.Null, "GamePage is missing BoardView.");
            var serialized = new SerializedObject(board);
            SerializedProperty icons = serialized.FindProperty("patternIcons");
            Assert.That(icons, Is.Not.Null);
            Assert.That(icons.arraySize, Is.EqualTo(12));
            for (int index = 0; index < icons.arraySize; index++)
            {
                Assert.That(
                    icons.GetArrayElementAtIndex(index).objectReferenceValue,
                    Is.Not.Null,
                    "patternIcons[" + index + "] is not assigned.");
            }
        }

        [Test]
        public void ResultPagePrefabs_HaveSourceBranchesAndPresenters()
        {
            GameObject win =
                AssetDatabase.LoadAssetAtPath<GameObject>(WinPath);
            GameObject fail =
                AssetDatabase.LoadAssetAtPath<GameObject>(FailPath);

            Assert.That(win, Is.Not.Null);
            Assert.That(fail, Is.Not.Null);
            Assert.That(win.GetComponent<GameWinPagePresenter>(), Is.Not.Null);
            Assert.That(fail.GetComponent<GameFailPagePresenter>(), Is.Not.Null);
            Assert.That(win.transform.Find("Root/Visuals"), Is.Not.Null);
            Assert.That(win.transform.Find("Root/Content/Actions"), Is.Not.Null);
            Assert.That(win.transform.Find("Root/PassPanel/Popup/Statistics"),
                Is.Not.Null);
            Assert.That(
                win.transform.Find("Root/PassPanel/Popup/ExtraStatistics"),
                Is.Not.Null);
            Assert.That(win.transform.Find("Root/PassPanel/Actions/Next"),
                Is.Not.Null);
            Assert.That(fail.transform.Find("Root/Visuals"), Is.Not.Null);
            Assert.That(fail.transform.Find("Root/Content/Actions"), Is.Not.Null);
            Assert.That(
                GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(win),
                Is.Zero);
            Assert.That(
                GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(fail),
                Is.Zero);
        }

        [Test]
        public void GeneratedPagePrefabs_HaveRequiredPresenters()
        {
            GameObject splash =
                AssetDatabase.LoadAssetAtPath<GameObject>(SplashPath);
            GameObject game = AssetDatabase.LoadAssetAtPath<GameObject>(GamePath);

            Assert.That(splash, Is.Not.Null);
            Assert.That(game, Is.Not.Null);
            Assert.That(splash.GetComponent<SplashPagePresenter>(), Is.Not.Null);
            Assert.That(game.GetComponent<GameplayPagePresenter>(), Is.Not.Null);
            Assert.That(
                game.GetComponentInChildren<GameplayManager>(true),
                Is.Not.Null);
            Assert.That(game.transform.Find("Overlays/WinToast"), Is.Not.Null);
            Assert.That(
                game.GetComponentInChildren<GameplayWinToastPresenter>(true),
                Is.Not.Null);
            Assert.That(
                GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(splash),
                Is.Zero);
            Assert.That(
                GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(game),
                Is.Zero);
        }

        [Test]
        public void AppScene_HasSerializedBootstrapAndUiManager()
        {
            Scene scene = EditorSceneManager.OpenPreviewScene(AppScenePath);
            try
            {
                AppBootstrap bootstrap = Find<AppBootstrap>(scene);
                UIManager manager = Find<UIManager>(scene);
                AdRuntime adRuntime = Find<AdRuntime>(scene);
                AbConfigRuntime abRuntime = Find<AbConfigRuntime>(scene);
                AuthRuntime authRuntime = Find<AuthRuntime>(scene);
                DataSyncHttpApi dataSyncApi = Find<DataSyncHttpApi>(scene);
                DataSyncRuntime dataSyncRuntime = Find<DataSyncRuntime>(scene);
                PrivacyPermissionRuntime platformRuntime =
                    Find<PrivacyPermissionRuntime>(scene);
                Assert.That(bootstrap, Is.Not.Null);
                Assert.That(manager, Is.Not.Null);
                Assert.That(adRuntime, Is.Not.Null);
                Assert.That(abRuntime, Is.Not.Null);
                Assert.That(authRuntime, Is.Not.Null);
                Assert.That(dataSyncApi, Is.Not.Null);
                Assert.That(dataSyncRuntime, Is.Not.Null);
                Assert.That(platformRuntime, Is.Not.Null);
                Assert.That(platformRuntime.transform.parent.name,
                    Is.EqualTo("App"));
                Assert.That(platformRuntime.transform.name,
                    Is.EqualTo("Systems"));
                var bootstrapData = new SerializedObject(bootstrap);
                Assert.That(
                    bootstrapData.FindProperty("dataSyncRuntime")
                        .objectReferenceValue,
                    Is.SameAs(dataSyncRuntime));
                Assert.That(
                    bootstrapData.FindProperty("platformRuntime")
                        .objectReferenceValue,
                    Is.SameAs(platformRuntime));
                var syncData = new SerializedObject(dataSyncRuntime);
                Assert.That(
                    syncData.FindProperty("authRuntime").objectReferenceValue,
                    Is.SameAs(authRuntime));
                Assert.That(
                    syncData.FindProperty("apiAdapter").objectReferenceValue,
                    Is.SameAs(dataSyncApi));
                var managerData = new SerializedObject(manager);
                Assert.That(
                    managerData.FindProperty("dataSyncRuntime")
                        .objectReferenceValue,
                    Is.SameAs(dataSyncRuntime));
                Assert.That(
                    managerData.FindProperty("platformRuntime")
                        .objectReferenceValue,
                    Is.SameAs(platformRuntime));
                var platformData = new SerializedObject(platformRuntime);
                Assert.That(
                    platformData.FindProperty("uiManager")
                        .objectReferenceValue,
                    Is.SameAs(manager));
                Assert.That(
                    platformData.FindProperty("localization")
                        .objectReferenceValue,
                    Is.Not.Null);
                Assert.That(
                    platformData.FindProperty("abConfigRuntime")
                        .objectReferenceValue,
                    Is.SameAs(abRuntime));
                Assert.That(
                    platformData.FindProperty("trackingRuntime")
                        .objectReferenceValue,
                    Is.Not.Null);
                Assert.That(FindRoot(scene, "App"), Is.Not.Null);
                Assert.That(FindRoot(scene, "EventSystem"), Is.Not.Null);
            }
            finally
            {
                if (scene.IsValid())
                    EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void BuildSettings_StartWithAppScene()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            Assert.That(scenes, Is.Not.Empty);
            Assert.That(scenes[0].path, Is.EqualTo(AppScenePath));
            Assert.That(scenes[0].enabled, Is.True);
        }

        [Test]
        public void BoardPrewarm_BuildsFourCellsPerFrameAndReusesThem()
        {
            var root = new GameObject("BoardRoot", typeof(RectTransform));
            var cellPrefab = new GameObject(
                "CellPrefab",
                typeof(RectTransform),
                typeof(CellView));
            try
            {
                BoardView board = root.AddComponent<BoardView>();
                board.cellPrefab = cellPrefab;
                board.cellsContainer = root.transform;

                IEnumerator prewarm = board.PrewarmCells(4);
                Assert.That(prewarm.MoveNext(), Is.True);
                Assert.That(
                    root.GetComponentsInChildren<CellView>(true).Length,
                    Is.EqualTo(4));
                while (prewarm.MoveNext()) { }
                Assert.That(
                    root.GetComponentsInChildren<CellView>(true).Length,
                    Is.EqualTo(16));

                IEnumerator secondPass = board.PrewarmCells(4);
                Assert.That(secondPass.MoveNext(), Is.False);
                Assert.That(
                    root.GetComponentsInChildren<CellView>(true).Length,
                    Is.EqualTo(16));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cellPrefab);
            }
        }

        private static T Find<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null) return component;
            }
            return null;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == name)
                    return root;
            return null;
        }

        private static void AssertBindings<T>(
            string prefabPath,
            params string[] propertyNames)
            where T : Component
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null, prefabPath);
            T presenter = prefab.GetComponent<T>();
            Assert.That(presenter, Is.Not.Null, typeof(T).Name);
            var serialized = new SerializedObject(presenter);
            foreach (string propertyName in propertyNames)
            {
                SerializedProperty property =
                    serialized.FindProperty(propertyName);
                Assert.That(
                    property,
                    Is.Not.Null,
                    typeof(T).Name + "." + propertyName +
                    " is not serialized.");
                Assert.That(
                    property.objectReferenceValue,
                    Is.Not.Null,
                    typeof(T).Name + "." + propertyName +
                    " is not assigned in " + prefabPath + ".");
            }
        }

        private static void AssertRegistryPage<T>(
            UIRegistry registry,
            UiName name,
            string expectedPath)
            where T : Component
        {
            Assert.That(
                registry.TryGetPrefab(name, out UIFrameWindow window),
                Is.True,
                name + " is missing from UIRegistry.");
            Assert.That(AssetDatabase.GetAssetPath(window),
                Is.EqualTo(expectedPath));
            Assert.That(window.GetComponent<T>(), Is.Not.Null);
            Assert.That(
                GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(
                    window.gameObject),
                Is.Zero);
        }

        private static void AssertArraySize<T>(
            string prefabPath,
            string propertyName,
            int expected)
            where T : Component
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null, prefabPath);
            T presenter = prefab.GetComponent<T>();
            Assert.That(presenter, Is.Not.Null, typeof(T).Name);
            var serialized = new SerializedObject(presenter);
            SerializedProperty property =
                serialized.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null);
            Assert.That(property.isArray, Is.True);
            Assert.That(
                property.arraySize,
                Is.EqualTo(expected),
                typeof(T).Name + "." + propertyName);
            for (int index = 0; index < property.arraySize; index++)
            {
                Assert.That(
                    property.GetArrayElementAtIndex(index).objectReferenceValue,
                    Is.Not.Null,
                    typeof(T).Name + "." + propertyName +
                    "[" + index + "] is not assigned.");
            }
        }

        private readonly struct PrimaryPageSpec
        {
            public PrimaryPageSpec(
                UiName name,
                string path,
                Type presenterType)
            {
                Name = name;
                Path = path;
                PresenterType = presenterType;
            }

            public UiName Name { get; }
            public string Path { get; }
            public Type PresenterType { get; }
        }
    }
}
