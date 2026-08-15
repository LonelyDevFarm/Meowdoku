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
        private RankActivityRepository _repository;
        private bool _subscribed;
        private AbConfigRuntime _abConfigRuntime;
        private readonly LeaderboardFuncConfig _fallbackLeaderboard = new();

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

        public void BindAbConfigRuntime(AbConfigRuntime runtime)
        {
            _abConfigRuntime = runtime;
        }

        public void ResetData()
        {
            Manager.ResetData();
        }

        internal void ConfigureForTests(RankActivityManager manager)
        {
            FlushPendingWrites();
            _repository = null;
            _manager = manager;
        }

        private void EnsureInitialized()
        {
            if (_manager != null) return;
            if (robotRuntime == null || profileRuntime == null ||
                dailyMetaRuntime == null)
                return;
            _repository = RankActivityRepository.CreateDefault();
            _manager = new RankActivityManager(
                _repository,
                robotRuntime.Service,
                profileRuntime.Service,
                dailyMetaRuntime.Awards,
                new RuntimeEnvironment(this));
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) FlushPendingWrites();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) FlushPendingWrites();
        }

        private void OnDestroy()
        {
            FlushPendingWrites();
        }

        private void FlushPendingWrites()
        {
            _repository?.FlushPendingWrites();
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
            private readonly RankActivityRuntime _owner;

            public RuntimeEnvironment(RankActivityRuntime owner)
            {
                _owner = owner;
            }

            public bool LeaderboardEnabled =>
                _owner.CurrentLeaderboardConfig.IsEnabled();
            public int LeaderboardGroup =>
                _owner.CurrentLeaderboardConfig.GetGroup();
            public int CurrentLevel => GameStateRuntime.Current.CurrentLevel;
        }

        private LeaderboardFuncConfig CurrentLeaderboardConfig =>
            _abConfigRuntime != null
                ? _abConfigRuntime.Home.Leaderboard
                : _fallbackLeaderboard;
    }
}
