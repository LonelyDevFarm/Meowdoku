using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Meowdoku.Core;
using Meowdoku.Core.Ads;
using Meowdoku.Core.Config;
using Meowdoku.Core.Daily;
using Meowdoku.Core.Localization;
using Meowdoku.Core.Platform;
using Meowdoku.Core.Profile;
using Meowdoku.Core.Rank;
using Meowdoku.Core.Robot;
using Meowdoku.Core.Tracking;
using Meowdoku.Core.Tutorial;
using Meowdoku.Core.UI;
using Meowdoku.Gameplay;
using Meowdoku.Services;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Meowdoku.Tests.PlayMode
{
    public sealed class PrimaryNavigationPlayModeTests
    {
        private const string AppScenePath =
            "Assets/_Project/Scenes/AppScene.unity";
        private const float StartupTimeoutSeconds = 12f;
        private const float TransitionTimeoutSeconds = 4f;

        private enum BankUiFlow
        {
            SizeThenTier,
            LevelRows
        }

        private sealed class RecordingSettingsExternalServices :
            ISettingsExternalServices
        {
            public bool IsOnline { get; set; }
            public bool IsConsentManagementRequired { get; set; }
            public int FeedbackOpenCount { get; private set; }
            public int ConsentOpenCount { get; private set; }
            public List<string> OpenedUrls { get; } = new();

            public void OpenFeedbackFaq()
            {
                FeedbackOpenCount++;
            }

            public void ShowConsentManagement()
            {
                ConsentOpenCount++;
            }

            public void OpenLocalizedPrivacyUrl(string defaultUrl)
            {
                OpenedUrls.Add(defaultUrl);
            }
        }

        private sealed class MutableSystemClock : ISystemClock
        {
            public DateTime LocalNow { get; set; }
            public double UnixSeconds { get; set; }
        }

        private sealed class MutableCurrentDate : ICurrentDateProvider
        {
            public MutableCurrentDate(string value)
            {
                CurrentDate = value;
            }

            public string CurrentDate { get; set; }
        }

        private sealed class MutableRobotTime : IRobotTimeProvider
        {
            public long UnixNow { get; set; }
        }

        private sealed class RankEnvironment : IRankActivityEnvironment
        {
            public bool LeaderboardEnabled { get; set; } = true;
            public int LeaderboardGroup { get; set; } =
                RankActivityConfig.GroupCats;
            public int CurrentLevel { get; set; } =
                RankActivityConfig.UnlockLevel;
        }

        private sealed class MemoryRankActivityStore : IRankActivityStore
        {
            public RankActivityData Current { get; private set; } = new();

            public RankActivityData Load() => Current;

            public bool Save(RankActivityData data)
            {
                Current = data;
                return true;
            }

            public void Reset()
            {
                Current = new RankActivityData();
            }
        }

        private sealed class MemoryRobotPoolStore : IRobotPoolStore
        {
            private readonly Dictionary<string, RobotPool> _pools = new();

            public IReadOnlyDictionary<string, RobotPool> LoadAll() => _pools;

            public bool SaveAll(IReadOnlyDictionary<string, RobotPool> pools)
            {
                _pools.Clear();
                foreach (KeyValuePair<string, RobotPool> pair in pools)
                    _pools[pair.Key] = pair.Value;
                return true;
            }

            public void Reset()
            {
                _pools.Clear();
            }

            public void ZeroAllScores()
            {
                foreach (RobotPool pool in _pools.Values)
                {
                    for (int index = 0; index < pool.Robots.Count; index++)
                    {
                        RobotData robot = pool.Robots[index];
                        robot.FinalScore = 0;
                        robot.Timeline.Clear();
                    }
                }
            }
        }

        private sealed class MemoryProfileDataStore : IProfileDataStore
        {
            private ProfileData _data;

            public MemoryProfileDataStore(ProfileData data = null)
            {
                _data = data ?? new ProfileData();
            }

            public ProfileData Load() => _data;

            public bool Save(ProfileData data)
            {
                _data = data;
                return true;
            }

            public void Reset()
            {
                _data = new ProfileData();
            }
        }

        private IDisposable _stateOverride;
        private string _stateDirectory;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            LevelBankIO.LoadOverride = null;
            BankData.ResetForTests();
            GameStateService service;
            if (TestContext.CurrentContext.Test.Name.Contains(
                    nameof(PlatformStartup_CorruptPlayerSlotsUseDefaultsAndExitSplash)))
            {
                _stateDirectory = Path.Combine(
                    Path.GetTempPath(),
                    "MeowdokuStartupFallbackTests",
                    Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(_stateDirectory);
                var repository = new GameStateRepository(_stateDirectory);
                Assert.That(repository.SavePlayer(new GameStateData
                {
                    CurrentLevel = 40,
                    TutorialDone = true
                }), Is.True);
                Assert.That(repository.SavePlayer(new GameStateData
                {
                    CurrentLevel = 41,
                    TutorialDone = true
                }), Is.True);
                string saveDirectory = Path.Combine(
                    _stateDirectory,
                    "save_store");
                File.WriteAllText(
                    Path.Combine(saveDirectory, "save_a.cfg"),
                    "corrupt-slot-a");
                File.WriteAllText(
                    Path.Combine(saveDirectory, "save_b.cfg"),
                    "corrupt-slot-b");
                service = new GameStateService(
                    repository.Load(),
                    repository,
                    null,
                    repository,
                    Application.version);
            }
            else
            {
                bool tutorialFlow = TestContext.CurrentContext.Test.Name.Contains(
                    nameof(PlatformTutorial_FullFlowRoutesGameAndReopensCleanly));
                bool coldLocaleFlow = TestContext.CurrentContext.Test.Name.Contains(
                    nameof(PlatformLocalization_ColdStartAppliesPersistedLocale));
                var data = new GameStateData
                {
                    CurrentLevel = 1,
                    IsFirstSession = false,
                    TutorialDone = !tutorialFlow,
                    LastSplashDate = DateTime.Now.ToString("yyyy-MM-dd"),
                    MaxDailyDate = DateTime.Now.ToString("yyyy-MM-dd"),
                    AppliedLocale = coldLocaleFlow ? "vi" : string.Empty
                };
                if (TestContext.CurrentContext.Test.Name.Contains(
                        nameof(PlatformLevelSelection_CrossLevelDuplicateRetriesOnceThenAcceptsFallback)))
                    ConfigureRecentDuplicateFixture(data);
                service = new GameStateService(data);
            }
            _stateOverride = GameStateRuntime.OverrideForTests(service);

            UnityEngine.Events.UnityAction<Scene, LoadSceneMode>
                configureColdLocale = null;
            if (TestContext.CurrentContext.Test.Name.Contains(
                    nameof(PlatformLocalization_ColdStartAppliesPersistedLocale)))
            {
                configureColdLocale = (scene, _) =>
                {
                    if (!string.Equals(scene.path, AppScenePath,
                            StringComparison.Ordinal))
                        return;
                    AbConfigRuntime runtime = Find<AbConfigRuntime>();
                    Assert.That(runtime, Is.Not.Null);
                    PlayModeAbProvider provider =
                        runtime.gameObject.AddComponent<PlayModeAbProvider>();
                    provider.SetInt(
                        "settings_language",
                        SettingsLanguageConfig.ValuePopup);
                    runtime.BindProvider(provider);
                };
                SceneManager.sceneLoaded += configureColdLocale;
            }

            AsyncOperation load = SceneManager.LoadSceneAsync(
                AppScenePath,
                LoadSceneMode.Single);
            Assert.That(load, Is.Not.Null, "AppScene could not be loaded.");
            yield return load;
            if (configureColdLocale != null)
                SceneManager.sceneLoaded -= configureColdLocale;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Scene appScene = SceneManager.GetSceneByPath(AppScenePath);
            if (appScene.IsValid() && appScene.isLoaded)
            {
                Scene cleanup = SceneManager.CreateScene(
                    "MeowdokuPlayModeCleanup");
                SceneManager.SetActiveScene(cleanup);
                AsyncOperation unload = SceneManager.UnloadSceneAsync(appScene);
                if (unload != null) yield return unload;
            }

            _stateOverride?.Dispose();
            _stateOverride = null;
            LevelBankIO.LoadOverride = null;
            BankData.ResetForTests();
            if (!string.IsNullOrEmpty(_stateDirectory) &&
                Directory.Exists(_stateDirectory))
                Directory.Delete(_stateDirectory, true);
            _stateDirectory = null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator AppScene_PrimaryRoutes_OpenCloseAndReuseAtRuntime()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null,
                "AppScene is missing AppBootstrap.");
            Assert.That(manager, Is.Not.Null,
                "AppScene is missing UIManager.");
            Assert.That(manager.transform.lossyScale.x, Is.GreaterThan(0.001f),
                "AppScene UI root was collapsed to zero scale at runtime.");
            Assert.That(manager.transform.Find("Windows").lossyScale.x,
                Is.GreaterThan(0.001f),
                "AppScene window root was collapsed to zero scale at runtime.");

            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(
                bootstrap.Phase,
                Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);

            yield return WaitForState(manager, UiName.Splash,
                UiWindowState.Hidden);
            AssertShowing(manager, UiName.Home);
            UIFrameWindow home = manager.Get(UiName.Home);
            Assert.That(home.gameObject.activeInHierarchy, Is.True);
            Assert.That(home.transform.lossyScale.x, Is.GreaterThan(0.001f),
                "Home was active but invisible because its scale was zero.");
            RectTransform homeRect = (RectTransform)home.transform;
            Assert.That(homeRect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(homeRect.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(homeRect.rect.width, Is.GreaterThan(100f));
            Assert.That(homeRect.rect.height, Is.GreaterThan(100f));

            UiName[] standaloneRoutes =
            {
                UiName.Tutorial,
                UiName.Language,
                UiName.HowToPlay,
                UiName.HowToPlayPaged,
                UiName.Bank
            };
            foreach (UiName route in standaloneRoutes)
                yield return ShowThenHide(manager, route);

            UIFrameWindow settings = manager.Show(UiName.Setting);
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.WindowState, Is.EqualTo(UiWindowState.Showing));
            Assert.That(manager.RequestBack(), Is.True,
                "Settings should consume the runtime back request.");
            yield return WaitForState(manager, UiName.Setting,
                UiWindowState.Hidden);

            UIFrameWindow language = manager.Show(UiName.Language);
            Assert.That(language, Is.Not.Null);
            manager.Hide(UiName.Language);
            Assert.That(language.WindowState, Is.EqualTo(UiWindowState.Closing));
            UIFrameWindow reopened = manager.Show(UiName.Language);
            Assert.That(reopened, Is.SameAs(language),
                "Reopening a closing page must reuse the cached instance.");
            Assert.That(reopened.WindowState, Is.EqualTo(UiWindowState.Showing));
            yield return null;
            Assert.That(reopened.WindowState, Is.EqualTo(UiWindowState.Showing));
            manager.Hide(UiName.Language);
            yield return WaitForState(manager, UiName.Language,
                UiWindowState.Hidden);

            var parameters = new Dictionary<string, object>(1)
            {
                ["level_index"] = 1
            };
            UIFrameWindow game = manager.Show(UiName.Game, parameters);
            Assert.That(game, Is.Not.Null);
            GameplayManager gameplay =
                game.GetComponentInChildren<GameplayManager>(true);
            Assert.That(gameplay, Is.Not.Null,
                "GamePage is missing GameplayManager at runtime.");
            yield return WaitUntil(
                () => gameplay.CurrentPuzzleSize > 0,
                TransitionTimeoutSeconds,
                "Gameplay did not build its level after GamePage opened.");
            Assert.That(gameplay.CurrentLevelNumber, Is.EqualTo(1));
            Assert.That(gameplay.CurrentPuzzleSize, Is.EqualTo(4));
            manager.Hide(UiName.Game);
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Hidden);

            AssertShowing(manager, UiName.Home);
            Assert.That(manager.IsAnyLoading, Is.False);
        }

        [UnityTest]
        public IEnumerator PlatformLifecycle_FocusPauseResumeDoesNotDuplicateSessionOrGamePage()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            TrackingRuntime tracking = Find<TrackingRuntime>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            Assert.That(tracking, Is.Not.Null);

            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            Assert.That(manager.IsBackInputEnabledForTests, Is.True,
                "UIManager global Back input is not bound.");
            Assert.That(GameStateRuntime.Current.Data.SessionCount, Is.EqualTo(1),
                "Startup must count one session despite TrackingRuntime Awake.");

            var parameters = new Dictionary<string, object>(1)
            {
                ["level_index"] = 1
            };
            UIFrameWindow gamePage = manager.Show(UiName.Game, parameters);
            Assert.That(gamePage, Is.Not.Null);
            GameplayManager gameplay =
                gamePage.GetComponentInChildren<GameplayManager>(true);
            Assert.That(gameplay, Is.Not.Null);
            yield return WaitForSession(gameplay, GameSessionState.Playing);

            int catColumn = gameplay.SolutionColumnForTests(0);
            SessionActionResult cat = gameplay.DoubleTapForTests(0, catColumn);
            Assert.That(cat.Accepted, Is.True);
            Assert.That(gameplay.GetCellState(0, catColumn),
                Is.EqualTo(CellStateType.CAT));

            string sessionId = tracking.Session.SessionId;
            int sessionRecord = tracking.Session.SessionRecord;
            int sessionCount = GameStateRuntime.Current.Data.SessionCount;
            int gamePageId = gamePage.GetInstanceID();
            int gameplayId = gameplay.GetInstanceID();

            for (int cycle = 0; cycle < 3; cycle++)
            {
                tracking.SendMessage(
                    "OnApplicationFocus",
                    false,
                    SendMessageOptions.RequireReceiver);
                tracking.SendMessage(
                    "OnApplicationPause",
                    true,
                    SendMessageOptions.RequireReceiver);
                gameplay.SendMessage(
                    "OnApplicationFocus",
                    false,
                    SendMessageOptions.RequireReceiver);
                gameplay.SendMessage(
                    "OnApplicationPause",
                    true,
                    SendMessageOptions.RequireReceiver);

                yield return null;

                tracking.SendMessage(
                    "OnApplicationPause",
                    false,
                    SendMessageOptions.RequireReceiver);
                tracking.SendMessage(
                    "OnApplicationFocus",
                    true,
                    SendMessageOptions.RequireReceiver);
                gameplay.SendMessage(
                    "OnApplicationPause",
                    false,
                    SendMessageOptions.RequireReceiver);
                gameplay.SendMessage(
                    "OnApplicationFocus",
                    true,
                    SendMessageOptions.RequireReceiver);
                bootstrap.SendMessage(
                    "OnApplicationFocus",
                    true,
                    SendMessageOptions.RequireReceiver);

                yield return null;

                Assert.That(manager.Get(UiName.Game), Is.SameAs(gamePage));
                Assert.That(gamePage.GetInstanceID(), Is.EqualTo(gamePageId));
                Assert.That(gamePage.GetComponentInChildren<GameplayManager>(true)
                    .GetInstanceID(), Is.EqualTo(gameplayId));
                Assert.That(gameplay.GetCellState(0, catColumn),
                    Is.EqualTo(CellStateType.CAT),
                    "Resume rebuilt the active GameSession.");
                Assert.That(gameplay.SessionState,
                    Is.EqualTo(GameSessionState.Playing));
            }

            Assert.That(tracking.Session.SessionId, Is.EqualTo(sessionId));
            Assert.That(tracking.Session.SessionRecord,
                Is.EqualTo(sessionRecord + 3),
                "Paired pause/focus callbacks must count one resume per cycle.");
            Assert.That(GameStateRuntime.Current.Data.SessionCount,
                Is.EqualTo(sessionCount),
                "A short resume must not start another source session.");
            Assert.That(
                Object.FindObjectsByType<GameplayManager>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None).Length,
                Is.EqualTo(1),
                "Focus/resume created a duplicate Game page hierarchy.");
        }

        [UnityTest]
        public IEnumerator PlatformStartup_CorruptPlayerSlotsUseDefaultsAndExitSplash()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);

            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "Corrupt player slots left AppBootstrap waiting on Splash.");

            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(
                manager,
                UiName.Splash,
                UiWindowState.Hidden);
            yield return WaitForState(
                manager,
                UiName.Tutorial,
                UiWindowState.Showing);

            SplashPagePresenter splash = manager.Get(UiName.Splash)
                .GetComponent<SplashPagePresenter>();
            TutorialPagePresenter tutorial = manager.Get(UiName.Tutorial)
                .GetComponent<TutorialPagePresenter>();
            Assert.That(splash, Is.Not.Null,
                "Startup did not use the registered Splash presenter.");
            Assert.That(tutorial, Is.Not.Null,
                "First-session startup did not route to the real Tutorial presenter.");
            Assert.That(splash.MinimumSecondsForTests, Is.EqualTo(3f)
                .Within(0.0001f),
                "Splash default progress duration differs from the source 3.0 seconds.");
            Assert.That(splash.ProgressForTests, Is.EqualTo(1f)
                .Within(0.0001f));
            Assert.That(splash.IsRunningForTests, Is.False);
            Assert.That(
                bootstrap.SplashForceRequestedAtForTests -
                bootstrap.StartupStartedAtForTests,
                Is.GreaterThanOrEqualTo(2.45f),
                "Launcher skipped the source 2.0 + 0.5 second splash gate.");
            Assert.That(
                bootstrap.SplashForceCompletedAtForTests -
                bootstrap.SplashForceRequestedAtForTests,
                Is.GreaterThanOrEqualTo(0.075f),
                "Splash force-complete skipped the source 0.1 second finish tween.");

            GameStateService state = GameStateRuntime.Current;
            Assert.That(state.CurrentLevel, Is.EqualTo(1));
            Assert.That(state.CurrentStrategy, Is.EqualTo(1));
            Assert.That(state.TutorialDone, Is.False);
            Assert.That(state.Data.ToolLocate, Is.EqualTo(5));
            Assert.That(state.Data.ToolHint, Is.EqualTo(5));
            Assert.That(state.Data.ToolUndo, Is.EqualTo(3));
            Assert.That(state.Data.SessionCount, Is.EqualTo(1));

            GameStateData persisted =
                new GameStateRepository(_stateDirectory).Load();
            Assert.That(persisted.CurrentLevel, Is.EqualTo(1));
            Assert.That(persisted.TutorialDone, Is.False);
            Assert.That(persisted.SessionCount, Is.EqualTo(1),
                "Startup did not recover a valid default player slot.");
        }

        [UnityTest]
        public IEnumerator PlatformTutorial_FullFlowRoutesGameAndReopensCleanly()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);

            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish for the Tutorial flow.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(manager, UiName.Splash, UiWindowState.Hidden);
            yield return WaitForState(manager, UiName.Tutorial, UiWindowState.Showing);

            TutorialPagePresenter tutorial = manager.Get(UiName.Tutorial)
                .GetComponent<TutorialPagePresenter>();
            Assert.That(tutorial, Is.Not.Null);
            Assert.That(tutorial.FailureReason, Is.Empty);
            Assert.That(tutorial.Phase, Is.EqualTo(TutorialPhase.PlaceFirstCat));
            Assert.That(tutorial.GuideFeedbackValueForTests,
                Is.EqualTo(GuideFeedbackConfig.ValueCurrent));
            Assert.That(tutorial.UsesDiagonalCopyForTests, Is.False);
            Assert.That(tutorial.BoardForTests.PuzzleSize, Is.EqualTo(4));
            Assert.That(tutorial.BoardInputGroupForTests.blocksRaycasts, Is.True);
            Assert.That(tutorial.BoardInputGroupForTests.interactable, Is.True);

            Transform mask = tutorial.transform.Find("Root/Mask");
            Assert.That(mask, Is.Not.Null);
            foreach (Graphic graphic in mask.GetComponentsInChildren<Graphic>(true))
                Assert.That(graphic.raycastTarget, Is.False,
                    "Tutorial mask graphics must not steal Board input.");
            int initialCellViews = tutorial.GetComponentsInChildren<CellView>(true).Length;
            Assert.That(initialCellViews, Is.GreaterThanOrEqualTo(17));
            Assert.That(tutorial.MaskCellCountForTests, Is.EqualTo(1));

            double time = 0.0;
            Assert.That(tutorial.TapCellForTests(0, 1, time += 0.1), Is.False,
                "A cell outside the first-step mask accepted input.");
            DoubleTapTutorial(tutorial, 0, 2, ref time);
            Assert.That(tutorial.Phase,
                Is.EqualTo(TutorialPhase.ConfirmOnePerColor));
            Assert.That(tutorial.BoardInputGroupForTests.blocksRaycasts, Is.False,
                "Board input was not locked during the source transition.");
            yield return new WaitForSecondsRealtime(0.45f);

            FindButton(manager.Get(UiName.Tutorial), "Confirm").onClick.Invoke();
            Assert.That(tutorial.Phase,
                Is.EqualTo(TutorialPhase.MarkRowAndColumn));
            Assert.That(tutorial.BoardInputGroupForTests.blocksRaycasts, Is.True);

            Vector2Int[] rowColumnMarks =
            {
                new(0, 0), new(0, 1), new(0, 3),
                new(1, 2), new(2, 2), new(3, 2)
            };
            TapTutorialCells(tutorial, rowColumnMarks, ref time);
            Assert.That(tutorial.Phase, Is.EqualTo(TutorialPhase.PlaceSecondCat));
            yield return new WaitForSecondsRealtime(0.45f);

            DoubleTapTutorial(tutorial, 3, 1, ref time);
            Assert.That(tutorial.Phase, Is.EqualTo(TutorialPhase.MarkNeighbors));
            yield return new WaitForSecondsRealtime(0.45f);

            Vector2Int[] neighborMarks =
            {
                new(2, 0), new(2, 1), new(3, 0)
            };
            TapTutorialCells(tutorial, neighborMarks, ref time);
            Assert.That(tutorial.Phase, Is.EqualTo(TutorialPhase.PlaceThirdCat));
            yield return new WaitForSecondsRealtime(0.45f);

            DoubleTapTutorial(tutorial, 1, 0, ref time);
            Assert.That(tutorial.Phase, Is.EqualTo(TutorialPhase.FreePlay));
            yield return new WaitForSecondsRealtime(0.45f);

            Button hint = FindButton(manager.Get(UiName.Tutorial), "Hint");
            hint.onClick.Invoke();
            Assert.That(tutorial.HintPhaseForTests, Is.EqualTo(1));
            Assert.That(tutorial.MaskCellCountForTests, Is.EqualTo(3),
                "Source hint shows two blank blue-row cells plus its cat mirror.");
            hint.onClick.Invoke();
            Assert.That(tutorial.HintPhaseForTests, Is.Zero);
            Assert.That(tutorial.BoardForTests.GetCellState(1, 1),
                Is.EqualTo(CellStateType.MARK));
            Assert.That(tutorial.BoardForTests.GetCellState(1, 3),
                Is.EqualTo(CellStateType.MARK));
            hint.onClick.Invoke();
            Assert.That(tutorial.HintPhaseForTests, Is.EqualTo(2));
            hint.onClick.Invoke();
            Assert.That(tutorial.BoardForTests.GetCellState(3, 3),
                Is.EqualTo(CellStateType.MARK));
            hint.onClick.Invoke();
            Assert.That(tutorial.HintPhaseForTests, Is.EqualTo(3));
            hint.onClick.Invoke();
            Assert.That(tutorial.Phase, Is.EqualTo(TutorialPhase.FinishConfirm));
            Assert.That(tutorial.BoardForTests.GetCellState(2, 3),
                Is.EqualTo(CellStateType.CAT));
            yield return new WaitForSecondsRealtime(0.55f);

            FindButton(manager.Get(UiName.Tutorial), "Confirm").onClick.Invoke();
            yield return WaitForState(manager, UiName.Tutorial, UiWindowState.Hidden);
            yield return WaitForState(manager, UiName.Game, UiWindowState.Showing);
            GameplayManager gameplay = Find<GameplayManager>();
            yield return WaitForSession(gameplay, GameSessionState.Playing);
            Assert.That(GameStateRuntime.Current.TutorialDone, Is.True);
            Assert.That(gameplay.CurrentLevelNumber, Is.EqualTo(1));

            UIFrameWindow reopenedWindow = manager.Show(UiName.Tutorial);
            Assert.That(reopenedWindow.GetComponent<TutorialPagePresenter>(),
                Is.SameAs(tutorial));
            Assert.That(tutorial.Phase, Is.EqualTo(TutorialPhase.PlaceFirstCat));
            Assert.That(tutorial.BoardForTests.GetCellState(0, 2),
                Is.EqualTo(CellStateType.EMPTY));
            Assert.That(tutorial.GetComponentsInChildren<CellView>(true).Length,
                Is.EqualTo(initialCellViews),
                "Tutorial reopen leaked pooled Board or mask CellView instances.");
            manager.Hide(UiName.Tutorial);
            yield return WaitForState(manager, UiName.Tutorial, UiWindowState.Hidden);
        }

        [UnityTest]
        public IEnumerator PlatformHomeHowToPlay_RefreshAndReopenCleanly()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            SoundService sound = Find<SoundService>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            Assert.That(sound, Is.Not.Null);

            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish for Home/HTP lifecycle coverage.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(manager, UiName.Splash, UiWindowState.Hidden);
            yield return WaitForState(manager, UiName.Home, UiWindowState.Showing);

            HomePagePresenter home = manager.Get(UiName.Home) as HomePagePresenter;
            Assert.That(home, Is.Not.Null);
            LocalizationCatalog localization = home.LocalizationForTests;
            Assert.That(localization, Is.Not.Null);
            Assert.That(home.LevelTextForTests,
                Is.EqualTo(ExpectedLevelText(localization, 1)));

            GameStateRuntime.Current.SetCurrentLevel(7);
            home.RefreshPresentation();
            Assert.That(home.LevelTextForTests,
                Is.EqualTo(ExpectedLevelText(localization, 7)),
                "Visible Home did not refresh its level from live state.");
            Assert.That(localization.SetLocale("vi"), Is.True);
            Assert.That(home.LevelTextForTests,
                Is.EqualTo(ExpectedLevelText(localization, 7)),
                "Home did not react to the source translation-change boundary.");

            int homeInstanceCount = Object.FindObjectsByType<HomePagePresenter>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None).Length;
            manager.Hide(UiName.Home);
            yield return WaitForState(manager, UiName.Home, UiWindowState.Hidden);
            GameStateRuntime.Current.SetCurrentLevel(9);
            UIFrameWindow reopenedHome = manager.Show(UiName.Home);
            Assert.That(reopenedHome, Is.SameAs(home));
            yield return WaitForState(manager, UiName.Home, UiWindowState.Showing);
            Assert.That(home.LevelTextForTests,
                Is.EqualTo(ExpectedLevelText(localization, 9)),
                "Reopened Home kept the previous level presentation.");
            yield return WaitUntil(
                () => !home.PopupQueueRunningForTests,
                TransitionTimeoutSeconds,
                "Home popup queue remained active after reopen.");
            Assert.That(Object.FindObjectsByType<HomePagePresenter>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None).Length,
                Is.EqualTo(homeInstanceCount),
                "Reopening Home created a duplicate presenter.");

            HowToPlayPagePresenter full = manager.Show(UiName.HowToPlay) as
                HowToPlayPagePresenter;
            Assert.That(full, Is.Not.Null);
            Assert.That(full.FailureReason, Is.Empty);
            Assert.That(sound.Silent, Is.True);
            Assert.That(full.DemoRunningForTests, Is.True);
            HowToPlayDemoBoardView[] fullBoards =
                full.GetComponentsInChildren<HowToPlayDemoBoardView>(true);
            Assert.That(fullBoards.Length,
                Is.EqualTo(HowToPlayContract.FullDemos.Count));
            int fullCellCount = full.GetComponentsInChildren<CellView>(true).Length;
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(HasNonEmptyDemoCell(fullBoards), Is.True,
                "Full HTP demo did not advance after its source start delay.");
            manager.Hide(UiName.HowToPlay);
            yield return WaitForState(manager, UiName.HowToPlay,
                UiWindowState.Hidden);
            Assert.That(sound.Silent, Is.False);
            Assert.That(full.DemoRunningForTests, Is.False);
            Assert.That(AllDemoCellsEmpty(fullBoards), Is.True);

            for (int cycle = 0; cycle < 3; cycle++)
            {
                UIFrameWindow reopened = manager.Show(UiName.HowToPlay);
                Assert.That(reopened, Is.SameAs(full));
                Assert.That(full.DemoRunningForTests, Is.True);
                manager.Hide(UiName.HowToPlay);
                yield return WaitForState(manager, UiName.HowToPlay,
                    UiWindowState.Hidden);
                Assert.That(full.DemoRunningForTests, Is.False);
                Assert.That(sound.Silent, Is.False);
            }
            Assert.That(full.GetComponentsInChildren<CellView>(true).Length,
                Is.EqualTo(fullCellCount),
                "Full HTP reopen changed its fixed demo-cell population.");

            HowToPlayPagedPagePresenter paged =
                manager.Show(UiName.HowToPlayPaged) as
                    HowToPlayPagedPagePresenter;
            Assert.That(paged, Is.Not.Null);
            Assert.That(paged.FailureReason, Is.Empty);
            Assert.That(paged.PageIndex, Is.Zero);
            Assert.That(paged.DemoRunningForTests, Is.True);
            Assert.That(sound.Silent, Is.True);
            HowToPlayDemoBoardView[] pagedBoards =
                paged.GetComponentsInChildren<HowToPlayDemoBoardView>(true);
            int pagedCellCount = paged.GetComponentsInChildren<CellView>(true).Length;
            FindButton(paged, "MainBtn").onClick.Invoke();
            Assert.That(paged.PageIndex, Is.EqualTo(1));
            manager.Hide(UiName.HowToPlayPaged);
            yield return WaitForState(manager, UiName.HowToPlayPaged,
                UiWindowState.Hidden);
            Assert.That(paged.DemoRunningForTests, Is.False);
            Assert.That(sound.Silent, Is.False);
            Assert.That(AllDemoCellsEmpty(pagedBoards), Is.True);

            UIFrameWindow reopenedPaged = manager.Show(UiName.HowToPlayPaged);
            Assert.That(reopenedPaged, Is.SameAs(paged));
            Assert.That(paged.PageIndex, Is.Zero,
                "Paged HTP did not restart from its first source page.");
            Assert.That(paged.DemoRunningForTests, Is.True);
            manager.Hide(UiName.HowToPlayPaged);
            yield return WaitForState(manager, UiName.HowToPlayPaged,
                UiWindowState.Hidden);
            Assert.That(paged.GetComponentsInChildren<CellView>(true).Length,
                Is.EqualTo(pagedCellCount),
                "Paged HTP reopen changed its fixed demo-cell population.");
            Assert.That(sound.Silent, Is.False);
            AssertShowing(manager, UiName.Home);
        }

        [UnityTest]
        public IEnumerator PlatformHomePopupQueue_RewardRestoreCollectCloseAndReopenCleanly()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);

            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish for Home reward-restore coverage.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(manager, UiName.Splash, UiWindowState.Hidden);
            yield return WaitForState(manager, UiName.Home, UiWindowState.Showing);

            HomePagePresenter home = manager.Get(UiName.Home) as HomePagePresenter;
            Assert.That(home, Is.Not.Null);
            yield return WaitUntil(
                () => !home.PopupQueueRunningForTests,
                TransitionTimeoutSeconds,
                "Initial Home popup queue did not settle.");

            GameStateService state = GameStateRuntime.Current;
            long now = DateTimeOffset.Now.ToUnixTimeSeconds();
            state.RecordNormalReward(now - 30);
            state.RecordNormalReward(now - 20);
            state.RecordNormalReward(now - 10);
            state.AddPendingReward(new Dictionary<string, object>
            {
                ["show_id"] = "restore-hint",
                ["source"] = TrackerCatalog.AdPosition.PropsNormalHint,
                ["ts"] = now - 2
            });
            state.AddPendingReward(new Dictionary<string, object>
            {
                ["show_id"] = "restore-locate",
                ["source"] = TrackerCatalog.AdPosition.PropsNormalLocate,
                ["ts"] = now - 1
            });
            int initialHint = state.GetToolCount("hint");
            int initialLocate = state.GetToolCount("locate");

            manager.Hide(UiName.Home);
            yield return WaitForState(manager, UiName.Home, UiWindowState.Hidden);
            Assert.That(manager.Show(UiName.Home), Is.SameAs(home));
            yield return WaitForState(manager, UiName.AdRewardRestored,
                UiWindowState.Showing,
                5f);

            UIFrameWindow firstWindow = manager.Get(UiName.AdRewardRestored);
            var rewardPage = firstWindow as AdRewardRestoredPagePresenter;
            Assert.That(rewardPage, Is.Not.Null);
            int rewardPageCount = Object.FindObjectsByType<
                AdRewardRestoredPagePresenter>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None).Length;
            FindButton(firstWindow, "CollectButton").onClick.Invoke();
            yield return WaitForState(manager, UiName.AdRewardRestored,
                UiWindowState.Hidden,
                5f);
            yield return WaitUntil(
                () => !home.PopupQueueRunningForTests,
                TransitionTimeoutSeconds,
                "Home queue did not continue after collecting restored rewards.");

            Assert.That(state.GetToolCount("hint"), Is.EqualTo(initialHint + 1));
            Assert.That(state.GetToolCount("locate"),
                Is.EqualTo(initialLocate + 1));
            Assert.That(state.RestoredTodayCount, Is.EqualTo(2));
            Assert.That(state.GetPendingRewards(), Is.Empty);
            Assert.That(state.GetInFlightAwards(), Is.Empty);

            state.AddPendingReward(new Dictionary<string, object>
            {
                ["show_id"] = "restore-close",
                ["source"] = TrackerCatalog.AdPosition.PropsDailyHint,
                ["ts"] = now
            });
            manager.Hide(UiName.Home);
            yield return WaitForState(manager, UiName.Home, UiWindowState.Hidden);
            Assert.That(manager.Show(UiName.Home), Is.SameAs(home));
            yield return WaitForState(manager, UiName.AdRewardRestored,
                UiWindowState.Showing,
                5f);
            Assert.That(manager.Get(UiName.AdRewardRestored), Is.SameAs(firstWindow),
                "Reward popup reopen must reuse its cached page instance.");
            FindButton(firstWindow, "CloseButton").onClick.Invoke();
            yield return WaitForState(manager, UiName.AdRewardRestored,
                UiWindowState.Hidden,
                5f);
            yield return WaitUntil(
                () => !home.PopupQueueRunningForTests,
                TransitionTimeoutSeconds,
                "Home queue did not continue after closing restored rewards.");

            Assert.That(state.GetToolCount("hint"), Is.EqualTo(initialHint + 1),
                "Closing reward restore must not grant its displayed reward.");
            Assert.That(state.GetToolCount("locate"),
                Is.EqualTo(initialLocate + 1));
            Assert.That(state.RestoredTodayCount, Is.EqualTo(2),
                "Closing reward restore must not consume the daily grant count.");
            Assert.That(state.GetPendingRewards(), Is.Empty,
                "Source removes a presented batch even when it is closed.");

            manager.Hide(UiName.Home);
            yield return WaitForState(manager, UiName.Home, UiWindowState.Hidden);
            Assert.That(manager.Show(UiName.Home), Is.SameAs(home));
            yield return new WaitForSecondsRealtime(
                HomePageContract.RewardRestoreDelaySeconds + 0.15f);
            Assert.That(IsShowing(manager, UiName.AdRewardRestored), Is.False,
                "An empty restore queue reopened the reward popup.");
            yield return WaitUntil(
                () => !home.PopupQueueRunningForTests,
                TransitionTimeoutSeconds,
                "Empty Home queue remained active after reward-popup reopen.");
            Assert.That(Object.FindObjectsByType<AdRewardRestoredPagePresenter>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None).Length,
                Is.EqualTo(rewardPageCount),
                "Reward popup reopen created a duplicate presenter.");
            Assert.That(manager.IsAnyLoading, Is.False);
        }

        [UnityTest]
        public IEnumerator PlatformLocalization_ColdStartAppliesPersistedLocale()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            AbConfigRuntime abRuntime = Find<AbConfigRuntime>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            Assert.That(abRuntime, Is.Not.Null);

            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish for cold locale coverage.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(manager, UiName.Splash, UiWindowState.Hidden);
            yield return WaitForState(manager, UiName.Home, UiWindowState.Showing);

            HomePagePresenter home = manager.Get(UiName.Home) as HomePagePresenter;
            Assert.That(home, Is.Not.Null);
            LocalizationCatalog catalog = home.LocalizationForTests;
            Assert.That(catalog, Is.Not.Null);
            Assert.That(abRuntime.Settings.Language.IsLanguageSwitchEnabledPeek(
                    abRuntime.ValueProvider),
                Is.True);
            Assert.That(GameStateRuntime.Current.AppliedLocale, Is.EqualTo("vi"));
            Assert.That(catalog.Locale, Is.EqualTo("vi"),
                "Startup ignored the persisted locale while language switching was enabled.");
            Assert.That(catalog.TranslationColumn, Is.EqualTo("vi"));
            Assert.That(home.LevelTextForTests,
                Is.EqualTo(ExpectedLevelText(catalog, 1)));
            Assert.That(home.LevelTextForTests, Does.Contain("Màn"));

            UIFrameWindow settings = manager.Show(UiName.Setting);
            Assert.That(settings, Is.Not.Null);
            Assert.That(FindButton(settings, "LanguageBtn")
                .gameObject.activeInHierarchy, Is.True,
                "The enabled language feature was not reflected after cold start.");
            manager.Hide(UiName.Setting);
            yield return WaitForState(manager, UiName.Setting,
                UiWindowState.Hidden);
            AssertShowing(manager, UiName.Home);
        }

        [UnityTest]
        public IEnumerator PlatformNavigation_HomeBackOpensSourceQuitConfirm()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);

            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(
                manager,
                UiName.Splash,
                UiWindowState.Hidden);
            yield return WaitForState(
                manager,
                UiName.Home,
                UiWindowState.Showing);
            Assert.That(manager.IsBackInputEnabledForTests, Is.True,
                "UIManager global Back input is not bound.");

            HomePagePresenter home = manager.Get(UiName.Home)
                .GetComponent<HomePagePresenter>();
            Assert.That(home, Is.Not.Null);
            int quitCount = 0;
            home.ConfigureQuitForTests(() => quitCount++);

            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                int eventsBefore = manager.BackInputEventCountForTests;
                int pressesBefore = manager.BackInputPressCountForTests;
                yield return PressEscape(keyboard);
                Assert.That(manager.BackInputEventCountForTests,
                    Is.GreaterThan(eventsBefore),
                    "UIManager did not receive the queued keyboard event.");
                Assert.That(manager.BackInputPressCountForTests,
                    Is.GreaterThan(pressesBefore),
                    "UIManager received the event but did not detect the Escape press edge.");
                Assert.That(manager.LastBackRequestHandledForTests, Is.True,
                    "Escape reached UIManager but the visible UI stack did not handle Back.");
                yield return WaitForState(
                    manager,
                    UiName.Confirm,
                    UiWindowState.Showing);

                UIFrameWindow confirmWindow = manager.Get(UiName.Confirm);
                ConfirmDialogPresenter confirm =
                    confirmWindow.GetComponent<ConfirmDialogPresenter>();
                Assert.That(confirm, Is.Not.Null);
                Assert.That(confirm.TitleForTests, Is.Not.Empty);
                Assert.That(confirm.TitleForTests,
                    Is.Not.EqualTo("DIALOG_QUIT_TITLE"));
                Assert.That(confirm.ContentForTests, Is.Not.Empty);
                Assert.That(confirm.ContentForTests,
                    Is.Not.EqualTo("DIALOG_QUIT_MSG"));
                Assert.That(confirm.ActionForTests, Is.Not.Empty);
                Assert.That(confirm.ActionForTests,
                    Is.Not.EqualTo("DIALOG_QUIT_BTN"));

                // Source ConfirmDialog has CloseButton, not the base
                // CloseBtn convention, so another hardware Back is consumed
                // by the top window without closing it.
                yield return PressEscape(keyboard);
                Assert.That(confirmWindow.WindowState,
                    Is.EqualTo(UiWindowState.Showing));

                FindButton(confirmWindow, "CloseButton").onClick.Invoke();
                yield return WaitForState(
                    manager,
                    UiName.Confirm,
                    UiWindowState.Hidden);
                Assert.That(quitCount, Is.Zero);
                AssertShowing(manager, UiName.Home);

                yield return PressEscape(keyboard);
                yield return WaitForState(
                    manager,
                    UiName.Confirm,
                    UiWindowState.Showing);
                confirmWindow = manager.Get(UiName.Confirm);
                Button action = FindButton(confirmWindow, "ActionButton");
                action.onClick.Invoke();
                action.onClick.Invoke();
                Assert.That(quitCount, Is.EqualTo(1),
                    "Rapid confirm must invoke the quit callback once.");
                yield return WaitForState(
                    manager,
                    UiName.Confirm,
                    UiWindowState.Hidden);
                AssertShowing(manager, UiName.Home);
            }
            finally
            {
                if (keyboard != null && keyboard.added)
                    InputSystem.RemoveDevice(keyboard);
            }
        }

        [UnityTest]
        public IEnumerator PlatformLevelSelection_MainUsesSourceControlSizeCycle()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            AbConfigRuntime abRuntime = Find<AbConfigRuntime>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            Assert.That(abRuntime, Is.Not.Null);

            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(
                manager,
                UiName.Splash,
                UiWindowState.Hidden);
            yield return WaitForState(
                manager,
                UiName.Home,
                UiWindowState.Showing);

            GameStateRuntime.Current.SetCurrentLevel(3);
            UIFrameWindow home = manager.Get(UiName.Home);
            FindButton(home, "StartBtn").onClick.Invoke();

            yield return WaitForState(
                manager,
                UiName.Game,
                UiWindowState.Showing);
            GameplayManager gameplay = manager.Get(UiName.Game)
                .GetComponentInChildren<GameplayManager>(true);
            Assert.That(gameplay, Is.Not.Null);
            yield return WaitUntil(
                () => gameplay.SessionState == GameSessionState.Playing,
                TransitionTimeoutSeconds,
                "Level 3 did not reach Playing state.");

            Assert.That(abRuntime.LevelSelection.SizeCycle.Value,
                Is.EqualTo(SizeCycleConfig.ValueControl));
            Assert.That(gameplay.CurrentLevelNumber, Is.EqualTo(3));
            Assert.That(LevelData.GetSize(3), Is.EqualTo(5),
                "The static bank schedule changed unexpectedly.");
            Assert.That(gameplay.CurrentPuzzleSize, Is.EqualTo(6),
                "Main gameplay bypassed the source control _get_ab_size schedule.");

            var tutorialCats = new List<Vector2Int>();
            for (int row = 0; row < gameplay.CurrentPuzzleSize; row++)
            {
                for (int column = 0; column < gameplay.CurrentPuzzleSize; column++)
                {
                    if (gameplay.GetCellState(row, column) == CellStateType.CAT)
                        tutorialCats.Add(new Vector2Int(row, column));
                }
            }
            Assert.That(tutorialCats, Has.Count.EqualTo(1),
                "Fresh Main levels 1-10 must apply the source tutorial prefill.");
            Dictionary<string, object> retry =
                GameStateRuntime.Current.GetRetryPuzzle(3);
            var retryPrefills = (List<object>)retry["prefill_positions"];
            Assert.That(retryPrefills, Has.Count.EqualTo(1));
            Assert.That(
                (List<object>)retryPrefills[0],
                Is.EqualTo(new object[]
                {
                    tutorialCats[0].x,
                    tutorialCats[0].y
                }),
                "The cached retry must preserve the same tutorial prefill.");
        }

        [UnityTest]
        public IEnumerator PlatformLevelSelection_PreCatUsesConfigAndKeepsLockedCellOnRetry()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            AbConfigRuntime abRuntime = Find<AbConfigRuntime>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            Assert.That(abRuntime, Is.Not.Null);

            GameStateService state = GameStateRuntime.Current;
            state.SetCurrentLevel(21);
            state.Data.PreCatPendingStruggle = true;
            LevelBankIO.LoadOverride = filename => filename == "bankData8x8.json"
                ? new Dictionary<string, object>
                {
                    ["1"] = new List<object>
                    {
                        PreCatEntry()
                    }
                }
                : null;
            PlayModeAbProvider provider =
                abRuntime.gameObject.AddComponent<PlayModeAbProvider>();
            provider.SetInt("pre_cat", PreCatConfig.ValueAlways);
            abRuntime.BindProvider(provider);

            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(
                manager,
                UiName.Splash,
                UiWindowState.Hidden);
            yield return WaitForState(
                manager,
                UiName.Home,
                UiWindowState.Showing);

            UIFrameWindow home = manager.Get(UiName.Home);
            Assert.That(state.CurrentLevel, Is.EqualTo(21));
            Assert.That(state.Data.PreCatPendingStruggle, Is.True);
            FindButton(home, "StartBtn").onClick.Invoke();
            yield return WaitForState(
                manager,
                UiName.Game,
                UiWindowState.Showing);
            UIFrameWindow game = manager.Get(UiName.Game);
            GameplayManager gameplay =
                game.GetComponentInChildren<GameplayManager>(true);
            yield return WaitUntil(
                () => gameplay != null &&
                      gameplay.SessionState == GameSessionState.Playing,
                TransitionTimeoutSeconds,
                "Pre-cat level did not reach Playing state.");

            Assert.That(abRuntime.LevelSelection.PreCat.Value,
                Is.EqualTo(PreCatConfig.ValueAlways));

            Dictionary<string, object> firstLock = state.GetPreCatLock(21);
            Assert.That(firstLock["locked"], Is.True);
            Assert.That(firstLock["pre_type"], Is.EqualTo("2"));
            Vector2Int lockedPosition = (Vector2Int)firstLock["position"];
            Assert.That(lockedPosition.x, Is.GreaterThanOrEqualTo(0));
            Assert.That(
                gameplay.GetCellState(lockedPosition.x, lockedPosition.y),
                Is.EqualTo(CellStateType.CAT));
            Assert.That(
                (List<object>)state.GetRetryPuzzle(21)["prefill_positions"],
                Is.Empty,
                "Pre-cat is reconstructed from its lock and must not be duplicated in retry prefills.");

            FindButton(game, "BackBtn").onClick.Invoke();
            yield return WaitForState(
                manager,
                UiName.Game,
                UiWindowState.Hidden);
            yield return WaitForState(
                manager,
                UiName.Home,
                UiWindowState.Showing);
            FindButton(home, "StartBtn").onClick.Invoke();
            yield return WaitForState(
                manager,
                UiName.Game,
                UiWindowState.Showing);
            yield return WaitUntil(
                () => gameplay.SessionState == GameSessionState.Playing,
                TransitionTimeoutSeconds,
                "Cached pre-cat retry did not reach Playing state.");

            Dictionary<string, object> retryLock = state.GetPreCatLock(21);
            Assert.That((Vector2Int)retryLock["position"],
                Is.EqualTo(lockedPosition));
            Assert.That(
                gameplay.GetCellState(lockedPosition.x, lockedPosition.y),
                Is.EqualTo(CellStateType.CAT),
                "Retry must reconstruct the exact locked pre-cat cell.");
        }

        [UnityTest]
        public IEnumerator PlatformColorMap_RegionColorRuntimeReachesBoard()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            AbConfigRuntime abRuntime = Find<AbConfigRuntime>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            Assert.That(abRuntime, Is.Not.Null);

            PlayModeAbProvider provider =
                abRuntime.gameObject.AddComponent<PlayModeAbProvider>();
            provider.SetInt(
                "region_color",
                RegionColorConfig.ValuePaletteV8);
            provider.SetInt(
                "game_grid_ui",
                GameGridUiConfig.ValueSingleLine);
            provider.SetInt(
                "board_size_big",
                BoardSizeBigConfig.ValueEnlarged);
            provider.SetInt(
                "rule_highlight",
                RuleHighlightConfig.ValueHighlightAllLevels);
            provider.SetInt(
                "prop_highlight",
                PropHighlightConfig.ValueControlRepeatable);
            provider.SetInt(
                "mark_sound",
                MarkSoundConfig.ValueSoft2);
            provider.SetInt("reward_unlock_level", 8);
            provider.SetInt(
                "dda_rank",
                DdaRankConfig.ValueAnyAction);
            abRuntime.BindProvider(provider);

            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(
                manager,
                UiName.Splash,
                UiWindowState.Hidden);
            yield return WaitForState(
                manager,
                UiName.Home,
                UiWindowState.Showing);

            FindButton(manager.Get(UiName.Home), "StartBtn").onClick.Invoke();
            yield return WaitForState(
                manager,
                UiName.Game,
                UiWindowState.Showing);
            GameplayManager gameplay = manager.Get(UiName.Game)
                .GetComponentInChildren<GameplayManager>(true);
            yield return WaitUntil(
                () => gameplay != null &&
                      gameplay.SessionState == GameSessionState.Playing,
                TransitionTimeoutSeconds,
                "Color-map fixture level did not reach Playing state.");

            BoardView board = manager.Get(UiName.Game)
                .GetComponentInChildren<BoardView>(true);
            Assert.That(abRuntime.Board.RegionColor.Value,
                Is.EqualTo(RegionColorConfig.ValuePaletteV8));
            Assert.That(abRuntime.Board.GameGridUi.Value,
                Is.EqualTo(GameGridUiConfig.ValueSingleLine));
            Assert.That(abRuntime.Board.BoardSizeBig.Value,
                Is.EqualTo(BoardSizeBigConfig.ValueEnlarged));
            Assert.That(abRuntime.Gameplay.RuleHighlight.IsAllLevels(), Is.True);
            Assert.That(abRuntime.Gameplay.PropHighlight.IsRepeatable(), Is.True);
            Assert.That(abRuntime.Gameplay.MarkSound.IsSoftVariant2(), Is.True);
            Assert.That(abRuntime.Gameplay.RewardUnlockLevel.Value, Is.EqualTo(8));
            Assert.That(abRuntime.Gameplay.DdaRank.IsAnyActionDemote(), Is.True);
            Assert.That(GameStateRuntime.Current.DdaRankValueForTests,
                Is.EqualTo(DdaRankConfig.ValueAnyAction));
            Assert.That(gameplay.ShouldHighlightRuleViolation(), Is.True);
            Assert.That(gameplay.GameGridUiValueForTests,
                Is.EqualTo(GameGridUiConfig.ValueSingleLine));
            Assert.That(gameplay.BoardSizeBigValueForTests,
                Is.EqualTo(BoardSizeBigConfig.ValueEnlarged));
            Assert.That(board, Is.Not.Null);
            Assert.That(board.GridPaddingPixels, Is.EqualTo(3));
            Assert.That(board.GridSlotPixels, Is.EqualTo(102));
            Assert.That(board.VisibleBoardPixels,
                Is.EqualTo(board.PuzzleSize >= 8 ? 1050.003f : 1008f)
                    .Within(0.01f));
            Assert.That(board.regionColors, Has.Length.EqualTo(12));
            Assert.That(board.regionColors[0].r,
                Is.EqualTo(211f / 255f).Within(0.00001f));
            Assert.That(board.regionColors[0].g,
                Is.EqualTo(213f / 255f).Within(0.00001f));
            Assert.That(board.regionColors[0].b,
                Is.EqualTo(81f / 255f).Within(0.00001f));

        }

        [UnityTest]
        public IEnumerator PlatformInput_TapToggleAndDoubleTapUseRuntimeGestureFlow()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            AbConfigRuntime abRuntime = Find<AbConfigRuntime>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            Assert.That(abRuntime, Is.Not.Null);

            PlayModeAbProvider provider =
                abRuntime.gameObject.AddComponent<PlayModeAbProvider>();
            provider.SetInt(
                "doubletap_protect",
                DoubleTapProtectConfig.ValueShorten);
            provider.SetInt(
                "swipe_protect",
                SwipeProtectConfig.ValueHotzone20);
            abRuntime.BindProvider(provider);

            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(manager, UiName.Splash,
                UiWindowState.Hidden);
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Showing);

            FindButton(manager.Get(UiName.Home), "StartBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Showing);
            GameplayManager gameplay = manager.Get(UiName.Game)
                .GetComponentInChildren<GameplayManager>(true);
            Assert.That(gameplay, Is.Not.Null);
            yield return WaitForSession(gameplay, GameSessionState.Playing);
            BoardView board = gameplay.boardView;
            Assert.That(board, Is.Not.Null);
            Assert.That(abRuntime.Input.DoubleTapProtect.Value,
                Is.EqualTo(DoubleTapProtectConfig.ValueShorten));
            Assert.That(abRuntime.Input.SwipeProtect.Value,
                Is.EqualTo(SwipeProtectConfig.ValueHotzone20));
            Assert.That(gameplay.DoubleTapProtectValueForTests,
                Is.EqualTo(DoubleTapProtectConfig.ValueShorten));
            Assert.That(gameplay.SwipeProtectValueForTests,
                Is.EqualTo(SwipeProtectConfig.ValueHotzone20));

            Vector2Int markCell = FindInputCell(gameplay, solution: false);
            Assert.That(markCell.x, Is.GreaterThanOrEqualTo(0));
            SendBoardGestureTap(board, markCell, 1000);
            Assert.That(gameplay.GetCellState(markCell.x, markCell.y),
                Is.EqualTo(CellStateType.MARK),
                "Pointer-down must apply EMPTY to MARK immediately.");

            SendBoardGestureTap(board, markCell, 1300);
            Assert.That(gameplay.GetCellState(markCell.x, markCell.y),
                Is.EqualTo(CellStateType.EMPTY),
                "A later single tap must toggle MARK back to EMPTY.");

            Vector2Int catCell = FindInputCell(gameplay, solution: true);
            Assert.That(catCell.x, Is.GreaterThanOrEqualTo(0));
            SendBoardGestureTap(board, catCell, 2000);
            Assert.That(gameplay.GetCellState(catCell.x, catCell.y),
                Is.EqualTo(CellStateType.MARK));
            SendBoardGestureTap(board, catCell, 2100);
            Assert.That(gameplay.GetCellState(catCell.x, catCell.y),
                Is.EqualTo(CellStateType.CAT),
                "The second tap must resolve the source solution cell to CAT.");

            gameplay.Undo();
            Assert.That(gameplay.GetCellState(catCell.x, catCell.y),
                Is.EqualTo(CellStateType.EMPTY),
                "Godot folds the prior MARK into the CAT step, so one Undo returns EMPTY.");
            Assert.That(board.GetCellForTests(catCell.x, catCell.y).GetState(),
                Is.EqualTo(CellStateType.EMPTY),
                "Undo must repaint BoardView from the authoritative model rollback.");
        }

        [UnityTest]
        public IEnumerator PlatformRuleHighlight_RespectsControlAndAllLevelVariants()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            AbConfigRuntime abRuntime = Find<AbConfigRuntime>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            Assert.That(abRuntime, Is.Not.Null);

            PlayModeAbProvider provider =
                abRuntime.gameObject.AddComponent<PlayModeAbProvider>();
            provider.SetInt(
                "rule_highlight",
                RuleHighlightConfig.ValueHighlightAllLevels);
            abRuntime.BindProvider(provider);

            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(manager, UiName.Splash,
                UiWindowState.Hidden);
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Showing);

            FindButton(manager.Get(UiName.Home), "StartBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Showing);
            UIFrameWindow gamePage = manager.Get(UiName.Game);
            GameplayManager gameplay = gamePage
                .GetComponentInChildren<GameplayManager>(true);
            GameplayRuleBarPresenter ruleBar = gamePage
                .GetComponentInChildren<GameplayRuleBarPresenter>(true);
            Assert.That(gameplay, Is.Not.Null);
            Assert.That(ruleBar, Is.Not.Null);
            yield return WaitForSession(gameplay, GameSessionState.Playing);

            Vector2Int seedCat = FindInputCell(gameplay, solution: true);
            Assert.That(seedCat.x, Is.GreaterThanOrEqualTo(0));
            SessionActionResult seeded = gameplay.DoubleTapForTests(
                seedCat.x,
                seedCat.y);
            Assert.That(seeded.Kind, Is.EqualTo(SessionActionKind.CorrectCat));

            RuleHighlightConfig config = abRuntime.Gameplay.RuleHighlight;
            Assert.That(config.IsAllLevels(), Is.True);
            config.SetDebugOverride(RuleHighlightConfig.ValueControl);
            Vector2Int firstWrong = FindEmptyWrongCell(gameplay);
            SessionActionResult disabled = gameplay.DoubleTapForTests(
                firstWrong.x,
                firstWrong.y);
            Assert.That(disabled.Kind, Is.EqualTo(SessionActionKind.WrongGuess));
            Assert.That(disabled.RuleViolation,
                Is.Not.EqualTo(QueendokuCore.Rule.None));
            Assert.That(ruleBar.IsHighlightVisibleForTests(
                    disabled.RuleViolation),
                Is.False,
                "Control must keep every rule highlight hidden.");

            yield return WaitForSession(gameplay, GameSessionState.Playing);
            config.ClearDebugOverride();
            Assert.That(gameplay.ShouldHighlightRuleViolation(), Is.True);
            Vector2Int secondWrong = FindEmptyWrongCell(gameplay);
            SessionActionResult enabled = gameplay.DoubleTapForTests(
                secondWrong.x,
                secondWrong.y);
            Assert.That(enabled.Kind, Is.EqualTo(SessionActionKind.WrongGuess));
            Assert.That(enabled.RuleViolation,
                Is.Not.EqualTo(QueendokuCore.Rule.None));
            Assert.That(ruleBar.IsHighlightVisibleForTests(
                    enabled.RuleViolation),
                Is.True,
                "All-level variant must pulse the classified rule.");
        }

        [UnityTest]
        public IEnumerator PlatformMainGame_StartActionsFailRestartAndWinStayCoherent()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            AbConfigRuntime abRuntime = Find<AbConfigRuntime>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            Assert.That(abRuntime, Is.Not.Null);
            PlayModeAbProvider provider =
                abRuntime.gameObject.AddComponent<PlayModeAbProvider>();
            provider.SetInt("win_toast", WinToastConfig.ValueP20);
            provider.SetInt("pass_page", PassPageConfig.ValueG4);
            provider.SetInt("pass_text", PassTextConfig.ValueV3G3);
            provider.SetInt("fail_text", FailTextConfig.ValueRevivePromote);
            abRuntime.BindProvider(provider);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(manager, UiName.Splash,
                UiWindowState.Hidden);
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Showing);

            FindButton(manager.Get(UiName.Home), "StartBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Showing);
            UIFrameWindow gamePage = manager.Get(UiName.Game);
            GameplayManager gameplay = gamePage
                .GetComponentInChildren<GameplayManager>(true);
            Assert.That(gameplay, Is.Not.Null);
            yield return WaitForSession(gameplay, GameSessionState.Playing);

            BoardView board = gameplay.boardView;
            GameplayWinToastPresenter winToast = gamePage
                .GetComponentInChildren<GameplayWinToastPresenter>(true);
            Assert.That(winToast, Is.Not.Null);
            Assert.That(winToast.ConfigValueForTests,
                Is.EqualTo(WinToastConfig.ValueP20));
            int size = gameplay.CurrentPuzzleSize;
            string puzzleId = gameplay.PuzzleIdForTests;
            var solution = new int[size];
            var initial = new CellStateType[size][];
            int initialCats = 0;
            for (int row = 0; row < size; row++)
            {
                solution[row] = gameplay.SolutionColumnForTests(row);
                initial[row] = new CellStateType[size];
                for (int column = 0; column < size; column++)
                {
                    CellStateType state = gameplay.GetCellState(row, column);
                    initial[row][column] = state;
                    Assert.That(board.GetCellForTests(row, column).GetState(),
                        Is.EqualTo(state),
                        $"Initial model/view diverged at ({row},{column}).");
                    if (state == CellStateType.CAT) initialCats++;
                }
            }
            Assert.That(gameplay.CurrentLevelNumber, Is.EqualTo(1));
            Assert.That(initialCats, Is.EqualTo(1),
                "Source levels 1–10 start with one tutorial prefill CAT.");
            Assert.That(gameplay.RemainingCatsForTests,
                Is.EqualTo(size - initialCats));
            Assert.That(gameplay.LivesForTests, Is.EqualTo(3));
            Assert.That(gameplay.ScoreForTests, Is.Zero);
            Assert.That(gameplay.ComboForTests, Is.Zero);
            Assert.That(gameplay.MistakeCountForTests, Is.Zero);

            int failedTransitions = 0;
            int wonTransitions = 0;
            gameplay.GameTransitioned += transition =>
            {
                if (transition?.Kind == MainGameTransitionKind.Failed)
                    failedTransitions++;
                else if (transition?.Kind == MainGameTransitionKind.Won)
                    wonTransitions++;
            };

            Vector2Int correctCell = FindInputCell(gameplay, solution: true);
            Assert.That(correctCell.x, Is.GreaterThanOrEqualTo(0));
            int remainingBefore = gameplay.RemainingCatsForTests;
            SessionActionResult correct = gameplay.DoubleTapForTests(
                correctCell.x,
                correctCell.y);
            Assert.That(correct.Kind, Is.EqualTo(SessionActionKind.CorrectCat));
            Assert.That(gameplay.GetCellState(correctCell.x, correctCell.y),
                Is.EqualTo(CellStateType.CAT));
            Assert.That(board.GetCellForTests(correctCell.x, correctCell.y)
                    .GetState(),
                Is.EqualTo(CellStateType.CAT));
            Assert.That(gameplay.RemainingCatsForTests,
                Is.EqualTo(remainingBefore - 1));
            Assert.That(gameplay.ScoreForTests, Is.GreaterThan(0));
            Assert.That(gameplay.ComboForTests, Is.EqualTo(1));

            Vector2Int firstWrong = FindEmptyWrongCell(gameplay);
            SessionActionResult wrong = gameplay.DoubleTapForTests(
                firstWrong.x,
                firstWrong.y);
            Assert.That(wrong.Kind, Is.EqualTo(SessionActionKind.WrongGuess));
            Assert.That(gameplay.SessionState,
                Is.EqualTo(GameSessionState.ResolvingWrongGuess));
            Assert.That(gameplay.LivesForTests, Is.EqualTo(2));
            Assert.That(gameplay.MistakeCountForTests, Is.EqualTo(1));
            Assert.That(gameplay.ComboForTests, Is.Zero);
            Assert.That(gameplay.GetCellState(firstWrong.x, firstWrong.y),
                Is.EqualTo(CellStateType.ERROR));
            Assert.That(board.GetCellForTests(firstWrong.x, firstWrong.y)
                    .GetState(),
                Is.EqualTo(CellStateType.ERROR));
            Assert.That(gameplay.DoubleTapForTests(
                    correctCell.x,
                    correctCell.y).Accepted,
                Is.False,
                "Wrong-guess pending must reject input until resolution.");

            while (gameplay.LivesForTests > 0)
            {
                yield return WaitForSession(gameplay, GameSessionState.Playing);
                Vector2Int nextWrong = FindEmptyWrongCell(gameplay);
                SessionActionResult next = gameplay.DoubleTapForTests(
                    nextWrong.x,
                    nextWrong.y);
                Assert.That(next.Kind,
                    Is.EqualTo(SessionActionKind.WrongGuess));
            }
            yield return WaitForSession(gameplay, GameSessionState.Failed);
            yield return WaitForState(manager, UiName.Fail,
                UiWindowState.Showing);
            yield return null;
            Assert.That(failedTransitions, Is.EqualTo(1),
                "Terminal wrong guess must publish Fail exactly once.");
            Assert.That(gameplay.DoubleTapForTests(0, 0).Accepted, Is.False);

            UIFrameWindow fail = manager.Get(UiName.Fail);
            GameFailPagePresenter failPresenter =
                fail as GameFailPagePresenter;
            Assert.That(failPresenter, Is.Not.Null);
            Assert.That(failPresenter.FailTextValueForTests,
                Is.EqualTo(FailTextConfig.ValueRevivePromote),
                "Fail must reload the source game_end timing before presentation.");
            Button restart = FindButton(fail, "RestartButton", false);
            yield return WaitUntil(
                () => restart.isActiveAndEnabled && restart.interactable,
                TransitionTimeoutSeconds,
                "RestartButton did not unlock after Fail appeared.");
            restart.onClick.Invoke();
            yield return WaitForState(manager, UiName.Fail,
                UiWindowState.Hidden);
            yield return WaitForSession(gameplay, GameSessionState.Playing);
            Assert.That(gameplay.PuzzleIdForTests, Is.EqualTo(puzzleId));
            Assert.That(gameplay.RestartCountForTests, Is.EqualTo(1));
            Assert.That(gameplay.LivesForTests, Is.EqualTo(3));
            Assert.That(gameplay.ScoreForTests, Is.Zero);
            Assert.That(gameplay.ComboForTests, Is.Zero);
            Assert.That(gameplay.MistakeCountForTests, Is.Zero);
            Assert.That(gameplay.RemainingCatsForTests,
                Is.EqualTo(size - initialCats));
            for (int row = 0; row < size; row++)
            {
                Assert.That(gameplay.SolutionColumnForTests(row),
                    Is.EqualTo(solution[row]));
                for (int column = 0; column < size; column++)
                {
                    Assert.That(gameplay.GetCellState(row, column),
                        Is.EqualTo(initial[row][column]),
                        $"Restart state mismatch at ({row},{column}).");
                    Assert.That(board.GetCellForTests(row, column).GetState(),
                        Is.EqualTo(initial[row][column]),
                        $"Restart view mismatch at ({row},{column}).");
                }
            }

            SessionActionResult auto = gameplay.RunAutoComplete();
            Assert.That(auto.Accepted, Is.True);
            Assert.That(auto.IsComplete, Is.True);
            Assert.That(gameplay.SessionState, Is.EqualTo(GameSessionState.Won));
            Assert.That(gameplay.DoubleTapForTests(0, 0).Accepted, Is.False,
                "AutoComplete must lock input before visual settlement.");
            Assert.That(gameplay.ApplyCellStateForTests(
                    0,
                    0,
                    CellStateType.EMPTY),
                Is.False);
            yield return CompletePreWinMetaFlow(manager);
            yield return null;
            Assert.That(wonTransitions, Is.EqualTo(1),
                "Completion must publish Win exactly once.");
            Assert.That(winToast.TryShowCountForTests, Is.EqualTo(1),
                "A single Win transition must evaluate its configured toast once.");
            Assert.That(manager.Get(UiName.Win).WindowState,
                Is.EqualTo(UiWindowState.Showing));
            GameWinPagePresenter winPresenter =
                manager.Get(UiName.Win) as GameWinPagePresenter;
            Assert.That(winPresenter, Is.Not.Null);
            Assert.That(winPresenter.PassPageValueForTests,
                Is.EqualTo(PassPageConfig.ValueG4));
            Assert.That(winPresenter.PassTextValueForTests,
                Is.EqualTo(PassTextConfig.ValueV3G3));
        }

        [UnityTest]
        public IEnumerator PlatformSettings_TogglesAndModeLayoutReopenMatchSource()
        {
            AbConfigRuntime abRuntime = Find<AbConfigRuntime>();
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(abRuntime, Is.Not.Null);
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);

            PlayModeAbProvider provider =
                abRuntime.gameObject.AddComponent<PlayModeAbProvider>();
            provider.SetInt(
                "settings_language",
                SettingsLanguageConfig.ValuePopup);
            provider.SetInt(
                "blind_mod",
                BlindModConfig.ValueHideOnFilled);
            provider.SetInt(
                "rule_text",
                RuleTextConfig.ValueSettingEntry);
            abRuntime.BindProvider(provider);

            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish for Settings matrix.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Showing);

            UIFrameWindow home = manager.Get(UiName.Home);
            FindButton(home, "SettingsBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Setting,
                UiWindowState.Showing);
            UIFrameWindow settings = manager.Get(UiName.Setting);
            SettingsPagePresenter presenter =
                settings as SettingsPagePresenter;
            Assert.That(presenter, Is.Not.Null);
            Assert.That(presenter.IsGameMode, Is.False);

            const string box =
                "Root/Content/PanelContainer/VBoxContainer";
            Transform panel = settings.transform.Find(
                "Root/Content/PanelContainer");
            Transform grid = settings.transform.Find(box + "/GridContainer");
            Transform music = grid?.Find("MusicCtrl");
            Transform sound = grid?.Find("SoundCtrl");
            Transform people = grid?.Find("PeopleCtrl");
            Transform vibration = grid?.Find("VibrationCtrl");
            Transform optional = settings.transform.Find(
                box + "/ToggleContainer");
            Transform pattern = optional?.Find("PatternModeSwitch");
            Transform actions = settings.transform.Find(box + "/BtnContainer");
            Transform language = actions?.Find("LanguageBtn");
            Transform feedback = actions?.Find("FeedbackBtn");
            Transform howToPlay = actions?.Find("HowToPlayBtn");
            Transform restart = actions?.Find("OrangeRestartBtn");
            Transform cmp = settings.transform.Find(
                box + "/PrivacyContainer");
            Transform terms = settings.transform.Find(box + "/TermContainer");
            Transform version = settings.transform.Find(box + "/HBoxContainer");
            Transform afterActions = settings.transform.Find(box + "/Control3");
            Transform bottom = settings.transform.Find(box + "/Control6");
            Assert.That(panel, Is.Not.Null);
            Assert.That(grid, Is.Not.Null);
            Assert.That(music, Is.Not.Null);
            Assert.That(sound, Is.Not.Null);
            Assert.That(people, Is.Not.Null);
            Assert.That(vibration, Is.Not.Null);
            Assert.That(optional, Is.Not.Null);
            Assert.That(pattern, Is.Not.Null);
            Assert.That(actions, Is.Not.Null);
            Assert.That(language, Is.Not.Null);
            Assert.That(feedback, Is.Not.Null);
            Assert.That(howToPlay, Is.Not.Null);
            Assert.That(restart, Is.Not.Null);
            Assert.That(cmp, Is.Not.Null);
            Assert.That(terms, Is.Not.Null);
            Assert.That(version, Is.Not.Null);
            Assert.That(afterActions, Is.Not.Null);
            Assert.That(bottom, Is.Not.Null);

            Assert.That(music.gameObject.activeSelf, Is.False,
                "Source Settings always hides MusicCtrl.");
            Assert.That(sound.gameObject.activeSelf, Is.True);
            Assert.That(people.gameObject.activeSelf, Is.True);
            Assert.That(vibration.gameObject.activeSelf, Is.True);
            HorizontalLayoutGroup gridLayout =
                grid.GetComponent<HorizontalLayoutGroup>();
            Assert.That(gridLayout, Is.Not.Null);
            Assert.That(gridLayout.spacing, Is.EqualTo(30f));
            Assert.That(optional.gameObject.activeSelf, Is.False);
            Assert.That(pattern.gameObject.activeSelf, Is.False);
            Assert.That(language.gameObject.activeSelf, Is.True);
            Assert.That(feedback.gameObject.activeSelf, Is.True);
            Assert.That(howToPlay.gameObject.activeSelf, Is.False);
            Assert.That(restart.gameObject.activeSelf, Is.False);
            Assert.That(cmp.gameObject.activeSelf, Is.False);
            Assert.That(terms.gameObject.activeSelf, Is.True);
            Assert.That(version.gameObject.activeSelf, Is.True);
            Assert.That(afterActions.GetComponent<LayoutElement>()
                    .preferredHeight,
                Is.EqualTo(50f));
            Assert.That(bottom.GetComponent<LayoutElement>().preferredHeight,
                Is.EqualTo(30f));
            Assert.That((panel as RectTransform).anchoredPosition,
                Is.EqualTo(Vector2.zero));

            SettingsToggleView soundView =
                sound.GetComponent<SettingsToggleView>();
            SettingsToggleView vibrationView =
                vibration.GetComponent<SettingsToggleView>();
            SettingsToggleView peopleView =
                people.GetComponent<SettingsToggleView>();
            Assert.That(soundView, Is.Not.Null);
            Assert.That(vibrationView, Is.Not.Null);
            Assert.That(peopleView, Is.Not.Null);
            AssertSettingsToggle(soundView, true);
            AssertSettingsToggle(vibrationView, true);
            AssertSettingsToggle(peopleView, true);

            ClickThroughPointerPhases(soundView.Button);
            ClickThroughPointerPhases(vibrationView.Button);
            ClickThroughPointerPhases(peopleView.Button);
            Assert.That(GameStateRuntime.Current.SoundOn, Is.False);
            Assert.That(GameStateRuntime.Current.VibrationOn, Is.False);
            Assert.That(GameStateRuntime.Current.PeopleOn, Is.False);
            AssertSettingsToggle(soundView, false);
            AssertSettingsToggle(vibrationView, false);
            AssertSettingsToggle(peopleView, false);

            FindButton(settings, "CloseBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Setting,
                UiWindowState.Hidden);
            FindButton(home, "SettingsBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Setting,
                UiWindowState.Showing);
            Assert.That(manager.Get(UiName.Setting), Is.SameAs(settings),
                "Outgame Settings reopen must reuse its cached presenter.");
            AssertSettingsToggle(soundView, false);
            AssertSettingsToggle(vibrationView, false);
            AssertSettingsToggle(peopleView, false);
            FindButton(settings, "CloseBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Setting,
                UiWindowState.Hidden);

            FindButton(home, "StartBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Showing);
            UIFrameWindow game = manager.Get(UiName.Game);
            GameplayManager gameplay =
                game.GetComponentInChildren<GameplayManager>(true);
            Assert.That(gameplay, Is.Not.Null);
            yield return WaitForSession(gameplay, GameSessionState.Playing);
            FindButton(game, "SettingsBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Setting,
                UiWindowState.Showing);
            Assert.That(manager.Get(UiName.Setting), Is.SameAs(settings),
                "Game Settings must reuse the same cached source page.");
            Assert.That(presenter.IsGameMode, Is.True);

            Assert.That(music.gameObject.activeSelf, Is.False);
            Assert.That(language.gameObject.activeSelf, Is.False);
            Assert.That(cmp.gameObject.activeSelf, Is.False);
            Assert.That(terms.gameObject.activeSelf, Is.False);
            Assert.That(version.gameObject.activeSelf, Is.False);
            Assert.That(optional.gameObject.activeSelf, Is.True);
            Assert.That(pattern.gameObject.activeSelf, Is.True);
            Assert.That(howToPlay.gameObject.activeSelf, Is.True);
            Assert.That(restart.gameObject.activeSelf, Is.True);
            Assert.That(afterActions.GetComponent<LayoutElement>()
                    .preferredHeight,
                Is.Zero);
            Assert.That(bottom.GetComponent<LayoutElement>().preferredHeight,
                Is.EqualTo(90f));
            Assert.That((panel as RectTransform).anchoredPosition,
                Is.EqualTo(Vector2.zero));
            AssertSettingsToggle(soundView, false);
            AssertSettingsToggle(vibrationView, false);
            AssertSettingsToggle(peopleView, false);

            Transform patternOn = pattern.Find("Content/Switch/On");
            Transform patternOff = pattern.Find("Content/Switch/Off");
            Transform patternDot = pattern.Find("RedDot");
            Assert.That(patternOn, Is.Not.Null);
            Assert.That(patternOff, Is.Not.Null);
            Assert.That(patternDot, Is.Not.Null);
            Assert.That(patternOn.gameObject.activeSelf, Is.False);
            Assert.That(patternOff.gameObject.activeSelf, Is.True);
            Assert.That(patternDot.gameObject.activeSelf, Is.True);

            ClickThroughPointerPhases(pattern.GetComponent<Button>());
            Assert.That(GameStateRuntime.Current.PatternModeOn, Is.True);
            Assert.That(
                GameStateRuntime.Current.PatternSwitchDotDismissed,
                Is.True);
            Assert.That(patternOn.gameObject.activeSelf, Is.True);
            Assert.That(patternOff.gameObject.activeSelf, Is.False);
            Assert.That(patternDot.gameObject.activeSelf, Is.False);
            Assert.That(gameplay.boardView.PatternOnForTests, Is.True);

            FindButton(settings, "CloseBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Setting,
                UiWindowState.Hidden);
            FindButton(game, "SettingsBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Setting,
                UiWindowState.Showing);
            Assert.That(presenter.IsGameMode, Is.True);
            Assert.That(GameStateRuntime.Current.PatternModeOn, Is.True);
            Assert.That(patternOn.gameObject.activeSelf, Is.True);
            Assert.That(patternOff.gameObject.activeSelf, Is.False);
            Assert.That(patternDot.gameObject.activeSelf, Is.False);
            FindButton(settings, "CloseBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Setting,
                UiWindowState.Hidden);

            FindButton(game, "BackBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Hidden);
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Showing);
            Assert.That(manager.IsAnyLoading, Is.False);
        }

        [UnityTest]
        public IEnumerator PlatformWinPage_PassPageAndPassTextVariantsReopenCleanly()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            AbConfigRuntime abRuntime = Find<AbConfigRuntime>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            Assert.That(abRuntime, Is.Not.Null);

            PlayModeAbProvider provider =
                abRuntime.gameObject.AddComponent<PlayModeAbProvider>();
            provider.SetInt("daily_streak", DailyStreakConfig.ValueControl);
            provider.SetInt("win_toast", WinToastConfig.ValueControl);
            provider.SetInt("pass_page", PassPageConfig.ValueControl);
            provider.SetInt("pass_text", PassTextConfig.ValueControl);
            abRuntime.BindProvider(provider);

            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish for Win page matrix.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Showing);

            int[] passPages =
            {
                PassPageConfig.ValueControl,
                PassPageConfig.ValueG1,
                PassPageConfig.ValueG2,
                PassPageConfig.ValueG4,
                PassPageConfig.ValueG1,
                PassPageConfig.ValueG2
            };
            int[] passTexts =
            {
                PassTextConfig.ValueControl,
                PassTextConfig.ValueBeatPercent,
                PassTextConfig.ValueV2,
                PassTextConfig.ValueV3G1,
                PassTextConfig.ValueV3G2,
                PassTextConfig.ValueV3G3
            };
            var transition = new MainGameTransitionData
            {
                Kind = MainGameTransitionKind.Won,
                Level = 1,
                CurrentLevelAfter = 2,
                Lives = 2,
                MistakeCount = 2,
                FinalScore = 12345,
                MaxCombo = 7,
                Size = 6,
                ElapsedSeconds = 65.2f,
                CompletionRate = 87,
                ToolsUsed = 3,
                StepsUsed = 20
            };
            GameWinPagePresenter cached = null;

            for (int index = 0; index < passPages.Length; index++)
            {
                int passPage = passPages[index];
                int passText = passTexts[index];
                provider.SetInt("pass_page", passPage);
                provider.SetInt("pass_text", passText);
                abRuntime.ReloadTiming(AbConfigTiming.GameStart);
                GameStateRuntime.Current.SetLastWinBeatPercent(-1f);

                bool panelVariant = passPage == PassPageConfig.ValueG1 ||
                                    passPage == PassPageConfig.ValueG2;

                UIFrameWindow shown = manager.Show(
                    UiName.Win,
                    new Dictionary<string, object>(1)
                    {
                        ["transition"] = transition
                    });
                Assert.That(shown, Is.Not.Null);
                if (panelVariant)
                {
                    Button openingNext = shown.transform.Find(
                            "Root/PassPanel/Actions/Next")
                        .GetComponent<Button>();
                    Assert.That(openingNext.interactable, Is.False,
                        "Pass page CTA must lock when its source appear starts.");
                }
                yield return WaitForState(manager, UiName.Win,
                    UiWindowState.Showing);
                GameWinPagePresenter presenter =
                    manager.Get(UiName.Win) as GameWinPagePresenter;
                Assert.That(presenter, Is.Not.Null);
                if (cached == null) cached = presenter;
                else Assert.That(presenter, Is.SameAs(cached),
                    "Win variants must reuse the cached source page.");
                Assert.That(presenter.PassPageValueForTests,
                    Is.EqualTo(passPage));
                Assert.That(presenter.PassTextValueForTests,
                    Is.EqualTo(passText));

                Transform root = presenter.transform.Find("Root");
                Transform visuals = presenter.transform.Find("Root/Visuals");
                Transform content = presenter.transform.Find("Root/Content");
                Transform body = presenter.transform.Find("Root/Content/Body");
                Transform defaultStatistics = presenter.transform.Find(
                    "Root/Content/Statistics");
                Transform passPanel = presenter.transform.Find(
                    "Root/PassPanel");
                Transform popup = presenter.transform.Find(
                    "Root/PassPanel/Popup");
                Transform passStatistics = presenter.transform.Find(
                    "Root/PassPanel/Popup/Statistics");
                Transform extraStatistics = presenter.transform.Find(
                    "Root/PassPanel/Popup/ExtraStatistics");
                Transform praise = presenter.transform.Find(
                    "Root/PassPanel/Praise");
                Assert.That(root, Is.Not.Null);
                Assert.That(visuals, Is.Not.Null);
                Assert.That(content, Is.Not.Null);
                Assert.That(body, Is.Not.Null);
                Assert.That(defaultStatistics, Is.Not.Null);
                Assert.That(passPanel, Is.Not.Null);
                Assert.That(popup, Is.Not.Null);
                Assert.That(passStatistics, Is.Not.Null);
                Assert.That(extraStatistics, Is.Not.Null);
                Assert.That(praise, Is.Not.Null);

                Assert.That(passPanel.gameObject.activeSelf,
                    Is.EqualTo(panelVariant));
                Assert.That(visuals.gameObject.activeSelf,
                    Is.EqualTo(!panelVariant));
                Assert.That(content.gameObject.activeSelf,
                    Is.EqualTo(!panelVariant));

                if (panelVariant)
                {
                    Assert.That(extraStatistics.gameObject.activeSelf,
                        Is.EqualTo(passPage == PassPageConfig.ValueG2));
                    Assert.That(
                        passStatistics.Find("Row1/Value")
                            .GetComponent<Text>().text,
                        Is.EqualTo("6×6"));
                    Assert.That(
                        passStatistics.Find("Row2/Value")
                            .GetComponent<Text>().text,
                        Is.EqualTo("01:06"));
                    Assert.That(
                        passStatistics.Find("Row3/Value")
                            .GetComponent<Text>().text,
                        Is.EqualTo("12,345"));
                    Assert.That(
                        passStatistics.Find("Row4/Value")
                            .GetComponent<Text>().text,
                        Is.EqualTo("7"));

                    RectTransform popupRect = popup as RectTransform;
                    Assert.That(popupRect, Is.Not.Null);
                    bool group2 = passPage == PassPageConfig.ValueG2;
                    Assert.That(popupRect.anchoredPosition,
                        Is.EqualTo(new Vector2(0f, group2 ? 366f : 372f)));
                    Assert.That(popupRect.sizeDelta,
                        Is.EqualTo(new Vector2(
                            900f,
                            group2 ? 1072f : 912f)));
                    if (group2)
                    {
                        Assert.That(extraStatistics.Find(
                                "CompletionRate/Value")
                                .GetComponent<Text>().text,
                            Is.EqualTo("87%"));
                        Assert.That(extraStatistics.Find(
                                "MistakeCount/Value")
                                .GetComponent<Text>().text,
                            Is.EqualTo("2"));
                        Assert.That(extraStatistics.Find(
                                "ToolsUsed/Value")
                                .GetComponent<Text>().text,
                            Is.EqualTo("3"));
                    }

                    Text praiseText = praise.GetComponent<Text>();
                    Assert.That(praiseText, Is.Not.Null);
                    Assert.That(praise.gameObject.activeSelf,
                        Is.EqualTo(passText != PassTextConfig.ValueControl));
                    if (passText != PassTextConfig.ValueControl)
                    {
                        Assert.That(praiseText.text, Is.Not.Empty);
                        Assert.That(praiseText.text, Does.Not.Contain("[center]"));
                        Assert.That(praiseText.text, Does.Not.Contain("[font_size"));
                    }
                    if (passText == PassTextConfig.ValueBeatPercent)
                    {
                        Assert.That(praiseText.text,
                            Does.Contain("#F19320"));
                        Assert.That(praiseText.text,
                            Does.Not.Contain("#02BE52"));
                    }

                    Button panelNext = presenter.transform.Find(
                            "Root/PassPanel/Actions/Next")
                        .GetComponent<Button>();
                    yield return WaitUntil(
                        () => panelNext.interactable,
                        1f,
                        "Pass page CTA did not unlock after its source marker.");
                }
                else
                {
                    Assert.That(defaultStatistics.gameObject.activeSelf,
                        Is.EqualTo(passPage == PassPageConfig.ValueG4));
                    Assert.That(body.gameObject.activeSelf,
                        Is.EqualTo(passText != PassTextConfig.ValueControl));
                    if (passText != PassTextConfig.ValueControl)
                    {
                        Text bodyText = body.GetComponentInChildren<Text>(true);
                        Assert.That(bodyText.text, Is.Not.Empty);
                        Assert.That(bodyText.text, Does.Not.Contain("[center]"));
                        Assert.That(bodyText.text, Does.Not.Contain("[font_size"));
                    }
                    if (passPage == PassPageConfig.ValueG4)
                    {
                        CanvasGroup statsGroup =
                            defaultStatistics.GetComponent<CanvasGroup>();
                        Assert.That(statsGroup, Is.Not.Null);
                        Assert.That(statsGroup.alpha, Is.Zero.Within(0.001f));
                        yield return new WaitForSecondsRealtime(1.75f);
                        Assert.That(statsGroup.alpha, Is.EqualTo(1f).Within(0.01f));
                        Assert.That(defaultStatistics.Find("Time")
                                .GetComponent<Text>().text,
                            Does.EndWith("01:06"));
                        Assert.That(defaultStatistics.Find("Score")
                                .GetComponent<Text>().text,
                            Does.EndWith("12,345"));
                        Assert.That(defaultStatistics.Find("Combo")
                                .GetComponent<Text>().text,
                            Does.EndWith("7"));
                    }
                }

                Assert.That(manager.RequestBack(), Is.True,
                    "Win page must consume Back for every source variant.");
                yield return null;
                Assert.That(IsShowing(manager, UiName.Win), Is.True);
                manager.Hide(UiName.Win);
                yield return WaitForState(manager, UiName.Win,
                    UiWindowState.Hidden);
            }

            Assert.That(manager.IsAnyLoading, Is.False);
        }

        [UnityTest]
        public IEnumerator PlatformWinToast_ConfiguredAndControlBranchesUseSourceDelays()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            AbConfigRuntime abRuntime = Find<AbConfigRuntime>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            Assert.That(abRuntime, Is.Not.Null);

            PlayModeAbProvider provider =
                abRuntime.gameObject.AddComponent<PlayModeAbProvider>();
            provider.SetInt("daily_streak", DailyStreakConfig.ValueControl);
            provider.SetInt("win_toast", WinToastConfig.ValueP20);
            abRuntime.BindProvider(provider);

            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish for Win Toast coverage.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(manager, UiName.Home, UiWindowState.Showing);

            GameStateRuntime.Current.SetCurrentLevel(11);
            HomePagePresenter home = manager.Get(UiName.Home) as HomePagePresenter;
            Assert.That(home, Is.Not.Null);
            home.RefreshPresentation();
            FindButton(home, "StartBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Game, UiWindowState.Showing);

            UIFrameWindow game = manager.Get(UiName.Game);
            GameplayManager gameplay =
                game.GetComponentInChildren<GameplayManager>(true);
            GameplayWinToastPresenter toast =
                game.GetComponentInChildren<GameplayWinToastPresenter>(true);
            Assert.That(gameplay, Is.Not.Null);
            Assert.That(toast, Is.Not.Null);
            yield return WaitForSession(gameplay, GameSessionState.Playing);
            Assert.That(gameplay.CurrentLevelNumber, Is.EqualTo(11));
            Assert.That(gameplay.CurrentPuzzleSize, Is.EqualTo(6));
            Assert.That(toast.ConfigValueForTests,
                Is.EqualTo(WinToastConfig.ValueP20));

            MainGameTransitionData firstWin = null;
            int winTransitionCount = 0;
            float enabledCompletedAt = 0f;
            float controlCompletedAt = 0f;
            gameplay.GameTransitioned += transition =>
            {
                if (transition?.Kind != MainGameTransitionKind.Won) return;
                winTransitionCount++;
                if (winTransitionCount == 1)
                {
                    firstWin = transition;
                    enabledCompletedAt = Time.realtimeSinceStartup;
                }
                else if (winTransitionCount == 2)
                {
                    controlCompletedAt = Time.realtimeSinceStartup;
                }
            };
            for (int row = 0; row < gameplay.CurrentPuzzleSize; row++)
            {
                Assert.That(gameplay.GetCellState(
                        row,
                        gameplay.SolutionColumnForTests(row)),
                    Is.EqualTo(CellStateType.EMPTY),
                    "Level 11 fixture unexpectedly contains a prefilled cat.");
                SessionActionResult action = gameplay.DoubleTapForTests(
                    row,
                    gameplay.SolutionColumnForTests(row));
                Assert.That(action.Accepted, Is.True);
            }

            yield return WaitUntil(
                () => firstWin != null,
                TransitionTimeoutSeconds,
                "Level 11 did not publish its Win transition.");
            Assert.That(firstWin, Is.Not.Null);
            Assert.That(firstWin.StepsUsed, Is.EqualTo(6));
            Assert.That(WinToastTierContract.DetermineTier(
                    firstWin.Size,
                    firstWin.StepsUsed),
                Is.EqualTo(WinToastTierContract.TierPerfect));
            Assert.That(toast.TryShowCountForTests, Is.EqualTo(1));
            Assert.That(toast.IsVisibleForTests, Is.True,
                "Enabled Win Toast did not become visible before Win.");
            Assert.That(toast.MessageForTests, Does.Not.Contain("[b]"));
            Assert.That(toast.MessageForTests, Does.Not.Contain("[/b]"));
            Assert.That(toast.TierIconForTests, Is.Not.Null);
            Assert.That(IsShowing(manager, UiName.Win), Is.False);

            yield return new WaitForSecondsRealtime(1.0f);
            Assert.That(IsShowing(manager, UiName.Win), Is.False,
                "Win appeared before the source 1.5 second toast delay.");
            Assert.That(toast.IsVisibleForTests, Is.True);
            yield return WaitForState(manager, UiName.Win,
                UiWindowState.Showing,
                3f);
            Assert.That(
                Time.realtimeSinceStartup - enabledCompletedAt,
                Is.GreaterThanOrEqualTo(1.4f));
            yield return WaitUntil(
                () => !toast.IsVisibleForTests,
                1f,
                "Win Toast did not finish its source hide lifecycle.");

            provider.SetInt("win_toast", WinToastConfig.ValueControl);
            UIFrameWindow firstWinPage = manager.Get(UiName.Win);
            Button next = FindActiveButton(
                firstWinPage,
                "Next",
                requireInteractable: false);
            yield return WaitUntil(
                () => next.interactable,
                TransitionTimeoutSeconds,
                "Win Next did not unlock for the control branch.");
            next.onClick.Invoke();
            yield return WaitForState(manager, UiName.Win, UiWindowState.Hidden);
            yield return WaitUntil(
                () => gameplay.CurrentLevelNumber == 12 &&
                      gameplay.SessionState == GameSessionState.Playing,
                10f,
                "Next did not load level 12 for the control branch.");
            Assert.That(gameplay.CurrentPuzzleSize, Is.EqualTo(6));
            Assert.That(toast.ConfigValueForTests,
                Is.EqualTo(WinToastConfig.ValueControl));

            for (int row = 0; row < gameplay.CurrentPuzzleSize; row++)
            {
                SessionActionResult action = gameplay.DoubleTapForTests(
                    row,
                    gameplay.SolutionColumnForTests(row));
                Assert.That(action.Accepted, Is.True);
            }
            yield return WaitUntil(
                () => winTransitionCount == 2,
                TransitionTimeoutSeconds,
                "Level 12 did not publish its Win transition.");
            Assert.That(toast.TryShowCountForTests, Is.EqualTo(2));
            Assert.That(toast.IsVisibleForTests, Is.False,
                "Control Win Toast variant must remain hidden.");
            Assert.That(IsShowing(manager, UiName.Win), Is.False);
            yield return new WaitForSecondsRealtime(0.9f);
            Assert.That(IsShowing(manager, UiName.Win), Is.False,
                "Control branch skipped the source 1.2 second Win delay.");
            yield return WaitForState(manager, UiName.Win,
                UiWindowState.Showing,
                2f);
            Assert.That(
                Time.realtimeSinceStartup - controlCompletedAt,
                Is.GreaterThanOrEqualTo(1.1f));
            Assert.That(manager.IsAnyLoading, Is.False);
        }

        [UnityTest]
        public IEnumerator PlatformProfile_PendingCloseConfirmLockedAndRedDotReopenMatchSource()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            ProfileRuntime runtime = Find<ProfileRuntime>();
            RankActivityRuntime rankRuntime = Find<RankActivityRuntime>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            Assert.That(runtime, Is.Not.Null);
            Assert.That(rankRuntime, Is.Not.Null);

            var data = new ProfileData
            {
                Nickname = "CAT001",
                AvatarId = 1,
                FrameId = 1,
                Initialized = true
            };
            foreach (int id in ProfileCatalog.ClassicFrameIds)
                data.OwnedFrames[id] = new AvatarFrame(id, -1);
            var service = new ProfileService(
                new MemoryProfileDataStore(data));
            runtime.ConfigureForTests(service);
            var rankTime = new MutableRobotTime { UnixNow = 2_000_000 };
            var rank = new RankActivityManager(
                new MemoryRankActivityStore(),
                new RobotService(
                    new MemoryRobotPoolStore(),
                    rankTime,
                    new SystemRobotRandomFactory()),
                service,
                new AwardManager(GameStateRuntime.Current, service),
                new RankEnvironment(),
                rankTime,
                new SystemRobotRandomFactory());
            rankRuntime.ConfigureForTests(rank);
            Assert.That(rank.MaybeOpen(true), Is.True);
            rank.ConfirmParticipation();

            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Showing);

            ProfilePagePresenter page = manager.Show(UiName.Profile) as
                ProfilePagePresenter;
            Assert.That(page, Is.Not.Null);
            yield return WaitForState(manager, UiName.Profile,
                UiWindowState.Showing);
            yield return null;
            Assert.That(page.CellCountForTests, Is.EqualTo(8));
            Assert.That(page.PendingAvatarIdForTests, Is.EqualTo(1));
            Assert.That(page.PendingFrameIdForTests, Is.EqualTo(1));
            Assert.That(page.ScrollViewportPositionForTests,
                Is.EqualTo(new Vector2(71f, -546f)));
            Assert.That(page.ScrollViewportSizeForTests,
                Is.EqualTo(new Vector2(758f, 396f)),
                "Avatar tab must extend the source scroll bottom by 20 px.");

            ClickProfileCell(FindProfileCell(page, 2));
            Assert.That(page.PendingAvatarIdForTests, Is.EqualTo(2));
            Assert.That(service.AvatarId, Is.EqualTo(1),
                "Avatar selection must remain pending until Confirm.");
            FindButton(page, "CloseBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Profile,
                UiWindowState.Hidden);
            Assert.That(service.AvatarId, Is.EqualTo(1),
                "Close must discard the pending avatar selection.");
            Assert.That(page.CellCountForTests, Is.EqualTo(0),
                "Profile cells must be released when the pooled page hides.");

            Assert.That(manager.Show(UiName.Profile), Is.SameAs(page));
            yield return WaitForState(manager, UiName.Profile,
                UiWindowState.Showing);
            yield return null;
            Assert.That(page.PendingAvatarIdForTests, Is.EqualTo(1));
            Assert.That(page.CellCountForTests, Is.EqualTo(8));
            ClickProfileCell(FindProfileCell(page, 2));

            FindButton(page, "RenameBtn").onClick.Invoke();
            InputField nickname = page.GetComponentInChildren<InputField>(true);
            Assert.That(nickname, Is.Not.Null);
            Assert.That(nickname.readOnly, Is.False);
            nickname.text = "  NEWCAT  ";

            FindProfileTabButton(page, "FrameTab").onClick.Invoke();
            yield return null;
            Assert.That(page.ShowingFramesForTests, Is.True);
            Assert.That(page.CellCountForTests, Is.EqualTo(9));
            Assert.That(page.ScrollViewportPositionForTests,
                Is.EqualTo(new Vector2(71f, -556f)));
            Assert.That(page.ScrollViewportSizeForTests,
                Is.EqualTo(new Vector2(758f, 376f)),
                "Frame tab must move the source viewport down by 10 px.");

            ClickProfileCell(FindProfileCell(page, 100));
            Assert.That(page.PendingFrameIdForTests, Is.EqualTo(1),
                "A locked leaderboard frame must not become pending.");
            Assert.That(page.LockTipVisibleForTests, Is.True,
                "Locked frame did not open the source tooltip.");
            Assert.That(page.LockTipGoVisibleForTests, Is.True,
                "Running Rank must expose the source GO action.");
            FindButton(page, "Catcher").onClick.Invoke();
            yield return WaitUntil(
                () => !page.LockTipVisibleForTests,
                1f,
                "Locked-frame tooltip did not finish closing.");

            ClickProfileCell(FindProfileCell(page, 2));
            Assert.That(page.PendingFrameIdForTests, Is.EqualTo(2));
            Assert.That(service.FrameId, Is.EqualTo(1),
                "Frame selection must remain pending until Confirm.");
            FindButton(page, "Confirm").onClick.Invoke();
            yield return WaitForState(manager, UiName.Profile,
                UiWindowState.Hidden);
            Assert.That(service.Nickname, Is.EqualTo("NEWCAT"));
            Assert.That(service.AvatarId, Is.EqualTo(2));
            Assert.That(service.FrameId, Is.EqualTo(2));

            Assert.That(manager.Show(
                    UiName.Profile,
                    new Dictionary<string, object>
                    {
                        ["from_rank_open_guide"] = true
                    }),
                Is.SameAs(page));
            yield return WaitForState(manager, UiName.Profile,
                UiWindowState.Showing);
            FindProfileTabButton(page, "FrameTab").onClick.Invoke();
            yield return null;
            ClickProfileCell(FindProfileCell(
                page,
                ProfileCatalog.FirstPlaceFrameId));
            Assert.That(page.LockTipVisibleForTests, Is.True);
            Assert.That(page.LockTipGoVisibleForTests, Is.False,
                "Profile opened by the Rank guide must suppress recursive GO.");
            FindButton(page, "Catcher").onClick.Invoke();
            yield return WaitUntil(
                () => !page.LockTipVisibleForTests,
                1f,
                "Guide tooltip did not finish closing.");
            FindButton(page, "CloseBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Profile,
                UiWindowState.Hidden);

            Assert.That(manager.Show(UiName.Profile), Is.SameAs(page));
            yield return WaitForState(manager, UiName.Profile,
                UiWindowState.Showing);
            FindProfileTabButton(page, "FrameTab").onClick.Invoke();
            yield return null;
            ClickProfileCell(FindProfileCell(
                page,
                ProfileCatalog.FirstPlaceFrameId));
            Assert.That(page.LockTipGoVisibleForTests, Is.True);
            FindButton(page, "GoBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Profile,
                UiWindowState.Hidden);
            yield return WaitForState(manager, UiName.RankActivityPage,
                UiWindowState.Showing);
            FindButton(manager.Get(UiName.RankActivityPage), "BackBtn")
                .onClick.Invoke();
            yield return WaitForState(manager, UiName.RankActivityPage,
                UiWindowState.Hidden);

            Assert.That(service.GrantFrame(
                ProfileCatalog.FirstPlaceFrameId), Is.True);
            Assert.That(service.HasFrameRedDot, Is.True);
            Assert.That(manager.Show(UiName.Profile), Is.SameAs(page));
            yield return WaitForState(manager, UiName.Profile,
                UiWindowState.Showing);
            yield return null;
            Assert.That(page.FrameRedDotVisibleForTests, Is.True);
            FindProfileTabButton(page, "FrameTab").onClick.Invoke();
            yield return null;
            Assert.That(service.HasFrameRedDot, Is.False,
                "Opening the Frame tab must clear the source red dot.");
            Assert.That(page.FrameRedDotVisibleForTests, Is.False);

            ClickProfileCell(FindProfileCell(
                page,
                ProfileCatalog.FirstPlaceFrameId));
            Assert.That(page.PendingFrameIdForTests,
                Is.EqualTo(ProfileCatalog.FirstPlaceFrameId));
            Assert.That(page.LockTipVisibleForTests, Is.False);
            FindButton(page, "Confirm").onClick.Invoke();
            yield return WaitForState(manager, UiName.Profile,
                UiWindowState.Hidden);
            Assert.That(service.FrameId,
                Is.EqualTo(ProfileCatalog.FirstPlaceFrameId));

            Assert.That(manager.Show(UiName.Profile), Is.SameAs(page));
            yield return WaitForState(manager, UiName.Profile,
                UiWindowState.Showing);
            yield return null;
            Assert.That(page.PendingAvatarIdForTests, Is.EqualTo(2));
            Assert.That(page.PendingFrameIdForTests,
                Is.EqualTo(ProfileCatalog.FirstPlaceFrameId));
            Assert.That(page.CellCountForTests, Is.EqualTo(8),
                "Reopen must rebuild one avatar grid without duplicates.");
            FindButton(page, "CloseBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Profile,
                UiWindowState.Hidden);
            Assert.That(manager.IsAnyLoading, Is.False);
        }

        [UnityTest]
        public IEnumerator PlatformAward_FastCollectForcedHideAndReopenPersistExactlyOnce()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            DailyMetaRuntime dailyRuntime = Find<DailyMetaRuntime>();
            ProfileRuntime profileRuntime = Find<ProfileRuntime>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            Assert.That(dailyRuntime, Is.Not.Null);
            Assert.That(profileRuntime, Is.Not.Null);

            var profileData = new ProfileData
            {
                Nickname = "CAT001",
                AvatarId = 1,
                FrameId = 1,
                Initialized = true
            };
            foreach (int id in ProfileCatalog.ClassicFrameIds)
                profileData.OwnedFrames[id] = new AvatarFrame(id, -1);
            var profile = new ProfileService(
                new MemoryProfileDataStore(profileData));
            profileRuntime.ConfigureForTests(profile);
            var awards = new AwardManager(
                GameStateRuntime.Current,
                profile);
            dailyRuntime.ConfigureForTests(dailyRuntime.Streak, awards);

            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Showing);

            int initialHint = GameStateRuntime.Current.GetToolCount("hint");
            int initialFrame = profile.GetFrameCount(
                ProfileCatalog.FirstPlaceFrameId);
            int endedCount = 0;
            int callbackCount = 0;
            awards.AwardEnded += _ => endedCount++;

            int mixedUid = awards.Dispatch(
                new[]
                {
                    AwardItem.Tool("hint", 2),
                    AwardItem.Frame(ProfileCatalog.FirstPlaceFrameId)
                },
                AwardDisplayType.StreakGift,
                AwardManager.StreakChestReason,
                AwardManager.StreakRewardAdReason);
            Assert.That(mixedUid, Is.GreaterThan(0));
            Assert.That(awards.DoubleAward(mixedUid), Is.True);
            Assert.That(awards.ContinueWhenAwardEnd(
                mixedUid,
                _ => callbackCount++), Is.True);
            Assert.That(awards.ShowAward(mixedUid), Is.True);
            yield return WaitForState(manager, UiName.Award,
                UiWindowState.Showing);

            UIFrameWindow page = manager.Get(UiName.Award);
            Button collect = FindActiveButton(
                page,
                "CollectBtn",
                requireInteractable: false);
            Assert.That(collect.interactable, Is.False,
                "Award input must remain gated during source Appear.");
            ClickThroughPointerPhases(collect);
            ClickThroughPointerPhases(collect);
            yield return null;
            Assert.That(IsShowing(manager, UiName.Award), Is.True);
            Assert.That(GameStateRuntime.Current.GetToolCount("hint"),
                Is.EqualTo(initialHint));
            Assert.That(profile.GetFrameCount(
                    ProfileCatalog.FirstPlaceFrameId),
                Is.EqualTo(initialFrame));
            Assert.That(manager.RequestBack(), Is.True,
                "Award must consume Back like the source page.");
            yield return null;
            Assert.That(IsShowing(manager, UiName.Award), Is.True,
                "Back must not dismiss Award.");

            yield return WaitUntil(
                () => collect.interactable,
                5f,
                "Award CollectBtn did not unlock.");
            ClickThroughPointerPhases(collect);
            collect.onClick.Invoke();
            FrameAwardEffectView effect =
                page.GetComponentInChildren<FrameAwardEffectView>(true);
            Assert.That(effect, Is.Not.Null);
            Assert.That(effect.IsPlaying, Is.True,
                "Mixed Award did not enter its frame effect.");
            Assert.That(GameStateRuntime.Current.GetInFlightAwards(),
                Has.Count.EqualTo(1),
                "Award must remain durable until the page hides.");
            Assert.That(GameStateRuntime.Current.GetToolCount("hint"),
                Is.EqualTo(initialHint));
            Assert.That(profile.GetFrameCount(
                    ProfileCatalog.FirstPlaceFrameId),
                Is.EqualTo(initialFrame));

            manager.Hide(UiName.Award);
            manager.Hide(UiName.Award);
            yield return WaitForState(manager, UiName.Award,
                UiWindowState.Hidden,
                10f);
            Assert.That(GameStateRuntime.Current.GetToolCount("hint"),
                Is.EqualTo(initialHint + 4),
                "Double Award must duplicate only its tool item.");
            Assert.That(profile.GetFrameCount(
                    ProfileCatalog.FirstPlaceFrameId),
                Is.EqualTo(initialFrame + 1),
                "Double Award must grant its frame exactly once.");
            Assert.That(GameStateRuntime.Current.GetInFlightAwards(), Is.Empty);
            Assert.That(awards.ActiveRenderCount, Is.EqualTo(0));
            Assert.That(endedCount, Is.EqualTo(1));
            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(awards.CompleteAward(mixedUid), Is.False,
                "A completed Award must reject duplicate persistence.");

            yield return new WaitForSecondsRealtime(2f);
            Assert.That(GameStateRuntime.Current.GetToolCount("hint"),
                Is.EqualTo(initialHint + 4),
                "A cancelled frame callback granted the Award again.");
            Assert.That(profile.GetFrameCount(
                    ProfileCatalog.FirstPlaceFrameId),
                Is.EqualTo(initialFrame + 1));
            Assert.That(endedCount, Is.EqualTo(1));
            Assert.That(callbackCount, Is.EqualTo(1));

            int locateBefore = GameStateRuntime.Current.GetToolCount("locate");
            int secondUid = awards.Dispatch(
                new[] { AwardItem.Tool("locate", 1) },
                AwardDisplayType.StreakGift,
                AwardManager.StreakChestReason);
            Assert.That(secondUid, Is.GreaterThan(0));
            Assert.That(awards.ContinueWhenAwardEnd(
                secondUid,
                _ => callbackCount++), Is.True);
            Assert.That(awards.ShowAward(secondUid), Is.True);
            yield return WaitForState(manager, UiName.Award,
                UiWindowState.Showing);
            Assert.That(manager.Get(UiName.Award), Is.SameAs(page),
                "Award reopen must reuse its cached presenter.");
            Button secondCollect = FindActiveButton(
                page,
                "CollectBtn",
                requireInteractable: false);
            yield return WaitUntil(
                () => secondCollect.interactable,
                5f,
                "Reopened Award CollectBtn did not unlock.");
            ClickThroughPointerPhases(secondCollect);
            secondCollect.onClick.Invoke();
            yield return WaitForState(manager, UiName.Award,
                UiWindowState.Hidden,
                10f);
            Assert.That(GameStateRuntime.Current.GetToolCount("locate"),
                Is.EqualTo(locateBefore + 1));
            Assert.That(awards.ActiveRenderCount, Is.EqualTo(0));
            Assert.That(GameStateRuntime.Current.GetInFlightAwards(), Is.Empty);
            Assert.That(endedCount, Is.EqualTo(2));
            Assert.That(callbackCount, Is.EqualTo(2));
            Assert.That(manager.IsAnyLoading, Is.False);
        }

        [UnityTest]
        public IEnumerator PlatformToolBar_SourceBadgeClickAndPulseStayCoherent()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            AbConfigRuntime abRuntime = Find<AbConfigRuntime>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            Assert.That(abRuntime, Is.Not.Null);
            PlayModeAbProvider provider =
                abRuntime.gameObject.AddComponent<PlayModeAbProvider>();
            provider.SetInt("reward_unlock_level", 8);
            provider.SetInt(
                "prop_highlight",
                PropHighlightConfig.ValueLocateOnce);
            abRuntime.BindProvider(provider);

            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(manager, UiName.Splash,
                UiWindowState.Hidden);
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Showing);
            FindButton(manager.Get(UiName.Home), "StartBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Showing);

            UIFrameWindow gamePage = manager.Get(UiName.Game);
            GameplayManager gameplay = gamePage
                .GetComponentInChildren<GameplayManager>(true);
            GameplayToolBarPresenter toolBar = gamePage
                .GetComponentInChildren<GameplayToolBarPresenter>(true);
            Assert.That(gameplay, Is.Not.Null);
            Assert.That(toolBar, Is.Not.Null);
            Assert.That(toolBar.transform.parent.name, Is.EqualTo("HUD"));
            Assert.That(toolBar.name, Is.EqualTo("BottomTools"));
            yield return WaitForSession(gameplay, GameSessionState.Playing);

            ToolButtonView locate = toolBar.LocateButtonForTests;
            ToolButtonView hint = toolBar.HintButtonForTests;
            Assert.That(locate, Is.Not.Null);
            Assert.That(hint, Is.Not.Null);
            Assert.That(locate.transform.parent, Is.EqualTo(toolBar.transform));
            Assert.That(hint.transform.parent, Is.EqualTo(toolBar.transform));
            Assert.That(locate.transform.Find("Visual/Background"), Is.Not.Null);
            Assert.That(hint.transform.Find("Visual/Background"), Is.Not.Null);
            Assert.That(locate.transform.Find("Background"), Is.Null,
                "Source tool_loop scales Control/Visual, including its background.");
            Assert.That(locate.State, Is.EqualTo(ToolButtonVisualState.Free));
            Assert.That(hint.State, Is.EqualTo(ToolButtonVisualState.Free));

            GameStateService state = GameStateRuntime.Current;
            abRuntime.Gameplay.RewardUnlockLevel.SetDebugOverride(1);
            state.SetToolCount("locate", 2);
            state.SetToolCount("hint", 3);
            Assert.That(locate.State,
                Is.EqualTo(ToolButtonVisualState.HasTool));
            Assert.That(locate.BadgeCount, Is.EqualTo(2));
            Assert.That(hint.State,
                Is.EqualTo(ToolButtonVisualState.HasTool));
            Assert.That(hint.BadgeCount, Is.EqualTo(3));

            locate.GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.That(state.GetToolCount("locate"), Is.EqualTo(1));
            Assert.That(locate.BadgeCount, Is.EqualTo(1));
            Assert.That(locate.State,
                Is.EqualTo(ToolButtonVisualState.HasTool));

            FindButton(gamePage, "BackBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Hidden);
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Showing);
            FindButton(manager.Get(UiName.Home), "StartBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Showing);
            gamePage = manager.Get(UiName.Game);
            gameplay = gamePage.GetComponentInChildren<GameplayManager>(true);
            toolBar = gamePage
                .GetComponentInChildren<GameplayToolBarPresenter>(true);
            yield return WaitForSession(gameplay, GameSessionState.Playing);
            locate = toolBar.LocateButtonForTests;
            hint = toolBar.HintButtonForTests;
            Assert.That(state.GetToolCount("locate"), Is.EqualTo(1));
            Assert.That(locate.BadgeCount, Is.EqualTo(1));
            Assert.That(locate.State,
                Is.EqualTo(ToolButtonVisualState.HasTool));

            state.SetToolCount("locate", 0);
            Assert.That(locate.State,
                Is.EqualTo(ToolButtonVisualState.NoTool));
            Assert.That(locate.BadgeCount, Is.Zero);

            Assert.That(gameplay.PlayIdleToolHintForTests(GameToolKind.Hint),
                Is.True);
            Assert.That(hint.IsIdlePulsePlaying, Is.True);
            yield return null;
            gameplay.StopIdleToolHintForTests(GameToolKind.Hint);
            Assert.That(hint.IsIdlePulsePlaying, Is.False);
            Assert.That(hint.transform.localScale, Is.EqualTo(Vector3.one));
        }

        [UnityTest]
        public IEnumerator PlatformLevelSelection_CrossLevelDuplicateRetriesOnceThenAcceptsFallback()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(
                manager,
                UiName.Splash,
                UiWindowState.Hidden);
            yield return WaitForState(
                manager,
                UiName.Home,
                UiWindowState.Showing);

            GameStateService state = GameStateRuntime.Current;
            Assert.That(state.CurrentLevel, Is.EqualTo(3));
            Assert.That(state.GetRecentPuzzles().Count, Is.EqualTo(2));
            UIFrameWindow home = manager.Get(UiName.Home);
            FindButton(home, "StartBtn").onClick.Invoke();

            yield return WaitForState(
                manager,
                UiName.Game,
                UiWindowState.Showing);
            GameplayManager gameplay = manager.Get(UiName.Game)
                .GetComponentInChildren<GameplayManager>(true);
            Assert.That(gameplay, Is.Not.Null);
            yield return WaitUntil(
                () => gameplay.SessionState == GameSessionState.Playing,
                TransitionTimeoutSeconds,
                "Dedup retry level did not reach Playing state.");

            string firstId = LevelData.ComputePuzzleId(
                6,
                DedupRegionMap(0));
            string retriedId = LevelData.ComputePuzzleId(
                6,
                DedupRegionMap(2));
            Assert.That(retriedId, Is.Not.EqualTo(firstId));
            Assert.That(gameplay.PuzzleIdForTests, Is.EqualTo(retriedId));
            Assert.That(gameplay.BankIndexForTests, Is.EqualTo(3),
                "Source selector advances normally, skips one extra entry, then retries once.");
            Assert.That(state.GetBankIndex(6, 1, "N"), Is.EqualTo(3));

            List<object> recent = state.GetRecentPuzzles();
            Assert.That(recent.Count, Is.EqualTo(4));
            Assert.That(ReadHistoryString(recent[0], "puzzle_id"), Is.EqualTo(firstId));
            Assert.That(ReadHistoryString(recent[1], "puzzle_id"), Is.EqualTo(retriedId),
                "The retry candidate is also historically duplicated.");
            Assert.That(ReadHistoryString(recent[2], "puzzle_id"), Is.EqualTo(firstId));
            Assert.That(ReadHistoryInt(recent[2], "level"), Is.EqualTo(3));
            Assert.That(ReadHistoryString(recent[3], "puzzle_id"), Is.EqualTo(retriedId));
            Assert.That(ReadHistoryInt(recent[3], "level"), Is.EqualTo(3));

            Dictionary<string, object> retry = state.GetRetryPuzzle(3);
            Assert.That(retry, Is.Not.Empty);
            Assert.That(System.Convert.ToInt32(retry["bank_index"]), Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator PlatformStartup_PrivacyPushAndDailyNotifications()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            AbConfigRuntime abRuntime = Find<AbConfigRuntime>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            Assert.That(abRuntime, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);

            PrivacyPermissionRuntime runtime = CreatePlatformRuntime(
                manager,
                abRuntime,
                out PlayModePlatformPermissionProvider provider);
            provider.IsPrivacyRequiredValue = true;
            provider.IsMobileValue = true;
            provider.IsNotificationPermissionEnabledValue = true;
            bool completed = false;
            runtime.StartCoroutine(CompleteWhenDone(
                runtime.AwaitPrivacyAndPush(),
                () => completed = true));

            yield return WaitForState(
                manager, UiName.Privacy, UiWindowState.Showing);
            FindButton(manager.Get(UiName.Privacy), "AcceptButton")
                .onClick.Invoke();
            yield return WaitUntil(
                () => completed,
                TransitionTimeoutSeconds,
                "Privacy/startup push flow did not complete.");
            yield return WaitForState(
                manager, UiName.Privacy, UiWindowState.Hidden);
            yield return WaitUntil(
                () => provider.SavedNotifications.Count == 2,
                TransitionTimeoutSeconds,
                "Daily local notifications were not registered.");

            Assert.That(provider.AgreePrivacyCount, Is.EqualTo(1));
            Assert.That(provider.InitializeTrackingCount, Is.EqualTo(1));
            Assert.That(provider.NotificationRequestCount, Is.EqualTo(1));
            Assert.That(provider.NotificationRequestType,
                Is.EqualTo(NotificationPermissionRequestType.System));
            Assert.That(provider.NotificationPosition,
                Is.EqualTo("app_start"));
            Assert.That(provider.PushEnabled, Is.True);
            Assert.That(provider.RemovedNotificationIds,
                Is.EquivalentTo(new[] { "daily_noon", "daily_evening" }));
            Assert.That(GameStateRuntime.Current.PushAskCount, Is.EqualTo(1));
            Assert.That(GameStateRuntime.Current.PushGuideShownCount,
                Is.EqualTo(1));
            Object.Destroy(runtime.gameObject);
        }

        [UnityTest]
        public IEnumerator PlatformAtt_CustomGuideContinuesBeforeSystemRequest()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            AbConfigRuntime abRuntime = Find<AbConfigRuntime>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            Assert.That(abRuntime, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");

            PrivacyPermissionRuntime runtime = CreatePlatformRuntime(
                manager,
                abRuntime,
                out PlayModePlatformPermissionProvider provider);
            provider.IsIosValue = true;
            provider.CanShowTrackingAuthorizationValue = true;
            bool completed = false;
            runtime.StartCoroutine(CompleteWhenDone(
                runtime.AwaitConsentAndTracking(2f),
                () => completed = true));

            yield return WaitForState(
                manager, UiName.PreAttGuide, UiWindowState.Showing);
            Assert.That(provider.TrackingRequestCount, Is.Zero,
                "System ATT must wait for the custom guide.");
            FindButton(manager.Get(UiName.PreAttGuide), "ContinueButton")
                .onClick.Invoke();
            yield return WaitUntil(
                () => completed,
                5f,
                "ATT flow did not complete after Continue.");
            yield return WaitForState(
                manager, UiName.PreAttGuide, UiWindowState.Hidden);

            Assert.That(provider.ConsentCheckCount, Is.EqualTo(1));
            Assert.That(provider.TrackingRequestCount, Is.EqualTo(1));
            Assert.That(provider.TrackingSource, Is.EqualTo("splash_scr"));
            Assert.That(GameStateRuntime.Current.HasShownAttGuide, Is.True);
            Object.Destroy(runtime.gameObject);
        }

        [UnityTest]
        public IEnumerator PlatformPushGuide_AllowUsesSourceRequestAndCounters()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            AbConfigRuntime abRuntime = Find<AbConfigRuntime>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            Assert.That(abRuntime, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");

            string today = DateTime.Now.ToString("yyyy-MM-dd");
            GameStateService state = GameStateRuntime.Current;
            state.Data.TodayDate = today;
            state.Data.RecentWinCountsByDay = new Dictionary<string, object>
            {
                [today] = 20
            };
            abRuntime.Platform.PushPermission.SetDebugOverride(
                PushPermissionConfig.ValueThreeDayProgress);

            PrivacyPermissionRuntime runtime = CreatePlatformRuntime(
                manager,
                abRuntime,
                out PlayModePlatformPermissionProvider provider);
            provider.IsMobileValue = true;
            provider.IsNotificationPermissionEnabledValue = true;
            bool completed = false;
            runtime.StartCoroutine(CompleteWhenDone(
                runtime.TryShowPushGuide(20),
                () => completed = true));

            yield return WaitForState(
                manager, UiName.PrePushGuide, UiWindowState.Showing);
            FindButton(manager.Get(UiName.PrePushGuide), "AllowButton")
                .onClick.Invoke();
            yield return WaitUntil(
                () => completed,
                TransitionTimeoutSeconds,
                "Push guide Allow flow did not complete.");
            yield return WaitForState(
                manager, UiName.PrePushGuide, UiWindowState.Hidden);

            Assert.That(provider.NotificationRequestCount, Is.EqualTo(1));
            Assert.That(provider.NotificationRequestType,
                Is.EqualTo(
                    NotificationPermissionRequestType.SystemAndSetting));
            Assert.That(provider.NotificationPosition,
                Is.EqualTo("push_guide"));
            Assert.That(state.PushAskCount, Is.EqualTo(1));
            Assert.That(state.PushGuidePopupCount, Is.EqualTo(1));
            Assert.That(state.PushGuideShownCount, Is.EqualTo(1));
            Assert.That(state.PushGuideLastDate, Is.EqualTo(today));
            Object.Destroy(runtime.gameObject);
        }

        [UnityTest]
        public IEnumerator AppScene_PrimaryButtons_NavigateSettingsAndGame()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);

            UIFrameWindow home = manager.Get(UiName.Home);
            Assert.That(home, Is.Not.Null);
            AssertShowing(manager, UiName.Home);

            FindButton(home, "SettingsBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Setting,
                UiWindowState.Showing);
            UIFrameWindow settings = manager.Get(UiName.Setting);
            FindButton(settings, "CloseBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Setting,
                UiWindowState.Hidden);
            AssertShowing(manager, UiName.Home);

            FindButton(home, "StartBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Showing);
            UIFrameWindow game = manager.Get(UiName.Game);
            GameplayManager gameplay =
                game.GetComponentInChildren<GameplayManager>(true);
            Assert.That(gameplay, Is.Not.Null);
            yield return WaitUntil(
                () => gameplay.CurrentPuzzleSize > 0,
                TransitionTimeoutSeconds,
                "StartBtn opened Game without building a puzzle.");
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Hidden);

            FindButton(game, "BackBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Hidden);
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Showing);
            Assert.That(manager.IsAnyLoading, Is.False);
        }

        [UnityTest]
        public IEnumerator AppScene_TransitionAndInputGuards_SurviveStress()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);

            UIFrameWindow home = manager.Get(UiName.Home);
            AssertShowing(manager, UiName.Home);
            int baselineMaskCount = manager.MaskReferenceCount;
            int settingShown = 0;
            int settingHidden = 0;
            void CountSettingShown(UiName name, UIFrameWindow _) =>
                settingShown += name == UiName.Setting ? 1 : 0;
            void CountSettingHidden(UiName name, UIFrameWindow _) =>
                settingHidden += name == UiName.Setting ? 1 : 0;
            manager.Events.WindowShown += CountSettingShown;
            manager.Events.WindowHidden += CountSettingHidden;

            Button settingsButton = FindButton(home, "SettingsBtn");
            Assert.That(
                settingsButton.GetComponent<UIButtonPressGuard>(),
                Is.Not.Null,
                "Static Home buttons must own the source release guard.");
            ClickThroughPointerPhases(settingsButton);

            UIFrameWindow setting = manager.Get(UiName.Setting);
            Assert.That(setting, Is.Not.Null);
            Assert.That(setting.WindowState, Is.EqualTo(UiWindowState.Showing));
            Assert.That(manager.IsInputGuardActive, Is.True,
                "The release frame that opened Settings was not guarded.");
            yield return new WaitForEndOfFrame();
            yield return null;
            Assert.That(manager.IsInputGuardActive, Is.False,
                "Release-frame guard did not clean itself up.");

            int expectedMaskCount = baselineMaskCount +
                                    (setting.ShowMask ? 1 : 0);
            Assert.That(manager.MaskReferenceCount,
                Is.EqualTo(expectedMaskCount));

            RectTransform settingRect = setting.transform as RectTransform;
            manager.BlockInputBriefly(settingRect, 0.25f);
            Assert.That(manager.IsInputBrieflyBlocked(settingRect), Is.True);
            AssertLocalInputBlocker(settingRect);
            yield return new WaitForSecondsRealtime(0.1f);
            manager.BlockInputBriefly(settingRect, 0.25f);
            yield return null;
            Assert.That(manager.IsInputBrieflyBlocked(settingRect), Is.True,
                "Refreshing a timed blocker must replace, not cancel, it.");
            AssertLocalInputBlocker(settingRect);
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(manager.IsInputBrieflyBlocked(settingRect), Is.True,
                "The refreshed blocker expired on the old deadline.");
            yield return WaitUntil(
                () => !manager.IsInputBrieflyBlocked(settingRect),
                1f,
                "Timed local input blocker did not clean itself up.");

            for (int iteration = 0; iteration < 96; iteration++)
            {
                manager.Hide(UiName.Setting);
                Assert.That(setting.WindowState,
                    Is.EqualTo(UiWindowState.Closing));
                manager.Hide(UiName.Setting);
                UIFrameWindow reopened = manager.Show(UiName.Setting);
                Assert.That(reopened, Is.SameAs(setting));
                Assert.That(reopened.WindowState,
                    Is.EqualTo(UiWindowState.Showing));
                Assert.That(manager.MaskReferenceCount,
                    Is.EqualTo(expectedMaskCount));
                yield return null;
                Assert.That(reopened.WindowState,
                    Is.EqualTo(UiWindowState.Showing));
            }

            Assert.That(settingShown, Is.EqualTo(1),
                "Aborted closes must not emit duplicate shown events.");
            Assert.That(settingHidden, Is.Zero,
                "Aborted closes must not emit hidden events.");
            Assert.That(setting.SortingOrder,
                Is.GreaterThanOrEqualTo((int)setting.Layer));
            Assert.That(setting.SortingOrder,
                Is.LessThan((int)setting.Layer + UiLayerConfig.ZMax),
                "Sorting order did not compact inside the source Z range.");

            manager.Hide(UiName.Setting);
            yield return WaitForState(
                manager,
                UiName.Setting,
                UiWindowState.Hidden);
            Assert.That(settingHidden, Is.EqualTo(1));
            Assert.That(manager.MaskReferenceCount,
                Is.EqualTo(baselineMaskCount));
            manager.Events.WindowShown -= CountSettingShown;
            manager.Events.WindowHidden -= CountSettingHidden;

            bool languageWasCached = manager.Has(UiName.Language);
            int languageCreated = 0;
            int languageShown = 0;
            void CountLanguageCreated(UiName name, UIFrameWindow _) =>
                languageCreated += name == UiName.Language ? 1 : 0;
            void CountLanguageShown(UiName name, UIFrameWindow _) =>
                languageShown += name == UiName.Language ? 1 : 0;
            manager.Events.WindowCreated += CountLanguageCreated;
            manager.Events.WindowShown += CountLanguageShown;
            UIFrameWindow first = null;
            UIFrameWindow second = null;
            manager.StartCoroutine(manager.ShowAsync(
                UiName.Language,
                completed: window => first = window));
            manager.StartCoroutine(manager.ShowAsync(
                UiName.Language,
                completed: window => second = window));
            yield return WaitUntil(
                () => first != null && second != null,
                TransitionTimeoutSeconds,
                "Concurrent ShowAsync calls did not settle.");
            Assert.That(first, Is.SameAs(second));
            Assert.That(languageCreated,
                Is.EqualTo(languageWasCached ? 0 : 1),
                "One-flight loading created duplicate Language pages.");
            Assert.That(languageShown, Is.EqualTo(1),
                "One-flight loading emitted duplicate shown events.");
            Assert.That(manager.IsAnyLoading, Is.False);
            manager.Events.WindowCreated -= CountLanguageCreated;
            manager.Events.WindowShown -= CountLanguageShown;
            manager.Hide(UiName.Language);
            yield return WaitForState(
                manager,
                UiName.Language,
                UiWindowState.Hidden);
            AssertShowing(manager, UiName.Home);
        }

        [UnityTest]
        public IEnumerator AppScene_SettingsConditionalButtons_UseSharedAbRuntimeAndSourceRoutes()
        {
            AbConfigRuntime abRuntime = Find<AbConfigRuntime>();
            Assert.That(abRuntime, Is.Not.Null,
                "AppScene is missing AbConfigRuntime.");
            PlayModeAbProvider provider =
                abRuntime.gameObject.AddComponent<PlayModeAbProvider>();
            provider.SetInt(
                "settings_language",
                SettingsLanguageConfig.ValuePopup);
            provider.SetInt(
                "rule_text",
                RuleTextConfig.ValueSettingEntry);
            provider.SetInt(
                "blind_mod",
                BlindModConfig.ValueHideOnFilled);
            abRuntime.BindProvider(provider);

            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);

            UIFrameWindow home = manager.Get(UiName.Home);
            FindButton(home, "SettingsBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Setting,
                UiWindowState.Showing);
            UIFrameWindow settings = manager.Get(UiName.Setting);
            Button language = FindButton(settings, "LanguageBtn");
            Button homeHowToPlay = FindButton(
                settings,
                "HowToPlayBtn",
                requireInteractable: false,
                requireActive: false);
            Assert.That(homeHowToPlay.gameObject.activeInHierarchy, Is.False,
                "HowToPlayBtn belongs only to game-mode Settings.");

            language.onClick.Invoke();
            yield return WaitForState(manager, UiName.Language,
                UiWindowState.Showing);
            AssertShowing(manager, UiName.Setting);
            manager.Hide(UiName.Language);
            yield return WaitForState(manager, UiName.Language,
                UiWindowState.Hidden);
            FindButton(settings, "CloseBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Setting,
                UiWindowState.Hidden);

            provider.SetInt(
                "settings_language",
                SettingsLanguageConfig.ValueDropdown);
            var settingsExternal = new RecordingSettingsExternalServices
            {
                IsConsentManagementRequired = true,
                IsOnline = false
            };
            manager.BindSettingsExternalServices(settingsExternal);
            SettingsPagePresenter settingsPresenter =
                settings as SettingsPagePresenter;
            Assert.That(settingsPresenter, Is.Not.Null);
            settingsPresenter.OverrideSystemLocaleForTests("vi_VN");

            FindButton(home, "SettingsBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Setting,
                UiWindowState.Showing);
            settings = manager.Get(UiName.Setting);
            LanguageSwitchWidget languageWidget =
                settings.GetComponentInChildren<LanguageSwitchWidget>(true);
            Assert.That(languageWidget, Is.Not.Null);
            Assert.That(languageWidget.gameObject.activeInHierarchy, Is.True,
                "Non-English system locale must expose dropdown mode.");
            language = FindButton(
                settings,
                "LanguageBtn",
                requireInteractable: false,
                requireActive: false);
            Assert.That(language.gameObject.activeInHierarchy, Is.False);

            FindButton(settings, "FeedbackBtn").onClick.Invoke();
            Assert.That(settingsExternal.FeedbackOpenCount, Is.Zero,
                "Offline Feedback must stop at the source network gate.");
            settingsExternal.IsOnline = true;
            FindButton(settings, "FeedbackBtn").onClick.Invoke();
            Assert.That(settingsExternal.FeedbackOpenCount, Is.EqualTo(1));
            FindButton(settings, "PrivacyPreferenceBtn").onClick.Invoke();
            Assert.That(settingsExternal.ConsentOpenCount, Is.EqualTo(1));
            FindButton(settings, "TermsBtn").onClick.Invoke();
            FindButton(settings, "PrivacyBtn").onClick.Invoke();
            Assert.That(settingsExternal.OpenedUrls,
                Is.EqualTo(new[]
                {
                    "https://oakevergames.com/tos.html",
                    "https://oakevergames.com/pp.html"
                }));

            FindButton(settings, "Row").onClick.Invoke();
            Assert.That(languageWidget.IsOpen, Is.True);
            Graphic outside = FindNamedComponent<Graphic>(
                settings,
                "OutsideBlocker");
            Assert.That(outside.raycastTarget, Is.True);
            var outsidePress = new PointerEventData(EventSystem.current)
            {
                pointerCurrentRaycast = new RaycastResult
                {
                    gameObject = outside.gameObject
                }
            };
            languageWidget.OnPointerDown(outsidePress);
            Assert.That(languageWidget.IsOpen, Is.False,
                "Godot closes the dropdown on outside pointer-down.");
            AssertShowing(manager, UiName.Setting);

            FindButton(settings, "Row").onClick.Invoke();
            Assert.That(languageWidget.IsOpen, Is.True);
            FindButton(settings, "SystemLangOption").onClick.Invoke();
            yield return WaitForState(manager, UiName.Setting,
                UiWindowState.Hidden);
            Assert.That(GameStateRuntime.Current.AppliedLocale,
                Is.EqualTo("vi_VN"));

            FindButton(home, "StartBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Showing);
            UIFrameWindow game = manager.Get(UiName.Game);
            GameplayManager gameplay =
                game.GetComponentInChildren<GameplayManager>(true);
            Assert.That(gameplay, Is.Not.Null);
            yield return WaitForSession(gameplay, GameSessionState.Playing);
            BoardView board = gameplay.boardView;
            Assert.That(board, Is.Not.Null);
            Assert.That(board.PatternOnForTests, Is.False);

            CellView patternCell = null;
            for (int row = 0; row < board.PuzzleSize && patternCell == null; row++)
            {
                for (int column = 0;
                     column < board.PuzzleSize && patternCell == null;
                     column++)
                {
                    if (board.GetCellState(row, column) == CellStateType.EMPTY)
                        patternCell = board.GetCellForTests(row, column);
                }
            }
            Assert.That(patternCell, Is.Not.Null,
                "The source level needs one empty cell for the pattern test.");
            Assert.That(patternCell.patternImage, Is.Not.Null,
                "Cell prefab did not serialize its Pattern image.");
            Assert.That(patternCell.IsPatternVisibleForTests, Is.False);

            FindButton(game, "SettingsBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Setting,
                UiWindowState.Showing);
            Assert.That(GameStateRuntime.Current.PatternEntryDotDismissed,
                Is.True,
                "Opening game Settings must dismiss the source pattern-entry dot.");
            settings = manager.Get(UiName.Setting);
            language = FindButton(
                settings,
                "LanguageBtn",
                requireInteractable: false,
                requireActive: false);
            Assert.That(language.gameObject.activeInHierarchy, Is.False,
                "Language entry must stay hidden in game-mode Settings.");

            bool soundBefore = GameStateRuntime.Current.SoundOn;
            FindButton(settings, "SoundBtn").onClick.Invoke();
            Assert.That(GameStateRuntime.Current.SoundOn, Is.EqualTo(!soundBefore));
            bool vibrationBefore = GameStateRuntime.Current.VibrationOn;
            FindButton(settings, "VibrationBtn").onClick.Invoke();
            Assert.That(GameStateRuntime.Current.VibrationOn,
                Is.EqualTo(!vibrationBefore));
            bool peopleBefore = GameStateRuntime.Current.PeopleOn;
            FindButton(settings, "PeopleBtn").onClick.Invoke();
            Assert.That(GameStateRuntime.Current.PeopleOn, Is.EqualTo(!peopleBefore));

            FindButton(settings, "PatternModeSwitch").onClick.Invoke();
            Assert.That(GameStateRuntime.Current.PatternModeOn, Is.True);
            Assert.That(GameStateRuntime.Current.PatternSwitchDotDismissed,
                Is.True);
            Assert.That(board.PatternOnForTests, Is.True);
            Assert.That(board.PatternKeepOnFilledForTests, Is.False);
            Assert.That(patternCell.IsPatternVisibleForTests, Is.True,
                "An empty cell must show its source pattern when enabled.");
            board.SetCellState(
                patternCell.Row,
                patternCell.Col,
                CellStateType.MARK,
                false);
            Assert.That(patternCell.IsPatternVisibleForTests, Is.False,
                "blind_mod=1 must hide the pattern on a filled cell.");
            board.SetCellState(
                patternCell.Row,
                patternCell.Col,
                CellStateType.EMPTY,
                false);
            Assert.That(patternCell.IsPatternVisibleForTests, Is.True);

            int restartCount = gameplay.RestartCountForTests;
            RectTransform restartRow = FindNamedComponent<RectTransform>(
                settings,
                "OrangeRestartBtn");
            Button restart = FindOnlyButton(restartRow);
            restart.onClick.Invoke();
            restart.onClick.Invoke();
            yield return WaitForState(manager, UiName.Setting,
                UiWindowState.Hidden);
            yield return WaitForSession(gameplay, GameSessionState.Playing);
            Assert.That(gameplay.RestartCountForTests,
                Is.EqualTo(restartCount + 1),
                "Rapid Settings Restart presses must be consumed once.");

            FindButton(game, "SettingsBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Setting,
                UiWindowState.Showing);
            settings = manager.Get(UiName.Setting);

            Button howToPlay = FindButton(settings, "HowToPlayBtn");
            howToPlay.onClick.Invoke();

            yield return WaitForState(manager, UiName.HowToPlayPaged,
                UiWindowState.Showing);
            yield return WaitForState(manager, UiName.Setting,
                UiWindowState.Hidden);
            AssertShowing(manager, UiName.Game);
            Assert.That(gameplay.SessionState,
                Is.EqualTo(GameSessionState.Playing));

            HowToPlayPagedPagePresenter paged =
                manager.Get(UiName.HowToPlayPaged) as
                    HowToPlayPagedPagePresenter;
            Assert.That(paged, Is.Not.Null);
            Button main = FindButton(paged, "MainBtn");
            Button previous = FindButton(
                paged,
                "BackBtn",
                requireActive: false);
            Assert.That(paged.PageIndex, Is.EqualTo(0));
            main.onClick.Invoke();
            Assert.That(paged.PageIndex, Is.EqualTo(1));
            previous.onClick.Invoke();
            Assert.That(paged.PageIndex, Is.EqualTo(0));
            main.onClick.Invoke();
            main.onClick.Invoke();
            Assert.That(paged.PageIndex,
                Is.EqualTo(HowToPlayContract.PagedDemos.Count - 1));

            bool closedRaised = false;
            UiWindowState stateAtClosed = UiWindowState.Hidden;
            paged.Closed += () =>
            {
                closedRaised = true;
                stateAtClosed = paged.WindowState;
            };
            main.onClick.Invoke();
            Assert.That(closedRaised, Is.True,
                "Paged HTP must emit Closed from the user's close request.");
            Assert.That(stateAtClosed, Is.EqualTo(UiWindowState.Showing),
                "Godot emits closed before UIManager begins the close animation.");
            Assert.That(paged.WindowState, Is.EqualTo(UiWindowState.Closing));
            yield return WaitForState(manager, UiName.HowToPlayPaged,
                UiWindowState.Hidden);
            AssertShowing(manager, UiName.Game);
            Assert.That(manager.IsAnyLoading, Is.False);
        }

        [UnityTest]
        public IEnumerator AppScene_MainResultLoop_RestartWinAndContinue()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);

            UIFrameWindow home = manager.Get(UiName.Home);
            FindButton(home, "StartBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Showing);
            UIFrameWindow game = manager.Get(UiName.Game);
            GameplayManager gameplay =
                game.GetComponentInChildren<GameplayManager>(true);
            Assert.That(gameplay, Is.Not.Null);
            yield return WaitForSession(gameplay, GameSessionState.Playing);

            yield return FailCurrentSession(gameplay, manager);
            UIFrameWindow fail = manager.Get(UiName.Fail);
            Button revive = FindButton(
                fail,
                "ReviveButton",
                requireInteractable: false,
                requireActive: false);
            Assert.That(revive.gameObject.activeInHierarchy, Is.False,
                "Offline default requires a rewarded ad, so Revive must stay hidden when the provider is unavailable.");
            Button restart = FindButton(fail, "RestartButton", false);
            yield return WaitUntil(
                () => restart.isActiveAndEnabled && restart.interactable,
                TransitionTimeoutSeconds,
                "RestartButton did not unlock after Fail appeared.");
            restart.onClick.Invoke();
            yield return WaitForState(manager, UiName.Fail,
                UiWindowState.Hidden);
            yield return WaitForSession(gameplay, GameSessionState.Playing);
            Assert.That(gameplay.CurrentLevelNumber, Is.EqualTo(1));
            Assert.That(gameplay.LivesForTests, Is.EqualTo(3));
            Assert.That(GameStateRuntime.Current.CurrentLevel, Is.EqualTo(1));

            SessionActionResult completed = gameplay.RunAutoComplete();
            Assert.That(completed.Accepted, Is.True);
            Assert.That(completed.IsComplete, Is.True);
            yield return CompletePreWinMetaFlow(manager);
            UIFrameWindow win = manager.Get(UiName.Win);
            Button next = FindActiveButton(win, "Next", false);
            yield return WaitUntil(
                () => next.interactable,
                TransitionTimeoutSeconds,
                "Win Next button did not become interactable.");
            next.onClick.Invoke();
            yield return WaitForState(
                manager,
                UiName.Win,
                UiWindowState.Hidden,
                15f);
            yield return WaitUntil(
                () => gameplay.CurrentLevelNumber == 2 &&
                      gameplay.SessionState == GameSessionState.Playing,
                15f,
                "Continue did not load level 2 into the active Game page.");
            Assert.That(GameStateRuntime.Current.CurrentLevel, Is.EqualTo(2));
            AssertShowing(manager, UiName.Game);
        }

        [UnityTest]
        public IEnumerator AppScene_HomeFeatureEntries_FollowSharedAbAndNavigate()
        {
            AbConfigRuntime abRuntime = Find<AbConfigRuntime>();
            Assert.That(abRuntime, Is.Not.Null,
                "AppScene is missing AbConfigRuntime.");
            PlayModeAbProvider provider =
                abRuntime.gameObject.AddComponent<PlayModeAbProvider>();
            provider.SetInt(
                "daily_streak",
                DailyStreakConfig.ValueBasic);
            provider.SetInt(
                "leaderboard_func",
                LeaderboardFuncConfig.ValueCatsProp);
            provider.SetInt(
                "hard_button",
                HardButtonConfig.ValueDefault);
            abRuntime.BindProvider(provider);

            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Showing);

            UIFrameWindow home = manager.Get(UiName.Home);
            HomePagePresenter presenter =
                home.GetComponentInChildren<HomePagePresenter>(true);
            DailyChallengeEntryPresenter daily =
                home.GetComponentInChildren<DailyChallengeEntryPresenter>(true);
            StreakEntryPresenter streak =
                home.GetComponentInChildren<StreakEntryPresenter>(true);
            RankActivityEntryPresenter rank =
                home.GetComponentInChildren<RankActivityEntryPresenter>(true);
            Assert.That(presenter, Is.Not.Null);
            Assert.That(daily, Is.Not.Null);
            Assert.That(streak, Is.Not.Null);
            Assert.That(rank, Is.Not.Null);
            Assert.That(
                FindNamedComponent<RectTransform>(home, "ProfileEntry")
                    .gameObject.activeInHierarchy,
                Is.True,
                "leaderboard_func must control the Profile entry like the source.");

            Button dailyButton = FindEntryButton(daily);
            Button streakButton = FindEntryButton(streak);
            Button rankButton = FindEntryButton(rank);
            Assert.That(dailyButton.gameObject.activeInHierarchy, Is.True);
            Assert.That(streakButton.gameObject.activeInHierarchy, Is.True);
            Assert.That(rankButton.gameObject.activeInHierarchy, Is.False,
                "Rank entry must stay unavailable below source unlock level 11.");

            dailyButton.onClick.Invoke();
            yield return null;
            AssertShowing(manager, UiName.Home);
            Assert.That(manager.Has(UiName.DailyGame), Is.False,
                "Locked Daily entry must not create/open DailyGame.");

            streakButton.onClick.Invoke();
            yield return WaitForState(manager, UiName.Streak,
                UiWindowState.Showing);
            FindButton(manager.Get(UiName.Streak), "BackBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Streak,
                UiWindowState.Hidden);
            AssertShowing(manager, UiName.Home);

            GameStateRuntime.Current.Data.CurrentLevel = 21;
            presenter.RefreshPresentation();
            daily.RefreshNow();
            dailyButton = FindEntryButton(daily);
            dailyButton.onClick.Invoke();
            yield return WaitForState(manager, UiName.DailyGame,
                UiWindowState.Showing);
            UIFrameWindow dailyGame = manager.Get(UiName.DailyGame);
            GameplayManager dailyGameplay =
                dailyGame.GetComponentInChildren<GameplayManager>(true);
            Assert.That(dailyGameplay, Is.Not.Null);
            yield return WaitForSession(dailyGameplay, GameSessionState.Playing);
            Assert.That(dailyGameplay.SessionMode,
                Is.EqualTo(GameplaySessionMode.Daily));
            FindButton(dailyGame, "BackBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.DailyGame,
                UiWindowState.Hidden);
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Showing);

            RankActivityRuntime rankRuntime = Find<RankActivityRuntime>();
            Assert.That(rankRuntime, Is.Not.Null);
            yield return WaitUntil(
                () => IsShowing(manager, UiName.RankActivityOpenPopup) ||
                      FindEntryButton(rank).gameObject.activeInHierarchy,
                TransitionTimeoutSeconds,
                "Rank entry did not become available at level 21.");
            if (!IsShowing(manager, UiName.RankActivityOpenPopup))
                FindEntryButton(rank).onClick.Invoke();
            yield return WaitForState(manager, UiName.RankActivityOpenPopup,
                UiWindowState.Showing);

            UIFrameWindow rankOpen = manager.Get(UiName.RankActivityOpenPopup);
            FindButton(rankOpen, "ActionButton").onClick.Invoke();
            yield return WaitForState(manager, UiName.RankActivityOpenPopup,
                UiWindowState.Hidden);
            yield return WaitUntil(
                () => IsShowing(manager, UiName.Profile) ||
                      IsShowing(manager, UiName.Game),
                TransitionTimeoutSeconds,
                "Rank participation did not continue to Profile guide or Game.");
            if (IsShowing(manager, UiName.Profile))
            {
                FindButton(manager.Get(UiName.Profile), "CloseBtn")
                    .onClick.Invoke();
                yield return WaitForState(manager, UiName.Profile,
                    UiWindowState.Hidden);
            }
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Showing,
                15f);
            Assert.That(rankRuntime.Manager.IsJoined, Is.True);
        }

        [UnityTest]
        public IEnumerator AppScene_DailyEntries_RolloverOnResumeAndHonorMaxDate()
        {
            AbConfigRuntime abRuntime = Find<AbConfigRuntime>();
            ClockTicker ticker = Find<ClockTicker>();
            DailyMetaRuntime dailyRuntime = Find<DailyMetaRuntime>();
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(abRuntime, Is.Not.Null);
            Assert.That(ticker, Is.Not.Null);
            Assert.That(dailyRuntime, Is.Not.Null);
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);

            PlayModeAbProvider provider =
                abRuntime.gameObject.AddComponent<PlayModeAbProvider>();
            provider.SetInt(
                "daily_streak",
                DailyStreakConfig.ValueBasic);
            abRuntime.BindProvider(provider);

            var clock = new MutableSystemClock
            {
                LocalNow = new DateTime(2026, 8, 10, 23, 59, 59),
                UnixSeconds = 100.25
            };
            var date = new MutableCurrentDate("2026-08-10");
            ticker.ConfigureForTests(clock);
            var streakFeature = new StreakFeature(
                dateProvider: date,
                streakConfig: abRuntime.Home.DailyStreak,
                initialData: new StreakData
                {
                    CurrentStreak = 3,
                    BestStreak = 3,
                    RewardCycleDay = 3,
                    LastCheckinDate = "2026-08-10",
                    StreakStartWeekday = 1
                });
            dailyRuntime.ConfigureForTests(
                streakFeature,
                dailyRuntime.Awards);
            dailyRuntime.BindAbConfigRuntime(abRuntime);

            GameStateService state = GameStateRuntime.Current;
            state.Data.CurrentLevel = 21;
            state.Data.DailyCompletedDate = "2026-08-10";
            state.Data.MaxDailyDate = "2026-08-09";

            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Showing);

            UIFrameWindow home = manager.Get(UiName.Home);
            DailyChallengeEntryPresenter dailyEntry =
                home.GetComponentInChildren<DailyChallengeEntryPresenter>(true);
            StreakEntryPresenter streakEntry =
                home.GetComponentInChildren<StreakEntryPresenter>(true);
            Assert.That(dailyEntry, Is.Not.Null);
            Assert.That(streakEntry, Is.Not.Null);
            Assert.That(dailyEntry.StateForTests,
                Is.EqualTo(DailyEntryState.Done));
            Assert.That(streakEntry.IsCheckedForTests, Is.True);
            Assert.That(state.MaxDailyDate, Is.EqualTo("2026-08-10"),
                "Home show must advance max_daily_date with the shared clock.");

            clock.LocalNow = new DateTime(2026, 8, 11, 0, 0, 0);
            clock.UnixSeconds = 200.25;
            date.CurrentDate = "2026-08-11";
            ticker.SendMessage(
                "OnApplicationFocus",
                true,
                SendMessageOptions.RequireReceiver);
            yield return WaitUntil(
                () => dailyEntry.StateForTests == DailyEntryState.Normal &&
                      !streakEntry.IsCheckedForTests,
                2f,
                "Daily/Streak entries did not roll over after focus resumed.");
            Assert.That(state.MaxDailyDate, Is.EqualTo("2026-08-10"),
                "Live ticks must refresh entries without inventing a Home show.");
            Assert.That(streakFeature.DisplayStreak, Is.EqualTo(3),
                "Day-watch refresh must not mutate streak progress.");

            manager.Hide(UiName.Home);
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Hidden);
            manager.Show(UiName.Home);
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Showing);
            Assert.That(state.MaxDailyDate, Is.EqualTo("2026-08-11"),
                "Reopening Home must persist the newly observed local date.");

            state.Data.DailyCompletedDate = string.Empty;
            clock.LocalNow = new DateTime(2026, 8, 10, 12, 0, 0);
            clock.UnixSeconds = 300.25;
            date.CurrentDate = "2026-08-10";
            ticker.SendMessage(
                "OnApplicationPause",
                false,
                SendMessageOptions.RequireReceiver);
            yield return WaitUntil(
                () => dailyEntry.StateForTests == DailyEntryState.Done &&
                      streakEntry.IsCheckedForTests,
                2f,
                "Backdated local clock did not refresh through the pause hook.");
            Assert.That(state.MaxDailyDate, Is.EqualTo("2026-08-11"));
        }

        [UnityTest]
        public IEnumerator AppScene_Streak_MultiDayCycleRewardAndBrokenReset()
        {
            AbConfigRuntime abRuntime = Find<AbConfigRuntime>();
            DailyMetaRuntime dailyRuntime = Find<DailyMetaRuntime>();
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(abRuntime, Is.Not.Null);
            Assert.That(dailyRuntime, Is.Not.Null);
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);

            PlayModeAbProvider provider =
                abRuntime.gameObject.AddComponent<PlayModeAbProvider>();
            provider.SetInt(
                "daily_streak",
                DailyStreakConfig.ValueBasic);
            abRuntime.BindProvider(provider);

            var date = new MutableCurrentDate("2026-08-10");
            var protect = new StreakProtectConfig();
            protect.SetDebugOverride(StreakProtectConfig.ValueControl);
            AwardManager awards = dailyRuntime.Awards;
            var streak = new StreakFeature(
                dateProvider: date,
                streakConfig: abRuntime.Home.DailyStreak,
                protectConfig: protect,
                rewardBoundary: awards,
                initialData: new StreakData());
            dailyRuntime.ConfigureForTests(streak, awards);
            dailyRuntime.BindAbConfigRuntime(abRuntime);

            GameStateService state = GameStateRuntime.Current;
            state.Data.CurrentLevel = 21;
            int initialHint = state.GetToolCount("hint");
            int initialLocate = state.GetToolCount("locate");

            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Showing);

            UIFrameWindow home = manager.Get(UiName.Home);
            StreakEntryPresenter entry =
                home.GetComponentInChildren<StreakEntryPresenter>(true);
            Assert.That(entry, Is.Not.Null);
            entry.RefreshNow();
            FindEntryButton(entry).onClick.Invoke();
            yield return WaitForState(manager, UiName.Streak,
                UiWindowState.Showing);
            StreakPagePresenter page = manager.Get(UiName.Streak)
                .GetComponentInChildren<StreakPagePresenter>(true);
            Assert.That(page, Is.Not.Null);
            Assert.That(page.StateForTests,
                Is.EqualTo(StreakDisplayState.Main));
            Assert.That(CountCheckedStreakSlots(page), Is.Zero);
            FindButton(manager.Get(UiName.Streak), "BackBtn")
                .onClick.Invoke();
            yield return WaitForState(manager, UiName.Streak,
                UiWindowState.Hidden);

            for (int day = 1; day <= StreakFeature.CycleLength; day++)
            {
                date.CurrentDate = "2026-08-" +
                                   (9 + day).ToString("00");
                streak.TickDayWatch();
                dailyRuntime.SettleWin(StreakCheckinSource.Main);

                Assert.That(streak.Data.CurrentStreak, Is.EqualTo(day));
                Assert.That(streak.Data.BestStreak, Is.EqualTo(day));
                Assert.That(streak.Data.RewardCycleDay, Is.EqualTo(day));
                Assert.That(streak.Data.LastCheckinDate,
                    Is.EqualTo(date.CurrentDate));
                Assert.That(CountCheckedWeekSlots(streak), Is.EqualTo(day));
                Assert.That(streak.PendingShowUid,
                    day == StreakFeature.CycleLength
                        ? Is.GreaterThan(0)
                        : Is.Zero);

                yield return PresentPendingStreak(
                    manager,
                    streak,
                    day - 1,
                    day);
                Assert.That(streak.HasPendingShow, Is.False);
            }

            Assert.That(state.GetToolCount("hint"),
                Is.EqualTo(initialHint + 2));
            Assert.That(state.GetToolCount("locate"),
                Is.EqualTo(initialLocate + 2));
            Assert.That(state.GetInFlightAwards(), Is.Empty,
                "The seventh-day chest must complete its durable award.");

            dailyRuntime.SettleWin(StreakCheckinSource.Main);
            Assert.That(streak.Data.CurrentStreak,
                Is.EqualTo(StreakFeature.CycleLength));
            Assert.That(streak.HasPendingShow, Is.False,
                "A second win on the same local day must be idempotent.");
            Assert.That(state.GetToolCount("hint"),
                Is.EqualTo(initialHint + 2));

            date.CurrentDate = "2026-08-17";
            streak.TickDayWatch();
            dailyRuntime.SettleWin(StreakCheckinSource.Main);
            Assert.That(streak.Data.CurrentStreak, Is.EqualTo(8));
            Assert.That(streak.Data.RewardCycleDay, Is.EqualTo(8));
            Assert.That(CountCheckedWeekSlots(streak), Is.EqualTo(1));
            yield return PresentPendingStreak(manager, streak, 0, 1);

            date.CurrentDate = "2026-08-19";
            streak.TickDayWatch();
            Assert.That(streak.IsBroken(), Is.True);
            Assert.That(streak.DisplayStreak, Is.Zero);
            dailyRuntime.SettleWin(StreakCheckinSource.Main);
            Assert.That(streak.Data.CurrentStreak, Is.EqualTo(1));
            Assert.That(streak.Data.BestStreak, Is.EqualTo(8));
            Assert.That(streak.Data.RewardCycleDay, Is.EqualTo(1));
            Assert.That(streak.Data.LastCheckinDate,
                Is.EqualTo("2026-08-19"));
            yield return PresentPendingStreak(manager, streak, 0, 1);

            Assert.That(state.GetToolCount("hint"),
                Is.EqualTo(initialHint + 2),
                "Only the seventh-day chest may grant tools in this matrix.");
            Assert.That(state.GetToolCount("locate"),
                Is.EqualTo(initialLocate + 2));
            Assert.That(manager.IsAnyLoading, Is.False);
        }

        [UnityTest]
        public IEnumerator AppScene_RankFirstPeriod_CloseStillJoinsAndEntersMain()
        {
            AbConfigRuntime abRuntime = Find<AbConfigRuntime>();
            Assert.That(abRuntime, Is.Not.Null);
            PlayModeAbProvider provider =
                abRuntime.gameObject.AddComponent<PlayModeAbProvider>();
            provider.SetInt(
                "leaderboard_func",
                LeaderboardFuncConfig.ValueCatsProp);
            abRuntime.BindProvider(provider);

            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Showing);

            GameStateRuntime.Current.Data.CurrentLevel = 21;
            RankActivityRuntime rankRuntime = Find<RankActivityRuntime>();
            Assert.That(rankRuntime, Is.Not.Null);
            Assert.That(rankRuntime.Manager.MaybeOpen(true), Is.True);
            Assert.That(rankRuntime.Manager.PeriodCount, Is.EqualTo(1));
            Assert.That(rankRuntime.Manager.IsOpenNotJoined, Is.True);

            UIFrameWindow home = manager.Get(UiName.Home);
            HomePagePresenter presenter =
                home.GetComponentInChildren<HomePagePresenter>(true);
            RankActivityEntryPresenter rankEntry =
                home.GetComponentInChildren<RankActivityEntryPresenter>(true);
            Assert.That(presenter, Is.Not.Null);
            Assert.That(rankEntry, Is.Not.Null);
            presenter.RefreshPresentation();
            rankEntry.RefreshNow();
            yield return WaitUntil(
                () => IsShowing(manager, UiName.RankActivityOpenPopup) ||
                      FindEntryButton(rankEntry).gameObject.activeInHierarchy,
                TransitionTimeoutSeconds,
                "First-period Rank entry did not become available.");
            if (!IsShowing(manager, UiName.RankActivityOpenPopup))
                FindEntryButton(rankEntry).onClick.Invoke();
            yield return WaitForState(manager, UiName.RankActivityOpenPopup,
                UiWindowState.Showing);

            var popup = manager.Get(UiName.RankActivityOpenPopup) as
                RankActivityOpenPopupPresenter;
            Assert.That(popup, Is.Not.Null);
            Assert.That(popup.WasStarted, Is.False);
            FindButton(popup, "CloseBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.RankActivityOpenPopup,
                UiWindowState.Hidden);
            Assert.That(popup.WasStarted, Is.False,
                "Close must remain distinct from the Play action.");
            yield return WaitUntil(
                () => IsShowing(manager, UiName.Profile) ||
                      IsShowing(manager, UiName.Game),
                TransitionTimeoutSeconds,
                "First-period Close did not continue through Profile or Game.");
            if (IsShowing(manager, UiName.Profile))
            {
                FindButton(manager.Get(UiName.Profile), "CloseBtn")
                    .onClick.Invoke();
                yield return WaitForState(manager, UiName.Profile,
                    UiWindowState.Hidden);
            }
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Showing,
                15f);
            Assert.That(rankRuntime.Manager.IsJoined, Is.True,
                "Godot confirms Rank participation after either popup exit.");
            GameplayManager gameplay = manager.Get(UiName.Game)
                .GetComponentInChildren<GameplayManager>(true);
            Assert.That(gameplay, Is.Not.Null);
            yield return WaitForSession(gameplay, GameSessionState.Playing);
            Assert.That(gameplay.CurrentLevelNumber, Is.EqualTo(21));
        }

        [UnityTest]
        public IEnumerator AppScene_RankExpiryInGame_RewardThenOpensNextPeriodAtHome()
        {
            GameStateRuntime.Current.Data.CurrentLevel = 21;

            AbConfigRuntime abRuntime = Find<AbConfigRuntime>();
            RankActivityRuntime rankRuntime = Find<RankActivityRuntime>();
            DailyMetaRuntime dailyRuntime = Find<DailyMetaRuntime>();
            Assert.That(abRuntime, Is.Not.Null);
            Assert.That(rankRuntime, Is.Not.Null);
            Assert.That(dailyRuntime, Is.Not.Null);

            PlayModeAbProvider provider =
                abRuntime.gameObject.AddComponent<PlayModeAbProvider>();
            provider.SetInt(
                "leaderboard_func",
                LeaderboardFuncConfig.ValueCatsProp);
            abRuntime.BindProvider(provider);

            var time = new MutableRobotTime { UnixNow = 2_000_000 };
            var robotStore = new MemoryRobotPoolStore();
            var robots = new RobotService(
                robotStore,
                time,
                new SystemRobotRandomFactory());
            var profile = new ProfileService(new MemoryProfileDataStore());
            var awards = new AwardManager(
                GameStateRuntime.Current,
                profile);
            dailyRuntime.ConfigureForTests(dailyRuntime.Streak, awards);
            var rank = new RankActivityManager(
                new MemoryRankActivityStore(),
                robots,
                profile,
                awards,
                new RankEnvironment(),
                time,
                new SystemRobotRandomFactory());
            rankRuntime.ConfigureForTests(rank);

            Assert.That(rank.MaybeOpen(true), Is.True);
            rank.ConfirmParticipation();
            robotStore.ZeroAllScores();
            Assert.That(rank.PeriodCount, Is.EqualTo(1));
            Assert.That(rank.IsJoined, Is.True);

            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Showing);

            UIFrameWindow home = manager.Get(UiName.Home);
            FindButton(home, "StartBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Showing);
            UIFrameWindow game = manager.Get(UiName.Game);
            GameplayManager gameplay =
                game.GetComponentInChildren<GameplayManager>(true);
            Assert.That(gameplay, Is.Not.Null);
            yield return WaitForSession(gameplay, GameSessionState.Playing);
            Assert.That(rank.IsInLevelForTests, Is.True);

            time.UnixNow += RankActivityConfig.PeriodDurationSeconds + 1;
            rank.Tick();
            Assert.That(rank.State, Is.EqualTo(RankActivityState.Settling));
            Assert.That(rank.GetPendingReward(), Is.Null,
                "Expiry during a level must defer settlement until win/exit.");
            Assert.That(rank.IsInLevelForTests, Is.True);

            int initialHint = GameStateRuntime.Current.GetToolCount("hint");
            int initialLocate = GameStateRuntime.Current.GetToolCount("locate");
            int initialFrame = profile.GetFrameCount(
                ProfileCatalog.FirstPlaceFrameId);
            SessionActionResult completed = gameplay.RunAutoComplete();
            Assert.That(completed.Accepted, Is.True);
            Assert.That(completed.IsComplete, Is.True);
            yield return WaitUntil(
                () => !rank.IsInLevelForTests &&
                      rank.GetPendingReward() != null,
                10f,
                "Rank settlement did not follow the win collection flight.");
            Assert.That(rank.IsInLevelForTests, Is.False);
            Assert.That(rank.GetPendingReward(), Is.Not.Null);
            Assert.That(rank.GetPendingReward().Rank, Is.EqualTo(1));

            yield return WaitForState(manager, UiName.RankActivityChange,
                UiWindowState.Showing,
                15f);
            UIFrameWindow change = manager.Get(UiName.RankActivityChange);
            Button tapToContinue = FindButton(
                change,
                "TapToContinue",
                requireInteractable: false,
                requireActive: false);
            yield return WaitUntil(
                () => tapToContinue.gameObject.activeInHierarchy &&
                      tapToContinue.interactable,
                8f,
                "Rank Change did not unlock Tap to Continue.");
            tapToContinue.onClick.Invoke();
            yield return WaitForState(manager, UiName.RankActivityChange,
                UiWindowState.Hidden);

            yield return WaitForState(manager, UiName.Award,
                UiWindowState.Showing,
                10f);
            yield return CollectRankGift(manager);
            Assert.That(rank.State, Is.EqualTo(RankActivityState.NotOpened));
            Assert.That(rank.PeriodCount, Is.EqualTo(1),
                "An in-game reward must wait for Home before opening period 2.");
            Assert.That(GameStateRuntime.Current.GetToolCount("hint"),
                Is.EqualTo(initialHint + 2));
            Assert.That(GameStateRuntime.Current.GetToolCount("locate"),
                Is.EqualTo(initialLocate + 2));
            Assert.That(profile.GetFrameCount(ProfileCatalog.FirstPlaceFrameId),
                Is.EqualTo(initialFrame + 1));
            Assert.That(GameStateRuntime.Current.GetInFlightAwards(), Is.Empty);

            yield return WaitForState(manager, UiName.Win,
                UiWindowState.Showing,
                15f);
            Button next = FindActiveButton(
                manager.Get(UiName.Win),
                "Next",
                requireInteractable: false);
            yield return WaitUntil(
                () => next.interactable,
                TransitionTimeoutSeconds,
                "Win Next button did not become interactable.");
            next.onClick.Invoke();
            yield return WaitUntil(
                () => gameplay.CurrentLevelNumber == 22 &&
                      gameplay.SessionState == GameSessionState.Playing,
                15f,
                "Next did not load level 22.");

            FindButton(game, "BackBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Showing,
                15f);
            yield return WaitForState(manager, UiName.RankActivityOpenPopup,
                UiWindowState.Showing,
                15f);
            Assert.That(rank.PeriodCount, Is.EqualTo(2));
            Assert.That(rank.IsOpenNotJoined, Is.True);

            var periodTwoPopup = manager.Get(UiName.RankActivityOpenPopup) as
                RankActivityOpenPopupPresenter;
            Assert.That(periodTwoPopup, Is.Not.Null);
            Assert.That(periodTwoPopup.WasStarted, Is.False);
            FindButton(periodTwoPopup, "CloseBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.RankActivityOpenPopup,
                UiWindowState.Hidden);
            yield return WaitUntil(
                () => rank.IsJoined,
                TransitionTimeoutSeconds,
                "Period 2 participation was not confirmed after popup close.");
            Assert.That(rank.IsJoined, Is.True);
            Assert.That(rank.PeriodCount, Is.EqualTo(2));
            AssertShowing(manager, UiName.Home);
            Assert.That(manager.IsAnyLoading, Is.False);
        }

        [UnityTest]
        public IEnumerator AppScene_RankFrameOnlyGift_UsesFrameEffectAndPersistsOnce()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            DailyMetaRuntime dailyRuntime = Find<DailyMetaRuntime>();
            ProfileRuntime profileRuntime = Find<ProfileRuntime>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            Assert.That(dailyRuntime, Is.Not.Null);
            Assert.That(profileRuntime, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Showing);

            var awards = new AwardManager(
                GameStateRuntime.Current,
                profileRuntime);
            dailyRuntime.ConfigureForTests(dailyRuntime.Streak, awards);
            int initialFrame = profileRuntime.Service.GetFrameCount(
                ProfileCatalog.FirstPlaceFrameId);
            int initialHint = GameStateRuntime.Current.GetToolCount("hint");
            int uid = awards.Dispatch(
                new[]
                {
                    AwardItem.Frame(ProfileCatalog.FirstPlaceFrameId)
                },
                AwardDisplayType.RankGift,
                RankActivityManager.RewardReason);
            Assert.That(uid, Is.GreaterThan(0));
            Assert.That(awards.ShowAward(
                uid,
                new Dictionary<string, object>
                {
                    ["place"] = 1,
                    ["win_count"] = 1,
                    ["top3_infos"] = Array.Empty<object>()
                }), Is.True);

            yield return WaitForState(manager, UiName.Award,
                UiWindowState.Showing,
                10f);
            UIFrameWindow award = manager.Get(UiName.Award);
            FrameAwardEffectView effect =
                award.GetComponentInChildren<FrameAwardEffectView>(true);
            Assert.That(effect, Is.Not.Null);
            Button podiumCollect = FindActiveButton(
                award,
                "CollectBtn",
                requireInteractable: false);
            yield return WaitUntil(
                () => podiumCollect.interactable,
                5f,
                "Frame-only Rank Gift collect did not unlock.");
            podiumCollect.onClick.Invoke();
            yield return null;

            Assert.That(effect.gameObject.activeInHierarchy, Is.True);
            Assert.That(effect.IsPlaying, Is.True);
            Assert.That(effect.DisplayedFrameId,
                Is.EqualTo(ProfileCatalog.FirstPlaceFrameId));
            Assert.That(award.transform.Find("AwardPanel")
                .gameObject.activeInHierarchy, Is.False,
                "Frame-only phase must not expose the generic item panel.");
            Assert.That(profileRuntime.Service.GetFrameCount(
                    ProfileCatalog.FirstPlaceFrameId),
                Is.EqualTo(initialFrame),
                "The award must persist only after the frame effect ends.");

            yield return WaitForState(manager, UiName.Award,
                UiWindowState.Hidden,
                10f);
            Assert.That(profileRuntime.Service.GetFrameCount(
                    ProfileCatalog.FirstPlaceFrameId),
                Is.EqualTo(initialFrame + 1));
            Assert.That(GameStateRuntime.Current.GetToolCount("hint"),
                Is.EqualTo(initialHint));
            Assert.That(GameStateRuntime.Current.GetInFlightAwards(), Is.Empty);
            Assert.That(awards.ActiveRenderCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator AppScene_DailyResultLoop_IsolatedReviveRestartWinAndReturnsToMain()
        {
            AdRuntime adRuntime = Find<AdRuntime>();
            AbConfigRuntime abRuntime = Find<AbConfigRuntime>();
            Assert.That(adRuntime, Is.Not.Null);
            Assert.That(abRuntime, Is.Not.Null);
            PlayModeAdProvider adProvider =
                adRuntime.gameObject.AddComponent<PlayModeAdProvider>();
            adRuntime.BindProvider(adProvider);
            PlayModeAbProvider abProvider =
                abRuntime.gameObject.AddComponent<PlayModeAbProvider>();
            abProvider.SetInt(
                "daily_streak",
                DailyStreakConfig.ValueBasic);
            abProvider.SetInt(
                "leaderboard_func",
                LeaderboardFuncConfig.ValueCatsProp);
            abRuntime.BindProvider(abProvider);

            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Showing);

            GameStateService state = GameStateRuntime.Current;
            state.Data.CurrentLevel = 21;
            state.Data.CurrentStrategy = 3;
            state.Data.ConsecutiveFails = 5;
            state.Data.RetryPuzzleLevel = 21;
            state.Data.RetryPuzzleParameters = new Dictionary<string, object>
            {
                ["sentinel"] = "main-retry"
            };
            state.Data.EndgameSnapshot = new Dictionary<string, object>
            {
                ["sentinel"] = "main-snapshot"
            };
            state.Data.MainGameTotalStats = new Dictionary<string, object>
            {
                ["sentinel"] = 7
            };

            RankActivityRuntime rankRuntime = Find<RankActivityRuntime>();
            Assert.That(rankRuntime, Is.Not.Null);
            Assert.That(rankRuntime.Manager.MaybeOpen(true), Is.True);
            rankRuntime.Manager.ConfirmParticipation();
            rankRuntime.Manager.SetLevelCollect(17);
            Assert.That(rankRuntime.Manager.IsInLevelForTests, Is.False);

            UIFrameWindow home = manager.Get(UiName.Home);
            HomePagePresenter homePresenter =
                home.GetComponentInChildren<HomePagePresenter>(true);
            DailyChallengeEntryPresenter dailyEntry =
                home.GetComponentInChildren<DailyChallengeEntryPresenter>(true);
            Assert.That(homePresenter, Is.Not.Null);
            Assert.That(dailyEntry, Is.Not.Null);
            homePresenter.RefreshPresentation();
            dailyEntry.RefreshNow();
            FindEntryButton(dailyEntry).onClick.Invoke();

            yield return WaitForState(manager, UiName.DailyGame,
                UiWindowState.Showing);
            UIFrameWindow dailyGame = manager.Get(UiName.DailyGame);
            GameplayManager gameplay =
                dailyGame.GetComponentInChildren<GameplayManager>(true);
            Assert.That(gameplay, Is.Not.Null);
            yield return WaitForSession(gameplay, GameSessionState.Playing);
            Assert.That(gameplay.SessionMode,
                Is.EqualTo(GameplaySessionMode.Daily));
            string dailyDate = gameplay.DailyDateForTests;
            int dailyIndex = gameplay.DailyIndexForTests;
            int dailySize = gameplay.CurrentPuzzleSize;
            var solution = new int[dailySize];
            for (int row = 0; row < dailySize; row++)
                solution[row] = gameplay.SolutionColumnForTests(row);
            Assert.That(dailyDate, Is.Not.Empty);
            Assert.That(rankRuntime.Manager.LevelCacheForTests, Is.EqualTo(17));
            Assert.That(rankRuntime.Manager.IsLevelCacheActiveForTests, Is.True);
            Assert.That(rankRuntime.Manager.IsInLevelForTests, Is.False,
                "DailyGame must not enter the Main-only Rank level lifecycle.");
            AssertDailyDidNotMutateMainState(state);

            yield return FailCurrentSession(
                gameplay,
                manager,
                UiName.DailyFail,
                UiName.DailyGame);
            UIFrameWindow dailyFail = manager.Get(UiName.DailyFail);
            Button revive = FindButton(
                dailyFail,
                "ReviveButton",
                requireInteractable: false);
            yield return WaitUntil(
                () => revive.interactable,
                TransitionTimeoutSeconds,
                "Daily rewarded ReviveButton did not unlock.");
            int showsBeforeRevive = adProvider.ShowCount;
            revive.onClick.Invoke();
            Assert.That(adProvider.ShowCount,
                Is.EqualTo(showsBeforeRevive + 1));
            Assert.That(adProvider.LastPosition,
                Is.EqualTo(TrackerCatalog.AdPosition.DailyGameFail));
            Assert.That(gameplay.SessionState,
                Is.EqualTo(GameSessionState.Failed));
            adProvider.EmitShown();
            adProvider.EmitRewarded();
            yield return WaitForState(manager, UiName.DailyFail,
                UiWindowState.Hidden);
            yield return WaitForSession(gameplay, GameSessionState.Playing);
            adProvider.EmitClosed();
            Assert.That(gameplay.LivesForTests, Is.EqualTo(1));
            Assert.That(state.GetGameTotalStat("daily", "revive_count"),
                Is.EqualTo(1));
            Assert.That(state.GetGameTotalStat("main", "revive_count"),
                Is.Zero);
            AssertDailyDidNotMutateMainState(state);

            Vector2Int lastWrong = FindEmptyWrongCell(gameplay);
            SessionActionResult failed = gameplay.DoubleTapForTests(
                lastWrong.x,
                lastWrong.y);
            Assert.That(failed.Accepted, Is.True);
            Assert.That(failed.Kind, Is.EqualTo(SessionActionKind.WrongGuess));
            Assert.That(failed.LivesAfter, Is.Zero);
            yield return WaitForSession(gameplay, GameSessionState.Failed);
            yield return WaitForState(manager, UiName.DailyFail,
                UiWindowState.Showing);
            dailyFail = manager.Get(UiName.DailyFail);
            Button restart = FindButton(
                dailyFail,
                "RestartButton",
                requireInteractable: false);
            yield return WaitUntil(
                () => restart.interactable,
                TransitionTimeoutSeconds,
                "Daily RestartButton did not unlock.");
            restart.onClick.Invoke();
            yield return WaitForState(manager, UiName.DailyFail,
                UiWindowState.Hidden);
            yield return WaitForSession(gameplay, GameSessionState.Playing);
            Assert.That(gameplay.SessionMode,
                Is.EqualTo(GameplaySessionMode.Daily));
            Assert.That(gameplay.RestartCountForTests, Is.EqualTo(1));
            Assert.That(gameplay.LivesForTests, Is.EqualTo(3));
            Assert.That(gameplay.DailyDateForTests, Is.EqualTo(dailyDate));
            Assert.That(gameplay.DailyIndexForTests, Is.EqualTo(dailyIndex));
            Assert.That(gameplay.CurrentPuzzleSize, Is.EqualTo(dailySize));
            for (int row = 0; row < dailySize; row++)
                Assert.That(gameplay.SolutionColumnForTests(row),
                    Is.EqualTo(solution[row]));
            Assert.That(rankRuntime.Manager.LevelCacheForTests, Is.EqualTo(17));
            Assert.That(rankRuntime.Manager.IsLevelCacheActiveForTests, Is.True);
            Assert.That(rankRuntime.Manager.IsInLevelForTests, Is.False);
            AssertDailyDidNotMutateMainState(state);

            SessionActionResult completed = gameplay.RunAutoComplete();
            Assert.That(completed.Accepted, Is.True);
            Assert.That(completed.IsComplete, Is.True);
            yield return CompletePreWinMetaFlow(manager, UiName.DailyWin);
            UIFrameWindow dailyWin = manager.Get(UiName.DailyWin);
            Assert.That(gameplay.SessionState, Is.EqualTo(GameSessionState.Won));
            Assert.That(state.DailyCompletedDate, Is.EqualTo(dailyDate));
            Assert.That(state.DailyElapsedSeconds, Is.GreaterThanOrEqualTo(0));
            Assert.That(state.DailyBeatPercent, Is.InRange(0f, 100f));
            Assert.That(rankRuntime.Manager.CollectTotal, Is.Zero,
                "Daily Win must not commit Main-only Rank collect.");
            Assert.That(rankRuntime.Manager.LevelCacheForTests, Is.EqualTo(17));
            Assert.That(rankRuntime.Manager.IsLevelCacheActiveForTests, Is.True);
            Assert.That(rankRuntime.Manager.IsInLevelForTests, Is.False);
            AssertDailyDidNotMutateMainState(state);

            Button dailyContinue = FindActiveButton(
                dailyWin,
                "Continue",
                requireInteractable: false);
            yield return WaitUntil(
                () => dailyContinue.interactable &&
                      !manager.IsInputBrieflyBlocked(
                          dailyWin.transform as RectTransform),
                3f,
                "Daily Continue did not unlock after the source 2 second gate.");
            dailyContinue.onClick.Invoke();
            yield return WaitForState(manager, UiName.DailyWin,
                UiWindowState.Hidden,
                15f);
            yield return WaitForState(manager, UiName.DailyGame,
                UiWindowState.Hidden,
                15f);
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Showing,
                15f);
            GameplayManager mainGameplay = manager.Get(UiName.Game)
                .GetComponentInChildren<GameplayManager>(true);
            Assert.That(mainGameplay, Is.Not.Null);
            Assert.That(mainGameplay, Is.Not.SameAs(gameplay),
                "Daily Continue must open the real Main Game page.");
            yield return WaitForSession(mainGameplay, GameSessionState.Playing);
            Assert.That(mainGameplay.SessionMode,
                Is.EqualTo(GameplaySessionMode.Main));
            Assert.That(mainGameplay.CurrentLevelNumber, Is.EqualTo(21));
            Assert.That(state.CurrentLevel, Is.EqualTo(21),
                "Daily Win must not advance Main progression.");
            Assert.That(manager.IsAnyLoading, Is.False);
        }

        [UnityTest]
        public IEnumerator AppScene_LifecycleBoundary_PreservesPlayingFailReviveWinAndNextState()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);

            FindButton(manager.Get(UiName.Home), "StartBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Showing);
            GameplayManager gameplay = manager.Get(UiName.Game)
                .GetComponentInChildren<GameplayManager>(true);
            Assert.That(gameplay, Is.Not.Null);
            yield return WaitForSession(gameplay, GameSessionState.Playing);

            Vector2Int markedCell = FindEmptyWrongCell(gameplay);
            Assert.That(
                gameplay.ApplyCellStateForTests(
                    markedCell.x,
                    markedCell.y,
                    CellStateType.MARK),
                Is.True);
            Assert.That(GameStateRuntime.Current.GetEndgameSnapshot(), Is.Empty,
                "A MARK must remain debounced until its save deadline or a lifecycle boundary.");

            gameplay.SuspendApplicationForTests();
            Dictionary<string, object> playingSnapshot =
                GameStateRuntime.Current.GetEndgameSnapshot();
            Assert.That(playingSnapshot, Is.Not.Empty);
            Assert.That(playingSnapshot["level"], Is.EqualTo(1));
            Assert.That(((IList)playingSnapshot["marks"]).Count, Is.EqualTo(1));
            Assert.That(Convert.ToDouble(playingSnapshot["in_game_sec"]),
                Is.EqualTo(gameplay.SnapshotElapsedSecondsForTests).Within(0.05));

            gameplay.SuspendApplicationForTests();
            Assert.That(
                GameStateRuntime.Current.GetEndgameSnapshot(),
                Is.SameAs(playingSnapshot),
                "Focus-out plus pause must share one durability boundary.");
            gameplay.ResumeApplicationForTests();

            yield return FailCurrentSession(gameplay, manager);
            gameplay.SuspendApplicationForTests();
            Dictionary<string, object> failedSnapshot =
                GameStateRuntime.Current.GetEndgameSnapshot();
            Assert.That(failedSnapshot["lives"], Is.EqualTo(0));
            Assert.That(failedSnapshot["level"], Is.EqualTo(1));
            gameplay.ResumeApplicationForTests();

            Assert.That(gameplay.ReviveFromFail(1), Is.True);
            yield return WaitForState(manager, UiName.Fail,
                UiWindowState.Hidden);
            yield return WaitForSession(gameplay, GameSessionState.Playing);
            Assert.That(GameStateRuntime.Current.GetEndgameSnapshot()["lives"],
                Is.EqualTo(1));
            gameplay.SuspendApplicationForTests();
            Assert.That(GameStateRuntime.Current.GetEndgameSnapshot()["lives"],
                Is.EqualTo(1));
            gameplay.ResumeApplicationForTests();

            SessionActionResult completed = gameplay.RunAutoComplete();
            Assert.That(completed.Accepted, Is.True);
            Assert.That(completed.IsComplete, Is.True);
            yield return CompletePreWinMetaFlow(manager);
            Assert.That(GameStateRuntime.Current.GetEndgameSnapshot(), Is.Empty,
                "Win settlement must clear the resumable snapshot.");
            gameplay.SuspendApplicationForTests();
            Assert.That(GameStateRuntime.Current.GetEndgameSnapshot(), Is.Empty,
                "Suspending on Win must not recreate a completed snapshot.");
            gameplay.ResumeApplicationForTests();

            UIFrameWindow win = manager.Get(UiName.Win);
            Button next = FindActiveButton(win, "Next", false);
            yield return WaitUntil(
                () => next.interactable,
                TransitionTimeoutSeconds,
                "Win Next button did not become interactable.");
            next.onClick.Invoke();
            yield return WaitUntil(
                () => gameplay.CurrentLevelNumber == 2 &&
                      gameplay.SessionState == GameSessionState.Playing,
                15f,
                "Next did not load level 2.");

            Vector2Int levelTwoMark = FindEmptyWrongCell(gameplay);
            Assert.That(
                gameplay.ApplyCellStateForTests(
                    levelTwoMark.x,
                    levelTwoMark.y,
                    CellStateType.MARK),
                Is.True);
            gameplay.SuspendApplicationForTests();
            Dictionary<string, object> nextSnapshot =
                GameStateRuntime.Current.GetEndgameSnapshot();
            Assert.That(nextSnapshot["level"], Is.EqualTo(2));
            Assert.That(((IList)nextSnapshot["marks"]).Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator AppScene_FailRewardedRevive_RequiresRewardAndRecoversAfterCloseFailure()
        {
            AdRuntime adRuntime = Find<AdRuntime>();
            Assert.That(adRuntime, Is.Not.Null,
                "AppScene is missing AdRuntime.");
            PlayModeAdProvider provider =
                adRuntime.gameObject.AddComponent<PlayModeAdProvider>();
            adRuntime.BindProvider(provider);

            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);

            UIFrameWindow home = manager.Get(UiName.Home);
            FindButton(home, "StartBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Showing);
            UIFrameWindow game = manager.Get(UiName.Game);
            GameplayManager gameplay =
                game.GetComponentInChildren<GameplayManager>(true);
            Assert.That(gameplay, Is.Not.Null);
            yield return WaitForSession(gameplay, GameSessionState.Playing);

            yield return FailCurrentSession(gameplay, manager);
            UIFrameWindow fail = manager.Get(UiName.Fail);
            Button revive = FindButton(
                fail,
                "ReviveButton",
                requireInteractable: false);
            yield return WaitUntil(
                () => revive.interactable,
                TransitionTimeoutSeconds,
                "Rewarded ReviveButton did not unlock.");

            revive.onClick.Invoke();
            Assert.That(provider.ShowCount, Is.EqualTo(1));
            Assert.That(provider.LastPlacementId,
                Is.EqualTo(TrackerCatalog.Placement.Reward));
            Assert.That(provider.LastPosition,
                Is.EqualTo(TrackerCatalog.AdPosition.NormalGameFail));
            Assert.That(gameplay.SessionState,
                Is.EqualTo(GameSessionState.Failed),
                "Opening a rewarded ad must not revive before its reward callback.");

            provider.EmitShown();
            provider.EmitRewarded();
            yield return WaitForState(manager, UiName.Fail,
                UiWindowState.Hidden);
            yield return WaitForSession(gameplay, GameSessionState.Playing);
            Assert.That(gameplay.LivesForTests, Is.EqualTo(1));
            Assert.That(
                GameStateRuntime.Current.GetGameTotalStat(
                    "main",
                    "revive_count"),
                Is.EqualTo(1));

            provider.EmitClosed();
            yield return null;
            Assert.That(gameplay.SessionState,
                Is.EqualTo(GameSessionState.Playing),
                "Closing an already rewarded ad must not settle revive twice.");
            Assert.That(gameplay.LivesForTests, Is.EqualTo(1));

            Vector2Int wrongCell = FindEmptyWrongCell(gameplay);
            SessionActionResult failed = gameplay.DoubleTapForTests(
                wrongCell.x,
                wrongCell.y);
            Assert.That(failed.Accepted, Is.True);
            Assert.That(failed.Kind, Is.EqualTo(SessionActionKind.WrongGuess));
            Assert.That(failed.LivesAfter, Is.EqualTo(0));
            yield return WaitForSession(gameplay, GameSessionState.Failed);
            yield return WaitForState(manager, UiName.Fail,
                UiWindowState.Showing);

            fail = manager.Get(UiName.Fail);
            revive = FindButton(
                fail,
                "ReviveButton",
                requireInteractable: false);
            yield return WaitUntil(
                () => revive.interactable,
                TransitionTimeoutSeconds,
                "Reused Fail ReviveButton did not unlock.");
            revive.onClick.Invoke();
            Assert.That(provider.ShowCount, Is.EqualTo(2));
            provider.EmitShown();
            provider.EmitClosed();
            yield return null;

            AssertShowing(manager, UiName.Fail);
            Assert.That(gameplay.SessionState,
                Is.EqualTo(GameSessionState.Failed),
                "Closing without ad_rewarded must not revive the session.");
            Assert.That(gameplay.LivesForTests, Is.EqualTo(0));
            Assert.That(revive.interactable, Is.True,
                "A failed reward attempt must re-enable ReviveButton.");
            Assert.That(
                GameStateRuntime.Current.GetGameTotalStat(
                    "main",
                    "revive_count"),
                Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator AppScene_BankSpecialButtons_LaunchAndReturn()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);

            UIFrameWindow bank = manager.Show(UiName.Bank);
            Assert.That(bank, Is.Not.Null);
            AssertShowing(manager, UiName.Bank);

            BankRootCardView specialCard = FindNamedComponent<BankRootCardView>(
                bank,
                "SPCard");
            FindOnlyButton(specialCard).onClick.Invoke();
            yield return null;

            BankLevelRowView firstSpecial =
                FindNamedComponent<BankLevelRowView>(bank, "SpecialRow1");
            Assert.That(firstSpecial.gameObject.activeInHierarchy, Is.True,
                "SP root card did not open the special-level list.");
            FindOnlyButton(firstSpecial).onClick.Invoke();

            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Showing);
            UIFrameWindow game = manager.Get(UiName.Game);
            GameplayManager gameplay =
                game.GetComponentInChildren<GameplayManager>(true);
            Assert.That(gameplay, Is.Not.Null);
            yield return WaitForSession(gameplay, GameSessionState.Playing);
            Assert.That(gameplay.SessionMode, Is.EqualTo(GameplaySessionMode.Bank));
            Assert.That(gameplay.CurrentPuzzleSize, Is.GreaterThan(0));

            Button returnBank = FindButton(game, "ReturnBankBtn");
            Assert.That(returnBank.gameObject.activeInHierarchy, Is.True,
                "A bank session must expose ReturnBankBtn.");
            returnBank.onClick.Invoke();
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Hidden);
            yield return WaitForState(manager, UiName.Bank,
                UiWindowState.Showing);

            Assert.That(specialCard.gameObject.activeInHierarchy, Is.True,
                "Returning from Game must restore Bank at its root panel.");
            Assert.That(manager.IsAnyLoading, Is.False);
        }

        [UnityTest]
        public IEnumerator AppScene_BankSpecialWinNextAndFailRestart_PreserveSession()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);

            UIFrameWindow bank = manager.Show(UiName.Bank);
            BankRootCardView specialCard = FindNamedComponent<BankRootCardView>(
                bank,
                "SPCard");
            FindOnlyButton(specialCard).onClick.Invoke();
            yield return null;
            BankLevelRowView firstSpecial =
                FindNamedComponent<BankLevelRowView>(bank, "SpecialRow1");
            FindOnlyButton(firstSpecial).onClick.Invoke();

            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Showing);
            UIFrameWindow game = manager.Get(UiName.Game);
            GameplayManager gameplay =
                game.GetComponentInChildren<GameplayManager>(true);
            Assert.That(gameplay, Is.Not.Null);
            yield return WaitForSession(gameplay, GameSessionState.Playing);
            Assert.That(gameplay.BankIndexForTests, Is.EqualTo(1));

            SessionActionResult completed = gameplay.RunAutoComplete();
            Assert.That(completed.Accepted, Is.True);
            Assert.That(completed.IsComplete, Is.True);
            yield return CompletePreWinMetaFlow(manager);

            UIFrameWindow win = manager.Get(UiName.Win);
            Button next = FindActiveButton(win, "Next", false);
            yield return WaitUntil(
                () => next.interactable,
                TransitionTimeoutSeconds,
                "Bank Win Next button did not become interactable.");
            next.onClick.Invoke();

            yield return WaitForState(
                manager,
                UiName.Win,
                UiWindowState.Hidden,
                15f);
            yield return WaitUntil(
                () => gameplay.SessionMode == GameplaySessionMode.Bank &&
                      gameplay.BankIndexForTests == 2 &&
                      gameplay.SessionState == GameSessionState.Playing,
                15f,
                "Bank Next did not load SP entry #2.");

            Button returnBank = FindButton(
                game,
                "ReturnBankBtn",
                requireInteractable: false,
                requireActive: false);
            Assert.That(returnBank.gameObject.activeInHierarchy, Is.False,
                "Bank Next must drop the direct-browser return control like the source.");

            yield return FailCurrentSession(gameplay, manager);
            UIFrameWindow fail = manager.Get(UiName.Fail);
            Button restart = FindButton(fail, "RestartButton", false);
            yield return WaitUntil(
                () => restart.isActiveAndEnabled && restart.interactable,
                TransitionTimeoutSeconds,
                "Bank Fail RestartButton did not unlock.");
            restart.onClick.Invoke();
            yield return WaitForState(manager, UiName.Fail,
                UiWindowState.Hidden);
            yield return WaitForSession(gameplay, GameSessionState.Playing);

            Assert.That(gameplay.SessionMode, Is.EqualTo(GameplaySessionMode.Bank));
            Assert.That(gameplay.BankIndexForTests, Is.EqualTo(2));
            Assert.That(gameplay.LivesForTests, Is.EqualTo(3));
            Assert.That(returnBank.gameObject.activeInHierarchy, Is.False,
                "Restarting a post-Next bank entry must not restore direct-return UI.");
            AssertShowing(manager, UiName.Game);
            Assert.That(manager.IsAnyLoading, Is.False);
        }

        [UnityTest]
        public IEnumerator AppScene_BankPoolMatrix_LaunchNextAndReuseDynamicRows()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);

            var branches = new[]
            {
                ("RegularCard", BankPoolKind.Regular,
                    BankUiFlow.SizeThenTier),
                ("LKCard", BankPoolKind.Lk,
                    BankUiFlow.LevelRows),
                ("LKModifiedCard", BankPoolKind.LkModified,
                    BankUiFlow.LevelRows),
                ("LKStyleCard", BankPoolKind.LkStyle,
                    BankUiFlow.SizeThenTier),
                ("GCCard", BankPoolKind.Gc,
                    BankUiFlow.SizeThenTier)
            };

            UIFrameWindow bank = manager.Show(UiName.Bank);
            Assert.That(bank, Is.Not.Null);
            BankBrowserPagePresenter browser =
                bank.GetComponent<BankBrowserPagePresenter>();
            Assert.That(browser, Is.Not.Null);
            UIFrameWindow game = null;
            GameplayManager gameplay = null;

            foreach ((string rootName, BankPoolKind pool, BankUiFlow flow)
                     in branches)
            {
                BankRootCardView root = FindNamedComponent<BankRootCardView>(
                    bank,
                    rootName);
                Assert.That(root.gameObject.activeInHierarchy, Is.True,
                    rootName + " is unavailable with the shipped bank data.");
                FindOnlyButton(root).onClick.Invoke();
                yield return null;

                int expectedLaunchIndex = 1;

                if (flow == BankUiFlow.SizeThenTier)
                {
                    BankSizeCardView size =
                        FindFirstActiveComponent<BankSizeCardView>(bank);
                    FindOnlyButton(size).onClick.Invoke();
                    yield return null;
                    BankTierCardView tier =
                        FindFirstActiveComponent<BankTierCardView>(bank);
                    Assert.That(tier.CountForTests, Is.GreaterThan(0));
                    Assert.That(tier.NumberForTests, Is.EqualTo(1));
                    Button minus = FindChildButton(
                        tier,
                        "MinusBtn",
                        requireInteractable: false);
                    Assert.That(minus.interactable, Is.False,
                        rootName + " tier selector must clamp at one.");
                    minus.onClick.Invoke();
                    Assert.That(tier.NumberForTests, Is.EqualTo(1));
                    Button plus = FindChildButton(
                        tier,
                        "PlusBtn",
                        requireInteractable: false);
                    if (tier.CountForTests > 1)
                    {
                        Assert.That(plus.interactable, Is.True);
                        plus.onClick.Invoke();
                        Assert.That(tier.NumberForTests, Is.EqualTo(2));
                        Assert.That(minus.interactable, Is.True);
                        minus.onClick.Invoke();
                        Assert.That(tier.NumberForTests, Is.EqualTo(1));
                        Assert.That(minus.interactable, Is.False);
                        if (pool == BankPoolKind.Regular)
                        {
                            for (int number = 1;
                                 number < tier.CountForTests;
                                 number++)
                                plus.onClick.Invoke();
                            plus.onClick.Invoke();
                            Assert.That(tier.NumberForTests,
                                Is.EqualTo(tier.CountForTests));
                            Assert.That(plus.interactable, Is.False,
                                "Tier selector must clamp at its source count.");
                            for (int number = tier.CountForTests;
                                 number > 1;
                                 number--)
                                minus.onClick.Invoke();
                            minus.onClick.Invoke();
                            Assert.That(tier.NumberForTests, Is.EqualTo(1));
                        }
                        plus.onClick.Invoke();
                        expectedLaunchIndex = 2;
                    }
                    FindChildButton(tier, "GoBtn").onClick.Invoke();
                }
                else
                {
                    Assert.That(browser.StateForTests.Panel,
                        Is.EqualTo(BankBrowserPanel.LkList));
                    Assert.That(browser.LkCountForTests, Is.GreaterThan(0));
                    Assert.That(browser.LkNumberForTests, Is.EqualTo(1));
                    Button minus = FindActiveButton(
                        bank,
                        "MinusBtn",
                        requireInteractable: false);
                    Assert.That(minus.interactable, Is.False,
                        rootName + " selector must clamp at one.");
                    minus.onClick.Invoke();
                    Assert.That(browser.LkNumberForTests, Is.EqualTo(1));
                    Button plus = FindActiveButton(
                        bank,
                        "PlusBtn",
                        requireInteractable: false);
                    if (browser.LkCountForTests > 1)
                    {
                        Assert.That(plus.interactable, Is.True);
                        plus.onClick.Invoke();
                        Assert.That(browser.LkNumberForTests, Is.EqualTo(2));
                        Assert.That(minus.interactable, Is.True);
                        minus.onClick.Invoke();
                        Assert.That(browser.LkNumberForTests, Is.EqualTo(1));
                        Assert.That(minus.interactable, Is.False);
                        if (pool == BankPoolKind.Lk)
                        {
                            for (int number = 1;
                                 number < browser.LkCountForTests;
                                 number++)
                                plus.onClick.Invoke();
                            plus.onClick.Invoke();
                            Assert.That(browser.LkNumberForTests,
                                Is.EqualTo(browser.LkCountForTests));
                            Assert.That(plus.interactable, Is.False,
                                "LK selector must clamp at its source count.");
                            for (int number = browser.LkCountForTests;
                                 number > 1;
                                 number--)
                                minus.onClick.Invoke();
                            minus.onClick.Invoke();
                            Assert.That(browser.LkNumberForTests,
                                Is.EqualTo(1));
                        }
                        plus.onClick.Invoke();
                        expectedLaunchIndex = 2;
                    }
                    FindActiveButton(bank, "GoBtn").onClick.Invoke();
                }

                yield return WaitForState(manager, UiName.Game,
                    UiWindowState.Showing);
                game = manager.Get(UiName.Game);
                gameplay = game.GetComponentInChildren<GameplayManager>(true);
                Assert.That(gameplay, Is.Not.Null);
                yield return WaitForSession(gameplay,
                    GameSessionState.Playing);
                Assert.That(gameplay.SessionMode,
                    Is.EqualTo(GameplaySessionMode.Bank));
                Assert.That(gameplay.BankPoolForTests, Is.EqualTo(pool));
                Assert.That(gameplay.BankIndexForTests,
                    Is.EqualTo(expectedLaunchIndex));
                Assert.That(gameplay.BankTotalForTests, Is.GreaterThan(0));

                Button directReturn = FindButton(
                    game,
                    "ReturnBankBtn",
                    requireInteractable: false,
                    requireActive: false);
                Assert.That(directReturn.gameObject.activeInHierarchy, Is.True,
                    rootName + " launch must expose direct Bank return.");

                int expectedNext =
                    gameplay.BankIndexForTests % gameplay.BankTotalForTests + 1;
                SessionActionResult completed = gameplay.RunAutoComplete();
                Assert.That(completed.Accepted, Is.True);
                Assert.That(completed.IsComplete, Is.True);
                yield return CompletePreWinMetaFlow(manager);

                UIFrameWindow win = manager.Get(UiName.Win);
                Button next = FindActiveButton(win, "Next", false);
                yield return WaitUntil(
                    () => next.interactable,
                    TransitionTimeoutSeconds,
                    rootName + " Win Next button did not unlock.");
                next.onClick.Invoke();
                yield return WaitForState(
                    manager,
                    UiName.Win,
                    UiWindowState.Hidden,
                    15f);
                yield return WaitUntil(
                    () => gameplay.SessionState == GameSessionState.Playing &&
                          gameplay.BankPoolForTests == pool &&
                          gameplay.BankIndexForTests == expectedNext,
                    15f,
                    rootName + " Next did not preserve its Bank pool/index.");
                Assert.That(directReturn.gameObject.activeInHierarchy, Is.False,
                    rootName + " Next must drop direct-browser return.");

                manager.Show(UiName.Bank);
                manager.Hide(UiName.Game);
                yield return WaitForState(manager, UiName.Game,
                    UiWindowState.Hidden);
                AssertShowing(manager, UiName.Bank);
            }

            int sizePoolCount =
                bank.GetComponentsInChildren<BankSizeCardView>(true).Length;
            int tierPoolCount =
                bank.GetComponentsInChildren<BankTierCardView>(true).Length;

            for (int cycle = 0; cycle < 8; cycle++)
            {
                BankRootCardView regular =
                    FindNamedComponent<BankRootCardView>(bank, "RegularCard");
                FindOnlyButton(regular).onClick.Invoke();
                yield return null;
                Assert.That(browser.StateForTests.Panel,
                    Is.EqualTo(BankBrowserPanel.RegularSize));
                BankSizeCardView size =
                    FindFirstActiveComponent<BankSizeCardView>(bank);
                FindOnlyButton(size).onClick.Invoke();
                yield return null;
                Assert.That(browser.StateForTests.Panel,
                    Is.EqualTo(BankBrowserPanel.Tier));
                Assert.That(manager.RequestBack(), Is.True);
                yield return null;
                Assert.That(browser.StateForTests.Panel,
                    Is.EqualTo(BankBrowserPanel.RegularSize));
                Assert.That(manager.RequestBack(), Is.True);
                yield return null;
                Assert.That(browser.StateForTests.Panel,
                    Is.EqualTo(BankBrowserPanel.Root));
            }

            BankRootCardView lkStyle =
                FindNamedComponent<BankRootCardView>(bank, "LKStyleCard");
            FindOnlyButton(lkStyle).onClick.Invoke();
            yield return null;
            FindOnlyButton(FindFirstActiveComponent<BankSizeCardView>(bank))
                .onClick.Invoke();
            yield return null;
            Assert.That(browser.StateForTests.Panel,
                Is.EqualTo(BankBrowserPanel.Tier));
            Assert.That(manager.RequestBack(), Is.True);
            yield return null;
            Assert.That(browser.StateForTests.Panel,
                Is.EqualTo(BankBrowserPanel.VariantSize));
            Assert.That(manager.RequestBack(), Is.True);
            yield return null;

            BankRootCardView gc =
                FindNamedComponent<BankRootCardView>(bank, "GCCard");
            FindOnlyButton(gc).onClick.Invoke();
            yield return null;
            FindOnlyButton(FindFirstActiveComponent<BankSizeCardView>(bank))
                .onClick.Invoke();
            yield return null;
            Assert.That(manager.RequestBack(), Is.True);
            yield return null;
            Assert.That(browser.StateForTests.Panel,
                Is.EqualTo(BankBrowserPanel.Root),
                "GC Tier back follows the source and returns to Bank root.");

            foreach (string rootName in new[] { "LKCard", "LKModifiedCard" })
            {
                FindOnlyButton(FindNamedComponent<BankRootCardView>(
                    bank,
                    rootName)).onClick.Invoke();
                yield return null;
                Assert.That(browser.StateForTests.Panel,
                    Is.EqualTo(BankBrowserPanel.LkList));
                Assert.That(manager.RequestBack(), Is.True);
                yield return null;
                Assert.That(browser.StateForTests.Panel,
                    Is.EqualTo(BankBrowserPanel.Root));
            }

            FindOnlyButton(FindNamedComponent<BankRootCardView>(
                bank,
                "SPCard")).onClick.Invoke();
            yield return null;
            Assert.That(browser.StateForTests.Panel,
                Is.EqualTo(BankBrowserPanel.LevelList));
            Assert.That(manager.RequestBack(), Is.True);
            yield return null;
            Assert.That(browser.StateForTests.Panel,
                Is.EqualTo(BankBrowserPanel.Root));

            int levelPoolCount =
                bank.GetComponentsInChildren<BankLevelRowView>(true).Length;
            FindOnlyButton(FindNamedComponent<BankRootCardView>(
                bank,
                "SPCard")).onClick.Invoke();
            yield return null;
            Assert.That(manager.RequestBack(), Is.True);
            yield return null;

            Assert.That(
                bank.GetComponentsInChildren<BankSizeCardView>(true).Length,
                Is.EqualTo(sizePoolCount),
                "Back-stack stress must reuse dynamic size rows.");
            Assert.That(
                bank.GetComponentsInChildren<BankTierCardView>(true).Length,
                Is.EqualTo(tierPoolCount),
                "Back-stack stress must reuse dynamic tier rows.");
            Assert.That(
                bank.GetComponentsInChildren<BankLevelRowView>(true).Length,
                Is.EqualTo(levelPoolCount),
                "Back-stack stress must reuse dynamic level rows.");
            Assert.That(manager.IsAnyLoading, Is.False);
        }

        private static IEnumerator CompletePreWinMetaFlow(
            UIManager manager,
            UiName resultRoute = UiName.Win)
        {
            float deadline = Time.realtimeSinceStartup + 35f;
            while (!IsShowing(manager, resultRoute) &&
                   Time.realtimeSinceStartup < deadline)
            {
                if (IsShowing(manager, UiName.Award))
                {
                    yield return CollectAward(manager);
                    continue;
                }

                if (IsShowing(manager, UiName.Streak))
                {
                    UIFrameWindow streak = manager.Get(UiName.Streak);
                    Button lit = FindButton(
                        streak,
                        "LitTapSurface",
                        requireInteractable: false,
                        requireActive: false);
                    if (lit.gameObject.activeInHierarchy)
                    {
                        yield return WaitUntil(
                            () => lit.interactable,
                            TransitionTimeoutSeconds,
                            "Streak Lit input did not unlock.");
                        lit.onClick.Invoke();
                    }

                    Button claim = FindButton(
                        streak,
                        "ClaimBtn",
                        requireInteractable: false,
                        requireActive: false);
                    while ((!claim.gameObject.activeInHierarchy ||
                            !claim.interactable) &&
                           Time.realtimeSinceStartup < deadline)
                    {
                        if (IsShowing(manager, UiName.Award))
                            yield return CollectAward(manager);
                        else
                            yield return null;
                    }
                    Assert.That(claim.gameObject.activeInHierarchy, Is.True,
                        "Streak ClaimBtn did not become active.");
                    Assert.That(claim.interactable, Is.True,
                        "Streak ClaimBtn did not unlock.");
                    claim.onClick.Invoke();
                    yield return WaitForState(
                        manager,
                        UiName.Streak,
                        UiWindowState.Hidden,
                        10f);
                    continue;
                }

                yield return null;
            }

            Assert.That(IsShowing(manager, resultRoute), Is.True,
                resultRoute +
                " did not appear after completing pre-result meta flow.");
        }

        private static IEnumerator CollectAward(UIManager manager)
        {
            UIFrameWindow award = manager.Get(UiName.Award);
            Button collect = FindActiveButton(
                award,
                "CollectBtn",
                requireInteractable: false);
            yield return WaitUntil(
                () => collect.interactable,
                5f,
                "Award CollectBtn did not unlock.");
            collect.onClick.Invoke();
            yield return WaitForState(
                manager,
                UiName.Award,
                UiWindowState.Hidden,
                10f);
        }

        private static IEnumerator CollectRankGift(UIManager manager)
        {
            UIFrameWindow award = manager.Get(UiName.Award);
            Button podiumCollect = FindActiveButton(
                award,
                "CollectBtn",
                requireInteractable: false);
            yield return WaitUntil(
                () => podiumCollect.interactable,
                5f,
                "Rank Gift podium CollectBtn did not unlock.");
            podiumCollect.onClick.Invoke();
            yield return null;
            Assert.That(IsShowing(manager, UiName.Award), Is.True,
                "Rank Gift with a chest must keep Award open for item phase.");

            yield return WaitUntil(
                () => !podiumCollect.gameObject.activeInHierarchy,
                5f,
                "Rank Gift chest did not reach its source item-phase cue.");

            Button itemCollect = FindActiveButton(
                award,
                "CollectBtn",
                requireInteractable: false);
            Assert.That(itemCollect, Is.Not.SameAs(podiumCollect));
            yield return WaitUntil(
                () => itemCollect.interactable,
                5f,
                "Rank Gift item CollectBtn did not unlock.");
            itemCollect.onClick.Invoke();
            yield return WaitForState(
                manager,
                UiName.Award,
                UiWindowState.Hidden,
                10f);
        }

        private static IEnumerator PresentPendingStreak(
            UIManager manager,
            StreakFeature streak,
            int checkedBeforeReveal,
            int checkedAfterReveal)
        {
            Assert.That(streak.HasPendingShow, Is.True);
            int pendingUid = streak.PendingShowUid;
            StreakDisplayState requested =
                streak.Data.CurrentStreak == 1 &&
                !streak.ShouldSkipLit
                    ? StreakDisplayState.Lit
                    : StreakDisplayState.Settle;
            UIFrameWindow frame = manager.Show(
                UiName.Streak,
                new Dictionary<string, object>
                {
                    [StreakPagePresenter.StateParameter] = (int)requested
                });
            Assert.That(frame, Is.Not.Null);
            yield return WaitForState(manager, UiName.Streak,
                UiWindowState.Showing);
            StreakPagePresenter page =
                frame.GetComponentInChildren<StreakPagePresenter>(true);
            Assert.That(page, Is.Not.Null);
            Assert.That(page.StateForTests, Is.EqualTo(requested));

            if (requested == StreakDisplayState.Lit)
            {
                Button lit = FindButton(
                    frame,
                    "LitTapSurface",
                    requireInteractable: false,
                    requireActive: false);
                yield return WaitUntil(
                    () => lit.interactable,
                    TransitionTimeoutSeconds,
                    "Streak Lit input did not unlock.");
                lit.onClick.Invoke();
                Assert.That(page.StateForTests,
                    Is.EqualTo(StreakDisplayState.Settle));
            }

            Assert.That(CountCheckedStreakSlots(page),
                Is.EqualTo(checkedBeforeReveal),
                "Settle must initially hide the newest check-in like Godot.");
            Button claim = FindButton(
                frame,
                "ClaimBtn",
                requireInteractable: false,
                requireActive: false);
            Assert.That(claim.interactable, Is.False);

            if (pendingUid > 0)
            {
                yield return WaitForState(manager, UiName.Award,
                    UiWindowState.Showing,
                    8f);
                yield return CollectAward(manager);
            }

            yield return WaitUntil(
                () => claim.interactable,
                5f,
                "Streak Continue did not unlock after settle.");
            Assert.That(page.SettleRevealCompleteForTests, Is.True);
            Assert.That(CountCheckedStreakSlots(page),
                Is.EqualTo(checkedAfterReveal),
                "Settle did not reveal the new check-in slot.");
            claim.onClick.Invoke();
            yield return WaitForState(manager, UiName.Streak,
                UiWindowState.Hidden,
                10f);
        }

        [UnityTest]
        public IEnumerator PlatformGameplayLayout_ShortLongAndSafeInsetsKeepPrimaryRegionsVisible()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);

            UIFrameWindow home = manager.Get(UiName.Home);
            Assert.That(home, Is.Not.Null);
            FindButton(home, "StartBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Showing);

            UIFrameWindow game = manager.Get(UiName.Game);
            GameplayManager gameplay =
                game.GetComponentInChildren<GameplayManager>(true);
            GameplayPageLayoutPresenter presenter =
                game.GetComponentInChildren<GameplayPageLayoutPresenter>(true);
            Assert.That(gameplay, Is.Not.Null);
            Assert.That(presenter, Is.Not.Null,
                "GamePage prefab is missing GameplayPageLayoutPresenter.");
            yield return WaitForSession(gameplay, GameSessionState.Playing);

            RectTransform header = presenter.HeaderForTests;
            RectTransform catHeart = presenter.CatHeartRowForTests;
            RectTransform rule = presenter.RuleBarForTests;
            RectTransform board = presenter.BoardForTests;
            RectTransform tools = presenter.BottomToolsForTests;
            Assert.That(header, Is.Not.Null);
            Assert.That(catHeart, Is.Not.Null);
            Assert.That(rule, Is.Not.Null);
            Assert.That(board, Is.Not.Null);
            Assert.That(tools, Is.Not.Null,
                "BottomTools is not wired into the page layout presenter.");

            Assert.That(header.rect.height, Is.EqualTo(120f).Within(0.1f));
            Assert.That(catHeart.rect.height, Is.EqualTo(88f).Within(0.1f));
            Assert.That(rule.rect.height,
                Is.EqualTo(SourceGameplayPageLayout.RuleBarHeight).Within(0.1f));
            Assert.That(tools.rect.height, Is.EqualTo(200f).Within(0.1f));
            float visibleBoardHeight = gameplay.boardView.VisibleBoardPixels;
            Assert.That(board.rect.height * Mathf.Abs(board.localScale.y),
                Is.EqualTo(visibleBoardHeight).Within(0.1f));

            var profiles = new[]
            {
                new Vector3(1920f, 0f, 0f),
                new Vector3(2160f, 0f, 0f),
                new Vector3(2400f, 0f, 0f),
                new Vector3(1920f, 96f, 54f),
                new Vector3(2400f, 120f, 80f)
            };
            foreach (Vector3 profile in profiles)
            {
                float viewportHeight = profile.x;
                float topInset = profile.y;
                float bottomInset = profile.z;
                SourceGameplayPageLayoutResult expected =
                    SourceGameplayPageLayout.Calculate(
                        viewportHeight,
                        topInset,
                        bottomInset,
                        visibleBoardHeight,
                        gameplay.boardView.UsesEnlargedBoard);

                presenter.ApplyLayoutForTests(
                    viewportHeight,
                    topInset,
                    bottomInset);

                AssertLayoutCenter(header, expected.HeaderCenterY);
                AssertLayoutCenter(catHeart, expected.CatHeartCenterY);
                AssertLayoutCenter(rule, expected.RuleCenterY);
                AssertLayoutCenter(board, expected.BoardCenterY);
                AssertLayoutCenter(tools, expected.BottomToolsCenterY);

                float safeTop = viewportHeight * 0.5f - topInset;
                float safeBottom = -viewportHeight * 0.5f + bottomInset;
                Assert.That(header.anchoredPosition.y + header.rect.height * 0.5f,
                    Is.LessThanOrEqualTo(safeTop + 0.1f),
                    "Header crossed the top safe edge.");
                Assert.That(board.anchoredPosition.y - visibleBoardHeight * 0.5f,
                    Is.GreaterThanOrEqualTo(safeBottom - 0.1f),
                    "Board crossed the bottom safe edge.");
                Assert.That(tools.anchoredPosition.y - tools.rect.height * 0.5f,
                    Is.GreaterThanOrEqualTo(safeBottom - 0.1f),
                    "BottomTools crossed the gesture safe edge.");
                Assert.That(header.anchoredPosition.y,
                    Is.GreaterThan(catHeart.anchoredPosition.y));
                Assert.That(catHeart.anchoredPosition.y,
                    Is.GreaterThan(rule.anchoredPosition.y));
                Assert.That(rule.anchoredPosition.y,
                    Is.GreaterThan(board.anchoredPosition.y));
                Assert.That(board.anchoredPosition.y,
                    Is.GreaterThan(tools.anchoredPosition.y));
            }

            presenter.ApplyLayout();
            AssertShowing(manager, UiName.Game);
        }

        private static void AssertLayoutCenter(
            RectTransform rect,
            float expectedY)
        {
            Assert.That(rect.anchorMin,
                Is.EqualTo(new Vector2(0.5f, 0.5f)), rect.name);
            Assert.That(rect.anchorMax,
                Is.EqualTo(new Vector2(0.5f, 0.5f)), rect.name);
            Assert.That(rect.pivot,
                Is.EqualTo(new Vector2(0.5f, 0.5f)), rect.name);
            Assert.That(rect.anchoredPosition.x,
                Is.EqualTo(0f).Within(0.001f), rect.name);
            Assert.That(rect.anchoredPosition.y,
                Is.EqualTo(expectedY).Within(0.02f), rect.name);
        }

        private static int CountCheckedWeekSlots(StreakFeature streak)
        {
            int count = 0;
            IReadOnlyList<StreakWeekSlot> slots = streak.GetWeekSlots();
            for (int index = 0; index < slots.Count; index++)
                if (slots[index].IsChecked)
                    count++;
            return count;
        }

        private static int CountCheckedStreakSlots(StreakPagePresenter page)
        {
            int count = 0;
            StreakDaySlotView[] slots =
                page.GetComponentsInChildren<StreakDaySlotView>(true);
            Assert.That(slots.Length, Is.EqualTo(StreakFeature.CycleLength));
            for (int index = 0; index < slots.Length; index++)
                if (slots[index].IsCheckedForTests)
                    count++;
            return count;
        }

        private static IEnumerator ShowThenHide(
            UIManager manager,
            UiName route)
        {
            UIFrameWindow page = manager.Show(route);
            Assert.That(page, Is.Not.Null, route + " is not registered.");
            Assert.That(page.WindowState, Is.EqualTo(UiWindowState.Showing));
            manager.Hide(route);
            yield return WaitForState(manager, route, UiWindowState.Hidden);
        }

        private static string ExpectedLevelText(
            LocalizationCatalog localization,
            int level)
        {
            string format = localization.Translate("GAME_LEVEL_TITLE");
            if (format == "GAME_LEVEL_TITLE") format = "Level %d";
            return format.Replace("%d", level.ToString());
        }

        private static bool HasNonEmptyDemoCell(
            IEnumerable<HowToPlayDemoBoardView> boards)
        {
            foreach (HowToPlayDemoBoardView board in boards)
            {
                for (int row = 0; row < board.Rows; row++)
                for (int column = 0; column < board.Columns; column++)
                    if (board.Cell(row, column)?.GetState() != CellStateType.EMPTY)
                        return true;
            }
            return false;
        }

        private static bool AllDemoCellsEmpty(
            IEnumerable<HowToPlayDemoBoardView> boards)
        {
            foreach (HowToPlayDemoBoardView board in boards)
            {
                for (int row = 0; row < board.Rows; row++)
                for (int column = 0; column < board.Columns; column++)
                    if (board.Cell(row, column)?.GetState() != CellStateType.EMPTY)
                        return false;
            }
            return true;
        }

        private static IEnumerator WaitForState(
            UIManager manager,
            UiName route,
            UiWindowState expected,
            float timeoutSeconds = TransitionTimeoutSeconds)
        {
            yield return WaitUntil(
                () => manager.Get(route)?.WindowState == expected,
                timeoutSeconds,
                route + " did not reach state " + expected + ".");
        }

        private static void DoubleTapTutorial(
            TutorialPagePresenter tutorial,
            int row,
            int column,
            ref double time)
        {
            time += 0.5;
            Assert.That(tutorial.TapCellForTests(row, column, time), Is.True);
            time += 0.1;
            Assert.That(tutorial.TapCellForTests(row, column, time), Is.True);
        }

        private static void TapTutorialCells(
            TutorialPagePresenter tutorial,
            IEnumerable<Vector2Int> cells,
            ref double time)
        {
            foreach (Vector2Int cell in cells)
            {
                time += 0.5;
                Assert.That(tutorial.TapCellForTests(cell.x, cell.y, time), Is.True);
            }
        }

        private static IEnumerator WaitForSession(
            GameplayManager gameplay,
            GameSessionState expected)
        {
            yield return WaitUntil(
                () => gameplay.SessionState == expected,
                TransitionTimeoutSeconds,
                "Gameplay did not reach session state " + expected + ".");
        }

        private static IEnumerator FailCurrentSession(
            GameplayManager gameplay,
            UIManager manager,
            UiName failRoute = UiName.Fail,
            UiName gameRoute = UiName.Game)
        {
            for (int mistake = 0; mistake < 3; mistake++)
            {
                yield return WaitForSession(
                    gameplay,
                    GameSessionState.Playing);
                Vector2Int cell = FindEmptyWrongCell(gameplay);
                SessionActionResult result = gameplay.DoubleTapForTests(
                    cell.x,
                    cell.y);
                Assert.That(result.Accepted, Is.True);
                Assert.That(result.Kind,
                    Is.EqualTo(SessionActionKind.WrongGuess));
                Assert.That(result.LivesAfter, Is.EqualTo(2 - mistake));
            }
            yield return WaitForSession(gameplay, GameSessionState.Failed);
            yield return WaitForState(manager, failRoute,
                UiWindowState.Showing);

            UIFrameWindow failPage = manager.Get(failRoute);
            UIFrameWindow gamePage = manager.Get(gameRoute);
            Assert.That(failPage, Is.Not.Null);
            Assert.That(gamePage, Is.Not.Null);
            Assert.That(manager.IsInputBrieflyBlocked(
                    failPage.transform as RectTransform),
                Is.True,
                "Fail must block its whole page for the source 1.5 seconds.");
            Assert.That(manager.IsInputBrieflyBlocked(
                    gamePage.transform as RectTransform),
                Is.True,
                "The terminal wrong guess must block Game for 2 seconds.");
            yield return WaitUntil(
                () => !manager.IsInputBrieflyBlocked(
                    failPage.transform as RectTransform),
                3f,
                "Fail page input blocker did not release.");
        }

        private static void AssertDailyDidNotMutateMainState(
            GameStateService state)
        {
            Assert.That(state.CurrentLevel, Is.EqualTo(21));
            Assert.That(state.CurrentStrategy, Is.EqualTo(3));
            Assert.That(state.Data.ConsecutiveFails, Is.EqualTo(5));
            Assert.That(state.Data.RetryPuzzleLevel, Is.EqualTo(21));
            Assert.That(state.Data.RetryPuzzleParameters["sentinel"],
                Is.EqualTo("main-retry"));
            Assert.That(state.GetEndgameSnapshot()["sentinel"],
                Is.EqualTo("main-snapshot"));
            Assert.That(state.Data.MainGameTotalStats["sentinel"],
                Is.EqualTo(7));
            Assert.That(state.IsCurrentLevelDirty, Is.False);
            Assert.That(state.WasDdaToolOrReviveUsed, Is.False);
            Assert.That(state.WasDdaReviveUsed, Is.False);
        }

        private static Vector2Int FindEmptyWrongCell(
            GameplayManager gameplay)
        {
            int size = gameplay.CurrentPuzzleSize;
            for (int row = 0; row < size; row++)
            {
                int solution = gameplay.SolutionColumnForTests(row);
                for (int column = 0; column < size; column++)
                {
                    if (column == solution ||
                        gameplay.GetCellState(row, column) !=
                        CellStateType.EMPTY)
                        continue;
                    return new Vector2Int(row, column);
                }
            }
            Assert.Fail("No empty wrong cell remained for the fail fixture.");
            return new Vector2Int(-1, -1);
        }

        private static IEnumerator WaitUntil(
            Func<bool> predicate,
            float timeoutSeconds,
            string failureMessage)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!predicate() && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(predicate(), Is.True, failureMessage);
        }

        private static void ConfigureRecentDuplicateFixture(GameStateData data)
        {
            data.CurrentLevel = 3;
            int[][] first = DedupRegionMap(0);
            int[][] skipped = DedupRegionMap(1);
            int[][] retried = DedupRegionMap(2);
            LevelBankIO.LoadOverride = filename => filename == "bankData6x6.json"
                ? new Dictionary<string, object>
                {
                    ["1"] = new List<object>
                    {
                        DedupEntry(first),
                        DedupEntry(skipped),
                        DedupEntry(retried)
                    }
                }
                : null;
            data.RecentPuzzles.Add(new Dictionary<string, object>
            {
                ["puzzle_id"] = LevelData.ComputePuzzleId(6, first),
                ["level"] = 2,
                ["v"] = "previous",
                ["src"] = "regular",
                ["ts"] = 1L,
                ["bank_progress"] = new Dictionary<string, object>(),
                ["main_bank_progress"] = new Dictionary<string, object>(),
                ["lkmod_progress"] = new Dictionary<string, object>()
            });
            data.RecentPuzzles.Add(new Dictionary<string, object>
            {
                ["puzzle_id"] = LevelData.ComputePuzzleId(6, retried),
                ["level"] = 2,
                ["v"] = "previous",
                ["src"] = "regular",
                ["ts"] = 2L,
                ["bank_progress"] = new Dictionary<string, object>(),
                ["main_bank_progress"] = new Dictionary<string, object>(),
                ["lkmod_progress"] = new Dictionary<string, object>()
            });
        }

        private static Dictionary<string, object> DedupEntry(int[][] regions)
        {
            return new Dictionary<string, object>
            {
                ["size"] = 6,
                ["r"] = 1,
                ["tier"] = "N",
                ["regionMap"] = regions,
                ["solution"] = new[] { 1, 3, 5, 0, 2, 4 }
            };
        }

        private static Dictionary<string, object> PreCatEntry()
        {
            var regions = new int[8][];
            for (int row = 0; row < regions.Length; row++)
            {
                regions[row] = new int[8];
                for (int column = 0; column < regions[row].Length; column++)
                    regions[row][column] = row;
            }
            return new Dictionary<string, object>
            {
                ["size"] = 8,
                ["r"] = 1,
                ["tier"] = "N",
                ["regionMap"] = regions,
                ["solution"] = new[] { 1, 3, 5, 7, 0, 2, 4, 6 }
            };
        }

        private static int[][] DedupRegionMap(int variant)
        {
            var result = new int[6][];
            for (int row = 0; row < 6; row++)
            {
                result[row] = new int[6];
                for (int column = 0; column < 6; column++)
                    result[row][column] = row;
            }
            if (variant == 1) result[0][0] = 2;
            else if (variant == 2) result[0][0] = 1;
            return result;
        }

        private static string ReadHistoryString(object raw, string key)
        {
            return raw is Dictionary<string, object> entry &&
                   entry.TryGetValue(key, out object value) && value != null
                ? value.ToString()
                : string.Empty;
        }

        private static int ReadHistoryInt(object raw, string key)
        {
            return raw is Dictionary<string, object> entry &&
                   entry.TryGetValue(key, out object value) && value != null
                ? System.Convert.ToInt32(value)
                : 0;
        }

        private static IEnumerator PressEscape(Keyboard keyboard)
        {
            Assert.That(keyboard, Is.Not.Null);
            InputSystem.QueueStateEvent(
                keyboard,
                new KeyboardState(Key.Escape));
            InputSystem.Update();
            Assert.That(keyboard.escapeKey.isPressed, Is.True,
                "Queued Escape press was not processed by Input System.");
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            Assert.That(keyboard.escapeKey.isPressed, Is.False,
                "Queued Escape release was not processed by Input System.");
            yield return null;
        }

        private static PrivacyPermissionRuntime CreatePlatformRuntime(
            UIManager manager,
            AbConfigRuntime abRuntime,
            out PlayModePlatformPermissionProvider provider)
        {
            var host = new GameObject("PlatformPermissionRuntimeTest");
            provider = host.AddComponent<
                PlayModePlatformPermissionProvider>();
            PrivacyPermissionRuntime runtime =
                host.AddComponent<PrivacyPermissionRuntime>();
            runtime.ConfigureForTests(
                manager,
                abRuntime,
                tracking: Find<TrackingRuntime>());
            runtime.BindProvider(provider);
            return runtime;
        }

        private static IEnumerator CompleteWhenDone(
            IEnumerator routine,
            Action completed)
        {
            yield return routine;
            completed?.Invoke();
        }

        private static void ClickThroughPointerPhases(Button button)
        {
            Assert.That(button, Is.Not.Null);
            Assert.That(EventSystem.current, Is.Not.Null,
                "AppScene is missing an active EventSystem.");
            var eventData = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                pointerId = -1,
                clickCount = 1,
                position = RectTransformUtility.WorldToScreenPoint(
                    null,
                    button.transform.position)
            };
            Assert.That(ExecuteEvents.Execute(
                    button.gameObject,
                    eventData,
                    ExecuteEvents.pointerDownHandler),
                Is.True);
            Assert.That(ExecuteEvents.Execute(
                    button.gameObject,
                    eventData,
                    ExecuteEvents.pointerUpHandler),
                Is.True);
            // ExecuteEvents does not build EventSystem's pointer-click state
            // machine when called in isolation. Invoke the Button event after
            // the real down/up handlers so the ordering matches UGUI: release
            // is queued first, then the navigation callback opens the page.
            button.onClick.Invoke();
        }

        private static ProfileSelectionCell FindProfileCell(
            ProfilePagePresenter page,
            int id)
        {
            Assert.That(page, Is.Not.Null);
            ProfileSelectionCell found = null;
            int count = 0;
            foreach (ProfileSelectionCell cell in
                     page.GetComponentsInChildren<ProfileSelectionCell>(true))
            {
                if (cell == null || cell.Id != id ||
                    !cell.gameObject.activeInHierarchy)
                    continue;
                found = cell;
                count++;
            }
            Assert.That(count, Is.EqualTo(1),
                $"Profile expected one active cell for id {id}.");
            return found;
        }

        private static void ClickProfileCell(ProfileSelectionCell cell)
        {
            Assert.That(cell, Is.Not.Null);
            Button button = cell.GetComponent<Button>();
            Assert.That(button, Is.Not.Null);
            ClickThroughPointerPhases(button);
        }

        private static void AssertSettingsToggle(
            SettingsToggleView view,
            bool expected)
        {
            Assert.That(view, Is.Not.Null);
            Assert.That(view.Button, Is.Not.Null);
            Assert.That(view.Value, Is.EqualTo(expected));
            Transform toggleOn = view.Button.transform.Find("ToggleOn");
            Transform toggleOff = view.Button.transform.Find("ToggleOff");
            Assert.That(toggleOn, Is.Not.Null);
            Assert.That(toggleOff, Is.Not.Null);
            Assert.That(toggleOn.gameObject.activeSelf, Is.EqualTo(expected));
            Assert.That(toggleOff.gameObject.activeSelf,
                Is.EqualTo(!expected));
            RawImage icon = view.Button.GetComponentInChildren<RawImage>(true);
            Assert.That(icon, Is.Not.Null);
            Assert.That(icon.texture, Is.Not.Null);
        }

        private static Button FindProfileTabButton(
            ProfilePagePresenter page,
            string tabName)
        {
            Transform button = page.transform.Find(
                $"Content/TabGroup/{tabName}/Button");
            Assert.That(button, Is.Not.Null,
                $"Profile is missing {tabName}/Button.");
            Button result = button.GetComponent<Button>();
            Assert.That(result, Is.Not.Null);
            Assert.That(result.isActiveAndEnabled, Is.True);
            return result;
        }

        private static Vector2Int FindInputCell(
            GameplayManager gameplay,
            bool solution)
        {
            BoardView board = gameplay.boardView;
            for (int row = 0; row < board.PuzzleSize; row++)
            {
                int solutionColumn = gameplay.SolutionColumnForTests(row);
                for (int column = 0; column < board.PuzzleSize; column++)
                {
                    if ((column == solutionColumn) != solution ||
                        gameplay.GetCellState(row, column) != CellStateType.EMPTY)
                        continue;
                    return new Vector2Int(row, column);
                }
            }
            return new Vector2Int(-1, -1);
        }

        private static void SendBoardGestureTap(
            BoardView board,
            Vector2Int cell,
            int nowMilliseconds)
        {
            Vector2 boardPosition = new Vector2(
                board.GridPaddingPixels +
                cell.y * board.GridSlotPixels +
                board.CellPixels * 0.5f,
                board.GridPaddingPixels +
                cell.x * board.GridSlotPixels +
                board.CellPixels * 0.5f);
            board.OnGesturePointerStarted?.Invoke(
                boardPosition,
                new Vector2Int(cell.y, cell.x),
                nowMilliseconds);
            board.OnGestureEnded?.Invoke();
        }

        private static void AssertLocalInputBlocker(RectTransform target)
        {
            Assert.That(target, Is.Not.Null);
            Transform blocker = target.Find("_InputBlocker");
            Assert.That(blocker, Is.Not.Null,
                "Target has no local _InputBlocker child.");
            Image image = blocker.GetComponent<Image>();
            Canvas canvas = blocker.GetComponent<Canvas>();
            Assert.That(image, Is.Not.Null);
            Assert.That(image.raycastTarget, Is.True);
            Assert.That(blocker.GetComponent<GraphicRaycaster>(), Is.Not.Null);
            Assert.That(canvas, Is.Not.Null);
            Assert.That(canvas.overrideSorting, Is.True);
            Assert.That(canvas.sortingOrder, Is.EqualTo(4095));

            int count = 0;
            foreach (Transform child in target)
                if (child.name == "_InputBlocker") count++;
            Assert.That(count, Is.EqualTo(1),
                "Refreshing a target must leave exactly one local blocker.");
        }

        private static void AssertShowing(UIManager manager, UiName route)
        {
            UIFrameWindow page = manager.Get(route);
            Assert.That(page, Is.Not.Null, route + " has not been created.");
            Assert.That(page.WindowState, Is.EqualTo(UiWindowState.Showing));
        }

        private static bool IsShowing(UIManager manager, UiName route)
        {
            return manager.Get(route)?.WindowState == UiWindowState.Showing;
        }

        private static Button FindButton(
            UIFrameWindow page,
            string name,
            bool requireInteractable = true,
            bool requireActive = true)
        {
            Assert.That(page, Is.Not.Null);
            Button found = null;
            int count = 0;
            Button[] buttons = page.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                if (button.name != name) continue;
                found = button;
                count++;
            }
            Assert.That(count, Is.EqualTo(1),
                page.UiName + " expected exactly one " + name + ".");
            if (requireActive)
                Assert.That(found.isActiveAndEnabled, Is.True,
                    page.UiName + "/" + name + " is not active.");
            if (requireInteractable)
                Assert.That(found.interactable, Is.True,
                    page.UiName + "/" + name + " is not interactable.");
            return found;
        }

        private static Button FindActiveButton(
            UIFrameWindow page,
            string name,
            bool requireInteractable = true)
        {
            Assert.That(page, Is.Not.Null);
            Button found = null;
            int count = 0;
            Button[] buttons = page.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                if (button.name != name || !button.gameObject.activeInHierarchy)
                    continue;
                found = button;
                count++;
            }
            Assert.That(count, Is.EqualTo(1),
                page.UiName + " expected one active " + name + ".");
            if (requireInteractable)
                Assert.That(found.interactable, Is.True,
                    page.UiName + "/" + name + " is not interactable.");
            return found;
        }

        private static Button FindEntryButton(Component entry)
        {
            Assert.That(entry, Is.Not.Null);
            Button found = null;
            int count = 0;
            Button[] buttons = entry.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                if (button.name != "ClickBtn") continue;
                found = button;
                count++;
            }
            Assert.That(count, Is.EqualTo(1),
                entry.name + " expected exactly one ClickBtn.");
            return found;
        }

        private static T FindNamedComponent<T>(
            UIFrameWindow page,
            string name) where T : Component
        {
            Assert.That(page, Is.Not.Null);
            T found = null;
            int count = 0;
            T[] components = page.GetComponentsInChildren<T>(true);
            foreach (T component in components)
            {
                if (component.name != name) continue;
                found = component;
                count++;
            }
            Assert.That(count, Is.EqualTo(1),
                page.UiName + " expected exactly one " + name + ".");
            return found;
        }

        private static T FindFirstActiveComponent<T>(
            UIFrameWindow page) where T : Component
        {
            Assert.That(page, Is.Not.Null);
            T[] components = page.GetComponentsInChildren<T>(true);
            foreach (T component in components)
                if (component.gameObject.activeInHierarchy)
                    return component;
            Assert.Fail(page.UiName + " has no active " + typeof(T).Name + ".");
            return null;
        }

        private static Button FindChildButton(
            Component component,
            string name,
            bool requireInteractable = true)
        {
            Assert.That(component, Is.Not.Null);
            Button found = null;
            int count = 0;
            foreach (Button button in
                     component.GetComponentsInChildren<Button>(true))
            {
                if (button.name != name) continue;
                found = button;
                count++;
            }
            Assert.That(count, Is.EqualTo(1),
                component.name + " expected exactly one " + name + ".");
            Assert.That(found.isActiveAndEnabled, Is.True);
            if (requireInteractable)
                Assert.That(found.interactable, Is.True);
            return found;
        }

        private static Button FindOnlyButton(Component component)
        {
            Assert.That(component, Is.Not.Null);
            Button[] buttons = component.GetComponentsInChildren<Button>(true);
            Assert.That(buttons.Length, Is.EqualTo(1),
                component.name + " expected exactly one Button.");
            Assert.That(buttons[0].isActiveAndEnabled, Is.True,
                component.name + " button is not active.");
            Assert.That(buttons[0].interactable, Is.True,
                component.name + " button is not interactable.");
            return buttons[0];
        }

        private static T Find<T>() where T : Object
        {
            return Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
        }
    }
}
