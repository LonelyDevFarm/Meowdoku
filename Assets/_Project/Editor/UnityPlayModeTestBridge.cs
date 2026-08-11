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
        internal const string ResultPath =
            "Temp/MeowdokuPlayModeTestResult.txt";
        internal const string XmlResultPath =
            "Temp/MeowdokuPlayModeTestResult.xml";

        private const string ActiveSessionKey =
            "Meowdoku.UnityPlayModeTestBridge.Active";

        private static EventWaitHandle _runEvent;
        private static bool _runPending;
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

        [MenuItem("Tools/Meowdoku/Run PlayMode Tests")]
        private static void RunFromMenu()
        {
            QueueRun();
        }

        private static void TryCreateEvent()
        {
            try
            {
                _runEvent = new EventWaitHandle(
                    false,
                    EventResetMode.AutoReset,
                    EventName);
            }
            catch (Exception)
            {
                _runEvent = null;
            }
        }

        private static void Poll()
        {
            try
            {
                if (_runEvent != null && _runEvent.WaitOne(0))
                    QueueRun();
            }
            catch (ObjectDisposedException)
            {
                _runEvent = null;
            }
        }

        private static void QueueRun()
        {
            if (_runPending || SessionState.GetBool(ActiveSessionKey, false))
                return;
            _runPending = true;
            EditorApplication.delayCall += RunWhenReady;
        }

        private static void RunWhenReady()
        {
            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += RunWhenReady;
                return;
            }

            _runPending = false;
            SessionState.SetBool(ActiveSessionKey, true);
            WriteResult("RUNNING");
            RegisterCallbacks();
            try
            {
                _runner.Execute(new ExecutionSettings(new Filter
                {
                    testMode =
                        UnityEditor.TestTools.TestRunner.Api.TestMode.PlayMode,
                    assemblyNames = new[] { "Meowdoku.PlayModeTests" }
                }));
            }
            catch (Exception exception)
            {
                WriteResult("BRIDGE_ERROR\n" + exception);
                SessionState.EraseBool(ActiveSessionKey);
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
                TestRunnerApi.SaveResultToFile(result, XmlResultPath);
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
                WriteResult(builder.ToString());
            }
            catch (Exception exception)
            {
                WriteResult("BRIDGE_ERROR\n" + exception);
            }
            finally
            {
                SessionState.EraseBool(ActiveSessionKey);
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

        private static void WriteResult(string value)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)
                ?.FullName;
            if (string.IsNullOrEmpty(projectRoot)) return;
            string path = Path.Combine(projectRoot, ResultPath);
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
            _callbacks = null;
            _runner = null;
        }

        private static void DisposeForQuit()
        {
            SessionState.EraseBool(ActiveSessionKey);
            ReleaseCallbacks();
            _runEvent?.Dispose();
            _runEvent = null;
        }

        private sealed class ResultCallbacks : ScriptableObject, ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                CompleteRun(result);
            }
        }
    }
}
