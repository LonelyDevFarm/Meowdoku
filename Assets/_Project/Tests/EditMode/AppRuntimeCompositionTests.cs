using System;
using System.Collections;
using Meowdoku.Core.Ads;
using Meowdoku.Core.Config;
using Meowdoku.Core.UI;
using Meowdoku.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        private const string WinPath =
            "Assets/_Project/Prefabs/UI/WinPage.prefab";
        private const string FailPath =
            "Assets/_Project/Prefabs/UI/FailPage.prefab";
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
                Assert.That(bootstrap, Is.Not.Null);
                Assert.That(manager, Is.Not.Null);
                Assert.That(adRuntime, Is.Not.Null);
                Assert.That(abRuntime, Is.Not.Null);
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
