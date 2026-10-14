using GLMod.Class;
using GLMod.Constants;
using System;
using System.Collections;

namespace GLMod.Menu
{
    /// <summary>
    /// Data of a menu tab fetched from the API for the connected account, with a small cache:
    /// reloaded after MENU_REFRESH_SECONDS (MENU_RETRY_SECONDS after an error) or on demand.
    /// </summary>
    internal sealed class RemoteData<T> where T : class
    {
        private readonly Func<string, Action<T>, Action<string>, IEnumerator> _load;
        private string _username;

        public T Value { get; private set; }
        public string Error { get; private set; }
        public bool Loading { get; private set; }
        public DateTime LoadedAt { get; private set; } = DateTime.MinValue;

        public RemoteData(Func<string, Action<T>, Action<string>, IEnumerator> load)
        {
            _load = load ?? throw new ArgumentNullException(nameof(load));
        }

        /// <summary>
        /// Value of this account (null while the first load of a new account is pending)
        /// </summary>
        public T ValueFor(string username) => username == _username ? Value : null;

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

            double maxAge = Error != null ? GameConstants.MENU_RETRY_SECONDS : GameConstants.MENU_REFRESH_SECONDS;
            bool stale = username != _username || (DateTime.UtcNow - LoadedAt).TotalSeconds >= maxAge;
            if (!force && !stale)
            {
                return;
            }

            Loading = true;
            CoroutineRunner.Run(Load(username));
        }

        private IEnumerator Load(string username)
        {
            try
            {
                T result = null;
                string error = null;
                yield return _load(username, value => result = value, message => error = message);

                if (username != _username)
                {
                    Value = null;
                }
                _username = username;
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
