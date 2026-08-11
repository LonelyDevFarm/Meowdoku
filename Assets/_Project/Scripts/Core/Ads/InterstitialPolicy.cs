using System;
using Meowdoku.Core.Config;

namespace Meowdoku.Core.Ads
{
    public enum InterstitialBlockReason
    {
        None,
        RewardViewProbability,
        EndgameRestore,
        AdsDisabled,
        LevelLocked,
        SessionLocked,
        MemoryLocked,
        ExtraProtection,
        Cooldown,
        NotReady
    }

    public readonly struct InterstitialContext
    {
        public InterstitialContext(
            bool endgameRestore,
            int physicalMemoryMb,
            int sessionActiveSeconds = 0,
            long firstOpenUnixMilliseconds = 0,
            long nowUnixMilliseconds = 0,
            int segmentIndex = -1,
            int segmentCount = 0)
        {
            EndgameRestore = endgameRestore;
            PhysicalMemoryMb = physicalMemoryMb;
            SessionActiveSeconds = sessionActiveSeconds;
            FirstOpenUnixMilliseconds = firstOpenUnixMilliseconds;
            NowUnixMilliseconds = nowUnixMilliseconds;
            SegmentIndex = segmentIndex;
            SegmentCount = segmentCount;
        }

        public bool EndgameRestore { get; }
        public int PhysicalMemoryMb { get; }
        public int SessionActiveSeconds { get; }
        public long FirstOpenUnixMilliseconds { get; }
        public long NowUnixMilliseconds { get; }
        public int SegmentIndex { get; }
        public int SegmentCount { get; }
    }

    public readonly struct InterstitialPolicyResult
    {
        public InterstitialPolicyResult(
            bool shown,
            InterstitialBlockReason reason)
        {
            Shown = shown;
            Reason = reason;
        }

        public bool Shown { get; }
        public InterstitialBlockReason Reason { get; }
    }

    public interface IAdRandom
    {
        int Range(int minimumInclusive, int maximumExclusive);
    }

    public sealed class SystemAdRandom : IAdRandom
    {
        public static readonly SystemAdRandom Instance = new();
        private readonly Random _random = new();
        private SystemAdRandom() { }
        public int Range(int minimumInclusive, int maximumExclusive) =>
            _random.Next(minimumInclusive, maximumExclusive);
    }

    /// <summary>
    /// Port of BaseGamePage._compute_start_interstitial. It preserves source
    /// gate ordering so rejected entries do not mutate later-gate state.
    /// </summary>
    public sealed class InterstitialPolicy
    {
        private readonly GameStateService _state;
        private readonly AdService _ads;
        private readonly IAdRandom _random;
        private readonly InterUnlockLevelConfig _level;
        private readonly InterUnlockSessionConfig _session;
        private readonly InterUnlockMemoryConfig _memory;
        private readonly InterCdLcConfig _cooldown;
        private readonly InterExtraProtectLcConfig _protection;

        public InterstitialPolicy(
            GameStateService state,
            AdService ads,
            IAdRandom random = null,
            InterUnlockLevelConfig level = null,
            InterUnlockSessionConfig session = null,
            InterUnlockMemoryConfig memory = null,
            InterCdLcConfig cooldown = null,
            InterExtraProtectLcConfig protection = null)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _ads = ads;
            _random = random ?? SystemAdRandom.Instance;
            _level = level ?? new InterUnlockLevelConfig();
            _session = session ?? new InterUnlockSessionConfig();
            _memory = memory ?? new InterUnlockMemoryConfig();
            _cooldown = cooldown ?? new InterCdLcConfig();
            _protection = protection ?? new InterExtraProtectLcConfig();
        }

        public InterstitialPolicyResult TryShow(
            string position,
            InterstitialContext context,
            bool adsEnabled = true)
        {
            if (_state.SessionRewardViewCount >= 1)
            {
                _state.ResetSessionRewardViewCount();
                if (_random.Range(0, 100) >= 80)
                    return Blocked(InterstitialBlockReason.RewardViewProbability);
            }
            if (context.EndgameRestore)
                return Blocked(InterstitialBlockReason.EndgameRestore);
            if (!adsEnabled)
                return Blocked(InterstitialBlockReason.AdsDisabled);

            if (!_state.InterstitialUnlocked)
            {
                bool levelUnlocked = _level.IsDebugDisabled ||
                                     _level.IsUnlockedAt(_state.CurrentLevel);
                if (!levelUnlocked)
                    return Blocked(InterstitialBlockReason.LevelLocked);
                bool sessionUnlocked = _session.IsDebugDisabled ||
                    _session.IsUnlockedAt(_state.Data.SessionCount);
                if (!sessionUnlocked)
                    return Blocked(InterstitialBlockReason.SessionLocked);
                _state.MarkInterstitialUnlocked();
            }

            if (!_memory.IsDebugDisabled &&
                !_memory.IsUnlockedForDevice(context.PhysicalMemoryMb))
                return Blocked(InterstitialBlockReason.MemoryLocked);

            if (!_protection.IsDebugDisabled &&
                AdProtectionEvaluator.IsProtected(
                    _state,
                    _protection.GetScheme(
                        context.SegmentIndex,
                        context.SegmentCount),
                    context))
                return Blocked(InterstitialBlockReason.ExtraProtection);

            if (_ads != null &&
                _ads.IsInterstitialInCooldown(_cooldown.GetSeconds(
                    context.SegmentIndex,
                    context.SegmentCount)))
                return Blocked(InterstitialBlockReason.Cooldown);
            if (_ads == null || !_ads.TryShowInterstitial(position))
                return Blocked(InterstitialBlockReason.NotReady);
            return new InterstitialPolicyResult(true, InterstitialBlockReason.None);
        }

        private static InterstitialPolicyResult Blocked(
            InterstitialBlockReason reason) => new(false, reason);
    }

    internal static class AdProtectionEvaluator
    {
        public static bool IsProtected(
            GameStateService state,
            string scheme,
            InterstitialContext context)
        {
            if (string.IsNullOrEmpty(scheme) || scheme == "no") return false;
            if (TryThreshold(scheme, "session_game_", out int n))
                return state.SessionPlayedCount < n - 1;
            if (TryThreshold(scheme, "day_game_", out n))
                return state.Data.TodayPlayedCount < n - 1;
            if (TryThreshold(scheme, "day_min_", out n))
                return state.Data.TodayActiveSeconds < n * 60;
            if (TryThreshold(scheme, "session_min_", out n))
                return context.SessionActiveSeconds < n * 60;
            if (TryThreshold(scheme, "first_day_", out n))
            {
                if (context.FirstOpenUnixMilliseconds <= 0) return false;
                DateTime firstDay = DateTimeOffset
                    .FromUnixTimeMilliseconds(context.FirstOpenUnixMilliseconds)
                    .LocalDateTime.Date;
                DateTime today = (context.NowUnixMilliseconds > 0
                        ? DateTimeOffset.FromUnixTimeMilliseconds(
                            context.NowUnixMilliseconds)
                        : DateTimeOffset.Now)
                    .LocalDateTime.Date;
                return (today - firstDay).Days < n - 1;
            }
            return false;
        }

        private static bool TryThreshold(
            string scheme,
            string prefix,
            out int value)
        {
            value = -1;
            return scheme.StartsWith(prefix, StringComparison.Ordinal) &&
                   int.TryParse(scheme.Substring(prefix.Length), out value);
        }

    }
}
