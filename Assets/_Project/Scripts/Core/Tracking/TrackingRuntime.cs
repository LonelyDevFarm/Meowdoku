using UnityEngine;

namespace Meowdoku.Core.Tracking
{
    [DisallowMultipleComponent]
    public sealed class TrackingRuntime : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour trackingSink;

        private TrackerService _tracker;
        private SessionService _session;
        private bool _backgrounded;
        private float _flushDeadline;

        public TrackerService Tracker
        {
            get
            {
                EnsureInitialized();
                return _tracker;
            }
        }

        public SessionService Session
        {
            get
            {
                EnsureInitialized();
                return _session;
            }
        }

        private void Awake()
        {
            EnsureInitialized();
            ScheduleFlush();
        }

        private void Update()
        {
            if (_backgrounded ||
                Time.realtimeSinceStartup < _flushDeadline)
                return;
            _session.FlushActiveSegment();
            ScheduleFlush();
        }

        private void OnApplicationFocus(bool focused)
        {
            SetBackgrounded(!focused);
        }

        private void OnApplicationPause(bool paused)
        {
            SetBackgrounded(paused);
        }

        private void OnDestroy()
        {
            if (!_backgrounded) _session?.FlushActiveSegment();
        }

        public void BindSink(MonoBehaviour sink)
        {
            if (trackingSink == sink) return;
            trackingSink = sink;
            _tracker = null;
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_tracker == null)
                _tracker = new TrackerService(
                    GameStateRuntime.Current,
                    trackingSink as ITrackingSink);
            if (_session == null)
            {
                // AppBootstrap already calls OnSessionStarted once during
                // RuntimeSetup. SessionService owns only later 30-minute
                // refreshes here, avoiding a duplicate startup count.
                _session = new SessionService(
                    GameStateRuntime.Current,
                    countInitialSession: false);
            }
        }

        private void SetBackgrounded(bool backgrounded)
        {
            if (_backgrounded == backgrounded) return;
            _backgrounded = backgrounded;
            EnsureInitialized();
            if (backgrounded)
                _session.OnFocusOut();
            else
            {
                _session.OnFocusIn();
                ScheduleFlush();
            }
        }

        private void ScheduleFlush()
        {
            _flushDeadline = Time.realtimeSinceStartup +
                SessionService.ActiveFlushIntervalSeconds;
        }
    }
}
