using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Meowdoku.Core.Robot
{
    public interface IRobotPoolStore
    {
        IReadOnlyDictionary<string, RobotPool> LoadAll();
        bool SaveAll(IReadOnlyDictionary<string, RobotPool> pools);
        void Reset();
    }

    /// <summary>
    /// Persistence adapter for source user://robots.cfg. Every pool remains a
    /// top-level section whose data key contains the source-shaped pool model.
    /// </summary>
    public sealed class RobotRepository : IRobotPoolStore
    {
        private const string SavePassword =
            "rb_n6V2xF9cQ4mK7sW1dH8pL3zT5gA0";

        private readonly SaveStore _store;

        public RobotRepository(string persistentDataPath)
        {
            if (string.IsNullOrEmpty(persistentDataPath))
                throw new ArgumentException(
                    "Persistent data path cannot be empty.",
                    nameof(persistentDataPath));
            _store = new SaveStore(
                SavePassword,
                persistentDataPath,
                false,
                Path.Combine(persistentDataPath, "robots.cfg"));
        }

        public static RobotRepository CreateDefault() =>
            new(Application.persistentDataPath);

        public IReadOnlyDictionary<string, RobotPool> LoadAll()
        {
            Dictionary<string, object> document = _store.LoadConfig();
            var pools = new Dictionary<string, RobotPool>(
                StringComparer.Ordinal);
            if (document == null) return pools;

            foreach (KeyValuePair<string, object> section in document)
            {
                if (section.Value is not
                    IReadOnlyDictionary<string, object> values ||
                    !values.TryGetValue("data", out object raw) ||
                    raw is not IReadOnlyDictionary<string, object> data)
                    continue;
                RobotPool pool = RobotPool.FromDictionary(data);
                if (pool == null || string.IsNullOrEmpty(pool.Key)) continue;
                pools[pool.Key] = pool;
            }
            return pools;
        }

        public bool SaveAll(IReadOnlyDictionary<string, RobotPool> pools)
        {
            var document = new Dictionary<string, object>(
                StringComparer.Ordinal);
            if (pools != null)
            {
                foreach (KeyValuePair<string, RobotPool> pair in pools)
                {
                    if (string.IsNullOrEmpty(pair.Key) || pair.Value == null)
                        continue;
                    document[pair.Key] = new Dictionary<string, object>
                    {
                        ["data"] = pair.Value.ToDictionary()
                    };
                }
            }
            return _store.SaveConfig(document);
        }

        public void Reset() => _store.Remove();
    }
}
