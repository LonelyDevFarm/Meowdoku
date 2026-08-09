using System;
using System.Collections.Generic;

namespace Meowdoku.Gameplay
{
    /// <summary>
    /// Source-derived timing plan for ComboFeedbackView. It is independent of
    /// Unity UI so transition timing remains deterministic and testable.
    /// </summary>
    public readonly struct GameplayFeedbackPresentationPlan
    {
        public const float BubbleDurationSeconds = 1.0166667f;
        public const float ScoreRollDurationSeconds = 0.35f;
        public const float FlyDurationSeconds = 0.57f;
        public const float FlyLingerSeconds = 0.067f;
        public const float LifeSequenceGapSeconds = 0.3f;
        public const float LifeFinalSettleSeconds = 0.35f;

        public GameplayFeedbackPresentationPlan(
            float correctFlyLaunchDelay,
            int lifeBonusCount,
            float completionDelay)
        {
            CorrectFlyLaunchDelay = correctFlyLaunchDelay;
            LifeBonusCount = lifeBonusCount;
            CompletionDelay = completionDelay;
        }

        public float CorrectFlyLaunchDelay { get; }
        public int LifeBonusCount { get; }
        public float CompletionDelay { get; }

        public static GameplayFeedbackPresentationPlan Build(
            IReadOnlyList<GameplayFeedbackData> feedback)
        {
            if (feedback == null || feedback.Count == 0)
                return new GameplayFeedbackPresentationPlan(0f, 0, 0f);

            float correctFlyLaunchDelay = 0f;
            int lifeBonusCount = 0;
            bool hasCorrectBubble = false;
            for (int index = 0; index < feedback.Count; index++)
            {
                GameplayFeedbackData item = feedback[index];
                if (item == null) continue;
                if (item.Kind == GameplayFeedbackKind.CorrectCat)
                {
                    hasCorrectBubble = true;
                    if (item.HasFlyEffect)
                        correctFlyLaunchDelay = Math.Max(
                            correctFlyLaunchDelay,
                            Math.Max(0f, item.FlyDelaySeconds));
                }
                else if (item.Kind == GameplayFeedbackKind.LifeBonus)
                    lifeBonusCount++;
            }

            // Only a Won session consumes this deadline. Keeping at least the
            // source bubble duration prevents the final cat feedback from being
            // covered by the terminal presentation on non-fly variants.
            float completionDelay = hasCorrectBubble
                ? Math.Max(correctFlyLaunchDelay, BubbleDurationSeconds)
                : correctFlyLaunchDelay;
            if (lifeBonusCount > 0)
            {
                float lifeCompletion = correctFlyLaunchDelay;
                lifeCompletion += (lifeBonusCount - 1) * LifeSequenceGapSeconds;
                lifeCompletion += FlyDurationSeconds + LifeFinalSettleSeconds;
                completionDelay = Math.Max(completionDelay, lifeCompletion);
            }

            return new GameplayFeedbackPresentationPlan(
                correctFlyLaunchDelay,
                lifeBonusCount,
                completionDelay);
        }
    }
}
