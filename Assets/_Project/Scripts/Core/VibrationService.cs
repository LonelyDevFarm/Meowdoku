using System;
using UnityEngine;

namespace Meowdoku.Core
{
    // Numeric order matches VibrateManager.Level in the Godot source.
    public enum VibrationLevel
    {
        Level1 = 0,
        Level2 = 1,
        Level3 = 2,
        Level4 = 3,
        Level5 = 4,
        Level6 = 5,
        Level7 = 6,
        Level10 = 7
    }

    public readonly struct VibrationPulse
    {
        public VibrationPulse(int durationMilliseconds, int amplitude)
        {
            DurationMilliseconds = Math.Max(0, durationMilliseconds);
            Amplitude = Math.Max(1, Math.Min(255, amplitude));
        }

        public int DurationMilliseconds { get; }
        public int Amplitude { get; }
    }

    public interface IVibrationPlatformAdapter
    {
        bool HasVibrator { get; }
        bool HasAmplitudeControl { get; }
        void Vibrate(VibrationPulse pulse);
        void Cancel();
    }

    /// <summary>
    /// Source-compatible vibration gate and level table. Android uses the
    /// platform VibrationEffect API when available; iOS falls back to Unity's
    /// coarse vibration because Unity exposes no native haptic-level API.
    /// Unsupported platforms intentionally remain silent.
    /// </summary>
    public sealed class VibrationService : IVibrationStateSink
    {
        private const int HighRamThresholdMb = 3800;
        private readonly IVibrationPlatformAdapter _platform;
        private readonly bool _highRam;
        private bool _enabled = true;

        public VibrationService(
            IVibrationPlatformAdapter platform,
            int systemMemorySizeMb)
        {
            _platform = platform ?? throw new ArgumentNullException(nameof(platform));
            _highRam = systemMemorySizeMb > HighRamThresholdMb;
        }

        public bool Enabled => _enabled;
        public bool HasVibrator => _platform.HasVibrator;
        public bool HasAmplitudeControl => _platform.HasAmplitudeControl;

        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            if (!enabled) _platform.Cancel();
        }

        public void Play(VibrationLevel level)
        {
            if (!_enabled || !_platform.HasVibrator) return;
            _platform.Vibrate(ResolvePulse(level, _highRam));
        }

        public void Play(int sourceLevel)
        {
            if (sourceLevel < (int)VibrationLevel.Level1 ||
                sourceLevel > (int)VibrationLevel.Level10)
                return;
            Play((VibrationLevel)sourceLevel);
        }

        public void Cancel()
        {
            _platform.Cancel();
        }

        public static VibrationPulse ResolvePulse(VibrationLevel level, bool highRam)
        {
            switch (level)
            {
                case VibrationLevel.Level1:
                    return new VibrationPulse(highRam ? 20 : 40, 10);
                case VibrationLevel.Level2:
                    return new VibrationPulse(highRam ? 20 : 40, 80);
                case VibrationLevel.Level3:
                    return new VibrationPulse(highRam ? 20 : 40, highRam ? 150 : 200);
                case VibrationLevel.Level4:
                    return new VibrationPulse(200, 50);
                case VibrationLevel.Level5:
                    return new VibrationPulse(highRam ? 30 : 60, 250);
                case VibrationLevel.Level6:
                    return new VibrationPulse(highRam ? 30 : 60, 255);
                case VibrationLevel.Level7:
                    return new VibrationPulse(130, 50);
                case VibrationLevel.Level10:
                    return new VibrationPulse(200, 50);
                default:
                    return new VibrationPulse(0, 1);
            }
        }
    }

    public static class VibrationRuntime
    {
        private static readonly VibrationService Service = new VibrationService(
            new UnityVibrationPlatformAdapter(),
            SystemInfo.systemMemorySize);

        public static VibrationService Current => Service;
    }

    internal sealed class UnityVibrationPlatformAdapter : IVibrationPlatformAdapter
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private const int VibratorManagerApiLevel = 31;
        private const int VibrationEffectApiLevel = 26;
        private const int DefaultAmplitude = -1;
#endif

        public bool HasVibrator
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                try
                {
                    using AndroidJavaObject vibrator = GetAndroidVibrator();
                    return vibrator != null && vibrator.Call<bool>("hasVibrator");
                }
                catch
                {
                    return SystemInfo.supportsVibration;
                }
#elif UNITY_IOS && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        public bool HasAmplitudeControl
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                try
                {
                    using AndroidJavaObject vibrator = GetAndroidVibrator();
                    return AndroidSdkInt() >= 26 && vibrator != null &&
                           vibrator.Call<bool>("hasAmplitudeControl");
                }
                catch
                {
                    return false;
                }
#else
                return false;
#endif
            }
        }

        public void Vibrate(VibrationPulse pulse)
        {
            if (pulse.DurationMilliseconds <= 0) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using AndroidJavaObject vibrator = GetAndroidVibrator();
                if (vibrator == null) return;

                if (AndroidSdkInt() >= VibrationEffectApiLevel)
                {
                    using var effectClass = new AndroidJavaClass("android.os.VibrationEffect");
                    int amplitude = vibrator.Call<bool>("hasAmplitudeControl")
                        ? pulse.Amplitude
                        : DefaultAmplitude;
                    using AndroidJavaObject effect = effectClass.CallStatic<AndroidJavaObject>(
                        "createOneShot",
                        (long)pulse.DurationMilliseconds,
                        amplitude);
                    vibrator.Call("vibrate", effect);
                    return;
                }

                vibrator.Call("vibrate", (long)pulse.DurationMilliseconds);
            }
            catch
            {
                // Keep a Unity fallback for vendor-specific Android services.
                try
                {
                    Handheld.Vibrate();
                }
                catch
                {
                    // Devices without an accessible vibrator remain silent.
                }
            }
#elif UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        public void Cancel()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using AndroidJavaObject vibrator = GetAndroidVibrator();
                vibrator?.Call("cancel");
            }
            catch
            {
                // A missing or restricted device service is an expected no-op.
            }
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject GetAndroidVibrator()
        {
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using AndroidJavaObject activity =
                unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            if (activity == null) return null;

            if (AndroidSdkInt() >= VibratorManagerApiLevel)
            {
                using AndroidJavaObject manager =
                    activity.Call<AndroidJavaObject>("getSystemService", "vibrator_manager");
                AndroidJavaObject vibrator =
                    manager?.Call<AndroidJavaObject>("getDefaultVibrator");
                if (vibrator != null) return vibrator;
            }

            return activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
        }

        private static int AndroidSdkInt()
        {
            using var version = new AndroidJavaClass("android.os.Build$VERSION");
            return version.GetStatic<int>("SDK_INT");
        }
#endif
    }
}
