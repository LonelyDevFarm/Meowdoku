using System;
using System.Collections;
using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.Ads;
using Meowdoku.Core.Config;
using Meowdoku.Core.Daily;
using Meowdoku.Core.Platform;
using Meowdoku.Core.Profile;
using Meowdoku.Core.Rank;
using Meowdoku.Core.Robot;
using Meowdoku.Core.Tracking;
using Meowdoku.Core.UI;
using Meowdoku.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
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
            private ProfileData _data = new();

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
