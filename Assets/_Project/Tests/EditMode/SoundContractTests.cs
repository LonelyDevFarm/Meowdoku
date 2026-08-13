using System;
using System.Linq;
using Meowdoku.Services;
using NUnit.Framework;
using UnityEditor;

namespace Meowdoku.Tests.EditMode
{
    public sealed class SoundContractTests
    {
        [Test]
        public void KindOrderAndMappedCount_MatchGodotEnumAndPathTable()
        {
            Assert.That(Enum.GetValues(typeof(SoundKind)), Has.Length.EqualTo(29));
            Assert.That((int)SoundKind.BoardEnter, Is.Zero);
            Assert.That((int)SoundKind.PassPageSettle, Is.EqualTo(28));

            int mapped = 0;
            foreach (SoundKind kind in Enum.GetValues(typeof(SoundKind)))
                if (SoundContract.SourcePath(kind).Length > 0) mapped++;

            Assert.That(mapped, Is.EqualTo(27));
            Assert.That(SoundContract.SourcePath(SoundKind.MarkWrongLow), Is.Empty);
            Assert.That(SoundContract.SourcePath(SoundKind.LevelFailLow), Is.Empty);
            Assert.That(
                SoundContract.SourcePath(SoundKind.PassPageSettle),
                Is.EqualTo("res://assets/audio/sfx/pass_page_settle.ogg"));
        }

        [TestCase(SoundKind.MarkX, 4)]
        [TestCase(SoundKind.UnmarkX, 4)]
        [TestCase(SoundKind.MarkCat, 3)]
        [TestCase(SoundKind.MarkWrong, 2)]
        [TestCase(SoundKind.ButtonClick, 4)]
        [TestCase(SoundKind.Clap, 2)]
        [TestCase(SoundKind.BlowTrumpet, 2)]
        [TestCase(SoundKind.Combo, 2)]
        [TestCase(SoundKind.ComboVoice, 2)]
        [TestCase(SoundKind.MarkXSoft1, 4)]
        [TestCase(SoundKind.MarkXSoft2, 4)]
        [TestCase(SoundKind.LevelWin, 1)]
        public void FixedPolyphony_MatchesSource(SoundKind kind, int expected)
        {
            Assert.That(SoundContract.Polyphony(kind), Is.EqualTo(expected));
        }

        [Test]
        public void PlaybackGates_UseSeparateSoundAndPeopleSettings()
        {
            Assert.That(SoundContract.CanPlaySfx(false, true, SoundKind.MarkCat), Is.True);
            Assert.That(SoundContract.CanPlaySfx(true, true, SoundKind.MarkCat), Is.False);
            Assert.That(SoundContract.CanPlaySfx(false, false, SoundKind.MarkCat), Is.False);
            Assert.That(SoundContract.CanPlaySfx(false, true, SoundKind.MarkWrongLow), Is.False);

            Assert.That(SoundContract.CanPlayPeople(false, true, "voice.ogg"), Is.True);
            Assert.That(SoundContract.CanPlayPeople(false, false, "voice.ogg"), Is.False);
            Assert.That(SoundContract.CanPlayPeople(false, true, string.Empty), Is.False);
            Assert.That(SoundContract.CanPlayMeow(false, true, "meow.ogg"), Is.True);
            Assert.That(SoundContract.CanPlayMeow(false, false, "meow.ogg"), Is.False);
        }

        [Test]
        public void BgmContract_RemainsDisabledAndOnlyTwoKindsDuck()
        {
            Assert.That(SoundContract.ShouldPlayBgm(), Is.False);
            Assert.That(SoundContract.DucksBgm(SoundKind.BoardEnter), Is.True);
            Assert.That(SoundContract.DucksBgm(SoundKind.LevelWin), Is.True);
            Assert.That(SoundContract.DucksBgm(SoundKind.LevelFail), Is.False);
        }

        [Test]
        public void DynamicCatalogPaths_CoverSourceComboAndMeowSets()
        {
            Assert.That(SoundContract.DynamicSourcePaths, Has.Count.EqualTo(39));
            Assert.That(SoundContract.DynamicSourcePaths, Is.Unique);
            Assert.That(SoundContract.DynamicSourcePaths.Contains(
                "res://assets/audio/sfx/combo_nice_s6.ogg"), Is.True);
            Assert.That(SoundContract.DynamicSourcePaths.Contains(
                "res://assets/audio/sfx/meow_rand_7.ogg"), Is.True);
        }

        [Test]
        public void SerializedCatalog_ContainsEveryMappedClipAndDynamicPath()
        {
            SoundCatalog catalog = AssetDatabase.LoadAssetAtPath<SoundCatalog>(
                "Assets/_Project/Settings/SoundCatalog.asset");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.FixedClips, Has.Count.EqualTo(27));
            Assert.That(catalog.PathClips, Has.Count.EqualTo(39));

            foreach (SoundClipEntry entry in catalog.FixedClips)
            {
                Assert.That(entry, Is.Not.Null);
                Assert.That(entry.clip, Is.Not.Null, entry.kind.ToString());
                Assert.That(SoundContract.SourcePath(entry.kind), Is.Not.Empty);
            }
            foreach (string sourcePath in SoundContract.DynamicSourcePaths)
            {
                Assert.That(catalog.TryGetPathClip(sourcePath, out var clip),
                    Is.True,
                    sourcePath);
                Assert.That(clip, Is.Not.Null, sourcePath);
            }
        }
    }
}
