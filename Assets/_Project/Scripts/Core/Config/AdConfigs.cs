using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Meowdoku.Core.Config
{
    public sealed class InterUnlockLevelConfig : AbConfigBase<int>
    {
        public const int DefaultUnlockLevel = 11;
        public InterUnlockLevelConfig()
            : base("inter_unlock_level", DefaultUnlockLevel, AbConfigTiming.GameStart) { }
        public bool IsUnlockedAt(int level) => level >= Value;
    }

    public sealed class InterUnlockSessionConfig : AbConfigBase<int>
    {
        public const int DefaultUnlockSession = 2;
        public InterUnlockSessionConfig()
            : base("inter_unlock_session", DefaultUnlockSession, AbConfigTiming.GameStart) { }
        public bool IsUnlockedAt(int sessionCount) => sessionCount >= Value;
    }

    public sealed class InterUnlockMemoryConfig : AbConfigBase<int>
    {
        public const int DefaultUnlockMemoryMb = 300;
        public InterUnlockMemoryConfig()
            : base("inter_unlock_memory", DefaultUnlockMemoryMb, AbConfigTiming.GameStart) { }
        public bool IsUnlockedForDevice(int physicalMemoryMb) =>
            physicalMemoryMb <= 0 || physicalMemoryMb >= Value;
    }

    public sealed class InterCdLcConfig : AbConfigBase<string>
    {
        public const string DefaultSegments = "{60}";
        public InterCdLcConfig()
            : base("inter_cd_lc", DefaultSegments, AbConfigTiming.GameStart) { }

        public int GetSeconds(int segmentIndex = -1, int segmentCount = 0)
        {
            List<int> values = ParseIntSegments(Value);
            if (values.Count == 0) return 60;
            return segmentIndex >= 0 && segmentCount == values.Count &&
                   segmentIndex < values.Count
                ? values[segmentIndex]
                : values[0];
        }

        internal static List<int> ParseIntSegments(string value)
        {
            var result = new List<int>();
            foreach (string segment in ParseSegments(value))
                if (int.TryParse(segment, out int parsed)) result.Add(parsed);
            return result;
        }

        internal static List<string> ParseSegments(string value)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(value)) return result;
            MatchCollection matches = Regex.Matches(value, "\\{([^}]*)\\}");
            for (int index = 0; index < matches.Count; index++)
                result.Add(matches[index].Groups[1].Value.Trim());
            return result;
        }
    }

    public sealed class InterExtraProtectLcConfig : AbConfigBase<string>
    {
        public const string DefaultSegments = "{session_game_2}";
        public InterExtraProtectLcConfig()
            : base("inter_extra_protect_lc", DefaultSegments, AbConfigTiming.GameStart) { }

        public string GetScheme(int segmentIndex = -1, int segmentCount = 0)
        {
            List<string> schemes = InterCdLcConfig.ParseSegments(Value);
            if (schemes.Count == 0) return string.Empty;
            return segmentIndex >= 0 && segmentCount == schemes.Count &&
                   segmentIndex < schemes.Count
                ? schemes[segmentIndex]
                : schemes[0];
        }
    }

    public sealed class BannerUnlockSessionConfig : AbConfigBase<int>
    {
        public const int DefaultUnlockSession = 2;
        public BannerUnlockSessionConfig()
            : base("banner_unlock_session", DefaultUnlockSession,
                AbConfigTiming.GameStart) { }
        public bool IsUnlockedAt(int sessionCount) => sessionCount >= Value;
    }

    public sealed class BannerUnlockLevelConfig : AbConfigBase<int>
    {
        public const int DefaultUnlockLevel = 11;
        public BannerUnlockLevelConfig()
            : base("banner_unlock_level", DefaultUnlockLevel,
                AbConfigTiming.GameStart) { }
        public bool IsUnlockedAt(int level) => level >= Value;
    }

    public sealed class BannerExtraProtectLcConfig : AbConfigBase<string>
    {
        public const string DefaultSegments = "{no}";
        public BannerExtraProtectLcConfig()
            : base("banner_extra_protect_lc", DefaultSegments,
                AbConfigTiming.GameStart) { }

        public string GetScheme(int segmentIndex = -1, int segmentCount = 0)
        {
            List<string> schemes = InterCdLcConfig.ParseSegments(Value);
            if (schemes.Count == 0) return string.Empty;
            return segmentIndex >= 0 && segmentCount == schemes.Count &&
                   segmentIndex < schemes.Count
                ? schemes[segmentIndex]
                : schemes[0];
        }
    }

    public sealed class BannerUnlockDiffLcConfig : AbConfigBase<string>
    {
        public const string DefaultSegments = "{all}";
        public BannerUnlockDiffLcConfig()
            : base("banner_unlock_diff_lc", DefaultSegments,
                AbConfigTiming.GameStart) { }

        public bool IsUnlockedForSize(
            int size,
            int segmentIndex = -1,
            int segmentCount = 0)
        {
            List<string> segments = InterCdLcConfig.ParseSegments(Value);
            if (segments.Count == 0) return true;
            int index = segmentIndex >= 0 && segmentCount == segments.Count &&
                        segmentIndex < segments.Count
                ? segmentIndex
                : 0;
            string segment = segments[index].Trim().ToLowerInvariant();
            if (segment == "no") return false;
            if (segment == "all" || segment == "yes") return true;
            string[] values = segment.Split(',');
            for (int i = 0; i < values.Length; i++)
                if (int.TryParse(values[i].Trim(), out int allowed) &&
                    allowed == size)
                    return true;
            return false;
        }
    }

    public sealed class CommonRewardAdLogicConfig : AbConfigBase<int>
    {
        public const int ValueNoRestore = 0;
        public const int ValueRestore = 1;
        public CommonRewardAdLogicConfig()
            : base("common_rewardad_logic", ValueNoRestore,
                AbConfigTiming.GameStart) { }
        public bool ShouldGrantRewardRestore() => Value == ValueRestore;
    }
}
