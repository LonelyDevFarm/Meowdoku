using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Meowdoku.Core.Profile
{
    public interface IProfileDataStore
    {
        ProfileData Load();
        bool Save(ProfileData data);
        void Reset();
    }

    /// <summary>
    /// Persistence adapter for source user://profile.cfg. The logical profile
    /// section and keys remain source-shaped while SaveStore provides the
    /// project's verified atomic Unity file format.
    /// </summary>
    public sealed class ProfileRepository : IProfileDataStore
    {
        internal const string SavePassword =
            "pf_q7K2mX9cV4nR8sL1wT6hB3zD5gY0";

        private readonly SaveStore _store;

        public ProfileRepository(string persistentDataPath)
        {
            if (string.IsNullOrEmpty(persistentDataPath))
                throw new ArgumentException(
                    "Persistent data path cannot be empty.",
                    nameof(persistentDataPath));
            _store = new SaveStore(
                SavePassword,
                persistentDataPath,
                false,
                Path.Combine(persistentDataPath, "profile.cfg"));
        }

        public static ProfileRepository CreateDefault() =>
            new(Application.persistentDataPath);

        public ProfileData Load()
        {
            Dictionary<string, object> document = _store.LoadConfig();
            if (document == null ||
                !document.TryGetValue("profile", out object section) ||
                section is not IReadOnlyDictionary<string, object> profile)
                return new ProfileData();
            if (profile.TryGetValue("data", out object raw) &&
                raw is IReadOnlyDictionary<string, object> sourceData)
                return ProfileData.FromDictionary(sourceData);

            // Compatibility for profiles written by the early Unity port,
            // before its logical ConfigFile schema matched profile.cfg.
            return ProfileData.FromDictionary(profile);
        }

        public bool Save(ProfileData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return _store.SaveConfig(new Dictionary<string, object>
            {
                ["profile"] = new Dictionary<string, object>
                {
                    ["data"] = data.ToDictionary()
                }
            });
        }

        public void Reset() => _store.Remove();
    }
}
