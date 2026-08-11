using System;
using System.Collections;
using System.Collections.Generic;
using Meowdoku.Core.Tracking;

namespace Meowdoku.Core.Daily
{
    public enum AwardDisplayType
    {
        Direct = 0,
        StreakGift = 1,
        RankGift = 2
    }

    public interface IFrameAwardSink
    {
        bool GrantFrame(int frameId, int count);
    }

    internal sealed class NullFrameAwardSink : IFrameAwardSink
    {
        public static readonly NullFrameAwardSink Instance = new();
        private NullFrameAwardSink() { }
        public bool GrantFrame(int frameId, int count) => false;
    }

    public sealed class AwardPresentationRequest
    {
        public AwardPresentationRequest(
            int uid,
            AwardDisplayType displayType,
            IReadOnlyList<AwardItem> items,
            IReadOnlyDictionary<string, object> displayParameters)
        {
            Uid = uid;
            DisplayType = displayType;
            Items = items;
            DisplayParameters = displayParameters;
        }

        public int Uid { get; }
        public AwardDisplayType DisplayType { get; }
        public IReadOnlyList<AwardItem> Items { get; }
        public IReadOnlyDictionary<string, object> DisplayParameters { get; }
    }

    public static class GlobalUniqueId
    {
        private static int _counter;

        public static int Next()
        {
            _counter++;
            return _counter;
        }

        internal static void ResetForTests()
        {
            _counter = 0;
        }
    }

    /// <summary>
    /// Durable award transaction manager ported from award_manager.gd. Award
    /// entries are saved before presentation and removed before inventory is
    /// mutated, so repeat completion or cold-start sweep cannot double grant.
    /// </summary>
    public sealed class AwardManager : IStreakRewardBoundary
    {
        public const string StreakChestReason = "streak_chest";
        public const string StreakRewardAdReason = "streak_reward_ad";
        public const string SwitchGroupReason = "switch_group";

        private readonly GameStateService _gameState;
        private readonly IFrameAwardSink _frameSink;
        private TrackerService _tracker;
        private readonly Dictionary<int, Dictionary<string, object>> _renders =
            new();
        private readonly Dictionary<int, List<Action<int>>> _completion = new();

        public AwardManager(
            GameStateService gameState,
            IFrameAwardSink frameSink = null,
            TrackerService tracker = null)
        {
            _gameState = gameState ??
                         throw new ArgumentNullException(nameof(gameState));
            _frameSink = frameSink ?? NullFrameAwardSink.Instance;
            _tracker = tracker;
            SweepInFlightOnColdStart();
        }

        public event Action<AwardPresentationRequest>
            AwardPresentationRequested;
        public event Action<int> AwardEnded;

        public int ActiveRenderCount => _renders.Count;

        public void BindTracker(TrackerService tracker)
        {
            _tracker = tracker;
        }

        public int Dispatch(
            IReadOnlyList<AwardItem> items,
            AwardDisplayType displayType,
            string reason,
            string bonusReason = "")
        {
            if (items == null || items.Count == 0 ||
                string.IsNullOrEmpty(reason))
                return -1;
            for (int index = 0; index < items.Count; index++)
                if (items[index] == null || !items[index].IsValid())
                    return -1;

            AwardDisplayType resolvedType = Enum.IsDefined(
                typeof(AwardDisplayType),
                displayType)
                ? displayType
                : AwardDisplayType.Direct;
            int uid = GlobalUniqueId.Next();
            var itemDictionaries = new List<object>(items.Count);
            for (int index = 0; index < items.Count; index++)
                itemDictionaries.Add(items[index].ToDictionary());
            var entry = new Dictionary<string, object>
            {
                ["uid"] = uid,
                ["items"] = itemDictionaries,
                ["display_type"] = (int)resolvedType,
                ["reason"] = reason,
                ["bonus_reason"] = bonusReason ?? string.Empty
            };
            _gameState.AddInFlightAward(entry);
            _renders[uid] = entry;

            if (resolvedType == AwardDisplayType.Direct)
                ShowAward(uid);
            return uid;
        }

        public bool ShowAward(
            int uid,
            IReadOnlyDictionary<string, object> displayParameters = null)
        {
            if (!_renders.TryGetValue(uid, out Dictionary<string, object> entry))
                return false;
            AwardDisplayType displayType = (AwardDisplayType)AwardItem.ReadInt(
                entry,
                "display_type",
                (int)AwardDisplayType.Direct);
            if (displayType == AwardDisplayType.Direct)
                return CompleteAward(uid);

            Action<AwardPresentationRequest> handler =
                AwardPresentationRequested;
            if (handler == null) return false;
            handler.Invoke(new AwardPresentationRequest(
                uid,
                displayType,
                ReadItems(entry),
                displayParameters ?? new Dictionary<string, object>()));
            return true;
        }

