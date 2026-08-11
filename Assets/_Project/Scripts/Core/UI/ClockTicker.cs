using System;
using System.Globalization;
using UnityEngine;

namespace Meowdoku.Core.UI
{
    public interface IClockTickConsumer
    {
        void BindClockTicker(ClockTicker ticker);
    }

    internal interface ISystemClock
    {
        DateTime LocalNow { get; }
        double UnixSeconds { get; }
    }

    internal sealed class SystemClock : ISystemClock
    {
        public static readonly SystemClock Instance = new();

        private SystemClock() { }

        public DateTime LocalNow => DateTime.Now;
        public double UnixSeconds =>
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
    }

    public static class ClockTickerContract
    {
        private const double BoundaryGuardSeconds = 0.001;

        public static double SecondsUntilFirstTick(double unixSeconds)
        {
            if (double.IsNaN(unixSeconds) || double.IsInfinity(unixSeconds))
                return 1.0;

            double delay = Math.Ceiling(
                               unixSeconds + BoundaryGuardSeconds) -
                           unixSeconds;
            return Math.Max(BoundaryGuardSeconds, delay);
        }

        public static string LocalDateKey(DateTime localDateTime)
        {
            return localDateTime.ToString(
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Scene-owned equivalent of Godot's ClockTicker autoload. It emits one
    /// source-aligned tick per wall-clock second and emits at most once after
    /// a long application pause instead of replaying missed seconds.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ClockTicker : MonoBehaviour
    {
        private ISystemClock _clock = SystemClock.Instance;
        private double _nextRealtimeTick;

        public event Action SecondTick;

        public DateTime LocalNow => _clock.LocalNow;

        private void OnEnable()
        {
            ScheduleFirstTick();
        }

        private void Update()
        {
            double realtime = Time.realtimeSinceStartupAsDouble;
            if (realtime < _nextRealtimeTick)
                return;

            SecondTick?.Invoke();
            _nextRealtimeTick = realtime + 1.0;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && isActiveAndEnabled)
                ScheduleFirstTick();
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (!isPaused && isActiveAndEnabled)
                ScheduleFirstTick();
        }

        private void ScheduleFirstTick()
        {
            _nextRealtimeTick = Time.realtimeSinceStartupAsDouble +
                                ClockTickerContract.SecondsUntilFirstTick(
                                    _clock.UnixSeconds);
        }

        internal void ConfigureForTests(ISystemClock clock)
        {
            _clock = clock ?? SystemClock.Instance;
            if (isActiveAndEnabled)
                ScheduleFirstTick();
        }
    }
}
