using System;
using Meowdoku.Core.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class LanguageOptionView : MonoBehaviour, IPointerDownHandler
    {
        private static readonly Color SelectedBackground =
            new(0.2784314f, 0.7019608f, 0.3419608f, 1f);
        private static readonly Color NormalBackground =
            new(0.9764706f, 0.9254902f, 0.88235295f, 1f);
        private static readonly Color SelectedText = Color.white;
        private static readonly Color NormalText =
            new(0.5769231f, 0.3522559f, 0.3522559f, 1f);
        private static readonly Color SelectedSubtitle =
            new(0.5686275f, 0.81960785f, 0.6039216f, 1f);
        private static readonly Color NormalSubtitle =
            new(0.8156863f, 0.69803923f, 0.67058825f, 1f);

        [SerializeField] private Button button;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Image background;
        [SerializeField] private Text nativeLabel;
        [SerializeField] private Text subtitleLabel;
        [SerializeField] private GameObject checkMark;
        [SerializeField] private Font primaryFont;
        [SerializeField] private Font eastAsianFallbackFont;

        private bool _hasPointerPress;
        private float _pressScrollY;

        public event Action<LanguageOptionView> Pressed;

        public int Index { get; private set; } = -1;

        private void Awake()
        {
            if (button != null) button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
            Pressed = null;
        }

        public void Setup(
            int index,
            LanguageOptionDefinition definition,
            LocalizationCatalog catalog)
        {
            Index = index;
            if (nativeLabel != null)
            {
                nativeLabel.text = definition.NativeName;
                nativeLabel.font = FontFor(definition.Locale);
            }

            if (subtitleLabel != null)
            {
                string subtitle = definition.NativeName;
                if (catalog != null &&
                    !string.IsNullOrEmpty(definition.TranslationKey))
                {
                    string translated = catalog.Translate(
                        definition.TranslationKey);
                    if (translated != definition.TranslationKey)
                        subtitle = translated;
                }
                subtitleLabel.text = subtitle;
                subtitleLabel.font = FontFor(catalog?.Locale);
            }
        }

        public void BindScrollRect(ScrollRect value)
        {
            scrollRect = value;
        }

        public void SetSelected(bool selected)
        {
            if (background != null)
                background.color = selected
                    ? SelectedBackground
                    : NormalBackground;
            if (nativeLabel != null)
                nativeLabel.color = selected ? SelectedText : NormalText;
            if (subtitleLabel != null)
                subtitleLabel.color = selected
                    ? SelectedSubtitle
                    : NormalSubtitle;
            if (checkMark != null) checkMark.SetActive(selected);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _hasPointerPress = true;
            _pressScrollY = CurrentScrollY();
        }

        private void HandleClick()
        {
            if (_hasPointerPress &&
                !LanguageSelectionContract.IsTapWithinScrollTolerance(
                    _pressScrollY,
                    CurrentScrollY()))
            {
                _hasPointerPress = false;
                return;
            }
            _hasPointerPress = false;
            Pressed?.Invoke(this);
        }

        private float CurrentScrollY()
        {
            return scrollRect != null && scrollRect.content != null
                ? scrollRect.content.anchoredPosition.y
                : 0f;
        }

        private Font FontFor(string locale)
        {
            return eastAsianFallbackFont != null &&
                   LocalizationLocaleContract.UsesEastAsianFallback(locale)
                ? eastAsianFallbackFont
                : primaryFont;
        }
    }
}
