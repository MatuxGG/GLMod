using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace GLMod.Class
{
    /// <summary>
    /// Translated texts by language code and key, with fallbacks. Pure logic: no IO, no logging.
    /// </summary>
    public class TranslationTable
    {
        /// <summary>
        /// Language used when the game language is unknown or a text is missing.
        /// </summary>
        public const string DefaultLanguage = "en";

        private readonly Dictionary<string, Dictionary<string, string>> _texts =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Languages that have at least one text
        /// </summary>
        public IEnumerable<string> Languages => _texts.Keys;

        /// <summary>
        /// Adds (or replaces) texts of a language
        /// </summary>
        public void Add(string language, IEnumerable<KeyValuePair<string, string>> texts)
        {
            if (string.IsNullOrWhiteSpace(language) || texts == null)
            {
                return;
            }

            if (!_texts.TryGetValue(language, out Dictionary<string, string> table))
            {
                table = new Dictionary<string, string>(StringComparer.Ordinal);
                _texts[language] = table;
            }
            foreach (KeyValuePair<string, string> text in texts)
            {
                if (!string.IsNullOrEmpty(text.Key) && text.Value != null)
                {
                    table[text.Key] = text.Value;
                }
            }
        }

        /// <summary>
        /// Adds the texts of a flat JSON object ({"key": "text"}); false when the JSON is invalid
        /// </summary>
        public bool AddJson(string language, string json)
        {
            try
            {
                var texts = JsonSerializer.Deserialize<Dictionary<string, string>>(json ?? "");
                if (texts == null)
                {
                    return false;
                }
                Add(language, texts);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        /// <summary>
        /// Text of a key: in the language, then its base language ("pt-BR" -> "pt"), then English;
        /// null when no language has it
        /// </summary>
        public string Find(string language, string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            foreach (string candidate in FallbackChain(language))
            {
                if (_texts.TryGetValue(candidate, out Dictionary<string, string> table)
                    && table.TryGetValue(key, out string text))
                {
                    return text;
                }
            }
            return null;
        }

        /// <summary>
        /// Text of a key with {0}, {1}... replaced by the arguments; the key itself when it has no text
        /// </summary>
        public string Get(string language, string key, params object[] args)
        {
            string text = Find(language, key) ?? key ?? "";
            if (args == null || args.Length == 0)
            {
                return text;
            }

            try
            {
                return string.Format(CultureInfo.InvariantCulture, text, args);
            }
            catch (FormatException)
            {
                return text;
            }
        }

        /// <summary>
        /// Languages to try, in order: "pt-BR" -> pt-BR, pt, en
        /// </summary>
        public static IEnumerable<string> FallbackChain(string language)
        {
            var chain = new List<string>();
            if (!string.IsNullOrWhiteSpace(language))
            {
                chain.Add(language);
                int separator = language.IndexOf('-');
                if (separator > 0)
                {
                    chain.Add(language.Substring(0, separator));
                }
            }
            if (!chain.Contains(DefaultLanguage))
            {
                chain.Add(DefaultLanguage);
            }
            return chain;
        }
    }
}
