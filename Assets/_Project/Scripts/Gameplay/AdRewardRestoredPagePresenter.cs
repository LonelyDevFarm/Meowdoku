using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Meowdoku.Core.Ads;
using Meowdoku.Core.Daily;
using Meowdoku.Core.Localization;
using Meowdoku.Core.Tracking;
using Meowdoku.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class AdRewardRestoredPagePresenter : UIFrameWindow,
        IDailyMetaConsumer
    {
        private const int ObtainToDismissDelayFrames = 12;

        [SerializeField] private RectTransform content;
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text collectText;
        [SerializeField] private Button collectButton;
        [SerializeField] private Button actionCloseButton;
        [SerializeField] private AwardItemView locateReward;
        [SerializeField] private AwardItemView hintReward;
        [SerializeField] private LocalizationCatalog localization;

        private DailyMetaRuntime _runtime;
        private RewardRestoreBatch _batch;
        private bool _closing;
        private Tween _appearTween;

        public event Action Collected;
        public event Action Closed;

        public override string GetTrackingDialogName() =>
            TrackerCatalog.Dialog.RewardFail;

        protected override void OnCreate()
        {
            Add(collectButton, Collect);
            Add(actionCloseButton, CloseWithoutCollect);
        }

        protected override void OnShow(
            IReadOnlyDictionary<string, object> parameters)
        {
            _closing = false;
            _batch = ReadBatch(parameters);
            SetText(titleText, Translate(
                "AD_REWARD_RESTORED_TITLE",
                "Reward restored"));
            SetText(bodyText, Translate(
                "AD_REWARD_RESTORED_DESC",
                "Your missing ad reward has been restored."));
            SetText(collectText, Translate(
                "AD_REWARD_RESTORED_COLLECT",
                "Collect"));
            Render(_batch);
            if (collectButton != null) collectButton.interactable = _batch != null;
            if (actionCloseButton != null)
                actionCloseButton.interactable = true;
            PlayAppear();
        }

        protected override IEnumerator OnHide()
        {
            _appearTween?.Kill();
            _appearTween = null;
            _batch = null;
            yield break;
        }

        protected override bool OnBackRequest()
        {
            CloseWithoutCollect();
            return true;
        }

        protected override void OnDestroyWindow()
        {
            _appearTween?.Kill();
            Remove(collectButton, Collect);
            Remove(actionCloseButton, CloseWithoutCollect);
            Collected = null;
            Closed = null;
            base.OnDestroyWindow();
        }

        public void BindDailyMetaRuntime(DailyMetaRuntime runtime)
        {
            _runtime = runtime;
        }

        private void Collect()
        {
            if (_closing || _batch == null || _runtime?.Awards == null)
                return;
            _closing = true;
            SetInteractable(false);
            Tracking?.TrackButtonClick(
                TrackerCatalog.Button.Collect,
                GetTrackingDialogName());
            int uid = _runtime.Awards.Dispatch(
                _batch.Items,
                AwardDisplayType.Direct,
                TrackerCatalog.PropSource.RewardFailDialog);
            if (uid < 0)
            {
                _closing = false;
                SetInteractable(true);
                return;
            }
            StartManagedCoroutine(FinishCollect());
        }

        private IEnumerator FinishCollect()
        {
            for (int frame = 0;
                 frame < ObtainToDismissDelayFrames;
                 frame++)
                yield return null;
            Collected?.Invoke();
            Owner?.Hide(UiName.AdRewardRestored);
        }

        private void CloseWithoutCollect()
        {
            if (_closing) return;
            _closing = true;
            SetInteractable(false);
            Closed?.Invoke();
            Owner?.Hide(UiName.AdRewardRestored);
        }

        private void Render(RewardRestoreBatch batch)
        {
            AwardItem locate = null;
            AwardItem hint = null;
            if (batch != null)
            {
                for (int index = 0; index < batch.Items.Count; index++)
                {
                    AwardItem item = batch.Items[index];
                    if (item.Kind == "locate") locate = item;
                    else if (item.Kind == "hint") hint = item;
                }
            }
            locateReward?.Apply(locate);
            hintReward?.Apply(hint);
        }

        private void PlayAppear()
        {
            _appearTween?.Kill();
            if (overlayGroup != null) overlayGroup.alpha = 0f;
            if (content != null) content.localScale = Vector3.one * 0.7f;
            Sequence sequence = DOTween.Sequence().SetLink(gameObject);
            if (overlayGroup != null)
                sequence.Join(overlayGroup.DOFade(1f, 0.2f));
            if (content != null)
                sequence.Join(content.DOScale(1f, 0.3f)
                    .SetEase(Ease.OutBack));
            _appearTween = sequence;
        }

        private void SetInteractable(bool value)
        {
            if (collectButton != null) collectButton.interactable = value;
            if (actionCloseButton != null)
                actionCloseButton.interactable = value;
        }

        private string Translate(string key, string fallback)
        {
            if (localization == null) return fallback;
            string value = localization.Translate(key);
            return string.IsNullOrEmpty(value) || value == key
                ? fallback
                : value;
        }

        private static RewardRestoreBatch ReadBatch(
            IReadOnlyDictionary<string, object> parameters)
        {
            return parameters != null &&
                   parameters.TryGetValue("batch", out object value)
                ? value as RewardRestoreBatch
                : null;
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

        private static void SetText(Text text, string value)
        {
            if (text != null) text.text = value ?? string.Empty;
        }
    }
}
