using GLMod.GLEntities;
using System.Collections;
using System.Collections.Generic;

namespace GLMod.Services.Interfaces
{
    /// <summary>
    /// Interface for managing translations and languages
    /// </summary>
    public interface ITranslationService
    {
        /// <summary>
        /// Gets the list of available languages
        /// </summary>
        List<GLLanguage> Languages { get; }

        /// <summary>
        /// Gets or sets the current language code
        /// </summary>
        string CurrentLanguage { get; set; }

        /// <summary>
        /// Loads all available translations from the API
        /// </summary>
        /// <returns>Coroutine</returns>
        IEnumerator LoadTranslations();

        /// <summary>
        /// Translates a given key to the current language
        /// </summary>
        /// <param name="key">Translation key</param>
        /// <returns>Translated string or original key if not found</returns>
        string Translate(string key);

        /// <summary>
        /// Sets the current language
        /// </summary>
        /// <param name="languageCode">Language code (e.g., "en", "fr")</param>
        /// <returns>True if language was set successfully</returns>
        bool SetLanguage(string languageCode);

        /// <summary>
        /// Gets the language name from a language code
        /// </summary>
        /// <param name="code">Language code</param>
        /// <returns>Language name</returns>
        string GetLanguageName(string code);

        /// <summary>
        /// Gets the language code from a language name
        /// </summary>
        /// <param name="name">Language name</param>
        /// <returns>Language code</returns>
        string GetLanguageCode(string name);
    }
}
