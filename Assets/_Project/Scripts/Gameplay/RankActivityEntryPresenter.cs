using System;
using Meowdoku.Core.Rank;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class RankActivityEntryPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject contentRoot;
        [SerializeField] private GameObject pendingRewardState;
        [SerializeField] private GameObject activeState;
        [SerializeField] private GameObject rankMedal;
        [SerializeField] private Text rankText;
        [SerializeField] private Text countdownText;
        [SerializeField] private GameObject[] chestTiers = new GameObject[0];
        [SerializeField] private GameObject frameOnlyChest;
        [SerializeField] private Button clickButton;

        private RankActivityRuntime _runtime;
        private bool _presenting;

        public event Action OpenRequested;

        private void Awake()
        {
            if (clickButton != null)
                clickButton.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            Unsubscribe();
            if (clickButton != null)
                clickButton.onClick.RemoveListener(HandleClick);
        }

        public void BindRankActivityRuntime(RankActivityRuntime runtime)
        {
            if (_runtime == runtime) return;
            Unsubscribe();
            _runtime = runtime;
            Subscribe();
            RefreshNow();
        }

        public void Show()
        {
            _presenting = true;
            Subscribe();
            RefreshNow();
        }

        public void Hide()
        {
            _presenting = false;
            Unsubscribe();
        }

        public void RefreshNow()
        {
            RankActivityManager manager = _runtime?.Manager;
            bool shown = manager != null && manager.HasHomeEntry;
            SetActive(contentRoot != null ? contentRoot : gameObject, shown);
            if (!shown) return;

            RankSettlementResult pending = manager.GetPendingReward();
            bool hasPending = pending != null;
            SetActive(pendingRewardState, hasPending);
            SetActive(activeState, !hasPending);
            if (hasPending)
            {
                bool frameOnly = manager.Group ==
                                 RankActivityConfig.GroupFrameOnly;
                SetActive(frameOnlyChest, frameOnly);
                int tier = RankPresentationContract.EntryChestTier(
                    pending.Rank);
                for (int index = 0; index < chestTiers.Length; index++)
                    SetActive(
                        chestTiers[index],
                        !frameOnly && index == tier - 1);
            }
            else
            {
                bool hasRank = RankPresentationContract.ShowsPlayerRank(
                    manager.IsJoined,
                    manager.CollectTotal);
                SetActive(rankMedal, hasRank);
                if (rankText != null && hasRank)
                    rankText.text = manager.GetPlayerRank().ToString();
                if (countdownText != null)
                    countdownText.text = RankPresentationContract.FormatHms(
                        manager.RemainingSeconds);
            }
        }

        private void Subscribe()
        {
            if (!_presenting || _runtime?.Manager == null) return;
            RankActivityManager manager = _runtime.Manager;
            manager.StateChanged -= HandleStateChanged;
            manager.StateChanged += HandleStateChanged;
            manager.RankingChanged -= HandleRankingChanged;
            manager.RankingChanged += HandleRankingChanged;
            manager.TimeTicked -= HandleTimeTicked;
            manager.TimeTicked += HandleTimeTicked;
        }

        private void Unsubscribe()
        {
            if (_runtime?.Manager == null) return;
            RankActivityManager manager = _runtime.Manager;
            manager.StateChanged -= HandleStateChanged;
            manager.RankingChanged -= HandleRankingChanged;
            manager.TimeTicked -= HandleTimeTicked;
        }

        private void HandleStateChanged(RankActivityState _) => RefreshNow();
        private void HandleRankingChanged() => RefreshNow();
        private void HandleTimeTicked(int remaining, bool _)
        {
            if (activeState != null && activeState.activeInHierarchy &&
                countdownText != null)
                countdownText.text = RankPresentationContract.FormatHms(
                    remaining);
        }

        private void HandleClick()
        {
            if (_runtime?.Manager != null && _runtime.Manager.HasHomeEntry)
                OpenRequested?.Invoke();
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
