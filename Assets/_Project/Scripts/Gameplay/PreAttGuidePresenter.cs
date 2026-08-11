using System;
using System.Collections;
using System.Collections.Generic;
using Meowdoku.Core.Localization;
using Meowdoku.Core.Platform;
using Meowdoku.Core.Tracking;
using Meowdoku.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PreAttGuidePresenter : UIFrameWindow,
        IPreAttGuideWindow
    {
        [SerializeField] private GenericPopupAnimator popupAnimator;
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text continueText;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button guideCloseButton;
        [SerializeField] private LocalizationCatalog localization;
        [SerializeField] private bool restyled;

        private bool _closing;
        private bool _closeAlreadyPlayed;

        public event Action Continued;
        public override string GetTrackingDialogName() =>
            TrackerCatalog.Dialog.PreAttGuide;

        protected override void OnCreate()
        {
            Add(continueButton, Continue);
            Add(guideCloseButton, Continue);
        }

        protected override void OnShow(
            IReadOnlyDictionary<string, object> parameters)
        {
            _closing = false;
            _closeAlreadyPlayed = false;
            Set(titleText,
                Translate("PRE_ATT_GUIDE_TITLE", "Please Allow Tracking"));
            Set(descriptionText, restyled
                ? BuildRestyledDescription()
                : Translate(
                    "PRE_ATT_GUIDE_DESC",
                    "Help support our ability to offer this app for free."));
            Set(continueText,
                Translate("PRE_ATT_GUIDE_CONTINUE", "Continue"));
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
            Remove(continueButton, Continue);
            Remove(guideCloseButton, Continue);
            Continued = null;
            base.OnDestroyWindow();
        }

        private void Continue()
        {
            if (_closing) return;
            Tracking?.TrackButtonClick(
                TrackerCatalog.Button.AttContinue,
                TrackerCatalog.Dialog.PreAttGuide);
            _closing = true;
            StartManagedCoroutine(ContinueAfterClose());
        }

        private IEnumerator ContinueAfterClose()
        {
            if (popupAnimator != null) yield return popupAnimator.PlayClose();
            _closeAlreadyPlayed = true;
            Continued?.Invoke();
        }

        private string BuildRestyledDescription()
        {
            return "• " + Translate(
                       "PRE_ATT_GUIDE_BULLET_FREE",
                       "Stay Free: Support free app access.") +
                   "\n\n• " + Translate(
                       "PRE_ATT_GUIDE_BULLET_RELEVANT",
                       "Relevant Ads: Less noise, more relevance.") +
                   "\n\n• " + Translate(
                       "PRE_ATT_GUIDE_BULLET_CHANGE",
                       "Change Anytime: Flexible system settings.");
        }

        private string Translate(string key, string fallback)
        {
            if (localization == null) return fallback;
            string result = localization.Translate(key);
            return result == key ? fallback : result;
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
