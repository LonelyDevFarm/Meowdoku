using Meowdoku.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Meowdoku.Tests.EditMode
{
    public sealed class GameplayScoreFlightMathTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void FlightCurve_PreservesExactEndpoints(bool life)
        {
            var from = new Vector2(-120f, 430f);
            var to = new Vector2(340f, 810f);

            Assert.That(GameplayScoreFlightMath.Evaluate(0f, from, to, life),
                Is.EqualTo(from));
            Assert.That(GameplayScoreFlightMath.Evaluate(1f, from, to, life),
                Is.EqualTo(to));
        }

        [Test]
        public void LifeFlight_UsesDeeperSourceArcThanNormalFlight()
        {
            Vector2 from = Vector2.zero;
            var to = new Vector2(100f, 100f);

            Vector2 normal = GameplayScoreFlightMath.Evaluate(0.25f, from, to, false);
            Vector2 life = GameplayScoreFlightMath.Evaluate(0.25f, from, to, true);

            Assert.That(life.y, Is.LessThan(normal.y));
            Assert.That(normal.x, Is.GreaterThan(0f).And.LessThan(100f));
            Assert.That(life.x, Is.GreaterThan(0f).And.LessThan(100f));
        }

        [Test]
        public void FlightCurve_ClampsInputTime()
        {
            var from = new Vector2(10f, 20f);
            var to = new Vector2(30f, 40f);

            Assert.That(GameplayScoreFlightMath.Evaluate(-1f, from, to, false),
                Is.EqualTo(from));
            Assert.That(GameplayScoreFlightMath.Evaluate(2f, from, to, true),
                Is.EqualTo(to));
        }
    }
}
