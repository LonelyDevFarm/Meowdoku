using System.Collections;
using System.Collections.Generic;
using Meowdoku.Core.Daily;
using Meowdoku.Core.Localization;
using Meowdoku.Core.Profile;
using Meowdoku.Core.Tracking;
using Meowdoku.Core.UI;
using Meowdoku.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class AwardPagePresenter : UIFrameWindow,
        IDailyMetaConsumer,
        IProfileConsumer,
        ISoundServiceConsumer
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
        [SerializeField] private FrameAwardEffectView frameAddEffect;
        [SerializeField] private AwardItemView[] itemViews =
            System.Array.Empty<AwardItemView>();
        [SerializeField] private LocalizationCatalog localization;

        private DailyMetaRuntime _runtime;
        private ProfileRuntime _profileRuntime;
        private AwardPresentationRequest _request;
        private AwardItem _frameItem;
        private bool _completed;
        private bool _closing;
        private int _generation;
        private int _rankPhase;
        private SoundService _soundService;

        protected override void OnCreate()
        {
            if (collectButton != null)
                collectButton.onClick.AddListener(Collect);
            if (doubleCollectButton != null)
                doubleCollectButton.gameObject.SetActive(false);
            if (rankGiftView != null)
            {
                rankGiftView.CollectRequested += HandleRankGiftCollect;
                if (_soundService != null)
                    rankGiftView.BindSoundService(_soundService);
            }
        }

        protected override void OnShow(
            IReadOnlyDictionary<string, object> parameters)
        {
            _generation++;
            _completed = false;
            _closing = false;
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
            frameAddEffect?.StopImmediate();
            _frameItem = FindFrameItem(_request.Items);
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
                if (IsFrameOnly(_request.Items))
                {
                    SetActive(regularRoot, false);
                    if (collectButton != null)
                        collectButton.gameObject.SetActive(false);
                    BeginCloseWithFrameEffect();
                    return;
                }
                if (collectButton != null)
                    collectButton.interactable = false;
                StartManagedCoroutine(UnlockCollect(_generation));
            }
        }

        protected override IEnumerator OnHide()
        {
            _generation++;
            rankGiftView?.StopImmediate();
            frameAddEffect?.StopImmediate();
            CompleteOnce();
            _request = null;
            _frameItem = null;
            _closing = false;
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
            rankGiftView?.StopImmediate();
            frameAddEffect?.StopImmediate();
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

        public void BindProfileRuntime(ProfileRuntime runtime)
        {
            _profileRuntime = runtime;
        }

        public void BindSoundService(SoundService service)
        {
            _soundService = service;
            rankGiftView?.BindSoundService(service);
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
            if (rankGiftView.HasBox)
            {
                SetActive(regularRoot, true);
                RenderItems(_request.Items);
                if (collectButton != null)
                    collectButton.interactable = false;
                StartManagedCoroutine(UnlockCollect(_generation));
            }
            else
            {
                SetActive(regularRoot, false);
                if (collectButton != null)
                    collectButton.gameObject.SetActive(false);
                BeginCloseWithFrameEffect();
            }
        }

        private void Collect()
        {
            if (_completed || _closing || collectButton == null ||
                !collectButton.interactable)
                return;
            if (_rankPhase > 0)
                Tracking?.TrackButtonClick(
                    TrackerCatalog.Button.Collect,
                    GetTrackingDialogName());
            BeginCloseWithFrameEffect();
        }

        private void BeginCloseWithFrameEffect()
        {
            if (_completed || _closing) return;
            _closing = true;
            if (collectButton != null)
            {
                collectButton.interactable = false;
                collectButton.gameObject.SetActive(false);
            }
            if (doubleCollectButton != null)
                doubleCollectButton.gameObject.SetActive(false);

            if (_frameItem == null || frameAddEffect == null ||
                _profileRuntime == null)
            {
                FinishClose();
                return;
            }

            SetActive(regularRoot, false);
            SetActive(
                rankGiftView != null ? rankGiftView.gameObject : null,
                false);
            ProfileService profile = _profileRuntime.Service;
            HomePagePresenter home = Owner?.Get(UiName.Home) as
                HomePagePresenter;
            RectTransform profileTarget = home != null && home.IsShowing
                ? home.ProfileEntryRect
                : null;
            frameAddEffect.Play(
                profile.AvatarId,
                _frameItem.FrameId,
                profile.GetFrameCount(_frameItem.FrameId),
                _frameItem.Count,
                profileTarget,
                () => Owner?.FadeOutMaskEarly(0.2f),
                () => home?.PlayProfileShake(),
                FinishClose);
        }

        private void FinishClose()
        {
            if (!_closing || !IsShowing) return;
            if (CompleteOnce()) Owner?.Hide(UiName.Award);
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
            int itemIndex = 0;
            for (int index = 0; index < itemViews.Length; index++)
            {
                AwardItemView view = itemViews[index];
                if (view == null) continue;
                AwardItem item = null;
                while (items != null && itemIndex < items.Count)
                {
                    AwardItem candidate = items[itemIndex++];
                    if (candidate?.Category == AwardCategory.Frame) continue;
                    item = candidate;
                    break;
                }
                view.Apply(item);
            }
        }

        private static AwardItem FindFrameItem(
            IReadOnlyList<AwardItem> items)
        {
            if (items == null) return null;
            for (int index = 0; index < items.Count; index++)
            {
                AwardItem item = items[index];
                if (item?.Category == AwardCategory.Frame && item.IsValid())
                    return item;
            }
            return null;
        }

        private static bool IsFrameOnly(IReadOnlyList<AwardItem> items)
        {
            bool hasFrame = false;
            if (items == null) return false;
            for (int index = 0; index < items.Count; index++)
            {
                AwardItem item = items[index];
                if (item == null || !item.IsValid()) continue;
                if (item.Category == AwardCategory.Frame) hasFrame = true;
                else return false;
            }
            return hasFrame;
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
