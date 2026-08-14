using UnityEditor;

namespace Meowdoku.Editor
{
    [InitializeOnLoad]
    internal static class RankVisualPrefabRefreshMarker
    {
        static RankVisualPrefabRefreshMarker()
        {
            EditorApplication.delayCall += Install;
        }

        private static void Install()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Install;
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            DailyMetaPagePrefabInstaller.InstallIfReady();
            RankActivityPagePrefabInstaller.InstallIfReady();
            AssetDatabase.SaveAssets();
        }
    }
}
