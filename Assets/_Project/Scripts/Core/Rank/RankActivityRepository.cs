using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Meowdoku.Core.Rank
{
    public interface IRankActivityStore
    {
        RankActivityData Load();
        bool Save(RankActivityData data);
        void Reset();
    }

    /// <summary>
    /// Persistence adapter for source user://rank_activity.cfg.
    /// </summary>
    public sealed class RankActivityRepository : IRankActivityStore
    {
        private const string SavePassword =
            "rk_h3Q8nC5vM1xT7pL4sD9gF2zW6aB0";

        private readonly SaveStore _store;
        private readonly BackgroundSaveWriter _writer;

        public RankActivityRepository(
            string persistentDataPath,
            bool useBackgroundWrites = false)
        {
            if (string.IsNullOrEmpty(persistentDataPath))
                throw new ArgumentException(
                    "Persistent data path cannot be empty.",
                    nameof(persistentDataPath));
            _store = new SaveStore(
                SavePassword,
                persistentDataPath,
                false,
                Path.Combine(persistentDataPath, "rank_activity.cfg"));
            if (useBackgroundWrites)
                _writer = new BackgroundSaveWriter(_store);
        }

        public static RankActivityRepository CreateDefault() =>
            new(Application.persistentDataPath, useBackgroundWrites: true);

        public RankActivityData Load()
        {
            Dictionary<string, object> document = _store.LoadConfig();
            if (document == null ||
                !document.TryGetValue("rank_activity", out object section) ||
                section is not IReadOnlyDictionary<string, object> values)
                return new RankActivityData();
            return RankActivityData.FromDictionary(values);
        }

        public bool Save(RankActivityData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            string serialized = MiniJson.Serialize(new Dictionary<string, object>
            {
                ["rank_activity"] = data.ToDictionary()
            });
            return _writer != null
                ? _writer.RequestSave(serialized)
                : _store.SaveSerializedConfig(serialized);
        }

        public void Reset()
        {
            if (_writer != null)
                _writer.RequestRemove();
            else
                _store.Remove();
        }

        public bool FlushPendingWrites()
        {
            return _writer == null || _writer.Flush();
        }
    }
}
