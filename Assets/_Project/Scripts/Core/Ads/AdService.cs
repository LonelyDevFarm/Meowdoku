using System;
using System.Collections.Generic;
using Meowdoku.Core.Config;
using Meowdoku.Core.Tracking;

namespace Meowdoku.Core.Ads
{
    public readonly struct AdImpression
    {
        public AdImpression(string placementId, string position = "")
        {
            PlacementId = placementId ?? string.Empty;
            Position = position ?? string.Empty;
        }

        public string PlacementId { get; }
        public string Position { get; }
    }

    public interface IAdProvider
    {
        bool IsAvailable { get; }
        event Action<string> AdShown;
        event Action<string> AdClosed;
        event Action<string> AdRewarded;
        event Action<string, string> AdError;
        event Action<AdImpression> AdImpression;
        string CreateShowId();
        bool IsReady(string placementId, string position, string showId);
        bool IsValid(string placementId, string position);
        void Show(string placementId, string position, string showId);
        void ShowBanner(
            string placementId,
            string position,
            bool anchorBottom,
            int offsetBase,
            int heightBase);
        void Destroy(string placementId);
    }

    public sealed class NullAdProvider : IAdProvider
    {
        public static readonly NullAdProvider Instance = new();
        private NullAdProvider() { }
        public bool IsAvailable => false;
        public event Action<string> AdShown { add { } remove { } }
        public event Action<string> AdClosed { add { } remove { } }
        public event Action<string> AdRewarded { add { } remove { } }
        public event Action<string, string> AdError { add { } remove { } }
        public event Action<AdImpression> AdImpression { add { } remove { } }
        public string CreateShowId() => string.Empty;
        public bool IsReady(string placementId, string position, string showId) => false;
        public bool IsValid(string placementId, string position) => false;
        public void Show(string placementId, string position, string showId) { }
        public void ShowBanner(
            string placementId,
            string position,
            bool anchorBottom,
            int offsetBase,
            int heightBase) { }
        public void Destroy(string placementId) { }
    }

    /// <summary>
    /// Provider-neutral port of UniKitManager's ad boundary. Provider callbacks
    /// remain distinct: closing an ad never grants a reward, and impression is
    /// the only point that records a completed ad show.
    /// </summary>
    public sealed class AdService : IDisposable
    {
        public const int RewardGrantTimeoutSeconds = 30;

        public interface IClock
        {
            long UnixNow { get; }
        }

        private sealed class SystemClock : IClock
        {
            public static readonly SystemClock Instance = new();
            private SystemClock() { }
            public long UnixNow => DateTimeOffset.Now.ToUnixTimeSeconds();
        }

        private sealed class RewardWatchdog
        {
            public string ShowId;
            public string Position;
            public long StartedUnix;
            public long DueUnix;
        }

        private readonly IAdProvider _provider;
        private readonly TrackerService _tracker;
        private readonly GameStateService _gameState;
        private readonly IClock _clock;
        private readonly Func<int> _sessionActiveSeconds;
        private readonly CommonRewardAdLogicConfig _rewardRestoreConfig;
        private readonly Dictionary<string, string> _pendingPositions = new();
        private readonly List<RewardWatchdog> _rewardWatchdogs = new();
        private Action<bool> _rewardCompletion;
        private bool _rewardRequestActive;
        private bool _rewardShowSessionActive;
        private bool _rewardShown;
        private bool _rewardReceived;
        private string _rewardActiveShowId = string.Empty;
        private string _rewardActivePosition = string.Empty;
        private bool _bannerActive;
        private long _lastAdCloseUnix;
        private bool _disposed;

        public AdService(
            GameStateService gameState,
            TrackerService tracker,
            IAdProvider provider = null,
            IClock clock = null,
            Func<int> sessionActiveSeconds = null,
            CommonRewardAdLogicConfig rewardRestoreConfig = null)
        {
            _gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
            _tracker = tracker;
            _provider = provider ?? NullAdProvider.Instance;
            _clock = clock ?? SystemClock.Instance;
            _sessionActiveSeconds = sessionActiveSeconds;
            _rewardRestoreConfig =
                rewardRestoreConfig ?? new CommonRewardAdLogicConfig();
            _provider.AdShown += HandleShown;
            _provider.AdClosed += HandleClosed;
            _provider.AdRewarded += HandleRewarded;
            _provider.AdError += HandleError;
            _provider.AdImpression += HandleImpression;
        }

