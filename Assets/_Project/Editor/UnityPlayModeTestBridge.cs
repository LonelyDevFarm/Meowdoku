using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.TestTools;

namespace Meowdoku.Editor
{
    /// <summary>
    /// Runs the project's PlayMode smoke suite from local tooling. SessionState
    /// keeps the callback registration alive across the domain reloads caused
    /// by entering and leaving Play Mode.
    /// </summary>
    [InitializeOnLoad]
    internal static class UnityPlayModeTestBridge
    {
        internal const string EventName =
            @"Local\Meowdoku.UnityPlayModeTests";
        internal const string PlatformEventName =
            @"Local\Meowdoku.UnityPlayModeTests.Platform";
        internal const string VisualAuditEventName =
            @"Local\Meowdoku.UnityPlayModeTests.VisualAudit";
        internal const string RankVisualAuditEventName =
            @"Local\Meowdoku.UnityPlayModeTests.RankVisualAudit";
        internal const string ResetEventName =
            @"Local\Meowdoku.UnityPlayModeTests.Reset";
        internal const string ResultPath =
            "Temp/MeowdokuPlayModeTestResult.txt";
        internal const string XmlResultPath =
            "Temp/MeowdokuPlayModeTestResult.xml";
        internal const string PlatformResultPath =
            "Temp/MeowdokuPlatformPlayModeTestResult.txt";
        internal const string PlatformXmlResultPath =
            "Temp/MeowdokuPlatformPlayModeTestResult.xml";
        internal const string VisualAuditResultPath =
            "Temp/MeowdokuVisualAuditPlayModeTestResult.txt";
        internal const string VisualAuditXmlResultPath =
            "Temp/MeowdokuVisualAuditPlayModeTestResult.xml";
        internal const string RankVisualAuditResultPath =
            "Temp/MeowdokuRankVisualAuditPlayModeTestResult.txt";
        internal const string RankVisualAuditXmlResultPath =
            "Temp/MeowdokuRankVisualAuditPlayModeTestResult.xml";

        private const string ActiveSessionKey =
            "Meowdoku.UnityPlayModeTestBridge.Active";
        private const string PlatformSessionKey =
            "Meowdoku.UnityPlayModeTestBridge.Platform";
        private const string VisualAuditSessionKey =
            "Meowdoku.UnityPlayModeTestBridge.VisualAudit";
        private const string RankVisualAuditSessionKey =
            "Meowdoku.UnityPlayModeTestBridge.RankVisualAudit";

        private static EventWaitHandle _runEvent;
        private static EventWaitHandle _platformEvent;
        private static EventWaitHandle _visualAuditEvent;
        private static EventWaitHandle _rankVisualAuditEvent;
        private static EventWaitHandle _resetEvent;
        private static bool _runPending;
        private static bool _platformPending;
        private static bool _visualAuditPending;
        private static bool _rankVisualAuditPending;
        private static TestRunnerApi _runner;
        private static ResultCallbacks _callbacks;

        static UnityPlayModeTestBridge()
        {
            if (AssetDatabase.IsAssetImportWorkerProcess()) return;
            TryCreateEvent();
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
            AssemblyReloadEvents.beforeAssemblyReload -= BeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReload;
            EditorApplication.quitting -= DisposeForQuit;
            EditorApplication.quitting += DisposeForQuit;

            if (SessionState.GetBool(ActiveSessionKey, false))
                EditorApplication.delayCall += RegisterCallbacks;
        }

        [MenuItem("Tools/Meowdoku/Run PlayMode Tests %#&p")]
        private static void RunFromMenu()
        {
            QueueRun(false, false);
        }

        [MenuItem("Tools/Meowdoku/Run Platform PlayMode Tests")]
        private static void RunPlatformFromMenu()
        {
            QueueRun(true, false);
        }

        [MenuItem("Tools/Meowdoku/Run Portfolio Visual Audit")]
        private static void RunVisualAuditFromMenu()
        {
            QueueRun(false, true);
        }

        [MenuItem("Tools/Meowdoku/Run Rank Visual Audit")]
        private static void RunRankVisualAuditFromMenu()
        {
            QueueRun(false, false, true);
        }

        private static void TryCreateEvent()
        {
            try
            {
                _runEvent = new EventWaitHandle(
                    false,
                    EventResetMode.AutoReset,
                    EventName);
                _platformEvent = new EventWaitHandle(
                    false,
                    EventResetMode.AutoReset,
                    PlatformEventName);
                _visualAuditEvent = new EventWaitHandle(
                    false,
                    EventResetMode.AutoReset,
                    VisualAuditEventName);
                _rankVisualAuditEvent = new EventWaitHandle(
                    false,
                    EventResetMode.AutoReset,
                    RankVisualAuditEventName);
                _resetEvent = new EventWaitHandle(
                    false,
                    EventResetMode.AutoReset,
                    ResetEventName);
            }
            catch (Exception)
            {
                _runEvent?.Dispose();
                _platformEvent?.Dispose();
                _visualAuditEvent?.Dispose();
                _rankVisualAuditEvent?.Dispose();
                _resetEvent?.Dispose();
                _runEvent = null;
                _platformEvent = null;
                _visualAuditEvent = null;
                _rankVisualAuditEvent = null;
                _resetEvent = null;
            }
        }

