using System;
using Meowdoku.Core.Config;

namespace Meowdoku.Core.Ads
{
    public enum BannerBlockReason
    {
        None,
        AdsDisabled,
        SessionLocked,
        LevelLocked,
        ExtraProtection,
        SizeLocked,
        ProviderUnavailable
    }

    public readonly struct BannerContext
    {
        public BannerContext(
            bool hasLevel,
            int level,
            int boardSize,
            int sessionActiveSeconds = 0,
            long firstOpenUnixMilliseconds = 0,
            long nowUnixMilliseconds = 0,
            int segmentIndex = -1,
            int segmentCount = 0)
        {
            HasLevel = hasLevel;
            Level = level;
            BoardSize = boardSize;
            SessionActiveSeconds = sessionActiveSeconds;
            FirstOpenUnixMilliseconds = firstOpenUnixMilliseconds;
            NowUnixMilliseconds = nowUnixMilliseconds;
            SegmentIndex = segmentIndex;
            SegmentCount = segmentCount;
        }

        public bool HasLevel { get; }
        public int Level { get; }
        public int BoardSize { get; }
        public int SessionActiveSeconds { get; }
        public long FirstOpenUnixMilliseconds { get; }
        public long NowUnixMilliseconds { get; }
        public int SegmentIndex { get; }
        public int SegmentCount { get; }
    }

    public readonly struct BannerPolicyResult
    {
        public BannerPolicyResult(bool shown, BannerBlockReason reason)
        {
            Shown = shown;
            Reason = reason;
        }

        public bool Shown { get; }
        public BannerBlockReason Reason { get; }
    }

    /// <summary>
    /// Port of BaseGamePage._eval_start_banner and
    /// _show_banner_if_eligible. Unlock is durable once both source gates pass.
    /// </summary>
    public sealed class BannerPolicy
    {
        public const int SourceBannerHeight = 180;

        private readonly GameStateService _state;
        private readonly AdService _ads;
        private readonly BannerUnlockSessionConfig _session;
        private readonly BannerUnlockLevelConfig _level;
        private readonly BannerExtraProtectLcConfig _protection;
        private readonly BannerUnlockDiffLcConfig _difficulty;

        public BannerPolicy(
            GameStateService state,
            AdService ads,
            BannerUnlockSessionConfig session = null,
            BannerUnlockLevelConfig level = null,
            BannerExtraProtectLcConfig protection = null,
            BannerUnlockDiffLcConfig difficulty = null)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _ads = ads;
            _session = session ?? new BannerUnlockSessionConfig();
            _level = level ?? new BannerUnlockLevelConfig();
            _protection = protection ?? new BannerExtraProtectLcConfig();
            _difficulty = difficulty ?? new BannerUnlockDiffLcConfig();
        }

        public BannerPolicyResult TryShow(
            string position,
            BannerContext context,
            bool adsEnabled = true,
            int downAdaptHeight = 0)
        {
            if (!adsEnabled)
                return Blocked(BannerBlockReason.AdsDisabled);

            if (!_state.BannerUnlocked)
            {
                bool sessionUnlocked = _session.IsDebugDisabled ||
                    _session.IsUnlockedAt(_state.Data.SessionCount);
                if (!sessionUnlocked)
                    return Blocked(BannerBlockReason.SessionLocked);

                bool levelUnlocked = !context.HasLevel ||
                    _level.IsDebugDisabled ||
                    _level.IsUnlockedAt(context.Level);
                if (!levelUnlocked)
                    return Blocked(BannerBlockReason.LevelLocked);

                if (context.HasLevel && sessionUnlocked && levelUnlocked)
                    _state.MarkBannerUnlocked();
            }

            var protectionContext = new InterstitialContext(
                false,
                0,
                context.SessionActiveSeconds,
                context.FirstOpenUnixMilliseconds,
                context.NowUnixMilliseconds,
                context.SegmentIndex,
                context.SegmentCount);
            if (!_protection.IsDebugDisabled &&
                AdProtectionEvaluator.IsProtected(
                    _state,
                    _protection.GetScheme(
                        context.SegmentIndex,
                        context.SegmentCount),
                    protectionContext))
                return Blocked(BannerBlockReason.ExtraProtection);

            if (!_difficulty.IsDebugDisabled &&
                !_difficulty.IsUnlockedForSize(
                    context.BoardSize,
                    context.SegmentIndex,
                    context.SegmentCount))
                return Blocked(BannerBlockReason.SizeLocked);

            if (_ads == null || !_ads.TryShowBanner(
                    position,
                    true,
                    0,
                    SourceBannerHeight + Math.Max(0, downAdaptHeight)))
                return Blocked(BannerBlockReason.ProviderUnavailable);

            return new BannerPolicyResult(true, BannerBlockReason.None);
        }

        private static BannerPolicyResult Blocked(BannerBlockReason reason) =>
            new(false, reason);
    }
}
