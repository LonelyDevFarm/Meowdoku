using System;
using System.Collections;
using System.Collections.Generic;
using Meowdoku.Core.Localization;
using Meowdoku.Core.Platform;
using Meowdoku.Core.Tracking;
using Meowdoku.Core.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    /// <summary>
    /// Source port of feedback_page.gd. The native support surface is opened
    /// by ProductServiceRuntime; this window only owns the local feedback form.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FeedbackPagePresenter : UIFrameWindow, IFeedbackWindow
    {
        [SerializeField] private GenericPopupAnimator popupAnimator;
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text submitText;
        [SerializeField] private Text thanksText;
        [SerializeField] private InputField inputField;
        [SerializeField] private Button submitButton;
        [SerializeField] private Button feedbackCloseButton;
        [SerializeField] private LocalizationCatalog localization;

        private bool _asDialog;
        private bool _closing;
        private bool _closeAlreadyPlayed;
        private bool _submitted;

        public event Action Closed;
        public bool IsSubmitted => _submitted;

        public override string GetTrackingScreenName() =>
            _asDialog ? string.Empty : TrackerCatalog.Screen.Feedback;

        public override string GetTrackingDialogName() =>
            _asDialog ? TrackerCatalog.Dialog.Feedback : string.Empty;

        protected override bool UsesDefaultCloseButton => false;

        protected override void OnCreate()
        {
            if (submitButton != null) submitButton.onClick.AddListener(Submit);
            if (feedbackCloseButton != null)
                feedbackCloseButton.onClick.AddListener(Close);
            if (inputField != null)
            {
                inputField.onValueChanged.AddListener(HandleInputChanged);
                FeedbackFocusReleaseView focusRelease =
                    inputField.gameObject.GetComponent<FeedbackFocusReleaseView>();
                if (focusRelease == null)
                    focusRelease = inputField.gameObject.AddComponent<FeedbackFocusReleaseView>();
                focusRelease.Bind(inputField);
            }
        }

        protected override void OnShow(
            IReadOnlyDictionary<string, object> parameters)
        {
            _asDialog = ReadBool(parameters, "as_dlg");
            _closing = false;
            _closeAlreadyPlayed = false;
            _submitted = false;
            if (inputField != null)
            {
                inputField.text = string.Empty;
                inputField.interactable = true;
                inputField.Select();
                inputField.ActivateInputField();
            }
            SetText(titleText, Translate("FEEDBACK_TITLE", "Feedback"));
            SetText(descriptionText, Translate(
                "FEEDBACK_DESC", "Tell us what happened"));
            SetText(submitText, Translate("FEEDBACK_SUBMIT", "Submit"));
            SetText(thanksText, Translate("FEEDBACK_THANKS", "Thank you!"));
            if (thanksText != null) thanksText.gameObject.SetActive(false);
            RefreshSubmitState();
            popupAnimator?.PlayOpen();
        }

        protected override IEnumerator PlayCloseAnimation()
        {
            if (_closeAlreadyPlayed)
            {
                _closeAlreadyPlayed = false;
                yield break;
            }
            if (popupAnimator != null) yield return popupAnimator.PlayClose();
        }

        protected override IEnumerator OnHide()
        {
            popupAnimator?.Stop();
            if (inputField != null) inputField.DeactivateInputField();
            _closing = false;
            yield break;
        }

        protected override bool OnBackRequest()
        {
            Close();
            return true;
        }

        protected override void OnDestroyWindow()
        {
            if (submitButton != null) submitButton.onClick.RemoveListener(Submit);
            if (feedbackCloseButton != null)
                feedbackCloseButton.onClick.RemoveListener(Close);
            if (inputField != null)
                inputField.onValueChanged.RemoveListener(HandleInputChanged);
            Closed = null;
            base.OnDestroyWindow();
        }

        private void Submit()
        {
            if (_closing || inputField == null ||
                string.IsNullOrWhiteSpace(inputField.text)) return;

            Tracking?.TrackButtonClick(
                TrackerCatalog.Button.Submit,
                GetTrackingDialogName());
            // The source event includes free-form text. Keep the Unity port's
            // telemetry boundary content-free until that transfer is approved.
            _submitted = true;
            if (submitButton != null) submitButton.interactable = false;
            if (thanksText != null) thanksText.gameObject.SetActive(true);
            StartManagedCoroutine(CloseAfterSubmit());
        }

        private IEnumerator CloseAfterSubmit()
        {
            yield return new WaitForSecondsRealtime(0.35f);
            yield return CloseAfterAnimation();
        }

        private void Close()
        {
            if (_closing) return;
            Tracking?.TrackButtonClick(
                TrackerCatalog.Button.Close,
                GetTrackingDialogName());
            StartManagedCoroutine(CloseAfterAnimation());
        }

        private IEnumerator CloseAfterAnimation()
        {
            if (_closing) yield break;
            _closing = true;
            if (popupAnimator != null) yield return popupAnimator.PlayClose();
            _closeAlreadyPlayed = true;
            Closed?.Invoke();
            Owner?.Hide(UiName);
        }

        private void HandleInputChanged(string value) => RefreshSubmitState();

        private void RefreshSubmitState()
        {
            if (submitButton != null)
                submitButton.interactable = inputField != null &&
                    !string.IsNullOrWhiteSpace(inputField.text) && !_closing;
        }

        private string Translate(string key, string fallback)
        {
            if (localization == null) return fallback;
            string translated = localization.Translate(key);
            return translated == key || string.IsNullOrEmpty(translated)
                ? fallback
                : translated;
        }

        private static bool ReadBool(
            IReadOnlyDictionary<string, object> parameters,
            string key)
        {
            return parameters != null && parameters.TryGetValue(key, out object value) &&
                value is bool result && result;
        }

        private static void SetText(Text target, string value)
        {
            if (target != null) target.text = value ?? string.Empty;
        }
    }

    /// <summary>
    /// Releases the input field when a pointer starts outside its text box,
    /// matching the source page's keyboard-dismiss behavior.
    /// </summary>
    internal sealed class FeedbackFocusReleaseView : MonoBehaviour,
        IPointerDownHandler
    {
        private InputField _input;

        public void Bind(InputField input) => _input = input;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_input == null) return;
            RectTransform rect = _input.transform as RectTransform;
            if (rect == null || !RectTransformUtility.RectangleContainsScreenPoint(
                    rect, eventData.position, eventData.pressEventCamera))
                _input.DeactivateInputField();
        }
    }
}
