using System.Collections.Generic;
using Meowdoku.Gameplay;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class GameplayFeedbackPresentationPlanTests
    {
        [Test]
        public void EmptyBatch_HasNoGate()
        {
            GameplayFeedbackPresentationPlan plan =
                GameplayFeedbackPresentationPlan.Build(null);

            Assert.That(plan.CompletionDelay, Is.Zero);
            Assert.That(plan.LifeBonusCount, Is.Zero);
        }

        [Test]
        public void ScoreFly_GatesUntilOriginalLaunchDeadlineOnly()
        {
            var feedback = new[]
            {
                new GameplayFeedbackData
                {
                    Kind = GameplayFeedbackKind.CorrectCat,
                    HasFlyEffect = true,
                    FlyDelaySeconds = 1.367f
                }
            };

            GameplayFeedbackPresentationPlan plan =
                GameplayFeedbackPresentationPlan.Build(feedback);

            Assert.That(plan.CorrectFlyLaunchDelay, Is.EqualTo(1.367f).Within(0.0001f));
            Assert.That(plan.CompletionDelay, Is.EqualTo(1.367f).Within(0.0001f));
        }

        [Test]
        public void NonFlyFinalCat_KeepsBubbleVisibleBeforeWinPresentation()
        {
            var feedback = new[]
            {
                new GameplayFeedbackData
                {
                    Kind = GameplayFeedbackKind.CorrectCat,
                    HasFlyEffect = false
                }
            };

            GameplayFeedbackPresentationPlan plan =
                GameplayFeedbackPresentationPlan.Build(feedback);

            Assert.That(plan.CompletionDelay,
                Is.EqualTo(GameplayFeedbackPresentationPlan.BubbleDurationSeconds)
                    .Within(0.0001f));
        }

        [Test]
        public void LifeBonus_FollowsOriginalSequentialSpacingAndFinalSettle()
        {
            var feedback = new List<GameplayFeedbackData>
            {
                new GameplayFeedbackData
                {
                    Kind = GameplayFeedbackKind.CorrectCat,
                    HasFlyEffect = true,
                    FlyDelaySeconds = 0.8f
                },
                new GameplayFeedbackData { Kind = GameplayFeedbackKind.LifeBonus },
                new GameplayFeedbackData { Kind = GameplayFeedbackKind.LifeBonus },
                new GameplayFeedbackData { Kind = GameplayFeedbackKind.LifeBonus }
            };

            GameplayFeedbackPresentationPlan plan =
                GameplayFeedbackPresentationPlan.Build(feedback);

            // The correct-cat score flight (0.8s launch delay) and heart
            // sequence start together in base_game_page.gd. The gate uses the
            // longer timeline instead of summing them.
            float expected = 0.6f + 0.57f + 0.35f;
            Assert.That(plan.CompletionDelay, Is.EqualTo(expected).Within(0.0001f));
            Assert.That(plan.LifeBonusCount, Is.EqualTo(3));
        }
    }
}
