using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.Config;
using Meowdoku.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Meowdoku.Tests.EditMode
{
    public sealed class GameSessionSnapshotTests
    {
        [Test]
        public void BuildAndRead_RoundTripsPuzzleBoardScoreAndHistoryContract()
        {
            LevelEntry entry = Entry();
            var session = new GameSession(4, entry.RegionMap, entry.Solution, 1, new ScoreEncourageConfig());
            session.FinishEntering();
            Assert.That(session.TryApplyBoardEdit(0, 1, CellStateType.MARK, true, out _), Is.True);
            session.CommitCurrentStep();

            var context = new GameSessionSnapshotContext
            {
                Level = 12,
                BankIndex = 7,
                Entry = entry,
                PreType = "2"
            };
            context.PrefillPositions.Add(new Vector2Int(1, 2));
            Dictionary<string, object> snapshot = GameSessionSnapshot.Build(session, context);

            Assert.That(GameSessionSnapshot.TryRead(snapshot, 12, out GameSessionSnapshotRestore restore), Is.True);
            Assert.That(restore.Entry.Size, Is.EqualTo(4));
            Assert.That(restore.Session.Marks, Is.EquivalentTo(new[] { new Vector2Int(0, 1) }));
            Assert.That(restore.PrefillPositions, Is.EquivalentTo(new[] { new Vector2Int(1, 2) }));
            Assert.That(restore.PreType, Is.EqualTo("2"));
            Assert.That(restore.Session.StepHistoryData.Count, Is.EqualTo(1));
        }

        [Test]
        public void TryRead_RejectsWrongVersionLevelDeadSessionAndIllegalCat()
        {
            Dictionary<string, object> snapshot = ValidSnapshot();
            snapshot["version"] = 1;
            Assert.That(GameSessionSnapshot.TryRead(snapshot, 12, out _), Is.False);

            snapshot = ValidSnapshot();
            Assert.That(GameSessionSnapshot.TryRead(snapshot, 13, out _), Is.False);
            snapshot["lives"] = 0;
            Assert.That(GameSessionSnapshot.TryRead(snapshot, 12, out _), Is.False);

            snapshot = ValidSnapshot();
            snapshot["placed_cats"] = new List<object> { new List<object> { 0, 3 } };
            Assert.That(GameSessionSnapshot.TryRead(snapshot, 12, out _), Is.False);
        }

        [Test]
        public void HasUserProgress_AcceptsValidPrefillLikeGodotSource()
        {
            Dictionary<string, object> snapshot = ValidSnapshot();
            snapshot["prefill_positions"] = new List<object> { new List<object> { 1, 2 } };
            snapshot["placed_cats"] = new List<object> { new List<object> { 1, 2 } };
            Assert.That(GameSessionSnapshot.HasUserProgress(snapshot, 12), Is.True);
        }

        private static Dictionary<string, object> ValidSnapshot()
        {
            var session = new GameSession(4, Regions(), new[] { 0, 2, 3, 1 }, 1, new ScoreEncourageConfig());
            var context = new GameSessionSnapshotContext { Level = 12, BankIndex = 7, Entry = Entry() };
            return GameSessionSnapshot.Build(session, context);
        }

        private static LevelEntry Entry()
        {
            return LevelEntry.FromDictionary(new Dictionary<string, object>
            {
                { "size", 4 }, { "r", 1 }, { "seed", 99 },
                { "regionMap", Rows(Regions()) },
                { "solution", new List<object> { 0, 2, 3, 1 } }
            });
        }

        private static int[][] Regions()
        {
            return new[]
            {
                new[] { 0, 0, 1, 1 }, new[] { 0, 2, 2, 1 },
                new[] { 3, 2, 2, 1 }, new[] { 3, 3, 3, 1 }
            };
        }

        private static List<object> Rows(int[][] values)
        {
            var rows = new List<object>();
            for (int row = 0; row < values.Length; row++)
            {
                var columns = new List<object>();
                for (int column = 0; column < values[row].Length; column++) columns.Add(values[row][column]);
                rows.Add(columns);
            }
            return rows;
        }
    }
}
