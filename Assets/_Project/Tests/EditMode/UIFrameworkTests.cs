using System.Collections;
using Meowdoku.Core.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Meowdoku.Tests.EditMode
{
    public sealed class UIFrameworkTests
    {
        [Test]
        public void LayerValues_MatchGodotContract()
        {
            Assert.That((int)UiLayer.Default, Is.EqualTo(0));
            Assert.That((int)UiLayer.Popup, Is.EqualTo(100));
            Assert.That((int)UiLayer.Notice, Is.EqualTo(200));
            Assert.That((int)UiLayer.Modal, Is.EqualTo(300));
            Assert.That((int)UiLayer.Tutorial, Is.EqualTo(400));
            Assert.That((int)UiLayer.Loading, Is.EqualTo(500));
            Assert.That(UiLayerConfig.ZStep, Is.EqualTo(50));
            Assert.That(UiLayerConfig.ZMax, Is.EqualTo(4000));
            Assert.That(
                UiLayerConfig.SortingBase(UiLayer.Default),
                Is.EqualTo(0));
            Assert.That(
                UiLayerConfig.SortingBase(UiLayer.Popup),
                Is.EqualTo(UiLayerConfig.RuntimeLayerStride));
        }

        [Test]
        public void Registry_RejectsDuplicateAndMissingPrefabEntries()
        {
            UIRegistry registry = ScriptableObject.CreateInstance<UIRegistry>();
            GameObject prefabObject = CreateWindowPrefab("HomePrefab");
            UIFrameWindow prefab = prefabObject.GetComponent<UIFrameWindow>();
            registry.SetEntriesForTests(
                new UIRegistryEntry(UiName.Home, prefab),
                new UIRegistryEntry(UiName.Home, prefab),
                new UIRegistryEntry(UiName.Game, null));

            Assert.That(registry.ValidateEntries(), Has.Count.EqualTo(2));
            Assert.That(registry.TryGetPrefab(UiName.Home, out UIFrameWindow found),
                Is.True);
            Assert.That(found, Is.SameAs(prefab));
            Assert.That(registry.TryGetPrefab(UiName.Game, out _), Is.False);

            Object.DestroyImmediate(prefabObject);
            Object.DestroyImmediate(registry);
        }

        [UnityTest]
        public IEnumerator ShowHide_ReusesCachedWindowAndEmitsOncePerTransition()
        {
            Setup setup = CreateSetup(UiName.Home, UiLayer.Default);
            int created = 0;
            int shown = 0;
            int hidden = 0;
            setup.Manager.Events.WindowCreated += (_, _) => created++;
            setup.Manager.Events.WindowShown += (_, _) => shown++;
            setup.Manager.Events.WindowHidden += (_, _) => hidden++;

            UIFrameWindow first = setup.Manager.Show(UiName.Home);
            UIFrameWindow second = setup.Manager.Show(UiName.Home);

            Assert.That(second, Is.SameAs(first));
            Assert.That(first.WindowState, Is.EqualTo(UiWindowState.Showing));
            Assert.That(setup.Manager.CachedWindowCount, Is.EqualTo(1));
            Assert.That(setup.Manager.GetWindowCount(UiLayer.Default), Is.EqualTo(1));
            Assert.That(created, Is.EqualTo(1));
            Assert.That(shown, Is.EqualTo(1));

            setup.Manager.Hide(UiName.Home);
            Assert.That(first.WindowState, Is.EqualTo(UiWindowState.Closing));

            UIFrameWindow reopened = setup.Manager.Show(UiName.Home);
            Assert.That(reopened, Is.SameAs(first));
            Assert.That(first.WindowState, Is.EqualTo(UiWindowState.Showing));
            Assert.That(shown, Is.EqualTo(1));

            yield return setup.Manager.HideForTests(UiName.Home);

            Assert.That(first.WindowState, Is.EqualTo(UiWindowState.Hidden));
            Assert.That(setup.Manager.GetWindowCount(UiLayer.Default), Is.Zero);
            Assert.That(hidden, Is.EqualTo(1));
            setup.Dispose();
        }

        [Test]
        public void SameLayerWindows_KeepIncreasingSortingAfterActivation()
        {
            GameObject root = new GameObject(
                "UIRoot",
                typeof(RectTransform),
                typeof(Canvas));
            UIManager manager = root.AddComponent<UIManager>();
            UIRegistry registry = ScriptableObject.CreateInstance<UIRegistry>();

            GameObject homeObject = CreateWindowPrefab("HomePrefab");
            UIFrameWindow home = homeObject.GetComponent<UIFrameWindow>();
            home.ConfigureForTests(UiLayer.Default, false, false);
            GameObject gameObject = CreateWindowPrefab("GamePrefab");
            UIFrameWindow game = gameObject.GetComponent<UIFrameWindow>();
            game.ConfigureForTests(UiLayer.Default, false, false);

            registry.SetEntriesForTests(
                new UIRegistryEntry(UiName.Home, home),
                new UIRegistryEntry(UiName.Game, game));
            manager.ConfigureForTests(registry, root.GetComponent<RectTransform>());

            UIFrameWindow shownHome = manager.Show(UiName.Home);
            UIFrameWindow shownGame = manager.Show(UiName.Game);

            Assert.That(shownHome.SortingOrder, Is.EqualTo(0));
            Assert.That(shownGame.SortingOrder, Is.EqualTo(UiLayerConfig.ZStep));
            Assert.That(shownGame.SortingOrder, Is.GreaterThan(shownHome.SortingOrder));
            Assert.That(shownHome.GetComponent<Canvas>().overrideSorting, Is.True);
            Assert.That(shownGame.GetComponent<Canvas>().overrideSorting, Is.True);

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(homeObject);
            Object.DestroyImmediate(gameObject);
            Object.DestroyImmediate(registry);
        }

        [Test]
        public void FullscreenWindow_OccludesOnlyWindowsBelowIt()
        {
            GameObject root = new GameObject("UIRoot", typeof(RectTransform));
            UIManager manager = root.AddComponent<UIManager>();
            UIRegistry registry = ScriptableObject.CreateInstance<UIRegistry>();

            GameObject homeObject = CreateWindowPrefab("HomePrefab");
            UIFrameWindow home = homeObject.GetComponent<UIFrameWindow>();
            home.ConfigureForTests(UiLayer.Default, true, false);
            GameObject popupObject = CreateWindowPrefab("PopupPrefab");
            UIFrameWindow popup = popupObject.GetComponent<UIFrameWindow>();
            popup.ConfigureForTests(UiLayer.Popup, false, true);
            GameObject tutorialObject = CreateWindowPrefab("TutorialPrefab");
            UIFrameWindow tutorial = tutorialObject.GetComponent<UIFrameWindow>();
            tutorial.ConfigureForTests(UiLayer.Tutorial, true, false);

            registry.SetEntriesForTests(
                new UIRegistryEntry(UiName.Home, home),
                new UIRegistryEntry(UiName.Setting, popup),
                new UIRegistryEntry(UiName.Tutorial, tutorial));
            manager.ConfigureForTests(registry, root.GetComponent<RectTransform>());

            UIFrameWindow shownHome = manager.Show(UiName.Home);
            UIFrameWindow shownPopup = manager.Show(UiName.Setting);
            Assert.That(shownHome.IsOccluded, Is.False);
            Assert.That(shownPopup.IsOccluded, Is.False);
            Assert.That(
                shownPopup.SortingOrder,
                Is.EqualTo(UiLayerConfig.SortingBase(UiLayer.Popup)));
            Assert.That(manager.MaskReferenceCount, Is.EqualTo(1));

            UIFrameWindow shownTutorial = manager.Show(UiName.Tutorial);
            Assert.That(shownTutorial.IsOccluded, Is.False);
            Assert.That(shownPopup.IsOccluded, Is.True);
            Assert.That(shownHome.IsOccluded, Is.True);

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(homeObject);
            Object.DestroyImmediate(popupObject);
            Object.DestroyImmediate(tutorialObject);
            Object.DestroyImmediate(registry);
        }

        private static Setup CreateSetup(UiName name, UiLayer layer)
        {
            GameObject root = new GameObject("UIRoot", typeof(RectTransform));
            UIManager manager = root.AddComponent<UIManager>();
            UIRegistry registry = ScriptableObject.CreateInstance<UIRegistry>();
            GameObject prefabObject = CreateWindowPrefab(name + "Prefab");
            UIFrameWindow prefab = prefabObject.GetComponent<UIFrameWindow>();
            prefab.ConfigureForTests(layer, false, false);
            registry.SetEntriesForTests(new UIRegistryEntry(name, prefab));
            manager.ConfigureForTests(registry, root.GetComponent<RectTransform>());
            return new Setup(root, prefabObject, registry, manager);
        }

        private static GameObject CreateWindowPrefab(string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.AddComponent<UIFrameWindow>();
            gameObject.SetActive(false);
            return gameObject;
        }

        private readonly struct Setup
        {
            private readonly GameObject _root;
            private readonly GameObject _prefab;
            private readonly UIRegistry _registry;

            public Setup(
                GameObject root,
                GameObject prefab,
                UIRegistry registry,
                UIManager manager)
            {
                _root = root;
                _prefab = prefab;
                _registry = registry;
                Manager = manager;
            }

            public UIManager Manager { get; }

            public void Dispose()
            {
                Object.DestroyImmediate(_root);
                Object.DestroyImmediate(_prefab);
                Object.DestroyImmediate(_registry);
            }
        }
    }
}
