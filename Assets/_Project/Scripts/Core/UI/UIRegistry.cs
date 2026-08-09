using System;
using System.Collections.Generic;
using UnityEngine;

namespace Meowdoku.Core.UI
{
    [Serializable]
    public sealed class UIRegistryEntry
    {
        [SerializeField] private UiName name;
        [SerializeField] private UIFrameWindow prefab;

        public UiName Name => name;
        public UIFrameWindow Prefab => prefab;

        internal UIRegistryEntry(UiName name, UIFrameWindow prefab)
        {
            this.name = name;
            this.prefab = prefab;
        }
    }

    [CreateAssetMenu(
        fileName = "UIRegistry",
        menuName = "Meowdoku/UI/UI Registry")]
    public sealed class UIRegistry : ScriptableObject
    {
        [SerializeField] private List<UIRegistryEntry> entries = new();

        private readonly Dictionary<UiName, UIFrameWindow> _lookup = new();
        private bool _built;

        public bool TryGetPrefab(UiName name, out UIFrameWindow prefab)
        {
            EnsureLookup();
            return _lookup.TryGetValue(name, out prefab) && prefab != null;
        }

        public IReadOnlyList<string> ValidateEntries()
        {
            var errors = new List<string>();
            var names = new HashSet<UiName>();
            for (int index = 0; index < entries.Count; index++)
            {
                UIRegistryEntry entry = entries[index];
                if (entry == null)
                {
                    errors.Add($"Entry {index} is null.");
                    continue;
                }

                if (!names.Add(entry.Name))
                    errors.Add($"Duplicate UI name: {entry.Name}.");
                if (entry.Prefab == null)
                    errors.Add($"Missing prefab: {entry.Name}.");
            }

            return errors;
        }

        private void OnEnable()
        {
            _built = false;
        }

        private void OnValidate()
        {
            _built = false;
        }

        private void EnsureLookup()
        {
            if (_built) return;
            _lookup.Clear();
            foreach (UIRegistryEntry entry in entries)
            {
                if (entry == null || entry.Prefab == null ||
                    _lookup.ContainsKey(entry.Name))
                    continue;
                _lookup.Add(entry.Name, entry.Prefab);
            }

            _built = true;
        }

        internal void SetEntriesForTests(params UIRegistryEntry[] testEntries)
        {
            entries.Clear();
            entries.AddRange(testEntries);
            _built = false;
        }
    }
}
