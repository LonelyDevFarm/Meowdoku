using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Meowdoku.Gameplay;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Meowdoku.Editor
{
    [InitializeOnLoad]
    internal static class CatSpriteAnimationInstaller
    {
        internal const string CatalogPath =
            "Assets/_Project/Settings/CatSpriteAnimationCatalog.asset";
        internal const string AtlasPath =
            "Assets/_Project/Sprites/game/cat/cat_atlas.png";
        internal const string JsonPath =
            "Assets/_Project/Sprites/game/cat/cat_atlas_regions.json";
        internal const string CellPrefabPath =
            "Assets/_Project/Prefabs/Cell.prefab";

        private static readonly Regex HeightRegex = new Regex(
            "\\\"height\\\"\\s*:\\s*(\\d+)", RegexOptions.Compiled);
        private static readonly Regex FrameRegex = new Regex(
            @"\[\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\]",
            RegexOptions.Compiled);

        private static bool installQueued;

        private const int AppearCount = 41;
        private const int CryCount = 79;
        private const int FrustratedCount = 96;
        private const int IdleCount = 81;
        private const int TotalCount =
            AppearCount + CryCount + FrustratedCount + IdleCount;

        static CatSpriteAnimationInstaller()
        {
            EditorApplication.delayCall += InstallIfPossible;
        }

        [MenuItem("Meowdoku/Port/Install Cat Sprite Animation")]
        private static void InstallFromMenu()
        {
            InstallIfPossible();
            Selection.activeObject =
                AssetDatabase.LoadAssetAtPath<CatSpriteAnimationCatalog>(CatalogPath);
        }

        internal static bool Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling || EditorApplication.isUpdating)
                return false;

            if (EnsureAtlasImport())
            {
                QueueInstall();
                return false;
            }

            List<Sprite> sprites = LoadSortedSprites();
            if (sprites.Count != TotalCount)
                return false;

            EnsureFolder("Assets/_Project", "Settings");
            CatSpriteAnimationCatalog catalog =
                AssetDatabase.LoadAssetAtPath<CatSpriteAnimationCatalog>(CatalogPath);
            bool created = catalog == null;
            if (created)
            {
                catalog = ScriptableObject.CreateInstance<CatSpriteAnimationCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            bool catalogChanged = ConfigureCatalog(catalog, sprites);
            bool prefabChanged = ConfigurePrefab(catalog);
            if (created || catalogChanged || prefabChanged)
                AssetDatabase.SaveAssets();
            return true;
        }

        internal static bool InstallIfReady()
        {
            return Install();
        }

        private static void InstallIfPossible()
        {
            installQueued = false;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                QueueInstall();
                return;
            }

            Install();
        }

        private static void QueueInstall()
        {
            if (installQueued) return;
            installQueued = true;
            EditorApplication.delayCall += InstallIfPossible;
        }

        private static bool EnsureAtlasImport()
        {
            TextureImporter importer =
                AssetImporter.GetAtPath(AtlasPath) as TextureImporter;
            if (importer == null || !TryLoadSourceRects(out Rect[] sourceRects))
                return false;

            var textureSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(textureSettings);
            bool spriteMeshTypeChanged =
                textureSettings.spriteMeshType != SpriteMeshType.FullRect;
            bool settingsChanged =
                importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Multiple ||
                importer.textureCompression != TextureImporterCompression.Uncompressed ||
                importer.maxTextureSize != 8192 || importer.mipmapEnabled ||
                !importer.alphaIsTransparency ||
                spriteMeshTypeChanged;

            if (settingsChanged)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 8192;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                if (spriteMeshTypeChanged)
                {
                    textureSettings.spriteMeshType = SpriteMeshType.FullRect;
                    importer.SetTextureSettings(textureSettings);
                }
            }

            var factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider =
                factories.GetSpriteEditorDataProviderFromObject(importer);
            if (provider == null) return false;
            provider.InitSpriteEditorDataProvider();

            SpriteRect[] current = provider.GetSpriteRects();
            bool rectsChanged = !SpriteRectsMatch(current, sourceRects);
            if (rectsChanged)
            {
                var idsByName = new Dictionary<string, GUID>(current.Length);
                for (int i = 0; i < current.Length; i++)
                {
                    if (!string.IsNullOrEmpty(current[i].name) &&
                        !idsByName.ContainsKey(current[i].name))
                        idsByName.Add(current[i].name, current[i].spriteID);
                }

                var updated = new SpriteRect[sourceRects.Length];
                for (int i = 0; i < updated.Length; i++)
                {
                    string name = "cat_atlas_" + i;
                    updated[i] = new SpriteRect
                    {
                        name = name,
                        rect = sourceRects[i],
                        alignment = SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                        spriteID = idsByName.TryGetValue(name, out GUID id)
                            ? id
                            : GUID.Generate()
                    };
                }
                provider.SetSpriteRects(updated);
                provider.Apply();
            }

            if (!settingsChanged && !rectsChanged) return false;
            importer.SaveAndReimport();
            return true;
        }

        private static bool TryLoadSourceRects(out Rect[] rects)
        {
            rects = null;
            if (!File.Exists(JsonPath)) return false;

            string json = File.ReadAllText(JsonPath);
            Match heightMatch = HeightRegex.Match(json);
            if (!heightMatch.Success ||
                !int.TryParse(heightMatch.Groups[1].Value, out int textureHeight))
                return false;

            MatchCollection frames = FrameRegex.Matches(json);
            if (frames.Count != TotalCount) return false;

            rects = new Rect[TotalCount];
            for (int i = 0; i < frames.Count; i++)
            {
                Match frame = frames[i];
                int x = int.Parse(frame.Groups[1].Value);
                int y = int.Parse(frame.Groups[2].Value);
                int width = int.Parse(frame.Groups[3].Value);
                int height = int.Parse(frame.Groups[4].Value);
                rects[i] = new Rect(x, textureHeight - y - height, width, height);
            }
            return true;
        }

        private static bool SpriteRectsMatch(SpriteRect[] current, Rect[] expected)
        {
            if (current.Length != expected.Length) return false;
            for (int i = 0; i < expected.Length; i++)
            {
                if (current[i].name != "cat_atlas_" + i ||
                    current[i].rect != expected[i])
                    return false;
            }
            return true;
        }

        private static List<Sprite> LoadSortedSprites()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(AtlasPath);
            var sprites = new List<Sprite>(assets.Length);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite) sprites.Add(sprite);
            }
            sprites.Sort((left, right) =>
                NumericSuffix(left.name).CompareTo(NumericSuffix(right.name)));
            return sprites;
        }

        private static int NumericSuffix(string name)
        {
            int start = name.Length;
            while (start > 0 && char.IsDigit(name[start - 1])) start--;
            return start < name.Length &&
                   int.TryParse(name.Substring(start), out int value)
                ? value
                : int.MaxValue;
        }

        private static bool ConfigureCatalog(
            CatSpriteAnimationCatalog catalog,
            List<Sprite> sprites)
        {
            SerializedObject data = new SerializedObject(catalog);
            bool changed = false;
            int offset = 0;
            changed |= SetSprites(data.FindProperty("appear"), sprites, offset, AppearCount);
            offset += AppearCount;
            changed |= SetSprites(data.FindProperty("cry"), sprites, offset, CryCount);
            offset += CryCount;
            changed |= SetSprites(data.FindProperty("frustrated"), sprites, offset, FrustratedCount);
            offset += FrustratedCount;
            changed |= SetSprites(data.FindProperty("idle"), sprites, offset, IdleCount);
            changed |= SetFloat(data.FindProperty("fps"), 30f);
            changed |= SetFloat(data.FindProperty("idleInterval"), 5f);
            if (changed)
            {
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(catalog);
            }
            return changed;
        }

        private static bool ConfigurePrefab(CatSpriteAnimationCatalog catalog)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(CellPrefabPath);
            if (root == null) return false;
            try
            {
                CellView cell = root.GetComponent<CellView>();
                if (cell == null) return false;
                Image target = cell.catIcon;
                if (target == null) return false;

                CatSpriteAnimationView view =
                    target.GetComponent<CatSpriteAnimationView>();
                bool changed = false;
                if (view == null)
                {
                    view = target.gameObject.AddComponent<CatSpriteAnimationView>();
                    changed = true;
                }

                SerializedObject viewData = new SerializedObject(view);
                changed |= SetObject(viewData.FindProperty("target"), target);
                changed |= SetObject(viewData.FindProperty("catalog"), catalog);
                if (viewData.hasModifiedProperties)
                    viewData.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject cellData = new SerializedObject(cell);
                changed |= SetObject(
                    cellData.FindProperty("catSpriteAnimation"), view);
                if (cellData.hasModifiedProperties)
                    cellData.ApplyModifiedPropertiesWithoutUndo();

                if (!target.preserveAspect)
                {
                    target.preserveAspect = true;
                    changed = true;
                }
                if (changed) PrefabUtility.SaveAsPrefabAsset(root, CellPrefabPath);
                return changed;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool SetSprites(
            SerializedProperty property,
            List<Sprite> sprites,
            int offset,
            int count)
        {
            bool changed = property.arraySize != count;
            if (!changed)
            {
                for (int i = 0; i < count; i++)
                {
                    if (property.GetArrayElementAtIndex(i).objectReferenceValue ==
                        sprites[offset + i]) continue;
                    changed = true;
                    break;
                }
            }
            if (!changed) return false;
            property.arraySize = count;
            for (int i = 0; i < count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue =
                    sprites[offset + i];
            return true;
        }

        private static bool SetFloat(SerializedProperty property, float value)
        {
            if (Mathf.Approximately(property.floatValue, value)) return false;
            property.floatValue = value;
            return true;
        }

        private static bool SetObject(SerializedProperty property, Object value)
        {
            if (property.objectReferenceValue == value) return false;
            property.objectReferenceValue = value;
            return true;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
