using System;

namespace Meowdoku.Core.UI
{
    // Values mirror scripts/module/ui/ui_name.gd. The enum is intentionally
    // stable because registry assets serialize it by integer.
    public enum UiName
    {
        Splash = 0,
        Home = 1,
        Game = 2,
        Bank = 3,
        Tutorial = 4,
        Setting = 5,
        Language = 6,
        HowToPlay = 7,
        HowToPlayPaged = 8,
        DailyGame = 9,
        Feedback = 10,
        RateUs = 11,
        RateUsV2 = 12,
        Privacy = 13,
        PreAttGuide = 14,
        PreAttGuideV2 = 15,
        PrePushGuide = 16,
        Confirm = 17,
        Win = 18,
        DailyWin = 19,
        Fail = 20,
        DailyFail = 21,
        AdRewardRestored = 22,
        Award = 23,
        Streak = 24,
        StreakResume = 25,
        StreakBackfill = 26,
        AbSwitchPopup = 27,
        RankActivityOpenPopup = 28,
        RankActivityHowToPlay = 29,
        RankActivityPage = 30,
        RankActivityChange = 31,
        Profile = 32,
        Debug = 33,
        Generator = 34,
        AbDebug = 35,
        LevelJsonInput = 36,
        MockAd = 37,
        MockBanner = 38
    }

    public enum UiLayer
    {
        Default = 0,
        Popup = 100,
        Notice = 200,
        Modal = 300,
        Tutorial = 400,
        Loading = 500
    }

    public enum UiWindowState
    {
        Invalid = 0,
        Creating = 1,
        Showing = 2,
        Hidden = 3,
        Closing = 4,
        Destroyed = 5
    }

    public static class UiLayerConfig
    {
        public const int ZStep = 50;
        public const int ZMax = 4000;
        // Godot keeps CanvasItem layers in separate ordering domains. Unity
        // override-sorting Canvases share one global integer domain, so the
        // serialized source layer values (0, 100, 200...) need disjoint
        // runtime ranges or a repeatedly reopened Default page can cover a
        // Popup page.
        public const int RuntimeLayerStride = 5000;
        public const int LocalOverlayOffset = ZStep - 1;

        public static int SortingBase(UiLayer layer)
        {
            return ((int)layer / 100) * RuntimeLayerStride;
        }
    }

    /// <summary>
    /// Pure startup timing and routing rules ported from launcher.gd.
    /// This belongs to the shared UI contract assembly surface so EditMode
    /// fixtures do not depend on the scene-owned AppBootstrap component.
    /// </summary>
    public static class AppStartupContract
    {
        public const float ExternalWaitMaximumSeconds = 2f;
        public const float MinimumSplashSeconds = 2f;
        public const float SplashCompletionPaddingSeconds = 0.5f;

        public static float SplashWaitRemaining(float elapsedSeconds)
        {
            return elapsedSeconds >= MinimumSplashSeconds
                ? SplashCompletionPaddingSeconds
                : MinimumSplashSeconds - Math.Max(0f, elapsedSeconds) +
                  SplashCompletionPaddingSeconds;
        }

        public static UiName InitialRoute(bool tutorialDone)
        {
            return tutorialDone ? UiName.Home : UiName.Tutorial;
        }
    }

    public sealed class UIEvents
    {
        public event Action<UiName, UIFrameWindow> WindowCreated;
        public event Action<UiName, UIFrameWindow> WindowShown;
        public event Action<UiName, UIFrameWindow> WindowHidden;

        internal void RaiseCreated(UiName name, UIFrameWindow window) =>
            WindowCreated?.Invoke(name, window);

        internal void RaiseShown(UiName name, UIFrameWindow window) =>
            WindowShown?.Invoke(name, window);

        internal void RaiseHidden(UiName name, UIFrameWindow window) =>
            WindowHidden?.Invoke(name, window);

        internal void Clear()
        {
            WindowCreated = null;
            WindowShown = null;
            WindowHidden = null;
        }
    }
}
