using GLMod.Class;
using GLMod.Services.Interfaces;
using GLMod.Constants;
using GLMod.GLEntities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GLMod.Services.Implementations
{
    /// <summary>
    /// Handles translations and language management
    /// </summary>
    public class TranslationService : ITranslationService
    {
        private List<GLLanguage> _languages;
        private string _currentLanguage;

        public List<GLLanguage> Languages => _languages;
        public string CurrentLanguage
        {
            get => _currentLanguage;
            set => _currentLanguage = value?.ToLower();
        }

        public TranslationService()
        {
            _languages = new List<GLLanguage>();
            _currentLanguage = GameConstants.DEFAULT_LANGUAGE;
        }

        public IEnumerator LoadTranslations()
        {
            string languagesURL = GameConstants.API_ENDPOINT + "/trans";
            string languagesJson = null;
            string error = null;
            bool done = false;

            // Load language list
            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    languagesJson = await HttpHelper.Client.GetStringAsync(languagesURL);
                }
                catch (Exception e)
                {
                    error = e.Message;
                }
                finally
                {
                    done = true;
                }
            });

            // Wait for loading to complete
            while (!done)
                yield return null;

            // Check for error
            if (error != null)
            {
                GLMod.log("Load translations error: " + error);
                yield break;
            }

            // Deserialize languages
            _languages = GLJson.Deserialize<List<GLLanguage>>(languagesJson);

            if (_languages == null || _languages.Count == 0)
            {
                GLMod.log("No languages loaded");
                yield break;
            }

            // Counter to track completed loads
            int totalLanguages = _languages.Count;
            int loadedLanguages = 0;

            // Launch all loading coroutines in parallel
            foreach (GLLanguage language in _languages)
            {
                CoroutineRunner.Run(LoadLanguageWithCallback(language, () => { loadedLanguages++; }));
            }

            // Wait for all languages to be loaded
            while (loadedLanguages < totalLanguages)
                yield return null;

            GLMod.log($"All {totalLanguages} languages loaded successfully.");
        }

        private IEnumerator LoadLanguageWithCallback(GLLanguage language, System.Action onComplete)
        {
            yield return language.load();
            onComplete?.Invoke();
        }

        public string Translate(string key)
        {
            if (_languages == null || _languages.Count == 0)
            {
                return key;
            }

            try
            {
                var currentLanguage = _languages.Find(l => l.code == _currentLanguage);
                if (currentLanguage == null || currentLanguage.translations == null)
                {
                    return key;
                }

                var translation = currentLanguage.translations.Find(t => t.original == key);
                return translation?.translation ?? key;
            }
            catch (Exception e)
            {
                GLMod.log($"[Translate] Error: {e.Message}");
                return key;
            }
        }

        public bool SetLanguage(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
            {
                return false;
            }

            string lowerCode = languageCode.ToLower();

            // Check if language exists
            if (_languages != null && _languages.Any(l => l.code == lowerCode))
            {
                _currentLanguage = lowerCode;
                return true;
            }

            return false;
        }

        public string GetLanguageName(string code)
        {
            if (_languages == null || string.IsNullOrEmpty(code))
            {
                return null;
            }

            try
            {
                return _languages.Find(l => l.code == code)?.name;
            }
            catch (Exception e)
            {
                GLMod.log($"[GetLanguageName] Error: {e.Message}");
                return null;
            }
        }

        public string GetLanguageCode(string name)
        {
            if (_languages == null || string.IsNullOrEmpty(name))
            {
                return null;
            }

            try
            {
                return _languages.Find(l => l.name == name)?.code;
            }
            catch (Exception e)
            {
                GLMod.log($"[GetLanguageCode] Error: {e.Message}");
                return null;
            }
        }
    }
}
