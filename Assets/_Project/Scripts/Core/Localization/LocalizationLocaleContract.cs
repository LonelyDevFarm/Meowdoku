using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Meowdoku.Core.Localization
{
    /// <summary>
    /// Locale policy ported from language_manager.gd. The methods that accept
    /// strings are deterministic so source aliases and fallbacks can be tested
    /// without depending on the editor machine's locale.
    /// </summary>
    public static class LocalizationLocaleContract
    {
        public const string FallbackLocale = "en";

        private static readonly HashSet<string> SupportedLanguages =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "en", "zh", "pt", "hi", "id", "in", "fil", "tl", "ru",
                "ja", "de", "fr", "ko", "es", "az", "be", "hr", "cs",
                "da", "ar", "fi", "el", "hu", "fa", "he", "iw", "it",
                "lt", "ms", "nl", "no", "nb", "pl", "ro", "sk", "sv",
                "th", "tr", "uk", "uz", "vi", "af", "am", "bn", "bs",
                "ca", "gu", "is", "kk", "km", "kn", "lo", "mk", "ml",
                "mn", "mr", "ne", "pa", "si", "sl", "sr", "sw", "ta",
                "te", "ur"
            };

        private static readonly Dictionary<string, string> SystemAliases =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["tl"] = "fil",
                ["in"] = "id",
                ["iw"] = "he",
                ["no"] = "nb"
            };

        public static string ResolveSystemLocale(
            string systemLanguage,
            string fullSystemLocale)
        {
            string language = MainLanguage(systemLanguage);
            if (!SupportedLanguages.Contains(language))
                return FallbackLocale;
            if (SystemAliases.TryGetValue(language, out string alias))
                return alias;

            string locale = NormalizeLocale(fullSystemLocale);
            return string.IsNullOrEmpty(locale) ? language : locale;
        }

        public static string ResolveCurrentSystemLocale()
        {
            string language = string.Empty;
            string locale = string.Empty;
            try
            {
                CultureInfo culture = CultureInfo.CurrentUICulture;
                language = culture.TwoLetterISOLanguageName;
                locale = culture.Name;
            }
            catch (CultureNotFoundException) { }

            if (string.IsNullOrEmpty(language))
                language = UnityLanguageCode(Application.systemLanguage);
            return ResolveSystemLocale(language, locale);
        }

        public static string NormalizeLocale(string locale)
        {
            return string.IsNullOrWhiteSpace(locale)
                ? string.Empty
                : locale.Trim().Replace('-', '_');
        }

        public static string MainLanguage(string locale)
        {
            string normalized = NormalizeLocale(locale);
            int separator = normalized.IndexOf('_');
            string main = separator > 0
                ? normalized.Substring(0, separator)
                : normalized;
            return main.ToLowerInvariant();
        }

        public static string CanonicalizeChinese(string locale)
        {
            string normalized = NormalizeLocale(locale);
            if (!string.Equals(MainLanguage(normalized), "zh",
                    StringComparison.OrdinalIgnoreCase))
                return normalized;

            bool traditional =
                normalized.IndexOf("_TW", StringComparison.OrdinalIgnoreCase) >= 0 ||
                normalized.IndexOf("_HK", StringComparison.OrdinalIgnoreCase) >= 0 ||
                normalized.IndexOf("_MO", StringComparison.OrdinalIgnoreCase) >= 0 ||
                normalized.IndexOf("Hant", StringComparison.OrdinalIgnoreCase) >= 0;
            return traditional ? "zh_TW" : "zh_CN";
        }

        public static bool UsesEastAsianFallback(string locale)
        {
            string main = MainLanguage(locale);
            return main == "zh" || main == "ja" || main == "ko";
        }

        internal static int ResolveTranslationColumn(
            IReadOnlyList<string> headers,
            string locale)
        {
            if (headers == null || headers.Count == 0) return -1;
            string normalized = NormalizeLocale(locale);
            int exact = IndexOfHeader(headers, normalized);
            if (exact >= 0) return exact;

            string main = MainLanguage(normalized);
            if (SystemAliases.TryGetValue(main, out string alias)) main = alias;
            if (main == "zh")
            {
                int chinese = IndexOfHeader(
                    headers,
                    CanonicalizeChinese(normalized));
                if (chinese >= 0) return chinese;
            }

            int language = IndexOfHeader(headers, main);
            return language >= 0
                ? language
                : IndexOfHeader(headers, FallbackLocale);
        }

        private static int IndexOfHeader(
            IReadOnlyList<string> headers,
            string expected)
        {
            for (int index = 0; index < headers.Count; index++)
            {
                if (string.Equals(
                        NormalizeLocale(headers[index]),
                        expected,
                        StringComparison.OrdinalIgnoreCase))
                    return index;
            }
            return -1;
        }

        private static string UnityLanguageCode(SystemLanguage language)
        {
            return language switch
            {
                SystemLanguage.Chinese or
                    SystemLanguage.ChineseSimplified or
                    SystemLanguage.ChineseTraditional => "zh",
                SystemLanguage.Portuguese => "pt",
                SystemLanguage.Indonesian => "id",
                SystemLanguage.Russian => "ru",
                SystemLanguage.Japanese => "ja",
                SystemLanguage.German => "de",
                SystemLanguage.French => "fr",
                SystemLanguage.Korean => "ko",
                SystemLanguage.Spanish => "es",
                SystemLanguage.Czech => "cs",
                SystemLanguage.Danish => "da",
                SystemLanguage.Arabic => "ar",
                SystemLanguage.Finnish => "fi",
                SystemLanguage.Greek => "el",
                SystemLanguage.Hungarian => "hu",
                SystemLanguage.Italian => "it",
                SystemLanguage.Dutch => "nl",
                SystemLanguage.Norwegian => "no",
                SystemLanguage.Polish => "pl",
                SystemLanguage.Romanian => "ro",
                SystemLanguage.Swedish => "sv",
                SystemLanguage.Thai => "th",
                SystemLanguage.Turkish => "tr",
                SystemLanguage.Ukrainian => "uk",
                SystemLanguage.Vietnamese => "vi",
                _ => language == SystemLanguage.English ? "en" : string.Empty
            };
        }
    }
}
