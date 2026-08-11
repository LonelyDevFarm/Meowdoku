using Meowdoku.Core.Config;
using Meowdoku.Core.Daily;
using Meowdoku.Core.Profile;
using Meowdoku.Core.Robot;
using Meowdoku.Core.UI;
using UnityEngine;

namespace Meowdoku.Core.Rank
{
    public interface IRankActivityConsumer
    {
        void BindRankActivityRuntime(RankActivityRuntime runtime);
    }

    /// <summary>
    /// Scene-owned composition root for RankActivity. Offline/default config
    /// remains disabled exactly like leaderboard_func=0 in the source.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RankActivityRuntime : MonoBehaviour
    {
        [SerializeField] private ClockTicker clockTicker;
        [SerializeField] private RobotRuntime robotRuntime;
        [SerializeField] private ProfileRuntime profileRuntime;
        [SerializeField] private DailyMetaRuntime dailyMetaRuntime;

        private RankActivityManager _manager;
        private bool _subscribed;

        public RankActivityManager Manager
        {
            get
            {
                EnsureInitialized();
                return _manager;
            }
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            SubscribeClock();
        }

        private void OnDisable()
        {
            UnsubscribeClock();
        }

        public void BindClockTicker(ClockTicker ticker)
        {
            if (clockTicker == ticker) return;
            UnsubscribeClock();
            clockTicker = ticker;
            SubscribeClock();
        }

        public void ResetData()
        {
            Manager.ResetData();
        }

        internal void ConfigureForTests(RankActivityManager manager)
        {
            _manager = manager;
        }

        private void EnsureInitialized()
        {
            if (_manager != null) return;
            if (robotRuntime == null || profileRuntime == null ||
                dailyMetaRuntime == null)
                return;
            var leaderboard = new LeaderboardFuncConfig();
            leaderboard.ReloadValue(DefaultAbValueProvider.Instance);
            _manager = new RankActivityManager(
                RankActivityRepository.CreateDefault(),
                robotRuntime.Service,
                profileRuntime.Service,
                dailyMetaRuntime.Awards,
                new RuntimeEnvironment(leaderboard));
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
            _manager?.Tick();
        }

        private sealed class RuntimeEnvironment : IRankActivityEnvironment
        {
            private readonly LeaderboardFuncConfig _config;

            public RuntimeEnvironment(LeaderboardFuncConfig config)
            {
                _config = config;
            }

            public bool LeaderboardEnabled => _config.IsEnabled();
            public int LeaderboardGroup => _config.GetGroup();
            public int CurrentLevel => GameStateRuntime.Current.CurrentLevel;
        }
    }
}
