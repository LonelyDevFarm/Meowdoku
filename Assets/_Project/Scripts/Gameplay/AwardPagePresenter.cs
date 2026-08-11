using System.Collections;
using System.Collections.Generic;
using Meowdoku.Core.Daily;
using Meowdoku.Core.Localization;
using Meowdoku.Core.Tracking;
using Meowdoku.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class AwardPagePresenter : UIFrameWindow,
        IDailyMetaConsumer
    {
        public override string GetTrackingDialogName() => _rankPhase switch
        {
            1 => TrackerCatalog.Dialog.ChallengeReward,
            2 => TrackerCatalog.Dialog.ChallengeRewardGet,
            _ => string.Empty
        };

        public const float AppearGateSeconds = 1.6f;

        [SerializeField] private Text titleText;
        [SerializeField] private Text collectText;
        [SerializeField] private Button collectButton;
        [SerializeField] private Button doubleCollectButton;
        [SerializeField] private GameObject regularRoot;
        [SerializeField] private RankGiftView rankGiftView;
        [SerializeField] private AwardItemView[] itemViews =
            System.Array.Empty<AwardItemView>();
        [SerializeField] private LocalizationCatalog localization;

        private DailyMetaRuntime _runtime;
        private AwardPresentationRequest _request;
        private bool _completed;
        private int _generation;
        private int _rankPhase;

        protected override void OnCreate()
        {
            if (collectButton != null)
                collectButton.onClick.AddListener(Collect);
            if (doubleCollectButton != null)
                doubleCollectButton.gameObject.SetActive(false);
            if (rankGiftView != null)
                rankGiftView.CollectRequested += HandleRankGiftCollect;
        }

        protected override void OnShow(
            IReadOnlyDictionary<string, object> parameters)
        {
            _generation++;
            _completed = false;
            _rankPhase = 0;
            _request = ReadRequest(parameters);
            if (_request == null || _runtime == null)
            {
                Owner?.Hide(UiName.Award);
                return;
            }

            SetText(titleText, Translate(
                "DAILY_STREAK_GET_IT",
                "Get it"));
            SetText(collectText, Translate(
                "AD_REWARD_RESTORED_COLLECT",
                "Collect"));
            if (collectButton != null)
                collectButton.gameObject.SetActive(true);
            RenderItems(_request.Items);
            bool rankGift =
                _request.DisplayType == AwardDisplayType.RankGift;
            if (rankGift) _rankPhase = 1;
            SetActive(regularRoot, !rankGift);
            SetActive(
                rankGiftView != null ? rankGiftView.gameObject : null,
                rankGift);
            if (rankGift && rankGiftView != null)
            {
                rankGiftView.Apply(_request);
                rankGiftView.SetInteractable(false);
                StartManagedCoroutine(UnlockRankGift(_generation));
            }
            else
            {
                if (collectButton != null)
                    collectButton.interactable = false;
                StartManagedCoroutine(UnlockCollect(_generation));
            }
        }

        protected override IEnumerator OnHide()
        {
            _generation++;
            CompleteOnce();
            _request = null;
            _rankPhase = 0;
            SetActive(
                rankGiftView != null ? rankGiftView.gameObject : null,
                false);
            yield break;
        }

        protected override bool OnBackRequest() => true;

        protected override void OnDestroyWindow()
        {
            _generation++;
            CompleteOnce();
            if (rankGiftView != null)
                rankGiftView.CollectRequested -= HandleRankGiftCollect;
            if (collectButton != null)
                collectButton.onClick.RemoveListener(Collect);
            base.OnDestroyWindow();
        }

        public void BindDailyMetaRuntime(DailyMetaRuntime runtime)
        {
            _runtime = runtime;
        }

        private IEnumerator UnlockCollect(int generation)
        {
            yield return new WaitForSecondsRealtime(AppearGateSeconds);
            if (generation == _generation && IsShowing &&
                collectButton != null)
                collectButton.interactable = true;
        }

        private IEnumerator UnlockRankGift(int generation)
        {
            yield return new WaitForSecondsRealtime(AppearGateSeconds);
            if (generation == _generation && IsShowing)
                rankGiftView?.SetInteractable(true);
        }

        private IEnumerator CompleteFrameOnly(int generation)
        {
            yield return new WaitForSecondsRealtime(0.8f);
            if (generation != _generation || !IsShowing) yield break;
            if (CompleteOnce()) Owner?.Hide(UiName.Award);
        }

        private void HandleRankGiftCollect()
        {
            if (_completed || _request == null ||
                _request.DisplayType != AwardDisplayType.RankGift)
                return;
            Tracking?.TrackButtonClick(
                TrackerCatalog.Button.Collect,
                TrackerCatalog.Dialog.ChallengeReward);
            Tracking?.NotifyDialogClosed(
                TrackerCatalog.Dialog.ChallengeReward);
            _rankPhase = 2;
            Tracking?.TrackDialogShown(
                TrackerCatalog.Dialog.ChallengeRewardGet);
            rankGiftView.SetInteractable(false);
            SetActive(rankGiftView.gameObject, false);
            SetActive(regularRoot, true);
            RenderItems(_request.Items);
            if (rankGiftView.HasBox)
            {
                if (collectButton != null)
                    collectButton.interactable = false;
                StartManagedCoroutine(UnlockCollect(_generation));
            }
            else
            {
                if (collectButton != null)
                    collectButton.gameObject.SetActive(false);
                StartManagedCoroutine(CompleteFrameOnly(_generation));
            }
        }

        private void Collect()
        {
            if (_completed || collectButton == null ||
                !collectButton.interactable)
                return;
            if (_rankPhase > 0)
                Tracking?.TrackButtonClick(
                    TrackerCatalog.Button.Collect,
                    GetTrackingDialogName());
            if (!CompleteOnce()) return;
            Owner?.Hide(UiName.Award);
        }

        private bool CompleteOnce()
        {
            if (_completed || _request == null || _runtime == null)
                return false;
            _completed = _runtime.Awards.CompleteAward(_request.Uid);
            return _completed;
        }

        private void RenderItems(IReadOnlyList<AwardItem> items)
        {
            if (itemViews == null) return;
            for (int index = 0; index < itemViews.Length; index++)
            {
                AwardItemView view = itemViews[index];
                if (view == null) continue;
                view.Apply(items != null && index < items.Count
                    ? items[index]
                    : null);
            }
        }

        private static AwardPresentationRequest ReadRequest(
            IReadOnlyDictionary<string, object> parameters)
        {
            return parameters != null &&
                   parameters.TryGetValue(
                       "award_request",
                       out object value)
                ? value as AwardPresentationRequest
                : null;
        }

        private string Translate(string key, string fallback)
        {
            if (localization == null) return fallback;
            string value = localization.Translate(key);
            return string.IsNullOrEmpty(value) || value == key
                ? fallback
                : value;
        }

        private static void SetText(Text target, string value)
        {
            if (target != null) target.text = value ?? string.Empty;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
