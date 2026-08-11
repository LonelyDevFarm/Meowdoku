using System;
using System.Collections.Generic;
using Meowdoku.Core.Config;
using UnityEngine;

namespace Meowdoku.Tests.PlayMode
{
    [DisallowMultipleComponent]
    public sealed class PlayModeAbProvider : MonoBehaviour, IAbRuntimeProvider
    {
        private readonly Dictionary<string, int> _ints = new();
        private readonly Dictionary<string, string> _strings = new();

        public event Action Initialized;
        public event Action RemoteReady;
        public event Action<string> ParamsUpdated;

        public bool IsInitialized => true;
        public bool IsRemoteReady => true;
        public long FirstOpenUnixMilliseconds => 0;

        public int GetInt(string key, int defaultValue) =>
            _ints.TryGetValue(key ?? string.Empty, out int value)
                ? value
                : defaultValue;

        public string GetString(string key, string defaultValue) =>
            _strings.TryGetValue(key ?? string.Empty, out string value)
                ? value
                : defaultValue;

        public void Dye(string key)
        {
        }

        public void SetInt(string key, int value)
        {
            if (!string.IsNullOrEmpty(key)) _ints[key] = value;
        }

        public void SetString(string key, string value)
        {
            if (!string.IsNullOrEmpty(key))
                _strings[key] = value ?? string.Empty;
        }

        public void NotifyInitialized() => Initialized?.Invoke();
        public void NotifyRemoteReady() => RemoteReady?.Invoke();
        public void NotifyParamsUpdated(string updateType) =>
            ParamsUpdated?.Invoke(updateType ?? string.Empty);
    }
}
