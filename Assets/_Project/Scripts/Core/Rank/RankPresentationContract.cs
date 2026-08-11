using System;
using System.Text;

namespace Meowdoku.Core.Rank
{
    public static class RankPresentationContract
    {
        public static string FormatHms(int seconds)
        {
            int value = Math.Max(0, seconds);
            return $"{value / 3600:00}:{value % 3600 / 60:00}:{value % 60:00}";
        }

        public static bool ScoreIsCat(int group) =>
            group != RankActivityConfig.GroupFish;

        public static bool HasRewardBox(int group) =>
            group != RankActivityConfig.GroupFrameOnly;

        public static bool ShowsPlayerRank(bool joined, int collectTotal) =>
            joined && collectTotal > 0;

        public static int EntryChestTier(int rank) =>
            4 - Math.Clamp(rank, 1, 3);

        /// <summary>
        /// Godot's source copy uses RichTextLabel BBCode with an inline image.
        /// UGUI Text receives the same localized string, while the presenter
        /// renders that image through a serialized Image beside the text.
        /// </summary>
        public static string GodotRichTextToPlainText(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var result = new StringBuilder(value.Length);
            bool skippingImagePath = false;
            for (int index = 0; index < value.Length; index++)
            {
                if (value[index] != '[')
                {
                    if (!skippingImagePath) result.Append(value[index]);
                    continue;
                }

                int end = value.IndexOf(']', index + 1);
                if (end < 0)
                {
                    if (!skippingImagePath) result.Append(value[index]);
                    continue;
                }

                string tag = value.Substring(index + 1, end - index - 1);
                if (tag.StartsWith("img", StringComparison.OrdinalIgnoreCase))
                    skippingImagePath = true;
                else if (string.Equals(
                             tag,
                             "/img",
                             StringComparison.OrdinalIgnoreCase))
                    skippingImagePath = false;
                index = end;
            }

            return CollapseWhitespace(result.ToString());
        }

        private static string CollapseWhitespace(string value)
        {
            var result = new StringBuilder(value.Length);
            bool pendingSpace = false;
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (char.IsWhiteSpace(character))
                {
                    pendingSpace = result.Length > 0;
                    continue;
                }
                if (pendingSpace) result.Append(' ');
                result.Append(character);
                pendingSpace = false;
            }
            return result.ToString().Trim();
        }
    }
}
