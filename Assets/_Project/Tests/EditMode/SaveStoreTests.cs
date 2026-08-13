using System;
using System.Collections.Generic;
using System.IO;
using Meowdoku.Core;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class SaveStoreTests
    {
        private string _directory;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                "MeowdokuSaveStoreTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        [Test]
        public void SingleSlot_SaveAndLoadRoundTrip()
        {
            string path = Path.Combine(_directory, "endgame.cfg");
            var store = new SaveStore("test-password", _directory, false, path);

            Assert.That(store.SaveConfig(Document(7)), Is.True);
            Dictionary<string, object> restored = store.LoadConfig();

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored["level"], Is.EqualTo(7L));
        }

        [Test]
        public void DualSlot_AlternatesAndUsesLatestGoodSlot()
        {
            Paths paths = CreatePaths();
            SaveStore store = CreateDualStore(paths, "test-password");

            Assert.That(store.SaveConfig(Document(10)), Is.True);
            Assert.That(File.ReadAllText(paths.Flag), Is.EqualTo("A"));
            Assert.That(store.SaveConfig(Document(11)), Is.True);
            Assert.That(File.ReadAllText(paths.Flag), Is.EqualTo("B"));

            Assert.That(store.LoadConfig()["level"], Is.EqualTo(11L));
        }

        [Test]
        public void DualSlot_CorruptPrimaryFallsBackToPreviousSlot()
        {
            Paths paths = CreatePaths();
            SaveStore store = CreateDualStore(paths, "test-password");
            store.SaveConfig(Document(20));
            store.SaveConfig(Document(21));
            File.WriteAllText(paths.B, "corrupted");

            Dictionary<string, object> restored = store.LoadConfig();

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored["level"], Is.EqualTo(20L));
        }

        [Test]
        public void WrongPassword_CannotReadPayload()
        {
            Paths paths = CreatePaths();
            SaveStore writer = CreateDualStore(paths, "correct-password");
            writer.SaveConfig(Document(30));
            SaveStore reader = CreateDualStore(paths, "wrong-password");

            Assert.That(reader.LoadConfig(), Is.Null);
        }

        [Test]
        public void TamperedPayload_IsRejectedByAuthenticationTag()
        {
            string path = Path.Combine(_directory, "endgame.cfg");
            var store = new SaveStore("test-password", _directory, false, path);
            store.SaveConfig(Document(40));
            byte[] payload = File.ReadAllBytes(path);
            payload[payload.Length / 2] ^= 0x7f;
            File.WriteAllBytes(path, payload);

            Assert.That(store.LoadConfig(), Is.Null);
        }

        [Test]
        public void LegacyPath_IsUsedAfterBothDualSlotsFail()
        {
            Paths paths = CreatePaths();
            var legacyWriter = new SaveStore(
                "test-password",
                _directory,
                false,
                paths.Legacy);
            legacyWriter.SaveConfig(Document(50));
            SaveStore reader = CreateDualStore(paths, "test-password");

            Assert.That(reader.LoadConfig()["level"], Is.EqualTo(50L));
        }

        [Test]
        public void Remove_DeletesSingleSlotPayload()
        {
            string path = Path.Combine(_directory, "endgame.cfg");
            var store = new SaveStore("test-password", _directory, false, path);
            store.SaveConfig(Document(60));

            store.Remove();

            Assert.That(File.Exists(path), Is.False);
            Assert.That(store.LoadConfig(), Is.Null);
        }

        [Test]
        public void DualSlot_BothCorruptWithoutLegacyReturnsNull()
        {
            Paths paths = CreatePaths();
            SaveStore store = CreateDualStore(paths, "test-password");
            store.SaveConfig(Document(70));
            store.SaveConfig(Document(71));
            File.WriteAllText(paths.A, "corrupt-a");
            File.WriteAllText(paths.B, "corrupt-b");

            Assert.That(store.LoadConfig(), Is.Null);
        }

        [Test]
        public void InvalidFlag_DefaultsToSlotAThenSlotB()
        {
            Paths paths = CreatePaths();
            SaveStore store = CreateDualStore(paths, "test-password");
            store.SaveConfig(Document(80));
            store.SaveConfig(Document(81));
            File.WriteAllText(paths.Flag, "invalid");

            Dictionary<string, object> restored = store.LoadConfig();

            Assert.That(restored["level"], Is.EqualTo(80L));
        }

        [Test]
        public void LegacyMigration_WritesFirstSlotAndPreservesLegacy()
        {
            Paths paths = CreatePaths();
            var legacyWriter = new SaveStore(
                "test-password", _directory, false, paths.Legacy);
            legacyWriter.SaveConfig(Document(90));
            SaveStore store = CreateDualStore(paths, "test-password");

            LegacySaveMigrationResult result = store.MigrateLegacyIfNeeded();

            Assert.That(result, Is.EqualTo(LegacySaveMigrationResult.Migrated));
            Assert.That(File.Exists(paths.A), Is.True);
            Assert.That(File.Exists(paths.B), Is.False);
            Assert.That(File.ReadAllText(paths.Flag), Is.EqualTo("A"));
            Assert.That(File.Exists(paths.Legacy), Is.True);
            Assert.That(store.LoadConfig()["level"], Is.EqualTo(90L));

            byte[] firstSlotAfterMigration = File.ReadAllBytes(paths.A);
            Assert.That(
                store.MigrateLegacyIfNeeded(),
                Is.EqualTo(LegacySaveMigrationResult.NotNeeded));
            Assert.That(
                File.ReadAllBytes(paths.A),
                Is.EqualTo(firstSlotAfterMigration),
                "A second migration check must not rewrite the committed slot.");
        }

        [Test]
        public void CorruptLegacyMigrationFailsWithoutCreatingFlag()
        {
            Paths paths = CreatePaths();
            File.WriteAllText(paths.Legacy, "corrupt-legacy");
            SaveStore store = CreateDualStore(paths, "test-password");

            Assert.That(
                store.MigrateLegacyIfNeeded(),
                Is.EqualTo(LegacySaveMigrationResult.Failed));
            Assert.That(File.Exists(paths.Flag), Is.False);
            Assert.That(File.Exists(paths.A), Is.False);
        }

        [Test]
        public void ExistingInvalidFlag_PreventsProactiveLegacyMigration()
        {
            Paths paths = CreatePaths();
            var legacyWriter = new SaveStore(
                "test-password", _directory, false, paths.Legacy);
            legacyWriter.SaveConfig(Document(100));
            File.WriteAllText(paths.Flag, "invalid");
            SaveStore store = CreateDualStore(paths, "test-password");

            Assert.That(
                store.MigrateLegacyIfNeeded(),
                Is.EqualTo(LegacySaveMigrationResult.NotNeeded));
            Assert.That(File.Exists(paths.A), Is.False);
            Assert.That(store.LoadConfig()["level"], Is.EqualTo(100L));
        }

        [Test]
        public void OrphanTemporaryFile_IsNotTreatedAsCommittedSave()
        {
            Paths paths = CreatePaths();
            File.WriteAllText(paths.A + ".tmp", "partial-write");
            SaveStore store = CreateDualStore(paths, "test-password");

            Assert.That(store.LoadConfig(), Is.Null);
        }

        [Test]
        public void DualSlot_VerifyFailurePreservesCommittedSlotsAndFlag()
        {
            Paths paths = CreatePaths();
            SaveStore store = CreateDualStore(paths, "test-password");
            Assert.That(store.SaveConfig(Document(110)), Is.True);
            Assert.That(store.SaveConfig(Document(111)), Is.True);
            byte[] committedSlotA = File.ReadAllBytes(paths.A);

            SaveStore failingStore = CreateDualStore(
                paths,
                "test-password",
                temporaryPath => File.WriteAllText(
                    temporaryPath,
                    "corrupt-before-verify"));

            Assert.That(failingStore.SaveConfig(Document(112)), Is.False);
            Assert.That(File.ReadAllText(paths.Flag), Is.EqualTo("B"));
            Assert.That(File.ReadAllBytes(paths.A), Is.EqualTo(committedSlotA));
            Assert.That(File.Exists(paths.A + ".tmp"), Is.False);
            Assert.That(failingStore.LoadConfig()["level"], Is.EqualTo(111L));
        }

        private static Dictionary<string, object> Document(int level)
        {
            return new Dictionary<string, object>
            {
                { "level", level },
                { "settings", new Dictionary<string, object> { { "sound", true } } }
            };
        }

        private Paths CreatePaths()
        {
            return new Paths
            {
                A = Path.Combine(_directory, "save_a.cfg"),
                B = Path.Combine(_directory, "save_b.cfg"),
                Flag = Path.Combine(_directory, "flag.txt"),
                Legacy = Path.Combine(_directory, "save.cfg")
            };
        }

        private SaveStore CreateDualStore(
            Paths paths,
            string password,
            Action<string> beforeVerify = null)
        {
            if (beforeVerify != null)
            {
                return new SaveStore(
                    password,
                    _directory,
                    true,
                    paths.A,
                    paths.B,
                    paths.Flag,
                    paths.Legacy,
                    beforeVerify);
            }

            return new SaveStore(
                password,
                _directory,
                true,
                paths.A,
                paths.B,
                paths.Flag,
                paths.Legacy);
        }

        private sealed class Paths
        {
            public string A;
            public string B;
            public string Flag;
            public string Legacy;
        }
    }
}
