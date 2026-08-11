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
    public sealed class PrePushGuidePresenter : UIFrameWindow,
        IPrePushGuideWindow
    {
        [SerializeField] private PushGuidePopupAnimator popupAnimator;
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text allowText;
        [SerializeField] private Button allowButton;
        [SerializeField] private Button guideCloseButton;
        [SerializeField] private LocalizationCatalog localization;

        private bool _closing;
        private bool _closeAlreadyPlayed;
        private int _showCount;

        public event Action<PushGuideCloseSource> Closed;
        public override string GetTrackingDialogName() =>
            TrackerCatalog.Dialog.PushGuide;
        public override IReadOnlyDictionary<string, object>
            GetTrackingDialogExtra() =>
            new Dictionary<string, object>(1)
            {
                ["show_count"] = _showCount
            };

        protected override void OnCreate()
        {
            Add(allowButton, Allow);
            Add(guideCloseButton, Close);
        }

        protected override void OnShow(
            IReadOnlyDictionary<string, object> parameters)
        {
            _closing = false;
            _closeAlreadyPlayed = false;
            _showCount = ReadInt(parameters, "show_count");
            Set(titleText, Translate(
                "PRE_PUSH_GUIDE_TITLE",
                "Your cat's been thinking about you"));
            Set(descriptionText, Translate(
                "PRE_PUSH_GUIDE_DESC",
                "A little cat company can brighten your day."));
            Set(allowText, Translate(
                "PRE_PUSH_GUIDE_ALLOW",
                "Allow Notifications"));
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
            Remove(allowButton, Allow);
            Remove(guideCloseButton, Close);
            Closed = null;
            base.OnDestroyWindow();
        }

        private void Allow()
        {
            Tracking?.TrackButtonClick(
                TrackerCatalog.Button.PushGuideJoin,
                TrackerCatalog.Dialog.PushGuide);
            RequestClose(PushGuideCloseSource.AllowButton);
        }

        private void Close()
        {
            Tracking?.TrackButtonClick(
                TrackerCatalog.Button.PushGuideClose,
                TrackerCatalog.Dialog.PushGuide);
            RequestClose(PushGuideCloseSource.CloseButton);
        }

        private void RequestClose(PushGuideCloseSource source)
        {
            if (_closing) return;
            _closing = true;
            StartManagedCoroutine(CloseAfterAnimation(source));
        }

        private IEnumerator CloseAfterAnimation(PushGuideCloseSource source)
        {
            if (popupAnimator != null) yield return popupAnimator.PlayClose();
            _closeAlreadyPlayed = true;
            Closed?.Invoke(source);
        }

        private string Translate(string key, string fallback)
        {
            if (localization == null) return fallback;
            string result = localization.Translate(key);
            return result == key ? fallback : result;
        }

        private static int ReadInt(
            IReadOnlyDictionary<string, object> parameters,
            string key)
        {
            if (parameters == null ||
                !parameters.TryGetValue(key, out object value) ||
                value == null)
                return 0;
            try { return Convert.ToInt32(value); }
            catch (Exception) { return 0; }
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
