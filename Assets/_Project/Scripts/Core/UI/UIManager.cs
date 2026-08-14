using System;
using System.Collections;
using System.Collections.Generic;
using Meowdoku.Core.Ads;
using Meowdoku.Core.Config;
using Meowdoku.Core.Daily;
using Meowdoku.Core.Online;
using Meowdoku.Core.Platform;
using Meowdoku.Core.Profile;
using Meowdoku.Core.Rank;
using Meowdoku.Core.Tracking;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
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
        private const string TimedInputBlockerName = "_InputBlocker";

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
        [SerializeField] private DataSyncRuntime dataSyncRuntime;
        [SerializeField] private PrivacyPermissionRuntime platformRuntime;
        [SerializeField] private ProductServiceRuntime productServiceRuntime;

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
        private readonly Dictionary<int, TimedInputBlock> _timedInputBlocks =
            new();

        private int _maskReferenceCount;
        private bool _guardActive;
        private Coroutine _maskFade;
        private UITrackerObserver _trackerObserver;
        private ISettingsExternalServices _settingsExternalServices =
            OfflineSettingsExternalServices.Instance;
        private bool _backInputBound;
        private bool _keyboardBackPressed;
        private bool _gamepadBackPressed;
        private int _lastBackInputFrame = -1;

        public UIEvents Events { get; } = new();
        public bool IsAnyLoading => _loading.Count > 0;
        public int CachedWindowCount => _cache.Count;
        public int MaskReferenceCount => _maskReferenceCount;
        public bool IsInputGuardActive => _guardActive;
#if UNITY_INCLUDE_TESTS
        internal bool IsBackInputEnabledForTests => _backInputBound;
        internal int BackInputEventCountForTests { get; private set; }
        internal int BackInputPressCountForTests { get; private set; }
        internal bool LastBackRequestHandledForTests { get; private set; }
