using AmongUs.Data;
using System;
using System.Collections.Generic;

namespace GLMod.Class
{
    /// <summary>
    /// Language selected in the Among Us settings, as a language code ("fr", "pt-BR"...).
    /// </summary>
    public static class GameLanguage
    {
        // SupportedLangs names -> language codes
        private static readonly Dictionary<string, string> Codes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "English", "en" },
            { "Latam", "es-419" },
            { "Brazilian", "pt-BR" },
            { "Portuguese", "pt" },
            { "Korean", "ko" },
            { "Russian", "ru" },
            { "Dutch", "nl" },
            { "Filipino", "fil" },
            { "French", "fr" },
            { "German", "de" },
            { "Italian", "it" },
            { "Japanese", "ja" },
            { "Spanish", "es" },
            { "SChinese", "zh-Hans" },
            { "TChinese", "zh-Hant" },
            { "Irish", "ga" },
        };

        /// <summary>
        /// Code of a SupportedLangs name, or null when unknown. Pure.
        /// </summary>
        public static string ToCode(string supportedLangName)
        {
            return supportedLangName != null && Codes.TryGetValue(supportedLangName, out string code) ? code : null;
        }

        /// <summary>
        /// Code of the language selected in the game, or null when the settings are not loaded yet.
        /// Must be called from the Unity main thread.
        /// </summary>
        public static string CurrentCode()
        {
            try
            {
                return ToCode(DataManager.Settings?.Language?.CurrentLanguage.ToString());
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
