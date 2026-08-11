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

        private static EventWaitHandle _refreshEvent;
        private static bool _refreshPending;

        static UnityRefreshBridge()
        {
            if (AssetDatabase.IsAssetImportWorkerProcess()) return;
            TryCreateEvent();

            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
            AssemblyReloadEvents.beforeAssemblyReload -= Dispose;
            AssemblyReloadEvents.beforeAssemblyReload += Dispose;
            EditorApplication.quitting -= Dispose;
            EditorApplication.quitting += Dispose;
        }

        [MenuItem("Tools/Meowdoku/Refresh Project Assets")]
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

        private static void Poll()
        {
            try
            {
                if (_refreshEvent != null && _refreshEvent.WaitOne(0))
                    QueueRefresh();
            }
            catch (ObjectDisposedException)
            {
                _refreshEvent = null;
            }
        }

        private static void QueueRefresh()
        {
            if (_refreshPending)
                return;

            _refreshPending = true;
            EditorApplication.delayCall += RefreshWhenReady;
        }

        private static void RefreshWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += RefreshWhenReady;
                return;
            }

            _refreshPending = false;
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        private static void Dispose()
        {
            EditorApplication.update -= Poll;
            AssemblyReloadEvents.beforeAssemblyReload -= Dispose;
            EditorApplication.quitting -= Dispose;

            _refreshEvent?.Dispose();
            _refreshEvent = null;
        }
    }
}
