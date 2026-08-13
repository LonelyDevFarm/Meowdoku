using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    public enum ToolButtonVisualState
    {
        NoTool,
        HasTool,
        Free
    }

    /// <summary>
    /// UGUI port of tool_button.gd. Resource policy remains in
    /// ToolResourceCoordinator; this component owns only presentation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ToolButtonView : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerExitHandler
    {
        private const float PressScale = 0.9f;
        private const float ReleaseOvershootScale = 1.03f;
        private const float PressSeconds = 0.0667f;
        private const float ReleaseOvershootSeconds = 0.167f;
        private const float ReleaseRecoverSeconds = 0.2f;

        [SerializeField] private Button hitButton;
        [SerializeField] private RectTransform visualRoot;
        [SerializeField] private RectTransform icon;
        [SerializeField] private Image toolLight;
        [SerializeField] private GameObject badgeRoot;
        [SerializeField] private GameObject countBadgeRoot;
        [SerializeField] private Text countLabel;
        [SerializeField] private GameObject actionBadgeRoot;
        [SerializeField] private GameObject plusIcon;
        [SerializeField] private Text freeLabel;
        [SerializeField] private bool showBadge = true;

        private Sequence _pressTween;
        private Sequence _idleTween;
        private Sequence _obtainTween;
        private Vector3 _baseScale = Vector3.one;
        private ToolButtonVisualState _state;
        private int _badgeCount;

        public event Action Pressed;

        public ToolButtonVisualState State => _state;
        public int BadgeCount => _badgeCount;
        public bool IsIdlePulsePlaying => _idleTween != null && _idleTween.IsActive();

        private void Awake()
        {
            if (visualRoot == null) visualRoot = transform as RectTransform;
            if (icon == null) icon = visualRoot;
            _baseScale = transform.localScale;
            if (hitButton != null) hitButton.onClick.AddListener(HandlePressed);
            Refresh();
        }

        private void OnDisable()
        {
            StopIdlePulse();
            Kill(ref _pressTween);
            Kill(ref _obtainTween);
            transform.localScale = _baseScale;
        }

        private void OnDestroy()
        {
            if (hitButton != null) hitButton.onClick.RemoveListener(HandlePressed);
            StopIdlePulse();
            Kill(ref _pressTween);
            Kill(ref _obtainTween);
        }

        public void SetState(ToolButtonVisualState state, int count)
        {
            _state = state;
            _badgeCount = Mathf.Max(0, count);
            Refresh();
        }

        public bool PlayIdlePulse()
        {
            if (!isActiveAndEnabled || visualRoot == null || icon == null)
                return false;
            StopIdlePulse();
            if (toolLight != null)
            {
                toolLight.gameObject.SetActive(true);
                toolLight.color = new Color(1f, 1f, 1f, 0f);
            }
            visualRoot.localScale = Vector3.one;
            icon.localScale = Vector3.one;
            icon.localRotation = Quaternion.identity;

            _idleTween = DOTween.Sequence().SetUpdate(true).SetLoops(-1);
            _idleTween.Insert(0f,
                visualRoot.DOScale(1.1f, 0.53333336f).SetEase(Ease.OutCubic));
            _idleTween.Insert(0.53333336f,
                visualRoot.DOScale(1f, 0.53333336f).SetEase(Ease.InOutCubic));
            _idleTween.Insert(0.2f,
                icon.DOScale(1.15f, 0.7f).SetEase(Ease.OutCubic));
            _idleTween.Insert(0.9f,
                icon.DOScale(1f, 0.1666667f).SetEase(Ease.InOutCubic));
            _idleTween.Insert(0.2f,
                icon.DORotate(new Vector3(0f, 0f, -15f), 0.1f));
            _idleTween.Insert(0.3f,
                icon.DORotate(new Vector3(0f, 0f, 15f), 0.16666667f));
            _idleTween.Insert(0.46666667f,
                icon.DORotate(new Vector3(0f, 0f, -15f), 0.16666666f));
            _idleTween.Insert(0.6333333f,
                icon.DORotate(new Vector3(0f, 0f, 15f), 0.1666667f));
            _idleTween.Insert(0.8f,
                icon.DORotate(Vector3.zero, 0.1f));
            if (toolLight != null)
            {
                _idleTween.Insert(0f,
                    toolLight.DOFade(1f, 0.53333336f).SetEase(Ease.OutQuad));
                _idleTween.Insert(0.53333336f,
                    toolLight.DOFade(0f, 0.53333336f).SetEase(Ease.InQuad));
            }
            _idleTween.AppendInterval(0.4333333f);
            return true;
        }

        public void StopIdlePulse()
        {
            Kill(ref _idleTween);
            if (visualRoot != null) visualRoot.localScale = Vector3.one;
            if (icon != null)
            {
                icon.localScale = Vector3.one;
                icon.localRotation = Quaternion.identity;
            }
            if (toolLight != null)
            {
                toolLight.color = new Color(1f, 1f, 1f, 0f);
                toolLight.gameObject.SetActive(false);
            }
        }

        public void PlayObtain()
        {
            if (!isActiveAndEnabled || visualRoot == null) return;
            Kill(ref _obtainTween);
            visualRoot.localScale = Vector3.one;
            _obtainTween = DOTween.Sequence().SetUpdate(true)
                .Append(visualRoot.DOScale(1.2f, 0.21666668f)
                    .SetEase(Ease.OutCubic))
                .Append(visualRoot.DOScale(1f, 0.15f)
                    .SetEase(Ease.InOutCubic));
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (hitButton == null || !hitButton.IsInteractable()) return;
            Kill(ref _pressTween);
            _pressTween = DOTween.Sequence().SetUpdate(true)
                .Append(transform.DOScale(_baseScale * PressScale, PressSeconds)
                    .SetEase(Ease.OutCubic));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            PlayReleaseScale();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (eventData.pointerPress == hitButton?.gameObject)
                PlayReleaseScale();
        }

        private void HandlePressed()
        {
            Pressed?.Invoke();
        }

        private void PlayReleaseScale()
        {
            if (!isActiveAndEnabled) return;
            Kill(ref _pressTween);
            _pressTween = DOTween.Sequence().SetUpdate(true)
                .Append(transform.DOScale(
                        _baseScale * ReleaseOvershootScale,
                        ReleaseOvershootSeconds)
                    .SetEase(Ease.OutCubic))
                .Append(transform.DOScale(_baseScale, ReleaseRecoverSeconds)
                    .SetEase(Ease.OutCubic));
        }

        private void Refresh()
        {
            if (badgeRoot != null) badgeRoot.SetActive(showBadge);
            if (!showBadge) return;
            bool hasTool = _state == ToolButtonVisualState.HasTool;
            bool free = _state == ToolButtonVisualState.Free;
            if (countBadgeRoot != null) countBadgeRoot.SetActive(hasTool);
            if (actionBadgeRoot != null) actionBadgeRoot.SetActive(!hasTool);
            if (plusIcon != null) plusIcon.SetActive(!hasTool && !free);
            if (freeLabel != null)
            {
                freeLabel.gameObject.SetActive(free);
                freeLabel.text = "Free";
            }
            if (countLabel != null)
                countLabel.text = _badgeCount > 99 ? "99+" : _badgeCount.ToString();
        }

        private static void Kill(ref Sequence sequence)
        {
            if (sequence != null && sequence.IsActive()) sequence.Kill();
            sequence = null;
        }
    }
}
