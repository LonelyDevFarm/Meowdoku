using System.Linq;
using Meowdoku.Core.Config;
using Meowdoku.Gameplay;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class GameplayFeedbackTests
    {
        [Test]
        public void LegacyCorrectCats_EmitSourceOrderedScoreAndComboPayload()
        {
            GameSession session = CreateSession(new ScoreEncourageConfig());

            GameplayFeedbackData first = session.DoubleTap(0, 1).Feedback.Single();
            GameplayFeedbackData second = session.DoubleTap(1, 3).Feedback.Single();
            GameplayFeedbackData third = session.DoubleTap(2, 0).Feedback.Single();

            Assert.That(first.BaseGain, Is.EqualTo(600));
            Assert.That(first.ScoreAfter, Is.EqualTo(600));
            Assert.That(first.ShowsComboText, Is.False);
            Assert.That(second.BaseGain, Is.EqualTo(680));
            Assert.That(second.ScoreAfter, Is.EqualTo(1280));
            Assert.That(third.BaseGain, Is.EqualTo(760));
            Assert.That(third.ComboCount, Is.EqualTo(3));
            Assert.That(third.ShowsComboText, Is.True);
            Assert.That(third.Source, Is.EqualTo(GameplayFeedbackSource.UserAction));
        }

        [Test]
        public void MultiplierVariant_EmitsDisplayGainPreviousMultiplierAndFlyDelay()
        {
            var config = new ScoreEncourageConfig();
            config.SetDebugOverride(ScoreEncourageConfig.ValueMultiplier);
            GameSession session = CreateSession(config);
            GameplayFeedbackData first = session.DoubleTap(0, 1).Feedback.Single();
            session.DoubleTap(1, 3);

            GameplayFeedbackData feedback = session.DoubleTap(2, 0).Feedback.Single();

            Assert.That(feedback.BaseGain, Is.EqualTo(600));
            Assert.That(feedback.DisplayGain, Is.EqualTo(600));
            Assert.That(feedback.Multiplier, Is.EqualTo(1.5f));
            Assert.That(feedback.PreviousMultiplier, Is.EqualTo(1f));
            Assert.That(first.ShowsMultiplier, Is.False);
            Assert.That(feedback.ShowsMultiplier, Is.True);
            Assert.That(feedback.UsesScrollMultiplierAnimation, Is.False);
            Assert.That(feedback.TotalGain, Is.EqualTo(900));
            Assert.That(feedback.HasFlyEffect, Is.True);
            Assert.That(first.FlyDelaySeconds, Is.EqualTo(0.8f));
            Assert.That(feedback.FlyDelaySeconds, Is.EqualTo(1.45f));
        }

        [Test]
        public void WrongGuess_EmitsDeductionLivesAndConflictContract()
        {
            var config = new ScoreEncourageConfig();
            config.SetDebugOverride(ScoreEncourageConfig.ValueDeduction);
            GameSession session = CreateSession(config);
            session.DoubleTap(0, 1);

            SessionActionResult result = session.DoubleTap(0, 0);
            GameplayFeedbackData feedback = result.Feedback.Single();

            Assert.That(feedback.Kind, Is.EqualTo(GameplayFeedbackKind.WrongGuess));
            Assert.That(feedback.Deduction, Is.EqualTo(100));
            Assert.That(feedback.ScoreBefore, Is.EqualTo(600));
            Assert.That(feedback.ScoreAfter, Is.EqualTo(500));
            Assert.That(feedback.LivesBefore, Is.EqualTo(3));
            Assert.That(feedback.LivesAfter, Is.EqualTo(2));
            Assert.That(feedback.RuleViolation, Is.EqualTo(result.RuleViolation));
            Assert.That(feedback.ConflictingCats, Is.EqualTo(result.ConflictingCats));
        }

        [Test]
        public void LifeBonus_WinAppendsOneOrderedEventPerHeartBeforeFinalScore()
        {
            var config = new ScoreEncourageConfig();
            config.SetDebugOverride(ScoreEncourageConfig.ValueLifeBonus);
            GameSession session = CreateSession(config);

            SessionActionResult result = session.AutoComplete();
            GameplayFeedbackData[] bonuses = result.Feedback
                .Where(item => item.Kind == GameplayFeedbackKind.LifeBonus)
                .ToArray();

            Assert.That(config.HasLifeBonus(), Is.True, "life bonus config override");
            Assert.That(result.IsComplete, Is.True, "auto-complete terminal state");
            Assert.That(bonuses, Has.Length.EqualTo(3));
            Assert.That(bonuses.Select(item => item.TotalGain), Is.EqualTo(new[] { 100, 100, 200 }));
            Assert.That(bonuses.Select(item => item.LifeSlotIndex), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(bonuses[2].ScoreAfter, Is.EqualTo(3280));
            Assert.That(session.Score.Score, Is.EqualTo(3280));
            Assert.That(session.State, Is.EqualTo(GameSessionState.Won));
        }

        [Test]
        public void FourthCorrectCat_EmitsScoreFeedbackBeforeWinning()
        {
            GameSession session = CreateSession(new ScoreEncourageConfig());
            session.DoubleTap(0, 1);
            session.DoubleTap(1, 3);
            session.DoubleTap(2, 0);

            SessionActionResult result = session.DoubleTap(3, 2);
            GameplayFeedbackData feedback = result.Feedback.Single();

            Assert.That(result.IsComplete, Is.True);
            Assert.That(session.State, Is.EqualTo(GameSessionState.Won));
            Assert.That(feedback.Kind, Is.EqualTo(GameplayFeedbackKind.CorrectCat));
            Assert.That(feedback.Position, Is.EqualTo(new UnityEngine.Vector2Int(3, 2)));
            Assert.That(feedback.DisplayGain, Is.EqualTo(840));
            Assert.That(feedback.ScoreAfter, Is.EqualTo(2880));
            Assert.That(session.Score.Score, Is.EqualTo(feedback.ScoreAfter));
        }

        private static GameSession CreateSession(ScoreEncourageConfig config)
        {
            var session = new GameSession(
                4,
                new[]
                {
                    new[] { 0, 0, 0, 0 },
                    new[] { 1, 1, 1, 1 },
                    new[] { 2, 2, 2, 2 },
                    new[] { 3, 3, 3, 3 }
                },
                new[] { 1, 3, 0, 2 },
                1,
                config);
            session.FinishEntering();
            return session;
        }
    }
}
