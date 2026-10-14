using System.Collections.Generic;

namespace GLMod.Services.Interfaces
{
    /// <summary>
    /// Interface for the translations of the Good Loss menu, following the language selected in the game
    /// (English when it is unknown or a text is missing). Mods can add their own texts and languages.
    /// Must be used from the Unity main thread.
    /// </summary>
    public interface ITranslationService
    {
        /// <summary>
        /// Gets the current language code ("fr", "pt-BR"...; "en" when the game language is unknown)
        /// </summary>
        string Language { get; }

        /// <summary>
        /// Gets the text of a key in the current language (fallbacks: base language, English, then the key itself)
        /// </summary>
        /// <param name="key">Text key, e.g. "tab.account"</param>
        /// <returns>Translated text</returns>
        string Get(string key);

        /// <summary>
        /// Gets the text of a key with {0}, {1}... replaced by the arguments
        /// </summary>
        /// <param name="key">Text key</param>
        /// <param name="args">Values of the placeholders</param>
        /// <returns>Translated text</returns>
        string Get(string key, params object[] args);

        /// <summary>
        /// Gets whether a key has a text in the current language or one of its fallbacks
        /// </summary>
        /// <param name="key">Text key</param>
        /// <returns>True when translated</returns>
        bool Has(string key);

        /// <summary>
        /// Adds or replaces texts of a language (e.g. a mod's own keys, or a new language for GLMod's keys)
        /// </summary>
        /// <param name="language">Language code: "en", "fr", "es", "pt-BR"...</param>
        /// <param name="texts">Texts by key</param>
        void AddTranslations(string language, IReadOnlyDictionary<string, string> texts);

        /// <summary>
        /// Reads the language selected in the game again (called by the menu every frame)
        /// </summary>
        void RefreshLanguage();
    }
}
