using Meowdoku.Core.Localization;
using UnityEditor;
using UnityEngine;

namespace Meowdoku.Editor
{
    [InitializeOnLoad]
    internal static class LocalizationCatalogAssetInstaller
    {
        internal const string CatalogPath =
            "Assets/_Project/Settings/LocalizationCatalog.asset";
        private const string CsvPath =
            "Assets/_Project/Localization/translations.csv";

        static LocalizationCatalogAssetInstaller()
        {
            EditorApplication.delayCall += InstallIfPossible;
        }

        [MenuItem("Meowdoku/Port/Create Localization Catalog")]
        private static void InstallFromMenu()
        {
            LocalizationCatalog catalog = GetOrCreate();
            if (catalog != null) Selection.activeObject = catalog;
        }

        internal static LocalizationCatalog GetOrCreate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling || EditorApplication.isUpdating)
                return null;

            TextAsset csv = AssetDatabase.LoadAssetAtPath<TextAsset>(CsvPath);
            if (csv == null) return null;
            EnsureFolder("Assets/_Project", "Settings");

            LocalizationCatalog catalog =
                AssetDatabase.LoadAssetAtPath<LocalizationCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<LocalizationCatalog>();
                SetCsv(catalog, csv);
                AssetDatabase.CreateAsset(catalog, CatalogPath);
                AssetDatabase.SaveAssets();
                return catalog;
            }

            SerializedObject data = new(catalog);
            SerializedProperty property = data.FindProperty("translationsCsv");
            if (property != null && property.objectReferenceValue != csv)
            {
                property.objectReferenceValue = csv;
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();
            }
            return catalog;
        }

        private static void InstallIfPossible()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += InstallIfPossible;
                return;
            }
            GetOrCreate();
        }

        private static void SetCsv(LocalizationCatalog catalog, TextAsset csv)
        {
            SerializedObject data = new(catalog);
            SerializedProperty property = data.FindProperty("translationsCsv");
            if (property != null) property.objectReferenceValue = csv;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
