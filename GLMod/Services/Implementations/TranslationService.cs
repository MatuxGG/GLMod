using BepInEx.Logging;
using GLMod.Class;
using GLMod.Constants;
using GLMod.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace GLMod.Services.Implementations
{
    /// <summary>
    /// Translations of the Good Loss menu: embedded JSON files (GLMod/Resources/Translations/&lt;language&gt;.json)
    /// plus texts added by mods, in the language selected in the game
    /// </summary>
    public class TranslationService : ITranslationService
    {
        private readonly ManualLogSource _logger;
        private readonly Func<string> _languageProvider;
        private readonly TranslationTable _table = new TranslationTable();

        public string Language { get; private set; } = TranslationTable.DefaultLanguage;

        /// <param name="logger">Logger</param>
        /// <param name="languageProvider">Returns the game language code, or null when unknown</param>
        public TranslationService(ManualLogSource logger, Func<string> languageProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _languageProvider = languageProvider ?? throw new ArgumentNullException(nameof(languageProvider));
            LoadEmbeddedTranslations();
        }

        private void Log(string message) => ServiceLogger.Log(_logger, nameof(TranslationService), message);

        public string Get(string key) => _table.Get(Language, key);

        public string Get(string key, params object[] args) => _table.Get(Language, key, args);

        public bool Has(string key) => _table.Find(Language, key) != null;

        public void AddTranslations(string language, IReadOnlyDictionary<string, string> texts)
        {
            _table.Add(language, texts);
        }

        public void RefreshLanguage()
        {
            string language = null;
            try
            {
                language = _languageProvider();
            }
            catch (Exception e)
            {
                Log("[RefreshLanguage] " + e.Message);
            }

            language = string.IsNullOrEmpty(language) ? TranslationTable.DefaultLanguage : language;
            if (language != Language)
            {
                Language = language;
                Log("Menu language: " + language);
            }
        }

        private void LoadEmbeddedTranslations()
        {
            Assembly assembly = typeof(TranslationService).Assembly;
            foreach (string resource in assembly.GetManifestResourceNames())
            {
                if (!resource.StartsWith(GameConstants.TRANSLATION_RESOURCE_PREFIX, StringComparison.Ordinal)
                    || !resource.EndsWith(".json", StringComparison.Ordinal))
                {
                    continue;
                }

                string language = resource.Substring(GameConstants.TRANSLATION_RESOURCE_PREFIX.Length,
                    resource.Length - GameConstants.TRANSLATION_RESOURCE_PREFIX.Length - ".json".Length);
                try
                {
                    using Stream stream = assembly.GetManifestResourceStream(resource);
                    using var reader = new StreamReader(stream);
                    if (!_table.AddJson(language, reader.ReadToEnd()))
                    {
                        Log("[Load] Invalid translation file: " + resource);
                    }
                }
                catch (Exception e)
                {
                    Log("[Load] " + resource + ": " + e.Message);
                }
            }
        }
    }
}
