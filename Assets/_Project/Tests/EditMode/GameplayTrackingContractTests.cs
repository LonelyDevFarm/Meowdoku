using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.Tracking;
using Meowdoku.Gameplay;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class GameplayTrackingContractTests
    {
        [Test]
        public void BuildQid_UsesSourceHardTierStrategyMapping()
        {
            GameSessionSnapshotContext context = Context(
                GameplaySessionMode.Main,
                rank: 4,
                tier: "H",
                transform: 5);

            Assert.That(
                GameplayTrackingContract.BuildQid(context),
                Is.EqualTo("8_regular_5_42_5"));
            Assert.That(GameplayTrackingContract.QidStrategy(5, ""),
                Is.EqualTo(6));
            Assert.That(GameplayTrackingContract.QidStrategy(5, "H"),
                Is.EqualTo(7));
        }

        [Test]
        public void BuildStart_DailyUsesCurrentLevelAndDisablesChallenge()
        {
            GameplayTrackingStartData start =
                GameplayTrackingContract.BuildStart(
                    Context(GameplaySessionMode.Daily, 3, "", 9),
                    TrackerCatalog.GameStatus.Continue,
                    currentLevel: 20,
                    isHard: true,
                    isChallenge: true);

            Assert.That(start.GameType,
                Is.EqualTo(TrackerCatalog.GameType.Daily));
            Assert.That(start.Level, Is.EqualTo(20));
            Assert.That(start.Difficulty, Is.EqualTo(1));
            Assert.That(start.IsChallenge, Is.Zero);
            Assert.That(start.QuestionRotation, Is.EqualTo("V90"));
        }

        [Test]
        public void BuildEnd_ContainsSourceKeysAndCapturedTransitionState()
        {
            GameStateService state = State();
            state.SetToolCount("hint", 2);
            state.SetToolCount("locate", 1);
            state.IncrementGameTotalStat("normal", "time_total", 12);
            state.IncrementGameTotalStat("normal", "step_total", 18);
            state.IncrementGameTotalStat("normal", "hint_used_total", 5);
            state.IncrementGameTotalStat("normal", "locate_used_total", 4);
            state.IncrementGameTotalStat("normal", "clear_used_total", 3);
            state.IncrementGameTotalStat("normal", "invalid_sign_total", 2);
            var tracker = new TrackerService(state);
            tracker.SetActiveGameType(TrackerCatalog.GameType.Normal);
            tracker.IncrementStat("hint_used", 1);
            tracker.IncrementStat("locate_used", 2);
            tracker.IncrementStat("hint_apply_used", 3);
            tracker.IncrementStat("hint_stop_used", 4);
            tracker.IncrementStat("hint_detail_used", 5);
            tracker.IncrementStat("clear_used", 6);
            tracker.IncrementStat("step_used", 9);
            tracker.IncrementStat("erase_count", 7);
            tracker.IncrementStat("hint_cross_count", 8);
            tracker.IncrementStat("gamedie_count", 1);
            tracker.IncrementStat("restart_count", 2);

            GameplayTrackingStartData start =
                GameplayTrackingContract.BuildStart(
                    Context(GameplaySessionMode.Main, 4, "H", 5),
                    TrackerCatalog.GameStatus.Restart,
                    currentLevel: 17,
                    isHard: false,
                    isChallenge: true);
            var transition = new MainGameTransitionData
            {
                ElapsedSeconds = 12.9f,
                StepsUsed = 7,
                CrossCount = 21,
                CorrectCrossCount = 19,
                FalseCrossCount = 2,
                ErrorCount = 1,
                RemainingCats = 3,
                Lives = 2,
                FinalScore = 2560
            };

            Dictionary<string, object> values =
                GameplayTrackingContract.BuildEnd(
                    start,
                    transition,
                    TrackerCatalog.GameResult.Quit,
                    tracker,
                    state);

            Assert.That(values["qid"],
                Is.EqualTo("8_regular_5_42_5"));
            Assert.That(values["result"], Is.EqualTo("quit"));
            Assert.That(values["hint"], Is.EqualTo(2));
            Assert.That(values["hint_used"], Is.EqualTo(1));
            Assert.That(values["locate_used"], Is.EqualTo(2));
            Assert.That(values["hint_apply_used"], Is.EqualTo(3));
            Assert.That(values["hint_stop_used"], Is.EqualTo(4));
            Assert.That(values["hint_detail_used"], Is.EqualTo(5));
            Assert.That(values["clear_used"], Is.EqualTo(6));
            Assert.That(values["step_used"], Is.EqualTo(9));
            Assert.That(values["step_total"], Is.EqualTo(18));
            Assert.That(values["hint_used_total"], Is.EqualTo(5));
            Assert.That(values["locate_used_total"], Is.EqualTo(4));
            Assert.That(values["clear_used_total"], Is.EqualTo(3));
            Assert.That(values["time"], Is.EqualTo(12));
            Assert.That(values["time_total"], Is.EqualTo(12));
            Assert.That(values["cross_count"], Is.EqualTo(21));
            Assert.That(values["hint_cross_count"], Is.EqualTo(8));
            Assert.That(values["invalid_sign"], Is.EqualTo(1));
            Assert.That(values["invalid_sign_total"], Is.EqualTo(2));
            Assert.That(values["fail_sign"], Is.EqualTo(3));
            Assert.That(values["erase_count"], Is.EqualTo(7));
            Assert.That(values["gamedie_count"], Is.EqualTo(1));
            Assert.That(values["restart_count"], Is.EqualTo(2));
            Assert.That(values["hp_count"], Is.EqualTo(2));
            Assert.That(values["se_score"], Is.EqualTo(2560));
            Assert.That(values.ContainsKey("percent"), Is.False);
        }

        private static GameSessionSnapshotContext Context(
            GameplaySessionMode mode,
            int rank,
            string tier,
            int transform)
        {
            LevelEntry entry = LevelEntry.FromDictionary(
                new Dictionary<string, object>
                {
                    ["size"] = 8,
                    ["r"] = rank,
                    ["bank_source"] = "regular",
                    ["bank_tier"] = tier,
                    ["bank_transform"] = transform
                });
            return new GameSessionSnapshotContext
            {
                Level = 17,
                BankIndex = 42,
                Entry = entry,
                Mode = mode,
                PreType = "0"
            };
        }

        private static GameStateService State() =>
            new(new GameStateData(), new MemoryStore());

        private sealed class MemoryStore : IGameStatePlayerStore
        {
            public bool SavePlayer(GameStateData data) => true;
        }
    }
}