        private static void Poll()
        {
            if (_runEvent == null || _platformEvent == null ||
                _visualAuditEvent == null || _rankVisualAuditEvent == null ||
                _resetEvent == null)
                TryCreateEvent();

            try
            {
                if (_resetEvent != null && _resetEvent.WaitOne(0))
                    ResetOrphanedRun();
                if (_runEvent != null && _runEvent.WaitOne(0))
                    QueueRun(false, false);
                if (_platformEvent != null && _platformEvent.WaitOne(0))
                    QueueRun(true, false);
                if (_visualAuditEvent != null &&
                    _visualAuditEvent.WaitOne(0))
                    QueueRun(false, true);
                if (_rankVisualAuditEvent != null &&
                    _rankVisualAuditEvent.WaitOne(0))
                    QueueRun(false, false, true);
                if (_runPending &&
                    !SessionState.GetBool(ActiveSessionKey, false))
                    RunWhenReady();
            }
            catch (ObjectDisposedException)
            {
                _runEvent = null;
                _platformEvent = null;
                _visualAuditEvent = null;
                _rankVisualAuditEvent = null;
                _resetEvent = null;
            }
        }

        private static void ResetOrphanedRun()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            _runPending = false;
            _platformPending = false;
            _visualAuditPending = false;
            _rankVisualAuditPending = false;
            SessionState.EraseBool(ActiveSessionKey);
            SessionState.EraseBool(PlatformSessionKey);
            SessionState.EraseBool(VisualAuditSessionKey);
            SessionState.EraseBool(RankVisualAuditSessionKey);
            ReleaseCallbacks();
            WriteResult("RESET", ResultPath);
        }

        private static void QueueRun(
            bool platformOnly,
            bool visualAuditOnly,
            bool rankVisualAuditOnly = false)
        {
            if (_runPending || SessionState.GetBool(ActiveSessionKey, false))
                return;
            _runPending = true;
            _platformPending = platformOnly;
            _visualAuditPending = visualAuditOnly;
            _rankVisualAuditPending = rankVisualAuditOnly;
        }

        private static void RunWhenReady()
        {
            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            _runPending = false;
            SessionState.SetBool(ActiveSessionKey, true);
            SessionState.SetBool(PlatformSessionKey, _platformPending);
            SessionState.SetBool(VisualAuditSessionKey, _visualAuditPending);
            SessionState.SetBool(
                RankVisualAuditSessionKey,
                _rankVisualAuditPending);
            _platformPending = false;
            _visualAuditPending = false;
            _rankVisualAuditPending = false;
            WriteResult("RUNNING", ActiveResultPath);
            RegisterCallbacks();
            try
            {
                var filter = new Filter
                {
                    testMode =
                        UnityEditor.TestTools.TestRunner.Api.TestMode.PlayMode,
                    assemblyNames = new[] { "Meowdoku.PlayModeTests" }
                };
                if (SessionState.GetBool(RankVisualAuditSessionKey, false))
                    filter.groupNames = new[]
                    {
                        @"^Meowdoku\.Tests\.PlayMode\.PrimaryNavigationPlayModeTests\.PortfolioVisualCapture_RankAfterWin$"
                    };
                else if (SessionState.GetBool(VisualAuditSessionKey, false))
                    filter.groupNames = new[]
                    {
                        @"^Meowdoku\.Tests\.PlayMode\.PrimaryNavigationPlayModeTests\.PortfolioVisualCapture_.*$"
                    };
                else if (SessionState.GetBool(PlatformSessionKey, false))
                    filter.groupNames = new[]
                    {
                        @"^Meowdoku\.Tests\.PlayMode\.PrimaryNavigationPlayModeTests\.Platform"
                    };
                _runner.Execute(new ExecutionSettings(filter));
            }
            catch (Exception exception)
            {
                WriteResult("BRIDGE_ERROR\n" + exception, ActiveResultPath);
                SessionState.EraseBool(ActiveSessionKey);
                SessionState.EraseBool(PlatformSessionKey);
                SessionState.EraseBool(VisualAuditSessionKey);
                SessionState.EraseBool(RankVisualAuditSessionKey);
                ReleaseCallbacks();
            }
        }

        private static void RegisterCallbacks()
        {
            if (!SessionState.GetBool(ActiveSessionKey, false) ||
                _callbacks != null)
                return;
            _runner = ScriptableObject.CreateInstance<TestRunnerApi>();
            _callbacks = ScriptableObject.CreateInstance<ResultCallbacks>();
            _runner.RegisterCallbacks(_callbacks);
        }

