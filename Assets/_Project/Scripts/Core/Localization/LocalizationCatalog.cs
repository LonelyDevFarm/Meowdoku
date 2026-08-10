using System;
using System.Collections.Generic;
using UnityEngine;

namespace Meowdoku.Core.Localization
{
    [CreateAssetMenu(
        fileName = "LocalizationCatalog",
        menuName = "Meowdoku/Localization/Catalog")]
    public sealed class LocalizationCatalog : ScriptableObject
    {
        [SerializeField] private TextAsset translationsCsv;

        private readonly Dictionary<string, string> _translations =
            new(StringComparer.Ordinal);
        private string _locale = LocalizationLocaleContract.FallbackLocale;
        private string _translationColumn =
            LocalizationLocaleContract.FallbackLocale;
        private bool _loaded;

        public event Action LocaleChanged;

        public string Locale => _locale;
        public string TranslationColumn => _translationColumn;
        public bool IsLoaded => _loaded;
        public int TranslationCount => _translations.Count;

        public void ApplySystemLocale(
            GameStateService gameState,
            bool languageSwitchEnabled)
        {
            ApplySystemLocale(
                gameState,
                languageSwitchEnabled,
                LocalizationLocaleContract.ResolveCurrentSystemLocale());
        }

        public void ApplySystemLocale(
            GameStateService gameState,
            bool languageSwitchEnabled,
            string resolvedSystemLocale)
        {
            string applied = gameState?.AppliedLocale;
            string locale = languageSwitchEnabled &&
                            !string.IsNullOrWhiteSpace(applied)
                ? applied
                : resolvedSystemLocale;
            SetLocale(locale);
        }

        public bool SetLocale(string locale)
        {
            string normalized = LocalizationLocaleContract.NormalizeLocale(locale);
            if (string.IsNullOrEmpty(normalized))
                normalized = LocalizationLocaleContract.FallbackLocale;
            if (_loaded && string.Equals(
                    _locale,
                    normalized,
                    StringComparison.OrdinalIgnoreCase))
                return false;

            BuildDictionary(normalized);
            _locale = normalized;
            LocaleChanged?.Invoke();
            return true;
        }

        public string Translate(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            if (!_loaded)
                BuildDictionary(_locale);
            return _translations.TryGetValue(key, out string translated) &&
                   !string.IsNullOrEmpty(translated)
                ? translated
                : key;
        }

        internal void ConfigureForTests(TextAsset csv)
        {
            translationsCsv = csv;
            _loaded = false;
            _translations.Clear();
        }

        private void OnEnable()
        {
            _loaded = false;
            _translations.Clear();
        }

        private void OnValidate()
        {
            _loaded = false;
            _translations.Clear();
        }

        private void BuildDictionary(string locale)
        {
            _translations.Clear();
            _translationColumn = LocalizationLocaleContract.FallbackLocale;
            _loaded = true;
            if (translationsCsv == null ||
                string.IsNullOrEmpty(translationsCsv.text))
                return;

            using IEnumerator<string[]> rows =
                LocalizationCsvReader.ReadRows(translationsCsv.text)
                    .GetEnumerator();
            if (!rows.MoveNext()) return;
            string[] headers = rows.Current;
            if (headers.Length == 0) return;
            headers[0] = headers[0].TrimStart('\uFEFF');

            int keyColumn = FindHeader(headers, "key");
            int englishColumn = FindHeader(
                headers,
                LocalizationLocaleContract.FallbackLocale);
            int selectedColumn =
                LocalizationLocaleContract.ResolveTranslationColumn(
                    headers,
                    locale);
            if (keyColumn < 0 || selectedColumn < 0) return;
            _translationColumn = headers[selectedColumn];

            while (rows.MoveNext())
            {
                string[] row = rows.Current;
                string key = ValueAt(row, keyColumn);
                if (string.IsNullOrEmpty(key)) continue;
                string value = ValueAt(row, selectedColumn);
                if (string.IsNullOrEmpty(value))
                    value = ValueAt(row, englishColumn);
                _translations[key] = string.IsNullOrEmpty(value) ? key : value;
            }
        }

        private static int FindHeader(
            IReadOnlyList<string> headers,
            string expected)
        {
            for (int index = 0; index < headers.Count; index++)
            {
                if (string.Equals(
                        headers[index].TrimStart('\uFEFF'),
                        expected,
                        StringComparison.OrdinalIgnoreCase))
                    return index;
            }
            return -1;
        }

        private static string ValueAt(IReadOnlyList<string> row, int index)
        {
            return row != null && index >= 0 && index < row.Count
                ? row[index]
                : string.Empty;
        }
    }
}
