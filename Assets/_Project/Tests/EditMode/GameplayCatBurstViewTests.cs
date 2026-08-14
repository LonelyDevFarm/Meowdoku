using Meowdoku.Gameplay;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class GameplayCatBurstViewTests
    {
        [Test]
        public void SourceTimingAndCount_ArePreserved()
        {
            Assert.That(GameplayCatBurstView.StarCount, Is.EqualTo(24));
            Assert.That(GameplayCatBurstView.EmissionDelaySeconds,
                Is.EqualTo(0.1164f).Within(0.0001f));
            Assert.That(GameplayCatBurstView.GlowLifetimeSeconds,
                Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(GameplayCatBurstView.StarLifetimeSeconds,
                Is.EqualTo(1.02f).Within(0.0001f));
            Assert.That(
                GameplayCatBurstView.EmissionDelaySeconds +
                GameplayCatBurstView.StarLifetimeSeconds,
                Is.EqualTo(1.1364f).Within(0.0001f));
        }
    }
}