        public bool ContinueWhenAwardEnd(int uid, Action<int> callback)
        {
            if (!_renders.ContainsKey(uid) || callback == null) return false;
            if (!_completion.TryGetValue(uid, out List<Action<int>> callbacks))
            {
                callbacks = new List<Action<int>>();
                _completion.Add(uid, callbacks);
            }
            callbacks.Add(callback);
            return true;
        }

        public bool DoubleAward(int uid)
        {
            if (!_renders.TryGetValue(uid, out Dictionary<string, object> entry))
                return false;
            entry["doubled"] = true;
            return true;
        }

        public bool CompleteAward(int uid)
        {
            if (!_renders.ContainsKey(uid)) return false;
            TryPersistAward(uid, out _);
            AwardEnded?.Invoke(uid);
            if (_completion.TryGetValue(uid, out List<Action<int>> callbacks))
            {
                _completion.Remove(uid);
                for (int index = 0; index < callbacks.Count; index++)
                    callbacks[index]?.Invoke(uid);
            }
            return true;
        }

        public int PersistAward(int uid)
        {
            return TryPersistAward(uid, out int granted) ? granted : 0;
        }

        public int DispatchStreakChest(
            IReadOnlyDictionary<string, int> rewards)
        {
            return Dispatch(
                BuildToolItems(rewards),
                AwardDisplayType.StreakGift,
                StreakChestReason,
                StreakRewardAdReason);
        }

        public void DispatchSwitchGift(
            IReadOnlyDictionary<string, int> rewards)
        {
            Dispatch(
                BuildToolItems(rewards),
                AwardDisplayType.Direct,
                SwitchGroupReason);
        }

        void IStreakRewardBoundary.ShowAward(int uid)
        {
            ShowAward(uid);
        }

        private void SweepInFlightOnColdStart()
        {
            List<object> entries = _gameState.GetInFlightAwards();
            for (int index = 0; index < entries.Count; index++)
            {
                if (entries[index] is not Dictionary<string, object> entry)
                    continue;
                int uid = AwardItem.ReadInt(entry, "uid", -1);
                TryPersistAward(uid, out _);
            }
        }

        private bool TryPersistAward(int uid, out int granted)
        {
            granted = 0;
            Dictionary<string, object> entry =
                _gameState.FindInFlightAward(uid);
            if (entry == null)
            {
                _renders.Remove(uid);
                _completion.Remove(uid);
                return false;
            }

            _gameState.RemoveInFlightAward(uid);
            IReadOnlyList<AwardItem> items = ReadItems(entry);
            string reason = AwardItem.ReadString(entry, "reason");
            for (int index = 0; index < items.Count; index++)
                if (ApplyItem(items[index], reason)) granted++;

            bool doubled = ReadBool(entry, "doubled");
            if (doubled)
            {
                string bonusReason = AwardItem.ReadString(
                    entry,
                    "bonus_reason");
                if (string.IsNullOrEmpty(bonusReason))
                    bonusReason = reason;
                for (int index = 0; index < items.Count; index++)
                {
                    AwardItem item = items[index];
                    if (item.Category != AwardCategory.Tool) continue;
                    if (ApplyItem(item, bonusReason)) granted++;
                }
            }

            _renders.Remove(uid);
            return true;
        }

        private bool ApplyItem(AwardItem item, string reason)
        {
            if (item == null || !item.IsValid()) return false;
            if (item.Category == AwardCategory.Frame)
                return _frameSink.GrantFrame(item.FrameId, item.Count);
            int current = _gameState.GetToolCount(item.Kind);
            int updated = current + item.Count;
            _gameState.SetToolCount(item.Kind, updated);
            _tracker?.TrackProp(
                true,
                item.Kind,
                reason,
                item.Count,
                updated);
            return true;
        }

        private static IReadOnlyList<AwardItem> ReadItems(
            IReadOnlyDictionary<string, object> entry)
        {
            var result = new List<AwardItem>();
            if (entry == null ||
                !entry.TryGetValue("items", out object value) ||
                value is not IList list)
                return result;
            for (int index = 0; index < list.Count; index++)
            {
                if (list[index] is not
                    IReadOnlyDictionary<string, object> dictionary)
                    continue;
                result.Add(AwardItem.FromDictionary(dictionary));
            }
            return result;
        }

        private static IReadOnlyList<AwardItem> BuildToolItems(
            IReadOnlyDictionary<string, int> rewards)
        {
            var result = new List<AwardItem>();
            if (rewards == null) return result;
            foreach (KeyValuePair<string, int> pair in rewards)
                if (pair.Value > 0)
                    result.Add(AwardItem.Tool(pair.Key, pair.Value));
            return result;
        }

        private static bool ReadBool(
            IReadOnlyDictionary<string, object> dictionary,
            string key)
        {
            if (dictionary == null ||
                !dictionary.TryGetValue(key, out object value) ||
                value == null)
                return false;
            try
            {
                return Convert.ToBoolean(value);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
