using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using GLMod.Class;
using GLMod.Constants;
using Xunit;

namespace GLMod.Tests
{
    public class TranslationTableTests
    {
        private static TranslationTable Table()
        {
            var table = new TranslationTable();
            table.Add("en", new Dictionary<string, string> { { "hello", "Hello" }, { "games", "{0} GAMES" }, { "only.en", "English only" } });
            table.Add("fr", new Dictionary<string, string> { { "hello", "Bonjour" }, { "games", "{0} PARTIES" } });
            table.Add("pt", new Dictionary<string, string> { { "hello", "Olá" } });
            return table;
        }

        [Fact]
        public void Get_UsesTheLanguage()
        {
            Assert.Equal("Bonjour", Table().Get("fr", "hello"));
            Assert.Equal("Hello", Table().Get("en", "hello"));
        }

        [Fact]
        public void Get_FallsBackToBaseLanguageThenEnglishThenKey()
        {
            TranslationTable table = Table();

            Assert.Equal("Olá", table.Get("pt-BR", "hello"));
            Assert.Equal("English only", table.Get("fr", "only.en"));
            Assert.Equal("Hello", table.Get("ko", "hello"));
            Assert.Equal("Hello", table.Get(null, "hello"));
            Assert.Equal("missing.key", table.Get("fr", "missing.key"));
            Assert.Null(table.Find("fr", "missing.key"));
        }

        [Fact]
        public void Get_FormatsArguments()
        {
            Assert.Equal("8 PARTIES", Table().Get("fr", "games", 8));
            Assert.Equal("8 GAMES", Table().Get("de", "games", 8));
        }

        [Fact]
        public void Get_KeepsTextWhenFormatIsInvalid()
        {
            var table = new TranslationTable();
            table.Add("en", new Dictionary<string, string> { { "broken", "Value {1" } });

            Assert.Equal("Value {1", table.Get("en", "broken", 1));
        }

        [Fact]
        public void Add_ReplacesAndExtends()
        {
            TranslationTable table = Table();
            table.Add("fr", new Dictionary<string, string> { { "hello", "Salut" }, { "mymod.title", "Règles" } });

            Assert.Equal("Salut", table.Get("fr", "hello"));
            Assert.Equal("Règles", table.Get("fr", "mymod.title"));
            Assert.Equal("8 PARTIES", table.Get("fr", "games", 8));
        }

        [Fact]
        public void AddJson_RejectsInvalidJson()
        {
            var table = new TranslationTable();

            Assert.True(table.AddJson("en", "{\"a\":\"A\"}"));
            Assert.False(table.AddJson("en", "{not json"));
            Assert.False(table.AddJson("en", "[1]"));
            Assert.Equal("A", table.Get("en", "a"));
        }

        [Fact]
        public void FallbackChain_EndsWithEnglish()
        {
            Assert.Equal(new[] { "es-419", "es", "en" }, TranslationTable.FallbackChain("es-419"));
            Assert.Equal(new[] { "fr", "en" }, TranslationTable.FallbackChain("fr"));
            Assert.Equal(new[] { "en" }, TranslationTable.FallbackChain("en"));
            Assert.Equal(new[] { "en" }, TranslationTable.FallbackChain(""));
        }
    }

    public class GameLanguageTests
    {
        [Theory]
        [InlineData("English", "en")]
        [InlineData("French", "fr")]
        [InlineData("Brazilian", "pt-BR")]
        [InlineData("Latam", "es-419")]
        [InlineData("SChinese", "zh-Hans")]
        [InlineData("Irish", "ga")]
        [InlineData("Klingon", null)]
        [InlineData(null, null)]
        public void ToCode_MapsSupportedLangs(string supportedLang, string expected)
        {
            Assert.Equal(expected, GameLanguage.ToCode(supportedLang));
        }
    }

    public class EmbeddedTranslationTests
    {
        private static Dictionary<string, Dictionary<string, string>> LoadAll()
        {
            var assembly = typeof(TranslationTable).Assembly;
            var languages = new Dictionary<string, Dictionary<string, string>>();
            foreach (string resource in assembly.GetManifestResourceNames()
                .Where(r => r.StartsWith(GameConstants.TRANSLATION_RESOURCE_PREFIX) && r.EndsWith(".json")))
            {
                using Stream stream = assembly.GetManifestResourceStream(resource);
                using var reader = new StreamReader(stream);
                string language = resource.Substring(GameConstants.TRANSLATION_RESOURCE_PREFIX.Length).Replace(".json", "");
                languages[language] = JsonSerializer.Deserialize<Dictionary<string, string>>(reader.ReadToEnd());
            }
            return languages;
        }

        [Fact]
        public void EnglishAndFrenchAreEmbedded()
        {
            Dictionary<string, Dictionary<string, string>> languages = LoadAll();

            Assert.Contains("en", languages.Keys);
            Assert.Contains("fr", languages.Keys);
        }

        [Fact]
        public void EveryLanguageOnlyUsesEnglishKeys()
        {
            Dictionary<string, Dictionary<string, string>> languages = LoadAll();
            HashSet<string> english = languages["en"].Keys.ToHashSet();

            foreach (var language in languages.Where(l => l.Key != "en"))
            {
                Assert.Empty(language.Value.Keys.Where(key => !english.Contains(key)));
            }
        }

        [Fact]
        public void FrenchTranslatesEveryMenuText()
        {
            Dictionary<string, Dictionary<string, string>> languages = LoadAll();

            // Role names may stay in English when the official name is unknown
            var missing = languages["en"].Keys.Where(key => !key.StartsWith("role.") && !languages["fr"].ContainsKey(key));
            Assert.Empty(missing);
        }
    }
}
