using Meowdoku.Core.UI;
using UnityEngine;

namespace Meowdoku.Core.Daily
{
    public interface IDailyMetaConsumer
    {
        void BindDailyMetaRuntime(DailyMetaRuntime runtime);
    }

    /// <summary>
    /// Scene-owned composition root for offline Daily meta systems. It keeps
    /// persistence and cold-start award sweep out of UI presenters.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DailyMetaRuntime : MonoBehaviour
    {
        [SerializeField] private ClockTicker clockTicker;
        [SerializeField] private MonoBehaviour frameAwardSink;
        [SerializeField] private UIManager uiManager;

        private StreakFeature _streak;
        private AwardManager _awards;
        private bool _subscribed;
        private bool _awardSubscribed;

        public StreakFeature Streak
        {
            get
            {
                EnsureInitialized();
                return _streak;
            }
        }

        public AwardManager Awards
        {
            get
            {
                EnsureInitialized();
                return _awards;
            }
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            SubscribeClock();
            SubscribeAwards();
        }

        private void OnDisable()
        {
            UnsubscribeClock();
            UnsubscribeAwards();
        }

        public void BindClockTicker(ClockTicker ticker)
        {
            if (clockTicker == ticker) return;
            UnsubscribeClock();
            clockTicker = ticker;
            SubscribeClock();
        }

        public void BindUiManager(UIManager manager)
        {
            uiManager = manager;
        }

        public void SettleWin(StreakCheckinSource source)
        {
            EnsureInitialized();
            _streak.SettleWin(
                source,
                GameStateRuntime.Current.TutorialDone);
        }

        internal void ConfigureForTests(
            StreakFeature streak,
            AwardManager awards)
        {
            UnsubscribeAwards();
            _streak = streak;
            _awards = awards;
            SubscribeAwards();
        }

        private void EnsureInitialized()
        {
            if (_awards == null)
                _awards = new AwardManager(
                    GameStateRuntime.Current,
                    frameAwardSink as IFrameAwardSink,
                    tracker: uiManager != null ? uiManager.Tracker : null);
            if (_streak == null)
                _streak = new StreakFeature(
                    StreakRepository.CreateDefault(),
                    rewardBoundary: _awards);
            SubscribeAwards();
        }

        private void SubscribeClock()
        {
            if (_subscribed || !isActiveAndEnabled || clockTicker == null)
                return;
            clockTicker.SecondTick += HandleSecondTick;
            _subscribed = true;
        }

        private void UnsubscribeClock()
        {
            if (!_subscribed) return;
            if (clockTicker != null)
                clockTicker.SecondTick -= HandleSecondTick;
            _subscribed = false;
        }

        private void HandleSecondTick()
        {
            _streak?.TickDayWatch();
        }

        private void SubscribeAwards()
        {
            if (_awardSubscribed || _awards == null ||
                !isActiveAndEnabled)
                return;
            _awards.AwardPresentationRequested += HandleAwardPresentation;
            _awardSubscribed = true;
        }

        private void UnsubscribeAwards()
        {
            if (!_awardSubscribed) return;
            if (_awards != null)
                _awards.AwardPresentationRequested -=
                    HandleAwardPresentation;
            _awardSubscribed = false;
        }

        private void HandleAwardPresentation(
            AwardPresentationRequest request)
        {
            if (request == null || uiManager == null) return;
            uiManager.Show(
                UiName.Award,
                new System.Collections.Generic.Dictionary<string, object>
                {
                    ["award_request"] = request
                });
        }
    }
}
