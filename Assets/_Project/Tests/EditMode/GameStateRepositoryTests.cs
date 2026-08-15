using System;
using System.Collections.Generic;
using System.IO;
using Meowdoku.Core;
using NUnit.Framework;
using UnityEngine;

namespace Meowdoku.Tests.EditMode
{
    public sealed class GameStateRepositoryTests
    {
        private string _directory;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                "MeowdokuGameStateTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        [Test]
        public void MissingSave_LoadsSourceDefaults()
        {
            GameStateData data = new GameStateRepository(_directory).Load();

            Assert.That(data.CurrentLevel, Is.EqualTo(1));
            Assert.That(data.IsFirstSession, Is.True);
            Assert.That(data.CurrentStrategy, Is.EqualTo(1));
            Assert.That(data.ToolLocate, Is.EqualTo(99));
            Assert.That(data.ToolHint, Is.EqualTo(99));
            Assert.That(data.ToolUndo, Is.EqualTo(3));
            Assert.That(data.MusicOn, Is.True);
            Assert.That(data.SoundOn, Is.True);
            Assert.That(data.VibrationOn, Is.True);
            Assert.That(data.PeopleOn, Is.True);
            Assert.That(data.PatternModeOn, Is.False);
            Assert.That(data.PatternEntryDotDismissed, Is.False);
            Assert.That(data.PatternSwitchDotDismissed, Is.False);
            Assert.That(data.PushAskCount, Is.Zero);
            Assert.That(data.PushGuideLastDate, Is.Empty);
            Assert.That(data.PushGuideShownCount, Is.Zero);
            Assert.That(data.PushGuidePopupCount, Is.Zero);
            Assert.That(data.HasShownAttGuide, Is.False);
            Assert.That(data.PreCatLockPosition, Is.EqualTo(new Vector2Int(-1, -1)));
            Assert.That(data.SavedGameAutoMark, Is.EqualTo(-1));
        }