        public event Action<string> AdShown;
        public event Action<string> AdClosed;
        public event Action<string> AdRewarded;
        public event Action<string, string> AdError;
        public event Action<AdImpression> AdImpression;

        public bool IsAvailable => !_disposed && _provider.IsAvailable;
        public bool IsRewardRequestActive => _rewardRequestActive;
        public long LastAdCloseUnix => _lastAdCloseUnix;
        public int SessionActiveSeconds => Math.Max(
            0,
            _sessionActiveSeconds?.Invoke() ?? 0);
        public int PendingRewardWatchdogCount => _rewardWatchdogs.Count;
        public bool IsBannerActive => _bannerActive;

        public bool IsInterstitialInCooldown(int seconds) =>
            seconds > 0 && _clock.UnixNow - _lastAdCloseUnix < seconds;

        public bool IsValid(string placementId, string position) =>
            IsAvailable && _provider.IsValid(
                placementId ?? string.Empty,
                position ?? string.Empty);

        public string GenerateShowId() => IsAvailable
            ? _provider.CreateShowId() ?? string.Empty
            : string.Empty;

        public bool IsReady(
            string placementId,
            string position,
            string showId)
        {
            if (!IsAvailable || string.IsNullOrEmpty(placementId) ||
                string.IsNullOrEmpty(showId))
                return false;

            string safePosition = position ?? string.Empty;
            _tracker?.TrackAdShowTiming(
                showId,
                placementId,
                placementId,
                safePosition);
            _tracker?.RememberAdShowId(placementId, showId);
            _pendingPositions[placementId] = safePosition;
            return _provider.IsReady(placementId, safePosition, showId);
        }

        public bool TryShowInterstitial(string position)
        {
            string showId = GenerateShowId();
            if (!IsReady(
                    TrackerCatalog.Placement.Interstitial,
                    position,
                    showId))
                return false;
            _provider.Show(
                TrackerCatalog.Placement.Interstitial,
                position ?? string.Empty,
                showId);
            return true;
        }

        public bool TryShowReward(
            string position,
            Action<bool> completed)
        {
            if (_rewardRequestActive) return false;
            string showId = GenerateShowId();
            if (!IsReady(
                    TrackerCatalog.Placement.Reward,
                    position,
                    showId))
                return false;

            _rewardRequestActive = true;
            _rewardShowSessionActive = true;
            _rewardShown = false;
            _rewardReceived = false;
            _rewardActiveShowId = showId;
            _rewardActivePosition = position ?? string.Empty;
            _rewardCompletion = completed;
            try
            {
                _provider.Show(
                    TrackerCatalog.Placement.Reward,
                    position ?? string.Empty,
                    showId);
                return true;
            }
            catch (Exception)
            {
                CompleteReward(false);
                return false;
            }
        }

        public bool TryShowBanner(
            string position,
            bool anchorBottom = true,
            int offsetBase = 0,
            int heightBase = 180)
        {
            if (!IsAvailable) return false;
            _provider.ShowBanner(
                TrackerCatalog.Placement.Banner,
                position ?? string.Empty,
                anchorBottom,
                offsetBase,
                Math.Max(0, heightBase));
            _bannerActive = true;
            return true;
        }

        public void DestroyBanner()
        {
            if (!_bannerActive) return;
            _bannerActive = false;
            if (IsAvailable)
                _provider.Destroy(TrackerCatalog.Placement.Banner);
        }

