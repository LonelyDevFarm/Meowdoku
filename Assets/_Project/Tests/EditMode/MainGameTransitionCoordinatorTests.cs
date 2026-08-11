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
            Assert.That(result.Size, Is.EqualTo(4));
            Assert.That(result.CompletionRate, Is.EqualTo(25));
            Assert.That(result.StepsUsed, Is.EqualTo(4));
            Assert.That(result.IsBankSession, Is.False);
            Assert.That(data.CurrentLevel, Is.EqualTo(13));
            Assert.That(data.LastLevelCleanWin, Is.True);
            Assert.That(data.RetryPuzzleLevel, Is.Zero);
            Assert.That(data.RetryPuzzleParameters, Is.Empty);
            Assert.That(data.EndgameSnapshot, Is.Empty);
            Assert.That(store.Operations, Is.EqualTo(new[] { "player", "player", "endgame" }));
            Assert.That(coordinator.TrySettleWin(session, Context(), out _), Is.False);
        }

        [TestCase(20, true, false)]
        [TestCase(40, true, true)]
        public void Win_SpecialOrHardLevelAdvancesExactlyOnce(
            int level,
            bool isSpecial,
            bool isHard)
        {
            var store = new RecordingStore();
            var data = StateData();
            data.CurrentLevel = level;
            var state = Service(data, store);
            GameSession session = PlayingSession();
            Complete(session);
            GameSessionSnapshotContext context = Context();
            context.Level = level;
            var coordinator = new MainGameTransitionCoordinator(state);

            Assert.That(LevelData.IsSpecialLevel(level), Is.EqualTo(isSpecial));
            Assert.That(LevelData.IsHardLevel(level), Is.EqualTo(isHard));
            Assert.That(
                coordinator.TrySettleWin(
                    session,
                    context,
                    out MainGameTransitionData result),
                Is.True);

            Assert.That(result.Level, Is.EqualTo(level));
            Assert.That(result.CurrentLevelAfter, Is.EqualTo(level + 1));
            Assert.That(data.CurrentLevel, Is.EqualTo(level + 1));
            Assert.That(coordinator.TrySettleWin(session, context, out _), Is.False);
        }

        [Test]
        public void RepeatedFailReviveCycles_CanStillWinAndAdvanceOnlyOnce()
        {
            var store = new RecordingStore();
            var data = StateData();
            var state = Service(data, store);
            GameSession session = FailedSession();
            GameSessionSnapshotContext context = Context();
            var coordinator = new MainGameTransitionCoordinator(state);

            Assert.That(coordinator.TrySettleFail(session, context, out _), Is.True);
            Assert.That(coordinator.TryRevive(session, context, 1, out _), Is.True);

            SessionActionResult wrong = session.DoubleTap(3, 0);
            Assert.That(wrong.Accepted, Is.True);
            Assert.That(session.ResolveWrongGuess(), Is.EqualTo(GameSessionState.Failed));
            Assert.That(coordinator.TrySettleFail(session, context, out _), Is.True);
            Assert.That(coordinator.TryRevive(session, context, 1, out _), Is.True);

            Complete(session);
            Assert.That(
                coordinator.TrySettleWin(
                    session,
                    context,
                    out MainGameTransitionData result),
                Is.True);

            Assert.That(result.Kind, Is.EqualTo(MainGameTransitionKind.Won));
            Assert.That(result.ReviveCount, Is.EqualTo(2));
            Assert.That(data.PreCatPendingStruggle, Is.True);
            Assert.That(data.PreCatFailCount, Is.Zero);
            Assert.That(data.CurrentLevel, Is.EqualTo(13));
            Assert.That(coordinator.TrySettleWin(session, context, out _), Is.False);
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
        public void RestartAfterFail_DoesNotSettleFailureTwiceAndCarriesCount()
        {
            var store = new RecordingStore();
            var data = StateData();
            var state = Service(data, store);
            GameSession session = FailedSession();
            var coordinator = new MainGameTransitionCoordinator(state);

            Assert.That(
                coordinator.TrySettleFail(session, Context(), out _),
                Is.True);
            int savesAfterFail = store.PlayerSaveCount;

            Assert.That(
                coordinator.TryRestartAfterFail(
                    session,
                    Context(),
                    out MainGameTransitionData restart),
                Is.True);

            Assert.That(restart.Kind, Is.EqualTo(MainGameTransitionKind.Restart));
            Assert.That(restart.RestartCount, Is.EqualTo(1));
            Assert.That(restart.RetryParameters, Is.Not.Empty);
            Assert.That(store.PlayerSaveCount, Is.EqualTo(savesAfterFail));
            Assert.That(data.EndgameSnapshot, Is.Empty);
            Assert.That(
                coordinator.TryRestartAfterFail(session, Context(), out _),
                Is.False);
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

        [Test]
        public void BankQuit_DoesNotOverwriteNormalLevelSnapshot()
        {
            var store = new RecordingStore();
            var data = StateData();
            Dictionary<string, object> normalSnapshot = data.EndgameSnapshot;
            var state = Service(data, store);
            GameSessionSnapshotContext context = Context();
            context.Level = 0;
            var coordinator = new MainGameTransitionCoordinator(state);

            Assert.That(
                coordinator.TryQuit(
                    PlayingSession(),
                    context,
                    out MainGameTransitionData result),
                Is.True);

            Assert.That(result.Level, Is.Zero);
            Assert.That(data.EndgameSnapshot, Is.SameAs(normalSnapshot));
            Assert.That(state.IsCurrentLevelDirty, Is.False);
            Assert.That(store.ImmediateEndgameSaveCount, Is.Zero);
        }

        [Test]
        public void BankWin_DoesNotClearNormalLevelSnapshotOrAdvanceProgress()
        {
            var store = new RecordingStore();
            var data = StateData();
            Dictionary<string, object> normalSnapshot = data.EndgameSnapshot;
            var state = Service(data, store);
            GameSession session = PlayingSession();
            Complete(session);
            GameSessionSnapshotContext context = Context();
            context.Level = 0;
            var coordinator = new MainGameTransitionCoordinator(state);

            Assert.That(
                coordinator.TrySettleWin(
                    session,
                    context,
                    out MainGameTransitionData transition),
                Is.True);

            Assert.That(transition.IsBankSession, Is.True);
            Assert.That(transition.BankParameters, Is.Not.Empty);
            Assert.That(data.CurrentLevel, Is.EqualTo(12));
            Assert.That(data.EndgameSnapshot, Is.SameAs(normalSnapshot));
            Assert.That(store.ImmediateEndgameSaveCount, Is.Zero);
        }

        [Test]
        public void BankRetryAfterNext_KeepsPoolMetadataWithoutDirectReturn()
        {
            LevelEntry entry = LevelEntry.FromDictionary(
                new Dictionary<string, object>
                {
                    { "id", 22 },
                    { "size", 4 },
                    { "r", 1 },
                    { "regionMap", Regions() },
                    { "solution", new[] { 1, 3, 0, 2 } },
                    { "bank_sp", true },
                    { "bank_index", 2 },
                    { "bank_total", 5 },
                    { "r1", 4 },
                    { "r2", 3 },
                    { "r3", 2 },
                    { "r4", 1 },
                    { "r5", 0 }
                });
            var context = new GameSessionSnapshotContext
            {
                Level = 0,
                BankIndex = 2,
                Entry = entry,
                Mode = GameplaySessionMode.Bank
            };

            Dictionary<string, object> retry =
                GameRetryParameters.BuildFailure(context);

            Assert.That(retry["bank_index"], Is.EqualTo(2));
            Assert.That(retry["bank_total"], Is.EqualTo(5));
            Assert.That(retry["bank_sp"], Is.True);
            Assert.That(retry["r4_steps"], Is.EqualTo(1));
            Assert.That(retry.ContainsKey("from_bank_browser"), Is.False);
        }

        [Test]
        public void DailyFailRevive_DoesNotTouchMainRetrySnapshotOrDda()
        {
            var store = new RecordingStore();
            var data = StateData();
            Dictionary<string, object> mainSnapshot = data.EndgameSnapshot;
            var state = Service(data, store);
            GameSession session = FailedSession();
            GameSessionSnapshotContext context = DailyContext();
            var coordinator = new DailyGameTransitionCoordinator(state);

            Assert.That(
                coordinator.TrySettleFail(
                    session,
                    context,
                    out MainGameTransitionData failed),
                Is.True);
            Assert.That(failed.IsDailySession, Is.True);
            Assert.That(failed.IsBankSession, Is.False);
            Assert.That(data.CurrentLevel, Is.EqualTo(12));
            Assert.That(data.EndgameSnapshot, Is.SameAs(mainSnapshot));
            Assert.That(data.RetryPuzzleLevel, Is.Zero);
            Assert.That(state.IsCurrentLevelDirty, Is.False);
            Assert.That(state.WasDdaToolOrReviveUsed, Is.False);

            Assert.That(
                coordinator.TryRevive(session, context, 1, out _),
                Is.True);
            Assert.That(state.GetGameTotalStat("daily", "revive_count"), Is.EqualTo(1));
            Assert.That(state.GetGameTotalStat("daily", "rv_count"), Is.EqualTo(1));
            Assert.That(state.GetGameTotalStat("main", "revive_count"), Is.Zero);
            Assert.That(data.PreCatRevivedThisLevel, Is.False);
            Assert.That(data.EndgameSnapshot, Is.SameAs(mainSnapshot));
        }

        [Test]
        public void DailyRestart_ReusesFreshLaunchWithoutSettlingFailTwice()
        {
            var store = new RecordingStore();
            var state = Service(StateData(), store);
            GameSession session = FailedSession();
            GameSessionSnapshotContext context = DailyContext();
            var coordinator = new DailyGameTransitionCoordinator(state);
            Assert.That(coordinator.TrySettleFail(session, context, out _), Is.True);
            int savesAfterFail = store.PlayerSaveCount;

            Assert.That(
                coordinator.TryRestartAfterFail(
                    session,
                    context,
                    out MainGameTransitionData restart),
                Is.True);

            Assert.That(restart.IsDailySession, Is.True);
            Assert.That(restart.RestartCount, Is.EqualTo(1));
            Assert.That(restart.RetryParameters["daily_mode"], Is.True);
            Assert.That(restart.RetryParameters["daily_date"], Is.EqualTo("2026-08-09"));
            Assert.That(
                (List<object>)restart.RetryParameters["prefill_positions"],
                Is.Empty);
            Assert.That(store.PlayerSaveCount, Is.EqualTo(savesAfterFail));
        }

        [Test]
        public void DailyWin_CommitsDateTimeAndPercentOnceWithoutAdvancingMain()
        {
            var store = new RecordingStore();
            var data = StateData();
            Dictionary<string, object> mainSnapshot = data.EndgameSnapshot;
            var state = Service(data, store);
            GameSession session = PlayingSession();
            Complete(session);
            var coordinator = new DailyGameTransitionCoordinator(state);

            Assert.That(
                coordinator.TrySettleWin(
                    session,
                    DailyContext(),
                    out MainGameTransitionData transition),
                Is.True);
            Assert.That(data.CurrentLevel, Is.EqualTo(12));
            Assert.That(data.EndgameSnapshot, Is.SameAs(mainSnapshot));
            Assert.That(
                DailyWinSettlement.Commit(state, transition, 75, 3, 10),
                Is.True);
            Assert.That(transition.DailyCompletionCommitted, Is.True);
            Assert.That(transition.DailyBeatPercent, Is.EqualTo(92.6f));
            Assert.That(state.DailyCompletedDate, Is.EqualTo("2026-08-09"));
            Assert.That(state.DailyElapsedSeconds, Is.EqualTo(75));
            Assert.That(state.DailyBeatPercent, Is.EqualTo(92.6f));
            Assert.That(
                DailyWinSettlement.Commit(state, transition, 20, 3, 10),
                Is.False);
            Assert.That(state.DailyElapsedSeconds, Is.EqualTo(75));
        }

        [Test]
        public void MainCoordinator_RejectsDailyContext()
        {
            var coordinator = new MainGameTransitionCoordinator(
                Service(StateData(), new RecordingStore()));

            Assert.That(
                coordinator.TryQuit(
                    PlayingSession(),
                    DailyContext(),
                    out _),
                Is.False);
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

        private static GameSessionSnapshotContext DailyContext()
        {
            GameSessionSnapshotContext context = Context();
            context.Level = 0;
            context.Mode = GameplaySessionMode.Daily;
            context.DailyDate = "2026-08-09";
            context.DailyIndex = 11;
            context.LaunchParameters = new Dictionary<string, object>
            {
                { "daily_mode", true },
                { "daily_date", "2026-08-09" },
                { "daily_index", 11 },
                { "prefill_positions", new List<object> { new List<object> { 0, 1 } } }
            };
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
