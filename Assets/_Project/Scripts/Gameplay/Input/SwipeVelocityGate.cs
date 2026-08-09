using System;
using System.Collections.Generic;

namespace Meowdoku.Gameplay.Input
{
    /// <summary>
    /// Direct logic port of gameplay/core/swipe_velocity_gate.gd.
    /// </summary>
    public sealed class SwipeVelocityGate
    {
        private readonly List<Sample> _samples = new List<Sample>();
        private int _windowMilliseconds = 100;
        private double _thresholdPixelsPerMillisecond = 1.2;

        public void Configure(int windowMilliseconds, double thresholdPixelsPerMillisecond)
        {
            _windowMilliseconds = Math.Max(1, windowMilliseconds);
            _thresholdPixelsPerMillisecond = Math.Max(0.0, thresholdPixelsPerMillisecond);
        }

        public void Reset()
        {
            _samples.Clear();
        }

        public void AddSample(double pixelX, double pixelY, int timeMilliseconds)
        {
            _samples.Add(new Sample(pixelX, pixelY, timeMilliseconds));
            while (_samples.Count > 1 &&
                   _samples[0].TimeMilliseconds < timeMilliseconds - _windowMilliseconds)
            {
                _samples.RemoveAt(0);
            }
        }

        public bool IsFast()
        {
            if (_samples.Count < 2)
            {
                return true;
            }

            Sample oldest = _samples[0];
            Sample newest = _samples[_samples.Count - 1];
            int deltaTime = newest.TimeMilliseconds - oldest.TimeMilliseconds;
            if (deltaTime <= 0)
            {
                return true;
            }

            double deltaX = newest.PixelX - oldest.PixelX;
            double deltaY = newest.PixelY - oldest.PixelY;
            double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            return distance / deltaTime >= _thresholdPixelsPerMillisecond;
        }

        private readonly struct Sample
        {
            public Sample(double pixelX, double pixelY, int timeMilliseconds)
            {
                PixelX = pixelX;
                PixelY = pixelY;
                TimeMilliseconds = timeMilliseconds;
            }

            public double PixelX { get; }
            public double PixelY { get; }
            public int TimeMilliseconds { get; }
        }
    }
}