#endif

        /// <summary>
        /// Source-equivalent early mask fade used while a reward visual flies
        /// into an already-visible Home page. Ownership/ref-count is unchanged;
        /// a later stack update restores the correct mask for the new top page.
        /// </summary>
        public void FadeOutMaskEarly(float durationSeconds)
        {
            StopMaskFade();
            if (maskGroup == null) return;
            if (durationSeconds <= 0f)
            {
                maskGroup.alpha = 0f;
                return;
            }
            _maskFade = StartCoroutine(FadeMaskRoutine(durationSeconds));
        }

        private sealed class TimedInputBlock
        {
            public GameObject Blocker;
            public Coroutine Lifetime;
        }

        private void Awake()
        {
            if (windowRoot == null) windowRoot = transform as RectTransform;
            // RectTransforms created by editor installers can be serialized at
            // zero scale while they are not part of an active Canvas. A zero
            // scale parent keeps the complete runtime UI invisible even though
            // window lifecycle and navigation continue to work normally.
            EnsureRenderableScale(transform);
            EnsureRenderableScale(windowRoot);
            EnsureRenderableScale(maskCanvas != null
                ? maskCanvas.transform
                : null);
            EnsureRenderableScale(inputBlockerCanvas != null
                ? inputBlockerCanvas.transform
                : null);
            dailyMetaRuntime?.BindAbConfigRuntime(abConfigRuntime);
            rankActivityRuntime?.BindAbConfigRuntime(abConfigRuntime);
            ResetTrackerObserver();
            SetMaskVisible(false, 0f, 0);
            SetInputGuard(false);
        }

        private void OnEnable()
        {
            BindBackInput();
        }

        private void OnDisable()
        {
            UnbindBackInput();
        }

        internal TrackerService Tracker => trackingRuntime != null
            ? trackingRuntime.Tracker
            : null;

        public void BindSettingsExternalServices(
            ISettingsExternalServices services)
        {
            _settingsExternalServices = services ??
                OfflineSettingsExternalServices.Instance;
            foreach (UIFrameWindow window in _cache.Values)
            {
                if (window is ISettingsExternalServicesConsumer consumer)
                    consumer.BindSettingsExternalServices(
                        _settingsExternalServices);
            }
        }

        public void BindProductServiceRuntime(ProductServiceRuntime runtime)
        {
            productServiceRuntime = runtime;
            foreach (UIFrameWindow window in _cache.Values)
            {
                if (window is IProductServiceRuntimeConsumer consumer)
                    consumer.BindProductServiceRuntime(productServiceRuntime);
            }
        }

        public UIFrameWindow Show(
            UiName name,
            IReadOnlyDictionary<string, object> parameters = null)
        {
            UIFrameWindow window = GetOrCreate(name);
            if (window == null) return null;

            parameters ??= EmptyParameters;
            if (window.IsShowing)
            {
                AssignSortingOrder(window);
                int reassignedSortingOrder = window.SortingOrder;
                PushStack(window.Layer, window);
                window.ShowLifecycle(parameters);
                window.SetSortingOrder(reassignedSortingOrder);
                window.SetOccluded(false);
                window.transform.SetAsLastSibling();
                RefreshOcclusion();
                UpdateMask();
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
            int assignedSortingOrder = window.SortingOrder;
            PushStack(window.Layer, window);
            // Unity 6 can resync an inactive nested Canvas from prefab state on
            // SetActive(true), so reapply the reserved order after activation.
            window.ShowLifecycle(parameters);
            window.SetSortingOrder(assignedSortingOrder);
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

        /// <summary>
        /// Port of Godot UIManager.block_input_briefly. The transparent local
        /// canvas consumes pointer input without changing the enabled or tint
        /// state of the target window's buttons.
        /// </summary>
        public void BlockInputBriefly(
            RectTransform target,
            float durationSeconds = 1.5f)
        {
            if (target == null || durationSeconds <= 0f) return;

            int targetId = target.GetInstanceID();
            if (_timedInputBlocks.TryGetValue(
                    targetId,
                    out TimedInputBlock existing))
            {
                if (existing.Lifetime != null)
                    StopCoroutine(existing.Lifetime);
                if (existing.Blocker != null)
                    Destroy(existing.Blocker);
                _timedInputBlocks.Remove(targetId);
            }

            var blocker = new GameObject(
                TimedInputBlockerName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster),
                typeof(Image));
            blocker.layer = target.gameObject.layer;
            RectTransform blockerRect =
                blocker.GetComponent<RectTransform>();
            blockerRect.SetParent(target, false);
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;
            blockerRect.SetAsLastSibling();

            Canvas blockerCanvas = blocker.GetComponent<Canvas>();
            blockerCanvas.overrideSorting = true;
            Canvas targetCanvas = target.GetComponent<Canvas>();
            if (targetCanvas == null)
                targetCanvas = target.GetComponentInParent<Canvas>();
            blockerCanvas.sortingOrder =
                (targetCanvas != null ? targetCanvas.sortingOrder : 0) +
                UiLayerConfig.LocalOverlayOffset;
            Image blockerImage = blocker.GetComponent<Image>();
            blockerImage.color = Color.clear;
            blockerImage.raycastTarget = true;

            var entry = new TimedInputBlock { Blocker = blocker };
            _timedInputBlocks.Add(targetId, entry);
            entry.Lifetime = StartCoroutine(RemoveTimedInputBlock(
                targetId,
                blocker,
                durationSeconds));
        }

        internal bool IsInputBrieflyBlocked(RectTransform target)
        {
            if (target == null) return false;
            return _timedInputBlocks.TryGetValue(
                       target.GetInstanceID(),
                       out TimedInputBlock entry) &&
                   entry.Blocker != null &&
                   entry.Blocker.activeSelf;
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
            StartCoroutine(ReleaseButtonOnNextFrame(guardId, generation));
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
            NormalizeWindowRect(window.transform);
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
            if (window is IDataSyncConsumer dataSyncConsumer)
                dataSyncConsumer.BindDataSyncRuntime(dataSyncRuntime);
            if (window is IPlatformPermissionRuntimeConsumer platformConsumer)
                platformConsumer.BindPlatformPermissionRuntime(platformRuntime);
            if (window is IProductServiceRuntimeConsumer productConsumer)
                productConsumer.BindProductServiceRuntime(productServiceRuntime);
            if (window is ISettingsExternalServicesConsumer settingsConsumer)
                settingsConsumer.BindSettingsExternalServices(
                    _settingsExternalServices);
            window.InitializeFrame(this, name);
            Events.RaiseCreated(name, window);
            return window;
        }

        private void EnsureRenderableScale(Transform node)
        {
            while (node != null)
            {
                node.localScale = Vector3.one;
                if (node == transform) break;
                node = node.parent;
            }
        }

        private static void NormalizeWindowRect(Transform window)
        {
            window.localPosition = Vector3.zero;
            window.localRotation = Quaternion.identity;
            window.localScale = Vector3.one;
            if (window is not RectTransform rect) return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
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
            int layerBase = UiLayerConfig.SortingBase(layer);
            if (!_nextZ.TryGetValue(layer, out int next)) next = layerBase;
            if (next >= layerBase + UiLayerConfig.ZMax)
            {
                CompactSortingOrders(layer);
                next = _nextZ[layer];
            }
            window.SetSortingOrder(next);
            _nextZ[layer] = next + UiLayerConfig.ZStep;
        }

        private void CompactSortingOrders(UiLayer layer)
        {
            int next = UiLayerConfig.SortingBase(layer);
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
            StopMaskFade();
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

        private IEnumerator ReleaseButtonOnNextFrame(int guardId, int generation)
        {
            // Keep the release owned by the source window for the remainder of
            // this frame. Unlike WaitForEndOfFrame, a normal frame yield cannot
            // remain suspended when the Unity Editor switches away from Game
            // view while a navigation transition is running.
            yield return null;
            if (_heldButtonGenerations.TryGetValue(guardId, out int current) &&
                current == generation)
                _heldButtonGenerations.Remove(guardId);
            if (_heldButtonGenerations.Count == 0 && _guardActive)
            {
                _guardActive = false;
                SetInputGuard(false);
            }
        }

        private IEnumerator FadeMaskRoutine(float durationSeconds)
        {
            float from = maskGroup != null ? maskGroup.alpha : 0f;
            float elapsed = 0f;
            while (maskGroup != null && elapsed < durationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                maskGroup.alpha = Mathf.Lerp(
                    from,
                    0f,
                    Mathf.Clamp01(elapsed / durationSeconds));
                yield return null;
            }
            if (maskGroup != null) maskGroup.alpha = 0f;
            _maskFade = null;
        }

        private void StopMaskFade()
        {
            if (_maskFade != null) StopCoroutine(_maskFade);
            _maskFade = null;
        }

        private IEnumerator RemoveTimedInputBlock(
            int targetId,
            GameObject blocker,
            float durationSeconds)
        {
            yield return new WaitForSecondsRealtime(durationSeconds);
            if (!_timedInputBlocks.TryGetValue(
                    targetId,
                    out TimedInputBlock entry) ||
                entry.Blocker != blocker)
                yield break;

            _timedInputBlocks.Remove(targetId);
            if (blocker != null) Destroy(blocker);
        }

        private void SetInputGuard(bool active)
        {
            if (inputBlockerCanvas != null)
            {
                inputBlockerCanvas.overrideSorting = true;
                inputBlockerCanvas.sortingOrder =
                    UiLayerConfig.SortingBase(UiLayer.Loading) +
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
            UnbindBackInput();
            StopMaskFade();
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
            foreach (TimedInputBlock entry in _timedInputBlocks.Values)
            {
                if (entry.Blocker != null) Destroy(entry.Blocker);
            }
            _timedInputBlocks.Clear();
            Events.Clear();
        }

        private void BindBackInput()
        {
            if (_backInputBound) return;
            _keyboardBackPressed = Keyboard.current != null &&
                                   Keyboard.current.escapeKey.isPressed;
            _gamepadBackPressed = Gamepad.current != null &&
                                  Gamepad.current.buttonEast.isPressed;
            InputSystem.onEvent += HandleBackInputEvent;
            _backInputBound = true;
        }

        private void UnbindBackInput()
        {
            if (!_backInputBound) return;
            InputSystem.onEvent -= HandleBackInputEvent;
            _backInputBound = false;
            _keyboardBackPressed = false;
            _gamepadBackPressed = false;
            _lastBackInputFrame = -1;
        }

        private void HandleBackInputEvent(
            InputEventPtr eventPtr,
            InputDevice device)
        {
#if UNITY_INCLUDE_TESTS
            if (device is Keyboard || device is Gamepad)
                BackInputEventCountForTests++;
#endif
            if (device is Keyboard keyboard)
            {
                if (!keyboard.escapeKey.ReadValueFromEvent(
                        eventPtr,
                        out float value))
                    return;
                HandleBackButtonState(
                    value >= InputSystem.settings.defaultButtonPressPoint,
                    ref _keyboardBackPressed);
                return;
            }

            if (device is not Gamepad gamepad ||
                !gamepad.buttonEast.ReadValueFromEvent(
                    eventPtr,
                    out float gamepadValue))
                return;

            HandleBackButtonState(
                gamepadValue >= InputSystem.settings.defaultButtonPressPoint,
                ref _gamepadBackPressed);
        }

        private void HandleBackButtonState(bool pressed, ref bool wasPressed)
        {
            if (pressed == wasPressed) return;
            wasPressed = pressed;
            if (!pressed || _lastBackInputFrame == Time.frameCount) return;

            _lastBackInputFrame = Time.frameCount;
#if UNITY_INCLUDE_TESTS
            BackInputPressCountForTests++;
            LastBackRequestHandledForTests = RequestBack();
#else
            RequestBack();
#endif
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
