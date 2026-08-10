using Meowdoku.Core.Localization;
using Meowdoku.Core.UI;
using Meowdoku.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Meowdoku.Tests.EditMode
{
    public sealed class LocalizationTests
    {
        private const string FixtureCsv =
            "key,en,vi,zh_CN,zh_TW\n" +
            "HELLO,Hello,Xin chào,你好,你好\n" +
            "COMMA,\"Hello, cat\",\"Chào, mèo\",猫咪,貓咪\n" +
            "MULTILINE,\"First\nSecond\",\"Một\nHai\",一二,一二\n" +
            "LEVEL,Level %d,Màn %d,第 %d 关,第 %d 關\n" +
            "FALLBACK,English only,,,,\n";

        [TestCase("tl", "tl_PH", "fil")]
        [TestCase("in", "in_ID", "id")]
        [TestCase("iw", "iw_IL", "he")]
        [TestCase("no", "no_NO", "nb")]
        [TestCase("vi", "vi-VN", "vi_VN")]
        [TestCase("eo", "eo_EO", "en")]
        public void ResolveSystemLocale_MatchesSourceAliasesAndFallback(
            string language,
            string fullLocale,
            string expected)
        {
            Assert.That(
                LocalizationLocaleContract.ResolveSystemLocale(
                    language,
                    fullLocale),
                Is.EqualTo(expected));
        }

        [TestCase("zh-Hant-TW", "zh_TW")]
        [TestCase("zh_HK", "zh_TW")]
        [TestCase("zh-Hans-CN", "zh_CN")]
        [TestCase("zh", "zh_CN")]
        public void CanonicalizeChinese_MatchesSourceVariantPolicy(
            string locale,
            string expected)
        {
            Assert.That(
                LocalizationLocaleContract.CanonicalizeChinese(locale),
                Is.EqualTo(expected));
        }

        [Test]
        public void Catalog_ParsesQuotedCommaNewlineAndEnglishFallback()
        {
            LocalizationCatalog catalog = CreateFixtureCatalog();
            try
            {
                catalog.SetLocale("vi_VN");
                Assert.That(catalog.TranslationColumn, Is.EqualTo("vi"));
                Assert.That(catalog.Translate("HELLO"), Is.EqualTo("Xin chào"));
                Assert.That(catalog.Translate("COMMA"), Is.EqualTo("Chào, mèo"));
                Assert.That(catalog.Translate("MULTILINE"), Is.EqualTo("Một\nHai"));
                Assert.That(catalog.Translate("FALLBACK"), Is.EqualTo("English only"));
                Assert.That(catalog.Translate("UNKNOWN"), Is.EqualTo("UNKNOWN"));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void Catalog_SelectsTraditionalAndSimplifiedChineseColumns()
        {
            LocalizationCatalog catalog = CreateFixtureCatalog();
            try
            {
                catalog.SetLocale("zh-Hant-HK");
                Assert.That(catalog.TranslationColumn, Is.EqualTo("zh_TW"));
                Assert.That(catalog.Translate("COMMA"), Is.EqualTo("貓咪"));

                catalog.SetLocale("zh_CN");
                Assert.That(catalog.TranslationColumn, Is.EqualTo("zh_CN"));
                Assert.That(catalog.Translate("COMMA"), Is.EqualTo("猫咪"));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void SourceTranslationAsset_IsCopiedAndProvidesVietnameseSettings()
        {
            TextAsset source = AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/_Project/Localization/translations.csv");
            Assert.That(source, Is.Not.Null);

            LocalizationCatalog catalog =
                ScriptableObject.CreateInstance<LocalizationCatalog>();
            try
            {
                catalog.ConfigureForTests(source);
                catalog.SetLocale("vi");
                Assert.That(catalog.Translate("SETTING_TITLE"), Is.EqualTo("Cài Đặt"));
                Assert.That(catalog.Translate("SETTING_SOUND_ON"), Is.EqualTo("Đã bật âm"));
                Assert.That(catalog.TranslationCount, Is.EqualTo(1645));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void LanguageDisplay_PutsSystemBaseLanguageFirst()
        {
            IReadOnlyList<LanguageOptionDefinition> display =
                LanguageSelectionContract.BuildDisplay("fr_CA");

            Assert.That(display.Count, Is.EqualTo(9));
            Assert.That(display[0].Locale, Is.EqualTo("fr"));
            Assert.That(display[1].Locale, Is.EqualTo("en"));
            Assert.That(display[8].Locale, Is.EqualTo("tr"));
        }

        [TestCase("zh_HK", "zh_TW", "繁體中文")]
        [TestCase("zh_CN", "zh_CN", "简体中文")]
        [TestCase("vi_VN", "vi_VN", "Tiếng Việt")]
        public void LanguageDisplay_AddsNonBaseSystemOptionBeforeNineDefaults(
            string system,
            string expectedLocale,
            string expectedNative)
        {
            IReadOnlyList<LanguageOptionDefinition> display =
                LanguageSelectionContract.BuildDisplay(system);

            Assert.That(display.Count, Is.EqualTo(10));
            Assert.That(display[0].Locale, Is.EqualTo(expectedLocale));
            Assert.That(display[0].NativeName, Is.EqualTo(expectedNative));
            Assert.That(display[1].Locale, Is.EqualTo("en"));
        }

        [Test]
        public void CurrentLanguage_UsesExactThenMainLocaleFallback()
        {
            IReadOnlyList<LanguageOptionDefinition> display =
                LanguageSelectionContract.BuildDisplay("vi_VN");

            Assert.That(LanguageSelectionContract.ResolveCurrentIndex(
                    display,
                    "",
                    "vi_VN"),
                Is.EqualTo(0));
            Assert.That(LanguageSelectionContract.ResolveCurrentIndex(
                    display,
                    "pt_BR",
                    "vi_VN"),
                Is.EqualTo(7));
            Assert.That(LanguageSelectionContract.IsTapWithinScrollTolerance(
                    100f,
                    106f),
                Is.True);
            Assert.That(LanguageSelectionContract.IsTapWithinScrollTolerance(
                    100f,
                    106.01f),
                Is.False);
        }

        [Test]
        public void DropdownAnimation_MatchesSourceResource()
        {
            Assert.That(LanguageSwitchWidget.OpenSeconds, Is.EqualTo(0.1f));
            Assert.That(LanguageSwitchWidget.FadeStepSeconds,
                Is.EqualTo(0.033333335f));
            Assert.That(LanguageSwitchWidget.PanelOpenHeight, Is.EqualTo(508f));
        }

        [Test]
        public void SourceBackedRegistry_ContainsInstalledR12Pages()
        {
            UIRegistry registry = AssetDatabase.LoadAssetAtPath<UIRegistry>(
                "Assets/_Project/Settings/UIRegistry.asset");
            Assert.That(registry, Is.Not.Null);
            Assert.That(registry.ValidateEntries(), Is.Empty);
            Assert.That(registry.TryGetPrefab(UiName.Home, out _), Is.True);
            Assert.That(registry.TryGetPrefab(UiName.Tutorial, out _), Is.True);
            Assert.That(registry.TryGetPrefab(UiName.Setting, out _), Is.True);
            Assert.That(registry.TryGetPrefab(UiName.Language, out _), Is.True);
            Assert.That(registry.TryGetPrefab(UiName.HowToPlay, out _), Is.True);
            Assert.That(registry.TryGetPrefab(UiName.HowToPlayPaged, out _),
                Is.True);
            Assert.That(registry.TryGetPrefab(UiName.Bank, out _), Is.True);
        }

        [Test]
        public void LocalizedText_ReplacesGodotPercentPlaceholders()
        {
            LocalizationCatalog catalog = CreateFixtureCatalog();
            GameObject target = new("LocalizedTextTest");
            try
            {
                catalog.SetLocale("vi");
                Text label = target.AddComponent<Text>();
                LocalizedText localized = target.AddComponent<LocalizedText>();
                localized.ConfigureForTests(
                    catalog,
                    label,
                    "LEVEL",
                    "Level %d",
                    null,
                    null);

                localized.SetArguments(42);

                Assert.That(label.text, Is.EqualTo("Màn 42"));
            }
            finally
            {
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(catalog);
            }
        }

        private static LocalizationCatalog CreateFixtureCatalog()
        {
            LocalizationCatalog catalog =
                ScriptableObject.CreateInstance<LocalizationCatalog>();
            catalog.ConfigureForTests(new TextAsset(FixtureCsv));
            return catalog;
        }
    }
}
