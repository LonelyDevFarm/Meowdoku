using System.Collections;
using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.Localization;
using Meowdoku.Core.Tracking;
using Meowdoku.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class LanguagePagePresenter : UIFrameWindow
    {
        public override string GetTrackingDialogName() =>
            TrackerCatalog.Dialog.LanguagePicker;

        [SerializeField] private GenericPopupAnimator popupAnimator;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private LanguageOptionView[] optionViews;
        [SerializeField] private Button confirmButton;
        [SerializeField] private LocalizationCatalog localization;

        private IReadOnlyList<LanguageOptionDefinition> _display;
        private int _selectedIndex = -1;
        private bool _confirmed;

        protected override void OnCreate()
        {
            if (optionViews != null)
            {
                foreach (LanguageOptionView option in optionViews)
                {
                    if (option == null) continue;
                    option.BindScrollRect(scrollRect);
                    option.Pressed += HandleOptionPressed;
                }
            }
            if (confirmButton != null)
                confirmButton.onClick.AddListener(ConfirmSelection);
            if (localization != null)
                localization.LocaleChanged += RefreshOptionLabels;
        }

        protected override void OnShow(
            IReadOnlyDictionary<string, object> parameters)
        {
            _confirmed = false;
            string system =
                LocalizationLocaleContract.ResolveCurrentSystemLocale();
            localization?.ApplySystemLocale(
                GameStateRuntime.Current,
                true,
                system);
            _display = LanguageSelectionContract.BuildDisplay(system);
            _selectedIndex = LanguageSelectionContract.ResolveCurrentIndex(
                _display,
                GameStateRuntime.Current.AppliedLocale,
                system);
            RefreshOptionLabels();
            RefreshSelection();
            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.StopMovement();
                scrollRect.verticalNormalizedPosition = 1f;
            }
            popupAnimator?.PlayOpen();
        }

        protected override IEnumerator PlayCloseAnimation()
        {
            if (popupAnimator != null)
                yield return popupAnimator.PlayClose();
        }

        protected override IEnumerator OnHide()
        {
            if (!_confirmed)
                Tracking?.TrackButtonClick(
                    TrackerCatalog.Button.LanguageCancel,
                    GetTrackingDialogName());
            popupAnimator?.Stop();
            yield break;
        }

        protected override void OnDestroyWindow()
        {
            popupAnimator?.Stop();
            if (optionViews != null)
            {
                foreach (LanguageOptionView option in optionViews)
                {
                    if (option != null)
                        option.Pressed -= HandleOptionPressed;
                }
            }
            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(ConfirmSelection);
            if (localization != null)
                localization.LocaleChanged -= RefreshOptionLabels;
            base.OnDestroyWindow();
        }

        public void BindLocalization(LocalizationCatalog catalog)
        {
            if (localization == catalog) return;
            if (localization != null)
                localization.LocaleChanged -= RefreshOptionLabels;
            localization = catalog;
            if (localization != null)
                localization.LocaleChanged += RefreshOptionLabels;
            RefreshOptionLabels();
        }

        private void HandleOptionPressed(LanguageOptionView option)
        {
            if (option == null || _display == null || option.Index < 0 ||
                option.Index >= _display.Count)
                return;
            _selectedIndex = option.Index;
            RefreshSelection();
        }

        private void ConfirmSelection()
        {
            if (_confirmed || _display == null || _selectedIndex < 0 ||
                _selectedIndex >= _display.Count)
                return;
            _confirmed = true;
            string locale = _display[_selectedIndex].Locale;
            Tracking?.TrackButtonClick(
                TrackerCatalog.Button.LanguageConfirm,
                GetTrackingDialogName());
            Tracking?.TrackUiLanguage(locale);
            localization?.SetLocale(locale);
            GameStateRuntime.Current.SetAppliedLocale(locale);
            Owner?.Hide(UiName.Language);
        }

        private void RefreshOptionLabels()
        {
            if (_display == null || optionViews == null) return;
            for (int index = 0; index < optionViews.Length; index++)
            {
                LanguageOptionView option = optionViews[index];
                if (option == null) continue;
                bool visible = index < _display.Count;
                option.gameObject.SetActive(visible);
                if (visible) option.Setup(index, _display[index], localization);
            }
        }

        private void RefreshSelection()
        {
            if (optionViews == null) return;
            for (int index = 0; index < optionViews.Length; index++)
            {
                LanguageOptionView option = optionViews[index];
                if (option != null && option.gameObject.activeSelf)
                    option.SetSelected(index == _selectedIndex);
            }
        }
    }
}
