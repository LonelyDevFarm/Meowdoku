using Meowdoku.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Tests.EditMode
{
    public sealed class CatSpriteAnimationInstallationTests
    {
        private const string CatalogPath =
            "Assets/_Project/Settings/CatSpriteAnimationCatalog.asset";
        private const string CellPrefabPath =
            "Assets/_Project/Prefabs/Cell.prefab";

        [Test]
        public void Catalog_ContainsSourceAnimationPartitions()
        {
            CatSpriteAnimationCatalog catalog =
                AssetDatabase.LoadAssetAtPath<CatSpriteAnimationCatalog>(CatalogPath);

            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Appear, Has.Length.EqualTo(41));
            Assert.That(catalog.Cry, Has.Length.EqualTo(79));
            Assert.That(catalog.Frustrated, Has.Length.EqualTo(96));
            Assert.That(catalog.Idle, Has.Length.EqualTo(81));
            Assert.That(catalog.Fps, Is.EqualTo(30f));
            Assert.That(catalog.IdleInterval, Is.EqualTo(5f));
        }

        [Test]
        public void CellPrefab_WiresAnimationViewToCatIconAndCatalog()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CellPrefabPath);
            CatSpriteAnimationCatalog catalog =
                AssetDatabase.LoadAssetAtPath<CatSpriteAnimationCatalog>(CatalogPath);
            Assert.That(prefab, Is.Not.Null);

            CellView cell = prefab.GetComponent<CellView>();
            Assert.That(cell, Is.Not.Null);
            Assert.That(cell.catIcon, Is.Not.Null);
            CatSpriteAnimationView view =
                cell.catIcon.GetComponent<CatSpriteAnimationView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(cell.catIcon.preserveAspect, Is.True);

            SerializedObject viewData = new SerializedObject(view);
            Assert.That(
                viewData.FindProperty("target").objectReferenceValue,
                Is.SameAs(cell.catIcon));
            Assert.That(
                viewData.FindProperty("catalog").objectReferenceValue,
                Is.SameAs(catalog));

            SerializedObject cellData = new SerializedObject(cell);
            Assert.That(
                cellData.FindProperty("catSpriteAnimation").objectReferenceValue,
                Is.SameAs(view));
        }

        [Test]
        public void CatFrames_KeepSourceNativeSizeScaleAndOffset()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(CellPrefabPath);
            CatSpriteAnimationCatalog catalog =
                AssetDatabase.LoadAssetAtPath<CatSpriteAnimationCatalog>(CatalogPath);
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                CellView cell = instance.GetComponent<CellView>();
                RectTransform cellRect = instance.GetComponent<RectTransform>();
                cellRect.sizeDelta = new Vector2(100f, 100f);

                cell.PlayDemoCat(true);

                Sprite first = catalog.Appear[0];
                RectTransform icon = cell.catIcon.rectTransform;
                Assert.That(icon.sizeDelta.x,
                    Is.EqualTo(first.rect.width).Within(0.001f));
                Assert.That(icon.sizeDelta.y,
                    Is.EqualTo(first.rect.height).Within(0.001f));
                Assert.That(icon.localScale.x,
                    Is.EqualTo(CatSpriteAnimationView.SourceScaleForTests)
                        .Within(0.0001f));
                Assert.That(icon.anchoredPosition,
                    Is.EqualTo(
                        CatSpriteAnimationView.SourceAnchoredPositionForTests));

                cell.PlayDemoCat(false);

                Sprite idleFinal = catalog.Idle[catalog.Idle.Length - 1];
                Assert.That(icon.sizeDelta.x,
                    Is.EqualTo(idleFinal.rect.width).Within(0.001f));
                Assert.That(icon.sizeDelta.y,
                    Is.EqualTo(idleFinal.rect.height).Within(0.001f));
                Assert.That(icon.localScale.x,
                    Is.EqualTo(CatSpriteAnimationView.SourceScaleForTests)
                        .Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
