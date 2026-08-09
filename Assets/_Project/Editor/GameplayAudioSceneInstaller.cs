using System;
using System.Collections.Generic;
using Meowdoku.Gameplay;
using Meowdoku.Services;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Meowdoku.Editor
{
    /// <summary>
    /// Creates the serialized Unity equivalent of the SoundManager preload table.
    /// Playback never performs Resources or path lookup at runtime.
    /// </summary>
    [InitializeOnLoad]
    internal static class GameplayAudioSceneInstaller
    {
        private const string ScenePath = "Assets/_Project/Scenes/GameplayScene.unity";
        private const int MaxInstallAttempts = 300;
        private static int _remainingInstallAttempts;

        private const string CatalogPath = "Assets/_Project/Settings/SoundCatalog.asset";
        private const string UnityAudioRoot = "Assets/_Project/Audio/sfx/";
        private const string SourceAudioRoot = "res://assets/audio/sfx/";

        static GameplayAudioSceneInstaller()
        {
            QueueInstall();
            EditorSceneManager.sceneOpened += HandleSceneOpened;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        private static void HandleSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.path == ScenePath) QueueInstall();
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode) QueueInstall();
        }

        private static void QueueInstall()
        {
            _remainingInstallAttempts = MaxInstallAttempts;
            EditorApplication.update -= TryInstallWhenReady;
            EditorApplication.update += TryInstallWhenReady;
        }

        private static void TryInstallWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlaying ||
                EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Scene scene = SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.path == ScenePath)
            {
                EditorApplication.update -= TryInstallWhenReady;
                InstallIfNeeded();
                return;
            }

            if (scene.IsValid() && !string.IsNullOrEmpty(scene.path) ||
                --_remainingInstallAttempts <= 0)
                EditorApplication.update -= TryInstallWhenReady;
        }

        [MenuItem("Meowdoku/Port/Install Gameplay Audio")]
        private static void InstallFromMenu()
        {
            InstallIfNeeded();
        }

        private static void InstallIfNeeded()
        {
            if (EditorApplication.isPlaying ||
                EditorApplication.isPlayingOrWillChangePlaymode) return;
            SoundCatalog catalog = EnsureCatalog();
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath || catalog == null) return;
            GameplayManager manager = UnityEngine.Object.FindFirstObjectByType<GameplayManager>();
            GameplayFeedbackPresenter feedback =
                UnityEngine.Object.FindFirstObjectByType<GameplayFeedbackPresenter>();
            if (manager == null) return;

            Transform systems = FindOrCreateRoot(scene, "Systems");
            Transform audio = FindOrCreateChild(systems, "Audio");
            SoundService service = audio.GetComponent<SoundService>();
            if (service == null) service = audio.gameObject.AddComponent<SoundService>();
            Transform bgmObject = FindOrCreateChild(audio, "Bgm");
            AudioSource bgm = bgmObject.GetComponent<AudioSource>();
            if (bgm == null) bgm = bgmObject.gameObject.AddComponent<AudioSource>();
            bgm.playOnAwake = false;
            bgm.loop = true;

            SerializedObject serviceData = new SerializedObject(service);
            serviceData.FindProperty("catalog").objectReferenceValue = catalog;
            serviceData.FindProperty("bgmSource").objectReferenceValue = bgm;
            serviceData.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject managerData = new SerializedObject(manager);
            managerData.FindProperty("soundService").objectReferenceValue = service;
            managerData.ApplyModifiedPropertiesWithoutUndo();
            if (feedback != null)
            {
                SerializedObject feedbackData = new SerializedObject(feedback);
                feedbackData.FindProperty("soundService").objectReferenceValue = service;
                feedbackData.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static SoundCatalog EnsureCatalog()
        {
            SoundCatalog catalog = AssetDatabase.LoadAssetAtPath<SoundCatalog>(CatalogPath);
            if (catalog == null)
            {
                EnsureSettingsFolder();
                catalog = ScriptableObject.CreateInstance<SoundCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            SerializedObject data = new SerializedObject(catalog);
            SerializedProperty fixedClips = data.FindProperty("fixedClips");
            var fixedEntries = new List<KeyValuePair<SoundKind, AudioClip>>(27);
            foreach (SoundKind kind in Enum.GetValues(typeof(SoundKind)))
            {
                string sourcePath = SoundContract.SourcePath(kind);
                if (sourcePath.Length == 0) continue;
                fixedEntries.Add(new KeyValuePair<SoundKind, AudioClip>(
                    kind, LoadSourceClip(sourcePath)));
            }
            fixedClips.arraySize = fixedEntries.Count;
            for (int index = 0; index < fixedEntries.Count; index++)
            {
                SerializedProperty entry = fixedClips.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("kind").enumValueIndex =
                    (int)fixedEntries[index].Key;
                entry.FindPropertyRelative("clip").objectReferenceValue =
                    fixedEntries[index].Value;
            }

            IReadOnlyList<string> paths = SoundContract.DynamicSourcePaths;
            SerializedProperty pathClips = data.FindProperty("pathClips");
            pathClips.arraySize = paths.Count;
            for (int index = 0; index < paths.Count; index++)
            {
                SerializedProperty entry = pathClips.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("sourcePath").stringValue = paths[index];
                entry.FindPropertyRelative("clip").objectReferenceValue =
                    LoadSourceClip(paths[index]);
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static void EnsureSettingsFolder()
        {
            const string folder = "Assets/_Project/Settings";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/_Project", "Settings");
        }

        private static AudioClip LoadSourceClip(string sourcePath)
        {
            if (!sourcePath.StartsWith(SourceAudioRoot, StringComparison.Ordinal))
                return null;
            return AssetDatabase.LoadAssetAtPath<AudioClip>(
                UnityAudioRoot + sourcePath.Substring(SourceAudioRoot.Length));
        }

        private static Transform FindOrCreateRoot(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
                if (roots[index].name == name) return roots[index].transform;
            var gameObject = new GameObject(name);
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            return gameObject.transform;
        }

        private static Transform FindOrCreateChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) return child;
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            return gameObject.transform;
        }
    }
}
