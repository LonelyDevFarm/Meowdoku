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
        [SerializeField] private RectTransform floatingRowLayer;
        [SerializeField] private RankActivityRowView rowPrefab;
        [SerializeField] private RankActivityPodiumView[] podiums =
            new RankActivityPodiumView[0];
        [SerializeField] private LocalizationCatalog localization;

        private readonly List<RankActivityRowView> _rows = new();
        private RankActivityRuntime _runtime;
        private bool _subscribed;
        private bool _rewardFlowActive;
        private bool _profileOpening;
        private bool _playIntroPending;
        private RankActivityRowView _selfRow;
        private RankActivityRowView _floatingSelfRow;
        private int _selfIndex = -1;
        private bool _floatingAtTop;
        private bool _floatingAtBottom;
        private readonly Vector3[] _viewportCorners = new Vector3[4];
        private readonly Vector3[] _rowCorners = new Vector3[4];

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
            _playIntroPending = true;
            Subscribe();
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
            RefreshAll();
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
            _playIntroPending = false;
            for (int index = 0; index < _rows.Count; index++)
                _rows[index]?.ShowStatic();
            if (_floatingSelfRow != null)
            {
                _floatingSelfRow.SetSelfShadow(false);
                _floatingSelfRow.ShowStatic();
                _floatingSelfRow.gameObject.SetActive(false);
            }
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
            if (_floatingSelfRow != null)
                _floatingSelfRow.SelfRequested -= OpenSelfProfile;
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
            _selfRow = null;
            _selfIndex = -1;
            RankInfo selfInfo = null;
            for (int index = 0; index < _rows.Count; index++)
            {
                RankInfo info = index < infos.Count ? infos[index] : null;
                _rows[index].Apply(
                    info,
                    manager.Group);
                if (info?.IsSelf != true) continue;
                _selfRow = _rows[index];
                _selfIndex = index;
                selfInfo = info;
            }
            ApplyFloatingSelf(selfInfo, manager.Group);
            if (countdownText != null)
                countdownText.text = RankPresentationContract.FormatHms(
                    manager.RemainingSeconds);
            RefreshText();
            Canvas.ForceUpdateCanvases();
            if (rowList != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rowList);
            SyncFloatingSelf();
            if (_playIntroPending)
            {
                _playIntroPending = false;
                PlayIntro();
            }
        }

        private void LateUpdate()
        {
            if (IsShowing) SyncFloatingSelf();
        }

        private void ApplyFloatingSelf(RankInfo info, int group)
        {
            if (info == null || floatingRowLayer == null || rowPrefab == null)
            {
                if (_floatingSelfRow != null)
                    _floatingSelfRow.gameObject.SetActive(false);
                return;
            }
            if (_floatingSelfRow == null)
            {
                _floatingSelfRow = Instantiate(rowPrefab, floatingRowLayer);
                _floatingSelfRow.name = "FloatingSelfRow";
                _floatingSelfRow.SelfRequested += OpenSelfProfile;
                _floatingSelfRow.transform.SetAsLastSibling();
            }
            _floatingSelfRow.Apply(info, group);
            _floatingSelfRow.gameObject.SetActive(true);
        }

        private void SyncFloatingSelf()
        {
            if (_selfRow == null || _floatingSelfRow == null ||
                !_floatingSelfRow.gameObject.activeSelf || scroll?.viewport == null)
            {
                _floatingAtTop = false;
                _floatingAtBottom = false;
                _floatingSelfRow?.SetSelfShadow(false);
                return;
            }

            RectTransform source = (RectTransform)_selfRow.transform;
            RectTransform floating = (RectTransform)_floatingSelfRow.transform;
            scroll.viewport.GetWorldCorners(_viewportCorners);
            source.GetWorldCorners(_rowCorners);
            float halfHeight = Mathf.Abs(_rowCorners[1].y - _rowCorners[0].y) * 0.5f;
            float low = _viewportCorners[0].y + halfHeight;
            float high = _viewportCorners[1].y - halfHeight;
            Vector3 position = source.position;
            _floatingAtTop = low <= high && position.y > high;
            _floatingAtBottom = low <= high && position.y < low;
            position.y = low <= high
                ? Mathf.Clamp(position.y, low, high)
                : (low + high) * 0.5f;
            floating.position = position;
            _floatingSelfRow.SetSelfShadow(
                _floatingAtTop || _floatingAtBottom,
                _floatingAtTop);
        }

        private void PlayIntro()
        {
            int visibleIndex = 0;
            for (int index = 0; index < _rows.Count; index++)
            {
                RankActivityRowView row = _rows[index];
                if (row == null || !row.gameObject.activeSelf) continue;
                row.PlayIntro(
                    RankActivityRowIntro.Appear1,
                    0.06f + 0.05f * visibleIndex);
                visibleIndex++;
            }

            if (_floatingSelfRow == null ||
                !_floatingSelfRow.gameObject.activeSelf)
                return;
            if (_floatingAtTop || _floatingAtBottom)
            {
                _floatingSelfRow.PlayIntro(
                    RankActivityRowIntro.Appear2,
                    0.3f);
                return;
            }
            float selfDelay = _selfIndex >= 0
                ? 0.06f + 0.05f * _selfIndex
                : 0.3f;
            _floatingSelfRow.PlayIntro(
                RankActivityRowIntro.Appear1,
                selfDelay);
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
