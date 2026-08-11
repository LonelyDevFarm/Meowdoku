using System;
using System.Collections.Generic;
using Meowdoku.Core.UI;
using UnityEditor;
using UnityEngine;

namespace Meowdoku.Editor
{
    [InitializeOnLoad]
    internal static class UIRegistryAssetInstaller
    {
        internal const string RegistryPath =
            "Assets/_Project/Settings/UIRegistry.asset";

        private static readonly Registration[] Registrations =
        {
            new(UiName.Splash, "Assets/_Project/Prefabs/UI/SplashPage.prefab"),
            new(UiName.Home, "Assets/_Project/Prefabs/UI/HomePage.prefab"),
            new(UiName.Game, "Assets/_Project/Prefabs/UI/GamePage.prefab"),
            new(UiName.DailyGame, "Assets/_Project/Prefabs/UI/GamePage.prefab"),
            new(UiName.Tutorial, "Assets/_Project/Prefabs/UI/TutorialPage.prefab"),
            new(UiName.Setting, "Assets/_Project/Prefabs/UI/SettingsPage.prefab"),
            new(UiName.Language, "Assets/_Project/Prefabs/UI/LanguagePage.prefab"),
            new(UiName.Privacy,
                PlatformGuidePrefabInstaller.PrivacyPath),
            new(UiName.PreAttGuide,
                PlatformGuidePrefabInstaller.PreAttPath),
            new(UiName.PreAttGuideV2,
                PlatformGuidePrefabInstaller.PreAttV2Path),
            new(UiName.PrePushGuide,
                PlatformGuidePrefabInstaller.PrePushPath),
            new(UiName.Bank, "Assets/_Project/Prefabs/UI/BankPage.prefab"),
            new(UiName.HowToPlay,
                "Assets/_Project/Prefabs/UI/HowToPlayPage.prefab"),
            new(UiName.HowToPlayPaged,
                "Assets/_Project/Prefabs/UI/HowToPlayPagedPage.prefab"),
            new(UiName.Win, "Assets/_Project/Prefabs/UI/WinPage.prefab"),
            new(UiName.DailyWin, "Assets/_Project/Prefabs/UI/WinPage.prefab"),
            new(UiName.Fail, "Assets/_Project/Prefabs/UI/FailPage.prefab"),
            new(UiName.DailyFail, "Assets/_Project/Prefabs/UI/FailPage.prefab"),
            new(UiName.AdRewardRestored,
                AdRewardRestoredPagePrefabInstaller.PrefabPath),
            new(UiName.Award,
                "Assets/_Project/Prefabs/UI/AwardPage.prefab"),
            new(UiName.Streak,
                "Assets/_Project/Prefabs/UI/StreakPage.prefab"),
            new(UiName.StreakResume,
                "Assets/_Project/Prefabs/UI/StreakResumePage.prefab"),
            new(UiName.StreakBackfill,
                "Assets/_Project/Prefabs/UI/StreakBackfillPage.prefab"),
            new(UiName.AbSwitchPopup,
                "Assets/_Project/Prefabs/UI/AbSwitchPopup.prefab"),
            new(UiName.RankActivityOpenPopup,
                RankActivityPagePrefabInstaller.OpenPopupPath),
            new(UiName.RankActivityPage,
                RankActivityPagePrefabInstaller.PagePath),
            new(UiName.RankActivityHowToPlay,
                RankActivityPagePrefabInstaller.HowToPlayPath),
            new(UiName.RankActivityChange,
                RankActivityPagePrefabInstaller.ChangePath),
            new(UiName.Profile,
                "Assets/_Project/Prefabs/UI/ProfilePage.prefab")
        };

        static UIRegistryAssetInstaller()
        {
            EditorApplication.delayCall += QueueInstall;
            EditorApplication.playModeStateChanged += HandlePlayModeChanged;
        }

        [MenuItem("Meowdoku/Port/Update UI Registry")]
        private static void InstallFromMenu()
        {
            UIRegistry registry = Install();
            if (registry != null) Selection.activeObject = registry;
        }

        private static void QueueInstall()
        {
            EditorApplication.delayCall += InstallIfPossible;
        }

        private static void InstallIfPossible()
        {
            Install();
        }

        internal static UIRegistry InstallIfReady()
        {
            return Install();
        }

        private static UIRegistry Install()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += InstallIfPossible;
                return null;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return null;

            var resolved = new List<ResolvedRegistration>(Registrations.Length);
            foreach (Registration registration in Registrations)
            {
                GameObject prefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(registration.Path);
                UIFrameWindow window =
                    prefab != null ? prefab.GetComponent<UIFrameWindow>() : null;
                if (window != null)
                    resolved.Add(new ResolvedRegistration(
                        registration.Name,
                        window));
            }
            if (resolved.Count == 0) return null;

            EnsureFolder("Assets/_Project", "Settings");
            UIRegistry registry =
                AssetDatabase.LoadAssetAtPath<UIRegistry>(RegistryPath);
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<UIRegistry>();
                AssetDatabase.CreateAsset(registry, RegistryPath);
            }

            SerializedObject data = new(registry);
            SerializedProperty entries = data.FindProperty("entries");
            if (EntriesMatch(entries, resolved)) return registry;
            entries.arraySize = resolved.Count;
            for (int index = 0; index < resolved.Count; index++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("name").intValue =
                    (int)resolved[index].Name;
                entry.FindPropertyRelative("prefab").objectReferenceValue =
                    resolved[index].Prefab;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            return registry;
        }

        private static bool EntriesMatch(
            SerializedProperty entries,
            IReadOnlyList<ResolvedRegistration> resolved)
        {
            if (entries == null || entries.arraySize != resolved.Count)
                return false;
            for (int index = 0; index < resolved.Count; index++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                if (entry.FindPropertyRelative("name").intValue !=
                        (int)resolved[index].Name ||
                    entry.FindPropertyRelative("prefab").objectReferenceValue !=
                        resolved[index].Prefab)
                    return false;
            }
            return true;
        }

        private static void HandlePlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += QueueInstall;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }

        private readonly struct Registration
        {
            public Registration(UiName name, string path)
            {
                Name = name;
                Path = path;
            }

            public UiName Name { get; }
            public string Path { get; }
        }

        private readonly struct ResolvedRegistration
        {
            public ResolvedRegistration(UiName name, UIFrameWindow prefab)
            {
                Name = name;
                Prefab = prefab;
            }

            public UiName Name { get; }
            public UIFrameWindow Prefab { get; }
        }
    }
}
