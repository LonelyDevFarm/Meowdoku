using System;
using System.Collections.Generic;

namespace Meowdoku.Core.Daily
{
    public enum AwardCategory
    {
        Tool = 0,
        Frame = 1
    }

    public sealed class AwardItem
    {
        public string Kind { get; set; } = string.Empty;
        public int FrameId { get; set; }
        public int Count { get; set; }
        public AwardCategory Category { get; set; } = AwardCategory.Tool;

        public static AwardItem Tool(string kind, int count)
        {
            return new AwardItem
            {
                Kind = kind ?? string.Empty,
                Count = count,
                Category = AwardCategory.Tool
            };
        }

        public static AwardItem Frame(int frameId, int count = 1)
        {
            return new AwardItem
            {
                FrameId = frameId,
                Count = count,
                Category = AwardCategory.Frame
            };
        }

        public bool IsValid()
        {
            if (Count <= 0) return false;
            return Category == AwardCategory.Frame
                ? FrameId > 0
                : !string.IsNullOrEmpty(Kind);
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["kind"] = Kind ?? string.Empty,
                ["frame_id"] = FrameId,
                ["count"] = Count,
                ["category"] = (int)Category
            };
        }

        public static AwardItem FromDictionary(
            IReadOnlyDictionary<string, object> dictionary)
        {
            if (dictionary == null) return new AwardItem();
            return new AwardItem
            {
                Kind = ReadString(dictionary, "kind"),
                FrameId = ReadInt(dictionary, "frame_id"),
                Count = ReadInt(dictionary, "count"),
                Category = (AwardCategory)ReadInt(
                    dictionary,
                    "category",
                    (int)AwardCategory.Tool)
            };
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
            try
            {
                return Convert.ToInt32(value);
            }
            catch (Exception)
            {
                return fallback;
            }
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
}
