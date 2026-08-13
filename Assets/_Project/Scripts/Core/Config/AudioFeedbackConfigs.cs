using System;
using System.Collections.Generic;

namespace Meowdoku.Core.Config
{
    public sealed class VibrateComboConfig : AbConfigBase<int>
    {
        public const int ValueControl = 0;
        public const int ValueStrong = 1;
        public const int ValueStronger = 2;
        public const int ValueWeakToStrong = 3;
        public const int ValueWeakerToStrong = 4;

        public VibrateComboConfig()
            : base("vibrate_combo", ValueControl, AbConfigTiming.GameStart) { }

        public bool IsEnabled() => Value != ValueControl;

        public int ComboVibrationLevel(int combo)
        {
            int value = Math.Max(combo, 1);
            switch (Value)
            {
                case ValueStrong:
                    return value <= 2
                        ? (int)VibrationLevel.Level3
                        : (int)VibrationLevel.Level5;
                case ValueStronger:
                    if (value <= 2) return (int)VibrationLevel.Level3;
                    return value <= 4
                        ? (int)VibrationLevel.Level5
                        : (int)VibrationLevel.Level6;
                case ValueWeakToStrong:
                    if (value == 1) return (int)VibrationLevel.Level2;
                    return value == 2
                        ? (int)VibrationLevel.Level3
                        : (int)VibrationLevel.Level5;
                case ValueWeakerToStrong:
                    if (value == 1) return (int)VibrationLevel.Level1;
                    if (value == 2) return (int)VibrationLevel.Level2;
                    if (value == 3) return (int)VibrationLevel.Level3;
                    return value == 4
                        ? (int)VibrationLevel.Level5
                        : (int)VibrationLevel.Level6;
                default:
                    return -1;
            }
        }
    }

    public sealed class ComboVoiceConfig : AbConfigBase<int>
    {
        public const int ValueRealMale1 = 6;
        public const int ValueRealMaleAText1 = 9;
        public const int ValueRealMaleBText2 = 10;
        public const int ValueRealFemaleMeowText3 = 11;

        private static readonly IReadOnlyDictionary<int, string> Male1 =
            new Dictionary<int, string>
            {
                { 3, Path("combo_nice_s6.ogg") },
                { 4, Path("combo_great_s6.ogg") },
                { 5, Path("combo_perfect_s6.ogg") },
                { 6, Path("combo_excellent_s6.ogg") },
                { 7, Path("combo_amazing_s6.ogg") },
                { 8, Path("combo_unbelievable_s6.ogg") }
            };

        private static readonly IReadOnlyDictionary<int, string> MaleAText1 =
            new Dictionary<int, string>
            {
                { 9, Path("combo_incredible_s9.ogg") },
                { 10, Path("combo_phenomenal_s9.ogg") },
                { 11, Path("combo_spectacular_s9.ogg") },
                { 12, Path("combo_legendary_s9.ogg") }
            };

        private static readonly IReadOnlyDictionary<int, string> MaleBText2 =
            new Dictionary<int, string>
            {
                { 9, Path("combo_unreal_s10.ogg") },
                { 10, Path("combo_insane_s10.ogg") },
                { 11, Path("combo_epic_s10.ogg") },
                { 12, Path("combo_breathtaking_s10.ogg") }
            };

        private static readonly IReadOnlyDictionary<int, string> FemaleMeowText3 =
            new Dictionary<int, string>
            {
                { 3, Path("combo_nice_meow_s11.ogg") },
                { 4, Path("combo_great_kitty_s11.ogg") },
                { 5, Path("combo_purr_fect_s11.ogg") },
                { 6, Path("combo_excellent_kitten_s11.ogg") },
                { 7, Path("combo_amazing_mew_s11.ogg") },
                { 8, Path("combo_paw_some_s11.ogg") },
                { 9, Path("combo_incredible_whiskers_s11.ogg") },
                { 10, Path("combo_catnip_level_s11.ogg") },
                { 11, Path("combo_epic_pounce_s11.ogg") },
                { 12, Path("combo_legendary_furball_s11.ogg") }
            };

        public ComboVoiceConfig()
            : base("combo_voice", ValueRealMale1, AbConfigTiming.GameStart) { }

        public string GetComboVoice(int comboCount)
        {
            int level = Math.Max(3, Math.Min(12, comboCount));
            IReadOnlyDictionary<int, string> paths = ResolveSet(Value);
            if (paths.TryGetValue(level, out string path)) return path;
            if ((Value == ValueRealMaleAText1 || Value == ValueRealMaleBText2) &&
                Male1.TryGetValue(level, out path))
                return path;
            return paths.TryGetValue(8, out path) ? path : string.Empty;
        }

        private static IReadOnlyDictionary<int, string> ResolveSet(int value)
        {
            switch (value)
            {
                case ValueRealMaleAText1: return MaleAText1;
                case ValueRealMaleBText2: return MaleBText2;
                case ValueRealFemaleMeowText3: return FemaleMeowText3;
                default: return Male1;
            }
        }

        private static string Path(string fileName)
        {
            return "res://assets/audio/sfx/" + fileName;
        }
    }

    public sealed class MeowFeedbackConfig : AbConfigBase<int>
    {
        public const int ValueDisabled = 0;
        public const int ValueEvery = 1;
        public const int ValueFirstOnly = 2;
        public const int ValueCrescendo = 3;
        public const int ValueRandom = 4;

        public MeowFeedbackConfig()
            : base("meow_feedback", ValueDisabled, AbConfigTiming.AppStart) { }

        public bool IsEnabled() => Value != ValueDisabled;

        public string GetMeowPath(int triggerIndex, int randomVariant = 1)
        {
            switch (Value)
            {
                case ValueEvery:
                    return Path("meow_single.ogg");
                case ValueFirstOnly:
                    return triggerIndex == 1 ? Path("meow_single.ogg") : string.Empty;
                case ValueCrescendo:
                    return Path($"meow_cresc_{ClampVariant(triggerIndex)}.ogg");
                case ValueRandom:
                    return Path($"meow_rand_{ClampVariant(randomVariant)}.ogg");
                default:
                    return string.Empty;
            }
        }

        private static int ClampVariant(int value) => Math.Max(1, Math.Min(7, value));
        private static string Path(string fileName) =>
            "res://assets/audio/sfx/" + fileName;
    }

    public sealed class ThumbUpConfig : AbConfigBase<int>
    {
        public const int ValueDisableAll = 0;
        public const int ValueLikeOnly = 1;
        public const int ValueClapOnly = 2;
        public const int ValueBlowTrumpetOnly = 3;
        public const int ValueAllByPriority = 4;
        public const int ValueCorrectionCheer = 5;
        public const int ValueMissedCat = 6;
        public const int ValueHawkEye = 7;
        public const int ValueAllFive = 8;
        public const int ValueAllFiveRelaxed = 9;
        public const int ValueMagnifierCat = 10;
        public const int ValueMissedCross = 11;
        public const int ValueAllFiveV3 = 12;

        public ThumbUpConfig()
            : base("thumb_up", ValueDisableAll, AbConfigTiming.GameStart) { }

        public bool IsAnyFeedbackEnabled() => Value != ValueDisableAll;
    }
}
