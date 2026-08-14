using System;
using System.Collections.Generic;
using System.Linq;
using Meowdoku.Services;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Meowdoku.Tests.EditMode
{
    public sealed class SoundContractTests
    {
        [Test]
        public void SerializedCatalog_AllClipsDecodeWithFiniteNonSilentSignal()
        {
            SoundCatalog catalog = AssetDatabase.LoadAssetAtPath<SoundCatalog>(
                "Assets/_Project/Settings/SoundCatalog.asset");
            Assert.That(catalog, Is.Not.Null);

            var clips = new HashSet<AudioClip>();
            foreach (SoundClipEntry entry in catalog.FixedClips)
                if (entry?.clip != null) clips.Add(entry.clip);
            foreach (PathSoundClipEntry entry in catalog.PathClips)
                if (entry?.clip != null) clips.Add(entry.clip);

            Assert.That(clips, Has.Count.EqualTo(66));
            foreach (AudioClip clip in clips)
            {
                Assert.That(clip.samples, Is.GreaterThan(0), clip.name);
                Assert.That(clip.channels, Is.GreaterThan(0), clip.name);
                Assert.That(clip.frequency, Is.GreaterThan(0), clip.name);
                Assert.That(clip.length, Is.GreaterThan(0f), clip.name);
                Assert.That(clip.length, Is.LessThan(10f), clip.name);

                if (clip.loadState != AudioDataLoadState.Loaded)
                    clip.LoadAudioData();
                Assert.That(clip.loadState,
                    Is.Not.EqualTo(AudioDataLoadState.Failed), clip.name);

                var samples = new float[clip.samples * clip.channels];
                Assert.That(clip.GetData(samples, 0), Is.True, clip.name);

                float peak = 0f;
                double squareSum = 0d;
                foreach (float sample in samples)
                {
                    Assert.That(float.IsNaN(sample), Is.False, clip.name);
                    Assert.That(float.IsInfinity(sample), Is.False, clip.name);
                    peak = Mathf.Max(peak, Mathf.Abs(sample));
                    squareSum += sample * sample;
                }

                double rms = Math.Sqrt(squareSum / samples.Length);
                Assert.That(peak, Is.GreaterThan(0.0001f), clip.name);
                Assert.That(rms, Is.GreaterThan(0.00001d), clip.name);
                Assert.That(peak, Is.LessThanOrEqualTo(1.0001f), clip.name);
            }
        }
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
