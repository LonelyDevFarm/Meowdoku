using System.Collections;
using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.Localization;
using Meowdoku.Core.Rank;
using Meowdoku.Core.Robot;
using Meowdoku.Core.Tracking;
using Meowdoku.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class RankActivityPagePresenter : UIFrameWindow,
        IRankActivityConsumer
    {
        public override string GetTrackingScreenName() =>
            TrackerCatalog.Screen.ChallengeRank;

        [SerializeField] private Button backButton;
        [SerializeField] private Button infoButton;
        [SerializeField] private Button ctaButton;
        [SerializeField] private Text titleText;
        [SerializeField] private Text countdownText;
        [SerializeField] private Text ctaText;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private RectTransform rowList;
        [SerializeField] private RankActivityRowView rowPrefab;
        [SerializeField] private RankActivityPodiumView[] podiums =
            new RankActivityPodiumView[0];
        [SerializeField] private LocalizationCatalog localization;

        private readonly List<RankActivityRowView> _rows = new();
        private RankActivityRuntime _runtime;
        private bool _subscribed;
        private bool _rewardFlowActive;
        private bool _profileOpening;

        protected override void OnCreate()
        {
            Add(backButton, Close);
            Add(infoButton, OpenHowToPlay);
            Add(ctaButton, StartGame);
            for (int index = 0; index < podiums.Length; index++)
                if (podiums[index] != null)
                    podiums[index].SelfRequested += OpenSelfProfile;
            if (localization != null)
                localization.LocaleChanged += RefreshText;
            RefreshText();
        }

        protected override void OnShow(
            IReadOnlyDictionary<string, object> parameters)
        {
            _rewardFlowActive = false;
            _profileOpening = false;
            Subscribe();
            RefreshAll();
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
            if (_runtime?.Manager?.GetPendingReward() != null)
            {
                _rewardFlowActive = true;
                StartManagedCoroutine(ClaimRewardDeferred());
            }
        }

        protected override IEnumerator OnHide()
        {
            Unsubscribe();
            _profileOpening = false;
            yield break;
        }

        protected override bool OnBackRequest()
        {
            Close();
            return true;
        }

        protected override void OnDestroyWindow()
        {
            Unsubscribe();
            Remove(backButton, Close);
            Remove(infoButton, OpenHowToPlay);
            Remove(ctaButton, StartGame);
            for (int index = 0; index < podiums.Length; index++)
                if (podiums[index] != null)
                    podiums[index].SelfRequested -= OpenSelfProfile;
            for (int index = 0; index < _rows.Count; index++)
                if (_rows[index] != null)
                    _rows[index].SelfRequested -= OpenSelfProfile;
            if (localization != null)
                localization.LocaleChanged -= RefreshText;
            base.OnDestroyWindow();
        }

        public void BindRankActivityRuntime(RankActivityRuntime runtime)
        {
            if (_runtime == runtime) return;
            Unsubscribe();
            _runtime = runtime;
            Subscribe();
            if (IsShowing) RefreshAll();
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

        private IEnumerator ClaimRewardDeferred()
        {
            yield return null;
            if (IsShowing && _runtime?.Manager?.GetPendingReward() != null)
                _runtime.Manager.ClaimReward();
        }

        private void Subscribe()
        {
            if (_subscribed || !IsShowing || _runtime?.Manager == null) return;
            RankActivityManager manager = _runtime.Manager;
            manager.RankingChanged += RefreshAll;
            manager.StateChanged += HandleStateChanged;
            manager.TimeTicked += HandleTimeTicked;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            if (_runtime?.Manager != null)
            {
                _runtime.Manager.RankingChanged -= RefreshAll;
                _runtime.Manager.StateChanged -= HandleStateChanged;
                _runtime.Manager.TimeTicked -= HandleTimeTicked;
            }
            _subscribed = false;
        }

        private void HandleStateChanged(RankActivityState state)
        {
            if (!IsShowing) return;
            if (state == RankActivityState.NotOpened)
            {
                if (_rewardFlowActive) Owner?.Hide(UiName);
                return;
            }
            if (state == RankActivityState.Settling &&
                _runtime?.Manager?.GetPendingReward() != null)
            {
                _rewardFlowActive = true;
                _runtime.Manager.ClaimReward();
            }
        }

        private void HandleTimeTicked(int remaining, bool _)
        {
            if (countdownText != null)
                countdownText.text = RankPresentationContract.FormatHms(
                    remaining);
        }

        private void RefreshAll()
        {
            RankActivityManager manager = _runtime?.Manager;
            if (manager == null) return;
            List<RankInfo> infos = manager.GetRankInfos();
            ApplyPodiums(infos, manager.Group);
            EnsureRows(infos.Count);
            for (int index = 0; index < _rows.Count; index++)
                _rows[index].Apply(
                    index < infos.Count ? infos[index] : null,
                    manager.Group);
            if (countdownText != null)
                countdownText.text = RankPresentationContract.FormatHms(
                    manager.RemainingSeconds);
            RefreshText();
        }

        private void ApplyPodiums(IReadOnlyList<RankInfo> infos, int group)
        {
            for (int place = 1; place <= podiums.Length; place++)
            {
                RankInfo found = null;
                for (int index = 0; index < infos.Count; index++)
                {
                    if (infos[index]?.Rank != place) continue;
                    found = infos[index];
                    break;
                }
                podiums[place - 1]?.Apply(found, group, place);
            }
        }

        private void EnsureRows(int count)
        {
            if (rowPrefab == null || rowList == null) return;
            while (_rows.Count < count)
            {
                RankActivityRowView row = Instantiate(rowPrefab, rowList);
                row.name = $"RankRow_{_rows.Count + 1:00}";
                row.SelfRequested += OpenSelfProfile;
                _rows.Add(row);
            }
        }

        private void OpenSelfProfile()
        {
            if (_profileOpening || Owner == null) return;
            Tracking?.TrackButtonClick(
                TrackerCatalog.Button.SelfInfo,
                GetTrackingScreenName());
            StartManagedCoroutine(OpenSelfProfileRoutine());
        }

        private IEnumerator OpenSelfProfileRoutine()
        {
            _profileOpening = true;
            UIFrameWindow profile = Owner.Show(UiName.Profile);
            if (profile != null) yield return Owner.AwaitHidden(UiName.Profile);
            _profileOpening = false;
            if (IsShowing) RefreshAll();
        }

        private void Close()
        {
            Tracking?.TrackButtonClick(
                TrackerCatalog.Button.Back,
                GetTrackingScreenName());
            Owner?.Hide(UiName);
        }

        private void OpenHowToPlay()
        {
            Tracking?.TrackButtonClick(
                TrackerCatalog.Button.ChallengeInfo,
                GetTrackingScreenName());
            Owner?.Show(UiName.RankActivityHowToPlay);
        }

        private void StartGame()
        {
            if (Owner == null) return;
            Tracking?.TrackButtonClick(
                TrackerCatalog.Button.Play,
                GetTrackingScreenName());
            UIFrameWindow game = Owner.Show(
                UiName.Game,
                new Dictionary<string, object>(1)
                {
                    ["level_index"] = GameStateRuntime.Current.CurrentLevel
                });
            if (game == null) return;
            UIFrameWindow home = Owner.Get(UiName.Home);
            if (home != null && home.IsShowing) Owner.Hide(UiName.Home);
            Owner.Hide(UiName);
        }

        private void RefreshText()
        {
            if (titleText != null)
                titleText.text = Translate("RANK_TITLE", "Leaderboard");
            if (ctaText != null)
                ctaText.text = Translate("RANK_CTA_COLLECT", "Go to Collect");
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
    }
}
