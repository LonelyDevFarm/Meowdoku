using System.Collections;
using Meowdoku.Core.Config;
using Meowdoku.Core.Localization;
using Meowdoku.Core.Online;
using Meowdoku.Core.Platform;
using UnityEngine;

namespace Meowdoku.Core.UI
{
    public enum AppStartupPhase
    {
        Idle = 0,
        RuntimeSetup = 1,
        Splash = 2,
        PlatformBoundary = 3,
        Prewarming = 4,
        DataSyncBoundary = 5,
        SplashCompletion = 6,
        Routing = 7,
        Complete = 8,
        Failed = 9
    }

    public interface IAppStartupExternalServices
    {
        void ApplySystemLocale(GameStateService gameState);
        void HideNativeSplash(int milliseconds);
        IEnumerator AwaitPrivacyAndPush();
        IEnumerator AwaitConsentAndTracking(float maximumSeconds);
        IEnumerator AwaitRemoteDefaults(float maximumSeconds);
        void SetupScreen();
        bool IsDataSyncAvailable { get; }
        IEnumerator AwaitDataSync(float maximumSeconds);
        bool TryHandleShortcut();
    }

    public interface IStartupSplashWindow
    {
        IEnumerator ForceCompleteAndWait();
    }

    public interface IStartupGamePrewarm
    {
        IEnumerator PrewarmBoard(int boardSize);
    }

    public interface IStartupWindowPrewarm
    {
        IEnumerator PrewarmForFirstShow();
    }

    public sealed class OfflineStartupExternalServices : IAppStartupExternalServices
    {
        public static readonly OfflineStartupExternalServices Instance = new();
        private OfflineStartupExternalServices() { }

        public bool IsDataSyncAvailable => false;
        public void ApplySystemLocale(GameStateService gameState) { }
        public void HideNativeSplash(int milliseconds) { }
        public IEnumerator AwaitPrivacyAndPush() { yield break; }
        public IEnumerator AwaitConsentAndTracking(float maximumSeconds) { yield break; }
        public IEnumerator AwaitRemoteDefaults(float maximumSeconds) { yield break; }
        public void SetupScreen() { }
        public IEnumerator AwaitDataSync(float maximumSeconds) { yield break; }
        public bool TryHandleShortcut() => false;
    }

