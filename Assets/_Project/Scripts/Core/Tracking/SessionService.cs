using System;
using System.Diagnostics;

namespace Meowdoku.Core.Tracking
{
    public interface ITrackingClock
    {
        long UnixNow { get; }
        long MonotonicMilliseconds { get; }
    }

    public sealed class SystemTrackingClock : ITrackingClock
    {
        public static readonly SystemTrackingClock Instance = new();
        private SystemTrackingClock() { }
        public long UnixNow => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        public long MonotonicMilliseconds =>
            Stopwatch.GetTimestamp() * 1000L / Stopwatch.Frequency;
    }

    /// <summary>
    /// Pure application-session and active-time state ported from
    /// session_manager.gd. The Unity runtime owns focus notifications and
    /// periodic flushing; this class owns the deterministic policy.
    /// </summary>
    public sealed class SessionService
    {
        public const int SessionRefreshIntervalSeconds = 30 * 60;
        public const int ActiveFlushIntervalSeconds = 60;

        private readonly GameStateService _gameState;
        private readonly ITrackingClock _clock;
        private readonly ITrackingIdProvider _ids;
        private long _lastPauseUnix;
        private long _activeSegmentStartMilliseconds = -1;
        private int _sessionActiveSeconds;

        public SessionService(
            GameStateService gameState,
            bool countInitialSession = true,
            ITrackingClock clock = null,
            ITrackingIdProvider ids = null)
        {
            _gameState = gameState ??
                         throw new ArgumentNullException(nameof(gameState));
            _clock = clock ?? SystemTrackingClock.Instance;
            _ids = ids ?? GuidTrackingIdProvider.Instance;
            ResetSession();
            if (countInitialSession) _gameState.OnSessionStarted();
            _activeSegmentStartMilliseconds =
                _clock.MonotonicMilliseconds;
        }

        public event Action<string> SessionChanged;

        public string SessionId { get; private set; } = string.Empty;
        public int SessionRecord { get; private set; } = 1;

        public int TodayActiveSeconds
        {
            get
            {
                int persisted = _gameState.Data.TodayActiveSeconds;
                return persisted + CurrentSegmentSeconds();
            }
        }

        public int SessionActiveSeconds =>
            _sessionActiveSeconds + CurrentSegmentSeconds();

        public int FlushActiveSegment()
        {
            int elapsed = CurrentSegmentSeconds();
            if (elapsed <= 0) return 0;
            _gameState.AddActiveSeconds(elapsed);
            _sessionActiveSeconds += elapsed;
            _activeSegmentStartMilliseconds += elapsed * 1000L;
            return elapsed;
        }

        public void OnFocusOut()
        {
            _lastPauseUnix = _clock.UnixNow;
            FlushActiveSegment();
            _activeSegmentStartMilliseconds = -1;
        }

        public bool OnFocusIn()
        {
            _activeSegmentStartMilliseconds =
                _clock.MonotonicMilliseconds;
            if (_lastPauseUnix == 0) return false;
            long span = _clock.UnixNow - _lastPauseUnix;
            if (span > SessionRefreshIntervalSeconds)
            {
                ResetSession();
                _activeSegmentStartMilliseconds =
                    _clock.MonotonicMilliseconds;
                _gameState.OnSessionStarted();
                SessionChanged?.Invoke(SessionId);
                return true;
            }
            SessionRecord++;
            return false;
        }

        public void ForceNewSession()
        {
            FlushActiveSegment();
            ResetSession();
            _activeSegmentStartMilliseconds =
                _clock.MonotonicMilliseconds;
            _gameState.OnSessionStarted();
            SessionChanged?.Invoke(SessionId);
        }

        private void ResetSession()
        {
            SessionId = _ids.NewId();
            SessionRecord = 1;
            _lastPauseUnix = _clock.UnixNow;
            _sessionActiveSeconds = 0;
        }

        private int CurrentSegmentSeconds()
        {
            if (_activeSegmentStartMilliseconds < 0) return 0;
            long elapsed =
                _clock.MonotonicMilliseconds -
                _activeSegmentStartMilliseconds;
            return elapsed <= 0
                ? 0
                : (int)Math.Min(int.MaxValue, elapsed / 1000L);
        }
    }
}
