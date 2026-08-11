using System;
using DG.Tweening;
using Meowdoku.Core.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class LanguageSwitchWidget : MonoBehaviour,
        IPointerDownHandler
    {
        public const float OpenSeconds = 0.1f;
        public const float FadeStepSeconds = 0.033333335f;
        public const float PanelOpenHeight = 508f;

        [SerializeField] private Graphic outsideBlocker;
        [SerializeField] private Button rowButton;
        [SerializeField] private RectTransform arrow;
        [SerializeField] private GameObject dropdown;
        [SerializeField] private RectTransform panelBackground;
        [SerializeField] private RectTransform panelShadow;
        [SerializeField] private CanvasGroup shadowGroup;
        [SerializeField] private Button systemOption;
        [SerializeField] private CanvasGroup systemGroup;
        [SerializeField] private Text systemLabel;
        [SerializeField] private Font primaryFont;
        [SerializeField] private Font eastAsianFallbackFont;
        [SerializeField] private Button englishOption;
        [SerializeField] private CanvasGroup englishGroup;

        private Sequence _openTween;
        private string _systemLocale = string.Empty;

        public event Action<string> LanguagePicked;
        public event Action DropdownOpened;
        public event Action DropdownClosed;

        public bool IsOpen => dropdown != null && dropdown.activeSelf;

        private void Awake()
        {
            AddListener(rowButton, Toggle);
            AddListener(systemOption, PickSystem);
            AddListener(englishOption, PickEnglish);
            SetOpen(false, false);
        }

        private void OnDisable()
        {
            SetOpen(false, false);
        }

        private void OnDestroy()
        {
            KillTween();
            RemoveListener(rowButton, Toggle);
            RemoveListener(systemOption, PickSystem);
            RemoveListener(englishOption, PickEnglish);
            LanguagePicked = null;
            DropdownOpened = null;
            DropdownClosed = null;
        }

        public void Setup(string systemLocale)
        {
            _systemLocale = LocalizationLocaleContract.MainLanguage(systemLocale) ==
                            "zh"
                ? LocalizationLocaleContract.CanonicalizeChinese(systemLocale)
                : LocalizationLocaleContract.NormalizeLocale(systemLocale);
            if (systemLabel != null)
            {
                systemLabel.text = LanguageSelectionContract.NativeNameOf(
                    _systemLocale);
                systemLabel.font = eastAsianFallbackFont != null &&
                                   LocalizationLocaleContract
                                       .UsesEastAsianFallback(_systemLocale)
                    ? eastAsianFallbackFont
                    : primaryFont;
            }
            SetOpen(false, false);
        }

        public void ForceClose()
        {
            SetOpen(false, true);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsOpen || outsideBlocker == null || eventData == null)
                return;
            GameObject target = eventData.pointerCurrentRaycast.gameObject;
            if (target == null) return;
            Transform blocker = outsideBlocker.transform;
            if (target == outsideBlocker.gameObject ||
                target.transform.IsChildOf(blocker))
                ForceClose();
        }

        private void Toggle()
        {
            SetOpen(!IsOpen, true);
        }

        private void PickSystem()
        {
            string value = _systemLocale;
            SetOpen(false, true);
            if (!string.IsNullOrEmpty(value)) LanguagePicked?.Invoke(value);
        }

        private void PickEnglish()
        {
            SetOpen(false, true);
            LanguagePicked?.Invoke("en");
        }

        private void SetOpen(bool open, bool notify)
        {
            bool wasOpen = IsOpen;
            if (wasOpen == open)
            {
                if (!open) ResetVisuals();
                return;
            }

            KillTween();
            if (open)
            {
                if (outsideBlocker != null)
                    outsideBlocker.gameObject.SetActive(true);
                dropdown.SetActive(true);
                SetHeight(panelBackground, 0f);
                SetHeight(panelShadow, 0f);
                SetAlpha(shadowGroup, 0f);
                SetAlpha(systemGroup, 0f);
                SetAlpha(englishGroup, 0f);
                if (arrow != null)
                    arrow.localEulerAngles = new Vector3(0f, 0f, -90f);

                _openTween = DOTween.Sequence().SetLink(gameObject);
                if (panelBackground != null)
                    _openTween.Insert(0f, panelBackground
                        .DOSizeDelta(
                            new Vector2(
                                panelBackground.sizeDelta.x,
                                PanelOpenHeight),
                            OpenSeconds)
                        .SetEase(Ease.Linear));
                if (panelShadow != null)
                    _openTween.Insert(0f, panelShadow
                        .DOSizeDelta(
                            new Vector2(
                                panelShadow.sizeDelta.x,
                                PanelOpenHeight),
                            OpenSeconds)
                        .SetEase(Ease.Linear));
                Fade(
                    _openTween, shadowGroup,
                    FadeStepSeconds, FadeStepSeconds);
                Fade(
                    _openTween, systemGroup,
                    FadeStepSeconds, FadeStepSeconds);
                Fade(
                    _openTween, englishGroup,
                    FadeStepSeconds * 2f, FadeStepSeconds);
                _openTween.OnComplete(() => _openTween = null);
                if (notify) DropdownOpened?.Invoke();
                return;
            }

            ResetVisuals();
            if (notify) DropdownClosed?.Invoke();
        }

        private void ResetVisuals()
        {
            KillTween();
            if (dropdown != null) dropdown.SetActive(false);
            if (outsideBlocker != null)
                outsideBlocker.gameObject.SetActive(false);
            SetHeight(panelBackground, 0f);
            SetHeight(panelShadow, 0f);
            SetAlpha(shadowGroup, 0f);
            SetAlpha(systemGroup, 0f);
            SetAlpha(englishGroup, 0f);
            if (arrow != null)
                arrow.localEulerAngles = new Vector3(0f, 0f, 90f);
        }

        private void KillTween()
        {
            _openTween?.Kill(false);
            _openTween = null;
        }

        private static void Fade(
            Sequence sequence,
            CanvasGroup group,
            float start,
            float duration)
        {
            if (sequence != null && group != null)
                sequence.Insert(start,
                    group.DOFade(1f, duration).SetEase(Ease.Linear));
        }

        private static void SetHeight(RectTransform target, float height)
        {
            if (target != null)
                target.sizeDelta = new Vector2(target.sizeDelta.x, height);
        }

        private static void SetAlpha(CanvasGroup target, float alpha)
        {
            if (target != null) target.alpha = alpha;
        }

        private static void AddListener(
            Button button,
            UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.AddListener(action);
        }

        private static void RemoveListener(
            Button button,
            UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.RemoveListener(action);
        }
    }
}
