using System;
using Meowdoku.Core.UI;
using Meowdoku.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Editor
{
    [InitializeOnLoad]
    internal static class PresenterCloseButtonMigration
    {
        private static readonly Spec[] Specs =
        {
            new(
                DailyMetaPagePrefabInstaller.ResumePrefabPath,
                typeof(StreakRevivePagePresenter)),
            new(
                DailyMetaPagePrefabInstaller.BackfillPrefabPath,
                typeof(StreakRevivePagePresenter)),
            new(
                DailyMetaPagePrefabInstaller.AbSwitchPrefabPath,
                typeof(AbSwitchPopupPresenter)),
            new(ProfilePagePrefabInstaller.PagePath,
                typeof(ProfilePagePresenter)),
            new(RankActivityPagePrefabInstaller.OpenPopupPath,
                typeof(RankActivityOpenPopupPresenter))
        };

        static PresenterCloseButtonMigration()
        {
            EditorApplication.delayCall += UpgradeIfPossible;
        }

        private static void UpgradeIfPossible()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += UpgradeIfPossible;
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            for (int index = 0; index < Specs.Length; index++)
                Upgrade(Specs[index]);
        }

        private static void Upgrade(Spec spec)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(spec.Path) == null)
                return;
            GameObject root = PrefabUtility.LoadPrefabContents(spec.Path);
            try
            {
                Component presenter =
                    root.GetComponentInChildren(spec.PresenterType, true);
                if (presenter == null) return;
                Button close = FindCloseButton(root);
                if (close == null) return;

                SerializedObject data = new(presenter);
                SerializedProperty action =
                    data.FindProperty("actionCloseButton");
                SerializedProperty baseClose = data.FindProperty("closeButton");
                bool changed = false;
                if (action != null &&
                    action.objectReferenceValue != close)
                {
                    action.objectReferenceValue = close;
                    changed = true;
                }
                if (baseClose != null &&
                    baseClose.objectReferenceValue != null)
                {
                    baseClose.objectReferenceValue = null;
                    changed = true;
                }
                if (!changed) return;
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, spec.Path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Button FindCloseButton(GameObject root)
        {
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int index = 0; index < buttons.Length; index++)
                if (buttons[index].name == "CloseBtn" ||
                    buttons[index].name == "CloseButton")
                    return buttons[index];
            return null;
        }

        private readonly struct Spec
        {
            public Spec(string path, Type presenterType)
            {
                Path = path;
                PresenterType = presenterType;
            }

            public string Path { get; }
            public Type PresenterType { get; }
        }
    }
}
