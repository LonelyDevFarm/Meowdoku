using System;
using System.Threading;
using UnityEditor;

namespace Meowdoku.Editor
{
    /// <summary>
    /// Lets local development tools ask the already-running Unity Editor to
    /// refresh assets without stealing window focus. This class is excluded
    /// from players by the Editor-only assembly definition.
    /// </summary>
    [InitializeOnLoad]
    internal static class UnityRefreshBridge
    {
        internal const string EventName = @"Local\Meowdoku.UnityRefresh";
        internal const string StopPlayModeEventName = @"Local\Meowdoku.UnityStopPlayMode";

        private static EventWaitHandle _refreshEvent;
        private static EventWaitHandle _stopPlayModeEvent;
        private static bool _refreshPending;

        static UnityRefreshBridge()
        {
            if (AssetDatabase.IsAssetImportWorkerProcess()) return;
            TryCreateEvent();
            TryCreateStopPlayModeEvent();

            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
            AssemblyReloadEvents.beforeAssemblyReload -= Dispose;
            AssemblyReloadEvents.beforeAssemblyReload += Dispose;
            EditorApplication.quitting -= Dispose;
            EditorApplication.quitting += Dispose;
        }

        [MenuItem("Tools/Meowdoku/Refresh Project Assets %#&r")]
        private static void RefreshFromMenu()
        {
            QueueRefresh();
        }

        private static void TryCreateEvent()
        {
            try
            {
                _refreshEvent = new EventWaitHandle(
                    false,
                    EventResetMode.AutoReset,
                    EventName);
            }
            catch (Exception)
            {
                // The menu command remains available if the OS-level bridge
                // cannot be created in a restricted Editor environment.
                _refreshEvent = null;
            }
        }

        private static void TryCreateStopPlayModeEvent()
        {
            try
            {
                _stopPlayModeEvent = new EventWaitHandle(
                    false,
                    EventResetMode.AutoReset,
                    StopPlayModeEventName);
            }
            catch (Exception)
            {
                _stopPlayModeEvent = null;
            }
        }

        private static void Poll()
        {
            if (_refreshEvent == null)
                TryCreateEvent();

            if (_stopPlayModeEvent == null)
                TryCreateStopPlayModeEvent();

            try
            {
                if (_refreshEvent != null && _refreshEvent.WaitOne(0))
                    QueueRefresh();

                if (_refreshPending &&
                    !EditorApplication.isCompiling &&
                    !EditorApplication.isUpdating)
                {
                    _refreshPending = false;
                    AppRuntimeSceneInstaller.NormalizeAppSceneUiScale();
                    GameplayPresentationSceneInstaller.UpgradeCellPrefab();
                    GameplayPresentationSceneInstaller.UpgradeGamePageRuleBar();
                    AppRuntimeSceneInstaller.UpgradeGamePageBackground();
                    AppRuntimeSceneInstaller.UpgradeGamePageToolBar();
                    ProductServicePrefabInstaller.InstallIfReady();
                    PlatformGuidePrefabInstaller.InstallIfReady();
                    ConfirmDialogPrefabInstaller.InstallIfReady();
                    ProfilePagePrefabInstaller.InstallIfReady();
                    TutorialPagePrefabInstaller.InstallIfReady();
                    HowToPlayPagePrefabInstaller.InstallIfReady();
                    ResultPagePrefabInstaller.InstallIfReady();
                    PortfolioBuildSettingsInstaller.InstallIfReady();
                    CatSpriteAnimationInstaller.InstallIfReady();
                    HomePagePrefabInstaller.InstallIfReady();
                    DailyMetaPagePrefabInstaller.InstallIfReady();
                    RankActivityPagePrefabInstaller.InstallIfReady();
                    SettingsPagePrefabInstaller.InstallIfReady();
                    BankBrowserPagePrefabInstaller.InstallIfReady();
                    AppRuntimeSceneInstaller.InstallIfReady();
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                }
            }
            catch (ObjectDisposedException)
            {
                _refreshEvent = null;
            }

            try
            {
                if (_stopPlayModeEvent != null && _stopPlayModeEvent.WaitOne(0) &&
                    EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.ExitPlaymode();
                }
            }
            catch (ObjectDisposedException)
            {
                _stopPlayModeEvent = null;
            }
        }

        private static void QueueRefresh()
        {
            if (_refreshPending)
                return;

            _refreshPending = true;
        }

        private static void Dispose()
        {
            EditorApplication.update -= Poll;
            AssemblyReloadEvents.beforeAssemblyReload -= Dispose;
            EditorApplication.quitting -= Dispose;

            _refreshEvent?.Dispose();
            _refreshEvent = null;
            _stopPlayModeEvent?.Dispose();
            _stopPlayModeEvent = null;
        }
    }
}
