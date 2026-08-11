using System;
using System.Collections.Generic;
using Meowdoku.Core.Daily;
using Meowdoku.Core.Tracking;

namespace Meowdoku.Core.Ads
{
    public sealed class RewardRestoreBatch
    {
        internal RewardRestoreBatch(
            IReadOnlyList<AwardItem> items,
            IReadOnlyList<object> entries)
        {
            Items = items;
            Entries = entries;
        }

        public IReadOnlyList<AwardItem> Items { get; }
        public int RewardedAdCount => Entries.Count;
        internal IReadOnlyList<object> Entries { get; }
    }

    /// <summary>
    /// Home-side port of _show_ad_reward_restored. It filters recoverable
    /// placements, chooses newest entries first, enforces the source
    /// three-per-day anti-abuse gate and removes the presented batch whether
    /// it is collected or closed.
    /// </summary>
    public sealed class RewardRestoreService
    {
        private readonly GameStateService _state;

        public RewardRestoreService(GameStateService state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public RewardRestoreBatch BuildBatch(long unixNow)
        {
            List<object> pending = _state.GetPendingRewards();
            if (pending.Count == 0) return null;

            var grantable = new List<Grantable>();
            for (int index = 0; index < pending.Count; index++)
            {
                if (pending[index] is not Dictionary<string, object> entry)
                    continue;
                IReadOnlyList<AwardItem> items =
                    ItemsForPosition(Read(entry, "source"));
                if (items.Count > 0)
                    grantable.Add(new Grantable(pending[index], items));
            }
            if (grantable.Count == 0)
            {
                _state.PopAllPendingRewards();
                return null;
            }

            int remaining = _state.GetRestoreRemainingToday(unixNow);
            if (remaining <= 0) return null;

            int take = Math.Min(remaining, grantable.Count);
            var totals = new Dictionary<string, int>(StringComparer.Ordinal);
            var entries = new List<object>(take);
            for (int sourceIndex = grantable.Count - 1;
                 sourceIndex >= grantable.Count - take;
                 sourceIndex--)
            {
                Grantable item = grantable[sourceIndex];
                entries.Add(item.Entry);
                for (int rewardIndex = 0;
                     rewardIndex < item.Items.Count;
                     rewardIndex++)
                {
                    AwardItem reward = item.Items[rewardIndex];
                    totals.TryGetValue(reward.Kind, out int current);
                    totals[reward.Kind] = current + reward.Count;
                }
            }

            var rewards = new List<AwardItem>(totals.Count);
            foreach (KeyValuePair<string, int> pair in totals)
                rewards.Add(AwardItem.Tool(pair.Key, pair.Value));
            return rewards.Count > 0
                ? new RewardRestoreBatch(rewards, entries)
                : null;
        }

        public void Complete(RewardRestoreBatch batch, bool collected)
        {
            if (batch == null) return;
            if (collected)
                _state.AddRestoredTodayCount(batch.RewardedAdCount);
            _state.RemovePendingRewardEntries(batch.Entries);
        }

        public static IReadOnlyList<AwardItem> ItemsForPosition(
            string position)
        {
            switch (position)
            {
                case TrackerCatalog.AdPosition.PropsNormalHint:
                case TrackerCatalog.AdPosition.PropsDailyHint:
                    return new[] { AwardItem.Tool("hint", 1) };
                case TrackerCatalog.AdPosition.PropsNormalLocate:
                case TrackerCatalog.AdPosition.PropsDailyLocate:
                    return new[] { AwardItem.Tool("locate", 1) };
                case TrackerCatalog.AdPosition.StreakDoubleReward:
                    return new[]
                    {
                        AwardItem.Tool("hint", 2),
                        AwardItem.Tool("locate", 2)
                    };
                default:
                    return Array.Empty<AwardItem>();
            }
        }

        private static string Read(
            IReadOnlyDictionary<string, object> values,
            string key)
        {
            return values.TryGetValue(key, out object value) && value != null
                ? value.ToString()
                : string.Empty;
        }

        private readonly struct Grantable
        {
            public Grantable(
                object entry,
                IReadOnlyList<AwardItem> items)
            {
                Entry = entry;
                Items = items;
            }

            public object Entry { get; }
            public IReadOnlyList<AwardItem> Items { get; }
        }
    }
}
