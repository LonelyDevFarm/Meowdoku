using UnityEngine;
using UnityEngine.UI;
using System;
using System.Globalization;

namespace Meowdoku.Core.Localization
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField] private LocalizationCatalog catalog;
        [SerializeField] private Text target;
        [SerializeField] private string key;
        [SerializeField] private string fallbackText;
        [SerializeField] private Font primaryFont;
        [SerializeField] private Font eastAsianFallbackFont;

        private object[] _arguments;

        private void Awake()
        {
            if (target == null) target = GetComponent<Text>();
        }

        private void OnEnable()
        {
            if (catalog != null) catalog.LocaleChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (catalog != null) catalog.LocaleChanged -= Refresh;
        }

        public void Bind(LocalizationCatalog value)
        {
            if (catalog == value) return;
            if (isActiveAndEnabled && catalog != null)
                catalog.LocaleChanged -= Refresh;
            catalog = value;
            if (isActiveAndEnabled && catalog != null)
                catalog.LocaleChanged += Refresh;
            Refresh();
        }

        public void SetKey(string value)
        {
            key = value ?? string.Empty;
            Refresh();
        }

        public void SetArguments(params object[] values)
        {
            _arguments = values;
            Refresh();
        }

        public void Refresh()
        {
            if (target == null) return;
            string translated = catalog != null
                ? catalog.Translate(key)
                : fallbackText;
            string value = string.IsNullOrEmpty(translated)
                ? key
                : translated;
            target.text = ApplyArguments(value, _arguments);

            if (primaryFont == null) primaryFont = target.font;
            target.font = catalog != null && eastAsianFallbackFont != null &&
                          LocalizationLocaleContract.UsesEastAsianFallback(
                              catalog.Locale)
                ? eastAsianFallbackFont
                : primaryFont;
        }

        private static string ApplyArguments(string format, object[] arguments)
        {
            if (string.IsNullOrEmpty(format) || arguments == null ||
                arguments.Length == 0)
                return format;

            string value = format;
            foreach (object argument in arguments)
            {
                int stringToken = value.IndexOf("%s", StringComparison.Ordinal);
                int numberToken = value.IndexOf("%d", StringComparison.Ordinal);
                int token;
                if (stringToken < 0) token = numberToken;
                else if (numberToken < 0) token = stringToken;
                else token = Math.Min(stringToken, numberToken);
                if (token < 0) break;
                string replacement = Convert.ToString(
                    argument,
                    CultureInfo.InvariantCulture) ?? string.Empty;
                value = value.Remove(token, 2).Insert(token, replacement);
            }
            return value;
        }

        internal void ConfigureForTests(
            LocalizationCatalog value,
            Text text,
            string translationKey,
            string fallback,
            Font primary,
            Font eastAsian)
        {
            catalog = value;
            target = text;
            key = translationKey;
            fallbackText = fallback;
            primaryFont = primary;
            eastAsianFallbackFont = eastAsian;
        }
    }
}
