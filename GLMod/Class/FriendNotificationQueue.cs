using System.Collections.Generic;

namespace GLMod.Class
{
    /// <summary>
    /// Friend notifications waiting to be shown, one at a time. Nothing appears during a game: requests
    /// wait for the lobby (the badge of the menu counts them meanwhile), connection alerts are dropped
    /// by the caller. Pure.
    /// </summary>
    public sealed class FriendNotificationQueue
    {
        private readonly Queue<(string key, string name)> _waiting = new Queue<(string key, string name)>();
        private (string key, string name)? _shown;
        private float _shownUntil;

        /// <summary>
        /// Adds a notification: a translation key and the name it mentions.
        /// </summary>
        public void Enqueue(string key, string name)
        {
            _waiting.Enqueue((key, name));
        }

        public int Count => _waiting.Count + (_shown.HasValue ? 1 : 0);

        /// <summary>
        /// The notification to display now, null when there is none or during a game.
        /// </summary>
        public (string key, string name)? Current(float now, float durationSeconds, bool inGame)
        {
            if (inGame)
            {
                return null;
            }

            if (_shown.HasValue && now >= _shownUntil)
            {
                _shown = null;
            }
            if (!_shown.HasValue && _waiting.Count > 0)
            {
                _shown = _waiting.Dequeue();
                _shownUntil = now + durationSeconds;
            }
            return _shown;
        }
    }
}
