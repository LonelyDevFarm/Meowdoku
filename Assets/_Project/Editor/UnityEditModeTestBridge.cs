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
    /// Runs the project's real Unity EditMode suite on request from local
    /// development tools. Results are written under Temp and this class is
    /// excluded from players by the Editor-only assembly definition.
    /// </summary>
    [InitializeOnLoad]
    internal static class UnityEditModeTestBridge
    {
        internal const string EventName =
            @"Local\Meowdoku.UnityEditModeTests";
        internal const string PlatformEventName =
            @"Local\Meowdoku.UnityEditModeTests.Platform";
        internal const string ResultPath =
            "Temp/MeowdokuEditModeTestResult.txt";
        internal const string XmlResultPath =
            "Temp/MeowdokuEditModeTestResult.xml";
        internal const string PlatformResultPath =
            "Temp/MeowdokuPlatformEditModeTestResult.txt";
        internal const string PlatformXmlResultPath =
            "Temp/MeowdokuPlatformEditModeTestResult.xml";

        private static EventWaitHandle _runEvent;
        private static EventWaitHandle _platformEvent;
        private static bool _runPending;
        private static bool _runActive;
        private static bool _platformPending;
        private static bool _platformActive;
        private static TestRunnerApi _runner;
        private static ResultCallbacks _callbacks;

        static UnityEditModeTestBridge()
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

        [MenuItem("Tools/Meowdoku/Run EditMode Tests %#&e")]
        private static void RunFromMenu()
        {
            QueueRun(false);
        }

        [MenuItem("Tools/Meowdoku/Run Platform EditMode Tests")]
        private static void RunPlatformFromMenu()
        {
            QueueRun(true);
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
            }
            catch (Exception)
            {
                _runEvent = null;
                _platformEvent = null;
            }
        }

        private static void Poll()
        {
            if (_runEvent == null || _platformEvent == null)
                TryCreateEvent();

            try
            {
                if (_runEvent != null && _runEvent.WaitOne(0))
                    QueueRun(false);
                if (_platformEvent != null && _platformEvent.WaitOne(0))
                    QueueRun(true);
                if (_runPending && !_runActive)
                    RunWhenReady();
            }
            catch (ObjectDisposedException)
            {
                _runEvent = null;
            }
        }

        private static void QueueRun(bool platformOnly)
        {
            if (_runPending || _runActive)
                return;

            _runPending = true;
            _platformPending = platformOnly;
        }

        private static void RunWhenReady()
        {
            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            _runPending = false;
            _runActive = true;
            _platformActive = _platformPending;
            _platformPending = false;
            WriteResult("RUNNING", ActiveResultPath);

            _runner = ScriptableObject.CreateInstance<TestRunnerApi>();
            _callbacks = new ResultCallbacks(CompleteRun);
            _runner.RegisterCallbacks(_callbacks);
            try
            {
                var filter = new Filter
                {
                    testMode =
                        UnityEditor.TestTools.TestRunner.Api.TestMode.EditMode,
                    assemblyNames = new[] { "Meowdoku.EditModeTests" }
                };
                if (_platformActive)
                    filter.groupNames = new[]
                    {
                        @"^Meowdoku\.Tests\.EditMode\.(AppRuntimeCompositionTests|PlatformPermissionTests|ProductServiceTests|SaveStoreTests|GameStateRepositoryTests|MetaPersistenceDurabilityTests|UIFrameworkTests|GameplayCatBurstViewTests|BoardIntroContractTests|SoundContractTests|StreakVisualCompositionTests|SourceRankActivityLayoutTests)"
                    };
                _runner.Execute(new ExecutionSettings(filter));
            }
            catch (Exception exception)
            {
                WriteResult("BRIDGE_ERROR\n" + exception, ActiveResultPath);
                ReleaseRunner();
            }
        }

        private static void CompleteRun(ITestResultAdaptor result)
        {
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
                ReleaseRunner();
            }
        }

        private static void CollectFailures(
            ITestResultAdaptor result,
            ICollection<ITestResultAdaptor> failures)
        {
            if (result == null)
                return;
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
            _platformActive ? PlatformResultPath : ResultPath;

        private static string ActiveXmlResultPath =>
            _platformActive ? PlatformXmlResultPath : XmlResultPath;

        private static void WriteResult(string value, string resultPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)
                ?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
                return;
            string path = Path.Combine(projectRoot, resultPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path) ??
                                      projectRoot);
            File.WriteAllText(path, value ?? string.Empty);
        }

        private static void ReleaseRunner()
        {
            if (_runner != null && _callbacks != null)
                _runner.UnregisterCallbacks(_callbacks);
            if (_runner != null)
                UnityEngine.Object.DestroyImmediate(_runner);
            _runner = null;
            _callbacks = null;
            _runActive = false;
            _platformActive = false;
        }

        private static void Dispose()
        {
            EditorApplication.update -= Poll;
            AssemblyReloadEvents.beforeAssemblyReload -= Dispose;
            EditorApplication.quitting -= Dispose;
            ReleaseRunner();
            _runEvent?.Dispose();
            _runEvent = null;
            _platformEvent?.Dispose();
            _platformEvent = null;
        }

        private sealed class ResultCallbacks : ICallbacks
        {
            private readonly Action<ITestResultAdaptor> _completed;

            public ResultCallbacks(Action<ITestResultAdaptor> completed)
            {
                _completed = completed;
            }

            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                _completed?.Invoke(result);
            }
        }
    }
}
