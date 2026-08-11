using System;
using System.Collections;
using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.UI;
using Meowdoku.Gameplay;
using NUnit.Framework;
using UnityEngine;
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

        private IDisposable _stateOverride;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var data = new GameStateData
            {
                CurrentLevel = 1,
                IsFirstSession = false,
                TutorialDone = true,
                LastSplashDate = DateTime.Now.ToString("yyyy-MM-dd"),
                MaxDailyDate = DateTime.Now.ToString("yyyy-MM-dd")
            };
            _stateOverride = GameStateRuntime.OverrideForTests(
                new GameStateService(data));

            AsyncOperation load = SceneManager.LoadSceneAsync(
                AppScenePath,
                LoadSceneMode.Single);
            Assert.That(load, Is.Not.Null, "AppScene could not be loaded.");
            yield return load;
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
        public IEnumerator AppScene_BankSpecialWin_NextLoadsNextBankEntry()
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
            AssertShowing(manager, UiName.Game);
            Assert.That(manager.IsAnyLoading, Is.False);
        }

        private static IEnumerator CompletePreWinMetaFlow(UIManager manager)
        {
            float deadline = Time.realtimeSinceStartup + 35f;
            while (!IsShowing(manager, UiName.Win) &&
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

            Assert.That(IsShowing(manager, UiName.Win), Is.True,
                "Win did not appear after completing pre-result meta flow.");
        }

        private static IEnumerator CollectAward(UIManager manager)
        {
            UIFrameWindow award = manager.Get(UiName.Award);
            Button collect = FindButton(
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
            UIManager manager)
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
            yield return WaitForState(manager, UiName.Fail,
                UiWindowState.Showing);
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
