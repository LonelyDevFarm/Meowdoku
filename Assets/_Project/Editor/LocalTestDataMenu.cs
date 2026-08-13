using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Meowdoku.Editor
{
    internal static class LocalTestDataMenu
    {
        private const string MenuPath =
            "Meowdoku/Test/Reset All Local Data";

        [MenuItem(MenuPath, false, 500)]
        private static void ResetAllLocalData()
        {
            string dataPath = Path.GetFullPath(Application.persistentDataPath);
            if (!IsExpectedMeowdokuPath(dataPath))
            {
                EditorUtility.DisplayDialog(
                    "Reset cancelled",
                    "Unity returned an unexpected persistent-data path:\n\n" +
                    dataPath,
                    "Close");
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Reset all Meowdoku local data?",
                "This removes tutorial, level, active game, profile, streak, " +
                "rank and settings data for this local portfolio build.\n\n" +
                dataPath,
                "Reset",
                "Cancel");
            if (!confirmed) return;

            try
            {
                if (Directory.Exists(dataPath))
                    Directory.Delete(dataPath, true);
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                EditorUtility.DisplayDialog(
                    "Local data reset",
                    "The next AppScene Play starts as a new player.",
                    "OK");
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog(
                    "Reset failed",
                    "Close every Meowdoku player build and try again.\n\n" +
                    exception.Message,
                    "Close");
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool CanResetAllLocalData() =>
            !EditorApplication.isPlayingOrWillChangePlaymode &&
            !EditorApplication.isCompiling &&
            !EditorApplication.isUpdating;

        private static bool IsExpectedMeowdokuPath(string dataPath)
        {
            if (string.IsNullOrWhiteSpace(dataPath)) return false;
            string normalized = dataPath.Replace('\\', '/').TrimEnd('/');
            return normalized.EndsWith(
                "/Meowdoku Portfolio/Meowdoku",
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
