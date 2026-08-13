using System.Collections;
using System.Collections.Generic;
using Meowdoku.Services;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Meowdoku.Tests.PlayMode
{
    public sealed class SoundServicePlayModeTests
    {
        [UnityTest]
        public IEnumerator PoolSettingsAndDuckLifecycle_StaySourceCompatible()
        {
            var settings = new MutableSettings();
            SoundCatalog catalog = ScriptableObject.CreateInstance<SoundCatalog>();
            AudioClip clip = AudioClip.Create("audio-contract", 441, 1, 44100, false);
            catalog.ReplaceEntries(
                new[]
                {
                    new SoundClipEntry { kind = SoundKind.BoardEnter, clip = clip },
                    new SoundClipEntry { kind = SoundKind.MarkX, clip = clip },
                    new SoundClipEntry { kind = SoundKind.MarkCat, clip = clip }
                },
                new List<PathSoundClipEntry>());

            var gameObject = new GameObject("SoundServiceTest");
            SoundService service = gameObject.AddComponent<SoundService>();
            service.Configure(catalog, settings);

            Assert.That(service.FixedVoiceCount(SoundKind.BoardEnter), Is.EqualTo(1));
            Assert.That(service.FixedVoiceCount(SoundKind.MarkX), Is.EqualTo(4));
            Assert.That(service.FixedVoiceCount(SoundKind.MarkCat), Is.EqualTo(3));

            service.SetSilent(true);
            service.Play(SoundKind.MarkX);
            Assert.That(service.FixedPlayCount(SoundKind.MarkX), Is.Zero);
            service.SetSilent(false);
            settings.SoundOn = false;
            service.Play(SoundKind.MarkX);
            Assert.That(service.FixedPlayCount(SoundKind.MarkX), Is.Zero);
            settings.SoundOn = true;
            service.Play(SoundKind.MarkX);
            Assert.That(service.FixedPlayCount(SoundKind.MarkX), Is.EqualTo(1));

            service.StartBgm();
            service.SetBgmPaused(true);
            Assert.That(service.BgmPausedForDialog, Is.True);
            service.SetBgmPaused(false);
            service.NotifyAdShown("banner");
            Assert.That(service.BgmPausedForAd, Is.False);
            service.NotifyAdShown("interstitial");
            Assert.That(service.BgmPausedForAd, Is.True);
            service.NotifyAdClosed("interstitial");
            Assert.That(service.BgmPausedForAd, Is.False);
            service.Play(SoundKind.BoardEnter);
            Assert.That(service.BgmDucking, Is.True);
            yield return new WaitForSecondsRealtime(0.03f);
            Assert.That(service.BgmDucking, Is.False);

            Object.Destroy(gameObject);
            Object.Destroy(catalog);
            Object.Destroy(clip);
            yield return null;
        }

        private sealed class MutableSettings : ISoundSettingsReader
        {
            public bool MusicOn { get; set; } = true;
            public bool SoundOn { get; set; } = true;
            public bool PeopleOn { get; set; } = true;
        }
    }
}
