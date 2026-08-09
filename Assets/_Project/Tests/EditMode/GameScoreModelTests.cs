using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.Config;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class GameScoreModelTests
    {
        [Test]
        public void ComboScoreAndReset_FollowModelContract()
        {
            var model = new GameScoreModel();

            model.AddCombo();
            model.AddCombo();
            model.AddScore(250);
            model.ApplyDeduction(40);
            model.ResetCombo();

            Assert.That(model.Score, Is.EqualTo(210));
            Assert.That(model.Combo, Is.Zero);
            Assert.That(model.MaxCombo, Is.EqualTo(2));

            model.ResetAll();
            Assert.That(model.ToDict(), Is.EqualTo(new Dictionary<string, int>
            {
                { "score", 0 },
                { "combo", 0 },
                { "max_combo", 0 }
            }));
        }

        [Test]
        public void Restore_LoadsSerializedValues()
        {
            var model = new GameScoreModel();

            model.Restore(new Dictionary<string, int>
            {
                { "score", 900 },
                { "combo", 3 },
                { "max_combo", 7 }
            });

            Assert.That(model.Score, Is.EqualTo(900));
            Assert.That(model.Combo, Is.EqualTo(3));
            Assert.That(model.MaxCombo, Is.EqualTo(7));
        }

        [Test]
        public void Restore_MissingMaxComboFallsBackToComboLikeSource()
        {
            var model = new GameScoreModel();
            model.AddScore(500);

            model.Restore(new Dictionary<string, int> { { "combo", 4 } });

            Assert.That(model.Score, Is.Zero);
            Assert.That(model.Combo, Is.EqualTo(4));
            Assert.That(model.MaxCombo, Is.EqualTo(4));
        }

        [Test]
        public void DefaultScoring_UsesLegacyComboGainNotPrototypeHundreds()
        {
            var model = new GameScoreModel();
            var config = new ScoreEncourageConfig();
            int successfulCats = 0;

            ScoreGainResult first = GameScoringRules.ApplyCorrectCat(
                model, config, ref successfulCats);
            ScoreGainResult second = GameScoringRules.ApplyCorrectCat(
                model, config, ref successfulCats);

            Assert.That(first.TotalGain, Is.EqualTo(600));
            Assert.That(second.TotalGain, Is.EqualTo(680));
            Assert.That(model.Score, Is.EqualTo(1280));
            Assert.That(model.Combo, Is.EqualTo(2));
            Assert.That(successfulCats, Is.EqualTo(2));
        }

        [Test]
        public void MultiplierAndSkillVariants_UseSourceFormulas()
        {
            var multiplierModel = new GameScoreModel();
            var multiplier = new ScoreEncourageConfig();
            multiplier.SetDebugOverride(ScoreEncourageConfig.ValueMultiplier);
            int multiplierCount = 0;
            GameScoringRules.ApplyCorrectCat(multiplierModel, multiplier, ref multiplierCount);
            GameScoringRules.ApplyCorrectCat(multiplierModel, multiplier, ref multiplierCount);
            ScoreGainResult third = GameScoringRules.ApplyCorrectCat(
                multiplierModel, multiplier, ref multiplierCount);

            Assert.That(third.Multiplier, Is.EqualTo(1.5f));
            Assert.That(third.TotalGain, Is.EqualTo(900));
            Assert.That(multiplierModel.Score, Is.EqualTo(2100));

            var skillModel = new GameScoreModel();
            var skill = new ScoreEncourageConfig();
            skill.SetDebugOverride(ScoreEncourageConfig.ValueSkillScore);
            int skillCount = 0;
            ScoreGainResult skilled = GameScoringRules.ApplyCorrectCat(
                skillModel, skill, ref skillCount, 7);
            Assert.That(skilled.SkillBonus, Is.EqualTo(300));
            Assert.That(skilled.TotalGain, Is.EqualTo(900));
        }

        [Test]
        public void WrongGuessAndLifeBonus_AreGatedByExactVariants()
        {
            var model = new GameScoreModel();
            model.AddScore(500);
            model.AddCombo();
            int successfulCats = 3;
            var config = new ScoreEncourageConfig();

            Assert.That(
                GameScoringRules.ApplyWrongGuess(model, config, ref successfulCats),
                Is.Zero);
            Assert.That(model.Score, Is.EqualTo(500));
            Assert.That(model.Combo, Is.Zero);
            Assert.That(successfulCats, Is.Zero);

            config.SetDebugOverride(ScoreEncourageConfig.ValueDeduction);
            Assert.That(
                GameScoringRules.ApplyWrongGuess(model, config, ref successfulCats),
                Is.EqualTo(100));
            Assert.That(model.Score, Is.EqualTo(400));

            config.SetDebugOverride(ScoreEncourageConfig.ValueLifeBonus);
            Assert.That(GameScoringRules.ApplyLifeBonus(model, config, 3), Is.EqualTo(400));
            Assert.That(model.Score, Is.EqualTo(800));
        }
    }
}