    /// <summary>
    /// Unity equivalent of launcher.gd. It owns the startup sequence through
    /// serialized composition and keeps SDK/online work behind a no-op-able
    /// boundary so offline startup cannot be blocked by absent services.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AppBootstrap : MonoBehaviour
    {
        [SerializeField] private UIManager uiManager;
        [SerializeField] private LocalizationCatalog localizationCatalog;
        [SerializeField] private AbConfigRuntime abConfigRuntime;
        [SerializeField] private DataSyncRuntime dataSyncRuntime;
        [SerializeField] private PrivacyPermissionRuntime platformRuntime;
        [SerializeField] private ProductServiceRuntime productServiceRuntime;
        [SerializeField] private MonoBehaviour externalServicesAdapter;
        [SerializeField] private bool runOnStart = true;

        private Coroutine _gamePrewarm;
        private bool _running;
        private bool _runtimeInitialized;
        private IAppStartupExternalServices _externalServices;

        public AppStartupPhase Phase { get; private set; } = AppStartupPhase.Idle;
        public bool IsComplete => Phase == AppStartupPhase.Complete;
        public string FailureReason { get; private set; } = string.Empty;
#if UNITY_INCLUDE_TESTS
        internal float StartupStartedAtForTests { get; private set; } = -1f;
        internal float SplashForceRequestedAtForTests { get; private set; } = -1f;
        internal float SplashForceCompletedAtForTests { get; private set; } = -1f;
#endif

        private IEnumerator Start()
        {
            if (runOnStart) yield return RunStartup();
        }

        public IEnumerator RunStartup()
        {
            if (_running || IsComplete) yield break;
            _running = true;
            FailureReason = string.Empty;
            float startupTime = Time.realtimeSinceStartup;
#if UNITY_INCLUDE_TESTS
            StartupStartedAtForTests = startupTime;
            SplashForceRequestedAtForTests = -1f;
            SplashForceCompletedAtForTests = -1f;
#endif

            if (uiManager == null)
            {
                Fail("UIManager reference is missing.");
                yield break;
            }

            IAppStartupExternalServices external =
                externalServicesAdapter as IAppStartupExternalServices ??
                (IAppStartupExternalServices)platformRuntime ??
                OfflineStartupExternalServices.Instance;
            _externalServices = external;
            uiManager.BindSettingsExternalServices(
                external as ISettingsExternalServices ??
                OfflineSettingsExternalServices.Instance);

            Phase = AppStartupPhase.RuntimeSetup;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            GameStateService gameState = GameStateRuntime.Current;
            abConfigRuntime?.Initialize(gameState);
            if (!_runtimeInitialized)
            {
                gameState.OnSessionStarted();
                if (localizationCatalog != null)
                {
                    SettingsLanguageConfig languageConfig =
                        abConfigRuntime != null
                            ? abConfigRuntime.Settings.Language
                            : new SettingsLanguageConfig();
                    localizationCatalog.ApplySystemLocale(
                        gameState,
                        languageConfig.IsLanguageSwitchEnabledPeek(
                            abConfigRuntime?.ValueProvider));
                }
                else
                {
                    external.ApplySystemLocale(gameState);
                }
                gameState.ConsumeFirstSessionPersist();
                _runtimeInitialized = true;
            }
            productServiceRuntime?.InitializeProductServices();

            // Source delays one second only on Android before showing its UI
            // splash, while the startup timer is already running.
            if (Application.platform == RuntimePlatform.Android)
                yield return new WaitForSecondsRealtime(1f);

            Phase = AppStartupPhase.Splash;
            UIFrameWindow splash = uiManager.Show(UiName.Splash);
            if (splash == null)
            {
                Fail("Splash is not registered.");
                yield break;
            }
            external.HideNativeSplash(500);

            Phase = AppStartupPhase.PlatformBoundary;
            yield return RunOptional(external.AwaitPrivacyAndPush());
            yield return null;
            yield return RunOptional(external.AwaitConsentAndTracking(
                AppStartupContract.ExternalWaitMaximumSeconds));
            yield return RunOptional(external.AwaitRemoteDefaults(
                AppStartupContract.ExternalWaitMaximumSeconds));
            if (abConfigRuntime != null)
                yield return abConfigRuntime.AwaitRemoteReady(
                    AppStartupContract.ExternalWaitMaximumSeconds);
            external.SetupScreen();

            Phase = AppStartupPhase.Prewarming;
            _gamePrewarm = StartCoroutine(PrewarmGame(gameState));

            Phase = AppStartupPhase.DataSyncBoundary;
            if (dataSyncRuntime != null &&
                dataSyncRuntime.IsStartupAvailable)
                yield return RunOptional(dataSyncRuntime.AwaitStartup(
                    AppStartupContract.ExternalWaitMaximumSeconds));
            else if (external.IsDataSyncAvailable)
                yield return RunOptional(external.AwaitDataSync(
                    AppStartupContract.ExternalWaitMaximumSeconds));

            Phase = AppStartupPhase.SplashCompletion;
            float elapsed = Time.realtimeSinceStartup - startupTime;
            yield return new WaitForSecondsRealtime(
                AppStartupContract.SplashWaitRemaining(elapsed));
#if UNITY_INCLUDE_TESTS
            SplashForceRequestedAtForTests = Time.realtimeSinceStartup;
#endif
            if (splash is IStartupSplashWindow splashWindow)
                yield return RunOptional(splashWindow.ForceCompleteAndWait());
#if UNITY_INCLUDE_TESTS
            SplashForceCompletedAtForTests = Time.realtimeSinceStartup;
#endif

            if (_gamePrewarm != null)
                while (_gamePrewarm != null) yield return null;
            while (uiManager.IsAnyLoading) yield return null;

            Phase = AppStartupPhase.Routing;
            bool handledShortcut = external.TryHandleShortcut();
            if (!handledShortcut)
            {
                UiName route = AppStartupContract.InitialRoute(gameState.TutorialDone);
                if (uiManager.Show(route) == null)
                {
                    Fail($"Initial route {route} is not registered.");
                    yield break;
                }
            }

            uiManager.Hide(UiName.Splash);
            Phase = AppStartupPhase.Complete;
            _running = false;
        }

        internal void ConfigureForTests(
            UIManager manager,
            MonoBehaviour externalAdapter = null,
            bool autoRun = false,
            LocalizationCatalog localization = null,
            DataSyncRuntime dataSync = null,
            PrivacyPermissionRuntime platform = null,
            ProductServiceRuntime product = null)
        {
            uiManager = manager;
            externalServicesAdapter = externalAdapter;
            runOnStart = autoRun;
            localizationCatalog = localization;
            dataSyncRuntime = dataSync;
            platformRuntime = platform;
            productServiceRuntime = product;
        }

        private IEnumerator PrewarmGame(GameStateService gameState)
        {
            yield return uiManager.WarmPoolAsync(UiName.Game);
            int size = LevelData.GetSize(gameState.CurrentLevel);
            UIFrameWindow game = uiManager.Get(UiName.Game);
            if (game is IStartupGamePrewarm gamePrewarm && size > 0)
                yield return RunOptional(gamePrewarm.PrewarmBoard(size));

            UiName[] firstUseWindows =
            {
                UiName.DailyGame,
                UiName.Setting,
                UiName.Profile,
                UiName.Streak,
                UiName.RankActivityPage,
                UiName.RankActivityHowToPlay
            };
            for (int index = 0; index < firstUseWindows.Length; index++)
            {
                UiName name = firstUseWindows[index];
                yield return uiManager.WarmPoolAsync(name);
                UIFrameWindow window = uiManager.Get(name);
                if (window is IStartupGamePrewarm dailyPrewarm && size > 0)
                    yield return RunOptional(dailyPrewarm.PrewarmBoard(size));
                if (window is IStartupWindowPrewarm windowPrewarm)
                    yield return RunOptional(windowPrewarm.PrewarmForFirstShow());
            }

            if (size > 0)
            {
                BankData.GetRanks(size);
                BankData.GetLkStyleRanks(size);
                BankData.GetGcRanks(size);
            }
            _gamePrewarm = null;
        }

        private static IEnumerator RunOptional(IEnumerator routine)
        {
            if (routine != null) yield return routine;
        }

        private void Fail(string reason)
        {
            if (_gamePrewarm != null) StopCoroutine(_gamePrewarm);
            _gamePrewarm = null;
            FailureReason = reason;
            Phase = AppStartupPhase.Failed;
            _running = false;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && IsComplete)
                _externalServices?.TryHandleShortcut();
        }

        private void OnDestroy()
        {
            if (_gamePrewarm != null) StopCoroutine(_gamePrewarm);
            _gamePrewarm = null;
        }
    }
}
