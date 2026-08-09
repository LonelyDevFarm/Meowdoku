using Meowdoku.Gameplay.Input;
using NUnit.Framework;
using UnityEngine;

namespace Meowdoku.Tests.EditMode
{
    public sealed class SwipeAxisGuardTests
    {
        private const int Size = 6;
        private const int Slot = 108;
        private const int Padding = 20;
        private const int Cell = 100;
        private const int Threshold = 4;
        private const float Tolerance = 40f;

        [Test]
        public void ActiveGuard_LocksAfterThresholdCells()
        {
            SwipeAxisGuard guard = NewGuard(true);
            LockRowZeroByFour(guard);

            Assert.That(guard.GetDebugLock(), Contains.Key("axis"));
        }

        [Test]
        public void InactiveGuard_NeverLocksBeforeThreshold()
        {
            SwipeAxisGuard guard = NewGuard(false);
            for (int column = 0; column <= 4; column++)
            {
                guard.Process(CenterX(column), CenterY(0));
            }

            Assert.That(guard.GetDebugLock(), Does.Not.ContainKey("axis"));
        }

        [Test]
        public void ActiveLockedGuard_HoldsWithinTolerance()
        {
            SwipeAxisGuard guard = NewGuard(true);
            LockRowZeroByFour(guard);

            Vector2Int result = guard.Process(CenterX(4), 150f);

            Assert.That(guard.GetDebugLock(), Contains.Key("axis"));
            Assert.That(result, Is.EqualTo(new Vector2Int(4, 0)));
        }

        [Test]
        public void ActiveLockedGuard_ReleasesAfterToleranceOvershoot()
        {
            SwipeAxisGuard guard = NewGuard(true);
            LockRowZeroByFour(guard);

            guard.Process(CenterX(4), 200f);

            Assert.That(guard.GetDebugLock(), Does.Not.ContainKey("axis"));
        }

        [Test]
        public void DisablingLockedGuard_ReleasesToRawCell()
        {
            SwipeAxisGuard guard = NewGuard(true);
            LockRowZeroByFour(guard);
            guard.SetActive(false);

            Vector2Int result = guard.Process(CenterX(4), 150f);

            Assert.That(guard.GetDebugLock(), Does.Not.ContainKey("axis"));
            Assert.That(result, Is.EqualTo(new Vector2Int(4, 1)));
        }

        [Test]
        public void ReenabledGuard_CanLockAlongNewAxis()
        {
            SwipeAxisGuard guard = NewGuard(true);
            LockRowZeroByFour(guard);
            guard.SetActive(false);
            guard.Process(CenterX(4), CenterY(0));
            Assert.That(guard.GetDebugLock(), Does.Not.ContainKey("axis"));

            guard.SetActive(true);
            guard.Process(CenterX(5), CenterY(0));
            for (int row = 1; row <= 3; row++)
            {
                guard.Process(CenterX(5), CenterY(row));
            }

            Assert.That(guard.GetDebugLock(), Contains.Key("axis"));
        }

        private static SwipeAxisGuard NewGuard(bool active)
        {
            var guard = new SwipeAxisGuard();
            guard.Begin(Size, Slot, Padding, Cell, new Vector2Int(0, 0));
            guard.Configure(active, Threshold, Tolerance);
            return guard;
        }

        private static void LockRowZeroByFour(SwipeAxisGuard guard)
        {
            for (int column = 0; column <= 3; column++)
            {
                guard.Process(CenterX(column), CenterY(0));
            }
        }

        private static float CenterX(int column)
        {
            return Padding + column * Slot + Cell / 2f;
        }

        private static float CenterY(int row)
        {
            return Padding + row * Slot + Cell / 2f;
        }
    }
}