        [Test]
        public void PlayerState_RoundTripsP0Fields()
        {
            var repository = new GameStateRepository(_directory);
            var source = new GameStateData
            {
                CurrentLevel = 42,
                IsFirstSession = false,
                TutorialDone = true,
                CurrentStrategy = 3,
                DailyIndex = 17,
                DailyCompletedDate = "2026-08-07",
                MaxDailyDate = "2026-08-08",
                DailyElapsedSeconds = 754,
                DailyBeatPercent = 77.3f,
                DailyBestBeatPercent = 88.4f,
                DailyStartedDate = "2026-08-08",
                DailyFirstEasyDate = "2026-08-08",
                RecentWinCountsByDay = new Dictionary<string, object> { { "2026-08-08", 3 } },
                SessionCount = 7,
                TodaySessionCount = 2,
                LastDaySessionCount = 4,
                ActiveDays = 5,
                TodayPlayedCount = 6,
                TodayActiveSeconds = 70,
                TotalActiveSeconds = 800,
                TodayDate = "2026-08-08",
                ToolLocate = 4,
                ToolHint = 2,
                ToolUndo = 1,
                HasUsedTool = true,
                PropHighlightShown = true,
                PushAskCount = 2,
                PushGuideLastDate = "2026-08-03",
                PushGuideShownCount = 4,
                PushGuidePopupCount = 3,
                HasShownAttGuide = true,
                AppliedLocale = "vi",
                MusicOn = false,
                MusicUserModified = true,
                SoundOn = false,
                VibrationOn = false,
                PeopleOn = false,
                PatternModeOn = true,
                PatternEntryDotDismissed = true,
                PatternSwitchDotDismissed = true,
                RetryPuzzleLevel = 41,
                PreCatLockLevel = 42,
                PreCatLockType = "2",
                PreCatLockPosition = new Vector2Int(3, 5),
                SavedGameAutoMark = 1,
                BankProgress = new Dictionary<string, object> { { "6_3", 9 } },
                SavedAbGroups = new Dictionary<string, object> { { "swipe_protect", 1 } }
            };

            Assert.That(repository.SavePlayer(source), Is.True);
            GameStateData restored = repository.Load();

            Assert.That(restored.CurrentLevel, Is.EqualTo(42));
            Assert.That(restored.IsFirstSession, Is.False);
            Assert.That(restored.TutorialDone, Is.True);
            Assert.That(restored.CurrentStrategy, Is.EqualTo(3));
            Assert.That(restored.DailyIndex, Is.EqualTo(17));
            Assert.That(restored.DailyCompletedDate, Is.EqualTo("2026-08-07"));
            Assert.That(restored.MaxDailyDate, Is.EqualTo("2026-08-08"));
            Assert.That(restored.DailyElapsedSeconds, Is.EqualTo(754));
            Assert.That(restored.DailyBeatPercent, Is.EqualTo(77.3f));
            Assert.That(restored.DailyBestBeatPercent, Is.EqualTo(88.4f));
            Assert.That(restored.DailyStartedDate, Is.EqualTo("2026-08-08"));
            Assert.That(restored.DailyFirstEasyDate, Is.EqualTo("2026-08-08"));
            Assert.That(restored.RecentWinCountsByDay["2026-08-08"], Is.EqualTo(3L));
            Assert.That(restored.SessionCount, Is.EqualTo(7));
            Assert.That(restored.TodaySessionCount, Is.EqualTo(2));
            Assert.That(restored.LastDaySessionCount, Is.EqualTo(4));
            Assert.That(restored.ActiveDays, Is.EqualTo(5));
            Assert.That(restored.TodayPlayedCount, Is.EqualTo(6));
            Assert.That(restored.TodayActiveSeconds, Is.EqualTo(70));
            Assert.That(restored.TotalActiveSeconds, Is.EqualTo(800));
            Assert.That(restored.TodayDate, Is.EqualTo("2026-08-08"));
            Assert.That(restored.ToolLocate, Is.EqualTo(4));
            Assert.That(restored.ToolHint, Is.EqualTo(2));
            Assert.That(restored.ToolUndo, Is.EqualTo(1));
            Assert.That(restored.HasUsedTool, Is.True);
            Assert.That(restored.PropHighlightShown, Is.True);
            Assert.That(restored.PushAskCount, Is.EqualTo(2));
            Assert.That(restored.PushGuideLastDate, Is.EqualTo("2026-08-03"));
            Assert.That(restored.PushGuideShownCount, Is.EqualTo(4));
            Assert.That(restored.PushGuidePopupCount, Is.EqualTo(3));
            Assert.That(restored.HasShownAttGuide, Is.True);
            Assert.That(restored.AppliedLocale, Is.EqualTo("vi"));
            Assert.That(restored.MusicOn, Is.False);
            Assert.That(restored.MusicUserModified, Is.True);
            Assert.That(restored.SoundOn, Is.False);
            Assert.That(restored.VibrationOn, Is.False);
            Assert.That(restored.PeopleOn, Is.False);
            Assert.That(restored.PatternModeOn, Is.True);
            Assert.That(restored.PatternEntryDotDismissed, Is.True);
            Assert.That(restored.PatternSwitchDotDismissed, Is.True);
            Assert.That(restored.RetryPuzzleLevel, Is.EqualTo(41));
            Assert.That(restored.PreCatLockLevel, Is.EqualTo(42));
            Assert.That(restored.PreCatLockType, Is.EqualTo("2"));
            Assert.That(restored.PreCatLockPosition, Is.EqualTo(new Vector2Int(3, 5)));
            Assert.That(restored.SavedGameAutoMark, Is.EqualTo(1));
            Assert.That(restored.BankProgress["6_3"], Is.EqualTo(9L));
            Assert.That(restored.SavedAbGroups["swipe_protect"], Is.EqualTo(1L));
        }

        [Test]
        public void EndgameState_IsStoredSeparatelyAndCanBeCleared()
        {
            var repository = new GameStateRepository(_directory);
            var source = new GameStateData
            {
                CurrentLevel = 8,
                EndgameSnapshot = new Dictionary<string, object> { { "level", 8 } },
                MainGameId = "main-game-id"
            };
            repository.SavePlayer(source);
            Assert.That(repository.SaveEndgame(source), Is.True);

            GameStateData restored = repository.Load();
            Assert.That(restored.EndgameSnapshot["level"], Is.EqualTo(8L));
            Assert.That(restored.MainGameId, Is.EqualTo("main-game-id"));

            repository.ClearEndgame();
            restored = repository.Load();
            Assert.That(restored.EndgameSnapshot, Is.Empty);
            Assert.That(restored.MainGameId, Is.Empty);
        }

