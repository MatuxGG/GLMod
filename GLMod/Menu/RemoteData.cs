using GLMod.Class;
using GLMod.Constants;
using System;
using System.Collections;

namespace GLMod.Menu
{
    /// <summary>
    /// Data of a menu tab fetched from the API for the connected account and the API theme of the current menu
    /// theme (none for Good Loss, "ranked" for Among Us Ranked), with a small cache: reloaded after
    /// MENU_REFRESH_SECONDS (MENU_RETRY_SECONDS after an error), on demand, or when the account or theme changes.
    /// </summary>
    internal sealed class RemoteData<T> where T : class
    {
        // (username, theme, onComplete, onError) -> coroutine
        private readonly Func<string, string, Action<T>, Action<string>, IEnumerator> _load;
        private string _key;

        public T Value { get; private set; }
        public string Error { get; private set; }
        public bool Loading { get; private set; }
        public DateTime LoadedAt { get; private set; } = DateTime.MinValue;

        public RemoteData(Func<string, string, Action<T>, Action<string>, IEnumerator> load)
        {
            _load = load ?? throw new ArgumentNullException(nameof(load));
        }

        private static string CurrentTheme => MenuUI.Theme.ApiTheme;

        private static string KeyOf(string username, string theme) => username + "\n" + (theme ?? "");

        /// <summary>
        /// Value of this account and the current theme (null while its first load is pending)
        /// </summary>
        public T ValueFor(string username) => KeyOf(username, CurrentTheme) == _key ? Value : null;

        public void Invalidate() => LoadedAt = DateTime.MinValue;

        /// <summary>
        /// Starts a load when the data is stale (or always with force), unless one is running
        /// </summary>
        public void Refresh(string username, bool force = false)
        {
            if (Loading || string.IsNullOrEmpty(username))
            {
                return;
            }

            string theme = CurrentTheme;
            double maxAge = Error != null ? GameConstants.MENU_RETRY_SECONDS : GameConstants.MENU_REFRESH_SECONDS;
            bool stale = KeyOf(username, theme) != _key || (DateTime.UtcNow - LoadedAt).TotalSeconds >= maxAge;
            if (!force && !stale)
            {
                return;
            }

            Loading = true;
            CoroutineRunner.Run(Load(username, theme));
        }

        private IEnumerator Load(string username, string theme)
        {
            try
            {
                T result = null;
                string error = null;
                yield return _load(username, theme, value => result = value, message => error = message);

                string key = KeyOf(username, theme);
                if (key != _key)
                {
                    Value = null;
                }
                _key = key;
                LoadedAt = DateTime.UtcNow;
                Error = error;
                if (error == null)
                {
                    Value = result;
                }
            }
            finally
            {
                Loading = false;
            }
        }
    }
}
