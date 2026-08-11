using System.Collections;
using System.Collections.Generic;
using Meowdoku.Core.Localization;
using Meowdoku.Core.Rank;
using Meowdoku.Core.Tracking;
using Meowdoku.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class RankActivityOpenPopupPresenter : UIFrameWindow,
        IRankActivityConsumer,
        IClockTickConsumer
    {
        public override string GetTrackingDialogName() =>
            TrackerCatalog.Dialog.ChallengeGuide;

        [SerializeField] private GenericPopupAnimator popupAnimator;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text countdownText;
        [SerializeField] private Text actionText;
        [SerializeField] private GameObject catVisual;
        [SerializeField] private GameObject fishVisual;
        [SerializeField] private Button actionButton;
        [SerializeField] private Button actionCloseButton;
        [SerializeField] private LocalizationCatalog localization;

        private RankActivityRuntime _runtime;
        private ClockTicker _clockTicker;
        private bool _started;
        private bool _clockSubscribed;

        public bool WasStarted => _started;

        protected override void OnCreate()
        {
            Add(actionButton, StartActivity);
            Add(actionCloseButton, Close);
            if (localization != null)
                localization.LocaleChanged += RefreshText;
            RefreshText();
        }

        protected override void OnShow(
            IReadOnlyDictionary<string, object> parameters)
        {
            _started = false;
            RefreshText();
            RefreshCountdown();
            SubscribeClock();
            popupAnimator?.PlayOpen();
        }

        protected override IEnumerator PlayCloseAnimation()
        {
            if (popupAnimator != null)
                yield return popupAnimator.PlayClose();
        }

        protected override IEnumerator OnHide()
        {
            UnsubscribeClock();
            popupAnimator?.Stop();
            yield break;
        }

        protected override void OnDestroyWindow()
        {
            UnsubscribeClock();
            if (localization != null)
                localization.LocaleChanged -= RefreshText;
            Remove(actionButton, StartActivity);
            Remove(actionCloseButton, Close);
            base.OnDestroyWindow();
        }

        public void BindRankActivityRuntime(RankActivityRuntime runtime)
        {
            _runtime = runtime;
            RefreshText();
        }

        public void BindClockTicker(ClockTicker ticker)
        {
            if (_clockTicker == ticker) return;
            UnsubscribeClock();
            _clockTicker = ticker;
            SubscribeClock();
        }

        public void BindLocalization(LocalizationCatalog catalog)
        {
            if (localization == catalog) return;
            if (localization != null)
                localization.LocaleChanged -= RefreshText;
            localization = catalog;
            if (localization != null)
                localization.LocaleChanged += RefreshText;
            RefreshText();
        }

        private void StartActivity()
        {
            Tracking?.TrackButtonClick(
                TrackerCatalog.Button.Play,
                GetTrackingDialogName());
            _started = true;
            Owner?.Hide(UiName);
        }

        private void Close()
        {
            Tracking?.TrackButtonClick(
                TrackerCatalog.Button.Close,
                GetTrackingDialogName());
            Owner?.Hide(UiName);
        }

        private void RefreshText()
        {
            int group = _runtime?.Manager?.Group ??
                        RankActivityConfig.GroupCats;
            bool fish = group == RankActivityConfig.GroupFish;
            SetActive(catVisual, !fish);
            SetActive(fishVisual, fish);
            if (titleText != null)
                titleText.text = Translate("RANK_OPEN_TITLE", "New Session");
            if (bodyText != null)
                bodyText.text = RankPresentationContract
                    .GodotRichTextToPlainText(Translate(
                        fish ? "RANK_OPEN_DESC_FISH" : "RANK_OPEN_DESC",
                        fish
                            ? "Play games to collect and rank up during each event. Aim for higher ranks!"
                            : "Play games to find and rank up during each event. Aim for higher ranks!"));
            if (actionText != null)
                actionText.text = Translate("RANK_OPEN_CONFIRM", "Got it");
        }

        private void RefreshCountdown()
        {
            if (countdownText == null) return;
            int remaining = _runtime?.Manager?.RemainingSeconds ?? 0;
            countdownText.text = RankPresentationContract.FormatHms(remaining);
        }

        private void SubscribeClock()
        {
            if (_clockSubscribed || !IsShowing || _clockTicker == null) return;
            _clockTicker.SecondTick += RefreshCountdown;
            _clockSubscribed = true;
        }

        private void UnsubscribeClock()
        {
            if (!_clockSubscribed) return;
            if (_clockTicker != null)
                _clockTicker.SecondTick -= RefreshCountdown;
            _clockSubscribed = false;
        }

        private string Translate(string key, string fallback)
        {
            if (localization == null) return fallback;
            string value = localization.Translate(key);
            return string.IsNullOrEmpty(value) || value == key
                ? fallback
                : value;
        }

        private static void Add(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.AddListener(action);
        }

        private static void Remove(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.RemoveListener(action);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
