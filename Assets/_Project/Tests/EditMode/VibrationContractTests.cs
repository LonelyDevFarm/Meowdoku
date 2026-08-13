using Meowdoku.Core;
using Meowdoku.Core.Config;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class VibrationContractTests
    {
        [Test]
        public void LevelTable_MatchesGodotLowAndHighRamDurations()
        {
            AssertPulse(VibrationLevel.Level1, false, 40, 10);
            AssertPulse(VibrationLevel.Level2, false, 40, 80);
            AssertPulse(VibrationLevel.Level3, false, 40, 200);
            AssertPulse(VibrationLevel.Level5, false, 60, 250);
            AssertPulse(VibrationLevel.Level3, true, 20, 150);
            AssertPulse(VibrationLevel.Level5, true, 30, 250);
            AssertPulse(VibrationLevel.Level7, true, 130, 50);
            AssertPulse(VibrationLevel.Level10, false, 200, 50);
        }

        [Test]
        public void UnsupportedOrDisabledPlatform_IsSafeNoOp()
        {
            var platform = new RecordingPlatform { HasVibrator = false };
            var service = new VibrationService(platform, 4096);

            service.Play(VibrationLevel.Level3);
            Assert.That(platform.PlayCount, Is.Zero);

            platform.HasVibrator = true;
            service.SetEnabled(false);
            service.Play(VibrationLevel.Level3);
            Assert.That(platform.CancelCount, Is.EqualTo(1));
            Assert.That(platform.PlayCount, Is.Zero);

            service.SetEnabled(true);
            service.Play(VibrationLevel.Level3);
            Assert.That(platform.PlayCount, Is.EqualTo(1));
            Assert.That(platform.LastPulse.DurationMilliseconds, Is.EqualTo(20));
            Assert.That(platform.LastPulse.Amplitude, Is.EqualTo(150));
        }

        [TestCase(VibrateComboConfig.ValueControl, 5, -1)]
        [TestCase(VibrateComboConfig.ValueStrong, 1, (int)VibrationLevel.Level3)]
        [TestCase(VibrateComboConfig.ValueStrong, 3, (int)VibrationLevel.Level5)]
        [TestCase(VibrateComboConfig.ValueStronger, 5, (int)VibrationLevel.Level6)]
        [TestCase(VibrateComboConfig.ValueWeakToStrong, 1, (int)VibrationLevel.Level2)]
        [TestCase(VibrateComboConfig.ValueWeakerToStrong, 1, (int)VibrationLevel.Level1)]
        [TestCase(VibrateComboConfig.ValueWeakerToStrong, 4, (int)VibrationLevel.Level5)]
        public void VibrateComboConfig_MatchesSourceGroups(
            int value,
            int combo,
            int expected)
        {
            var config = new VibrateComboConfig();
            config.SetDebugOverride(value);
            Assert.That(config.ComboVibrationLevel(combo), Is.EqualTo(expected));
        }

        private static void AssertPulse(
            VibrationLevel level,
            bool highRam,
            int duration,
            int amplitude)
        {
            VibrationPulse pulse = VibrationService.ResolvePulse(level, highRam);
            Assert.That(pulse.DurationMilliseconds, Is.EqualTo(duration));
            Assert.That(pulse.Amplitude, Is.EqualTo(amplitude));
        }

        private sealed class RecordingPlatform : IVibrationPlatformAdapter
        {
            public bool HasVibrator { get; set; }
            public bool HasAmplitudeControl => true;
            public int PlayCount { get; private set; }
            public int CancelCount { get; private set; }
            public VibrationPulse LastPulse { get; private set; }

            public void Vibrate(VibrationPulse pulse)
            {
                PlayCount++;
                LastPulse = pulse;
            }

            public void Cancel()
            {
                CancelCount++;
            }
        }
    }
}
