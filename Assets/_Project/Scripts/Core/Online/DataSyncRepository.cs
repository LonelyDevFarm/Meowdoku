using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Meowdoku.Core.Online
{
    /// <summary>
    /// Persistence adapters for source data_sync_state.cfg and the
    /// development-only data_sync_cheat.cfg switch.
    /// </summary>
    public sealed class DataSyncSnapshotRepository :
        IDataSyncSnapshotStore
    {
        internal const string SavePassword =
            "ds_6pR1wN8cQ4mH9vK2xT7bL3zF5gY0";

        private readonly SaveStore _store;

        public DataSyncSnapshotRepository(string persistentDataPath)
        {
            if (string.IsNullOrEmpty(persistentDataPath))
                throw new ArgumentException(
                    "Persistent data path cannot be empty.",
                    nameof(persistentDataPath));
            _store = new SaveStore(
                SavePassword,
                persistentDataPath,
                false,
                Path.Combine(persistentDataPath, "data_sync_state.cfg"));
        }

        public static DataSyncSnapshotRepository CreateDefault() =>
            new(Application.persistentDataPath);

        public Dictionary<string, object> LoadRemoteRoot()
        {
            Dictionary<string, object> document = _store.LoadConfig();
            if (document == null ||
                !document.TryGetValue("sync", out object section) ||
                section is not IReadOnlyDictionary<string, object> sync ||
                !sync.TryGetValue("remote_root", out object raw) ||
                raw == null)
                return new Dictionary<string, object>();
            string json = Convert.ToString(raw) ?? string.Empty;
            if (string.IsNullOrEmpty(json))
                return new Dictionary<string, object>();
            try
            {
                return MiniJson.Deserialize(json) is
                    IReadOnlyDictionary<string, object> root
                        ? DataSyncValues.DeepClone(root)
                        : new Dictionary<string, object>();
            }
            catch (Exception)
            {
                return new Dictionary<string, object>();
            }
        }

        public bool SaveRemoteRoot(
            IReadOnlyDictionary<string, object> root)
        {
            return _store.SaveConfig(new Dictionary<string, object>
            {
                ["sync"] = new Dictionary<string, object>
                {
                    ["remote_root"] = MiniJson.Serialize(
                        DataSyncValues.DeepClone(root))
                }
            });
        }
    }

    public sealed class DataSyncEnableRepository : IDataSyncEnableStore
    {
        internal const string SavePassword =
            "dc_2qM8vL5xC1nR7wK4pT9bH3zF6gY0";

        private readonly SaveStore _store;

        public DataSyncEnableRepository(string persistentDataPath)
        {
            if (string.IsNullOrEmpty(persistentDataPath))
                throw new ArgumentException(
                    "Persistent data path cannot be empty.",
                    nameof(persistentDataPath));
            _store = new SaveStore(
                SavePassword,
                persistentDataPath,
                false,
                Path.Combine(persistentDataPath, "data_sync_cheat.cfg"));
        }

        public static DataSyncEnableRepository CreateDefault() =>
            new(Application.persistentDataPath);

        public bool TryLoad(out bool enabled)
        {
            enabled = false;
            Dictionary<string, object> document = _store.LoadConfig();
            if (document == null ||
                !document.TryGetValue("cheat", out object section) ||
                section is not IReadOnlyDictionary<string, object> cheat ||
                !cheat.TryGetValue("sync_enabled", out object raw) ||
                raw == null)
                return false;
            try
            {
                enabled = Convert.ToBoolean(raw);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool Save(bool enabled)
        {
            return _store.SaveConfig(new Dictionary<string, object>
            {
                ["cheat"] = new Dictionary<string, object>
                {
                    ["sync_enabled"] = enabled
                }
            });
        }
    }
}
