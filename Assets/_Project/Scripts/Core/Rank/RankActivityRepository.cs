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

        public RankActivityRepository(string persistentDataPath)
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
        }

        public static RankActivityRepository CreateDefault() =>
            new(Application.persistentDataPath);

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
            return _store.SaveConfig(new Dictionary<string, object>
            {
                ["rank_activity"] = data.ToDictionary()
            });
        }

        public void Reset() => _store.Remove();
    }
}
