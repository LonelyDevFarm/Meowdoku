using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.Config;
using Meowdoku.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Meowdoku.Tests.EditMode
{
    public sealed class MainGameTransitionCoordinatorTests
    {
        [Test]
        public void Fail_SettlesOnceKeepsZeroLifeSnapshotAndBuildsCleanRetry()
        {
            var store = new RecordingStore();
            var data = StateData();
            Dictionary<string, object> failedSnapshot = data.EndgameSnapshot;
            var state = Service(data, store);
            GameSession session = FailedSession();
            GameSessionSnapshotContext context = Context();
            var coordinator = new MainGameTransitionCoordinator(state);

            Assert.That(
                coordinator.TrySettleFail(session, context, out MainGameTransitionData result),
                Is.True);

            Assert.That(result.Kind, Is.EqualTo(MainGameTransitionKind.Failed));
            Assert.That(result.Lives, Is.Zero);
            Assert.That(result.RemainingCats, Is.EqualTo(4));
            Assert.That(result.MistakeCount, Is.EqualTo(3));
            Assert.That(data.EndgameSnapshot, Is.SameAs(failedSnapshot));
            Assert.That(data.RetryPuzzleLevel, Is.EqualTo(12));
            Assert.That(state.IsCurrentLevelRetried, Is.True);
            Assert.That(state.IsCurrentLevelDirty, Is.True);
            Assert.That(data.PreCatFailCount, Is.EqualTo(1));
            Assert.That(store.PlayerSaveCount, Is.EqualTo(3));

            var prefills = (List<object>)result.RetryParameters["prefill_positions"];
            Assert.That(prefills.Count, Is.EqualTo(1));
            Assert.That((List<object>)prefills[0], Is.EqualTo(new object[] { 2, 0 }));
            Assert.That(
                coordinator.TrySettleFail(session, context, out _),
                Is.False);
        }

        [Test]
        public void Revive_AfterSettledFailMarksDdaStatsAndFlushesSnapshotImmediately()
        {
            var store = new RecordingStore();
            var data = StateData();
            var state = Service(data, store);
            GameSession session = FailedSession();
            GameSessionSnapshotContext context = Context();
            var coordinator = new MainGameTransitionCoordinator(state);
            Assert.That(coordinator.TrySettleFail(session, context, out _), Is.True);

            Assert.That(
                coordinator.TryRevive(session, context, 1, out MainGameTransitionData result),
                Is.True);

            Assert.That(result.Kind, Is.EqualTo(MainGameTransitionKind.Revived));
            Assert.That(session.State, Is.EqualTo(GameSessionState.Playing));
            Assert.That(session.Lives, Is.EqualTo(1));
            Assert.That(session.ReviveCount, Is.EqualTo(1));
            Assert.That(state.WasDdaToolOrReviveUsed, Is.True);
            Assert.That(state.WasDdaReviveUsed, Is.True);
            Assert.That(data.PreCatRevivedThisLevel, Is.True);
            Assert.That(state.GetGameTotalStat("main", "revive_count"), Is.EqualTo(1));
            Assert.That(state.GetGameTotalStat("main", "rv_count"), Is.EqualTo(1));
            Assert.That(data.EndgameSnapshot["lives"], Is.EqualTo(1));
            Assert.That(store.RequestedEndgameSaveCount, Is.EqualTo(2));
            Assert.That(store.ImmediateEndgameSaveCount, Is.EqualTo(1));
        }

        [Test]
        public void Win_SettlesOnceAdvancesProgressAndClearsSnapshotAfterStateMutation()
        {
            var store = new RecordingStore();
            var data = StateData();
            data.RetryPuzzleLevel = 12;
            data.RetryPuzzleParameters = new Dictionary<string, object> { { "seed", 9 } };
            var state = Service(data, store);
            GameSession session = PlayingSession();
            Complete(session);
            var coordinator = new MainGameTransitionCoordinator(state);

            Assert.That(
                coordinator.TrySettleWin(session, Context(), out MainGameTransitionData result),
                Is.True);

            Assert.That(result.Kind, Is.EqualTo(MainGameTransitionKind.Won));
            Assert.That(result.CurrentLevelAfter, Is.EqualTo(13));
            Assert.That(result.FinalScore, Is.EqualTo(2880));
            Assert.That(data.CurrentLevel, Is.EqualTo(13));
            Assert.That(data.LastLevelCleanWin, Is.True);
            Assert.That(data.RetryPuzzleLevel, Is.Zero);
            Assert.That(data.RetryPuzzleParameters, Is.Empty);
            Assert.That(data.EndgameSnapshot, Is.Empty);
            Assert.That(store.Operations, Is.EqualTo(new[] { "player", "player", "endgame" }));
            Assert.That(coordinator.TrySettleWin(session, Context(), out _), Is.False);
        }

        [Test]
        public void Restart_ClearsSnapshotBeforeFailureAndCarriesRestartCount()
        {
            var store = new RecordingStore();
            var data = StateData();
            var state = Service(data, store);
            GameSession session = PlayingSession();
            var coordinator = new MainGameTransitionCoordinator(state);

            Assert.That(
                coordinator.TryRestart(session, Context(), out MainGameTransitionData result),
                Is.True);

            Assert.That(result.Kind, Is.EqualTo(MainGameTransitionKind.Restart));
            Assert.That(result.RestartCount, Is.EqualTo(1));
            Assert.That(data.EndgameSnapshot, Is.Empty);
            Assert.That(state.IsCurrentLevelRetried, Is.True);
            Assert.That(store.Operations, Is.EqualTo(new[] { "endgame", "player", "player" }));
            Assert.That(coordinator.TryRestart(session, Context(), out _), Is.False);
        }

        [Test]
        public void Quit_MarksDirtyAndPersistsCurrentBoardWithoutFailingLevel()
        {
            var store = new RecordingStore();
            var data = StateData();
            var state = Service(data, store);
            GameSession session = PlayingSession();
            session.TryApplyBoardEdit(0, 0, CellStateType.MARK, true, out _);
            var coordinator = new MainGameTransitionCoordinator(state);

            Assert.That(
                coordinator.TryQuit(session, Context(), out MainGameTransitionData result),
                Is.True);

            Assert.That(result.Kind, Is.EqualTo(MainGameTransitionKind.Quit));
            Assert.That(state.IsCurrentLevelDirty, Is.True);
            Assert.That(state.IsCurrentLevelRetried, Is.False);
            Assert.That(data.PreCatFailCount, Is.Zero);
            Assert.That(data.EndgameSnapshot, Is.Not.Empty);
            Assert.That(((List<object>)data.EndgameSnapshot["marks"]).Count, Is.EqualTo(1));
            Assert.That(store.PlayerSaveCount, Is.Zero);
            Assert.That(store.ImmediateEndgameSaveCount, Is.EqualTo(1));
        }

        private static GameStateData StateData()
        {
            return new GameStateData
            {
                CurrentLevel = 12,
                TodayDate = "2026-08-09",
                EndgameSnapshot = new Dictionary<string, object>
                {
                    { "level", 12 }, { "lives", 0 }
                }
            };
        }

        private static GameStateService Service(GameStateData data, RecordingStore store)
        {
            return new GameStateService(
                data,
                store,
                null,
                store,
                dateProvider: new DateProvider("2026-08-09"));
        }

        private static GameSession PlayingSession()
        {
            var session = new GameSession(
                4,
                Regions(),
                new[] { 1, 3, 0, 2 },
                1,
                new ScoreEncourageConfig());
            session.FinishEntering();
            return session;
        }

        private static GameSession FailedSession()
        {
            GameSession session = PlayingSession();
            int[][] wrongCells =
            {
                new[] { 0, 0 }, new[] { 1, 0 }, new[] { 2, 1 }
            };
            for (int index = 0; index < wrongCells.Length; index++)
            {
                session.DoubleTap(wrongCells[index][0], wrongCells[index][1]);
                session.ResolveWrongGuess();
            }
            return session;
        }

        private static void Complete(GameSession session)
        {
            int[] solution = { 1, 3, 0, 2 };
            for (int row = 0; row < solution.Length; row++)
                session.DoubleTap(row, solution[row]);
        }

        private static GameSessionSnapshotContext Context()
        {
            LevelEntry entry = LevelEntry.FromDictionary(new Dictionary<string, object>
            {
                { "size", 4 },
                { "r", 1 },
                { "id", 7 },
                { "seed", 99 },
                { "regionMap", Regions() },
                { "solution", new[] { 1, 3, 0, 2 } },
                { "colorMap", new[] { 0, 1, 2, 3 } },
                { "bank_source", "regular" },
                { "bank_source_main", "regular" },
                { "bank_tier", "N" }
            });
            var context = new GameSessionSnapshotContext
            {
                Level = 12,
                BankIndex = 7,
                Entry = entry,
                PreType = "2",
                PreCatPosition = new Vector2Int(0, 1)
            };
            context.PrefillPositions.Add(new Vector2Int(2, 0));
            context.PrefillPositions.Add(context.PreCatPosition);
            return context;
        }

        private static int[][] Regions()
        {
            return new[]
            {
                new[] { 0, 0, 0, 0 },
                new[] { 1, 1, 1, 1 },
                new[] { 2, 2, 2, 2 },
                new[] { 3, 3, 3, 3 }
            };
        }

        private sealed class DateProvider : ICurrentDateProvider
        {
            public DateProvider(string date) { CurrentDate = date; }
            public string CurrentDate { get; }
        }

        private sealed class RecordingStore : IGameStatePlayerStore, IGameStateEndgameStore
        {
            public int PlayerSaveCount { get; private set; }
            public int ImmediateEndgameSaveCount { get; private set; }
            public int RequestedEndgameSaveCount { get; private set; }
            public List<string> Operations { get; } = new List<string>();

            public bool SavePlayer(GameStateData data)
            {
                PlayerSaveCount++;
                Operations.Add("player");
                return true;
            }

            public bool SaveEndgame(GameStateData data)
            {
                ImmediateEndgameSaveCount++;
                Operations.Add("endgame");
                return true;
            }

            public bool RequestSaveEndgame(GameStateData data)
            {
                RequestedEndgameSaveCount++;
                return true;
            }

            public void ClearEndgame() { }
        }
    }
}
