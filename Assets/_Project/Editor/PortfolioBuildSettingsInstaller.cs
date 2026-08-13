using System;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Meowdoku.Editor
{
    /// <summary>
    /// Owns the reproducible offline portfolio player configuration. Prototype
    /// scenes remain in Assets for comparison but are excluded from players.
    /// </summary>
    [InitializeOnLoad]
    internal static class PortfolioBuildSettingsInstaller
    {
        internal const string WindowsBuildEventName =
            @"Local\Meowdoku.UnityPortfolioBuild.Windows";
        internal const string WindowsResultPath =
            "Temp/MeowdokuWindowsBuildResult.txt";
        internal const string AppScenePath =
            "Assets/_Project/Scenes/AppScene.unity";
        internal const string WindowsOutputPath =
            "Builds/Windows/Meowdoku.exe";
        internal const string ApplicationIdentifier =
            "com.meowdoku.portfolio";

        private static EventWaitHandle _windowsBuildEvent;
        private static bool _windowsBuildPending;
        private static bool _windowsBuildActive;

        static PortfolioBuildSettingsInstaller()
        {
            if (AssetDatabase.IsAssetImportWorkerProcess()) return;
            TryCreateEvent();
            EditorApplication.delayCall += ApplyWhenReady;
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
            AssemblyReloadEvents.beforeAssemblyReload -= Dispose;
            AssemblyReloadEvents.beforeAssemblyReload += Dispose;
            EditorApplication.quitting -= Dispose;
            EditorApplication.quitting += Dispose;
        }

        [MenuItem("Tools/Meowdoku/Apply Portfolio Build Settings")]
        private static void ApplyFromMenu()
        {
            InstallIfReady();
        }

        [MenuItem("Tools/Meowdoku/Build Windows Portfolio")]
        private static void BuildWindowsFromMenu()
        {
            QueueWindowsBuild();
        }

        internal static bool InstallIfReady()
        {
            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
                return false;

            bool changed = EnsureBuildSettings();
            changed |= SetString(
                () => PlayerSettings.companyName,
                value => PlayerSettings.companyName = value,
                "Meowdoku Portfolio");
            changed |= SetString(
                () => PlayerSettings.productName,
                value => PlayerSettings.productName = value,
                "Meowdoku");
            changed |= SetString(
                () => PlayerSettings.bundleVersion,
                value => PlayerSettings.bundleVersion = value,
                "0.0.1");

            if (PlayerSettings.defaultScreenWidth != 540)
            {
                PlayerSettings.defaultScreenWidth = 540;
                changed = true;
            }
            if (PlayerSettings.defaultScreenHeight != 960)
            {
                PlayerSettings.defaultScreenHeight = 960;
                changed = true;
            }
            if (PlayerSettings.fullScreenMode != FullScreenMode.Windowed)
            {
                PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
                changed = true;
            }
            if (!PlayerSettings.resizableWindow)
            {
                PlayerSettings.resizableWindow = true;
                changed = true;
            }
            if (PlayerSettings.defaultInterfaceOrientation !=
                UIOrientation.Portrait)
            {
                PlayerSettings.defaultInterfaceOrientation =
                    UIOrientation.Portrait;
                changed = true;
            }
            changed |= SetAutorotation();
            changed |= SetIdentifier(
                NamedBuildTarget.Standalone,
                ApplicationIdentifier);
            changed |= SetIdentifier(
                NamedBuildTarget.Android,
                ApplicationIdentifier);

            if (PlayerSettings.Android.targetArchitectures !=
                AndroidArchitecture.ARM64)
            {
                PlayerSettings.Android.targetArchitectures =
                    AndroidArchitecture.ARM64;
                changed = true;
            }
            if (PlayerSettings.Android.minSdkVersion !=
                AndroidSdkVersions.AndroidApiLevel25)
            {
                PlayerSettings.Android.minSdkVersion =
                    AndroidSdkVersions.AndroidApiLevel25;
                changed = true;
            }
            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) !=
                ScriptingImplementation.IL2CPP)
            {
                PlayerSettings.SetScriptingBackend(
                    NamedBuildTarget.Android,
                    ScriptingImplementation.IL2CPP);
                changed = true;
            }

            if (changed) AssetDatabase.SaveAssets();
            return true;
        }

        private static void ApplyWhenReady()
        {
            if (!InstallIfReady() &&
                !EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.delayCall += ApplyWhenReady;
        }

        private static bool EnsureBuildSettings()
        {
            EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
            if (current.Length == 1 && current[0].enabled &&
                string.Equals(
                    current[0].path,
                    AppScenePath,
                    StringComparison.Ordinal))
                return false;
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(AppScenePath, true)
            };
            return true;
        }

        private static bool SetAutorotation()
        {
            bool changed = false;
            if (!PlayerSettings.allowedAutorotateToPortrait)
            {
                PlayerSettings.allowedAutorotateToPortrait = true;
                changed = true;
            }
            if (PlayerSettings.allowedAutorotateToPortraitUpsideDown)
            {
                PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
                changed = true;
            }
            if (PlayerSettings.allowedAutorotateToLandscapeLeft)
            {
                PlayerSettings.allowedAutorotateToLandscapeLeft = false;
                changed = true;
            }
            if (PlayerSettings.allowedAutorotateToLandscapeRight)
            {
                PlayerSettings.allowedAutorotateToLandscapeRight = false;
                changed = true;
            }
            return changed;
        }

        private static bool SetIdentifier(
            NamedBuildTarget target,
            string value)
        {
            if (string.Equals(
                    PlayerSettings.GetApplicationIdentifier(target),
                    value,
                    StringComparison.Ordinal))
                return false;
            PlayerSettings.SetApplicationIdentifier(target, value);
            return true;
        }

        private static bool SetString(
            Func<string> getter,
            Action<string> setter,
            string value)
        {
            if (string.Equals(getter(), value, StringComparison.Ordinal))
                return false;
            setter(value);
            return true;
        }

        private static void TryCreateEvent()
        {
            try
            {
                _windowsBuildEvent = new EventWaitHandle(
                    false,
                    EventResetMode.AutoReset,
                    WindowsBuildEventName);
            }
            catch (Exception)
            {
                _windowsBuildEvent = null;
            }
        }

        private static void Poll()
        {
            try
            {
                if (_windowsBuildEvent != null &&
                    _windowsBuildEvent.WaitOne(0))
                    QueueWindowsBuild();
                if (_windowsBuildPending && !_windowsBuildActive)
                    BuildWindowsWhenReady();
            }
            catch (ObjectDisposedException)
            {
                _windowsBuildEvent = null;
            }
        }

        private static void QueueWindowsBuild()
        {
            if (_windowsBuildPending || _windowsBuildActive) return;
            _windowsBuildPending = true;
        }

        private static void BuildWindowsWhenReady()
        {
            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            _windowsBuildPending = false;
            _windowsBuildActive = true;
            WriteResult("RUNNING");
            try
            {
                if (!InstallIfReady())
                    throw new InvalidOperationException(
                        "Portfolio build settings are not ready.");
                string projectRoot = Directory.GetParent(Application.dataPath)
                    ?.FullName;
                if (string.IsNullOrEmpty(projectRoot))
                    throw new InvalidOperationException(
                        "Project root could not be resolved.");
                string output = Path.Combine(
                    projectRoot,
                    WindowsOutputPath.Replace('/', Path.DirectorySeparatorChar));
                string directory = Path.GetDirectoryName(output);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                var options = new BuildPlayerOptions
                {
                    scenes = new[] { AppScenePath },
                    locationPathName = output,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.Development
                };
                BuildReport report = BuildPipeline.BuildPlayer(options);
                BuildSummary summary = report.summary;
                WriteResult(
                    $"RESULT result={summary.result} " +
                    $"errors={summary.totalErrors} " +
                    $"warnings={summary.totalWarnings} " +
                    $"size={summary.totalSize} " +
                    $"duration={summary.totalTime.TotalSeconds:F3} " +
                    $"output={summary.outputPath}");
            }
            catch (Exception exception)
            {
                WriteResult("BUILD_ERROR\n" + exception);
            }
            finally
            {
                _windowsBuildActive = false;
            }
        }

        private static void WriteResult(string value)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)
                ?.FullName;
            if (string.IsNullOrEmpty(projectRoot)) return;
            string path = Path.Combine(projectRoot, WindowsResultPath);
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(path, value);
        }

        private static void Dispose()
        {
            EditorApplication.update -= Poll;
            AssemblyReloadEvents.beforeAssemblyReload -= Dispose;
            EditorApplication.quitting -= Dispose;
            _windowsBuildEvent?.Dispose();
            _windowsBuildEvent = null;
        }
    }
}
