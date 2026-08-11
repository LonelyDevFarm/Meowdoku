using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Meowdoku.Core.Localization;
using Meowdoku.Core.Platform;
using Meowdoku.Core.Tracking;
using Meowdoku.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PrivacyDialogPresenter : UIFrameWindow,
        IPrivacyDialogWindow,
        ISettingsExternalServicesConsumer
    {
        [SerializeField] private GenericPopupAnimator popupAnimator;
        [SerializeField] private Text titleText;
        [SerializeField] private Text contentText;
        [SerializeField] private Text acceptText;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button termsButton;
        [SerializeField] private Button privacyButton;
        [SerializeField] private LocalizationCatalog localization;

        private ISettingsExternalServices _external =
            OfflineSettingsExternalServices.Instance;
        private bool _closing;
        private bool _closeAlreadyPlayed;

        public event Action Accepted;
        public override string GetTrackingDialogName() =>
            TrackerCatalog.Dialog.Privacy;

        protected override void OnCreate()
        {
            Add(acceptButton, Accept);
            Add(termsButton, OpenTerms);
            Add(privacyButton, OpenPrivacy);
        }

        protected override void OnShow(
            IReadOnlyDictionary<string, object> parameters)
        {
            _closing = false;
            _closeAlreadyPlayed = false;
            Set(titleText, Translate("PRIVACY_DIALOG_TITLE", "Welcome"));
            string description = Translate(
                "PRIVACY_DIALOG_DESC_RICH",
                "Please read and accept our Terms of Service and Privacy Policy.");
            description = ReplaceFirst(description, "%s", TermsUrl);
            description = ReplaceFirst(description, "%s", PrivacyUrl);
            Set(contentText,
                Regex.Replace(description, @"\[/?[^\]]+\]", string.Empty));
            Set(acceptText, Translate("PRIVACY_DIALOG_ACCEPT", "Accept"));
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
            _closing = false;
            yield break;
        }

        protected override void OnDestroyWindow()
        {
            Remove(acceptButton, Accept);
            Remove(termsButton, OpenTerms);
            Remove(privacyButton, OpenPrivacy);
            Accepted = null;
            base.OnDestroyWindow();
        }

        public void BindSettingsExternalServices(ISettingsExternalServices services)
        {
            _external = services ?? OfflineSettingsExternalServices.Instance;
        }

        private void Accept()
        {
            if (_closing) return;
            Tracking?.TrackButtonClick(
                TrackerCatalog.Button.Accept,
                TrackerCatalog.Dialog.Privacy);
            _closing = true;
            StartManagedCoroutine(AcceptAfterClose());
        }

        private IEnumerator AcceptAfterClose()
        {
            if (popupAnimator != null) yield return popupAnimator.PlayClose();
            _closeAlreadyPlayed = true;
            Accepted?.Invoke();
        }

        private void OpenTerms() =>
            _external.OpenLocalizedPrivacyUrl(TermsUrl);

        private void OpenPrivacy() =>
            _external.OpenLocalizedPrivacyUrl(PrivacyUrl);

        private string Translate(string key, string fallback)
        {
            if (localization == null) return fallback;
            string result = localization.Translate(key);
            return result == key ? fallback : result;
        }

        private const string TermsUrl = "https://oakevergames.com/tos.html";
        private const string PrivacyUrl = "https://oakevergames.com/pp.html";

        private static string ReplaceFirst(
            string value,
            string target,
            string replacement)
        {
            int index = value?.IndexOf(target, StringComparison.Ordinal) ?? -1;
            return index < 0
                ? value ?? string.Empty
                : value.Substring(0, index) + replacement +
                  value.Substring(index + target.Length);
        }

        private static void Add(
            Button button,
            UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.AddListener(action);
        }

        private static void Remove(
            Button button,
            UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.RemoveListener(action);
        }

        private static void Set(Text text, string value)
        {
            if (text != null) text.text = value ?? string.Empty;
        }
    }
}
