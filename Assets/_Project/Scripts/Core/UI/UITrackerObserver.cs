using System;
using Meowdoku.Core.Tracking;

namespace Meowdoku.Core.UI
{
    /// <summary>
    /// Direct adapter for ui_tracker_observer.gd. It observes successful
    /// window shows and leaves pages responsible only for their source
    /// screen/dialog metadata.
    /// </summary>
    internal sealed class UITrackerObserver : IDisposable
    {
        private readonly UIEvents _events;
        private readonly TrackingRuntime _runtime;

        public UITrackerObserver(UIEvents events, TrackingRuntime runtime)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _runtime = runtime;
            _events.WindowShown += OnWindowShown;
        }

        public void Dispose()
        {
            _events.WindowShown -= OnWindowShown;
        }

        private void OnWindowShown(UiName _, UIFrameWindow window)
        {
            if (window == null || _runtime == null) return;
            string dialog = window.GetTrackingDialogName();
            if (!string.IsNullOrEmpty(dialog))
            {
                _runtime.Tracker.TrackDialogShown(
                    dialog,
                    extra: window.GetTrackingDialogExtra());
                return;
            }

            string screen = window.GetTrackingScreenName();
            if (!string.IsNullOrEmpty(screen))
                _runtime.Tracker.TrackScreenShown(screen);
        }
    }
}
