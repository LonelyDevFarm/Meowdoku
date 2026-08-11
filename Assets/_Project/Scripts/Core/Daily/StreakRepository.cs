using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Meowdoku.Core.Daily
{
    public interface IStreakDataStore
    {
        StreakData Load();
        bool Save(StreakData data);
        void Reset();
    }

    /// <summary>
    /// Unity persistence adapter for source user://streak.cfg. The logical
    /// document remains a separate streak section; the bytes use the existing
    /// verified Unity SaveStore format instead of Godot ConfigFile encoding.
    /// </summary>
    public sealed class StreakRepository : IStreakDataStore
    {
        private const string SavePassword =
            "st_x4M7qLb2Vn9Rc5Wy8Kd1Fg6Ph3Za0";

        private readonly SaveStore _store;

        public StreakRepository(string persistentDataPath)
        {
            if (string.IsNullOrEmpty(persistentDataPath))
                throw new ArgumentException(
                    "Persistent data path cannot be empty.",
                    nameof(persistentDataPath));
            _store = new SaveStore(
                SavePassword,
                persistentDataPath,
                false,
                Path.Combine(persistentDataPath, "streak.cfg"));
        }

        public static StreakRepository CreateDefault()
        {
            return new StreakRepository(Application.persistentDataPath);
        }

        public StreakData Load()
        {
            Dictionary<string, object> document = _store.LoadConfig();
            if (document == null ||
                !document.TryGetValue("streak", out object section) ||
                section is not IReadOnlyDictionary<string, object> streak)
                return new StreakData();
            return StreakData.FromDictionary(streak);
        }

        public bool Save(StreakData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return _store.SaveConfig(new Dictionary<string, object>
            {
                ["streak"] = data.ToDictionary()
            });
        }

        public void Reset()
        {
            _store.Remove();
        }
    }
}
