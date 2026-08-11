using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.Config;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class GameStateServiceTests
    {
        [Test]
        public void BankProgress_UsesSourceKeyShapeAndTierRule()
        {
            var service = new GameStateService(new GameStateData());

            service.AdvanceBankIndex(7, 3, "N", false);
            service.AdvanceBankIndex(7, 3, "H", false);
            service.AdvanceBankIndex(7, 3, "H", false);

            Assert.That(service.GetBankIndex(7, 3, ""), Is.EqualTo(1));
            Assert.That(service.GetBankIndex(7, 3, "N"), Is.EqualTo(1));
            Assert.That(service.GetBankIndex(7, 3, "H"), Is.EqualTo(2));
            Assert.That(service.Data.BankProgress, Contains.Key("7_3"));
            Assert.That(service.Data.BankProgress, Contains.Key("7_3_H"));
        }

        [Test]
        public void PersistFalse_BatchesUntilCommit()
        {
            var store = new CountingStore();
            var service = new GameStateService(new GameStateData(), store);

            service.AdvanceBankIndex(6, 2, "", false);
            service.AdvanceBankIndex(6, 2, "", false);
            Assert.That(store.SaveCount, Is.Zero);

            Assert.That(service.CommitBankProgress(), Is.True);
            Assert.That(store.SaveCount, Is.EqualTo(1));
            Assert.That(store.LastData.BankProgress["6_2"], Is.EqualTo(2));
        }

        [Test]
        public void PersistTrue_SavesImmediately()
        {
            var store = new CountingStore();
            var service = new GameStateService(new GameStateData(), store);

            service.AdvanceBankIndex(5, 1);

            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void MainProgress_DefaultPreservesLegacyMigrationTrigger()
        {
            var service = new GameStateService(new GameStateData());

            Dictionary<string, object> progress = service.GetMainProgress(8, 4, "H");

            Assert.That(progress, Contains.Key("lk_mod"));
            Assert.That(progress, Contains.Key("regular"));
            Assert.That(progress, Contains.Key("lkstyle"));
            Assert.That(progress, Contains.Key("transform"));
            Assert.That(progress, Does.Not.ContainKey("idx"));
            Assert.That(service.Data.MainBankProgress, Contains.Key("8_4_H"));
        }

        [Test]
        public void LkModifiedProgress_IgnoresTierAndDefaultsIndexToZero()
        {
            var service = new GameStateService(new GameStateData());

            Dictionary<string, object> progress = service.GetLkModifiedProgress(10, 2);

            Assert.That(progress["idx"], Is.EqualTo(0));
            Assert.That(service.Data.LkModifiedProgress, Contains.Key("10_2"));
        }

        [Test]
        public void Snapshot_IsDeepCopyOfNestedProgress()
        {
            var service = new GameStateService(new GameStateData());
            Dictionary<string, object> progress = service.GetMainProgress(9, 3);
            progress["transform"] = 2;

            Dictionary<string, object> snapshot = service.GetMainBankProgressSnapshot();
            var snapshotProgress = (Dictionary<string, object>)snapshot["9_3"];
            snapshotProgress["transform"] = 7;

            Assert.That(progress["transform"], Is.EqualTo(2));
        }

        [Test]
        public void ProgressionSetters_PreserveValuesAndPersistEachCall()
        {
            var store = new CountingStore();
            var service = new GameStateService(new GameStateData(), store);

            service.SetCurrentLevel(51);
            service.SetTutorialDone(true);
            service.SetCurrentStrategy(4);
            service.SetAppliedLocale("vi");

            Assert.That(service.CurrentLevel, Is.EqualTo(51));
            Assert.That(service.TutorialDone, Is.True);
            Assert.That(service.CurrentStrategy, Is.EqualTo(4));
            Assert.That(service.AppliedLocale, Is.EqualTo("vi"));
            Assert.That(store.SaveCount, Is.EqualTo(4));
        }

        [Test]
        public void PatternSettings_PersistAndDismissDotsIdempotently()
        {
            var store = new CountingStore();
            var service = new GameStateService(new GameStateData(), store);

            service.SetPatternModeOn(true);
            service.MarkPatternEntryDotDismissed();
            service.MarkPatternEntryDotDismissed();
            service.MarkPatternSwitchDotDismissed();
            service.MarkPatternSwitchDotDismissed();

            Assert.That(service.PatternModeOn, Is.True);
            Assert.That(service.PatternEntryDotDismissed, Is.True);
            Assert.That(service.PatternSwitchDotDismissed, Is.True);
            Assert.That(store.SaveCount, Is.EqualTo(3));
        }

        [Test]
        public void FirstSession_PersistsFalseButRemainsTrueForCurrentRuntime()
        {
            var store = new CountingStore();
            var service = new GameStateService(new GameStateData(), store);

            Assert.That(service.IsFirstSession, Is.True);
            service.ConsumeFirstSessionPersist();

            Assert.That(service.Data.IsFirstSession, Is.False);
            Assert.That(service.IsFirstSession, Is.True);
            Assert.That(store.SaveCount, Is.EqualTo(1));

            service.MarkFirstSessionDone();
            Assert.That(service.IsFirstSession, Is.False);
        }

        [Test]
        public void SplashDate_IsFirstOncePerDayAndPersistsSourceKey()
        {
            var store = new CountingStore();
            var service = new GameStateService(
                new GameStateData(),
                store,
                dateProvider: new DateProvider("2026-08-10"));

            Assert.That(service.MarkSplashShownToday(), Is.True);
            Assert.That(service.MarkSplashShownToday(), Is.False);
            Assert.That(service.LastSplashDate, Is.EqualTo("2026-08-10"));
            Assert.That(store.SaveCount, Is.EqualTo(1));

            Dictionary<string, object> player =
                service.Data.ToPlayerDocument();
            var progress = (Dictionary<string, object>)player["progress"];
            Assert.That(
                progress["last_splash_date"],
                Is.EqualTo("2026-08-10"));
            Assert.That(
                GameStateData.FromDocuments(player, null).LastSplashDate,
                Is.EqualTo("2026-08-10"));
        }

        [Test]
        public void FreeReviveFlag_IsIdempotentAndPersistsSourceKey()
        {
            var store = new CountingStore();
            var service = new GameStateService(new GameStateData(), store);

            service.MarkReviveFreeUsed();
            service.MarkReviveFreeUsed();

            Assert.That(service.HasUsedReviveFree, Is.True);
            Assert.That(store.SaveCount, Is.EqualTo(1));
            Dictionary<string, object> player =
                service.Data.ToPlayerDocument();
            var progress = (Dictionary<string, object>)player["progress"];
            Assert.That(progress["has_used_revive_free"], Is.True);
            Assert.That(
                GameStateData.FromDocuments(player, null).HasUsedReviveFree,
                Is.True);
        }

        [Test]
        public void LastWinBeatPercent_PersistsSourceProgressKey()
        {
            var store = new CountingStore();
            var service = new GameStateService(new GameStateData(), store);

            service.SetLastWinBeatPercent(83.7f);
            service.SetLastWinBeatPercent(83.7f);

            Assert.That(service.LastWinBeatPercent, Is.EqualTo(83.7f));
            Assert.That(store.SaveCount, Is.EqualTo(1));
            Dictionary<string, object> player =
                service.Data.ToPlayerDocument();
            var progress = (Dictionary<string, object>)player["progress"];
            Assert.That(progress["last_win_beat_percent"], Is.EqualTo(83.7f));
            Assert.That(
                GameStateData.FromDocuments(player, null).LastWinBeatPercent,
                Is.EqualTo(83.7f));
        }

        [Test]
        public void MusicUserChoice_BlocksLaterDefaultInitialization()
        {
            var store = new CountingStore();
            var service = new GameStateService(new GameStateData(), store);

            service.SetMusicOn(false);
            service.InitMusicDefault(true);

            Assert.That(service.MusicOn, Is.False);
            Assert.That(service.Data.MusicUserModified, Is.True);
            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void MusicDefault_OnlyPersistsWhenItChangesUntouchedValue()
        {
            var store = new CountingStore();
            var service = new GameStateService(new GameStateData(), store);

            service.InitMusicDefault(true);
            Assert.That(store.SaveCount, Is.Zero);

            service.InitMusicDefault(false);
            Assert.That(service.MusicOn, Is.False);
            Assert.That(service.Data.MusicUserModified, Is.False);
            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void SettingsSetters_SaveAndVibrationUpdatesSink()
        {
            var store = new CountingStore();
            var vibration = new RecordingVibrationSink();
            var service = new GameStateService(new GameStateData(), store, vibration);

            Assert.That(vibration.LastEnabled, Is.True);
            Assert.That(vibration.CallCount, Is.EqualTo(1));

            service.SetSoundOn(false);
            service.SetVibrationOn(false);
            service.SetPeopleOn(false);

            Assert.That(service.SoundOn, Is.False);
            Assert.That(service.VibrationOn, Is.False);
            Assert.That(service.PeopleOn, Is.False);
            Assert.That(vibration.LastEnabled, Is.False);
            Assert.That(vibration.CallCount, Is.EqualTo(2));
            Assert.That(store.SaveCount, Is.EqualTo(3));
        }

        [Test]
        public void ToolDecrease_MarksUsagePersistsAndEmitsSourceSignal()
        {
            var store = new CountingStore();
            var service = new GameStateService(new GameStateData(), store);
            string emittedKind = null;
            int emittedCount = -1;
            service.ToolCountChanged += (kind, count) =>
            {
                emittedKind = kind;
                emittedCount = count;
            };

            service.SetToolCount("hint", 4);

            Assert.That(service.GetToolCount("hint"), Is.EqualTo(4));
            Assert.That(service.HasUsedTool, Is.True);
            Assert.That(store.SaveCount, Is.EqualTo(1));
            Assert.That(emittedKind, Is.EqualTo("hint"));
            Assert.That(emittedCount, Is.EqualTo(4));
        }

        [Test]
        public void PropHighlightShown_PersistsOnceAndRuntimeFlagsResetTogether()
        {
            var store = new CountingStore();
            var service = new GameStateService(new GameStateData(), store);

            service.MarkPropHighlightShown();
            service.MarkPropHighlightShown();
            service.MarkCurrentLevelDirty();
            service.MarkDdaToolOrReviveUsed();
            service.MarkDdaReviveUsed();

            Assert.That(service.HasPropHighlightShown, Is.True);
            Assert.That(service.IsCurrentLevelDirty, Is.True);
            Assert.That(service.WasDdaToolOrReviveUsed, Is.True);
            Assert.That(service.WasDdaReviveUsed, Is.True);
            Assert.That(store.SaveCount, Is.EqualTo(1));

            service.ResetCurrentLevelRuntimeFlags();

            Assert.That(service.IsCurrentLevelDirty, Is.False);
            Assert.That(service.WasDdaToolOrReviveUsed, Is.False);
            Assert.That(service.WasDdaReviveUsed, Is.False);
            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void UndoTool_RemainsSerializedLegacyFieldButRuntimeApiIgnoresIt()
        {
            var store = new CountingStore();
            var data = new GameStateData { ToolUndo = 3 };
            var service = new GameStateService(data, store);
            bool emitted = false;
            service.ToolCountChanged += (kind, count) => emitted = true;

            service.SetToolCount("undo", 1);

            Assert.That(service.GetToolCount("undo"), Is.Zero);
            Assert.That(data.ToolUndo, Is.EqualTo(3));
            Assert.That(store.SaveCount, Is.Zero);
            Assert.That(emitted, Is.False);
        }

        [Test]
        public void RetryPuzzle_ReturnsParametersOnlyForMatchingLevel()
        {
            var store = new CountingStore();
            var service = new GameStateService(new GameStateData(), store);
            var parameters = new Dictionary<string, object> { { "seed", 42 } };

            service.SetRetryPuzzle(12, parameters);

            Assert.That(service.GetRetryPuzzle(11), Is.Empty);
            Assert.That(service.GetRetryPuzzle(12), Is.SameAs(parameters));
            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void MarkPreCatRevived_IsIdempotent()
        {
            var store = new CountingStore();
            var service = new GameStateService(new GameStateData(), store);

            service.MarkPreCatRevived();
            service.MarkPreCatRevived();

            Assert.That(service.Data.PreCatRevivedThisLevel, Is.True);
            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void ConsumePreCatPending_ReturnsThenClearsFlagsOnce()
        {
            var store = new CountingStore();
            var data = new GameStateData
            {
                PreCatPendingHard = true,
                PreCatPendingStruggle = false,
                PreCatPendingDemote = true
            };
            var service = new GameStateService(data, store);

            Dictionary<string, object> first = service.ConsumePreCatPending();
            Dictionary<string, object> second = service.ConsumePreCatPending();

            Assert.That(first["hard"], Is.True);
            Assert.That(first["struggle"], Is.False);
            Assert.That(first["demote"], Is.True);
            Assert.That(second["hard"], Is.False);
            Assert.That(second["struggle"], Is.False);
            Assert.That(second["demote"], Is.False);
            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void PreCatLock_IsRestoredOnlyForMatchingPositiveLevel()
        {
            var store = new CountingStore();
            var service = new GameStateService(new GameStateData(), store);
            var position = new UnityEngine.Vector2Int(2, 4);

            service.SetPreCatLock(15, "2", position);

            Assert.That(service.GetPreCatLock(14)["locked"], Is.False);
            Dictionary<string, object> matching = service.GetPreCatLock(15);
            Assert.That(matching["locked"], Is.True);
            Assert.That(matching["pre_type"], Is.EqualTo("2"));
            Assert.That(matching["position"], Is.EqualTo(position));
            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void EndgameSnapshot_AddsVersionAndUsesImmediateStore()
        {
            var store = new CombinedStore();
            var service = new GameStateService(
                new GameStateData(), store, null, store, "1.2.3");
            var snapshot = new Dictionary<string, object> { { "lives", 2 } };

            Assert.That(service.SetEndgameSnapshot(snapshot), Is.True);

            Assert.That(snapshot["app_version"], Is.EqualTo("1.2.3"));
            Assert.That(service.GetEndgameSnapshot(), Is.SameAs(snapshot));
            Assert.That(store.ImmediateEndgameSaveCount, Is.EqualTo(1));
            Assert.That(store.RequestedEndgameSaveCount, Is.Zero);
        }

        [Test]
        public void EndgameStats_RouteDailyAndMainAndPreserveSaveModes()
        {
            var store = new CombinedStore();
            var service = new GameStateService(new GameStateData(), store, null, store);

            service.IncrementGameTotalStat("main", "step", 2);
            service.IncrementGameTotalStat("daily", "step", 3);
            service.SetPersistedGameId("daily", "daily-id");
            service.PersistGameRoundStats(
                "main",
                new Dictionary<string, object> { { "score", 100 } });

            Assert.That(service.GetGameTotalStat("main", "step"), Is.EqualTo(2));
            Assert.That(service.GetGameTotalStat("daily", "step"), Is.EqualTo(3));
            Assert.That(service.GetPersistedGameId("daily"), Is.EqualTo("daily-id"));
            Assert.That(service.GetGameRoundStats("main")["score"], Is.EqualTo(100));
            Assert.That(store.RequestedEndgameSaveCount, Is.EqualTo(3));
            Assert.That(store.ImmediateEndgameSaveCount, Is.EqualTo(1));
        }

        [Test]
        public void RoundStats_AreCopiedOnSetAndGet()
        {
            var store = new CombinedStore();
            var service = new GameStateService(new GameStateData(), store, null, store);
            var source = new Dictionary<string, object> { { "score", 20 } };

            service.PersistGameRoundStats("main", source);
            source["score"] = 99;
            Dictionary<string, object> restored = service.GetGameRoundStats("main");
            restored["score"] = 50;

            Assert.That(service.Data.MainGameRoundStats["score"], Is.EqualTo(20));
        }

        [Test]
        public void RecordPuzzle_ReturnsPreviousMatchAndKeepsIndependentSnapshots()
        {
            var store = new CountingStore();
            var data = new GameStateData();
            data.BankProgress["4_1"] = 3;
            var service = new GameStateService(data, store);

            Assert.That(service.RecordPuzzle("pid", 10, "1.0", "regular"), Is.Empty);
            data.BankProgress["4_1"] = 4;
            Dictionary<string, object> previous =
                service.RecordPuzzle("pid", 11, "1.1", "regular");

            Assert.That(previous["level"], Is.EqualTo(10));
            var previousBank = (Dictionary<string, object>)previous["bank_progress"];
            Assert.That(previousBank["4_1"], Is.EqualTo(3));
            previousBank["4_1"] = 99;
            var storedFirst = (Dictionary<string, object>)service.GetRecentPuzzles()[0];
            var storedBank = (Dictionary<string, object>)storedFirst["bank_progress"];
            Assert.That(storedBank["4_1"], Is.EqualTo(3));
            Assert.That(store.SaveCount, Is.EqualTo(2));
        }

        [Test]
        public void RecordPuzzle_TrimsHistoryToSourceLimit()
        {
            var service = new GameStateService(new GameStateData());
            for (int index = 0; index < 101; index++)
                service.RecordPuzzle("p" + index, index);

            List<object> recent = service.GetRecentPuzzles();
            Assert.That(recent.Count, Is.EqualTo(100));
            Assert.That(((Dictionary<string, object>)recent[0])["puzzle_id"], Is.EqualTo("p1"));
        }

        [Test]
        public void DailyFirstEasy_EvaluatesConsumesAndPersistsDate()
        {
            var store = new CountingStore();
            var data = new GameStateData { CurrentLevel = 12 };
            var service = new GameStateService(
                data,
                store,
                dateProvider: new DateProvider("2026-08-08"));

            service.EvaluateDailyFirstEasy();
            Assert.That(service.IsDailyFirstEasyAvailable, Is.True);
            service.ConsumeDailyFirstEasy(true);

            Assert.That(data.DailyFirstEasyDate, Is.EqualTo("2026-08-08"));
            Assert.That(service.IsDailyFirstEasyAvailable, Is.False);
            Assert.That(service.IsCurrentLevelDailyFirstEasy, Is.True);
            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void DailyFirstEasy_ExistingPlayedSnapshotConsumesOpportunityOnEvaluation()
        {
            var store = new CountingStore();
            var data = new GameStateData { CurrentLevel = 12 };
            data.EndgameSnapshot = new Dictionary<string, object>
            {
                { "level", 12 }, { "lives", 2 },
                { "prefill_positions", new List<object>() },
                { "placed_cats", new List<object> { new object() } },
                { "marks", new List<object>() }, { "errors", new List<object>() }
            };
            var service = new GameStateService(
                data,
                store,
                dateProvider: new DateProvider("2026-08-08"));

            service.EvaluateDailyFirstEasy();

            Assert.That(service.IsDailyFirstEasyAvailable, Is.False);
            Assert.That(data.DailyFirstEasyDate, Is.EqualTo("2026-08-08"));
            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void GameFinished_RollsDailyCountersThenCountsExactlyOnce()
        {
            var store = new CountingStore();
            var data = new GameStateData
            {
                TodayDate = "2026-08-05",
                TodaySessionCount = 4,
                TodayPlayedCount = 9,
                TodayActiveSeconds = 30,
                ActiveDays = 2,
                RecentWinCountsByDay = new Dictionary<string, object>
                {
                    { "2026-08-05", 1 },
                    { "2026-08-06", 2 },
                    { "2026-08-07", 3 }
                }
            };
            var service = new GameStateService(
                data,
                store,
                dateProvider: new DateProvider("2026-08-09"));

            service.OnGameFinished();

            Assert.That(service.SessionPlayedCount, Is.EqualTo(1));
            Assert.That(data.TodayDate, Is.EqualTo("2026-08-09"));
            Assert.That(data.LastDaySessionCount, Is.EqualTo(4));
            Assert.That(data.TodaySessionCount, Is.Zero);
            Assert.That(data.TodayPlayedCount, Is.EqualTo(1));
            Assert.That(data.TodayActiveSeconds, Is.Zero);
            Assert.That(data.ActiveDays, Is.EqualTo(3));
            Assert.That(data.RecentWinCountsByDay, Does.Not.ContainKey("2026-08-05"));
            Assert.That(data.RecentWinCountsByDay, Does.Not.ContainKey("2026-08-06"));
            Assert.That(data.RecentWinCountsByDay, Contains.Key("2026-08-07"));
            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void TwoFailsThenCleanRetryWin_PreservesSourceDirtyAndRetrySemantics()
        {
            var store = new CountingStore();
            var data = new GameStateData
            {
                CurrentLevel = 21,
                CurrentStrategy = 2,
                RetryPuzzleLevel = 21,
                RetryPuzzleParameters = new Dictionary<string, object> { { "seed", 3 } }
            };
            var service = new GameStateService(data, store);
            var settled = new List<bool>();
            service.LevelSettled += settled.Add;

            service.OnLevelFailed(21);
            service.OnLevelFailed(21);
            service.ClearCurrentLevelDirty();
            service.OnLevelWon(21);

            Assert.That(data.CurrentLevel, Is.EqualTo(22));
            Assert.That(data.CurrentStrategy, Is.EqualTo(1));
            Assert.That(data.LastLevelCleanWin, Is.True);
            Assert.That(data.PreCatPendingStruggle, Is.True);
            Assert.That(data.PreCatPendingDemote, Is.True);
            Assert.That(data.PreCatFailCount, Is.Zero);
            Assert.That(data.PreCatFailLevel, Is.Zero);
            Assert.That(data.RetryPuzzleLevel, Is.Zero);
            Assert.That(data.RetryPuzzleParameters, Is.Empty);
            Assert.That(service.IsCurrentLevelRetried, Is.False);
            Assert.That(settled, Is.EqualTo(new[] { false, false, true }));
            Assert.That(store.SaveCount, Is.EqualTo(3));
        }

        [Test]
        public void ToolDda_WinDemotesImmediatelyWhenNextLevelIsNotSkipped()
        {
            var dda = new DdaRankConfig();
            dda.SetDebugOverride(DdaRankConfig.ValueToolRevive);
            var data = new GameStateData { CurrentLevel = 21, CurrentStrategy = 3 };
            var service = new GameStateService(data, ddaRankConfig: dda);
            service.MarkDdaToolOrReviveUsed();

            service.OnLevelWon(21);

            Assert.That(data.CurrentStrategy, Is.EqualTo(2));
            Assert.That(data.PreCatPendingDemote, Is.True);
            Assert.That(service.WasDdaToolOrReviveUsed, Is.False);
        }

        private sealed class CountingStore : IGameStatePlayerStore
        {
            public int SaveCount { get; private set; }
            public GameStateData LastData { get; private set; }

            public bool SavePlayer(GameStateData data)
            {
                SaveCount++;
                LastData = data;
                return true;
            }
        }

        private sealed class DateProvider : ICurrentDateProvider
        {
            public DateProvider(string date) { CurrentDate = date; }
            public string CurrentDate { get; }
        }

        private sealed class RecordingVibrationSink : IVibrationStateSink
        {
            public bool LastEnabled { get; private set; }
            public int CallCount { get; private set; }

            public void SetEnabled(bool enabled)
            {
                LastEnabled = enabled;
                CallCount++;
            }
        }

        private sealed class CombinedStore : IGameStatePlayerStore, IGameStateEndgameStore
        {
            public int PlayerSaveCount { get; private set; }
            public int ImmediateEndgameSaveCount { get; private set; }
            public int RequestedEndgameSaveCount { get; private set; }

            public bool SavePlayer(GameStateData data)
            {
                PlayerSaveCount++;
                return true;
            }

            public bool SaveEndgame(GameStateData data)
            {
                ImmediateEndgameSaveCount++;
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