        public void Tick()
        {
            if (_disposed || _rewardWatchdogs.Count == 0) return;
            long now = _clock.UnixNow;
            for (int index = _rewardWatchdogs.Count - 1;
                 index >= 0;
                 index--)
            {
                RewardWatchdog watchdog = _rewardWatchdogs[index];
                if (now < watchdog.DueUnix) continue;
                _rewardWatchdogs.RemoveAt(index);
                if (!_rewardRestoreConfig.ShouldGrantRewardRestore())
                    continue;
                _gameState.AddPendingReward(
                    new Dictionary<string, object>
                    {
                        ["show_id"] = watchdog.ShowId,
                        ["source"] = watchdog.Position,
                        ["ts"] = now
                    });
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            DestroyBanner();
            _disposed = true;
            _provider.AdShown -= HandleShown;
            _provider.AdClosed -= HandleClosed;
            _provider.AdRewarded -= HandleRewarded;
            _provider.AdError -= HandleError;
            _provider.AdImpression -= HandleImpression;
            CompleteReward(false);
            ResetRewardSession();
            _rewardWatchdogs.Clear();
            _pendingPositions.Clear();
            AdShown = null;
            AdClosed = null;
            AdRewarded = null;
            AdError = null;
            AdImpression = null;
        }

        private void HandleShown(string placementId)
        {
            if (_disposed) return;
            AdShown?.Invoke(placementId ?? string.Empty);
            if (placementId == TrackerCatalog.Placement.Reward &&
                _rewardShowSessionActive)
                _rewardShown = true;
        }

        private void HandleClosed(string placementId)
        {
            if (_disposed) return;
            string safePlacement = placementId ?? string.Empty;
            _lastAdCloseUnix = _clock.UnixNow;
            AdClosed?.Invoke(safePlacement);
            if (safePlacement == TrackerCatalog.Placement.Reward)
            {
                if (_rewardShowSessionActive && _rewardShown)
                    _gameState.IncrementSessionRewardViewCount();
                MaybeStartRewardGrantWatchdog();
                CompleteReward(false);
            }
        }

        private void HandleRewarded(string placementId)
        {
            if (_disposed) return;
            string safePlacement = placementId ?? string.Empty;
            AdRewarded?.Invoke(safePlacement);
            if (safePlacement == TrackerCatalog.Placement.Reward)
            {
                MarkRewardReceived();
                _gameState.RecordNormalReward(_clock.UnixNow);
                CompleteReward(true);
            }
        }

        private void HandleError(string placementId, string message)
        {
            if (_disposed) return;
            string safePlacement = placementId ?? string.Empty;
            AdError?.Invoke(safePlacement, message ?? string.Empty);
            if (safePlacement == TrackerCatalog.Placement.Reward)
                CompleteReward(false);
        }

        private void HandleImpression(AdImpression impression)
        {
            if (_disposed || string.IsNullOrEmpty(impression.PlacementId))
                return;
            string showId = _tracker?.ConsumeAdShowId(
                impression.PlacementId) ?? string.Empty;
            string position = _pendingPositions.TryGetValue(
                impression.PlacementId,
                out string pending)
                ? pending
                : impression.Position;
            _pendingPositions.Remove(impression.PlacementId);
            if (impression.PlacementId == TrackerCatalog.Placement.Interstitial)
                _tracker?.TrackInterstitialAdShow(
                    showId,
                    _gameState.CurrentLevel,
                    position);
            else if (impression.PlacementId == TrackerCatalog.Placement.Reward)
                _tracker?.TrackRewardedAdShow(
                    showId,
                    _gameState.CurrentLevel,
                    position);
            AdImpression?.Invoke(new AdImpression(
                impression.PlacementId,
                position));
        }

        private void CompleteReward(bool rewarded)
        {
            Action<bool> completion = _rewardCompletion;
            _rewardCompletion = null;
            _rewardRequestActive = false;
            completion?.Invoke(rewarded);
        }

        private void MarkRewardReceived()
        {
            if (!string.IsNullOrEmpty(_rewardActiveShowId))
            {
                _rewardReceived = true;
                return;
            }
            if (_rewardWatchdogs.Count == 0) return;
            int earliest = 0;
            for (int index = 1; index < _rewardWatchdogs.Count; index++)
                if (_rewardWatchdogs[index].StartedUnix <
                    _rewardWatchdogs[earliest].StartedUnix)
                    earliest = index;
            _rewardWatchdogs.RemoveAt(earliest);
        }

        private void MaybeStartRewardGrantWatchdog()
        {
            if (string.IsNullOrEmpty(_rewardActiveShowId))
                return;
            if (_rewardReceived || !_rewardShown)
            {
                ResetRewardSession();
                return;
            }
            long now = _clock.UnixNow;
            _rewardWatchdogs.Add(new RewardWatchdog
            {
                ShowId = _rewardActiveShowId,
                Position = _rewardActivePosition,
                StartedUnix = now,
                DueUnix = now + RewardGrantTimeoutSeconds
            });
            ResetRewardSession();
        }

        private void ResetRewardSession()
        {
            _rewardShowSessionActive = false;
            _rewardShown = false;
            _rewardReceived = false;
            _rewardActiveShowId = string.Empty;
            _rewardActivePosition = string.Empty;
        }
    }
}
