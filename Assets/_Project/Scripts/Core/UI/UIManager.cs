using System;
using System.Collections;
using System.Collections.Generic;
using Meowdoku.Core.Ads;
using Meowdoku.Core.Config;
using Meowdoku.Core.Daily;
using Meowdoku.Core.Profile;
using Meowdoku.Core.Rank;
using Meowdoku.Core.Tracking;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Core.UI
{
    /// <summary>
    /// Cached, layered window manager ported from Godot UIManager. It is owned
    /// by the future AppBootstrap through serialized references; it is not a
    /// global self-creating singleton.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIManager : MonoBehaviour
    {
        private static readonly IReadOnlyDictionary<string, object>
            EmptyParameters = new Dictionary<string, object>(0);

        [Header("Registry and ownership")]
        [SerializeField] private UIRegistry registry;
        [SerializeField] private RectTransform windowRoot;
        [SerializeField] private ClockTicker clockTicker;
        [SerializeField] private DailyMetaRuntime dailyMetaRuntime;
        [SerializeField] private ProfileRuntime profileRuntime;
        [SerializeField] private RankActivityRuntime rankActivityRuntime;
        [SerializeField] private TrackingRuntime trackingRuntime;
        [SerializeField] private AdRuntime adRuntime;
        [SerializeField] private AbConfigRuntime abConfigRuntime;

        [Header("Shared mask")]
        [SerializeField] private Canvas maskCanvas;
        [SerializeField] private CanvasGroup maskGroup;

        [Header("Release-frame input guard")]
        [SerializeField] private Graphic inputBlocker;
        [SerializeField] private Canvas inputBlockerCanvas;

        private readonly Dictionary<UiName, UIFrameWindow> _cache = new();
        private readonly Dictionary<UiName, UIFrameWindow> _sourcePrefabs = new();
        private readonly Dictionary<UiLayer, List<UIFrameWindow>> _stacks = new();
        private readonly Dictionary<UiLayer, int> _nextZ = new();
        private readonly Dictionary<UiName, Coroutine> _closing = new();
        private readonly HashSet<UiName> _loading = new();
        private readonly Dictionary<int, int> _heldButtonGenerations = new();

        private int _maskReferenceCount;
        private bool _guardActive;
        private UITrackerObserver _trackerObserver;

        public UIEvents Events { get; } = new();
        public bool IsAnyLoading => _loading.Count > 0;
        public int CachedWindowCount => _cache.Count;
        public int MaskReferenceCount => _maskReferenceCount;
        public bool IsInputGuardActive => _guardActive;

        private void Awake()
        {
            if (windowRoot == null) windowRoot = transform as RectTransform;
            ResetTrackerObserver();
            SetMaskVisible(false, 0f, 0);
            SetInputGuard(false);
        }

        internal TrackerService Tracker => trackingRuntime != null
            ? trackingRuntime.Tracker
            : null;

        public UIFrameWindow Show(
            UiName name,
            IReadOnlyDictionary<string, object> parameters = null)
        {
            UIFrameWindow window = GetOrCreate(name);
            if (window == null) return null;

            parameters ??= EmptyParameters;
            if (window.IsShowing)
            {
                window.ShowLifecycle(parameters);
                window.transform.SetAsLastSibling();
                ReorderWindowTree();
                return window;
            }

            bool wasClosing = window.WindowState == UiWindowState.Closing;
            if (_closing.TryGetValue(name, out Coroutine closing))
            {
                if (closing != null) StopCoroutine(closing);
                _closing.Remove(name);
            }

            AssignSortingOrder(window);
            PushStack(window.Layer, window);
            window.ShowLifecycle(parameters);
            window.SetOccluded(false);

            if (window.ShowMask && !wasClosing)
                _maskReferenceCount++;
            if (!wasClosing)
            {
                Events.RaiseShown(name, window);
                if (_heldButtonGenerations.Count > 0)
                {
                    _guardActive = true;
                    SetInputGuard(true);
                }
            }

            RefreshOcclusion();
            UpdateMask();
            ReorderWindowTree();
            return window;
        }

        public void Hide(UiName name)
        {
            if (!TryBeginClosing(name, out UIFrameWindow window))
                return;

            Coroutine routine = StartCoroutine(HideRoutine(name, window));
            _closing[name] = routine;
        }

#if UNITY_EDITOR
        /// <summary>
        /// EditMode tests do not advance MonoBehaviour-started coroutines.
        /// This drives the exact same close routine through the test
        /// enumerator without changing player/runtime scheduling.
        /// </summary>
        internal IEnumerator HideForTests(UiName name)
        {
            if (!TryBeginClosing(name, out UIFrameWindow window))
                yield break;
            yield return HideRoutine(name, window);
        }
#endif

        public UIFrameWindow Get(UiName name)
        {
            return _cache.TryGetValue(name, out UIFrameWindow window) &&
                   window != null
                ? window
                : null;
        }

        public bool Has(UiName name) => Get(name) != null;

        public IEnumerator AwaitHidden(UiName name)
        {
            UIFrameWindow target = Get(name);
            if (target == null || !IsInStack(target)) yield break;

            bool hidden = false;
            void HandleHidden(UiName hiddenName, UIFrameWindow _) =>
                hidden |= hiddenName == name;
            Events.WindowHidden += HandleHidden;
            try
            {
                while (!hidden && IsInStack(target)) yield return null;
            }
            finally
            {
                Events.WindowHidden -= HandleHidden;
            }
        }

        public void HideAll()
        {
            UiName[] names = new UiName[_cache.Count];
            _cache.Keys.CopyTo(names, 0);
            foreach (UiName name in names) Hide(name);
        }

        public void HideAllExcept(IReadOnlyCollection<UiName> exceptions)
        {
            UiName[] names = new UiName[_cache.Count];
            _cache.Keys.CopyTo(names, 0);
            foreach (UiName name in names)
            {
                if (Contains(exceptions, name)) continue;
                Hide(name);
            }
        }

        public UIFrameWindow WarmPool(UiName name)
        {
            UIFrameWindow window = GetOrCreate(name);
            if (window != null && window.WindowState == UiWindowState.Hidden)
                window.gameObject.SetActive(false);
            return window;
        }

        public IEnumerator WarmPoolAsync(UiName name)
        {
            if (Has(name) || _loading.Contains(name)) yield break;
            _loading.Add(name);
            // Serialized prefab references are already loaded. Yielding once
            // preserves one-flight/prewarm ordering without pretending that
            // Addressables exists in this project.
            yield return null;
            WarmPool(name);
            _loading.Remove(name);
        }

        public IEnumerator ShowAsync(
            UiName name,
            IReadOnlyDictionary<string, object> parameters = null,
            Action<UIFrameWindow> completed = null)
        {
            if (!Has(name))
            {
                if (_loading.Contains(name))
                {
                    while (_loading.Contains(name)) yield return null;
                }
                else
                {
                    yield return WarmPoolAsync(name);
                }
            }

            completed?.Invoke(Show(name, parameters));
        }

        public bool RequestBack()
        {
            List<UIFrameWindow> ordered = OrderedWindows();
            for (int index = ordered.Count - 1; index >= 0; index--)
            {
                UIFrameWindow window = ordered[index];
                if (window == null || window.IsOccluded ||
                    window.WindowState != UiWindowState.Showing)
                    continue;
                return window.Escape();
            }

            return false;
        }

        public int GetWindowCount(UiLayer layer)
        {
            return _stacks.TryGetValue(layer, out List<UIFrameWindow> stack)
                ? stack.Count
                : 0;
        }

        public void SetLayerVisible(UiLayer layer, bool visible)
        {
            if (!_stacks.TryGetValue(layer, out List<UIFrameWindow> stack)) return;
            foreach (UIFrameWindow window in stack)
            {
                if (window != null) window.SetOccluded(!visible);
            }
        }

        public void Evict(UiName name)
        {
            if (!_cache.TryGetValue(name, out UIFrameWindow window)) return;
            if (window != null && window.WindowState != UiWindowState.Hidden) return;
            _cache.Remove(name);
            _sourcePrefabs.Remove(name);
            if (window == null) return;
            window.DestroyLifecycle();
            Destroy(window.gameObject);
        }

        internal void NotifyButtonHeld(int guardId)
        {
            _heldButtonGenerations.TryGetValue(guardId, out int generation);
            _heldButtonGenerations[guardId] = generation + 1;
        }

        internal void NotifyButtonReleased(int guardId)
        {
            if (!_heldButtonGenerations.TryGetValue(guardId, out int generation))
                return;
            StartCoroutine(ReleaseButtonAtEndOfFrame(guardId, generation));
        }

        internal void NotifyDialogClosing(UIFrameWindow window)
        {
            if (window == null || trackingRuntime == null) return;
            string dialog = window.GetTrackingDialogName();
            if (!string.IsNullOrEmpty(dialog))
                trackingRuntime.Tracker.NotifyDialogClosed(dialog);
        }

        internal void ConfigureForTests(
            UIRegistry testRegistry,
            RectTransform testWindowRoot,
            Canvas testMaskCanvas = null,
            CanvasGroup testMaskGroup = null,
            Graphic testInputBlocker = null,
            Canvas testInputBlockerCanvas = null,
            TrackingRuntime testTrackingRuntime = null)
        {
            registry = testRegistry;
            windowRoot = testWindowRoot;
            maskCanvas = testMaskCanvas;
            maskGroup = testMaskGroup;
            inputBlocker = testInputBlocker;
            inputBlockerCanvas = testInputBlockerCanvas;
            trackingRuntime = testTrackingRuntime;
            ResetTrackerObserver();
            SetMaskVisible(false, 0f, 0);
            SetInputGuard(false);
        }

        private UIFrameWindow GetOrCreate(UiName name)
        {
            if (registry == null || !registry.TryGetPrefab(name, out UIFrameWindow prefab))
                return null;

            if (_cache.TryGetValue(name, out UIFrameWindow cached) && cached != null)
            {
                if (_sourcePrefabs.TryGetValue(name, out UIFrameWindow source) &&
                    source == prefab || cached.WindowState != UiWindowState.Hidden)
                {
                    cached.transform.SetAsLastSibling();
                    return cached;
                }

                Evict(name);
            }

            UIFrameWindow window = Instantiate(prefab, windowRoot);
            window.name = prefab.name;
            _cache[name] = window;
            _sourcePrefabs[name] = prefab;
            if (window is IClockTickConsumer clockConsumer)
                clockConsumer.BindClockTicker(clockTicker);
            if (window is IDailyMetaConsumer metaConsumer)
                metaConsumer.BindDailyMetaRuntime(dailyMetaRuntime);
            if (window is IProfileConsumer profileConsumer)
                profileConsumer.BindProfileRuntime(profileRuntime);
            if (window is IRankActivityConsumer rankConsumer)
                rankConsumer.BindRankActivityRuntime(rankActivityRuntime);
            if (window is IAdServiceConsumer adConsumer)
                adConsumer.BindAdService(adRuntime != null
                    ? adRuntime.Service
                    : null);
            if (window is IAbConfigRuntimeConsumer abConsumer)
                abConsumer.BindAbConfigRuntime(abConfigRuntime);
            window.InitializeFrame(this, name);
            Events.RaiseCreated(name, window);
            return window;
        }

        private IEnumerator HideRoutine(UiName name, UIFrameWindow window)
        {
            yield return window.PlayCloseLifecycle();
            _closing.Remove(name);
            if (window == null || window.WindowState != UiWindowState.Hidden)
                yield break;

            if (window.ShowMask)
                _maskReferenceCount = Mathf.Max(0, _maskReferenceCount - 1);
            PopStack(window.Layer, window);
            Events.RaiseHidden(name, window);
            RefreshOcclusion();
            UpdateMask();
        }

        private bool TryBeginClosing(
            UiName name,
            out UIFrameWindow window)
        {
            if (!_cache.TryGetValue(name, out window) ||
                window == null || !window.IsShowing)
                return false;

            window.BeginClosingLifecycle();
            window.SetOccluded(false);
            return true;
        }

        private void PushStack(UiLayer layer, UIFrameWindow window)
        {
            if (!_stacks.TryGetValue(layer, out List<UIFrameWindow> stack))
            {
                stack = new List<UIFrameWindow>();
                _stacks.Add(layer, stack);
            }
            stack.Remove(window);
            stack.Add(window);
        }

        private void PopStack(UiLayer layer, UIFrameWindow window)
        {
            if (_stacks.TryGetValue(layer, out List<UIFrameWindow> stack))
                stack.Remove(window);
        }

        private bool IsInStack(UIFrameWindow window)
        {
            return window != null &&
                   _stacks.TryGetValue(window.Layer, out List<UIFrameWindow> stack) &&
                   stack.Contains(window);
        }

        private void AssignSortingOrder(UIFrameWindow window)
        {
            UiLayer layer = window.Layer;
            if (!_nextZ.TryGetValue(layer, out int next)) next = (int)layer;
            if (next >= (int)layer + UiLayerConfig.ZMax)
            {
                CompactSortingOrders(layer);
                next = _nextZ[layer];
            }
            window.SetSortingOrder(next);
            _nextZ[layer] = next + UiLayerConfig.ZStep;
        }

        private void CompactSortingOrders(UiLayer layer)
        {
            int next = (int)layer;
            if (_stacks.TryGetValue(layer, out List<UIFrameWindow> stack))
            {
                stack.Sort((left, right) =>
                    left.SortingOrder.CompareTo(right.SortingOrder));
                foreach (UIFrameWindow window in stack)
                {
                    if (window == null) continue;
                    window.SetSortingOrder(next);
                    next += UiLayerConfig.ZStep;
                }
            }
            _nextZ[layer] = next;
        }

        private List<UIFrameWindow> OrderedWindows()
        {
            var layers = new List<UiLayer>(_stacks.Keys);
            layers.Sort();
            var ordered = new List<UIFrameWindow>();
            foreach (UiLayer layer in layers)
            {
                List<UIFrameWindow> stack = _stacks[layer];
                foreach (UIFrameWindow window in stack)
                {
                    if (window != null) ordered.Add(window);
                }
            }
            return ordered;
        }

        private void RefreshOcclusion()
        {
            List<UIFrameWindow> ordered = OrderedWindows();
            bool occluded = false;
            for (int index = ordered.Count - 1; index >= 0; index--)
            {
                UIFrameWindow window = ordered[index];
                if (window == null) continue;
                window.SetOccluded(occluded);
                if (!occluded && window.IsFullscreen) occluded = true;
            }
        }

        private void ReorderWindowTree()
        {
            foreach (UIFrameWindow window in OrderedWindows())
            {
                if (window != null && window.transform.parent == windowRoot)
                    window.transform.SetAsLastSibling();
            }
        }

        private void UpdateMask()
        {
            UIFrameWindow top = null;
            foreach (UIFrameWindow window in OrderedWindows())
            {
                if (window == null || !window.ShowMask ||
                    window.WindowState == UiWindowState.Hidden)
                    continue;
                if (top == null || window.SortingOrder > top.SortingOrder)
                    top = window;
            }

            if (top == null || _maskReferenceCount <= 0)
            {
                SetMaskVisible(false, 0f, 0);
                return;
            }
            SetMaskVisible(true, top.MaskOpacity, top.SortingOrder - 1);
        }

        private void SetMaskVisible(bool visible, float opacity, int order)
        {
            if (maskCanvas != null)
            {
                maskCanvas.overrideSorting = true;
                maskCanvas.sortingOrder = order;
                maskCanvas.gameObject.SetActive(visible);
            }
            if (maskGroup != null)
            {
                maskGroup.alpha = visible ? opacity : 0f;
                maskGroup.blocksRaycasts = visible;
                maskGroup.interactable = visible;
            }
        }

        private IEnumerator ReleaseButtonAtEndOfFrame(int guardId, int generation)
        {
            yield return new WaitForEndOfFrame();
            if (_heldButtonGenerations.TryGetValue(guardId, out int current) &&
                current == generation)
                _heldButtonGenerations.Remove(guardId);
            if (_heldButtonGenerations.Count == 0 && _guardActive)
            {
                _guardActive = false;
                SetInputGuard(false);
            }
        }

        private void SetInputGuard(bool active)
        {
            if (inputBlockerCanvas != null)
            {
                inputBlockerCanvas.overrideSorting = true;
                inputBlockerCanvas.sortingOrder = (int)UiLayer.Loading +
                                                  UiLayerConfig.ZMax + 1;
            }
            if (inputBlocker != null)
            {
                inputBlocker.raycastTarget = active;
                inputBlocker.gameObject.SetActive(active);
            }
        }

        private void OnDestroy()
        {
            _trackerObserver?.Dispose();
            _trackerObserver = null;
            foreach (UIFrameWindow window in _cache.Values)
            {
                if (window != null) window.DestroyLifecycle();
            }
            _cache.Clear();
            _sourcePrefabs.Clear();
            _stacks.Clear();
            _closing.Clear();
            _loading.Clear();
            _heldButtonGenerations.Clear();
            Events.Clear();
        }

        private void ResetTrackerObserver()
        {
            _trackerObserver?.Dispose();
            _trackerObserver = new UITrackerObserver(Events, trackingRuntime);
        }

        private static bool Contains(
            IReadOnlyCollection<UiName> collection,
            UiName value)
        {
            if (collection == null) return false;
            foreach (UiName item in collection)
            {
                if (item == value) return true;
            }
            return false;
        }
    }
}
