using Meowdoku.Gameplay.Input;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class SwipeVelocityGateTests
    {
        [Test]
        public void EmptyHistory_IsFastByDefault()
        {
            SwipeVelocityGate gate = NewGate();
            Assert.That(gate.IsFast(), Is.True);
        }

        [Test]
        public void SingleSample_IsFastByDefault()
        {
            SwipeVelocityGate gate = NewGate();
            gate.AddSample(0f, 0f, 1000);
            Assert.That(gate.IsFast(), Is.True);
        }

        [Test]
        public void ZeroDeltaTime_IsFastByDefault()
        {
            SwipeVelocityGate gate = NewGate();
            gate.AddSample(0f, 0f, 1000);
            gate.AddSample(10f, 10f, 1000);
            Assert.That(gate.IsFast(), Is.True);
        }

        [Test]
        public void SlowMotion_IsNotFast()
        {
            SwipeVelocityGate gate = NewGate();
            gate.AddSample(0f, 0f, 1000);
            gate.AddSample(10f, 0f, 1100);
            Assert.That(gate.IsFast(), Is.False);
        }

        [Test]
        public void FastMotion_IsFast()
        {
            SwipeVelocityGate gate = NewGate();
            gate.AddSample(0f, 0f, 1000);
            gate.AddSample(200f, 0f, 1100);
            Assert.That(gate.IsFast(), Is.True);
        }

        [Test]
        public void ThresholdBoundary_IsInclusive()
        {
            SwipeVelocityGate gate = NewGate();
            gate.AddSample(0f, 0f, 1000);
            gate.AddSample(120f, 0f, 1100);
            Assert.That(gate.IsFast(), Is.True);
        }

        [Test]
        public void HoverHistory_IsNotFast()
        {
            SwipeVelocityGate gate = NewGate();
            gate.AddSample(50f, 50f, 1000);
            gate.AddSample(50f, 50f, 1050);
            gate.AddSample(50f, 50f, 1099);
            Assert.That(gate.IsFast(), Is.False);
        }

        [Test]
        public void SamplesOutsideWindow_AreEvicted()
        {
            SwipeVelocityGate gate = NewGate();
            gate.AddSample(0f, 0f, 0);
            gate.AddSample(500f, 0f, 50);
            gate.AddSample(510f, 0f, 200);
            gate.AddSample(515f, 0f, 300);
            Assert.That(gate.IsFast(), Is.False);
        }

        [Test]
        public void Reset_ClearsHistory()
        {
            SwipeVelocityGate gate = NewGate();
            gate.AddSample(0f, 0f, 1000);
            gate.AddSample(200f, 0f, 1100);
            gate.Reset();
            gate.AddSample(0f, 0f, 1200);
            Assert.That(gate.IsFast(), Is.True);
        }

        [Test]
        public void Configure_UpdatesThreshold()
        {
            var gate = new SwipeVelocityGate();
            gate.Configure(100, 5.0);
            gate.AddSample(0f, 0f, 1000);
            gate.AddSample(200f, 0f, 1100);
            Assert.That(gate.IsFast(), Is.False);
        }

        private static SwipeVelocityGate NewGate()
        {
            var gate = new SwipeVelocityGate();
            gate.Configure(100, 1.2);
            return gate;
        }
    }
}
