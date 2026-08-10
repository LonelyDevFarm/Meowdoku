using System;
using System.Collections.Generic;

namespace Meowdoku.Core.Localization
{
    public readonly struct LanguageOptionDefinition
    {
        public LanguageOptionDefinition(
            string locale,
            string nativeName,
            string translationKey)
        {
            Locale = locale;
            NativeName = nativeName;
            TranslationKey = translationKey;
        }

        public string Locale { get; }
        public string NativeName { get; }
        public string TranslationKey { get; }
    }

    /// <summary>
    /// Display ordering and selection policy ported from language_page.gd.
    /// </summary>
    public static class LanguageSelectionContract
    {
        public const int ScrollTapTolerance = 6;
        public const int MaximumVisibleOptions = 10;

        private static readonly LanguageOptionDefinition[] BaseOptions =
        {
            Option("en", "English"),
            Option("ja", "日本語"),
            Option("es", "Español"),
            Option("fr", "Français"),
            Option("de", "Deutsch"),
            Option("ru", "Русский"),
            Option("pt", "Português"),
            Option("ko", "한국어"),
            Option("tr", "Türkçe")
        };

        private static readonly Dictionary<string, string> NativeNames =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["en"] = "English", ["zh"] = "简体中文",
                ["pt"] = "Português", ["hi"] = "हिन्दी",
                ["id"] = "Bahasa Indonesia", ["fil"] = "Filipino",
                ["ru"] = "Русский", ["ja"] = "日本語",
                ["de"] = "Deutsch", ["fr"] = "Français",
                ["ko"] = "한국어", ["es"] = "Español",
                ["az"] = "Azərbaycan", ["be"] = "Беларуская",
                ["hr"] = "Hrvatski", ["cs"] = "Čeština",
                ["da"] = "Dansk", ["ar"] = "العربية",
                ["fi"] = "Suomi", ["el"] = "Ελληνικά",
                ["hu"] = "Magyar", ["fa"] = "فارسی",
                ["he"] = "עברית", ["it"] = "Italiano",
                ["lt"] = "Lietuvių", ["ms"] = "Bahasa Melayu",
                ["nl"] = "Nederlands", ["nb"] = "Norsk",
                ["pl"] = "Polski", ["ro"] = "Română",
                ["sk"] = "Slovenčina", ["sv"] = "Svenska",
                ["th"] = "ไทย", ["tr"] = "Türkçe",
                ["uk"] = "Українська", ["uz"] = "Oʻzbek",
                ["vi"] = "Tiếng Việt", ["af"] = "Afrikaans",
                ["am"] = "አማርኛ", ["bn"] = "বাংলা",
                ["bs"] = "Bosanski", ["ca"] = "Català",
                ["gu"] = "ગુજરાતી", ["is"] = "Íslenska",
                ["kk"] = "Қазақ", ["km"] = "ខ្មែរ",
                ["kn"] = "ಕನ್ನಡ", ["lo"] = "ລາວ",
                ["mk"] = "Македонски", ["ml"] = "മലയാളം",
                ["mn"] = "Монгол", ["mr"] = "मराठी",
                ["ne"] = "नेपाली", ["pa"] = "ਪੰਜਾਬੀ",
                ["si"] = "සිංහල", ["sl"] = "Slovenščina",
                ["sr"] = "Српски", ["sw"] = "Kiswahili",
                ["ta"] = "தமிழ்", ["te"] = "తెలుగు",
                ["ur"] = "اردو"
            };

        public static IReadOnlyList<LanguageOptionDefinition> BuildDisplay(
            string systemLocale)
        {
            string system =
                LocalizationLocaleContract.NormalizeLocale(systemLocale);
            string main = LocalizationLocaleContract.MainLanguage(system);
            int matched = FindBaseExact(system);
            if (matched < 0) matched = FindBaseMain(main);

            var display = new List<LanguageOptionDefinition>(
                MaximumVisibleOptions);
            if (matched >= 0)
            {
                display.Add(BaseOptions[matched]);
                for (int index = 0; index < BaseOptions.Length; index++)
                {
                    if (index != matched) display.Add(BaseOptions[index]);
                }
                return display;
            }

            if (main == "zh")
            {
                bool traditional = string.Equals(
                    LocalizationLocaleContract.CanonicalizeChinese(system),
                    "zh_TW",
                    StringComparison.OrdinalIgnoreCase);
                display.Add(new LanguageOptionDefinition(
                    traditional ? "zh_TW" : "zh_CN",
                    traditional ? "繁體中文" : NativeNames["zh"],
                    traditional ? "LANG_NAME_ZH_TW" : "LANG_NAME_ZH_CN"));
            }
            else
            {
                string locale = string.IsNullOrEmpty(system)
                    ? LocalizationLocaleContract.FallbackLocale
                    : system;
                string native = NativeNames.TryGetValue(main, out string value)
                    ? value
                    : locale;
                display.Add(new LanguageOptionDefinition(
                    locale,
                    native,
                    "LANG_NAME_" + main.ToUpperInvariant()));
            }

            display.AddRange(BaseOptions);
            return display;
        }

        public static int ResolveCurrentIndex(
            IReadOnlyList<LanguageOptionDefinition> display,
            string appliedLocale,
            string systemLocale)
        {
            if (display == null) return -1;
            string applied = string.IsNullOrWhiteSpace(appliedLocale)
                ? systemLocale
                : appliedLocale;
            applied = LocalizationLocaleContract.NormalizeLocale(applied);
            for (int index = 0; index < display.Count; index++)
            {
                if (string.Equals(
                        LocalizationLocaleContract.NormalizeLocale(
                            display[index].Locale),
                        applied,
                        StringComparison.OrdinalIgnoreCase))
                    return index;
            }

            string main = LocalizationLocaleContract.MainLanguage(applied);
            for (int index = 0; index < display.Count; index++)
            {
                if (LocalizationLocaleContract.MainLanguage(
                        display[index].Locale) == main)
                    return index;
            }
            return -1;
        }

        public static bool IsTapWithinScrollTolerance(
            float pressScrollY,
            float releaseScrollY)
        {
            return Math.Abs(releaseScrollY - pressScrollY) <=
                   ScrollTapTolerance;
        }

        public static string NativeNameOf(string locale)
        {
            string normalized =
                LocalizationLocaleContract.NormalizeLocale(locale);
            string main = LocalizationLocaleContract.MainLanguage(normalized);
            if (main == "zh")
            {
                return string.Equals(
                    LocalizationLocaleContract.CanonicalizeChinese(normalized),
                    "zh_TW",
                    StringComparison.OrdinalIgnoreCase)
                    ? "繁體中文"
                    : NativeNames["zh"];
            }
            return NativeNames.TryGetValue(main, out string native)
                ? native
                : normalized;
        }

        private static LanguageOptionDefinition Option(
            string locale,
            string native)
        {
            return new LanguageOptionDefinition(
                locale,
                native,
                "LANG_NAME_" + locale.ToUpperInvariant());
        }

        private static int FindBaseExact(string locale)
        {
            for (int index = 0; index < BaseOptions.Length; index++)
            {
                if (string.Equals(
                        BaseOptions[index].Locale,
                        locale,
                        StringComparison.OrdinalIgnoreCase))
                    return index;
            }
            return -1;
        }

        private static int FindBaseMain(string main)
        {
            for (int index = 0; index < BaseOptions.Length; index++)
            {
                if (LocalizationLocaleContract.MainLanguage(
                        BaseOptions[index].Locale) == main)
                    return index;
            }
            return -1;
        }
    }
}