        [Test]
        public void BackgroundEndgameWrites_CoalesceAndFlushLatestImmutableState()
        {
            var repository = new GameStateRepository(
                _directory,
                useBackgroundEndgameWrites: true);
            var source = new GameStateData
            {
                EndgameSnapshot = new Dictionary<string, object> { { "level", 8 } }
            };

            Assert.That(repository.SaveEndgame(source), Is.True);
            source.EndgameSnapshot["level"] = 9;
            Assert.That(repository.SaveEndgame(source), Is.True);
            source.EndgameSnapshot["level"] = 10;

            Assert.That(repository.FlushEndgameWrites(), Is.True);
            GameStateData restored = new GameStateRepository(_directory).Load();
            Assert.That(restored.EndgameSnapshot["level"], Is.EqualTo(9L));

            repository.ClearEndgame();
            Assert.That(repository.FlushEndgameWrites(), Is.True);
            restored = new GameStateRepository(_directory).Load();
            Assert.That(restored.EndgameSnapshot, Is.Empty);
        }

        [Test]
        public void BackgroundPlayerWrites_CoalesceAndFlushLatestImmutableState()
        {
            var repository = new GameStateRepository(
                _directory,
                useBackgroundEndgameWrites: false,
                useBackgroundPlayerWrites: true);
            var source = new GameStateData { CurrentLevel = 8 };

            Assert.That(repository.SavePlayer(source), Is.True);
            source.CurrentLevel = 9;
            Assert.That(repository.SavePlayer(source), Is.True);
            source.CurrentLevel = 10;

            Assert.That(repository.FlushPlayerWrites(), Is.True);
            GameStateData restored = new GameStateRepository(_directory).Load();
            Assert.That(restored.CurrentLevel, Is.EqualTo(9));
        }

        [Test]
        public void BothPlayerSlotsCorrupt_LoadsSourceDefaultsSafely()
        {
            var repository = new GameStateRepository(_directory);
            Assert.That(repository.SavePlayer(new GameStateData
            {
                CurrentLevel = 17,
                CurrentStrategy = 3,
                ToolHint = 1
            }), Is.True);
            Assert.That(repository.SavePlayer(new GameStateData
            {
                CurrentLevel = 18,
                CurrentStrategy = 4,
                ToolHint = 0
            }), Is.True);

            string saveDirectory = Path.Combine(_directory, "save_store");
            File.WriteAllText(Path.Combine(saveDirectory, "save_a.cfg"),
                "corrupt-slot-a");
            File.WriteAllText(Path.Combine(saveDirectory, "save_b.cfg"),
                "corrupt-slot-b");

            GameStateData restored =
                new GameStateRepository(_directory).Load();

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored.CurrentLevel, Is.EqualTo(1));
            Assert.That(restored.CurrentStrategy, Is.EqualTo(1));
            Assert.That(restored.ToolHint, Is.EqualTo(99));
            Assert.That(restored.IsFirstSession, Is.True);
        }

        [Test]
        public void CorruptEndgame_DoesNotDiscardValidPlayerState()
        {
            var repository = new GameStateRepository(_directory);
            var source = new GameStateData
            {
                CurrentLevel = 31,
                CurrentStrategy = 4,
                TutorialDone = true,
                EndgameSnapshot = new Dictionary<string, object>
                {
                    { "level", 31 },
                    { "lives", 2 }
                },
                MainGameId = "durable-player"
            };
            Assert.That(repository.SavePlayer(source), Is.True);
            Assert.That(repository.SaveEndgame(source), Is.True);

            File.WriteAllText(
                Path.Combine(_directory, "save_store", "endgame.cfg"),
                "corrupt-endgame");

            GameStateData restored =
                new GameStateRepository(_directory).Load();

            Assert.That(restored.CurrentLevel, Is.EqualTo(31));
            Assert.That(restored.CurrentStrategy, Is.EqualTo(4));
            Assert.That(restored.TutorialDone, Is.True);
            Assert.That(restored.EndgameSnapshot, Is.Empty);
            Assert.That(restored.MainGameId, Is.Empty);
        }

        [Test]
        public void WrongTypedPlayerFields_FallBackIndependently()
        {
            var player = new Dictionary<string, object>
            {
                {
                    "progress",
                    new Dictionary<string, object>
                    {
                        { "current_level", "not-a-number" },
                        { "current_strategy", 3L },
                        { "music_on", "not-a-bool" },
                        { "sound_on", false }
                    }
                }
            };

            GameStateData restored = GameStateData.FromDocuments(player, null);

            Assert.That(restored.CurrentLevel, Is.EqualTo(1));
            Assert.That(restored.CurrentStrategy, Is.EqualTo(3));
            Assert.That(restored.MusicOn, Is.True);
            Assert.That(restored.SoundOn, Is.False);
        }
    }
}