        private static void CompleteRun(ITestResultAdaptor result)
        {
            if (!SessionState.GetBool(ActiveSessionKey, false))
                return;
            try
            {
                TestRunnerApi.SaveResultToFile(result, ActiveXmlResultPath);
                var builder = new StringBuilder();
                builder.Append("RESULT passed=")
                    .Append(result.PassCount)
                    .Append(" failed=")
                    .Append(result.FailCount)
                    .Append(" skipped=")
                    .Append(result.SkipCount)
                    .Append(" inconclusive=")
                    .Append(result.InconclusiveCount)
                    .Append(" duration=")
                    .Append(result.Duration.ToString("0.000"))
                    .AppendLine();

                var failures = new List<ITestResultAdaptor>();
                CollectFailures(result, failures);
                foreach (ITestResultAdaptor failure in failures)
                {
                    builder.Append("FAIL ")
                        .Append(failure.FullName)
                        .Append(": ")
                        .AppendLine(failure.Message);
                }
                WriteResult(builder.ToString(), ActiveResultPath);
            }
            catch (Exception exception)
            {
                WriteResult("BRIDGE_ERROR\n" + exception, ActiveResultPath);
            }
            finally
            {
                SessionState.EraseBool(ActiveSessionKey);
                SessionState.EraseBool(PlatformSessionKey);
                SessionState.EraseBool(VisualAuditSessionKey);
                SessionState.EraseBool(RankVisualAuditSessionKey);
                ReleaseCallbacks();
            }
        }

        private static void FailRun(string message)
        {
            try
            {
                WriteResult(
                    "BRIDGE_ERROR\n" + (message ?? string.Empty),
                    ActiveResultPath);
            }
            finally
            {
                SessionState.EraseBool(ActiveSessionKey);
                SessionState.EraseBool(PlatformSessionKey);
                SessionState.EraseBool(VisualAuditSessionKey);
                SessionState.EraseBool(RankVisualAuditSessionKey);
                ReleaseCallbacks();
            }
        }

        private static void CollectFailures(
            ITestResultAdaptor result,
            ICollection<ITestResultAdaptor> failures)
        {
            if (result == null) return;
            if (!result.HasChildren)
            {
                if (result.TestStatus == TestStatus.Failed)
                    failures.Add(result);
                return;
            }
            foreach (ITestResultAdaptor child in result.Children)
                CollectFailures(child, failures);
        }

        private static string ActiveResultPath =>
            SessionState.GetBool(RankVisualAuditSessionKey, false)
                ? RankVisualAuditResultPath
                : SessionState.GetBool(VisualAuditSessionKey, false)
                ? VisualAuditResultPath
                : SessionState.GetBool(PlatformSessionKey, false)
                ? PlatformResultPath
                : ResultPath;

        private static string ActiveXmlResultPath =>
            SessionState.GetBool(RankVisualAuditSessionKey, false)
                ? RankVisualAuditXmlResultPath
                : SessionState.GetBool(VisualAuditSessionKey, false)
                ? VisualAuditXmlResultPath
                : SessionState.GetBool(PlatformSessionKey, false)
                ? PlatformXmlResultPath
                : XmlResultPath;

        private static void WriteResult(string value, string resultPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)
                ?.FullName;
            if (string.IsNullOrEmpty(projectRoot)) return;
            string path = Path.Combine(projectRoot, resultPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path) ??
                                      projectRoot);
            File.WriteAllText(path, value ?? string.Empty);
        }

        private static void ReleaseCallbacks()
        {
            if (_runner != null && _callbacks != null)
                _runner.UnregisterCallbacks(_callbacks);
            if (_callbacks != null)
                UnityEngine.Object.DestroyImmediate(_callbacks);
            if (_runner != null)
                UnityEngine.Object.DestroyImmediate(_runner);
            _callbacks = null;
            _runner = null;
        }

        private static void BeforeAssemblyReload()
        {
            EditorApplication.update -= Poll;
            AssemblyReloadEvents.beforeAssemblyReload -= BeforeAssemblyReload;
            EditorApplication.quitting -= DisposeForQuit;
            _runEvent?.Dispose();
            _runEvent = null;
            _platformEvent?.Dispose();
            _platformEvent = null;
            _visualAuditEvent?.Dispose();
            _visualAuditEvent = null;
            _rankVisualAuditEvent?.Dispose();
            _rankVisualAuditEvent = null;
            _resetEvent?.Dispose();
            _resetEvent = null;
            _callbacks = null;
            _runner = null;
        }

        private static void DisposeForQuit()
        {
            SessionState.EraseBool(ActiveSessionKey);
            SessionState.EraseBool(PlatformSessionKey);
            SessionState.EraseBool(VisualAuditSessionKey);
            SessionState.EraseBool(RankVisualAuditSessionKey);
            ReleaseCallbacks();
            _runEvent?.Dispose();
            _runEvent = null;
            _platformEvent?.Dispose();
            _platformEvent = null;
            _visualAuditEvent?.Dispose();
            _visualAuditEvent = null;
            _rankVisualAuditEvent?.Dispose();
            _rankVisualAuditEvent = null;
            _resetEvent?.Dispose();
            _resetEvent = null;
        }

        private sealed class ResultCallbacks : ScriptableObject, IErrorCallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                CompleteRun(result);
            }

            public void OnError(string message)
            {
                FailRun(message);
            }
        }
    }
}
