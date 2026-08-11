using System.Collections;
using System.Collections.Generic;
using Meowdoku.Core.Tracking;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Core.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class UIFrameWindow : UIBaseWindow
    {
        [Header("Source UIFrameWindow contract")]
        [SerializeField] private UiLayer uiLayer = UiLayer.Default;
        [SerializeField] private bool isFullscreen;
        [SerializeField] private bool showMask;
        [SerializeField, Range(0f, 1f)] private float maskOpacity = 0.8f;
        [SerializeField] private bool playOpenSound;

        [Header("UGUI adapter")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private CanvasGroup rootCanvasGroup;
        [SerializeField] private Button closeButton;

        private UIManager _owner;
        private bool _occluded;

        protected UIManager Owner => _owner;
        protected TrackerService Tracking => _owner?.Tracker;

        public UiName UiName { get; private set; }
        public UiLayer Layer => uiLayer;
        public bool IsFullscreen => isFullscreen;
        public bool ShowMask => showMask;
        public float MaskOpacity => maskOpacity;
        public bool PlayOpenSound => playOpenSound;
        public bool IsOccluded => _occluded;
        public int SortingOrder => rootCanvas != null
            ? rootCanvas.sortingOrder
            : (int)uiLayer;

        // Mirrors UIFrameWindow.get_scr_name/get_dlg_name/get_dlg_extra.
        // Concrete pages override only the metadata that exists in Godot.
        public virtual string GetTrackingScreenName() => string.Empty;
        public virtual string GetTrackingDialogName() => string.Empty;
        public virtual IReadOnlyDictionary<string, object>
            GetTrackingDialogExtra() => null;

        internal void InitializeFrame(UIManager owner, UiName uiName)
        {
            _owner = owner;
            UiName = uiName;
            EnsureCanvasComponents();
            ResolveCloseButton();
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseFromButton);
            CreateLifecycle(owner);
        }

        internal void SetSortingOrder(int order)
        {
            EnsureCanvasComponents();
            rootCanvas.overrideSorting = true;
            rootCanvas.sortingOrder = order;
        }

        internal void SetOccluded(bool occluded)
        {
            if (_occluded == occluded) return;
            _occluded = occluded;
            EnsureCanvasComponents();
            rootCanvasGroup.alpha = occluded ? 0f : 1f;
            rootCanvasGroup.interactable = !occluded;
            rootCanvasGroup.blocksRaycasts = !occluded;
            if (occluded) OnStackBottom();
            else OnStackTop();
        }

        internal IEnumerator PlayCloseLifecycle()
        {
            IEnumerator animation = PlayCloseAnimation();
            if (animation != null) yield return animation;
            if (WindowState != UiWindowState.Closing) yield break;
            _owner?.NotifyDialogClosing(this);
            yield return HideLifecycle();
        }

        internal bool Escape()
        {
            if (closeButton != null && closeButton.isActiveAndEnabled &&
                closeButton.interactable)
            {
                closeButton.onClick.Invoke();
                return true;
            }

            return OnBackRequest();
        }

        protected virtual void OnStackTop() { }
        protected virtual void OnStackBottom() { }
        protected virtual bool OnBackRequest() => false;
        protected virtual void OnCloseButtonPressed() { }

        protected virtual IEnumerator PlayCloseAnimation()
        {
            yield break;
        }

        protected override void OnDestroyWindow()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(CloseFromButton);
            base.OnDestroyWindow();
        }

        private void CloseFromButton()
        {
            OnCloseButtonPressed();
            if (_owner != null) _owner.Hide(UiName);
        }

        private void EnsureCanvasComponents()
        {
            if (rootCanvas == null)
                rootCanvas = GetComponent<Canvas>();
            if (rootCanvas == null)
                rootCanvas = gameObject.AddComponent<Canvas>();
            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();
            if (rootCanvasGroup == null)
                rootCanvasGroup = GetComponent<CanvasGroup>();
            if (rootCanvasGroup == null)
                rootCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private void ResolveCloseButton()
        {
            if (closeButton != null) return;
            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                if (button.name != "CloseBtn") continue;
                closeButton = button;
                return;
            }
        }

        internal void ConfigureForTests(
            UiLayer layer,
            bool fullscreen,
            bool masked,
            float opacity = 0.8f)
        {
            uiLayer = layer;
            isFullscreen = fullscreen;
            showMask = masked;
            maskOpacity = opacity;
        }
    }
}
