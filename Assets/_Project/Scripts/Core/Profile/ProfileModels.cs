using System;
using System.Collections;
using System.Collections.Generic;

namespace Meowdoku.Core.Profile
{
    public sealed class AvatarFrame
    {
        public AvatarFrame(int id = 0, int acquiredCount = -1)
        {
            Id = id;
            AcquiredCount = acquiredCount;
        }

        public int Id { get; set; }
        public int AcquiredCount { get; set; }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["id"] = Id,
                ["acquired_count"] = AcquiredCount
            };
        }

        public static AvatarFrame FromDictionary(
            IReadOnlyDictionary<string, object> dictionary)
        {
            return new AvatarFrame(
                ProfileData.ReadInt(dictionary, "id"),
                ProfileData.ReadInt(dictionary, "acquired_count", -1));
        }
    }

    public sealed class PlayerInfo
    {
        public const string LocalPlayerId = "self";

        public string Nickname { get; set; } = string.Empty;
        public int AvatarId { get; set; }
        public AvatarFrame Frame { get; set; } = new();
        public string PlayerId { get; set; } = string.Empty;
        public int LevelIndex { get; set; }
        public bool IsRobot { get; set; }

        public bool IsSelf => string.Equals(
            PlayerId,
            LocalPlayerId,
            StringComparison.Ordinal);

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["nickname"] = Nickname ?? string.Empty,
                ["avatar_id"] = AvatarId,
                ["frame"] = Frame?.ToDictionary() ??
                            new AvatarFrame().ToDictionary(),
                ["player_id"] = PlayerId ?? string.Empty,
                ["level_index"] = LevelIndex,
                ["is_robot"] = IsRobot
            };
        }

        public static PlayerInfo FromDictionary(
            IReadOnlyDictionary<string, object> dictionary)
        {
            var result = new PlayerInfo
            {
                Nickname = ProfileData.ReadString(dictionary, "nickname"),
                AvatarId = ProfileData.ReadInt(dictionary, "avatar_id"),
                PlayerId = ProfileData.ReadString(dictionary, "player_id"),
                LevelIndex = ProfileData.ReadInt(dictionary, "level_index"),
                IsRobot = ProfileData.ReadBool(dictionary, "is_robot")
            };
            if (dictionary != null &&
                dictionary.TryGetValue("frame", out object frame) &&
                frame is IReadOnlyDictionary<string, object> frameDictionary)
                result.Frame = AvatarFrame.FromDictionary(frameDictionary);
            return result;
        }
    }

    public sealed class ProfileData
    {
        public string Nickname { get; set; } = string.Empty;
        public int AvatarId { get; set; }
        public int FrameId { get; set; }
        public Dictionary<int, AvatarFrame> OwnedFrames { get; } = new();
        public bool FrameRedDot { get; set; }
        public bool Initialized { get; set; }
        public bool IdentityCustomized { get; set; }

        public Dictionary<string, object> ToDictionary()
        {
            var frames = new List<object>(OwnedFrames.Count);
            foreach (KeyValuePair<int, AvatarFrame> pair in OwnedFrames)
                if (pair.Value != null)
                    frames.Add(pair.Value.ToDictionary());
            return new Dictionary<string, object>
            {
                ["nickname"] = Nickname ?? string.Empty,
                ["avatar_id"] = AvatarId,
                ["frame_id"] = FrameId,
                ["owned_frames"] = frames,
                ["frame_red_dot"] = FrameRedDot,
                ["initialized"] = Initialized,
                ["identity_customized"] = IdentityCustomized
            };
        }

        public static ProfileData FromDictionary(
            IReadOnlyDictionary<string, object> dictionary)
        {
            var data = new ProfileData();
            if (dictionary == null) return data;
            data.Nickname = ReadString(dictionary, "nickname");
            data.AvatarId = ReadInt(dictionary, "avatar_id");
            data.FrameId = ReadInt(dictionary, "frame_id");
            data.FrameRedDot = ReadBool(dictionary, "frame_red_dot");
            data.Initialized = ReadBool(dictionary, "initialized");
            data.IdentityCustomized = ReadBool(
                dictionary,
                "identity_customized");
            if (!dictionary.TryGetValue("owned_frames", out object frames) ||
                frames is not IList list)
                return data;
            for (int index = 0; index < list.Count; index++)
            {
                if (list[index] is not
                    IReadOnlyDictionary<string, object> frameDictionary)
                    continue;
                AvatarFrame frame = AvatarFrame.FromDictionary(frameDictionary);
                if (frame.Id != 0) data.OwnedFrames[frame.Id] = frame;
            }
            return data;
        }

        internal static int ReadInt(
            IReadOnlyDictionary<string, object> dictionary,
            string key,
            int fallback = 0)
        {
            if (dictionary == null ||
                !dictionary.TryGetValue(key, out object value) ||
                value == null)
                return fallback;
            try { return Convert.ToInt32(value); }
            catch (Exception) { return fallback; }
        }

        internal static bool ReadBool(
            IReadOnlyDictionary<string, object> dictionary,
            string key,
            bool fallback = false)
        {
            if (dictionary == null ||
                !dictionary.TryGetValue(key, out object value) ||
                value == null)
                return fallback;
            try { return Convert.ToBoolean(value); }
            catch (Exception) { return fallback; }
        }

        internal static string ReadString(
            IReadOnlyDictionary<string, object> dictionary,
            string key)
        {
            return dictionary != null &&
                   dictionary.TryGetValue(key, out object value) &&
                   value != null
                ? Convert.ToString(value) ?? string.Empty
                : string.Empty;
        }
    }

    public sealed class ProfileFrameGroup
    {
        public ProfileFrameGroup(string group, IReadOnlyList<int> ids)
        {
            Group = group ?? string.Empty;
            Ids = ids ?? Array.Empty<int>();
        }

        public string Group { get; }
        public IReadOnlyList<int> Ids { get; }
    }

    public readonly struct ProfileFrameInfo
    {
        public ProfileFrameInfo(bool unlocked, int count)
        {
            Unlocked = unlocked;
            Count = count;
        }

        public bool Unlocked { get; }
        public int Count { get; }
    }
}
